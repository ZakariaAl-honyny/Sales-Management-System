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
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 5,
            warehouseId: (short)1,
            currencyId: (short)1,
            notes: "Customer return"
        );

        sr.ReturnNo.Should().Be(1);
        sr.WarehouseId.Should().Be(1);
        sr.CustomerId.Should().Be(5);
        sr.SalesInvoiceId.Should().Be(10);
        sr.Notes.Should().Be("Customer return");
        sr.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_GivenInvalidReturnNo_ShouldThrowDomainException(int invalidReturnNo)
    {
        var action = () => SalesReturn.Create(
            returnNo: invalidReturnNo,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الإرجاع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: 0,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenSalesInvoiceIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 0,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1);

        action.Should().Throw<DomainException>()
            .WithMessage("فاتورة المبيعات الأصلية مطلوبة.");
    }

    [Fact]
    public void AddLine_GivenValidData_ShouldAddLineAndRecalculateTotal()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        var line = SalesReturnLine.Create(salesInvoiceLineId: 5, quantity: 2m, amount: 190m);
        sr.AddLine(line);

        sr.Lines.Should().HaveCount(1);
        sr.TotalAmount.Should().Be(190m);
    }

    [Fact]
    public void AddLine_MultipleLines_ShouldSumAmountsCorrectly()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.AddLine(SalesReturnLine.Create(salesInvoiceLineId: 1, quantity: 1m, amount: 100m));
        sr.AddLine(SalesReturnLine.Create(salesInvoiceLineId: 2, quantity: 2m, amount: 95m));

        sr.Lines.Should().HaveCount(2);
        sr.TotalAmount.Should().Be(195m);
    }

    [Fact]
    public void RecalculateTotals_EmptyLines_ShouldSetTotalToZero()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.RecalculateTotals();

        sr.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void Post_GivenDraftReturn_ShouldTransitionToPosted()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.AddLine(SalesReturnLine.Create(1, 1m, 100m));
        sr.Post();

        sr.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyReturn_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        var action = () => sr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل مرتجع بدون أصناف.");
    }

    [Fact]
    public void Post_GivenAlreadyPostedReturn_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.AddLine(SalesReturnLine.Create(1, 1m, 100m));
        sr.Post();

        var action = () => sr.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط مرتجعات المبيعات المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenDraftReturn_ShouldTransitionToCancelled()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.Cancel();

        sr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedReturn_ShouldTransitionToCancelled()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.AddLine(SalesReturnLine.Create(1, 1m, 100m));
        sr.Post();
        sr.Cancel();

        sr.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelled_ShouldThrowDomainException()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.Cancel();

        var action = () => sr.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("مرتجع المبيعات ملغي بالفعل.");
    }

    [Fact]
    public void Create_GivenSalesInvoiceIdRequired_ShouldNotBeNull()
    {
        var sr = SalesReturn.Create(
            returnNo: 1,
            salesInvoiceId: 10,
            customerId: 1,
            warehouseId: (short)1,
            currencyId: (short)1
        );

        sr.SalesInvoiceId.Should().Be(10);
    }
}

public class SalesReturnLineTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateLine()
    {
        var line = SalesReturnLine.Create(
            salesInvoiceLineId: 5,
            quantity: 2m,
            amount: 190m
        );

        line.SalesInvoiceLineId.Should().Be(5);
        line.Quantity.Should().Be(2m);
        line.Amount.Should().Be(190m);
    }

    [Fact]
    public void Create_GivenSalesInvoiceLineIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SalesReturnLine.Create(
            salesInvoiceLineId: 0,
            quantity: 1m,
            amount: 100m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("رقم بند الفاتورة الأصلي مطلوب.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidQuantity_ShouldThrowDomainException(decimal invalidQuantity)
    {
        var action = () => SalesReturnLine.Create(
            salesInvoiceLineId: 1,
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
        var action = () => SalesReturnLine.Create(
            salesInvoiceLineId: 1,
            quantity: 1m,
            amount: negativeAmount
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ لا يمكن أن يكون سالباً.");
    }

    [Fact]
    public void Create_GivenZeroAmount_ShouldSucceed()
    {
        var line = SalesReturnLine.Create(
            salesInvoiceLineId: 1,
            quantity: 1m,
            amount: 0m
        );

        line.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenMinimumQuantity_ShouldSucceed()
    {
        var line = SalesReturnLine.Create(
            salesInvoiceLineId: 1,
            quantity: 0.001m,
            amount: 100m
        );

        line.Quantity.Should().Be(0.001m);
    }
}
