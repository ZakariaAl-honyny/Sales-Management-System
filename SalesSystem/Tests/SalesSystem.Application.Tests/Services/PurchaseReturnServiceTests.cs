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

using DomainPaymentType = SalesSystem.Domain.Enums.PaymentType;
using MovementType = SalesSystem.Domain.Enums.MovementType;

/// <summary>
/// Unit tests for PurchaseReturnService business logic.
/// </summary>
public class PurchaseReturnServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<PurchaseReturnService>> _mockLogger;

    private readonly PurchaseReturnService _sut;

    public PurchaseReturnServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] PurchaseReturnServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<PurchaseReturnService>>();

        _mockUow.Setup(u => u.PurchaseReturns).Returns(new InMemoryEfCoreRepository<PurchaseReturn>(_dbContext));
        _mockUow.Setup(u => u.PurchaseInvoices).Returns(new InMemoryEfCoreRepository<PurchaseInvoice>(_dbContext));
        _mockUow.Setup(u => u.Suppliers).Returns(new InMemoryEfCoreRepository<Supplier>(_dbContext));
        _mockUow.Setup(u => u.WarehouseStocks).Returns(new InMemoryEfCoreRepository<WarehouseStock>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));

        _mockUow.Setup(u => u.ExecuteAsync<Result<PurchaseReturnDto>>(
            It.IsAny<Func<Task<Result<PurchaseReturnDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<PurchaseReturnDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteAsync<Result>(
            It.IsAny<Func<Task<Result>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        var storeSettingsMock = new Mock<IGenericRepository<StoreSettings>>();
        storeSettingsMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<StoreSettings, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings)null!);
        _mockUow.Setup(u => u.StoreSettings).Returns(storeSettingsMock.Object);

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("PR-2026-000001"));

        _mockInventoryService.Setup(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new PurchaseReturnService(
            _mockUow.Object,
            _mockInventoryService.Object,
            _mockSequenceService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesReturnWithStockDecrease()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesReturnWithStockDecrease");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var supplier = Supplier.Create("Test Supplier", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest(
            PurchaseInvoiceId: null,
            SupplierId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: "Return for defective item",
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, Quantity: 2m, UnitPrice: 50m, DiscountAmount: 0m, Notes: null)
            }
        );

        var createResult = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.TotalAmount.Should().Be(100m, "2 * 50 = 100");

        var postResult = await _sut.PostAsync(createResult.Value.Id, userId: 1, CancellationToken.None);
        postResult.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 2m,
            movementType: MovementType.PurchaseReturnOut,
            "PurchaseReturn",
            It.IsAny<int>(),
            50m,
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should decrease for returned items");

        _output.WriteLine("[PASS] Purchase return creates and decreases stock");
    }

    [Fact]
    public async Task CreateAsync_SupplierWithBalance_DecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_SupplierWithBalance_DecreasesBalance");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var supplier = Supplier.Create("Test Supplier", openingBalance: 5000m, phone: null, email: null, address: null, createdByUserId: null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest(
            PurchaseInvoiceId: null,
            SupplierId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, Quantity: 5m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null)
            }
        );

        var createResult = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var postResult = await _sut.PostAsync(createResult.Value!.Id, userId: 1, CancellationToken.None);
        postResult.IsSuccess.Should().BeTrue();

        supplier.CurrentBalance.Should().Be(4500m, "We owed supplier 5000, return worth 500, now owe 4500");

        _output.WriteLine("[PASS] Purchase return decreases supplier balance");
    }

    [Fact]
    public async Task CreateAsync_WithOriginalInvoice_ValidatesQuantities()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOriginalInvoice_ValidatesQuantities");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var supplier = Supplier.Create("Test Supplier", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
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
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 5m, unitCost: 50m));
        invoice.SetPaidAmount(250m);
        invoice.Post();
        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest(
            PurchaseInvoiceId: 1,
            SupplierId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, Quantity: 10m, UnitPrice: 50m, DiscountAmount: 0m, Notes: null) // Exceeds original
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من الكمية المشتراة");

        _output.WriteLine("[PASS] Return quantity exceeding original invoice fails");
    }

    [Fact]
    public async Task CreateAsync_NonExistentOriginalInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_NonExistentOriginalInvoice_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest(
            PurchaseInvoiceId: 999,
            SupplierId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, Quantity: 1m, UnitPrice: 50m, DiscountAmount: 0m, Notes: null)
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الفاتورة الأصلية غير موجودة");

        _output.WriteLine("[PASS] Non-existent original invoice returns failure");
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_InsufficientStock_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var supplier = Supplier.Create("Test Supplier", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 5m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        // Setup validation to fail
        _mockInventoryService.Setup(i => i.ValidateStockAsync(1, 1, 10m, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("الكمية المتوفرة أقل من الكمية المطلوبة"));

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest(
            PurchaseInvoiceId: null,
            SupplierId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, Quantity: 10m, UnitPrice: 50m, DiscountAmount: 0m, Notes: null)
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الكمية المتوفرة أقل من الكمية المطلوبة");

        _output.WriteLine("[PASS] Insufficient stock returns failure");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingReturn_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingReturn_ReturnsDto");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var supplier = Supplier.Create("Test Supplier", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var returnEntity = PurchaseReturn.Create(
            "PR-2026-000001",
            warehouseId: 1,
            supplierId: 1,
            purchaseInvoiceId: null,
            returnDate: DateTime.Now,
            notes: "Test return",
            userId: 1
        );
        returnEntity.AddItem(productId: 1, quantity: 1m, unitCost: 50m, discountAmount: 0m, notes: null);
        _dbContext.PurchaseReturns.Add(returnEntity);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(returnEntity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReturnNo.Should().Be("PR-2026-000001");

        _output.WriteLine("[PASS] GetByIdAsync returns return dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentReturn_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentReturn_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجود");

        _output.WriteLine("[PASS] Non-existent return returns NotFound");
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

        public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
        public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
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