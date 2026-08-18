using System;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerceById
{
    public record CommerceDetailDto(
        int Id,
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc,
        bool IsActive,
        DateTime CreatedAt,
        CommerceAssociatedUserDto? AssociatedUser);

    public record CommerceAssociatedUserDto(
        string Id,
        string UserName,
        string Email,
        bool IsActive);
}