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
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Role).IsRequired().HasConversion<byte>();
        builder.HasQueryFilter(u => u.IsActive);
    }
}