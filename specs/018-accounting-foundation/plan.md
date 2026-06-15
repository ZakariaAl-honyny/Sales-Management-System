# Implementation Plan: Accounting Foundation (Phase 18)

**Branch**: `018-accounting-foundation` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification, database schema (Module 4), global analysis

---

## Summary

The Accounting Foundation phase builds the central financial backbone of the Sales Management System. It implements five database tables: AccountCategories (تصنيفات الحسابات), Accounts (دليل الحسابات), JournalEntries (القيود اليومية), JournalEntryLines (تفاصيل القيود), and SystemAccountMappings (ربط حسابات النظام). These tables form the double-entry bookkeeping engine that all financial operations — sales, purchases, payments, receipts, inventory adjustments — ultimately write to. The phase enforces strict accounting integrity: every journal entry must be balanced (total debit equals total credit), system accounts are protected from modification, and leaf-level accounts alone permit transaction posting. AccountCategories provide logical grouping of accounts (e.g., "أصول متداولة", "مطلوبات متداولة") for reporting and filtering. SystemAccountMappings use a flexible key-value design, replacing the old fixed-column approach, so that new system mappings can be added without schema changes.

---

## Scope

**In Scope:**
- AccountCategories — logical grouping entity for Chart of Accounts
- Accounts — hierarchical chart of accounts with self-referencing parent, nature (Asset/Liability/Equity/Revenue/Expense), leaf flag, and system protection
- JournalEntries — double-entry document with status lifecycle (Draft→Posted→Cancelled), reversal tracking, and reference to source documents
- JournalEntryLines — individual debit/credit lines with CHECK constraints enforcing single-sided entry and non-negative values
- SystemAccountMappings — flexible key-value mapping from business operation names to Account IDs
- Seeding: default account categories and a standard Chart of Accounts with protected system accounts
- Domain validation: balance validation, system account guard, leaf-only transaction guard
- Application services: journal entry creation, account balance query, account ledger/statement query, system mapping lookups
- Infrastructure: EF Core Fluent API configurations with DeleteBehavior.Restrict on all FKs, decimal(18,2) precision, CHECK constraints, filtered unique indexes

**Out of Scope:**
- Desktop UI for Chart of Accounts management (separate phase)
- Desktop UI for manual Journal Entry creation (separate phase)
- Advanced financial reports (Balance Sheet, Income Statement — Phase 31)
- ReceiptVouchers, PaymentVouchers, Expenses, CashBoxes, Banks (later phases)
- Multi-currency in journal entries (added in Phase 30)
- FiscalYear entity and Annual Closing (Phase 30)

---

## Key Entities

### 4.1 AccountCategories (تصنيفات الحسابات)
A lookup table using `smallint` PK that groups accounts by logical category such as "أصول متداولة" (Current Assets) or "مطلوبات متداولة" (Current Liabilities). Each category has a name (nvarchar 100) and optional description (nvarchar 300). Categories follow soft-delete via IsActive flag and inherit the standard audit columns (CreatedByUserId, CreatedAt, etc.). An account may optionally belong to one category via `CategoryId` FK.

### 4.2 Accounts (دليل الحسابات)
The Chart of Accounts is a self-referencing hierarchy (`ParentId` nullable FK to itself). Each account has:
- **AccountCode** (nvarchar 20) — unique across active accounts via a filtered unique index `WHERE IsActive = 1`
- **Name** (nvarchar 200) — Arabic display name
- **Nature** (tinyint) — determines the account's normal balance side using the `AccountNature` enum: Asset=1 (normal debit), Liability=2 (normal credit), Equity=3 (normal credit), Revenue=4 (normal credit), Expense=5 (normal debit)
- **IsLeaf** (bit, default 1) — leaf accounts permit transaction posting; parent (non-leaf) accounts are for grouping and reporting only
- **IsSystem** (bit, default 0) — system accounts (seeded as Level 1 and Level 2 accounts) are protected from modification or deletion
- **CategoryId** (smallint, nullable FK to AccountCategories) — optional grouping

The hierarchical structure supports multi-level nesting (up to 10 levels in practice, though no strict depth limit is enforced by the schema). The `IsLeaf` flag combined with `ParentId` ensures that journal entry lines can only post to leaf-level accounts.

