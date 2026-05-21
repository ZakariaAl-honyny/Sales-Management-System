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
/// Unit tests for CustomerService business logic.
/// </summary>
public class CustomerServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
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
        _mockLogger = new Mock<ILogger<CustomerService>>();

        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new CustomerService(_mockUow.Object, _mockLogger.Object);
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

        var customer = Customer.Create("Test Customer", 0m, "C001", "1234567890", null, null, null);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(customer.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Customer");
        result.Value.Code.Should().Be("C001");

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

        var request = new SalesSystem.Contracts.Requests.CreateCustomerRequest
        {
            Name = "New Customer",
            Code = "NC001",
            Phone = "1234567890",
            Email = "test@test.com",
            Address = "Test Address",
            OpeningBalance = 1000m
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Customer");
        result.Value.OpeningBalance.Should().Be(1000m);
        result.Value.CurrentBalance.Should().Be(1000m);

        _output.WriteLine("[PASS] CreateAsync creates customer correctly");
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateCode_ReturnsFailure");

        var existing = Customer.Create("Existing Customer", 0m, "C001", null, null, null, null);
        _dbContext.Customers.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateCustomerRequest
        {
            Name = "New Customer",
            Code = "C001", // Duplicate
            OpeningBalance = 0m
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("كود العميل مستخدم بالفعل");

        _output.WriteLine("[PASS] Duplicate code returns failure");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesCustomer()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesCustomer");

        var customer = Customer.Create("Original Name", 0m, "C001", "1234567890", null, null, null);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest
        {
            Name = "Updated Name",
            Phone = "0987654321",
            Email = "updated@test.com",
            Address = "New Address",
            Code = null,
            IsActive = true
        };

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

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest
        {
            Name = "Updated Name",
            IsActive = true
        };

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("العميل غير موجود");

        _output.WriteLine("[PASS] Update non-existent customer returns NotFound");
    }

    [Fact]
    public async Task UpdateAsync_DeactivateCustomer_MarksAsDeleted()
    {
        _output.WriteLine("[TEST] UpdateAsync_DeactivateCustomer_MarksAsDeleted");

        var customer = Customer.Create("Test Customer", 0m, "C001", null, null, null, null);
        customer.Activate();
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateCustomerRequest
        {
            Name = "Test Customer",
            IsActive = false // Deactivate
        };

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

        var customer = Customer.Create("Test Customer", 0m, "C001", null, null, null, null);
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

        var customer1 = Customer.Create("Ahmed", 0m, "C001", null, null, null, null);
        var customer2 = Customer.Create("Mohamed", 0m, "C002", null, null, null, null);
        _dbContext.Customers.Add(customer1);
        _dbContext.Customers.Add(customer2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("Ahmed", 1, 10, CancellationToken.None);

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
            var customer = Customer.Create($"Customer {i}", 0m, $"C{i:D3}", null, null, null, null);
            _dbContext.Customers.Add(customer);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, 2, 10, CancellationToken.None);

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

        var request = new SalesSystem.Contracts.Requests.CreateCustomerRequest
        {
            Name = "New Customer",
            OpeningBalance = 500m // Customer owes us 500
        };

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