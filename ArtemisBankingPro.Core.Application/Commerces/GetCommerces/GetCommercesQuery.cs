using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;
using System;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerces
{
    public record GetCommercesQuery(int Page = 1, int PageSize = 20, string? Status = null)
        : IRequest<PagedResult<CommerceListItemDto>>;
}