using MediatR;

namespace ArtemisBankingPro.Core.Application.Commerces.CreateCommerce
{
    public record CreateCommerceCommand(
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc,
        string PerformedByUserId = default!
    ) : IRequest<CommerceResponseDto>;
}