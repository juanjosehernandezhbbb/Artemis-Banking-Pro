using MediatR;
using System;

namespace ArtemisBankingPro.Core.Application.HermesPay.GetCommerceTransactions
{
    public record GetCommerceTransactionsQuery(int CommerceId, int Page = 1, int PageSize = 20)
        : IRequest<CommerceTransactionsDto>;
}