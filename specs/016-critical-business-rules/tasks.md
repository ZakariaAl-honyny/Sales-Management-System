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

- [X] T000 Infrastructure verification — All required projects, dependencies, and configuration are in place

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T001 Implement `DocumentSequenceService` with `SemaphoreSlim` to guarantee thread-safe document number generation in `SalesSystem.Application/Services/DocumentSequenceService.cs` — ✅ Already implemented (`private static readonly SemaphoreSlim _lock = new(1, 1)`). Review fix: added `OperationCanceledException` guard on `WaitAsync` for strict RULE-202 compliance; removed RULE-195 dead entries PRD/CUST/SUP from `DetermineDocumentType()`.
- [X] T002 Implement `BeginTransactionAsync`, `CommitAsync`, and `RollbackAsync` in `SalesSystem.Infrastructure/Data/UnitOfWork.cs` using `IDbContextTransaction` from EF Core — ✅ Already implemented via `DbContextTransactionWrapper`
- [X] T003 Ensure global exception handler in `SalesSystem.Api/Middleware/ExceptionMiddleware.cs` gracefully catches `DomainException` and maps to HTTP 400 Bad Request — ✅ Added `DomainException` handler returning `400 Bad Request` with `DOMAIN_VALIDATION_ERROR` code. Review fix: removed unused `GetInnerMessage()` dead code.

**Checkpoint**: Foundation ready - UnitOfWork and concurrency locks are active.

---

## Phase 3: User Story 1 - Safe and Atomic Sales Processing (Priority: P1) 🎯 MVP

**Goal**: Guarantee that when a cashier processes a sale, either the entire transaction succeeds perfectly, or the system aborts without making partial changes.

**Independent Test**: Attempt to process an invoice where requested quantity > stock, or PaidAmount > TotalAmount. The transaction must block or rollback completely.

### Implementation for User Story 1

- [X] T004 [P] [US1] Audit `WarehouseStock` entity in `SalesSystem.Domain/Entities/WarehouseStock.cs` to ensure `DecreaseStock` method has Arabic Guard Clauses — ✅ Already implemented: `DecreaseQuantity(amount)` throws `DomainException("المخزون غير كافٍ.")`; `DeductStock(...)` has Arabic Guard Clause with available/requested quantities; `Create(...)` validates all inputs with Arabic messages. Verified: 3 test files (WarehouseStockTests.cs, WarehouseStockUnitConversionTests.cs) with 485 Domain tests passing.
- [X] T005 [P] [US1] Audit `SalesInvoice` entity in `SalesSystem.Domain/Entities/Sales/SalesInvoice.cs` to enforce `PaidAmount <= TotalAmount` domain validation — ✅ Already implemented: `SetPaidAmount(amount)` throws `DomainException("المبلغ المدفوع أكبر من الإجمالي.")` when `amount > TotalAmount`; `Post()` validates Draft status and non-empty items; `Cancel()` validates not already cancelled and not paid. All Guard Clauses in Arabic. Verified by Domain tests passing.
- [X] T006 [US1] Refactor `SalesService.CreateInvoiceAsync` in `SalesSystem.Application/Services/SalesService.cs` to explicitly wrap logic inside `await _uow.BeginTransactionAsync(ct)` (Depends on T004, T005) — ✅ Already implemented: `CreateAsync()` opens `BeginTransactionAsync`, creates invoice/items, validates via `SetPaidAmount()`, saves, commits on success, rolls back on `DomainException` or `Exception`; `PostAsync()` validates stock BEFORE transaction, wraps stock deduction + balance update + cash recording inside `BeginTransactionAsync`; `CancelAsync()` follows same pattern. **Code Review Fixes Applied: (1)** Added `invoice.SetPaidAmount(0)` before `invoice.Cancel()` to fix paid-invoice cancellation bug; **(2)** Changed already-cancelled return from success to `Result.Failure("الفاتورة ملغاة بالفعل")` for consistency; **(3)** Added `_logger.LogWarning` in `DomainException` catch per RULE-183. Verified by 208 Application tests + 603 API tests passing.

**Checkpoint**: At this point, Sales transactions are fully atomic and enforce business rules natively.

---

## Phase 4: User Story 2 - Unified Purchase, Return, and Transfer Flows (Priority: P1)

**Goal**: Apply identical atomic transaction guarantees to purchasing, returning, and transferring stock.

**Independent Test**: Execute a stock transfer and simulate a failure. Verify that source stock is not permanently decreased without the destination increasing.

### Implementation for User Story 2

