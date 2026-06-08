using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
{
    public void Configure(EntityTypeBuilder<CashBox> builder)
    {
        builder.ToTable("CashBoxes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BoxName).IsRequired().HasMaxLength(100);

        // FK to Account (required — balance lives on the Chart of Accounts)
        builder.Property(x => x.AccountId).IsRequired();
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Category (optional — for classifying cash box type)
        builder.Property(x => x.CategoryId).IsRequired(false);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.BranchId).IsRequired(false);
        builder.Property(x => x.CurrencyId).IsRequired(false);
        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.TaxNumber).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.BoxName);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.CashBox)
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.Currency); // Keep EF from mapping the navigation twice — mapped above

        builder.HasQueryFilter(x => x.IsActive);
    }
}