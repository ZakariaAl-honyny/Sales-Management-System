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

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("PR-2026-000001"));

        _mockInventoryService.Setup(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
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

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var supplier = Supplier.Create("Test Supplier", 0m, "S001", null, null, null, null, null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest
        {
            WarehouseId = 1,
            SupplierId = 1,
            PurchaseInvoiceId = null,
            ReturnDate = DateTime.Now,
            Notes = "Return for defective item",
            Items = new List<SalesSystem.Contracts.Requests.PurchaseReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 2m, UnitPrice = 50m, UnitCost = 50m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(100m, "2 * 50 = 100");

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

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var supplier = Supplier.Create("Test Supplier", openingBalance: 5000m, code: "S001", phone: null, email: null, address: null, createdByUserId: null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest
        {
            WarehouseId = 1,
            SupplierId = 1,
            PurchaseInvoiceId = null,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.PurchaseReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 5m, UnitPrice = 100m, UnitCost = 100m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4500m, "We owed supplier 5000, return worth 500, now owe 4500");

        _output.WriteLine("[PASS] Purchase return decreases supplier balance");
    }

    [Fact]
    public async Task CreateAsync_WithOriginalInvoice_ValidatesQuantities()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOriginalInvoice_ValidatesQuantities");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var supplier = Supplier.Create("Test Supplier", 0m, "S001", null, null, null, null, null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create(
            "PUR-2026-000001",
            warehouseId: 1,
            supplierId: 1,
            DateTime.Now,
            DateTime.Now.AddDays(30),
            DomainPaymentType.Cash,
            discountAmount: 0m,
            notes: null
        );
        invoice.AddItem(PurchaseInvoiceItem.Create(productId: 1, quantity: 5m, unitCost: 50m));
        invoice.SetPaidAmount(250m);
        invoice.Post();
        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest
        {
            WarehouseId = 1,
            SupplierId = 1,
            PurchaseInvoiceId = 1,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.PurchaseReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 10m, UnitPrice = 50m, UnitCost = 50m, DiscountAmount = 0m, Notes = null } // Exceeds original
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من الكمية المشتراة");

        _output.WriteLine("[PASS] Return quantity exceeding original invoice fails");
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

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest
        {
            WarehouseId = 1,
            SupplierId = 1,
            PurchaseInvoiceId = 999, // Non-existent
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.PurchaseReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 1m, UnitPrice = 50m, UnitCost = 50m, DiscountAmount = 0m, Notes = null }
            }
        };

        var result = await _sut.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الفاتورة الأصلية غير موجودة");

        _output.WriteLine("[PASS] Non-existent original invoice returns failure");
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_InsufficientStock_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var supplier = Supplier.Create("Test Supplier", 0m, "S001", null, null, null, null, null);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Suppliers.Add(supplier);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 5m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        // Setup validation to fail
        _mockInventoryService.Setup(i => i.ValidateStockAsync(1, 1, 10m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("الكمية المتوفرة أقل من الكمية المطلوبة"));

        var request = new SalesSystem.Contracts.Requests.CreatePurchaseReturnRequest
        {
            WarehouseId = 1,
            SupplierId = 1,
            PurchaseInvoiceId = null,
            ReturnDate = DateTime.Now,
            Notes = null,
            Items = new List<SalesSystem.Contracts.Requests.PurchaseReturnItemRequest>
            {
                new() { ProductId = 1, Quantity = 10m, UnitPrice = 50m, UnitCost = 50m, DiscountAmount = 0m, Notes = null }
            }
        };

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

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var supplier = Supplier.Create("Test Supplier", 0m, "S001", null, null, null, null, null);
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
            createdByUserId: 1
        );
        returnEntity.AddItem(productId: 1, quantity: 1m, unitPrice: 50m, discountAmount: 0m, notes: null);
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