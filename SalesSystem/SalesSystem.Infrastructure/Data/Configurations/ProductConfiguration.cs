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
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        // Barcode is varchar(50) for ASCII-only barcodes — not nvarchar
        builder.Property(p => p.Barcode)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired(false)
            .HasComment("Primary barcode for quick lookup — ASCII-only, not a unique identifier");
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.ImagePath).HasMaxLength(500);

        // ─── Numeric properties ─────────────────────────────
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);

        // ─── Indexes ───────────────────────────────────────────
        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [IsActive] = 1")
            .HasDatabaseName("IX_Products_Barcode");

        // ─── Relationships ─────────────────────────────────────
        builder.HasOne(p => p.ProductCategory)
            .WithMany(pc => pc.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Tax)
            .WithMany()
            .HasForeignKey(p => p.TaxId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

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
