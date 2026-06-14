# Phase 20 — Currencies Module Implementation Plan

> **Version**: 2.0 — Rewritten to align with database schema v2.3-2.4 and current codebase state
> **Status**: Core implementation complete (entity, config, service, controller, desktop UI, seed data exist)
> **Scope**: Multi-currency support — Currencies CRUD + CurrencyRates (exchange rate history with effective date ranges)

---

## 1. Summary

The Currencies Module provides multi-currency support across the entire system. The system always ships with two currencies: the **local currency** (e.g., YER for Yemen) and the **US Dollar (USD)**. The **base currency** is set at system creation and **cannot be changed afterward** — it is protected by IsSystem and DB-level constraints. Users may add additional currencies (e.g., SAR for Saudi Riyal) and manage exchange rates via the CurrencyRates sub-module, which tracks rate history with effective-from/to date ranges.

The module consists of two tables:
- **Currencies** — master list of currencies (smallint PK, char(3) ISO 4217 Code)
- **CurrencyRates** — exchange rate history with effective date ranges (RateToBase meaning: how many base currency units = 1 unit of this currency)

All financial entities (invoices, payments, cash boxes, journal entries) carry a `CurrencyId` FK (nullable — null means base currency) and an `ExchangeRate` field to freeze the rate at transaction time.

---

## 2. Key Entities

### 2.1 Currency (Currencies table)

| Column | Type | Notes |
|--------|------|-------|
| Id | smallint PK | Small lookup table — overrides base `int Id` |
| Name | nvarchar(100) not null | Unique, filtered `[IsActive]=1` |
| Code | char(3) not null | ISO 4217 standard — exactly 3 characters. Unique, filtered `[IsActive]=1` |
| FractionName | nvarchar(50) not null | e.g., "فلس", "سنت", "هللة" |
| Symbol | nvarchar(20) nullable | Display symbol: ﷼, $, € |
| DecimalPlaces | tinyint not null default 2 | 0-4 range, determines display precision |
| IsBaseCurrency | bit not null default 0 | Only one allowed — filtered unique index `IsBaseCurrency=1 AND IsActive=1` |
| IsSystem | bit not null default 0 | Protects system-seeded currencies from deletion |
| IsActive | bit not null default 1 | Soft-delete query filter |

**Design decisions:**
- **Base currency is non-changeable**: Once set at system creation via seed data, no user can toggle `IsBaseCurrency`. This protects accounting integrity — changing the base currency would invalidate historical reports, inventory valuations, and journal entries.
- **IsSystem guard**: Currencies seeded by the system (local currency + USD) have `IsSystem=true`. Both soft-delete (`MarkAsDeleted()`) and permanent-delete reject attempts with a DomainException. Only custom-added currencies (IsSystem=false) can be deleted.
- **DB-level protection**: Filtered unique index `WHERE IsBaseCurrency = 1 AND IsActive = 1` ensures only one active base currency exists. A soft-deleted base currency does not block a new one.
- **What users CAN edit**: Currency name, symbol, fraction name, decimal places, and exchange rate. They CANNOT change Code (ISO 4217 is fixed) or IsBaseCurrency.
- **Code validation**: Code must be exactly 3 characters (ISO 4217). Domain entity validates `code.Trim().Length != 3` and throws DomainException with Arabic message.

### 2.2 CurrencyRate (CurrencyRates table)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| CurrencyId | smallint FK not null | DeleteBehavior.Restrict |
| RateToBase | decimal(18,6) not null | CHK > 0 — how many base currency units = 1 of this currency |
| EffectiveFrom | datetime2 not null | Start date for this rate |
| EffectiveTo | datetime2 nullable | End date — null means currently active |
| Index | (CurrencyId, EffectiveFrom) | Composite for fast lookup by date |

**Rate interpretation**: RateToBase = amount of base currency equal to 1 unit of this currency. If YER is base and USD RateToBase = 550, then 1 USD = 550 YER. Conversion formula: `AmountInBaseCurrency = AmountInForeign × RateToBase`.

**Full history vs. single rate**: The Currency entity itself carries an `ExchangeRateToBase` field for the **current/active rate** (convenience for quick lookups). The CurrencyRates table stores the **full history** with effective date ranges, enabling point-in-time rate lookups and trend analysis.

### 2.3 ExchangeRateHistory (Separate Audit Trail)

