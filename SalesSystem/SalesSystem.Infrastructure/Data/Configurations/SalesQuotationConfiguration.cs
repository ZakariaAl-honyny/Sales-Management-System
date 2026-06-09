using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// تكوين كيان عرض السعر (SalesQuotation) وجدول عرض السعر في قاعدة البيانات
/// </summary>
public class SalesQuotationConfiguration : IEntityTypeConfiguration<SalesQuotation>
{
    public void Configure(EntityTypeBuilder<SalesQuotation> builder)
    {
        builder.ToTable("SalesQuotations");
        builder.HasKey(q => q.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.HasIndex(q => q.QuotationNo).IsUnique().HasFilter("[IsActive] = 1");
        builder.Property(q => q.QuotationDate).IsRequired();
        builder.Property(q => q.ExpiryDate).IsRequired(false);
        builder.Property(q => q.Status).HasConversion<byte>();
        builder.Property(q => q.SubTotal).HasPrecision(18, 2);
        builder.Property(q => q.DiscountAmount).HasPrecision(18, 2);
        builder.Property(q => q.TaxAmount).HasPrecision(18, 2);
        builder.Property(q => q.TotalAmount).HasPrecision(18, 2);
        builder.Property(q => q.Notes).HasMaxLength(500);
        builder.Property(q => q.ExchangeRate).HasPrecision(18, 6).IsRequired(false);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(q => q.Customer)
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Warehouse)
            .WithMany()
            .HasForeignKey(q => q.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Currency)
            .WithMany()
            .HasForeignKey(q => q.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Items collection ────────────────────────────────────────
        builder.HasMany(q => q.Items)
            .WithOne(i => i.Quotation)
            .HasForeignKey(i => i.SalesQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(q => q.IsActive);
    }
}

/// <summary>
/// تكوين كيان صنف عرض السعر (SalesQuotationItem) وجدوله في قاعدة البيانات
/// </summary>
public class SalesQuotationItemConfiguration : IEntityTypeConfiguration<SalesQuotationItem>
{
    public void Configure(EntityTypeBuilder<SalesQuotationItem> builder)
    {
        builder.ToTable("SalesQuotationItems");
        builder.HasKey(qi => qi.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(qi => qi.Quantity).HasPrecision(18, 3);
        builder.Property(qi => qi.UnitPrice).HasPrecision(18, 2);
        builder.Property(qi => qi.DiscountAmount).HasPrecision(18, 2);
        builder.Property(qi => qi.LineTotal).HasPrecision(18, 2);
        builder.Property(qi => qi.Mode).HasConversion<byte>();
        builder.Property(qi => qi.Notes).HasMaxLength(250);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(qi => qi.Product)
            .WithMany()
            .HasForeignKey(qi => qi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(qi => qi.IsActive);
    }
}
