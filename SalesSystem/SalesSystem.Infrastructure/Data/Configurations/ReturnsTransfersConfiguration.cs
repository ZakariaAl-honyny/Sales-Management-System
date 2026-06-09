using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");
        builder.HasKey(sr => sr.Id);
        builder.Property(sr => sr.ReturnNo).IsRequired().HasMaxLength(30).HasColumnName("ReturnNo");
        builder.HasIndex(sr => sr.ReturnNo).IsUnique();
        builder.Property(sr => sr.SubTotal).HasPrecision(18, 2);
        builder.Property(sr => sr.TotalAmount).HasPrecision(18, 2);
        builder.Property(sr => sr.Notes).HasColumnName("Reason").HasMaxLength(250);
        builder.Property(sr => sr.Status).HasConversion<byte>();

        builder.HasOne(sr => sr.Customer)
            .WithMany()
            .HasForeignKey(sr => sr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.Warehouse)
            .WithMany()
            .HasForeignKey(sr => sr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.Currency)
            .WithMany()
            .HasForeignKey(sr => sr.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sr => sr.ExchangeRate).HasPrecision(18, 6).IsRequired(false);

        builder.HasMany(sr => sr.Items)
            .WithOne(i => i.SalesReturn)
            .HasForeignKey(i => i.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sr => sr.IsActive);
    }
}

public class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.ToTable("SalesReturnItems");
        builder.HasKey(sri => sri.Id);
        builder.Property(sri => sri.Quantity).HasPrecision(18, 3);
        builder.Property(sri => sri.UnitPrice).HasPrecision(18, 2);
        builder.Property(sri => sri.DiscountAmount).HasPrecision(18, 2);
        builder.Property(sri => sri.LineTotal).HasPrecision(18, 2);
        builder.Ignore(sri => sri.Notes); // DB Schema doesn't have Notes for return items

        builder.HasOne(sri => sri.Product)
            .WithMany()
            .HasForeignKey(sri => sri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sri => sri.IsActive);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers");
        builder.HasKey(st => st.Id);
        builder.Property(st => st.TransferNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(st => st.TransferNo).IsUnique();
        builder.Property(st => st.Notes).HasMaxLength(500);
        builder.Property(st => st.Status).HasConversion<byte>();

        builder.HasOne(st => st.FromWarehouse)
            .WithMany()
            .HasForeignKey(st => st.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.ToWarehouse)
            .WithMany()
            .HasForeignKey(st => st.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(st => st.Items)
            .WithOne(i => i.StockTransfer)
            .HasForeignKey(i => i.StockTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(st => st.IsActive);
    }
}

public class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> builder)
    {
        builder.ToTable("StockTransferItems");
        builder.HasKey(sti => sti.Id);
        builder.Property(sti => sti.Quantity).HasPrecision(18, 3);
        builder.Property(sti => sti.Notes).HasMaxLength(250);

        builder.HasOne(sti => sti.Product)
            .WithMany()
            .HasForeignKey(sti => sti.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sti => sti.IsActive);
    }
}