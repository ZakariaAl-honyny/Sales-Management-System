using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerReceiptConfiguration : IEntityTypeConfiguration<CustomerReceipt>
{
    public void Configure(EntityTypeBuilder<CustomerReceipt> builder)
    {
        builder.ToTable("CustomerReceipts");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceiptNo).IsRequired();
        builder.Property(r => r.ReceiptDate).IsRequired().HasColumnType("date");
        builder.Property(r => r.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.Status).HasConversion<byte>().IsRequired();
        builder.Property(r => r.PaymentMethod).HasConversion<byte>().HasColumnType("tinyint").HasDefaultValue(PaymentMethod.Cash).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(500).IsRequired(false);

        // FK to Customer
        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // FK to CashBox
        builder.HasOne(r => r.CashBox)
            .WithMany()
            .HasForeignKey(r => r.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Applications (allocations to sales invoices)
        builder.HasMany(r => r.Applications)
            .WithOne(a => a.CustomerReceipt)
            .HasForeignKey(a => a.CustomerReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("IX_CustomerReceipts_CustomerId");

        builder.HasIndex(r => r.ReceiptNo)
            .IsUnique()
            .HasDatabaseName("IX_CustomerReceipts_ReceiptNo");

        builder.HasQueryFilter(r => r.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);
    }
}
