using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Users.CreateUser
{
    public record CreateUserCommand(
        string FirstName,
        string LastName,
        string Identification,
        string Email,
        string UserName,
        [property: SensitiveData] string Password,
        [property: SensitiveData] string ConfirmPassword,
        string Role,
        decimal InitialAmount) : IRequest<string>; 
}