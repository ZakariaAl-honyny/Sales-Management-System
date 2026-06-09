using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(po => po.Id);
        builder.Property(po => po.OrderNo).IsRequired();
        builder.HasIndex(po => po.OrderNo).IsUnique();
        builder.Property(po => po.OrderDate).IsRequired();
        builder.Property(po => po.Status).HasConversion<byte>();
        builder.Property(po => po.SubTotal).HasPrecision(18, 2);
        builder.Property(po => po.DiscountAmount).HasPrecision(18, 2);
        builder.Property(po => po.TaxAmount).HasPrecision(18, 2);
        builder.Property(po => po.TotalAmount).HasPrecision(18, 2);
        builder.Property(po => po.ExchangeRate).HasPrecision(18, 6).IsRequired(false);
        builder.Property(po => po.Notes).HasMaxLength(500);

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

        builder.HasMany(po => po.Items)
            .WithOne(i => i.PurchaseOrder)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(po => po.IsActive);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");
        builder.HasKey(poi => poi.Id);
        builder.Property(poi => poi.Quantity).HasPrecision(18, 3);
        builder.Property(poi => poi.ReceivedQuantity).HasPrecision(18, 3).HasDefaultValue(0);
        builder.Ignore(poi => poi.PendingReceiveQuantity); // Computed property
        builder.Property(poi => poi.UnitCost).HasPrecision(18, 2);
        builder.Property(poi => poi.LineTotal).HasPrecision(18, 2);
        builder.Property(poi => poi.Notes).HasMaxLength(250);

        builder.HasOne(poi => poi.Product)
            .WithMany()
            .HasForeignKey(poi => poi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(poi => poi.ProductUnit)
            .WithMany()
            .HasForeignKey(poi => poi.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(poi => poi.IsActive);
    }
}
