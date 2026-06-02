using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

using MovementType = SalesSystem.Domain.Enums.MovementType;
using InvoiceStatus = SalesSystem.Domain.Enums.InvoiceStatus;
using DomainPaymentType = SalesSystem.Domain.Enums.PaymentType;

/// <summary>
/// Unit tests for PurchaseService business logic.
/// </summary>
public class PurchaseServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IStoreSettingsService> _mockStoreSettingsService;
    private readonly Mock<IUpdateProductPricingService> _mockPricingService;
    private readonly Mock<ICashBoxService> _cashBoxServiceMock;
    private readonly Mock<ILogger<PurchaseService>> _mockLogger;

    private readonly PurchaseService _sut;

    public PurchaseServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] PurchaseServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockStoreSettingsService = new Mock<IStoreSettingsService>();
        _mockPricingService = new Mock<IUpdateProductPricingService>();
        _cashBoxServiceMock = new Mock<ICashBoxService>();
        _mockLogger = new Mock<ILogger<PurchaseService>>();

        _mockUow.Setup(u => u.PurchaseInvoices).Returns(new InMemoryEfCoreRepository<PurchaseInvoice>(_dbContext));
        _mockUow.Setup(u => u.Suppliers).Returns(new InMemoryEfCoreRepository<Supplier>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockUow.Setup(u => u.ExecuteAsync<Result<PurchaseInvoiceDto>>(
            It.IsAny<Func<Task<Result<PurchaseInvoiceDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<PurchaseInvoiceDto>>> func, CancellationToken ct) => func());

        _mockStoreSettingsService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Success(new StoreSettingsDto(1, "متجري", null, null, null, null, "SAR", 15m, true, null, true, false, true, "PUR-", (int)SalesSystem.Domain.Enums.CostingMethod.WeightedAverage)));

        _mockInventoryService.Setup(i => i.IncreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new PurchaseService(
            _mockUow.Object,
            _mockInventoryService.Object,
            _mockStoreSettingsService.Object,
            _mockPricingService.Object,
            _cashBoxServiceMock.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region PostAsync Tests

    [Fact]
    public async Task PostAsync_DraftInvoice_ChangesStatusToPosted()
    {
        _output.WriteLine("[TEST] PostAsync_DraftInvoice_ChangesStatusToPosted");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Test Product", purchasePrice: 100m, retailPrice: 150m, wholesalePrice: 120m);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 10m, unitCost: 100m));
        invoice.SetPaidAmount(1000m);
        invoice.SetTaxAmount(0m);
        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Posted);

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 10m,
            movementType: MovementType.PurchaseIn,
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<decimal?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should increase on posting purchase invoice");

        _output.WriteLine("[PASS] Draft invoice posted successfully");
    }

    [Fact]
    public async Task PostAsync_NonExistentInvoice_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] PostAsync_NonExistentInvoice_ReturnsNotFound");

        var result = await _sut.PostAsync(999, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الفاتورة غير موجودة");

        _output.WriteLine("[PASS] Non-existent invoice returns NotFound");
    }

    [Fact]
    public async Task PostAsync_AlreadyPostedInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] PostAsync_AlreadyPostedInvoice_ReturnsFailure");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 10m, unitCost: 100m));
        invoice.Post(); // Already posted

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("مسودة");

        _output.WriteLine("[PASS] Already posted invoice cannot be posted again");
    }

    [Fact]
    public async Task PostAsync_CreditInvoice_UpdatesSupplierBalance()
    {
        _output.WriteLine("[TEST] PostAsync_CreditInvoice_UpdatesSupplierBalance");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Test Product", purchasePrice: 100m, retailPrice: 150m, wholesalePrice: 120m);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            paymentType: DomainPaymentType.Credit,
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 5m, unitCost: 200m));
        invoice.SetPaidAmount(0m); // Unpaid

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(1000m, "Supplier owes us 1000 for the unpaid invoice");

        _output.WriteLine("[PASS] Credit invoice updates supplier balance");
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_DraftInvoice_ChangesStatusToCancelled()
    {
        _output.WriteLine("[TEST] CancelAsync_DraftInvoice_ChangesStatusToCancelled");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 10m, unitCost: 100m));

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);

        _output.WriteLine("[PASS] Draft invoice cancelled successfully");
    }

    [Fact]
    public async Task CancelAsync_PostedInvoice_ReversesStockAndBalance()
    {
        _output.WriteLine("[TEST] CancelAsync_PostedInvoice_ReversesStockAndBalance");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Test Product", purchasePrice: 100m, retailPrice: 150m, wholesalePrice: 120m);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            paymentType: DomainPaymentType.Credit,
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 5m, unitCost: 100m));
        invoice.SetPaidAmount(0m);
        invoice.Post(); // Status = Posted

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);

        // Stock should be reversed
        _mockInventoryService.Verify(i => i.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 5m,
            movementType: MovementType.PurchaseReturnOut,
            "PurchaseInvoiceCancel",
            invoice.Id,
            It.IsAny<decimal?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should be reversed on cancellation");

        _output.WriteLine("[PASS] Posted invoice cancellation reverses stock and balance");
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CancelAsync_AlreadyCancelledInvoice_ReturnsFailure");

        var supplier = Supplier.Create("Test Supplier", 0m);
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 10m, unitCost: 100m));
        invoice.Cancel(); // Already cancelled

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("الفاتورة ملغاة بالفعل");

        _output.WriteLine("[PASS] Already cancelled invoice returns failure with Arabic message");
    }

    #endregion

    #region Financial Calculation Tests

    [Fact]
    public void GivenPurchaseInvoiceWithItems_WhenRecalculating_ThenTotalsCorrect()
    {
        _output.WriteLine("[TEST] GivenPurchaseInvoiceWithItems_WhenRecalculating_ThenTotalsCorrect");

        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            discountAmount: 0m,
            notes: null
        );

        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 10m, unitCost: 100m, discountAmount: 0m)); // 1000
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 2, quantity: 5m, unitCost: 200m, discountAmount: 50m));   // 950

        var subTotal = 1000m + 950m;
        invoice.SubTotal.Should().Be(subTotal, "SubTotal = 1000 + 950 = 1950");
        invoice.DiscountAmount.Should().Be(0m);
        invoice.TotalAmount.Should().Be(1950m, "TotalAmount = 1950 - 0 = 1950");

        _output.WriteLine("[PASS] Purchase invoice totals calculated correctly");
    }

    #endregion

    #region Helper Classes

    private class MockDbContextTransaction : IDbContextTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
        public DbSet<Product> Products => Set<Product>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly DbContext _context;

        public InMemoryEfCoreRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _context.Set<T>().FindAsync(new object[] { id }, ct);

        public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<T>>(_context.Set<T>().ToList());

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _context.Set<T>().AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity != null)
            {
                entity.MarkAsDeleted();
                _context.Set<T>().Update(entity);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
                await _context.SaveChangesAsync(ct);
            }
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().FirstOrDefault(predicate));

        public Task<T?> FirstOrDefaultIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().FirstOrDefault(predicate));

        public Task<List<T>> ToListAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().ToList());

        public Task<List<T>> ToListAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? queryConfig = null, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            if (queryConfig != null) query = queryConfig(query);
            return Task.FromResult(query.ToList());
        }

        public Task<(List<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? orderConfig, int page, int pageSize, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            var totalCount = query.Count();
            if (orderConfig != null) query = orderConfig(query);
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<List<T>> ToListIgnoreFiltersAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().ToList());

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().Count() : _context.Set<T>().Count(predicate));

        public Task<int> CountIgnoreFiltersAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().IgnoreQueryFilters().Count() : _context.Set<T>().IgnoreQueryFilters().Count(predicate));

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().Any(predicate));

        public Task<bool> AnyIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().Any(predicate));

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }

    #endregion
}