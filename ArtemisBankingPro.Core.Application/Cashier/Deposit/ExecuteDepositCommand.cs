using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Cashier.Deposit
{
    public record ExecuteDepositCommand(
        string CashierId,
        int AccountId,
        decimal Amount
    ) : IRequest<ExecuteDepositResult>;

    public record ExecuteDepositResult(bool Success, string Message = "");

    public class ExecuteDepositCommandHandler : IRequestHandler<ExecuteDepositCommand, ExecuteDepositResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<ExecuteDepositCommandHandler> _logger;

        public ExecuteDepositCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<ExecuteDepositCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ExecuteDepositResult> Handle(ExecuteDepositCommand request, CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
                return new ExecuteDepositResult(false, "El monto a depositar debe ser mayor que cero.");

            var accountRepo = _unitOfWork.Repository<CuentaAhorro>();
            var account = await accountRepo.GetByIdAsync(request.AccountId);

            if (account == null || account.Estado != EstadoCuenta.Activa)
                return new ExecuteDepositResult(false, "El número de cuenta ingresado no corresponde a una cuenta válida.");

            var now = _dateTimeProvider.UtcNow;
            string lastFour = account.NumeroCuenta.Length >= 4 ? account.NumeroCuenta[^4..] : account.NumeroCuenta;

            // Procesar depósito
            account.Balance += request.Amount;
            accountRepo.Update(account);

            var transactionRepo = _unitOfWork.Repository<Transaccion>();
            var transaction = new Transaccion
            {
                CuentaAhorroId = account.Id,
                Tipo = TipoTransaccion.Credito,
                TipoOperacion = TipoOperacion.Deposito,
                Monto = request.Amount,
                Origen = "DEPÓSITO",
                Destino = account.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now,
                RealizadoPorUserId = request.CashierId
            };

            await transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // Enviar correo de notificación (No bloqueante)
            try
            {
                var clientUser = await _identityService.GetUserDetailByIdAsync(account.ClienteId);
                if (clientUser != null && !string.IsNullOrEmpty(clientUser.Email))
                {
                    await _emailService.SendDepositEmailAsync(
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
                _logger.LogError(ex, "El depósito se realizó pero falló el envío del correo para la cuenta {AccountId}", account.Id);
                return new ExecuteDepositResult(true, "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
            }

            return new ExecuteDepositResult(true, "Depósito realizado correctamente.");
        }
    }
}