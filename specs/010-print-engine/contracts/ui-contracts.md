# UI Contracts: Print Engine (v4.3)

**Feature**: `010-print-engine`
**Date**: 2026-05-24

---

## ViewModels

### `PdfPreviewViewModel`

A generic ViewModel to hold the path to a generated PDF file. The Desktop app will write the byte array returned from the API to a temporary file (`Path.GetTempFileName() + ".pdf"`).

- **Properties**: `string PdfFilePath`
- **Commands**: `PrintCommand`, `CloseCommand`
- **Logic**: `PrintCommand` launches `Process.Start` with `UseShellExecute = true` on the `PdfFilePath` to open the default system PDF viewer.

### `PrintSettingsViewModel`

Manages configuration for the print engine.

- **Properties**: `StoreName`, `StoreAddress`, `StorePhone`, `ReceiptHeader`, `ReceiptFooter`, `AutoPrintOnPost` (bool), `LogoPath`, `ThermalPrinterName`, `EscPosCodePage`.
- **Commands**: `SaveCommand`, `BrowseLogoCommand`, `TestThermalPrinterCommand`.
- **Logic**: Loads settings on init. `TestThermalPrinterCommand` calls a new dummy API endpoint or just prints a static string to verify Win32 connection.

### Modifications to Existing ViewModels

#### `SalesInvoiceEditorViewModel`
- **Commands**: Add `PrintA4Command`, `PrintThermalCommand`.
- **Logic**:
  - `PrintA4Command` calls `IPrintApiService.GetA4PdfAsync(InvoiceId)`. Upon receiving bytes, saves to temp file and opens `PdfPreviewWindow`.
  - `PrintThermalCommand` calls `IPrintApiService.PrintThermalAsync(InvoiceId)`. Displays Toast success/failure based on `Result`.
- **Visibility**: Both commands are only enabled when `InvoiceId > 0` and `Status == Posted`.

#### `PurchaseInvoiceEditorViewModel`
- Same additions as `SalesInvoiceEditorViewModel` (Purchase invoices use the same print engine, just with "فاتورة مشتريات" title).

---

## Views

### `PdfPreviewWindow.xaml`
- A non-modal window (or `ScreenWindow`) informing the user the PDF has been generated and providing buttons to open it or cancel. (Since WPF doesn't have a native, lightweight PDF viewer control without third-party libraries, we rely on the system viewer).

### `PrintSettingsView.xaml`
- Form layout with TextBox inputs for store details.
- CheckBox for `AutoPrintOnPost`.
- ComboBox for `ThermalPrinterName` (populated via `System.Drawing.Printing.PrinterSettings.InstalledPrinters`).
- File picker for `LogoPath`.