### 4.5 JournalEntries (القيود اليومية)
The core financial document. Each entry has:
- **EntryNo** (int, unique per fiscal year) — the user-facing journal entry number, generated via DocumentSequenceService
- **EntryDate** (date) — the accounting date of the entry
- **EntryType** (tinyint) — identifies the source using `JournalEntryType` enum: Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7
- **ReferenceType** (nvarchar 50) and **ReferenceId** (int) — link the entry to its source document (e.g., "SalesInvoice" + InvoiceId) with a composite index for efficient lookups
- **Status** (tinyint) — lifecycle: Draft=1 → Posted=2 → Cancelled=3. Posted entries are immutable. Errors are corrected via a reversing entry that links back via `ReversedByEntryId`
- **IsReversed** (bit) — marks this entry as a reversal of another entry
- **ReversedByEntryId** (int, nullable self-FK with Restrict) — when an entry is reversed, the new reversal entry points back here

The status lifecycle mirrors the invoice lifecycle (Draft→Posted→Cancelled). A posted entry can only be "removed" by creating a reversal entry that mirrors the original amounts with debits and credits swapped.

### 4.6 JournalEntryLines (تفاصيل القيود)
The individual lines that make up a journal entry. Each line belongs to exactly one JournalEntry and references exactly one Account. Key constraints:
- **Debit** and **Credit** (both decimal(18,2), default 0) — a line must have exactly one non-zero side. The CHECK constraint `CHK_DebitOrCredit` enforces that `(Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0) OR (Debit = 0 AND Credit = 0)`. The `CHK_NoNegativeValues` constraint ensures `Debit >= 0 AND Credit >= 0`.
- **SortOrder** (smallint) — preserves the original line ordering for display and audit
- **Description** (nvarchar 300, nullable) — per-line explanation

Each journal entry must have at least two lines (one debit, one credit) and the sum of all debits must equal the sum of all credits.

### 4.10 SystemAccountMappings (ربط حسابات النظام)
This table replaces the old fixed-column approach (which had columns like `DefaultCashAccountId`, `SalesRevenueAccountId`, etc.) with a flexible key-value design. Each row has:
- **MappingKey** (nvarchar 100) — a unique string identifier such as `"SalesRevenue"`, `"COGS"`, `"VatOutput"`, `"DefaultCash"`, `"AccountsReceivable"`, `"AccountsPayable"`, `"InventoryAsset"`, `"VatInput"`, `"Capital"`, `"SalesReturn"`, `"GeneralExpense"`, `"PurchaseReturn"`
- **AccountId** (int FK to Accounts) — the account to use for this operation
- **BranchId** (smallint, nullable FK to Branches) — optional branch-specific override; when null, the mapping is the global default for all branches

This design allows adding new system mappings without schema migrations, and supports branch-specific account overrides (e.g., different cash accounts per branch).

---

## Enums (Reference Tables)

| Enum | Values |
|------|--------|
| **AccountNature** | Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5 |
| **JournalEntryType** | Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7 |
| **InvoiceStatus** (used by JournalEntries.Status) | Draft=1, Posted=2, Cancelled=3 |

AccountNature determines normal balance direction: Assets and Expenses have normal debit balances (increase by debit, decrease by credit). Liabilities, Equity, and Revenue have normal credit balances (increase by credit, decrease by debit). Account balance calculation uses this direction to determine whether the net balance is debit-normal (positive) or credit-normal (negative in debit terms).

---

## Business Rules

1. **Strict Double-Entry**: Every journal entry must have at least two lines. Total debit must equal total credit within a tolerance of 0.001 (to handle micro-fractional rounding). The system validates this at two levels: in the Domain entity (`ValidateAndPost()`) and in the Application-layer FluentValidation validator. This defense-in-depth ensures no unbalanced entry can persist.

2. **Single-Sided Lines**: Each journal entry line must have exactly one non-zero side. A line cannot have both debit and credit greater than zero simultaneously, nor can either side be negative. The database enforces this with CHECK constraints `CHK_DebitOrCredit` and `CHK_NoNegativeValues`.

3. **Posting Immutability**: Once a journal entry is posted (Status=2), it cannot be modified or deleted. Corrections must be made by creating a new entry that reverses the original (swapping all debits and credits). The reversal entry links back via `ReversedByEntryId`.

4. **Leaf-Only Posting**: Journal entry lines can only reference accounts where `IsLeaf = true`. Parent accounts (non-leaf) are for hierarchy and reporting only. This prevents posting to grouping-level accounts.

5. **System Account Protection**: Accounts marked `IsSystem = true` cannot be modified (Name, Nature, AccountCode changes rejected) or soft-deleted. Any attempt to do so throws a `DomainException` with an Arabic error message.

