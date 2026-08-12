using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class CuentaAhorroConfiguration : IEntityTypeConfiguration<CuentaAhorro>
{
    public void Configure(EntityTypeBuilder<CuentaAhorro> builder)
    {
        builder.HasIndex(x => x.NumeroCuenta).IsUnique();
        builder.Property(x => x.NumeroCuenta).HasMaxLength(9).IsFixedLength().IsRequired();
        builder.Property(x => x.Balance).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Cliente)
            .WithMany(u => u.CuentasAhorro)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
