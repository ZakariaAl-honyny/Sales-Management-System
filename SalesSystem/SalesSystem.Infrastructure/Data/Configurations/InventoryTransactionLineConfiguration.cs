using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="InventoryTransactionLine"/>.
/// Maps to "InventoryTransactionLines" table.
/// Schema: int PK, int InventoryTransactionId FK, int ProductUnitId FK,
/// decimal(18,3) Quantity, decimal(18,2) UnitCost,
/// nvarchar(50) BatchNo (nullable), date ExpiryDate (nullable), smallint WarehouseId (nullable).
/// Entity (no audit).
/// </summary>
public class InventoryTransactionLineConfiguration : IEntityTypeConfiguration<InventoryTransactionLine>
{
    public void Configure(EntityTypeBuilder<InventoryTransactionLine> builder)
    {
        builder.ToTable("InventoryTransactionLines");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired()
            .HasComment("الكمية بوحدات التخزين الأساسية");

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("تكلفة الوحدة");

        builder.Property(x => x.BatchNo)
            .HasMaxLength(50)
            .IsRequired(false)
            .HasComment("رقم الدفعة");

        builder.Property(x => x.ExpiryDate)
            .HasColumnType("date")
            .IsRequired(false)
            .HasComment("تاريخ انتهاء الصلاحية");

        builder.Property(x => x.WarehouseId)
            .HasColumnType("smallint")
            .IsRequired(false)
            .HasComment("معرف المستودع (إذا كان مختلفاً عن المستودع الرئيسي)");

        // CHECK constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_InvTxLines_Quantity_Positive",
                "[Quantity] > 0");
            t.HasCheckConstraint("CHK_InvTxLines_UnitCost_NonNegative",
                "[UnitCost] >= 0");
        });

        // Relationships
        builder.HasOne(x => x.InventoryTransaction)
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.InventoryTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.InventoryTransactionId)
            .HasDatabaseName("IX_InvTxLines_TransactionId");

        builder.HasIndex(x => x.ProductUnitId)
            .HasDatabaseName("IX_InvTxLines_ProductUnitId");
    }
}
