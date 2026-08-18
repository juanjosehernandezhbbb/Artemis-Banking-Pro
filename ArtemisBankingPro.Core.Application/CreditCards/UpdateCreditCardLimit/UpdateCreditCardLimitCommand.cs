using MediatR;

namespace ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit
{
    public record UpdateCreditCardLimitCommand(int Id, decimal CreditLimit) : IRequest;
}