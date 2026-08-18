using FluentValidation;

namespace ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard
{
    public class CancelCreditCardCommandValidator : AbstractValidator<CancelCreditCardCommand>
    {
        public CancelCreditCardCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}