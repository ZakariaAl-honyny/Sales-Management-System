# Feature Specification: Business Logic Implementation

**Feature Branch**: `003-business-logic`
**Created**: 2026-05-07
**Status**: Draft
**Input**: User description: "Phase 3 — Business Logic (Critical)"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete Purchase Flow (Priority: P1)

A Manager logs in and creates a purchase invoice to restock inventory. They select a supplier, choose a destination warehouse, add one or more products with quantities and unit costs, optionally apply line-level or invoice-level discounts, specify a payment type (Cash, Credit, or Mixed), and post the invoice. The system validates inputs, saves the invoice, increases stock levels in the destination warehouse for each line item, records a corresponding inventory movement for every stock change, and — for Credit or Mixed payment types — increases the supplier's outstanding balance by the amount still owed.

**Why this priority**: Purchasing is the entry point for all inventory. Without a functional purchase flow the system has no stock to sell and no supplier ledger to track, making every other business operation impossible.

**Independent Test**: Create a purchase invoice via the API for a known supplier and warehouse, post it, then verify the following are all true simultaneously:
- The invoice exists with the correct `SubTotal`, `TotalAmount`, `PaidAmount`, and `DueAmount`
- Each `WarehouseStock` row for the involved products has increased by the exact purchased quantity
- One `InventoryMovement` record of type `PurchaseIn` exists per line item with correct `QuantityBefore` and `QuantityAfter`
- The supplier's `CurrentBalance` increased by `DueAmount` when `PaymentType` is Credit or Mixed
- If the API call is repeated with an intentionally bad line (e.g., negative quantity) the entire transaction is absent from the database

**Acceptance Scenarios**:

1. **Given** a Manager is authenticated and a supplier, warehouse, and at least one active product exist, **When** they submit a valid Create Purchase Invoice request with Cash payment type and post it, **Then** the invoice is created with `Status = Posted`, stock increases for each line, one `InventoryMovement` of type `PurchaseIn` is created per line, and the supplier's `CurrentBalance` is unchanged.

2. **Given** a Manager submits a purchase invoice with `PaymentType = Credit` and `PaidAmount = 0`, **When** the invoice is posted, **Then** `DueAmount = TotalAmount` and the supplier's `CurrentBalance` increases by `DueAmount`.

3. **Given** a Manager submits a purchase invoice with `PaymentType = Mixed`, `TotalAmount = 1000`, and `PaidAmount = 400`, **When** the invoice is posted, **Then** `DueAmount = 600` and the supplier's `CurrentBalance` increases by `600`.

4. **Given** a Manager submits a purchase invoice where `PaidAmount > TotalAmount`, **When** the request is processed, **Then** the system rejects it with a validation error and no data is persisted.

5. **Given** a posted purchase invoice exists, **When** a Manager cancels it, **Then** `Status = Cancelled`, stock decreases back to pre-purchase levels, one `InventoryMovement` of type `PurchaseReturnOut` is created per line, and the supplier's `CurrentBalance` is reduced back to its original value.

---

### User Story 2 - Complete Sales Flow (Priority: P1)

A Cashier (or Manager) logs in and processes a sale. They select a customer (or use the default walk-in cash customer), choose a source warehouse, add products with quantities and unit prices, optionally apply discounts, specify a payment type, and post the invoice. Before saving the invoice, the system verifies that each product has enough stock in the selected warehouse. On success it reduces stock, records inventory movements, and updates the customer's outstanding balance if the payment is not fully paid in cash.

**Why this priority**: Sales processing is the primary revenue operation. Accurate stock deduction and balance tracking at point-of-sale are non-negotiable for financial integrity.

**Independent Test**: Create a sales invoice via the API for a customer against a warehouse that has known stock levels, post it, then verify:
- Invoice exists with correct totals and `Status = Posted`
- Each `WarehouseStock` row decreased by the exact sold quantity
- One `InventoryMovement` of type `SaleOut` per line item with correct before/after quantities
- Customer `CurrentBalance` increased by `DueAmount` when payment is Credit or Mixed
- Attempting to sell more than available stock returns a validation error and leaves stock unchanged

