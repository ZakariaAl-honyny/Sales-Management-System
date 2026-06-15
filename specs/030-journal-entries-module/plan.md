# Phase 30 — Journal Entries & Fiscal Year Module: Implementation Plan

> **Version**: 1.0 — Aligned with V1 database-schema.md tables 2.9, 4.1, 4.5, 4.6, 4.9, 4.10
> **Scope**: Manual journal entry creation, expense recording, fiscal year management, annual closing, system account mapping configuration, viewing/searching entries
> **Dependency**: Phase 22 (Chart of Accounts — Accounts entity with 4-level hierarchy), Phase 24 (Accounting Engine Automation — auto-entry providers), Phase 29 (Receipts & Payments — CashBox integration)

---

## 1. Summary

The Journal Entries module is the core accounting engine of the system. It provides a unified `JournalEntry` entity that stores both **manually created entries** (by accountants for adjustments, accruals, corrections) and **automatically generated entries** (from sales, purchases, payments, receipts, inventory operations). Every entry has a 3-state lifecycle (Draft → Posted → Cancelled) and supports multi-line debit/credit pairs with strict balancing rules.

Supporting entities include `FiscalYears` (open/close lifecycle with annual closing), `Expenses` (recorded as PaymentVoucher-type journal entries), `SystemAccountMappings` (flexible key-value configuration resolving which Account to use for each economic event), and `AccountCategories` (lightweight grouping of accounts).

The design prioritises **audit trail integrity**: no hard-delete of posted entries, reversal via mirrored entries, and `CreatedByUserId` extracted exclusively from JWT claims.

---

## 2. Key Entities

### 2.1 AccountCategories (فئات الحسابات)

| Field | Type | Notes |
|-------|------|-------|
| Id | smallint PK | Small lookup table |
| Name | nvarchar(100) not null | Arabic category name |
| Description | nvarchar(300) nullable | Optional description |

Groups accounts by functional category (e.g., "نقدية", "بنوك", "عملاء", "مخزون", "أصول ثابتة"). Lightweight — separate from the hierarchical tree. Seeded with ~8 categories covering cash, banks, customers, suppliers, inventory, fixed assets, taxes, revenues, and expenses.

### 2.2 FiscalYears (السنوات المالية)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| YearName | nvarchar(20) not null | e.g., "2026" |
| StartDate | date not null | First day of fiscal year |
| EndDate | date not null | Last day, CHK `EndDate > StartDate` |
| IsClosed | bit not null default 0 | True after annual closing |

**Inherited fields**: CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId.

Only open/close lifecycle — no deletion of fiscal years. On close, all revenue and expense account balances transfer to retained earnings, and the year is locked against new entries.

### 2.3 JournalEntries (القيود اليومية)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| EntryNo | int not null | User-facing number, unique per fiscal year |
| EntryDate | date not null | Document date |
| EntryType | tinyint not null | 1=Manual, 2=Sales, 3=Purchase, 4=Receipt, 5=Payment, 6=Inventory, 7=Adjustment |
| ReferenceType | nvarchar(50) nullable | Source document type: 'SalesInvoice', 'PurchaseInvoice', 'CustomerReceipt', 'SupplierPayment', 'Expense', etc. |
| ReferenceId | int nullable | FK to the source document |
| Description | nvarchar(500) nullable | Arabic description of entry purpose |
| Status | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| IsReversed | bit not null default 0 | Flag indicating this entry reverses another |
| ReversedByEntryId | int nullable FK → self | Points to reversal entry (Restrict delete) |

**Inherited fields**: CreatedAt (from JWT), CreatedByUserId, UpdatedAt, UpdatedByUserId.

**Indexes**: `(EntryNo)`, `(ReferenceType, ReferenceId)` composite filtered `WHERE ReferenceType IS NOT NULL AND ReferenceId IS NOT NULL`.

### 2.4 JournalEntryLines (تفاصيل القيد)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| JournalEntryId | int FK → JournalEntries | Parent entry (Restrict) |
| AccountId | int FK → Accounts | Account being debited/credited (Restrict) |
| Debit | decimal(18,2) not null default 0 | CHK `>= 0` |
| Credit | decimal(18,2) not null default 0 | CHK `>= 0` |
| Description | nvarchar(300) nullable | Line-level explanation |
| SortOrder | smallint not null default 0 | Display ordering |

