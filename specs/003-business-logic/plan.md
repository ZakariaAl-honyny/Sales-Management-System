# Implementation Plan: Phase 3 — Business Logic (Application Service Layer)

**Branch**: `003-business-logic` | **Date**: 2026-06-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/003-business-logic/spec.md`
**Version**: 2.0 — Rewritten for Clean Architecture alignment and v4.6.9+ rules

---

## Summary

Implement the Application Service Layer for the Sales Management System, covering all transactional business logic. This layer sits between the API controllers and the Infrastructure/Domain layers. Seven service classes orchestrate multi-table database operations: InventoryService (single stock authority), SalesService, PurchaseService, SalesReturnService, PurchaseReturnService, StockTransferService, and PaymentService. Every service follows the Result<T> pattern, adheres to the 7-step transaction protocol (validate → begin → save → stock → movements → balance → commit/rollback), and maintains full audit trails via InventoryMovements and balance updates. All services respect Clean Architecture boundaries — zero EF Core references, zero business logic in controllers, zero exceptions thrown.

**Key architectural decisions**: Service Layer pattern (NOT MediatR/CQRS per AGENTS.md RULE-147), IUnitOfWork for all data access, ExecuteTransactionAsync for atomic multi-save operations (wraps CreateExecutionStrategy + explicit transaction), DomainException for business rule violations, and EventBus for cross-ViewModel Desktop communication.

---

## 1. Service Layer Architecture

### Pattern Choice: Service Layer (Not MediatR/CQRS)

Per AGENTS.md RULE-147 and RULE-148, the system uses a direct Service Layer pattern. MediatR is removed from the project. Every business capability maps to a C# interface + implementation pair, injected via constructor DI. This keeps the codebase simple, avoids the indirection of MediatR handlers, and aligns with the small-team single-store deployment profile.

**Interface location**: `SalesSystem.Application/Interfaces/Services/`
**Implementation location**: `SalesSystem.Application/Services/`

### Service Responsibilities

| Service | Stock Impact | Balance Impact | Key Side Effects |
|---------|-------------|----------------|------------------|
| **InventoryService** | ✅ All stock changes | ❌ None | Creates InventoryMovement audit trail; singular authority over WarehouseStock modification |
| **SalesService** | ✅ Decrease (via InventoryService) | ✅ Customer + | Post: stock out, balance up. Cancel: reverse both |
| **PurchaseService** | ✅ Increase (via InventoryService) | ✅ Supplier + | Post: stock in, balance up. Cancel: reverse both |
| **SalesReturnService** | ✅ Increase (via InventoryService) | ✅ Customer − | Reverses sale line by line |
| **PurchaseReturnService** | ✅ Decrease (via InventoryService) | ✅ Supplier − | Reverses purchase line by line |
| **StockTransferService** | ✅ Dual (source − / dest +) | ❌ None | Two movements per line (TransferOut + TransferIn) |
| **PaymentService** | ❌ None | ✅ Customer − / Supplier − | CashBox transaction (+ journal entry in Phase 24) |

### Dependency Injection Registration

All services are registered as Scoped in `Program.cs`. Each receives:
- `IUnitOfWork` for data access
- `IDocumentSequenceService` for thread-safe invoice number generation
- `ILogger<T>` for Serilog auditing
- `IInventoryService` (consumed by SalesService, PurchaseService, Return services, TransferService)

---

## 2. Result Pattern

### RULE-006: All Service Methods Return Result<T> or Result

Every public service method returns `Result<T>` (for read/query operations) or `Result` (for command operations). Services NEVER throw exceptions.

**Result<T> contract**:
- `Result<T>.Success(value)` — wraps a successful result with typed payload
- `Result<T>.Failure(error, errorCode?)` — wraps a failure with Arabic user-facing message and optional ErrorCode
- Caller (controller) inspects `.IsSuccess` and translates to appropriate HTTP status code

**Error codes** (from `SalesSystem.Contracts/ErrorCodes.cs`):
- `NotFound` → 404
- `ValidationError` → 400
- `Conflict` → 409
- `DomainRuleViolation` → 422
- `Unauthorized` → 403

**What NOT to do**: Never throw `KeyNotFoundException`, `InvalidOperationException`, or any other exception from a service method. The only exception allowed is `DomainException` (caught inside the service and converted to `Result.Failure`).

---

## 3. Unit of Work / Transactions

### IUnitOfWork Pattern (RULE-024)

`IUnitOfWork` wraps the EF Core DbContext and provides:
- Typed read/write repositories for every entity (e.g., `IUnitOfWork.SalesInvoices`, `IUnitOfWork.WarehouseStocks`)
- `SaveChangesAsync(CancellationToken)` — persists all pending changes
- `ExecuteTransactionAsync(Func<Task>, CancellationToken)` — atomic multi-save wrapper

### ExecuteTransactionAsync (RULE-275, RULE-276, RULE-281)

Because `SqlServerRetryingExecutionStrategy` is configured on the DbContext, calling `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()` directly is **prohibited** — the execution strategy does not support user-initiated transactions.

Instead, all multi-table financial operations use `IUnitOfWork.ExecuteTransactionAsync()`, which wraps:
1. `DbContext.Database.CreateExecutionStrategy().ExecuteAsync()` — retry-safe outer shell
2. Inside: `BeginTransactionAsync()` → operation → `CommitAsync()` (rolled back on any exception)

### 7-Step Transaction Protocol

Every service that modifies multiple tables follows this exact sequence:

| Step | Action | Example |
|------|--------|---------|
| 1 | Validate BEFORE transaction | Stock availability, PaidAmount ≤ TotalAmount, FromWarehouse ≠ ToWarehouse |
| 2 | Open transaction (via ExecuteTransactionAsync) | Wrapped in CreateExecutionStrategy + BeginTransactionAsync |
| 3 | Save the document entity (invoice, return, transfer, payment) | AddAsync + SaveChangesAsync (document now has an Id) |
| 4 | Modify stock (via InventoryService) | IncreaseStock / DecreaseStock — only AFTER document has Id |
| 5 | Update party balance | Customer.IncreaseBalance / Supplier.DecreaseBalance |
| 6 | Commit | CommitAsync inside ExecuteTransactionAsync delegate |
| 7 | On failure: rollback | Automatic via transaction disposal; caller receives Result.Failure |

---

## 4. Domain Rules & Entity Integrity

### DomainException (RULE-052, RULE-053)

All Domain entities use guard clauses in their factory methods and constructors. Violations throw `DomainException` with an Arabic message. Services catch these and convert to `Result.Failure`.

**Entities with guard clauses**: Product, Customer, Supplier, SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, WarehouseStock, StockTransfer, InventoryMovement, CustomerPayment, SupplierPayment, and all their line-item child entities.

### Financial Formulas (Computed in Domain Only — RULE-001, RULE-002)

All monetary calculations live inside Entity methods — NEVER in services, controllers, or UI:

| Formula | Computed In | Notes |
|---------|------------|-------|
| `LineTotal = Quantity × UnitPrice` | InvoiceLine / InvoiceItem | Per line item |
| `SubTotal = Σ LineTotal` | Invoice entity | Sum of all line totals |
| `NetTotal = SubTotal − Discount + Tax + OtherCharges` | Invoice entity | Final invoice total |
| `RemainingAmount = NetTotal − PaidAmount` | Invoice entity | Due/unpaid amount |
| `PaidAmount ≤ NetTotal` | Guard clause | DomainException if violated |

### Invoice Lifecycle State Machine (RULE-019, RULE-020, RULE-021)

All document entities (SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, StockTransfer) use `InvoiceStatus` enum:

```
Draft (1) → Posted (2) → Cancelled (3)   [terminal]
```

**Valid transitions**:
- Draft → Posted ✅ (stock + balance changes happen HERE)
- Draft → Cancelled ✅ (no stock/balance impact — document was never active)
- Posted → Cancelled ✅ (MUST reverse ALL stock and balance effects)

**Forbidden transitions**:
- Posted → Draft ❌ (never allow editing a posted document)
- Cancelled → anything ❌ (terminal state)

### Soft Delete Strategy (RULE-050, RULE-051)

- All `ActivatableEntity` inheritors use `IsActive = false` for soft delete
- Global EF Core query filter: `.HasQueryFilter(x => x.IsActive)`
- Document entities use `Status = Cancelled` — never `IsActive = false`
- Users are NEVER hard-deleted (invoices reference them via FK)
- `DeleteStrategy` enum: Cancel (0) / Deactivate (1) / Permanent (2)

### Thread-Safe Document Numbering (RULE-011, RULE-254 to RULE-261)

`DocumentSequenceService` uses `SemaphoreSlim` to serialize access. Every document type (SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, StockTransfer, CustomerPayment, SupplierPayment) has a row in `DocumentSequences` table with `NextNumber` (int). Invoice numbers are **int**, thread-safe, and UNIQUE per document type.

### Audit Trail (RULE-016, RULE-026)

All entities carry `CreatedByUserId` (int null FK → Users) and `CreatedAt` (datetime2). Document entities additionally carry `UpdatedByUserId` and `UpdatedAt`. Every stock change creates an `InventoryMovement` record with `QuantityBefore`, `QuantityChange`, `QuantityAfter`, `MovementType`, `ReferenceType`, and `ReferenceId`.

### Balance Direction Convention

```
Customer.CurrentBalance > 0 = Customer owes US money
Customer.CurrentBalance < 0 = We owe the customer
Supplier.CurrentBalance > 0 = We owe the supplier
Supplier.CurrentBalance < 0 = Supplier owes US
```

---

## 5. Key Flows

### Login Flow (RULE-305 to RULE-320)

1. User is created **passwordless**: `PasswordHash = null`, `MustChangePassword = true`
2. On first login: system returns `RequiresPasswordSetup` — redirect to password set screen
3. `SetInitialPassword()` validates `MustChangePassword == true` before hashing via BCrypt (work factor 12)
4. Subsequent logins: `RecordLoginAttempt()` — success resets counter, failure increments
5. At 5 failed attempts: `Status = UserStatus.Locked`
6. Every login attempt creates an `AuditLog` entry (LoginSuccess / LoginFailed / LoginBlocked)

### Sales Invoice Post Flow

1. **Validate** (pre-transaction): each line item has sufficient stock in selected warehouse; PaidsAmount ≤ NetTotal
2. **ExecuteTransactionAsync**:
   a. Create SalesInvoice with Status = Draft, compute all financial fields in domain
   b. Save invoice → ID assigned
   c. For each line: call InventoryService.DecreaseStock(productId, warehouseId, quantity) → creates InventoryMovement
   d. Update invoice Status = Posted
   e. If DueAmount > 0: call Customer.IncreaseBalance(DueAmount)
   f. Commit
3. Return `Result<SalesInvoiceDto>` with the posted invoice data

### Purchase Invoice Post Flow

1. **Validate**: supplier exists, warehouse exists, all product IDs valid
2. **ExecuteTransactionAsync**:
   a. Create PurchaseInvoice with Status = Draft
   b. Save invoice → ID assigned
   c. For each line: call InventoryService.IncreaseStock(productId, warehouseId, quantity) → creates InventoryBatch + InventoryMovement
   d. Update invoice Status = Posted
   e. If DueAmount > 0: call Supplier.IncreaseBalance(DueAmount)
   f. Commit

### Sales Return / Purchase Return Flow

1. **Validate** (pre-transaction): return quantities ≤ original invoice line quantities (aggregate all prior returns)
2. **ExecuteTransactionAsync**:
   a. Create return document (SalesReturn or PurchaseReturn) with Status = Draft
   b. Save → ID assigned
   c. Reverse stock: SalesReturn → InventoryService.IncreaseStock; PurchaseReturn → InventoryService.DecreaseStock
   d. Reverse balance: Customer.DecreaseBalance / Supplier.DecreaseBalance
   e. Status = Posted
   f. Commit

### Stock Transfer Flow

1. **Validate**: FromWarehouseId ≠ ToWarehouseId; each line has sufficient stock at source
2. **ExecuteTransactionAsync**:
   a. Create WarehouseTransfer with Status = Draft
   b. Save → ID assigned
   c. For each line: DecreaseStock at source, IncreaseStock at destination (two movements per line)
   d. Status = Posted
   e. Commit

### Payment Recording Flow

1. **Validate**: Amount > 0, party exists
2. **ExecuteTransactionAsync**:
   a. Create CustomerReceipt or SupplierPayment
   b. Save → ID assigned
   c. If CustomerReceipt: Customer.DecreaseBalance(Amount)
   d. If SupplierPayment: Supplier.DecreaseBalance(Amount)
   e. Status = Posted
   f. Commit
3. Note: CashBox transaction is recorded (no balance field on CashBox — balance lives on linked Account)

### EventBus Pattern (Desktop — RULE-012, RULE-013, RULE-034)

The Desktop (WPF) layer uses an EventBus for cross-ViewModel communication. After any mutating operation completes (save, post, cancel, delete), the ViewModel publishes a message with the entity ID only. Other ViewModels subscribe and reload from the API.

**Messages**: `ProductChangedMessage`, `CustomerChangedMessage`, `SupplierChangedMessage`, `SaleInvoiceChangedMessage`, `PurchaseInvoiceChangedMessage`, etc.

**Rules**:
- Messages carry entity ID only — NO data payloads (RULE-034)
- Subscribe in constructor, unsubscribe in `Dispose()` (RULE-012)
- Marshal handlers to UI thread via `Application.Current.Dispatcher` (RULE-013)

---

## 6. Task Breakdown

| # | Task | Dependencies | Description |
|---|------|-------------|-------------|
| 1 | Extend IUnitOfWork | Phase 2 entities & repos | Add repository properties for SalesInvoices, PurchaseInvoices, Returns, Transfers, Payments |
| 2 | Implement ExecuteTransactionAsync | IUnitOfWork | Add method wrapping CreateExecutionStrategy + explicit transaction |
| 3 | Implement InventoryService | IUnitOfWork | IncreaseStock, DecreaseStock, GetStock, CreateMovement — singular authority |
| 4 | Implement SalesService | InventoryService, IDocumentSequenceService | CreateDraft, Post, Cancel — full lifecycle |
| 5 | Implement PurchaseService | InventoryService, IDocumentSequenceService | CreateDraft, Post, Cancel — full lifecycle |
| 6 | Implement SalesReturnService | SalesService, InventoryService | Post with validation against original invoice |
| 7 | Implement PurchaseReturnService | PurchaseService, InventoryService | Post with validation against original invoice |
| 8 | Implement StockTransferService | InventoryService | Post with dual-warehouse stock movement |
| 9 | Implement PaymentService | IUnitOfWork | CustomerReceipt + SupplierPayment recording |
| 10 | Create API Request DTOs | Contracts project | CreateSalesInvoiceRequest, CreatePurchaseInvoiceRequest, PostInvoiceRequest, return requests, transfer request, payment requests |
| 11 | Create FluentValidators | Request DTOs | All 7 request validators with Arabic messages |
| 12 | Create API Controllers | All services | 6 controllers with [Authorize] + Result-to-HTTP translation |
| 13 | Register all services in DI | All implementations | Scoped registration in Program.cs |
| 14 | Wire EventBus in Desktop | API layer | Publish messages on save/post/cancel operations |

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Concurrent stock oversell | Medium | High | DB-level `CHECK (Quantity >= 0)` as safety net; pre-transaction validation; single-threaded DocumentSequenceService |
| Transaction not rolled back on exception | Low | Critical | ExecuteTransactionAsync always disposes transaction; failures automatically roll back |
| Duplicate invoice numbers | Low | High | SemaphoreSlim lock in DocumentSequenceService; UNIQUE index on InvoiceNo per table |
| Partial return exceeding original quantity | Medium | Medium | Aggregate all prior return quantities before validating; reject if sum > original |
| FK constraint violation on Cancel | Low | Medium | All FKs use DeleteBehavior.Restrict; cancellation only changes Status, never deletes |
| Passwordless user creation bypass | Low | Critical | Guard clauses enforce MustChangePassword validation before allowing any operation |
| JWT userId spoofing in journal entries | Low | Critical | Controller always extracts userId from JWT claims — never from request body |

---

## 8. Constitution Compliance

| Rule | Check | Notes |
|------|-------|-------|
| RULE-001/002 | ✅ | All money decimal(18,2), all quantities decimal(18,3) |
| RULE-006 | ✅ | All services return Result<T> or Result |
| RULE-011 | ✅ | DocumentSequenceService uses SemaphoreSlim lock |
| RULE-019/020/021 | ✅ | Invoice lifecycle: Draft → Posted → Cancelled |
| RULE-024 | ✅ | All multi-table ops via IUnitOfWork |
| RULE-050/051 | ✅ | DeleteStrategy enum; soft delete via IsActive |
| RULE-052/053 | ✅ | DomainException with Arabic guard clauses |
| RULE-141-146 | ✅ | ViewModel async operations via ExecuteAsync wrapper |
| RULE-147/148 | ✅ | Service Layer pattern — no MediatR |
| RULE-254-261 | ✅ | InvoiceNo as int, thread-safe via DocumentSequenceService |
| RULE-275/276 | ✅ | ExecuteTransactionAsync wraps CreateExecutionStrategy |
| RULE-305-320 | ✅ | Passwordless user creation; 5-failed-login lockout |
