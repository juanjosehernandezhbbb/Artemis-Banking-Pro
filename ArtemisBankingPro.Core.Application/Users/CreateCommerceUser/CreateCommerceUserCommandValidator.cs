using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Users.CreateCommerceUser
{
    public class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
    {
        public CreateCommerceUserCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Identification).NotEmpty().Matches(@"^\d+$").WithMessage("La cédula solo debe contener números.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");
            RuleFor(x => x.CommerceId).GreaterThan(0);
            RuleFor(x => x.InitialAmount).GreaterThanOrEqualTo(0).WithMessage("El monto inicial no puede ser negativo.");
        }
    }
}