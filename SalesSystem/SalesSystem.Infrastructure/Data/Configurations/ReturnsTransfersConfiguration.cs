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
        builder.Property(sr => sr.Reason).HasMaxLength(250);
        builder.Property(sr => sr.Status).HasConversion<byte>();
        
        builder.HasOne(sr => sr.Customer)
            .WithMany()
            .HasForeignKey(sr => sr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(sr => sr.Warehouse)
            .WithMany()
            .HasForeignKey(sr => sr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(sr => sr.Items)
            .WithOne(i => i.SalesReturn)
            .HasForeignKey(i => i.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.ToTable("SalesReturnItems");
        builder.HasKey(sri => sri.SalesReturnItemId);
        builder.Property(sri => sri.Quantity).HasPrecision(18, 3);
        builder.Property(sri => sri.UnitPrice).HasPrecision(18, 2);
        builder.Property(sri => sri.LineTotal).HasPrecision(18, 2);
        
        builder.HasOne(sri => sri.Product)
            .WithMany()
            .HasForeignKey(sri => sri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns");
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.ReturnNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(pr => pr.ReturnNo).IsUnique();
        builder.Property(pr => pr.SubTotal).HasPrecision(18, 2);
        builder.Property(pr => pr.TotalAmount).HasPrecision(18, 2);
        builder.Property(pr => pr.Reason).HasMaxLength(250);
        builder.Property(pr => pr.Status).HasConversion<byte>();
        
        builder.HasOne(pr => pr.Supplier)
            .WithMany()
            .HasForeignKey(pr => pr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(pr => pr.Warehouse)
            .WithMany()
            .HasForeignKey(pr => pr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(pr => pr.Items)
            .WithOne(i => i.PurchaseReturn)
            .HasForeignKey(i => i.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.ToTable("PurchaseReturnItems");
        builder.HasKey(pri => pri.PurchaseReturnItemId);
        builder.Property(pri => pri.Quantity).HasPrecision(18, 3);
        builder.Property(pri => pri.UnitCost).HasPrecision(18, 2);
        builder.Property(pri => pri.LineTotal).HasPrecision(18, 2);
        
        builder.HasOne(pri => pri.Product)
            .WithMany()
            .HasForeignKey(pri => pri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
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
    }
}

public class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> builder)
    {
        builder.ToTable("StockTransferItems");
        builder.HasKey(sti => sti.StockTransferItemId);
        builder.Property(sti => sti.Quantity).HasPrecision(18, 3);
        builder.Property(sti => sti.Notes).HasMaxLength(250);
        
        builder.HasOne(sti => sti.Product)
            .WithMany()
            .HasForeignKey(sti => sti.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}