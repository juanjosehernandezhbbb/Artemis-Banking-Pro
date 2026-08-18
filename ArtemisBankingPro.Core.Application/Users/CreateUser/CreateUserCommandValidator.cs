using ArtemisBankingPro.Core.Domain.Common; 
using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Users.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Identification).NotEmpty().Matches(@"^\d+$").WithMessage("La cédula solo debe contener números.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("La contraseña y la confirmación de contraseña deben coincidir.");

            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(r => r == DomainConstants.RolAdministrador || r == DomainConstants.RolCajero || r == DomainConstants.RolCliente)
                .WithMessage("Rol inválido para este endpoint. Use Administrador, Cajero o Cliente.");

            When(x => x.Role == DomainConstants.RolCliente, () =>
            {
                RuleFor(x => x.InitialAmount)
                    .GreaterThanOrEqualTo(0).WithMessage("El monto inicial no puede ser negativo.");
            });
        }
    }
}