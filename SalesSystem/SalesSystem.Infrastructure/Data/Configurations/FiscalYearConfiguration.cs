using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year)
            .IsRequired();

        builder.HasIndex(x => x.Year)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.IsOpen)
            .HasDefaultValue(true);

        builder.Property(x => x.OpenedAt)
            .IsRequired();

        builder.Property(x => x.OpenedByUserId);

        builder.Property(x => x.ClosedAt);

        builder.Property(x => x.ClosedByUserId);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
