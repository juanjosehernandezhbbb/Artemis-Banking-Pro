namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(string userId, string userName, string role, int? commerceId = null);
    }
}