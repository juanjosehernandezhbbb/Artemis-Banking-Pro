using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;
using System;

namespace ArtemisBankingPro.Core.Application.SavingsAccounts.GetSavingsAccounts
{
    public record GetSavingsAccountsQuery(
        int Page = 1,
        int PageSize = 20,
        string? Identification = null,
        string? Status = null,
        string? Type = null)
        : IRequest<PagedResult<SavingsAccountListItemDto>>;
}