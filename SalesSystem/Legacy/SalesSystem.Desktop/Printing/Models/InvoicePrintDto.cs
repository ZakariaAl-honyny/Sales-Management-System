using SalesSystem.Contracts.Enums;

namespace SalesSystem.Desktop.Printing.Models;

public class InvoicePrintDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string TypeName { get; set; } = string.Empty; // Sales, Purchase, Return
    public string CashierName { get; set; } = string.Empty;
    public string CustomerOrSupplierName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
}
