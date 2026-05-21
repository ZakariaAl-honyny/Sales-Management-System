using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_ShouldSetConversionFactorToOne()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة");
        Assert.Equal(1, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
        Assert.Equal("حبة", unit.UnitName);
    }

    [Fact]
    public void CreateBaseUnit_EmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => ProductUnit.CreateBaseUnit(1, ""));
    }

    [Fact]
    public void CreateDerivedUnit_FactorLessThanOrEqualToOne_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => 
            ProductUnit.CreateDerivedUnit(1, "كرتون", 1));
        Assert.Throws<DomainException>(() => 
            ProductUnit.CreateDerivedUnit(1, "كرتون", 0.5m));
    }

    [Fact]
    public void CreateDerivedUnit_FactorMoreThanOne_ShouldSucceed()
    {
        var unit = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        Assert.Equal(12, unit.BaseConversionFactor);
        Assert.False(unit.IsBaseUnit);
    }

    [Fact]
    public void ToBaseUnitQuantity_ShouldMultiplyByFactor()
    {
        var baseUnit = ProductUnit.CreateBaseUnit(1, "حبة");
        var derivedUnit = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);

        Assert.Equal(5, baseUnit.ToBaseUnitQuantity(5));
        Assert.Equal(60, derivedUnit.ToBaseUnitQuantity(5));
    }

    [Fact]
    public void UpdatePurchaseCost_Negative_ShouldThrow()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة");
        Assert.Throws<DomainException>(() => unit.UpdatePurchaseCost(-5));
    }

    [Fact]
    public void UpdatePurchaseCost_ShouldReturnOldValue()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة", purchaseCost: 10);
        var oldCost = unit.UpdatePurchaseCost(15);
        Assert.Equal(10, oldCost);
        Assert.Equal(15, unit.PurchaseCost);
        Assert.Equal(15, unit.LastPurchasePrice);
    }

    [Fact]
    public void AddBarcode_EmptyValue_ShouldThrow()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة");
        Assert.Throws<DomainException>(() => unit.AddBarcode(""));
    }

    [Fact]
    public void AddBarcode_Valid_ShouldAddToList()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة");
        unit.AddBarcode("123456", isDefault: true);
        unit.AddBarcode("789012", isDefault: false);

        Assert.Equal(2, unit.Barcodes.Count);
        Assert.Equal("123456", unit.Barcodes.First(b => b.IsDefault).BarcodeValue);
    }

    [Fact]
    public void CalculateCostFromBaseUnitCost_ShouldMultiplyByFactor()
    {
        var unit = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        var cost = unit.CalculateCostFromBaseUnitCost(10);
        Assert.Equal(120, cost);
    }
}
