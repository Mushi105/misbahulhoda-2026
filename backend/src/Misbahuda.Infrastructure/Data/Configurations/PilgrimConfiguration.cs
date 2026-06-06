using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misbahuda.Domain.Entities;

namespace Misbahuda.Infrastructure.Data.Configurations;

public class PilgrimConfiguration : IEntityTypeConfiguration<Pilgrim>
{
    public void Configure(EntityTypeBuilder<Pilgrim> builder)
    {
        builder.ToTable("Pilgrims");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PassportNumber).IsRequired(false).HasMaxLength(50);
        builder.Property(p => p.Country).IsRequired().HasMaxLength(100);

        builder.HasIndex(p => p.PassportNumber).IsUnique().HasFilter("\"PassportNumber\" IS NOT NULL");
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasMany(p => p.Documents).WithOne(f => f.Pilgrim)
            .HasForeignKey(f => f.PilgrimId).OnDelete(DeleteBehavior.Cascade);
    }
}
