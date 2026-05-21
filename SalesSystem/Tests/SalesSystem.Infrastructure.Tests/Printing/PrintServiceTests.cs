using Microsoft.Extensions.Logging;
using Moq;
using QuestPDF.Infrastructure;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Infrastructure.Printing;

namespace SalesSystem.Infrastructure.Tests.Printing;

public class PrintServiceTests
{
    private readonly Mock<ILogger<PrintService>> _loggerMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepoMock;
    private readonly PrintService _service;

    public PrintServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        _loggerMock = new Mock<ILogger<PrintService>>();
        _settingsRepoMock = new Mock<ISystemSettingsRepository>();
        _service = new PrintService(_loggerMock.Object, _settingsRepoMock.Object);
    }

    private static InvoicePrintDto CreateTestInvoice(byte[]? logoBytes = null)
    {
        return new InvoicePrintDto
        {
            InvoiceId = 1,
            InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            StorePhone = "0555555555",
            CustomerOrSupplierName = "عميل اختبار",
            CustomerPhone = "0566666666",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
                new("منتج ب", "قطعة", 1m, 150m, 10m, 140m),
            },
            SubTotal = 240m,
            DiscountAmount = 10m,
            TaxAmount = 0m,
            GrandTotal = 230m,
            PaymentMethod = "نقداً",
            AmountPaid = 230m,
            ChangeAmount = 0m,
            InvoiceType = InvoiceTypePrint.Sales,
            LogoBytes = logoBytes,
        };
    }

    // ─────────────────────────────────────────────
    // ShowPreviewAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ShowPreviewAsync_ShouldReturnSuccess_WithTempFilePath()
    {
        var invoice = CreateTestInvoice();

        var result = await _service.ShowPreviewAsync(invoice);

        result.IsSuccess.Should().BeTrue();
        result.OutputFilePath.Should().NotBeNullOrWhiteSpace();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldCreatePdfFileOnDisk()
    {
        var invoice = CreateTestInvoice();

        var result = await _service.ShowPreviewAsync(invoice);

        result.IsSuccess.Should().BeTrue();
        File.Exists(result.OutputFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task ShowPreviewAsync_ShouldReturnFailure_WhenGenerationThrows()
    {
        var invoice = CreateTestInvoice(logoBytes: new byte[] { 0, 1, 2, 3, 4 });

        var result = await _service.ShowPreviewAsync(invoice);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("تعذر فتح معاينة الطباعة");
    }

    // ─────────────────────────────────────────────
    // PrintA4Async
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PrintA4Async_ShouldReturnFailure_WhenNoPrinterConfigured()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("A4PrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var service = new OverriddenPrinterNameService(
            _loggerMock.Object, _settingsRepoMock.Object,
            a4Name: null, thermalName: null);

        var invoice = CreateTestInvoice();

        var result = await service.PrintA4Async(invoice);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("لم يتم تحديد طابعة A4");
    }

    [Fact]
    public async Task PrintA4Async_ShouldReturnFailure_WhenPrinterNotFound()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("A4PrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var service = new OverriddenPrinterNameService(
            _loggerMock.Object, _settingsRepoMock.Object,
            a4Name: "NonExistentPrinter_XYZ", thermalName: null);

        var invoice = CreateTestInvoice();

        var result = await service.PrintA4Async(invoice);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("فشل إرسال الفاتورة للطابعة");
    }

    // ─────────────────────────────────────────────
    // PrintThermalAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PrintThermalAsync_ShouldReturnFailure_WhenNoThermalPrinterConfigured()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("ThermalPrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var service = new OverriddenPrinterNameService(
            _loggerMock.Object, _settingsRepoMock.Object,
            a4Name: null, thermalName: null);

        var invoice = CreateTestInvoice();

        var result = await service.PrintThermalAsync(invoice);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("لم يتم تحديد الطابعة الحرارية");
    }

    [Fact]
    public async Task PrintThermalAsync_ShouldReturnFailure_WhenPrinterNotFound()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("ThermalPrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var service = new OverriddenPrinterNameService(
            _loggerMock.Object, _settingsRepoMock.Object,
            a4Name: null, thermalName: "NonExistentThermal_XYZ");

        var invoice = CreateTestInvoice();

        var result = await service.PrintThermalAsync(invoice);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("فشلت طباعة الإيصال الحراري");
    }

    // ─────────────────────────────────────────────
    // SavePdfAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task SavePdfAsync_ShouldCreatePdfAtSpecifiedPath()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"TestInvoice_{Guid.NewGuid()}.pdf");
        try
        {
            var invoice = CreateTestInvoice();

            var result = await _service.SavePdfAsync(invoice, tempPath);

            result.IsSuccess.Should().BeTrue();
            result.OutputFilePath.Should().Be(tempPath);
            File.Exists(tempPath).Should().BeTrue();
            new FileInfo(tempPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SavePdfAsync_ShouldReturnFailure_WhenPathIsInvalid()
    {
        var invoice = CreateTestInvoice();
        var invalidPath = Path.Combine("X:\\NonExistent_Directory_12345", "invoice.pdf");

        var result = await _service.SavePdfAsync(invoice, invalidPath);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("تعذر حفظ ملف PDF");
    }

    // ─────────────────────────────────────────────
    // Printer Name Resolution
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetA4PrinterNameAsync_ShouldReadFromSettings()
    {
        var sut = new ExposingPrintService(_loggerMock.Object, _settingsRepoMock.Object);
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("A4PrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("HP LaserJet 2000");

        var name = await sut.CallGetA4PrinterNameAsync();

        name.Should().Be("HP LaserJet 2000");
    }

    [Fact]
    public async Task GetA4PrinterNameAsync_ShouldFallbackToSystemDefault_WhenSettingIsEmpty()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("A4PrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var sut = new ExposingPrintService(_loggerMock.Object, _settingsRepoMock.Object);

        var name = await sut.CallGetA4PrinterNameAsync();

        var installedPrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>().ToList();
        if (installedPrinters.Count > 0)
            name.Should().Be(installedPrinters.First());
        else
            name.Should().BeNull();
    }

    [Fact]
    public async Task GetThermalPrinterNameAsync_ShouldReadFromSettings()
    {
        var sut = new ExposingPrintService(_loggerMock.Object, _settingsRepoMock.Object);
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("ThermalPrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Epson TM-T88VI");

        var name = await sut.CallGetThermalPrinterNameAsync();

        name.Should().Be("Epson TM-T88VI");
    }

    [Fact]
    public async Task GetThermalPrinterNameAsync_ShouldFallbackToSystemDefault_WhenSettingIsEmpty()
    {
        _settingsRepoMock
            .Setup(r => r.GetStringAsync("ThermalPrinterName", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var sut = new ExposingPrintService(_loggerMock.Object, _settingsRepoMock.Object);

        var name = await sut.CallGetThermalPrinterNameAsync();

        var installedPrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>().ToList();
        if (installedPrinters.Count > 0)
            name.Should().Be(installedPrinters.First());
        else
            name.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // Testable subclasses
    // ─────────────────────────────────────────────

    /// <summary>
    /// Exposes protected methods for direct testing (no override behavior).
    /// </summary>
    private class ExposingPrintService : PrintService
    {
        public ExposingPrintService(
            ILogger<PrintService> logger,
            ISystemSettingsRepository settingsRepo)
            : base(logger, settingsRepo)
        {
        }

        public Task<string?> CallGetA4PrinterNameAsync()
            => GetA4PrinterNameAsync();

        public Task<string?> CallGetThermalPrinterNameAsync()
            => GetThermalPrinterNameAsync();
    }

    /// <summary>
    /// Overrides printer name resolution to bypass system default fallback.
    /// </summary>
    private class OverriddenPrinterNameService : PrintService
    {
        private readonly string? _a4Name;
        private readonly string? _thermalName;

        public OverriddenPrinterNameService(
            ILogger<PrintService> logger,
            ISystemSettingsRepository settingsRepo,
            string? a4Name,
            string? thermalName)
            : base(logger, settingsRepo)
        {
            _a4Name = a4Name;
            _thermalName = thermalName;
        }

        protected override Task<string?> GetA4PrinterNameAsync()
            => Task.FromResult(_a4Name);

        protected override Task<string?> GetThermalPrinterNameAsync()
            => Task.FromResult(_thermalName);
    }
}
