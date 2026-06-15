using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

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
        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(150)
            .HasComment("اسم المستودع");

        builder.Property(w => w.Phone)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(w => w.Address)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(w => w.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        // === FK: BranchId → Branches ===
        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // === Global query filter — soft delete ===
        builder.HasQueryFilter(w => w.IsActive);
    }
}
