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
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace SalesSystem.Application.Tests.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: PaymentServiceTests relied on old Customer.Create/Supplier.Create
//  signatures (with accountId/openingBalance/CurrentBalance params) and
//  PaymentService constructor changed (now requires IAccountingIntegrationService
//  and ICashBoxService). Customer/Supplier no longer have CurrentBalance.
//  Preserved for reference — NOT included in build.
// ═══════════════════════════════════════════════════════════════════════════
#if false
public class PaymentServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IDocumentSequenceService> _mockSequenceService;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly Mock<IAccountingIntegrationService> _mockAccountingService = new();

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
        _mockUow.Setup(u => u.SupplierPayments).Returns(new InMemoryEfCoreRepository<SupplierPayment>(_dbContext));
        _mockUow.Setup(u => u.WarehouseStocks).Returns(new InMemoryEfCoreRepository<WarehouseStock>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result<SupplierPaymentDto>>(
            It.IsAny<Func<Task<Result<SupplierPaymentDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SupplierPaymentDto>>> func, CancellationToken ct) => func());

        _mockUow.Setup(u => u.ExecuteTransactionAsync<Result>(
            It.IsAny<Func<Task<Result>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result>> func, CancellationToken ct) => func());

        _mockSequenceService.Setup(s => s.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("CP-2026-000001"));

        _mockAccountingService.Setup(a => a.CreateSupplierPaymentEntryAsync(
            It.IsAny<SupplierPayment>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));
        _mockAccountingService.Setup(a => a.ReverseSupplierPaymentEntryAsync(
            It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _sut = new PaymentService(
            _mockUow.Object,
            _mockSequenceService.Object,
            _mockLogger.Object,
            _mockAccountingService.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateSupplierPaymentAsync Tests

    [Fact]
    public async Task CreateSupplierPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_ValidRequest_CreatesPaymentAndDecreasesBalance");

        var supplier = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 1000m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, "Payment made");

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4000m, "We owed supplier 5000, paid 1000, now owe 4000");

        _output.WriteLine("[PASS] Supplier payment creates payment and decreases balance");
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_ZeroAmount_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_ZeroAmount_ReturnsFailure");

        var supplier = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 0m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("أكبر من صفر");

        _output.WriteLine("[PASS] Zero amount returns failure");
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_NonExistentSupplier_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_NonExistentSupplier_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(999, 1000m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, null);

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المورد غير موجود");

        _output.WriteLine("[PASS] Non-existent supplier returns NotFound");
    }

    #endregion

    #region WarehouseStock Not Affected (T012)

    [Fact]
    public async Task CreateSupplierPaymentAsync_DoesNotAffectWarehouseStock()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentAsync_DoesNotAffectWarehouseStock");

        var supplier = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 1000m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, "Payment made");

        var result = await _sut.CreateSupplierPaymentAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4000m);

        var stockRecords = await _dbContext.WarehouseStocks.ToListAsync();
        stockRecords.Should().BeEmpty("Supplier payments should never create or modify warehouse stock records");

        _output.WriteLine("[PASS] Supplier payment does not affect warehouse stock");
    }

    #endregion

    #region GetSupplierPaymentsAsync Tests

    [Fact]
    public async Task GetSupplierPaymentsAsync_WithFilter_ReturnsFilteredResults()
    {
        _output.WriteLine("[TEST] GetSupplierPaymentsAsync_WithFilter_ReturnsFilteredResults");

        var supplier1 = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 0m);
        var supplier2 = Supplier.Create(partyId: 2, accountId: 1, openingBalance: 0m);
        _dbContext.Suppliers.Add(supplier1);
        _dbContext.Suppliers.Add(supplier2);
        await _dbContext.SaveChangesAsync();

        var payment1 = SupplierPayment.Create("SP-2026-000001", supplierId: 1, amount: 500m, paymentMethod: SalesSystem.Domain.Enums.PaymentMethod.Cash, purchaseInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
        var payment2 = SupplierPayment.Create("SP-2026-000002", supplierId: 2, amount: 600m, paymentMethod: SalesSystem.Domain.Enums.PaymentMethod.Cash, purchaseInvoiceId: null, referenceNo: null, notes: null, createdByUserId: 1, paymentDate: DateTime.Now);
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

    #region Update and Delete Payment Tests (T012)

    [Fact]
    public async Task UpdateSupplierPaymentAsync_ReversesOldAndAppliesNewBalance()
    {
        _output.WriteLine("[TEST] UpdateSupplierPaymentAsync_ReversesOldAndAppliesNewBalance");

        var supplier = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        // Create initial payment of 1000
        var createRequest = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 1000m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, "Initial payment");
        var createResult = await _sut.CreateSupplierPaymentAsync(createRequest, userId: 1, CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4000m, "After payment of 1000, balance should decrease from 5000 to 4000");

        // Update payment amount from 1000 to 500
        var updateRequest = new SalesSystem.Contracts.Requests.UpdateSupplierPaymentRequest(1, 500m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, "Updated payment");
        var updateResult = await _sut.UpdateSupplierPaymentAsync(createResult.Value!.Id, updateRequest, userId: 1, CancellationToken.None);

        updateResult.IsSuccess.Should().BeTrue();
        // Old amount (1000) reversed: balance 4000 -> 5000, then new amount (500) applied: 5000 -> 4500
        supplier.CurrentBalance.Should().Be(4500m, "Old 1000 reversed then new 500 deducted => 5000 - 500 = 4500");

        _output.WriteLine("[PASS] Update supplier payment reverses old and applies new balance");
    }

    [Fact]
    public async Task DeleteSupplierPaymentAsync_ReversesBalance()
    {
        _output.WriteLine("[TEST] DeleteSupplierPaymentAsync_ReversesBalance");

        var supplier = Supplier.Create(partyId: 1, accountId: 1, openingBalance: 5000m);
        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        // Create payment of 1000
        var createRequest = new SalesSystem.Contracts.Requests.CreateSupplierPaymentRequest(1, 1000m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, null, "Payment to delete");
        var createResult = await _sut.CreateSupplierPaymentAsync(createRequest, userId: 1, CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(4000m, "After payment, balance (what we owe) should be 4000");

        // Delete the payment — balance should be restored
        var deleteResult = await _sut.DeleteSupplierPaymentAsync(createResult.Value!.Id, userId: 1, CancellationToken.None);

        deleteResult.IsSuccess.Should().BeTrue();
        supplier.CurrentBalance.Should().Be(5000m, "After deletion, balance should be restored to original 5000");

        _output.WriteLine("[PASS] Delete supplier payment reverses balance");
    }

    [Fact]
    public async Task UpdateSupplierPaymentAsync_NonExistent_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateSupplierPaymentAsync_NonExistent_ReturnsNotFound");

        var updateRequest = new SalesSystem.Contracts.Requests.UpdateSupplierPaymentRequest(1, 500m, SalesSystem.Contracts.Enums.PaymentMethod.Cash, DateTime.Now, "Updated");
        var result = await _sut.UpdateSupplierPaymentAsync(999, updateRequest, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجودة");

        _output.WriteLine("[PASS] Non-existent supplier payment update returns NotFound");
    }

    [Fact]
    public async Task DeleteSupplierPaymentAsync_NonExistent_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] DeleteSupplierPaymentAsync_NonExistent_ReturnsNotFound");

        var result = await _sut.DeleteSupplierPaymentAsync(999, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجودة");

        _output.WriteLine("[PASS] Non-existent supplier payment delete returns NotFound");
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
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
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
            if (entity != null && entity is ActivatableEntity activatable)
            {
                activatable.MarkAsDeleted();
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
#endif