using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.NameEn).HasMaxLength(100);
        builder.Property(u => u.Symbol).HasMaxLength(20);
        builder.Property(u => u.IsSystem).HasDefaultValue(false);

        builder.HasIndex(u => u.Name)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasQueryFilter(u => u.IsActive);
    }
}