**CHECK constraints**:
- `CHK_DebitOrCredit`: Exactly one of `(Debit > 0 AND Credit = 0)` OR `(Credit > 0 AND Debit = 0)` OR `(Debit = 0 AND Credit = 0)` — a line cannot have both non-zero.
- `CHK_NoNegativeValues`: `Debit >= 0 AND Credit >= 0`.

**Entry-level balance rule** (enforced in service): `SUM(Debit) == SUM(Credit)`. Every entry must be balanced.

### 2.5 Expenses (المصروفات)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| ExpenseNo | int not null | Unique user-facing number |
| ExpenseDate | date not null | Date of expense |
| ExpenseAccountId | int FK → Accounts | Expense account (e.g., "مصروف إيجار") — Restrict |
| CashBoxId | int FK → CashBoxes | Cash box used for payment — Restrict |
| CurrencyId | smallint FK → Currencies | Transaction currency — Restrict |
| Amount | decimal(18,2) not null | Total expense amount |
| Notes | nvarchar(500) nullable | Optional notes |
| Status | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |

**Inherited fields**: CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId.

Expenses are a specialised form of PaymentVoucher. When Posted, the service creates an automatic journal entry: **Dr** `ExpenseAccountId` (expense account), **Cr** `CashBox.AccountId` (linked cash account). Cancellation creates a reversal entry.

### 2.6 SystemAccountMappings (ربط الحسابات الافتراضية)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| MappingKey | nvarchar(100) not null | e.g., "SalesRevenue", "COGS", "InventoryAsset" |
| AccountId | int FK → Accounts | Target account — Restrict |
| BranchId | smallint nullable FK → Branches | Branch-specific override (optional) |

**Inherited fields**: CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId.

**Flexible key-value design**: Instead of fixed columns on a configuration table, each system function is mapped by a unique string key. This allows adding new mappings without schema changes. Seeded keys include:

| MappingKey | Purpose | Default Account |
|-----------|---------|----------------|
| `SalesRevenue` | Revenue side of sales | 1520 — إيرادات المبيعات |
| `COGS` | Cost of goods sold debit | 1510 — تكلفة المبيعات |
| `InventoryAsset` | Inventory asset account | 1310 — المخزون |
| `DefaultCashAccount` | Default cash account (1110 parent auto-created) | 1110 — النقدية |
| `VATOutput` | VAT collected on sales | 2520 — ضريبة المبيعات |
| `VATInput` | VAT paid on purchases | 1315 — ضريبة المشتريات |
| `OpeningBalanceEquity` | Counter-entry for opening balances | 3130 — أرصدة افتتاحية |
| `SalesDiscount` | Discount allowed on sales | 5230 — خصم مسموح به |
| `PurchaseDiscount` | Discount received on purchases | 4520 — خصم مكتسب |
| `RetainedEarnings` | Annual closing transfer target | 3120 — أرباح مبقاة |
| `PurchaseReturn` | Contra-expense for purchase returns | 1632 — مردودات مشتريات |

---

## 3. Enums

| Enum | Values |
|------|--------|
| `JournalEntryType` | Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7 |
| `JournalEntryStatus` | Draft=1, Posted=2, Cancelled=3 |
| `AccountNature` | Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5 |
| `ExpenseStatus` | Draft=1, Posted=2, Cancelled=3 |

---

## 4. Business Rules

### 4.1 Journal Entry Rules (RULE-416 → RULE-420)

