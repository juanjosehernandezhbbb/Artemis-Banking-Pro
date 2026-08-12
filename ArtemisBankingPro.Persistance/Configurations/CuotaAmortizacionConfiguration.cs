using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class CuotaAmortizacionConfiguration : IEntityTypeConfiguration<CuotaAmortizacion>
{
    public void Configure(EntityTypeBuilder<CuotaAmortizacion> builder)
    {
        builder.Property(x => x.ValorCuota).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MontoInteres).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MontoCapital).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SaldoPendiente).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Prestamo)
            .WithMany(p => p.Cuotas)
            .HasForeignKey(x => x.PrestamoId)
            .OnDelete(DeleteBehavior.Cascade); // única cascada: una cuota no existe sin su préstamo
    }
}
