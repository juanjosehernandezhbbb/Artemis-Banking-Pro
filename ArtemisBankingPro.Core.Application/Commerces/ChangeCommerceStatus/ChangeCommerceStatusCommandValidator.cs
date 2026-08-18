using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Commerces.ChangeCommerceStatus
{
    public class ChangeCommerceStatusCommandValidator : AbstractValidator<ChangeCommerceStatusCommand>
    {
        public ChangeCommerceStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Status).NotNull().WithMessage("El campo status es obligatorio.");
        }
    }
}