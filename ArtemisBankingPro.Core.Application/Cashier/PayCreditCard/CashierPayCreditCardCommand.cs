using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Cashier.PayCreditCard
{
    public record CashierPayCreditCardCommand(
        string CashierId,
        string SourceAccountNumber,
        string CardNumber,
        decimal Amount
    ) : IRequest<CashierPayResult>;

    public record CashierPayResult(bool Success, string Message = "");

    public class CashierPayCreditCardCommandHandler : IRequestHandler<CashierPayCreditCardCommand, CashierPayResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<CashierPayCreditCardCommandHandler> _logger;

        public CashierPayCreditCardCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<CashierPayCreditCardCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CashierPayResult> Handle(CashierPayCreditCardCommand request, CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
                return new CashierPayResult(false, "El monto a pagar debe ser mayor que cero.");

            if (string.IsNullOrWhiteSpace(request.CardNumber) || request.CardNumber.Length != 16)
                return new CashierPayResult(false, "El número de tarjeta debe contener 16 dígitos.");

            // 1. Buscar Cuenta Origen
            var accountRepo = _unitOfWork.Repository<CuentaAhorro>();
            var accounts = await accountRepo.FindAsync(c => c.NumeroCuenta == request.SourceAccountNumber && c.Estado == EstadoCuenta.Activa);
            var account = accounts.FirstOrDefault();

            if (account == null)
                return new CashierPayResult(false, "El número de cuenta ingresado no corresponde a una cuenta válida.");

            // 2. Buscar Tarjeta de Crédito Destino
            var cardRepo = _unitOfWork.Repository<TarjetaCredito>();
            var cards = await cardRepo.FindAsync(t => t.NumeroTarjeta == request.CardNumber && t.Estado == EstadoTarjeta.Activa);
            var card = cards.FirstOrDefault();

            if (card == null)
                return new CashierPayResult(false, "El número de tarjeta ingresado no corresponde a una tarjeta válida.");

            if (card.MontoAdeudado <= 0)
                return new CashierPayResult(false, "La tarjeta seleccionada no tiene deuda pendiente.");

            // Regla para evitar sobrepago: tomar el monto efectivo real
            decimal effectiveAmount = request.Amount > card.MontoAdeudado ? card.MontoAdeudado : request.Amount;

            var transactionRepo = _unitOfWork.Repository<Transaccion>();
            var now = _dateTimeProvider.UtcNow;
            string accountLastFour = account.NumeroCuenta.Length >= 4 ? account.NumeroCuenta[^4..] : account.NumeroCuenta;
            string cardLastFour = card.NumeroTarjeta.Length >= 4 ? card.NumeroTarjeta[^4..] : card.NumeroTarjeta;

            // 3. Validar fondos suficientes en cuenta origen
            if (account.Balance < effectiveAmount)
            {
                var rejectedTx = new Transaccion
                {
                    CuentaAhorroId = account.Id,
                    Tipo = TipoTransaccion.Debito,
                    TipoOperacion = TipoOperacion.PagoTarjetaCredito,
                    Monto = effectiveAmount,
                    Origen = account.NumeroCuenta,
                    Destino = $"****{cardLastFour}",
                    Estado = EstadoTransaccion.Rechazada,
                    MotivoRechazo = "El monto ingresado excede el saldo disponible de la cuenta.",
                    Fecha = now,
                    RealizadoPorUserId = request.CashierId
                };

                await transactionRepo.AddAsync(rejectedTx);
                await _unitOfWork.SaveChangesAsync();

                return new CashierPayResult(false, "El monto ingresado excede el saldo disponible de la cuenta.");
            }

            // 4. Procesar el pago
            account.Balance -= effectiveAmount;
            card.MontoAdeudado -= effectiveAmount;

            accountRepo.Update(account);
            cardRepo.Update(card);

            var approvedTx = new Transaccion
            {
                CuentaAhorroId = account.Id,
                Tipo = TipoTransaccion.Debito,
                TipoOperacion = TipoOperacion.PagoTarjetaCredito,
                Monto = effectiveAmount,
                Origen = account.NumeroCuenta,
                Destino = $"****{cardLastFour}",
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now,
                RealizadoPorUserId = request.CashierId
            };

            await transactionRepo.AddAsync(approvedTx);
            await _unitOfWork.SaveChangesAsync();

            // 5. Envío de Notificaciones por Correo (No bloqueantes)
            try
            {
                var cardOwner = await _identityService.GetUserDetailByIdAsync(card.ClienteId);
                if (cardOwner != null && !string.IsNullOrEmpty(cardOwner.Email))
                {
                    await _emailService.SendCreditCardPaymentEmailAsync(
                        cardOwner.Email,
                        $"{cardOwner.FirstName} {cardOwner.LastName}",
                        effectiveAmount,
                        accountLastFour,
                        cardLastFour,
                        now
                    );
                }

                // Si el dueño de la cuenta es diferente al dueño de la tarjeta, notificar también al dueño de la cuenta
                if (account.ClienteId != card.ClienteId)
                {
                    var accountOwner = await _identityService.GetUserDetailByIdAsync(account.ClienteId);
                    if (accountOwner != null && !string.IsNullOrEmpty(accountOwner.Email))
                    {
                        await _emailService.SendAccountDebitedForCardPaymentEmailAsync(
                            accountOwner.Email,
                            $"{accountOwner.FirstName} {accountOwner.LastName}",
                            effectiveAmount,
                            accountLastFour,
                            cardLastFour,
                            now
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "El pago a tarjeta se procesó pero falló el envío de correos.");
                return new CashierPayResult(true, "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
            }

            return new CashierPayResult(true, "Pago a tarjeta de crédito realizado correctamente.");
        }
    }
}