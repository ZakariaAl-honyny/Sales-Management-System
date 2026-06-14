using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LogLevel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Level)
            .IsRequired(false);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Source)
            .HasMaxLength(100);

        builder.Property(e => e.Context)
            .HasMaxLength(200);

        builder.Property(e => e.MachineName)
            .HasMaxLength(100);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
