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
/// Unit tests for UnitService business logic.
/// </summary>
public class UnitServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<UnitService>> _mockLogger;

    private readonly UnitService _sut;

    public UnitServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] UnitServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UnitService>>();

        _mockUow.Setup(u => u.Units).Returns(new InMemoryEfCoreRepository<Unit>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new UnitService(_mockUow.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUnit_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingUnit_ReturnsDto");

        var unit = Unit.Create("Kilogram", "kg", null);
        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(unit.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Kilogram");
        result.Value.Symbol.Should().Be("kg");

        _output.WriteLine("[PASS] GetByIdAsync returns unit dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUnit_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentUnit_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الوحدة غير موجودة");

        _output.WriteLine("[PASS] Non-existent unit returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesUnit()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesUnit");

        var request = new SalesSystem.Contracts.Requests.CreateUnitRequest("Piece", "pcs");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Piece");
        result.Value.Symbol.Should().Be("pcs");

        _output.WriteLine("[PASS] CreateAsync creates unit correctly");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateName_ReturnsFailure");

        var existing = Unit.Create("Kilogram", "kg", null);
        _dbContext.Units.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateUnitRequest("Kilogram", null); // Duplicate

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("اسم الوحدة مستخدم بالفعل");

        _output.WriteLine("[PASS] Duplicate name returns failure");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesUnit()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesUnit");

        var unit = Unit.Create("Kilo", "kg", null);
        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateUnitRequest("Kilogram", "KG", true);

        var result = await _sut.UpdateAsync(unit.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Kilogram");
        result.Value.Symbol.Should().Be("KG");

        _output.WriteLine("[PASS] UpdateAsync updates unit correctly");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentUnit_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateAsync_NonExistentUnit_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.UpdateUnitRequest("Updated", null, true);

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الوحدة غير موجودة");

        _output.WriteLine("[PASS] Update non-existent unit returns NotFound");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_UnitWithProducts_ReturnsFailure()
    {
        _output.WriteLine("[TEST] DeleteAsync_UnitWithProducts_ReturnsFailure");

        var unit = Unit.Create("Kilogram", "kg", null);
        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Product", 10m, 100m, barcode: "P001", retailUnitId: unit.Id);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(unit.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يمكن حذف الوحدة لأنها مرتبطة بمنتجات");

        _output.WriteLine("[PASS] Cannot delete unit with linked products");
    }

    [Fact]
    public async Task DeleteAsync_UnitWithoutProducts_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_UnitWithoutProducts_SoftDeletes");

        var unit = Unit.Create("Unlinked Unit", "U", null);
        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(unit.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] Unit without products can be deleted");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithSearch_FiltersResults");

        var unit1 = Unit.Create("Kilogram", "kg", null);
        var unit2 = Unit.Create("Meter", "m", null);
        _dbContext.Units.Add(unit1);
        _dbContext.Units.Add(unit2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("kg", 1, 10, false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Kilogram");

        _output.WriteLine("[PASS] Search filters units correctly");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Unit> Units => Set<Unit>();
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