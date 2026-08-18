using MediatR;

namespace ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard
{
    public record CancelCreditCardCommand(int Id) : IRequest;
}