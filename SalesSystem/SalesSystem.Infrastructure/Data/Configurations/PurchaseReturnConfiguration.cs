using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns");
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.ReturnNo).IsRequired();
        builder.HasIndex(pr => pr.ReturnNo).IsUnique();
        builder.Property(pr => pr.ReturnDate).HasColumnType("date");
        builder.Property(pr => pr.TotalAmount).HasPrecision(18, 2);
        builder.Property(pr => pr.ReturnedDiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(pr => pr.ReturnedTaxAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(pr => pr.ReturnedChargeAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(pr => pr.TaxId).HasColumnType("smallint").IsRequired(false);
        builder.Property(pr => pr.Notes).HasMaxLength(500);
        builder.Property(pr => pr.CurrencyId).HasColumnType("smallint");
        builder.Property(pr => pr.Status).HasConversion<byte>();

        builder.HasOne(pr => pr.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(pr => pr.PurchaseInvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Supplier)
            .WithMany()
            .HasForeignKey(pr => pr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Warehouse)
            .WithMany()
            .HasForeignKey(pr => pr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Currency)
            .WithMany()
            .HasForeignKey(pr => pr.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pr => pr.Lines)
            .WithOne(l => l.PurchaseReturn)
            .HasForeignKey(l => l.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(pr => pr.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled);
    }
}

public class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLine> builder)
    {
        builder.ToTable("PurchaseReturnLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.Amount).HasPrecision(18, 2);

        builder.HasOne(l => l.PurchaseReturn)
            .WithMany(pr => pr.Lines)
            .HasForeignKey(l => l.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.PurchaseInvoiceLine)
            .WithMany()
            .HasForeignKey(l => l.PurchaseInvoiceLineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ProductUnit)
            .WithMany()
            .HasForeignKey(l => l.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
