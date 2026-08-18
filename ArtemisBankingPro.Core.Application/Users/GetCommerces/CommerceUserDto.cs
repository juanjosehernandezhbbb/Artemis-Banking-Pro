namespace ArtemisBankingPro.Core.Application.Users.GetCommerces
{
    public record CommerceUserDto(
        string Id,
        string UserName,
        string Identification,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        int CommerceId,
        string CommerceName,
        bool IsActive);
}