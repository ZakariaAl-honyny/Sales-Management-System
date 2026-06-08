using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.ToTable("InventoryBatches");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired()
            .HasComment("الكمية المتبقية في الدفعة");

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("تكلفة الوحدة عند الشراء");

        builder.Property(x => x.BatchNo)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("رقم الدفعة / رقم التشغيلة");

        builder.Property(x => x.ManufactureDate)
            .IsRequired(false)
            .HasComment("تاريخ التصنيع");

        builder.Property(x => x.ExpiryDate)
            .IsRequired(false)
            .HasComment("تاريخ انتهاء الصلاحية");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // CHECK constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_InventoryBatches_Quantity_NonNegative",
                "[Quantity] >= 0");
            t.HasCheckConstraint("CHK_InventoryBatches_UnitCost_NonNegative",
                "[UnitCost] >= 0");
        });

        // Relationships
        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryBatches)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseInvoiceItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for query performance
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId })
            .HasDatabaseName("IX_InventoryBatches_Product_Warehouse");

        builder.HasIndex(x => x.BatchNo)
            .HasDatabaseName("IX_InventoryBatches_BatchNo");

        builder.HasIndex(x => x.ExpiryDate)
            .HasDatabaseName("IX_InventoryBatches_ExpiryDate")
            .HasFilter("[ExpiryDate] IS NOT NULL");

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
