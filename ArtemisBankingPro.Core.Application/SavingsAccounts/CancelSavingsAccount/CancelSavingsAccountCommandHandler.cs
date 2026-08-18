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

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount
{
    public class CancelSavingsAccountCommandHandler : IRequestHandler<CancelSavingsAccountCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CancelSavingsAccountCommandHandler(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var cuentaRepo = _unitOfWork.Repository<CuentaAhorro>();
            var cuentas = await cuentaRepo.FindAsync(c => c.NumeroCuenta == request.AccountNumber);
            var cuenta = cuentas.FirstOrDefault();

            if (cuenta == null)
                throw new NotFoundException("Cuenta de ahorro", request.AccountNumber);

            if (cuenta.TipoCuenta == TipoCuenta.Principal)
                throw new BusinessRuleException("Las cuentas principales no pueden ser canceladas.");

            if (cuenta.Estado == EstadoCuenta.Cancelada)
                throw new BusinessRuleException("La cuenta ya se encuentra cancelada.");

            var cuentasCliente = await cuentaRepo.FindAsync(c => c.ClienteId == cuenta.ClienteId && c.TipoCuenta == TipoCuenta.Principal);
            var cuentaPrincipal = cuentasCliente.FirstOrDefault(c => c.Estado == EstadoCuenta.Activa);

            if (cuentaPrincipal == null)
                throw new BusinessRuleException("El cliente debe tener una cuenta de ahorro principal activa para recibir los fondos.");

            var grupoOperacion = Guid.NewGuid();

            if (cuenta.Balance > 0)
            {
                var transaccionRepo = _unitOfWork.Repository<Transaccion>();
                var fecha = _dateTimeProvider.UtcNow;

                var debito = new Transaccion
                {
                    CuentaAhorroId = cuenta.Id,
                    TipoOperacion = TipoOperacion.TransferenciaEntreCuentasPropias,
                    Tipo = TipoTransaccion.Debito,
                    Monto = cuenta.Balance,
                    Origen = cuenta.NumeroCuenta,
                    Destino = cuentaPrincipal.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    GrupoOperacionId = grupoOperacion,
                    Fecha = fecha,
                    RealizadoPorUserId = request.PerformedByUserId
                };

                var credito = new Transaccion
                {
                    CuentaAhorroId = cuentaPrincipal.Id,
                    TipoOperacion = TipoOperacion.TransferenciaEntreCuentasPropias,
                    Tipo = TipoTransaccion.Credito,
                    Monto = cuenta.Balance,
                    Origen = cuenta.NumeroCuenta,
                    Destino = cuentaPrincipal.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    GrupoOperacionId = grupoOperacion,
                    Fecha = fecha,
                    RealizadoPorUserId = request.PerformedByUserId
                };

                await transaccionRepo.AddAsync(debito);
                await transaccionRepo.AddAsync(credito);

                cuentaPrincipal.Balance += cuenta.Balance;
                cuentaRepo.Update(cuentaPrincipal);

                cuenta.Balance = 0m;
            }

            cuenta.Estado = EstadoCuenta.Cancelada;
            cuentaRepo.Update(cuenta);
        }
    }
}