**Acceptance Scenarios**:

1. **Given** a Cashier is authenticated and sufficient stock exists in the selected warehouse, **When** they post a sales invoice with `PaymentType = Cash` and `PaidAmount = TotalAmount`, **Then** the invoice is `Posted`, stock decreases per line, one `SaleOut` movement per line is recorded, and the customer's `CurrentBalance` is unchanged.

2. **Given** a sales invoice is submitted with `PaymentType = Credit` and `PaidAmount = 0`, **When** it is posted, **Then** `DueAmount = TotalAmount` and the customer's `CurrentBalance` increases by `DueAmount`.

3. **Given** a sales invoice line item requests a quantity greater than the current `WarehouseStock.Quantity`, **When** the invoice is submitted, **Then** the system returns a descriptive validation error identifying the specific product and the shortfall amount, and no database changes are made.

4. **Given** a posted sales invoice exists, **When** a Manager cancels it, **Then** `Status = Cancelled`, stock is restored to pre-sale levels, `SaleReturnIn` movements are recorded per line, and the customer's `CurrentBalance` is reduced back to its original value.

5. **Given** a Cashier attempts to post an invoice that is already `Posted` or `Cancelled`, **When** the request is processed, **Then** the system returns a state-transition validation error and makes no changes.

---

### User Story 3 - Return Processing (Priority: P2)

A Manager processes a merchandise return — either a sales return (customer returning goods) or a purchase return (returning goods to supplier). They optionally reference the original invoice, specify the products and quantities being returned, and post the return. The system validates return quantities against the original transaction, creates the return record, reverses stock movements in the correct direction, and adjusts the customer or supplier balance accordingly.

**Why this priority**: Returns are a routine retail operation. Unhandled returns corrupt both stock counts and ledger balances, undermining business reporting.

**Independent Test**:
1. Create and post a sales invoice for 10 units of Product A
2. Create and post a sales return for 4 units referencing that invoice
3. Verify stock increased by 4 in the same warehouse
4. Verify a `SaleReturnIn` inventory movement exists with correct quantities
5. Verify the customer's balance decreased by the return amount
6. Repeat mirrored steps for a purchase return and verify `PurchaseReturnOut` movements and supplier balance reduction

**Acceptance Scenarios**:

1. **Given** a posted sales invoice exists for 10 units of Product A, **When** a Manager posts a sales return for 4 units referencing that invoice, **Then** stock increases by 4, a `SaleReturnIn` inventory movement is created, and the customer's `CurrentBalance` decreases by the return line total.

2. **Given** a Manager attempts to return 11 units against a sales invoice that sold only 10, **When** the return is submitted, **Then** the system rejects it with a validation error stating the maximum returnable quantity.

3. **Given** a posted purchase invoice exists for 20 units of Product B, **When** a Manager posts a purchase return for 5 units, **Then** stock decreases by 5 in the relevant warehouse, a `PurchaseReturnOut` movement is created, and the supplier's `CurrentBalance` decreases by the return line total.

4. **Given** a sales return is submitted without a reference invoice (standalone return), **When** it is posted, **Then** the system accepts it using the provided quantities, performs the same stock and balance adjustments, and records the return with `SalesInvoiceId = null`.

---

### User Story 4 - Stock Transfer Between Warehouses (Priority: P2)

A Manager moves stock from one warehouse to another. They select a source warehouse and a different destination warehouse, add products with transfer quantities, and post the transfer. The system validates that the source warehouse holds sufficient stock for each line, then atomically decreases stock at the source and increases it at the destination, creating two inventory movement records per product line (one `TransferOut`, one `TransferIn`) — all within a single transaction.

**Why this priority**: Multi-warehouse businesses need to rebalance stock between locations. An incorrect transfer leaves one warehouse over-counted and another under-counted, causing downstream sales failures.

