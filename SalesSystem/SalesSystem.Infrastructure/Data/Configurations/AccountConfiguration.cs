using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", t =>
        {
            t.HasCheckConstraint("CHK_Account_Level_Range", "[Level] >= 1 AND [Level] <= 10");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.AccountCode).IsUnique().HasFilter("[IsActive] = 1");
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NameEn).HasMaxLength(200);
        builder.Property(x => x.AccountType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Level).IsRequired().HasDefaultValue(4);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ColorCode).HasMaxLength(7);
        builder.Property(x => x.AllowTransactions).HasDefaultValue(false);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Explanation).HasMaxLength(500).HasColumnType("nvarchar(500)");
        builder.Property(x => x.IsSystemAccount).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Self-referencing parent relationship with navigation properties
        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.SubAccounts)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
