using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("السعر");

        builder.Property(x => x.EffectiveFrom)
            .HasColumnType("date")
            .IsRequired()
            .HasComment("تاريخ بدء السريان");

        builder.Property(x => x.EffectiveTo)
            .HasColumnType("date")
            .IsRequired(false)
            .HasComment("تاريخ انتهاء السريان (اختياري)");

        // Relationships
        builder.HasOne(x => x.ProductUnit)
            .WithMany() // ProductUnit has no Prices navigation — prices managed via ProductPrice table directly
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHECK constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_ProductPrices_Price_NonNegative",
            "[Price] >= 0"));

        // Indexes
        builder.HasIndex(x => new { x.ProductUnitId, x.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("IX_ProductPrices_ProductUnit_Date");
    }
}
