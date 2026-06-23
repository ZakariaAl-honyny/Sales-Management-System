using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // ─── PK ────────────────────────────────────────────────
        builder.HasKey(p => p.Id);

        // ─── String properties ──────────────────────────────
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.ImagePath).HasMaxLength(500);

        // ─── Barcode ────────────────────────────────────────
        builder.Property(p => p.Barcode)
            .HasColumnType("varchar")
            .HasMaxLength(50)
            .IsRequired(false)
            .HasComment("Primary barcode for quick lookup — ASCII-only, not a unique identifier");

        builder.HasIndex(p => p.Barcode)
            .IsUnique(false)
            .HasDatabaseName("IX_Products_Barcode")
            .HasFilter("[Barcode] IS NOT NULL AND [IsActive] = 1");

        // ─── Numeric properties ─────────────────────────────
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);

        // ─── Relationships ─────────────────────────────────────
        builder.HasOne(p => p.ProductCategory)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Units — dynamic UOM collection
        builder.HasMany(p => p.Units)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory batches for FIFO/FEFO costing
        builder.HasMany(p => p.InventoryBatches)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // WarehouseStocks
        builder.HasMany(p => p.WarehouseStocks)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Global query filter ───────────────────────────────
        builder.HasQueryFilter(p => p.IsActive);
    }
}
