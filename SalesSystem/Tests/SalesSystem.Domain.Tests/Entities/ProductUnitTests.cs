using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_ShouldSetConversionFactorToOne()
    {
        var unit = ProductUnit.CreateBaseUnit(1, unitId: 1);
        Assert.Equal(1, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
    }

    [Fact]
    public void CreateBaseUnit_InvalidUnitId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => ProductUnit.CreateBaseUnit(1, unitId: 0));
    }

    [Fact]
    public void CreateDerivedUnit_FactorLessThanOrEqualToOne_ShouldThrow()
    {
        Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, unitId: 2, baseConversionFactor: 1));
        Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, unitId: 2, baseConversionFactor: 0.5m));
    }

    [Fact]
    public void CreateDerivedUnit_FactorMoreThanOne_ShouldSucceed()
    {
        var unit = ProductUnit.CreateDerivedUnit(1, unitId: 2, baseConversionFactor: 12);
        Assert.Equal(12, unit.BaseConversionFactor);
        Assert.False(unit.IsBaseUnit);
    }

    [Fact]
    public void ToBaseUnitQuantity_ShouldMultiplyByFactor()
    {
        var baseUnit = ProductUnit.CreateBaseUnit(1, unitId: 1);
        var derivedUnit = ProductUnit.CreateDerivedUnit(1, unitId: 2, baseConversionFactor: 12);

        Assert.Equal(5, baseUnit.ToBaseUnitQuantity(5));
        Assert.Equal(60, derivedUnit.ToBaseUnitQuantity(5));
    }

    [Fact]
    public void CreateDerivedUnit_SortOrder_DefaultsToOne()
    {
        var unit = ProductUnit.CreateDerivedUnit(1, unitId: 2, baseConversionFactor: 12);
        Assert.NotNull(unit);
    }

    [Fact]
    public void ChangeUnit_ValidUnitId_UpdatesUnitId()
    {
        var unit = ProductUnit.CreateBaseUnit(1, unitId: 1);
        unit.ChangeUnit(2);
        Assert.Equal(2, unit.UnitId);
    }

    [Fact]
    public void ChangeUnit_InvalidUnitId_ThrowsDomainException()
    {
        var unit = ProductUnit.CreateBaseUnit(1, unitId: 1);
        Assert.Throws<DomainException>(() => unit.ChangeUnit(0));
    }
}
