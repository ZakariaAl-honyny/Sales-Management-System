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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            invoiceDate: new DateTime(2027, 1, 1),
            dueDate: new DateOnly(2027, 1, 31),
            paymentType: PaymentType.Cash,
            discountAmount: 0,
            notes: "Test invoice",
            createdByUserId: 1
        );

        invoice.InvoiceNo.Should().Be("PUR-2026-000001");
        invoice.SupplierId.Should().Be(1);
        invoice.WarehouseId.Should().Be(1);
        invoice.PaymentType.Should().Be(PaymentType.Cash);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void Create_GivenInvoiceNoIsEmpty_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoice.Create(
            invoiceNo: "",
            supplierId: 1,
            warehouseId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("رقم الفاتورة مطلوب.");
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoice.Create(
            invoiceNo: "PUR-001",
            supplierId: 0,
            warehouseId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المورد مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoice.Create(
            invoiceNo: "PUR-001",
            supplierId: 1,
            warehouseId: 0
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Fact]
    public void AddItem_GivenValidItem_ShouldAddItemAndRecalculateSubTotal()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item1 = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitCost: 100m,
            discountAmount: 10m
        );

        var item2 = PurchaseInvoiceItem.Create(
            productId: 2,
            quantity: 3,
            unitCost: 50m,
            discountAmount: 5m
        );

        invoice.AddItem(item1);
        invoice.AddItem(item2);

        invoice.Items.Should().HaveCount(2);
        invoice.SubTotal.Should().Be(335m); // (2*100-10) + (3*50-5)
    }

    [Fact]
    public void RemoveItem_GivenValidItem_ShouldRemoveItemAndRecalculate()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item1 = PurchaseInvoiceItem.Create(productId: 1, quantity: 2, unitCost: 100m);
        var item2 = PurchaseInvoiceItem.Create(productId: 2, quantity: 1, unitCost: 50m);

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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            discountAmount: 50m,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitCost: 100m,
            discountAmount: 20m
        );
        invoice.AddItem(item);
        invoice.SetTaxAmount(30m);

        invoice.SubTotal.Should().Be(180m);
        invoice.DiscountAmount.Should().Be(50m);
        invoice.TaxAmount.Should().Be(30m);
        invoice.TotalAmount.Should().Be(160m); // 180 - 50 + 30
    }

    [Fact]
    public void SetPaidAmount_GivenAmountExceedingTotalAmount_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitCost: 100m
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
        invoice.AddItem(item);
        invoice.Post();
        invoice.SetTaxAmount(20m);

        invoice.SetPaidAmount(50m);

        invoice.PaidAmount.Should().Be(50m);
        invoice.DueAmount.Should().Be(70m); // 120 - 50
    }

    [Fact]
    public void SetTaxAmount_GivenNegativeTaxAmount_ShouldThrowArgumentException()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
        invoice.AddItem(item);

        invoice.Post();

        invoice.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
        invoice.AddItem(item);
        invoice.Post();

        invoice.Cancel();

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelledInvoice_ShouldThrowDomainException()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m);
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
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 2, unitCost: 100m);
        invoice.AddItem(item);

        invoice.UpdateTotals(discountAmount: 50m, taxAmount: 30m);

        invoice.DiscountAmount.Should().Be(50m);
        invoice.TaxAmount.Should().Be(30m);
        invoice.TotalAmount.Should().Be(180m); // SubTotal=200, 200 - 50 + 30 = 180
    }

    [Fact]
    public void TotalAmount_Formula_ShouldBeSubTotalMinusDiscountPlusTax()
    {
        var invoice = PurchaseInvoice.Create(
            invoiceNo: "PUR-2026-000001",
            supplierId: 1,
            warehouseId: 1,
            discountAmount: 100m,
            createdByUserId: 1
        );

        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 500m);
        invoice.AddItem(item);
        invoice.SetTaxAmount(60m);

        invoice.SubTotal.Should().Be(500m);
        invoice.TotalAmount.Should().Be(460m); // 500 - 100 + 60
    }
}

public class PurchaseInvoiceItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 2,
            unitCost: 100m,
            discountAmount: 10m
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(2);
        item.UnitCost.Should().Be(100m);
        item.DiscountAmount.Should().Be(10m);
        item.LineTotal.Should().Be(190m); // (2 * 100) - 10
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 0,
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
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
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
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitCost: negativeCost
        );

        action.Should().Throw<DomainException>()
            .WithMessage("تكلفة الوحدة لا يمكن أن تكون سالبة.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeDiscountAmount_ShouldThrowArgumentException(decimal negativeDiscount)
    {
        var action = () => PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitCost: 100m,
            discountAmount: negativeDiscount
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الخصم لا يمكن أن يكون سالباً.");
    }

    [Fact]
    public void Create_GivenZeroUnitCost_ShouldSucceed()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitCost: 0m
        );

        item.UnitCost.Should().Be(0m);
        item.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenNoDiscount_ShouldHaveZeroDiscount()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 1,
            unitCost: 100m
        );

        item.DiscountAmount.Should().Be(0m);
        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void RecalculateLineTotal_ShouldUpdateLineTotal()
    {
        var item = PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 3,
            unitCost: 50m,
            discountAmount: 25m
        );

        item.RecalculateLineTotal();

        item.LineTotal.Should().Be(125m); // (3 * 50) - 25
    }

    [Fact]
    public void Create_VariousCombinations_ShouldCalculateCorrectly()
    {
        var item1 = PurchaseInvoiceItem.Create(productId: 1, quantity: 1, unitCost: 100m, discountAmount: 0);
        item1.LineTotal.Should().Be(100m);

        var item2 = PurchaseInvoiceItem.Create(productId: 1, quantity: 2, unitCost: 100m, discountAmount: 10m);
        item2.LineTotal.Should().Be(190m);

        var item3 = PurchaseInvoiceItem.Create(productId: 1, quantity: 5, unitCost: 50m, discountAmount: 5m);
        item3.LineTotal.Should().Be(245m);
    }
}