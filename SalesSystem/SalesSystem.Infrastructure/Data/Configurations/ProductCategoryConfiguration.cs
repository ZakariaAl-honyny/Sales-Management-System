using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Name).IsRequired().HasMaxLength(100);
        builder.Property(pc => pc.Description).HasMaxLength(500);

        // Unique filtered index on Name — allows soft-deleted records to coexist with active ones
        builder.HasIndex(pc => pc.Name)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_ProductCategories_Name");

        builder.HasQueryFilter(pc => pc.IsActive);
    }
}
