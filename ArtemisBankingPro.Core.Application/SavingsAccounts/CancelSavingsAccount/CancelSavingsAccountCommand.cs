using MediatR;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount
{
    public record CancelSavingsAccountCommand(string AccountNumber, string PerformedByUserId = default!) : IRequest;
}