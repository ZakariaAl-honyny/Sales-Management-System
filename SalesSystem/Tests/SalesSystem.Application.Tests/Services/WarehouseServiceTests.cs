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

/// <summary>
/// Unit tests for WarehouseService business logic.
/// </summary>
public class WarehouseServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<WarehouseService>> _mockLogger;

    private readonly WarehouseService _sut;

    public WarehouseServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] WarehouseServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WarehouseService>>();

        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _sut = new WarehouseService(_mockUow.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingWarehouse_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingWarehouse_ReturnsDto");

        var warehouse = Warehouse.Create("Main Warehouse", true);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(warehouse.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Main Warehouse");
        result.Value.IsDefault.Should().BeTrue();

        _output.WriteLine("[PASS] GetByIdAsync returns warehouse dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentWarehouse_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentWarehouse_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المخزن غير موجود");

        _output.WriteLine("[PASS] Non-existent warehouse returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesWarehouse()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesWarehouse");

        var request = new SalesSystem.Contracts.Requests.CreateWarehouseRequest
        {
            Name = "New Warehouse",
            Code = "W001",
            Location = "Building A",
            IsDefault = false
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Warehouse");
        result.Value.Code.Should().Be("W001");

        _output.WriteLine("[PASS] CreateAsync creates warehouse correctly");
    }

    [Fact]
    public async Task CreateAsync_IsDefault_UnsetsOtherDefaults()
    {
        _output.WriteLine("[TEST] CreateAsync_IsDefault_UnsetsOtherDefaults");

        var existing = Warehouse.Create("Existing Warehouse", true);
        _dbContext.Warehouses.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateWarehouseRequest
        {
            Name = "New Default Warehouse",
            IsDefault = true // Set as new default
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();

        // Reload to check other warehouse
        var oldWarehouse = await _dbContext.Warehouses.FindAsync(existing.Id);
        oldWarehouse!.IsDefault.Should().BeFalse("Old default should be unset");

        _output.WriteLine("[PASS] Creating new default warehouse unsets old defaults");
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateCode_ReturnsFailure");

        var existing = Warehouse.Create("Existing Warehouse", false, code: "W001");
        _dbContext.Warehouses.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateWarehouseRequest
        {
            Name = "New Warehouse",
            Code = "W001" // Duplicate
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("كود المخزن مستخدم بالفعل");

        _output.WriteLine("[PASS] Duplicate code returns failure");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesWarehouse()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesWarehouse");

        var warehouse = Warehouse.Create("Original Warehouse", false, code: "W001");
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateWarehouseRequest
        {
            Name = "Updated Warehouse",
            Code = null,
            Location = "New Location",
            IsDefault = false,
            IsActive = true
        };

        var result = await _sut.UpdateAsync(warehouse.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Warehouse");
        result.Value.Location.Should().Be("New Location");

        _output.WriteLine("[PASS] UpdateAsync updates warehouse correctly");
    }

    [Fact]
    public async Task UpdateAsync_SetAsDefault_UnsetsOtherDefaults()
    {
        _output.WriteLine("[TEST] UpdateAsync_SetAsDefault_UnsetsOtherDefaults");

        var warehouse1 = Warehouse.Create("Warehouse 1", true);
        var warehouse2 = Warehouse.Create("Warehouse 2", false);
        _dbContext.Warehouses.Add(warehouse1);
        _dbContext.Warehouses.Add(warehouse2);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateWarehouseRequest
        {
            Name = "Warehouse 2",
            IsDefault = true // Set as new default
        };

        var result = await _sut.UpdateAsync(warehouse2.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();

        // Reload old warehouse
        var oldWarehouse = await _dbContext.Warehouses.FindAsync(warehouse1.Id);
        oldWarehouse!.IsDefault.Should().BeFalse("Old default should be unset");

        _output.WriteLine("[PASS] Updating warehouse to default unsets old defaults");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DefaultWarehouse_ReturnsFailure()
    {
        _output.WriteLine("[TEST] DeleteAsync_DefaultWarehouse_ReturnsFailure");

        var warehouse = Warehouse.Create("Default Warehouse", true);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(warehouse.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يمكن حذف المخزن الافتراضي");

        _output.WriteLine("[PASS] Cannot delete default warehouse");
    }

    [Fact]
    public async Task DeleteAsync_NonDefaultWarehouse_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_NonDefaultWarehouse_SoftDeletes");

        var warehouse = Warehouse.Create("Test Warehouse", false);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(warehouse.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] Non-default warehouse can be deleted");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithSearch_FiltersResults");

        var warehouse1 = Warehouse.Create("Main Store", false);
        var warehouse2 = Warehouse.Create("Backup Store", false);
        _dbContext.Warehouses.Add(warehouse1);
        _dbContext.Warehouses.Add(warehouse2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("Main", 1, 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Contain("Main");

        _output.WriteLine("[PASS] Search filters warehouses correctly");
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

        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
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