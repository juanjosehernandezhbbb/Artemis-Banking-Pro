using System;

namespace ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard
{
    public record CreditCardResponseDto(
        string Id,
        string MaskedCardNumber,
        string LastFourDigits,
        string ClientId,
        string ClientFullName,
        decimal CreditLimit,
        decimal AvailableCredit,
        decimal CurrentDebt,
        string ExpirationDate,
        string Status,
        DateTime CreatedAt);
}