using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SalesInvoiceTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateInvoice()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            customerId: 1,
            invoiceDate: new DateTime(2027, 1, 1),
            dueDate: new DateOnly(2027, 1, 31),
            paymentType: PaymentType.Cash,
            discountAmount: 0,
            notes: "Test invoice",
            createdByUserId: 1
        );

        invoice.InvoiceNo.Should().Be("INV-2026-000001");
        invoice.WarehouseId.Should().Be(1);
        invoice.CustomerId.Should().Be(1);
        invoice.InvoiceDate.Should().Be(new DateTime(2027, 1, 1));
        invoice.DueDate.Should().Be(new DateOnly(2027, 1, 31));
        invoice.PaymentType.Should().Be(PaymentType.Cash);
        invoice.DiscountAmount.Should().Be(0);
        invoice.Notes.Should().Be("Test invoice");
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void AddItem_GivenValidItem_ShouldAddItemAndRecalculateSubTotal()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item1 = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitPrice: 100m,
            discountAmount: 10m
        );

        var item2 = SalesInvoiceItem.Create(
            productId: 2,
            quantity: 3,
            unitPrice: 50m,
            discountAmount: 5m
        );

        invoice.AddItem(item1);
        invoice.AddItem(item2);

        invoice.Items.Should().HaveCount(2);
        invoice.SubTotal.Should().Be(335m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldSumLineTotalsCorrectly()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 5,
            unitPrice: 200m,
            discountAmount: 50m
        );

        invoice.AddItem(item);

        invoice.SubTotal.Should().Be(950m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetPaidAmount_GivenNegativeAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.SetPaidAmount(invalidAmount);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetPaidAmount_GivenAmountExceedingTotalAmount_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.SetPaidAmount(150m);

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ المدفوع أكبر من الإجمالي.");
    }

    [Fact]
    public void SetPaidAmount_GivenValidAmount_ShouldSetPaidAmount()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        invoice.SetPaidAmount(50m);

        invoice.PaidAmount.Should().Be(50m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetTaxAmount_GivenNegativeTaxAmount_ShouldThrowArgumentException(decimal negativeTax)
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var action = () => invoice.SetTaxAmount(negativeTax);

        action.Should().Throw<DomainException>()
            .WithMessage("الضريبة لا يمكن أن تكون سالبة.");
    }

    [Fact]
    public void SetTaxAmount_GivenValidTaxAmount_ShouldSetTaxAmount()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);

        invoice.SetTaxAmount(15m);

        invoice.TaxAmount.Should().Be(15m);
        invoice.TotalAmount.Should().Be(115m);
    }

    [Fact]
    public void Post_GivenDraftInvoice_ShouldTransitionToPosted()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);

        invoice.Post();

        invoice.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenInvoiceWithNoItems_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var action = () => invoice.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل فاتورة بدون أصناف.");
    }

    [Fact]
    public void Cancel_GivenPostedInvoice_ShouldTransitionToCancelled()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        invoice.Cancel();

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenDraftInvoice_ShouldSucceed()
    {
        // Draft invoices can be cancelled (no stock/balance to reverse)
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);

        var action = () => invoice.Cancel();

        action.Should().NotThrow();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedInvoiceWithPaidAmount_ShouldThrowDomainException()
    {
        // Posted invoices with PaidAmount > 0 cannot be cancelled directly
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.SetPaidAmount(50m); // Partial payment
        invoice.Post();

        var action = () => invoice.Cancel();

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelledInvoice_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();
        invoice.Cancel();

        var action = () => invoice.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("الفاتورة ملغاة بالفعل.");
    }

    [Fact]
    public void Cancel_GivenPaidInvoice_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();
        invoice.SetPaidAmount(100m);

        var action = () => invoice.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن إلغاء فاتورة مدفوعة مباشرة.");
    }

    [Fact]
    public void RecalculateTotals_GivenItemsAndDiscountAndTax_ShouldCalculateCorrectly()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            discountAmount: 50m,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitPrice: 100m,
            discountAmount: 20m
        );
        invoice.AddItem(item);

        invoice.SetTaxAmount(30m);

        invoice.SubTotal.Should().Be(180m);
        invoice.DiscountAmount.Should().Be(50m);
        invoice.TaxAmount.Should().Be(30m);
        invoice.TotalAmount.Should().Be(160m);
    }

    [Fact]
    public void RecalculateTotals_TotalAmountFormula_ShouldBeSubTotalMinusDiscountPlusTax()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            discountAmount: 100m,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 500m
        );
        invoice.AddItem(item);

        invoice.SetTaxAmount(60m);

        invoice.SubTotal.Should().Be(500m);
        invoice.TotalAmount.Should().Be(460m);
    }

    [Fact]
    public void SetPaidAmount_ShouldRecalculateDueAmount()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 200m
        );
        invoice.AddItem(item);
        invoice.SetTaxAmount(20m);

        invoice.SetPaidAmount(100m);

        invoice.TotalAmount.Should().Be(220m);
        invoice.PaidAmount.Should().Be(100m);
        invoice.DueAmount.Should().Be(120m);
    }

    [Fact]
    public void Create_GivenInvoiceNoIsEmpty_ShouldThrowArgumentException()
    {
        var action = () => SalesInvoice.Create(
            invoiceNo: "",
            warehouseId: 1,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الفاتورة مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 0,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsNegative_ShouldThrowArgumentException()
    {
        var action = () => SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: -1,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void AddItem_GivenNonDraftInvoice_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.AddItem(item);

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن إضافة أصناف لفاتورة غير مسودة.");
    }

    [Fact]
    public void RemoveItem_GivenNonDraftInvoice_ShouldThrowDomainException()
    {
        var invoice = SalesInvoice.Create(
            invoiceNo: "INV-2026-000001",
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = SalesInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.RemoveItem(item);

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن حذف أصناف من فاتورة غير مسودة.");
    }
}