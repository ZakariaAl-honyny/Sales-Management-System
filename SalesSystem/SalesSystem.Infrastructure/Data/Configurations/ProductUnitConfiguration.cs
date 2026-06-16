using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.HasKey(x => x.Id);

        // ─── Properties ────────────────────────────────────────
        builder.Property(x => x.UnitId)
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(x => x.Factor)
            .HasColumnName("Factor")
            .HasPrecision(18, 3)
            .IsRequired();

        // ─── CHECK constraint: base units must have Factor = 1 ──
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_ProductUnits_BaseUnitFactor",
            "IsBaseUnit = 0 OR Factor = 1"));

        // ─── Relationships ──────────────────────────────────────
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Unique index: one unit per product ──────────────
        builder.HasIndex(x => new { x.ProductId, x.UnitId })
            .IsUnique()
            .HasDatabaseName("IX_ProductUnits_ProductId_UnitId");
    }
}