**Independent Test**:
1. Set Warehouse A = 50 units of Product X, Warehouse B = 10 units
2. Post a transfer of 20 units from A to B
3. Verify A stock = 30, B stock = 30
4. Verify one `TransferOut` movement for Warehouse A and one `TransferIn` movement for Warehouse B
5. Verify the entire operation rolled back when one line has insufficient stock

**Acceptance Scenarios**:

1. **Given** Warehouse A has 50 units and Warehouse B has 10 units of Product X, **When** a Manager posts a transfer of 20 units from A to B, **Then** A has 30 units, B has 30 units, one `TransferOut` and one `TransferIn` movement exist for Product X.

2. **Given** a transfer request specifying the same warehouse as both source and destination, **When** it is submitted, **Then** the system rejects it with a validation error before any database operation.

3. **Given** a transfer includes Product Y where Warehouse A only has 5 units but 10 are requested, **When** the transfer is submitted, **Then** no stock is modified anywhere, and the system returns a descriptive insufficient-stock error for Product Y.

4. **Given** a transfer covers multiple products and one line fails stock validation, **When** the transfer is submitted, **Then** the entire transfer is rejected — no partial stock changes are applied.

---

### User Story 5 - Payment Recording (Priority: P2)

A Cashier records a payment from a customer or a payment made to a supplier. They identify the payer/payee, enter the payment amount and method, optionally link the payment to a specific invoice, and submit. The system creates the payment record and adjusts the customer's or supplier's outstanding balance accordingly.

**Why this priority**: Separate payment recording is required when invoices are created on credit. Without it, balances cannot be settled and the accounts receivable/payable ledgers become inaccurate.

**Independent Test**:
1. Create a customer with `CurrentBalance = 500` (from a credit sale)
2. Record a customer payment of 200 linked to that customer
3. Verify `CurrentBalance = 300`
4. Verify a `CustomerPayment` record exists with the correct amount, method, and reference
5. Mirror for a supplier payment and verify supplier balance decreases

**Acceptance Scenarios**:

1. **Given** a customer has `CurrentBalance = 500`, **When** a Cashier records a customer payment of `200` with method Cash, **Then** a `CustomerPayment` record is created and the customer's `CurrentBalance` becomes `300`.

2. **Given** a supplier has `CurrentBalance = 1000`, **When** a Manager records a supplier payment of `400`, **Then** a `SupplierPayment` record is created and the supplier's `CurrentBalance` becomes `600`.

3. **Given** a payment request with `Amount = 0` or a negative amount, **When** it is submitted, **Then** the system rejects it with a validation error and no record is created.

4. **Given** a customer payment is optionally linked to a specific sales invoice, **When** it is recorded, **Then** the `CustomerPayment.SalesInvoiceId` is set and the balance update is still applied to the customer.

---

## Edge Cases

