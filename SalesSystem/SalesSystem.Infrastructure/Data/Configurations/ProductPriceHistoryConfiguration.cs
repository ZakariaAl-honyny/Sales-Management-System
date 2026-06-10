using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.ToTable("ProductPriceHistories");

        builder.HasKey(x => x.Id);

        // ─── Legacy fields (backward compatibility) ──────────────────────

        builder.Property(x => x.ChangeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CostingMethod)
            .HasMaxLength(50);

        builder.Property(x => x.OldValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.NewValue)
            .HasPrecision(18, 2);

        // ─── Detailed price history fields ───────────────────────────────

        builder.Property(x => x.OldRetailPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.NewRetailPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.OldWholesalePrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.NewWholesalePrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.OldCost)
            .HasColumnName("OldAvgCost")
            .HasPrecision(18, 2);

        builder.Property(x => x.NewCost)
            .HasColumnName("NewAvgCost")
            .HasPrecision(18, 2);

        builder.Property(x => x.ChangeReason)
            .HasMaxLength(500);

        // ─── Timestamp & required fields ─────────────────────────────────

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.Property(x => x.ProductUnitId)
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .IsRequired();

        builder.Property(x => x.ChangedByUserId)
            .IsRequired();

        // ─── Foreign keys ────────────────────────────────────────────────

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────────

        builder.HasQueryFilter(x => x.IsActive);
    }
}
