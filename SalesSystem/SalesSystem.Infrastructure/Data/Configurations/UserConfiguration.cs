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

        // Core fields
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired(false).HasMaxLength(256);
        builder.Property(u => u.EmployeeId).IsRequired(false);

        // Status — replaces IsActive-based query filter
        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(UserStatus.Active)
            .HasSentinel((UserStatus)0);
        builder.HasQueryFilter(u => u.Status == UserStatus.Active);

        // Password policy
        builder.Property(u => u.MustChangePassword).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.PasswordChangedAt).IsRequired(false);
        builder.Property(u => u.LoginAttempts).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LastLoginAt).IsRequired(false);

        // Navigation: UserRoles (many-to-many via join table)
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: UserBranches (many-to-many via join table)
        builder.HasMany(u => u.UserBranches)
            .WithOne(ub => ub.User)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
