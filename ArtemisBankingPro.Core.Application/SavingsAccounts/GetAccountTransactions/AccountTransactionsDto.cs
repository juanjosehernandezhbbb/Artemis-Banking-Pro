using ArtemisBankingPro.Core.Application.Common.Models;
using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetAccountTransactions
{
    public record AccountTransactionsDto(
        string AccountNumber,
        string ClientFullName,
        decimal Balance,
        string Type,
        string Status,
        PagedResult<AccountTransactionItemDto> Transactions);

    public record AccountTransactionItemDto(
        string Id,
        DateTime Date,
        decimal Amount,
        string TransactionType,
        string Origin,
        string Beneficiary,
        string Status);
}