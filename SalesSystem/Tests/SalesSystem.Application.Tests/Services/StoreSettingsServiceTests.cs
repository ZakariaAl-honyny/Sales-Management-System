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
/// Unit tests for StoreSettingsService business logic.
/// </summary>
public class StoreSettingsServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
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
        _mockLogger = new Mock<ILogger<StoreSettingsService>>();

        _mockUow.Setup(u => u.StoreSettings).Returns(new InMemoryEfCoreRepository<StoreSettings>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new StoreSettingsService(_mockUow.Object, _mockLogger.Object);
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
        settings.Update("Updated Store", "123456789", "Store Address", null, "EUR", 0.15m, true);
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

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest
        {
            StoreName = "New Store Name",
            Phone = "1234567890",
            Address = "New Address",
            LogoUrl = null,
            Currency = "USD",
            DefaultTaxRate = 0.10m,
            IsTaxEnabled = true
        };

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

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest
        {
            StoreName = "Updated Store",
            Phone = "0987654321",
            Address = "New Address",
            LogoUrl = "/path/to/logo.png",
            Currency = "EUR",
            DefaultTaxRate = 0.15m,
            IsTaxEnabled = true
        };

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StoreName.Should().Be("Updated Store");
        result.Value.Phone.Should().Be("0987654321");
        result.Value.CurrencyCode.Should().Be("EUR");
        result.Value.DefaultTaxRate.Should().Be(0.15m);
        result.Value.LogoPath.Should().Be("/path/to/logo.png");

        _output.WriteLine("[PASS] UpdateSettings updates existing settings");
    }

    [Fact]
    public async Task UpdateSettingsAsync_TaxEnabled_CreatesSettingsWithTax()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_TaxEnabled_CreatesSettingsWithTax");

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest
        {
            StoreName = "Store With Tax",
            Currency = "SAR",
            DefaultTaxRate = 0.15m,
            IsTaxEnabled = true
        };

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsTaxEnabled.Should().BeTrue();
        result.Value.DefaultTaxRate.Should().Be(0.15m);

        _output.WriteLine("[PASS] UpdateSettings correctly handles tax settings");
    }

    [Fact]
    public async Task UpdateSettingsAsync_TaxDisabled_CreatesSettingsWithoutTax()
    {
        _output.WriteLine("[TEST] UpdateSettingsAsync_TaxDisabled_CreatesSettingsWithoutTax");

        var request = new SalesSystem.Contracts.Requests.UpdateSettingsRequest
        {
            StoreName = "Store Without Tax",
            Currency = "SAR",
            DefaultTaxRate = 0m,
            IsTaxEnabled = false
        };

        var result = await _sut.UpdateSettingsAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsTaxEnabled.Should().BeFalse();

        _output.WriteLine("[PASS] UpdateSettings correctly handles disabled tax");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
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