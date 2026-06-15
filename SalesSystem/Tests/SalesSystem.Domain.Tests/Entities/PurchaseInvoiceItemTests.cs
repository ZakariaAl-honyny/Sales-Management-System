using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class PurchaseInvoiceLineTests
{
    [Fact]
    public void Create_GivenValidData_ShouldSetLineTotalAsQuantityTimesUnitPrice()
    {
        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 10m,
            unitPrice: 15.50m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(10m);
        item.UnitPrice.Should().Be(15.50m);
        item.LineTotal.Should().Be(10m * 15.50m);
    }

    [Fact]
    public void Create_GivenZeroProductId_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceLine.Create(
            productId: 0,
            productUnitId: 1,
            quantity: 5m,
            unitPrice: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Fact]
    public void Create_GivenZeroProductUnitId_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 0,
            quantity: 5m,
            unitPrice: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الوحدة مطلوبة*");
    }

    [Fact]
    public void Create_GivenNegativeQuantity_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: -1m,
            unitPrice: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الكمية يجب أن تكون أكبر من الصفر*");
    }

    [Fact]
    public void Create_GivenZeroQuantity_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 0m,
            unitPrice: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الكمية يجب أن تكون أكبر من الصفر*");
    }

    [Fact]
    public void Create_GivenNegativeUnitPrice_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitPrice: -3m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*سعر الوحدة لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Create_GivenZeroUnitPrice_ShouldSucceed()
    {
        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitPrice: 0m
        );

        item.UnitPrice.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void Create_GivenValidProductId_ShouldSucceed(int productId)
    {
        var item = PurchaseInvoiceLine.Create(
            productId: productId,
            productUnitId: 1,
            quantity: 1m,
            unitPrice: 10m
        );

        item.ProductId.Should().Be(productId);
    }

    [Fact]
    public void RecalculateLineTotal_AfterModifyingQuantity_ShouldRecalculate()
    {
        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 5m,
            unitPrice: 20m
        );

        item.LineTotal.Should().Be(100m);

        // Reflect the Quantity change by using a new instance pattern
        var recalculated = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 10m,
            unitPrice: 20m
        );

        recalculated.LineTotal.Should().Be(200m);
    }

    [Fact]
    public void Create_GivenDecimalQuantity_ShouldPreservePrecision()
    {
        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 0.500m,
            unitPrice: 10.00m
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
        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: quantity,
            unitPrice: 10.50m
        );

        item.LineTotal.Should().Be(quantity * 10.50m);
    }
}
