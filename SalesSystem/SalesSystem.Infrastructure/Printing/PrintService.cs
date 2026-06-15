using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Infrastructure.Printing.A4;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Contracts.Common;
using SalesSystem.Infrastructure.Printing.Thermal;

namespace SalesSystem.Infrastructure.Printing;

public class PrintService : IPrintService
{
    private readonly ILogger<PrintService> _logger;
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly ThermalReceiptGenerator _thermalGenerator;

    public PrintService(ILogger<PrintService> logger, ISystemSettingsRepository settingsRepo)
    {
        _logger = logger;
        _settingsRepo = settingsRepo;
        _thermalGenerator = new ThermalReceiptGenerator();
    }

    // ═══════════════════════════════════════════════
    // SHOW PREVIEW
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> PreviewA4Async(InvoicePrintDto invoice)
    {
        try
        {
            var pdfBytes = await GeneratePdfBytesAsync(invoice);

            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:HHmmss}.pdf");

            await File.WriteAllBytesAsync(tempPath, pdfBytes);

            _logger.LogInformation(
                "Preview PDF generated for invoice {InvoiceNumber} at {Path}",
                invoice.InvoiceNumber, tempPath);

            return PrintResult.Success(tempPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for invoice {Invoice}",
                invoice.InvoiceNumber);
            return PrintResult.Failure(
                $"تعذر فتح معاينة الطباعة:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // PRINT A4
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> PrintA4Async(InvoicePrintDto invoice)
    {
        try
        {
            var printerName = await GetA4PrinterNameAsync();

            if (string.IsNullOrWhiteSpace(printerName))
            {
                return PrintResult.Failure(
                    "لم يتم تحديد طابعة A4 في الإعدادات.\n" +
                    "يرجى الذهاب إلى الإعدادات ← إعداد الطباعة وتحديد الطابعة.");
            }

            var pdfBytes = await GeneratePdfBytesAsync(invoice);
            var tempPath = Path.GetTempFileName() + ".pdf";
            await File.WriteAllBytesAsync(tempPath, pdfBytes);

            await Task.Run(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    Verb = "printto",
                    Arguments = $"\"{printerName}\"",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });

            _logger.LogInformation(
                "A4 invoice {InvoiceNumber} sent to printer {Printer}",
                invoice.InvoiceNumber, printerName);

            TryDeleteFile(tempPath, delayMs: 3000);
            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A4 print failed for invoice {Invoice}",
                invoice.InvoiceNumber);
            return PrintResult.Failure(
                $"فشل إرسال الفاتورة للطابعة:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // PRINT THERMAL
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> PrintThermalAsync(InvoicePrintDto invoice)
    {
        try
        {
            var printerName = await GetThermalPrinterNameAsync();

            if (string.IsNullOrWhiteSpace(printerName))
            {
                return PrintResult.Failure(
                    "لم يتم تحديد الطابعة الحرارية بعد.\n" +
                    "يرجى الذهاب إلى الإعدادات ← إعداد الطباعة وتحديد الطابعة الحرارية.");
            }

            var escPosCodePage = await GetEscPosCodePageAsync();
            var escPosData = _thermalGenerator.GenerateEscPosCommands(invoice, escPosCodePage);

            await Task.Run(() => SendRawToPrinter(printerName, escPosData));

            _logger.LogInformation(
                "Thermal receipt {InvoiceNumber} printed to {Printer}",
                invoice.InvoiceNumber, printerName);

            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thermal print failed for invoice {Invoice}",
                invoice.InvoiceNumber);
            return PrintResult.Failure(
                $"فشلت طباعة الإيصال الحراري:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // SAVE PDF
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> SavePdfAsync(InvoicePrintDto invoice, string filePath)
    {
        try
        {
            var pdfBytes = await GeneratePdfBytesAsync(invoice);
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            _logger.LogInformation("PDF saved to {Path}", filePath);
            return PrintResult.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PDF for invoice {Invoice}",
                invoice.InvoiceNumber);
            return PrintResult.Failure(
                $"تعذر حفظ ملف PDF:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // GENERATE A4 PDF BYTES
    // ═══════════════════════════════════════════════
    public async Task<Result<byte[]>> GenerateA4PdfBytesAsync(InvoicePrintDto invoice)
    {
        try
        {
            var bytes = await Task.Run(() =>
            {
                var document = new A4InvoiceDocument(invoice);
                return document.GeneratePdf();
            });
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for invoice {Invoice}", invoice.InvoiceNumber);
            return Result<byte[]>.Failure("فشل في إنشاء ملف PDF");
        }
    }

    // ─── Private Helpers ──────────────────────────

    private Task<byte[]> GeneratePdfBytesAsync(InvoicePrintDto invoice)
    {
        return Task.Run(() =>
        {
            var document = new A4InvoiceDocument(invoice);
            return document.GeneratePdf();
        });
    }

    /// <summary>
    /// Gets the ESC/POS code page from settings (default 1256 for Arabic Windows).
    /// </summary>
    protected virtual async Task<int> GetEscPosCodePageAsync()
    {
        var codePageStr = await _settingsRepo.GetStringAsync("EscPosCodePage", "1256");
        if (int.TryParse(codePageStr, out var codePage) && codePage > 0 && codePage <= 65535)
            return codePage;
        return 1256;
    }

    /// <summary>
    /// Gets the A4 printer name. 
    /// Override this method to read from settings repository.
    /// </summary>
    protected virtual async Task<string?> GetA4PrinterNameAsync()
    {
        var printerName = await _settingsRepo.GetStringAsync("A4PrinterName");
        if (!string.IsNullOrWhiteSpace(printerName))
            return printerName;
        // Fallback to system default
        return System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the thermal printer name.
    /// Override this method to read from settings repository.
    /// </summary>
    protected virtual async Task<string?> GetThermalPrinterNameAsync()
    {
        var printerName = await _settingsRepo.GetStringAsync("ThermalPrinterName");
        if (!string.IsNullOrWhiteSpace(printerName))
            return printerName;
        // Fallback to system default
        return System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Sends raw bytes directly to printer bypassing Windows GDI.
    /// Required for ESC/POS commands.
    /// </summary>
    private static void SendRawToPrinter(string printerName, byte[] data)
    {
        var docInfo = new DOCINFOA
        {
            pDocName = "Thermal Receipt",
            pDataType = "RAW"
        };

        var handle = OpenPrinter(printerName, out var printerHandle, IntPtr.Zero);

        if (!handle)
            throw new PrinterException(
                $"لا يمكن الاتصال بالطابعة الحرارية '{printerName}'");

        try
        {
            StartDocPrinter(printerHandle, 1, ref docInfo);
            StartPagePrinter(printerHandle);

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            WritePrinter(printerHandle, gcHandle.AddrOfPinnedObject(),
                data.Length, out _);
            gcHandle.Free();

            EndPagePrinter(printerHandle);
            EndDocPrinter(printerHandle);
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    private static string GetUserFriendlyError(Exception ex) => ex switch
    {
        UnauthorizedAccessException => "ليس لديك صلاحية الوصول للطابعة.",
        PrinterException pe => pe.Message,
        _ when ex.Message.Contains("printer", StringComparison.OrdinalIgnoreCase)
            => "الطابعة غير متصلة أو لا تستجيب.",
        _ => "حدث خطأ غير متوقع. يرجى إعادة المحاولة."
    };

    private static void TryDeleteFile(string path, int delayMs = 0)
    {
        Task.Run(async () =>
        {
            if (delayMs > 0) await Task.Delay(delayMs);
            try { File.Delete(path); }
            catch { /* Best-effort cleanup */ }
        });
    }

    // ─── Win32 API for raw printer access ─────────

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName,
        out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter,
        int level, ref DOCINFOA pDocInfo);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes,
        int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
    }
}
