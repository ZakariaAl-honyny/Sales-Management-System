using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class StockWriteOffConfiguration : IEntityTypeConfiguration<StockWriteOff>
{
    public void Configure(EntityTypeBuilder<StockWriteOff> builder)
    {
        builder.ToTable("StockWriteOffs");

        builder.HasKey(x => x.Id);

        // ─── Properties ───────────────────────────────────────────────────

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 3);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.WriteOffDate)
            .IsRequired();

        // ─── Foreign keys ────────────────────────────────────────────────

        builder.HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────────

        builder.HasQueryFilter(w => w.IsActive);
    }
}
