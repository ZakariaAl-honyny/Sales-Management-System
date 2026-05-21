namespace SalesSystem.DesktopPWF.Printing.Models;

/// <summary>
/// Totals and financial data for printing
/// </summary>
public record InvoiceTotalsPrintDto(
    decimal SubTotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    decimal Paid,
    decimal Due
);
