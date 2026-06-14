using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerReceiptApplicationConfiguration : IEntityTypeConfiguration<CustomerReceiptApplication>
{
    public void Configure(EntityTypeBuilder<CustomerReceiptApplication> builder)
    {
        builder.ToTable("CustomerReceiptApplications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppliedAmount).HasPrecision(18, 2).IsRequired();

        // FK back to CustomerReceipt
        builder.HasOne(a => a.CustomerReceipt)
            .WithMany(r => r.Applications)
            .HasForeignKey(a => a.CustomerReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to SalesInvoice
        builder.HasOne(a => a.SalesInvoice)
            .WithMany()
            .HasForeignKey(a => a.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
