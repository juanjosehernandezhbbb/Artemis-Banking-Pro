using FluentValidation;

namespace ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit
{
    public class UpdateCreditCardLimitCommandValidator : AbstractValidator<UpdateCreditCardLimitCommand>
    {
        public UpdateCreditCardLimitCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.CreditLimit).GreaterThan(0).WithMessage("El nuevo límite debe ser mayor que cero.");
        }
    }
}