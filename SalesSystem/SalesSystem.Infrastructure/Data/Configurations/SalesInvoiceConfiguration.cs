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
        builder.HasIndex(si => si.InvoiceNo).IsUnique();
        builder.Property(si => si.SubTotal).HasPrecision(18, 2);
        builder.Property(si => si.DiscountAmount).HasPrecision(18, 2);
        builder.Property(si => si.TaxAmount).HasPrecision(18, 2);
        builder.Property(si => si.OtherCharges).HasPrecision(18, 2);
        builder.Property(si => si.NetTotal).HasPrecision(18, 2);
        builder.Property(si => si.PaidAmount).HasPrecision(18, 2);
        builder.Property(si => si.RemainingAmount).HasPrecision(18, 2);
        builder.Property(si => si.Notes).HasMaxLength(500);
        builder.Property(si => si.InvoiceDate).HasColumnType("date");
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

        builder.HasOne(si => si.Tax)
            .WithMany()
            .HasForeignKey(si => si.TaxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Currency)
            .WithMany()
            .HasForeignKey(si => si.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(si => si.ExchangeRate).HasPrecision(18, 6).IsRequired(false);

        builder.HasMany(si => si.Items)
            .WithOne(i => i.SalesInvoice)
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(si => si.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);

        builder.ToTable(t => t.HasCheckConstraint("CHK_SalesInvoices_PaidAmount", "[PaidAmount] >= 0 AND [PaidAmount] <= [NetTotal]"));
    }
}

public class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable("SalesInvoiceLines");
        builder.HasKey(sii => sii.Id);
        builder.Property(sii => sii.Quantity).HasPrecision(18, 3);
        builder.Property(sii => sii.UnitPrice).HasPrecision(18, 2);
        builder.Property(sii => sii.LineTotal).HasPrecision(18, 2);
        builder.Property(sii => sii.ProductUnitId).IsRequired();

        builder.HasOne(sii => sii.Product)
            .WithMany()
            .HasForeignKey(sii => sii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sii => sii.ProductUnit)
            .WithMany()
            .HasForeignKey(sii => sii.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
