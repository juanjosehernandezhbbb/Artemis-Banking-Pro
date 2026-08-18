using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate
{
    public class UpdateLoanRateCommandHandler : IRequestHandler<UpdateLoanRateCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly ILoanCalculatorService _loanCalculator;
        private readonly IEmailService _emailService;
        private readonly ILogger<UpdateLoanRateCommandHandler> _logger;

        public UpdateLoanRateCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            ILoanCalculatorService loanCalculator,
            IEmailService emailService,
            ILogger<UpdateLoanRateCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _loanCalculator = loanCalculator;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken)
        {
            var prestamoRepo = _unitOfWork.Repository<Prestamo>();
            var prestamo = await prestamoRepo.GetByIdAsync(request.Id);
            if (prestamo == null)
                throw new NotFoundException("Préstamo", request.Id);

            if (prestamo.Estado != EstadoPrestamo.Activo)
                throw new BusinessRuleException("Solo se puede modificar la tasa de interés de un préstamo activo.");

            var now = DateTime.UtcNow;
            var cuotaRepo = _unitOfWork.Repository<CuotaAmortizacion>();
            var allCuotas = (await cuotaRepo.FindAsync(c => c.PrestamoId == prestamo.Id))
                .OrderBy(c => c.NumeroCuota)
                .ToList();

            var futurePending = allCuotas
                .Where(c => c.FechaPago == null
                            && c.EstadoPago == EstadoCuota.Pendiente
                            && !c.Atrasada
                            && c.FechaVencimiento > now.Date)
                .ToList();

            if (futurePending.Count == 0)
                throw new BusinessRuleException("No existen cuotas futuras pendientes para recalcular.");

            var remainingCapital = futurePending.Sum(c => c.MontoCapital);
            var monthlyRate = (request.AnnualInterestRate / 100m) / 12m;
            var (newInstallment, _) = _loanCalculator.CalculateFrenchAmortization(
                remainingCapital, request.AnnualInterestRate, futurePending.Count);

            var remaining = remainingCapital;
            foreach (var cuota in futurePending)
            {
                var interest = remaining * monthlyRate;
                var capitalPart = newInstallment - interest;
                remaining -= capitalPart;

                cuota.ValorCuota = Math.Round(newInstallment, 2, MidpointRounding.AwayFromZero);
                cuota.MontoInteres = Math.Round(interest, 2, MidpointRounding.AwayFromZero);
                cuota.MontoCapital = Math.Round(capitalPart, 2, MidpointRounding.AwayFromZero);
                cuota.SaldoPendiente = Math.Round(remaining, 2, MidpointRounding.AwayFromZero);

                cuotaRepo.Update(cuota);
            }

            prestamo.TasaInteresAnual = request.AnnualInterestRate;
            prestamoRepo.Update(prestamo);

            var client = await _identityService.GetUserDetailByIdAsync(prestamo.ClienteId);
            if (client != null)
            {
                try
                {
                    await _emailService.SendLoanRateChangeEmailAsync(
                        client.Email, $"{client.FirstName} {client.LastName}", prestamo.NumeroPrestamo, request.AnnualInterestRate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falló el envío del correo de cambio de tasa del préstamo {LoanId}", prestamo.Id);
                }
            }
        }
    }
}