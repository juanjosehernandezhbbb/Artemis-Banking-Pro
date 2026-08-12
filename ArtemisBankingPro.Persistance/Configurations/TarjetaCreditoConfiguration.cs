using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class TarjetaCreditoConfiguration : IEntityTypeConfiguration<TarjetaCredito>
{
    public void Configure(EntityTypeBuilder<TarjetaCredito> builder)
    {
        builder.HasIndex(x => x.NumeroTarjeta).IsUnique();
        builder.Property(x => x.NumeroTarjeta).HasMaxLength(16).IsFixedLength().IsRequired();
        builder.Property(x => x.LimiteCredito).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MontoAdeudado).HasColumnType("decimal(18,2)");

        // Propiedad calculada en C#, no es columna de base de datos
        builder.Ignore(x => x.CreditoDisponible);

        builder.HasOne(x => x.Cliente)
            .WithMany(u => u.TarjetasCredito)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
