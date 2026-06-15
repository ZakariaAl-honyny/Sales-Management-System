using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable("InventoryCounts");
        builder.HasKey(ic => ic.Id);
        builder.Property(ic => ic.CountNo).IsRequired();
        builder.Property(ic => ic.CountDate).IsRequired().HasColumnType("date");
        builder.Property(ic => ic.Status).HasConversion<byte>();
        builder.Property(ic => ic.Notes).HasMaxLength(500);

        builder.HasOne(ic => ic.Warehouse)
            .WithMany()
            .HasForeignKey(ic => ic.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(ic => ic.PostedByUser)
            .WithMany()
            .HasForeignKey(ic => ic.PostedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(ic => ic.Lines)
            .WithOne(l => l.InventoryCount)
            .HasForeignKey(l => l.InventoryCountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ic => ic.Status != InventoryCountStatus.Cancelled);
    }
}
