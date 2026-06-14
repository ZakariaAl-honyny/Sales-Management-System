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
        builder.Property(l => l.SystemQuantity).HasPrecision(18, 3);
        builder.Property(l => l.ActualQuantity).HasPrecision(18, 3);
        builder.Property(l => l.Difference).HasPrecision(18, 3);

        builder.HasOne(l => l.InventoryCount)
            .WithMany(ic => ic.Lines)
            .HasForeignKey(l => l.InventoryCountId)
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
