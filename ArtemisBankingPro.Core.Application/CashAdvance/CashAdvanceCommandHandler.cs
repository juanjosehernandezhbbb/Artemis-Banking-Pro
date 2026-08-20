using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.CashAdvance
{
    public class CashAdvanceCommandHandler : IRequestHandler<CashAdvanceCommand, CashAdvanceResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<CashAdvanceCommandHandler> _logger;

        public CashAdvanceCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<CashAdvanceCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CashAdvanceResult> Handle(CashAdvanceCommand request, CancellationToken cancellationToken)
        {
            if (request.Monto.Value <= 0)
                return new CashAdvanceResult(false, "El monto del avance debe ser mayor que cero.");

            // 1. Validar Tarjeta de Crédito
            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();
            var tarjeta = await tarjetaRepo.GetByIdAsync(request.TarjetaCreditoId);

            if (tarjeta == null || tarjeta.ClienteId != request.ClienteId)
                return new CashAdvanceResult(false, "La tarjeta seleccionada no es válida.");

            if (tarjeta.Estado != EstadoTarjeta.Activa)
                return new CashAdvanceResult(false, "La tarjeta seleccionada no se encuentra activa.");

            if (tarjeta.FechaExpiracion < _dateTimeProvider.UtcNow)
                return new CashAdvanceResult(false, "La tarjeta seleccionada se encuentra vencida.");

            // 2. Validar Cuenta de Ahorro
            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuenta = await cuentaRepo.GetByIdAsync(request.CuentaAhorroId);

            if (cuenta == null || cuenta.ClienteId != request.ClienteId)
                return new CashAdvanceResult(false, "La cuenta de ahorro seleccionada no es válida.");

            if (cuenta.Estado != EstadoCuenta.Activa)
                return new CashAdvanceResult(false, "La cuenta de ahorro seleccionada no se encuentra activa.");

            // 3. Cálculos Financieros
            decimal montoAvance = request.Monto.Value;
            decimal interes = montoAvance * 0.0625m;
            decimal totalCargado = montoAvance + interes;
            decimal creditoDisponible = tarjeta.CreditoDisponible;

            var transaccionRepo = _unitOfWork.Repository<Transaccion>();
            var now = _dateTimeProvider.UtcNow;
            var grupoOperacion = Guid.NewGuid();
            string lastFourCard = tarjeta.NumeroTarjeta[^4..];

            // 4. Validación de Crédito Disponible
            if (totalCargado > creditoDisponible)
            {
                var consumoRechazado = new Transaccion
                {
                    TarjetaCreditoId = tarjeta.Id,
                    TipoOperacion = TipoOperacion.AvanceEfectivo,
                    Tipo = TipoTransaccion.Debito,
                    Monto = totalCargado,
                    Origen = "AVANCE",
                    Destino = cuenta.NumeroCuenta,
                    Estado = EstadoTransaccion.Rechazada,
                    MotivoRechazo = "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.",
                    GrupoOperacionId = grupoOperacion,
                    Fecha = now
                };

                await transaccionRepo.AddAsync(consumoRechazado);
                await _unitOfWork.SaveChangesAsync();

                return new CashAdvanceResult(false, "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.");
            }

            // 5. Procesamiento Exitoso
            tarjeta.MontoAdeudado += totalCargado;
            tarjetaRepo.Update(tarjeta);

            cuenta.Balance += montoAvance;
            cuentaRepo.Update(cuenta);

            var consumoAprobado = new Transaccion
            {
                TarjetaCreditoId = tarjeta.Id,
                TipoOperacion = TipoOperacion.AvanceEfectivo,
                Tipo = TipoTransaccion.Debito,
                Monto = totalCargado,
                Origen = "AVANCE",
                Destino = cuenta.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                GrupoOperacionId = grupoOperacion,
                Fecha = now
            };
            await transaccionRepo.AddAsync(consumoAprobado);

            var creditoAprobado = new Transaccion
            {
                CuentaAhorroId = cuenta.Id,
                TipoOperacion = TipoOperacion.AvanceEfectivo,
                Tipo = TipoTransaccion.Credito,
                Monto = montoAvance,
                Origen = lastFourCard,
                Destino = cuenta.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                GrupoOperacionId = grupoOperacion,
                Fecha = now
            };
            await transaccionRepo.AddAsync(creditoAprobado);

            await _unitOfWork.SaveChangesAsync();

            // 6. Envío de Notificación por Correo
            try
            {
                var clientUser = await _identityService.GetUserDetailByIdAsync(request.ClienteId);
                if (clientUser != null)
                {
                    string accountLastFour = cuenta.NumeroCuenta.Length >= 4 ? cuenta.NumeroCuenta[^4..] : cuenta.NumeroCuenta;
                    await _emailService.SendCashAdvanceEmailAsync(
                        clientUser.Email,
                        $"{clientUser.FirstName} {clientUser.LastName}",
                        montoAvance,
                        interes,
                        totalCargado,
                        accountLastFour,
                        now
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo de avance de efectivo para el cliente {ClienteId}", request.ClienteId);
                return new CashAdvanceResult(true, "El avance fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
            }

            return new CashAdvanceResult(true, "Avance de efectivo realizado exitosamente.");
        }
    }
}