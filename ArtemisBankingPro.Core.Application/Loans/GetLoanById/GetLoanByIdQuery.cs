using MediatR;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoanById
{
    public record GetLoanByIdQuery(int Id) : IRequest<LoanDetailDto>;
}