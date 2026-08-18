using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Application.Users.GetCommerces;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool Succeeded, ApplicationUser? User)> ValidateCredentialsAsync(string userName, string password);
        Task<IList<string>> GetRolesAsync(ApplicationUser user);
        Task<bool> ConfirmAccountAsync(string token);

        Task<ApplicationUser?> FindByUserNameAsync(string userName);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

        Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);

        Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(ApplicationUser user, string password, string role);
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);

        Task<(IReadOnlyList<UserDto> Users, int TotalCount)> GetUsersPagedAsync(int page, int pageSize, string? role);
        Task<UserDetailDto?> GetUserDetailByIdAsync(string id);

        Task<(IReadOnlyList<CommerceUserDto> Users, int TotalCount)> GetCommercesPagedAsync(int page, int pageSize);

        Task<bool> CheckCedulaExistsAsync(string cedula, string? excludeUserId = null);
        Task<bool> CheckEmailExistsAsync(string email, string? excludeUserId = null);
        Task<bool> CheckUserNameExistsAsync(string userName, string? excludeUserId = null);

        Task<bool> UpdateUserAdminAsync(string id, string firstName, string lastName, string identification, string email, string userName, string? newPassword);

        Task SetUserActiveStatusAsync(string id, bool status);

        Task<int> GetActiveClientCountAsync();
        Task<int?> GetCommerceIdByUserIdAsync(string userId);
        Task<string?> GetUserIdByCedulaAsync(string cedula);
    }
}