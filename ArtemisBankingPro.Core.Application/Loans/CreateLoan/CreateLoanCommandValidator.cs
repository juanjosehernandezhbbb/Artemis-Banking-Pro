using ArtemisBankingPro.Core.Domain.Common;
using FluentValidation;
using System.Linq;

namespace ArtemisBankingPro.Core.Application.Loans.CreateLoan
{
    public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
    {
        public CreateLoanCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.CapitalAmount).GreaterThan(0);
            RuleFor(x => x.AnnualInterestRate).GreaterThan(0);
            RuleFor(x => x.TermInMonths)
                .Must(t => DomainConstants.PlazosPrestamoPermitidos.Contains(t))
                .WithMessage("El plazo en meses no es válido. Consulte los plazos permitidos.");
        }
    }
}