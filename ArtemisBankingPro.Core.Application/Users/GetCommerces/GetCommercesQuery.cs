using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.GetCommerces
{
    public record GetCommercesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<CommerceUserDto>>;
}