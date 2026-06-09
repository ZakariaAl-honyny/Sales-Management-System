using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Unit> Units { get; }
    IGenericRepository<Tax> Taxes { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<Product> Products { get; }
    IGenericRepository<Warehouse> Warehouses { get; }
    IGenericRepository<Supplier> Suppliers { get; }
    IGenericRepository<Customer> Customers { get; }
    IGenericRepository<DocumentSequence> DocumentSequences { get; }
    IGenericRepository<WarehouseStock> WarehouseStocks { get; }
    IGenericRepository<InventoryMovement> InventoryMovements { get; }
    IGenericRepository<SalesInvoice> SalesInvoices { get; }
    IGenericRepository<PurchaseInvoice> PurchaseInvoices { get; }
    IGenericRepository<SalesReturn> SalesReturns { get; }
    IGenericRepository<PurchaseReturn> PurchaseReturns { get; }
    IGenericRepository<StockTransfer> StockTransfers { get; }
    IGenericRepository<CustomerPayment> CustomerPayments { get; }
    IGenericRepository<SupplierPayment> SupplierPayments { get; }
    IGenericRepository<StoreSettings> StoreSettings { get; }
    IGenericRepository<SystemLog> SystemLogs { get; }
    IGenericRepository<StockTransferItem> StockTransferItems { get; }
    IGenericRepository<SalesInvoiceItem> SalesInvoiceItems { get; }
    IGenericRepository<PurchaseInvoiceItem> PurchaseInvoiceItems { get; }
    IGenericRepository<ProductBarcode> ProductBarcodes { get; }
    IGenericRepository<UnitBarcode> UnitBarcodes { get; }
    IGenericRepository<ProductUnit> ProductUnits { get; }
    IGenericRepository<CashBox> CashBoxes { get; }
    IGenericRepository<CashTransaction> CashTransactions { get; }
    IGenericRepository<SystemSetting> SystemSettings { get; }
    IGenericRepository<DailyClosure> DailyClosures { get; }
    IGenericRepository<ProductPriceHistory> ProductPriceHistory { get; }
    IGenericRepository<StockWriteOff> StockWriteOffs { get; }
    IGenericRepository<Account> Accounts { get; }
    IGenericRepository<JournalEntry> JournalEntries { get; }
    IGenericRepository<JournalEntryLine> JournalEntryLines { get; }
    IGenericRepository<SystemAccountMappings> SystemAccountMappings { get; }
    IGenericRepository<FiscalYearClosure> FiscalYearClosures { get; }
    IGenericRepository<FiscalYear> FiscalYears { get; }
    IGenericRepository<Currency> Currencies { get; }
    IGenericRepository<ExchangeRateHistory> ExchangeRateHistories { get; }
    IGenericRepository<Permission> Permissions { get; }
    IGenericRepository<RolePermission> RolePermissions { get; }
    IGenericRepository<InventoryBatch> InventoryBatches { get; }
    IGenericRepository<ProductPrice> ProductPrices { get; }
    IGenericRepository<CustomerGroup> CustomerGroups { get; }
    IGenericRepository<ProductImage> ProductImages { get; }
    IGenericRepository<InventoryOperation> InventoryOperations { get; }
    IGenericRepository<InventoryOperationItem> InventoryOperationItems { get; }
    IGenericRepository<BillOfMaterials> BillOfMaterials { get; }
    IGenericRepository<PurchaseOrder> PurchaseOrders { get; }
    IGenericRepository<PurchaseOrderItem> PurchaseOrderItems { get; }
    IGenericRepository<AdditionalFee> AdditionalFees { get; }
    IGenericRepository<AdditionalFeeAllocation> AdditionalFeeAllocations { get; }
    IGenericRepository<SalesQuotation> SalesQuotations { get; }
    IGenericRepository<SalesQuotationItem> SalesQuotationItems { get; }
    IGenericRepository<Cheque> Cheques { get; }
    IGenericRepository<PaymentAllocation> PaymentAllocations { get; }
    IAuditLogRepository AuditLogs { get; }
    IGenericRepository<UserSession> UserSessions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    
    /// <summary>
    /// Executes the given operation within an execution strategy + explicit transaction.
    /// Use this when multiple SaveChangesAsync calls must be atomic.
    /// The execution strategy provides retry for transient failures; the transaction ensures atomicity.
    /// </summary>
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken ct = default);

    /// <summary>
    /// Executes the given typed operation within an execution strategy + explicit transaction.
    /// Returns the operation's result. Use when the operation must return a value atomically.
    /// </summary>
    Task<TResult> ExecuteTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken ct = default);
}

public interface IDbContextTransaction : IAsyncDisposable, IDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}