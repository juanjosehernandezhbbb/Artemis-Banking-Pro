using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;
using System;

namespace ArtemisBankingPro.Core.Application.CreditCards.GetCreditCards
{
    public record GetCreditCardsQuery(int Page = 1, int PageSize = 20, string? Status = null, string? Identification = null)
        : IRequest<PagedResult<CreditCardListItemDto>>;
}