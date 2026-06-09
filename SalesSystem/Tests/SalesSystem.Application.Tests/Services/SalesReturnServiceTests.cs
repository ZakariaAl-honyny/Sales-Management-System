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
/// Unit tests for SalesReturnService business logic.
/// </summary>
public class SalesReturnServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<SalesReturnService>> _mockLogger;

    private readonly SalesReturnService _sut;

    public SalesReturnServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] SalesReturnServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<SalesReturnService>>();

        _mockUow.Setup(u => u.SalesReturns).Returns(new InMemoryEfCoreRepository<SalesReturn>(_dbContext));
        _mockUow.Setup(u => u.SalesInvoices).Returns(new InMemoryEfCoreRepository<SalesInvoice>(_dbContext));
        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));
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

        _mockUow.Setup(u => u.ExecuteAsync<Result<SalesReturnDto>>(
            It.IsAny<Func<Task<Result<SalesReturnDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SalesReturnDto>>> func, CancellationToken ct) => func());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("SR-2026-000001"));

        _mockInventoryService.Setup(i => i.IncreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new SalesReturnService(
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
    public async Task CreateAsync_ValidRequest_CreatesReturnWithStockIncrease()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesReturnWithStockIncrease");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var customer = Customer.Create("Test Customer", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest(
            SalesInvoiceId: null,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: "Return for defective item",
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 2m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null)
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(200m, "2 * 100 = 200");

        result = await _sut.PostAsync(result.Value.Id, userId: 1, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 2m,
            movementType: MovementType.SaleReturnIn,
            "SalesReturn",
            result.Value!.Id,
            100m,
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should increase for returned items after posting");

        _output.WriteLine("[PASS] Sales return creates, posts, and increases stock");
    }

    [Fact]
    public async Task CreateAsync_WithOriginalInvoice_ValidatesQuantities()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOriginalInvoice_ValidatesQuantities");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var customer = Customer.Create("Test Customer", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 100m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(500m);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest(
            SalesInvoiceId: 1,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 10m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null) // Exceeds original quantity
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من الكمية المباعة");

        _output.WriteLine("[PASS] Return quantity exceeding original invoice fails");
    }

    [Fact]
    public async Task CreateAsync_CustomerWithBalance_DecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_CustomerWithBalance_DecreasesBalance");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var customer = Customer.Create("Test Customer", openingBalance: 1000m, phone: null, email: null, address: null, createdByUserId: null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest(
            SalesInvoiceId: null,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 2m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null)
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        result = await _sut.PostAsync(result.Value!.Id, userId: 1, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        customer.CurrentBalance.Should().Be(800m, "Customer owed 1000, return worth 200, now owes 800");

        _output.WriteLine("[PASS] Sales return posts and decreases customer balance");
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

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest(
            SalesInvoiceId: 999,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 1m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null)
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الفاتورة الأصلية غير موجودة");

        _output.WriteLine("[PASS] Non-existent original invoice returns failure");
    }

    [Fact]
    public async Task CreateAsync_ProductNotInOriginalInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_ProductNotInOriginalInvoice_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var product1 = Product.Create("Product 1", 10m, 100m);
        var product2 = Product.Create("Product 2", 20m, 200m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product1);
        _dbContext.Products.Add(product2);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: null, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 100m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(500m);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest(
            SalesInvoiceId: 1,
            CustomerId: null,
            WarehouseId: 1,
            ReturnDate: DateTime.Now,
            Notes: null,
            Items: new List<SalesSystem.Contracts.Requests.ReturnItemRequest>
            {
                new(ProductId: 2, ProductUnitId: 2, Quantity: 1m, UnitPrice: 200m, DiscountAmount: 0m, Notes: null) // Product 2 not in invoice
            }
        );

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجود في الفاتورة الأصلية");

        _output.WriteLine("[PASS] Product not in original invoice returns failure");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingReturn_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingReturn_ReturnsDto");

        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var customer = Customer.Create("Test Customer", 0m);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var returnEntity = SalesReturn.Create(
            "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            salesInvoiceId: null,
            returnDate: DateTime.Now,
            notes: "Test return",
            userId: 1
        );
        returnEntity.AddItem(productId: 1, quantity: 1m, unitPrice: 100m, discountAmount: 0m, notes: null);
        _dbContext.SalesReturns.Add(returnEntity);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(returnEntity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReturnNo.Should().Be("SR-2026-000001");

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

        public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
        public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
        public DbSet<Customer> Customers => Set<Customer>();
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
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task HardDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public void DeleteRange(IEnumerable<T> entities)
            => throw new NotImplementedException();

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
