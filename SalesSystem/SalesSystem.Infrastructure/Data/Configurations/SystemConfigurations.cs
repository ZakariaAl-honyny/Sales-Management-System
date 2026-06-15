using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("DocumentSequences");
        builder.HasKey(ds => ds.Id);
        builder.Property(ds => ds.DocumentType).IsRequired().HasMaxLength(50);
        builder.Property(ds => ds.Prefix).IsRequired().HasMaxLength(10);
        builder.HasIndex(ds => new { ds.Prefix, ds.Year }).IsUnique();
        builder.Property(ds => ds.Year).IsRequired();
        builder.Property(ds => ds.LastNumber).IsRequired();
    }
}
