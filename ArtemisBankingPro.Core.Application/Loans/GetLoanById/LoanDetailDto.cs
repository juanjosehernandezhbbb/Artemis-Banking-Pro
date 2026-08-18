using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoanById
{
    public record LoanDetailDto(
        string Id,
        string LoanNumber,
        string ClientId,
        string ClientFullName,
        decimal CapitalAmount,
        decimal AnnualInterestRate,
        int TermInMonths,
        decimal MonthlyInstallment,
        decimal PendingAmount,
        string Status,
        string ClientPaymentStatus,
        DateTime CreatedAt,
        IReadOnlyList<AmortizationItemDto> Amortization);

    public record AmortizationItemDto(
        int InstallmentNumber,
        DateTime DueDate,
        decimal InstallmentAmount,
        decimal InterestAmount,
        decimal CapitalAmount,
        decimal PendingInstallmentAmount,
        string PaymentStatus,
        bool IsLate);
}