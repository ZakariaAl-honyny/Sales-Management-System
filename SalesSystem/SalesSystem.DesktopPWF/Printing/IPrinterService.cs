using SalesSystem.DesktopPWF.Printing.Models;

namespace SalesSystem.DesktopPWF.Printing;

/// <summary>
/// Defines the contract for printing documents
/// </summary>
public interface IPrinterService
{
    /// <summary>
    /// Prints the specified invoice
    /// </summary>
    Task PrintAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo);

    /// <summary>
    /// Shows a print preview for the specified invoice
    /// </summary>
    Task PreviewAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo);
}
