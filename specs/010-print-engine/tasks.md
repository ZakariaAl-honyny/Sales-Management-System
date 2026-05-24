---
description: "Task list for Print Engine (v4.3)"
---

# Tasks: Print Engine (v4.3)

**Input**: `specs/010-print-engine/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [ ] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS all user stories)

**Goal**: Establish the common data structures and the data gathering service used by both A4 and Thermal print engines.

- [ ] T002 [P] Create `PrintResult` record. Properties: `IsSuccess (bool)`, `ErrorMessage (string?)`. Static methods: `Success()`, `Failure(string)` — FILE: `SalesSystem/SalesSystem.Contracts/Responses/PrintResult.cs`

- [ ] T003 [P] Create `InvoicePrintDto` and `InvoiceLinePrintDto` classes matching the data-model.md — FILE: `SalesSystem/SalesSystem.Contracts/Responses/InvoicePrintDto.cs`

- [ ] T004 Add `IPrintDataService` interface with `Task<Result<InvoicePrintDto>> GetInvoicePrintDataAsync(int invoiceId, CancellationToken ct)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IPrintDataService.cs`

- [ ] T005 Implement `PrintDataService`. Inject `IUnitOfWork` and `ISystemSettingsRepository`. Logic: fetch SalesInvoice (or PurchaseInvoice) by ID with Lines and Product/Unit includes. Map to `InvoicePrintDto`. Fetch Print settings (`StoreName`, `StoreAddress`, `StorePhone`, `LogoPath`, `ReceiptHeader`, `ReceiptFooter`) and map to the DTO. Return `Result<InvoicePrintDto>` — FILE: `SalesSystem/SalesSystem.Application/Services/PrintDataService.cs`

- [ ] T006 Register `IPrintDataService` to `PrintDataService` in DI container — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs`

**Checkpoint**: `IPrintDataService` can assemble a complete `InvoicePrintDto` from the database.

---

## Phase 3: US1 — A4 Invoice PDF Generation (Priority: P1)

**Goal**: Generate a pixel-perfect RTL A4 PDF using QuestPDF and stream it to the desktop for preview.

- [ ] T007 [P] [US1] Create `A4InvoiceDocument` class implementing QuestPDF's `IDocument`. Constructor accepts `InvoicePrintDto`. Implement `Compose` method: Page size A4, margin 1cm, `.RightToLeft()`. Header (logo null-check, store details, invoice info), Content (table of items), Footer (totals, discount, tax). — FILE: `SalesSystem/SalesSystem.Infrastructure/Printing/A4InvoiceDocument.cs`

- [ ] T008 [P] [US1] Add `IPrintService` interface with `Task<Result<byte[]>> GetA4PdfAsync(int invoiceId, CancellationToken ct)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IPrintService.cs`

- [ ] T009 [US1] Implement `PrintService` class. Inject `IPrintDataService`, `ILogger`. In `GetA4PdfAsync`: call `GetInvoicePrintDataAsync`. If failure, return failure. If success, `var document = new A4InvoiceDocument(dto); var pdfBytes = document.GeneratePdf(); return Result<byte[]>.Success(pdfBytes);`. Wrap in try/catch returning failure on error. — FILE: `SalesSystem/SalesSystem.Application/Services/PrintService.cs`

- [ ] T010 [US1] Create `PrintController` with `[Authorize]` and `[Route("api/v1/print")]`. Add `POST a4/{invoiceId}` endpoint. Inject `IPrintService`. Call `GetA4PdfAsync`. Return `File(bytes, "application/pdf", $"Invoice_{invoiceId}.pdf")` on success, or 400/404 on failure — FILE: `SalesSystem/SalesSystem.Api/Controllers/PrintController.cs`

- [ ] T011 [P] [US1] Extend `ICashBoxApiService` or create new `IPrintApiService` interface + `PrintApiService` class in Desktop. Add `Task<Result<string>> GetA4PdfAsync(int invoiceId)`. Calls API, receives bytes, writes to `Path.GetTempFileName() + ".pdf"`, returns file path — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/PrintApiService.cs`

- [ ] T012 [P] [US1] Create `PdfPreviewWindow.xaml` + `PdfPreviewViewModel.cs`. Window displays text: "تم إنشاء ملف PDF بنجاح." Buttons: "فتح الملف" (calls `Process.Start(PdfFilePath)`), "إغلاق" — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/PdfPreviewWindow.xaml` + `ViewModels/PdfPreviewViewModel.cs`

