# Tasks: Critical Business Rules Reference (Phase 16)

**Input**: Design documents from `/specs/016-critical-business-rules/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure
*(Skipped: Infrastructure already exists in v4.6.4)*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T001 Implement `DocumentSequenceService` with `SemaphoreSlim` to guarantee thread-safe document number generation in `SalesSystem.Application/Services/DocumentSequenceService.cs`
- [ ] T002 Implement `BeginTransactionAsync`, `CommitAsync`, and `RollbackAsync` in `SalesSystem.Infrastructure/Persistence/UnitOfWork.cs` using `IDbContextTransaction` from EF Core
- [ ] T003 Ensure global exception handler in `SalesSystem.Api/Middlewares/ExceptionHandlingMiddleware.cs` gracefully catches `DomainException` and maps to HTTP 400 Bad Request

**Checkpoint**: Foundation ready - UnitOfWork and concurrency locks are active.

---

## Phase 3: User Story 1 - Safe and Atomic Sales Processing (Priority: P1) 🎯 MVP

**Goal**: Guarantee that when a cashier processes a sale, either the entire transaction succeeds perfectly, or the system aborts without making partial changes.

**Independent Test**: Attempt to process an invoice where requested quantity > stock, or PaidAmount > TotalAmount. The transaction must block or rollback completely.

### Implementation for User Story 1

- [ ] T004 [P] [US1] Audit `WarehouseStock` entity in `SalesSystem.Domain/Entities/Inventory/WarehouseStock.cs` to ensure `DecreaseStock` method has Arabic Guard Clauses
- [ ] T005 [P] [US1] Audit `SalesInvoice` entity in `SalesSystem.Domain/Entities/Sales/SalesInvoice.cs` to enforce `PaidAmount <= TotalAmount` domain validation
- [ ] T006 [US1] Refactor `SalesInvoiceService.CreateInvoiceAsync` in `SalesSystem.Application/Services/Sales/SalesInvoiceService.cs` to explicitly wrap logic inside `await _uow.BeginTransactionAsync(ct)` (Depends on T004, T005)

**Checkpoint**: At this point, Sales transactions are fully atomic and enforce business rules natively.

---

## Phase 4: User Story 2 - Unified Purchase, Return, and Transfer Flows (Priority: P1)

**Goal**: Apply identical atomic transaction guarantees to purchasing, returning, and transferring stock.

**Independent Test**: Execute a stock transfer and simulate a failure. Verify that source stock is not permanently decreased without the destination increasing.

### Implementation for User Story 2

- [ ] T007 [P] [US2] Audit `PurchaseInvoice` entity in `SalesSystem.Domain/Entities/Purchases/PurchaseInvoice.cs` to enforce `PaidAmount` validation
- [ ] T008 [P] [US2] Refactor `PurchaseInvoiceService` in `SalesSystem.Application/Services/Purchases/PurchaseInvoiceService.cs` to implement atomic transaction flow
- [ ] T009 [P] [US2] Refactor `SalesReturnService` and `PurchaseReturnService` to execute all reversal logic within a single transaction
- [ ] T010 [US2] Refactor `StockTransferService` in `SalesSystem.Application/Services/Inventory/StockTransferService.cs` to perform source decrease and destination increase atomically

**Checkpoint**: All inventory-mutating workflows are now transaction-safe.

---

## Phase 5: User Story 3 - Payment Flows and Balance Adjustments (Priority: P2)

**Goal**: Guarantee that standalone payments strictly alter balances and never trigger physical stock logic.

**Independent Test**: Process a standalone customer payment and verify warehouse stock remains untouched.

### Implementation for User Story 3

- [ ] T011 [P] [US3] Refactor `CustomerPaymentService` and `SupplierPaymentService` to execute balance changes within `BeginTransactionAsync`
- [ ] T012 [US3] Write/Update integration tests for payment flows to assert that `WarehouseStock` is unchanged

**Checkpoint**: Payment flows are hardened.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening and logging integration

- [ ] T013 Verify Serilog logging correctly captures and logs all transaction rollbacks `transaction.RollbackAsync()` across all updated Application Services.

---

## Implementation Strategy & Dependencies

### Execution Order
1. **Foundational Phase** must be completed first as all Application services depend on the `IUnitOfWork` interface updates.
2. **Phase 3 (MVP)** must be completed to prove the transaction pattern in the most critical flow (Sales).
3. **Phases 4 and 5** can be completed in parallel.

### Parallel Opportunities
- **US1 & US2 Entity Audits**: The domain entity refactoring (T004, T005, T007) can be done entirely in parallel since they don't depend on each other.
- **US2 Service Refactoring**: T008, T009, and T010 can be divided among different team members once the transaction pattern is established.
