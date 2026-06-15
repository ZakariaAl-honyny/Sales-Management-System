using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierPaymentApplicationConfiguration : IEntityTypeConfiguration<SupplierPaymentApplication>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentApplication> builder)
    {
        builder.ToTable("SupplierPaymentApplications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppliedAmount).HasPrecision(18, 2).IsRequired();

        // FK back to SupplierPayment
        builder.HasOne(a => a.SupplierPayment)
            .WithMany()
            .HasForeignKey(a => a.SupplierPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to PurchaseInvoice
        builder.HasOne(a => a.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(a => a.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
