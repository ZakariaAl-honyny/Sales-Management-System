# Implementation Plan: Cash Boxes (v4.3)

**Branch**: `009-cash-boxes` | **Date**: 2026-05-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/009-cash-boxes/spec.md`

---

## Summary

Implement a lightweight cash box subsystem where each cash register (desk, safe, bank account) is linked to a Chart of Accounts ledger account via `AccountId` FK — the balance lives on the GL Account, not on the CashBox entity. Cash movements are recorded through existing financial entities (`CustomerReceipts`, `SupplierPayments`, `ReceiptVouchers`, `PaymentVouchers`, `Expenses`) with `CashBoxId` FK — there is no separate `CashTransactions` table. When a CashBox is created without an explicit `AccountId`, the service auto-creates a Level-4 sub-account under parent `"1110 — النقدية"` (Cash & Cash Equivalents). Cash transfers between boxes are atomic dual-entry operations using `ReceiptVoucher` + `PaymentVoucher` inside a single transaction. `DailyClosure` is deferred to V2 — V1 focuses on core cash tracking with immutable transaction records.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, FluentValidation 11, Serilog 8
**Storage**: SQL Server 2019+ — new table: `CashBoxes`; extended: `SalesInvoices`, `PurchaseInvoices`, `CustomerReceipts`, `SupplierPayments`, `Expenses` (add `CashBoxId` FK); `Accounts` (linked GL balance)
**Testing**: xUnit + Moq + FluentAssertions
**Target Platform**: Windows x64 (API as Windows Service + WPF Desktop)
**Project Type**: Desktop App + REST API (Clean Architecture — 6 existing projects)
**Performance Goals**: Balance inquiry (sum of receipts less payments for a cash box) resolves in < 1 second for up to 10,000 entries
**Constraints**: All amounts = `decimal(18,2)`. Negative amounts forbidden at DB level via CHECK constraints. All multi-table operations inside `ExecuteTransactionAsync` (using `CreateExecutionStrategy`)
**Scale/Scope**: 1–10 cash boxes per store, each linked to its own GL account

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | All `Amount`, `TotalAmount` fields → `decimal(18,2)`; no float/double |
| II | Domain-Computed Financial Formulas | ✅ PASS | CashBox balance computed as SUM of linked receipts minus linked payments — never stored as a column |
| III | Transactional Integrity | ✅ PASS | Cash transfers: `PaymentVoucher` + `ReceiptVoucher` inside single `ExecuteTransactionAsync`. Invoice posting: receipt/payment created inside existing invoice transaction |
| IV | Invoice Lifecycle State Machine | ✅ PASS | Cash entry created on `Draft → Posted`. Reversal entry created on `Posted → Cancelled` via corresponding voucher |
| V | Stock Integrity | ✅ N/A | Cash boxes do not affect stock |
| VI | Result Pattern | ✅ PASS | All service methods return `Result<T>` or `Result` — never throw |
| VII | Clean Architecture Boundaries | ✅ PASS | `CashBox` domain entity has zero EF Core dependencies; `ICashBoxService` in Application layer |
| VIII | Security | ✅ PASS | All new endpoints carry `[Authorize]` with appropriate policies (AllStaff for reads, ManagerAndAbove for writes) |
| IX | Four-Layer Validation | ✅ PASS | Domain guard (name not empty, AccountId not null); Application pre-check (balance sufficiency for transfers); FluentValidation (amount > 0, valid references); DB CHECK (Amount > 0 on all financial tables) |
| X | Logging | ✅ PASS | All cash box creation, transfer, and balance-affecting operations logged via Serilog at `Information` level |
| XI | EF Core Conventions | ✅ PASS | Fluent API only; `DeleteBehavior.Restrict` on ALL FKs; `nvarchar` for name fields; `AccountId` FK is non-nullable |
| XII | Audit Trail | ✅ PASS | `CreatedByUserId` FK → Users on all financial entities; Account balance changes tracked via JournalEntries |
| XIII | Delete Strategy | ✅ PASS | `CashBox` — soft delete (`IsActive = false`) only. Hard delete blocked by referencing receipts/payments via FK with Restrict |
| XIV | Defensive Programming | ✅ PASS | `CashBox` constructor: name not empty, AccountId not null. `CustomerReceipt.Create()`: amount > 0, valid CashBoxId |
| XV | WPF Interactive Dialogs | ✅ PASS | All dialogs via `IDialogService` |
| XVI | Toast Notifications | ✅ PASS | Cash box created, transfer completed, expense recorded → toast notifications |
| XVII | Real-Time UI Validation | ✅ PASS | `CashBoxEditorViewModel` implements `INotifyDataErrorInfo` |

**Gate Result**: ✅ ALL CLEAR — proceed to phased implementation.

---

## Entity Architecture — Key Design Decisions

### CashBox Is Lightweight — Balance Lives on GL Account

**The old plan** stored `OpeningBalance` and `CurrentBalance` on `CashBox` with `CashTransaction` as the balance-building mechanism. **The new schema** takes a fundamentally different approach:

- `CashBox` has **no `OpeningBalance` or `CurrentBalance` fields**.
- `CashBox` has a **required `AccountId` FK** (int, non-nullable) linking to a Chart of Accounts `Account`.
- The cash balance is tracked on the linked `Account` through the general ledger (`JournalEntries` + `JournalEntryLines`), not on the CashBox entity.
- Every cash movement (receipt, payment, expense, transfer) creates a journal entry on the linked Account, keeping the GL always in sync.

This aligns with the accounting-first philosophy: **كل صندوق = حساب** (every cash box is an account). The Account entity owns the balance; CashBox is simply a label + metadata wrapper around that Account.

### No CashTransactions Table — Existing Entities Handle Movements

**KEY CHANGE from the old plan**: There is **no `CashTransactions` table**. The database schema replaces it with existing financial entities that carry a `CashBoxId` FK:

| Cash Movement | Entity | Table | Direction |
|---------------|--------|-------|-----------|
| Customer payment received | `CustomerReceipt` | 6.5 | Increases cash (Dr Cash, Cr AR) |
| Supplier payment made | `SupplierPayment` | 7.5 | Decreases cash (Dr AP, Cr Cash) |
| Manual cash receipt (non-invoice) | `ReceiptVoucher` | 4.7 | Increases cash |
| Manual cash payment (non-invoice) | `PaymentVoucher` | 4.8 | Decreases cash |
| Petty cash expense | `Expense` | 4.9 | Decreases cash |
| Sales invoice cash payment | `SalesInvoice.CashBoxId` | 6.1 | Increases cash (via CustomerReceipt linkage) |
| Purchase invoice cash payment | `PurchaseInvoice.CashBoxId` | 7.1 | Decreases cash (via SupplierPayment linkage) |

This design eliminates the redundant `CashTransaction` table and keeps all cash movements recorded through domain-appropriate entities that already have well-defined lifecycle and status management (Draft → Posted → Cancelled).

### CashBalance Computation

Since there is no `CashBox.CurrentBalance` column, the balance must be computed on demand. The `CashBoxService` provides a `GetBalanceAsync(cashBoxId)` method that queries:

```
Balance = SUM(CustomerReceipt.Amount WHERE CashBoxId = X AND Status = Posted)
        + SUM(ReceiptVoucher.TotalAmount WHERE CashBoxId = X AND Status = Posted)
        - SUM(SupplierPayment.Amount WHERE CashBoxId = X AND Status = Posted)
        - SUM(PaymentVoucher.TotalAmount WHERE CashBoxId = X AND Status = Posted)
        - SUM(Expense.Amount WHERE CashBoxId = X AND Status = Posted)