1. **3-state lifecycle**: Draft (1) → Posted (2) → Cancelled (3). A Draft entry can be edited freely. Once Posted, only cancellation is allowed — no editing.
2. **Balance enforcement**: `SUM(Debit) == SUM(Credit)` validated in service before save. An entry with unbalanced totals is rejected.
3. **No hard-delete**: Journal entries are NEVER hard-deleted. Cancellation sets `Status = Cancelled` and creates a reversal entry.
4. **Reversal pattern**: When cancelling a Posted entry, a new entry is created with `IsReversed = true` and `ReversedByEntryId` pointing back. The reversal entry mirrors the original with Dr↔Cr swapped on every line.
5. **EntryNo uniqueness**: Entry numbers are unique per fiscal year. A `DocumentSequence` for `"JournalEntry"` generates `EntryNo` incrementally. On fiscal year rollover, the sequence resets.
6. **Multi-line support**: Any number of lines is allowed (minimum 2 for a valid balanced entry). A single-line entry is rejected.
7. **Reference tracing**: `ReferenceType`/`ReferenceId` composite index enables fast lookup for reversal and audit queries. System-generated entries always populate these fields; manual entries leave them null.
8. **UserId from JWT**: `CreatedByUserId` is extracted from JWT claims by the controller and passed through the service chain. NEVER accepted from client request payload.

### 4.2 Fiscal Year Rules

1. **Open/close only**: Fiscal years cannot be deleted. Once created, they are either open or closed.
2. **Entry guard**: Before creating ANY journal entry, the service checks if the entry date falls within an open fiscal year. If the year is closed, return `Result.Failure("السنة المالية مغلقة")`.
3. **Annual closing process** (see Section 6):
   - Transfer all Revenue account balances (Nature=4) to `RetainedEarnings`.
   - Transfer all Expense account balances (Nature=5) to `RetainedEarnings`.
   - Close all revenue and expense accounts (zero their balance).
   - Set `IsClosed = true` on the fiscal year.
   - Create an opening entry for the new fiscal year (mapping asset/liability/equity balances forward).
4. **No partial close**: The entire year closes atomically inside `ExecuteTransactionAsync`.

### 4.3 Expense Rules

1. **Expenses as mini PaymentVouchers**: When Posted, an Expense automatically generates a journal entry: Dr `ExpenseAccountId`, Cr `CashBox.AccountId`.
2. **Status lifecycle**: Same 3-state lifecycle (Draft → Posted → Cancelled).
3. **Cancellation creates reversal**: Cancelling a Posted expense creates a reversal journal entry swapping Dr↔Cr.
4. **ExpenseNo unique**: Independently generated via `DocumentSequence` key `"Expense"`.
5. **Multi-currency**: Each expense carries its own `CurrencyId` FK. The amount is always in the expense's transaction currency.

### 4.4 SystemAccountMappings Rules

1. **MappingKey uniqueness**: Each key is unique per branch (unique constraint on `(MappingKey, BranchId)` where `BranchId` is treated as NULL for global mappings).
2. **Lookup resolution**: Service queries by `MappingKey + BranchId`. If no branch-specific mapping exists, fall back to `BranchId IS NULL` (global default).
3. **Seeded mappings cannot be deleted**: `IsSystem` protection is handled at the Accounts level (system accounts cannot be removed from mappings). Mappings themselves can be reconfigured to point to different accounts.

---

## 5. Journal Entry Lifecycle

### 5.1 Manual Entry Creation (by Accountant)

1. User opens "قيد يدوي جديد" screen.
2. Enters: EntryDate, Description (required), then adds lines selecting AccountId + Debit/Credit ± optional line description.
3. UI shows running totals for `SUM(Debit)` vs `SUM(Credit)` — balance indicator turns green when equal.
4. User clicks **Save as Draft** (entry saved with Status=Draft, no fiscal impact) or **Post** (Status=Posted, entry is final).
5. On Post validation:
   - At least 2 lines with non-zero amounts.
   - SUM(Debit) == SUM(Credit) exactly.
   - No line has both Debit > 0 AND Credit > 0.
   - EntryDate falls within an open fiscal year.
   - All AccountIds exist and are leaf accounts (IsLeaf=true).
6. On success, `EntryNo` is generated from `DocumentSequence` key `"JournalEntry"`.

### 5.2 Posting a Draft Entry

- Changes `Status` from Draft to Posted.
- No reversal or undo (must cancel and recreate).
- Once Posted, all lines are immutable.

### 5.3 Cancelling a Posted Entry

