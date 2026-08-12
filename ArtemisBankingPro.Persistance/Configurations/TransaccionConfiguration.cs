using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class TransaccionConfiguration : IEntityTypeConfiguration<Transaccion>
{
    public void Configure(EntityTypeBuilder<Transaccion> builder)
    {
        builder.Property(x => x.Monto).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Origen).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Destino).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MotivoRechazo).HasMaxLength(250);

        builder.HasOne(x => x.CuentaAhorro)
            .WithMany(c => c.Transacciones)
            .HasForeignKey(x => x.CuentaAhorroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TarjetaCredito)
            .WithMany(t => t.Consumos)
            .HasForeignKey(x => x.TarjetaCreditoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Prestamo)
            .WithMany()
            .HasForeignKey(x => x.PrestamoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para acelerar el listado de transacciones por cuenta ordenado por fecha (lo vas a usar mucho)
        builder.HasIndex(x => new { x.CuentaAhorroId, x.Fecha });
    }
}
