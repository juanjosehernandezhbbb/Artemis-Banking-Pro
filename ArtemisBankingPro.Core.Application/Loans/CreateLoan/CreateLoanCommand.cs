using MediatR;

namespace ArtemisBankingPro.Core.Application.Loans.CreateLoan
{
    public record CreateLoanCommand(
        string ClientId,
        decimal CapitalAmount,
        int TermInMonths,
        decimal AnnualInterestRate,
        bool ConfirmHighRisk = false,
        string PerformedByUserId = default! 
    ) : IRequest<LoanResponseDto>;
}