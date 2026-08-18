using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Application.Users.GetCommerces;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Persistance.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ArtemisBankingPro.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IdentityService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<(bool Succeeded, ApplicationUser? User)> ValidateCredentialsAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return (false, null);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid) return (false, null);

            return (true, user);
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> ConfirmAccountAsync(string token)
        {
            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(token);
                var decodedToken = Encoding.UTF8.GetString(decodedBytes);
                var parts = decodedToken.Split(':');

                if (parts.Length != 2) return false;

                var userId = parts[0];
                var nativeToken = parts[1];

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.ConfirmEmailAsync(user, nativeToken);
                if (!result.Succeeded) return false;

                user.Activo = true;
                await _userManager.UpdateAsync(user);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public async Task<ApplicationUser?> FindByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            user.Activo = false;
            await _userManager.UpdateAsync(user);

            var nativeToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var combined = $"{user.Id}:{nativeToken}";

            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(combined));
        }

        public async Task<bool> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(token);
                var decodedToken = Encoding.UTF8.GetString(decodedBytes);
                var parts = decodedToken.Split(':');

                if (parts.Length != 2) return false;

                var tokenUserId = parts[0];
                var nativeToken = parts[1];

                if (userId != tokenUserId) return false;

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.ResetPasswordAsync(user, nativeToken, newPassword);
                if (!result.Succeeded) return false;

                user.Activo = true;
                await _userManager.UpdateAsync(user);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(ApplicationUser user, string password, string role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return (false, roleResult.Errors.Select(e => e.Description));
            }

            return (true, Array.Empty<string>());
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
        {
            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task SetUserActiveStatusAsync(string id, bool status)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.Activo = status;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task<bool> CheckCedulaExistsAsync(string cedula, string? excludeUserId = null)
        {
            return await _context.Users.AnyAsync(u => u.Cedula == cedula && u.Id != excludeUserId);
        }

        public async Task<bool> CheckEmailExistsAsync(string email, string? excludeUserId = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null && user.Id != excludeUserId;
        }

        public async Task<bool> CheckUserNameExistsAsync(string userName, string? excludeUserId = null)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return user != null && user.Id != excludeUserId;
        }

        public async Task<bool> UpdateUserAdminAsync(string id, string firstName, string lastName, string identification, string email, string userName, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            user.Nombre = firstName;
            user.Apellido = lastName;
            user.Cedula = identification;
            user.Email = email;
            user.UserName = userName;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return false;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!passResult.Succeeded) return false;
            }

            return true;
        }

        public async Task<(IReadOnlyList<UserDto> Users, int TotalCount)> GetUsersPagedAsync(int page, int pageSize, string? role)
        {
            var query = from u in _context.Users.AsNoTracking()
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where r.Name != DomainConstants.RolComercio
                        select new { User = u, RoleName = r.Name };

            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleLower = role.ToLower();
                query = query.Where(x => x.RoleName.ToLower() == roleLower);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(x => x.User.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserDto(
                    x.User.Id,
                    x.User.UserName,
                    x.User.Cedula,
                    x.User.Nombre,
                    x.User.Apellido,
                    x.User.Email,
                    x.RoleName,
                    x.User.Activo
                ))
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<int> GetActiveClientCountAsync()
        {
            return await (from u in _context.Users.AsNoTracking()
                          join ur in _context.UserRoles on u.Id equals ur.UserId
                          join r in _context.Roles on ur.RoleId equals r.Id
                          where r.Name == DomainConstants.RolCliente && u.Activo
                          select u.Id).CountAsync();
        }

        public async Task<UserDetailDto?> GetUserDetailByIdAsync(string id)
        {
            var userQuery = from u in _context.Users.AsNoTracking()
                            where u.Id == id
                            join ur in _context.UserRoles on u.Id equals ur.UserId
                            join r in _context.Roles on ur.RoleId equals r.Id
                            select new { User = u, RoleName = r.Name };

            var userData = await userQuery.FirstOrDefaultAsync();

            if (userData == null)
            {
                return null;
            }

            AccountSummaryDto? mainAccountDto = null;

            var cuentaPrincipal = await _context.CuentasAhorro
                .AsNoTracking()
                .Where(c => c.ClienteId == id && c.TipoCuenta == TipoCuenta.Principal)
                .FirstOrDefaultAsync();

            if (cuentaPrincipal != null)
            {
                mainAccountDto = new AccountSummaryDto(
                    cuentaPrincipal.NumeroCuenta,
                    cuentaPrincipal.Balance,
                    true,
                    cuentaPrincipal.Estado.ToString()
                );
            }

            return new UserDetailDto(
                userData.User.Id,
                userData.User.UserName,
                userData.User.Cedula,
                userData.User.Nombre,
                userData.User.Apellido,
                userData.User.Email,
                userData.RoleName,
                userData.User.Activo,
                userData.User.FechaCreacion,
                mainAccountDto
            );
        }

        public async Task<(IReadOnlyList<CommerceUserDto> Users, int TotalCount)> GetCommercesPagedAsync(int page, int pageSize)
        {
            var query = from u in _context.Users.AsNoTracking()
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        join c in _context.Comercios on u.Id equals c.ApplicationUserId
                        where r.Name == DomainConstants.RolComercio
                        select new { User = u, RoleName = r.Name, Commerce = c };

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(x => x.User.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CommerceUserDto(
                    x.User.Id,
                    x.User.UserName,
                    x.User.Cedula,
                    x.User.Nombre,
                    x.User.Apellido,
                    x.User.Email,
                    x.RoleName,
                    x.Commerce.Id,
                    x.Commerce.Name,
                    x.User.Activo))
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<int?> GetCommerceIdByUserIdAsync(string userId)
        {
            return await _context.Comercios
                .AsNoTracking()
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetUserIdByCedulaAsync(string cedula)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Cedula == cedula)
                .Select(u => (string?)u.Id)
                .FirstOrDefaultAsync();
        }
    }
}