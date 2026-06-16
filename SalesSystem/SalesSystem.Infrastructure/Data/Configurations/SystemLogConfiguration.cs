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
            .HasMaxLength(200);

        builder.Property(e => e.ActionName)
            .HasMaxLength(200);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // Index per schema 8.2: (Level, CreatedAt DESC) for error monitoring
        builder.HasIndex(e => new { e.Level, e.CreatedAt }).IsDescending(false, true);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