1. System finds the original Posted entry.
2. Creates a NEW reversal entry with:
   - `EntryType` = same as original.
   - `IsReversed = true`.
   - `ReversedByEntryId` = original entry's Id.
   - `Description` = "عكس: " + original description.
   - Each line has Debit↔Credit swapped.
3. Original entry's `IsReversed` is set to `true`.
4. Original entry's `ReversedByEntryId` points to the new reversal entry.
5. Both original and reversal entries remain visible in the ledger for audit.

### 5.4 Auto-Generated Entries

Automatic entries (from Phase 24 Accounting Engine) follow the same lifecycle:
- Created directly as Posted (no Draft step) inside the parent transaction.
- `ReferenceType`/`ReferenceId` populated with the source document type and ID.
- `EntryType` set according to the operation:
  - Sales = 2, Purchase = 3, Receipt = 4, Payment = 5, Inventory = 6.
- Manual entries = 1, Adjustments = 7.

---

## 6. Fiscal Year Lifecycle

### 6.1 Opening a Fiscal Year

1. Admin creates a new fiscal year with `YearName`, `StartDate`, `EndDate`.
2. `IsClosed = false`.
3. Only one fiscal year can be active (open) at a time for entry creation.
4. Overlapping date ranges across years are validated (no two open years can have overlapping date ranges).

### 6.2 Annual Closing Process

Executed atomically inside `ExecuteTransactionAsync`:

1. **Guard checks**:
   - Fiscal year is not already closed.
   - No Draft (unposted) journal entries exist for this year.
   - Calling user has Admin privilege.

2. **Revenue transfer**: For each Account with Nature=4 (Revenue):
   - Calculate net balance = SUM(Credit) − SUM(Debit) from all Posted journal entry lines.
   - Create closing entry: Dr RevenueAccount (zero it), Cr RetainedEarnings.
   - Uses `SystemAccountMappings["RetainedEarnings"]`.

3. **Expense transfer**: For each Account with Nature=5 (Expense):
   - Calculate net balance = SUM(Debit) − SUM(Credit).
   - Create closing entry: Dr RetainedEarnings, Cr ExpenseAccount (zero it).

4. **Lock the year**: Set `IsClosed = true`.

5. **Opening entry for new year**: Create an opening entry with EntryType=Adjustment:
   - For each Asset account (Nature=1) with non-zero balance: Dr NewYearAssetAccount with opening balance.
   - For each Liability account (Nature=2) with non-zero balance: Cr NewYearLiabilityAccount with opening balance.
   - For each Equity account (Nature=3) with non-zero balance: carry forward.
   - The balancing line is the `RetainedEarnings` account.

6. All closing entries are themselves JournalEntries with `EntryType=Adjustment` and `ReferenceType="AnnualClosing"`.

### 6.3 Post-Close Restrictions

- No new entries can be created with dates in a closed fiscal year.
- Reports can still query closed year data (read-only).
- Reopening a closed year requires Admin intervention (out of scope for V1 — would require reversing the closing entries).

---

## 7. Integration with Other Modules

| Module | Integration Point |
|--------|------------------|
| **Phase 22 — Chart of Accounts** | JournalEntryLine.AccountId FK → Accounts. Leaf accounts (IsLeaf=true) accept entries. AccountCategories group accounts. |
| **Phase 24 — Accounting Engine** | All auto-generated entries use the same JournalEntry/JournalEntryLine tables. `ReferenceType`/`ReferenceId` links back to source documents. |
| **Phase 29 — Receipts & Payments** | Payments create automatic entries via AccountingEngine. Manual payment vouchers (ReceiptVouchers, PaymentVouchers) each generate one journal entry on Post. |
| **Phase 25 — Products / Phase 26 — Warehouses** | Inventory adjustments (stock count differences, damage write-offs) create Inventory-type journal entries (EntryType=6). |
| **Phase 19 — Settings** | SystemSettings.CostingMethod influences COGS calculation for auto-generated entries. |
| **Phase 20 — Currencies** | All entries are in base currency. Multi-currency support is via CurrencyRates conversion at entry creation time. |
| **Phase 31 — Reports** | Journal entries feed the Income Statement (revenue/expense accounts) and Balance Sheet (asset/liability/equity accounts). |

