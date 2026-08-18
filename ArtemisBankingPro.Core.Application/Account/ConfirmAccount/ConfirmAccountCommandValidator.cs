using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Account.ConfirmAccount
{
    public class ConfirmAccountCommandValidator : AbstractValidator<ConfirmAccountCommand>
    {
        public ConfirmAccountCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token de confirmación es requerido.");
        }
    }
}