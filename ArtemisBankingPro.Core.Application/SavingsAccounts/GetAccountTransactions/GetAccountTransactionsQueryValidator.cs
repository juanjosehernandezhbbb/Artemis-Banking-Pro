using ArtemisBankingPro.Core.Domain.Common;
using FluentValidation;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetAccountTransactions
{
    public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
    {
        public GetAccountTransactionsQueryValidator()
        {
            RuleFor(x => x.AccountNumber).NotEmpty();
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.");
            RuleFor(x => x.PageSize).LessThanOrEqualTo(DomainConstants.TamanoPaginaMaximo)
                .WithMessage($"El valor máximo permitido para pageSize es {DomainConstants.TamanoPaginaMaximo}.");
        }
    }
}