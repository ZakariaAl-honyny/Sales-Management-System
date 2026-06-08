using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", t =>
        {
            t.HasCheckConstraint("CHK_Warehouse_Type_Range", "[Type] >= 1 AND [Type] <= 4");
        });
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.Type).IsRequired().HasConversion<int>().HasDefaultValue(WarehouseType.Main);
        builder.Property(w => w.Location).HasMaxLength(250);
        builder.Property(w => w.Phone).HasMaxLength(20);
        builder.Property(w => w.Address).HasMaxLength(250);
        builder.Property(w => w.ManagerName).HasMaxLength(100);
        builder.Property(w => w.Notes).HasMaxLength(500);
        builder.HasOne(w => w.Account)
            .WithMany()
            .HasForeignKey(w => w.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(w => w.IsActive);
    }
}

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(ws => ws.Id);
        builder.Property(ws => ws.Quantity).IsRequired().HasPrecision(18, 3);
        builder.Property(ws => ws.ReorderLevel).IsRequired().HasPrecision(18, 3);
        builder.HasIndex(ws => new { ws.WarehouseId, ws.ProductId }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CHK_WarehouseStocks_Quantity_NonNegative", "[Quantity] >= 0"));

        builder.HasOne(ws => ws.Warehouse)
            .WithMany()
            .HasForeignKey(ws => ws.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.Product)
            .WithMany(p => p.WarehouseStocks)
            .HasForeignKey(ws => ws.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ws => ws.IsActive);
    }
}