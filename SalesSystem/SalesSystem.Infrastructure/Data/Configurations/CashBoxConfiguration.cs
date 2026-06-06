using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
{
    public void Configure(EntityTypeBuilder<CashBox> builder)
    {
        builder.ToTable("CashBoxes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BoxName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        builder.Ignore(x => x.CurrencyCode); // DEPRECATED — computed from Currency navigation
        builder.Property(x => x.CurrencyId).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.BoxName);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.CashBox)
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}