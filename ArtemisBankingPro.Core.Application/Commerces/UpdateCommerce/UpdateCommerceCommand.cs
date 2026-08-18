using MediatR;

namespace ArtemisBankingPro.Core.Application.Commerces.UpdateCommerce
{
    public record UpdateCommerceCommand(
        int Id,
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc
    ) : IRequest;
}