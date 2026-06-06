using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ExchangeRateHistoryConfiguration : IEntityTypeConfiguration<ExchangeRateHistory>
{
    public void Configure(EntityTypeBuilder<ExchangeRateHistory> builder)
    {
        builder.ToTable("ExchangeRateHistories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OldRate).HasPrecision(18, 6);
        builder.Property(e => e.NewRate).HasPrecision(18, 6);
        builder.Property(e => e.EffectiveDate).HasColumnType("date");
        builder.Property(e => e.RateType).HasMaxLength(20).IsRequired(false);
        builder.Property(e => e.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(e => e.ChangedByUserId).IsRequired(false);
        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(e => e.IsActive);
        builder.HasIndex(e => new { e.CurrencyId, e.EffectiveDate });
    }
}
