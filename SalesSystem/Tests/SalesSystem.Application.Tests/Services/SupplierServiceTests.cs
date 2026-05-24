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
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for SupplierService business logic.
/// </summary>
public class SupplierServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<SupplierService>> _mockLogger;

    private readonly SupplierService _sut;

    public SupplierServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] SupplierServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<SupplierService>>();

        _mockUow.Setup(u => u.Suppliers).Returns(new InMemoryEfCoreRepository<Supplier>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new SupplierService(_mockUow.Object, _mockSequenceService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingSupplier_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingSupplier_ReturnsDto");

        var supplier = Supplier.Create("Test Supplier", 0m, "1234567890");
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(supplier.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Supplier");

        _output.WriteLine("[PASS] GetByIdAsync returns supplier dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentSupplier_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentSupplier_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المورد غير موجود");

        _output.WriteLine("[PASS] Non-existent supplier returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesSupplier()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesSupplier");

        var request = new SalesSystem.Contracts.Requests.CreateSupplierRequest("New Supplier", "1234567890", "test@supplier.com", "Test Address", null, 2000m);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Supplier");
        result.Value.OpeningBalance.Should().Be(2000m);
        result.Value.CurrentBalance.Should().Be(2000m);

        _output.WriteLine("[PASS] CreateAsync creates supplier correctly");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesSupplier()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesSupplier");

        var supplier = Supplier.Create("Original Supplier", 0m, "1234567890");
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateSupplierRequest("Updated Supplier", "0987654321", "updated@supplier.com", "New Address", null, 0, true);

        var result = await _sut.UpdateAsync(supplier.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Supplier");
        result.Value.Phone.Should().Be("0987654321");

        _output.WriteLine("[PASS] UpdateAsync updates supplier correctly");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentSupplier_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateAsync_NonExistentSupplier_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.UpdateSupplierRequest("Updated Supplier", null, null, null, null, 0, true);

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المورد غير موجود");

        _output.WriteLine("[PASS] Update non-existent supplier returns NotFound");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingSupplier_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_ExistingSupplier_SoftDeletes");

        var supplier = Supplier.Create("Test Supplier", 0m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(supplier.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] DeleteAsync soft deletes supplier");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSupplier_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] DeleteAsync_NonExistentSupplier_ReturnsNotFound");

        var result = await _sut.DeleteAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المورد غير موجود");

        _output.WriteLine("[PASS] Delete non-existent supplier returns NotFound");
    }

    #endregion

    #region Balance Direction Tests

    [Fact]
    public async Task CreateAsync_WithOpeningBalance_SetsCorrectBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOpeningBalance_SetsCorrectBalance");

        var request = new SalesSystem.Contracts.Requests.CreateSupplierRequest("New Supplier", null, null, null, null, 3000m); // We owe them 3000

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OpeningBalance.Should().Be(3000m);
        result.Value.CurrentBalance.Should().Be(3000m, "We owe the supplier 3000");

        _output.WriteLine("[PASS] Opening balance for supplier sets correctly");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithSearch_FiltersResults");

        var supplier1 = Supplier.Create("Ahmed & Co");
        var supplier2 = Supplier.Create("Mohamed Trading");
        _dbContext.Suppliers.Add(supplier1);
        _dbContext.Suppliers.Add(supplier2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("Ahmed", 1, 10, false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Contain("Ahmed");

        _output.WriteLine("[PASS] Search filters suppliers correctly");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Supplier> Suppliers => Set<Supplier>();
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