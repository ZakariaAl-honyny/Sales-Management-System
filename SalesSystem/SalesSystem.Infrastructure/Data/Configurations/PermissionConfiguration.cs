using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        // Code (was Name) — unique, required, max 100
        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Code).IsUnique();

        // DisplayName — required, max 150 (schema: nvarchar(150))
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(150);

        // Category — now non-nullable, required, max 100
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);

        // IsSystem — protects system permissions
        builder.Property(p => p.IsSystem).IsRequired().HasDefaultValue(false);

        builder.HasQueryFilter(p => p.IsActive);

        // Navigation: RolePermissions
        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
