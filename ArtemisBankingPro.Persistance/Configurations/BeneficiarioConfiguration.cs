using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class BeneficiarioConfiguration : IEntityTypeConfiguration<Beneficiario>
{
    public void Configure(EntityTypeBuilder<Beneficiario> builder)
    {
        builder.HasOne(x => x.Cliente)
            .WithMany(u => u.Beneficiarios)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CuentaAhorro)
            .WithMany(c => c.RegistradaComoBeneficiariaEn)
            .HasForeignKey(x => x.CuentaAhorroId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un cliente no debería poder agregar la misma cuenta como beneficiario dos veces
        builder.HasIndex(x => new { x.ClienteId, x.CuentaAhorroId }).IsUnique();
    }
}
