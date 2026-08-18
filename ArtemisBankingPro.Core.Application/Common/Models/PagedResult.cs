using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Common.Models
{
    public record PagedResult<T>(int Page, int PageSize, int TotalRecords, int TotalPages, IReadOnlyList<T> Data);
}