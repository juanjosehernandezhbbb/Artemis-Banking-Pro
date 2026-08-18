using System;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerces
{
    public record CommerceListItemDto(
        int Id,
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc,
        bool IsActive,
        bool HasAssociatedUser,
        DateTime CreatedAt);
}