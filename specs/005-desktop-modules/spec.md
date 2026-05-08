# Feature Specification: Desktop Modules

**Feature Branch**: `005-desktop-modules`  
**Created**: 2026-05-08  
**Status**: Draft  
**Input**: User description: "Phase 5 — Desktop Modules: Replace all placeholder controls with fully functional WinForms modules that communicate with the backend API. Each module provides list views with search/filter, add/edit dialogs, delete/deactivate with confirmation, and EventBus-driven cross-module refresh."

## Clarifications

### Session 2026-05-08

- Q: Should the Sales/Purchase module UI include tax calculation logic in Phase 5? → A: Add a "Tax Inclusive/Exclusive" toggle on the invoice form itself to handle tax calculations dynamically.
- Q: Should the system allow recording a payment that results in a negative balance? → A: Allow overpayments (balance can become negative), enabling credit balances for customers and suppliers.
- Q: Should the system support exporting reports to external formats in Phase 5? → A: Export to both Excel and CSV for all generated reports.
- Q: Should there be a UI mechanism to view and re-activate deactivated entities? → A: Add a "Show Deactivated" toggle on each list screen to allow managers to find and re-activate entities.
- Q: How should Categories and Units management be accessed? → A: Accessible via entry points inside the Product Editor dialog for on-the-fly management.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Products CRUD Module (Priority: P1)

A manager opens the Products screen and sees a searchable, filterable table of all products. They can add a new product by filling out a form (Code, Barcode, Name, Category, Unit, PurchasePrice, SalePrice, MinStock, Description), where Categories and Units can be managed on-the-fly via quick-add buttons. They can edit an existing product inline or via dialog, and deactivate a product with a confirmation prompt. When a product is added or modified, every other open screen that displays product data automatically refreshes.

**Why this priority**: Products is the foundational CRUD module. Its implementation establishes the reusable pattern (List + Editor + EventBus) that all subsequent modules replicate. Every financial module depends on product data.

**Independent Test**: Open the Products screen, add a product via the form, verify it appears in the list. Edit a field, verify the change persists after a page reload. Deactivate a product, confirm it disappears from the default list. Verify search by Name, Code, and Barcode returns matching results. Verify filter by Category and Active status works correctly.

**Acceptance Scenarios**:

1. **Given** a manager is on the Products screen, **When** they click "Add New", fill out all required fields, and click Save, **Then** the product is created via the API and appears in the list within 2 seconds.
2. **Given** products exist in the system, **When** the user types in the search bar, **Then** after 300ms debounce, the list filters to show only products matching by Name, Code, or Barcode.
3. **Given** a product is selected, **When** the user clicks "Edit", modifies the SalePrice, and saves, **Then** the updated price is persisted and all screens displaying that product refresh automatically via EventBus.
4. **Given** a product is selected, **When** the user clicks "Deactivate" and confirms the dialog, **Then** the product is soft-deleted (IsActive=false) and removed from the default active-only list.
5. **Given** the user is a Cashier, **When** they navigate to Products, **Then** they cannot access the screen (role restriction: ManagerAndAbove).
6. **Given** products with categories exist, **When** the user selects a Category filter, **Then** only products in that category are displayed.
7. **Given** deactivated products exist, **When** the user toggles "Show Deactivated", **Then** the list includes inactive products, allowing them to be edited or re-activated.

---

### User Story 2 - Customers & Suppliers CRUD Modules (Priority: P1)

A manager opens the Customers screen and views a table of all customers with their current balance. They can add, edit, or deactivate customers. The same pattern applies to Suppliers. Both modules display balance information (how much is owed to/by the entity) and support search by name or phone number.

**Why this priority**: Customers and Suppliers are required dependencies for Sales and Purchase invoices respectively. Without these modules, financial transactions cannot reference the correct parties.

**Independent Test**: Open the Customers screen, add a customer with Name, Phone, and Address. Verify the customer appears in the list with a zero balance. Edit the customer's phone number. Deactivate the customer. Repeat the same flow for Suppliers.

**Acceptance Scenarios**:

1. **Given** a manager is on the Customers screen, **When** they add a new customer with Name, Phone, and Address, **Then** the customer is created with CurrentBalance = 0.
2. **Given** customers exist, **When** the user searches by name or phone, **Then** the list filters to matching results within 500ms.
3. **Given** a customer has a non-zero balance, **When** the user views the customer list, **Then** the balance is displayed in the correct format (2 decimal places) with color coding (red for owing, green for credit).
4. **Given** the user is a Cashier, **When** they access Customers, **Then** they can view the list but cannot add, edit, or deactivate customers.
5. **Given** a supplier is selected, **When** the user deactivates them, **Then** a confirmation dialog appears in Arabic, and upon confirmation, the supplier is soft-deleted.
6. **Given** deactivated customers/suppliers exist, **When** the user toggles "Show Deactivated", **Then** they appear in the list for re-activation.

---

### User Story 3 - Warehouses Module (Priority: P1)

An admin opens the Warehouses screen to manage storage locations. They can add new warehouses, edit warehouse details, mark one warehouse as the default, and view stock levels per warehouse. Only admins have access to warehouse management.

**Why this priority**: Warehouses are required for all inventory operations — sales, purchases, returns, and transfers all reference a specific warehouse.

**Independent Test**: Open the Warehouses screen, add a warehouse with Name and Address. Set it as default. Verify stock levels display correctly per product per warehouse.

**Acceptance Scenarios**:

1. **Given** an admin is on the Warehouses screen, **When** they add a new warehouse, **Then** it appears in the list and is available as a selection in Sales, Purchases, and Transfer screens.
2. **Given** multiple warehouses exist, **When** the admin sets one as IsDefault, **Then** the previous default is unset and the new default is highlighted.
3. **Given** a warehouse has stock, **When** the admin views warehouse details, **Then** a per-product stock breakdown is displayed with current quantities.
4. **Given** the user is a Manager or Cashier, **When** they try to access Warehouses, **Then** they are denied access (AdminOnly restriction).

---

### User Story 4 - Sales Invoice Module (Priority: P1)

A cashier creates a new sales invoice by selecting a customer (or using the default Cash customer), choosing a source warehouse, adding products with quantities and prices, applying line-level and invoice-level discounts, specifying a tax-inclusive or tax-exclusive calculation toggle, specifying payment type (Cash/Credit/Mixed), and posting the invoice. The system validates stock availability, computes totals and taxes automatically, deducts stock, and updates customer balance upon posting.

**Why this priority**: Sales is the most important business function — it is the primary revenue-generating operation for the retail shop.

**Independent Test**: Create a sales invoice with 3 line items, apply a line discount to one item and an invoice-level discount. Post the invoice. Verify stock is deducted from the correct warehouse, customer balance is updated for credit sales, and the invoice number follows the format INV-YYYY-NNNNNN.

**Acceptance Scenarios**:

1. **Given** a cashier opens the Sales screen, **When** they click "New Invoice", **Then** a blank invoice form appears with the default Cash customer and default warehouse pre-selected.
2. **Given** an invoice is being created, **When** the user adds a product by scanning a barcode or searching by name, **Then** the product is added as a line item with its default SalePrice.
3. **Given** line items exist, **When** the user modifies quantity, applies a discount, or toggles tax mode, **Then** the LineTotal, SubTotal, TaxAmount, TotalAmount, and DueAmount recalculate instantly in the UI.
4. **Given** the payment type is Mixed, **When** the user enters a PaidAmount, **Then** the DueAmount is calculated as TotalAmount - PaidAmount, and the system validates PaidAmount ≤ TotalAmount.
5. **Given** sufficient stock exists, **When** the invoice is posted, **Then** stock is deducted from the selected warehouse, InventoryMovements are created, and customer balance is updated if DueAmount > 0.
6. **Given** insufficient stock for any line item, **When** the user attempts to post, **Then** a clear Arabic error message identifies which product(s) have insufficient stock.
7. **Given** a posted invoice exists, **When** the user views the invoice list, **Then** they can see invoice details but cannot edit the posted invoice.
8. **Given** a draft invoice exists, **When** the user edits and saves it, **Then** changes are persisted without affecting stock or balance.

---

### User Story 5 - Purchase Invoice Module (Priority: P1)

A manager creates a purchase invoice by selecting a supplier, choosing a destination warehouse, adding products with quantities and unit costs, applying discounts and tax toggles, and specifying payment type. On posting, stock increases in the destination warehouse and supplier balance updates.

**Why this priority**: Purchases are the primary way stock enters the system. Without this module, the shop cannot replenish inventory.

**Independent Test**: Create a purchase invoice with multiple items, post it. Verify stock increased in the destination warehouse and supplier balance updated correctly.

**Acceptance Scenarios**:

1. **Given** a manager opens Purchases, **When** they create a new invoice and select a supplier, **Then** the invoice form shows supplier details and the default warehouse as destination.
2. **Given** a purchase invoice is posted, **When** the user checks warehouse stock, **Then** quantities for each purchased product have increased by the purchased amounts.
3. **Given** a purchase invoice with Credit payment, **When** posted, **Then** the supplier's CurrentBalance increases by the DueAmount (we owe the supplier).
4. **Given** the user is a Cashier, **When** they try to access Purchases, **Then** access is denied (ManagerAndAbove restriction).
5. **Given** a posted purchase invoice, **When** the user attempts to cancel it, **Then** stock is reversed (decreased) and supplier balance is reversed.

---

### User Story 6 - Returns Modules (Priority: P2)

A cashier processes a sales return by optionally referencing the original sales invoice, selecting products to return with quantities, and posting the return. On posting, stock increases back into the warehouse and customer balance decreases. Similarly, a manager processes purchase returns (stock decreases, supplier balance decreases).

**Why this priority**: Returns handle the reversal of sales and purchases — essential for accurate inventory and balance tracking, but secondary to the primary transaction flows.

**Independent Test**: Create a sales return referencing an original invoice. Verify the return quantity is validated against the original sold quantity minus previously returned quantities. Post the return and verify stock and balance reversal.

**Acceptance Scenarios**:

1. **Given** a sales return references an original invoice, **When** the user enters a return quantity exceeding (sold qty - already returned qty), **Then** validation prevents posting with a clear error message.
2. **Given** a sales return is posted, **When** the user checks warehouse stock, **Then** stock for returned products has increased.
3. **Given** a sales return is posted for a credit sale, **When** the user checks customer balance, **Then** the balance has decreased by the return amount.
4. **Given** a purchase return is posted, **When** the user checks warehouse stock, **Then** stock for returned products has decreased.
5. **Given** a purchase return is posted, **When** the user checks supplier balance, **Then** the supplier balance has decreased by the return amount.

---

### User Story 7 - Stock Transfer Module (Priority: P2)

A manager creates a stock transfer between two warehouses by selecting a source and destination warehouse, adding products with transfer quantities, and posting the transfer. The system validates that source ≠ destination and that sufficient stock exists in the source warehouse.

**Why this priority**: Stock transfers enable multi-warehouse inventory balancing, required for shops with more than one storage location.

**Independent Test**: Create a stock transfer from Warehouse A to Warehouse B with 10 units of Product X. Post it. Verify Warehouse A stock decreased by 10 and Warehouse B stock increased by 10.

**Acceptance Scenarios**:

1. **Given** a manager opens Stock Transfers, **When** they select the same warehouse as both source and destination, **Then** the system prevents submission with an error.
2. **Given** the source warehouse has 20 units of Product X, **When** the user transfers 15 units, **Then** source stock becomes 5 and destination stock increases by 15.
3. **Given** the source warehouse has 5 units, **When** the user attempts to transfer 10 units, **Then** validation prevents posting with an insufficient stock error.
4. **Given** a transfer is posted, **When** InventoryMovements are checked, **Then** two records exist: TransferOut for source and TransferIn for destination.

---

### User Story 8 - Payments Module (Priority: P2)

A manager records customer payments (cash received from customer → decreases customer balance) and supplier payments (cash paid to supplier → decreases supplier balance). Payments can optionally be linked to a specific invoice.

**Why this priority**: Payments close the credit cycle for both customers and suppliers, enabling accurate balance tracking.

**Independent Test**: Record a customer payment of 500, verify customer balance decreases by 500. Record a supplier payment of 300, verify supplier balance decreases by 300. Record an overpayment (e.g., 1000 on a 500 balance) and verify the balance becomes negative (-500).

**Acceptance Scenarios**:

1. **Given** a customer has a balance of 1000, **When** a payment of 500 is recorded, **Then** the customer balance becomes 500.
2. **Given** a supplier has a balance of 2000 (we owe them), **When** a payment of 800 is recorded, **Then** the supplier balance becomes 1200.
3. **Given** a payment is being created, **When** the user optionally links it to a specific invoice, **Then** the payment is associated with that invoice for audit purposes.
4. **Given** a customer has a balance of 1000, **When** a payment of 1500 is recorded, **Then** the customer balance becomes -500 (credit) and a confirmation notification appears.
5. **Given** the payment list screen, **When** the user filters by date range, **Then** only payments within the selected range are displayed.

---

