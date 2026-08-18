using MediatR;

namespace ArtemisBankingPro.Core.Application.CreditCards.GetCreditCardById
{
    public record GetCreditCardByIdQuery(int Id) : IRequest<CreditCardDetailDto>;
}