using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        // Supplier.Id is auto-increment PK
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        // Direct contact fields (replaces Party relationship)
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Phone)
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .HasMaxLength(100);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.TaxNumber)
            .HasMaxLength(30);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        // FK to Account
        builder.HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // CategoryId is a lookup — no FK constraint (type mismatch with smallint PK)
        builder.Property(s => s.CategoryId).IsRequired(false);

        // Indexes
        builder.HasIndex(s => s.AccountId)
            .HasDatabaseName("IX_Suppliers_AccountId");

        builder.HasIndex(s => s.CategoryId)
            .HasDatabaseName("IX_Suppliers_CategoryId");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Suppliers_Name");

        builder.HasIndex(s => s.Phone)
            .HasDatabaseName("IX_Suppliers_Phone");

        // Global query filter — soft delete
        builder.HasQueryFilter(s => s.IsActive);
    }
}
