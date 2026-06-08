using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("CashTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RunningBalance).HasPrecision(18, 2);
        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.CashBoxId);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

        builder.HasOne(ct => ct.CashBox)
            .WithMany(cb => cb.Transactions)
            .HasForeignKey(ct => ct.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ct => ct.Currency)
            .WithMany()
            .HasForeignKey(ct => ct.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(x => x.IsActive);
    }
}