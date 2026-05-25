using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/print")]
public class PrintController : ControllerBase
{
    private readonly IPrintService _printService;
    private readonly IPrintDataService _printDataService;
    private readonly ILogger<PrintController> _logger;

    public PrintController(
        IPrintService printService,
        IPrintDataService printDataService,
        ILogger<PrintController> logger)
    {
        _printService = printService;
        _printDataService = printDataService;
        _logger = logger;
    }

    [HttpPost("preview/sales/{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> PreviewSalesInvoice(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var previewResult = await _printService.PreviewA4Async(result.Value!);
        return previewResult.IsSuccess ? Ok(previewResult) : BadRequest(previewResult);
    }

    [HttpPost("a4/sales/{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> PrintSalesA4(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var printResult = await _printService.PrintA4Async(result.Value!);
        return printResult.IsSuccess ? Ok(printResult) : BadRequest(printResult);
    }

    [HttpPost("thermal/sales/{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> PrintSalesThermal(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var printResult = await _printService.PrintThermalAsync(result.Value!);
        return printResult.IsSuccess ? Ok(printResult) : BadRequest(printResult);
    }

    [HttpPost("save/sales/{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> SaveSalesPdf(int id, [FromBody] SavePdfRequest request, CancellationToken ct)
    {
        var result = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        // Validate file path is in allowed directory
        var allowedBase = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports"));
        var resolvedPath = Path.GetFullPath(request.FilePath);
        if (!resolvedPath.StartsWith(allowedBase, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "مسار الحفظ غير صالح. يرجى استخدام مجلد التصدير" });

        var saveResult = await _printService.SavePdfAsync(result.Value!, request.FilePath);
        return saveResult.IsSuccess ? Ok(saveResult) : BadRequest(saveResult);
    }

    [HttpPost("preview/purchase/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PreviewPurchaseInvoice(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var previewResult = await _printService.PreviewA4Async(result.Value!);
        return previewResult.IsSuccess ? Ok(previewResult) : BadRequest(previewResult);
    }

    [HttpPost("a4/purchase/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PrintPurchaseA4(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var printResult = await _printService.PrintA4Async(result.Value!);
        return printResult.IsSuccess ? Ok(printResult) : BadRequest(printResult);
    }

    [HttpPost("thermal/purchase/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PrintPurchaseThermal(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var printResult = await _printService.PrintThermalAsync(result.Value!);
        return printResult.IsSuccess ? Ok(printResult) : BadRequest(printResult);
    }

    [HttpPost("save/purchase/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> SavePurchasePdf(int id, [FromBody] SavePdfRequest request, CancellationToken ct)
    {
        var result = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        // Validate file path is in allowed directory
        var allowedBase = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports"));
        var resolvedPath = Path.GetFullPath(request.FilePath);
        if (!resolvedPath.StartsWith(allowedBase, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "مسار الحفظ غير صالح. يرجى استخدام مجلد التصدير" });

        var saveResult = await _printService.SavePdfAsync(result.Value!, request.FilePath);
        return saveResult.IsSuccess ? Ok(saveResult) : BadRequest(saveResult);
    }

    // ═══════════════════════════════════════════════
    // GENERATE A4 PDF (Returns raw PDF bytes)
    // ═══════════════════════════════════════════════

    [HttpGet("generate-a4/sales/{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GenerateSalesA4Pdf(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var pdfResult = await _printService.GenerateA4PdfBytesAsync(result.Value!);
        if (!pdfResult.IsSuccess)
            return BadRequest(new { error = pdfResult.Error });

        return File(pdfResult.Value!, "application/pdf", $"Invoice_{id}.pdf");
    }

    [HttpGet("generate-a4/purchase/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GeneratePurchaseA4Pdf(int id, CancellationToken ct)
    {
        var result = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        var pdfResult = await _printService.GenerateA4PdfBytesAsync(result.Value!);
        if (!pdfResult.IsSuccess)
            return BadRequest(new { error = pdfResult.Error });

        return File(pdfResult.Value!, "application/pdf", $"PurchaseInvoice_{id}.pdf");
    }

    /// <summary>
    /// Prints a test page to verify printer connectivity.
    /// </summary>
    [HttpPost("test")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> PrintTestPage(CancellationToken ct)
    {
        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate) =
            await LoadStoreInfoAsync(ct);

        // Create a minimal test invoice DTO
        var testInvoice = new InvoicePrintDto
        {
            InvoiceType = InvoiceTypePrint.Test,
            InvoiceNumber = "TEST",
            InvoiceDate = DateTime.Now,
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            CustomerOrSupplierName = "---",
            TaxRate = taxRate,
            Items = new List<InvoiceItemPrintDto>
            {
                new("هذه طباعة تجريبية", "", 1, 0, 0, 0)
            },
            SubTotal = 0,
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotal = 0,
            AmountPaid = 0,
            ChangeAmount = 0,
            PaymentMethod = "نقدي",
            Notes = "طباعة تجريبية للتحقق من اتصال الطابعة"
        };

        // Try A4 first
        var a4Result = await _printService.PrintA4Async(testInvoice);
        if (a4Result.IsSuccess)
            return Ok(new { message = "تم إرسال صفحة الاختبار إلى طابعة A4 بنجاح" });

        // Fallback to thermal
        var thermalResult = await _printService.PrintThermalAsync(testInvoice);
        if (thermalResult.IsSuccess)
            return Ok(new { message = "تم إرسال صفحة الاختبار إلى الطابعة الحرارية بنجاح" });

        // Both failed
        return BadRequest(new
        {
            error = "فشلت طباعة الاختبار",
            a4Error = a4Result.ErrorMessage,
            thermalError = thermalResult.ErrorMessage
        });
    }

    private async Task<(string name, string phone, string address, string taxNumber,
        byte[]? logoBytes, decimal taxRate)> LoadStoreInfoAsync(CancellationToken ct)
    {
        // For the test page, load store info directly since we need it without an invoice.
        // This is the ONLY place that still directly accesses store data.
        // The PrintDataService handles store info for all invoice-based endpoints.
        var settingsResult = await _printDataService.GetStoreSettingsAsync(ct);
        var sysSettingsResult = await _printDataService.GetPrintSystemSettingsAsync(ct);

        var settings = settingsResult.IsSuccess ? settingsResult.Value : null;
        var sysSettings = sysSettingsResult.IsSuccess && sysSettingsResult.Value != null ? sysSettingsResult.Value : new List<SystemSetting>();

        var storeName = settings?.StoreName ?? "متجري";
        var storePhone = settings?.Phone ?? string.Empty;
        var storeAddress = settings?.Address ?? string.Empty;
        var storeTaxNumber = sysSettings
            .FirstOrDefault(s => s.SettingKey == "StoreTaxNumber")?.SettingValue ?? string.Empty;
        var logoPath = sysSettings
            .FirstOrDefault(s => s.SettingKey == "LogoPath")?.SettingValue;
        var taxRateStr = sysSettings
            .FirstOrDefault(s => s.SettingKey == "TaxRate")?.SettingValue ?? "15";

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && System.IO.File.Exists(logoPath))
        {
            try { logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath, ct); }
            catch { _logger.LogWarning("Could not load logo from {Path}", logoPath); }
        }

        decimal.TryParse(taxRateStr, out var taxRate);
        return (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate);
    }
}

public record SavePdfRequest(string FilePath);
