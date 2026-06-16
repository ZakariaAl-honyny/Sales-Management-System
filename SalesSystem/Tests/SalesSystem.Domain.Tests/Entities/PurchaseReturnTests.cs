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
            supplierId: 5,
            warehouseId: (short)1,
            currencyId: (short)1,
            purchaseInvoiceId: 20,
            notes: "Supplier return"
        );

        pr.ReturnNo.Should().Be(1);
        pr.WarehouseId.Should().Be(1);
        pr.SupplierId.Should().Be(5);
        pr.PurchaseInvoiceId.Should().Be(20);
        pr.Notes.Should().Be("Supplier return");
        pr.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_GivenInvalidReturnNo_ShouldThrowDomainException(int invalidReturnNo)
    {
        var action = () => PurchaseReturn.Create(
            returnNo: invalidReturnNo,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الإرجاع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: 0,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 0,
            warehouseId: (short)1,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("المورد مطلوب.");
    }

    [Fact]
    public void AddLine_GivenValidData_ShouldAddLineAndRecalculateTotal()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        var line = PurchaseReturnLine.Create(productId: 1, productUnitId: 1, quantity: 2m, amount: 200m, purchaseInvoiceLineId: 5);
        pr.AddLine(line);

        pr.Lines.Should().HaveCount(1);
        pr.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public void AddLine_MultipleLines_ShouldSumAmountsCorrectly()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.AddLine(PurchaseReturnLine.Create(productId: 1, productUnitId: 1, quantity: 1m, amount: 100m, purchaseInvoiceLineId: 1));
        pr.AddLine(PurchaseReturnLine.Create(productId: 2, productUnitId: 1, quantity: 2m, amount: 100m, purchaseInvoiceLineId: 2));

        pr.Lines.Should().HaveCount(2);
        pr.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public void RecalculateTotals_EmptyLines_ShouldSetTotalToZero()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.RecalculateTotals();

        pr.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void Post_GivenDraftReturn_ShouldTransitionToPosted()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.AddLine(PurchaseReturnLine.Create(productId: 1, productUnitId: 1, quantity: 1m, amount: 100m));
        pr.Post();

        pr.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyReturn_ShouldThrowDomainException()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
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
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.AddLine(PurchaseReturnLine.Create(productId: 1, productUnitId: 1, quantity: 1m, amount: 100m));
        pr.Post();

        var action = () => pr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط مرتجعات المشتريات المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenDraftReturn_ShouldTransitionToCancelled()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.Cancel();

        pr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedReturn_ShouldTransitionToCancelled()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.AddLine(PurchaseReturnLine.Create(productId: 1, productUnitId: 1, quantity: 1m, amount: 100m));
        pr.Post();
        pr.Cancel();

        pr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelled_ShouldThrowDomainException()
    {
        var pr = PurchaseReturn.Create(
            returnNo: 1,
            supplierId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        pr.Cancel();

        var action = () => pr.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("مرتجع المشتريات ملغي بالفعل.");
    }
}

public class PurchaseReturnLineTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateLine()
    {
        var line = PurchaseReturnLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 2m,
            amount: 200m,
            purchaseInvoiceLineId: 5
        );

        line.ProductId.Should().Be(1);
        line.ProductUnitId.Should().Be(1);
        line.PurchaseInvoiceLineId.Should().Be(5);
        line.Quantity.Should().Be(2m);
        line.Amount.Should().Be(200m);
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturnLine.Create(
            productId: 0,
            productUnitId: 1,
            quantity: 1m,
            amount: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Fact]
    public void Create_GivenProductUnitIdIsZero_ShouldThrowDomainException()
    {
        var action = () => PurchaseReturnLine.Create(
            productId: 1,
            productUnitId: 0,
            quantity: 1m,
            amount: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الوحدة مطلوبة.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidQuantity_ShouldThrowDomainException(decimal invalidQuantity)
    {
        var action = () => PurchaseReturnLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: invalidQuantity,
            amount: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية يجب أن تكون أكبر من الصفر.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeAmount_ShouldThrowDomainException(decimal negativeAmount)
    {
        var action = () => PurchaseReturnLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1m,
            amount: negativeAmount
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ لا يمكن أن يكون سالباً.");
    }

    [Fact]
    public void Create_GivenZeroAmount_ShouldSucceed()
    {
        var line = PurchaseReturnLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1m,
            amount: 0m
        );

        line.Amount.Should().Be(0m);
    }
}
