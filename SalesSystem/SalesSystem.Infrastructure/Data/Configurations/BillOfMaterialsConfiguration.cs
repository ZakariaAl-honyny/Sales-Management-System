using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class BillOfMaterialsConfiguration : IEntityTypeConfiguration<BillOfMaterials>
{
    public void Configure(EntityTypeBuilder<BillOfMaterials> builder)
    {
        builder.ToTable("BillOfMaterials");

        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.QuantityRequired)
            .HasPrecision(18, 3)
            .IsRequired()
            .HasComment("الكمية المطلوبة من المكوّن لإنتاج وحدة واحدة من المنتج المُجمَّع");

        builder.Property(x => x.WastePercentage)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("نسبة الهالك (مثال: 5 تعني 5% إضافية مطلوبة)");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(x => x.AssemblyProduct)
            .WithMany() // Product has no BOM navigation
            .HasForeignKey(x => x.AssemblyProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ComponentProduct)
            .WithMany()
            .HasForeignKey(x => x.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ComponentUnit)
            .WithMany()
            .HasForeignKey(x => x.ComponentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on (AssemblyProductId, ComponentProductId) with soft-delete filter
        builder.HasIndex(x => new { x.AssemblyProductId, x.ComponentProductId })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_BillOfMaterials_AssemblyProduct_ComponentProduct");

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
