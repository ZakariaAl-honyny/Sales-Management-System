using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Position)
            .HasMaxLength(100);

        builder.Property(c => c.Notes)
            .HasMaxLength(300);

        // FK to Customer
        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.CustomerId)
            .HasDatabaseName("IX_CustomerContacts_CustomerId");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_CustomerContacts_Name");

        // Global query filter — soft delete
        builder.HasQueryFilter(c => c.IsActive);
    }
}
