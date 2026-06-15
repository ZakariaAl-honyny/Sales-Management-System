using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> builder)
    {
        builder.ToTable("SupplierContacts");
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Phone)
            .HasMaxLength(30);

        builder.Property(s => s.Email)
            .HasMaxLength(100);

        builder.Property(s => s.Position)
            .HasMaxLength(100);

        builder.Property(s => s.Notes)
            .HasMaxLength(300);

        // FK to Supplier
        builder.HasOne(s => s.Supplier)
            .WithMany()
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.SupplierId)
            .HasDatabaseName("IX_SupplierContacts_SupplierId");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_SupplierContacts_Name");

        // Global query filter — soft delete
        builder.HasQueryFilter(s => s.IsActive);
    }
}
