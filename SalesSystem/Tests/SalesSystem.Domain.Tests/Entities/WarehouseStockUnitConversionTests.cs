using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

/// <summary>
/// Tests for the new DeductStock / AddStock methods on WarehouseStock
/// that perform unit-type-aware quantity conversion.
/// </summary>
public class WarehouseStockUnitConversionTests
{
    // ── DeductStock (Wholesale) ──────────────────────────────────────────────

    [Fact]
    public void DeductStock_Wholesale_DeductsCorrectPieces()
    {
        // Arrange: 24 pieces in stock
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 24m);

        // Act: Deduct 2 boxes, each box = 12 pieces → 24 pieces deducted
        stock.DeductStock(quantity: 2m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        // Assert: 24 - (2 × 12) = 0 remaining
        stock.Quantity.Should().Be(0m);
    }

    [Fact]
    public void DeductStock_Wholesale_PartialBox_DeductsCorrectPieces()
    {
        // Arrange: 30 pieces in stock
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 30m);

        // Act: Deduct 1 box of 12 → deducts 12
        stock.DeductStock(quantity: 1m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        // Assert: 30 - 12 = 18
        stock.Quantity.Should().Be(18m);
    }

    // ── DeductStock (Retail) ─────────────────────────────────────────────────

    [Fact]
    public void DeductStock_Retail_DeductsOnePiece()
    {
        // Arrange: 24 pieces
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 24m);

        // Act: Deduct 1 retail unit (= 1 piece regardless of conversionFactor)
        stock.DeductStock(quantity: 1m, unitType: UnitType.Retail, conversionFactor: 12m);

        // Assert: 24 - 1 = 23
        stock.Quantity.Should().Be(23m);
    }

    [Fact]
    public void DeductStock_Retail_DeductsCorrectAmount()
    {
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 50m);

        stock.DeductStock(quantity: 7m, unitType: UnitType.Retail, conversionFactor: 12m);

        stock.Quantity.Should().Be(43m);
    }

    // ── DeductStock (Insufficient Stock) ────────────────────────────────────

    [Fact]
    public void DeductStock_InsufficientStock_Wholesale_ThrowsDomainException()
    {
        // Arrange: only 5 pieces in stock
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 5m);

        // Act: try to deduct 1 box = 12 pieces — should fail
        var action = () => stock.DeductStock(quantity: 1m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        action.Should().Throw<DomainException>()
            .WithMessage("*المخزون غير كافٍ*");
    }

    [Fact]
    public void DeductStock_InsufficientStock_Retail_ThrowsDomainException()
    {
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 3m);

        var action = () => stock.DeductStock(quantity: 5m, unitType: UnitType.Retail, conversionFactor: 12m);

        action.Should().Throw<DomainException>()
            .WithMessage("*المخزون غير كافٍ*");
    }

    [Fact]
    public void DeductStock_ExactQuantity_Wholesale_SetsQuantityToZero()
    {
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 24m);

        // 2 boxes × 12 = 24 pieces exactly
        stock.DeductStock(quantity: 2m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        stock.Quantity.Should().Be(0m);
    }

    // ── AddStock (Wholesale) ─────────────────────────────────────────────────

    [Fact]
    public void AddStock_Wholesale_AddsCorrectPieces()
    {
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 0m);

        // Add 3 boxes of 12 = 36 pieces
        stock.AddStock(quantity: 3m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        stock.Quantity.Should().Be(36m);
    }

    // ── AddStock (Retail) ────────────────────────────────────────────────────

    [Fact]
    public void AddStock_Retail_AddsCorrectPieces()
    {
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 10m);

        // Add 5 retail units = 5 pieces
        stock.AddStock(quantity: 5m, unitType: UnitType.Retail, conversionFactor: 12m);

        stock.Quantity.Should().Be(15m);
    }

    // ── Mixed Scenario ───────────────────────────────────────────────────────

    [Fact]
    public void AddThenDeduct_MixedUnits_CalculatesCorrectly()
    {
        // Start: 0 pieces
        var stock = WarehouseStock.Create(warehouseId: 1, productId: 1, quantity: 0m);

        // Add 2 boxes (24 pieces)
        stock.AddStock(quantity: 2m, unitType: UnitType.Wholesale, conversionFactor: 12m);
        // Sell 3 retail pieces
        stock.DeductStock(quantity: 3m, unitType: UnitType.Retail, conversionFactor: 12m);
        // Sell 1 box (12 pieces)
        stock.DeductStock(quantity: 1m, unitType: UnitType.Wholesale, conversionFactor: 12m);

        // Expected: 24 - 3 - 12 = 9
        stock.Quantity.Should().Be(9m);
    }
}
