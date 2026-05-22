using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Tests.Services;

public class UpdateProductPricingServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ISystemSettingsRepository> _settingsMock;
    private readonly Mock<ILogger<UpdateProductPricingService>> _loggerMock;
    private readonly UpdateProductPricingService _sut;

    private readonly Product _product;
    private readonly ProductUnit _baseUnit;
    private readonly ProductUnit _derivedUnit;

    public UpdateProductPricingServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _settingsMock = new Mock<ISystemSettingsRepository>();
        _loggerMock = new Mock<ILogger<UpdateProductPricingService>>();
        _sut = new UpdateProductPricingService(_uowMock.Object, _settingsMock.Object, _loggerMock.Object);

        _product = Product.Create("منتج تجريبي",
            purchasePrice: 10,
            retailPrice: 25,
            createdByUserId: 1);

        _baseUnit = ProductUnit.CreateBaseUnit(
            productId: 1, unitName: "حبة", salesPrice: 25, purchaseCost: 10);

        _derivedUnit = ProductUnit.CreateDerivedUnit(
            productId: 1, unitName: "كرتونة", baseConversionFactor: 12,
            salesPrice: 300, purchaseCost: 120);

        SetId(_baseUnit, 1);
        SetId(_derivedUnit, 2);
        SetProductUnits(_product, [_baseUnit, _derivedUnit]);
        SetNavigation(_baseUnit, nameof(ProductUnit.Product), _product);
        SetNavigation(_derivedUnit, nameof(ProductUnit.Product), _product);
    }

    // ─── Reflection Helpers ─────────────────────────────────────

    private static void SetProductUnits(Product product, List<ProductUnit> units)
    {
        var field = typeof(Product).GetField("_units", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(product, units);
    }

    private static void SetId<T>(T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, id);
    }

    private static void SetNavigation<T, TNavigation>(T entity, string propertyName, TNavigation? value)
        where T : class
    {
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, value);
    }

    // ─── Mock Setup Helpers ─────────────────────────────────────

    private void SetupProductUnitQuery(ProductUnit result)
    {
        var repoMock = new Mock<IGenericRepository<ProductUnit>>();
        repoMock.Setup(r => r.Query()).Returns(new[] { result }.AsAsyncQueryable());
        _uowMock.Setup(u => u.ProductUnits).Returns(repoMock.Object);
    }

    private void SetupWarehouseStockQuery(decimal quantity)
    {
        var repoMock = new Mock<IGenericRepository<WarehouseStock>>();

        if (quantity > 0)
        {
            var productId = _product.Id > 0 ? _product.Id : 1;
            var stock = WarehouseStock.Create(warehouseId: 1, productId: productId, quantity: quantity);
            repoMock.Setup(r => r.Query()).Returns(new[] { stock }.AsAsyncQueryable());
        }
        else
        {
            repoMock.Setup(r => r.Query()).Returns(Array.Empty<WarehouseStock>().AsAsyncQueryable());
        }

        _uowMock.Setup(u => u.WarehouseStocks).Returns(repoMock.Object);
    }

    private void SetupPriceHistory(Mock<IGenericRepository<ProductPriceHistory>>? customMock = null)
    {
        var repoMock = customMock ?? new Mock<IGenericRepository<ProductPriceHistory>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPriceHistory e, CancellationToken _) => e);
        _uowMock.Setup(u => u.ProductPriceHistory).Returns(repoMock.Object);
    }

    private void SetupSaveChanges()
    {
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private UpdatePricingRequest CreateRequest(int productUnitId, decimal newCost, decimal newQty,
        decimal? newSalesPrice = null, int invoiceId = 100, int changedBy = 1)
    {
        return new UpdatePricingRequest(productUnitId, newCost, newQty, newSalesPrice, invoiceId, changedBy);
    }

    // ─── WeightedAverage Tests ──────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_WeightedAverage_ShouldCalculateCorrectly()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.WeightedAverage);
        SetupProductUnitQuery(_derivedUnit);
        SetupWarehouseStockQuery(100); // 100 base units in stock at 10 = 1000 value
        SetupPriceHistory();
        SetupSaveChanges();

        // Purchase 24 boxes at 15 per base-unit cost
        // invoiceCostForPurchasedUnit = 180 (15 * 12)
        // newBaseCostFromInvoice = 180 / 12 = 15
        // newQuantityInBaseUnits = 24 * 12 = 288
        // weightedAverage = (100*10 + 288*15) / (100+288) = (1000+4320)/388 = 13.7113
        var request = CreateRequest(productUnitId: _derivedUnit.Id,
            newCost: 180, newQty: 24, invoiceId: 200, changedBy: 1);

        await _sut.UpdateFromPurchaseAsync(request);

        _baseUnit.PurchaseCost.Should().BeApproximately(13.71m, 0.01m);
        _derivedUnit.PurchaseCost.Should().BeApproximately(164.52m, 0.01m);
    }

    [Fact]
    public async Task UpdateFromPurchaseAsync_WeightedAverage_WithZeroStock_ShouldUseInvoiceCost()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.WeightedAverage);
        SetupProductUnitQuery(_derivedUnit);
        SetupWarehouseStockQuery(0);
        SetupPriceHistory();
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 180, newQty: 24);

        await _sut.UpdateFromPurchaseAsync(request);

        _baseUnit.PurchaseCost.Should().Be(15);
        _derivedUnit.PurchaseCost.Should().Be(180);
    }

    // ─── LastPurchasePrice Tests ────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_LastPurchasePrice_ShouldUseInvoiceCost()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 240, newQty: 10);

        await _sut.UpdateFromPurchaseAsync(request);

        _baseUnit.PurchaseCost.Should().Be(20);
        _derivedUnit.PurchaseCost.Should().Be(240);
    }

    // ─── SupplierPrice Tests ────────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_SupplierPrice_WhenSupplierPriceSet_ShouldUseIt()
    {
        _baseUnit.UpdateSupplierPrice(18);
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.SupplierPrice);
        SetupProductUnitQuery(_baseUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: _baseUnit.Id, newCost: 999, newQty: 1);

        await _sut.UpdateFromPurchaseAsync(request);

        _baseUnit.PurchaseCost.Should().Be(18);
    }

    [Fact]
    public async Task UpdateFromPurchaseAsync_SupplierPrice_WhenSupplierPriceZero_ShouldFallbackToInvoiceCost()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.SupplierPrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 180, newQty: 24);

        await _sut.UpdateFromPurchaseAsync(request);

        _derivedUnit.PurchaseCost.Should().Be(180);
        _baseUnit.PurchaseCost.Should().Be(15);
    }

    // ─── Sales Price Update ─────────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_WithNewSalesPrice_ShouldUpdateSalesPriceAndAddHistory()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var historyRepoMock = Mock.Get(_uowMock.Object.ProductPriceHistory);
        historyRepoMock.Invocations.Clear();

        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 120, newQty: 10,
            newSalesPrice: 350);

        await _sut.UpdateFromPurchaseAsync(request);

        _derivedUnit.SalesPrice.Should().Be(350);
        historyRepoMock.Verify(r => r.AddAsync(
            It.Is<ProductPriceHistory>(h => h.ChangeType == "SalesPrice"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateFromPurchaseAsync_WithoutNewSalesPrice_ShouldNotUpdateSalesPrice()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var originalPrice = _derivedUnit.SalesPrice;
        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 120, newQty: 10);

        await _sut.UpdateFromPurchaseAsync(request);

        _derivedUnit.SalesPrice.Should().Be(originalPrice);
    }

    // ─── Base Unit Purchase ─────────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_WhenPurchasingBaseUnit_ShouldUseCostDirectly()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_baseUnit);
        SetupPriceHistory();
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: _baseUnit.Id, newCost: 14, newQty: 50);

        await _sut.UpdateFromPurchaseAsync(request);

        _baseUnit.PurchaseCost.Should().Be(14);
    }

    // ─── History Recording ──────────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_ShouldRecordPriceHistoryPerUnit()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupSaveChanges();

        var historyRepoMock = new Mock<IGenericRepository<ProductPriceHistory>>();
        var historyList = new List<ProductPriceHistory>();
        historyRepoMock.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback((ProductPriceHistory h, CancellationToken _) => historyList.Add(h))
            .ReturnsAsync((ProductPriceHistory h, CancellationToken _) => h);
        _uowMock.Setup(u => u.ProductPriceHistory).Returns(historyRepoMock.Object);

        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 180, newQty: 24);

        await _sut.UpdateFromPurchaseAsync(request);

        historyList.Should().HaveCount(2);
        historyList.Should().AllSatisfy(h => h.InvoiceId.Should().Be(100));
        historyList.Should().OnlyContain(h => h.ChangeType == "PurchaseCost");
        historyList.Should().OnlyContain(h => h.CostingMethod == "LastPurchasePrice");
    }

    [Fact]
    public async Task UpdateFromPurchaseAsync_ShouldRecordOldAndNewCostValues()
    {
        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(_derivedUnit);
        SetupSaveChanges();

        var historyRepoMock = new Mock<IGenericRepository<ProductPriceHistory>>();
        var captured = new List<ProductPriceHistory>();
        historyRepoMock.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback((ProductPriceHistory h, CancellationToken _) => captured.Add(h))
            .ReturnsAsync((ProductPriceHistory h, CancellationToken _) => h);
        _uowMock.Setup(u => u.ProductPriceHistory).Returns(historyRepoMock.Object);

        var oldBaseCost = _baseUnit.PurchaseCost;
        var oldDerivedCost = _derivedUnit.PurchaseCost;
        var request = CreateRequest(productUnitId: _derivedUnit.Id, newCost: 240, newQty: 10);

        await _sut.UpdateFromPurchaseAsync(request);

        var baseHistory = captured.Where(h =>
            h.ProductUnitId == _baseUnit.Id && h.ChangeType == "PurchaseCost").ToList();
        baseHistory.Should().HaveCount(1);
        baseHistory[0].OldValue.Should().Be(oldBaseCost);
        baseHistory[0].NewValue.Should().Be(20);

        var derivedHistory = captured.Where(h =>
            h.ProductUnitId == _derivedUnit.Id && h.ChangeType == "PurchaseCost").ToList();
        derivedHistory.Should().HaveCount(1);
        derivedHistory[0].OldValue.Should().Be(oldDerivedCost);
        derivedHistory[0].NewValue.Should().Be(240);
    }

    // ─── Error Cases ────────────────────────────────────────────

    [Fact]
    public async Task UpdateFromPurchaseAsync_WhenProductUnitNotFound_ShouldReturnFailure()
    {
        var repoMock = new Mock<IGenericRepository<ProductUnit>>();
        repoMock.Setup(r => r.Query()).Returns(Array.Empty<ProductUnit>().AsAsyncQueryable());
        _uowMock.Setup(u => u.ProductUnits).Returns(repoMock.Object);

        var request = CreateRequest(productUnitId: 999, newCost: 10, newQty: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("وحدة المنتج غير موجودة");
    }

    [Fact]
    public async Task UpdateFromPurchaseAsync_WhenNoBaseUnit_ShouldReturnFailure()
    {
        var product = Product.Create("بلا وحدة أساسية", purchasePrice: 5, retailPrice: 10, createdByUserId: 1);
        var derivedOnly = ProductUnit.CreateDerivedUnit(
            productId: 2, unitName: "صندوق", baseConversionFactor: 6);
        SetProductUnits(product, [derivedOnly]);
        SetNavigation(derivedOnly, nameof(ProductUnit.Product), product);

        _settingsMock.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);
        SetupProductUnitQuery(derivedOnly);
        SetupSaveChanges();

        var request = CreateRequest(productUnitId: derivedOnly.Id, newCost: 60, newQty: 5);

        var result = await _sut.UpdateFromPurchaseAsync(request);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يحتوي على وحدة أساسية");
    }
}

// ─── EF Core Async Queryable Helpers ─────────────────────────────

internal static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
    {
        var queryable = source.AsQueryable();
        return new TestAsyncQueryable<T>(queryable);
    }
}

internal class TestAsyncQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
{
    private readonly IQueryable<T> _inner;

    public TestAsyncQueryable(IQueryable<T> inner)
    {
        _inner = inner;
        Provider = new TestAsyncQueryProvider<T>(inner.Provider);
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(_inner.AsEnumerable().GetEnumerator());

    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

    public Expression Expression => _inner.Expression;
    public Type ElementType => _inner.ElementType;
    public IQueryProvider Provider { get; }
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
        => new TestAsyncQueryable<T>(_inner.CreateQuery<T>(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncQueryable<TElement>(_inner.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var expectedType = typeof(TResult);

        if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = expectedType.GetGenericArguments()[0];
            var innerResult = _inner.Execute(expression);
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult),
                BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(innerType);
            return (TResult)fromResult.Invoke(null, [innerResult])!;
        }

        return (TResult)_inner.Execute(expression)!;
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        => new TestAsyncQueryable<TResult>(new EnumerableQuery<TResult>(expression));
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

    public T Current => _inner.Current;
}
