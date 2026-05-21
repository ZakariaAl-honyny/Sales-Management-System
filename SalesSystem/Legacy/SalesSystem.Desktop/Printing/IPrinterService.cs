using SalesSystem.Desktop.Printing.Models;

namespace SalesSystem.Desktop.Printing;

public interface IPrinterService
{
    void PrintPreview(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo);
    void Print(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo, string? printerName = null);
}

public interface IInvoicePrinter : IPrinterService { }
public interface IReceiptPrinter : IPrinterService { }