A third entity `ExchangeRateHistory` exists as a dedicated **audit trail** recording the old rate, new rate, who changed it, and why. Every `UpdateExchangeRate()` call on a Currency record:
1. Creates an `ExchangeRateHistory` entry (OldRate, NewRate, EffectiveDate, RateType, Notes, ChangedByUserId)
2. Updates the `Currency.ExchangeRateToBase` field

This gives auditors a clear before/after view of every rate change, separate from the date-range-based CurrencyRates table which is designed for application-level rate lookups.

---

## 3. Business Rules

### Delete Protection
- **System currencies** (IsSystem=true) cannot be soft-deleted or permanently deleted. The service returns `Result.Failure("لا يمكن حذف عملة النظام")`.
- **Non-system currencies** can be soft-deleted (sets IsActive=false) or permanently deleted (FK constraint catch returns `Result.Failure("لا يمكن حذف العملة لأنها مرتبطة بفواتير أو مدفوعات")`).
- **Permanent delete** catches `DbUpdateException` at the service level and returns a friendly Arabic message.

### Base Currency Immutability
- The seed data determines the base currency. No user-facing toggle exists.
- `Currency.Create()` accepts `isBaseCurrency` but it is only used during seeding. The Desktop editor does not expose this field.
- DB-level filtered unique index enforces the single-base-currency invariant regardless of application logic.

### Rate Change Audit Trail
- Every rate change on `Currency.ExchangeRateToBase` MUST create an `ExchangeRateHistory` record with OldRate, NewRate, ChangedByUserId, and a reason.
- The CurrencyRates table is the primary data source for application-level rate lookups by date.
- Rate history is returned newest-first (sorted by EffectiveDate DESC, then Id DESC).

### Unique Code Constraint
- Currency.Code is `char(3)` — ISO 4217 standard. Domain validation enforces exactly 3 characters.
- Unique filtered index `WHERE [IsActive] = 1` prevents active duplicate codes while allowing soft-deleted records to coexist.
- Currency.Name also has a unique filtered index for display-name uniqueness.

---

## 4. Seed Data

The `DbSeeder` seeds exactly 3 currencies (only when the Currencies table is empty):

| Name | Code | Symbol | RateToBase | IsBaseCurrency | FractionName | IsSystem |
|------|------|--------|-----------|----------------|-------------|----------|
| ريال يمني | YER | ﷼ | 1.0 | ✅ (true) | فلس | ✅ |
| دولار أمريكي | USD | $ | 550.0 | ❌ | سنت | ✅ |
| ريال سعودي | SAR | ﷼ | 71.4 | ❌ | هللة | ✅ |

**YER** is the default base currency (targeting the Yemeni market). **USD** and **SAR** are seeded as secondary currencies with `IsSystem=true` to protect them from accidental deletion. The rate values (550 for USD, 71.4 for SAR) are examples — the user can update them via the exchange rate editor. The seed block uses an `AnyAsync()` guard and only seeds when the table is empty.

---

## 5. UI Screens

### 5.1 Currencies List Screen
Displays a DataGrid with columns: Code (bold), Name, Symbol, FractionName, DecimalPlaces, ExchangeRateToBase, IsBaseCurrency (green badge "الأساسية"). Actions per row: Edit, Delete (soft), Exchange Rate (opens rate editor), Rate History (opens read-only history view). Newest-first sort by Id DESC. Empty state shows "➕ إضافة أول عملة" button with ToolTip.

### 5.2 Currency Editor Screen
Non-modal editor (via ScreenWindowService). Fields: Name (required), Code (required, 3 chars, read-only on edit), Symbol (required), FractionName, DecimalPlaces (0-4), ExchangeRateToBase (required, > 0). IsBaseCurrency and IsSystem are NOT shown to the user — they are set only via seed data. INotifyDataErrorInfo real-time validation. Save always enabled — validation shows styled warning dialog on click listing all errors.

### 5.3 CurrencyRates List Screen
Displays a DataGrid read-only with columns: CurrencyCode, RateToBase, EffectiveFrom, EffectiveTo, CreatedAt. Sorted newest-first by EffectiveFrom DESC. Accessible from the Currencies list as a sub-view or via button on each row.

