using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// تكوين كيان مرتجع الشراء (PurchaseReturn) وجدوله في قاعدة البيانات
/// </summary>
public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns");
        builder.HasKey(pr => pr.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(pr => pr.ReturnNo).HasMaxLength(50).IsRequired();
        builder.Property(pr => pr.SubTotal).HasPrecision(18, 2);
        builder.Property(pr => pr.DiscountAmount).HasPrecision(18, 2);
        builder.Property(pr => pr.DiscountType).HasConversion<byte?>().IsRequired(false);
        builder.Property(pr => pr.DiscountRate).HasPrecision(18, 2).IsRequired(false);
        builder.Property(pr => pr.TotalAmount).HasPrecision(18, 2);
        builder.Property(pr => pr.LinkToInvoice).IsRequired();
        builder.Property(pr => pr.CurrencyId).IsRequired(false);
        builder.Property(pr => pr.ExchangeRate).HasPrecision(18, 6).IsRequired(false);
        builder.Property(pr => pr.Notes).HasMaxLength(500);
        builder.Property(pr => pr.Status).HasConversion<byte>();
        // IsStandaloneReturn is computed (not stored)
        builder.Ignore(pr => pr.IsStandaloneReturn);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(pr => pr.Supplier)
            .WithMany()
            .HasForeignKey(pr => pr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Warehouse)
            .WithMany()
            .HasForeignKey(pr => pr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Currency)
            .WithMany()
            .HasForeignKey(pr => pr.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(pr => pr.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Items collection ────────────────────────────────────────
        builder.HasMany(pr => pr.Items)
            .WithOne(pri => pri.PurchaseReturn)
            .HasForeignKey(pri => pri.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(pr => pr.IsActive);

        // ─── CHECK constraints ───────────────────────────────────────
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_PurchaseReturns_DiscountRate",
            "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)"));
    }
}

/// <summary>
/// تكوين كيان صنف مرتجع الشراء (PurchaseReturnItem) وجدوله في قاعدة البيانات
/// </summary>
public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.ToTable("PurchaseReturnItems");
        builder.HasKey(pri => pri.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(pri => pri.ProductUnitId).IsRequired();
        builder.Property(pri => pri.Quantity).HasPrecision(18, 3);
        builder.Property(pri => pri.UnitCost).HasPrecision(18, 2);
        builder.Property(pri => pri.DiscountAmount).HasPrecision(18, 2);
        builder.Property(pri => pri.LineTotal).HasPrecision(18, 2);
        builder.Property(pri => pri.CostInBaseCurrency).HasPrecision(18, 2).IsRequired(false);
        builder.Property(pri => pri.Notes).HasMaxLength(250);
        builder.Property(pri => pri.Mode).HasConversion<byte>();

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(pri => pri.Product)
            .WithMany()
            .HasForeignKey(pri => pri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pri => pri.ProductUnit)
            .WithMany()
            .HasForeignKey(pri => pri.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(pri => pri.IsActive);
    }
}
