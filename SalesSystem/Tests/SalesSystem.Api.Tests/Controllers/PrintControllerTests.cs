using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Tests.Controllers;

public class PrintControllerTests
{
    private readonly Mock<IPrintService> _printServiceMock;
    private readonly Mock<IPrintDataService> _printDataServiceMock;
    private readonly Mock<ILogger<PrintController>> _loggerMock;
    private readonly PrintController _controller;

    public PrintControllerTests()
    {
        _printServiceMock = new Mock<IPrintService>();
        _printDataServiceMock = new Mock<IPrintDataService>();
        _loggerMock = new Mock<ILogger<PrintController>>();
        _controller = new PrintController(
            _printServiceMock.Object,
            _printDataServiceMock.Object,
            _loggerMock.Object);
    }

    private InvoicePrintDto CreateSampleSalesDto()
    {
        return new InvoicePrintDto
        {
            InvoiceId = 1,
            InvoiceNumber = "INV-2026-000001",
            StoreName = "متجري",
            StorePhone = string.Empty,
            StoreAddress = string.Empty,
            StoreTaxNumber = string.Empty,
            LogoBytes = null,
            CustomerOrSupplierName = "زبون تجريبي",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج تجريبي", "", 2, 50, 0, 100)
            },
            SubTotal = 100,
            DiscountAmount = 0,
            TaxRate = 15,
            TaxAmount = 0,
            GrandTotal = 100,
            AmountPaid = 100,
            ChangeAmount = 0,
            PaymentMethod = "نقدي",
            Notes = null
        };
    }

    private InvoicePrintDto CreateSamplePurchaseDto()
    {
        return new InvoicePrintDto
        {
            InvoiceId = 1,
            InvoiceNumber = "PUR-2026-000001",
            StoreName = "متجري",
            StorePhone = string.Empty,
            StoreAddress = string.Empty,
            StoreTaxNumber = string.Empty,
            LogoBytes = null,
            CustomerOrSupplierName = "مورد تجريبي",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج تجريبي", "", 5, 30, 0, 150)
            },
            SubTotal = 150,
            DiscountAmount = 0,
            TaxRate = 15,
            TaxAmount = 0,
            GrandTotal = 150,
            AmountPaid = 150,
            ChangeAmount = 0,
            PaymentMethod = "نقدي",
            Notes = null
        };
    }

    // ─── Sales Invoice Preview ────────────────────────────────────────

    [Fact]
    public async Task PreviewSalesInvoice_WhenInvoiceExists_ReturnsOkWithPrintResult()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PreviewSalesInvoice(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var printResult = okResult.Value.Should().BeOfType<PrintResult>().Subject;
        printResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewSalesInvoice_WhenInvoiceDoesNotExist_Returns404()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoicePrintDto?)null);

        var result = await _controller.PreviewSalesInvoice(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PreviewSalesInvoice_WhenPrintServiceFails_Returns400()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("الطابعة غير متصلة"));

        var result = await _controller.PreviewSalesInvoice(1, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ─── Sales Invoice A4 Print ───────────────────────────────────────

    [Fact]
    public async Task PrintSalesA4_WhenInvoiceExists_ReturnsOk()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintSalesA4(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintSalesA4_WhenInvoiceDoesNotExist_Returns404()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoicePrintDto?)null);

        var result = await _controller.PrintSalesA4(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PrintSalesA4_WhenPrintFails_Returns400()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("فشلت الطباعة"));

        var result = await _controller.PrintSalesA4(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Sales Invoice Thermal Print ──────────────────────────────────

    [Fact]
    public async Task PrintSalesThermal_WhenInvoiceExists_ReturnsOk()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintSalesThermal(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintSalesThermal_WhenInvoiceDoesNotExist_Returns404()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoicePrintDto?)null);

        var result = await _controller.PrintSalesThermal(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PrintSalesThermal_WhenPrintFails_Returns400()
    {
        _printDataServiceMock
            .Setup(x => x.GetSalesInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSalesDto());
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("فشلت الطباعة الحرارية"));

        var result = await _controller.PrintSalesThermal(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Purchase Invoice Preview ─────────────────────────────────────

    [Fact]
    public async Task PreviewPurchaseInvoice_WhenInvoiceExists_ReturnsOkWithPrintResult()
    {
        _printDataServiceMock
            .Setup(x => x.GetPurchaseInvoicePrintDataAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSamplePurchaseDto());
        _printServiceMock
            .Setup(x => x.ShowPreviewAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PreviewPurchaseInvoice(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var printResult = okResult.Value.Should().BeOfType<PrintResult>().Subject;
        printResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewPurchaseInvoice_WhenInvoiceDoesNotExist_Returns404()
    {
        _printDataServiceMock
            .Setup(x => x.GetPurchaseInvoicePrintDataAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoicePrintDto?)null);

        var result = await _controller.PreviewPurchaseInvoice(999, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    // ─── Test Page ────────────────────────────────────────────────────

    [Fact]
    public async Task PrintTestPage_WhenA4Succeeds_ReturnsOk()
    {
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings?)null);
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success());

        var result = await _controller.PrintTestPage(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PrintTestPage_WhenA4FailsAndThermalSucceeds_ReturnsOk()
    {
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings?)null);
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());
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
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings?)null);
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("A4 error"));
        _printServiceMock
            .Setup(x => x.PrintThermalAsync(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Failure("Thermal error"));

        var result = await _controller.PrintTestPage(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Store Info via Test Page ─────────────────────────────────────

    [Fact]
    public async Task LoadStoreInfoAsync_WhenStoreSettingsExist_UsesStoreSettings()
    {
        var settings = StoreSettings.Create("متجر الاختبار", phone: "0111111111", address: "الرياض");
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());

        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PrintTestPage(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StoreName.Should().Be("متجر الاختبار");
        captured.StorePhone.Should().Be("0111111111");
        captured.StoreAddress.Should().Be("الرياض");
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenNoStoreSettings_UsesDefaults()
    {
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreSettings?)null);
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());

        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PrintTestPage(CancellationToken.None);

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

            var sysSettings = new List<SystemSetting>
            {
                SystemSetting.Create("LogoPath", tempFile,
                    dataType: "string", category: "Print", displayName: "شعار المتجر")
            };

            _printDataServiceMock
                .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoreSettings.Create("متجري"));
            _printDataServiceMock
                .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(sysSettings);

            InvoicePrintDto? captured = null;
            _printServiceMock
                .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
                .ReturnsAsync(PrintResult.Success())
                .Callback<InvoicePrintDto>(dto => captured = dto);

            await _controller.PrintTestPage(CancellationToken.None);

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
        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoreSettings.Create("متجري"));
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());

        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PrintTestPage(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TaxRate.Should().Be(15m);
    }

    [Fact]
    public async Task LoadStoreInfoAsync_WhenCustomTaxRateSet_UsesCustomRate()
    {
        var sysSettings = new List<SystemSetting>
        {
            SystemSetting.Create("TaxRate", "10",
                dataType: "decimal", category: "Print", displayName: "نسبة الضريبة")
        };

        _printDataServiceMock
            .Setup(x => x.GetStoreSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoreSettings.Create("متجري"));
        _printDataServiceMock
            .Setup(x => x.GetPrintSystemSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sysSettings);

        InvoicePrintDto? captured = null;
        _printServiceMock
            .Setup(x => x.PrintA4Async(It.IsAny<InvoicePrintDto>()))
            .ReturnsAsync(PrintResult.Success())
            .Callback<InvoicePrintDto>(dto => captured = dto);

        await _controller.PrintTestPage(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TaxRate.Should().Be(10m);
    }
}
