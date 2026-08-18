using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.GetUsers
{
    public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Role = null) : IRequest<PagedResult<UserDto>>;
}