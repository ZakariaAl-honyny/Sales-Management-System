using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        // Id is both PK and FK to Parties(Id) — shared primary key pattern
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever(); // Id is assigned from Party.Id, not auto-generated

        // Properties
        builder.Property(s => s.PaymentTerms)
            .HasMaxLength(200);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        // 1:1 relationship with Party via shared PK
        builder.HasOne(s => s.Party)
            .WithOne()
            .HasForeignKey<Supplier>(s => s.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Global query filter — soft delete
        builder.HasQueryFilter(s => s.IsActive);
    }
}
