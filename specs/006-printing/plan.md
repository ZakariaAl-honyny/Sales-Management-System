# Implementation Plan: Phase 6 — Printing

**Branch**: `006-printing` | **Date**: 2026-05-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/006-printing/spec.md`

## Summary

Implement robust printing capabilities for the Sales Management System Desktop application. Provide two dedicated printing services: `InvoicePrinter` for standard A4 documents, and `ReceiptPrinter` for 80mm thermal point-of-sale printers. The implementation uses the native .NET `System.Drawing.Printing` library to manually render GDI+ graphics (text, lines, logos) in an RTL (Arabic) layout, ensuring compliance with strict decimal precision rules for all financial amounts and quantities.

## Technical Context

**Language/Version**: C# 12 / .NET 10 LTS WinForms
**Primary Dependencies**: `System.Drawing.Common` (native to WinForms), `Microsoft.Extensions.Http`
**Storage**: N/A (Read-only printing)
**Testing**: WinForms UI manual testing (PrintPreviewDialog)
**Target Platform**: Windows Desktop (WinForms)
**Project Type**: Desktop application module
**Performance Goals**: Print preview renders in < 1s
**Constraints**: Must support RTL (Arabic) rendering perfectly, must use decimal formatting (2dp money, 3dp quantity).
**Scale/Scope**: ~2 new printer classes, minimal UI integration in existing Invoice forms.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Decimal Types**: YES. All financial data passed to the printer uses `decimal`.
- **Financial Formulas**: YES. The printer reads computed values from the Domain/DTOs, it does not calculate them itself.
- **RTL / Arabic**: YES. `StringFormatFlags.DirectionRightToLeft` is used.
- **No Direct DB Access**: YES. Desktop fetches data via API/HttpClient.

## Project Structure

### Documentation (this feature)

```text
specs/006-printing/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
└── tasks.md             # Phase 2 output (future)
```

### Source Code (repository root)

```text
SalesSystem/
└── SalesSystem.Desktop/
    ├── Printing/
    │   ├── Models/
    │   │   ├── PrintDtoExtensions.cs
    │   │   ├── StoreInfoPrintDto.cs
    │   │   ├── InvoicePrintDto.cs
    │   │   ├── InvoiceItemPrintDto.cs
    │   │   └── InvoiceTotalsPrintDto.cs
    │   ├── Core/
    │   │   ├── PrintHelper.cs (Shared GDI+ RTL drawing utils)
    │   │   ├── InvoicePrinter.cs (A4)
    │   │   └── ReceiptPrinter.cs (80mm)
    │   └── IPrinterService.cs
    └── Forms/
        ├── SalesInvoiceForm.cs (Add Print buttons)
        └── PurchaseInvoiceForm.cs (Add Print buttons)
```

**Structure Decision**: Add a new `Printing` folder in the `SalesSystem.Desktop` project to encapsulate the GDI+ drawing logic and print models, keeping the WinForms UI code clean.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
