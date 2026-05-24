# Feature Specification: Print Engine (v4.3)

**Feature Branch**: `010-print-engine`
**Created**: 2026-05-24
**Status**: Draft
**Input**: User description: "Phase 10 — Print Engine (v4.3)"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — A4 Invoice PDF Generation & Preview (Priority: P1)

A cashier or manager finishes posting an invoice and wants to generate a professional A4 PDF document to hand to the customer or file for records. The system generates a pixel-perfect A4 invoice with the store logo, store details, item lines, subtotal, discount, tax breakdown, and totals — all correctly right-to-left in Arabic. The user can preview the document on screen before sending it to the printer.

**Why this priority**: This is the most common print need in retail. All other print capabilities build on the foundational document generation pipeline.

**Independent Test**: Post a sales invoice → trigger "Preview" → A4 PDF preview window opens showing: store name, invoice number, customer name, all items with quantities/prices, subtotal, tax, and total. The PDF is rendered without errors even when no store logo is configured.

**Acceptance Scenarios**:

1. **Given** a posted sales invoice, **When** the user clicks "طباعة A4", **Then** a preview window opens showing the invoice in A4 format with Arabic RTL layout, store details, item table, and financial summary.
2. **Given** the store has a logo configured, **When** the A4 PDF is generated, **Then** the logo appears in the document header.
3. **Given** the store has NO logo configured, **When** the A4 PDF is generated, **Then** the document generates without errors — the logo area is blank or replaced by the store name text.
4. **Given** the user is satisfied with the preview, **When** they click "طباعة", **Then** the document is sent to the default A4 printer.
5. **Given** a purchase invoice is posted, **When** the user triggers A4 print, **Then** the generated document is correctly labelled as a purchase invoice.

---

### User Story 2 — 80mm Thermal Receipt Printing (Priority: P1)

A cashier at a POS station posts a sales invoice and immediately needs to print a compact receipt on the thermal printer. The receipt uses the store's 80mm roll printer and includes: store name/address, invoice number, date, item lines (name + qty + price), total, paid amount, and change. Arabic text must print correctly on the thermal device.

**Why this priority**: Thermal receipt printing is the most time-critical operation at the point of sale — customers wait at the counter. It is equally important as A4 printing.

**Independent Test**: Post a sales invoice → trigger "طباعة إيصال" → thermal printer outputs a receipt with correct item lines, totals, Arabic store name, and invoice number. System returns a success/failure result without throwing exceptions.

**Acceptance Scenarios**:

1. **Given** a posted sales invoice and a connected thermal printer, **When** the user clicks "طباعة إيصال", **Then** a properly formatted 80mm receipt prints with Arabic text, item lines, and totals.
2. **Given** no thermal printer is connected, **When** print is triggered, **Then** the system returns a failure result with a user-friendly error message — it does NOT crash or throw an unhandled exception.
3. **Given** a thermal receipt is triggered, **When** the print job is sent, **Then** the result (success or failure) is reported back to the caller — the operation is never fire-and-forget for the UI.
4. **Given** the store has a configured header/footer message, **When** the receipt prints, **Then** those lines appear at the top and bottom of the receipt.

---

### User Story 3 — Print Settings Management (Priority: P2)

A store manager configures the print settings for their shop through the system settings screen. Settings include: store name for receipt, store address, store phone number, receipt footer message (e.g., "شكراً لزيارتكم"), whether to auto-print on post, and the default A4 paper size. These settings are stored persistently and applied to every print job.

**Why this priority**: Print settings are needed to customize every print job, but reasonable defaults can be used initially. The store can print with default settings while configuring proper branding later.

**Independent Test**: Update store name in print settings to "متجر النجمة" → trigger A4 print → store name "متجر النجمة" appears in the document header. Update receipt footer → print thermal receipt → footer message appears at the bottom.

**Acceptance Scenarios**:

1. **Given** print settings with `StoreName = "متجر النجمة"`, **When** an A4 invoice is generated, **Then** "متجر النجمة" appears in the document header.
2. **Given** print settings with `ReceiptFooter = "شكراً لزيارتكم"`, **When** a thermal receipt prints, **Then** that text appears at the receipt bottom.
3. **Given** print settings with `AutoPrintOnPost = true`, **When** an invoice is posted, **Then** the thermal receipt prints automatically without user clicking a button.
4. **Given** a manager navigates to the print settings screen, **When** they save changes, **Then** the next print job immediately reflects the updated values without restarting the application.

---

### User Story 4 — Centralized Print API (Priority: P2)

All print operations are initiated by the Desktop application through a dedicated API endpoint — the Desktop never drives printing directly from its own process. This ensures that print logic, document generation, and printer interaction are centralized in the API layer, keeping the Desktop lightweight.

**Why this priority**: This architectural constraint (RULE-088) prevents desktop-side PDF generation and printer binding, which would require distributing QuestPDF and Win32 print drivers with the desktop app. It centralizes complexity and enables future server-side printing scenarios.

