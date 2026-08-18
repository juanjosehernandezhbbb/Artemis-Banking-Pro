using MediatR;

namespace ArtemisBankingPro.Core.Application.Account.ConfirmAccount
{
    public record ConfirmAccountCommand(string Token) : IRequest;
}