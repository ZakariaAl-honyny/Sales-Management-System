using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class FiscalYearClosureConfiguration : IEntityTypeConfiguration<FiscalYearClosure>
{
    public void Configure(EntityTypeBuilder<FiscalYearClosure> builder)
    {
        builder.ToTable("FiscalYearClosures");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FiscalYear)
            .IsRequired();

        builder.Property(x => x.ClosedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");

        builder.Property(x => x.ClosedByUserId)
            .IsRequired();

        builder.Property(x => x.NetIncome)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.ClosingEntryId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // ─── Unique Index ────────────────────────────────
        builder.HasIndex(x => x.FiscalYear)
            .IsUnique()
            .HasDatabaseName("IX_FiscalYearClosures_FiscalYear");

        // ─── Foreign Keys (ALL Restrict) ─────────────────
        builder.HasOne(x => x.ClosedByUser)
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClosingEntry)
            .WithMany()
            .HasForeignKey(x => x.ClosingEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Global Query Filter ─────────────────────────
        builder.HasQueryFilter(x => x.IsActive);
    }
}
