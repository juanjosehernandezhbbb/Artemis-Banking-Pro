using MediatR;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerceById
{
    public record GetCommerceByIdQuery(int Id) : IRequest<CommerceDetailDto>;
}