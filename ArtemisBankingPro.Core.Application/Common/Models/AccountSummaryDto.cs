namespace ArtemisBankingPro.Core.Application.Common.Models
{
    public record AccountSummaryDto(string AccountNumber, decimal Balance, bool IsPrincipal, string Status);
}