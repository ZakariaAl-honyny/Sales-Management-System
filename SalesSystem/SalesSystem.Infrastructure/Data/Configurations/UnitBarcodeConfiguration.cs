using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UnitBarcodeConfiguration : IEntityTypeConfiguration<UnitBarcode>
{
    public void Configure(EntityTypeBuilder<UnitBarcode> builder)
    {
        builder.ToTable("UnitBarcodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BarcodeValue).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SupplierCode).HasMaxLength(100);
        builder.Property(x => x.IsDefault).HasDefaultValue(false);
        builder.HasIndex(x => x.BarcodeValue).IsUnique();

        builder.HasOne(x => x.ProductUnit)
            .WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}