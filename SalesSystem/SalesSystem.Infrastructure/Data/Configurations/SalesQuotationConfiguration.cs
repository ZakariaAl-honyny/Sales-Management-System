using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SalesQuotationConfiguration : IEntityTypeConfiguration<SalesQuotation>
{
    public void Configure(EntityTypeBuilder<SalesQuotation> builder)
    {
        builder.ToTable("SalesQuotations");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuotationNo).IsRequired();
        builder.HasIndex(q => q.QuotationNo).IsUnique();

        builder.Property(q => q.QuotationDate).IsRequired().HasColumnType("date");
        builder.Property(q => q.ValidUntil).HasColumnType("date").IsRequired(false);

        builder.Property(q => q.PaymentType).HasConversion<byte>();
        builder.Property(q => q.Status).HasConversion<byte>();

        builder.Property(q => q.SubTotal).HasPrecision(18, 2);
        builder.Property(q => q.DiscountAmount).HasPrecision(18, 2);
        builder.Property(q => q.TaxAmount).HasPrecision(18, 2);
        builder.Property(q => q.TotalAmount).HasPrecision(18, 2);

        builder.Property(q => q.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(q => q.TermsAndConditions).HasMaxLength(2000).IsRequired(false);
        builder.Property(q => q.RejectionReason).HasMaxLength(1000).IsRequired(false);

        // Foreign keys
        builder.HasOne(q => q.Customer)
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Warehouse)
            .WithMany()
            .HasForeignKey(q => q.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Items collection
        builder.HasMany(q => q.Items)
            .WithOne(i => i.SalesQuotation)
            .HasForeignKey(i => i.SalesQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hide rejected quotations (similar to cancelled invoices filter)
        builder.HasQueryFilter(q => q.Status != QuotationStatus.Rejected);
    }
}

public class SalesQuotationItemConfiguration : IEntityTypeConfiguration<SalesQuotationItem>
{
    public void Configure(EntityTypeBuilder<SalesQuotationItem> builder)
    {
        builder.ToTable("SalesQuotationItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);
        builder.Property(i => i.Notes).HasMaxLength(500).IsRequired(false);

        builder.Property(i => i.ProductUnitId).IsRequired();

        // Foreign keys
        builder.HasOne(i => i.SalesQuotation)
            .WithMany(q => q.Items)
            .HasForeignKey(i => i.SalesQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProductUnit)
            .WithMany()
            .HasForeignKey(i => i.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
