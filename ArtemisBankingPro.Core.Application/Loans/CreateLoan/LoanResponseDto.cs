using System;

namespace ArtemisBankingPro.Core.Application.Loans.CreateLoan
{
    public record LoanResponseDto(
        string Id,
        string LoanNumber,
        string ClientId,
        string ClientFullName,
        decimal CapitalAmount,
        int TermInMonths,
        decimal AnnualInterestRate,
        decimal MonthlyInstallment,
        decimal TotalAmountToPay,
        string Status,
        DateTime CreatedAt
    );
}