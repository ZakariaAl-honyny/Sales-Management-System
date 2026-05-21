using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SalesInvoiceItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitPrice: 100m,
            discountAmount: 10m,
            notes: "Test item"
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100m);
        item.DiscountAmount.Should().Be(10m);
        item.Notes.Should().Be("Test item");
        item.LineTotal.Should().Be(190m); // (2 * 100) - 10
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesInvoiceItem.Create(
            productId: 0,
            quantity: 1,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Fact]
    public void Create_GivenProductIdIsNegative_ShouldThrowDomainException()
    {
        var action = () => SalesInvoiceItem.Create(
            productId: -1,
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
        var action = () => SalesInvoiceItem.Create(
            productId: 1,
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
        var action = () => SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: negativePrice
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*سعر الوحدة لا يمكن أن يكون سالباً*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeDiscountAmount_ShouldThrowDomainException(decimal negativeDiscount)
    {
        var action = () => SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m,
            discountAmount: negativeDiscount
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الخصم لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Create_GivenZeroUnitPrice_ShouldSucceed()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 0m
        );

        item.UnitPrice.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenNoDiscount_ShouldHaveZeroDiscount()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        item.DiscountAmount.Should().Be(0m);
        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void Create_GivenNoNotes_ShouldHaveNullNotes()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        item.Notes.Should().BeNull();
    }

    [Fact]
    public void RecalculateLineTotal_ShouldUpdateLineTotal()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 3,
            unitPrice: 50m,
            discountAmount: 25m
        );

        item.RecalculateLineTotal();

        item.LineTotal.Should().Be(125m); // (3 * 50) - 25
    }

    [Fact]
    public void Create_GivenLargeQuantity_ShouldCalculateCorrectly()
    {
        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1000m,
            unitPrice: 10m,
            discountAmount: 5m
        );

        item.LineTotal.Should().Be(9995m); // (1000 * 10) - 5
    }
}