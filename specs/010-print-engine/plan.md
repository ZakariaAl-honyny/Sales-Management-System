# Implementation Plan: Phase 10 — Print Engine (Detailed)

**Branch**: `010-print-engine` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/010-print-engine/spec.md`

---

## Summary

Phase 10 builds the complete, production-ready print engine that Phase 6 introduced at an architectural level. Where Phase 6 defined the Desktop → API → PrintService pipeline and the high-level responsibilities of A4 and thermal printing, this phase specifies the internal structure of every document generator, command builder, and service method that makes printing actually work. The engine has three major components: the QuestPDF-based `A4InvoiceDocument` for full-size invoice PDFs, the hand-built `EscPos` command library plus `ThermalReceiptGenerator` for 80mm point-of-sale receipts, and the `PrintService` that orchestrates DTO assembly, document rendering, and printer communication — all returning `PrintResult` without exceptions.

The engine is designed for three operating modes: preview (return PDF bytes for on-screen display), direct print (submit document bytes to the Windows spooler for immediate printing), and save-to-file (write the PDF to a user-specified file path). All three modes share the same DTO assembly pipeline; only the final delivery mechanism differs.

---

## InvoicePrintDtoBuilder — The DTO Assembly Pipeline

The `InvoicePrintDtoBuilder` is the sole entry point for constructing print data. It lives in the Application layer and provides four overloads: `BuildFromSalesInvoiceAsync(int invoiceId)` loads a posted sales invoice with its lines, product names, barcodes, and customer details; `BuildFromPurchaseInvoiceAsync(int invoiceId)` does the same for purchases with supplier details; `BuildFromSalesReturnAsync(int returnId)` builds a return document referencing the original invoice; and `BuildFromPurchaseReturnAsync(int returnId)` builds a purchase return document. Each method accepts a `CancellationToken` and returns `Result<InvoicePrintDto>`.

Internally, the builder queries the repository layer for the invoice, includes the navigation properties needed for display (customer/supplier name and tax number, line items joined to products and product units for name and barcode, warehouse name, user name of the creator), reads the store branding from the `CompanySettings` singleton table (see `docs/database-schema.md` section 2.6 for the `CompanySettings` table — a singleton row with `CompanyName`, `Phone`, `Email`, `Address`, `TaxNumber`, `LogoPath`, and `DefaultCurrencyId`), reads the print-specific settings from the `SystemSettings` table (`Category = "Print"` as documented in section 2.7), and assembles the flat `InvoicePrintDto`. The builder also formats the Arabic "amount in words" string for the `NetTotal` field — this is computed by a dedicated helper that converts decimal amounts to Arabic text (e.g., `١٬٢٥٠٫٧٥` becomes `ألف ومئتان وخمسون و٧٥ هللة`). If the invoice is not found or not in `Posted` status, the builder returns `Result.Failure` with a specific Arabic error message — it never returns a null or incomplete DTO.

The `InvoicePrintDto` carries thirteen top-level fields: `DocumentType` (string: "فاتورة بيع", "فاتورة شراء", etc.), `InvoiceNumber` (string, formatted from the int `InvoiceNo`), `InvoiceDate` (string, formatted as yyyy-MM-dd), `PaymentType` (string: "نقدي", "آجل", "مختلط"), `CustomerName` or `SupplierName` with their `TaxNumber`, `WarehouseName`, `CreatedByUserName`, the store branding block (name, tax number, phone, address, and the logo image as a byte array loaded from `LogoPath` — or null if the path is empty or the file does not exist), the line items collection, and the financial summary block. The DTO is fully populated before any document generation begins — the printing components never reach back to the database.

---

## QuestPDF A4 Document (A4InvoiceDocument)

The `A4InvoiceDocument` class implements QuestPDF's `IDocument` interface and is responsible for composing a right-to-left A4 invoice PDF. It receives three inputs in its constructor: the fully populated `InvoicePrintDto`, a boolean `isSales` flag that controls label text (for sales invoices the header reads "فاتورة بيع", for purchase invoices "فاتورة شراء", for returns "مرتجع بيع" or "مرتجع مشتريات"), and the `PrintSettings` containing the optional store logo, footer note, and tax display preferences. The document is generated entirely in memory as a byte stream — no temporary files are created during PDF generation.

The document layout follows a strict RTL grid. The header section is a two-column table: the left column (visual right in RTL) contains the store logo centered and scaled to fit within a 120-point square boundary — if the `LogoPath` is null, the file is missing, or the image fails to load, this column is replaced by extra vertical space (the document never crashes on missing logo). The right column contains the store name in 18-point bold, the tax number in 11-point, and the store contact details in 10-point. Below the header, a horizontal rule separates branding from content.

The invoice metadata section is a two-column key-value grid: right column (RTL right) has the invoice type label and number, the date, and the payment type; left column (RTL left) has the customer/supplier name and their tax number. Below metadata, the line items table occupies the main body. It uses QuestPDF's `Table()` component with columns for serial number (5%), product name (35%), barcode (15%), quantity with unit (15%), unit price (15%), and line total (15%). The header row uses a dark blue background (`#1a237e`) with white bold text; data rows alternate between white and light gray (`#f5f5f5`) for readability. Long product names wrap within their cell — the row height adjusts automatically. All money values are right-aligned with two decimal places; quantities use three decimal places.

