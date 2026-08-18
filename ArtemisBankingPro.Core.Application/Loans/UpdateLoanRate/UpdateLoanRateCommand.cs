using MediatR;

namespace ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate
{
    public record UpdateLoanRateCommand(int Id, decimal AnnualInterestRate) : IRequest;
}