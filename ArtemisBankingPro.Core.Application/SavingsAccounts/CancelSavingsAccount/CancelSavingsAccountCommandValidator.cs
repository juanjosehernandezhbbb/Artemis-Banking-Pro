using FluentValidation;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount
{
    public class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
    {
        public CancelSavingsAccountCommandValidator()
        {
            RuleFor(x => x.AccountNumber).NotEmpty();
        }
    }
}