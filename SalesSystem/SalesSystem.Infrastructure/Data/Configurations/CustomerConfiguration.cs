using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Customer.Id is auto-increment PK
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Direct contact fields (replaces Party relationship)
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(30);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        // Properties
        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 2);

        // FK to Account
        builder.HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // CategoryId is a lookup — no FK constraint (type mismatch with smallint PK)
        builder.Property(c => c.CategoryId).IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.AccountId)
            .HasDatabaseName("IX_Customers_AccountId");

        builder.HasIndex(c => c.CategoryId)
            .HasDatabaseName("IX_Customers_CategoryId");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Customers_Name");

        builder.HasIndex(c => c.Phone)
            .HasDatabaseName("IX_Customers_Phone");

        // Global query filter — soft delete
        builder.HasQueryFilter(c => c.IsActive);
    }
}
