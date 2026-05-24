using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Printing;

public interface IPrintService
{
    /// <summary>
    /// Generates PDF preview and opens WPF preview window.
    /// </summary>
    Task<PrintResult> ShowPreviewAsync(InvoicePrintDto invoice);

    /// <summary>
    /// Generates PDF and sends directly to configured A4 printer.
    /// </summary>
    Task<PrintResult> PrintA4Async(InvoicePrintDto invoice);

    /// <summary>
    /// Sends condensed receipt to configured 80mm thermal printer via ESC/POS.
    /// </summary>
    Task<PrintResult> PrintThermalAsync(InvoicePrintDto invoice);

    /// <summary>
    /// Saves PDF to user-chosen file path.
    /// </summary>
    Task<PrintResult> SavePdfAsync(InvoicePrintDto invoice, string filePath);

    /// <summary>
    /// Generates raw A4 PDF bytes for the given invoice data.
    /// Returns Result with byte array for direct download or further processing.
    /// </summary>
    Task<Result<byte[]>> GenerateA4PdfBytesAsync(InvoicePrintDto invoice);
}

/// <summary>
/// Result object — never throw exceptions to the caller.
/// </summary>
public record PrintResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputFilePath { get; init; }

    public static PrintResult Success(string? filePath = null)
        => new() { IsSuccess = true, OutputFilePath = filePath };

    public static PrintResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
