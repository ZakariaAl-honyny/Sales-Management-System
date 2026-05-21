using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class WarehouseStockTests
{
    [Fact]
    public void Create_GivenValidQuantity_ShouldCreateWarehouseStock()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.WarehouseId.Should().Be(1);
        stock.ProductId.Should().Be(1);
        stock.Quantity.Should().Be(100m);
    }

    [Fact]
    public void Create_GivenZeroQuantity_ShouldCreateWarehouseStock()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 0
        );

        stock.Quantity.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeQuantity_ShouldThrowDomainException(decimal negativeQuantity)
    {
        var action = () => WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: negativeQuantity
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية لا يمكن أن تكون سالبة.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => WarehouseStock.Create(
            warehouseId: 0,
            productId: 1,
            quantity: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => WarehouseStock.Create(
            warehouseId: 1,
            productId: 0,
            quantity: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Fact]
    public void IncreaseQuantity_GivenValidAmount_ShouldIncreaseQuantity()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.IncreaseQuantity(50m);

        stock.Quantity.Should().Be(150m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void IncreaseQuantity_GivenInvalidAmount_ShouldThrowDomainException(decimal invalidAmount)
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        var action = () => stock.IncreaseQuantity(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ يجب أن يكون أكبر من الصفر.");
    }

    [Fact]
    public void IncreaseQuantity_MultipleTimes_ShouldAccumulateCorrectly()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.IncreaseQuantity(50m);
        stock.IncreaseQuantity(30m);
        stock.IncreaseQuantity(20m);

        stock.Quantity.Should().Be(200m);
    }

    [Fact]
    public void DecreaseQuantity_GivenSufficientStock_ShouldDecreaseQuantity()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.DecreaseQuantity(30m);

        stock.Quantity.Should().Be(70m);
    }

    [Fact]
    public void DecreaseQuantity_GivenExactStock_ShouldSetQuantityToZero()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 50m
        );

        stock.DecreaseQuantity(50m);

        stock.Quantity.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void DecreaseQuantity_GivenInvalidAmount_ShouldThrowDomainException(decimal invalidAmount)
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        var action = () => stock.DecreaseQuantity(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ يجب أن يكون أكبر من الصفر.");
    }

    [Fact]
    public void DecreaseQuantity_GivenInsufficientStock_ShouldThrowDomainException()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 50m
        );

        var action = () => stock.DecreaseQuantity(100m);

        action.Should().Throw<DomainException>()
            .WithMessage("المخزون غير كافٍ.");
    }

    [Fact]
    public void DecreaseQuantity_StockExactlyEqualToAmount_ShouldSucceed()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        var action = () => stock.DecreaseQuantity(100m);

        action.Should().NotThrow();
        stock.Quantity.Should().Be(0);
    }

    [Fact]
    public void SetQuantity_GivenValidQuantity_ShouldSetQuantity()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.SetQuantity(200m);

        stock.Quantity.Should().Be(200m);
    }

    [Fact]
    public void SetQuantity_GivenZeroQuantity_ShouldSetQuantity()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.SetQuantity(0);

        stock.Quantity.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetQuantity_GivenNegativeQuantity_ShouldThrowDomainException(decimal negativeQuantity)
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        var action = () => stock.SetQuantity(negativeQuantity);

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية لا يمكن أن تكون سالبة.");
    }

    [Fact]
    public void IncreaseQuantity_AfterDecrease_ShouldCalculateCorrectly()
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: 100m
        );

        stock.DecreaseQuantity(30m);
        stock.IncreaseQuantity(50m);

        stock.Quantity.Should().Be(120m);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.5)]
    [InlineData(999.999)]
    public void Create_GivenDecimalQuantity_ShouldAccept(decimal quantity)
    {
        var stock = WarehouseStock.Create(
            warehouseId: 1,
            productId: 1,
            quantity: quantity
        );

        stock.Quantity.Should().Be(quantity);
    }
}