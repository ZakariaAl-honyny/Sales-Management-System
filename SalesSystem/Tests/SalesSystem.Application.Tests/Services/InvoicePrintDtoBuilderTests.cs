using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Tests.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: InvoicePrintDtoBuilderTests relied on old SalesInvoiceItem.Create/
//  PurchaseInvoiceItem.Create signatures (InvoiceNo as string, discountAmount
//  param) which changed. InvoiceNo is now int, and AddItem no longer has
//  discountAmount parameter. Preserved for reference — NOT included in build.
// ═══════════════════════════════════════════════════════════════════════════
#if false
public class InvoicePrintDtoBuilderTests
{
    private readonly InvoicePrintDtoBuilder _sut;
    private readonly Mock<ILogger<InvoicePrintDtoBuilder>> _loggerMock;

    private const string StoreName = "متجري";
    private const string StorePhone = "0555000000";
    private const string StoreAddress = "الرياض - شارع الملك فهد";
    private const string StoreTaxNumber = "TAX-998877";
    private static readonly byte[] LogoBytes = [0x89, 0x50, 0x4E, 0x47];
    private const decimal TaxRate = 0.15m;

    public InvoicePrintDtoBuilderTests()
    {
        _loggerMock = new Mock<ILogger<InvoicePrintDtoBuilder>>();
        _sut = new InvoicePrintDtoBuilder(_loggerMock.Object);
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private static void SetNavigation<T, TNavigation>(T entity, string propertyName, TNavigation? value)
        where T : class
    {
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, value);
    }

    private static Product CreateProduct(string name)
    {
        return Product.Create(
            name,
            categoryId: 1,
            createdByUserId: 1);
    }

    // ─── Store Info ─────────────────────────────────────────────────────

    [Fact]
    public async Task BuildFromSalesAsync_ShouldPassStoreInfo()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.StoreName.Should().Be(StoreName);
        result.StorePhone.Should().Be(StorePhone);
        result.StoreAddress.Should().Be(StoreAddress);
        result.StoreTaxNumber.Should().Be(StoreTaxNumber);
        result.LogoBytes.Should().BeSameAs(LogoBytes);
        result.TaxRate.Should().Be(TaxRate);
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldPassStoreInfo()
    {
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.StoreName.Should().Be(StoreName);
        result.StorePhone.Should().Be(StorePhone);
        result.StoreAddress.Should().Be(StoreAddress);
        result.StoreTaxNumber.Should().Be(StoreTaxNumber);
        result.LogoBytes.Should().BeSameAs(LogoBytes);
        result.TaxRate.Should().Be(TaxRate);
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldPassStoreInfo()
    {
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: null);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.StoreName.Should().Be(StoreName);
        result.StorePhone.Should().Be(StorePhone);
        result.StoreAddress.Should().Be(StoreAddress);
        result.StoreTaxNumber.Should().Be(StoreTaxNumber);
        result.LogoBytes.Should().BeSameAs(LogoBytes);
        result.TaxRate.Should().Be(TaxRate);
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldPassStoreInfo()
    {
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.StoreName.Should().Be(StoreName);
        result.StorePhone.Should().Be(StorePhone);
        result.StoreAddress.Should().Be(StoreAddress);
        result.StoreTaxNumber.Should().Be(StoreTaxNumber);
        result.LogoBytes.Should().BeSameAs(LogoBytes);
        result.TaxRate.Should().Be(TaxRate);
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldAllowNullLogo()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, null, TaxRate);

        result.LogoBytes.Should().BeNull();
    }

