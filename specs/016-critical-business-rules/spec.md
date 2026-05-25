# Feature Specification: Critical Business Rules Reference (Phase 16)

**Feature Branch**: `016-critical-business-rules`
**Created**: 2026-05-25
**Status**: Draft
**Input**: Phase 16 — Critical Business Rules Reference

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Safe and Atomic Sales Processing (Priority: P1)

The business owner needs a guarantee that when a cashier processes a sale, either the entire transaction (stock deduction, invoice generation, payment validation) succeeds perfectly, or the system aborts without making partial, corrupted changes.

**Independent Test**: Attempt to process a sales invoice where the total quantity of an item exceeds the available warehouse stock. The system must reject the transaction immediately without creating a draft invoice or generating an invoice number. Attempt to pay an amount greater than the invoice total. The system must throw a domain validation error and roll back the transaction completely.

**Acceptance Scenarios**:

1. **Given** a user is posting a sales invoice, **When** an item's requested quantity exceeds available warehouse stock, **Then** the transaction is blocked before any database changes occur.
2. **Given** an invoice is being finalized, **When** the entered paid amount is strictly greater than the total invoice amount, **Then** the domain logic rejects the input and the entire transaction rolls back.
3. **Given** a valid sales invoice is processed, **When** the transaction commits, **Then** a thread-safe invoice number is generated, the invoice status becomes "Posted", and stock levels are accurately deducted.

---

### User Story 2 — Unified Purchase, Return, and Transfer Flows (Priority: P1)

The warehouse manager frequently creates purchase orders, processes customer returns, and transfers stock between warehouses. They need absolute certainty that stock increases and decreases happen symmetrically and securely, without discrepancies.

**Independent Test**: Execute a stock transfer between Warehouse A and Warehouse B. Force a system failure (e.g., database timeout) right after Warehouse A stock is decreased but before Warehouse B is increased. The system must roll back the decrease in Warehouse A, ensuring zero stock is lost in transit due to partial failures.

**Acceptance Scenarios**:

1. **Given** a user processes a Purchase Invoice, **When** the transaction succeeds, **Then** it accurately mirrors the sales flow by validating logic and correctly increasing warehouse stock and supplier balances atomically.
2. **Given** a user processes a Return Invoice (Sales or Purchase), **When** the transaction is committed, **Then** the original operations are perfectly reversed (stock is returned to/from the warehouse, and balances are adjusted) within a single atomic transaction.
3. **Given** a user executes a Stock Transfer, **When** the operation processes, **Then** the source warehouse stock decreases and the destination warehouse stock increases within the exact same database transaction, rolling back entirely if either operation fails.

---

### User Story 3 — Payment Flows and Balance Adjustments (Priority: P2)

The accountant needs to register customer payments and supplier payouts accurately, ensuring that these financial-only operations do not mistakenly alter physical inventory levels.

**Independent Test**: Process a direct payment from a customer. Verify that the customer's outstanding balance decreases exactly by the payment amount, and confirm that the warehouse stock for all items remains completely unaffected.

**Acceptance Scenarios**:

1. **Given** a user processes a Payment transaction (incoming or outgoing), **When** the transaction is committed, **Then** the corresponding customer or supplier balance is adjusted.
2. **Given** a Payment transaction is finalized, **When** auditing the system state, **Then** zero changes have been applied to any physical warehouse stock levels.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST execute all transactional flows (Sales, Purchase, Return, Transfer, Payment) inside an explicit, atomic Database Transaction (`BEGIN TRANSACTION` ... `COMMIT/ROLLBACK`).
- **FR-002**: For any Sales or Transfer operation, the system MUST pre-validate that sufficient stock exists in the source warehouse before beginning state mutations.
- **FR-003**: Invoice Number generation MUST be thread-safe to prevent duplicate sequence numbers during concurrent multi-user checkouts.
- **FR-004**: The system MUST initially create all invoices in a "Draft" status before applying financial or inventory changes.
- **FR-005**: All financial calculations (e.g., `LineTotal`) MUST be isolated and computed strictly within the Domain entities.
- **FR-006**: The Domain layer MUST strictly enforce the validation rule: `PaidAmount <= TotalAmount` for all invoices.
- **FR-007**: Only invoices successfully transitioning to a "Posted" status SHALL trigger physical stock deductions or additions.
- **FR-008**: Any unhandled exception or business rule violation during a flow MUST trigger a complete `ROLLBACK` of all pending database operations.
- **FR-009**: Payment-only flows MUST exclusively adjust financial balances (Customer/Supplier) and MUST NOT trigger any inventory movement logic.

---

## Success Criteria *(mandatory)*

- **SC-001**: 100% of integration tests verifying concurrent invoice creation pass without generating duplicate invoice numbers.
- **SC-002**: Intentionally injecting faults (e.g., timeouts) mid-transaction during a Stock Transfer results in a guaranteed 0% occurrence of orphaned or lost stock (database remains unchanged).
- **SC-003**: End-to-end automated tests verify that attempting to overpay an invoice successfully aborts the entire HTTP request and leaves the database unmutated.
- **SC-004**: System audit logs demonstrate that physical inventory counts perfectly match the sum of recorded `InventoryMovements` without unexplained deviations.

---

## Assumptions

- "Thread-safe invoice number generation" implies the use of a centralized locking mechanism (e.g., `SemaphoreSlim`) or a dedicated atomic database sequence.
- Business rule validations (like stock availability) are performed at the Application layer prior to committing the transaction, while mathematical constraints (like `PaidAmount`) are strictly enforced by Domain entity constructors/methods.
