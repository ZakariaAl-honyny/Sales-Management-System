using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.EntryNumber).IsUnique();
        builder.Property(x => x.EntryNo).IsRequired();
        builder.HasIndex(x => x.EntryNo).IsUnique();
        builder.Property(x => x.EntryDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.EntryType).HasColumnType("tinyint").HasConversion<byte>().IsRequired();

        // ─── 3-State Lifecycle (replaces IsPosted/IsReversed) ───────────
        builder.Property(x => x.Status)
            .HasColumnType("tinyint")
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50);
        builder.Property(x => x.ReferenceId);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId })
            .HasFilter("[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");

        builder.HasIndex(x => x.EntryDate);

        // Lines collection — Restrict (soft-delete only, no hard deletes)
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing FK for reversal entries — use navigation lambda (not generic HasOne)
        builder.HasOne(x => x.ReversedByEntry)
            .WithMany()
            .HasForeignKey(x => x.ReversedByEntryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.Status != JournalEntryStatus.Cancelled);
    }
}
