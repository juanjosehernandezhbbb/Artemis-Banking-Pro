using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.HasIndex(x => x.NumeroPrestamo).IsUnique();
        builder.Property(x => x.NumeroPrestamo).HasMaxLength(9).IsFixedLength().IsRequired();
        builder.Property(x => x.CapitalAprobado).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TasaInteresAnual).HasColumnType("decimal(5,2)");

        builder.HasOne(x => x.Cliente)
            .WithMany(u => u.Prestamos)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
