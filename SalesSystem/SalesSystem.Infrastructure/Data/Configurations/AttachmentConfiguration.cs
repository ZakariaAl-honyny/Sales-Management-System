using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityType).IsRequired();
        builder.Property(a => a.ReferenceType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.ReferenceId).IsRequired();
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.OriginalFileName).HasMaxLength(255);
        builder.Property(a => a.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(a => a.FileSize).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(500);

        // Composite index for typed lookups (EntityType + ReferenceId)
        builder.HasIndex(a => new { a.EntityType, a.ReferenceId })
            .HasDatabaseName("IX_Attachments_EntityType_ReferenceId");

        // Composite index for string reference lookups
        builder.HasIndex(a => new { a.ReferenceType, a.ReferenceId })
            .HasDatabaseName("IX_Attachments_ReferenceType_ReferenceId");
    }
}
