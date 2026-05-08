using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Common;

namespace SalesSystem.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Unit> Units { get; }
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
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public interface IDbContextTransaction : IAsyncDisposable, IDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}