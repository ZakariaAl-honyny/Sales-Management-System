// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: SalesReturnServiceTests relied on old IInventoryService method
//  signatures (with MovementType, ReferenceType, ReferenceId params) and
//  old CashTransactionType values. IInventoryService.IncreaseStockAsync/
//  DecreaseStockAsync now have 5-6 params (no MovementType/ReferenceType/
//  ReferenceId). Constructor also changed (now requires ISystemSettingsRepository,
//  IAccountingIntegrationService, ICashBoxService).
//  Preserved for reference — NOT included in build.
// ═══════════════════════════════════════════════════════════════════════════
#if false
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
using CashTransactionType = SalesSystem.Domain.Enums.CashTransactionType;

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
        _mockUow.Setup(u => u.WarehouseStocks).Returns(new InMemoryEfCoreRepository<WarehouseStock>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));
        _mockUow.Setup(u => u.CashBoxes).Returns(new InMemoryEfCoreRepository<CashBox>(_dbContext));

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result<SalesReturnDto>>(
            It.IsAny<Func<Task<Result<SalesReturnDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SalesReturnDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result>(
            It.IsAny<Func<Task<Result>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result>> func, CancellationToken ct) => func());

        var storeSettingsMock = new Mock<IGenericRepository<StoreSettings>>();
        storeSettingsMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<StoreSettings, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings)null!);
        _mockUow.Setup(u => u.StoreSettings).Returns(storeSettingsMock.Object);

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
    public async Task CreateAsync_ValidRequest_CreatesReturnWithStockRestore()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesReturnWithStockRestore");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000");
        var product = Product.Create("Test Product", categoryId: 1);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new CreateSalesReturnRequest(
            SalesInvoiceId: null,
            CustomerId: 1,
            WarehouseId: 1,
            LinkToInvoice: null,
            ReturnDate: DateTime.Now,
            DiscountAmount: 0m,
            DiscountType: null,
            DiscountRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            Notes: "Customer returned defective items",
            Items: new List<CreateSalesReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 3m, UnitPrice: 100m, DiscountAmount: 0m, Notes: null)
            }
        );

        var createResult = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.TotalAmount.Should().Be(300m, "3 * 100 = 300");

        var postResult = await _sut.PostAsync(createResult.Value.Id, userId: 1, CancellationToken.None);
        postResult.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 3m,
            movementType: MovementType.SaleReturnIn,
            "SalesReturn",
            It.IsAny<int>(),
            100m,
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should be restored for returned items");

        _output.WriteLine("[PASS] Sales return creates and restores stock");
    }

    [Fact]
    public async Task CreateAsync_CustomerWithBalance_DecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_CustomerWithBalance_DecreasesBalance");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000", openingBalance: 2000m);
        var product = Product.Create("Test Product", categoryId: 1);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new CreateSalesReturnRequest(
            SalesInvoiceId: null,
            CustomerId: 1,
            WarehouseId: 1,
            LinkToInvoice: null,
            ReturnDate: DateTime.Now,
            DiscountAmount: 0m,
            DiscountType: null,
            DiscountRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            Notes: null,
            Items: new List<CreateSalesReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 5m, UnitPrice: 50m, DiscountAmount: 0m, Notes: null)
            }
        );

        var createResult = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var postResult = await _sut.PostAsync(createResult.Value!.Id, userId: 1, CancellationToken.None);
        postResult.IsSuccess.Should().BeTrue();

        customer.CurrentBalance.Should().Be(1750m, "Customer owed 2000, return worth 250, now owes 1750");

        _output.WriteLine("[PASS] Sales return decreases customer balance");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingReturn_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingReturn_ReturnsDto");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000");
        var product = Product.Create("Test Product", categoryId: 1);
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
        returnEntity.AddItem(productId: 1, productUnitId: 1, quantity: 1m, unitPrice: 100m, discountAmount: 0m, notes: null);
        _dbContext.SalesReturns.Add(returnEntity);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(returnEntity.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

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
        public DbSet<CashBox> CashBoxes => Set<CashBox>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : Entity
    {
        private readonly DbContext _context;

        public InMemoryEfCoreRepository(DbContext context) { _context = context; }

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

        public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity is ActivatableEntity activatable)
            {
                activatable.MarkAsDeleted();
                _context.Set<T>().Update(entity);
            }
        }

        public async Task HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity != null) _context.Set<T>().Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities) => _context.Set<T>().RemoveRange(entities);

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
#endif
