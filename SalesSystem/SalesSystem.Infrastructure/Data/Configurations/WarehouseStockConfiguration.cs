using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="WarehouseStock"/> entity.
/// No ReorderLevel — replaced with AvgCost (decimal(18,2)).
/// </summary>
public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(ws => ws.Id);

        builder.Property(ws => ws.Quantity)
            .IsRequired()
            .HasPrecision(18, 3);

        builder.Property(ws => ws.AvgCost)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0)
            .HasComment("متوسط التكلفة المرجح");

        builder.HasIndex(ws => new { ws.WarehouseId, ws.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_WarehouseStocks_Warehouse_Product");

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_WarehouseStocks_Quantity_NonNegative",
            "[Quantity] >= 0"));

        builder.HasOne(ws => ws.Warehouse)
            .WithMany()
            .HasForeignKey(ws => ws.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.Product)
            .WithMany(p => p.WarehouseStocks)
            .HasForeignKey(ws => ws.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
