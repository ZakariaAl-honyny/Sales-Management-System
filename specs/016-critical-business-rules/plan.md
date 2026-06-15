# Implementation Plan: Critical Business Rules Enforcement (Phase 16)

**Branch**: `016-critical-business-rules` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase centralizes and enforces the core business rules that govern every money- and stock-affecting transaction across the system. It codifies the atomic transaction pattern (`IUnitOfWork.ExecuteTransactionAsync`), mandates the validation-ordering sequence (validate before opening transaction), enforces financial domain invariants (PaidAmount ≤ TotalAmount, balanced journal entries), standardizes thread-safe document numbering via `DocumentSequenceService`, and ensures stock mutations occur ONLY when an invoice transitions to the Posted state. The phase touches every transaction service: SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, StockTransfer, CustomerPayment, SupplierPayment, and InventoryAdjustment. It also ensures that reversal operations (cancellation of a Posted invoice, deletion of a payment) create symmetrical reverse journal entries through `AccountingIntegrationService`. No new entities or database schema changes are introduced — the focus is purely on the Application service layer and Domain method enforcement.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, ASP.NET Core)
**Architecture Scope**: Domain + Application layers primarily; minor API middleware updates for consistent error handling
**Key Patterns**: `IUnitOfWork.ExecuteTransactionAsync` (not `BeginTransactionAsync` — see RULE-275), `Result<T>` return types, Domain Guard Clauses, `AccountingIntegrationService` for journal entries
**Constraints**:
- `SqlServerRetryingExecutionStrategy` is configured — `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` are FORBIDDEN. Use `ExecuteTransactionAsync(Func<Task>, CancellationToken)` which wraps `CreateExecutionStrategy().ExecuteAsync()` with an explicit transaction.
- Every multi-entity mutation (stock + invoice + journal + balance) must occur within a single `ExecuteTransactionAsync` call.
- Stock validation must occur BEFORE opening the transaction to avoid unnecessary lock contention.
- Thread-safe InvoiceNo generation uses `DocumentSequenceService.GetNextIntAsync()` with `SemaphoreSlim` lock — NEVER `lastId + 1`.
- All service methods return `Result<T>` — no exceptions for business rule violations (use `Result.Failure` with Arabic error message).
- Domain entities enforce invariants via Guard Clauses in factory methods — throw `DomainException` which the service layer catches and maps to `Result.Failure`.
- Reversal operations (cancel, delete) MUST call `AccountingIntegrationService` to create offsetting journal entries — NEVER leave orphan ledger entries.

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ PASS | All money = decimal(18,2), quantity = decimal(18,3) — verified in every service |
| II | Domain Formulas | ✅ PASS | LineTotal, SubTotal, TotalAmount, DueAmount computed in Domain entities — never in service |
| III | Transactional Integrity | ✅ PASS | **CORE FOCUS** — ExecuteTransactionAsync wraps every multi-entity mutation |
| IV | Invoice Lifecycle | ✅ PASS | Draft→Posted→Cancelled transitions enforced; stock/balance affected ONLY on Posted |
| V | Stock Integrity | ✅ PASS | Stock pre-validated BEFORE transaction; WarehouseStock Quantity ≥ 0 CHECK constraint |
| VI | Result Pattern | ✅ PASS | All services return Result<T>; DomainException caught and mapped to Result.Failure |
| VII | Architecture Boundaries | ✅ PASS | Application coordinates; Domain is pure (zero dependencies) |
| VIII | Security | ✅ N/A | No security changes |
| IX | Four-Layer Validation | ✅ PASS | Domain guards + Service pre-checks + FluentValidation + DB constraints — all four layers active |
| X | Logging | ✅ PASS | Transaction rollbacks logged via Serilog.Log.Warning; system errors via Log.Error |
| XI | EF Core Conventions | ✅ PASS | ExecuteTransactionAsync uses CreateExecutionStrategy per RULE-275 |
| XII | Audit Trail | ✅ PASS | Every invoice mutation (create, post, cancel) records CreatedByUserId; payments record CreatedByUserId |
| XIII | Delete Strategy | ✅ PASS | Invoice cancellation uses Status = Cancelled (never hard delete) |
| XIV | Defensive Programming | ✅ PASS | Guard Clauses on all entity factory methods: SalesInvoice.Create(), Payment.Create(), etc. |

