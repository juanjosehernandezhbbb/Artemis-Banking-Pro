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

namespace ArtemisBankingPro.Core.Application.TransferBetweenAccounts
{
    public record TransferBetweenAccountsCommand(
        string ClienteId,
        int CuentaOrigenId,
        int CuentaDestinoId,
        decimal Monto
    ) : IRequest<TransferResult>;

    public record TransferResult(bool Success, string Message = "");

    public class TransferBetweenAccountsCommandHandler : IRequestHandler<TransferBetweenAccountsCommand, TransferResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<TransferBetweenAccountsCommandHandler> _logger;

        public TransferBetweenAccountsCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<TransferBetweenAccountsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<TransferResult> Handle(TransferBetweenAccountsCommand request, CancellationToken cancellationToken)
        {
            // 1. Validar que el cliente tenga al menos 2 cuentas activas
            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuentasActivas = await cuentaRepo.FindAsync(c => c.ClienteId == request.ClienteId && c.Estado == EstadoCuenta.Activa);

            if (cuentasActivas.Count() < 2)
                return new TransferResult(false, "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.");

            // 2. Validar Monto
            if (request.Monto <= 0)
                return new TransferResult(false, "El monto a transferir debe ser mayor que cero.");

            // 3. Validar Cuentas Origen y Destino
            if (request.CuentaOrigenId == request.CuentaDestinoId)
                return new TransferResult(false, "La cuenta de origen y la cuenta de destino no pueden ser la misma.");

            var cuentaOrigen = await cuentaRepo.GetByIdAsync(request.CuentaOrigenId);
            var cuentaDestino = await cuentaRepo.GetByIdAsync(request.CuentaDestinoId);

            if (cuentaOrigen == null || cuentaOrigen.ClienteId != request.ClienteId || cuentaOrigen.Estado != EstadoCuenta.Activa)
                return new TransferResult(false, "La cuenta de origen seleccionada no es válida o no está activa.");

            if (cuentaDestino == null || cuentaDestino.ClienteId != request.ClienteId || cuentaDestino.Estado != EstadoCuenta.Activa)
                return new TransferResult(false, "La cuenta de destino seleccionada no es válida o no está activa.");

            var transaccionRepo = _unitOfWork.Repository<Transaccion>();
            var now = _dateTimeProvider.UtcNow;
            string originLastFour = cuentaOrigen.NumeroCuenta[^4..];
            string targetLastFour = cuentaDestino.NumeroCuenta[^4..];

            // 4. Validar fondos suficientes
            if (cuentaOrigen.Balance < request.Monto)
            {
                // Registrar intento rechazado en origen
                var txRechazada = new Transaccion
                {
                    CuentaAhorroId = cuentaOrigen.Id,
                    Tipo = TipoTransaccion.Debito,
                    TipoOperacion = TipoOperacion.TransferenciaEntreCuentasPropias,
                    Monto = request.Monto,
                    Origen = cuentaOrigen.NumeroCuenta,
                    Destino = cuentaDestino.NumeroCuenta,
                    Estado = EstadoTransaccion.Rechazada,
                    MotivoRechazo = "No dispone del monto requerido en la cuenta seleccionada.",
                    Fecha = now
                };

                await transaccionRepo.AddAsync(txRechazada);
                await _unitOfWork.SaveChangesAsync();

                return new TransferResult(false, "No dispone del monto requerido en la cuenta seleccionada.");
            }

            // 5. Procesamiento transaccional de la transferencia
            cuentaOrigen.Balance -= request.Monto;
            cuentaDestino.Balance += request.Monto;

            cuentaRepo.Update(cuentaOrigen);
            cuentaRepo.Update(cuentaDestino);

            // Transacción DÉBITO en origen
            var txDebito = new Transaccion
            {
                CuentaAhorroId = cuentaOrigen.Id,
                Tipo = TipoTransaccion.Debito,
                TipoOperacion = TipoOperacion.TransferenciaEntreCuentasPropias,
                Monto = request.Monto,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now
            };

            // Transacción CRÉDITO en destino
            var txCredito = new Transaccion
            {
                CuentaAhorroId = cuentaDestino.Id,
                Tipo = TipoTransaccion.Credito,
                TipoOperacion = TipoOperacion.TransferenciaEntreCuentasPropias,
                Monto = request.Monto,
                Origen = cuentaOrigen.NumeroCuenta,
                Destino = cuentaDestino.NumeroCuenta,
                Estado = EstadoTransaccion.Aprobada,
                Fecha = now
            };

            await transaccionRepo.AddAsync(txDebito);
            await transaccionRepo.AddAsync(txCredito);

            await _unitOfWork.SaveChangesAsync();

            // 6. Notificación por correo (no bloqueante)
            try
            {
                var clientUser = await _identityService.GetUserDetailByIdAsync(request.ClienteId);
                if (clientUser != null && !string.IsNullOrEmpty(clientUser.Email))
                {
                    await _emailService.SendTransferBetweenAccountsEmailAsync(
                        clientUser.Email,
                        $"{clientUser.FirstName} {clientUser.LastName}",
                        request.Monto,
                        originLastFour,
                        targetLastFour,
                        now
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de transferencia propia para el cliente {ClienteId}", request.ClienteId);
                return new TransferResult(true, "La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.");
            }

            return new TransferResult(true, "Transferencia entre cuentas realizada con éxito.");
        }
    }
}