### User Story 9 - Reports Module (Priority: P3)

A manager accesses the Reports screen and can generate: Daily Sales Report, Daily Purchases Report, Stock Report (per warehouse or all), Customer Balance Report, Supplier Balance Report, Product Movement Report, and Low Stock Alert Report. All reports are filterable by date range and/or entity, can be viewed on-screen, and exported to Excel or CSV.

**Why this priority**: Reports provide business intelligence but do not affect transactional data. The system can operate fully without reports initially.

**Independent Test**: Generate a Daily Sales Report for a specific date range. Verify the report shows correct invoice totals, item counts, and payment breakdowns. Export the report to Excel and CSV, and verify the exported files contain the correct data.

**Acceptance Scenarios**:

1. **Given** sales data exists for a date range, **When** the user generates a Daily Sales Report, **Then** the report displays invoice count, total revenue, and payment type breakdown.
2. **Given** warehouse stock data exists, **When** the user generates a Stock Report for a specific warehouse, **Then** all products with their current quantities in that warehouse are listed.
3. **Given** products with MinStock configured, **When** the user generates a Low Stock Alert Report, **Then** only products where current stock < MinStock are shown.
4. **Given** a customer has transactions, **When** the user generates a Customer Balance Report for that customer, **Then** all invoices, returns, and payments affecting that customer's balance are listed.
5. **Given** reports are generated, **When** the user views them, **Then** data loads within 5 seconds for up to 1 year of transaction history.
6. **Given** a generated report, **When** the user clicks "Export to Excel" or "Export to CSV", **Then** a file save dialog appears and the report data is saved in the chosen format.

---

### User Story 10 - Dashboard Overview (Priority: P3)

When a user logs in, the Dashboard screen displays summary cards showing today's sales total, today's purchases total, total customers, total products, low stock alerts count, and recent transactions. The dashboard auto-refreshes when any financial event occurs via the EventBus.

**Why this priority**: The dashboard provides at-a-glance business health but is not required for core operations.

**Independent Test**: Log in and verify summary cards display correct totals. Create a sales invoice and verify the dashboard updates automatically.

**Acceptance Scenarios**:

1. **Given** the user logs in, **When** the Dashboard loads, **Then** summary cards display today's sales, purchases, product count, customer count, and low stock alert count.
2. **Given** the Dashboard is open, **When** a sales invoice is posted from another screen, **Then** the Dashboard refreshes its totals automatically via EventBus.
3. **Given** the user is a Cashier, **When** the Dashboard loads, **Then** it shows only sales-related summary cards (not purchases or supplier data).

---

### Edge Cases

- What happens when the API is unreachable while the user is working? The module displays a clear Arabic error notification via ToastForm and retains any unsaved data in the form.
- What happens when two users try to edit the same product simultaneously? The last save wins (optimistic concurrency); the first user's changes are overwritten, and both see the latest data on next refresh.
- What happens when a user adds a product to an invoice that gets deactivated by another user mid-entry? On posting, validation fails with a clear message identifying the deactivated product.
- What happens when the user enters Arabic text in search fields? The system supports Arabic search natively since all text columns use nvarchar.
- What happens when a sales invoice has zero line items? Validation prevents posting an invoice with no items.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST implement a reusable CRUD module pattern consisting of a list view (DataGridView with SearchBar), an editor dialog/panel, and EventBus integration for cross-module updates.
- **FR-002**: System MUST provide search functionality with 300ms debounce on all list screens, searching across Name and Code/Barcode fields.
- **FR-003**: System MUST support category-based and status-based filtering on list screens where applicable, including a "Show Deactivated" toggle.
- **FR-004**: System MUST enforce role-based access control at the UI level — modules visible only to authorized roles per the Permissions Matrix.
- **FR-005**: System MUST compute all financial totals (LineTotal, SubTotal, TaxAmount, TotalAmount, DueAmount) in real-time as the user modifies invoice line items or toggles the tax mode.
- **FR-006**: System MUST validate stock availability before allowing invoice posting, displaying specific per-item error messages in Arabic.
- **FR-007**: System MUST use soft-delete (IsActive=false) for all entity deactivation — no hard deletes.
- **FR-008**: System MUST publish EventBus messages (entity ID only, no data payloads) after every create, update, or deactivate operation.
- **FR-009**: System MUST display all monetary values formatted to 2 decimal places and all quantities to 3 decimal places.
- **FR-010**: System MUST support the full invoice lifecycle: Draft → Posted → Cancelled, with stock and balance changes only on Post.
- **FR-011**: System MUST auto-generate invoice numbers in the prescribed formats (INV-YYYY-NNNNNN, PUR-YYYY-NNNNNN, etc.).
- **FR-012**: System MUST validate PaidAmount ≤ TotalAmount on all invoices before posting.
- **FR-013**: System MUST support Mixed payment type where the user enters PaidAmount and the system calculates DueAmount.
- **FR-014**: System MUST display a loading overlay during API calls and show success/error toast notifications upon completion.
- **FR-015**: System MUST support barcode scanner input (keyboard simulation) for adding products to invoices.
- **FR-016**: System MUST record InventoryMovements for every stock change with full audit trail (QuantityBefore, QuantityAfter, MovementType, ReferenceId).
- **FR-017**: System MUST render all UI text in Arabic with full RTL layout support.
- **FR-018**: System MUST allow returns to optionally reference an original invoice and validate return quantities against remaining returnable quantities.
- **FR-019**: System MUST prevent stock transfers where source and destination warehouses are the same.
- **FR-020**: System MUST provide 7 report types: Daily Sales, Daily Purchases, Stock, Customer Balance, Supplier Balance, Product Movement, and Low Stock Alert.
- **FR-021**: System MUST support exporting all reports to Excel (.xlsx) and CSV (.csv) formats.

