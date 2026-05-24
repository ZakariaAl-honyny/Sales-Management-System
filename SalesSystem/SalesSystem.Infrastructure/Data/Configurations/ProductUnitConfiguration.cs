using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BaseConversionFactor).HasPrecision(18, 3);
        builder.Property(x => x.SalesPrice).HasPrecision(18, 2);
        builder.Property(x => x.WholesalePrice).HasPrecision(18, 2);
        builder.Property(x => x.PurchaseCost).HasPrecision(18, 2);
        builder.Property(x => x.SupplierPrice).HasPrecision(18, 2);
        builder.Property(x => x.LastPurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.SortOrder).HasDefaultValue(1);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_ProductUnits_BaseUnitFactor",
            "IsBaseUnit = 0 OR BaseConversionFactor = 1"));

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}