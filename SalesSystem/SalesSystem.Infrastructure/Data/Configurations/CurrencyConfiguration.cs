using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(10);
        builder.Property(c => c.Symbol).IsRequired().HasMaxLength(10);
        builder.Property(c => c.ExchangeRateToBase).HasPrecision(18, 6).HasDefaultValue(1.0m);
        builder.Property(c => c.IsBaseCurrency).HasDefaultValue(false);
        builder.Property(c => c.FractionName).HasMaxLength(20).IsRequired(false);
        builder.Property(c => c.IsSystem).HasDefaultValue(false);
        builder.HasQueryFilter(c => c.IsActive);
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(c => c.IsBaseCurrency).IsUnique().HasFilter("[IsBaseCurrency] = 1 AND [IsActive] = 1");
        builder.ToTable(t => t.HasCheckConstraint("CHK_Currencies_ExchangeRate", "[ExchangeRateToBase] > 0"));
    }
}
