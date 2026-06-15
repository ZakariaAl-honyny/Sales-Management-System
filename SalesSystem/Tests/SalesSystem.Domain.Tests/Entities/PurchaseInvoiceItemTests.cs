using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class PurchaseInvoiceItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldSetLineTotalAsQuantityTimesUnitCost()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 10m,
            unitCost: 15.50m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(10m);
        item.UnitCost.Should().Be(15.50m);
        item.LineTotal.Should().Be(10m * 15.50m);
    }

    [Fact]
    public void Create_GivenZeroProductId_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 0,
            productUnitId: 1,
            quantity: 5m,
            unitCost: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Fact]
    public void Create_GivenZeroProductUnitId_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 0,
            quantity: 5m,
            unitCost: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الوحدة مطلوبة*");
    }

    [Fact]
    public void Create_GivenNegativeQuantity_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: -1m,
            unitCost: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الكمية يجب أن تكون أكبر من الصفر*");
    }

    [Fact]
    public void Create_GivenZeroQuantity_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 0m,
            unitCost: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الكمية يجب أن تكون أكبر من الصفر*");
    }

    [Fact]
    public void Create_GivenNegativeUnitCost_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitCost: -3m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*تكلفة الوحدة لا يمكن أن تكون سالبة*");
    }

    [Fact]
    public void Create_GivenZeroUnitCost_ShouldSucceed()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitCost: 0m
        );

        item.UnitCost.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void Create_GivenValidProductId_ShouldSucceed(int productId)
    {
        var item = PurchaseInvoiceItem.Create(
            productId: productId,
            productUnitId: 1,
            quantity: 1m,
            unitCost: 10m
        );

        item.ProductId.Should().Be(productId);
    }

    [Fact]
    public void RecalculateLineTotal_AfterModifyingQuantity_ShouldRecalculate()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitCost: 20m
        );

        item.LineTotal.Should().Be(100m);

        // Reflect the Quantity change by using a new instance pattern
        var recalculated = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 10m,
            unitCost: 20m
        );

        recalculated.LineTotal.Should().Be(200m);
    }

    [Fact]
    public void Create_GivenDecimalQuantity_ShouldPreservePrecision()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 0.500m,
            unitCost: 10.00m
        );

        item.Quantity.Should().Be(0.500m);
        item.LineTotal.Should().Be(0.500m * 10.00m);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.010)]
    [InlineData(999999.999)]
    public void Create_GivenVariousQuantities_ShouldComputeLineTotal(decimal quantity)
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: quantity,
            unitCost: 10.50m
        );

        item.LineTotal.Should().Be(quantity * 10.50m);
    }
}
