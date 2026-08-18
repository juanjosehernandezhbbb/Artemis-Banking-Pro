using FluentValidation;

namespace ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard
{
    public class CreateCreditCardCommandValidator : AbstractValidator<CreateCreditCardCommand>
    {
        public CreateCreditCardCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.CreditLimit).GreaterThan(0).WithMessage("El límite de crédito debe ser mayor que cero.");
        }
    }
}