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
        builder.Property(pc => pc.NameEn).HasMaxLength(100);
        builder.Property(pc => pc.Description).HasMaxLength(500);
        builder.Property(pc => pc.SortOrder).HasDefaultValue(0);

        // Self-referencing parent relationship
        builder.HasOne(pc => pc.Parent)
            .WithMany(pc => pc.Children)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasQueryFilter(pc => pc.IsActive);
    }
}
