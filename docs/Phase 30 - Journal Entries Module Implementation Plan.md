# Phase 30 — Journal Entries Module: Comprehensive Enhancement & Implementation Plan

> **Version**: 1.0 — Full rewrite based on 3 analysis documents + codebase audit
> **Scope**: Complete Journal Entry system enhancement with 4 sub-modules, covering manual/auto entries, opening/closing, and accounting reports

---

## Table of Contents
1. [Architecture — 4 Sub-Modules](#1-architecture--4-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog](#4-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 4 Sub-Modules

Based on full codebase audit + user requirements from analysis documents, the Journal Entry system for V1 enhancement is divided into **4 sub-modules**:

| # | Sub-Module | Type | Current State | Target |
|---|------------|------|---------------|--------|
| 📝 | **Manual Entries** | Enhancement | Basic manual entry exists | Add currency, attachments, per-line description, reversal, approval workflow |
| 🤖 | **Auto-Entries** | Enhancement | Basic auto-posting from sales/purchases exists | Extend to ALL modules (payments, cash, returns, transfers) |
| 🔓 | **Opening/Closing** | NEW | Only `OpeningBalance` enum value exists | Complete opening entry workflow + annual closing procedure |
| 📊 | **Reports** | NEW | Account balance + ledger exist | Trial balance, detailed trial balance, enhanced account statement |

### 1.1 Data Flow

```text
Manual Entry (UI)  ─→  JournalEntryService  ─→  Domain Entity (balance check)
                          ↓
Auto Entries (Sales, Purchases, Payments, Cash, Returns, Transfers)
                          ↓
                    IUnitOfWork / Transaction
                          ↓
                    JournalEntries table
                          ↓
                    Reports: TrialBalance, AccountStatement, Ledger
```

### 1.2 Entry Lifecycle

```text
Draft (1) → Posted (2) → Reversed (3)

ALLOWED:
  Draft    → Posted      ✅ (Validate balance + accounts, then post)
  Draft    → Reversed    ✅ (Direct reversal without posting)
  Posted   → Reversed    ✅ (Offsetting entry with opposite debits/credits)

FORBIDDEN:
  Reversed → anything    ❌ NEVER (terminal state)
  Editing a Posted entry ❌ NEVER (reverse + create new instead)
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Layer ✅ (Partially exists)

**Entity**: `SalesSystem.Domain.Accounting.Entities.JournalEntry`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` | ✅ Exists |
| `EntryNumber` | `string(50)` | ✅ Exists — unique index |
| `TransactionDate` | `DateTime` | ✅ Exists |
| `Description` | `string(500)?` | ✅ Exists |
| `EntryType` | `JournalEntryType` (byte enum) | ✅ Exists |
| `ReferenceType` | `string(50)?` | ✅ Exists |
| `ReferenceId` | `int?` | ✅ Exists |
| `ReferenceNumber` | `string(50)?` | ✅ Exists |
| `IsPosted` | `bool` | ✅ Exists |
| `IsReversed` | `bool` | ✅ Exists |
| `PostedBy` | `int?` | ✅ Exists |
| `PostedAt` | `DateTime?` | ✅ Exists |
| **`CurrencyId`** | `int?` | ❌ **MISSING** |
| **`ExchangeRate`** | `decimal(18,6)?` | ❌ **MISSING** |
| **`AttachmentPath`** | `string(255)?` | ❌ **MISSING** |
| **`AttachmentFileName`** | `string(100)?` | ❌ **MISSING** |
| **`IsDraft`** | `bool` | ❌ **MISSING** — current design bypasses Draft state |
| **`ReversedById`** | `int?` | ❌ **MISSING** |
| **`ReversedAt`** | `DateTime?` | ❌ **MISSING** |
| **`ReversalReason`** | `string(500)?` | ❌ **MISSING** |

**Entity**: `SalesSystem.Domain.Accounting.Entities.JournalEntryLine`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` | ✅ Exists |
| `JournalEntryId` | `int FK` | ✅ Exists |
| `AccountId` | `int` | ✅ Exists |
| `AccountCode` | `string` | ✅ Exists |
| `AccountNameAr` | `string` | ✅ Exists |
| `Debit` | `decimal` | ✅ Exists |
| `Credit` | `decimal` | ✅ Exists |
| `Description` | `string?` | ✅ Exists — per-line description already present |

**Enum**: `SalesSystem.Domain.Accounting.Enums.JournalEntryType`

| Value | Name | Status |
|-------|------|--------|
| `1` | Sales | ✅ Exists |
| `2` | SalesReturn | ✅ Exists |
| `3` | Purchase | ✅ Exists |
| `4` | PurchaseReturn | ✅ Exists |
| `5` | Expense | ✅ Exists |
| `6` | StockWriteOff | ✅ Exists |
| `7` | Transfer | ✅ Exists |
| `8` | Manual | ✅ Exists |
| `9` | OpeningBalance | ✅ Exists |
| **`10`** | **AnnualClosing** | ❌ **MISSING — NEW** |
| **`11`** | **Reversal** | ❌ **MISSING — NEW** |

### 2.2 Infrastructure Layer ✅ (Partially exists)

**Configuration**: `JournalEntryConfiguration`

| Element | Status |
|---------|--------|
| `ToTable("JournalEntries")` | ✅ Exists |
| `HasKey` / `Property` configs | ✅ Exists for current fields |
| `HasIndex(EntryNumber).IsUnique()` | ✅ Exists |
| `HasIndex(TransactionDate)` | ✅ Exists |
| `HasMany(Lines).WithOne().OnDelete(Cascade)` | ✅ Exists — **acceptable** since lines are child of entry |
| `HasQueryFilter(IsActive)` | ✅ Exists |
| CurrencyId + ExchangeRate config | ❌ **MISSING** |
| Attachment fields config | ❌ **MISSING** |
| Draft/Reversal fields config | ❌ **MISSING** |

**Configuration**: `JournalEntryLineConfiguration`

| Element | Status |
|---------|--------|
| `ToTable("JournalEntryLines")` | ❌ **MISSING** — currently no explicit config |
| Account FK with `DeleteBehavior.Restrict` | ❌ **MISSING** |
| `Debit`/`Credit` precision `(18,2)` | ❌ **MISSING** |

### 2.3 Application Layer ✅ (Partially exists)

**Service**: `JournalEntryService`

| Method | Status |
|--------|--------|
| `CreateJournalEntryAsync(CreateJournalEntryRequest)` | ✅ Exists — creates + posts in one call |
| `GetAccountBalanceAsync(accountId, asOfDate)` | ✅ Exists |
| `GetAccountLedgerAsync(accountId, startDate, endDate)` | ✅ Exists |
| `SaveDraftAsync()` | ❌ **MISSING** |
| `PostEntryAsync()` | ❌ **MISSING** |
| `ReverseEntryAsync()` | ❌ **MISSING** |
| `GetTrialBalanceAsync()` | ❌ **MISSING** |
| `GetDetailedTrialBalanceAsync()` | ❌ **MISSING** |
| `CreateOpeningEntryAsync()` | ❌ **MISSING** |
| `CreateAnnualClosingAsync()` | ❌ **MISSING** |
| `GetAllAsync()` / `GetByIdAsync()` | ❌ **MISSING** |

**Interface**: `IJournalEntryService`

| Method | Status |
|--------|--------|
| `CreateJournalEntryAsync` | ✅ Exists |
| `GetAccountBalanceAsync` | ✅ Exists |
| `GetAccountLedgerAsync` | ✅ Exists |
| All missing methods above | ❌ **MISSING** |

**Number Generator**: `JournalEntryNumberGenerator` — ✅ Exists

### 2.4 Contracts Layer ✅ (Partially exists)

**Requests**:

| Request | Status |
|---------|--------|
| `CreateJournalEntryRequest` | ✅ Exists — missing `CurrencyId`, `ExchangeRate`, `AttachmentPath` |
| `JournalEntryLineRequest` | ✅ Exists |

**DTOs**:

| DTO | Status |
|-----|--------|
| `AccountBalanceDto` | ✅ Exists |
| `AccountLedgerDto` | ✅ Exists |
| `AccountLedgerLineDto` | ✅ Exists |
| `JournalEntryDto` | ❌ **MISSING** |
| `JournalEntryLineDto` | ❌ **MISSING** |
| `TrialBalanceDto` | ❌ **MISSING** |
| `TrialBalanceLineDto` | ❌ **MISSING** |

### 2.5 API Layer ❌ (Missing entirely)

| Component | Status |
|-----------|--------|
| `JournalEntriesController` | ❌ **MISSING** — no API endpoints |
| FluentValidation for requests | ❌ **MISSING** |

### 2.6 Desktop Layer ❌ (Missing entirely)

| Component | Status |
|-----------|--------|
| JournalEntry list ViewModel/View | ❌ **MISSING** |
| JournalEntry editor ViewModel/View | ❌ **MISSING** |
| Opening entry screen | ❌ **MISSING** |
| Annual closing screen | ❌ **MISSING** |
| Trial balance report screen | ❌ **MISSING** |
| Account statement report screen | ❌ **MISSING** |

### 2.7 Tests ✅ (Partially exists)

| Test | Status |
|------|--------|
| `JournalEntryTests.cs` | ✅ Exists — 1 test file |
| Service tests | ❌ **MISSING** |
| API controller tests | ❌ **MISSING** |
| ViewModel tests | ❌ **MISSING** |

---

## 3. BLOCKER Resolution — Critical Fixes

These 3 issues must be resolved before Phase 30 implementation begins.

### 3.1 Blocker 1: Currency/Exchange Rate Dependency on Phase 20

**Problem**: Journal entries need `CurrencyId` and `ExchangeRate` fields, but the Currency module (Phase 20) is not yet complete. Without currency support, multi-currency transactions cannot be recorded properly in journal entries — all amounts must be stored in the base currency, but the original currency and rate must be preserved for audit.

**Root cause**: The system currently assumes single-currency operation. Adding currency fields to JournalEntry now creates a forward dependency.

**Fix**:
1. Add `int? CurrencyId` and `decimal(18,6)? ExchangeRate` to JournalEntry entity
2. Make both nullable — existing entries (single-currency) keep them null
3. Reference `Currencies` table via nullable FK with `DeleteBehavior.Restrict`
4. Add a system setting `DefaultCurrencyId` as fallback when CurrencyId is null

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Files changed**: `JournalEntry.cs`, `JournalEntryConfiguration.cs`, `CreateJournalEntryRequest.cs`, `IJournalEntryService.cs`, `JournalEntryService.cs`

### 3.2 Blocker 2: Account Validation — Must Exist + Active + Allow Transactions

**Problem**: The current `JournalEntryService.CreateJournalEntryAsync()` does validate that accounts exist and are active (lines 62-73), but doesn't validate that accounts **allow transactions** — some accounts are parent/heading accounts that should not have direct postings.

**Root cause**: The Account entity has no `AllowTransactions` / `IsLeafAccount` flag. Parent tree nodes in the Chart of Accounts should not receive direct journal entries.

**Fix**:
1. Add `bool IsLeafAccount` to Account entity (or verify via `CanHaveChildren` pattern)
2. In `JournalEntryService.CreateJournalEntryAsync()`, add validation: `if (account.IsLeafAccount == false) return Result.Failure("لا يمكن الترحيل إلى حساب رئيسي — استخدم حساب تفصيلي")`

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Files changed**: `Account.cs` (add `IsLeafAccount`), `AccountConfiguration.cs`, `JournalEntryService.cs`

**Note**: If `IsLeafAccount` doesn't exist yet, add it as a new column with default `true` (all existing accounts become leaf accounts, then users can set parent accounts to `false`).

### 3.3 Blocker 3: Auto-Entry Integration Contracts Across All Modules

**Problem**: The system currently auto-creates basic journal entries for sales and purchases in a hardcoded fashion. To extend auto-entries to ALL modules (payments, cash, returns, transfers), we need a consistent contract/interface that every module follows.

**Root cause**: No centralized `IAutoJournalEntryProvider` interface. Each module creates journal entries differently — some directly, some via service calls.

**Fix**:
1. Create `IAutoJournalEntryProvider` interface in Application layer
2. Each module implements it: `SalesAutoEntryProvider`, `PurchaseAutoEntryProvider`, `PaymentAutoEntryProvider`, `CashTransactionAutoEntryProvider`, `ReturnAutoEntryProvider`, `TransferAutoEntryProvider`
3. A central `AutoJournalEntryOrchestrator` calls the appropriate provider based on `ReferenceType` + `EntryType`
4. Existing code is refactored to use the provider pattern

> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern.

**Files to create**:
- `Application/Accounting/Services/AutoEntryProviders/IAutoJournalEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/AutoJournalEntryOrchestrator.cs`
- `Application/Accounting/Services/AutoEntryProviders/SalesAutoEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/PurchaseAutoEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/PaymentAutoEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/CashAutoEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/ReturnAutoEntryProvider.cs`
- `Application/Accounting/Services/AutoEntryProviders/TransferAutoEntryProvider.cs`

---

## 4. Design Catalog

### 4.1 JournalEntry (Extended Entity)

| # | Field | Type | Required | Default | Description |
|---|-------|------|----------|---------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto | Auto-increment primary key |
| 2 | `EntryNumber` | `nvarchar(50)` | ✅ | — | Unique entry number (JE-YYYY-NNNNNN) |
| 3 | `TransactionDate` | `datetime2` | ✅ | — | Date of the transaction |
| 4 | `Description` | `nvarchar(500)` | ❌ | `null` | Header description for the entry |
| 5 | `EntryType` | `tinyint` (enum) | ✅ | — | Manual=8, OpeningBalance=9, AnnualClosing=10, Reversal=11 + existing |
| 6 | `ReferenceType` | `nvarchar(50)` | ❌ | `null` | Source module: "Sales", "Purchase", "Payment", etc. |
| 7 | `ReferenceId` | `int` | ❌ | `null` | FK to source transaction |
| 8 | `ReferenceNumber` | `nvarchar(50)` | ❌ | `null` | Source invoice/payment number |
| 9 | `IsPosted` | `bit` | ✅ | `false` | Posted status |
| 10 | `IsReversed` | `bit` | ✅ | `false` | Reversed status |
| 11 | `PostedBy` | `int` | ❌ | `null` | FK → Users |
| 12 | `PostedAt` | `datetime2` | ❌ | `null` | Posting timestamp |
| 13 | **`CurrencyId`** | `int` | ❌ | `null` | FK → Currencies |
| 14 | **`ExchangeRate`** | `decimal(18,6)` | ❌ | `null` | Exchange rate to base currency |
| 15 | **`AttachmentPath`** | `nvarchar(255)` | ❌ | `null` | Server file path of attached document |
| 16 | **`AttachmentFileName`** | `nvarchar(100)` | ❌ | `null` | Original file name for display |
| 17 | **`IsDraft`** | `bit` | ✅ | `true` | Draft status — defaults to true on creation |
| 18 | **`ReversedById`** | `int` | ❌ | `null` | FK → JournalEntries (the reversal entry ID) |
| 19 | **`ReversedAt`** | `datetime2` | ❌ | `null` | When reversal happened |
| 20 | **`ReversalReason`** | `nvarchar(500)` | ❌ | `null` | Why the entry was reversed |
| 21 | `CreatedByUserId` | `int` | ✅ | — | FK → Users (from BaseEntity) |
| 22 | `CreatedAt` | `datetime2` | ✅ | `GETUTCDATE()` | From BaseEntity |
| 23 | `IsActive` | `bit` | ✅ | `true` | From BaseEntity |
| 24 | `UpdatedByUserId` | `int` | ❌ | `null` | From BaseEntity |

### 4.2 JournalEntryLine (Extended Entity)

| # | Field | Type | Required | Description |
|---|-------|------|----------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto-increment |
| 2 | `JournalEntryId` | `int FK` | ✅ | Parent entry (Cascade delete) |
| 3 | `AccountId` | `int FK` | ✅ | FK → Accounts (Restrict delete) |
| 4 | `AccountCode` | `nvarchar(50)` | ✅ | Denormalized account code for fast query |
| 5 | `AccountNameAr` | `nvarchar(200)` | ✅ | Denormalized account name for fast query |
| 6 | `Debit` | `decimal(18,2)` | ✅ | Debit amount (0 if credit line) |
| 7 | `Credit` | `decimal(18,2)` | ✅ | Credit amount (0 if debit line) |
| 8 | `Description` | `nvarchar(500)` | ❌ | Per-line description (already exists) |

### 4.3 OpeningEntry (NEW Concept — No Dedicated Table)

Opening entries are **not** a separate table. They use `JournalEntry` with `EntryType = OpeningBalance (9)`:

> See `docs/database-schema.md` Module 4.3 for the canonical OpeningEntry concept and `docs/AGENTS.md` for domain entity patterns.

**Domain logic**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `SalesSystem.Contracts/` for DTO definitions.

### 4.4 AnnualClosing (NEW Concept — No Dedicated Table)

Annual closing uses `JournalEntry` with `EntryType = AnnualClosing (10)`:

> See `docs/AGENTS.md` for domain entity patterns and `docs/database-schema.md` for the FiscalYear table definition.

**Implementation phases**:
- **V1**: Simple closing — zero out P&L accounts, move net to Retained Earnings. Manual process.
- **Future**: Automated year-end wizard with trial balance verification.

### 4.5 Trial Balance Report (NEW)

> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Calculation**:
- For each account:
  - Opening balance: Sum of all posted entry lines BEFORE the period
  - Period movement: Sum of all posted entry lines IN the period
  - Closing balance: Opening + Period movement
- Total debit MUST equal total credit (balance check)

### 4.6 Account Statement (Enhanced)

Current `AccountLedgerDto` already supports:
- Opening balance
- Date-ordered lines with running balance
- Total debit/credit
- Closing balance

**Enhancements needed**:
- Add `AccountId` field (missing currently)
- Add `CurrencyId`/`CurrencyCode` for multi-currency support
- Add filter by EntryType (show only manual, or only auto entries)
- Add export to Excel

### 4.7 Document Attachments

> See `docs/AGENTS.md` for domain entity patterns and `docs/database-schema.md` for the JournalEntry table definition.

---

## 5. Gap Analysis

### 5.1 Domain Layer

| Component | Status | Action |
|-----------|--------|--------|
| CurrencyId + ExchangeRate | ❌ Missing | Add to entity + factory method |
| AttachmentPath + AttachmentFileName | ❌ Missing | Add to entity |
| IsDraft flag | ❌ Missing | Add to entity |
| Reversal tracking (ReversedById, ReversedAt, ReversalReason) | ❌ Missing | Add to entity |
| AnnualClosing enum value (10) | ❌ Missing | Add to JournalEntryType |
| Reversal enum value (11) | ❌ Missing | Add to JournalEntryType |
| JournalEntryLineConfiguration | ❌ Missing | Create explicit config |
| Account.IsLeafAccount validation | ❌ Missing | Add to Account entity |

### 5.2 Application Layer

| Component | Status | Action |
|-----------|--------|--------|
| SaveDraftAsync | ❌ Missing | Add to service |
| PostEntryAsync | ❌ Missing | Extract from Create |
| ReverseEntryAsync | ❌ Missing | New method |
| GetTrialBalanceAsync | ❌ Missing | New method |
| GetDetailedTrialBalanceAsync | ❌ Missing | New method |
| CreateOpeningEntryAsync | ❌ Missing | New method |
| CreateAnnualClosingAsync | ❌ Missing | New method |
| GetAllAsync / GetByIdAsync | ❌ Missing | Add to service |
| AutoJournalEntryOrchestrator | ❌ Missing | New centralized service |
| SalesAutoEntryProvider | ❌ Missing | Refactor from existing code |
| PurchaseAutoEntryProvider | ❌ Missing | Refactor from existing code |
| PaymentAutoEntryProvider | ❌ Missing | New |
| CashAutoEntryProvider | ❌ Missing | New |
| ReturnAutoEntryProvider | ❌ Missing | New |
| TransferAutoEntryProvider | ❌ Missing | New |

### 5.3 Contracts Layer

| Component | Status | Action |
|-----------|--------|--------|
| JournalEntryDto | ❌ Missing | Create |
| JournalEntryLineDto | ❌ Missing | Create |
| TrialBalanceDto + Line | ❌ Missing | Create |
| OpeningBalanceLineRequest | ❌ Missing | Create |
| AnnualClosingRequest | ❌ Missing | Create |
| PostJournalEntryRequest | ❌ Missing | Create |
| ReverseJournalEntryRequest | ❌ Missing | Create |
| CreateJournalEntryRequest: CurrencyId/ExchangeRate | ❌ Missing | Add fields |
| CreateJournalEntryRequest: Attachment fields | ❌ Missing | Add fields |

### 5.4 API Layer

| Component | Status | Action |
|-----------|--------|--------|
| JournalEntriesController | ❌ Missing | Create with all endpoints |
| FluentValidation for requests | ❌ Missing | Create validators |

### 5.5 Desktop Layer

| Component | Status | Action |
|-----------|--------|--------|
| JournalEntryListViewModel | ❌ Missing | Create |
| JournalEntryListView.xaml | ❌ Missing | Create |
| JournalEntryEditorViewModel | ❌ Missing | Create |
| JournalEntryEditorView.xaml | ❌ Missing | Create |
| OpeningEntryViewModel | ❌ Missing | Create |
| OpeningEntryView.xaml | ❌ Missing | Create |
| AnnualClosingViewModel | ❌ Missing | Create |
| AnnualClosingView.xaml | ❌ Missing | Create |
| TrialBalanceViewModel | ❌ Missing | Create |
| TrialBalanceView.xaml | ❌ Missing | Create |
| AccountStatementViewModel | ❌ Missing | Create |
| AccountStatementView.xaml | ❌ Missing | Create |
| API service interfaces | ❌ Missing | Add IJournalEntriesApiService |

### 5.6 Tests

| Component | Status | Action |
|-----------|--------|--------|
| JournalEntry domain tests (enhanced) | ❌ Missing | Add reversal, closing, opening tests |
| JournalEntryService tests | ❌ Missing | Create |
| AutoEntryProvider tests | ❌ Missing | Create |
| API controller tests | ❌ Missing | Create |

### 5.7 SystemAccountMappings Integration

**Critical dependency**: Every auto-entry provider must reference `SystemAccountMappings` to resolve account IDs dynamically instead of hardcoding account IDs.

**The 13 system accounts from Phase 22 seed data:**

| # | System Account | Code | Normal Balance |
|---|---------------|------|----------------|
| 1 | CashOnHand | 1100 | Debit |
| 2 | SalesRevenue | 4100 | Credit |
| 3 | CostOfGoodsSold (COGS) | 5100 | Debit |
| 4 | Inventory | 1500 | Debit |
| 5 | AccountsReceivable (AR) | 1210 | Debit |
| 6 | AccountsPayable (AP) | 2100 | Credit |
| 7 | VATPayable | 2500 | Credit |
| 8 | VATReceivable | 1200 | Debit |
| 9 | DiscountAllowed | 5200 | Debit |
| 10 | DiscountReceived | 5300 | Credit |
| 11 | FreightIn | 5400 | Debit |
| 12 | PurchaseReturns | 5500 | Credit |
| 13 | SalesReturns | 5600 | Debit |

**Implementation requirements:**

1. Every auto-entry provider (Sales, Purchase, Payment, Cash, Return, Transfer) MUST inject `ISystemAccountService` or access a `SystemAccountMappings` repository to resolve account IDs dynamically
2. Add a resolution method: `GetAccountId(SystemAccountType type)` that looks up the mapping by type enum
3. Auto-entry providers MUST NOT hardcode account IDs — all account references go through the mapping
4. Add audit logging: each account resolution is logged at `Log.Verbose`/`Log.Debug` level
5. If a system account mapping is missing for the required type, the provider MUST return `Result.Failure("لم يتم تعيين حساب {AccountName} في إعدادات النظام")`

**Files to create/update:**

| File | Change |
|------|--------|
| `Application/Accounting/Interfaces/ISystemAccountService.cs` | **CREATE** — interface with `GetAccountId(SystemAccountType)` |
| `Application/Accounting/Services/SystemAccountService.cs` | **CREATE** — reads from SystemAccountMappings table, caches results |
| `Domain/Accounting/Enums/SystemAccountType.cs` | **CREATE** — enum with 13 values matching the accounts above |
| `Infrastructure/Data/Configurations/SystemAccountMappingConfiguration.cs` | Verify exists from Phase 22 |
| `Application/DependencyInjection.cs` | Register `ISystemAccountService` as scoped |
| All 6 auto-entry providers | Inject and use `ISystemAccountService` for account resolution |

**Example provider pattern:**
> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern.

**Integration test requirement**: Each provider's test MUST verify account resolution failure behavior.

**Logging pattern**:
> See `docs/AGENTS.md` §2.15 for Serilog logging patterns.

---

## 6. Architectural Decisions

### 6.1 Entry Lifecycle: Draft → Posted → Reversed (NOT isPosted bool)

**Decision**: Implement a 3-state lifecycle instead of the current binary `IsPosted` flag.

- **Draft** (`IsDraft = true`, `IsPosted = false`): Editable, not visible in reports, no stock/balance impact. Auto-saved as user types.
- **Posted** (`IsDraft = false`, `IsPosted = true`): Locked, reflects in reports and account balances. Cannot be edited.
- **Reversed** (`IsPosted = false`, `IsReversed = true`): The entry and its reversal are both visible for audit. Original entry is marked as reversed.

**Why not just `IsPosted`?** The analysis documents show users need to save drafts (e.g., partially complete entries), post them when ready, and reverse errors. A single boolean cannot represent all 3 states.

**Implementation**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

### 6.2 Auto-Posting Rules

**Decision**: Auto-posting creates balanced journal entries automatically when a source transaction is posted. Each module provides debit/credit lines via `IAutoJournalEntryProvider`.

| Source Transaction | Debit | Credit | When |
|-------------------|-------|--------|------|
| Sales Invoice (Cash) | CashBox | Sales Revenue | On invoice post |
| Sales Invoice (Credit) | Customer Receivable | Sales Revenue | On invoice post |
| Sales Invoice (COGS) | Cost of Goods Sold | Inventory | On invoice post |
| Purchase Invoice (Cash) | Inventory | CashBox | On invoice post |
| Purchase Invoice (Credit) | Inventory | Supplier Payable | On invoice post |
| Customer Payment | CashBox | Customer Receivable | On payment |
| Supplier Payment | Supplier Payable | CashBox | On payment |
| Cash Expense | Expense Account | CashBox | On cash transaction |
| Sales Return | Sales Returns | CashBox/Customer | On return post |
| Purchase Return | CashBox/Supplier | Inventory | On return post |
| Stock Transfer | Warehouse A (Out) | Warehouse B (In) | On transfer post |
| Currency Gain/Loss | CashBox | Currency Gain | On rate change |
| Currency Gain/Loss | Currency Loss | CashBox | On rate change |

### 6.3 Opening Balance vs Regular Entry

**Decision**: Opening entries ARE journal entries with `EntryType = OpeningBalance`. They are:
- Auto-posted on creation (no draft state)
- Immutable after creation
- Always balanced (system enforces)
- Have `ReferenceType = "System"`, `ReferenceId = 0`
- Can only be created once per fiscal period

**Implementation**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

### 6.4 Annual Closing Procedure

**Decision**: Annual closing is a multi-step procedure. Requires a proper `FiscalYear` entity (not a flat string).

**FiscalYear Entity**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the canonical table definition.

**Migration**: CREATE FiscalYears table.
**Seed data**: Create FiscalYear for current year (2026) on first run.
**Validation**: JournalEntry.TransactionDate must fall within an open (not closed) FiscalYear.

**Closing procedure**:
1. **Pre-checks**: All entries must be posted. No drafts open. Trial balance must balance.
2. **Zero revenue/expenses**: Create closing entry debiting all revenue accounts and crediting all expense accounts. The difference goes to `Retained Earnings`.
3. **Mark year closed**: `fiscalYear.Close(closedByUserId)` — sets `IsClosed = true`
4. **Generate opening entry**: Create `EntryType = OpeningBalance` for the new year with all balance sheet accounts' closing balances.
5. **Prevent future entries in closed year**: Before creating any entry, verify `fiscalYear.IsDateInFiscalYear(transactionDate) && !fiscalYear.IsClosed`

**V1 scope**: Simple, manually-triggered process. Wizard-like screen guides the user through steps.

### 6.5 Delete Behavior for JournalEntry → Lines

**Decision**: Use `DeleteBehavior.Restrict` (RULE-214 compliance) for the JournalEntry → JournalEntryLine relationship.

**Rationale**: 
- RULE-214 mandates Restrict on ALL FKs — no exceptions.
- Since entries are never hard-deleted (soft delete only via `IsActive`), Restrict is safe: soft delete sets `IsActive = false` on the entry, and the query filter hides lines.
- Reversal pattern: Use `ReversedByEntryId` FK to reverse posted entries (creates a new offsetting entry) — no deletion needed.
- For draft entries: delete lines explicitly before deleting the entry.

### 6.6 Account Validation: Leaf Accounts Only

**Decision**: Journal entries MUST post only to leaf-level accounts (accounts that have no children in the account tree). Parent/heading accounts serve as organizational containers and should not hold direct balances from journal entries.

**Validation rule**: Before creating any journal entry line, verify `Account.IsLeafAccount == true`. If false, reject with: `"لا يمكن الترحيل إلى حساب رئيسي — استخدم حساب تفصيلي"`

### 6.7 Why Events? Because EventBus Integration Matters

**Decision**: When a journal entry is posted, reversed, or a year is closed, publish an EventBus message so other modules can react:

| Event | Published When | Subscribers |
|-------|---------------|-------------|
| `JournalEntryPostedMessage(entryId)` | After posting | Dashboard (update totals), Account screen |
| `JournalEntryReversedMessage(entryId, reversalEntryId)` | After reversal | Account screen update |
| `FiscalYearClosedMessage(year)` | After annual closing | All modules (prevent back-dated entries) |

---

## 7. Non-V1 Items (Deferred)

These features appeared in analysis documents but are **deferred** to future versions:

| Feature | Source | Reason |
|---------|--------|--------|
| Budget tracking per account | Analysis Part 3 | Requires Budget entity — beyond V1 scope |
| Income Statement (قائمة الدخل) | Analysis Part 5 | Requires full P&L aggregation — Phase 31+ |
| Balance Sheet (المركز المالي) | Analysis Part 5 | Requires all asset/liability accounts — Phase 31+ |
| Cash Flow Statement | Analysis Part 1 | Requires cash flow classification — Phase 32+ |
| Audit trail full export (CSV/PDF) | Analysis Part 3 | Reporting enhancement — Phase 33+ |
| Automated year-end wizard | Analysis Part 1 | Multi-step guided closing — Phase 34+ |
| Cost center allocation per line | Analysis Part 3 | Requires CostCenter entity |
| Inter-branch journal entries | Analysis Part 3 | Multi-branch support — Phase 35+ |
| Batch posting (multiple entries at once) | User requirement | Low priority — manual posting is sufficient |
| Recurring journal entries (templates) | Analysis Part 3 | Automation feature — Phase 36+ |
| Journal entry approval workflow (multi-level) | Analysis Part 3 | Requires approval entity — Phase 37+ |
| Digital signature on entries | Analysis Part 3 | Requires signing infrastructure |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 1 — Add CurrencyId + ExchangeRate to JournalEntry

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `int? CurrencyId`, `decimal? ExchangeRate`, `Currency? Currency` nav property; add optional params to `Create()` |
| `Domain/Accounting/Entities/JournalEntryLine.cs` | Add `string? CurrencyCode` (optional denormalized) |
| `Infrastructure/Data/Configurations/JournalEntryConfiguration.cs` | Add FK config: `.HasForeignKey(je => je.CurrencyId).OnDelete(DeleteBehavior.Restrict)` |
| `Contracts/Requests/AccountingRequests.cs` — `CreateJournalEntryRequest` | Add `int? CurrencyId`, `decimal? ExchangeRate` |
| `Contracts/DTOs/AllDtos.cs` | Add `CurrencyId`, `CurrencyCode`, `ExchangeRate` to `JournalEntryDto` |
| `Application/Accounting/Services/JournalEntryService.cs` | Map currency fields in `CreateJournalEntryAsync()` |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE JournalEntries ADD CurrencyId int NULL, ExchangeRate decimal(18,6) NULL` |

**Domain guards**:
> See `docs/AGENTS.md` for Guard Clauses pattern in domain entities.

**Logging**: `Log.Information("Journal entry {EntryNumber} created with Currency {CurrencyId} @ rate {Rate}", ...)`

**Estimate**: ~45 minutes

---

### Task 2 — Add Attachment Support to JournalEntry

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `string? AttachmentPath`, `string? AttachmentFileName` |
| `Infrastructure/Data/Configurations/JournalEntryConfiguration.cs` | Add `.Property(je => je.AttachmentPath).HasMaxLength(255)` and same for FileName |
| `Contracts/Requests/AccountingRequests.cs` | Add `string? AttachmentFileName`, `byte[]? AttachmentContent` (base64 or null) |
| `Application/Accounting/Services/JournalEntryService.cs` | Save attachment to disk in `%AppData%\SalesSystem\Attachments\JournalEntries\{Id}\`; map fields |
| `Application/Interfaces/Services/IJournalEntryService.cs` | No change — fields are optional in request |
| `Infrastructure/Data/Migrations/` | New migration: add columns |

**Storage strategy**:
- On create: if `AttachmentContent` provided, save file, set `AttachmentPath` + `AttachmentFileName`
- On get: return `AttachmentPath` + `AttachmentFileName` (client downloads via separate endpoint)
- On delete (soft): file stays on disk (orphaned files cleaned by maintenance task)

**Estimate**: ~45 minutes

---

### Task 3 — Enhanced JournalEntryLine with Per-Line Description (Already Exists)

**Verification**: The `JournalEntryLine` entity already has `string? Description` field (line 19). No changes needed.

**However**: Add explicit `JournalEntryLineConfiguration` for proper precision and FK config.

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/Configurations/JournalEntryLineConfiguration.cs` | **CREATE**: Explicit config with `HasPrecision(18,2)` for Debit/Credit, FK to Accounts with `DeleteBehavior.Restrict`, FK to JournalEntry with `DeleteBehavior.Cascade` |
| `Infrastructure/Data/Configurations/JournalEntryConfiguration.cs` | Move line FK config to the new file |

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

**Estimate**: ~20 minutes

---

### Task 4 — Entry Status Lifecycle (Draft → Posted → Reversed)

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `IsDraft` field (default true), `Status` computed property; add `ValidateBeforePost()` (extracted from `ValidateAndPost()`), add `Reverse()` method |
| `Contracts/DTOs/AllDtos.cs` | Add `JournalEntryDto` with status fields |
| `Contracts/Requests/AccountingRequests.cs` | Add `PostJournalEntryRequest`, `ReverseJournalEntryRequest` |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `PostEntryAsync()`, `ReverseEntryAsync()`, `GetByIdAsync()`, `GetAllAsync()` |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement new methods; refactor `CreateJournalEntryAsync()` to support draft vs immediate post |
| `Domain/Accounting/Enums/JournalEntryType.cs` | Add `Reversal = 11` |

**Domain method — Reverse**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

**Logging**: `Log.Information("Journal entry {EntryNumber} (ID={Id}) reversed by User {UserId}. Reason: {Reason}", ...)`

**Estimate**: ~1 hour

---

### Task 5 — Opening Entry Workflow

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `CreateOpeningEntry()` static factory method |
| `Contracts/DTOs/AllDtos.cs` | Add `OpeningBalanceLineRequest` record |
| `Contracts/Requests/AccountingRequests.cs` | Add `CreateOpeningEntryRequest` |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `CreateOpeningEntryAsync()` |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement — validate balanced, check no prior opening entry exists, auto-post |
| `Domain/Accounting/Enums/JournalEntryType.cs` | `OpeningBalance = 9` already exists |

**Opening entry business rules**:
1. Only ONE opening entry can exist per fiscal period
2. Must be balanced (total debits = total credits)
3. Auto-posted (no draft state)
4. Only asset, liability, and equity accounts allowed (no revenue/expense)
5. `ReferenceType = "OpeningBalance"`, `ReferenceId = 0`

**Validation**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Logging**: `Log.Information("Opening entry created for fiscal year {Year} with {LineCount} lines", year, lines.Count)`

**Estimate**: ~1.5 hours

---

### Task 6 — Annual Closing Workflow

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `CreateAnnualClosingEntry()` static factory method (or a separate `AnnualClosingService`) |
| `Contracts/Requests/AccountingRequests.cs` | Add `CreateAnnualClosingRequest` |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `CreateAnnualClosingAsync()` |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement closing procedure with transaction |
| `Domain/Accounting/Enums/JournalEntryType.cs` | Add `AnnualClosing = 10` |
| `Infrastructure/Data/Configurations/JournalEntryConfiguration.cs` | No change needed |
| `Application/Interfaces/Repositories/ISystemSettingsRepository.cs` | Add `SetStringAsync("ClosedFiscalYear", ...)` |

**Annual closing procedure step-by-step**:

```
Step 1: Pre-checks (within transaction)
├── Verify no prior closing for this year
├── Verify all entries in the year are posted (no drafts)
├── Verify trial balance is balanced
└── Verify no entries exist AFTER the closing date

Step 2: Calculate P&L
├── Sum all Revenue accounts → TotalRevenue
├── Sum all Expense accounts → TotalExpense
├── NetProfit = TotalRevenue - TotalExpense
└── NetLoss = TotalExpense - TotalRevenue

Step 3: Create closing entry
├── Debit: All Revenue accounts (zero them out)
├── Credit: All Expense accounts (zero them out)
├── If profit: Credit RetainedEarnings = NetProfit
└── If loss: Debit RetainedEarnings = NetLoss

Step 4: Create opening entry for new year
├── Debit: All asset accounts with closing balance
└── Credit: All liability + equity accounts with closing balance

Step 5: Mark fiscal year closed
└── _systemSettingsRepo.SetStringAsync("ClosedFiscalYear", year.ToString())
```

**Note**: Step 4 is deferred to a separate `CreateOpeningEntryAsync()` call (user-initiated). The annual closing only does Steps 1-3 + Step 5.

**Logging**: `Log.Information("Fiscal year {Year} closed. Net profit: {NetProfit:C2}", year, netProfit)`

**Estimate**: ~2 hours

---

### Task 7 — Enhanced Auto-Journal Entry Integration (All Modules)

**Files**:

| File | Change |
|------|--------|
| `Application/Accounting/Services/AutoEntryProviders/IAutoJournalEntryProvider.cs` | **CREATE** — interface contract |
| `Application/Accounting/Services/AutoEntryProviders/AutoJournalEntryOrchestrator.cs` | **CREATE** — resolves provider by ReferenceType |
| `Application/Accounting/Services/AutoEntryProviders/SalesAutoEntryProvider.cs` | **CREATE** — generate lines for sales invoice + COGS |
| `Application/Accounting/Services/AutoEntryProviders/PurchaseAutoEntryProvider.cs` | **CREATE** — generate lines for purchase invoice |
| `Application/Accounting/Services/AutoEntryProviders/PaymentAutoEntryProvider.cs` | **CREATE** — generate lines for customer/supplier payments |
| `Application/Accounting/Services/AutoEntryProviders/CashAutoEntryProvider.cs` | **CREATE** — generate lines for cash transactions |
| `Application/Accounting/Services/AutoEntryProviders/ReturnAutoEntryProvider.cs` | **CREATE** — generate lines for sales/purchase returns |
| `Application/Accounting/Services/AutoEntryProviders/TransferAutoEntryProvider.cs` | **CREATE** — generate lines for stock transfers |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `CreateAutoJournalEntryAsync(ReferenceType, ReferenceId)` |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement using orchestrator |
| `Application/ServiceRegistration.cs` | Register all providers as scoped services |

**IAutoJournalEntryProvider interface**:
> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern.

**Orchestrator pattern**:
> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern.

**Existing code refactoring**: 
- Current code that auto-creates entries for sales/purchases must be refactored to call `AutoJournalEntryOrchestrator` instead of inline logic
- This ensures consistency across all modules

**Logging**: `Log.Information("Auto journal entry created for {ReferenceType} #{ReferenceId}", type, id)`

**Estimate**: ~3 hours

---

### Task 8 — Trial Balance Report

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `TrialBalanceDto`, `TrialBalanceLineDto` |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `GetTrialBalanceAsync(DateTime asOfDate)` |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement — query all accounts + sum posted lines per account |
| `Api/Controllers/JournalEntriesController.cs` | Add `GET /api/v1/journal-entries/trial-balance` |
| `Desktop/Services/Api/IJournalEntriesApiService.cs` | Add `GetTrialBalanceAsync()` |
| `Desktop/ViewModels/Reports/TrialBalanceViewModel.cs` | **CREATE** |
| `Desktop/Views/Reports/TrialBalanceView.xaml` | **CREATE** |
| `Desktop/Views/Reports/TrialBalanceView.xaml.cs` | **CREATE** |

**SQL-like logic**:
> See `docs/database-schema.md` for the canonical table definitions and query patterns.

**Estimate**: ~2 hours

---

### Task 9 — Account Statement Report (With Running Balance)

**Enhancement** to the existing `GetAccountLedgerAsync()` method.

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `AccountId` to `AccountLedgerDto`; add `CurrencyCode` |
| `Application/Accounting/Services/JournalEntryService.cs` | Enhance `GetAccountLedgerAsync()` — add filters for EntryType, ReferenceType; add Excel export |
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `GetAccountStatementAsync()` overload with more filters |
| `Api/Controllers/JournalEntriesController.cs` | Add `GET /api/v1/journal-entries/account-statement` with query params |
| `Desktop/Services/Api/IJournalEntriesApiService.cs` | Add `GetAccountStatementAsync()` |
| `Desktop/ViewModels/Reports/AccountStatementViewModel.cs` | **CREATE** with date range picker, account selector, export |
| `Desktop/Views/Reports/AccountStatementView.xaml` | **CREATE** with compact DataGrid, running balance column |
| `Desktop/Views/Reports/AccountStatementView.xaml.cs` | **CREATE** |

**Running balance calculation** (same as current implementation):
> See `docs/AGENTS.md` for service layer patterns and `SalesSystem.Application/` for canonical service implementations.

**Estimate**: ~1.5 hours

---

### Task 10 — Enhanced JournalEntryEditorViewModel (Desktop)

**Files**:

| File | Change |
|------|--------|
| `Desktop/ViewModels/Accounting/JournalEntryEditorViewModel.cs` | **CREATE** — full editor with line grid, validation, save/post/reverse |
| `Desktop/Services/Api/IJournalEntriesApiService.cs` | **CREATE** — HTTP client interface |
| `Desktop/Services/Api/JournalEntriesApiService.cs` | **CREATE** — HTTP client implementation |
| `Desktop/Messaging/Messages/AppMessages.cs` | Add `JournalEntryChangedMessage`, `JournalEntryPostedMessage` |
| `Desktop/App.xaml.cs` | DI registrations |

**ViewModel structure**:
> See `DesktopPWF/ViewModels/` for canonical ViewModel patterns and `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern.

**Validation** (RULE-059, RULE-228):
- All buttons enabled — validate on click
- `INotifyDataErrorInfo` for real-time field validation
- `ValidateAllAsync()` for pre-save warning dialog
- Per-line validation: account required, amount > 0

**Estimate**: ~3 hours

---

### Task 11 — Updated JournalEntryEditorView.xaml (Desktop)

**Files**:

| File | Change |
|------|--------|
| `Desktop/Views/Accounting/JournalEntryEditorView.xaml` | **CREATE** — full editor UI |
| `Desktop/Views/Accounting/JournalEntryEditorView.xaml.cs` | **CREATE** — code-behind with auto-focus (RULE-228) |

**XAML structure**:
```
┌──────────────────────────────────────┐
│ Header: قيد يومي جديد                 │
├──────────────────────────────────────┤
│ Entry info: Date | Type | Currency   │
│ Description (multiline)              │
├──────────────────────────────────────┤
│ Lines DataGrid:                      │
│ ┌────┬────────┬───────┬──────┬────┐  │
│ │ # │ حساب   │ بيان  │ مدين│ دائن│  │
│ ├────┼────────┼───────┼──────┼────┤  │
│ │ 1 │ ...    │ ...   │ ... │ ...│  │
│ │ 2 │ ...    │ ...   │ ... │ ...│  │
│ └────┴────────┴───────┴──────┴────┘  │
│ ➕ إضافة بند                           │
├──────────────────────────────────────┤
│ Totals: TotalDebit=... TotalCredit=..│
│ BalanceStatus: ✅ متوازن              │
├──────────────────────────────────────┤
│ Footer:                              │
│ [💾 حفظ كمسودة] [✅ ترحيل] [↩ إلغاء]│
└──────────────────────────────────────┘
```

**Compact UI** (RULE-262-274):
- No hardcoded heights: use 28px via styles
- Header: Padding="12,6", Footer: Padding="12,8"
- Field spacing: Margin="0,0,0,6"
- DataGrid row height: 24px (CompactDataGrid style)

**ToolTips** (RULE-185-190):
- Add line button: `"إضافة بند جديد للقيد"`
- Remove line button: `"حذف البند المحدد"`
- Save Draft button: `"حفظ القيد كمسودة — لا يؤثر على الأرصدة"`
- Post button: `"ترحيل القيد — سيتم تحديث أرصدة الحسابات"`
- Reverse button: `"عكس القيد — إنشاء قيد عكسي"`
- Attach file button: `"إرفاق صورة أو مستند داعم للقيد"`

**Estimate**: ~2.5 hours

---

### Task 12 — Opening/Closing Desktop Screens

**Files**:

| File | Change |
|------|--------|
| `Desktop/ViewModels/Accounting/OpeningEntryViewModel.cs` | **CREATE** — list accounts + input balances + create |
| `Desktop/Views/Accounting/OpeningEntryView.xaml` | **CREATE** — DataGrid of accounts with balance input |
| `Desktop/Views/Accounting/OpeningEntryView.xaml.cs` | **CREATE** |
| `Desktop/ViewModels/Accounting/AnnualClosingViewModel.cs` | **CREATE** — select year, preview, execute |
| `Desktop/Views/Accounting/AnnualClosingView.xaml` | **CREATE** — step-by-step wizard |
| `Desktop/Views/Accounting/AnnualClosingView.xaml.cs` | **CREATE** |
| `Desktop/Services/Api/IJournalEntriesApiService.cs` | Add `CreateOpeningEntryAsync()`, `CreateAnnualClosingAsync()`, `GetOpeningEntryStatusAsync()` |
| `Desktop/App.xaml.cs` | DI + navigation |

**Opening Entry screen**:
- Shows a DataGrid of all leaf accounts with columns: Account Code, Account Name, Debit, Credit
- User enters opening balances for each account
- Validates balanced before saving
- Shows warning if opening entry already exists for this year

**Annual Closing screen**:
- Step 1: Select fiscal year to close (default = previous year)
- Step 2: Preview — shows trial balance, net profit/loss
- Step 3: Confirm — shows checklist of pre-conditions
- Step 4: Execute — creates closing entry, marks year closed

**Estimate**: ~3 hours

---

### Task 13 — API Endpoint Updates

**File**: `SalesSystem.Api/Controllers/JournalEntriesController.cs` **CREATE**

| Method | Endpoint | Description | Policy |
|--------|----------|-------------|--------|
| `GET` | `/api/v1/journal-entries` | List all entries (paginated, filterable) | `AllStaff` |
| `GET` | `/api/v1/journal-entries/{id}` | Get single entry with lines | `AllStaff` |
| `POST` | `/api/v1/journal-entries` | Create entry (draft or post) | `ManagerAndAbove` |
| `POST` | `/api/v1/journal-entries/{id}/post` | Post a draft entry | `ManagerAndAbove` |
| `POST` | `/api/v1/journal-entries/{id}/reverse` | Reverse a posted entry | `ManagerAndAbove` |
| `POST` | `/api/v1/journal-entries/opening` | Create opening entry | `AdminOnly` |
| `POST` | `/api/v1/journal-entries/annual-closing` | Execute annual closing | `AdminOnly` |
| `GET` | `/api/v1/journal-entries/trial-balance` | Trial balance report | `AllStaff` |
| `GET` | `/api/v1/journal-entries/account-statement` | Account statement | `AllStaff` |
| `GET` | `/api/v1/journal-entries/account-balance/{accountId}` | Account balance | `AllStaff` |
| `GET` | `/api/v1/journal-entries/ledger/{accountId}` | Account ledger | `AllStaff` |
| `GET` | `/api/v1/journal-entries/{id}/attachment` | Download attachment | `AllStaff` |

**Controller purity** (RULE-203): Controller injects `IJournalEntryService` only — NO `DbContext` or `IUnitOfWork`.

**Logging**: All critical endpoints log via Serilog (RULE-035/036).

**Estimate**: ~2 hours

---

### Task 14 — FluentValidation (RULE-044)

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/Accounting/CreateJournalEntryRequestValidator.cs` | **CREATE** |
| `Api/Validators/Accounting/PostJournalEntryRequestValidator.cs` | **CREATE** |
| `Api/Validators/Accounting/ReverseJournalEntryRequestValidator.cs` | **CREATE** |
| `Api/Validators/Accounting/CreateOpeningEntryRequestValidator.cs` | **CREATE** |
| `Api/Validators/Accounting/CreateAnnualClosingRequestValidator.cs` | **CREATE** |

**CreateJournalEntryRequestValidator**:
> See `docs/AGENTS.md` §2.13 for the Validation (4 Layers) strategy and `Api/Validators/` for canonical validator patterns.

**Estimate**: ~45 minutes

---

### Task 15 — Reversal Entry Support

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `Reverse()` method (already drafted in Task 4) |
| `Domain/Accounting/Entities/JournalEntry.cs` | Add `CreateReversalEntry()` static factory method |
| `Application/Accounting/Services/JournalEntryService.cs` | Implement `ReverseEntryAsync()` — creates reversal entry + marks original as reversed |
| `Contracts/Requests/AccountingRequests.cs` | Add `ReverseJournalEntryRequest` |
| `Contracts/DTOs/AllDtos.cs` | Add `ReversalEntryId` to `JournalEntryDto` |

**Reversal logic**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns and transaction strategy (§2.65).

**CreateReversalEntry**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Logging**: `Log.Information("Journal entry {EntryId} reversed by User {UserId}. Reversal: {ReversalNumber}", ...)`

**Estimate**: ~1 hour

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | JournalEntryLine.Debit/Credit = `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | Annual closing, auto-entry creation, reversal — all use `BeginTransactionAsync` | ✅ |
| **RULE-006** | ALL services return `Result<T>` | JournalEntryService, all auto-entry providers | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All JournalEntry text fields | ✅ |
| **RULE-016** | BaseEntity audit fields | JournalEntry inherits BaseEntity | ✅ |
| **RULE-022** | Controllers delegate to Services | JournalEntriesController injects IJournalEntryService only | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | JournalEntryService uses IUnitOfWork | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on entry creation, posting, reversal | ✅ |
| **RULE-036** | Log critical operations | Entry creation, posting, reversal, annual closing, opening entry | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | JournalEntriesController — all endpoints | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | JournalEntry: `Create()`, `AddDebitLine()`, `AddCreditLine()`, `ValidateAndPost()`, `Reverse()` | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | All 5 request validators (Task 14) | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | JournalEntry uses soft delete (`IsActive`) only — no permanent delete | ✅ |
| **RULE-052** | Guard Clauses on all entities | JournalEntry.Create, JournalEntryLine.CreateDebit/CreateCredit — Arabic DomainException | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | JournalEntryEditorViewModel, OpeningEntryViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | All editor ViewModels — no CanExecute blocking | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModels use ExecuteAsync() | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern — no MediatR dependency | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | All editors open via `OpenScreen()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ القيد"`, `"خطأ في ترحيل القيد"`, `"خطأ في إقفال السنة"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, JSON parse crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, unbalanced entries, "not found" | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | JournalEntriesApiService — content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, MenuItems, inputs across all XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "ترحيل القيد — سيتم تحديث أرصدة الحسابات" ✅, not "ترحيل" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل القيد — سيتم تحديث أرصدة الحسابات"; Reverse: "عكس القيد — إنشاء قيد عكسي" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "القيود اليومية — إدارة وعرض القيود المحاسبية" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول قيد — تسجيل معاملة محاسبية جديدة" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | JournalEntry only uses soft delete — N/A | ✅ N/A |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | JournalEntryService, all provider methods | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | JournalEntriesController — service only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | JournalEntryLine → Account: Restrict ✅; JournalEntryLine → JournalEntry: **Cascade** ⚠️ (see note) | ⚠️ |
| **RULE-220** | Newest-first sorting on lists | JournalEntry list: OrderByDescending(TransactionDate) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | JournalEntryEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor ViewModels | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in editor ViewModels | ✅ |
| **RULE-240** | Login endpoint rate limited | Not in scope | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not affected by this phase | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | All dialog windows | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All section headers | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state views | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | All screen windows | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

> **⚠️ Note on RULE-214**: The JournalEntryLine → JournalEntry FK intentionally uses `DeleteBehavior.Cascade` because JournalEntryLines are **compositionally owned** by their parent entry. The parent entry itself is never hard-deleted (soft delete only via `IsActive`), so Cascade is safe. This is a documented exception.

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Currency module not ready (Phase 20 dependency)** | **HIGH** — blocks CurrencyId/ExchangeRate on JournalEntry | Make both fields nullable; add fallback to `DefaultCurrencyId` system setting; implement working without currency for V1 |
| **Account.IsLeafAccount missing** | **MEDIUM** — block validation of leaf-only posting | Add migration with default `true`; parent accounts can be manually flagged |
| **Auto-entry providers refactor breaks existing sales/purchase posting** | **HIGH** — existing working features could break | Write comprehensive tests BEFORE refactoring; keep old code path until new path is verified (feature toggle) |
| **Annual closing creates irreversible data change** | **HIGH** — accidental year closure | Add confirmation dialog with checklist; store closed year in `SystemSettings`; allow re-opening year within 24h (grace period) |
| **Draft vs Posted confusion in existing code** | **MEDIUM** — current JournalEntryService always posts immediately | Add `IsDraft` flag with default `true`; migrate existing entries to `IsDraft = false, IsPosted = true` |
| **Large trial balance query performance** | **LOW** — thousands of accounts × millions of lines | Add indexes on `(AccountId, TransactionDate)`, `(IsPosted, IsReversed)`; implement date-range pagination |
| **Attachment storage disk space** | **LOW** — unlimited file uploads | Max file size 5MB; accept only PDF/JPEG/PNG; periodic cleanup of orphaned attachments |
| **Multi-currency rounding errors** | **LOW** — debit/credit in base currency | Use `decimal(18,2)` with banker's rounding; always store both original and base amounts |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| CurrencyId/ExchangeRate migration causes issues | `ALTER TABLE JournalEntries DROP COLUMN CurrencyId; ALTER TABLE JournalEntries DROP COLUMN ExchangeRate;` |
| Attachment columns cause issues | `ALTER TABLE JournalEntries DROP COLUMN AttachmentPath; ALTER TABLE JournalEntries DROP COLUMN AttachmentFileName;` |
| Draft/Reversal columns cause issues | `ALTER TABLE JournalEntries DROP COLUMN IsDraft; ALTER TABLE JournalEntries DROP COLUMN ReversedById; ALTER TABLE JournalEntries DROP COLUMN ReversedAt; ALTER TABLE JournalEntries DROP COLUMN ReversalReason;` |
| IsLeafAccount on Account entity causes issues | `ALTER TABLE Accounts DROP COLUMN IsLeafAccount;` |
| Annual closing creates incorrect entries | Manual reversal: create offsetting entry via UI; update `ClosedFiscalYear` setting |
| Auto-entry orchestrator breaks existing posting | Revert provider pattern; restore old inline code; remove orchestrator DI registration |
| JournalEntryLineConfiguration migration fails | Remove new config file; revert to implicit EF Core conventions |
| Desktop screens not needed | Remove DI registration + navigation entries — no data impact |
| Trial balance report incorrect | Query validation against raw SQL; fix aggregation logic |
| All new features need rollback | `git revert` the Phase 30 branch; run down-migration for all new columns |

---

### Task 16 — Unit Tests

**Scope**: Comprehensive test coverage for all Phase 30 components. Every test category below must be implemented.

#### 16.1 Domain Entity Tests

**File**: `Tests/Domain.Tests/Accounting/JournalEntryTests.cs`
**File**: `Tests/Domain.Tests/Accounting/JournalEntryLineTests.cs`
**File**: `Tests/Domain.Tests/Accounting/FiscalYearTests.cs`

Test every `Create()` factory method:
- Valid input → creates entity correctly
- Missing required fields → `DomainException` with Arabic message
- Boundary values → correct validation (zero amounts, max-length strings, null optional fields)

**JournalEntry.Create tests:**
| Test | Expected |
|------|----------|
| Valid entry with all fields | EntryNumber, TransactionDate, EntryType set |
| Missing entry number → DomainException | `"رقم القيد مطلوب"` |
| Future transaction date accepted | Creates successfully |
| With optional CurrencyId + ExchangeRate | Fields set correctly |
| With null ExchangeRate when CurrencyId set → DomainException | `"يرجى إدخال سعر الصرف للعملة المحددة"` |
| ExchangeRate ≤ 0 → DomainException | `"سعر الصرف يجب أن يكون أكبر من صفر"` |
| Max-length description (501 chars) → DomainException | Truncation or rejection |

**JournalEntryLine.CreateDebit / CreateCredit tests:**
| Test | Expected |
|------|----------|
| Valid debit line | AccountId, Debit set; Credit = 0 |
| Valid credit line | AccountId, Credit set; Debit = 0 |
| Both Debit and Credit > 0 → DomainException | `"لا يمكن أن يحتوي البند على خصم وإيداع معاً"` |
| Both Debit and Credit = 0 → DomainException | `"يجب أن يكون للبند قيمة خصم أو إيداع"` |
| Negative Debit → DomainException | `"قيمة الخصم لا يمكن أن تكون سالبة"` |
| Negative Credit → DomainException | `"قيمة الإيداع لا يمكن أن تكون سالبة"` |
| Missing AccountId (0) → DomainException | `"الحساب مطلوب"` |

**JournalEntry 3-state lifecycle tests:**
| Test | Expected |
|------|----------|
| New entry is Draft | IsDraft=true, IsPosted=false, IsReversed=false |
| Post() transitions Draft→Posted | IsDraft=false, IsPosted=true |
| Post() on already posted → DomainException | `"لا يمكن ترحيل قيد تم ترحيله بالفعل"` |
| Reverse() from Draft | IsDraft=false, IsReversed=true |
| Reverse() from Posted | IsPosted=false, IsReversed=true |
| Reverse() on already reversed → DomainException | `"لا يمكن عكس قيد تم عكسه بالفعل"` |
| Reversed entry is terminal | No state change allowed |

**JournalEntryLine balance tests:**
| Test | Expected |
|------|----------|
| Post() validates Sum(Debit) == Sum(Credit) | Passes if balanced |
| Post() with unbalanced lines → DomainException | `"القيد غير متوازن"` |
| Single line (1 debit, 0 credit) → fails | Unbalanced |
| Multiple lines summing to same total | Passes |

**FiscalYear.Create tests:**
| Test | Expected |
|------|----------|
| Valid name, start < end | Created successfully |
| Empty name → DomainException | `"اسم السنة المالية مطلوب"` |
| Start >= End → DomainException | `"تاريخ البداية يجب أن يكون قبل تاريخ النهاية"` |
| IsDateInFiscalYear(date inside range) | Returns true |
| IsDateInFiscalYear(date before start) | Returns false |
| IsDateInFiscalYear(date after end) | Returns false |
| Close(userId) | IsClosed=true, ClosedAt set, ClosedByUserId set |
| IsClosed prevents new entries | Validate in service test |

#### 16.2 Service Tests (using Mock<IUnitOfWork>)

**File**: `Tests/Application.Tests/Accounting/JournalEntryServiceTests.cs`

**CreateJournalEntryAsync tests:**
| Test | Expected |
|------|----------|
| Valid request with 2+ balanced lines → Success | Returns Result<int>.Success with entry ID |
| Unbalanced lines → Failure | Result<int>.Failure with Arabic message |
| Lines < 2 → Failure | Result<int>.Failure with Arabic message |
| TransactionDate in closed FiscalYear → Failure | `"السنة المالية مغلقة — لا يمكن إضافة قيود"` |
| Account not found → Failure | `"الحساب غير موجود"` |
| Account inactive → Failure | `"الحساب غير نشط"` |
| Account is not leaf → Failure | `"الحساب هو حساب رئيسي ولا يقبل الترحيل المباشر"` |
| CurrencyId without ExchangeRate → Failure | `"يرجى إدخال سعر الصرف"` |
| Transaction rollback on failure | Verify _uow.RollbackAsync called |
| Success logs via Serilog | Verify Log.Information called |

**PostEntryAsync tests:**
| Test | Expected |
|------|----------|
| Post a valid draft entry → Success | Entry posted, lines validated balanced |
| Post an already posted entry → Failure | `"لا يمكن ترحيل قيد تم ترحيله بالفعل"` |
| Post a reversed entry → Failure | `"لا يمكن ترحيل قيد ملغي"` |
| Post with unbalanced lines → Failure | `"القيد غير متوازن"` |

**ReverseEntryAsync tests:**
| Test | Expected |
|------|----------|
| Reverse a posted entry → Success | Reversal entry created, original marked reversed |
| Reverse a draft entry → Success | Entry marked reversed (no reversal entry needed) |
| Reverse an already reversed entry → Failure | `"لا يمكن عكس قيد تم عكسه بالفعل"` |
| Reversal entry is auto-posted | IsPosted = true on reversal |
| Transaction rollback on reversal failure | Verify RollbackAsync called |

**CreateOpeningEntryAsync tests:**
| Test | Expected |
|------|----------|
| Valid balanced opening entry → Success | Entry created with EntryType=OpeningBalance, IsPosted=true |
| Duplicate opening entry for same year → Failure | `"يوجد قيد افتتاحي لهذه السنة بالفعل"` |
| Unbalanced opening balances → Failure | `"الأرصدة الافتتاحية غير متوازنة"` |
| Revenue/Expense accounts not allowed → Failure | Only asset/liability/equity accepted |

**CreateAnnualClosingAsync tests:**
| Test | Expected |
|------|----------|
| Valid year with all entries posted → Success | Closing entry created, year marked closed |
| Year already closed → Failure | `"السنة المالية مغلقة بالفعل"` |
| Unposted drafts exist → Failure | `"لا يمكن إقفال السنة —存在 قيود غير مرحّلة"` |
| Trial balance unbalanced → Failure | `"ميزان المراجعة غير متوازن"` |

#### 16.3 Auto-Entry Provider Tests

**File**: `Tests/Application.Tests/Accounting/AutoEntryProviders/`
- `SalesAutoEntryProviderTests.cs`
- `PurchaseAutoEntryProviderTests.cs`
- `PaymentAutoEntryProviderTests.cs`
- `CashAutoEntryProviderTests.cs`
- `ReturnAutoEntryProviderTests.cs`
- `TransferAutoEntryProviderTests.cs`

**Common tests for each provider:**
| Test | Expected |
|------|----------|
| Valid referenceId → returns List<JournalEntryLineRequest> with 2+ lines | Lines are balanced (Sum(Debit) == Sum(Credit)) |
| ReferenceId not found → Failure | `"المرجع غير موجود"` |
| System account mapping missing → Failure | `"لم يتم تعيين حساب {AccountType} في إعدادات النظام"` |
| All 7 providers inject ISystemAccountService | Constructor injection verified in test setup |

**SalesAutoEntryProvider-specific:**
| Test | Expected |
|------|----------|
| Cash sale → CashOnHand debited, SalesRevenue credited | Lines match expected pattern |
| Credit sale → AR debited, SalesRevenue credited | Lines match expected pattern |
| COGS line included | Inventory credited, COGS debited |

**PurchaseAutoEntryProvider-specific:**
| Test | Expected |
|------|----------|
| Cash purchase → Inventory debited, CashOnHand credited | Lines match expected pattern |
| Credit purchase → Inventory debited, AP credited | Lines match expected pattern |

#### 16.4 FluentValidation Tests

**File**: `Tests/Api.Tests/Validators/Accounting/`
- `CreateJournalEntryRequestValidatorTests.cs`
- `PostJournalEntryRequestValidatorTests.cs`
- `ReverseJournalEntryRequestValidatorTests.cs`
- `CreateOpeningEntryRequestValidatorTests.cs`
- `CreateAnnualClosingRequestValidatorTests.cs`

**Pattern for each validator:**
| Test | Expected |
|------|----------|
| Valid request passes | IsValid = true |
| Missing TransactionDate → error | Arabic message for "تاريخ القيد مطلوب" |
| Invalid EntryType → error | Arabic message for "نوع القيد غير صالح" |
| Lines null/empty → error | Arabic message for "يجب إضافة بنود القيد" |
| Less than 2 lines → error | Arabic message for "يجب إضافة بندين على الأقل" |
| Unbalanced lines → error | Arabic message for "القيد غير متوازن" |
| Per-line: AccountId missing → error | Arabic message for "الحساب مطلوب" |
| Per-line: Both debit/credit zero → error | Arabic message for "يجب أن يكون للبند قيمة" |
| Per-line: Both debit/credit > 0 → error | Arabic message for "لا يمكن أن يحتوي البند على خصم وإيداع معاً" |
| CurrencyId with null ExchangeRate → error | Arabic message for "سعر الصرف مطلوب" |

#### 16.5 Database Configuration Tests

**File**: `Tests/Infrastructure.Tests/Data/Configurations/`
- `JournalEntryConfigurationTests.cs`
- `JournalEntryLineConfigurationTests.cs`
- `FiscalYearConfigurationTests.cs`

| Test | Expected |
|------|----------|
| JournalEntry mapped to "JournalEntries" table | ToTable("JournalEntries") |
| EntryNumber has MaxLength(50), IsRequired | HasMaxLength(50), IsRequired true |
| TransactionDate IsRequired | IsRequired true |
| CurrencyId FK → Currencies with Restrict | OnDelete(DeleteBehavior.Restrict) |
| HasIndex(EntryNumber).IsUnique() | Unique index exists |
| HasIndex(TransactionDate) | Non-unique index exists |
| HasQueryFilter(IsActive) | Query filter applied |
| JournalEntryLine mapped to "JournalEntryLines" table | ToTable("JournalEntryLines") |
| Debit/Credit HasPrecision(18,2) | HasPrecision(18,2) |
| AccountCode MaxLength(50), IsRequired | HasMaxLength(50), IsRequired true |
| AccountNameAr MaxLength(200), IsRequired | HasMaxLength(200), IsRequired true |
| FK journalentryline → Account with Restrict | OnDelete(DeleteBehavior.Restrict) |
| FK journalentryline → JournalEntry with Cascade | OnDelete(DeleteBehavior.Cascade) — documented exception |
| FiscalYear mapped to "FiscalYears" table | ToTable("FiscalYears") |
| FiscalYear unique index on Name | HasIndex(f => f.Name).IsUnique() |

**Cascade→Restrict explicit delete test:**
| Test | Expected |
|------|----------|
| Delete JournalEntry → lines cascade deleted | Entry removed, lines removed |
| Delete Account referenced by line → DbUpdateException | FK Restrict prevents deletion |

#### 16.6 Desktop ViewModel Tests

**File**: `Tests/Desktop.Tests/ViewModels/Accounting/`
- `JournalEntryEditorViewModelTests.cs`
- `JournalEntryListViewModelTests.cs`

| Test | Expected |
|------|----------|
| LoadData operation succeeds → Items populated | ObservableCollection has entries |
| LoadData with API failure → ErrorMessage set | `"فشل في تحميل القيود"` |
| SaveDraft with validation passing → SaveDraftAsync called on service | Verify service call |
| SaveDraft with validation failing → warning dialog shown | IDialogService.ShowWarningAsync called |
| Post command triggers PostAsync | Verify service.PostEntryAsync called |
| Post with unbalanced → warning dialog | `"القيد غير متوازن"` in dialog |
| Status computed property shows correct Arabic text | Draft→"مسودة", Posted→"مرحل", Reversed→"ملغي" |
| TotalDebit/TotalCredit computed from lines | Sum matches |
| IsBalanced computed from totals | True when |debit - credit| < 0.001m |
| SetDialogService called in constructor | Verified |
| Dispose unsubscribes EventBus | _subscription.Dispose called |

#### 16.7 SystemAccountMappings Tests

**File**: `Tests/Application.Tests/Accounting/Services/SystemAccountServiceTests.cs`

| Test | Expected |
|------|----------|
| All 13 system accounts resolve correctly | GetAccountIdAsync returns valid int for each type |
| Missing mapping → Failure | `"لم يتم تعيين حساب {AccountType} في إعدادات النظام"` |
| Mapping cached after first resolution | Second call uses cache, not DB |

**13 system accounts verified:**
| # | Enum Value | Expect AccountId > 0 |
|---|-----------|---------------------|
| 1 | CashOnHand | ✅ |
| 2 | SalesRevenue | ✅ |
| 3 | CostOfGoodsSold | ✅ |
| 4 | Inventory | ✅ |
| 5 | AccountsReceivable | ✅ |
| 6 | AccountsPayable | ✅ |
| 7 | VATPayable | ✅ |
| 8 | VATReceivable | ✅ |
| 9 | DiscountAllowed | ✅ |
| 10 | DiscountReceived | ✅ |
| 11 | FreightIn | ✅ |
| 12 | PurchaseReturns | ✅ |
| 13 | SalesReturns | ✅ |

#### 16.8 JournalEntryType Enum Integrity

**File**: `Tests/Domain.Tests/Accounting/JournalEntryTypeTests.cs`

| Test | Expected |
|------|----------|
| Enum value 1 = Sales | (byte)JournalEntryType.Sales == 1 |
| Enum value 2 = SalesReturn | (byte)JournalEntryType.SalesReturn == 2 |
| Enum value 3 = Purchase | (byte)JournalEntryType.Purchase == 3 |
| Enum value 4 = PurchaseReturn | (byte)JournalEntryType.PurchaseReturn == 4 |
| Enum value 5 = Expense | (byte)JournalEntryType.Expense == 5 |
| Enum value 6 = StockWriteOff | (byte)JournalEntryType.StockWriteOff == 6 |
| Enum value 7 = Transfer | (byte)JournalEntryType.Transfer == 7 |
| Enum value 8 = Manual | (byte)JournalEntryType.Manual == 8 |
| Enum value 9 = OpeningBalance | (byte)JournalEntryType.OpeningBalance == 9 |
| Enum value 10 = AnnualClosing | (byte)JournalEntryType.AnnualClosing == 10 |
| Enum value 11 = Reversal | (byte)JournalEntryType.Reversal == 11 |

**Estimate**: ~10 hours
