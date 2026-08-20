using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Cashier.Withdrawal
{
    public record ExecuteWithdrawalCommand(
        string CashierId,
        int AccountId,
        decimal Amount
    ) : IRequest<ExecuteWithdrawalResult>;

    public record ExecuteWithdrawalResult(bool Success, string Message = "");

    public class ExecuteWithdrawalCommandHandler : IRequestHandler<ExecuteWithdrawalCommand, ExecuteWithdrawalResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<ExecuteWithdrawalCommandHandler> _logger;

        public ExecuteWithdrawalCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<ExecuteWithdrawalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ExecuteWithdrawalResult> Handle(ExecuteWithdrawalCommand request, CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
                return new ExecuteWithdrawalResult(false, "El monto a retirar debe ser mayor que cero.");

            var accountRepo = _unitOfWork.Repository<CuentaAhorro>();
            var account = await accountRepo.GetByIdAsync(request.AccountId);

            if (account == null || account.Estado != EstadoCuenta.Activa)
                return new ExecuteWithdrawalResult(false, "El número de cuenta ingresado no corresponde a una cuenta válida.");

            var transactionRepo = _unitOfWork.Repository<Transaccion>();
            var now = _dateTimeProvider.UtcNow;
            string lastFour = account.NumeroCuenta.Length >= 4 ? account.NumeroCuenta[^4..] : account.NumeroCuenta;

            // Validación de fondos suficientes
            if (account.Balance < request.Amount)
            {
                // Registrar intento RECHAZADO sin modificar el balance
                var txRechazada = new Transaccion
                {
                    CuentaAhorroId = account.Id,
                    Tipo = TipoTransaccion.Debito,
                    TipoOperacion = TipoOperacion.Retiro,
                    Monto = request.Amount,
                    Origen = account.NumeroCuenta,
                    Destino = "RETIRO",
                    Estado = EstadoTransaccion.Rechazada,
                    MotivoRechazo = "El monto ingresado excede el saldo disponible de la cuenta.",
                    Fecha = now,
                    RealizadoPorUserId = request.CashierId
                };

                await transactionRepo.AddAsync(txRechazada);
                await _unitOfWork.SaveChangesAsync();

                return new ExecuteWithdrawalResult(false, "El monto ingresado excede el saldo disponible de la cuenta.");
            }

            // Procesar retiro exitoso
            account.Balance -= request.Amount;
            accountRepo.Update(account);

            var txAprobada = new Transaccion
            {
                CuentaAhorroId = account.Id,
                Tipo = TipoTransaccion.Debito,
                TipoOperacion = TipoOperacion.Retiro,
                Monto = request.Amount,
                Origen = account.NumeroCuenta,
                Destino = "RETIRO",
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now,
                RealizadoPorUserId = request.CashierId
            };

            await transactionRepo.AddAsync(txAprobada);
            await _unitOfWork.SaveChangesAsync();

            // Enviar correo de notificación (No bloqueante)
            try
            {
                var clientUser = await _identityService.GetUserDetailByIdAsync(account.ClienteId);
                if (clientUser != null && !string.IsNullOrEmpty(clientUser.Email))
                {
                    await _emailService.SendWithdrawalEmailAsync(
                        clientUser.Email,
                        $"{clientUser.FirstName} {clientUser.LastName}",
                        request.Amount,
                        lastFour,
                        now
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "El retiro se realizó pero falló el envío del correo para la cuenta {AccountId}", account.Id);
                return new ExecuteWithdrawalResult(true, "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
            }

            return new ExecuteWithdrawalResult(true, "Retiro realizado correctamente.");
        }
    }
}