```

For performance, the service optionally caches the balance with a per-cash-box invalidation strategy (cache is invalidated on any write to that cash box's entities).

### Auto-Account Creation on CashBox Setup

When creating a CashBox without a pre-existing `AccountId`, the service automatically creates a Level-4 detail account under parent `"1110 — النقدية"` (Cash & Cash Equivalents):

1. Query the parent account by code `"1110"` — this is a Level-3 account under Current Assets (`1100`).
2. Find the maximum child account code under parent 1110 (e.g., `"1111"`, `"1112"`, etc.).
3. Generate the next code as `(maxCode + 1).ToString()` — e.g., if max is `"1112"`, the new code is `"1113"`.
4. Create a new `Account` with:
   - `AccountCode` = auto-generated (e.g., `"1113"`)
   - `Name` = `"صندوق {boxName}"` (Arabic) / `"Cash Box {boxName}"` (English)
   - `Nature` = `Asset` (1)
   - `Level` = 4 (Detail — leaf account allowing transactions)
   - `IsLeaf` = true
   - `IsSystem` = false (user-modifiable)
   - `ParentId` = parent 1110 account ID
5. Link the new account to the CashBox via `AccountId`.
6. All journal entries for this cash box post directly to this account.

This auto-creation is transparent to the user — they simply name the cash box and select a branch; the account is created automatically.

### Cash Transfer — Atomic Dual-Entry

Cash transfers between boxes are handled as a pair of voucher entries inside a single `ExecuteTransactionAsync`:

1. **Pre-validation**: Source cash box must have sufficient balance (computed via `GetBalanceAsync`). Source and destination must differ. All done BEFORE opening the transaction.
2. **Inside the transaction**:
   a. Create a `PaymentVoucher` on the source CashBox (debit — reduces cash).
   b. Create a `ReceiptVoucher` on the destination CashBox (credit — increases cash).
   c. Both vouchers reference each other via a `Notes` field containing the transfer reference.
   d. Create journal entry lines: credit source Account, debit destination Account.
3. **Commit**: Both vouchers are posted atomically. If either fails, the entire transfer rolls back.

There is **no client-side balance validation** — the server always validates via the computed balance query inside the transaction. This prevents race conditions where two concurrent transfers could both pass a pre-check but only one should succeed.

### DailyClosure Deferred to V2

**KEY CHANGE from the old plan**: `DailyClosure` is **not in V1**. The end-of-day snapshot computation (`OpeningBalance + TotalIncome - TotalExpense = ClosingBalance`) and the `DailyClosure` entity itself are deferred to a future phase. Rationale:

- V1's primary goal is correct cash tracking through the GL — the balance is always accurate because it lives on the Account.
- A formal daily closure requires additional features (physical cash count, difference reconciliation, override approvals) that are outside V1 scope.
- The balance at any point in time can be queried from the linked Account's journal entries — formal closure is a reporting concern, not a tracking concern.
- `CustomerReceipt`, `SupplierPayment`, `ReceiptVoucher`, and `PaymentVoucher` all carry `Status` (Draft → Posted → Cancelled), providing the immutability guarantee that a daily closure would provide.

### SalesInvoice / PurchaseInvoice CashBoxId Extension

Both `SalesInvoices` and `PurchaseInvoices` gain a nullable `CashBoxId` FK:

- `CashBoxId` is **nullable** because credit invoices (`PaidAmount = 0`) have no cash movement and thus no cash box association.
- When `PaidAmount > 0`, the invoice must have a `CashBoxId` — this is validated at the FluentValidation layer.
- On invoice posting, the `PaidAmount` creates a corresponding `CustomerReceipt` (for sales) or `SupplierPayment` (for purchases) linked to the same `CashBoxId`.
- On invoice cancellation (Status → Cancelled), the corresponding receipt/payment is also cancelled (Status → Cancelled), which reverses the cash impact.

### CashBox Branch Association

Each CashBox is associated with a `Branch` via `BranchId` FK (smallint, not null). This enables:

- Per-branch cash tracking — each branch can have its own cash boxes.
- Branch-level reporting: "Show me all cash boxes in Branch A."
- A CashBox must belong to exactly one branch; a branch can have multiple cash boxes.

The `BranchId` is set at creation and changes are rare (requiring manager approval if changed).

---

## V1 Scope — What's In and What's Out

### In Scope (V1)
| Feature | Entity/Mechanism | Priority |
|---------|-----------------|----------|
| CashBox CRUD | `CashBox` entity with Account auto-creation | P1 |
| Customer cash receipts | `CustomerReceipt` with `CashBoxId` FK | P1 |
| Supplier cash payments | `SupplierPayment` with `CashBoxId` FK | P1 |
| Manual cash receipts | `ReceiptVoucher` | P2 |
| Manual cash payments | `PaymentVoucher` | P2 |
| Petty cash expenses | `Expense` with `CashBoxId` FK | P2 |
| Balance query by cash box | Computed via SUM across all linked entities | P1 |
| Transaction history | Per-cash-box query across linked entities | P1 |
| Cash transfer between boxes | `PaymentVoucher` + `ReceiptVoucher` atomic dual | P2 |
| Invoice-linked cash movement | On `Post` → receipt/payment; On `Cancel` → reversal | P1 |
| Auto-account creation under 1110 | Service-level, transparent to user | P1 |

### Out of Scope (V1 — Deferred)
| Feature | Rationale | Target |
|---------|-----------|--------|
| `DailyClosure` entity | Requires physical count, reconciliation, override approval | V2 |
| `CashTransaction` table | Replaced by existing financial entities with CashBoxId FK | N/A (never) |
| `OpeningBalance` / `CurrentBalance` on CashBox | Balance tracked on linked Account via GL | N/A (never) |
| Cash box balance caching | Performance optimization — V1 query performance is adequate at < 10K entries | V2 |
| POS terminal integration | External payment terminals | V3 |
| Multi-currency cash boxes | Per-box currency is set at creation; multi-currency handling deferred | V2 |

---

## Transaction Patterns

### Pattern 1: Sales Invoice Posting with Cash Payment

1. User creates `SalesInvoice` with `CashBoxId`, `PaidAmount > 0`.
2. On Post (inside the posting transaction):
   a. Invoice Status → Posted, InvoiceNo assigned.
   b. Stock deducted from warehouse (existing logic).
   c. `CustomerReceipt` created: `CustomerId`, `CashBoxId`, `Amount = PaidAmount`, `CurrencyId`, Status = Posted.
   d. `CustomerReceiptApplication` created linking the receipt to the invoice.
   e. Journal entry: Dr Cash (CashBox.AccountId) / Cr AR (Customer.AccountId) for the paid amount.
   f. If `PaidAmount < NetTotal`, the remaining balance stays on the customer's AR.
3. **Cancellation**: Creates a reversal receipt (Status → Cancelled on the original receipt) plus reverse journal entry.

### Pattern 2: Cash Transfer Between Boxes

1. Pre-validate: Source CashBox balance ≥ transfer amount. Source ≠ Destination.
2. Inside `ExecuteTransactionAsync`:
   a. `PaymentVoucher` for source CashBox: `Amount`, `AccountId = Source.AccountId`, `Notes = "تحويل إلى {DestinationName}"`
   b. `ReceiptVoucher` for destination CashBox: `Amount`, `AccountId = Dest.AccountId`, `Notes = "تحويل من {SourceName}"`
   c. Journal entry: Dr Dest.AccountId / Cr Source.AccountId for the transfer amount.
3. Commit — both vouchers posted atomically.

### Pattern 3: Cash Box Balance Query

1. Service method `GetBalanceAsync(cashBoxId)`:
2. Run five parallel queries (or a single UNION query for performance):
   - SUM of posted `CustomerReceipt.Amount` WHERE `CashBoxId = X`
   - SUM of posted `ReceiptVoucher.TotalAmount` WHERE `CashBoxId = X`
   - SUM of posted `SupplierPayment.Amount` WHERE `CashBoxId = X` (subtracted)
   - SUM of posted `PaymentVoucher.TotalAmount` WHERE `CashBoxId = X` (subtracted)
   - SUM of posted `Expense.Amount` WHERE `CashBoxId = X` (subtracted)
3. Return `Result<decimal>` with the computed balance.

---

## Project Structure — Source Code

```
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       └── CashBox.cs                          ← NEW (lightweight: Name, AccountId, BranchId, Description)
│
├── SalesSystem.Contracts/
│   ├── Requests/
│   │   ├── CreateCashBoxRequest.cs             ← NEW (Name, BranchId, optional AccountId)
│   │   ├── UpdateCashBoxRequest.cs             ← NEW (Name, Description only — AccountId read-only)
│   │   └── CashTransferRequest.cs              ← NEW (SourceCashBoxId, DestCashBoxId, Amount, Notes)
│   └── Responses/
│       ├── CashBoxDto.cs                       ← NEW (Id, Name, AccountId, AccountName, BranchName, Balance)
│       ├── CashBoxBalanceDto.cs                ← NEW (CashBoxId, Balance, LastComputedAt)
│       └── CashBoxTransactionDto.cs            ← NEW (unified view of receipts + payments for history)
│
├── SalesSystem.Application/
│   ├── Interfaces/Services/
│   │   └── ICashBoxService.cs                  ← NEW
│   └── Services/
│       ├── CashBoxService.cs                   ← NEW (CRUD, balance computation, auto-account creation)
│       └── CashTransferService.cs              ← NEW (atomic dual-entry transfer logic)
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs                   ← EXTEND (add DbSet for CashBox)
│   │   └── Configurations/
│   │       └── CashBoxConfiguration.cs         ← NEW (FK AccountId, FK BranchId, UK Name+BranchId)
│   └── Migrations/                             ← NEW migration
│
├── SalesSystem.Api/
│   ├── Controllers/
│   │   └── CashBoxesController.cs              ← NEW (/api/v1/cash-boxes CRUD + balance + transfer + history)
│   └── Validators/
│       ├── CreateCashBoxRequestValidator.cs    ← NEW (Name required, BranchId valid, AccountId optional)
│       ├── UpdateCashBoxRequestValidator.cs    ← NEW (Name required, Description max length)
│       └── CashTransferRequestValidator.cs     ← NEW (Amount > 0, Source ≠ Destination)
│
└── SalesSystem.DesktopPWF/
    ├── Services/Api/
    │   └── CashBoxApiService.cs                ← NEW
    ├── Views/CashBoxes/
    │   ├── CashBoxesListView.xaml              ← NEW (DataGrid with computed balance column)
    │   ├── CashBoxEditorView.xaml              ← NEW (Name, Branch, Account auto-created)
    │   └── CashTransferDialog.xaml             ← NEW (Source, Destination, Amount, Notes)
    └── ViewModels/CashBoxes/
        ├── CashBoxesListViewModel.cs           ← NEW
        ├── CashBoxEditorViewModel.cs           ← NEW (INotifyDataErrorInfo)
        └── CashTransferViewModel.cs            ← NEW
```

---

## Complexity Tracking

| Concern | Complexity | Mitigation |
|---------|------------|------------|
| Balance computed from 5 entity types | Medium | Single `GetBalanceAsync` method with parallel SUM queries; UNION SQL view in V2 for optimization |
| Auto-account creation under 1110 | Medium | Encapsulated in `CashBoxService.CreateAsync` — transparent to caller; parent 1110 seeded by AccountingSeeder |
| Atomic cash transfer (dual-voucher) | Medium | `CashTransferService` uses `ExecuteTransactionAsync` — PreValidate → Create Pair → Commit — full rollback on any failure |
| Invoice ↔ CashBox lifecycle sync | Medium | Receipt/payment created inside invoice posting transaction; cancellation creates reversal; all in one atomic unit |
| No CashTransaction migration from old design | Low | New schema eliminates the table entirely — existing entities carry CashBoxId; no migration needed |
| DailyClosure deferred | Low | Balance-at-a-point queries use the same SUM logic — no DailyClosure entity needed in V1 |
