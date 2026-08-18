using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount
{
    public class CreateSavingsAccountCommandHandler : IRequestHandler<CreateSavingsAccountCommand, SavingsAccountResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IAccountNumberGenerator _accountNumberGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateSavingsAccountCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IAccountNumberGenerator accountNumberGenerator,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _accountNumberGenerator = accountNumberGenerator;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<SavingsAccountResponseDto> Handle(CreateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var client = await _identityService.GetUserDetailByIdAsync(request.ClientId);
            if (client == null || !client.IsActive)
                throw new BusinessRuleException("El cliente no existe o se encuentra inactivo.");

            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuentas = await cuentaRepo.FindAsync(c => c.ClienteId == request.ClientId && c.TipoCuenta == TipoCuenta.Principal);
            var cuentaPrincipal = cuentas.FirstOrDefault(c => c.Estado == EstadoCuenta.Activa);

            if (cuentaPrincipal == null)
                throw new BusinessRuleException("El cliente debe tener una cuenta de ahorro principal activa.");

            var accountNumber = await _accountNumberGenerator.GenerateUniqueAccountNumberAsync();

            var cuenta = new CuentaAhorro
            {
                NumeroCuenta = accountNumber,
                ClienteId = request.ClientId,
                Balance = request.InitialBalance,
                TipoCuenta = TipoCuenta.Secundaria,
                Estado = EstadoCuenta.Activa,
                FechaCreacion = _dateTimeProvider.UtcNow
            };

            await cuentaRepo.AddAsync(cuenta);
            await _unitOfWork.SaveChangesAsync();

            if (request.InitialBalance > 0)
            {
                var transaccion = new Transaccion
                {
                    CuentaAhorroId = cuenta.Id,
                    TipoOperacion = TipoOperacion.MontoInicial,
                    Tipo = TipoTransaccion.Credito,
                    Monto = request.InitialBalance,
                    Origen = "APERTURA DE CUENTA",
                    Destino = cuenta.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    Fecha = _dateTimeProvider.UtcNow,
                    RealizadoPorUserId = request.PerformedByUserId
                };

                await _unitOfWork.Repository<Transaccion>().AddAsync(transaccion);
            }

            return new SavingsAccountResponseDto(
                cuenta.Id.ToString(),
                cuenta.NumeroCuenta,
                request.ClientId,
                $"{client.FirstName} {client.LastName}",
                cuenta.Balance,
                cuenta.TipoCuenta.ToString(),
                cuenta.Estado.ToString(),
                cuenta.FechaCreacion
            );
        }
    }
}