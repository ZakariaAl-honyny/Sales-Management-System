using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SalesInvoiceLineTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 2,
            unitPrice: 100m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100m);
        item.LineTotal.Should().Be(200m); // 2 * 100
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesInvoiceLine.Create(
            productId: 0,
            productUnitId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Fact]
    public void Create_GivenProductIdIsNegative_ShouldThrowDomainException()
    {
        var action = () => SalesInvoiceLine.Create(
            productId: -1,
            productUnitId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidQuantity_ShouldThrowDomainException(decimal invalidQuantity)
    {
        var action = () => SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: invalidQuantity,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الكمية يجب أن تكون أكبر من الصفر*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeUnitPrice_ShouldThrowDomainException(decimal negativePrice)
    {
        var action = () => SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitPrice: negativePrice
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*سعر الوحدة لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Create_GivenZeroUnitPrice_ShouldSucceed()
    {
        var item = SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitPrice: 0m
        );

        item.UnitPrice.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenNoDiscount_ShouldHaveLineTotalAsQuantityTimesUnitPrice()
    {
        var item = SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void RecalculateLineTotal_ShouldUpdateLineTotal()
    {
        var item = SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 3,
            unitPrice: 50m
        );

        item.RecalculateLineTotal();

        item.LineTotal.Should().Be(150m); // 3 * 50
    }

    [Fact]
    public void Create_GivenLargeQuantity_ShouldCalculateCorrectly()
    {
        var item = SalesInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1000m,
            unitPrice: 10m
        );

        item.LineTotal.Should().Be(10000m); // 1000 * 10
    }
}