using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class InventoryOperationConfiguration : IEntityTypeConfiguration<InventoryOperation>
{
    public void Configure(EntityTypeBuilder<InventoryOperation> builder)
    {
        builder.ToTable("InventoryOperations", t =>
        {
            t.HasCheckConstraint("CHK_InventoryOperation_Type_Range",
                "[OperationType] >= 1 AND [OperationType] <= 3");
            t.HasCheckConstraint("CHK_InventoryOperation_Status_Range",
                "[Status] >= 1 AND [Status] <= 3");
        });

        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.OperationNo)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(x => x.OperationNo)
            .IsUnique();

        builder.Property(x => x.OperationType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.OperationDate)
            .IsRequired();

        builder.Property(x => x.ReferenceNo)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.AdjustmentType)
            .HasConversion<int?>();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(InvoiceStatus.Draft);

        // Relationships
        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.InventoryOperation)
            .HasForeignKey(i => i.InventoryOperationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
