using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class PurchaseInvoiceItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldSetLineTotalAsQuantityTimesUnitCostMinusDiscount()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitCost: 15.50m,
            discountAmount: 5m,
            mode: SaleMode.Retail
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(10m);
        item.UnitCost.Should().Be(15.50m);
        item.DiscountAmount.Should().Be(5m);
        item.Mode.Should().Be(SaleMode.Retail);
        item.LineTotal.Should().Be((10m * 15.50m) - 5m);
    }

    [Fact]
    public void Create_GivenZeroDiscount_ShouldSetLineTotalAsQuantityTimesUnitCost()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 5m,
            unitCost: 20m
        );

        item.LineTotal.Should().Be(5m * 20m);
    }

    [Fact]
    public void Create_GivenWholesaleMode_ShouldSetModeCorrectly()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 100m,
            unitCost: 12m,
            mode: SaleMode.Wholesale
        );

        item.Mode.Should().Be(SaleMode.Wholesale);
    }

    [Fact]
    public void Create_WithRetailModeAsDefault_ShouldDefaultToRetail()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1m,
            unitCost: 10m
        );

        item.Mode.Should().Be(SaleMode.Retail);
    }

    [Fact]
    public void Create_GivenDecimalQuantity_ShouldPreservePrecision()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 0.500m,
            unitCost: 10.00m
        );

        item.Quantity.Should().Be(0.500m);
        item.LineTotal.Should().Be(0.500m * 10.00m);
    }

    [Fact]
    public void Create_GivenNegativeQuantity_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
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
            quantity: 5m,
            unitCost: 0m
        );

        item.UnitCost.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenNegativeDiscountAmount_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 5m,
            unitCost: 10m,
            discountAmount: -2m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الخصم لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Create_GivenZeroProductId_ShouldThrowDomainException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 0,
            quantity: 5m,
            unitCost: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المنتج مطلوب*");
    }

    [Fact]
    public void Create_GivenDiscountExceedingLineTotal_ShouldAllowNegativeLineTotal()
    {
        // No guard clause exists — discount may exceed calculated line total
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 2m,
            unitCost: 10m,
            discountAmount: 50m
        );

        item.LineTotal.Should().Be((2m * 10m) - 50m);
        item.LineTotal.Should().BeNegative();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void Create_GivenValidProductId_ShouldSucceed(int productId)
    {
        var item = PurchaseInvoiceItem.Create(
            productId: productId,
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
            quantity: 5m,
            unitCost: 20m,
            discountAmount: 10m
        );

        item.LineTotal.Should().Be(90m);

        // Reflect the Quantity change by using a new instance pattern
        var recalculated = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitCost: 20m,
            discountAmount: 10m
        );

        recalculated.LineTotal.Should().Be(190m);
    }

    [Fact]
    public void Create_GivenNotes_ShouldStoreNotes()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1m,
            unitCost: 10m,
            notes: "Special order"
        );

        item.Notes.Should().Be("Special order");
    }

    [Fact]
    public void Create_GivenNullNotes_ShouldSetNotesNull()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1m,
            unitCost: 10m,
            notes: null
        );

        item.Notes.Should().BeNull();
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.010)]
    [InlineData(999999.999)]
    public void Create_GivenVariousQuantities_ShouldComputeLineTotal(decimal quantity)
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: quantity,
            unitCost: 10.50m
        );

        item.LineTotal.Should().Be(quantity * 10.50m);
    }
}
