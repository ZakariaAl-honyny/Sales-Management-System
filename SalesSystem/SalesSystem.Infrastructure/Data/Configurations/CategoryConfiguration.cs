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
        builder.Property(u => u.Name).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Symbol).HasMaxLength(20);
        builder.Property(u => u.CreatedBy).HasMaxLength(150);
        builder.Property(u => u.UpdatedBy).HasMaxLength(150);
        builder.HasQueryFilter(u => u.IsActive);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(250);
        builder.Property(c => c.CreatedBy).HasMaxLength(150);
        builder.Property(c => c.UpdatedBy).HasMaxLength(150);
        builder.HasQueryFilter(c => c.IsActive);
    }
}