using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

public class UpdateProductPricingServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ISystemSettingsRepository> _mockSettings;
    private readonly Mock<ILogger<UpdateProductPricingService>> _mockLogger;
    private readonly UpdateProductPricingService _sut;

    public UpdateProductPricingServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockUow = new Mock<IUnitOfWork>();
        _mockSettings = new Mock<ISystemSettingsRepository>();
        _mockLogger = new Mock<ILogger<UpdateProductPricingService>>();

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new UpdateProductPricingService(
            _mockUow.Object, _mockSettings.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task WeightedAverage_WithExistingStock_CalculatesCorrectly()
    {
        _output.WriteLine("[TEST] WeightedAverage_WithExistingStock_CalculatesCorrectly");

        var product = Product.Create("Test Product", purchasePrice: 10m, retailPrice: 100m);
        SetEntityId(product, 1);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        product.AddUnit(baseUnit);
        SetNavigationProperty(baseUnit, "Product", product);

        var stock = WarehouseStock.Create(1, product.Id, quantity: 100m);

        var productUnits = new List<ProductUnit> { baseUnit };
        var warehouseStocks = new List<WarehouseStock> { stock };
        var priceHistory = new List<ProductPriceHistory>();

        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo(warehouseStocks);
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPriceHistory, CancellationToken>((h, _) => priceHistory.Add(h))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.WeightedAverage);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 12m,
            NewQuantityPurchased: 50m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(10.67m);
        priceHistory.Should().Contain(h =>
            h.ProductUnitId == baseUnit.Id &&
            h.ChangeType == "PurchaseCost" &&
            h.OldValue == 10m &&
            h.NewValue == 10.67m);
        _output.WriteLine("[PASS] WeightedAverage calculated correctly: (100*10 + 50*12) / 150 = 10.67");
    }

    [Fact]
    public async Task WeightedAverage_WithZeroStock_UsesNewCost()
    {
        _output.WriteLine("[TEST] WeightedAverage_WithZeroStock_UsesNewCost");

        var product = Product.Create("New Product", purchasePrice: 10m, retailPrice: 100m);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        product.AddUnit(baseUnit);
        SetNavigationProperty(baseUnit, "Product", product);

        var productUnits = new List<ProductUnit> { baseUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo<WarehouseStock>(new List<WarehouseStock>());
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.WeightedAverage);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 15m,
            NewQuantityPurchased: 100m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(15m);
        _output.WriteLine("[PASS] Zero stock uses new purchase cost directly");
    }

    [Fact]
    public async Task LastPurchasePrice_OverwritesCost()
    {
        _output.WriteLine("[TEST] LastPurchasePrice_OverwritesCost");

        var product = Product.Create("Test Product", purchasePrice: 10m, retailPrice: 100m);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        product.AddUnit(baseUnit);
        SetNavigationProperty(baseUnit, "Product", product);

        var productUnits = new List<ProductUnit> { baseUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo<WarehouseStock>(new List<WarehouseStock>());
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 25m,
            NewQuantityPurchased: 10m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(25m);
        baseUnit.LastPurchasePrice.Should().Be(25m);
        _output.WriteLine("[PASS] LastPurchasePrice overwrites cost directly to 25");
    }

    [Fact]
    public async Task SupplierPrice_UsesSupplierPrice()
    {
        _output.WriteLine("[TEST] SupplierPrice_UsesSupplierPrice");

        var product = Product.Create("Test Product", purchasePrice: 10m, retailPrice: 100m);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        baseUnit.UpdateSupplierPrice(8m);
        product.AddUnit(baseUnit);
        SetNavigationProperty(baseUnit, "Product", product);

        var productUnits = new List<ProductUnit> { baseUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo<WarehouseStock>(new List<WarehouseStock>());
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.SupplierPrice);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 12m,
            NewQuantityPurchased: 50m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(8m);
        _output.WriteLine("[PASS] SupplierPrice uses catalog price 8 instead of invoice cost 12");
    }

    [Fact]
    public async Task SupplierPrice_FallsBackToInvoiceCost_WhenSupplierPriceIsZero()
    {
        _output.WriteLine("[TEST] SupplierPrice_FallsBackToInvoiceCost_WhenSupplierPriceIsZero");

        var product = Product.Create("Test Product", purchasePrice: 10m, retailPrice: 100m);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        product.AddUnit(baseUnit);
        SetNavigationProperty(baseUnit, "Product", product);

        var productUnits = new List<ProductUnit> { baseUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo<WarehouseStock>(new List<WarehouseStock>());
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.SupplierPrice);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 20m,
            NewQuantityPurchased: 10m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(20m);
        _output.WriteLine("[PASS] SupplierPrice falls back to invoice cost 20 when SupplierPrice is 0");
    }

    [Fact]
    public async Task CostCascades_ToAllDerivedUnits()
    {
        _output.WriteLine("[TEST] CostCascades_ToAllDerivedUnits");

        var product = Product.Create("Test Product", purchasePrice: 10m, retailPrice: 100m);
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "قطعة", 100m, 10m);
        var derivedUnit = ProductUnit.CreateDerivedUnit(product.Id, "صندوق", 12m, 1200m, 120m, sortOrder: 1);
        product.AddUnit(baseUnit);
        product.AddUnit(derivedUnit);
        SetNavigationProperty(baseUnit, "Product", product);
        SetNavigationProperty(derivedUnit, "Product", product);

        var productUnits = new List<ProductUnit> { baseUnit, derivedUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        var mockStockRepo = CreateMockRepo<WarehouseStock>(new List<WarehouseStock>());
        var mockHistoryRepo = new Mock<IGenericRepository<ProductPriceHistory>>();
        var historyEntries = new List<ProductPriceHistory>();
        mockHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPriceHistory, CancellationToken>((h, _) => historyEntries.Add(h))
            .ReturnsAsync(default(ProductPriceHistory));

        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);
        _mockUow.Setup(u => u.WarehouseStocks).Returns(mockStockRepo.Object);
        _mockUow.Setup(u => u.ProductPriceHistory).Returns(mockHistoryRepo.Object);
        _mockSettings.Setup(s => s.GetCostingMethodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostingMethod.LastPurchasePrice);

        var request = new UpdatePricingRequest(
            ProductUnitId: baseUnit.Id,
            NewPurchaseCost: 15m,
            NewQuantityPurchased: 10m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeTrue();
        baseUnit.PurchaseCost.Should().Be(15m);
        derivedUnit.PurchaseCost.Should().Be(180m);
        historyEntries.Should().HaveCount(2);
        historyEntries.Should().Contain(h => h.ProductUnitId == baseUnit.Id && h.NewValue == 15m);
        historyEntries.Should().Contain(h => h.ProductUnitId == derivedUnit.Id && h.NewValue == 180m);
        _output.WriteLine("[PASS] Cost cascaded: base=15, derived(12x)=180");
    }

    [Fact]
    public async Task ReturnsFailure_WhenProductUnitNotFound()
    {
        _output.WriteLine("[TEST] ReturnsFailure_WhenProductUnitNotFound");

        var mockProductUnitsRepo = CreateMockRepo<ProductUnit>(new List<ProductUnit>());
        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);

        var request = new UpdatePricingRequest(
            ProductUnitId: 999,
            NewPurchaseCost: 10m,
            NewQuantityPurchased: 5m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("وحدة المنتج غير موجودة");
        _output.WriteLine("[PASS] Returns failure when ProductUnit not found");
    }

    [Fact]
    public async Task ReturnsFailure_WhenNoBaseUnit()
    {
        _output.WriteLine("[TEST] ReturnsFailure_WhenNoBaseUnit");

        var product = Product.Create("No Base Unit", purchasePrice: 10m, retailPrice: 100m);
        var derivedUnit = ProductUnit.CreateDerivedUnit(product.Id, "صندوق", 12m, 100m, 10m);
        product.AddUnit(derivedUnit);
        SetNavigationProperty(derivedUnit, "Product", product);

        var productUnits = new List<ProductUnit> { derivedUnit };
        var mockProductUnitsRepo = CreateMockRepo(productUnits);
        _mockUow.Setup(u => u.ProductUnits).Returns(mockProductUnitsRepo.Object);

        var request = new UpdatePricingRequest(
            ProductUnitId: derivedUnit.Id,
            NewPurchaseCost: 10m,
            NewQuantityPurchased: 5m,
            NewSalesPrice: null,
            InvoiceId: 1,
            ChangedBy: 1);

        var result = await _sut.UpdateFromPurchaseAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يحتوي على وحدة أساسية");
        _output.WriteLine("[PASS] Returns failure when product has no base unit");
    }

    private static Mock<IGenericRepository<T>> CreateMockRepo<T>(List<T> items) where T : SalesSystem.Domain.Common.BaseEntity
    {
        var mock = new Mock<IGenericRepository<T>>();
        mock.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<T, bool>> pred, CancellationToken ct, string[] includes) =>
                items.AsQueryable().FirstOrDefault(pred));

        mock.Setup(r => r.ToListAsync(
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(items.ToList());

        return mock;
    }

    private static void SetNavigationProperty<T>(object obj, string propertyName, T value)
    {
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(obj, value);
    }

    private static void SetEntityId(BaseEntity entity, int id)
    {
        var field = typeof(BaseEntity).GetField("<Id>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(entity, id);
    }
}
