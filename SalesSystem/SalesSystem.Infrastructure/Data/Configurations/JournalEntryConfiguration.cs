using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.EntryNumber).IsUnique();
        builder.Property(x => x.TransactionDate).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.EntryType).HasConversion<int>().IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50);
        builder.Property(x => x.IsPosted).HasDefaultValue(false);
        builder.Property(x => x.IsReversed).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.TransactionDate);

        // Lines collection — Restrict (soft-delete only, no hard deletes)
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing FK for reversal entries
        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.ReversedByEntryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
