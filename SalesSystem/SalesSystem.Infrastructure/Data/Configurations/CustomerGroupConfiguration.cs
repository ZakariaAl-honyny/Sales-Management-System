using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("CustomerGroups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Description).HasMaxLength(250);
        builder.HasQueryFilter(g => g.IsActive);
    }
}
