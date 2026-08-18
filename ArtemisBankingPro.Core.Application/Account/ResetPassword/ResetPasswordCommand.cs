using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Account.ResetPassword
{
    public record ResetPasswordCommand(
        string UserId,
        string Token,
        [property: SensitiveData] string Password,
        [property: SensitiveData] string ConfirmPassword) : IRequest;
}