- [ ] T013 [US1] Update `SalesInvoiceEditorViewModel` and `PurchaseInvoiceEditorViewModel`: add `PrintA4Command` (enabled if Id > 0 & Posted). Calls `IPrintApiService.GetA4PdfAsync`. On success, opens `PdfPreviewWindow` via WindowService — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs`

**Checkpoint**: Clicking A4 print on a posted invoice generates a PDF on the API, saves it to temp on Desktop, and opens the preview window.

---

## Phase 4: US2 — 80mm Thermal Receipt Printing (Priority: P1)

**Goal**: Build raw ESC/POS byte arrays and send them directly to the Win32 spooler via P/Invoke.

- [ ] T014 [P] [US2] Create `RawPrinterHelper` static class with Win32 P/Invoke methods (`OpenPrinter`, `StartDocPrinter`, `StartPagePrinter`, `WritePrinter`, `EndPagePrinter`, `EndDocPrinter`, `ClosePrinter`, `SendBytesToPrinter`) — FILE: `SalesSystem/SalesSystem.Infrastructure/Printing/RawPrinterHelper.cs`

- [ ] T015 [P] [US2] Create `EscPosCommandBuilder`. Methods: `Initialize()`, `SetCodePage(byte)`, `SetAlignment(align)`, `SetBold(bool)`, `PrintText(string, encoding)`, `PrintLine()`, `FeedLines(int)`, `CutPaper()`, `GetBytes()`. Handles encoding string to bytes — FILE: `SalesSystem/SalesSystem.Infrastructure/Printing/EscPosCommandBuilder.cs`

- [ ] T016 [P] [US2] Extend `IPrintService` with `Task<PrintResult> PrintThermalAsync(int invoiceId, CancellationToken ct)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IPrintService.cs`

- [ ] T017 [US2] In `PrintService`, implement `PrintThermalAsync`: get DTO. Read configured `ThermalPrinterName` and `EscPosCodePage`. Build receipt bytes using `EscPosCommandBuilder` (Header, Items, Totals, Footer, Cut). Call `RawPrinterHelper.SendBytesToPrinter`. Return `PrintResult`. Wrap in try-catch — FILE: `SalesSystem/SalesSystem.Application/Services/PrintService.cs`

- [ ] T018 [US2] Add `POST thermal/{invoiceId}` to `PrintController`. Calls `PrintThermalAsync`. Returns 200 OK on success, or 400 with error message on failure — FILE: `SalesSystem/SalesSystem.Api/Controllers/PrintController.cs`

- [ ] T019 [P] [US2] Add `Task<PrintResult> PrintThermalAsync(int invoiceId)` to `IPrintApiService` and implement in `PrintApiService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/PrintApiService.cs`

- [ ] T020 [US2] Update `SalesInvoiceEditorViewModel` and `PurchaseInvoiceEditorViewModel`: add `PrintThermalCommand`. Calls API. Shows success/error via `IToastNotificationService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs`

**Checkpoint**: Thermal print command sends raw bytes to the local printer without throwing exceptions.

---

## Phase 5: US3 — Print Settings Management (Priority: P2)

**Goal**: Allow managers to configure store details, thermal printer name, and auto-print preference.

- [ ] T021 [US3] Create `PrintSettingsView.xaml` and `PrintSettingsViewModel.cs`. Form fields for `StoreName`, `StoreAddress`, `StorePhone`, `ReceiptHeader`, `ReceiptFooter`, `LogoPath`, `ThermalPrinterName` (ComboBox from `PrinterSettings.InstalledPrinters`), `EscPosCodePage`, `AutoPrintOnPost` (CheckBox). `SaveCommand` saves all keys via `SystemSettingsApiService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/PrintSettingsView.xaml` + `ViewModels/Settings/PrintSettingsViewModel.cs`

- [ ] T022 [US3] Modify `SalesInvoiceService.PostAsync`. After successful commit, read `Print.AutoPrintOnPost` from settings. If true, inject and call `_printService.PrintThermalAsync` in fire-and-forget mode. Log warning on fail, do NOT fail the invoice post — FILE: `SalesSystem/SalesSystem.Application/Services/SalesInvoiceService.cs`

**Checkpoint**: Print settings are editable. Auto-print triggers thermal printing upon invoice post.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T023 [P] Verify API startup registers CodePages (required for `GetEncoding(1256)`): add `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);` to `Program.cs` — FILE: `SalesSystem/SalesSystem.Api/Program.cs`

- [ ] T024 [P] Verify no PDF or print library NuGet packages exist in the `SalesSystem.DesktopPWF` project file (enforcing RULE-088 and architecture boundary) — FILE: `SalesSystem/SalesSystem.DesktopPWF/SalesSystem.DesktopPWF.csproj`

- [ ] T025 [P] Add unit tests for `EscPosCommandBuilder` ensuring correct byte sequences for bold, cut, and encoding translation — FILE: `SalesSystem/Tests/SalesSystem.Infrastructure.Tests/Printing/EscPosCommandBuilderTests.cs`

- [ ] T026 Update `docs/CHANGELOG.md` with v4.3 entry: Print Engine (A4 QuestPDF, Thermal raw Win32 printing, Auto-print) — FILE: `docs/CHANGELOG.md`

---

## Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1
- **Phase 3 (US1 - A4)**: Depends on Phase 2
- **Phase 4 (US2 - Thermal)**: Depends on Phase 2
- **Phase 5 (US3 - Settings)**: Depends on Phases 3 and 4
- **Phase 6 (Polish)**: Depends on all previous phases

---

## Implementation Strategy

### MVP First (US1 + US2 only)
1. Complete Phase 2: Foundational (DTOs and Data Gathering)
2. Complete Phase 3: A4 PDF Generation (US1)
3. Complete Phase 4: Thermal Printing (US2)
4. Validate both print paths work with default/hardcoded settings.
5. Then implement Phase 5 to make settings configurable.
