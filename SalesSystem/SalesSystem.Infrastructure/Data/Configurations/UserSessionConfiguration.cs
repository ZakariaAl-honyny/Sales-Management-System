using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.Id);

        // UserId — non-nullable FK to Users
        builder.Property(s => s.UserId).IsRequired();

        // Token — required, max 500 (matches schema nvarchar(500))
        builder.Property(s => s.Token).IsRequired().HasMaxLength(500);
        builder.HasIndex(s => s.Token).IsUnique();

        // Device tracking
        builder.Property(s => s.DeviceName).IsRequired(false).HasMaxLength(200);
        builder.Property(s => s.IpAddress).IsRequired(false).HasMaxLength(50);
        builder.Property(s => s.UserAgent).IsRequired(false).HasMaxLength(500);

        // Timestamps
        builder.Property(s => s.LoginAt).IsRequired();
        builder.Property(s => s.LastActivityAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();

        // Revocation flag
        builder.Property(s => s.IsRevoked).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.UserId, s.IsRevoked });

        // Navigation: User
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
