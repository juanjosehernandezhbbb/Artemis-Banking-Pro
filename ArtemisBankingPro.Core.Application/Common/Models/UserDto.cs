namespace ArtemisBankingPro.Core.Application.Common.Models
{
    public record UserDto(
        string Id,
        string UserName,
        string Identification,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        bool IsActive);
}