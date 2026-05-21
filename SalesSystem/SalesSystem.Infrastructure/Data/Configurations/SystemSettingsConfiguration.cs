using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SettingKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SettingValue).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DataType).HasMaxLength(50).HasDefaultValue("string");
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.SettingKey).IsUnique();
    }
}