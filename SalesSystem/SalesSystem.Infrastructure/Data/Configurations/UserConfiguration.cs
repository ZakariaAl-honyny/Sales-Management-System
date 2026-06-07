using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired(false).HasMaxLength(256);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Role).IsRequired().HasConversion<byte>();

        // Status replaces IsActive for the query filter
        builder.Property(u => u.Status).IsRequired().HasConversion<byte>().HasDefaultValue(UserStatus.Active);
        builder.HasQueryFilter(u => u.Status == UserStatus.Active);

        builder.Property(u => u.MustChangePassword).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.Email).HasMaxLength(100);
        builder.Property(u => u.AvatarPath).HasMaxLength(500);
        builder.Property(u => u.LoginAttempts).IsRequired().HasDefaultValue(0);

        // Password reset token (nullable, plaintext — high-entropy, short-lived, one-time use)
        builder.Property(u => u.PasswordResetToken).IsRequired(false).HasMaxLength(256);
        builder.Property(u => u.PasswordResetTokenExpiresAt).IsRequired(false);

        // FK to CashBoxes
        builder.HasOne(u => u.DefaultCashBox)
            .WithMany()
            .HasForeignKey(u => u.DefaultCashBoxId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