---

## 8. Implementation Tasks

### Task 1: Domain Entities
- Create `AccountCategory` entity (smallint PK, Name, Description, IsActive, audit fields).
- Create `FiscalYear` entity (YearName, StartDate, EndDate, IsClosed, audit fields).
- Create `JournalEntry` entity (EntryNo, EntryDate, EntryType, ReferenceType?, ReferenceId?, Description?, Status, IsReversed, ReversedByEntryId? self-ref).
- Create `JournalEntryLine` entity (JournalEntryId FK, AccountId FK, Debit, Credit, Description?, SortOrder).
- Create `Expense` entity (ExpenseNo, ExpenseDate, ExpenseAccountId FK, CashBoxId FK, CurrencyId FK, Amount, Notes?, Status, audit fields).
- Create `SystemAccountMapping` entity (MappingKey, AccountId FK, BranchId? FK, audit fields).
- Add guard clauses: negative amounts, balanced entry validation, status transitions.
- Add domain methods: `Post()`, `Cancel()`, `AddLine()`, `RemoveLine()`, `UpdateLine()`.

### Task 2: EF Core Fluent Configurations
- Configure `AccountCategoryConfiguration` (smallint PK, MaxLength on Name/Description).
- Configure `FiscalYearConfiguration` (CHK `EndDate > StartDate`, unique YearName index).
- Configure `JournalEntryConfiguration` (EntryNo unique index per year, composite index on ReferenceType+ReferenceId, Restrict on self-ref ReversedByEntryId FK, HasConversion for EntryType/Status enums).
- Configure `JournalEntryLineConfiguration` (CHK_DebitOrCredit, CHK_NoNegativeValues, Restrict on both FKs, HasOne navigation for JournalEntry and Account).
- Configure `ExpenseConfiguration` (unique ExpenseNo, Restrict on all FKs, HasConversion for Status).
- Configure `SystemAccountMappingConfiguration` (unique on (MappingKey, BranchId), Restrict on AccountId FK).
- Add global query filters for IsActive where applicable.
- All FKs use `DeleteBehavior.Restrict`.

### Task 3: Database Migrations
- Create initial migration for FiscalYears, AccountCategories, JournalEntries, JournalEntryLines, Expenses, SystemAccountMappings.
- Add composite index on JournalEntry(ReferenceType, ReferenceId).
- Add CHECK constraints for debit/credit rules and date validity.
- Validate migration generates correct SQL.

### Task 4: DTOs and Requests
- `FiscalYearDto`, `CreateFiscalYearRequest`, `CloseFiscalYearRequest`.
- `JournalEntryDto` (with lines list), `JournalEntryLineDto`.
- `CreateManualJournalEntryRequest` (with list of line requests).
- `CancelJournalEntryRequest`.
- `JournalEntrySearchRequest` (filter by date range, EntryType, Status, keyword).
- `ExpenseDto`, `CreateExpenseRequest`, `UpdateExpenseRequest`.
- `SystemAccountMappingDto`, `UpdateMappingRequest`.

### Task 5: FluentValidators
- `CreateManualJournalEntryRequestValidator`: at least 2 lines, balanced totals, no mixed Debit+Credit on one line, valid date, open fiscal year.
- `JournalEntryLineValidator`: Debit >= 0, Credit >= 0, AccountId required, MaxLength on description.
- `CreateFiscalYearRequestValidator`: StartDate before EndDate, year name format, no overlap with existing open years.
- `CloseFiscalYearRequestValidator`: year not already closed, no unposted entries.
- `CreateExpenseRequestValidator`: Amount > 0, valid date, CashBoxId required, ExpenseAccountId required.
- `UpdateMappingRequestValidator`: MappingKey not empty, AccountId required.

### Task 6: Application Services
- `IFiscalYearService` / `FiscalYearService`:
  - `GetAllAsync()`, `GetByIdAsync()`, `GetCurrentAsync()` (the active open year).
  - `CreateAsync(CreateFiscalYearRequest)`, `CloseAsync(int id)`.
  - `CloseAsync` executes annual closing inside `ExecuteTransactionAsync`.
