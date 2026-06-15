using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        // Supplier.Id is auto-increment PK (separate from Party.Id)
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        // FK to Party
        builder.HasOne(s => s.Party)
            .WithMany()
            .HasForeignKey(s => s.PartyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // FK to Account
        builder.HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // CategoryId is a lookup — no FK constraint (type mismatch with smallint PK)
        builder.Property(s => s.CategoryId).IsRequired(false);

        // Indexes
        builder.HasIndex(s => s.PartyId)
            .HasDatabaseName("IX_Suppliers_PartyId");

        builder.HasIndex(s => s.AccountId)
            .HasDatabaseName("IX_Suppliers_AccountId");

        builder.HasIndex(s => s.CategoryId)
            .HasDatabaseName("IX_Suppliers_CategoryId");

        // Global query filter — soft delete
        builder.HasQueryFilter(s => s.IsActive);
    }
}
