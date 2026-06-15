using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="WarehouseTransfer"/>.
/// Maps to "WarehouseTransfers" table.
/// </summary>
public class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.ToTable("WarehouseTransfers");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.TransferNo)
            .IsRequired()
            .HasComment("رقم التحويل — فريد");

        builder.HasIndex(x => x.TransferNo)
            .IsUnique()
            .HasDatabaseName("IX_WarehouseTransfers_TransferNo");

        builder.Property(x => x.TransferDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("تاريخ التحويل");

        builder.Property(x => x.FromWarehouseId)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.ToWarehouseId)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Status as byte
        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired()
            .HasDefaultValue(Domain.Enums.InvoiceStatus.Draft);

        // Relationships
        builder.HasOne(x => x.FromWarehouse)
            .WithMany()
            .HasForeignKey(x => x.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToWarehouse)
            .WithMany()
            .HasForeignKey(x => x.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.WarehouseTransfer)
            .HasForeignKey(l => l.WarehouseTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.FromWarehouseId)
            .HasDatabaseName("IX_WarehouseTransfers_SourceWarehouseId");

        builder.HasIndex(x => x.ToWarehouseId)
            .HasDatabaseName("IX_WarehouseTransfers_DestWarehouseId");

        // Global query filter
        builder.HasQueryFilter(x => x.Status != Domain.Enums.InvoiceStatus.Cancelled);
    }
}