The financial summary section sits below the line items table, right-aligned (RTL right-aligned, so visually on the left side of the page). It contains the subtotal, discount amount (if non-zero, shown in red), tax amount with the applied rate (e.g., "ضريبة 15%: ١٥٠٫٠٠ ر.س"), other charges (if non-zero), a bold separator line, net total in 14-point bold, paid amount, remaining amount (if non-zero), and finally the "amount in words" field in 11-point italic. Below the summary, a footer section contains the `FooterNote` from print settings (if non-null and non-empty) and a thank-you message ("شكراً لتعاملكم معنا").

The document also includes a bottom-border page number on every page after the first. QuestPDF handles pagination automatically: if the line items table exceeds one page, it repeats the table header row on each subsequent page. The page margins are 20mm on all sides, standard for A4 business documents.

---

## Thermal Receipt Generator (ThermalReceiptGenerator)

The `ThermalReceiptGenerator` is the hot-path component — it must produce ESC/POS command bytes in under one second for a typical 10-line receipt. It receives the same `InvoicePrintDto` and produces a `byte[]` that is sent directly to the printer spooler. The generator does not instantiate any printer or network objects; it is a pure byte-array builder operating on a `MemoryStream` wrapped around the byte commands from `EscPos`.

The receipt layout is designed for 80mm thermal paper with a 42-character monospaced column width. The generation sequence is: send `EscPos.Initialize()` to reset the printer to default state; send `EscPos.SetAlignment(EscPos.Alignment.Center)` and `EscPos.SetFontSize(2)` (double-height) for the store name; send the store name as a UTF-8 string re-encoded to Windows-1256; send `EscPos.SetFontSize(1)` (normal) for remaining text; send a divider line of 42 `=` characters; send the invoice type and number as a two-column row (label left-aligned, value right-aligned); send the date and customer/supplier name similarly; send a divider of 42 `-` characters; send the column headers for the line items table as bold text: the four columns are "البيان" (description, 18 chars), "الكمية" (quantity, 8 chars), "السعر" (price, 8 chars), "الإجمالي" (total, 8 chars); for each line item, send a row with the product name (wrapped if longer than 18 characters — the generator handles word-wrapping by splitting on spaces and writing continuation lines indented by two spaces), the quantity formatted to three decimal places, the unit price formatted to two decimal places, and the line total formatted to two decimal places; if any item has a non-zero discount, append a bold "(خصم)" indicator; after all items, send a divider; send the subtotal, discount, tax, and net total rows using the two-column format with the label left-aligned and the value right-aligned (bold for net total); if payment type is credit and remaining amount is non-zero, print the remaining amount in red or underlined (via `EscPos.SetUnderline(true)`); send a divider; send the "amount in words" field (may wrap to multiple lines); send a thank-you message; feed four blank lines via `EscPos.FeedLines(4)`; and finally send `EscPos.CutPaper()` to trigger the automatic cutter.

The `ThermalReceiptGenerator` does not close or open any printer handles — that responsibility belongs to the `PrintService`. It only builds the byte array. Arabic strings are encoded using `Encoding.GetEncoding(1256)` and all non-ASCII characters are verified to fit within the code page before encoding (a guard clause logs a warning for any character that cannot be mapped).

---

## ESC/POS Command Library (EscPos Static Class)

The `EscPos` static class is a pure byte-array factory. Every method returns `byte[]` and has no side effects. The command set is organized into five categories: printer control (Initialize, Reset, FeedLines, CutPaper, OpenDrawer), text formatting (SetBold, SetUnderline, SetFontSize, SetAlignment), text output (PrintLine, PrintBoldLine, PrintTwoColumns, PrintDivider, PrintWrappedText — the last of which handles automatic word-wrapping to a specified width), barcode commands (Barcode — Code128 and EAN13 variants), and QR code commands (QrCode — model 2, configurable module size and error correction level).