- **Insufficient stock during sale**: System returns a descriptive validation error per product identifying how many units are short; no invoice or movement records are created.
- **Concurrent sales of the same product**: Inventory operations use pessimistic locking or serialized access to prevent overselling; the database-level `CHECK (Quantity >= 0)` constraint acts as a final safety net.
- **Returning more than originally purchased/sold**: System validates return quantity against the original invoice line quantity and rejects the excess with a clear error.
- **Self-transfer (same source and destination warehouse)**: Rejected immediately at the service layer before any database call.
- **Invoice already in terminal state**: Attempting to post a `Posted` invoice or cancel a `Cancelled` invoice returns a state-machine validation error.
- **PaidAmount exceeds TotalAmount**: Rejected at both domain and API validation layers before any persistence.
- **Zero-quantity line items**: Validation prevents line items with `Quantity <= 0` from being submitted.
- **Database failure mid-transaction**: The wrapping database transaction rolls back atomically; the caller receives a generic failure result with no partial data committed.
- **Document number sequence collision under concurrency**: The `DocumentSequenceService` uses a pessimistic lock (SemaphoreSlim or row-level DB lock) to guarantee uniqueness even under concurrent requests.
- **Payment amount of zero or negative**: Rejected at the API validation layer before processing.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a thread-safe `InventoryService` responsible for checking available stock, increasing stock (purchase/return-in), decreasing stock (sale/return-out/transfer-out), and creating `InventoryMovement` audit records for every operation
- **FR-002**: System MUST implement a `PurchaseService` covering the full lifecycle: create (Draft), post (Posted — triggers stock increase + supplier balance update), and cancel (Cancelled — reverses all stock and balance effects)
- **FR-003**: System MUST implement a `SalesService` covering the full lifecycle: create (Draft), post (Posted — triggers stock decrease after stock validation + customer balance update), and cancel (Cancelled — reverses all stock and balance effects)
- **FR-004**: System MUST implement a `SalesReturnService` that validates return quantities against original invoice lines, reverses stock (increase) and customer balance (decrease), and records `SaleReturnIn` movements
- **FR-005**: System MUST implement a `PurchaseReturnService` that validates return quantities against original invoice lines, reverses stock (decrease) and supplier balance (decrease), and records `PurchaseReturnOut` movements
- **FR-006**: System MUST implement a `StockTransferService` that validates source stock, atomically decreases source warehouse stock and increases destination warehouse stock, records `TransferOut` and `TransferIn` movements per line item
- **FR-007**: System MUST implement a `PaymentService` that creates `CustomerPayment` or `SupplierPayment` records and adjusts the corresponding party's `CurrentBalance`
- **FR-008**: System MUST expose API controllers for all business logic services: `PurchaseInvoicesController`, `SalesInvoicesController`, `SalesReturnsController`, `PurchaseReturnsController`, `StockTransfersController`, `PaymentsController` — each requiring appropriate role-based authorization
- **FR-009**: System MUST wrap all multi-table financial operations in a database transaction that automatically rolls back on any failure
- **FR-010**: System MUST create one `InventoryMovement` record per product line per stock-affecting operation, capturing `QuantityBefore`, `QuantityChange`, `QuantityAfter`, `MovementType`, `ReferenceType`, and `ReferenceId`
- **FR-011**: System MUST validate sufficient stock in the target warehouse BEFORE opening a transaction for any stock-decreasing operation (sale, purchase return, transfer-out)
- **FR-012**: System MUST prohibit hard deletion of any invoice — only `Status = Cancelled` transitions are permitted, and they must fully reverse all effects
- **FR-013**: System MUST enforce the invoice state machine: `Draft → Posted`, `Draft → Cancelled`, `Posted → Cancelled` are the only valid transitions; `Cancelled → *` and `Posted → Draft` are forbidden
- **FR-014**: System MUST validate that the sum of all return quantities for a given invoice line does not exceed the originally invoiced quantity
- **FR-015**: System MUST reject any stock transfer where `FromWarehouseId == ToWarehouseId`
- **FR-016**: System MUST compute all financial totals (`LineTotal`, `SubTotal`, `TotalAmount`, `DueAmount`) in the domain layer using decimal arithmetic, never in the API or client layers
- **FR-017**: System MUST enforce `PaidAmount <= TotalAmount` at both the domain entity level and API validation layer
- **FR-018**: System MUST auto-generate sequential, year-scoped document numbers (e.g., `INV-2026-000001`) for all invoice types using a thread-safe `DocumentSequenceService`
- **FR-019**: System MUST update `Customer.CurrentBalance` (increase) or `Supplier.CurrentBalance` (increase) only when a `Posted` invoice has `DueAmount > 0`
- **FR-020**: System MUST reverse `CurrentBalance` adjustments precisely when a `Posted` invoice is cancelled, restoring the balance to its pre-posting value

### Key Entities

