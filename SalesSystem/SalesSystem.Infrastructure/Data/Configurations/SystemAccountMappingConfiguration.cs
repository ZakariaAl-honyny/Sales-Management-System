using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SystemAccountMappingConfiguration : IEntityTypeConfiguration<SystemAccountMapping>
{
    public void Configure(EntityTypeBuilder<SystemAccountMapping> builder)
    {
        builder.ToTable("SystemAccountMappings");
        builder.HasKey(x => x.Id);

        // FK to Account (required)
        builder.Property(x => x.AccountId).IsRequired();
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mapping key stored as tinyint
        builder.Property(x => x.MappingKey)
            .HasConversion<byte>()
            .IsRequired();

        // Composite unique index: one mapping per (Key, BranchId)
        builder.HasIndex(x => new { x.MappingKey, x.BranchId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Property(x => x.BranchId).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DescriptionAr).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.DescriptionEn).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
