# Feature Specification: Phase 6 — Printing

**Feature Branch**: `006-printing`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "Phase 6 — Printing" based on PRD-MVP-v3.0.md

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Print A4 Sales/Purchase Invoice (Priority: P1)

As a store user, I want to print a full A4 invoice so that I can provide an official, detailed document to the customer or supplier for their records.

**Why this priority**: Official documentation of financial transactions is required for compliance and business record-keeping.

**Independent Test**: Can be fully tested by selecting a posted invoice and generating an A4 print preview, which accurately displays the logo, header, items, and totals.

**Acceptance Scenarios**:

1. **Given** a posted sales or purchase invoice, **When** the user clicks "Print A4", **Then** the system displays a print preview containing the store logo, store name, phone, address, invoice details, item lines (using correct decimal formatting), and calculated totals.
2. **Given** the A4 print preview is displayed, **When** the user confirms printing, **Then** the document is sent to the selected standard printer and prints correctly.

---

### User Story 2 - Print 80mm Thermal Receipt (Priority: P1)

As a cashier, I want to print an 80mm thermal receipt quickly so that I can hand it to the retail customer immediately after a sale.

**Why this priority**: Fast checkout in a retail shop demands quick, continuous thermal printing of receipts.

**Independent Test**: Can be fully tested by generating an 80mm receipt from a sales invoice and verifying it fits the paper width correctly without truncation.

**Acceptance Scenarios**:

1. **Given** a completed sales invoice, **When** the user clicks "Print Receipt", **Then** the system generates an 80mm formatted layout containing the store header, items list, and totals.
2. **Given** the 80mm receipt preview, **When** the user confirms printing, **Then** the document is sent to the default thermal printer and prints clearly on 80mm roll paper.

---

### Edge Cases

- What happens when the store logo is missing or the image file is corrupted? (Should gracefully print without the logo).
- How does the system handle invoice lines that are too long for the 80mm thermal paper width? (Text should wrap correctly).
- What happens if no printer is connected or installed on the system? (Should show a user-friendly error message).
- How does the system handle different DPI settings across different printer models?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an capability designed for full A4 page layouts.
- **FR-002**: System MUST provide a capability designed for 80mm thermal roll printers.
- **FR-003**: System MUST include the store logo, store name, phone number, and address in the document header for both formats.
- **FR-004**: System MUST display complete invoice details, including Invoice Number, Date, Customer/Supplier name, and Payment Type.
- **FR-005**: System MUST list all invoice items with their Code, Name, Quantity (formatted to 3 decimal places), Unit Price/Cost, Discount, and Line Total (formatted to 2 decimal places).
- **FR-006**: System MUST calculate and display the SubTotal, Tax Amount, Invoice Discount, Total Amount, Paid Amount, and Due Amount.
- **FR-007**: System MUST display a Print Preview dialog before executing the physical print job, allowing the user to select the printer and verify the layout.
- **FR-008**: Documents MUST be fully localized in Arabic (RTL layout) in accordance with the project's global rules.

### Key Entities

- **StoreSettings**: Contains the Store Name, Phone, Address, and Logo path to be printed in the header.
- **Invoice / Receipt Data**: The aggregated data representing the printed document, including header information, lines, and totals.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A full A4 invoice prints correctly on standard paper without margin overflow or text cut-offs.
- **SC-002**: An 80mm thermal receipt prints correctly and legibly on a standard POS printer, with proper text wrapping.
- **SC-003**: Print preview generates in under 1 second for an invoice containing up to 50 items.
- **SC-004**: 100% of financial values on printed documents use the correct decimal precision (2dp for money, 3dp for quantities).

## Assumptions

- Store settings (Logo, Name, Address, Phone) are already available in the system and can be fetched.
- The host operating system handles the printer driver installation and spooling.
- The UI layer provides standard print document and print preview dialog capabilities.
- The printed documents are read-only and represent a snapshot of the invoice data at the time of printing.
