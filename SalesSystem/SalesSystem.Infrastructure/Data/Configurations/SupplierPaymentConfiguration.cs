using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentNo)
            .IsRequired();

        builder.Property(x => x.SupplierId)
            .IsRequired();

        builder.Property(x => x.PaymentDate)
            .HasColumnType("date");

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.CashBoxId)
            .IsRequired(false);

        builder.Property(x => x.CurrencyId)
            .IsRequired();

        builder.Property(x => x.ReferenceNo)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashBox)
            .WithMany()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PaymentNo)
            .IsUnique()
            .HasDatabaseName("IX_SupplierPayments_PaymentNo");

        builder.HasQueryFilter(x => x.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);
    }
}
