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

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.MinStock).IsRequired().HasPrecision(18, 3);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);
        builder.Property(p => p.ConversionFactor).IsRequired().HasPrecision(18, 3).HasDefaultValue(1m);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.ImagePath).HasMaxLength(500);

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

        // New navigation collections for Phase 25
        builder.HasMany(p => p.InventoryBatches)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => p.IsActive);
    }
}