- [X] T007 [P] [US2] Audit `PurchaseInvoice` entity in `SalesSystem.Domain/Entities/PurchaseInvoice.cs` to enforce `PaidAmount` validation — ✅ Already implemented: `SetPaidAmount` validates `amount > TotalAmount` → `"المبلغ المدفوع أكبر من الإجمالي."`, `Post()` validates Draft status and non-empty items, `Cancel()` has Arabic Guard Clauses. Verified by backend-architect subagent.
- [X] T008 [P] [US2] Refactor `PurchaseInvoiceService` in `SalesSystem.Application/Services/PurchaseService.cs` to implement atomic transaction flow — ✅ All 3 methods (`CreateAsync`, `PostAsync`, `CancelAsync`) already wrapped in `BeginTransactionAsync`. **Fixes applied by backend-architect**: (1) Zero `PaidAmount` before `invoice.Cancel()`, (2) Already-cancelled returns `Result.Failure("الفاتورة ملغاة بالفعل")`, (3) 3× `_logger.LogWarning` in DomainException catches, (4) CashTransactionType corrected from `CustomerPayment` to `SupplierPayment`. Test updated: `CancelAsync_AlreadyCancelledInvoice_ReturnsSuccess` → ReturnsFailure. 1296 tests pass.
- [X] T009 [P] [US2] Refactor `SalesReturnService` and `PurchaseReturnService` to execute all reversal logic within a single transaction — ✅ All methods already wrapped in `BeginTransactionAsync`. **Fixes applied by backend-architect**: Idempotency returns failure (8 fixes: "مرتجع المبيعات ملغى بالفعل" / "مرتجع المشتريات ملغى بالفعل"), 6× `_logger.LogWarning` added to DomainException catches. 1296 tests pass.
- [X] T010 [US2] Refactor `StockTransferService` in `SalesSystem.Application/Services/Inventory/InventoryService.cs` to perform source decrease and destination increase atomically — ✅ Already implemented: `PostTransferAsync` validates stock BEFORE transaction, both DecreaseStock (source) + IncreaseStock (dest) inside same transaction. `CancelAsync` reverses correctly. Already-cancelled returns failure. **Fixes applied**: 4× `_logger.LogWarning` in DomainException catches. 1296 tests pass.

**Checkpoint**: All inventory-mutating workflows are now transaction-safe.

---

## Phase 5: User Story 3 - Payment Flows and Balance Adjustments (Priority: P2)

**Goal**: Guarantee that standalone payments strictly alter balances and never trigger physical stock logic.

**Independent Test**: Process a standalone customer payment and verify warehouse stock remains untouched.

### Implementation for User Story 3

- [X] T011 [P] [US3] Refactor `CustomerPaymentService` and `SupplierPaymentService` to execute balance changes within `BeginTransactionAsync` — ✅ Already implemented: `PaymentService.cs` wraps ALL 6 mutating methods (Create/Update/Delete for both Customer and Supplier) in `BeginTransactionAsync`. **Fixes applied (6×)**: Added `_logger.LogWarning` to all 6 DomainException catch blocks. Controllers are thin (inject `IPaymentService` only). Verified by backend-architect + code-reviewer. 213 Application tests pass.
- [X] T012 [US3] Write/Update integration tests for payment flows to assert that `WarehouseStock` is unchanged — ✅ **5 new tests added** by test-engineer: (1) Customer payment does NOT create WarehouseStock, (2) Supplier payment does NOT create WarehouseStock, (3) UpdateCustomerPayment reverses old balance and applies new, (4) DeleteCustomerPayment reverses balance, (5) DeleteSupplierPayment reverses balance. Application tests: 208→213 (+5). All PASS.

**Checkpoint**: Payment flows are hardened.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening and logging integration

- [X] T013 Verify Serilog logging correctly captures and logs all transaction rollbacks `transaction.RollbackAsync()` across all updated Application Services. ✅ Audit of 59 RollbackAsync calls found 11 missing Log calls; all 12 fixes (11 missing + 1 wrong log level) applied and verified by code-reviewer. Files: SalesService.cs (5 fixes), PurchaseService.cs (4 fixes), CashBoxService.cs (2 fixes), InventoryWriteOffService.cs (1 fix — LogError→LogWarning). All 1,306 tests pass.

---

## Implementation Strategy & Dependencies

### Execution Order
1. **Foundational Phase** must be completed first as all Application services depend on the `IUnitOfWork` interface updates.
2. **Phase 3 (MVP)** must be completed to prove the transaction pattern in the most critical flow (Sales).
3. **Phases 4 and 5** can be completed in parallel.

### Parallel Opportunities
- **US1 & US2 Entity Audits**: The domain entity refactoring (T004, T005, T007) can be done entirely in parallel since they don't depend on each other.
- **US2 Service Refactoring**: T008, T009, and T010 can be divided among different team members once the transaction pattern is established.
