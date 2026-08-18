using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.HermesPay.GetCommerceTransactions
{
    public record CommerceTransactionsDto(
        int Page,
        int PageSize,
        int TotalRecords,
        int TotalPages,
        int CommerceId,
        string CommerceName,
        IReadOnlyList<CommerceTransactionItemDto> Data);

    public record CommerceTransactionItemDto(
        string Id,
        DateTime TransactionDate,
        decimal Amount,
        string CardLastFourDigits,
        string Status);
}