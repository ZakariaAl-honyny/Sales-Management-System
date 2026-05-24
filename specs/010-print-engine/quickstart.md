# Quickstart: Print Engine (v4.3)

## Implementation Order

1. **Contracts**: Create `PrintResult` and `InvoicePrintDto` in `SalesSystem.Contracts`.
2. **Settings**: Seed default print settings in `SystemSettings` (or ensure they are created dynamically if missing).
3. **PrintDataService**: Implement `IPrintDataService` to query `SalesDbContext` for invoice details + settings and map to `InvoicePrintDto`.
4. **QuestPDF (A4)**: Implement `A4InvoiceDocument.cs` using QuestPDF's fluent API. Ensure RTL support (`.RightToLeft()`), null checks on logo path, and table layout for items.
5. **ESC/POS (Thermal)**: Implement `EscPosCommandBuilder.cs` with helper methods for alignment, bold, cut, and text encoding. Implement Win32 P/Invoke wrapper (`RawPrinterHelper`).
6. **PrintService**: Implement `IPrintService` methods to tie `PrintDataService` output to either QuestPDF or Win32 thermal printing. Handle all exceptions and return `PrintResult`.
7. **API**: Implement `PrintController` endpoints.
8. **Desktop API Service**: Implement `PrintApiService` to call endpoints.
9. **Desktop UI**: Add Print buttons to invoice editors. Build `PdfPreviewWindow` and `PrintSettingsView`.
10. **Auto-Print Integration**: Modify `SalesInvoiceService.PostAsync` to trigger thermal print if `AutoPrintOnPost` is true.

## Key Invariants to Verify

- **No Exceptions**: `PrintService` must NEVER throw. All Win32 interop and QuestPDF generation must be wrapped in try/catch returning `PrintResult.Failure`.
- **API Boundary**: The Desktop application MUST NOT contain references to QuestPDF or any Win32 printing logic. It only handles API HTTP calls and temporary PDF files.
- **Null Logo**: A missing logo file path, or a file that no longer exists on disk, MUST NOT crash the A4 generation. It should silently skip the logo block.
- **ESC/POS Encoding**: Ensure the `EscPosCommandBuilder` uses `System.Text.Encoding.GetEncoding(1256)` (or the configured codepage) when converting C# strings to byte arrays for the thermal printer. *Note: You may need to register codepages provider in API startup `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);`*.
