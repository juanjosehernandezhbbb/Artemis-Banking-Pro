using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Commerces.UpdateCommerce
{
    public class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
    {
        public UpdateCommerceCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del comercio es obligatorio.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("El correo electrónico es obligatorio.").EmailAddress();
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("El teléfono es obligatorio.");
            RuleFor(x => x.Rnc).NotEmpty().WithMessage("El RNC es obligatorio.");
        }
    }
}