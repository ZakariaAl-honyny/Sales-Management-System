using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="InventoryBatch"/> entity.
/// WarehouseId is smallint FK; includes navigation to PurchaseInvoice.
/// </summary>
public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.ToTable("InventoryBatches");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.BatchNo)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("رقم الدفعة (nvarchar 50)");

        builder.Property(x => x.WarehouseId)
            .HasColumnType("smallint")
            .IsRequired()
            .HasComment("معرف المستودع (smallint FK)");

        builder.Property(x => x.QuantityReceived)
            .HasPrecision(18, 3)
            .IsRequired()
            .HasComment("الكمية المستلمة في الدفعة");

        builder.Property(x => x.QuantityRemaining)
            .HasPrecision(18, 3)
            .IsRequired()
            .HasComment("الكمية المتبقية في الدفعة");

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("تكلفة الوحدة عند الشراء");

        builder.Property(x => x.SupplierBatchNo)
            .HasMaxLength(100)
            .IsRequired(false)
            .HasComment("رقم الدفعة من المورد");

        builder.Property(x => x.ExpiryDate)
            .HasColumnType("date")
            .IsRequired(false)
            .HasComment("تاريخ انتهاء الصلاحية");

        builder.Property(x => x.IsClosed)
            .HasColumnType("bit")
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("هل الدفعة مغلقة (تم استهلاكها بالكامل)");

        // CHECK constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_InventoryBatches_QuantityReceived_NonNegative",
                "[QuantityReceived] >= 0");
            t.HasCheckConstraint("CHK_InventoryBatches_QuantityRemaining_NonNegative",
                "[QuantityRemaining] >= 0");
            t.HasCheckConstraint("CHK_InventoryBatches_UnitCost_NonNegative",
                "[UnitCost] >= 0");
            t.HasCheckConstraint("CHK_InventoryBatches_IsClosed_Consistency",
                "([IsClosed] = 0 AND [QuantityRemaining] > 0) OR ([IsClosed] = 1 AND [QuantityRemaining] <= 0)");
        });

        // Relationships
        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryBatches)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.PurchaseInvoiceLine)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceLineId)
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

        builder.HasIndex(x => x.PurchaseInvoiceId)
            .HasDatabaseName("IX_InventoryBatches_PurchaseInvoiceId");
    }
}
