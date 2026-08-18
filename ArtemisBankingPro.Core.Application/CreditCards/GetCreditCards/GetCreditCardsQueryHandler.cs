using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.CreditCards.GetCreditCards
{
    public class GetCreditCardsQueryHandler : IRequestHandler<GetCreditCardsQuery, PagedResult<CreditCardListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetCreditCardsQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<PagedResult<CreditCardListItemDto>> Handle(GetCreditCardsQuery request, CancellationToken cancellationToken)
        {
            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();

            string? clientId = null;
            if (!string.IsNullOrWhiteSpace(request.Identification))
            {
                clientId = await _identityService.GetUserIdByCedulaAsync(request.Identification);
            }

            var status = request.Status?.ToLower() ?? "activa";

            List<TarjetaCredito> tarjetas;
            if (clientId != null)
            {
                tarjetas = (await tarjetaRepo.FindAsync(t => t.ClienteId == clientId)).ToList();
            }
            else
            {
                tarjetas = (await tarjetaRepo.FindAsync(_ => true)).ToList();
            }

            if (status != "todas")
            {
                var expectedState = status == "cancelada" ? EstadoTarjeta.Cancelada : EstadoTarjeta.Activa;
                tarjetas = tarjetas.Where(t => t.Estado == expectedState).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Identification) && status == "activa")
            {
                tarjetas = tarjetas
                    .OrderBy(t => t.Estado)
                    .ThenByDescending(t => t.FechaCreacion)
                    .ToList();
            }
            else
            {
                tarjetas = tarjetas.OrderByDescending(t => t.FechaCreacion).ToList();
            }

            var totalRecords = tarjetas.Count;
            var pagedTarjetas = tarjetas.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var clientNames = new Dictionary<string, string>();
            foreach (var client in pagedTarjetas.Select(t => t.ClienteId).Distinct())
            {
                var detail = await _identityService.GetUserDetailByIdAsync(client);
                clientNames[client] = detail != null ? $"{detail.FirstName} {detail.LastName}" : "Cliente";
            }

            var items = pagedTarjetas.Select(t => new CreditCardListItemDto(
                t.Id.ToString(),
                MaskCardNumber(t.NumeroTarjeta),
                t.NumeroTarjeta[^4..],
                t.ClienteId,
                clientNames.GetValueOrDefault(t.ClienteId),
                t.LimiteCredito,
                t.CreditoDisponible,
                t.MontoAdeudado,
                t.FechaExpiracion.ToString("MM/yy"),
                t.Estado.ToString(),
                t.FechaCreacion
            )).ToList();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<CreditCardListItemDto>(request.Page, request.PageSize, totalRecords, totalPages, items);
        }

        private static string MaskCardNumber(string cardNumber)
        {
            return cardNumber.Length >= 4
                ? new string('*', cardNumber.Length - 4) + cardNumber[^4..]
                : cardNumber;
        }
    }
}