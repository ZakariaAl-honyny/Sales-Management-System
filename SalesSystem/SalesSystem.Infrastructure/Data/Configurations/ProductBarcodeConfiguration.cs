using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("ProductBarcodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BarcodeValue)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.UnitType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.BarcodeValue)
            .IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => x.IsActive);
    }
}