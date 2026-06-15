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

        // FK to Account (optional — auto-created by service layer if null at box creation)
        builder.Property(x => x.AccountId).IsRequired(false);
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.BranchId).IsRequired(false);
        builder.Property(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CategoryId).IsRequired(false);

        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.TaxNumber).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.BoxName);

        // Currency is already mapped via HasForeignKey above

        builder.HasQueryFilter(x => x.IsActive);
    }
}