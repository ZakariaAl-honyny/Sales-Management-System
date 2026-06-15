using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments");
        builder.HasKey(ia => ia.Id);
        builder.Property(ia => ia.AdjustmentNo).IsRequired();
        builder.Property(ia => ia.AdjustmentDate).IsRequired().HasColumnType("date");
        builder.Property(ia => ia.AdjustmentType).HasConversion<byte>().IsRequired();
        builder.Property(ia => ia.Status).HasConversion<byte>().IsRequired();

        builder.HasOne(ia => ia.Warehouse)
            .WithMany()
            .HasForeignKey(ia => ia.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ia => ia.Account)
            .WithMany()
            .HasForeignKey(ia => ia.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ia => ia.Lines)
            .WithOne(l => l.InventoryAdjustment)
            .HasForeignKey(l => l.InventoryAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ia => ia.Status != InventoryCountStatus.Cancelled);
    }
}

public class InventoryAdjustmentLineConfiguration : IEntityTypeConfiguration<InventoryAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentLine> builder)
    {
        builder.ToTable("InventoryAdjustmentLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);

        builder.HasOne(l => l.InventoryAdjustment)
            .WithMany(ia => ia.Lines)
            .HasForeignKey(l => l.InventoryAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductUnit)
            .WithMany()
            .HasForeignKey(l => l.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
