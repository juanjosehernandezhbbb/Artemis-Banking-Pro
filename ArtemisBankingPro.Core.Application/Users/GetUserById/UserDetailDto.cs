using ArtemisBankingPro.Core.Application.Common.Models;
using System;

namespace ArtemisBankingPro.Core.Application.Users.GetUserById
{
    public record UserDetailDto(
        string Id,
        string UserName,
        string Identification,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        bool IsActive,
        DateTime CreatedAt,
        AccountSummaryDto? MainAccount);
}