Each method is documented with the exact ESC/POS byte sequence it emits. For example, the `Initialize()` method returns bytes `[0x1B, 0x40]` (ESC @); `CutPaper()` returns `[0x1D, 0x56, 0x00]` (GS V 0) for full cut or `[0x1D, 0x56, 0x01]` for partial cut; `OpenDrawer()` returns `[0x1B, 0x70, 0x00, 0x19, 0xFA]` (ESC p pin-0, 25ms on, 250ms off). The `Barcode` method accepts a `BarcodeType` enum (Code128, EAN13, UPC-A, CODE39) and generates the appropriate GS k command sequence with the correct header byte for each symbology.

The `PrintTwoColumns` method is essential for receipt layout. It accepts a left text and right text, each with a maximum character count. It builds a line of exactly 42 characters: left text left-aligned within its allocated width, right text right-aligned within the remaining width, separated by spaces. If either text exceeds its allocation, it truncates with an ellipsis character (`…`, which renders at 1 column width in most thermal printer fonts).

The `SetFontSize` method supports sizes 1 (normal, approximately 12 characters per inch on 80mm paper) and 2 (double-height and double-width, approximately 6 characters per inch). Font size selection is used sparingly — only the store name uses size 2, and the net total may optionally use size 2 if the `EmphasizeTotal` flag is set in the print options.

All command bytes are hand-written constants. The `EscPos` class contains no external dependencies, no NuGet references, and no platform-specific code beyond the encoding of the byte arrays themselves.

---

## PrintService — Orchestration and Printer Communication

The `PrintService` in the Infrastructure layer is the concrete implementation of `IPrintService` from the Application layer. It exposes four public methods. `PreviewA4Async(int invoiceId, InvoiceTypePrint type, CancellationToken ct)` assembles the DTO via `InvoicePrintDtoBuilder`, creates an `A4InvoiceDocument`, renders it to a PDF byte array via QuestPDF's `GeneratePdf()` method, and returns `PrintResult` with the byte array. `PrintA4Async(int invoiceId, InvoiceTypePrint type, string printerName, CancellationToken ct)` does the same but additionally submits the PDF to the Windows spooler for printing on the specified A4 printer. `PrintThermalAsync(int invoiceId, InvoiceTypePrint type, CancellationToken ct)` assembles the DTO, runs the `ThermalReceiptGenerator` to produce ESC/POS bytes, opens the configured thermal printer via Win32 `OpenPrinter`, submits the bytes via `WritePrinter`, and closes the printer handle. `SavePdfAsync(int invoiceId, InvoiceTypePrint type, string filePath, CancellationToken ct)` renders the PDF and writes the bytes to the specified file path, overwriting if the file exists.

All four methods return `PrintResult`. The `PrintResult` class has five properties: `IsSuccess` (bool), `ErrorMessage` (string, Arabic, null on success), `ErrorCode` (string, null on success — possible values include `PRINTER_NOT_FOUND`, `PRINTER_OFFLINE`, `PDF_GENERATION_FAILED`, `INVOICE_NOT_FOUND`, `INVOICE_NOT_POSTED`), `DocumentBytes` (byte[], populated for preview and save operations), and `JobId` (int, the spooler job identifier for direct print operations, useful for troubleshooting).

The Win32 printer communication is encapsulated in a `RawPrinterHelper` internal class that uses `DllImport` to import `OpenPrinter`, `ClosePrinter`, `StartDocPrinter`, `WritePrinter`, `EndDocPrinter`, and `StartPagePrinter`/`EndPagePrinter` from `winspool.drv`. The helper opens the printer by name (retrieved from `SystemSettings` based on print type — `ThermalPrinterName` for thermal, `A4PrinterName` for A4), starts a print job with a document name like "فاتورة بيع #INV-2026-000123", writes the byte array to the printer driver, and finalizes the job. If the printer name is not found or the `OpenPrinter` call fails, the helper returns a detailed error code that is mapped to an Arabic error message in the `PrintService`.

The `PrintService` does not cache printer handles — it opens, writes, and closes for every print job. This prevents stale handle issues when a printer is taken offline and brought back online.

---

## PrintController — The Full API Surface

