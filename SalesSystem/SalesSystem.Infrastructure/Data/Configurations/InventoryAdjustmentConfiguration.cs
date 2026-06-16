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

        builder.Property(ia => ia.AdjustmentNo)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(ia => ia.AdjustmentNo)
            .IsUnique()
            .HasDatabaseName("IX_InventoryAdjustments_AdjustmentNo");

        builder.Property(ia => ia.AdjustmentType)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(ia => ia.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(ia => ia.Status)
            .HasConversion<byte>()
            .IsRequired()
            .HasDefaultValue(InventoryCountStatus.Draft);

        builder.Property(ia => ia.WarehouseId)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(ia => ia.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ia => ia.CreatedByUserId)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ia => ia.PostedAt)
            .IsRequired(false);

        builder.Property(ia => ia.CancelledAt)
            .IsRequired(false);

        builder.HasOne(ia => ia.Warehouse)
            .WithMany()
            .HasForeignKey(ia => ia.WarehouseId)
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

        builder.Property(l => l.ExpectedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.ActualQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_InvAdjLines_ExpectedQuantity_NonNegative",
                "[ExpectedQuantity] >= 0");
            t.HasCheckConstraint("CHK_InvAdjLines_ActualQuantity_NonNegative",
                "[ActualQuantity] >= 0");
            t.HasCheckConstraint("CHK_InvAdjLines_UnitCost_NonNegative",
                "[UnitCost] >= 0");
        });

        builder.HasOne(l => l.InventoryAdjustment)
            .WithMany(ia => ia.Lines)
            .HasForeignKey(l => l.InventoryAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductUnit)
            .WithMany()
            .HasForeignKey(l => l.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.InventoryAdjustmentId)
            .HasDatabaseName("IX_InvAdjLines_AdjustmentId");

        builder.HasIndex(l => l.ProductUnitId)
            .HasDatabaseName("IX_InvAdjLines_ProductUnitId");
    }
}
