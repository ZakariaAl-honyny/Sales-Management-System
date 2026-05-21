using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
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

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("SR-2026-000001"));

        _mockInventoryService.Setup(i => i.IncreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
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

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var customer = Customer.Create("Test Customer", 0m, "C001", null, null, null, null, null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest
        {
            WarehouseId = 1,
            CustomerId = 1,
            SalesInvoiceId = null,
            ReturnDate = DateTime.Now,
            Notes = "Return for defective item",
            Items = new List<SalesSystem.Contracts.Requests.SalesReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 2m, UnitPrice = 100m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(200m, "2 * 100 = 200");

        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 2m,
            movementType: MovementType.SaleReturnIn,
            "SalesReturn",
            It.IsAny<int>(),
            100m,
            1,
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should increase for returned items");

        _output.WriteLine("[PASS] Sales return creates and increases stock");
    }

    [Fact]
    public async Task CreateAsync_WithOriginalInvoice_ValidatesQuantities()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOriginalInvoice_ValidatesQuantities");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var customer = Customer.Create("Test Customer", 0m, "C001", null, null, null, null, null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create("INV-2026-000001", warehouseId: 1, customerId: 1, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 100m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(500m);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest
        {
            WarehouseId = 1,
            CustomerId = 1,
            SalesInvoiceId = 1,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.SalesReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 10m, UnitPrice = 100m, DiscountAmount = 0m, Notes = null } // Exceeds original quantity
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من الكمية المباعة");

        _output.WriteLine("[PASS] Return quantity exceeding original invoice fails");
    }

    [Fact]
    public async Task CreateAsync_CustomerWithBalance_DecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_CustomerWithBalance_DecreasesBalance");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var customer = Customer.Create("Test Customer", openingBalance: 1000m, code: "C001", phone: null, email: null, address: null, createdByUserId: null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Customers.Add(customer);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest
        {
            WarehouseId = 1,
            CustomerId = 1,
            SalesInvoiceId = null,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.SalesReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 2m, UnitPrice = 100m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.CurrentBalance.Should().Be(800m, "Customer owed 1000, return worth 200, now owes 800");

        _output.WriteLine("[PASS] Sales return decreases customer balance");
    }

    [Fact]
    public async Task CreateAsync_NonExistentOriginalInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_NonExistentOriginalInvoice_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest
        {
            WarehouseId = 1,
            CustomerId = 1,
            SalesInvoiceId = 999, // Non-existent
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.SalesReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 1m, UnitPrice = 100m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الفاتورة الأصلية غير موجودة");

        _output.WriteLine("[PASS] Non-existent original invoice returns failure");
    }

    [Fact]
    public async Task CreateAsync_ProductNotInOriginalInvoice_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_ProductNotInOriginalInvoice_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product1 = Product.Create("Product 1", 10m, 100m);
        var product2 = Product.Create("Product 2", 20m, 200m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product1);
        _dbContext.Products.Add(product2);
        await _dbContext.SaveChangesAsync();

        var invoice = SalesInvoice.Create("INV-2026-000001", warehouseId: 1, customerId: null, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 100m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(500m);
        invoice.Post();
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSalesReturnRequest
        {
            WarehouseId = 1,
            CustomerId = null,
            SalesInvoiceId = 1,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.SalesReturnItemRequest>
            {
                new() { ProductId = 2, Quantity = 1m, UnitPrice = 200m, DiscountAmount = 0m, Notes = null } // Product 2 not in invoice
            }
        };

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

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var customer = Customer.Create("Test Customer", 0m, "C001", null, null, null, null, null);
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
            createdByUserId: 1
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

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }

    #endregion
}