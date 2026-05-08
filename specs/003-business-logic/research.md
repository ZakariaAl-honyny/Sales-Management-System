# Research: Business Logic Implementation

**Phase**: Phase 0 | **Date**: 2026-05-07

---

## Decision 1: InventoryService as Single Stock Authority

**Decision**: A dedicated `InventoryService` is the sole code path for reading and writing `WarehouseStock.Quantity` and creating `InventoryMovement` records. No other service touches stock directly.

**Rationale**: Centralizing stock operations eliminates race conditions and ensures the movement audit trail is never bypassed. All higher-level services (`PurchaseService`, `SalesService`, etc.) call `InventoryService` internally after their invoice is saved.

**Alternatives considered**:
- Inline stock logic in each service → rejected: duplicates logic, risks inconsistent movement records
- Repository-only approach → rejected: no single place to enforce the QuantityBefore/QuantityAfter calculation rule

---

## Decision 2: Pre-Transaction Stock Validation

**Decision**: Stock availability is validated **before** `BeginTransactionAsync()` is called. If any line item has insufficient stock, the service returns `Result.Failure` immediately without opening a transaction.

**Rationale**: Opening a transaction holds a DB lock. Failing fast before the transaction reduces lock contention and follows Constitution Principle III (7-step protocol step 1: "Validate ALL preconditions BEFORE opening transaction").

**Alternatives considered**:
- Validate inside transaction → rejected: unnecessary lock contention; also the DB CHECK constraint would surface as an unhandled exception rather than a clean business error

---

## Decision 3: IUnitOfWork Extension for Invoice Repositories

**Decision**: Extend `IUnitOfWork` with typed repositories for all new entities: `SalesInvoices`, `PurchaseInvoices`, `SalesReturns`, `PurchaseReturns`, `StockTransfers`, `CustomerPayments`, `SupplierPayments`.

**Rationale**: Consistent with the existing pattern for `WarehouseStocks`, `InventoryMovements`, etc. All multi-entity operations use the same `IUnitOfWork` transaction scope.

**Alternatives considered**:
- Separate repository interfaces injected directly → rejected: breaks the existing `IUnitOfWork` pattern and makes transaction management harder

---

## Decision 4: Invoice Item Line Total — Domain Only

**Decision**: `LineTotal = (Quantity * UnitPrice) - DiscountAmount` is computed inside the domain entity constructor/factory method. Request models carry the raw inputs only; the API and Application layers never compute totals.

**Rationale**: Constitution Principle II mandates this. The `SalesInvoice.AddItem()` / `PurchaseInvoice.AddItem()` domain methods handle the computation and keep the invoice aggregate consistent.

**Alternatives considered**:
- Compute in service → rejected: violates Constitution Principle II
- Compute in FluentValidation → rejected: validation layer must not produce business values

---

## Decision 5: Cancellation Reversal Strategy

**Decision**: When a `Posted` invoice is cancelled, the service iterates its existing line items and calls `InventoryService.ReverseAsync()` for each one, creating movements with the inverse `MovementType` (`SaleOut` → `SaleReturnIn`, `PurchaseIn` → `PurchaseReturnOut`, etc.). Balance adjustments are also reversed exactly.

**Rationale**: Using the actual persisted line items (not the cancellation request) guarantees the reversal is 1:1 with the original posting. The reference ID on the reversal movements points to the original invoice for a complete audit trail.

**Alternatives considered**:
- Soft-delete only → rejected: stock and balance would remain incorrect
- Create a formal return document on cancellation → rejected: over-engineered; cancellation is a direct reversal, not a customer-facing return

---

## Decision 6: SalesReturn Quantity Validation Scope

**Decision**: For Phase 3, return quantity validation compares against the **originally invoiced quantity** on the referenced line, not a running net of all returns. Partial multi-document returns are out of scope.

**Rationale**: Keeps the validation simple and deterministic. The spec explicitly declares partial multi-document returns out of scope for Phase 3.

**Alternatives considered**:
- Track cumulative returned quantities per invoice line → deferred to Phase 4/5 if needed

---

## Decision 7: Role-Based Authorization Mapping

**Decision**: New controllers map to the existing role policies:

| Controller | Policy | Roles Allowed |
|------------|--------|---------------|
| PurchaseInvoicesController | `ManagerAndAbove` | Admin, Manager |
| SalesInvoicesController | `AllStaff` | Admin, Manager, Cashier |
| SalesReturnsController | `AllStaff` | Admin, Manager, Cashier |
| PurchaseReturnsController | `ManagerAndAbove` | Admin, Manager |
| StockTransfersController | `ManagerAndAbove` | Admin, Manager |
| PaymentsController | `AllStaff` | Admin, Manager, Cashier |

**Rationale**: Matches the permissions matrix in `AGENTS.md` §6 exactly.

**Alternatives considered**: None — the matrix is a project constitution-level requirement.

---

## Decision 8: No New Migrations Required

**Decision**: The schema for all Phase 3 entities (`SalesInvoices`, `PurchaseInvoices`, `SalesReturns`, `PurchaseReturns`, `StockTransfers`, `CustomerPayments`, `SupplierPayments`, `InventoryMovements`, `WarehouseStocks`, `DocumentSequences`) is fully migrated. Phase 3 only adds application-layer code.

**Rationale**: The `SyncInventoryAndSettings` migration already covers all required tables. EF Core model snapshot matches the documentation schema (verified by the empty `VerifySchemaSync` migration check).

**Alternatives considered**: N/A — confirmed by migration verification run on 2026-05-07.
