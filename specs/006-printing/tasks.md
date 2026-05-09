# Implementation Tasks: Phase 6 — Printing

## Phase 1: Setup & Data Models

**Goal**: Establish the base DTOs for printing and extension methods for mapping.

- [ ] T001 [P] Create `StoreInfoPrintDto` in `SalesSystem/SalesSystem.Desktop/Printing/Models/StoreInfoPrintDto.cs`
- [ ] T002 [P] Create `InvoicePrintDto` in `SalesSystem/SalesSystem.Desktop/Printing/Models/InvoicePrintDto.cs`
- [ ] T003 [P] Create `InvoiceItemPrintDto` in `SalesSystem/SalesSystem.Desktop/Printing/Models/InvoiceItemPrintDto.cs`
- [ ] T004 [P] Create `InvoiceTotalsPrintDto` in `SalesSystem/SalesSystem.Desktop/Printing/Models/InvoiceTotalsPrintDto.cs`
- [ ] T005 Create `PrintDtoExtensions.cs` to map domain/API DTOs to Print DTOs in `SalesSystem/SalesSystem.Desktop/Printing/Models/PrintDtoExtensions.cs`

## Phase 2: Foundational Interfaces and Utilities

**Goal**: Define the printing contracts and shared GDI+ RTL drawing helper.

- [ ] T006 [P] Create `IPrinterService` interface in `SalesSystem/SalesSystem.Desktop/Printing/IPrinterService.cs`
- [ ] T007 Create `PrintHelper` with shared GDI+ RTL string formatting and drawing utilities in `SalesSystem/SalesSystem.Desktop/Printing/Core/PrintHelper.cs`

## Phase 3: User Story 1 - Print A4 Sales/Purchase Invoice

**Goal**: Implement the A4 InvoicePrinter and integrate it into the Sales and Purchase invoice forms.

- [ ] T008 [US1] Create `InvoicePrinter` implementing A4 rendering logic using `PrintHelper` in `SalesSystem/SalesSystem.Desktop/Printing/Core/InvoicePrinter.cs`
- [ ] T009 [US1] Register `InvoicePrinter` in DI container in `SalesSystem/SalesSystem.Desktop/Program.cs`
- [ ] T010 [US1] Add "Print A4" buttons and PrintPreview integration in `SalesSystem/SalesSystem.Desktop/Forms/SalesInvoiceForm.cs`
- [ ] T011 [US1] Add "Print A4" buttons and PrintPreview integration in `SalesSystem/SalesSystem.Desktop/Forms/PurchaseInvoiceForm.cs`

## Phase 4: User Story 2 - Print 80mm Thermal Receipt

**Goal**: Implement the 80mm ReceiptPrinter and integrate it into the Sales invoice form for quick checkout.

- [ ] T012 [US2] Create `ReceiptPrinter` implementing 80mm continuous roll rendering logic in `SalesSystem/SalesSystem.Desktop/Printing/Core/ReceiptPrinter.cs`
- [ ] T013 [US2] Register `ReceiptPrinter` in DI container in `SalesSystem/SalesSystem.Desktop/Program.cs`
- [ ] T014 [US2] Add "Print Receipt" button and PrintPreview integration in `SalesSystem/SalesSystem.Desktop/Forms/SalesInvoiceForm.cs`

## Phase 5: Polish & Cross-Cutting Concerns

**Goal**: Finalize edge cases, error handling, and formatting rules.

- [ ] T015 Ensure graceful fallback in `PrintHelper` if Store Logo file path is invalid or missing in `SalesSystem/SalesSystem.Desktop/Printing/Core/PrintHelper.cs`
- [ ] T016 Audit all printed amounts in `InvoicePrinter` and `ReceiptPrinter` to strictly enforce 2dp for currency and 3dp for quantities.
