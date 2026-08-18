using System;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoans
{
    public record LoanListItemDto(
        string Id,
        string LoanNumber,
        string ClientId,
        string ClientFullName,
        decimal CapitalAmount,
        int TotalInstallments,
        int PaidInstallments,
        decimal PendingAmount,
        decimal AnnualInterestRate,
        int TermInMonths,
        string Status,
        string ClientPaymentStatus,
        DateTime CreatedAt);
}