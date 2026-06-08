using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SystemAccountMappingsConfiguration : IEntityTypeConfiguration<SystemAccountMappings>
{
    public void Configure(EntityTypeBuilder<SystemAccountMappings> builder)
    {
        builder.ToTable("SystemAccountMappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).IsRequired(false);

        // FK to Account for each mapping
        builder.HasOne(x => x.DefaultCashAccount)
            .WithMany()
            .HasForeignKey(x => x.DefaultCashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultBankAccount)
            .WithMany()
            .HasForeignKey(x => x.DefaultBankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryAssetAccount)
            .WithMany()
            .HasForeignKey(x => x.InventoryAssetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AccountsReceivableAccount)
            .WithMany()
            .HasForeignKey(x => x.AccountsReceivableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AccountsPayableAccount)
            .WithMany()
            .HasForeignKey(x => x.AccountsPayableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VatOutputAccount)
            .WithMany()
            .HasForeignKey(x => x.VatOutputAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VatInputAccount)
            .WithMany()
            .HasForeignKey(x => x.VatInputAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CapitalAccount)
            .WithMany()
            .HasForeignKey(x => x.CapitalAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesRevenueAccount)
            .WithMany()
            .HasForeignKey(x => x.SalesRevenueAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesReturnAccount)
            .WithMany()
            .HasForeignKey(x => x.SalesReturnAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CogsAccount)
            .WithMany()
            .HasForeignKey(x => x.CogsAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GeneralExpenseAccount)
            .WithMany()
            .HasForeignKey(x => x.GeneralExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SpoilageLossAccount)
            .WithMany()
            .HasForeignKey(x => x.SpoilageLossAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Opening Balance Equity Account ────────────────
        builder.Property(x => x.OpeningBalanceEquityAccountId)
            .HasColumnName("OpeningBalanceEquityAccountId")
            .IsRequired(false);
        builder.HasOne(x => x.OpeningBalanceEquityAccount)
            .WithMany()
            .HasForeignKey(x => x.OpeningBalanceEquityAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(x => x.IsActive);
    }
}
