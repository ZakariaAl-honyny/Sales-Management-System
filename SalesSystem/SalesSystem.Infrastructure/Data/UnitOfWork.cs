using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Repositories;

using AppTransaction = SalesSystem.Application.Interfaces.IDbContextTransaction;
using EfTransaction = Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction;

namespace SalesSystem.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation providing access to repositories and transaction management.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SalesDbContext _context;
    private IGenericRepository<User>? _users;
    private IGenericRepository<Unit>? _units;
    private IGenericRepository<Tax>? _taxes;
    private IGenericRepository<Product>? _products;
    private IGenericRepository<Warehouse>? _warehouses;
    private IGenericRepository<Supplier>? _suppliers;
    private IGenericRepository<Customer>? _customers;
    private IGenericRepository<DocumentSequence>? _documentSequences;
    private IGenericRepository<WarehouseStock>? _warehouseStocks;
    private IGenericRepository<SalesInvoice>? _salesInvoices;
    private IGenericRepository<PurchaseInvoice>? _purchaseInvoices;
    private IGenericRepository<SalesReturn>? _salesReturns;
    private IGenericRepository<PurchaseReturn>? _purchaseReturns;
    private IGenericRepository<SupplierPayment>? _supplierPayments;
    private IGenericRepository<CurrencyRate>? _currencyRates;
    private ISystemLogRepository? _systemLogs;
    private IGenericRepository<SalesInvoiceLine>? _SalesInvoiceLines;
    private IGenericRepository<PurchaseInvoiceLine>? _PurchaseInvoiceLines;
    private IGenericRepository<ProductUnit>? _productUnits;
    private IGenericRepository<CashBox>? _cashBoxes;
    private IGenericRepository<SystemSetting>? _systemSettings;
    private IGenericRepository<Account>? _accounts;
    private IGenericRepository<JournalEntry>? _journalEntries;
    private IGenericRepository<JournalEntryLine>? _journalEntryLines;
    private IGenericRepository<SystemAccountMapping>? _systemAccountMappings;
    private IGenericRepository<ReceiptVoucher>? _receiptVouchers;
    private IGenericRepository<PaymentVoucher>? _paymentVouchers;
    private IGenericRepository<FiscalYear>? _fiscalYears;
    private IGenericRepository<Currency>? _currencies;
    private IGenericRepository<Permission>? _permissions;
    private IGenericRepository<Role>? _roles;
    private IGenericRepository<UserRole>? _userRoles;
    private IGenericRepository<RolePermission>? _rolePermissions;
    private IAuditLogRepository? _auditLogs;
    private IGenericRepository<UserSession>? _userSessions;
    private IGenericRepository<UserBranch>? _userBranches;
    private IGenericRepository<InventoryBatch>? _inventoryBatches;
    private IGenericRepository<ProductPrice>? _productPrices;

    // New entity repositories (v4.7+)
    private IGenericRepository<Party>? _parties;
    private IGenericRepository<Attachment>? _attachments;
    private IGenericRepository<Notification>? _notifications;
    private IGenericRepository<Branch>? _branches;
    private IGenericRepository<Department>? _departments;
    private IGenericRepository<Bank>? _banks;
    private IGenericRepository<Employee>? _employees;
    private IGenericRepository<ProductCategory>? _productCategories;
    private IGenericRepository<InventoryCount>? _inventoryCounts;
    private IGenericRepository<InventoryCountLine>? _inventoryCountLines;
    private IGenericRepository<InventoryAdjustment>? _inventoryAdjustments;
    private IGenericRepository<InventoryAdjustmentLine>? _inventoryAdjustmentLines;
    private IGenericRepository<Expense>? _expenses;
    private IGenericRepository<CustomerReceipt>? _customerReceipts;
    private IGenericRepository<CustomerReceiptApplication>? _customerReceiptApplications;
    private IGenericRepository<SupplierPaymentApplication>? _supplierPaymentApplications;
    private IGenericRepository<AccountCategory>? _accountCategories;
    private IGenericRepository<CompanySettings>? _companySettings;
    // Customer/Supplier Contact repositories
    private IGenericRepository<CustomerContact>? _customerContacts;
    private IGenericRepository<SupplierContact>? _supplierContacts;

    // === New Inventory Module (v4.10+) ===
    private IGenericRepository<InventoryTransaction>? _inventoryTransactions;
    private IGenericRepository<InventoryTransactionLine>? _inventoryTransactionLines;
    private IGenericRepository<WarehouseTransfer>? _warehouseTransfers;
    private IGenericRepository<WarehouseTransferLine>? _warehouseTransferLines;

    public UnitOfWork(SalesDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
    public IGenericRepository<Unit> Units => _units ??= new GenericRepository<Unit>(_context);
    public IGenericRepository<Tax> Taxes => _taxes ??= new GenericRepository<Tax>(_context);
    public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);
    public IGenericRepository<Warehouse> Warehouses => _warehouses ??= new GenericRepository<Warehouse>(_context);
    public IGenericRepository<Supplier> Suppliers => _suppliers ??= new GenericRepository<Supplier>(_context);
    public IGenericRepository<Customer> Customers => _customers ??= new GenericRepository<Customer>(_context);
    public IGenericRepository<DocumentSequence> DocumentSequences => _documentSequences ??= new GenericRepository<DocumentSequence>(_context);
    public IGenericRepository<WarehouseStock> WarehouseStocks => _warehouseStocks ??= new GenericRepository<WarehouseStock>(_context);
    public IGenericRepository<SalesInvoice> SalesInvoices => _salesInvoices ??= new GenericRepository<SalesInvoice>(_context);
    public IGenericRepository<PurchaseInvoice> PurchaseInvoices => _purchaseInvoices ??= new GenericRepository<PurchaseInvoice>(_context);
    public IGenericRepository<SalesReturn> SalesReturns => _salesReturns ??= new GenericRepository<SalesReturn>(_context);
    public IGenericRepository<PurchaseReturn> PurchaseReturns => _purchaseReturns ??= new GenericRepository<PurchaseReturn>(_context);
    public IGenericRepository<SupplierPayment> SupplierPayments => _supplierPayments ??= new GenericRepository<SupplierPayment>(_context);
    public IGenericRepository<CurrencyRate> CurrencyRates => _currencyRates ??= new GenericRepository<CurrencyRate>(_context);
    public ISystemLogRepository SystemLogs => _systemLogs ??= new SystemLogRepository(_context);
    public IGenericRepository<SalesInvoiceLine> SalesInvoiceLines => _SalesInvoiceLines ??= new GenericRepository<SalesInvoiceLine>(_context);
    public IGenericRepository<PurchaseInvoiceLine> PurchaseInvoiceLines => _PurchaseInvoiceLines ??= new GenericRepository<PurchaseInvoiceLine>(_context);
    public IGenericRepository<ProductUnit> ProductUnits => _productUnits ??= new GenericRepository<ProductUnit>(_context);
    public IGenericRepository<CashBox> CashBoxes => _cashBoxes ??= new GenericRepository<CashBox>(_context);
    public IGenericRepository<SystemSetting> SystemSettings => _systemSettings ??= new GenericRepository<SystemSetting>(_context);
    public IGenericRepository<Account> Accounts => _accounts ??= new GenericRepository<Account>(_context);
    public IGenericRepository<JournalEntry> JournalEntries => _journalEntries ??= new GenericRepository<JournalEntry>(_context);
    public IGenericRepository<JournalEntryLine> JournalEntryLines => _journalEntryLines ??= new GenericRepository<JournalEntryLine>(_context);
    public IGenericRepository<SystemAccountMapping> SystemAccountMappings => _systemAccountMappings ??= new GenericRepository<SystemAccountMapping>(_context);
    public IGenericRepository<ReceiptVoucher> ReceiptVouchers => _receiptVouchers ??= new GenericRepository<ReceiptVoucher>(_context);
    public IGenericRepository<PaymentVoucher> PaymentVouchers => _paymentVouchers ??= new GenericRepository<PaymentVoucher>(_context);
    public IGenericRepository<FiscalYear> FiscalYears => _fiscalYears ??= new GenericRepository<FiscalYear>(_context);
    public IGenericRepository<Currency> Currencies => _currencies ??= new GenericRepository<Currency>(_context);
    public IGenericRepository<Permission> Permissions => _permissions ??= new GenericRepository<Permission>(_context);
    public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
    public IGenericRepository<UserRole> UserRoles => _userRoles ??= new GenericRepository<UserRole>(_context);
    public IGenericRepository<RolePermission> RolePermissions => _rolePermissions ??= new GenericRepository<RolePermission>(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IGenericRepository<UserSession> UserSessions => _userSessions ??= new GenericRepository<UserSession>(_context);
    public IGenericRepository<UserBranch> UserBranches => _userBranches ??= new GenericRepository<UserBranch>(_context);
    public IGenericRepository<InventoryBatch> InventoryBatches => _inventoryBatches ??= new GenericRepository<InventoryBatch>(_context);
    public IGenericRepository<ProductPrice> ProductPrices => _productPrices ??= new GenericRepository<ProductPrice>(_context);
    // New entity repositories (v4.7+)
    public IGenericRepository<Party> Parties => _parties ??= new GenericRepository<Party>(_context);
    public IGenericRepository<Attachment> Attachments => _attachments ??= new GenericRepository<Attachment>(_context);
    public IGenericRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_context);
    public IGenericRepository<Branch> Branches => _branches ??= new GenericRepository<Branch>(_context);
    public IGenericRepository<Department> Departments => _departments ??= new GenericRepository<Department>(_context);
    public IGenericRepository<Bank> Banks => _banks ??= new GenericRepository<Bank>(_context);
    public IGenericRepository<Employee> Employees => _employees ??= new GenericRepository<Employee>(_context);
    public IGenericRepository<ProductCategory> ProductCategories => _productCategories ??= new GenericRepository<ProductCategory>(_context);
    public IGenericRepository<InventoryCount> InventoryCounts => _inventoryCounts ??= new GenericRepository<InventoryCount>(_context);
    public IGenericRepository<InventoryCountLine> InventoryCountLines => _inventoryCountLines ??= new GenericRepository<InventoryCountLine>(_context);
    public IGenericRepository<InventoryAdjustment> InventoryAdjustments => _inventoryAdjustments ??= new GenericRepository<InventoryAdjustment>(_context);
    public IGenericRepository<InventoryAdjustmentLine> InventoryAdjustmentLines => _inventoryAdjustmentLines ??= new GenericRepository<InventoryAdjustmentLine>(_context);
    public IGenericRepository<Expense> Expenses => _expenses ??= new GenericRepository<Expense>(_context);
    public IGenericRepository<CustomerReceipt> CustomerReceipts => _customerReceipts ??= new GenericRepository<CustomerReceipt>(_context);
    public IGenericRepository<CustomerReceiptApplication> CustomerReceiptApplications => _customerReceiptApplications ??= new GenericRepository<CustomerReceiptApplication>(_context);
    public IGenericRepository<SupplierPaymentApplication> SupplierPaymentApplications => _supplierPaymentApplications ??= new GenericRepository<SupplierPaymentApplication>(_context);
    public IGenericRepository<AccountCategory> AccountCategories => _accountCategories ??= new GenericRepository<AccountCategory>(_context);
    public IGenericRepository<CompanySettings> CompanySettings => _companySettings ??= new GenericRepository<CompanySettings>(_context);

    // Customer/Supplier Contact repositories
    public IGenericRepository<CustomerContact> CustomerContacts => _customerContacts ??= new GenericRepository<CustomerContact>(_context);
    public IGenericRepository<SupplierContact> SupplierContacts => _supplierContacts ??= new GenericRepository<SupplierContact>(_context);

    // === New Inventory Module (v4.10+) ===
    public IGenericRepository<InventoryTransaction> InventoryTransactions => _inventoryTransactions ??= new GenericRepository<InventoryTransaction>(_context);
    public IGenericRepository<InventoryTransactionLine> InventoryTransactionLines => _inventoryTransactionLines ??= new GenericRepository<InventoryTransactionLine>(_context);
    public IGenericRepository<WarehouseTransfer> WarehouseTransfers => _warehouseTransfers ??= new GenericRepository<WarehouseTransfer>(_context);
    public IGenericRepository<WarehouseTransferLine> WarehouseTransferLines => _warehouseTransferLines ??= new GenericRepository<WarehouseTransferLine>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<AppTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new DbContextTransactionWrapper(transaction);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(operation);
    }

    public async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync<Func<Task>, object?>(
            operation,
            async (ctx, op, token) =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync(token);
                try
                {
                    await op();
                    await transaction.CommitAsync(token);
                    return null;
                }
                catch
                {
                    await transaction.RollbackAsync(token);
                    throw;
                }
            },
            null,
            ct).ConfigureAwait(false);
    }

    public async Task<TResult> ExecuteTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<Func<Task<TResult>>, TResult>(
            operation,
            async (ctx, op, token) =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync(token);
                try
                {
                    var result = await op();
                    await transaction.CommitAsync(token);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(token);
                    throw;
                }
            },
            null,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Wrapper class that adapts EF Core's IDbContextTransaction to our custom IDbContextTransaction interface.
    /// </summary>
    private class DbContextTransactionWrapper : AppTransaction
    {
        private readonly EfTransaction _innerTransaction;

        public DbContextTransactionWrapper(EfTransaction innerTransaction)
        {
            _innerTransaction = innerTransaction;
        }

        public Task CommitAsync(CancellationToken ct = default)
        {
            return _innerTransaction.CommitAsync(ct);
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            return _innerTransaction.RollbackAsync(ct);
        }

        public ValueTask DisposeAsync()
        {
            return _innerTransaction.DisposeAsync();
        }

        public void Dispose()
        {
            _innerTransaction.Dispose();
        }
    }
}
