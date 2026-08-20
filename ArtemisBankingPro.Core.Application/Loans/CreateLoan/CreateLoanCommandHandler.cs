using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Loans.CreateLoan
{
    public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanResponseDto>
    {
        private readonly IIdentityService _identityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountNumberGenerator _accountNumberGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILoanCalculatorService _loanCalculator;
        private readonly IEmailService _emailService;

        public CreateLoanCommandHandler(
            IIdentityService identityService,
            IUnitOfWork unitOfWork,
            IAccountNumberGenerator accountNumberGenerator,
            IDateTimeProvider dateTimeProvider,
            ILoanCalculatorService loanCalculator,
            IEmailService emailService)
        {
            _identityService = identityService;
            _unitOfWork = unitOfWork;
            _accountNumberGenerator = accountNumberGenerator;
            _dateTimeProvider = dateTimeProvider;
            _loanCalculator = loanCalculator;
            _emailService = emailService;
        }

        public async Task<LoanResponseDto> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
        {
            var client = await _identityService.GetUserDetailByIdAsync(request.ClientId);
            if (client == null || !client.IsActive)
                throw new BusinessRuleException("El cliente no existe o se encuentra inactivo.");

            var prestamoRepo = _unitOfWork.Repository<Prestamo>();
            var activeLoans = await prestamoRepo.FindAsync(p => p.ClienteId == request.ClientId && p.Estado == EstadoPrestamo.Activo);
            if (activeLoans.Any())
                throw new BusinessRuleException("El cliente ya posee un préstamo activo.");

            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuentas = await cuentaRepo.FindAsync(c => c.ClienteId == request.ClientId && c.TipoCuenta == TipoCuenta.Principal);
            var cuentaPrincipal = cuentas.FirstOrDefault(c => c.Estado == EstadoCuenta.Activa) ?? cuentas.FirstOrDefault();

            if (cuentaPrincipal == null || cuentaPrincipal.Estado != EstadoCuenta.Activa)
                throw new BusinessRuleException("El cliente debe tener una cuenta de ahorro principal activa.");

            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();
            var cuotaRepo = _unitOfWork.Repository<CuotaAmortizacion>();

            var allTarjetasDebt = await tarjetaRepo.FindAsync(t => t.MontoAdeudado > 0);
            var totalTarjetasDebt = allTarjetasDebt.Sum(t => t.MontoAdeudado);

            var allActiveLoans = await prestamoRepo.FindAsync(p => p.Estado == EstadoPrestamo.Activo);
            var activeLoanIds = allActiveLoans.Select(p => p.Id).ToList();
            var pendingCuotas = await cuotaRepo.FindAsync(c => activeLoanIds.Contains(c.PrestamoId) && c.FechaPago == null);
            var totalPrestamosDebt = pendingCuotas.Sum(c => c.ValorCuota);

            var totalSystemDebt = totalTarjetasDebt + totalPrestamosDebt;
            var activeClientsCount = await _identityService.GetActiveClientCountAsync();
            var averageSystemDebt = activeClientsCount > 0 ? totalSystemDebt / activeClientsCount : 0;

            var clientTarjetas = await tarjetaRepo.FindAsync(t => t.ClienteId == request.ClientId);
            var currentDebt = clientTarjetas.Sum(t => t.MontoAdeudado);

            var (monthlyInstallment, totalAmountToPay) = _loanCalculator.CalculateFrenchAmortization(request.CapitalAmount, request.AnnualInterestRate, request.TermInMonths);
            var projectedDebt = currentDebt + totalAmountToPay;

            // SOLUCIÓN COLD START: Si el sistema no tiene ninguna deuda registrada (es el primer préstamo),
            // permitimos la creación automáticamente sin bloquear por alto riesgo, 
            // ya que al ser el único, lógicamente supera el promedio de 0.
            bool isColdStartSystem = totalSystemDebt == 0 && !allActiveLoans.Any();

            if (!request.ConfirmHighRisk && !isColdStartSystem)
            {
                if (currentDebt > averageSystemDebt)
                    throw new HighRiskClientException("CurrentHighRisk", currentDebt, projectedDebt, averageSystemDebt);

                if (projectedDebt > averageSystemDebt)
                    throw new HighRiskClientException("ProjectedHighRisk", currentDebt, projectedDebt, averageSystemDebt);
            }

            var loanNumber = await _accountNumberGenerator.GenerateUniqueAccountNumberAsync();
            var prestamo = new Prestamo
            {
                NumeroPrestamo = loanNumber,
                ClienteId = request.ClientId,
                CapitalAprobado = request.CapitalAmount,
                TasaInteresAnual = request.AnnualInterestRate,
                PlazoMeses = request.TermInMonths,
                Estado = EstadoPrestamo.Activo,
                FechaCreacion = _dateTimeProvider.UtcNow,
                AdministradorId = request.PerformedByUserId
            };

            await prestamoRepo.AddAsync(prestamo);
            await _unitOfWork.SaveChangesAsync();

            var cuotas = _loanCalculator.GenerateAmortizationSchedule(prestamo.Id, request.CapitalAmount, request.AnnualInterestRate, request.TermInMonths);

            foreach (var cuota in cuotas)
            {
                await cuotaRepo.AddAsync(cuota);
            }

            await _unitOfWork.SaveChangesAsync();

            cuentaPrincipal.Balance += request.CapitalAmount;
            cuentaRepo.Update(cuentaPrincipal);

            var transaccionRepo = _unitOfWork.Repository<Transaccion>();
            var desembolso = new Transaccion
            {
                CuentaAhorroId = cuentaPrincipal.Id,
                TipoOperacion = TipoOperacion.DesembolsoPrestamo,
                Tipo = TipoTransaccion.Credito,
                Monto = request.CapitalAmount,
                Origen = prestamo.NumeroPrestamo,
                Destino = cuentaPrincipal.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = _dateTimeProvider.UtcNow,
                RealizadoPorUserId = request.PerformedByUserId
            };
            await transaccionRepo.AddAsync(desembolso);

            try
            {
                await _emailService.SendLoanApprovalEmailAsync(
                    client.Email,
                    client.FirstName,
                    request.CapitalAmount,
                    loanNumber,
                    request.TermInMonths,
                    monthlyInstallment
                );
            }
            catch (Exception)
            {
            }

            return new LoanResponseDto(
                            prestamo.Id.ToString(),
                            prestamo.NumeroPrestamo,
                            client.Id,
                            $"{client.FirstName} {client.LastName}", // <-- Corregido: comillas dobles de C#
                            prestamo.CapitalAprobado,
                            prestamo.PlazoMeses,
                            prestamo.TasaInteresAnual,
                            monthlyInstallment,
                            totalAmountToPay,
                            prestamo.Estado.ToString(),
                            prestamo.FechaCreacion
                        );
        }
    }
}