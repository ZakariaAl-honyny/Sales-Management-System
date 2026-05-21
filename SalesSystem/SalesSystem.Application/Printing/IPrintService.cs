using SalesSystem.Application.Printing.Contracts;

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
