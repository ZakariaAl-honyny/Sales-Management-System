using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AllocatedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.InvoiceType)
            .IsRequired();

        builder.Property(x => x.CustomerPaymentId)
            .IsRequired(false);

        builder.Property(x => x.SupplierPaymentId)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.CustomerPayment)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.CustomerPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupplierPayment)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.SupplierPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CustomerPaymentId, x.InvoiceId })
            .HasDatabaseName("IX_PaymentAllocations_CustomerPaymentId_InvoiceId");

        builder.HasIndex(x => new { x.SupplierPaymentId, x.InvoiceId })
            .HasDatabaseName("IX_PaymentAllocations_SupplierPaymentId_InvoiceId");

        builder.HasQueryFilter(x => x.IsActive);
    }
}
