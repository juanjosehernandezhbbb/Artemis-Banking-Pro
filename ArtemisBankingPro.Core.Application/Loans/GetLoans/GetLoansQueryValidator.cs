using ArtemisBankingPro.Core.Domain.Common;
using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Loans.GetLoans
{
    public class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
    {
        public GetLoansQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.");
            RuleFor(x => x.PageSize).LessThanOrEqualTo(DomainConstants.TamanoPaginaMaximo)
                .WithMessage($"El valor máximo permitido para pageSize es {DomainConstants.TamanoPaginaMaximo}.");

            When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
            {
                RuleFor(x => x.Status!.ToLower())
                    .Must(s => s == "activos" || s == "completados" || s == "todos")
                    .WithMessage("El parámetro status solo puede tener los valores: activos, completados o todos.");
            });
        }
    }
}