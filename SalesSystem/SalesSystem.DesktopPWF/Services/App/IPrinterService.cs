using System.Collections.Generic;
using SalesSystem.DesktopPWF.Models.Printing;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IPrinterService
{
    void PrintPreview(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo);
    void Print(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo, string? printerName = null);
}

public interface IInvoicePrinter : IPrinterService { }
public interface IReceiptPrinter : IPrinterService { }

public class PaymentPrintDto
{
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountWord { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}

public interface IPaymentPrinter
{
    void PrintPreview(PaymentPrintDto payment, StoreInfoPrintDto storeInfo);
    void Print(PaymentPrintDto payment, StoreInfoPrintDto storeInfo, string? printerName = null);
}

public class TransferPrintDto
{
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FromWarehouse { get; set; } = string.Empty;
    public string ToWarehouse { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public interface ITransferPrinter
{
    void PrintPreview(TransferPrintDto transfer, IEnumerable<InvoiceItemPrintDto> items, StoreInfoPrintDto storeInfo);
}
