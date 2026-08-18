using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.ChangeUserStatus
{
    public record ChangeUserStatusCommand(string Id, bool? Status, string PerformedByUserId) : IRequest;
}