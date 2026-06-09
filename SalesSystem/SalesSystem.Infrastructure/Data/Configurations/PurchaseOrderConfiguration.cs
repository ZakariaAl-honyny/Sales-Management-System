using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// تكوين كيان أمر الشراء (PurchaseOrder) وجدول أمر الشراء في قاعدة البيانات
/// </summary>
public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(po => po.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(po => po.OrderNo).IsRequired();
        builder.Property(po => po.ExchangeRate).HasPrecision(18, 6).IsRequired(false);
        builder.Property(po => po.OrderDate).IsRequired();
        // DateOnly maps to date in SQL Server via EF Core 8+
        builder.Property(po => po.ExpectedDate).IsRequired(false);
        builder.Property(po => po.Status).HasConversion<byte>();
        builder.Property(po => po.SubTotal).HasPrecision(18, 2);
        builder.Property(po => po.DiscountAmount).HasPrecision(18, 2);
        builder.Property(po => po.TaxAmount).HasPrecision(18, 2);
        builder.Property(po => po.TotalAmount).HasPrecision(18, 2);
        builder.Property(po => po.Notes).HasMaxLength(500);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(po => po.Supplier)
            .WithMany()
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.Warehouse)
            .WithMany()
            .HasForeignKey(po => po.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.Currency)
            .WithMany()
            .HasForeignKey(po => po.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Items collection ────────────────────────────────────────
        builder.HasMany(po => po.Items)
            .WithOne(i => i.PurchaseOrder)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(po => po.IsActive);
    }
}

/// <summary>
/// تكوين كيان صنف أمر الشراء (PurchaseOrderItem) وجدوله في قاعدة البيانات
/// </summary>
public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");
        builder.HasKey(poi => poi.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(poi => poi.Quantity).HasPrecision(18, 3);
        builder.Property(poi => poi.ReceivedQuantity).HasPrecision(18, 3);
        // PendingReceiveQuantity is computed (not stored)
        builder.Ignore(poi => poi.PendingReceiveQuantity);
        builder.Property(poi => poi.UnitCost).HasPrecision(18, 2);
        builder.Property(poi => poi.LineTotal).HasPrecision(18, 2);
        builder.Property(poi => poi.Notes).HasMaxLength(250);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(poi => poi.Product)
            .WithMany()
            .HasForeignKey(poi => poi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(poi => poi.ProductUnit)
            .WithMany()
            .HasForeignKey(poi => poi.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(poi => poi.IsActive);
    }
}
