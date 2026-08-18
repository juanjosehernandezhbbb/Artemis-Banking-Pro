using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Persistance.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

    public static async Task SeedCommerceAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        if (await context.Comercios.AnyAsync())
            return;

        var user = await userManager.FindByNameAsync("comercio1");
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = "comercio1",
                Email = "comercio1@artemisbanking.com",
                EmailConfirmed = true,
                Nombre = "Comercio",
                Apellido = "Demo",
                Cedula = "00100000004",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, DomainConstants.RolComercio);
            }
        }

        var random = new Random();
        var accountNumber = random.Next(100000000, 999999999).ToString();
        while (await context.CuentasAhorro.AnyAsync(c => c.NumeroCuenta == accountNumber) ||
               await context.Prestamos.AnyAsync(p => p.NumeroPrestamo == accountNumber))
        {
            accountNumber = random.Next(100000000, 999999999).ToString();
        }

        var cuenta = new CuentaAhorro
        {
            NumeroCuenta = accountNumber,
            ClienteId = user.Id,
            Balance = 0m,
            TipoCuenta = TipoCuenta.Principal,
            Estado = EstadoCuenta.Activa,
            FechaCreacion = DateTime.UtcNow
        };

        var comercio = new Comercio
        {
            Name = "Comercio Demo",
            Description = "Comercio de prueba para pagos Hermes Pay",
            Email = "contacto@comerciodemo.com",
            PhoneNumber = "8095551234",
            Rnc = "101999999",
            IsActive = true,
            FechaCreacion = DateTime.UtcNow,
            ApplicationUserId = user.Id
        };

        await context.CuentasAhorro.AddAsync(cuenta);
        await context.Comercios.AddAsync(comercio);
        await context.SaveChangesAsync();
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