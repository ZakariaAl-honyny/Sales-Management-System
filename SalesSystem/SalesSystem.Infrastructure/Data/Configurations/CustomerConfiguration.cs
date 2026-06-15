using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Customer.Id is auto-increment PK (separate from Party.Id)
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 2);

        // FK to Party
        builder.HasOne(c => c.Party)
            .WithMany()
            .HasForeignKey(c => c.PartyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // FK to Account
        builder.HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // CategoryId is a lookup — no FK constraint (type mismatch with smallint PK)
        builder.Property(c => c.CategoryId).IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.PartyId)
            .HasDatabaseName("IX_Customers_PartyId");

        builder.HasIndex(c => c.AccountId)
            .HasDatabaseName("IX_Customers_AccountId");

        builder.HasIndex(c => c.CategoryId)
            .HasDatabaseName("IX_Customers_CategoryId");

        // Global query filter — soft delete
        builder.HasQueryFilter(c => c.IsActive);
    }
}
