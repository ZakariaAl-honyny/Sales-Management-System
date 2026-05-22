using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.ToTable("ProductPriceHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChangeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CostingMethod)
            .HasMaxLength(50);

        builder.Property(x => x.OldValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.NewValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.Property(x => x.ProductUnitId)
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .IsRequired();
    }
}
