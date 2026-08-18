using MediatR;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount
{
    public record CreateSavingsAccountCommand(
        string ClientId,
        decimal InitialBalance,
        string PerformedByUserId = default!
    ) : IRequest<SavingsAccountResponseDto>;
}