using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Persistance.Configurations;

public class ComercioConfiguration : IEntityTypeConfiguration<Comercio>
{
    public void Configure(EntityTypeBuilder<Comercio> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Rnc).HasMaxLength(11).IsRequired();

        builder.HasIndex(x => x.Rnc).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasOne(x => x.ApplicationUser)
            .WithOne(u => u.Comercio)
            .HasForeignKey<Comercio>(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
