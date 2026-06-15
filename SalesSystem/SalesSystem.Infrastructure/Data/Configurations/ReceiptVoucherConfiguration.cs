using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ReceiptVoucherConfiguration : IEntityTypeConfiguration<ReceiptVoucher>
{
    public void Configure(EntityTypeBuilder<ReceiptVoucher> builder)
    {
        builder.ToTable("ReceiptVouchers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VoucherNo).IsRequired();
        builder.HasIndex(x => x.VoucherNo).IsUnique();

        builder.Property(x => x.VoucherDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Status).IsRequired().HasDefaultValue((byte)1);

        // FK to Currency
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to CashBox
        builder.Property(x => x.CashBoxId).IsRequired();
        builder.HasOne(x => x.CashBox)
            .WithMany()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Account
        builder.Property(x => x.AccountId).IsRequired();
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // DocumentEntity does not use IsActive — use Status for lifecycle filtering
    }
}
