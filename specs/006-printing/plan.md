# Implementation Plan: Phase 6 — Printing (Introduction)

**Branch**: `006-printing` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/006-printing/spec.md`

---

## Summary

Phase 6 introduces a clean-architecture printing pipeline for the Sales Management System. Unlike traditional desktop-printing approaches where the WPF client handles document rendering and spooling directly, this phase establishes a **Desktop → API → PrintService** architecture that guarantees separation of concerns: the Desktop knows only how to request a print job via HTTP, the API orchestrates document generation and delivery, and the actual rendering happens inside the Infrastructure project using two distinct technologies — QuestPDF for A4 invoice documents and Win32 raw printer commands (via P/Invoke) for 80mm thermal receipts.

The design is guided by three immutable constraints: the Desktop must never contain PDF or printer libraries, all printing must return structured `PrintResult` objects (never throw exceptions), and missing branding assets (store logo) must be handled gracefully with null checks rather than crashes.

---

## Printing Architecture Overview

The pipeline has four layers. At the Desktop layer, the user clicks a Print or Preview button inside a Sales or Purchase invoice view — this triggers a call to `IPrintApiService`, an HTTP client wrapper that sends a typed request to the appropriate API endpoint. The **PrintController** in the API layer receives the request, validates that the invoice exists and is in `Posted` status, and dispatches to `IPrintService` in the Application layer. The **PrintService** uses a dedicated `InvoicePrintDtoBuilder` to assemble a complete, flat DTO containing store branding, invoice header, line items with extended descriptions, and financial summaries — all formatted with strict decimal precision (18,2 for money, 18,3 for quantities). This DTO is then passed to the Infrastructure printing engine which produces either a QuestPDF-composed PDF byte stream or a byte array of ESC/POS thermal commands.

The resulting bytes are returned to the controller wrapped inside a `PrintResult`. For preview scenarios, the controller returns the PDF bytes directly as a `FileStreamResult` (Content-Type `application/pdf`); for raw print scenarios, it submits the bytes to the local printer spooler via `OpenPrinter`/`StartDocPrinter`/`WritePrinter` Win32 API calls and returns a success indicator. The Desktop receives the result and either displays the PDF in a dedicated `PdfPreviewWindow` (which hosts a `WebBrowser` control bound to a local temporary file) or shows a success/failure toast.

---

## Two Print Targets

### A4 Invoice Documents (QuestPDF)

A4 printing targets full-size letterhead documents used for official delivery to customers and suppliers. These documents are generated using the QuestPDF library (Community license, eligible for businesses under USD 1 million annual revenue). The `A4InvoiceDocument` class implements QuestPDF's `IDocument` interface and composes a multi-section layout: a header area containing the company logo (scaled to fit, null-checked so missing logo omits the image block gracefully), store name, tax number, phone, and address from `CompanySettings`; a customer/supplier info section; a line-items table with columns for serial number, product name, barcode, quantity, unit price, discount, and line total; a financial summary footer showing subtotal, discount, tax amount (with breakdown by tax rate), other charges, net total, paid amount, and remaining balance; and finally a footer with terms, thank-you message, and a signature line.

The document flows right-to-left to support Arabic text. All money columns use two decimal places, all quantity columns use three. The `A4InvoiceDocument` is instantiated with the `InvoicePrintDto` and a boolean indicating whether to generate a sales or purchase variant — the only structural difference between the two is the header label (فاتورة بيع vs فاتورة شراء) and the financial summary label adjustments.

### Thermal Receipts (Win32 Raw Printing with ESC/POS)

Thermal receipts target 80mm point-of-sale printers using ESC/POS command language. The Desktop never communicates with the printer directly — instead, the `PrintController` receives the request, the `PrintService` generates an ESC/POS command byte array, and the Infrastructure `PrintService` submits those bytes to the spooler via the Win32 `OpenPrinter`, `StartDocPrinter`, `WritePrinter`, `EndDocPrinter`, `ClosePrinter` API sequence.

The ESC/POS command set is built entirely with a custom `EscPos` static class — no external NuGet packages are used. The command set includes approximately twenty methods: `Initialize()` resets the printer to default state; `PrintLine(string text)` prints a line with automatic word-wrapping at 42 monospaced characters; `PrintBoldLine(string text)` wraps text in bold on/off commands; `PrintDivider(char c)` prints a horizontal line of 42 characters (typically `-` or `=`); `PrintTwoColumns(string left, string right)` prints a key-value pair with left-aligned label and right-aligned value; `SetAlignment(Center/Left/Right)` controls horizontal positioning; `SetBold(bool)` toggles bold mode; `SetFontSize(int)` switches between normal and double-height/double-width modes; `Barcode(string data, BarcodeType type)` renders Code128 or EAN13 barcodes; `CutPaper()` sends full cut command; `OpenDrawer()` pulses pin 2 for cash drawer; `FeedLines(int n)` advances paper; `PrintQrCode(string data, int size)` renders a QR code for digital payment links or invoice references.

All Arabic text in thermal receipts is encoded using Windows-1256 code page. The `ThermalReceiptGenerator` class composes the byte array sequentially: initialize printer → print store header (double-height, centered) → print divider → print invoice type and number → print date and customer/supplier info → print divider → print column headers (البيان, الكمية, السعر, الإجمالي) → print each line item (wrapping long product names to next line) → print divider → print totals (subtotal, discount, tax, net total) → print payment info → print divider → print thank-you message → feed paper → cut.

---

## Print Data Flow and DTOs

The `InvoicePrintDto` is the single carrier DTO that travels across all layers. It is assembled by the `InvoicePrintDtoBuilder` in the Application layer, which queries the invoice from the database (via the repository layer), loads related entities (customer/supplier name, line items with product names and barcodes, warehouse name, user name), reads print settings from the `SystemSettings` table (see `docs/database-schema.md` section 2.7 for the table structure — `Category = "Print"` filters the relevant settings), and constructs a fully populated DTO. The `InvoicePrintDto` contains the company branding block (logo path, company name, tax number, phone, address), the invoice header (document type, invoice number, date, payment type, customer/supplier name and tax number, warehouse name, created by user name), the line items collection (each with line number, product name, barcode, quantity with unit name, unit price, line total), and the financial summary (subtotal, discount amount, tax amount with rate, other charges, net total, paid amount, remaining amount, amount in words).

The `InvoiceItemPrintDto` carries extended data: serial number, product name, barcode, quantity (decimal 18,3), unit name, unit price (decimal 18,2), discount amount, and line total. The financial summary section carries: subtotal, total discount amount, tax rate and amount, other charges, net total, paid amount, remaining amount, and a formatted string for "amount in words" rendered in Arabic.

Print settings stored in the `SystemSettings` table include: `ThermalPrinterName` (string, the Windows printer name for thermal output), `A4PrinterName` (string, the Windows printer name for A4 output), `LogoPath` (string, file path to the store logo image — null-checked at every usage), `StoreTaxNumber` (string, displayed on all print outputs), `FooterNote` (string, a custom message printed at the bottom of receipts and invoices), and `ShowLogo` (boolean, controls whether the logo is included in the generated document). These settings are managed from the Desktop Settings screen and are persisted via the `SettingsController` API endpoint.

---

## PrintController Endpoints

The `PrintController` exposes a RESTful API surface covering both sales and purchase invoices, plus a test page for printer configuration. All endpoints require authentication via the `[Authorize]` attribute. The endpoint set includes preview (returns PDF bytes for browser or WPF WebBrowser display), A4 print (generates PDF and submits to the configured A4 printer), thermal print (generates ESC/POS bytes and submits to the configured thermal printer), and save-to-file (writes the PDF to a user-specified path). For each of these four operations, there are dedicated routes for sales invoices, purchase invoices, and a test page.

The test page endpoint is a critical configuration tool: it generates a minimal test document (a single line with printer name, date, and "هذا اختبار للطابعة" text) for both A4 and thermal targets. The user can trigger this from the Settings screen to verify printer configuration without creating a real invoice.

All endpoints share the same validation: the requesting user must have a valid JWT, the referenced invoice must exist and be in `Posted` status, and the printer name (if specified in the request body) must correspond to an installed Windows printer. Validation failures return HTTP 400 with an Arabic error message; service-level failures (disk full, printer offline) return HTTP 503 with a structured error body; success returns the generated document or a `PrintResult` JSON object.

---

## Desktop Print Integration

The Desktop integration is minimal by design. The user interacts with print functionality through existing invoice list and invoice editor views: each `SalesInvoicesListView` and `PurchaseInvoicesListView` has a Print button in the toolbar (with ToolTip explaining the action), and the invoice editors have a Print action bound to a keyboard shortcut (F8) and a toolbar button. Clicking Print opens a small `PrintOptionsDialog` where the user selects the target (A4 or Thermal) and optionally overrides the printer name — this dialog is not a full window but a lightweight flyout.

The `SalesInvoicesListViewViewModel` and `PurchaseInvoicesListViewViewModel` each hold a `PrintSelectedInvoiceCommand` that: validates an invoice is selected, verifies it is in `Posted` status (showing a warning dialog if not), shows the `PrintOptionsDialog`, and dispatches the print request to `IPrintApiService`. The `IPrintApiService` has methods matching each controller endpoint — `GetPreviewAsync`, `PrintA4Async`, `PrintThermalAsync`, `SavePdfAsync` — each returning `PrintResult`. Success triggers a toast notification; failure shows a styled error dialog with the specific error message.

`PdfPreviewWindow` is a simple WPF window hosting a `WebBrowser` control. When the Desktop requests a preview, the API returns PDF bytes; the Desktop writes these bytes to a temporary file in `%TEMP%\SalesSystem\preview_*.pdf`, navigates the WebBrowser to that file path, and opens the window modally with zoom controls and a Print button. After the window closes, the temporary file is deleted.

---

## Error Handling and Logging

Every printing operation follows the Result pattern. The `PrintResult` class contains an `IsSuccess` boolean, an optional `ErrorMessage` string (Arabic, user-friendly), and an optional `ErrorCode` for programmatic handling. The `PrintService` wraps all QuestPDF composition and Win32 P/Invoke calls in try-catch blocks: PDF generation errors (font missing, image corrupt) return `PrintResult` with specific Arabic messages; printer communication errors (printer offline, paper jam, spooler not running) return 503 with retry suggestions; file I/O errors during PDF save return failure with permission guidance.

All print attempts are logged via Serilog. Success entries are logged at Information level with invoice ID, print type, and printer name. Failure entries are logged at Error level with full exception details. Crucially, no raw exception messages are shown to the user — all error messages are transformed to friendly Arabic text in the `HandleFailure` flow.

---

## Configuration and Dependencies

**Target Framework Constraint**: Both the `SalesSystem.Infrastructure` and `SalesSystem.Api` projects must target `net10.0-windows` (not `net10.0`) because the Win32 `DllImport` calls for `OpenPrinter`, `ClosePrinter`, `StartDocPrinter`, `WritePrinter`, and `EndDocPrinter` require Windows-specific APIs. This constraint is already captured in the project files. The Desktop project (`SalesSystem.DesktopPWF`) already targets `net10.0-windows` by virtue of being a WPF application.

**NuGet Dependencies**: The only new packages introduced in this phase are `QuestPDF` (2024.3+, Community license) in the Infrastructure project for A4 PDF generation, `SixLabors.ImageSharp` (3.1+) for logo image resizing within QuestPDF documents (QuestPDF requires ImageSharp for image handling), and `System.Drawing.Common` (10.x) which is a runtime dependency for `SixLabors.ImageSharp` on Windows. No additional packages are needed for thermal printing — the ESC/POS builder is entirely hand-written, and the Win32 spooler API is accessed via `DllImport` with no NuGet dependency.

**QuestPDF License Initialization**: The `PrintingBootstrapper` static class calls `QuestPDF.Settings.License = LicenseType.Community` once during application startup (in `Program.cs`). This is a one-time initialization required by QuestPDF before any document can be composed.

**Print Settings Schema Reference**: All print-related settings are stored in the `SystemSettings` table documented in `docs/database-schema.md` (Module 2.7). The `Category` column filters to `"Print"` for print-specific settings. The `SettingKey` column contains the setting names (`ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber`, `FooterNote`, `ShowLogo`). The `SettingType` column distinguishes string, integer, decimal, and boolean values. The `DisplayName` and `Description` columns provide the Arabic user-facing labels and help text shown in the Settings screen.

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| QuestPDF dependency adds 8 MB to deployment | QuestPDF is the only .NET library that reliably generates RTL Arabic PDFs with proper glyph shaping | WPF FixedDocument cannot export PDF without third-party wrapper; PDFSharp has incomplete RTL support |
| Win32 P/Invoke for thermal printing | External NuGet packages for ESC/POS are poorly maintained and license-incompatible | Any NuGet ESC/POS package would introduce an unapproved dependency per AGENTS.md Section 5 |
| `net10.0-windows` target on API project | Required for `DllImport` Win32 spooler calls | Using `net10.0` would force moving raw print to a separate Windows-only process |
