using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="WarehouseTransferLine"/>.
/// Maps to "WarehouseTransferLines" table.
/// Schema: int WarehouseTransferId FK, int ProductUnitId FK,
/// decimal(18,3) Quantity, nvarchar(50) BatchNo.
/// Entity (no audit).
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

        builder.Property(x => x.BatchNo)
            .HasMaxLength(50)
            .IsRequired(false)
            .HasComment("رقم الدفعة المنقولة");

        // CHECK constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_WHTxLines_Quantity_Positive",
                "[Quantity] > 0");
        });

        // Relationships
        builder.HasOne(x => x.WarehouseTransfer)
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.WarehouseTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.WarehouseTransferId)
            .HasDatabaseName("IX_WHTxLines_TransferId");

        builder.HasIndex(x => x.ProductUnitId)
            .HasDatabaseName("IX_WHTxLines_ProductUnitId");
    }
}
