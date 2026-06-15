using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.ToTable("Cheques");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChequeNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BankName)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.BankBranch)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.PaymentId)
            .IsRequired(false);

        builder.Property(x => x.CustomerReceiptId)
            .IsRequired(false);

        builder.Property(x => x.ReceiptVoucherId)
            .IsRequired(false);

        builder.Property(x => x.PaymentVoucherId)
            .IsRequired(false);

        builder.Property(x => x.IssueDate)
            .IsRequired();

        builder.Property(x => x.MaturityDate)
            .IsRequired(false);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Navigation: SupplierPayment
        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: CustomerReceipt
        builder.HasOne(x => x.CustomerReceipt)
            .WithMany()
            .HasForeignKey(x => x.CustomerReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: ReceiptVoucher
        builder.HasOne(x => x.ReceiptVoucher)
            .WithMany()
            .HasForeignKey(x => x.ReceiptVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: PaymentVoucher
        builder.HasOne(x => x.PaymentVoucher)
            .WithMany()
            .HasForeignKey(x => x.PaymentVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChequeNumber)
            .IsUnique()
            .HasDatabaseName("IX_Cheques_ChequeNumber");

        builder.HasQueryFilter(x => x.IsActive);
    }
}
