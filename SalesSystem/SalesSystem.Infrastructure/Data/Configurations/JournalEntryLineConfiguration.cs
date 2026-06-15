using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines", t =>
        {
            t.HasCheckConstraint("CHK_DebitOrCredit",
                "(Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0) OR (Debit = 0 AND Credit = 0)");
            t.HasCheckConstraint("CHK_NoNegativeValues", "Debit >= 0 AND Credit >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Debit).HasPrecision(18, 2);
        builder.Property(x => x.Credit).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.SortOrder).HasColumnType("smallint").HasDefaultValue((short)0);

        // FK to Account with navigation property and Restrict
        builder.HasOne(x => x.Account)
            .WithMany(x => x.JournalLines)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.JournalEntryId);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => new { x.JournalEntryId, x.AccountId });

    }
}
