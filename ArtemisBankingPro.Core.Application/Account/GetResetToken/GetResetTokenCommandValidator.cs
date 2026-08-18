using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Account.GetResetToken
{
    public class GetResetTokenCommandValidator : AbstractValidator<GetResetTokenCommand>
    {
        public GetResetTokenCommandValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es requerido.");
        }
    }
}