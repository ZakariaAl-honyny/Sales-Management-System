using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="InventoryTransaction"/>.
/// Maps to "InventoryTransactions" table.
/// </summary>
public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.TransactionNo)
            .IsRequired()
            .HasComment("رقم المعاملة — فريد");

        builder.HasIndex(x => x.TransactionNo)
            .IsUnique()
            .HasDatabaseName("IX_InventoryTransactions_TransactionNo");

        builder.Property(x => x.TransactionDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("تاريخ المعاملة");

        builder.Property(x => x.TransactionType)
            .HasConversion<byte>()
            .IsRequired()
            .HasComment("نوع المعاملة (مشتريات، مبيعات، تحويل، تسوية، إلخ)");

        builder.Property(x => x.ReferenceType)
            .HasConversion<byte>()
            .IsRequired(false)
            .HasComment("نوع المستند المرجعي");

        builder.Property(x => x.ReferenceId)
            .IsRequired(false)
            .HasComment("معرف المستند المرجعي");

        builder.Property(x => x.WarehouseId)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Status as byte for InvoiceStatus (Draft=1, Posted=2, Cancelled=3)
        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired()
            .HasDefaultValue(Domain.Enums.InvoiceStatus.Draft);

        // Relationships
        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.InventoryTransaction)
            .HasForeignKey(l => l.InventoryTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId })
            .HasDatabaseName("IX_InventoryTransactions_Reference")
            .HasFilter("[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName("IX_InventoryTransactions_WarehouseId");

        // Global query filter — exclude cancelled
        builder.HasQueryFilter(x => x.Status != Domain.Enums.InvoiceStatus.Cancelled);
    }
}
