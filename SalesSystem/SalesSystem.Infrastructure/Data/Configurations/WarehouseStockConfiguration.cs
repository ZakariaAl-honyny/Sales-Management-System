using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Code).HasMaxLength(30);
        builder.HasIndex(w => w.Code).IsUnique();
        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.Location).HasMaxLength(250);
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
        builder.HasIndex(ws => new { ws.WarehouseId, ws.ProductId }).IsUnique();
        
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