using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasColumnType("smallint")
            .ValueGeneratedOnAdd();
        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.Address).HasMaxLength(300);
        builder.Property(b => b.ManagerName).HasMaxLength(150);
        builder.Property(b => b.Notes).HasMaxLength(500);
        builder.Property(b => b.IsActive).HasDefaultValue(true);
        builder.HasIndex(b => b.Name).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasQueryFilter(b => b.IsActive);
    }
}
