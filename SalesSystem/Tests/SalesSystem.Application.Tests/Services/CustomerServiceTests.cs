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
/// Unit tests for CustomerService business logic.
/// </summary>
public class CustomerServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<CustomerService>> _mockLogger;

    private readonly CustomerService _sut;

    public CustomerServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] CustomerServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<CustomerService>>();

        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new CustomerService(_mockUow.Object, _mockSequenceService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingCustomer_ReturnsDto");

        var customer = Customer.Create("Test Customer", 0m, "1234567890");
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(customer.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Customer");

        _output.WriteLine("[PASS] GetByIdAsync returns customer dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCustomer_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentCustomer_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("العميل غير موجود");

        _output.WriteLine("[PASS] Non-existent customer returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCustomer()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesCustomer");

        var request = new SalesSystem.Contracts.Requests.CreateCustomerRequest("New Customer", "1234567890", "test@test.com", "Test Address", null, 1000m);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Customer");
        result.Value.OpeningBalance.Should().Be(1000m);
        result.Value.CurrentBalance.Should().Be(1000m);

        _output.WriteLine("[PASS] CreateAsync creates customer correctly");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesCustomer()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesCustomer");

        var customer = Customer.Create("Original Name", 0m, "1234567890");
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest("Updated Name", "0987654321", "updated@test.com", "New Address", null, 0, true);

        var result = await _sut.UpdateAsync(customer.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Phone.Should().Be("0987654321");

        _output.WriteLine("[PASS] UpdateAsync updates customer correctly");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentCustomer_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateAsync_NonExistentCustomer_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest("Updated Name", null, null, null, null, 0, true);

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("العميل غير موجود");

        _output.WriteLine("[PASS] Update non-existent customer returns NotFound");
    }

    [Fact]
    public async Task UpdateAsync_DeactivateCustomer_MarksAsDeleted()
    {
        _output.WriteLine("[TEST] UpdateAsync_DeactivateCustomer_MarksAsDeleted");

        var customer = Customer.Create("Test Customer", 0m);
        customer.Restore();
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest("Test Customer", null, null, null, null, 0, false); // Deactivate

        var result = await _sut.UpdateAsync(customer.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();

        _output.WriteLine("[PASS] Deactivating customer works correctly");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingCustomer_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_ExistingCustomer_SoftDeletes");

        var customer = Customer.Create("Test Customer", 0m);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(customer.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Verify soft delete
        var deleted = await _dbContext.Customers.FindAsync(customer.Id);
        deleted.Should().NotBeNull();

        _output.WriteLine("[PASS] DeleteAsync soft deletes customer");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentCustomer_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] DeleteAsync_NonExistentCustomer_ReturnsNotFound");

        var result = await _sut.DeleteAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("العميل غير موجود");

        _output.WriteLine("[PASS] Delete non-existent customer returns NotFound");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithSearch_FiltersResults");

        var customer1 = Customer.Create("Ahmed", 0m);
        var customer2 = Customer.Create("Mohamed", 0m);
        _dbContext.Customers.Add(customer1);
        _dbContext.Customers.Add(customer2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("Ahmed", 1, 10, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Ahmed");

        _output.WriteLine("[PASS] Search filters customers correctly");
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        _output.WriteLine("[TEST] GetAllAsync_Pagination_ReturnsCorrectPage");

        for (int i = 1; i <= 15; i++)
        {
            var customer = Customer.Create($"Customer {i}", 0m);
            _dbContext.Customers.Add(customer);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, 2, 10, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(15);

        _output.WriteLine("[PASS] Pagination works correctly");
    }

    #endregion

    #region Balance Direction Tests

    [Fact]
    public async Task CreateAsync_WithOpeningBalance_SetsCorrectBalance()
    {
        _output.WriteLine("[TEST] CreateAsync_WithOpeningBalance_SetsCorrectBalance");

        var request = new SalesSystem.Contracts.Requests.CreateCustomerRequest("New Customer", null, null, null, null, 500m); // Customer owes us 500

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OpeningBalance.Should().Be(500m);
        result.Value.CurrentBalance.Should().Be(500m, "Opening balance becomes current balance");

        _output.WriteLine("[PASS] Opening balance sets correctly");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
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