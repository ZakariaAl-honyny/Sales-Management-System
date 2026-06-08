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
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<Product>? _products;
    private IGenericRepository<Warehouse>? _warehouses;
    private IGenericRepository<Supplier>? _suppliers;
    private IGenericRepository<Customer>? _customers;
    private IGenericRepository<DocumentSequence>? _documentSequences;
    private IGenericRepository<WarehouseStock>? _warehouseStocks;
    private IGenericRepository<InventoryMovement>? _inventoryMovements;
    private IGenericRepository<SalesInvoice>? _salesInvoices;
    private IGenericRepository<PurchaseInvoice>? _purchaseInvoices;
    private IGenericRepository<SalesReturn>? _salesReturns;
    private IGenericRepository<PurchaseReturn>? _purchaseReturns;
    private IGenericRepository<StockTransfer>? _stockTransfers;
    private IGenericRepository<CustomerPayment>? _customerPayments;
    private IGenericRepository<SupplierPayment>? _supplierPayments;
    private IGenericRepository<StoreSettings>? _storeSettings;
    private IGenericRepository<SystemLog>? _systemLogs;
    private IGenericRepository<StockTransferItem>? _stockTransferItems;
    private IGenericRepository<SalesInvoiceItem>? _salesInvoiceItems;
    private IGenericRepository<PurchaseInvoiceItem>? _purchaseInvoiceItems;
private IGenericRepository<ProductBarcode>? _productBarcodes;
    private IGenericRepository<ProductUnit>? _productUnits;
    private IGenericRepository<CashBox>? _cashBoxes;
    private IGenericRepository<CashTransaction>? _cashTransactions;
    private IGenericRepository<SystemSetting>? _systemSettings;
    private IGenericRepository<UnitBarcode>? _unitBarcodes;
    private IGenericRepository<DailyClosure>? _dailyClosures;
    private IGenericRepository<StockWriteOff>? _stockWriteOffs;
    private IGenericRepository<ProductPriceHistory>? _productPriceHistory;
    private IGenericRepository<Account>? _accounts;
    private IGenericRepository<JournalEntry>? _journalEntries;
    private IGenericRepository<JournalEntryLine>? _journalEntryLines;
    private IGenericRepository<SystemAccountMappings>? _systemAccountMappings;
    private IGenericRepository<FiscalYearClosure>? _fiscalYearClosures;
    private IGenericRepository<Currency>? _currencies;
    private IGenericRepository<Permission>? _permissions;
    private IGenericRepository<RolePermission>? _rolePermissions;
    private IAuditLogRepository? _auditLogs;
    private IGenericRepository<UserSession>? _userSessions;
    private IGenericRepository<ExchangeRateHistory>? _exchangeRateHistories;
    private IGenericRepository<CustomerGroup>? _customerGroups;

    public UnitOfWork(SalesDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
    public IGenericRepository<Unit> Units => _units ??= new GenericRepository<Unit>(_context);
    public IGenericRepository<Tax> Taxes => _taxes ??= new GenericRepository<Tax>(_context);
    public IGenericRepository<Category> Categories => _categories ??= new GenericRepository<Category>(_context);
    public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);
    public IGenericRepository<Warehouse> Warehouses => _warehouses ??= new GenericRepository<Warehouse>(_context);
    public IGenericRepository<Supplier> Suppliers => _suppliers ??= new GenericRepository<Supplier>(_context);
    public IGenericRepository<Customer> Customers => _customers ??= new GenericRepository<Customer>(_context);
    public IGenericRepository<DocumentSequence> DocumentSequences => _documentSequences ??= new GenericRepository<DocumentSequence>(_context);
    public IGenericRepository<WarehouseStock> WarehouseStocks => _warehouseStocks ??= new GenericRepository<WarehouseStock>(_context);
    public IGenericRepository<InventoryMovement> InventoryMovements => _inventoryMovements ??= new GenericRepository<InventoryMovement>(_context);
    public IGenericRepository<SalesInvoice> SalesInvoices => _salesInvoices ??= new GenericRepository<SalesInvoice>(_context);
    public IGenericRepository<PurchaseInvoice> PurchaseInvoices => _purchaseInvoices ??= new GenericRepository<PurchaseInvoice>(_context);
    public IGenericRepository<SalesReturn> SalesReturns => _salesReturns ??= new GenericRepository<SalesReturn>(_context);
    public IGenericRepository<PurchaseReturn> PurchaseReturns => _purchaseReturns ??= new GenericRepository<PurchaseReturn>(_context);
    public IGenericRepository<StockTransfer> StockTransfers => _stockTransfers ??= new GenericRepository<StockTransfer>(_context);
    public IGenericRepository<CustomerPayment> CustomerPayments => _customerPayments ??= new GenericRepository<CustomerPayment>(_context);
    public IGenericRepository<SupplierPayment> SupplierPayments => _supplierPayments ??= new GenericRepository<SupplierPayment>(_context);
    public IGenericRepository<StoreSettings> StoreSettings => _storeSettings ??= new GenericRepository<StoreSettings>(_context);
    public IGenericRepository<SystemLog> SystemLogs => _systemLogs ??= new GenericRepository<SystemLog>(_context);
    public IGenericRepository<StockTransferItem> StockTransferItems => _stockTransferItems ??= new GenericRepository<StockTransferItem>(_context);
    public IGenericRepository<SalesInvoiceItem> SalesInvoiceItems => _salesInvoiceItems ??= new GenericRepository<SalesInvoiceItem>(_context);
    public IGenericRepository<PurchaseInvoiceItem> PurchaseInvoiceItems => _purchaseInvoiceItems ??= new GenericRepository<PurchaseInvoiceItem>(_context);