- `IJournalEntryService` / `JournalEntryService`:
  - `GetByIdAsync(int id)` with lines included.
  - `SearchAsync(JournalEntrySearchRequest)` with filtering + pagination.
  - `CreateManualAsync(CreateManualJournalEntryRequest)` — validates balance, sets EntryNo from DocumentSequence, saves as Draft or Posted.
  - `PostAsync(int id)` — transitions Draft→Posted, validates fiscal year open.
  - `CancelAsync(int id)` — creates reversal entry, sets IsReversed on original.
  - `GetLinesAsync(int entryId)`.
- `IExpenseService` / `ExpenseService`:
  - `CreateAsync(CreateExpenseRequest)` — saves expense, if Posted creates auto journal entry.
  - `PostAsync(int id)`, `CancelAsync(int id)` — Cancel creates reversal entry.
- `ISystemAccountMappingService` / `SystemAccountMappingService`:
  - `GetAllAsync()`, `GetByKeyAsync(string key)`.
  - `UpdateMappingAsync(int id, UpdateMappingRequest)`.
  - `ResolveAccountAsync(string mappingKey, int? branchId)` — lookup method for AccountingEngine.

### Task 7: API Controllers
- `FiscalYearsController`:
  - `GET /api/v1/fiscal-years` — list all (AdminOnly).
  - `GET /api/v1/fiscal-years/current` — get active year (AllStaff).
  - `POST /api/v1/fiscal-years` — create (AdminOnly).
  - `POST /api/v1/fiscal-years/{id}/close` — annual closing (AdminOnly).
- `JournalEntriesController`:
  - `GET /api/v1/journal-entries` — search with filters (ManagerAndAbove).
  - `GET /api/v1/journal-entries/{id}` — with lines (ManagerAndAbove).
  - `POST /api/v1/journal-entries` — create manual entry (ManagerAndAbove).
  - `POST /api/v1/journal-entries/{id}/post` — post draft (ManagerAndAbove).
  - `POST /api/v1/journal-entries/{id}/cancel` — cancel with reversal (ManagerAndAbove).
- `ExpensesController`:
  - `GET /api/v1/expenses` — list (AllStaff).
  - `GET /api/v1/expenses/{id}` — detail (AllStaff).
  - `POST /api/v1/expenses` — create (ManagerAndAbove).
  - `POST /api/v1/expenses/{id}/post` — post with auto entry (ManagerAndAbove).
  - `POST /api/v1/expenses/{id}/cancel` — cancel with reversal (ManagerAndAbove).
- `SystemAccountMappingsController`:
  - `GET /api/v1/system-account-mappings` — list all (AdminOnly).
  - `PUT /api/v1/system-account-mappings/{id}` — update mapping (AdminOnly).
- JWT `CreatedByUserId` extraction in all POST endpoints (not client-supplied).
- HTTP 404 for not-found, 400 for validation, 200 for success.

### Task 8: Desktop — Journal Entry List View
- `JournalEntriesListViewModel` implementing `IDisposable` with EventBus subscription.
- DataGrid columns: EntryNo, EntryDate, EntryType (Arabic display), Description, TotalDebit, TotalCredit, Status.
- Filter controls: date range, EntryType dropdown, Status dropdown, text search.
- Buttons: "قيد يدوي جديد" (opens editor), "عرض" (opens detail), "ترحيل" (post), "إلغاء" (cancel with confirmation).
- Sorting by EntryDate DESC (newest first).
- `IToastNotificationService` for minor success messages, `IDialogService` for confirmations.

### Task 9: Desktop — Journal Entry Editor View
- `JournalEntryEditorViewModel` implementing `INotifyDataErrorInfo`.
- Header section: EntryDate date picker, Description text box, EntryType read-only display.
- Lines DataGrid: AccountId searchable combo, Debit numeric, Credit numeric, line description.
- Summary bar: SUM(Debit), SUM(Credit), balance difference (with green/red indicator).
- Save as Draft / Post buttons (always enabled — validate on click).
- Non-modal via `ScreenWindowService.OpenScreen()`.

