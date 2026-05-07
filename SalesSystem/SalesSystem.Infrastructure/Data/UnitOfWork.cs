using Microsoft.EntityFrameworkCore.Storage;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data.Repositories;

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
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<Product>? _products;
    private IGenericRepository<Warehouse>? _warehouses;
    private IGenericRepository<Supplier>? _suppliers;
    private IGenericRepository<Customer>? _customers;
    private IGenericRepository<DocumentSequence>? _documentSequences;
    private IGenericRepository<WarehouseStock>? _warehouseStocks;
    private IGenericRepository<InventoryMovement>? _inventoryMovements;
    private IGenericRepository<StoreSettings>? _storeSettings;

    public UnitOfWork(SalesDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
    public IGenericRepository<Unit> Units => _units ??= new GenericRepository<Unit>(_context);
    public IGenericRepository<Category> Categories => _categories ??= new GenericRepository<Category>(_context);
    public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);
    public IGenericRepository<Warehouse> Warehouses => _warehouses ??= new GenericRepository<Warehouse>(_context);
    public IGenericRepository<Supplier> Suppliers => _suppliers ??= new GenericRepository<Supplier>(_context);
    public IGenericRepository<Customer> Customers => _customers ??= new GenericRepository<Customer>(_context);
    public IGenericRepository<DocumentSequence> DocumentSequences => _documentSequences ??= new GenericRepository<DocumentSequence>(_context);
    public IGenericRepository<WarehouseStock> WarehouseStocks => _warehouseStocks ??= new GenericRepository<WarehouseStock>(_context);
    public IGenericRepository<InventoryMovement> InventoryMovements => _inventoryMovements ??= new GenericRepository<InventoryMovement>(_context);
    public IGenericRepository<StoreSettings> StoreSettings => _storeSettings ??= new GenericRepository<StoreSettings>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<AppTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new DbContextTransactionWrapper(transaction);
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