### 5.4 Exchange Rate Editor
Inline or popup editor accessible from the Currencies list. Fields: NewRate (required, > 0), EffectiveFrom (defaults to today), EffectiveTo (optional), Notes. On save, the system: creates ExchangeRateHistory audit entry, updates Currency.ExchangeRateToBase, and creates a CurrencyRate record with the effective date range.

---

## 6. API Endpoints

All endpoints require `[Authorize]`. GET endpoints use `AllStaff` policy (read-only for all roles). Write endpoints (POST, PUT, DELETE) use `ManagerAndAbove` policy. All responses use `Result<T>` — 404 for "not found", 400 for business validation errors, 200/201 for success.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/currencies` | List all (with optional `includeInactive` parameter) |
| GET | `/api/v1/currencies/{id:int}` | Get by Id |
| GET | `/api/v1/currencies/by-code/{code}` | Get by ISO code |
| GET | `/api/v1/currencies/base` | Get the active base currency |
| GET | `/api/v1/currencies/{id:int}/history` | Get exchange rate history for a currency |
| POST | `/api/v1/currencies` | Create new currency |
| PUT | `/api/v1/currencies/{id:int}` | Update currency metadata |
| PUT | `/api/v1/currencies/{id:int}/exchange-rate` | Update exchange rate only (triggers audit) |
| DELETE | `/api/v1/currencies/{id:int}` | Soft delete (deactivate) |
| DELETE | `/api/v1/currencies/permanent/{id:int}` | Hard delete (FK guard) |

---

## 7. Desktop API Service

Desktop communicates with the Currencies API via `ICurrencyApiService` / `CurrencyApiService` (typed HttpClient). Methods: GetAllAsync, GetByIdAsync, GetByCodeAsync, GetBaseCurrencyAsync, GetHistoryAsync, CreateAsync, UpdateAsync, UpdateRateAsync, DeleteAsync, DeletePermanentlyAsync. All responses check ContentType before parsing JSON (prevents crash on HTML error pages). EventBus messages `CurrencyChangedMessage(CurrencyId)` and `ExchangeRateChangedMessage(CurrencyId)` trigger list refresh.

---

## 8. FK Integration Across System

The Currencies module is referenced by the following entities (all FKs use `DeleteBehavior.Restrict`):

| Entity | FK Field | Nullable | Notes |
|--------|----------|----------|-------|
| SalesInvoice | CurrencyId | ✅ | Null = base currency |
| PurchaseInvoice | CurrencyId | ✅ | Null = base currency |
| SalesReturn | CurrencyId | ✅ | |
| PurchaseReturn | CurrencyId | ✅ | |
| CustomerPayment | CurrencyId | ✅ | |
| SupplierPayment | CurrencyId | ✅ | |
| CashBox | CurrencyId | ✅ | |
| CashTransaction | CurrencyId | ✅ | |
| JournalEntry | CurrencyId | ✅ | |
| SalesQuotation | CurrencyId | ✅ | |
| CurrencyRate | CurrencyId | ❌ | smallint FK |
| ExchangeRateHistory | CurrencyId | ❌ | smallint FK |

Each invoice/payment also carries a frozen `ExchangeRate` decimal field to capture the rate at transaction time for historical accuracy. When `CurrencyId` is null, the system assumes base currency (RateToBase = 1.0).

---

## 9. Design Decisions

### 9.1 Base Currency Immutability (Why Non-Changeable)
Changing the base currency after transactions exist would corrupt:
- Historical financial reports (all values would need re-computation)
- Inventory valuation (costs stored in base currency)
- Journal entries (reference exchange rates relative to the original base)
- Customer/supplier balances (denormalized CurrentBalance in base currency)

If a user needs a different base currency, they must start a new company database.

### 9.2 smallint PK Instead of int
Currencies is a small lookup table — no system will have more than 32,767 currencies. Using smallint saves space in all FK columns that reference it (invoices, payments, cash boxes, journal entries, currency rates).

### 9.3 char(3) Instead of nvarchar for Code
ISO 4217 currency codes are always exactly 3 ASCII characters. Using `char(3)` enforces this at the database level and saves space compared to `nvarchar(3)`.

### 9.4 RateToBase Interpretation (Direct Quote)
RateToBase uses a **direct quote** convention: "how many base currency units for 1 unit of this currency."
- Base currency (YER): RateToBase = 1.0
- USD: RateToBase = 550 (meaning 1 USD = 550 YER)
- Conversion: AmountInBase = AmountInForeign × RateToBase

This is the most intuitive convention for users — "USD = 550" clearly means "1 dollar equals 550 riyals."

### 9.5 decimal(18,6) for Exchange Rates
Exchange rates use 6 decimal places of precision (not the standard 2 for money) because:
- Small currencies may need high precision (e.g., 1 IRR = 0.000023 USD)
- Rate fluctuations at 4+ decimal places are common in retail FX
- Money fields on invoices/payments remain decimal(18,2) — only the rate itself uses 18,6

### 9.6 No Multi-Currency Accounts in V1
Each account (CashBox, Customer, Supplier) does NOT have its own currency. The currency is stored on the transaction (invoice, payment, journal entry), not on the account. This simplifies the Chart of Accounts — one account can hold transactions in any currency, with rates converting to base for reporting.

### 9.7 CreditLimit Not Part of Currencies
The credit limit feature (سقف الحساب) that appeared in the reference app's currency section is a Customer/Supplier responsibility. Credit limits are per-entity, not per-currency. Our codebase correctly places `CreditLimit` on Customer and Supplier entities.

---

## 10. Implementation Tasks

The core implementation already exists. The following tasks represent verification, refinement, and any remaining work:

| # | Task | Description | Status |
|---|------|-------------|--------|
| 1 | Domain entity review | Verify Currency entity has: smallint Id override, char(3) Code validation, IsSystem guard, FractionName max 50, DecimalPlaces 0-4. Verify CurrencyRate entity has RateToBase, EffectiveFrom/To, CHK constraints. | ✅ Done |
| 2 | EF Core config review | Verify: decimal(18,6) precision on rate fields, unique filtered indexes (Name, Code, IsBaseCurrency), HASFILTER on all unique indexes (`[IsActive]=1`), FK Restrict on CurrencyRate.CurrencyId. | ✅ Done |
| 3 | Seeder data review | Verify 3 currencies seeded (local, USD, SAR) with correct IsSystem=true, only one IsBaseCurrency=true, AnyAsync guard. | ✅ Done |
| 4 | Service layer review | Verify: Result<T> pattern, IsSystem guard on both soft and hard delete, DbUpdateException catch on permanent delete, ExchangeRateHistory audit on every rate change. | ✅ Done |
| 5 | Controller review | Verify: AllStaff on GET, ManagerAndAbove on POST/PUT/DELETE, 404 vs 400 differentiation, GetUserId() from JWT claims. | ✅ Done |
| 6 | Desktop API service review | Verify ContentType check, Arabic error handling, EventBus integration. | ✅ Done |
| 7 | Desktop ViewModels review | Verify: INotifyDataErrorInfo, ExecuteAsync wrapper, SetDialogService(), ValidateAllAsync(), newest-first sort, no CanExecute predicates. | ✅ Done |
| 8 | FK integration verification | Verify CurrencyId FK exists on all 10+ entities with DeleteBehavior.Restrict. Verify ExchangeRate decimal(18,6) on invoice/payment entities. | ✅ Done |
| 9 | Rate change audit | Verify ExchangeRateHistory created on every rate change with OldRate, NewRate, ChangedByUserId. | ✅ Done |
| 10 | Integration tests | Verify build: 0 errors, 0 warnings. Verify all existing tests pass. | 📝 Pending |

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Two base currencies after seed | High — violates system invariant | Filtered unique index `WHERE IsBaseCurrency = 1 AND IsActive = 1` enforces at DB level |
| Base currency accidentally changed | High — corrupts accounting | No user-facing toggle for IsBaseCurrency. Only seed data sets it. |
| Currency permanently deleted while referenced | Medium — FK exception | Service catches DbUpdateException → Result.Failure with Arabic message |
| Exchange rate precision loss | Medium — rounding errors in reports | decimal(18,6) on rate fields, explicit decimal(18,2) on money fields |
| DateTime vs DateOnly confusion on EffectiveFrom | Medium — timezone inconsistency | CurrencyRate uses DateTime (timezone-aware), ExchangeRateHistory uses DateOnly (date-only) |
| Old StoreSettings.CurrencyCode still referenced | Medium — conflicting base currency source | StoreSettings.CurrencyCode is deprecated in favor of Currency.IsBaseCurrency lookup |
