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

        builder.Property(x => x.ClosureDate)
            .IsRequired();

        builder.Property(x => x.OpeningBalance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalIncome)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalExpense)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.ExpectedClosingBalance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.ActualCashCount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.Difference)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.IsReconciled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ClosedByUserId)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.CashBox)
            .WithMany()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CashBoxId, x.ClosureDate })
            .IsUnique()
            .HasDatabaseName("IX_DailyClosures_CashBoxId_ClosureDate");

        builder.HasQueryFilter(x => x.IsActive);
    }
}
