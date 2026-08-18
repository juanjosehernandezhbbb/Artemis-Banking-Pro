using System;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount
{
    public record SavingsAccountResponseDto(
        string Id,
        string AccountNumber,
        string ClientId,
        string ClientFullName,
        decimal Balance,
        string Type,
        string Status,
        DateTime CreatedAt);
}