public IGenericRepository<ProductBarcode> ProductBarcodes => _productBarcodes ??= new GenericRepository<ProductBarcode>(_context);
    public IGenericRepository<ProductUnit> ProductUnits => _productUnits ??= new GenericRepository<ProductUnit>(_context);
    public IGenericRepository<CashBox> CashBoxes => _cashBoxes ??= new GenericRepository<CashBox>(_context);
    public IGenericRepository<CashTransaction> CashTransactions => _cashTransactions ??= new GenericRepository<CashTransaction>(_context);
    public IGenericRepository<SystemSetting> SystemSettings => _systemSettings ??= new GenericRepository<SystemSetting>(_context);
    public IGenericRepository<UnitBarcode> UnitBarcodes => _unitBarcodes ??= new GenericRepository<UnitBarcode>(_context);
    public IGenericRepository<DailyClosure> DailyClosures => _dailyClosures ??= new GenericRepository<DailyClosure>(_context);
    public IGenericRepository<StockWriteOff> StockWriteOffs => _stockWriteOffs ??= new GenericRepository<StockWriteOff>(_context);
    public IGenericRepository<ProductPriceHistory> ProductPriceHistory => _productPriceHistory ??= new GenericRepository<ProductPriceHistory>(_context);
    public IGenericRepository<Account> Accounts => _accounts ??= new GenericRepository<Account>(_context);
    public IGenericRepository<JournalEntry> JournalEntries => _journalEntries ??= new GenericRepository<JournalEntry>(_context);
    public IGenericRepository<JournalEntryLine> JournalEntryLines => _journalEntryLines ??= new GenericRepository<JournalEntryLine>(_context);
    public IGenericRepository<SystemAccountMappings> SystemAccountMappings => _systemAccountMappings ??= new GenericRepository<SystemAccountMappings>(_context);
    public IGenericRepository<FiscalYearClosure> FiscalYearClosures => _fiscalYearClosures ??= new GenericRepository<FiscalYearClosure>(_context);
    public IGenericRepository<Currency> Currencies => _currencies ??= new GenericRepository<Currency>(_context);
    public IGenericRepository<Permission> Permissions => _permissions ??= new GenericRepository<Permission>(_context);
    public IGenericRepository<RolePermission> RolePermissions => _rolePermissions ??= new GenericRepository<RolePermission>(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IGenericRepository<UserSession> UserSessions => _userSessions ??= new GenericRepository<UserSession>(_context);
    public IGenericRepository<ExchangeRateHistory> ExchangeRateHistories => _exchangeRateHistories ??= new GenericRepository<ExchangeRateHistory>(_context);
    public IGenericRepository<CustomerGroup> CustomerGroups => _customerGroups ??= new GenericRepository<CustomerGroup>(_context);

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