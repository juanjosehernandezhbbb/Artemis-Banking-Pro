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

namespace ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment
{
    public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
    {
        private const string InsufficientCreditMessage = "El monto de la transacción excede el crédito disponible de la tarjeta.";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICreditCardService _creditCardService;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProcessPaymentCommandHandler> _logger;

        public ProcessPaymentCommandHandler(
            IUnitOfWork unitOfWork,
            ICreditCardService creditCardService,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<ProcessPaymentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _creditCardService = creditCardService;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
        {
            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();
            var tarjetas = await tarjetaRepo.FindAsync(t => t.NumeroTarjeta == request.CardNumber);
            var tarjeta = tarjetas.FirstOrDefault();

            if (tarjeta == null)
                throw new BusinessRuleException("La tarjeta no existe en el sistema.");

            if (tarjeta.Estado != EstadoTarjeta.Activa)
                throw new BusinessRuleException("La tarjeta se encuentra cancelada.");

            var cvcHash = _creditCardService.HashCvc(request.Cvc);
            if (!string.Equals(cvcHash, tarjeta.CvcHash, StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException("Los datos de la tarjeta no coinciden.");

            if (tarjeta.FechaExpiracion.Month.ToString("00") != request.MonthExpirationCard
                || tarjeta.FechaExpiracion.Year.ToString("0000") != request.YearExpirationCard)
                throw new BusinessRuleException("Los datos de expiración de la tarjeta no coinciden.");

            if (tarjeta.FechaExpiracion < _dateTimeProvider.UtcNow)
                throw new BusinessRuleException("La tarjeta se encuentra vencida.");

            var comercioRepo = _unitOfWork.Repository<Comercio>();
            var comercio = await comercioRepo.GetByIdAsync(request.CommerceId);
            if (comercio == null)
                throw new NotFoundException("Comercio", request.CommerceId);

            if (!comercio.IsActive)
                throw new BusinessRuleException("El comercio se encuentra inactivo.");

            if (string.IsNullOrWhiteSpace(comercio.ApplicationUserId))
                throw new BusinessRuleException("El comercio no tiene un usuario asociado.");

            var cuentas = await _unitOfWork.Repository<CuentaAhorro>()
                .FindAsync(c => c.ClienteId == comercio.ApplicationUserId && c.TipoCuenta == TipoCuenta.Principal);
            var cuentaPrincipal = cuentas.FirstOrDefault(c => c.Estado == EstadoCuenta.Activa);

            if (cuentaPrincipal == null)
                throw new BusinessRuleException("El usuario de comercio debe tener una cuenta de ahorro principal activa.");

            var lastFourDigits = tarjeta.NumeroTarjeta[^4..];
            var now = _dateTimeProvider.UtcNow;
            var grupoOperacion = Guid.NewGuid();
            var transaccionRepo = _unitOfWork.Repository<Transaccion>();

            var consumo = new Transaccion
            {
                TarjetaCreditoId = tarjeta.Id,
                TipoOperacion = TipoOperacion.PagoHermesPay,
                Tipo = TipoTransaccion.Debito,
                Monto = request.TransactionAmount,
                Origen = comercio.Name,
                Destino = lastFourDigits,
                Estado = EstadoTransaccion.Aprobada,
                GrupoOperacionId = grupoOperacion,
                Fecha = now
            };

            if (tarjeta.CreditoDisponible < request.TransactionAmount)
            {
                consumo.Estado = EstadoTransaccion.Rechazada;
                consumo.MotivoRechazo = InsufficientCreditMessage;
                await transaccionRepo.AddAsync(consumo);
                await _unitOfWork.SaveChangesAsync();

                return new ProcessPaymentResult(false, InsufficientCreditMessage);
            }

            tarjeta.MontoAdeudado += request.TransactionAmount;
            tarjetaRepo.Update(tarjeta);

            await transaccionRepo.AddAsync(consumo);

            cuentaPrincipal.Balance += request.TransactionAmount;
            _unitOfWork.Repository<CuentaAhorro>().Update(cuentaPrincipal);

            var credito = new Transaccion
            {
                CuentaAhorroId = cuentaPrincipal.Id,
                TipoOperacion = TipoOperacion.PagoHermesPay,
                Tipo = TipoTransaccion.Credito,
                Monto = request.TransactionAmount,
                Origen = lastFourDigits,
                Destino = cuentaPrincipal.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                GrupoOperacionId = grupoOperacion,
                Fecha = now
            };

            await transaccionRepo.AddAsync(credito);

            await _unitOfWork.SaveChangesAsync();

            await NotifyCardOwnerAndCommerceAsync(tarjeta, comercio, request.TransactionAmount, lastFourDigits, now);

            return new ProcessPaymentResult(true);
        }

        private async Task NotifyCardOwnerAndCommerceAsync(
            TarjetaCredito tarjeta, Comercio comercio, decimal amount, string lastFourDigits, DateTime now)
        {
            try
            {
                var owner = await _identityService.GetUserDetailByIdAsync(tarjeta.ClienteId);
                if (owner != null)
                {
                    await _emailService.SendCardConsumptionEmailAsync(
                        owner.Email, $"{owner.FirstName} {owner.LastName}", amount, lastFourDigits, comercio.Name, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el envío del correo de consumo al propietario de la tarjeta {CardId}", tarjeta.Id);
            }

            try
            {
                await _emailService.SendPaymentReceivedEmailAsync(
                    comercio.Email, comercio.Name, amount, lastFourDigits, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el envío del correo de pago recibido al comercio {CommerceId}", comercio.Id);
            }
        }
    }
}