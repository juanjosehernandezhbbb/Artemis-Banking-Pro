using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Persistance.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        // Create roles
        string[] roles = new[] { DomainConstants.RolAdministrador, DomainConstants.RolCajero, DomainConstants.RolCliente, DomainConstants.RolComercio };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default users
        await CrearUsuarioPorDefecto(userManager, "admin", "Admin", "Principal", "00100000001", DomainConstants.RolAdministrador);
        await CrearUsuarioPorDefecto(userManager, "cajero1", "Cajero", "Principal", "00100000002", DomainConstants.RolCajero);
        await CrearUsuarioPorDefecto(userManager, "cliente1", "Cliente", "Principal", "00100000003", DomainConstants.RolCliente);
    }

    private static async Task CrearUsuarioPorDefecto(UserManager<ApplicationUser> userManager, string username, string nombre, string apellido, string cedula, string rol)
    {
        var existing = await userManager.FindByNameAsync(username);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@artemisbanking.com",
            EmailConfirmed = true,
            Nombre = nombre,
            Apellido = apellido,
            Cedula = cedula,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, rol);
        }
    }
}
