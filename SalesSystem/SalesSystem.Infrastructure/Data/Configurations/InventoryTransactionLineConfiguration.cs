using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="InventoryTransactionLine"/>.
/// Maps to "InventoryTransactionLines" table.
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

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("التكلفة الإجمالية للسطر");

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

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(x => x.InventoryTransactionId)
            .HasDatabaseName("IX_InvTxLines_TransactionId");

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_InvTxLines_ProductId");
    }
}
