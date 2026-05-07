using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments");
        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.PaymentNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(cp => cp.PaymentNo).IsUnique();
        builder.Property(cp => cp.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(cp => cp.PaymentMethod).IsRequired();
        builder.Property(cp => cp.ReferenceNo).HasMaxLength(50);
        builder.Property(cp => cp.Notes).HasMaxLength(500);

        builder.HasOne(cp => cp.Customer)
            .WithMany()
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.PaymentNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(sp => sp.PaymentNo).IsUnique();
        builder.Property(sp => sp.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(sp => sp.PaymentMethod).IsRequired();
        builder.Property(sp => sp.ReferenceNo).HasMaxLength(50);
        builder.Property(sp => sp.Notes).HasMaxLength(500);

        builder.HasOne(sp => sp.Supplier)
            .WithMany()
            .HasForeignKey(sp => sp.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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
        builder.Property(ss => ss.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(ss => ss.DefaultTaxRate).HasPrecision(5, 2);
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
    }
}