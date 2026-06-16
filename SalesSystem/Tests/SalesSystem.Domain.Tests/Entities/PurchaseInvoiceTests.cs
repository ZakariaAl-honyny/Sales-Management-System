using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class PurchaseInvoiceTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreatePurchaseInvoice()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            invoiceDate: new DateOnly(2027, 1, 1),
            paymentType: PaymentType.Cash,
            discountAmount: 0,
            notes: "Test invoice",
            createdByUserId: 1
        );

        invoice.Id.Should().Be(0); // Id is assigned by DB, not entity constructor
        invoice.SupplierId.Should().Be(1);
        invoice.WarehouseId.Should().Be(1);
        invoice.PaymentType.Should().Be(PaymentType.Cash);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoice.Create(
            supplierId: 0,
            warehouseId: 1,
            invoiceNo: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المورد مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 0,
            invoiceNo: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void AddItem_GivenValidItem_ShouldAddItemAndRecalculateSubTotal()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item1 = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 2,
            unitPrice: 100m
        );

        var item2 = PurchaseInvoiceLine.Create(
            productId: 2,
            productUnitId: 1,
            quantity: 3,
            unitPrice: 50m
        );

        invoice.AddItem(item1);
        invoice.AddItem(item2);

        invoice.Items.Should().HaveCount(2);
        invoice.SubTotal.Should().Be(350m); // (2*100) + (3*50)
    }

    [Fact]
    public void RemoveItem_GivenValidItem_ShouldRemoveItemAndRecalculate()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item1 = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 2, unitPrice: 100m);
        var item2 = PurchaseInvoiceLine.Create(productId: 2, productUnitId: 1, quantity: 1, unitPrice: 50m);

        invoice.AddItem(item1);
        invoice.AddItem(item2);
        invoice.RemoveItem(item1);

        invoice.Items.Should().HaveCount(1);
        invoice.SubTotal.Should().Be(50m);
    }

    [Fact]
    public void AddItem_GivenNonDraftInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.AddItem(item);

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن إضافة أصناف لفاتورة غير مسودة.");
    }

    [Fact]
    public void RemoveItem_GivenNonDraftInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.RemoveItem(item);

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن حذف أصناف من فاتورة غير مسودة.");
    }

    [Fact]
    public void RecalculateTotals_WithTax_ShouldCalculateCorrectly()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            discountAmount: 50m,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 2,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.SetTaxAmount(30m);

        invoice.SubTotal.Should().Be(200m); // 2*100
        invoice.DiscountAmount.Should().Be(50m);
        invoice.TaxAmount.Should().Be(30m);
        invoice.NetTotal.Should().Be(180m); // 200 - 50 + 30
    }

    [Fact]
    public void SetPaidAmount_GivenAmountExceedingTotalAmount_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(
            productId: 1,
            productUnitId: 1,
            quantity: 1,
            unitPrice: 100m
        );
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.SetPaidAmount(150m);

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ المدفوع أكبر من الإجمالي*");
    }

    [Fact]
    public void SetPaidAmount_GivenNegativeAmount_ShouldThrowArgumentException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.SetPaidAmount(-10m);

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً.");
    }

    [Fact]
    public void SetPaidAmount_GivenValidAmount_ShouldSetPaidAmountAndRecalculateDue()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();
        invoice.SetTaxAmount(20m);

        invoice.SetPaidAmount(50m);

        invoice.PaidAmount.Should().Be(50m);
        invoice.RemainingAmount.Should().Be(70m); // 120 - 50
    }

    [Fact]
    public void SetTaxAmount_GivenNegativeTaxAmount_ShouldThrowArgumentException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var action = () => invoice.SetTaxAmount(-10m);

        action.Should().Throw<DomainException>()
            .WithMessage("الضريبة لا يمكن أن تكون سالبة.");
    }

    [Fact]
    public void Post_GivenDraftInvoice_ShouldTransitionToPosted()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);

        invoice.Post();

        invoice.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var action = () => invoice.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل فاتورة بدون أصناف.");
    }

    [Fact]
    public void Post_GivenAlreadyPostedInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        var action = () => invoice.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط الفواتير المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenPostedInvoice_ShouldTransitionToCancelled()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        invoice.Cancel();

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelledInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
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
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();
        invoice.SetPaidAmount(100m);

        var action = () => invoice.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن إلغاء فاتورة مدفوعة مباشرة.");
    }

    [Fact]
    public void UpdateTotals_GivenValidDiscountAndTax_ShouldUpdateAndRecalculate()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 2, unitPrice: 100m);
        invoice.AddItem(item);

        invoice.UpdateTotals(discountAmount: 50m, taxAmount: 30m);

        invoice.DiscountAmount.Should().Be(50m);
        invoice.TaxAmount.Should().Be(30m);
        invoice.NetTotal.Should().Be(180m); // SubTotal=200, 200 - 50 + 30 = 180
    }

    [Fact]
    public void NetTotal_Formula_ShouldBeSubTotalMinusDiscountPlusTax()
    {
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 1,
            discountAmount: 100m,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 1, unitPrice: 500m);
        invoice.AddItem(item);
        invoice.SetTaxAmount(60m);

        invoice.SubTotal.Should().Be(500m);
        invoice.NetTotal.Should().Be(460m); // 500 - 100 + 60
    }
}