using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="FiscalYear"/> entity.
/// </summary>
public class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears");
        builder.HasKey(x => x.Id);

        // === Properties ===

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.YearName)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.IsOpen)
            .HasDefaultValue(true);

        builder.Property(x => x.OpenedAt)
            .IsRequired(false);

        builder.Property(x => x.OpenedByUserId)
            .IsRequired(false);

        builder.Property(x => x.ClosedAt)
            .IsRequired(false);

        builder.Property(x => x.ClosedByUserId)
            .IsRequired(false);

        // === Indexes ===

        builder.HasIndex(x => x.Year)
            .IsUnique();

        builder.HasIndex(x => x.YearName)
            .IsUnique()
            .HasDatabaseName("IX_FiscalYears_YearName");

        // === Global query filter — only open years ===

        builder.HasQueryFilter(x => x.IsOpen);
    }
}
