using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="WarehouseTransferLine"/>.
/// Maps to "WarehouseTransferLines" table.
/// </summary>
public class WarehouseTransferLineConfiguration : IEntityTypeConfiguration<WarehouseTransferLine>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferLine> builder)
    {
        builder.ToTable("WarehouseTransferLines");
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
            t.HasCheckConstraint("CHK_WHTxLines_Quantity_Positive",
                "[Quantity] > 0");
            t.HasCheckConstraint("CHK_WHTxLines_UnitCost_NonNegative",
                "[UnitCost] >= 0");
        });

        // Relationships
        builder.HasOne(x => x.WarehouseTransfer)
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.WarehouseTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.WarehouseTransferId)
            .HasDatabaseName("IX_WHTxLines_TransferId");

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_WHTxLines_ProductId");
    }
}
