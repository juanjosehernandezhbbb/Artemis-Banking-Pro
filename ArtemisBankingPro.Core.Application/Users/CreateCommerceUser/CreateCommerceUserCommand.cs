using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.CreateCommerceUser
{
    public record CreateCommerceUserCommand(
        string FirstName,
        string LastName,
        string Identification,
        string Email,
        string UserName,
        [property: SensitiveData] string Password,
        [property: SensitiveData] string ConfirmPassword,
        int CommerceId,
        decimal InitialAmount = 0
    ) : IRequest<string>;
}