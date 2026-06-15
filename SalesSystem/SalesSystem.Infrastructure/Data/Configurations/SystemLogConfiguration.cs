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
        builder.Property(e => e.Id).HasColumnType("bigint");

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Source)
            .HasMaxLength(100);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Exception)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true);
    }
}