**Gate Result**: ✅ ALL CLEAR — No violations.

---

## Atomic Transaction Pattern

### The Problem with BeginTransactionAsync

EF Core's `SqlServerRetryingExecutionStrategy` (configured in `Program.cs` for resilience against transient SQL Server failures) does NOT support user-initiated `BeginTransactionAsync()`. When the execution strategy retries a failed operation, it does not replay user-managed transactions. This causes `InvalidOperationException` at runtime. Therefore, ALL explicit transactions must use `IUnitOfWork.ExecuteTransactionAsync()`.

### ExecuteTransactionAsync Contract

```text
IUnitOfWork.ExecuteTransactionAsync(operation, ct):
  1. Gets the DbContext's configured execution strategy
  2. Calls strategy.ExecuteAsync(...) which:
     a. Opens a new IDbContextTransaction via BeginTransactionAsync
     b. Executes the operation delegate
     c. If operation succeeds: CommitAsync the transaction
     d. If operation throws: RollbackAsync and let the strategy retry (if transient failure)
  3. Returns the result of the operation
```

The `operation` delegate receives no parameters — it accesses `IUnitOfWork` repositories and `SaveChangesAsync` through the closure. The delegate MUST NOT call `BeginTransactionAsync` or `CommitAsync` itself — the execution strategy manages these.

### Transactional Flow Diagram (Sales Invoice Example)

```text
1. Pre-validation (NO transaction):
   a. Validate request DTO via FluentValidation
   b. Check customer exists and is active
   c. For each item: check WarehouseStock.Quantity >= requested quantity
   d. Check customer credit limit (if PaymentType = Credit)
   e. Validate cash box exists (if PaymentType = Cash)

2. Open transaction via ExecuteTransactionAsync:
   a. Generate InvoiceNo via DocumentSequenceService.GetNextIntAsync("SalesInvoice")
   b. Create SalesInvoice entity (Status = Draft)
   c. Add invoice to repository
   d. SaveChangesAsync → invoice now has Id
   e. For each item:
      i.   Deduct stock: DeductStock(productId, warehouseId, quantity) — creates InventoryTransaction + InventoryTransactionLine
      ii.  Consume oldest InventoryBatch first (FIFO) — decrement QuantityRemaining
   f. Update Customer.CurrentBalance += invoice.DueAmount (if credit sale)
   g. Update invoice Status to Posted
   h. Call AccountingIntegrationService to create journal entries:
      - Dr Cash/AR / Cr SalesRevenue / Cr VAT Output
      - Dr COGS / Cr Inventory
   i. Create CashTransaction if payment is cash
   j. SaveChangesAsync (second save — captures all mutations from steps e–i)

3. On success: return Result<SalesInvoiceDto>.Success(dto)
4. On failure: ExecuteTransactionAsync rolls back automatically → return Result.Failure(error)
```

### Multi-Table Operations That MUST Use ExecuteTransactionAsync

