namespace SalesSystem.DesktopPWF.Printing.Models;

/// <summary>
/// Line item data for printing
/// </summary>
public record InvoiceItemPrintDto(
    int Index,
    string ProductName,
    string? ProductCode,
    decimal Quantity,
    string UnitName,
    decimal UnitPrice,
    decimal Discount,
    decimal Total
);
