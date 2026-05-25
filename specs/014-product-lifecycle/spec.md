# Feature Specification: Product Lifecycle & Media Management (Phase 14)

**Feature Branch**: `014-product-lifecycle`
**Created**: 2026-05-25
**Status**: Draft
**Input**: Phase 14 — Product Lifecycle & Media Management

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Optional Media & Expiration Tracking (Priority: P1)

A store manager adds a new shipment of dairy products. They need to easily record the expiration dates for these specific products to track them later, and optionally attach a photo of the product to help cashiers identify it quickly. However, they also sell non-perishable goods (like plastics) which don't have expiration dates.

**Independent Test**: Create a product and check the "Has Expiration Date" box. The date picker activates. Upload a `.jpg` image under 2MB. Save the product. Re-open the product and verify the date and image are correctly displayed without UI lag. Create a second product leaving the box unchecked, verifying it saves successfully without an expiration date.

**Acceptance Scenarios**:

1. **Given** a user is creating or editing a product, **When** they toggle the "Has Expiration Date" (له تاريخ انتهاء) option, **Then** the expiration date input becomes active or disabled accordingly.
2. **Given** the expiration date option is enabled during a new product/stock entry, **When** the user attempts to enter a date in the past, **Then** the system rejects the entry with a validation error.
3. **Given** a user wants to add an image to a product, **When** they upload a standard image file (JPG, PNG) under the maximum allowed size (e.g., 2MB), **Then** the image is previewed and attached to the product seamlessly without lagging the application.

---

### User Story 2 — Proactive Expiration Dashboard Notifications (Priority: P2)

The business owner opens the application at the start of the workday. They want the system to automatically inform them if any products are expired or nearing expiration so they can take action (like discounting them) before taking a total loss, without having to manually dig through reports every day.

**Independent Test**: Configure a product to expire tomorrow. Restart the application and view the main dashboard. A lightweight, non-intrusive badge or notification should appear indicating that a product requires attention regarding its expiration date.

**Acceptance Scenarios**:

1. **Given** the application is launched or the main dashboard is opened, **When** there are products in stock that are expired or within the near-expiry threshold (e.g., 30 days), **Then** a proactive, non-disruptive notification or badge appears to alert the user.

---

### User Story 3 — Expired Stock Management & Accounting Write-Offs (Priority: P1)

An inventory manager uses the expiration report to find completely expired goods. They need to remove these items from the active stock to prevent accidental sales.

**Independent Test**: Open the Expired Products Report. Select an expired product line and click the write-off button ("ترحيل كحذف/إتلاف"). Verify the product's available quantity in the warehouse is immediately decreased.

**Acceptance Scenarios**:

1. **Given** a user accesses the dedicated Expiration Report, **When** they view the data, **Then** they can filter the view to see currently expired items versus items nearing expiration.
2. **Given** a user identifies expired stock in the report, **When** they click the write-off action, **Then** the system permanently removes the specified quantity from the active warehouse stock.
3. **Given** an expired stock write-off is executed, **When** the transaction completes, **Then** the system logs the event in the write-off history table.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support storing an optional Expiration Date for products.
- **FR-002**: The UI MUST dynamically enable the expiration date picker only when the user explicitly checks the "Has Expiration Date" toggle.
- **FR-003**: The system MUST validate that newly entered expiration dates for incoming stock are not in the past.
- **FR-004**: The system MUST allow users to upload, preview, and delete an optional product image.
- **FR-005**: The system MUST validate image uploads, restricting formats to JPG/PNG and enforcing a maximum file size (e.g., 2MB).
- **FR-006**: Product images MUST be stored on the local file system (e.g., `%AppData%`) rather than directly inside the database, storing only the file path in the database to prevent database bloat.
- **FR-007**: The UI MUST load images asynchronously (Lazy Loading) to guarantee smooth scrolling and zero lag on product lists.
- **FR-008**: The system MUST provide a dedicated reporting view to filter and display "Expired Items" and "Near-Expiry Items" (configurable threshold, e.g., 30, 60, 90 days).
- **FR-009**: The Expired Products Report MUST include an action to write-off (إتلاف) selected expired quantities.
- **FR-010**: Executing a write-off MUST immediately deduct the quantity from active warehouse stock and log a record in a dedicated write-off history table.
- **FR-012**: The main dashboard MUST automatically check for expired/near-expiry products upon loading and display a proactive, non-blocking notification if any exist.

---

## Success Criteria *(mandatory)*

- **SC-001**: Users can successfully save products with or without expiration dates and images without errors.
- **SC-002**: Database backup (`.bak`) file sizes do not bloat disproportionately when hundreds of product images are added, verifying the external storage strategy.
- **SC-003**: The product list UI scrolls smoothly at 60fps even when displaying 100+ products with attached images.
- **SC-004**: Writing off an expired product successfully reduces the reported warehouse stock by the exact written-off amount.

---

## Assumptions

- The host machine running the desktop application will have sufficient local disk space in the user's `%AppData%` directory to store the product image files.
- Near-expiry thresholds (e.g., 30/60 days) can be hardcoded as predefined UI dropdown options or read from an existing system settings table.