- **InventoryService**: Manages all stock read/write operations; the single authority for modifying `WarehouseStock` and creating `InventoryMovement` records
- **PurchaseService / SalesService**: Orchestrate the full invoice lifecycle for their respective domains; delegate all stock changes to `InventoryService`
- **SalesReturnService / PurchaseReturnService**: Mirror their parent invoice services but with reversed stock and balance directions
- **StockTransferService**: Handles dual-warehouse atomic stock rebalancing
- **PaymentService**: Records standalone payments and updates party balances; no stock involvement
- **DocumentSequenceService**: Generates thread-safe, sequential, year-scoped document numbers for all invoice and payment types
- **WarehouseStock**: The authoritative stock-level record per product per warehouse; subject to a non-negative `CHECK` constraint and a unique constraint on `(WarehouseId, ProductId)`
- **InventoryMovement**: Append-only audit log for every stock change; links back to the originating transaction via `ReferenceType` + `ReferenceId`
- **InvoiceStatus**: Enum governing the three-state machine — `Draft (1)`, `Posted (2)`, `Cancelled (3)`
- **MovementType**: Enum classifying each inventory change — `PurchaseIn`, `SaleOut`, `SaleReturnIn`, `PurchaseReturnOut`, `TransferOut`, `TransferIn`, `Adjustment`

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A complete purchase flow (create Draft → post to Posted) completes end-to-end in under 2 seconds for an invoice with up to 20 line items
- **SC-002**: A complete sales flow (create Draft → post to Posted, including stock validation) completes end-to-end in under 2 seconds for an invoice with up to 20 line items
- **SC-003**: Stock levels remain non-negative across all warehouses after any sequence of purchases, sales, returns, and transfers — verified by querying `WarehouseStocks` directly after each operation
- **SC-004**: Every stock-affecting operation produces exactly one `InventoryMovement` per product line with correct `QuantityBefore` and `QuantityAfter` values
- **SC-005**: Invoice financial totals are arithmetically consistent: `SubTotal = Σ LineTotals`, `TotalAmount = SubTotal − Discount + Tax`, `DueAmount = TotalAmount − PaidAmount` — validated after every create and post operation
- **SC-006**: Sales and purchase return operations fully reverse the stock and balance effects of the original posted invoice — net change to stock and balance after post-then-cancel or post-then-full-return is zero
- **SC-007**: A stock transfer atomically updates both warehouses — no state exists where source has decreased but destination has not yet increased
- **SC-008**: Any operation that encounters a failure mid-way leaves the database in an identical state to before the operation started — verified by intentionally failing a transaction at different points
- **SC-009**: Document numbers are unique and sequential within each document type and calendar year — no gaps or duplicates under 10 concurrent requests
- **SC-010**: Customer and supplier `CurrentBalance` values are always consistent with the sum of their posted-but-unpaid invoice `DueAmount` values minus recorded payments

---

## Assumptions

- Phase 2 (Backend Core) is fully implemented and operational: authentication with JWT, all basic CRUD services, `IUnitOfWork` with `BeginTransactionAsync`, and `DocumentSequenceService` are available
- All domain entities (`SalesInvoice`, `PurchaseInvoice`, `SalesReturn`, `PurchaseReturn`, `StockTransfer`, `WarehouseStock`, `InventoryMovement`, `CustomerPayment`, `SupplierPayment`) are correctly modelled with EF Core Fluent API configurations and FK constraints
- The database enforces `CHECK (Quantity >= 0)` on `WarehouseStocks` and `UNIQUE (WarehouseId, ProductId)` as a final integrity backstop
- All new services will follow the established `Result<T>` pattern — services never throw exceptions; controllers translate results to HTTP status codes
- `FluentValidation` is used for all request-level validation (required fields, ranges, type constraints)
- `Serilog` is used for all logging — `Console.WriteLine` is forbidden
- Decimal precision rules from the project constitution apply without exception: `decimal(18,2)` for monetary values, `decimal(18,3)` for quantities
- A default "cash customer" record already exists in the database to support walk-in sales without a named customer
- The `InventoryService` is the **only** code path that directly modifies `WarehouseStock.Quantity` — no other service updates stock directly
- Return quantity validation compares against the originally invoiced quantity (not a running net); partial returns on the same invoice across multiple return documents is out of scope for Phase 3