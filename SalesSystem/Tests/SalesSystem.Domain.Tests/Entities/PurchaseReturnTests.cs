using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class PurchaseReturnTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreatePurchaseReturn()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 5,
            purchaseInvoiceId: 10,
            notes: "Supplier return",
            userId: 1
        );

        pr.ReturnNo.Should().Be(1);
        pr.WarehouseId.Should().Be(1);
        pr.SupplierId.Should().Be(5);
        pr.PurchaseInvoiceId.Should().Be(10);
        pr.Notes.Should().Be("Supplier return");
        pr.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_GivenInvalidReturnNo_ShouldThrowDomainException(int invalidReturnNo)
    {
        var action = () => PurchaseReturn.Create(returnNo: invalidReturnNo, warehouseId: (short)1, supplierId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الإرجاع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturn.Create(returnNo: 1, warehouseId: 0, supplierId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturn.Create(returnNo: 1, warehouseId: (short)1, supplierId: 0);

        action.Should().Throw<DomainException>()
            .WithMessage("المورد مطلوب.");
    }

    [Fact]
    public void AddItem_GivenValidData_ShouldAddItemAndRecalculateTotals()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.AddItem(productId: 1, productUnitId: 1, quantity: 2, unitCost: 100m);

        pr.Items.Should().HaveCount(1);
        pr.SubTotal.Should().Be(200m); // 2 * 100
        pr.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldSumLineTotalsCorrectly()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.AddItem(productId: 1, productUnitId: 1, quantity: 1, unitCost: 100m);
        pr.AddItem(productId: 2, productUnitId: 1, quantity: 2, unitCost: 50m);

        pr.Items.Should().HaveCount(2);
        pr.SubTotal.Should().Be(200m); // 100 + (2*50)
    }

    [Fact]
    public void RecalculateTotals_EmptyItems_ShouldSetTotalsToZero()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.RecalculateTotals();

        pr.SubTotal.Should().Be(0m);
        pr.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void Post_GivenDraftReturn_ShouldTransitionToPosted()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.AddItem(productId: 1, productUnitId: 1, quantity: 1, unitCost: 100m);
        pr.Post();

        pr.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyReturn_ShouldThrowDomainException()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        var action = () => pr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل مرتجع بدون أصناف.");
    }

    [Fact]
    public void Post_GivenAlreadyPostedReturn_ShouldThrowDomainException()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.AddItem(productId: 1, productUnitId: 1, quantity: 1, unitCost: 100m);
        pr.Post();

        var action = () => pr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط المرتجعات المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenDraftReturn_ShouldTransitionToCancelled()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.Cancel();

        pr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedReturn_ShouldTransitionToCancelled()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.AddItem(productId: 1, productUnitId: 1, quantity: 1, unitCost: 100m);
        pr.Post();
        pr.Cancel();

        pr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelled_ShouldThrowDomainException()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            userId: 1
        );

        pr.Cancel();

        var action = () => pr.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("المرتجع ملغى بالفعل.");
    }

    [Fact]
    public void Create_GivenNoPurchaseInvoiceId_ShouldBeNull()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            warehouseId: (short)1,
            supplierId: 1,
            purchaseInvoiceId: null,
            userId: 1
        );

        pr.PurchaseInvoiceId.Should().BeNull();
    }
}

public class PurchaseReturnItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = PurchaseReturnItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 2,
            unitCost: 100m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(2);
        item.UnitCost.Should().Be(100m);
        item.LineTotal.Should().Be(200m);
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseReturnItem.Create(
            productId: 0,
            productUnitId: 1,
            quantity: 1,
            unitCost: 100m
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
        var action = () => PurchaseReturnItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: invalidQuantity,
            unitCost: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية يجب أن تكون أكبر من الصفر.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeUnitCost_ShouldThrowArgumentException(decimal negativeCost)
    {
        var action = () => PurchaseReturnItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitCost: negativeCost
        );

        action.Should().Throw<DomainException>()
            .WithMessage("تكلفة الوحدة لا يمكن أن تكون سالبة.");
    }

    [Fact]
    public void Create_GivenZeroUnitCost_ShouldSucceed()
    {
        var item = PurchaseReturnItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitCost: 0m
        );

        item.UnitCost.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void RecalculateLineTotal_ShouldUpdateLineTotal()
    {
        var item = PurchaseReturnItem.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 3,
            unitCost: 50m
        );

        item.RecalculateLineTotal();

        item.LineTotal.Should().Be(150m);
    }
}