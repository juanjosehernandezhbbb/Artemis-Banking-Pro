using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.GetUserById
{
    public record GetUserByIdQuery(string Id) : IRequest<UserDetailDto>;
}