6. **Account Code Uniqueness**: The `AccountCode` column has a filtered unique index `WHERE IsActive = 1` — only active accounts must have unique codes. A soft-deleted account's code can be reused.

7. **Self-Referencing FK with Restrict**: The `ParentId` self-referencing FK uses `DeleteBehavior.Restrict`. An account cannot be deleted if any child accounts reference it.

8. **Reference Tracking**: Journal entries from automated processes (Sales, Purchase, Receipt, Payment, Inventory, Adjustment) carry `ReferenceType` and `ReferenceId` to trace back to the source document. A composite index on `(ReferenceType, ReferenceId)` enables efficient reverse lookups.

9. **EntryNo Uniqueness**: The `EntryNo` is unique across all journal entries (not per fiscal year in V1; fiscal year scoping is added in Phase 30). Generated via `DocumentSequenceService.GetNextIntAsync("JournalEntry")` using a thread-safe `SemaphoreSlim` lock.

10. **SystemAccountMappings Flexibility**: The key-value design allows the system to look up accounts dynamically at runtime (e.g., `GetAccountId("SalesRevenue")` returns the current Sales Revenue account). If a required mapping is missing, the system prevents the corresponding financial operation from proceeding.

---

## Design Decisions

### Decision 1: Flexible Key-Value Mappings over Fixed Columns
The old design had `SystemAccountMappings` with 13 fixed columns (one per mapped account). The new design uses a key-value pattern: one `MappingKey` column and one `AccountId` column per row, with 13+ rows. This eliminates schema migrations when new system accounts are needed and allows branch-specific overrides via the optional `BranchId` field. The application layer caches these mappings for performance.

### Decision 2: Int EntryNo over String EntryNumber
The journal entry number is stored as `int` (not `nvarchar`) and formatted for display as `"JE-YYYYMMDD-XXXX"` at the UI/reporting layer. This matches the invoice numbering strategy (`InvoiceNo` as `int`) and enables efficient range queries, sorting, and sequence generation.

### Decision 3: IsLeaf over Computed Balance Direction
Instead of computing `IsDebitNormal()` from AccountType, the schema stores `Nature` (the account nature enum) and `IsLeaf` (whether posting is permitted). Account balance calculation uses `Nature` to determine the normal balance side. `IsLeaf` is a separate concern: a parent account could theoretically be set to allow transactions (IsLeaf=true) but the system only seeds leaf accounts as posting-enabled.

### Decision 4: No Account Snapshots on JournalEntryLine
Unlike the previous design, `JournalEntryLine` does NOT store `AccountCode` or `AccountName` as denormalized snapshots. The rationale is that account names change rarely, and the audit trail requirement is satisfied by the `ProductPriceHistory` equivalent for accounts (to be added in a later phase if needed). The `AccountId` FK with `DeleteBehavior.Restrict` ensures referential integrity.

### Decision 5: Status-Based Lifecycle over IsPosted Bool
Journal entries use a `Status` tinyint (Draft=1, Posted=2, Cancelled=3) instead of a simple `IsPosted` boolean. This mirrors the invoice lifecycle exactly and supports Draft (saved but not yet posted) entries, which are useful for complex manual entries that are prepared in stages. Posted entries transition to Cancelled via a reversal entry — they are never directly modified.

### Decision 6: AccountCategories as Separate Lookup
AccountCategories is a separate smallint-PK table rather than an enum or a simple string column on Accounts. This allows the admin to add, rename, or reorganize categories without code changes, and provides a foundation for category-based filtering in reports.

### Decision 7: Two-Layer Validation (Domain + FluentValidation)
Balance validation (total debit == total credit) is enforced at two layers: Domain entity's `ValidateAndPost()` method (defense against programmatic bypass) and FluentValidation `CreateJournalEntryRequestValidator` (catches user errors before a database round-trip). The Domain validation uses a tolerance of 0.001 to handle micro-fractional rounding.

---

## Implementation Tasks (High-Level, No Code)

### Phase 1: Domain Layer — Enums
- Define `AccountNature` enum: Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5
- Define `JournalEntryType` enum: Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7

