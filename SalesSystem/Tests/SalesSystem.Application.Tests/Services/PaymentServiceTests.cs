using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for PaymentService business logic.
/// </summary>
public class PaymentServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;

    private readonly PaymentService _sut;

    public PaymentServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] PaymentServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSequenceService = new Mock<IDocumentSequenceService>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));
        _mockUow.Setup(u => u.Suppliers).Returns(new InMemoryEfCoreRepository<Supplier>(_dbContext));
        _mockUow.Setup(u => u.CustomerPayments).Returns(new InMemoryEfCoreRepository<CustomerPayment>(_dbContext));
        _mockUow.Setup(u => u.SupplierPayments).Returns(new InMemoryEfCoreRepository<SupplierPayment>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockUow.Setup(u => u.ExecuteAsync<Result<CustomerPaymentDto>>(
            It.IsAny<Func<Task<Result<CustomerPaymentDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<CustomerPaymentDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteAsync<Result<SupplierPaymentDto>>(
            It.IsAny<Func<Task<Result<SupplierPaymentDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SupplierPaymentDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteAsync<Result>(
            It.IsAny<Func<Task<Result>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result>> func, CancellationToken ct) => func());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("CP-2026-000001"));

        _sut = new PaymentService(
            _mockUow.Object,
            _mockSequenceService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateCustomerPaymentAsync Tests

    [Fact]
    public async Task CreateCustomerPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateCustomerPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance");

        var customer = Customer.Create("Test Customer", openingBalance: 1000m);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateCustomerPaymentRequest(1, 500m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, "Payment received");

        var result = await _sut.CreateCustomerPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.CurrentBalance.Should().Be(500m, "Customer owed 1000, paid 500, now owes 500");

        _output.WriteLine("[PASS] Customer payment creates payment and decreases balance");
    }

    [Fact]
    public async Task CreateCustomerPaymentAsync_ZeroAmount_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateCustomerPaymentAsync_ZeroAmount_ReturnsFailure");

        var customer = Customer.Create("Test Customer", openingBalance: 1000m);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateCustomerPaymentRequest(1, 0m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateCustomerPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من صفر");

        _output.WriteLine("[PASS] Zero amount returns failure");
    }

    [Fact]
    public async Task CreateCustomerPaymentAsync_NegativeAmount_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateCustomerPaymentAsync_NegativeAmount_ReturnsFailure");

        var customer = Customer.Create("Test Customer", openingBalance: 1000m);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateCustomerPaymentRequest(1, -100m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateCustomerPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من صفر");

        _output.WriteLine("[PASS] Negative amount returns failure");
    }

    [Fact]
    public async Task CreateCustomerPaymentAsync_NonExistentCustomer_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] CreateCustomerPaymentAsync_NonExistentCustomer_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.CreateCustomerPaymentRequest(999, 500m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateCustomerPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("العميل غير موجود");

        _output.WriteLine("[PASS] Non-existent customer returns NotFound");
    }

    #endregion

    #region CreateSupplierPaymentAsync Tests

    [Fact]
    public async Task CreateSupplierPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance");

        var supplier = Supplier.Create("Test Supplier", openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 1000m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, "Payment made");

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4000m, "We owed supplier 5000, paid 1000, now owe 4000");

        _output.WriteLine("[PASS] Supplier payment creates payment and decreases balance");
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_ZeroAmount_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_ZeroAmount_ReturnsFailure");

        var supplier = Supplier.Create("Test Supplier", openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 0m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من صفر");

        _output.WriteLine("[PASS] Zero amount returns failure");
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_NonExistentSupplier_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_NonExistentSupplier_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(999, 1000m, SalesSystem.Contracts.Enums.PaymentType.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المورد غير موجود");

        _output.WriteLine("[PASS] Non-existent supplier returns NotFound");
    }

    #endregion

    #region GetCustomerPaymentsAsync Tests

    [Fact]
    public async Task GetCustomerPaymentsAsync_WithFilter_ReturnsFilteredResults()
    {
        _output.WriteLine("[TEST] GetCustomerPaymentsAsync_WithFilter_ReturnsFilteredResults");

        var customer1 = Customer.Create("Customer 1", openingBalance: 0m);
        var customer2 = Customer.Create("Customer 2", openingBalance: 0m);
        _dbContext.Customers.Add(customer1);
        _dbContext.Customers.Add(customer2);
        await _dbContext.SaveChangesAsync();

        var payment1 = CustomerPayment.Create("CP-2026-000001", customerId: 1, amount: 100m, paymentMethod: (byte)SalesSystem.Contracts.Enums.PaymentType.Cash, salesInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
        var payment2 = CustomerPayment.Create("CP-2026-000002", customerId: 2, amount: 200m, paymentMethod: (byte)SalesSystem.Contracts.Enums.PaymentType.Cash, salesInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
        _dbContext.CustomerPayments.Add(payment1);
        _dbContext.CustomerPayments.Add(payment2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetCustomerPaymentsAsync("Customer 1", null, null, 1, 10, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Amount.Should().Be(100m);

        _output.WriteLine("[PASS] Customer payments filtered correctly");
    }

    #endregion

    #region GetSupplierPaymentsAsync Tests

    [Fact]
    public async Task GetSupplierPaymentsAsync_WithFilter_ReturnsFilteredResults()
    {
        _output.WriteLine("[TEST] GetSupplierPaymentsAsync_WithFilter_ReturnsFilteredResults");

        var supplier1 = Supplier.Create("Supplier 1", openingBalance: 0m);
        var supplier2 = Supplier.Create("Supplier 2", openingBalance: 0m);
        _dbContext.Suppliers.Add(supplier1);
        _dbContext.Suppliers.Add(supplier2);
        await _dbContext.SaveChangesAsync();

        var payment1 = SupplierPayment.Create("SP-2026-000001", supplierId: 1, amount: 500m, paymentMethod: (byte)SalesSystem.Contracts.Enums.PaymentType.Cash, purchaseInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
        var payment2 = SupplierPayment.Create("SP-2026-000002", supplierId: 2, amount: 600m, paymentMethod: (byte)SalesSystem.Contracts.Enums.PaymentType.Cash, purchaseInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
        _dbContext.SupplierPayments.Add(payment1);
        _dbContext.SupplierPayments.Add(payment2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetSupplierPaymentsAsync("Supplier 2", null, null, 1, 10, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Amount.Should().Be(600m);

        _output.WriteLine("[PASS] Supplier payments filtered correctly");
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

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
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