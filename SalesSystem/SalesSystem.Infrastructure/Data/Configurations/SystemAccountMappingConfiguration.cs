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

        // Mapping key stored as nvarchar(100) — unique across all branches
        builder.Property(x => x.MappingKey)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.MappingKey)
            .IsUnique()
            .HasFilter("[MappingKey] IS NOT NULL");

        // FK to Account (required)
        builder.Property(x => x.AccountId).IsRequired();
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // BranchId (smallint, nullable) — branch-specific override
        builder.Property(x => x.BranchId)
            .IsRequired(false)
            .HasColumnType("smallint");
    }
}