The `PrintController` in the API layer exposes fifteen endpoints organized under `/api/v1/print/`. All endpoints are authenticated via `[Authorize]`. The endpoints fall into five groups: preview (GET requests that return PDF file content for browser or WPF WebBrowser display), A4 print (POST requests that generate A4 PDF and submit to the configured A4 printer), thermal print (POST requests that generate thermal receipt and submit to the configured thermal printer), save PDF (POST requests that write the PDF to a server-accessible file path), and test page (POST requests that generate a minimal test document for printer configuration).

For sales invoices, the endpoints are structured as `GET /api/v1/print/sales/{id}/preview` (returns byte array as `application/pdf`), `POST /api/v1/print/sales/{id}/a4` (accepts optional `printerName` override, returns `PrintResult`), `POST /api/v1/print/sales/{id}/thermal` (returns `PrintResult`), `POST /api/v1/print/sales/{id}/save` (accepts `filePath` in body, returns `PrintResult`). The same pattern repeats for purchase invoices at `/api/v1/print/purchases/{id}/...`. The test endpoint is singular: `POST /api/v1/print/test` (accepts `printType: "a4" | "thermal"` in body, generates a minimal test document and prints it — used from the Settings screen to verify printer configuration).

Every endpoint validates: the user must have a valid JWT with the appropriate permission (viewing invoices for preview, editing invoices for print/save); the invoice must exist and be in `Posted` status (returns 404 with Arabic error if not found, 400 if not posted); and for direct print operations, the configured printer must be reachable (returns 503 if printer is offline or not found). The controller delegates all business logic to `IPrintService` and never accesses the database directly — this aligns with the controller purity requirement (RULE-203).

---

## Desktop Integration Details

The Desktop `PrintApiService` (implementation of `IPrintApiService`) mirrors the controller surface with methods for `GetSalesPreviewAsync`, `PrintSalesA4Async`, `PrintSalesThermalAsync`, `SaveSalesPdfAsync`, and their purchase counterparts, plus `PrintTestPageAsync`. Each method serializes the request to the appropriate endpoint and deserializes the response.

The `PdfPreviewWindow` is a lightweight WPF window. It receives the PDF bytes from the API call, writes them to a temporary file in `%TEMP%\SalesSystem` (creating the directory if it does not exist), and sets the `WebBrowser.Source` property to the temporary file URI. The window provides a toolbar with Zoom In, Zoom Out, Fit To Width buttons (controlling the `WebBrowser`'s zoom level via the underlying `IOleCommandTarget` interface), a Print button (which calls `PrintA4Async` directly — not the WebBrowser's print dialog), and a Close button. When the window closes, the temporary file is deleted in a `finally` block. If the PDF bytes are null or empty, the window shows an error message instead of navigating the WebBrowser.

---

## Error Handling Strategy

Every layer of the print engine follows the same error handling strategy: all failure modes are captured and returned as structured results — none are allowed to propagate as exceptions. The `InvoicePrintDtoBuilder` returns `Result<InvoicePrintDto>.Failure` for missing invoices or unposted status. The `A4InvoiceDocument` wraps QuestPDF's composition in a try-catch that catches `QuestPDF.Exceptions.QuestPDFException` (font, image, or layout errors) and returns failure. The `RawPrinterHelper` catches `Win32Exception` from P/Invoke calls (invalid handle, access denied, printer offline) and maps the native error code to a user-friendly message. The `PrintService` catches all of these and returns a single unified `PrintResult`.

The Desktop `PrintApiService` catches `HttpRequestException` (API unreachable), `TaskCanceledException` (timeout), and `JsonException` (malformed response). It transforms these into `PrintResult` with Arabic messages: "تعذر الاتصال بالخادم" for network errors, "انتهت مهلة الطباعة" for timeout, and "خطأ في استجابة الخادم" for unexpected responses. The calling ViewModel uses these results to show either a success toast or an error dialog — never a raw exception message.

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Hand-built ESC/POS without NuGet | No permissive-licensed ESC/POS library exists that is well-maintained and supports all required commands | RawprinterHelper NuGet is limited; PosPrinter SDK requires Windows Point of Service dependency |
| PDF preview via WebBrowser and temp file | QuestPDF cannot render to a WPF control directly | Converting PDF to XPS for WPF DocumentViewer adds a processing step with unpredictable fidelity |
| RTL text via Windows-1256 for thermal | ESC/POS printers do not support UTF-8 natively; Windows-1256 is the closest standard encoding | UTF-8 to thermal printer produces garbled Arabic on most Epson/Star models |
