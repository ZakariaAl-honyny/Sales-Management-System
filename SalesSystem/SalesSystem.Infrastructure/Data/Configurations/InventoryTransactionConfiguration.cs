using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="InventoryTransaction"/>.
/// Maps to "InventoryTransactions" table.
/// Schema: nvarchar(50) TransactionNo (unique), tinyint MovementType,
/// nvarchar(500) Notes, smallint WarehouseId FK,
/// int? ReferenceId, nvarchar(50) ReferenceType,
/// int CreatedByUserId, datetime2 CreatedAt.
/// BaseEntity with CreatedAt only — no Status, no lifecycle.
/// </summary>
public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.TransactionNo)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("رقم المعاملة — فريد");

        builder.HasIndex(x => x.TransactionNo)
            .IsUnique()
            .HasDatabaseName("IX_InventoryTransactions_TransactionNo");

        builder.Property(x => x.MovementType)
            .HasConversion<byte>()
            .IsRequired()
            .HasComment("نوع الحركة (مشتريات، مبيعات، تحويل، تسوية، إلخ)");

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

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("تاريخ الإنشاء");

        builder.Property(x => x.CreatedByUserId)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("معرف المستخدم المنشئ");

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
    }
}
