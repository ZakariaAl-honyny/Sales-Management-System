using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");
        builder.HasKey(si => si.Id);
        builder.Property(si => si.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(si => si.InvoiceNo).IsUnique();
        builder.Property(si => si.SubTotal).HasPrecision(18, 2);
        builder.Property(si => si.DiscountAmount).HasPrecision(18, 2);
        builder.Property(si => si.TaxAmount).HasPrecision(18, 2);
        builder.Property(si => si.TotalAmount).HasPrecision(18, 2);
        builder.Property(si => si.PaidAmount).HasPrecision(18, 2);
        builder.Property(si => si.DueAmount).HasPrecision(18, 2);
        builder.Property(si => si.Notes).HasMaxLength(500);
        builder.Property(si => si.PaymentType).HasConversion<byte>();
        builder.Property(si => si.Status).HasConversion<byte>();

        builder.HasOne(si => si.Customer)
            .WithMany()
            .HasForeignKey(si => si.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Warehouse)
            .WithMany()
            .HasForeignKey(si => si.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.CashBox)
            .WithMany()
            .HasForeignKey(si => si.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(si => si.Items)
            .WithOne(i => i.SalesInvoice)
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(si => si.IsActive);

        builder.ToTable(t => t.HasCheckConstraint("CHK_SalesInvoices_PaidAmount", "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]"));
    }
}

public class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.ToTable("SalesInvoiceItems");
        builder.HasKey(sii => sii.Id);
        builder.Property(sii => sii.Quantity).HasPrecision(18, 3);
        builder.Property(sii => sii.UnitPrice).HasPrecision(18, 2);
        builder.Property(sii => sii.DiscountAmount).HasPrecision(18, 2);
        builder.Property(sii => sii.LineTotal).HasPrecision(18, 2);
        builder.Property(sii => sii.Mode).HasConversion<byte>();
        builder.Property(sii => sii.Notes).HasMaxLength(250);

        builder.HasOne(sii => sii.Product)
            .WithMany()
            .HasForeignKey(sii => sii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sii => sii.IsActive);
    }
}