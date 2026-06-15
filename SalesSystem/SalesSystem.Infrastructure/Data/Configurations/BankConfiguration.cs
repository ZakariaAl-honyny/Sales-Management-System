using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Bank"/> entity.
/// </summary>
public class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ToTable("Banks");
        builder.HasKey(b => b.Id);

        // === Properties ===

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.AccountName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(b => b.AccountNumber)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(b => b.IBAN)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(b => b.BranchName)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(b => b.Phone)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(b => b.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(b => b.CurrencyId)
            .IsRequired();

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        // === FK: AccountId → Accounts ===
        // AccountId can be null at creation — the service layer auto-creates
        // a sub-account under "1120 — البنوك" and sets it via SetAccountId().

        builder.HasOne(b => b.Account)
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // === FK: CurrencyId → Currencies ===

        builder.HasOne(b => b.Currency)
            .WithMany()
            .HasForeignKey(b => b.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // === Indexes ===

        builder.HasIndex(b => b.AccountId)
            .HasDatabaseName("IX_Banks_AccountId");

        builder.HasIndex(b => b.CurrencyId)
            .HasDatabaseName("IX_Banks_CurrencyId");

        // === Global query filter — soft delete ===

        builder.HasQueryFilter(b => b.IsActive);
    }
}