### Key Entities

- **Product**: The central catalog item with Code, Barcode, Name, Category, Unit, PurchasePrice, SalePrice, MinStock. Referenced by all invoice types.
- **Customer**: The buyer entity with Name, Phone, Address, and CurrentBalance. Positive balance means customer owes money.
- **Supplier**: The vendor entity with Name, Phone, Address, and CurrentBalance. Positive balance means we owe the supplier.
- **Warehouse**: A storage location with Name, Address, and IsDefault flag. Contains WarehouseStocks per product.
- **SalesInvoice**: A sales transaction header with Customer, Warehouse, Status (Draft/Posted/Cancelled), PaymentType, line items, and computed totals.
- **PurchaseInvoice**: A purchase transaction header with Supplier, Warehouse, Status, PaymentType, line items, and computed totals.
- **SalesReturn / PurchaseReturn**: Reversal documents optionally referencing original invoices, affecting stock and balance in the opposite direction.
- **StockTransfer**: A document moving stock between two warehouses with source, destination, and transfer items.
- **CustomerPayment / SupplierPayment**: Cash receipt/disbursement records that decrease entity balances.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete a full sales invoice (add 5 items, apply discounts, post) in under 3 minutes.
- **SC-002**: Users can complete a full purchase invoice in under 3 minutes.
- **SC-003**: Product search returns results within 500ms of the user finishing typing.
- **SC-004**: All module list screens load their initial data within 2 seconds.
- **SC-005**: Invoice posting (including stock deduction and balance update) completes within 2 seconds.
- **SC-006**: EventBus-driven cross-module refresh occurs within 1 second of the triggering operation completing.
- **SC-007**: All 10 modules are fully functional and pass their acceptance scenarios.
- **SC-008**: Reports generate within 5 seconds for up to 1 year of transaction data.
- **SC-009**: 100% of role-based access restrictions are enforced correctly — unauthorized users cannot access restricted modules.
- **SC-010**: Zero data inconsistencies: every posted invoice produces correct stock changes, balance updates, and inventory movement records.

## Assumptions

- The backend API (Phase 2 & 3) is fully functional and all endpoints are operational for Products, Customers, Suppliers, Warehouses, Sales, Purchases, Returns, Transfers, Payments, and Reports.
- The Desktop Shell (Phase 4) is complete with NavigationService, EventBus, NotificationService, DialogService, and all common controls (SearchBar, LoadingOverlay, SummaryCard, MoneyTextBox) already implemented.
- Placeholder controls exist in `Controls/Placeholders/` and will be replaced with fully functional module controls organized in dedicated subdirectories (e.g., `Controls/Products/`).
- The system operates on a local network — latency between Desktop and API is minimal (<50ms).
- All 13 existing placeholder UserControls registered in `Program.cs` DI will be replaced or updated with real implementations.
- Categories and Units management (supporting CRUD for product metadata) will be implemented as sub-dialogs accessible from the Product Editor screen.
- Print functionality for invoices is out of scope for this phase (covered in Phase 6).
- User Management and Settings screens are out of scope for this phase (covered in Phase 7).
