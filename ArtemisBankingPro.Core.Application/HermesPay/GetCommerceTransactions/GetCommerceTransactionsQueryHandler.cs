using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.HermesPay.GetCommerceTransactions
{
    public class GetCommerceTransactionsQueryHandler : IRequestHandler<GetCommerceTransactionsQuery, CommerceTransactionsDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCommerceTransactionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CommerceTransactionsDto> Handle(GetCommerceTransactionsQuery request, CancellationToken cancellationToken)
        {
            var comercio = await _unitOfWork.Repository<Comercio>().GetByIdAsync(request.CommerceId);
            if (comercio == null)
                throw new NotFoundException("Comercio", request.CommerceId);

            if (!comercio.IsActive)
                throw new ForbiddenAccessException("El comercio asociado se encuentra inactivo.");

            var cuentas = await _unitOfWork.Repository<CuentaAhorro>()
                .FindAsync(c => c.ClienteId == comercio.ApplicationUserId && c.TipoCuenta == TipoCuenta.Principal);
            var cuentaPrincipal = cuentas.FirstOrDefault(c => c.Estado == EstadoCuenta.Activa) ?? cuentas.FirstOrDefault();

            var items = new List<CommerceTransactionItemDto>();
            var totalRecords = 0;

            if (cuentaPrincipal != null)
            {
                var transacciones = (await _unitOfWork.Repository<Transaccion>()
                    .FindAsync(t => t.CuentaAhorroId == cuentaPrincipal.Id && t.TipoOperacion == TipoOperacion.PagoHermesPay))
                    .OrderByDescending(t => t.Fecha)
                    .ToList();

                totalRecords = transacciones.Count;

                var paged = transacciones
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                items = paged.Select(t => new CommerceTransactionItemDto(
                    t.Id.ToString(),
                    t.Fecha,
                    t.Monto,
                    t.Origen,
                    t.Estado == EstadoTransaccion.Aprobada ? "APROBADO" : "RECHAZADO"
                )).ToList();
            }

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new CommerceTransactionsDto(
                request.Page,
                request.PageSize,
                totalRecords,
                totalPages,
                comercio.Id,
                comercio.Name,
                items
            );
        }
    }
}