| Operation | Tables Mutated (within one transaction) |
|-----------|----------------------------------------|
| Post Sales Invoice | SalesInvoices, SalesInvoiceItems, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries, JournalEntryLines, CashTransactions (if cash), Customers (balance) |
| Post Purchase Invoice | PurchaseInvoices, PurchaseInvoiceItems, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries, JournalEntryLines |
| Post Sales Return | SalesReturns, SalesReturnItems, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries, JournalEntryLines |
| Post Purchase Return | PurchaseReturns, PurchaseReturnItems, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries, JournalEntryLines |
| Stock Transfer | WarehouseTransfers, WarehouseTransferLines, WarehouseStocks (source + dest), InventoryTransactions, InventoryTransactionLines |
| Customer Payment | ReceiptVouchers, JournalEntries, JournalEntryLines, Customers (balance) |
| Supplier Payment | PaymentVouchers, JournalEntries, JournalEntryLines, Suppliers (balance) |
| Cancel Posted Invoice | SalesInvoices/PurchaseInvoices (Status), WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries (reversal), JournalEntryLines, Customers/Suppliers (balance) |
| Inventory Adjustment | InventoryAdjustments, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, JournalEntries, JournalEntryLines |

---

## Validation Ordering (RULE-027)

### Pattern: Validate Before Transaction

Stock validation is an expensive operation that reads current `WarehouseStock.Quantity` for each line item. Opening a transaction for validation wastes database resources and increases lock contention. The pattern is:

```text
1. Validate all pre-conditions (reads only, no writes):
   - For each item: query WarehouseStock, compare Quantity >= requested
   - For each item: verify product exists, is active, has active price
   - For customer: verify exists, is active, credit check (if credit sale)
   - For cash box: verify exists, is active
   
2. If ANY validation fails: return Result.Failure immediately — NO transaction opened

3. If ALL validations pass: open ExecuteTransactionAsync
```

This ensures that transaction duration is minimized to only the write operations. Long-running validation (e.g., checking FIFO batch availability) happens in the pre-transaction phase.

### When Validation MUST Be Inside the Transaction

Some validations depend on state that might change between validation and write. These MUST be inside the transaction:

1. **InvoiceNo uniqueness**: `DocumentSequenceService.GetNextIntAsync` is thread-safe by design (SemaphoreSlim lock) — the lock IS the validation. It's called inside the transaction but the lock ensures uniqueness regardless.
2. **Journal entry balance**: The `AccountingIntegrationService` verifies that `Sum(Debit) == Sum(Credit)` before creating entries — this happens inside the transaction.
3. **FIFO batch consumption**: The actual `QuantityRemaining` values are read inside the transaction because they may change between pre-validation and write under high concurrency. The pre-validation is a coarse check (`SUM(QuantityRemaining) >= requested`), but the actual FIFO allocation reads and decrements individual batches inside the transaction.

---

## Financial Domain Invariants

### PaidAmount Constraint

Every invoice entity (SalesInvoice, PurchaseInvoice) enforces in its `SetPaidAmount()` method:

```text
if (paidAmount > TotalAmount)
    throw new DomainException("المبلغ المدفوع أكبر من الإجمالي")
```

This is a Domain-level Guard Clause (RULE-052). The service layer catches the `DomainException` inside `ExecuteTransactionAsync`, which triggers a rollback. The service then maps the exception to `Result.Failure("المبلغ المدفوع أكبر من الإجمالي")` — the user never sees a raw exception.

### DueAmount Computation

`DueAmount` is always computed, never stored:

```text
DueAmount = TotalAmount - PaidAmount
```

This formula lives in the entity property getter, not in the database or service. The database stores `PaidAmount` (decimal(18,2)) and `TotalAmount` (decimal(18,2)), but never `DueAmount`.

### NetRevenue Validation (for AccountingIntegrationService)

When creating the revenue-side journal entry for sales:

```text
NetRevenue = SubTotal - Discount
if (NetRevenue < 0)
    return Result.Failure("الخصم أكبر من إجمالي الفاتورة — لا يمكن أن يكون صافي الإيراد سالباً")
```

NetRevenue is NEVER clamped to zero — that would create unbalanced journal entries (Dr side != Cr side). If the discount exceeds the subtotal, the operation is rejected with a clear Arabic error message.

### Balanced Journal Entry Assertion

`AccountingIntegrationService` validates before saving:

