using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Account.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");
        }
    }
}