using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(30);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(100);
        builder.Property(s => s.Address).HasMaxLength(250);
        builder.Property(s => s.OpeningBalance).HasPrecision(18, 2);
        builder.Property(s => s.CurrentBalance).HasPrecision(18, 2);
        builder.Property(s => s.CreatedBy).HasMaxLength(150);
        builder.Property(s => s.UpdatedBy).HasMaxLength(150);
        builder.HasQueryFilter(s => s.IsActive);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(30);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Address).HasMaxLength(250);
        builder.Property(c => c.OpeningBalance).HasPrecision(18, 2);
        builder.Property(c => c.CurrentBalance).HasPrecision(18, 2);
        builder.Property(c => c.CreatedBy).HasMaxLength(150);
        builder.Property(c => c.UpdatedBy).HasMaxLength(150);
        builder.HasQueryFilter(c => c.IsActive);
    }
}