using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.UpdateUser
{
    public record UpdateUserCommand(
        string Id,
        string FirstName,
        string LastName,
        string Identification,
        string Email,
        string UserName,
        [property: SensitiveData] string? Password,
        [property: SensitiveData] string? ConfirmPassword,
        decimal AdditionalAmount,
        string PerformedByUserId 
    ) : IRequest;
}