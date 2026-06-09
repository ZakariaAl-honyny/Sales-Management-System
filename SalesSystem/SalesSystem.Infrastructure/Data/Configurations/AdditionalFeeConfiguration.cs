using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

/// <summary>
/// تكوين كيان الرسم الإضافي (AdditionalFee) وجدوله في قاعدة البيانات
/// </summary>
public class AdditionalFeeConfiguration : IEntityTypeConfiguration<AdditionalFee>
{
    public void Configure(EntityTypeBuilder<AdditionalFee> builder)
    {
        builder.ToTable("AdditionalFees");
        builder.HasKey(af => af.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(af => af.FeeName).HasMaxLength(100).IsRequired();
        builder.Property(af => af.FeeAmount).HasPrecision(18, 2);
        builder.Property(af => af.DistributionMethod).HasConversion<byte>();

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(af => af.PurchaseInvoice)
            .WithMany(pi => pi.AdditionalFees)
            .HasForeignKey(af => af.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(af => af.Account)
            .WithMany()
            .HasForeignKey(af => af.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Allocations collection ──────────────────────────────────
        builder.HasMany(af => af.Allocations)
            .WithOne(a => a.AdditionalFee)
            .HasForeignKey(a => a.AdditionalFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(af => af.IsActive);
    }
}

/// <summary>
/// تكوين كيان توزيع الرسم الإضافي (AdditionalFeeAllocation) وجدوله في قاعدة البيانات
/// </summary>
public class AdditionalFeeAllocationConfiguration : IEntityTypeConfiguration<AdditionalFeeAllocation>
{
    public void Configure(EntityTypeBuilder<AdditionalFeeAllocation> builder)
    {
        builder.ToTable("AdditionalFeeAllocations");
        builder.HasKey(afa => afa.Id);

        // ─── Properties ──────────────────────────────────────────────
        builder.Property(afa => afa.AllocatedAmount).HasPrecision(18, 2);

        // ─── Foreign Keys ────────────────────────────────────────────
        builder.HasOne(afa => afa.AdditionalFee)
            .WithMany(af => af.Allocations)
            .HasForeignKey(afa => afa.AdditionalFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(afa => afa.PurchaseInvoiceItem)
            .WithMany()
            .HasForeignKey(afa => afa.PurchaseInvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Soft delete filter ──────────────────────────────────────
        builder.HasQueryFilter(afa => afa.IsActive);
    }
}
