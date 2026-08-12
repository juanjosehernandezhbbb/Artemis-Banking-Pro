using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Persistance.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<CuentaAhorro> CuentasAhorro => Set<CuentaAhorro>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();
    public DbSet<CuotaAmortizacion> CuotasAmortizacion => Set<CuotaAmortizacion>();
    public DbSet<TarjetaCredito> TarjetasCredito => Set<TarjetaCredito>();
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<Comercio> Comercios => Set<Comercio>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // primero Identity, o se rompen las tablas AspNet*

        // Aplica TODAS las clases IEntityTypeConfiguration<> que existan en este ensamblado.
        // No hace falta llamar cada una a mano ni agregar una línea nueva cada vez que crees una configuración.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
