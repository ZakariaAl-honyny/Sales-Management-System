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

using MovementType = SalesSystem.Domain.Enums.MovementType;

/// <summary>
/// Unit tests for InventoryService business logic.
/// </summary>
public class InventoryServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<InventoryService>> _mockLogger;

    private readonly InventoryService _sut;

    public InventoryServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] InventoryServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<InventoryService>>();

        _mockUow.Setup(u => u.WarehouseStocks).Returns(new InMemoryEfCoreRepository<WarehouseStock>(_dbContext));
        _mockUow.Setup(u => u.InventoryMovements).Returns(new InMemoryEfCoreRepository<InventoryMovement>(_dbContext));
        _mockUow.Setup(u => u.StockTransfers).Returns(new InMemoryEfCoreRepository<StockTransfer>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("TRF-2026-000001"));

        _sut = new InventoryService(
            _mockUow.Object,
            _mockSequenceService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetStockAsync Tests

    [Fact]
    public async Task GetStockAsync_ExistingStock_ReturnsQuantity()
    {
        _output.WriteLine("[TEST] GetStockAsync_ExistingStock_ReturnsQuantity");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 50m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetStockAsync(productId: 1, warehouseId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50m);

        _output.WriteLine("[PASS] GetStockAsync returns correct quantity");
    }

    [Fact]
    public async Task GetStockAsync_NonExistentStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetStockAsync_NonExistentStock_ReturnsFailure");

        var result = await _sut.GetStockAsync(productId: 999, warehouseId: 999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يوجد سجل مخزون");

        _output.WriteLine("[PASS] Non-existent stock returns failure");
    }

    #endregion

    #region ValidateStockAsync Tests

    [Fact]
    public async Task ValidateStockAsync_SufficientStock_ReturnsSuccess()
    {
        _output.WriteLine("[TEST] ValidateStockAsync_SufficientStock_ReturnsSuccess");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 100m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.ValidateStockAsync(productId: 1, warehouseId: 1, requiredQty: 50m, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] Sufficient stock returns success");
    }

    [Fact]
    public async Task ValidateStockAsync_InsufficientStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] ValidateStockAsync_InsufficientStock_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 30m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.ValidateStockAsync(productId: 1, warehouseId: 1, requiredQty: 50m, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أقل من الكمية المطلوبة");

        _output.WriteLine("[PASS] Insufficient stock returns failure");
    }

    #endregion

    #region IncreaseStockAsync Tests

    [Fact]
    public async Task IncreaseStockAsync_ExistingStock_IncreasesCorrectly()
    {
        _output.WriteLine("[TEST] IncreaseStockAsync_ExistingStock_IncreasesCorrectly");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 100m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 50m,
            movementType: MovementType.PurchaseIn,
            referenceType: "PurchaseInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stock.Quantity.Should().Be(150m, "Original 100 + 50 = 150");

        // Verify movement was recorded
        var movement = await _dbContext.InventoryMovements.FirstOrDefaultAsync(m => m.ProductId == 1);
        movement.Should().NotBeNull();
        movement!.QuantityChange.Should().Be(50m);
        movement.MovementType.Should().Be(MovementType.PurchaseIn);

        _output.WriteLine("[PASS] IncreaseStockAsync increases stock correctly");
    }

    [Fact]
    public async Task IncreaseStockAsync_NewStock_CreatesStockRecord()
    {
        _output.WriteLine("[TEST] IncreaseStockAsync_NewStock_CreatesStockRecord");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // No stock record exists
        var result = await _sut.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 25m,
            movementType: MovementType.PurchaseIn,
            referenceType: "PurchaseInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stock = await _dbContext.WarehouseStocks.FirstOrDefaultAsync(s => s.ProductId == 1 && s.WarehouseId == 1);
        stock.Should().NotBeNull();
        stock!.Quantity.Should().Be(25m);

        _output.WriteLine("[PASS] IncreaseStockAsync creates new stock record");
    }

    #endregion

    #region DecreaseStockAsync Tests

    [Fact]
    public async Task DecreaseStockAsync_SufficientQuantity_DecreasesCorrectly()
    {
        _output.WriteLine("[TEST] DecreaseStockAsync_SufficientQuantity_DecreasesCorrectly");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 100m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 30m,
            movementType: MovementType.SaleOut,
            referenceType: "SalesInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stock.Quantity.Should().Be(70m, "Original 100 - 30 = 70");

        // Verify movement was recorded with negative quantity
        var movement = await _dbContext.InventoryMovements.FirstOrDefaultAsync(m => m.ProductId == 1);
        movement.Should().NotBeNull();
        movement!.QuantityChange.Should().Be(-30m);

        _output.WriteLine("[PASS] DecreaseStockAsync decreases stock correctly");
    }

    [Fact]
    public async Task DecreaseStockAsync_InsufficientQuantity_ThrowsDomainException()
    {
        _output.WriteLine("[TEST] DecreaseStockAsync_InsufficientQuantity_ThrowsDomainException");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 20m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 50m,
            movementType: MovementType.SaleOut,
            referenceType: "SalesInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المخزون غير كافٍ");

        _output.WriteLine("[PASS] Insufficient stock returns failure");
    }

    [Fact]
    public async Task DecreaseStockAsync_ZeroQuantity_ThrowsDomainException()
    {
        _output.WriteLine("[TEST] DecreaseStockAsync_ZeroQuantity_ThrowsDomainException");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 50m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DecreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 0m,
            movementType: MovementType.SaleOut,
            referenceType: "SalesInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        _output.WriteLine("[PASS] Zero quantity returns failure");
    }

    [Fact]
    public async Task DecreaseStockAsync_NonExistentStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] DecreaseStockAsync_NonExistentStock_ReturnsFailure");

        var result = await _sut.DecreaseStockAsync(
            productId: 999,
            warehouseId: 999,
            quantity: 10m,
            movementType: MovementType.SaleOut,
            referenceType: "SalesInvoice",
            referenceId: 1,
            unitCost: 100m,
            userId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يوجد سجل مخزون");

        _output.WriteLine("[PASS] Non-existent stock returns failure");
    }

    #endregion

    #region CreateTransferAsync Tests

    [Fact]
    public async Task CreateTransferAsync_ValidRequest_CreatesTransferAndMovesStock()
    {
        _output.WriteLine("[TEST] CreateTransferAsync_ValidRequest_CreatesTransferAndMovesStock");

        var fromWarehouse = Warehouse.Create("Warehouse A", true);
        var toWarehouse = Warehouse.Create("Warehouse B", false);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(fromWarehouse);
        _dbContext.Warehouses.Add(toWarehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 100m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateStockTransferRequest
        {
            FromWarehouseId = 1,
            ToWarehouseId = 2,
            Items = new List<SalesSystem.Contracts.Requests.StockTransferItemRequest>
            {
                new() { ProductId = 1, Quantity = 20m, Notes = null }
            },
            TransferDate = DateTime.Now,
            Notes = "Test transfer"
        };

        var result = await _sut.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Verify stock moved
        var fromStock = await _dbContext.WarehouseStocks.FirstOrDefaultAsync(s => s.ProductId == 1 && s.WarehouseId == 1);
        fromStock!.Quantity.Should().Be(80m, "100 - 20 = 80");

        var toStock = await _dbContext.WarehouseStocks.FirstOrDefaultAsync(s => s.ProductId == 1 && s.WarehouseId == 2);
        toStock.Should().NotBeNull();
        toStock!.Quantity.Should().Be(20m);

        _output.WriteLine("[PASS] CreateTransferAsync moves stock correctly");
    }

    [Fact]
    public async Task CreateTransferAsync_SameWarehouse_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateTransferAsync_SameWarehouse_ReturnsFailure");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateStockTransferRequest
        {
            FromWarehouseId = 1,
            ToWarehouseId = 1, // Same warehouse
            Items = new List<SalesSystem.Contracts.Requests.StockTransferItemRequest>
            {
                new() { ProductId = 1, Quantity = 10m, Notes = null }
            },
            TransferDate = DateTime.Now,
            Notes = null
        };

        var result = await _sut.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يمكن أن يكون المخزن المصدر والمخزن الوجهة متطابقين");

        _output.WriteLine("[PASS] Same warehouse transfer returns failure");
    }

    [Fact]
    public async Task CreateTransferAsync_EmptyItems_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateTransferAsync_EmptyItems_ReturnsFailure");

        var fromWarehouse = Warehouse.Create("Warehouse A", true);
        var toWarehouse = Warehouse.Create("Warehouse B", false);
        _dbContext.Warehouses.Add(fromWarehouse);
        _dbContext.Warehouses.Add(toWarehouse);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateStockTransferRequest
        {
            FromWarehouseId = 1,
            ToWarehouseId = 2,
            Items = new List<SalesSystem.Contracts.Requests.StockTransferItemRequest>(), // Empty
            TransferDate = DateTime.Now,
            Notes = null
        };

        var result = await _sut.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("عنصر واحد على الأقل");

        _output.WriteLine("[PASS] Empty items returns failure");
    }

    [Fact]
    public async Task CreateTransferAsync_InsufficientStock_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateTransferAsync_InsufficientStock_ReturnsFailure");

        var fromWarehouse = Warehouse.Create("Warehouse A", true);
        var toWarehouse = Warehouse.Create("Warehouse B", false);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(fromWarehouse);
        _dbContext.Warehouses.Add(toWarehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 10m);
        _dbContext.WarehouseStocks.Add(stock);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateStockTransferRequest
        {
            FromWarehouseId = 1,
            ToWarehouseId = 2,
            Items = new List<SalesSystem.Contracts.Requests.StockTransferItemRequest>
            {
                new() { ProductId = 1, Quantity = 50m, Notes = null } // More than available
            },
            TransferDate = DateTime.Now,
            Notes = null
        };

        var result = await _sut.CreateTransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أقل من الكمية المطلوبة");

        _output.WriteLine("[PASS] Insufficient stock for transfer returns failure");
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

        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
        public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
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
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }

    #endregion
}