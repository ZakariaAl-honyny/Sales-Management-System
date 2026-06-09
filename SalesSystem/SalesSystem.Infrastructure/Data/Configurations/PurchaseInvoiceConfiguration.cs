using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("PurchaseInvoices");
        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.SubTotal).HasPrecision(18, 2);
        builder.Property(pi => pi.DiscountAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.TaxAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.TotalAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.PaidAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.DueAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.SupplierInvoiceNo).HasMaxLength(50);
        builder.Property(pi => pi.Notes).HasMaxLength(500);
        builder.Property(pi => pi.PaymentType).HasConversion<byte>();
        builder.Property(pi => pi.Status).HasConversion<byte>();

        // Phase 27 — Additional purchase invoice properties
        builder.Property(pi => pi.AdditionalFeesTotal).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(pi => pi.AttachmentPath).HasMaxLength(255).IsRequired(false);
        builder.Property(pi => pi.DiscountType).HasConversion<byte?>().IsRequired(false);
        builder.Property(pi => pi.DiscountRate).HasPrecision(18, 2).IsRequired(false);

        builder.HasOne(pi => pi.Supplier)
            .WithMany()
            .HasForeignKey(pi => pi.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Warehouse)
            .WithMany()
            .HasForeignKey(pi => pi.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.CashBox)
            .WithMany()
            .HasForeignKey(pi => pi.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Tax)
            .WithMany()
            .HasForeignKey(pi => pi.TaxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Currency)
            .WithMany()
            .HasForeignKey(pi => pi.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.ExchangeRate).HasPrecision(18, 6).IsRequired(false);

        builder.HasMany(pi => pi.Items)
            .WithOne(i => i.PurchaseInvoice)
            .HasForeignKey(i => i.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(pi => pi.IsActive);

        builder.ToTable(t => t.HasCheckConstraint("CHK_PurchaseInvoices_PaidAmount", "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]"));
    }
}

public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.ToTable("PurchaseInvoiceItems");
        builder.HasKey(pii => pii.Id);
        builder.Property(pii => pii.Quantity).HasPrecision(18, 3);
        builder.Property(pii => pii.UnitCost).HasPrecision(18, 2);
        builder.Property(pii => pii.DiscountAmount).HasPrecision(18, 2);
        builder.Property(pii => pii.LineTotal).HasPrecision(18, 2);
        builder.Property(pii => pii.Notes).HasMaxLength(250);

        // Phase 27 — Additional purchase invoice item properties
        builder.Property(pii => pii.ProductUnitId).IsRequired();
        builder.Property(pii => pii.DiscountType).HasConversion<byte?>().IsRequired(false);
        builder.Property(pii => pii.DiscountRate).HasPrecision(18, 2).IsRequired(false);
        builder.Property(pii => pii.CostInBaseCurrency).HasPrecision(18, 2).IsRequired(false);
        builder.Property(pii => pii.AdditionalFeesAmount).HasPrecision(18, 2).HasDefaultValue(0);

        builder.HasOne(pii => pii.Product)
            .WithMany()
            .HasForeignKey(pii => pii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pii => pii.ProductUnit)
            .WithMany()
            .HasForeignKey(pii => pii.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pii => pii.ProductUnitId).HasDatabaseName("IX_PurchaseInvoiceItems_ProductUnitId");

        builder.HasQueryFilter(pii => pii.IsActive);
    }
}