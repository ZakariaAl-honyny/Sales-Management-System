using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Tests.Services;

public class InvoicePrintDtoBuilderTests
{
    private readonly InvoicePrintDtoBuilder _sut;
    private readonly Mock<ILogger<InvoicePrintDtoBuilder>> _loggerMock;

    private const string StoreName = "ظ…طھط¬ط±ظٹ";
    private const string StorePhone = "0555000000";
    private const string StoreAddress = "ط§ظ„ط±ظٹط§ط¶ - ط´ط§ط±ط¹ ط§ظ„ظ…ظ„ظƒ ظپظ‡ط¯";
    private const string StoreTaxNumber = "TAX-998877";
    private static readonly byte[] LogoBytes = [0x89, 0x50, 0x4E, 0x47];
    private const decimal TaxRate = 0.15m;

    public InvoicePrintDtoBuilderTests()
    {
        _loggerMock = new Mock<ILogger<InvoicePrintDtoBuilder>>();
        _sut = new InvoicePrintDtoBuilder(_loggerMock.Object);
    }

    // â”€â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void SetNavigation<T, TNavigation>(T entity, string propertyName, TNavigation? value)
        where T : class
    {
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, value);
    }

    private static Product CreateProduct(string name, decimal purchasePrice = 10m, decimal retailPrice = 20m)
    {
        return Product.Create(
            name,
            purchasePrice: purchasePrice,
            retailPrice: retailPrice,
            createdByUserId: 1);
    }

    // â”€â”€â”€ Store Info â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€â”€ Sales Invoice â€” Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapHeaderFields()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1,
            invoiceNo: 1, paymentType: PaymentType.Cash, notes: "ظ…ظ„ط§ط­ط¸ط§طھ ط§ظ„ظپط§طھظˆط±ط©");

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be(invoice.Id.ToString());
        result.InvoiceDate.Should().Be(invoice.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.Sales);
        result.Notes.Should().Be("ظ…ظ„ط§ط­ط¸ط§طھ ط§ظ„ظپط§طھظˆط±ط©");
    }

    // â”€â”€â”€ Sales Invoice â€” Customer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapCustomerInfo()
    {
        var customer = Customer.Create("ط£ط­ظ…ط¯ ظ…ط­ظ…ط¯", phone: "0555123456",
            address: "ط¬ط¯ط© - ط§ظ„ط¨ظ„ط¯", taxNumber: "TAX-C-001");
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: 1);
        SetNavigation(invoice, nameof(SalesInvoice.Customer), customer);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("ط£ط­ظ…ط¯ ظ…ط­ظ…ط¯");
        result.CustomerPhone.Should().Be("0555123456");
        result.CustomerAddress.Should().Be("ط¬ط¯ط© - ط§ظ„ط¨ظ„ط¯");
    }

    [Fact]
    public async Task BuildFromSalesAsync_ShouldUseDefaultNameWhenCustomerNull()
    {
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, customerId: null);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("ط²ط¨ظˆظ† ظ†ظ‚ط¯ظٹ");
        result.CustomerPhone.Should().BeNull();
        result.CustomerAddress.Should().BeNull();
    }

    // â”€â”€â”€ Sales Invoice â€” Items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromSalesAsync_ShouldMapItems()
    {
        var product = CreateProduct("ظ…ظ†طھط¬ طھط¬ط±ظٹط¨ظٹ");
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 3, unitPrice: 25.50m, discountAmount: 5);
        SetNavigation(item, nameof(SalesInvoiceItem.Product), product);
        invoice.AddItem(item);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("ظ…ظ†طھط¬ طھط¬ط±ظٹط¨ظٹ");
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

        result.Items[0].ProductName.Should().Be("ظ…ظ†طھط¬ #42");
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
        var product1 = CreateProduct("ظ…ظ†طھط¬ ط£");
        var product2 = CreateProduct("ظ…ظ†طھط¬ ط¨");
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
        result.Items[0].ProductName.Should().Be("ظ…ظ†طھط¬ ط£");
        result.Items[1].ProductName.Should().Be("ظ…ظ†طھط¬ ط¨");
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
    [InlineData(PaymentType.Cash, "ظ†ظ‚ط¯ظٹ")]
    [InlineData(PaymentType.Credit, "ط¢ط¬ظ„")]
    [InlineData(PaymentType.Mixed, "ظ†ظ‚ط¯ظٹ + ط¢ط¬ظ„")]
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
        var invoice = SalesInvoice.Create(warehouseId: 1, invoiceNo: 1, notes: "ط§ط´طھط±ظ‰ ظ…ط¹ ط²ط¨ظˆظ† ط¢ط®ط±");
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 1, unitPrice: 10);
        invoice.AddItem(item);
        invoice.SetPaidAmount(10);

        var result = await _sut.BuildFromSalesAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Notes.Should().Be("ط§ط´طھط±ظ‰ ظ…ط¹ ط²ط¨ظˆظ† ط¢ط®ط±");
    }

    // â”€â”€â”€ Purchase Invoice â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapHeaderFields()
    {
        var supplier = Supplier.Create("ط§ظ„ظ…ظˆط±ط¯ ط§ظ„ط£ظˆظ„", phone: "0566000000", taxNumber: "TAX-S-001");
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1,
            paymentType: PaymentType.Credit, notes: "ظپط§طھظˆط±ط© ظ…ظˆط±ط¯");
        SetNavigation(invoice, nameof(PurchaseInvoice.Supplier), supplier);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be(invoice.Id.ToString());
        result.InvoiceDate.Should().Be(invoice.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.Purchase);
        result.CustomerOrSupplierName.Should().Be("ط§ظ„ظ…ظˆط±ط¯ ط§ظ„ط£ظˆظ„");
        result.CustomerPhone.Should().Be("0566000000");
        result.Notes.Should().Be("ظپط§طھظˆط±ط© ظ…ظˆط±ط¯");
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldUseDefaultSupplierNameWhenNull()
    {
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("ظ…ظˆط±ط¯");
        result.CustomerPhone.Should().BeNull();
        result.CustomerAddress.Should().BeNull();
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapItems()
    {
        var product = CreateProduct("ظ…ط§ط¯ط© ط®ط§ظ…", purchasePrice: 8, retailPrice: 15);
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1);
        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 10, unitCost: 8.50m, discountAmount: 2);
        SetNavigation(item, nameof(PurchaseInvoiceItem.Product), product);
        invoice.AddItem(item);

        var result = await _sut.BuildFromPurchaseAsync(
            invoice, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("ظ…ط§ط¯ط© ط®ط§ظ…");
        itemDto.Quantity.Should().Be(10);
        itemDto.UnitPrice.Should().Be(8.50m);
        itemDto.Discount.Should().Be(2);
        itemDto.Total.Should().Be((10 * 8.50m) - 2);
    }

    [Fact]
    public async Task BuildFromPurchaseAsync_ShouldMapFinancialTotals()
    {
        var invoice = PurchaseInvoice.Create(supplierId: 1, warehouseId: 1, invoiceNo: 1, discountAmount: 20);
        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 100, unitCost: 5);
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

    // â”€â”€â”€ Sales Return â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldMapHeader()
    {
        var customer = Customer.Create("ط¹ظ…ظٹظ„ ط§ظ„ظ…ط±طھط¬ط¹", phone: "0577777777");
        var returnEntity = SalesReturn.Create("SR-2025-0001", warehouseId: 1, customerId: 1, notes: "ظ…ط±طھط¬ط¹ طھط§ظ„ظپ");
        SetNavigation(returnEntity, nameof(SalesReturn.Customer), customer);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(returnEntity.Id);
        result.InvoiceNumber.Should().Be("SR-2025-0001");
        result.InvoiceDate.Should().Be(returnEntity.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.SalesReturn);
        result.CustomerOrSupplierName.Should().Be("ط¹ظ…ظٹظ„ ط§ظ„ظ…ط±طھط¬ط¹");
        result.CustomerPhone.Should().Be("0577777777");
        result.Notes.Should().Be("ظ…ط±طھط¬ط¹ طھط§ظ„ظپ");
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldUseDefaultCustomerNameWhenNull()
    {
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: null);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("ط¹ظ…ظٹظ„");
    }

    [Fact]
    public async Task BuildFromSalesReturnAsync_ShouldMapItems()
    {
        var product = CreateProduct("ظ…ظ†طھط¬ ظ…ط±طھط¬ط¹");
        var returnEntity = SalesReturn.Create("SR-001", warehouseId: 1, customerId: 1);
        returnEntity.AddItem(productId: 1, quantity: 2, unitPrice: 30, discountAmount: 5);
        SetNavigation(returnEntity.Items[0], nameof(SalesReturnItem.Product), product);

        var result = await _sut.BuildFromSalesReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("ظ…ظ†طھط¬ ظ…ط±طھط¬ط¹");
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
        result.PaymentMethod.Should().Be("ظ†ظ‚ط¯ظٹ");
        result.AmountPaid.Should().Be(returnEntity.TotalAmount);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }

    // â”€â”€â”€ Purchase Return â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldMapHeader()
    {
        var supplier = Supplier.Create("ظ…ظˆط±ط¯ ط§ظ„ظ…ط±طھط¬ط¹", phone: "0588888888");
        var returnEntity = PurchaseReturn.Create("PR-2025-0001", warehouseId: 1, supplierId: 1, notes: "ظ…ط±طھط¬ط¹ ظ…ط´طھط±ظٹط§طھ");
        SetNavigation(returnEntity, nameof(PurchaseReturn.Supplier), supplier);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.InvoiceId.Should().Be(returnEntity.Id);
        result.InvoiceNumber.Should().Be("PR-2025-0001");
        result.InvoiceDate.Should().Be(returnEntity.CreatedAt);
        result.InvoiceType.Should().Be(InvoiceTypePrint.PurchaseReturn);
        result.CustomerOrSupplierName.Should().Be("ظ…ظˆط±ط¯ ط§ظ„ظ…ط±طھط¬ط¹");
        result.CustomerPhone.Should().Be("0588888888");
        result.Notes.Should().Be("ظ…ط±طھط¬ط¹ ظ…ط´طھط±ظٹط§طھ");
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldUseDefaultSupplierNameWhenNull()
    {
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.CustomerOrSupplierName.Should().Be("ظ…ظˆط±ط¯");
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldMapItems()
    {
        var product = CreateProduct("ظ…ط§ط¯ط© ظ…ط±طھط¬ط¹ط©");
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);
        returnEntity.AddItem(productId: 1, quantity: 5, unitCost: 12, discountAmount: 3);
        SetNavigation(returnEntity.Items[0], nameof(PurchaseReturnItem.Product), product);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.Items.Should().HaveCount(1);
        var itemDto = result.Items[0];
        itemDto.ProductName.Should().Be("ظ…ط§ط¯ط© ظ…ط±طھط¬ط¹ط©");
        itemDto.Quantity.Should().Be(5);
        itemDto.UnitPrice.Should().Be(12);
        itemDto.Discount.Should().Be(3);
        itemDto.Total.Should().Be((5 * 12m) - 3);
    }

    [Fact]
    public async Task BuildFromPurchaseReturnAsync_ShouldHaveFixedFinancials()
    {
        var returnEntity = PurchaseReturn.Create("PR-001", warehouseId: 1, supplierId: 1);
        returnEntity.AddItem(productId: 1, quantity: 1, unitCost: 50);

        var result = await _sut.BuildFromPurchaseReturnAsync(
            returnEntity, StoreName, StorePhone, StoreAddress, StoreTaxNumber, LogoBytes, TaxRate);

        result.DiscountAmount.Should().Be(0);
        result.TaxAmount.Should().Be(0);
        result.PaymentMethod.Should().Be("ظ†ظ‚ط¯ظٹ");
        result.AmountPaid.Should().Be(returnEntity.TotalAmount);
        result.ChangeAmount.Should().Be(0);
        result.IsTaxInclusive.Should().BeFalse();
    }
}

