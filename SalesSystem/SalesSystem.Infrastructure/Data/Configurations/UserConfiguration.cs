using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // Core fields — matches schema §1.9
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);

        // Lock state — bit flag, default unlocked
        builder.Property(u => u.IsLocked).IsRequired().HasDefaultValue(false);

        // Password policy
        builder.Property(u => u.MustChangePassword).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.LoginAttempts).IsRequired().HasDefaultValue(0);

        // PermissionsMask — bitmask of granted permissions (0 = none)
        builder.Property(u => u.PermissionsMask).IsRequired().HasDefaultValue(0L);

        builder.Property(u => u.LastLoginAt).IsRequired(false);

        // Query filter — soft delete via IsActive (from ActivatableEntity)
        builder.HasQueryFilter(u => u.IsActive);

        // Navigation: UserRoles (many-to-many via join table)
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
