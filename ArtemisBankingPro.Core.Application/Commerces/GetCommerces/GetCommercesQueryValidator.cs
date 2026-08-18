using ArtemisBankingPro.Core.Domain.Common;
using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerces
{
    public class GetCommercesQueryValidator : AbstractValidator<GetCommercesQuery>
    {
        public GetCommercesQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.");
            RuleFor(x => x.PageSize).LessThanOrEqualTo(DomainConstants.TamanoPaginaMaximo)
                .WithMessage($"El valor máximo permitido para pageSize es {DomainConstants.TamanoPaginaMaximo}.");

            When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
            {
                RuleFor(x => x.Status!.ToLower())
                    .Must(s => s == "activo" || s == "inactivo" || s == "todos")
                    .WithMessage("El parámetro status solo puede tener los valores: activo, inactivo o todos.");
            });
        }
    }
}