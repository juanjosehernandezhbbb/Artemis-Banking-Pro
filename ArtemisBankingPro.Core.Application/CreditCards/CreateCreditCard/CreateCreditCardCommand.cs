using MediatR;

namespace ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard
{
    public record CreateCreditCardCommand(
        string ClientId,
        decimal CreditLimit,
        string PerformedByUserId = default!
    ) : IRequest<CreditCardResponseDto>;
}