using ArtemisBankingPro.Core.Domain.Common;
using FluentValidation;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts
{
    public class GetSavingsAccountsQueryValidator : AbstractValidator<GetSavingsAccountsQuery>
    {
        public GetSavingsAccountsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.");
            RuleFor(x => x.PageSize).LessThanOrEqualTo(DomainConstants.TamanoPaginaMaximo)
                .WithMessage($"El valor máximo permitido para pageSize es {DomainConstants.TamanoPaginaMaximo}.");

            When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
            {
                RuleFor(x => x.Status!.ToLower())
                    .Must(s => s == "activa" || s == "cancelada" || s == "todas")
                    .WithMessage("El parámetro status solo puede tener los valores: activa, cancelada o todas.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Type), () =>
            {
                RuleFor(x => x.Type!.ToLower())
                    .Must(s => s == "principal" || s == "secundaria" || s == "todas")
                    .WithMessage("El parámetro type solo puede tener los valores: principal, secundaria o todas.");
            });
        }
    }
}