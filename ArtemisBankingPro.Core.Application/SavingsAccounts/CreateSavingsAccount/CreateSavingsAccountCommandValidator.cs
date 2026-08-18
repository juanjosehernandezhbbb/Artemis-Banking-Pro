using FluentValidation;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount
{
    public class CreateSavingsAccountCommandValidator : AbstractValidator<CreateSavingsAccountCommand>
    {
        public CreateSavingsAccountCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0)
                .WithMessage("El balance inicial no puede ser negativo.");
        }
    }
}