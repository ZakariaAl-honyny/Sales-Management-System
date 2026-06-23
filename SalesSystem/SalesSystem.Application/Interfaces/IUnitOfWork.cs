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
    IGenericRepository<Product> Products { get; }
    IGenericRepository<Warehouse> Warehouses { get; }
    IGenericRepository<Supplier> Suppliers { get; }
    IGenericRepository<Customer> Customers { get; }
    IGenericRepository<DocumentSequence> DocumentSequences { get; }
    IGenericRepository<WarehouseStock> WarehouseStocks { get; }
    IGenericRepository<SalesInvoice> SalesInvoices { get; }
    IGenericRepository<PurchaseInvoice> PurchaseInvoices { get; }
    IGenericRepository<SalesReturn> SalesReturns { get; }
    IGenericRepository<PurchaseReturn> PurchaseReturns { get; }
    IGenericRepository<SupplierPayment> SupplierPayments { get; }
    IGenericRepository<CurrencyRate> CurrencyRates { get; }
    ISystemLogRepository SystemLogs { get; }
    IGenericRepository<SalesInvoiceLine> SalesInvoiceLines { get; }
    IGenericRepository<PurchaseInvoiceLine> PurchaseInvoiceLines { get; }
    IGenericRepository<ProductUnit> ProductUnits { get; }
    IGenericRepository<CashBox> CashBoxes { get; }
    IGenericRepository<SystemSetting> SystemSettings { get; }
    IGenericRepository<Account> Accounts { get; }
    IGenericRepository<JournalEntry> JournalEntries { get; }
    IGenericRepository<JournalEntryLine> JournalEntryLines { get; }
    IGenericRepository<SystemAccountMapping> SystemAccountMappings { get; }
    IGenericRepository<ReceiptVoucher> ReceiptVouchers { get; }
    IGenericRepository<PaymentVoucher> PaymentVouchers { get; }
    IGenericRepository<FiscalYear> FiscalYears { get; }
    IGenericRepository<Currency> Currencies { get; }
    IGenericRepository<Permission> Permissions { get; }
    IGenericRepository<RolePermission> RolePermissions { get; }
    IGenericRepository<InventoryBatch> InventoryBatches { get; }
    IGenericRepository<ProductPrice> ProductPrices { get; }
    IGenericRepository<Role> Roles { get; }
    IGenericRepository<UserRole> UserRoles { get; }
    IAuditLogRepository AuditLogs { get; }
    IGenericRepository<UserSession> UserSessions { get; }
    IGenericRepository<UserBranch> UserBranches { get; }

    // New entity repositories (v4.7+)
    IGenericRepository<Attachment> Attachments { get; }
    IGenericRepository<Notification> Notifications { get; }
    IGenericRepository<Branch> Branches { get; }
    IGenericRepository<Department> Departments { get; }
    IGenericRepository<Bank> Banks { get; }
    IGenericRepository<Employee> Employees { get; }
    IGenericRepository<ProductCategory> ProductCategories { get; }
    IGenericRepository<InventoryCount> InventoryCounts { get; }
    IGenericRepository<InventoryCountLine> InventoryCountLines { get; }
    IGenericRepository<InventoryAdjustment> InventoryAdjustments { get; }
    IGenericRepository<InventoryAdjustmentLine> InventoryAdjustmentLines { get; }
    IGenericRepository<Expense> Expenses { get; }
    IGenericRepository<CustomerReceipt> CustomerReceipts { get; }
    IGenericRepository<CustomerReceiptApplication> CustomerReceiptApplications { get; }
    IGenericRepository<SupplierPaymentApplication> SupplierPaymentApplications { get; }
    IGenericRepository<AccountCategory> AccountCategories { get; }
    IGenericRepository<CompanySettings> CompanySettings { get; }

    // Customer/Supplier Contact repositories
    IGenericRepository<CustomerContact> CustomerContacts { get; }
    IGenericRepository<SupplierContact> SupplierContacts { get; }

    // === Sales Quotation Module ===
    IGenericRepository<SalesQuotation> SalesQuotations { get; }
    IGenericRepository<SalesQuotationItem> SalesQuotationItems { get; }

    // === New Inventory Module (v4.10+) ===
    IGenericRepository<InventoryTransaction> InventoryTransactions { get; }
    IGenericRepository<InventoryTransactionLine> InventoryTransactionLines { get; }
    IGenericRepository<WarehouseTransfer> WarehouseTransfers { get; }
    IGenericRepository<WarehouseTransferLine> WarehouseTransferLines { get; }

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
