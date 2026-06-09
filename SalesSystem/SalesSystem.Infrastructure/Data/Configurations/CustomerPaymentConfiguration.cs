using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.SalesInvoiceId)
            .IsRequired(false);

        builder.Property(x => x.PaymentDate)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CashBoxId)
            .IsRequired(false);

        builder.Property(x => x.CurrencyId)
            .IsRequired(false);

        builder.Property(x => x.ExchangeRate)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.ReferenceNo)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashBox)
            .WithMany()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cheque is owned by the payment (1:1)
        builder.HasOne(x => x.Cheque)
            .WithOne(x => x.CustomerPayment)
            .HasForeignKey<Cheque>(x => x.CustomerPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Allocations
        builder.HasMany(x => x.Allocations)
            .WithOne(x => x.CustomerPayment)
            .HasForeignKey(x => x.CustomerPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PaymentNo)
            .IsUnique()
            .HasDatabaseName("IX_CustomerPayments_PaymentNo");

        builder.HasQueryFilter(x => x.IsActive);
    }
}
