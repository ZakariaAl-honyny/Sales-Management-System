using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/print")]
[Authorize]
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
    public async Task<IActionResult> PreviewSalesInvoice(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.ShowPreviewAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("a4/sales/{id:int}")]
    public async Task<IActionResult> PrintSalesA4(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.PrintA4Async(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("thermal/sales/{id:int}")]
    public async Task<IActionResult> PrintSalesThermal(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.PrintThermalAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview-data/sales/{id:int}")]
    public async Task<IActionResult> GetSalesPreviewData(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.ShowPreviewAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save/sales/{id:int}")]
    public async Task<IActionResult> SaveSalesPdf(int id, [FromBody] SavePdfRequest request, CancellationToken ct)
    {
        var dto = await _printDataService.GetSalesInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.SavePdfAsync(dto, request.FilePath);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview/purchase/{id:int}")]
    public async Task<IActionResult> PreviewPurchaseInvoice(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.ShowPreviewAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("a4/purchase/{id:int}")]
    public async Task<IActionResult> PrintPurchaseA4(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.PrintA4Async(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("thermal/purchase/{id:int}")]
    public async Task<IActionResult> PrintPurchaseThermal(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.PrintThermalAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview-data/purchase/{id:int}")]
    public async Task<IActionResult> GetPurchasePreviewData(int id, CancellationToken ct)
    {
        var dto = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.ShowPreviewAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save/purchase/{id:int}")]
    public async Task<IActionResult> SavePurchasePdf(int id, [FromBody] SavePdfRequest request, CancellationToken ct)
    {
        var dto = await _printDataService.GetPurchaseInvoicePrintDataAsync(id, ct);
        if (dto == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var result = await _printService.SavePdfAsync(dto, request.FilePath);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Prints a test page to verify printer connectivity.
    /// </summary>
    [HttpPost("test")]
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
        var settings = await _printDataService.GetStoreSettingsAsync(ct);
        var sysSettings = await _printDataService.GetPrintSystemSettingsAsync(ct);

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
