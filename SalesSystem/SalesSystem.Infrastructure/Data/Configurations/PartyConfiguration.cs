using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("Parties");
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Phone)
            .HasMaxLength(30);

        builder.Property(p => p.Email)
            .HasMaxLength(100);

        builder.Property(p => p.Address)
            .HasMaxLength(300);

        builder.Property(p => p.TaxNumber)
            .HasMaxLength(50);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Parties_Name");

        builder.HasIndex(p => p.Phone)
            .HasDatabaseName("IX_Parties_Phone");

        // Global query filter — soft delete
        builder.HasQueryFilter(p => p.IsActive);
    }
}
