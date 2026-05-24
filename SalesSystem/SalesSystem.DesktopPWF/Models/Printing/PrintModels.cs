using SalesSystem.Contracts.Enums;

namespace SalesSystem.DesktopPWF.Models.Printing;

public class StoreInfoPrintDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
}

public class InvoicePrintDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerOrSupplierName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public string TypeName { get; set; } = string.Empty; // e.g. "فاتورة مبيعات"
    public string WarehouseName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class InvoiceItemPrintDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public byte Mode { get; set; } = 1; // 1 = Retail, 2 = Wholesale
}

public class InvoiceTotalsPrintDto
{
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
}
