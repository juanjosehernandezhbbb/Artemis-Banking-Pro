using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate
{
    public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
    {
        public UpdateLoanRateCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.AnnualInterestRate).GreaterThanOrEqualTo(0)
                .WithMessage("La tasa de interés anual no puede ser negativa.");
        }
    }
}