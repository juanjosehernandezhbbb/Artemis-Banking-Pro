using MediatR;

namespace ArtemisBankingPro.Core.Application.Commerces.ChangeCommerceStatus
{
    public record ChangeCommerceStatusCommand(int Id, bool Status) : IRequest;
}