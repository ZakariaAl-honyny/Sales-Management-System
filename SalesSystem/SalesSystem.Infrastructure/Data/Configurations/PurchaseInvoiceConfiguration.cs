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
        builder.HasIndex(pi => pi.InvoiceNo).IsUnique();
        builder.Property(pi => pi.SubTotal).HasPrecision(18, 2);
        builder.Property(pi => pi.DiscountAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.TaxAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.OtherCharges).HasPrecision(18, 2);
        builder.Property(pi => pi.NetTotal).HasPrecision(18, 2);
        builder.Property(pi => pi.PaidAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.RemainingAmount).HasPrecision(18, 2);
        builder.Property(pi => pi.Notes).HasMaxLength(500);
        builder.Property(pi => pi.PaymentType).HasConversion<byte>();
        builder.Property(pi => pi.Status).HasConversion<byte>();

        builder.HasOne(pi => pi.Supplier)
            .WithMany()
            .HasForeignKey(pi => pi.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Warehouse)
            .WithMany()
            .HasForeignKey(pi => pi.WarehouseId)
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

        builder.Property(pi => pi.AttachmentPath).HasMaxLength(255).IsRequired(false);

        builder.HasMany(pi => pi.Items)
            .WithOne(i => i.PurchaseInvoice)
            .HasForeignKey(i => i.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(pi => pi.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);

        builder.ToTable(t => t.HasCheckConstraint("CHK_PurchaseInvoices_PaidAmount", "[PaidAmount] >= 0 AND [PaidAmount] <= [NetTotal]"));
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
        builder.Property(pii => pii.LineTotal).HasPrecision(18, 2);
        builder.Property(pii => pii.ProductUnitId).IsRequired();

        builder.HasOne(pii => pii.Product)
            .WithMany()
            .HasForeignKey(pii => pii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pii => pii.ProductUnit)
            .WithMany()
            .HasForeignKey(pii => pii.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
