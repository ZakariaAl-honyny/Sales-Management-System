using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Warehouse"/> entity.
/// Maps to "Warehouses" table — smallint PK, no AccountId/IsDefault/Notes.
/// </summary>
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        // smallint PK
        builder.Property(w => w.Id)
            .HasColumnType("smallint")
            .ValueGeneratedOnAdd();

        // === Properties ===
        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(10)
            .HasComment("كود المستودع — فريد");

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("اسم المستودع");

        builder.Property(w => w.Type)
            .HasConversion<int>()
            .HasDefaultValue(WarehouseType.Main)
            .IsRequired();

        builder.Property(w => w.Location)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(w => w.Phone)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(w => w.Address)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(w => w.ManagerName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        // === FK: BranchId → Branches ===
        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // === Indexes ===
        builder.HasIndex(w => w.Code)
            .IsUnique()
            .HasDatabaseName("IX_Warehouses_Code");

        builder.HasIndex(w => w.BranchId)
            .HasDatabaseName("IX_Warehouses_BranchId");

        // === Global query filter — soft delete ===
        builder.HasQueryFilter(w => w.IsActive);
    }
}
