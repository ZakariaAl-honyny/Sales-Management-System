using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class DailyClosureConfiguration : IEntityTypeConfiguration<DailyClosure>
{
    public void Configure(EntityTypeBuilder<DailyClosure> builder)
    {
        builder.ToTable("DailyClosures");
        builder.HasKey(x => x.Id);

        // ─── Properties ─────────────────────────────────────
        builder.Property(dc => dc.ClosureDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(dc => dc.OpeningBalance)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(dc => dc.TotalIncome)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(dc => dc.TotalExpense)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(dc => dc.ClosingBalance)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(dc => dc.ActualCashCount)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(dc => dc.Difference)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(dc => dc.IsReconciled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(dc => dc.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // ─── Foreign Keys ───────────────────────────────────
        builder.HasOne(dc => dc.CashBox)
            .WithMany()
            .HasForeignKey(dc => dc.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Indexes ────────────────────────────────────────
        builder.HasIndex(dc => new { dc.CashBoxId, dc.ClosureDate })
            .IsUnique();  // One closure per cash box per day

        builder.HasIndex(dc => dc.ClosureDate);
    }
}
