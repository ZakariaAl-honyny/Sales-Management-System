using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(im => im.Id);
        builder.Property(im => im.QuantityChange).IsRequired().HasPrecision(18, 3);
        builder.Property(im => im.QuantityBefore).IsRequired().HasPrecision(18, 3);
        builder.Property(im => im.QuantityAfter).IsRequired().HasPrecision(18, 3);
        builder.Property(im => im.ReferenceType).IsRequired().HasMaxLength(30);
        builder.Property(im => im.ReferenceId).IsRequired();
        builder.Property(im => im.UnitCost).HasPrecision(18, 2);
        builder.Property(im => im.Notes).HasMaxLength(500);

        builder.HasIndex(im => new { im.ProductId, im.MovementDate });
        builder.HasIndex(im => new { im.ReferenceType, im.ReferenceId });

        builder.HasOne(im => im.Product)
            .WithMany()
            .HasForeignKey(im => im.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(im => im.Warehouse)
            .WithMany()
            .HasForeignKey(im => im.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(im => im.IsActive);
    }
}

public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings");
        builder.HasKey(ss => ss.Id);
        builder.Property(ss => ss.StoreName).IsRequired().HasMaxLength(150);
        builder.Property(ss => ss.Phone).HasMaxLength(20);
        builder.Property(ss => ss.Address).HasMaxLength(250);
        builder.Property(ss => ss.LogoPath).HasMaxLength(255);
        builder.Property(ss => ss.Email).HasMaxLength(100);
        builder.Property(ss => ss.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(ss => ss.DefaultTaxRate).HasPrecision(18, 2);
        builder.Property(ss => ss.TaxNumber).HasMaxLength(50);
        builder.Property(ss => ss.InvoicePrefix).HasMaxLength(20).HasDefaultValue("INV");
        builder.Property(ss => ss.SignaturePath).HasMaxLength(255);

        builder.HasQueryFilter(ss => ss.IsActive);
    }
}

public class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("DocumentSequences");
        builder.HasKey(ds => ds.Id);
        builder.Property(ds => ds.DocumentType).IsRequired().HasMaxLength(10);
        builder.HasIndex(ds => ds.DocumentType).IsUnique();
        builder.Property(ds => ds.Prefix).IsRequired().HasMaxLength(10);
        builder.Property(ds => ds.Year).IsRequired();
        builder.Property(ds => ds.LastNumber).IsRequired();

        builder.HasQueryFilter(ds => ds.IsActive);
    }
}