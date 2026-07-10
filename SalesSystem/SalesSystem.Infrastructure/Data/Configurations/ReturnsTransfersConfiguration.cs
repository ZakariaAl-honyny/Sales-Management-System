using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");
        builder.HasKey(sr => sr.Id);
        builder.Property(sr => sr.ReturnNo).IsRequired();
        builder.HasIndex(sr => sr.ReturnNo).IsUnique();
        builder.Property(sr => sr.ReturnDate).IsRequired().HasColumnType("date");
        builder.Property(sr => sr.TotalAmount).HasPrecision(18, 2);
        builder.Property(sr => sr.ReturnedDiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(sr => sr.ReturnedTaxAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(sr => sr.ReturnedChargeAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(sr => sr.RefundAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(sr => sr.TaxId).HasColumnType("smallint").IsRequired(false);
        builder.Property(sr => sr.ReturnReason).HasMaxLength(500).IsRequired(false);
        builder.Property(sr => sr.Notes).HasMaxLength(500);
        builder.Property(sr => sr.Status).HasConversion<byte>();
        builder.Property(sr => sr.BaseNetTotal).HasPrecision(18, 2).IsRequired(false);

        builder.HasOne(sr => sr.SalesInvoice)
            .WithMany()
            .HasForeignKey(sr => sr.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.Customer)
            .WithMany()
            .HasForeignKey(sr => sr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.Warehouse)
            .WithMany()
            .HasForeignKey(sr => sr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sr => sr.Lines)
            .WithOne(l => l.SalesReturn)
            .HasForeignKey(l => l.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sr => sr.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);
    }
}

public class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("SalesReturnLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.CostInBaseCurrency).HasPrecision(18, 2).IsRequired(false);

        builder.HasOne(l => l.SalesReturn)
            .WithMany(sr => sr.Lines)
            .HasForeignKey(l => l.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.SalesInvoiceLine)
            .WithMany()
            .HasForeignKey(l => l.SalesInvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
