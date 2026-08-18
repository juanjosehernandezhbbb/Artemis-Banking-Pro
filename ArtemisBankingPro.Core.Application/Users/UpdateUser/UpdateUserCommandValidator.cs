using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Users.UpdateUser
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Identification).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.UserName).NotEmpty();

            RuleFor(x => x.Password)
                .Equal(x => x.ConfirmPassword)
                .When(x => !string.IsNullOrEmpty(x.Password) || !string.IsNullOrEmpty(x.ConfirmPassword))
                .WithMessage("Las contraseñas no coinciden.");

            RuleFor(x => x.AdditionalAmount).GreaterThanOrEqualTo(0);
        }
    }
}