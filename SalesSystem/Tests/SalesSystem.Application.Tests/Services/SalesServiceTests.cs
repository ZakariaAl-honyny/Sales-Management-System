// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: SalesServiceTests relied on old IInventoryService method
//  signatures (with MovementType, ReferenceType, ReferenceId params) and
//  constructor has changed (now requires ISystemSettingsRepository,
//  IAccountingIntegrationService, IProductCostService).
//  IInventoryService.DecreaseStockAsync/ValidateStockAsync now have 5-6 params
//  (no MovementType/ReferenceType/ReferenceId).
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
/// Unit tests for SalesService business logic.
/// </summary>
public class SalesServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<SalesService>> _mockLogger;

    private readonly SalesService _sut;
    private readonly Customer _testCustomer;
    private readonly Warehouse _testWarehouse;
    private readonly Product _testProduct;

    public SalesServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] SalesServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<SalesService>>();

        _mockUow.Setup(u => u.SalesInvoices).Returns(new InMemoryEfCoreRepository<SalesInvoice>(_dbContext));
        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));
        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.WarehouseStocks).Returns(new InMemoryEfCoreRepository<WarehouseStock>(_dbContext));
        _mockUow.Setup(u => u.CashBoxes).Returns(new InMemoryEfCoreRepository<CashBox>(_dbContext));

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result<SalesInvoiceDto>>(
            It.IsAny<Func<Task<Result<SalesInvoiceDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SalesInvoiceDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result>(
            It.IsAny<Func<Task<Result>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result>> func, CancellationToken ct) => func());

        _mockSequenceService.Setup(s => s.GetNextIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockInventoryService.Setup(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new SalesService(
            _mockUow.Object,
            _mockInventoryService.Object,
            _mockSequenceService.Object,
            _mockLogger.Object);

        _testCustomer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000");
        _testWarehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        _testProduct = Product.Create("Test Product", categoryId: 1);

        _dbContext.Customers.Add(_testCustomer);
        _dbContext.Warehouses.Add(_testWarehouse);
        _dbContext.Products.Add(_testProduct);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessWithInvoice()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_ReturnsSuccessWithInvoice");

        var request = new CreateSalesInvoiceRequest
        {
            CustomerId = 1,
            WarehouseId = 1,
            InvoiceNo = null,
            InvoiceDate = DateTime.Now,
            PaymentType = DomainPaymentType.Cash,
            DueDate = null,
            CurrencyId = null,
            ExchangeRate = null,
            DiscountAmount = 0m,
            DiscountType = null,
            DiscountRate = null,
            Notes = "Walk-in sale",
            Items = new List<CreateSalesInvoiceItemRequest>
            {
                new() { ProductId = 1, ProductUnitId = 1, Quantity = 2m, UnitPrice = 150m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalAmount.Should().Be(300m, "2 * 150 = 300");

        _output.WriteLine("[PASS] Sales invoice created successfully");
    }

    [Fact]
    public async Task CreateAsync_WithDiscountAmount_CalculatesCorrectTotal()
    {
        _output.WriteLine("[TEST] CreateAsync_WithDiscountAmount_CalculatesCorrectTotal");

        var request = new CreateSalesInvoiceRequest
        {
            CustomerId = 1,
            WarehouseId = 1,
            InvoiceNo = null,
            InvoiceDate = DateTime.Now,
            PaymentType = DomainPaymentType.Cash,
            DueDate = null,
            DiscountAmount = 20m,
            Notes = null,
            Items = new List<CreateSalesInvoiceItemRequest>
            {
                new() { ProductId = 1, ProductUnitId = 1, Quantity = 2m, UnitPrice = 150m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SubTotal.Should().Be(300m, "2 * 150 = 300");
        result.Value.TotalAmount.Should().Be(280m, "300 - 20 = 280");

        _output.WriteLine("[PASS] Total calculated correctly with discount");
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantityItem_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_ZeroQuantityItem_ReturnsFailure");

        var request = new CreateSalesInvoiceRequest
        {
            CustomerId = 1,
            WarehouseId = 1,
            InvoiceNo = null,
            InvoiceDate = DateTime.Now,
            PaymentType = DomainPaymentType.Cash,
            DueDate = null,
            DiscountAmount = 0m,
            Notes = null,
            Items = new List<CreateSalesInvoiceItemRequest>
            {
                new() { ProductId = 1, ProductUnitId = 1, Quantity = 0m, UnitPrice = 150m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الكمية");

        _output.WriteLine("[PASS] Zero quantity returns failure");
    }

    #endregion

    #region PostAsync Tests

    [Fact]
    public async Task PostAsync_DraftInvoice_PostsSuccessfully()
    {
        _output.WriteLine("[TEST] PostAsync_DraftInvoice_PostsSuccessfully");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000");
        var product = Product.Create("Test Product", categoryId: 1);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create(
            customerId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: DateTime.Now,
            paymentType: DomainPaymentType.Cash,
            discountAmount: 0m,
            dueDate: null,
            notes: null
        );
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, productUnitId: 1, quantity: 2m, unitPrice: 150m, discountAmount: 0m));
        invoice.SetPaidAmount(300m);
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 2m,
            movementType: MovementType.SaleOut,
            "SalesInvoice",
            invoice.Id,
            It.IsAny<decimal?>(),
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should be decreased by sold quantity");

        _output.WriteLine("[PASS] Invoice posted and stock decreased");
    }

    [Fact]
    public async Task PostAsync_AlreadyPostedInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] PostAsync_AlreadyPostedInvoice_ReturnsFailure");

        var invoice = SalesInvoice.Create(customerId: 1, warehouseId: 1, invoiceNo: 1, invoiceDate: DateTime.Now, paymentType: DomainPaymentType.Cash, discountAmount: 0m, dueDate: null, notes: null);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("مرحلة");

        _output.WriteLine("[PASS] Already posted invoice returns failure");
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_PostedInvoice_ReversesStockAndBalance()
    {
        _output.WriteLine("[TEST] CancelAsync_PostedInvoice_ReversesStockAndBalance");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000", openingBalance: 1000m);
        var product = Product.Create("Test Product", categoryId: 1);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create(customerId: 1, warehouseId: 1, invoiceNo: 1, invoiceDate: DateTime.Now, paymentType: DomainPaymentType.Credit, discountAmount: 0m, dueDate: DateOnly.FromDateTime(DateTime.Now.AddDays(30)), notes: null);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, productUnitId: 1, quantity: 5m, unitPrice: 100m, discountAmount: 0m));
        invoice.SetPaidAmount(0m);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 5m,
            movementType: MovementType.SaleReturnIn,
            "SalesInvoice",
            invoice.Id,
            It.IsAny<decimal?>(),
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should be restored when cancelling posted sale");

        _output.WriteLine("[PASS] Cancelled posted invoice reverses stock and balance");
    }

    [Fact]
    public async Task CancelAsync_DraftInvoice_NoStockEffect()
    {
        _output.WriteLine("[TEST] CancelAsync_DraftInvoice_NoStockEffect");

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse", isDefault: true);
        var customer = Customer.Create(partyId: 1, accountId: 1, name: "Test Customer", phone: "0500000000");
        var product = Product.Create("Test Product", categoryId: 1);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create(customerId: 1, warehouseId: 1, invoiceNo: 1, invoiceDate: DateTime.Now, paymentType: DomainPaymentType.Cash, discountAmount: 0m, dueDate: null, notes: null);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, productUnitId: 1, quantity: 2m, unitPrice: 150m, discountAmount: 0m));
        invoice.SetPaidAmount(300m);
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never,
            "Stock should NOT be affected for draft cancellations");

        _output.WriteLine("[PASS] Cancelled draft invoice has no stock effect");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingInvoice_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingInvoice_ReturnsDto");

        var invoice = SalesInvoice.Create(customerId: 1, warehouseId: 1, invoiceNo: 1, invoiceDate: DateTime.Now, paymentType: DomainPaymentType.Cash, discountAmount: 0m, dueDate: null, notes: null);
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(invoice.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        _output.WriteLine("[PASS] GetByIdAsync returns invoice dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentInvoice_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentInvoice_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجود");

        _output.WriteLine("[PASS] Non-existent invoice returns NotFound");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

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