### Phase 2: Domain Layer — Entities
- Create `AccountCategory` entity: smallint PK, Name, Description, IsActive, audit fields
- Create `Account` entity: int PK, self-referencing ParentId FK, AccountCode, Name, Nature, IsLeaf, IsSystem, CategoryId FK, IsActive, audit. Factory method `Create()` with guard clauses. Methods: `Update()` (guards IsSystem), `MarkAsDeleted()` (guards IsSystem + children check).
- Create `JournalEntry` entity: int PK, EntryNo, EntryDate, EntryType, ReferenceType/Id, Description, Status, IsReversed, ReversedByEntryId (self-FK), audit. Methods: `AddLine()`, `ValidateAndPost()` (checks balance, sets Status=Posted), `Reverse()` (creates reversal entry).
- Create `JournalEntryLine` entity: JournalEntryId FK, AccountId FK, Debit, Credit, Description, SortOrder. Internal constructors, created only through `JournalEntry.AddLine()`.
- Create `SystemAccountMapping` entity: MappingKey, AccountId FK, BranchId FK (nullable). Methods: static lookup helpers.

### Phase 3: Infrastructure — EF Core Configurations
- `AccountCategoryConfiguration`: smallint PK, nvarchar max lengths, IsActive query filter
- `AccountConfiguration`: unique filtered index on AccountCode + IsActive, self-referencing FK with Restrict, nature conversion to int, IsLeaf default 1, IsSystem default 0, decimal precision for balance fields
- `JournalEntryConfiguration`: int PK, composite index on (ReferenceType, ReferenceId), self-referencing ReversedByEntryId FK with Restrict, EntryNo unique index, status conversion to int
- `JournalEntryLineConfiguration`: composite FK to JournalEntry with Restrict (not Cascade per the new schema), FK to Account with Restrict, CHECK constraints CHK_DebitOrCredit and CHK_NoNegativeValues, decimal(18,2) precision on Debit/Credit
- `SystemAccountMappingConfiguration`: unique index on (MappingKey, BranchId) with null BranchId handling
- Register all configurations in `DbContext.OnModelCreating`

### Phase 4: Infrastructure — Seeder
- `AccountingSeeder`: two-pass approach (Level 1 categories → Save → query IDs → Level 2 accounts → Save → ...). Seed ~20 default accounts across all 5 natures, with system accounts protected. Seed default AccountCategories. Seed 13+ SystemAccountMapping rows for core operations.

### Phase 5: Application — Services
- `IJournalEntryService` / `JournalEntryService`: CreateJournalEntryAsync (validates balance, generates EntryNo, saves), CancelJournalEntryAsync (creates reversal), GetAccountBalanceAsync (sums posted lines per account, applies nature direction), GetAccountLedgerAsync (opening balance + period lines + running balance). All methods return `Result<T>`.
- `ISystemAccountService` / `SystemAccountService`: GetMappingAsync(mappingKey, branchId?) returns the AccountId for the given operation. Caches results for performance.
- `IJournalEntryNumberGenerator` / `JournalEntryNumberGenerator`: generates int EntryNo via DocumentSequenceService.

### Phase 6: Application — Validation
- `CreateJournalEntryRequestValidator`: FluentValidation rules — at least one line, balance == 0 within tolerance, all AccountIds exist and are leaf accounts, no negative amounts, EntryDate not in future (unless configured otherwise).

### Phase 7: API — Controller
- `JournalEntriesController`: standard CRUD endpoints with Result translation (404 for not found, 400 for validation errors, 200 for success). GET endpoints for balance and ledger queries. POST for create (extracts userId from JWT, never from client). POST for cancel/reverse.
- `SystemAccountMappingsController`: read-only in V1 (mappings managed via seeder/admin screen).

### Phase 8: Testing
- Domain tests: balanced entry passes, unbalanced entry throws, system account modification throws, reversal entry creation
- Application tests: service methods, validator rules
- Infrastructure tests: migration generates correct schema, seed data has correct counts

---

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Unbalanced entries due to rounding | Medium | Domain validation tolerance of 0.001; FluentValidation pre-check; decimal(18,2) precision avoids floating-point issues |
| Performance of account balance queries on large datasets | Medium | Composite indexes on (AccountId, Status) on JournalEntryLines; AsNoTracking for read queries; caching for SystemAccountMappings |
| Data loss from improper reversal flows | High | Reversal entries require explicit creation via CancelJournalEntryAsync; original entries are immutable once posted; FK to reversed entry is tracked |
| SystemAccountMappings key mismatch between code and database | Medium | All mapping keys defined as constants in Domain; seeder guarantees presence; missing key check returns Result.Failure before any operation |
| Concurrent EntryNo generation duplicates | Low | DocumentSequenceService uses SemaphoreSlim (thread-safe) — same pattern as Sales/Purchase InvoiceNo generation |
| Schema migration conflicts with existing data | Medium | Accounting is foundational — no existing accounting data to migrate; clean migration in V1 |
