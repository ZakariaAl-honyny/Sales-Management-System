using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Api.Tests.Controllers;

public class PrintControllerTests
{
    private readonly Mock<IPrintService> _printServiceMock;
    private readonly InvoicePrintDtoBuilder _builder;
    private readonly SalesDbContext _db;
    private readonly Mock<ILogger<PrintController>> _loggerMock;
    private readonly PrintController _controller;

    public PrintControllerTests()
    {
        _printServiceMock = new Mock<IPrintService>();

        var builderLogger = new Mock<ILogger<InvoicePrintDtoBuilder>>();
        _builder = new InvoicePrintDtoBuilder(builderLogger.Object);

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SalesDbContext(options);

        _loggerMock = new Mock<ILogger<PrintController>>();
        _controller = new PrintController(
            _printServiceMock.Object,
            _builder,
            _db,
            _loggerMock.Object);
    }

    private async Task<SalesInvoice> SeedSalesInvoiceAsync()
    {
        var customer = Customer.Create("زبون تجريبي", phone: "0555555555", address: "الرياض");
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var invoice = SalesInvoice.Create("INV-2026-000001", warehouseId: 1, customerId: customer.Id);
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 2, unitPrice: 50);
        invoice.AddItem(item);
        invoice.Post();
        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    private async Task<PurchaseInvoice> SeedPurchaseInvoiceAsync()
    {
        var supplier = Supplier.Create("مورد تجريبي", phone: "0555555555", address: "جدة");
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        var invoice = PurchaseInvoice.Create("PUR-2026-000001", supplierId: supplier.Id, warehouseId: 1);
        var item = PurchaseInvoiceItem.Create(productId: 1, quantity: 5, unitCost: 30);
        invoice.AddItem(item);
        invoice.Post();
        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task PreviewSalesInvoice_WhenInvoiceExists_ReturnsOkWithPrintResult()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var printResult = okResult.Value.Should().BeOfType<PrintResult>().Subject;
        printResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewSalesInvoice_WhenInvoiceDoesNotExist_Returns404()
    {
        var result = await _controller.PreviewSalesInvoice(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PreviewSalesInvoice_WhenPrintServiceFails_Returns400()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("الطابعة غير متصلة"));

        var result = await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task PrintSalesA4_WhenInvoiceExists_ReturnsOk()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintSalesA4(invoice.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintSalesA4_WhenInvoiceDoesNotExist_Returns404()
    {
        var result = await _controller.PrintSalesA4(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PrintSalesA4_WhenPrintFails_Returns400()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("فشلت الطباعة"));

        var result = await _controller.PrintSalesA4(invoice.Id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PrintSalesThermal_WhenInvoiceExists_ReturnsOk()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintSalesThermal(invoice.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintSalesThermal_WhenInvoiceDoesNotExist_Returns404()
    {
        var result = await _controller.PrintSalesThermal(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PrintSalesThermal_WhenPrintFails_Returns400()
    {
        var invoice = await SeedSalesInvoiceAsync();
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("فشلت الطباعة الحرارية"));

        var result = await _controller.PrintSalesThermal(invoice.Id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PreviewPurchaseInvoice_WhenInvoiceExists_ReturnsOkWithPrintResult()
    {
        var invoice = await SeedPurchaseInvoiceAsync();
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PreviewPurchaseInvoice(invoice.Id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var printResult = okResult.Value.Should().BeOfType<PrintResult>().Subject;
        printResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewPurchaseInvoice_WhenInvoiceDoesNotExist_Returns404()
    {
        var result = await _controller.PreviewPurchaseInvoice(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PrintTestPage_WhenA4Succeeds_ReturnsOk()
    {
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintTestPage(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintTestPage_WhenA4FailsAndThermalSucceeds_ReturnsOk()
    {
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("A4 error"));
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintTestPage(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintTestPage_WhenBothFail_Returns400()
    {
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("A4 error"));
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("Thermal error"));

        var result = await _controller.PrintTestPage(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenStoreSettingsExist_UsesStoreSettings()
    {
        var settings = StoreSettings.Create("متجر الاختبار", phone: "0111111111", address: "الرياض");
        _db.StoreSettings.Add(settings);
        await _db.SaveChangesAsync();

        var invoice = await SeedSalesInvoiceAsync();
        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StoreName.Should().Be("متجر الاختبار");
        captured.StorePhone.Should().Be("0111111111");
        captured.StoreAddress.Should().Be("الرياض");
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenNoStoreSettings_UsesDefaults()
    {
        var invoice = await SeedSalesInvoiceAsync();
        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StoreName.Should().Be("متجري");
        captured.StorePhone.Should().BeEmpty();
        captured.StoreAddress.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenLogoPathSetAndFileExists_LoadsLogo()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, [0x89, 0x50, 0x4E, 0x47]);

            _db.StoreSettings.Add(StoreSettings.Create("متجري"));
            _db.SystemSettings.Add(SystemSetting.Create("LogoPath", tempFile,
                dataType: "string", category: "Print", displayName: "شعار المتجر"));
            await _db.SaveChangesAsync();

            var invoice = await SeedSalesInvoiceAsync();
            InvoicePrintDto? captured = null;
            _printServiceMock
                .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
                .ReturnsAsync(PrintResult.Success())
                .Callback<InvoicePrintDto>(dto => captured = dto);

            await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.LogoBytes.Should().NotBeNull();
            captured.LogoBytes.Should().HaveCount(4);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenTaxRateMissing_UsesDefault15()
    {
        _db.StoreSettings.Add(StoreSettings.Create("متجري"));
        await _db.SaveChangesAsync();

        var invoice = await SeedSalesInvoiceAsync();
        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TaxRate.Should().Be(15m);
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenCustomTaxRateSet_UsesCustomRate()
    {
        _db.StoreSettings.Add(StoreSettings.Create("متجري"));
        _db.SystemSettings.Add(SystemSetting.Create("TaxRate", "10",
            dataType: "decimal", category: "Print", displayName: "نسبة الضريبة"));
        await _db.SaveChangesAsync();

        var invoice = await SeedSalesInvoiceAsync();
        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PreviewSalesInvoice(invoice.Id, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TaxRate.Should().Be(10m);
    }
}
