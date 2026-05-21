using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SalesReturnTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateSalesReturn()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 5,
            salesInvoiceId: 10,
            notes: "Customer return",
            userId: 1
        );

        sr.ReturnNo.Should().Be("SR-2026-000001");
        sr.WarehouseId.Should().Be(1);
        sr.CustomerId.Should().Be(5);
        sr.SalesInvoiceId.Should().Be(10);
        sr.Notes.Should().Be("Customer return");
        sr.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidReturnNo_ShouldThrowDomainException(string? invalidReturnNo)
    {
        var action = () => SalesReturn.Create(returnNo: invalidReturnNo!, warehouseId: 1, customerId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الإرجاع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesReturn.Create(returnNo: "SR-001", warehouseId: 0, customerId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsNegative_ShouldThrowDomainException()
    {
        var action = () => SalesReturn.Create(returnNo: "SR-001", warehouseId: -1, customerId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void AddItem_GivenValidData_ShouldAddItemAndRecalculateTotals()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 2, unitPrice: 100m, discountAmount: 10m);

        sr.Items.Should().HaveCount(1);
        sr.SubTotal.Should().Be(190m); // (2 * 100) - 10
        sr.TotalAmount.Should().Be(190m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldSumLineTotalsCorrectly()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 1, unitPrice: 100m);
        sr.AddItem(productId: 2, quantity: 2, unitPrice: 50m, discountAmount: 5m);

        sr.Items.Should().HaveCount(2);
        sr.SubTotal.Should().Be(195m); // 100 + (2*50-5)
    }

    [Fact]
    public void AddItem_GivenZeroDiscount_ShouldCalculateCorrectly()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 3, unitPrice: 50m, discountAmount: 0m);

        sr.SubTotal.Should().Be(150m);
    }

    [Fact]
    public void RecalculateTotals_EmptyItems_ShouldSetTotalsToZero()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.RecalculateTotals();

        sr.SubTotal.Should().Be(0m);
        sr.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void Post_GivenDraftReturn_ShouldTransitionToPosted()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 1, unitPrice: 100m);
        sr.Post();

        sr.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyReturn_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        var action = () => sr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل مرتجع بدون أصناف.");
    }

    [Fact]
    public void Post_GivenAlreadyPostedReturn_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 1, unitPrice: 100m);
        sr.Post();

        var action = () => sr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط المرتجعات المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenDraftReturn_ShouldTransitionToCancelled()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.Cancel();

        sr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedReturn_ShouldTransitionToCancelled()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.AddItem(productId: 1, quantity: 1, unitPrice: 100m);
        sr.Post();
        sr.Cancel();

        sr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelled_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            userId: 1
        );

        sr.Cancel();

        var action = () => sr.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("المرتجع ملغى بالفعل.");
    }

    [Fact]
    public void Create_GivenNoCustomerId_ShouldBeNull()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: null,
            userId: 1
        );

        sr.CustomerId.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNoSalesInvoiceId_ShouldBeNull()
    {
        var sr = SalesReturn.Create(
            returnNo: "SR-2026-000001",
            warehouseId: 1,
            customerId: 1,
            salesInvoiceId: null,
            userId: 1
        );

        sr.SalesInvoiceId.Should().BeNull();
    }
}

public class SalesReturnItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 2,
            unitPrice: 100m,
            discountAmount: 10m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100m);
        item.DiscountAmount.Should().Be(10m);
        item.LineTotal.Should().Be(190m); // (2 * 100) - 10
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => SalesReturnItem.Create(
            productId: 0,
            quantity: 1,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Fact]
    public void Create_GivenProductIdIsNegative_ShouldThrowArgumentException()
    {
        var action = () => SalesReturnItem.Create(
            productId: -1,
            quantity: 1,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidQuantity_ShouldThrowArgumentException(decimal invalidQuantity)
    {
        var action = () => SalesReturnItem.Create(
            productId: 1,
            quantity: invalidQuantity,
            unitPrice: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية يجب أن تكون أكبر من الصفر.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeUnitPrice_ShouldThrowArgumentException(decimal negativePrice)
    {
        var action = () => SalesReturnItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: negativePrice
        );

        action.Should().Throw<DomainException>()
            .WithMessage("سعر الوحدة لا يمكن أن يكون سالباً.");
    }

    [Fact]
    public void Create_GivenZeroUnitPrice_ShouldSucceed()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 0m
        );

        item.UnitPrice.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenMinimumQuantity_ShouldSucceed()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 0.001m,
            unitPrice: 100m
        );

        item.Quantity.Should().Be(0.001m);
        item.LineTotal.Should().Be(0.1m);
    }

    [Fact]
    public void Create_GivenNoDiscount_ShouldHaveZeroDiscount()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );

        item.DiscountAmount.Should().Be(0m);
        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void Create_GivenZeroDiscount_ShouldCalculateCorrectly()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 3,
            unitPrice: 50m,
            discountAmount: 0m
        );

        item.LineTotal.Should().Be(150m);
    }

    [Fact]
    public void RecalculateLineTotal_ShouldUpdateLineTotal()
    {
        var item = SalesReturnItem.Create(
            productId: 1,
            quantity: 2,
            unitPrice: 100m,
            discountAmount: 10m
        );

        // Simulate price change
        item.RecalculateLineTotal();

        item.LineTotal.Should().Be(190m);
    }
}