using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(30);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.HasIndex(p => p.Barcode).IsUnique();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.PurchasePrice).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.SalePrice).IsRequired().HasPrecision(18, 2); // Legacy
        builder.Property(p => p.WholesalePrice).IsRequired().HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(p => p.RetailPrice).IsRequired().HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(p => p.MinStock).IsRequired().HasPrecision(18, 3);
        builder.Property(p => p.ConversionFactor).IsRequired().HasPrecision(18, 3).HasDefaultValue(1m);
        builder.Property(p => p.Description).HasMaxLength(500);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Unit)
            .WithMany()
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.WholesaleUnit)
            .WithMany()
            .HasForeignKey(p => p.WholesaleUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RetailUnit)
            .WithMany()
            .HasForeignKey(p => p.RetailUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => p.IsActive);
    }
}