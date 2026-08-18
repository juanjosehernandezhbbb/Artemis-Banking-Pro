using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Account.Login
{
    public record LoginCommand(
        string UserName,
        [property: SensitiveData] string Password,
        IReadOnlyCollection<string> AllowedRoles) : IRequest<LoginResultDto>;
}