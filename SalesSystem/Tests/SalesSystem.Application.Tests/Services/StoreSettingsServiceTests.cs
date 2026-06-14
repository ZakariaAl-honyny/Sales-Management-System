// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: StoreSettingsServiceTests relied on the StoreSettings entity which
//  was REMOVED in the 65-table schema migration. Settings are now stored as
//  key-value pairs via SystemSetting entity.
//  The service (StoreSettingsService) still exists but uses ISystemSettingsRepository
//  with SystemSetting key-value pattern internally.
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
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for StoreSettingsService business logic.
/// </summary>
public class StoreSettingsServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ISystemSettingsRepository> _mockSystemSettingsRepo;
    private readonly Mock<ILogger<StoreSettingsService>> _mockLogger;

    private readonly StoreSettingsService _sut;

    public StoreSettingsServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] StoreSettingsServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSystemSettingsRepo = new Mock<ISystemSettingsRepository>();
        _mockLogger = new Mock<ILogger<StoreSettingsService>>();

        _mockUow.Setup(u => u.StoreSettings).Returns(new InMemoryEfCoreRepository<StoreSettings>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });
        _mockUow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (operation, ct) =>
            {
                await operation();
            });

        _sut = new StoreSettingsService(_mockUow.Object, _mockSystemSettingsRepo.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_NoSettings_CreatesDefault()
    {
        _output.WriteLine("[TEST] GetSettingsAsync_NoSettings_CreatesDefault");

        // No settings in database

        var result = await _sut.GetSettingsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StoreName.Should().Be("متجر المبيعات");
        result.Value.CurrencyCode.Should().Be("SAR");

        // Verify default settings were saved to database
        var savedSettings = await _dbContext.StoreSettings.FirstOrDefaultAsync();
        savedSettings.Should().NotBeNull();

        _output.WriteLine("[PASS] GetSettings creates default settings when none exist");
    }

    [Fact]
    public async Task GetSettingsAsync_ExistingSettings_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetSettingsAsync_ExistingSettings_ReturnsDto");

        var settings = StoreSettings.Create("My Store", "USD");
        settings.Update("Updated Store", "123456789", "Store Address", null, null, "EUR", 0.15m, true, null, true, false, false, "INV");
        _dbContext.StoreSettings.Add(settings);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetSettingsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StoreName.Should().Be("Updated Store");
        result.Value.Phone.Should().Be("123456789");
        result.Value.CurrencyCode.Should().Be("EUR");
        result.Value.DefaultTaxRate.Should().Be(0.15m);
        result.Value.IsTaxEnabled.Should().BeTrue();

        _output.WriteLine("[PASS] GetSettings returns existing settings");
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_NoExistingSettings_CreatesNew()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_NoExistingSettings_CreatesNew");

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest(
            "New Store Name", "New Address", "1234567890", null, null, "USD", 0.10m, true, null, true, false, false, "INV");

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StoreName.Should().Be("New Store Name");
        result.Value.CurrencyCode.Should().Be("USD");

        // Verify it was saved to database
        var savedSettings = await _dbContext.StoreSettings.FirstOrDefaultAsync();
        savedSettings.Should().NotBeNull();
        savedSettings!.StoreName.Should().Be("New Store Name");

        _output.WriteLine("[PASS] UpdateSettings creates new settings when none exist");
    }

    [Fact]
    public async Task UpdateSettingsAsync_ExistingSettings_Updates()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_ExistingSettings_Updates");

        var existing = StoreSettings.Create("Original Store", "SAR");
        _dbContext.StoreSettings.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest(
            "Updated Store", "New Address", "0987654321", null, "/path/to/logo.png", "EUR", 0.15m, true, null, true, false, false, "INV");

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StoreName.Should().Be("Updated Store");
        result.Value.Phone.Should().Be("0987654321");
        result.Value.CurrencyCode.Should().Be("EUR");
        // DEPRECATED: DefaultTaxRate hardcoded as 0m in service — Tax entity is source of truth
        result.Value.DefaultTaxRate.Should().Be(0M);
        result.Value.LogoPath.Should().Be("/path/to/logo.png");

        _output.WriteLine("[PASS] UpdateSettings updates existing settings");
    }

    [Fact]
    public async Task UpdateSettingsAsync_TaxEnabled_CreatesSettingsWithTax()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_TaxEnabled_CreatesSettingsWithTax");

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest(
            "Store With Tax", null, null, null, null, "SAR", 0.15m, true, null, true, false, false, "INV");

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsTaxEnabled.Should().BeTrue();
        // DEPRECATED: DefaultTaxRate hardcoded as 0m in service — Tax entity is source of truth
        result.Value.DefaultTaxRate.Should().Be(0M);

        _output.WriteLine("[PASS] UpdateSettings correctly handles tax settings");
    }

    [Fact]
    public async Task UpdateSettingsAsync_TaxDisabled_CreatesSettingsWithoutTax()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_TaxDisabled_CreatesSettingsWithoutTax");

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest(
            "Store Without Tax", null, null, null, null, "SAR", 0m, false, null, true, false, false, "INV");

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // DEPRECATED: IsTaxEnabled hardcoded as true in service — Tax entity is source of truth
        result.Value!.IsTaxEnabled.Should().BeTrue();

        _output.WriteLine("[PASS] UpdateSettings correctly handles disabled tax (IsTaxEnabled hardcoded true as Tax entity is source of truth)");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : Entity
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
#endif
