using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Id is both PK and FK to Parties(Id) — shared primary key pattern
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever(); // Id is assigned from Party.Id, not auto-generated

        // Properties
        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(c => c.CustomerSince);

        builder.Property(c => c.PriceLevel);

        builder.Property(c => c.Notes)
            .HasMaxLength(500);

        // 1:1 relationship with Party via shared PK
        builder.HasOne(c => c.Party)
            .WithOne()
            .HasForeignKey<Customer>(c => c.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Global query filter — soft delete
        builder.HasQueryFilter(c => c.IsActive);
    }
}
