# Data Model: Phase 6 — Printing

The Printing module in the Sales Management System primarily reads existing data rather than creating new database entities. However, it requires structured DTOs to encapsulate the data passed to the rendering engine.

## Printing DTOs

### 1. `StoreInfoPrintDto`
Represents the header information for the store.
- `StoreName` (string): e.g., "سوبر ماركت البركة"
- `Address` (string)
- `Phone` (string)
- `TaxNumber` (string)
- `LogoPath` (string): Absolute or relative path to the image file, or byte array.

### 2. `InvoicePrintDto`
Represents the master invoice data.
- `InvoiceNumber` (string)
- `InvoiceDate` (DateTime)
- `Type` (enum): Sales, Purchase, Return
- `CashierName` (string)
- `CustomerOrSupplierName` (string)
- `PaymentType` (PaymentType enum: Cash, Credit, Mixed)

### 3. `InvoiceItemPrintDto`
Represents an individual line item.
- `ProductName` (string)
- `Quantity` (decimal 18,3)
- `UnitPrice` (decimal 18,2)
- `Discount` (decimal 18,2)
- `LineTotal` (decimal 18,2)

### 4. `InvoiceTotalsPrintDto`
Represents the summary block at the bottom of the invoice.
- `SubTotal` (decimal 18,2)
- `TaxAmount` (decimal 18,2)
- `Discount` (decimal 18,2)
- `TotalAmount` (decimal 18,2)
- `PaidAmount` (decimal 18,2)
- `DueAmount` (decimal 18,2)

## Interfaces

### `IInvoicePrinter`
- `void PrintPreview(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo)`
- `void Print(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo, string printerName = null)`

### `IReceiptPrinter` (80mm)
- `void PrintPreview(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo)`
- `void Print(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo, string printerName = null)`