**Independent Test**: Trigger A4 print from Desktop → Desktop sends `POST /api/v1/print/a4` with invoice ID → API generates and streams back the PDF bytes → Desktop displays the preview. NO PDF library or Win32 print call exists in the Desktop project.

**Acceptance Scenarios**:

1. **Given** the Desktop triggers an A4 print, **When** the request is sent, **Then** it goes to `POST /api/v1/print/a4/{invoiceId}` — no PDF library runs in the Desktop process.
2. **Given** the Desktop triggers a thermal print, **When** the request is sent, **Then** it goes to `POST /api/v1/print/thermal/{invoiceId}` — the API handles the Win32 print call.
3. **Given** the API print endpoint is called, **When** the print operation completes or fails, **Then** a `PrintResult { IsSuccess, ErrorMessage }` is returned — never an exception.

---

### Edge Cases

- What if the printer is out of paper mid-job? → The Win32 spooler returns an error code; the system returns `PrintResult.Failure("فشل الطباعة — تحقق من الطابعة")` without crashing.
- What if the invoice has zero items? → Document generation is rejected with a validation error before reaching the print engine.
- What if the store logo file is corrupt or too large? → Logo is skipped silently; the document generates with a text-based header instead.
- What if Arabic text contains unsupported characters on the thermal printer? → The ESC/POS builder normalizes characters to the printer's supported codepage before sending.
- What if two users trigger print simultaneously? → Each request is independent — no shared state in the print service.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST generate A4 PDF invoices with: store logo (if available), store name/address, invoice number, date, customer/supplier name, item table (description, quantity, unit price, line total), subtotal, discount, tax breakdown, and grand total — all in Arabic RTL layout.
- **FR-002**: The system MUST print 80mm thermal receipts using the ESC/POS command protocol without relying on any third-party NuGet package for thermal printing — all ESC/POS commands are built in-house.
- **FR-003**: The Desktop application MUST send all print requests to the API layer — the Desktop NEVER calls print functions, PDF libraries, or Win32 APIs directly.
- **FR-004**: All print operations MUST return a `PrintResult` (success/failure + message) — exceptions must NEVER propagate to the caller.
- **FR-005**: The system MUST support a PDF preview window on the Desktop before the user confirms printing.
- **FR-006**: Print settings (store name, address, phone, receipt header/footer, auto-print preference) MUST be stored in the `SystemSettings` table under category `"Print"` and applied to every generated document.
- **FR-007**: Missing store logo MUST be handled gracefully — document generation continues without it.
- **FR-008**: The system MUST support printing both sales invoices and purchase invoices from the A4 engine.
- **FR-009**: The thermal receipt MUST include: store name, invoice number, date/time, item lines (name, qty, price), total, paid amount, change due, and configurable footer message.
- **FR-010**: The system MUST support auto-print on post (configurable via print settings) — when enabled, the thermal receipt prints automatically when an invoice is posted without additional user action.

### Key Entities

- **PrintResult**: Represents the outcome of a print operation. Attributes: `IsSuccess (bool)`, `ErrorMessage (string?, nullable)`. Immutable value object — never throws.
- **InvoicePrintDto**: Data transfer object assembled before printing. Contains all invoice data required to render both A4 and thermal formats — assembled once, used by both print engines.
- **PrintSettings**: Configuration entity stored in SystemSettings (Category = "Print"). Attributes: `StoreName`, `StoreAddress`, `StorePhone`, `ReceiptHeader`, `ReceiptFooter`, `AutoPrintOnPost (bool)`, `LogoPath`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A4 PDF generation completes and is ready for preview in under 3 seconds for any invoice with up to 100 line items.
- **SC-002**: Thermal receipt print job is sent to the spooler in under 1 second from the moment the user clicks print.
- **SC-003**: 100% of print failures return a `PrintResult` with an error message — zero unhandled exceptions propagate to the UI.
- **SC-004**: Documents generate correctly even when the store logo is missing — 0% generation failures due to missing logo.
- **SC-005**: Arabic text renders correctly in both A4 (right-to-left layout) and thermal (correct codepage encoding) outputs.
- **SC-006**: Print settings changes take effect on the next print job without requiring an application restart.

---

## Assumptions

- The API server runs on the same local network as the Desktop application — streaming PDF bytes back to the Desktop is acceptable for preview.
- The thermal printer is an 80mm roll printer connected via USB, COM port, or shared network — standard ESC/POS compatible (e.g., Epson TM series, Star, Xprinter).
- A4 printing is directed to the Windows default printer or a user-selected printer — no printer discovery/management UI is in scope for this phase.
- The store logo is a PNG or JPEG file stored on the server's filesystem, path configured in `SystemSettings`.
- Tax breakdown in the A4 document uses the configured tax rate from `SystemSettings` — the document does not recompute taxes, it displays what is stored on the invoice.
- Print settings already exist as a `SystemSettings` category — this feature populates and reads the `Print` category keys.
- Sales returns and purchase returns are out of scope for the initial print engine — only sales invoices and purchase invoices are printed in this phase.
