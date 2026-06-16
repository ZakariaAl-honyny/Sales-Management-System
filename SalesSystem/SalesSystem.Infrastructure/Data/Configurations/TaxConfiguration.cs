using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class TaxConfiguration : IEntityTypeConfiguration<Tax>
{
    public void Configure(EntityTypeBuilder<Tax> builder)
    {
        builder.ToTable("Taxes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnType("smallint")
            .ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Rate).HasPrecision(5, 2);
        builder.Property(t => t.TaxType).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasQueryFilter(t => t.IsActive);
        builder.ToTable(t => t.HasCheckConstraint("CHK_Taxes_Rate_Range", "[Rate] >= 0 AND [Rate] <= 100"));
        builder.ToTable(t => t.HasCheckConstraint("CHK_Taxes_TaxType_Range", "[TaxType] >= 1 AND [TaxType] <= 3"));
        builder.HasIndex(t => t.IsDefault).IsUnique().HasFilter("[IsDefault] = 1 AND [IsActive] = 1");
    }
}
