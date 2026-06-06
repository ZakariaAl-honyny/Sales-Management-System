namespace SalesSystem.DesktopPWF.Printing.Models;

public record InvoicePrintDto(
    int Id,
    string Title,
    DateTime Date,
    string CustomerName,
    string? CustomerAddress,
    string WarehouseName,
    List<InvoiceItemPrintDto> Items,
    InvoiceTotalsPrintDto Totals,
    string? Notes,
    string? TaxNumber
);
