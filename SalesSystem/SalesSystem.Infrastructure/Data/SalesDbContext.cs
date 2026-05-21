using Microsoft.EntityFrameworkCore;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data;

public class SalesDbContext : DbContext
{
    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<UnitBarcode> UnitBarcodes => Set<UnitBarcode>();
    public DbSet<CashBox> CashBoxes => Set<CashBox>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ProductPriceHistory> ProductPriceHistory => Set<ProductPriceHistory>();

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }

    public static string? GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");
    }
}