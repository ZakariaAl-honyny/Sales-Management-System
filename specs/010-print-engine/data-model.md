# Data Model: Print Engine (v4.3)

**Feature**: `010-print-engine`
**Date**: 2026-05-24

---

## Existing Entities (No new tables needed)

### `SystemSettings`

The print engine relies heavily on the `SystemSettings` table. We will insert new configuration rows.

| Key | Example Value | Description |
|-----|---------------|-------------|
| `Print.StoreName` | متجر النجمة | Printed as header on both A4 and Thermal |
| `Print.StoreAddress` | الرياض، شارع التحلية | Printed below store name |
| `Print.StorePhone` | 0501234567 | Printed on both formats |
| `Print.ReceiptHeader` | أهلاً بكم | Top line on thermal |
| `Print.ReceiptFooter` | شكراً لزيارتكم | Bottom line on thermal |
| `Print.AutoPrintOnPost` | true | Controls whether thermal receipt fires automatically |
| `Print.LogoPath` | `C:\Store\logo.png` | Used for A4 header image |
| `Print.ThermalPrinterName`| `POS-80` | Exact Win32 printer name for raw spooler |
| `Print.EscPosCodePage` | 22 | Decimal value for ESC/POS code page (e.g. 22 for IBM864) |

---

## Data Transfer Objects (DTOs)

### `InvoicePrintDto`

Assembled by `PrintDataService` to unify data sent to both `QuestPDF` and `EscPosCommandBuilder`.

```csharp
public class InvoicePrintDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public string CustomerOrSupplierName { get; set; }
    public string CashierName { get; set; }
    public string InvoiceType { get; set; } // "فاتورة مبيعات" / "فاتورة مشتريات"

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; } // TotalAmount - PaidAmount

    // Print Settings
    public string StoreName { get; set; }
    public string StoreAddress { get; set; }
    public string StorePhone { get; set; }
    public string LogoPath { get; set; }
    public string ReceiptHeader { get; set; }
    public string ReceiptFooter { get; set; }

    public List<InvoiceLinePrintDto> Lines { get; set; }
}

public class InvoiceLinePrintDto
{
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
```

### `PrintResult`

Returned by all print operations.

```csharp
public record PrintResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static PrintResult Success() => new() { IsSuccess = true };
    public static PrintResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
```
