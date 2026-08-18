using MediatR;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetAccountTransactions
{
    public record GetAccountTransactionsQuery(string AccountNumber, int Page = 1, int PageSize = 20)
        : IRequest<AccountTransactionsDto>;
}