# Feature Specification: Identifier Strategy & Validation (v4.5.3–v4.6.2)

**Feature Branch**: `013-identifier-validation`
**Created**: 2026-05-25
**Status**: Draft
**Input**: Phase 13 — Identifier Strategy & Validation (v4.5.3–v4.6.2)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Simplified Entity Identification (Priority: P1)

Users currently see arbitrary "Code" fields on products, customers, and suppliers that cause confusion alongside auto-generated IDs and barcodes. By removing these legacy codes, the system relies entirely on standard database IDs and specific barcodes, eliminating duplicate code errors and race conditions during fast data entry.

**Independent Test**: Create a new Product via the API or Desktop client. The request payload contains no "Code" field. The response returns the auto-incremented database `Id` and `Barcode`. Attempting to query or reference the product via the legacy "Code" property is no longer possible.

**Acceptance Scenarios**:

1. **Given** a user is creating or editing a master data entity (Product, Customer, Supplier, Warehouse, Category, Unit, User), **When** they view the form, **Then** there is no "Code" field visible.
2. **Given** the system generates new entities, **When** they are saved, **Then** it no longer relies on a sequence generator for these entities (reserving sequence generation strictly for financial documents like Invoices and Receipts).

---

### User Story 2 — Immediate Visual Validation Feedback (Priority: P1)

A cashier is rapidly entering a new customer but forgets to provide the mandatory "Phone Number". Instead of only finding out when they click save, the input field immediately highlights with a clear visual warning as soon as the data is invalid, preventing wasted time.

**Independent Test**: Open the Customer Editor. Clear the "Name" field and tab away. A red border immediately appears around the text box, along with a ❗ icon. Hovering over the icon displays the specific Arabic validation error (e.g., "اسم العميل مطلوب").

**Acceptance Scenarios**:

1. **Given** a user is interacting with an editor form, **When** they enter invalid data or leave a mandatory field empty, **Then** a red border and a warning icon appear on the specific field.
2. **Given** a field is marked as invalid, **When** the user hovers over the warning icon, **Then** a helpful ToolTip explains exactly why the field is invalid.

---

### User Story 3 — Comprehensive Save Validation Dialogue (Priority: P1)

A manager fills out a complex inventory transfer but misses three required fields. When they click the "Save" button (which is always enabled, never visually disabled or "greyed out"), a clean warning dialogue appears listing exactly which three fields are missing, allowing them to quickly rectify the issues.

**Independent Test**: Open an editor, leave multiple required fields blank. Click the "Save" button. A stylized warning dialogue appears summarizing all errors (e.g., "يرجى إكمال البيانات الإلزامية: \n - اسم المنتج مطلوب \n - السعر يجب أن يكون أكبر من الصفر").

**Acceptance Scenarios**:

1. **Given** a user is filling out a form, **When** they look at the primary action buttons (e.g., Save, Post), **Then** the buttons are always enabled and clickable regardless of form state.
2. **Given** a form has invalid or missing data, **When** the user clicks "Save", **Then** the save action is intercepted and a warning dialogue appears listing all validation errors present on the form.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST completely remove the `Code` property and database column from all non-document entities: Products, Customers, Suppliers, Warehouses, Categories, Units, and Users.
- **FR-002**: The `DocumentSequenceService` MUST be restricted to generate sequences ONLY for financial document entities (e.g., Sales Invoices, Purchase Invoices, Receipts, Returns).
- **FR-003**: All API requests, responses, and Data Transfer Objects (DTOs) MUST NOT contain a `Code` field.
- **FR-004**: The system MUST implement a standard UI Error Template across the desktop application that highlights invalid inputs with a red border and an interactive warning icon.
- **FR-005**: All 14 editor screens in the desktop application MUST adopt a standardized validation interface that immediately reports property-level errors to the UI.
- **FR-006**: Primary action buttons (like Save or Submit) MUST NOT use logic to disable themselves based on validation state; they must remain enabled to provide interactive feedback.
- **FR-007**: When a user triggers a save action on an invalid form, the system MUST compile all current validation errors and display them in a single, formatted warning dialogue via the centralized Dialog Service.

---

## Success Criteria *(mandatory)*

- **SC-001**: 0 occurrences of the property name "Code" exist on master data entities in the domain model.
- **SC-002**: 100% of master data database tables successfully migrate to drop the `Code` column without data loss on the primary `Id` column.
- **SC-003**: 100% of editor ViewModels in the desktop client use the standardized validation interface instead of manual boolean flags (e.g., removing `HasNameError`, `HasPriceError`).
- **SC-004**: 100% of form submissions on invalid data trigger the aggregated warning dialogue rather than silently failing or relying on disabled buttons.

---

## Assumptions

- Auto-incrementing database `Id` integers are sufficient for internal entity linking. User-facing identification will rely on `Name` or explicit `Barcode` where applicable.
- The removal of the `Code` column requires a database migration, but since this is v4.5.3-v4.6.2, we assume existing production data does not rely on `Code` for critical business relationships outside of the database schema (i.e. no external system relies on the Code string).