```text
var totalDebit = lines.Sum(l => l.Debit);
var totalCredit = lines.Sum(l => l.Credit);
if (totalDebit != totalCredit)
    throw new InvalidOperationException($"Journal entry unbalanced: Debit={totalDebit}, Credit={totalCredit}")
```

This is a safety net — properly designed journal entries are always balanced by construction, but this assertion catches programming errors in service code.

---

## Thread-Safe Document Numbering

### DocumentSequenceService Contract

The `IDocumentSequenceService` interface provides:

```text
Task<Result<int>> GetNextIntAsync(string documentType, CancellationToken ct)
```

Where `documentType` is one of: `"SalesInvoice"`, `"PurchaseInvoice"`, `"SalesReturn"`, `"PurchaseReturn"`, `"ReceiptVoucher"`, `"PaymentVoucher"`.

### Locking Strategy

The service uses a `SemaphoreSlim(1, 1)` static lock to serialize access to the `DocumentSequences` table:

```text
1. Acquire SemaphoreSlim lock (async) — waits if another thread holds it
2. Query DocumentSequences for the given documentType
3. If not found: create with NextNumber = 1
4. Read current NextNumber
5. Increment NextNumber in the database
6. SaveChangesAsync
7. Release SemaphoreSlim
8. Return the old NextNumber (the one assigned to this call)
```

This guarantees that no two concurrent callers receive the same number. The `SemaphoreSlim` is scoped to the service instance — since the service is registered as Singleton (or at least the lock is static), the lock is application-wide.

### InvoiceNo on Create Request DTOs

`CreateSalesInvoiceRequest.InvoiceNo` and `CreatePurchaseInvoiceRequest.InvoiceNo` are `int?`:

- `null` or `<= 0` → auto-generate via `DocumentSequenceService.GetNextIntAsync`
- `> 0` → use as-is (service validates uniqueness with `AnyAsync(i => i.InvoiceNo == request.InvoiceNo)`)

The UNIQUE index on `SalesInvoices.InvoiceNo` (and `PurchaseInvoices.InvoiceNo`) provides the last line of defense — if two services somehow get the same number, the `SaveChangesAsync` will throw `DbUpdateException` with a unique constraint violation, triggering a rollback.

---

## Stock Mutation Rules

### When Stock Changes

Stock is NEVER modified when an invoice is created in Draft status. Only the transition to Posted triggers stock changes:

| Operation | Stock Effect | When |
|-----------|-------------|------|
| SalesInvoice → Posted | Decrease | On post |
| SalesInvoice → Draft | None | On create/save |
| SalesInvoice → Cancelled (from Posted) | Increase (reverse) | On cancel |
| PurchaseInvoice → Posted | Increase | On post |
| PurchaseInvoice → Draft | None | On create/save |
| PurchaseInvoice → Cancelled (from Posted) | Decrease (reverse) | On cancel |
| SalesReturn → Posted | Increase | On post |
| PurchaseReturn → Posted | Decrease | On post |
| StockTransfer → Posted | Decrease source / Increase dest | On post |

### FIFO Batch Consumption

When posting a SalesInvoice, line items consume from `InventoryBatches` with `QuantityRemaining > 0`, ordered by `CreatedAt ASC` (oldest first). If the product has `TrackExpiry = true`, batches are ordered by `ExpiryDate ASC` (soonest expiry first — FEFO).

```text
remaining = saleQuantity
for each batch in batches ordered by (TrackExpiry ? ExpiryDate : CreatedAt) ASC:
    if remaining <= 0: break
    deductAmount = Min(batch.QuantityRemaining, remaining)
    batch.QuantityRemaining -= deductAmount
    remaining -= deductAmount
    record InventoryTransactionLine(batch.Id, deductAmount, batch.UnitCost)
```

COGS = `SUM(InventoryTransactionLine.CostAmount)` across all consumed batch lines.

### Sales Return Batch Handling

