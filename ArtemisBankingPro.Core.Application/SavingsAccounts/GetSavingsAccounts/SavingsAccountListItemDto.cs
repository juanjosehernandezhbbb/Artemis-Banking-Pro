using System;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts
{
    public record SavingsAccountListItemDto(
        string Id,
        string AccountNumber,
        string ClientId,
        string ClientFullName,
        string Identification,
        decimal Balance,
        string Type,
        string Status,
        DateTime CreatedAt);
}