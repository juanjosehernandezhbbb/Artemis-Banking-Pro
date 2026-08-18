using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.CreditCards.GetCreditCardById
{
    public record CreditCardDetailDto(
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
        IReadOnlyList<CardConsumptionItemDto> Consumptions);

    public record CardConsumptionItemDto(
        string Id,
        DateTime Date,
        decimal Amount,
        string CommerceName,
        string Status);
}