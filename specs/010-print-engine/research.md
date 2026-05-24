# Research: Print Engine (v4.3)

**Feature**: `010-print-engine`
**Date**: 2026-05-24
**Status**: Complete — All unknowns resolved

---

## Decision Log

### D-001: A4 PDF Library — QuestPDF

**Decision**: Use `QuestPDF` (already in solution per RULE-088) for A4 document generation. Document class: `A4InvoiceDocument` implementing `IDocument`.

**Rationale**: QuestPDF provides a fluent C# API, native RTL support, and produces high-quality PDF output without requiring an external PDF viewer on the server. Already approved by the project constitution.

**Alternatives considered**:
- *iTextSharp*: Rejected — licensing complexity and heavier API.
- *WPF FixedDocument*: Explicitly rejected by RULE-088.
- *PdfSharpCore*: Rejected — less mature RTL support.

---

### D-002: Thermal Printing — Win32 P/Invoke, No NuGet

**Decision**: Thermal receipt printing uses Win32 `winspool.drv` functions directly via P/Invoke (`OpenPrinter`, `StartDocPrinter`, `StartPagePrinter`, `WritePrinter`, `EndPagePrinter`, `EndDocPrinter`, `ClosePrinter`). A hand-built `EscPosCommandBuilder` class constructs the raw byte buffer.

**Rationale**: Explicitly required by the PRD ("no external NuGet for thermal"). Win32 raw printing bypasses the GDI stack, ensuring ESC/POS commands reach the printer unmodified, which is required for thermal formatting (cut commands, bold, alignment, codepage selection).

**Alternatives considered**:
- *ESCPOS.NET NuGet*: Explicitly rejected by PRD and RULE-088.
- *USB HID via LibUsbDotNet*: Rejected — unnecessary complexity, Win32 raw print handles USB printers via the spooler.

---

### D-003: ESC/POS Codepage for Arabic

**Decision**: Use codepage 864 (IBM Arabic) for thermal printers that support it, with a fallback to codepage 1256 (Windows Arabic). The `EscPosCommandBuilder` inserts an `ESC t n` command (Select Character Code Table) at the start of every receipt using the configured codepage value from `SystemSettings`.

**Rationale**: Arabic ESC/POS printing requires the correct codepage to be selected before text is sent; without it, Arabic characters print as garbage. Most mid-range thermal printers (Epson TM-T20, Xprinter XP-58) support codepage 864.

**Assumptions**: If the connected printer does not support Arabic codepages, text will be transliterated or the user must configure a supported model. This is documented in the quickstart.

---

### D-004: PDF Streaming Strategy

**Decision**: The `PrintController.GenerateA4Async` endpoint returns the PDF as a `FileContentResult` (`application/pdf`) with the invoice number as the filename. The Desktop's `PrintApiService` receives the byte array and writes it to a temp file, then opens it in `PdfPreviewWindow` using the system's default PDF viewer (via `Process.Start`).

**Rationale**: Streaming bytes back to the Desktop is simple and avoids the need for a PDF rendering library in the Desktop project — aligning with RULE-007 (Desktop never calls print APIs directly). The system PDF viewer handles rendering.

**Alternatives considered**:
- *Embedded PDF viewer in WPF*: Rejected — would require a PDF library in the Desktop project, violating RULE-088.
- *Open PDF directly on server*: Rejected — server is headless (Windows Service).

---

### D-005: PrintSettings Storage

**Decision**: Print settings are stored as individual rows in the existing `SystemSettings` table using `Category = "Print"` and specific `Key` values:
- `Print.StoreName`, `Print.StoreAddress`, `Print.StorePhone`
- `Print.ReceiptHeader`, `Print.ReceiptFooter`
- `Print.AutoPrintOnPost` (value = "true"/"false")
- `Print.LogoPath`
- `Print.ThermalPrinterName` (Windows printer name for spooler)

**Rationale**: Reuses existing `SystemSettings` infrastructure — no new table or migration needed. Settings are read at the start of each print job (not cached) to ensure changes take effect immediately (SC-006).

---

### D-006: PrintResult Value Object

**Decision**: `PrintResult` is a simple immutable record in `SalesSystem.Contracts`:
```
PrintResult { bool IsSuccess, string? ErrorMessage }
```
Static factory: `PrintResult.Success()` and `PrintResult.Failure(string message)`. All print service methods return `PrintResult` — exceptions are caught inside the service and converted to `PrintResult.Failure(ex.Message)`.

**Rationale**: Matches RULE-006 (Result pattern). Prevents exceptions from propagating up to the UI thread, which would crash the WPF application or API response pipeline.

---

### D-007: Auto-Print on Post Integration

**Decision**: When `Print.AutoPrintOnPost = "true"`, the `SalesInvoiceService.PostAsync` calls `IPrintService.PrintThermalAsync(invoiceId, userId, ct)` after the invoice transaction commits. The call is fire-and-forget at the service level — a Serilog warning is logged on failure but the invoice post is NOT rolled back.

**Rationale**: Printing is a best-effort operation — a printer failure must never invalidate a financial transaction. This matches the same pattern used for cost recalculation in `UpdateProductPricingService`.
