using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountCode)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(x => x.AccountCode)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.NameEn)
            .HasMaxLength(200);

        // Nature: tinyint — 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
        builder.Property(x => x.Nature)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(x => x.IsLeaf)
            .HasDefaultValue(true);

        builder.Property(x => x.IsSystem)
            .HasDefaultValue(false);

        builder.Property(x => x.CategoryId)
            .HasColumnType("smallint");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // Self-referencing parent relationship
        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.SubAccounts)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category FK
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
