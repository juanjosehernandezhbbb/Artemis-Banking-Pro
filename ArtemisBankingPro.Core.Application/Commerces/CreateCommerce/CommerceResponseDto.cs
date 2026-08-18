using System;

namespace ArtemisBankingPro.Core.Application.Commerces.CreateCommerce
{
    public record CommerceResponseDto(
        int Id,
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc,
        bool IsActive,
        DateTime CreatedAt);
}