### Task 10: Desktop — Fiscal Year Management View
- `FiscalYearsViewModel` (AdminOnly base).
- List with YearName, StartDate, EndDate, IsClosed indicator.
- "إضافة سنة مالية" button, "إغلاق السنة" button (with confirmation showing warning).
- Annual closing wizard: summary of revenue/expense balances to transfer.

### Task 11: Desktop — Expenses List & Editor
- `ExpensesListViewModel`: DataGrid with ExpenseNo, ExpenseDate, ExpenseAccount, CashBox, Amount, Status.
- Filter by date range, account, status.
- `ExpenseEditorViewModel`: date, account combo (Nature=5 expense accounts only), cash box combo, currency combo, amount, notes.
- On Post: auto-journal entry creation via `ExpenseService.PostAsync()`.
- On Cancel: reversal entry confirmation.

### Task 12: Desktop — System Account Mappings View
- `SystemAccountMappingsViewModel` (AdminOnly).
- DataGrid: MappingKey, AccountName, AccountCode, Branch override.
- Inline editing or dialog for changing account mapping.
- Explanation tooltips for each MappingKey describing its purpose.

### Task 13: Integration Testing
- Manual entry: create draft → edit → post → cancel (verify reversal created with swapped Dr/Cr).
- Fiscal year: create → close (verify revenue/expense transferred to retained earnings) → verify new entries blocked.
- Expense: create → post (verify auto-entry: Dr Expense, Cr CashBox) → cancel (verify reversal).
- SystemAccountMappings: update mapping → verify AccountingEngine uses new account.
- Edge cases: unbalanced entry rejected, entry in closed year rejected, multi-line entry with 10+ lines.

---

## 9. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Unbalanced entries corrupting ledger | CRITICAL | Service-level `SUM(Debit) == SUM(Credit)` check before any save. DB-level CHECK constraints on lines. Both layers must pass. |
| Accidental fiscal year close with unposted entries | HIGH | Guard clause rejecting close if any Draft entries exist in the year. Confirmation dialog with explicit warning listing pending entries. |
| Reversal chain complexity | MEDIUM | Simple design: each reversal is a full new entry with Dr↔Cr swapped. The `ReversedByEntryId` creates a bidirectional link. No cascading reversal logic needed. |
| Performance on large line counts | LOW | Journal entries rarely exceed 20–30 lines. No pagination needed for a single entry's lines. |
| SystemAccountMapping key drift | MEDIUM | MappingKey strings are seeded constants referenced by the service. Any code change adding a new key must be paired with a seed migration. No dynamic key creation in V1. |
| Multi-user concurrent EntryNo generation | LOW | `DocumentSequenceService` uses `SemaphoreSlim` (thread-safe) + DB transaction for atomic `GetNextIntAsync()`. |
| Fiscal year overlap | MEDIUM | Validator checks no two open years have overlapping date ranges. Overlap detection: `(StartDate <= newEnd AND EndDate >= newStart) AND IsClosed = false`. |
| Loss of audit trail on cancelled entries | LOW | Cancelled entries remain visible (`Status=Cancelled`). The reversal entry is linked via `ReversedByEntryId`. Both are queryable. |

---

## 10. Open Questions

1. **Void vs Cancel**: Should there be a "Void" for entries created in error before posting (same as deleting a Draft), or only Cancel? Decision: Draft entries can be deleted (hard-delete with guard). Posted entries must be cancelled. This aligns with the invoice lifecycle.

2. **Fiscal year auto-creation**: Should the system auto-create a new fiscal year when the current one is closed (default `StartDate = EndDate + 1 day`)? Decision: Yes — the closing process prompts the admin to create the next year, or auto-creates with default name and dates if the admin confirms.

3. **Multi-currency entries**: In V1, all journal entries are in the base currency. Multi-currency support (displaying both transaction currency and base currency amounts per line) is deferred to V2. Exchange rate at entry time is stored on the entry header if needed.

4. **Reversing a reversal**: Should reversing an already-reversed entry be allowed? Decision: No — the original entry's `IsReversed` flag prevents double-reversal. The service checks this guard before cancelling.
