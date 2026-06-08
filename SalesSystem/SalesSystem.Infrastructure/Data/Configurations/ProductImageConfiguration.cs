using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.ImagePath)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("مسار ملف الصورة");

        builder.Property(x => x.IsPrimary)
            .HasDefaultValue(false)
            .HasComment("صورة رئيسية للمنتج");

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0)
            .HasComment("ترتيب العرض");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
