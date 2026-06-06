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
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Rate).HasPrecision(18, 2);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasQueryFilter(t => t.IsActive);
        builder.ToTable(t => t.HasCheckConstraint("CHK_Taxes_Rate_Range", "[Rate] >= 0 AND [Rate] <= 100"));
        builder.HasIndex(t => t.IsDefault).IsUnique().HasFilter("[IsDefault] = 1 AND [IsActive] = 1");
    }
}
