using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.CreditCards.GetCreditCardById
{
    public class GetCreditCardByIdQueryHandler : IRequestHandler<GetCreditCardByIdQuery, CreditCardDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetCreditCardByIdQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<CreditCardDetailDto> Handle(GetCreditCardByIdQuery request, CancellationToken cancellationToken)
        {
            var tarjeta = await _unitOfWork.Repository<TarjetaCredito>().GetByIdAsync(request.Id);
            if (tarjeta == null)
                throw new NotFoundException("Tarjeta de crédito", request.Id);

            var client = await _identityService.GetUserDetailByIdAsync(tarjeta.ClienteId);
            var clientFullName = client != null ? $"{client.FirstName} {client.LastName}" : "Cliente";

            var consumos = (await _unitOfWork.Repository<Transaccion>()
                .FindAsync(t => t.TarjetaCreditoId == tarjeta.Id))
                .OrderByDescending(t => t.Fecha)
                .ToList();

            var consumptionItems = consumos.Select(c => new CardConsumptionItemDto(
                c.Id.ToString(),
                c.Fecha,
                c.Monto,
                c.TipoOperacion == TipoOperacion.AvanceEfectivo ? "AVANCE" : c.Origen,
                c.Estado == EstadoTransaccion.Aprobada ? "APROBADO" : "RECHAZADO"
            )).ToList();

            return new CreditCardDetailDto(
                tarjeta.Id.ToString(),
                new string('*', tarjeta.NumeroTarjeta.Length - 4) + tarjeta.NumeroTarjeta[^4..],
                tarjeta.NumeroTarjeta[^4..],
                tarjeta.ClienteId,
                clientFullName,
                tarjeta.LimiteCredito,
                tarjeta.CreditoDisponible,
                tarjeta.MontoAdeudado,
                tarjeta.FechaExpiracion.ToString("MM/yy"),
                tarjeta.Estado.ToString(),
                consumptionItems
            );
        }
    }
}