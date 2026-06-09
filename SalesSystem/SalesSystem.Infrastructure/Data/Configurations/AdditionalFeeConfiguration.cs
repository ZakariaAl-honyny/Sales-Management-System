using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class AdditionalFeeConfiguration : IEntityTypeConfiguration<AdditionalFee>
{
    public void Configure(EntityTypeBuilder<AdditionalFee> builder)
    {
        builder.ToTable("AdditionalFees");
        builder.HasKey(af => af.Id);
        builder.Property(af => af.FeeName).IsRequired().HasMaxLength(100);
        builder.Property(af => af.FeeAmount).HasPrecision(18, 2);
        builder.Property(af => af.DistributionMethod).HasConversion<byte>();

        builder.HasOne(af => af.PurchaseInvoice)
            .WithMany(pi => pi.AdditionalFees)
            .HasForeignKey(af => af.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(af => af.Account)
            .WithMany()
            .HasForeignKey(af => af.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(af => af.IsActive);
    }
}

public class AdditionalFeeAllocationConfiguration : IEntityTypeConfiguration<AdditionalFeeAllocation>
{
    public void Configure(EntityTypeBuilder<AdditionalFeeAllocation> builder)
    {
        builder.ToTable("AdditionalFeeAllocations");
        builder.HasKey(afa => afa.Id);
        builder.Property(afa => afa.AllocatedAmount).HasPrecision(18, 2);

        builder.HasOne(afa => afa.AdditionalFee)
            .WithMany()
            .HasForeignKey(afa => afa.AdditionalFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(afa => afa.PurchaseInvoiceItem)
            .WithMany()
            .HasForeignKey(afa => afa.PurchaseInvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(afa => afa.IsActive);
    }
}
