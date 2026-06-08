using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

using MovementType = SalesSystem.Domain.Enums.MovementType;
using InvoiceStatus = SalesSystem.Domain.Enums.InvoiceStatus;
using DomainPaymentType = SalesSystem.Domain.Enums.PaymentType;

/// <summary>
/// Unit tests for SalesService business logic.
/// Uses InMemory database to test multi-table operations.
/// </summary>
public class SalesServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<ICashBoxService> _cashBoxServiceMock;
    private readonly Mock<ILogger<SalesService>> _mockLogger;
    private readonly Mock<IPrintDataService> _mockPrintDataService = new();
    private readonly Mock<IPrintService> _mockPrintService = new();
    private readonly Mock<IAccountingIntegrationService> _mockAccountingService = new();
    private readonly Mock<IDocumentSequenceService> _mockDocumentSequenceService = new();
    private int _saveChangesCallCount = 0;

    private readonly SalesService _sut;

    public SalesServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] SalesServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockInventoryService = new Mock<IInventoryService>();
        _cashBoxServiceMock = new Mock<ICashBoxService>();
        _mockLogger = new Mock<ILogger<SalesService>>();

        _mockUow.Setup(u => u.SalesInvoices).Returns(new InMemoryEfCoreRepository<SalesInvoice>(_dbContext));
        _mockUow.Setup(u => u.Customers).Returns(new InMemoryEfCoreRepository<Customer>(_dbContext));
        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.Warehouses).Returns(new InMemoryEfCoreRepository<Warehouse>(_dbContext));

        var storeSettingsMock = new Mock<IGenericRepository<StoreSettings>>();
        storeSettingsMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<StoreSettings, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings)null!);
        _mockUow.Setup(u => u.StoreSettings).Returns(storeSettingsMock.Object);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _saveChangesCallCount++;
                _output.WriteLine($"[DEBUG] SaveChanges called (count: {_saveChangesCallCount})");
            })
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockDbContextTransaction());

        _mockInventoryService.Setup(i => i.ValidateStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockInventoryService.Setup(i => i.IncreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow.Setup(u => u.ExecuteAsync<Result<SalesInvoiceDto>>(
            It.IsAny<Func<Task<Result<SalesInvoiceDto>>>>(),
            It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<SalesInvoiceDto>>> func, CancellationToken ct) => func());

        // Setup accounting service to return success by default
        _mockAccountingService.Setup(a => a.CreateSalesPostEntryAsync(
            It.IsAny<SalesInvoice>(),
            It.IsAny<int>(),
            It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _mockAccountingService.Setup(a => a.ReverseSalesPostEntryAsync(
            It.IsAny<SalesInvoice>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _mockDocumentSequenceService.Setup(s => s.GetNextIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _sut = new SalesService(
            _mockUow.Object,
            _mockInventoryService.Object,
            _cashBoxServiceMock.Object,
            _mockPrintDataService.Object,
            _mockPrintService.Object,
            _mockAccountingService.Object,
            _mockDocumentSequenceService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region Stock Validation Tests

    [Fact]
    public async Task GivenInsufficientStock_WhenPosting_ThenReturnsFailureBeforeTransaction()
    {
        _output.WriteLine("[TEST] GivenInsufficientStock_WhenPosting_ThenReturnsFailureBeforeTransaction");

        // Setup warehouse and product for navigation property fixup
        var warehouse = Warehouse.Create("Main Warehouse", isDefault: true);
        var product = Product.Create("Test Product", 10m, 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Create invoice
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 100m, unitPrice: 50m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(5000m);

        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        // Setup stock validation to FAIL
        _mockInventoryService.Setup(i => i.ValidateStockAsync(1, 1, 100m, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المخزون غير كافٍ"));

        _output.WriteLine("[STEP 1] Calling PostAsync with insufficient stock...");
        var result = await _sut.PostAsync(invoice.Id, userId: 1, CancellationToken.None);

        _output.WriteLine($"[STEP 2] Verifying result is failure...");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المخزون غير كافٍ");

        // CRITICAL: Transaction should NOT have started
        _output.WriteLine($"[STEP 3] Verifying NO transaction started...");
        _mockUow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never,
            "Transaction should not start when stock validation fails");

        _output.WriteLine("[PASS] Stock validated BEFORE transaction starts");
    }

    [Fact]
    public async Task GivenNonExistentInvoice_WhenPosting_ThenReturnsNotFound()
    {
        _output.WriteLine("[TEST] GivenNonExistentInvoice_WhenPosting_ThenReturnsNotFound");

        var result = await _sut.PostAsync(999, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("غير موجودة");

        _output.WriteLine("[PASS] Non-existent invoice returns NotFound");
    }

    [Fact]
    public async Task GivenAlreadyPostedInvoice_WhenPosting_ThenReturnsFailure()
    {
        _output.WriteLine("[TEST] GivenAlreadyPostedInvoice_WhenPosting_ThenReturnsFailure");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 10m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(50m);
        invoice.Post(); // Already posted

        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.PostAsync(invoice.Id, 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("مسودة");

        _output.WriteLine("[PASS] Already posted invoice cannot be posted again");
    }

    #endregion

    #region Invoice Status Tests

    [Fact]
    public async Task GivenDraftInvoice_WhenCancelling_ThenNoStockOrBalanceChanges()
    {
        _output.WriteLine("[TEST] GivenDraftInvoice_WhenCancelling_ThenNoStockOrBalanceChanges");

        var warehouse = Warehouse.Create(name: "Main Warehouse", isDefault: true);
        var product = Product.Create(name: "Test Product", purchasePrice: 10m, retailPrice: 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Draft invoice can be cancelled (no stock/balance was affected yet)
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: null, paymentType: DomainPaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 10m, unitPrice: 100m));
        invoice.RecalculateTotals();
        // Status = Draft

        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, 1, CancellationToken.None);

        if (!result.IsSuccess)
            _output.WriteLine($"[DEBUG] Cancel FAILED: {result.Error}");
        result.IsSuccess.Should().BeTrue("Draft invoice can be cancelled. Error: {0}", result.Error ?? "null");
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);

        // NO stock operations for Draft invoices
        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never,
            "No stock restoration for Draft invoice (nothing was posted)");

        _mockInventoryService.Verify(i => i.DecreaseStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(),
            It.IsAny<MovementType>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never,
            "No stock decrease for Draft invoice cancellation");

        _output.WriteLine("[PASS] Draft invoice cancellation has no stock/balance impact");
    }

    [Fact]
    public async Task GivenPostedInvoice_WhenCancelling_ThenStockAndBalanceReversed()
    {
        _output.WriteLine("[TEST] GivenPostedInvoice_WhenCancelling_ThenStockAndBalanceReversed");

        // Setup warehouse and product first (same as Draft test)
        var warehouse = Warehouse.Create(name: "Main Warehouse", isDefault: true);
        var product = Product.Create(name: "Test Product", purchasePrice: 10m, retailPrice: 100m);
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Credit invoice with no payment can be cancelled and stock is reversed
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1, paymentType: DomainPaymentType.Credit);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 2m, unitPrice: 50m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(0m); // Unpaid
        invoice.Post(); // Status = Posted

        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CancelAsync(invoice.Id, 1, CancellationToken.None);

        if (!result.IsSuccess)
            _output.WriteLine($"[DEBUG] Cancel FAILED: {result.Error}");
        result.IsSuccess.Should().BeTrue("Posted unpaid invoice can be cancelled. Error: {0}", result.Error ?? "null");
        invoice.Status.Should().Be(InvoiceStatus.Cancelled, "Invoice status should be Cancelled after cancellation");

        // Stock should be restored (SaleReturnIn)
        _mockInventoryService.Verify(i => i.IncreaseStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 2m,
            movementType: MovementType.SaleReturnIn,
            referenceType: "SalesInvoiceCancel",
            referenceId: invoice.Id,
            It.IsAny<decimal?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once,
            "Stock should be restored for SaleReturnIn");

        _output.WriteLine("[PASS] Posted invoice cancellation reverses stock");
    }

    #endregion

    #region Financial Calculation Tests

    [Fact]
    public void GivenInvoiceWithItems_WhenRecalculating_ThenTotalsCorrect()
    {
        _output.WriteLine("[TEST] GivenInvoiceWithItems_WhenRecalculating_ThenTotalsCorrect");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1, discountAmount: 50m);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 10m, unitPrice: 100m, discountAmount: 10m)); // 990
        invoice.AddItem(SalesInvoiceItem.Create(productId: 2, quantity: 5m, unitPrice: 50m, discountAmount: 0m));   // 250
        invoice.RecalculateTotals();

        _output.WriteLine($"[DEBUG] SubTotal={invoice.SubTotal}, InvoiceDiscount={invoice.DiscountAmount}");

        invoice.SubTotal.Should().Be(1240m, "SubTotal = 990 + 250");
        invoice.DiscountAmount.Should().Be(50m);
        invoice.TotalAmount.Should().Be(1190m, "TotalAmount = 1240 - 50 = 1190");

        _output.WriteLine("[PASS] Invoice totals calculated correctly");
    }

    [Fact]
    public void GivenPartialPayment_WhenSettingPaidAmount_ThenDueAmountCorrect()
    {
        _output.WriteLine("[TEST] GivenPartialPayment_WhenSettingPaidAmount_ThenDueAmountCorrect");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 10m, unitPrice: 100m));
        invoice.RecalculateTotals();

        invoice.SetPaidAmount(300m); // Partial payment

        invoice.PaidAmount.Should().Be(300m);
        invoice.DueAmount.Should().Be(700m, "DueAmount = TotalAmount - PaidAmount = 1000 - 300");

        _output.WriteLine("[PASS] Partial payment sets DueAmount correctly");
    }

    [Fact]
    public void GivenPaidAmountExceedsTotal_WhenSetting_ThenThrowsDomainException()
    {
        _output.WriteLine("[TEST] GivenPaidAmountExceedsTotal_WhenSetting_ThenThrowsDomainException");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 10m, unitPrice: 100m));
        invoice.RecalculateTotals();
        // TotalAmount = 1000

        var action = () => invoice.SetPaidAmount(1500m); // Exceeds total

        action.Should().Throw<Domain.Exceptions.DomainException>()
            .WithMessage("*المبلغ المدفوع أكبر من الإجمالي*");

        _output.WriteLine("[PASS] SetPaidAmount throws when exceeding total");
    }

    [Fact]
    public void GivenNegativeTaxAmount_WhenSetting_ThenThrowsDomainException()
    {
        _output.WriteLine("[TEST] GivenNegativeTaxAmount_WhenSetting_ThenThrowsDomainException");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 10m, unitPrice: 100m));

        var action = () => invoice.SetTaxAmount(-10m);

        action.Should().Throw<Domain.Exceptions.DomainException>();

        _output.WriteLine("[PASS] SetTaxAmount rejects negative values");
    }

    [Fact]
    public void GivenLineItemDiscount_WhenCalculatingLineTotal_ThenCorrect()
    {
        _output.WriteLine("[TEST] GivenLineItemDiscount_WhenCalculatingLineTotal_ThenCorrect");

        // LineTotal = (Quantity * UnitPrice) - DiscountAmount
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 5m,
            unitPrice: 100m,
            discountAmount: 50m
        );

        item.LineTotal.Should().Be(450m, "LineTotal = (5 * 100) - 50 = 450");

        _output.WriteLine("[PASS] LineItem LineTotal calculated correctly");
    }

    [Fact]
    public void GivenMultipleItems_WhenAddingToInvoice_ThenSubTotalCorrect()
    {
        _output.WriteLine("[TEST] GivenMultipleItems_WhenAddingToInvoice_ThenSubTotalCorrect");

        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);

        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 2m, unitPrice: 100m, discountAmount: 0m)); // 200
        invoice.AddItem(SalesInvoiceItem.Create(productId: 2, quantity: 3m, unitPrice: 50m, discountAmount: 10m));  // 140
        invoice.AddItem(SalesInvoiceItem.Create(productId: 3, quantity: 1m, unitPrice: 200m, discountAmount: 0m));  // 200

        invoice.SubTotal.Should().Be(540m, "SubTotal = 200 + 140 + 200");

        _output.WriteLine("[PASS] Multiple items calculate SubTotal correctly");
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

        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
        public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
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

