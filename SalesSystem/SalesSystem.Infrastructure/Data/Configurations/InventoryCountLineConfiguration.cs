using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryCountLineConfiguration : IEntityTypeConfiguration<InventoryCountLine>
{
    public void Configure(EntityTypeBuilder<InventoryCountLine> builder)
    {
        builder.ToTable("InventoryCountLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ExpectedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.ActualQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.Difference)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CHK_InvCountLines_ExpectedQuantity_NonNegative",
                "[ExpectedQuantity] >= 0");
            t.HasCheckConstraint("CHK_InvCountLines_ActualQuantity_NonNegative",
                "[ActualQuantity] >= 0");
        });

        builder.HasOne(l => l.InventoryCount)
            .WithMany(ic => ic.Lines)
            .HasForeignKey(l => l.InventoryCountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductUnit)
            .WithMany()
            .HasForeignKey(l => l.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.InventoryCountId)
            .HasDatabaseName("IX_InvCountLines_CountId");

        builder.HasIndex(l => l.ProductUnitId)
            .HasDatabaseName("IX_InvCountLines_ProductUnitId");
    }
}