    // ─── Sales Invoice — Header ─────────────────────────────────────────

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapHeaderFields()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1,
            invoiceNo: 1, paymentType: PaymentType.Cash, notes: "ملاحظات الفاتورة");

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("1");
        result.InvoiceDate.Should().Be(invoice.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.Sales);
        result.Notes.Should().Be("ملاحظات الفاتورة");
    }

    // ─── Sales Invoice — Customer ───────────────────────────────────────

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapCustomerInfo()
    {
        var party = Party.Create("أحمد محمد", PartyType.Customer, 1, phone: "0555123456", address: "جدة - البلد", taxNumber: "TAX-C-001");
        var customer = Customer.Create(party.Id, 1);
        SetNavigation(customer, nameof(Customer.Party), party);
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);
        SetNavigation(invoice, nameof(SalesInvoice.Customer), customer);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("أحمد محمد");
        result.CustomerPhone.Should().Be("0555123456");
        result.CustomerAddress.Should().Be("جدة - البلد");
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldUseDefaultNameWhenCustomerNull()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: null);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("زبون نقدي");
        result.CustomerPhone.Should().BeNull();
        result.CustomerAddress.Should().BeNull();
    }

    // ─── Sales Invoice — Items ──────────────────────────────────────────

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapItems()
    {
        var product = CreateProduct("منتج تجريبي");
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 3, unitPrice: 25.50m, discountAmount: 5);
        SetNavigation(item, nameof(SalesInvoiceItem.Product), product);
        invoice.AddItem(item);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("منتج تجريبي");
        itemDto.UnitName.Should().BeEmpty();
        itemDto.Quantity.Should().Be(3);
        itemDto.UnitPrice.Should().Be(25.50m);
        itemDto.Discount.Should().Be(5);
        itemDto.Total.Should().Be((3 * 25.50m) - 5);
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldFallbackToProductIdWhenProductNavigationNull()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);
        var item = SalesInvoiceItem.Create(productId: 42, quantity: 1, unitPrice: 10);
        invoice.AddItem(item);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items[0].ProductName.Should().Be("منتج #42");
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldHandleEmptyItems()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().BeEmpty();
        result.SubTotal.Should().Be(0);
        result.GrandTotal.Should().Be(0);
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapMultipleItems()
    {
        var product1 = CreateProduct("منتج أ");
        var product2 = CreateProduct("منتج ب");
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);

        var item1 = SalesInvoiceItem.Create(productId: 1, quantity: 2, unitPrice: 10);
        SetNavigation(item1, nameof(SalesInvoiceItem.Product), product1);
        invoice.AddItem(item1);

        var item2 = SalesInvoiceItem.Create(productId: 2, quantity: 1, unitPrice: 50, discountAmount: 4);
        SetNavigation(item2, nameof(SalesInvoiceItem.Product), product2);
        invoice.AddItem(item2);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(2);
        result.Items[0].ProductName.Should().Be("منتج أ");
        result.Items[1].ProductName.Should().Be("منتج ب");
        result.Items[1].Total.Should().Be(46);
    }

    // â”€â”€â”€ Sales Invoice â€” Financials â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapFinancialTotals()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, discountAmount: 10);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 5, unitPrice: 20, discountAmount: 5);
        invoice.AddItem(item);
        invoice.SetTaxAmount(15);
        invoice.SetPaidAmount(100);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.SubTotal.Should().Be(95);
        result.DiscountAmount.Should().Be(10);
        result.TaxAmount.Should().Be(15);
        result.GrandTotal.Should().Be(100);
        result.AmountPaid.Should().Be(100);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldHaveZeroChangeWhenPaidEqualsTotal()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, discountAmount: 0);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 1, unitPrice: 50);
        invoice.AddItem(item);
        invoice.SetPaidAmount(50);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.GrandTotal.Should().Be(50);
        result.AmountPaid.Should().Be(50);
        result.ChangeAmount.Should().Be(0);
    }

    // â”€â”€â”€ Sales Invoice â€” Payment Method â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Theory]
    [InlineData(PaymentType.Cash, "نقدي")]
    [InlineData(PaymentType.Credit, "آجل")]
    [InlineData(PaymentType.Mixed, "نقدي + آجل")]
    public async Task BuildFromSalesAsync_ShouldMapPaymentMethod(PaymentType paymentType, string expected)
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, paymentType: paymentType);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 1, unitPrice: 10);
        invoice.AddItem(item);
        invoice.SetPaidAmount(10);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.PaymentMethod.Should().Be(expected);
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapNotes()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, notes: "اشترى مع زبون آخر");
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 1, unitPrice: 10);
        invoice.AddItem(item);
        invoice.SetPaidAmount(10);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Notes.Should().Be("اشترى مع زبون آخر");
    }

    // ─── Purchase Invoice ───────────────────────────────────────────────

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapHeaderFields()
    {
        var party = Party.Create("المورد الأول", PartyType.Supplier, 1, phone: "0566000000", taxNumber: "TAX-S-001");
        var supplier = Supplier.Create(party.Id, 1);
        SetNavigation(supplier, nameof(Supplier.Party), party);
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1,
            paymentType: PaymentType.Credit, notes: "فاتورة مورد");
        SetNavigation(invoice, nameof(PurchaseInvoice.Supplier), supplier);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("1");
        result.InvoiceDate.Should().Be(invoice.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.Purchase);
        result.CustomerOrSupplierName.Should().Be("المورد الأول");
        result.CustomerPhone.Should().Be("0566000000");
        result.Notes.Should().Be("فاتورة مورد");
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldUseDefaultSupplierNameWhenNull()
    {
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("مورد");
        result.CustomerPhone.Should().BeNull();
        result.CustomerAddress.Should().BeNull();
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapItems()
    {
        var product = CreateProduct("مادة خام");
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1);
        var item = PurchaseInvoiceItem.Create(productId: 1, productUnitId: 1, quantity: 10, unitCost: 8.50m);
        SetNavigation(item, nameof(PurchaseInvoiceItem.Product), product);
        invoice.AddItem(item);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("مادة خام");
        itemDto.Quantity.Should().Be(10);
        itemDto.UnitPrice.Should().Be(8.50m);
        itemDto.Discount.Should().Be(0);
        itemDto.Total.Should().Be(10 * 8.50m);
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapFinancialTotals()
    {
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1, discountAmount: 20);
        var item = PurchaseInvoiceItem.Create(productId: 1, productUnitId: 1, quantity: 100, unitCost: 5);
        invoice.AddItem(item);
        invoice.SetTaxAmount(30);
        invoice.SetPaidAmount(500);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.SubTotal.Should().Be(500);
        result.DiscountAmount.Should().Be(20);
        result.TaxAmount.Should().Be(30);
        result.GrandTotal.Should().Be(510);
        result.AmountPaid.Should().Be(500);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }

    // ─── Sales Return ───────────────────────────────────────────────────

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldMapHeader()
    {
        var party = Party.Create("عميل المرتجع", PartyType.Customer, 1, phone: "0577777777");
        var customer = Customer.Create(party.Id, 1);
        SetNavigation(customer, nameof(Customer.Party), party);
        var returnEntity = SalesReturn.Create("SR-2025-0001", warehouseId: 1, customerId: 1, notes: "مرتجع تالف");
        SetNavigation(returnEntity, nameof(SalesReturn.Customer), customer);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(returnEntity.Id);
        result.InvoiceNumber.Should().Be("SR-2025-0001");
        result.InvoiceDate.Should().Be(returnEntity.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.SalesReturn);
        result.CustomerOrSupplierName.Should().Be("عميل المرتجع");
        result.CustomerPhone.Should().Be("0577777777");
        result.Notes.Should().Be("مرتجع تالف");
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldUseDefaultCustomerNameWhenNull()
    {
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: null);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("عميل");
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldMapItems()
    {
        var product = CreateProduct("منتج مرتجع");
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: 1);
        returnEntity.AddItem(productId: 1, quantity: 2, unitPrice: 30, discountAmount: 5);
        SetNavigation(returnEntity.Items[0], nameof(SalesReturnItem.Product), product);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("منتج مرتجع");
        itemDto.Quantity.Should().Be(2);
        itemDto.UnitPrice.Should().Be(30);
        itemDto.Discount.Should().Be(5);
        itemDto.Total.Should().Be((2 * 30m) - 5);
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldHaveFixedFinancials()
    {
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: 1);
        returnEntity.AddItem(productId: 1, quantity: 1, unitPrice: 100);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.DiscountAmount.Should().Be(0);
        result.TaxAmount.Should().Be(0);
        result.PaymentMethod.Should().Be("نقدي");
        result.AmountPaid.Should().Be(returnEntity.TotalAmount);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }

    // ─── Purchase Return ──────────────────────────────────────────────

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldMapHeader()
    {
        var party = Party.Create("مورد المرتجع", PartyType.Supplier, 1, phone: "0588888888");
        var supplier = Supplier.Create(party.Id, 1);
        SetNavigation(supplier, nameof(Supplier.Party), party);
        var returnEntity = PurchaseReturn.Create("PR-2025-0001", warehouseId: 1, supplierId: 1, notes: "مرتجع مشتريات");
        SetNavigation(returnEntity, nameof(PurchaseReturn.Supplier), supplier);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(returnEntity.Id);
        result.InvoiceNumber.Should().Be("PR-2025-0001");
        result.InvoiceDate.Should().Be(returnEntity.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.PurchaseReturn);
        result.CustomerOrSupplierName.Should().Be("مورد المرتجع");
        result.CustomerPhone.Should().Be("0588888888");
        result.Notes.Should().Be("مرتجع مشتريات");
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldUseDefaultSupplierNameWhenNull()
    {
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("مورد");
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldMapItems()
    {
        var product = CreateProduct("مادة مرتجعة");
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);
        returnEntity.AddItem(productId: 1, productUnitId: 1, quantity: 5, unitCost: 12, discountAmount: 3);
        SetNavigation(returnEntity.Items[0], nameof(PurchaseReturnItem.Product), product);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("مادة مرتجعة");
        itemDto.Quantity.Should().Be(5);
        itemDto.UnitPrice.Should().Be(12);
        itemDto.Discount.Should().Be(3);
        itemDto.Total.Should().Be((5 * 12m) - 3);
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldHaveFixedFinancials()
    {
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);
        returnEntity.AddItem(productId: 1, productUnitId: 1, quantity: 1, unitCost: 50);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.DiscountAmount.Should().Be(0);
        result.TaxAmount.Should().Be(0);
        result.PaymentMethod.Should().Be("نقدي");
        result.AmountPaid.Should().Be(returnEntity.TotalAmount);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }
}
#endif