Sales returns do NOT restore original batches. Instead, a NEW `InventoryBatch` is created with:
- `UnitCost` = original cost from the consumed batch (preserves cost fidelity)
- `BatchNo` = next batch number for this product
- `QuantityReceived = returnQty`, `QuantityRemaining = returnQty`

This ensures that returned goods are treated as a new "incoming" batch — they will be consumed FIFO after existing stock but before future purchases.

---

## Payment and Balance Rules

### Customer Payment

A `ReceiptVoucher` records money received from a customer:

```text
1. Pre-validate: customer exists, amount > 0, cash box exists (if cash)
2. ExecuteTransactionAsync:
   a. Create ReceiptVoucher
   b. Decrease Customer.CurrentBalance by amount
   c. Create journal entry: Dr Cash / Cr AccountsReceivable (using Customer.AccountId)
   d. SaveChangesAsync
```

### Supplier Payment

A `PaymentVoucher` records money paid to a supplier:

```text
1. Pre-validate: supplier exists, amount > 0, cash box exists (if cash)
2. ExecuteTransactionAsync:
   a. Create PaymentVoucher
   b. Decrease Supplier.CurrentBalance by amount
   c. Create journal entry: Dr AccountsPayable (using Supplier.AccountId) / Cr Cash
   d. SaveChangesAsync
```

### Reversal on Payment Delete

When a payment is deleted (soft-delete via `IsActive = false`), a reversal journal entry is created:

```text
1. Load existing ReceiptVoucher/PaymentVoucher
2. ExecuteTransactionAsync:
   a. Set IsActive = false on the voucher
   b. Reverse the journal entry: swap Dr ↔ Cr on all lines
   c. Reverse Customer/Supplier balance change
   d. SaveChangesAsync
```

---

## Cancellation Rules

### Posted Invoice Cancellation

Cancelling a Posted invoice is the most complex reversal operation. It MUST:

1. Reverse ALL stock changes (increase for sales, decrease for purchases)
2. Reverse ALL batch consumption (restore original `QuantityRemaining` for the specific batches that were consumed — requires the `InventoryTransactionLine` records to know which batches were affected)
3. Reverse Customer/Supplier balance changes
4. Create reversal journal entries (Dr ↔ Cr swapped)
5. Reverse cash transactions (if any)
6. Set invoice `Status = Cancelled`

All six steps happen inside a single `ExecuteTransactionAsync`. If any step fails, the entire reversal rolls back — the invoice remains Posted.

### Draft Invoice Cancellation

A Draft invoice has zero stock, balance, or journal impact. Cancelling a Draft simply sets `Status = Cancelled`. No transaction needed — a single `SaveChangesAsync` suffices (which EF Core wraps in an implicit transaction).

---

## Error Handling Pattern

### In Service Layer

```text
try
{
    await _uow.ExecuteTransactionAsync(async () =>
    {
        // ... business logic ...
    }, ct);
    return Result<T>.Success(dto);
}
catch (DomainException ex)
{
    // Business rule violation — log as Warning per RULE-183
    Log.Warning("Business rule violation: {Error}", ex.Message);
    return Result<T>.Failure(ex.Message);
}
catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
{
    // Duplicate InvoiceNo or other unique constraint
    Log.Warning("Unique constraint violation: {Error}", ex.Message);
    return Result<T>.Failure("رقم الفاتورة موجود مسبقاً");
}
catch (Exception ex)
{
    // System error — log as Error per RULE-182
    Log.Error(ex, "Transaction failed for {Operation}", operationName);
    return Result<T>.Failure("حدث خطأ أثناء حفظ العملية");
}
```

### In API Middleware

The `ExceptionMiddleware` catches unhandled exceptions that escape the service layer:
- `DomainException` → 400 Bad Request with Arabic message
- `InvalidOperationException` (connection string issues) → 503 Service Unavailable with `DATABASE_CONNECTION_ERROR` code
- All other exceptions → 500 Internal Server Error with generic Arabic message (raw exception logged via Serilog)

