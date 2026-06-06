using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misbahuda.Domain.Entities;

namespace Misbahuda.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(u => u.PasswordHash).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.PhoneNumber);
        builder.HasIndex(u => u.Role);

        builder.HasOne(u => u.Pilgrim).WithOne(p => p.User)
            .HasForeignKey<Pilgrim>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Volunteer).WithOne(v => v.User)
            .HasForeignKey<Volunteer>(v => v.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens).WithOne(t => t.User)
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications).WithOne(n => n.User)
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
