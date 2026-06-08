using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryOperationItemConfiguration : IEntityTypeConfiguration<InventoryOperationItem>
{
    public void Configure(EntityTypeBuilder<InventoryOperationItem> builder)
    {
        builder.ToTable("InventoryOperationItems", t =>
        {
            t.HasCheckConstraint("CHK_InventoryOperationItem_Quantity_Positive",
                "[Quantity] > 0");
        });

        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.StockIssueReason)
            .HasConversion<int?>();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(x => new { x.InventoryOperationId, x.ProductId });

        // Relationships
        builder.HasOne(x => x.InventoryOperation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InventoryOperationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
