using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="CurrencyRate"/> entity.
/// Maps to "CurrencyRates" table.
/// </summary>
public class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.ToTable("CurrencyRates");

        builder.HasKey(cr => cr.Id);

        // === Properties ===

        builder.Property(cr => cr.RateToBase)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(cr => cr.EffectiveFrom)
            .IsRequired();

        builder.Property(cr => cr.EffectiveTo)
            .IsRequired(false);

        // === FK: CurrencyId → Currencies ===

        builder.HasOne(cr => cr.Currency)
            .WithMany()
            .HasForeignKey(cr => cr.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // === Indexes ===

        builder.HasIndex(cr => new { cr.CurrencyId, cr.EffectiveFrom })
            .HasDatabaseName("IX_CurrencyRates_CurrencyId_EffectiveFrom")
            .IsDescending(false, true);

        builder.HasIndex(cr => cr.EffectiveFrom)
            .HasDatabaseName("IX_CurrencyRates_EffectiveFrom");

        // === Check constraints ===

        builder.ToTable(t => t.HasCheckConstraint("CHK_CurrencyRates_RateToBase", "[RateToBase] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CHK_CurrencyRates_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
    }
}
