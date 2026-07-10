using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpenseNo).IsRequired();
        builder.Property(e => e.ExpenseDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.Status).HasColumnType("tinyint").HasConversion<byte>().IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(e => e.PostedAt).IsRequired(false);

        // FK to ExpenseAccount (Account)
        builder.HasOne(e => e.ExpenseAccount)
            .WithMany()
            .HasForeignKey(e => e.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // FK to CashBox
        builder.HasOne(e => e.CashBox)
            .WithMany()
            .HasForeignKey(e => e.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Unique index on ExpenseNo
        builder.HasIndex(e => e.ExpenseNo)
            .IsUnique()
            .HasDatabaseName("IX_Expenses_ExpenseNo");

        // Index on ExpenseAccountId for filtering
        builder.HasIndex(e => e.ExpenseAccountId)
            .HasDatabaseName("IX_Expenses_ExpenseAccountId");

        // Index on ExpenseDate for date-range queries
        builder.HasIndex(e => e.ExpenseDate)
            .HasDatabaseName("IX_Expenses_ExpenseDate");

        builder.HasQueryFilter(e => e.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);
    }
}