The middleware does NOT attempt to handle business logic exceptions — those are caught by the service layer and returned as `Result.Failure` responses. The middleware is a last-resort safety net for programming errors.

---

## Project Structure

```
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── Sales/SalesInvoice.cs              ← VERIFY: SetPaidAmount(), Cancel() guard clauses
│       ├── Purchases/PurchaseInvoice.cs       ← VERIFY: SetPaidAmount(), Cancel() guard clauses
│       └── Products/WarehouseStock.cs         ← VERIFY: DeductStock(), AddStock() guard clauses
├── SalesSystem.Application/
│   ├── Core/
│   │   └── IUnitOfWork.cs                     ← VERIFY: ExecuteTransactionAsync exists (already added)
│   ├── Services/
│   │   ├── Sales/SalesInvoiceService.cs       ← REFACTOR: wrap in ExecuteTransactionAsync, pre-validate stock
│   │   ├── Purchases/PurchaseInvoiceService.cs ← REFACTOR: same pattern
│   │   ├── Sales/SalesReturnService.cs        ← REFACTOR: same pattern
│   │   ├── Purchases/PurchaseReturnService.cs ← REFACTOR: same pattern
│   │   ├── Inventory/StockTransferService.cs  ← REFACTOR: atomic source/dest
│   │   ├── Inventory/InventoryAdjustmentService.cs ← REFACTOR: same pattern
│   │   ├── Payments/CustomerPaymentService.cs  ← REFACTOR: wrap in transaction
│   │   ├── Payments/SupplierPaymentService.cs  ← REFACTOR: wrap in transaction
│   │   └── DocumentSequenceService.cs         ← VERIFY: SemaphoreSlim lock exists
│   └── Accounting/
│       └── AccountingIntegrationService.cs    ← VERIFY: balanced entry assertion, reversal support
├── SalesSystem.Api/
│   └── Middleware/
│       └── ExceptionMiddleware.cs             ← VERIFY: DomainException → 400, DB exception → 503
└── SalesSystem.Infrastructure/
    └── Persistence/
        └── UnitOfWork.cs                      ← VERIFY: ExecuteTransactionAsync uses CreateExecutionStrategy
```

---

## Verification Checklist

- [ ] `SqlServerRetryingExecutionStrategy` is configured — `BeginTransactionAsync` is NEVER called directly
- [ ] Every multi-entity mutation uses `IUnitOfWork.ExecuteTransactionAsync`
- [ ] Stock pre-validation happens BEFORE transaction (read-only queries)
- [ ] All services return `Result<T>` — no raw `DomainException` leaks to API
- [ ] `PaidAmount ≤ TotalAmount` enforced in Domain entity (Guard Clause)
- [ ] `NetRevenue = SubTotal - Discount` validated as ≥ 0 (never clamped to zero)
- [ ] Journal entries always balanced (`Sum(Debit) == Sum(Credit)`)
- [ ] `DocumentSequenceService.GetNextIntAsync` uses `SemaphoreSlim` for thread safety
- [ ] InvoiceNo UNIQUE index on SalesInvoices and PurchaseInvoices tables
- [ ] Stock mutations happen ONLY on Posted status — never on Draft
- [ ] FIFO batch consumption: oldest batches consumed first (by CreatedAt or ExpiryDate)
- [ ] Sales returns create NEW batches (don't restore originals)
- [ ] Payment create/delete/create reversal journal entries through AccountingIntegrationService
- [ ] Posted invoice cancellation reverses ALL: stock, batches, balances, journal entries, cash transactions
- [ ] Draft invoice cancellation is a simple Status change (no stock/balance impact)
- [ ] DomainException → `Result.Failure` with Arabic message (not Exception propagation)
- [ ] `ExceptionMiddleware` handles unhandled exceptions with appropriate HTTP codes
- [ ] All cancellation operations use `Status = Cancelled` (never hard delete)
- [ ] Build: 0 errors, 0 warnings
