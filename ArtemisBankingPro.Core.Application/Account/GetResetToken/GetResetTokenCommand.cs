using MediatR;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Account.GetResetToken
{
    public record GetResetTokenCommand(string UserName, IReadOnlyCollection<string> AllowedRoles) : IRequest;
}