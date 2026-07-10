# Phase 20 — Currencies Module: Implementation Status & Refactoring Summary

> **Version**: 3.0 — Updated to reflect ACTUAL implementation status after v4.6.9 refactoring
> **Status**: ✅ **Core module fully implemented** — v4.6.9 refactoring (Phase 20 + Phase 18 remediations) complete
> **Current Build**: 0 errors, 0 warnings across all 6 projects

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Implementation Status](#2-implementation-status)
3. [What Was Already Built (Pre-v4.6.9)](#3-what-was-already-built-pre-v469)
4. [v4.6.8 Stabilization Fixes](#4-v468-stabilization-fixes)
5. [v4.6.9 Refactoring Changes](#5-v469-refactoring-changes)
6. [Design Catalog](#6-design-catalog)
7. [Key Architecture Decisions](#7-key-architecture-decisions)
8. [Sidebar Navigation](#8-sidebar-navigation)
9. [Permissions](#9-permissions)
10. [Compliance Matrix](#10-compliance-matrix)
11. [Non-V1 Items (Deferred)](#11-non-v1-items-deferred)
12. [Risks & Mitigations](#12-risks--mitigations)

---

## 1. Architecture Overview

The Currencies module consists of **3 sub-modules**:

| # | Sub-Module | Storage | Status |
|---|------------|---------|--------|
| 💱 | **Currencies CRUD** | `Currency` entity | ✅ **Complete** — Full CRUD with list + editor screens |
| 📊 | **Exchange Rate History** | `ExchangeRateHistory` entity | ✅ **Complete** — Standalone rates screen ("أسعار العملات") |
| 🔗 | **CurrencyId FK Integration** | FK columns on 8+ entities | ✅ **Complete** — All invoice, payment, cashbox entities linked |

### Data Flow

```
Desktop (WPF)
  │  HttpClient (ICurrencyApiService)
  ▼
CurrenciesController (API)
  │  ICurrencyService
  ▼
CurrencyService (Application)
  │  IUnitOfWork + IRepository<Currency>
  ▼
Infrastructure (EF Core + SQL Server)
  └── Currencies table
  └── ExchangeRateHistories table
  └── FK references from 8 entities
```

---

## 2. Implementation Status

### 2.1 Currency Entity ✅ COMPLETE

| Component | Status | File |
|-----------|--------|------|
| `Currency.cs` Domain entity | ✅ Complete | `Domain/Entities/Currency.cs` |
| `CurrencyConfiguration.cs` | ✅ Complete | `Infrastructure/Data/Configurations/CurrencyConfiguration.cs` |
| EF Core Migration | ✅ Complete | Multiple migrations applied |
| Seed data (YER, USD, SAR) | ✅ Complete | `Infrastructure/Data/DbSeeder.cs` |
| `CurrencyDto` | ✅ Complete | `Contracts/DTOs/AllDtos.cs` (lines 578-588) |
| `CreateCurrencyRequest` / `UpdateCurrencyRequest` | ✅ Complete | `Contracts/Requests/CurrencyRequests.cs` |
| `ICurrencyService` / `CurrencyService` | ✅ Complete | `Application/Interfaces/Services/`, `Application/Services/` |
| `CurrenciesController` | ✅ Complete | `Api/Controllers/CurrenciesController.cs` |
| FluentValidators | ✅ Complete | `Api/Validators/CurrencyValidators.cs` |
| `CurrenciesListViewModel` + View | ✅ Complete | DesktopPWF |
| `CurrencyEditorViewModel` + View | ✅ Complete | DesktopPWF |
| `ICurrencyApiService` + HTTP client | ✅ Complete | DesktopPWF |
| DI Registrations | ✅ Complete | `App.xaml.cs`, `Program.cs` |

### 2.2 Exchange Rate History ✅ COMPLETE

| Component | Status | File |
|-----------|--------|------|
| `ExchangeRateHistory.cs` Domain entity | ✅ Complete | `Domain/Entities/ExchangeRateHistory.cs` |
| `ExchangeRateHistoryConfiguration.cs` | ✅ Complete | `Infrastructure/Data/Configurations/ExchangeRateHistoryConfiguration.cs` |
| Audit recording on rate change | ✅ Complete | `CurrencyService.UpdateExchangeRateAsync()` |
| `CurrencyRatesViewModel` + View | ✅ Complete (standalone, not embedded) | DesktopPWF `Views/Currencies/CurrencyRatesView.xaml` |
| Rate history API endpoint | ✅ Complete | `GET /api/v1/currencies/{id}/history` |

### 2.3 CurrencyId FK Integration ✅ COMPLETE

All of the following entities already have `CurrencyId` FK with `DeleteBehavior.Restrict`:

| Entity | CurrencyId Field | ExchangeRate Field |
|--------|-----------------|-------------------|
| `CashBox` | `int CurrencyId` (required) | — |
| `SalesInvoice` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `PurchaseInvoice` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `SalesReturn` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `PurchaseReturn` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `CustomerPayment` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `SupplierPayment` | `int? CurrencyId` | `decimal? ExchangeRate` |
| `CashTransaction` | `int? CurrencyId` | — |

> **Note**: CashBox was already migrated from `string CurrencyCode` to `int CurrencyId` in a prior phase. No `CurrencyCode` string column remains.

### 2.4 StoreSettings.CurrencyCode ⚠️ Deprecated

| Component | Status | Action |
|-----------|--------|--------|
| `StoreSettings.CurrencyCode` (string) | ⚠️ Deprecated | Hidden from UI; system reads `IsBaseCurrency` from Currency table |
| Migration to remove column | ⏳ Deferred | Planned for Phase 25+ |

---

## 3. What Was Already Built (Pre-v4.6.9)

Before the v4.6.9 refactoring, the following was already implemented:

- **Domain**: `Currency.cs` with `Create()`, `Update()`, `UpdateExchangeRate()`, `MarkAsDeleted()` — including `DecimalPlaces` field
- **Infrastructure**: `CurrencyConfiguration.cs`, `ExchangeRateHistoryConfiguration.cs`, `DbSeeder.cs` with YER/USD/SAR seed data, all FK mappings with `DeleteBehavior.Restrict`
- **Contracts**: `CurrencyDto`, `ExchangeRateHistoryDto`, `CreateCurrencyRequest`, `UpdateCurrencyRequest`, `UpdateExchangeRateRequest`
- **API**: `CurrenciesController.cs` with 8+ endpoints, `CurrencyValidators.cs` (Create + Update + UpdateExchangeRate validators)
- **Application**: `CurrencyService.cs` with full CRUD, `ICurrencyService` interface
- **Desktop**: `CurrenciesListViewModel`, `CurrencyEditorViewModel`, `CurrenciesListView`, `CurrencyEditorView`, `ICurrencyApiService`, `CurrencyApiService`, `CurrencyChangedMessage`, DI registrations, sidebar navigation
- **FK Integration**: All 8 entities already had `CurrencyId` FK

### What Was NOT Built (v4.6.9 gaps)

| Gap | Detail |
|-----|--------|
| ❌ Standalone rates screen | Rate history was embedded in CurrencyEditorView.xaml as a sub-DataGrid — not a separate screen |
| ❌ `IsBaseCurrency` in `Update()` | `Currency.Update()` still accepted `isBaseCurrency` param, which was incorrect — base currency should be immutable |
| ❌ `IsBaseCurrency` in `UpdateCurrencyRequest` | The update request allowed changing `IsBaseCurrency` — violated immutability principle |
| ❌ `DecimalPlaces` in `UpdateCurrencyRequestValidator` | Create validator had it, Update validator didn't |
| ❌ `CurrenciesListViewModel.UpdateCurrencyRequest` call | Passed wrong arguments (missing `DecimalPlaces`) |

---

## 4. v4.6.8 Stabilization Fixes

### 4.1 CurrencyService: Removed Manual Transactions

**Problem**: `CurrencyService` used `BeginTransactionAsync()` which crashed with `SqlServerRetryingExecutionStrategy`:
```
System.InvalidOperationException: The configured execution strategy
'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions.
```

**Fix**: Removed all manual `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` calls. Each method now uses a single `SaveChangesAsync()` — EF Core auto-wraps in implicit transaction.

### 4.2 ExchangeRate Precision on Payment Entities

**Problem**: `CustomerPayment.ExchangeRate` and `SupplierPayment.ExchangeRate` (decimal?) lacked `.HasPrecision()` — EF Core defaulted to `decimal(18,2)`, truncating small rates.

**Fix**: Added `.HasPrecision(18, 2)` in `SystemConfigurations.cs`.

### 4.3 CurrencyEditorViewModel ValidateAsync Pattern

**Problem**: Editor VM built its own `List<string>` and called custom dialog — bypassing `INotifyDataErrorInfo` infrastructure.

**Fix**: Replaced with `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` pattern matching `CashBoxEditorViewModel`.

### 4.4 Missing Success Toast

**Problem**: Editor VM had no `IToastNotificationService` — users got no feedback on successful save.

**Fix**: Added `IToastNotificationService` via dual constructor pattern, showing toast before `RequestClose()`.

### 4.5 LogSystemError Misuse

**Problem**: `LogSystemError()` called for API business validation errors (should be Warning level).

**Fix**: Removed `LogSystemError` from business validation else-block — `HandleFailure()` already logs at Warning level.

---

## 5. v4.6.9 Refactoring Changes

These are the changes applied in the current session (June 2026):

### 5.1 Domain: Currency.cs

| Change | Detail |
|--------|--------|
| ❌ Removed `isBaseCurrency` from `Update()` | Base currency is immutable after seed — only `Create()` accepts it |
| ❌ Removed `SetAsBaseCurrency()` / `UnsetBaseCurrency()` | Base currency cannot be toggled from domain layer |
| ✅ `DecimalPlaces` stays | Already existed — validates 0-4 range |

### 5.2 Contracts: CurrencyRequests.cs

| Change | Detail |
|--------|--------|
| ❌ Removed `IsBaseCurrency` from `UpdateCurrencyRequest` | Base currency cannot be changed via update |
| ✅ Kept `IsBaseCurrency` in `CreateCurrencyRequest` | Only create allows setting base currency |
| ✅ `UpdateExchangeRateRequest` unchanged | Only has `NewRate` field |

### 5.3 Application: CurrencyService.cs

| Change | Detail |
|--------|--------|
| 🐛 **BUG-001 FIXED** | Removed broken `existingBase.Update(false)` call in `CreateAsync` — `bool` where `string?` expected (compile error) |
| 🐛 **BUG-002 FIXED** | Removed entire `IsBaseCurrency` toggle block in `UpdateAsync` |
| 🐛 **BUG-003 FIXED** | Fixed `currency.Update()` call — removed `request.IsBaseCurrency` argument |
| ✅ `UpdateAsync()` preserves existing `IsBaseCurrency` value | Update no longer changes base currency status |
| ✅ `CreateAsync()` still supports `IsBaseCurrency` | First-time setting allowed |

### 5.4 API: CurrencyValidators.cs

| Change | Detail |
|--------|--------|
| ✅ Added `DecimalPlaces` validation to `UpdateCurrencyRequestValidator` | `InclusiveBetween(0, 4)` with Arabic message |

### 5.5 Desktop: CurrencyEditorViewModel.cs

| Change | Detail |
|--------|--------|
| ❌ Removed `IsBaseCurrency` editable property | Replaced with read-only `IsBaseCurrencyReadOnly` computed property |
| ❌ Removed `RateHistory` + `LoadRateHistoryAsync()` | Moved to standalone rates screen |
| ✅ Added `DecimalPlaces` property | With validation (0-4), real-time `AddError/ClearErrors` |
| ✅ Added `IsBaseCurrencyReadOnly` | Shows green badge "عملة أساسية" when true |
| ✅ Updated `CreateCurrencyRequest` call | Passes `DecimalPlaces` explicitly |
| ✅ Updated `UpdateCurrencyRequest` call | New signature: `(Name, Symbol, ExchangeRateToBase, FractionName, DecimalPlaces)` |
| ✅ Updated `ValidateAsync()` | Added `DecimalPlaces` validation |

### 5.6 Desktop: CurrencyEditorView.xaml

| Change | Detail |
|--------|--------|
| ❌ Removed `IsBaseCurrency` CheckBox | No longer editable |
| ❌ Removed Rate History DataGrid section | Moved to standalone screen |
| ✅ Added `DecimalPlaces` input field | With helper text + ToolTip |
| ✅ Added Base Currency indicator badge | Read-only green badge with ⭐ icon |

### 5.7 Desktop: CurrenciesListViewModel.cs

| Change | Detail |
|--------|--------|
| 🐛 Fixed `UpdateCurrencyRequest` call (lines 278-283) | Matched new signature without `IsBaseCurrency` |
| ✅ All `UpdateCurrencyRequest` instantiations verified | No remaining references to old signature |

### 5.8 Desktop: CurrencyRatesViewModel + View (NEW)

| Artifact | Lines | Description |
|----------|-------|-------------|
| `CurrencyRatesViewModel.cs` | 315 | Standalone ViewModel: currency selector, rate entry, history DataGrid, EventBus subscriptions |
| `CurrencyRatesView.xaml` | 200 | WPF UI: ComboBox, TextBox, DatePicker, DataGrid, loading overlay, empty states |
| `CurrencyRatesView.xaml.cs` | 11 | Code-behind with InitializeComponent() |
| `App.xaml.cs` | +3 | DI registrations for ViewModel + View |

### 5.9 Desktop: Navigation (MainWindow.xaml + MainViewModel.cs)

| Change | Detail |
|--------|--------|
| ✅ New sidebar expander group **"العملات"** | Between الإعدادات and main stack |
| ✅ **"العملات"** button | Navigates to `CurrenciesListViewModel` (existing list) |
| ✅ **"أسعار العملات"** button | Navigates to `CurrencyRatesViewModel` (new rates screen) |
| ✅ `NavigateToCurrencyRatesCommand` | RelayCommand in MainViewModel |
| ✅ `CurrencyRatesViewModel` DataTemplate | Maps to `CurrencyRatesView` in MainWindow.xaml |
| ✅ Permission tag mapping | `CurrencyRatesViewModel` → `"Currencies"` (same permission as list) |

### 5.10 Before/After Navigation Tree

**Before:**
```
الإعدادات
├── العملات            ← Single button in Settings
├── دليل الحسابات
├── ...
```

**After:**
```
الإعدادات
├── ...
├── الضرائب
│
العملات                 ← 🆕 New expander group
├── العملات              ← Existing Currencies list
└── أسعار العملات         ← 🆕 New rates screen
```

---

## 6. Design Catalog

### 6.1 Currency Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `Name` | `nvarchar(100)` | — | ✅ | UNIQUE index |
| 3 | `Code` | `nvarchar(10)` | — | ✅ | UNIQUE index (e.g., "YER", "USD") |
| 4 | `Symbol` | `nvarchar(10)` | — | ✅ | e.g., "﷼", "$", "€" |
| 5 | `ExchangeRateToBase` | `decimal(18,6)` | `1` | ✅ | Rate against base currency |
| 6 | `IsBaseCurrency` | `bit` | `false` | ✅ | Filtered unique index (immutable after create) |
| 7 | `FractionName` | `nvarchar(20)` | `null` | ❌ | Fractional unit (e.g., "فلس", "سنت") |
| 8 | `DecimalPlaces` | `int` | `2` | ✅ | 0-4 range |
| 9 | `IsSystem` | `bit` | `false` | ❌ | Protects from deletion |
| 10 | `IsActive` | `bit` | `true` | ❌ | Soft delete filter |

### 6.2 ExchangeRateHistory Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `CurrencyId` | `int FK` | — | ✅ | `DeleteBehavior.Restrict` |
| 3 | `OldRate` | `decimal(18,6)` | — | ✅ | Previous exchange rate |
| 4 | `NewRate` | `decimal(18,6)` | — | ✅ | New exchange rate |
| 5 | `EffectiveDate` | `date` | — | ✅ | Date of change |
| 6 | `RateType` | `nvarchar(20)` | `"Daily"` | ❌ | "Daily", "Manual" |
| 7 | `Notes` | `nvarchar(500)` | `null` | ❌ | Reason for change |
| 8 | `ChangedByUserId` | `int?` | `null` | ❌ | User who changed |

### 6.3 Seed Data

| Name | Code | Symbol | ExchangeRateToBase | IsBaseCurrency | FractionName | IsSystem |
|------|------|--------|-------------------|----------------|-------------|---------|
| ريال يمني | YER | ﷼ | 1.0 | ✅ | فلس | ✅ |
| دولار أمريكي | USD | $ | 250.0 | ❌ | سنت | ✅ |
| ريال سعودي | SAR | ﷼ | 66.5 | ❌ | — | ✅ |

### 6.4 API Endpoints

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| GET | `/api/v1/currencies` | `AllStaff` | List all active currencies |
| GET | `/api/v1/currencies/{id}` | `AllStaff` | Get by ID (404 vs 400 differentiated) |
| GET | `/api/v1/currencies/code/{code}` | `AllStaff` | Get by ISO code |
| GET | `/api/v1/currencies/base` | `AllStaff` | Get base currency |
| GET | `/api/v1/currencies/{id}/history` | `ManagerAndAbove` | Get exchange rate history |
| POST | `/api/v1/currencies` | `AdminOnly` | Create currency |
| PUT | `/api/v1/currencies/{id}` | `AdminOnly` | Update currency |
| PUT | `/api/v1/currencies/{id}/exchange-rate` | `AdminOnly` | Update rate only |
| DELETE | `/api/v1/currencies/{id}` | `AdminOnly` | Soft delete |
| DELETE | `/api/v1/currencies/permanent/{id}` | `AdminOnly` | Hard delete (FK guard) |

### 6.5 Desktop ViewModels — Commands

| Screen | Command | Action |
|--------|---------|--------|
| **CurrenciesListViewModel** | `RefreshCommand` | Reload from API, newest-first sort |
| | `AddCommand` | Open CurrencyEditor (new) via ScreenWindowService |
| | `EditCommand` | Open CurrencyEditor (edit) via ScreenWindowService |
| | `DeleteCommand` | Show DeleteStrategy dialog (deactivate/permanent) |
| | `RestoreCommand` | Reactivate a soft-deleted currency |
| **CurrencyEditorViewModel** | `SaveCommand` | Create or update, then publish `CurrencyChangedMessage` |
| | `CancelCommand` | Close window |
| **CurrencyRatesViewModel** | `AddRateCommand` | Update exchange rate, refresh history |
| | `CancelCommand` | Close window |

---

## 7. Key Architecture Decisions

### 7.1 Base Currency: YER (Default for Yemen Market)

- YER seeded as `IsBaseCurrency = true`, USD and SAR as secondary
- **Immutability**: `IsBaseCurrency` can only be set during creation — NOT via update
- DB constraint: filtered unique index `WHERE IsBaseCurrency = 1` enforces single base currency

### 7.2 ExchangeRateToBase Convention

`ExchangeRateToBase` = number of base currency units per 1 unit of this currency:
- YER: `1.0` (base currency)
- USD: `250.0` (1 USD = 250 YER)
- SAR: `66.5` (1 SAR = 66.5 YER)

**Conversion**: `AmountInBaseCurrency = AmountInForeign × ExchangeRateToBase`

### 7.3 Precision: decimal(18,6) for Rates, decimal(18,2) for Money

Exchange rates use `decimal(18,6)` because currency pairs can require up to 6 decimal places (e.g., 1 YER = 0.000040 USD). Money fields on invoices/payments remain `decimal(18,2)`.

### 7.4 CurrencyId: Nullable on Invoices

When `CurrencyId` is null on an invoice, the system assumes base currency. `ExchangeRate` is also nullable — when null, the system uses the current rate from the Currency table at transaction time.

### 7.5 Standalone Rates Screen (Not in Editor)

Rate history was originally embedded in CurrencyEditorView as a sub-DataGrid. It was moved to a dedicated screen ("أسعار العملات") because:
- Exchange rate changes are a daily operation (shouldn't require opening the editor)
- Cleaner separation of concerns
- Follows the same pattern as other standalone list screens in the system

### 7.6 Why NOT a Tab-Based UI

Currency management is a full CRUD module with its own lifecycle — not a tab within Settings. The sidebar has its own expander group with two sub-items.

---

## 8. Sidebar Navigation

```
العملات  (SidebarExpanderCurrencies)
├── العملات       → NavigateToCurrenciesCommand     → CurrenciesListViewModel
└── أسعار العملات  → NavigateToCurrencyRatesCommand  → CurrencyRatesViewModel
```

### Menu Bar (Settings menu):
```
الإعدادات → العملات  → NavigateToCurrenciesCommand  (single menu item, opens list)
```

---

## 9. Permissions

| Screen | Policy | Notes |
|--------|--------|-------|
| Currencies List (Read) | `AllStaff` | All users can view |
| Currency Rates (Read) | `AllStaff` | All users can view rates |
| Currency Create/Update | `AdminOnly` | Only admins can modify currencies |
| Currency Delete | `AdminOnly` | Soft + permanent delete |
| Rate Update | `ManagerAndAbove` | Managers and admins can update rates |

Desktop permission tag: `"Currencies"` — uses `_sessionService.CanAccess(Permission.Settings)`.

---

## 10. Compliance Matrix

| Rule | Directive | Status |
|------|-----------|--------|
| **RULE-001** | `decimal(18,2)` for money | ✅ ExchangeRate: `decimal(18,6)` (documented exception), money: `decimal(18,2)` |
| **RULE-006** | Result<T> pattern | ✅ All service methods return `Result<T>` |
| **RULE-008** | nvarchar for text | ✅ All string columns |
| **RULE-042** | Rich Domain methods | ✅ `Create()`, `Update()`, `UpdateExchangeRate()`, `MarkAsDeleted()` |
| **RULE-044** | FluentValidation | ✅ All 3 request validators |
| **RULE-052** | Guard Clauses | ✅ Arabic DomainException on all entity methods |
| **RULE-058** | INotifyDataErrorInfo | ✅ CurrencyEditorViewModel, CurrencyRatesViewModel |
| **RULE-059** | Save always enabled | ✅ No CanExecute predicates |
| **RULE-141** | ExecuteAsync wrapper | ✅ All async commands |
| **RULE-160** | ScreenWindowService | ✅ Editors open non-modally |
| **RULE-171** | No ex.Message in dialogs | ✅ HandleFailure() + LogSystemError() |
| **RULE-173** | Screen-specific titles | ✅ "خطأ في حفظ العملة", etc. |
| **RULE-174** | No MessageBox.Show | ✅ IDialogService everywhere |
| **RULE-184** | ContentType check | ✅ HandleResponseAsync guards |
| **RULE-185** | Arabic ToolTips | ✅ All interactive controls |
| **RULE-200** | DbUpdateException catch | ✅ DeletePermanentlyAsync |
| **RULE-202** | Result<T> in services | ✅ |
| **RULE-203** | No DbContext in controllers | ✅ Service-only injection |
| **RULE-214** | DeleteBehavior.Restrict | ✅ All FKs |
| **RULE-220** | Newest-first sort | ✅ OrderByDescending(Id) / OrderByDescending(EffectiveDate) |
| **RULE-227** | SetDialogService() | ✅ CurrencyEditorViewModel, CurrencyRatesViewModel |
| **RULE-228** | INotifyDataErrorInfo (no HasXxxError) | ✅ |
| **RULE-229** | ClearAllErrors → AddError → ValidateAllAsync | ✅ |
| **RULE-262** | No hardcoded Height=36/40 | ✅ Compact styles |
| **RULE-269** | Sidebar Width=200 | ✅ |
| **RULE-275** | No BeginTransactionAsync with RetryingExecutionStrategy | ✅ Fixed in v4.6.8 |
| **RULE-277** | ExchangeRate HasPrecision(18,2) | ✅ Fixed in v4.6.8 |
| **RULE-280** | LogSystemError for system errors only | ✅ Fixed in v4.6.8 |
| **RULE-286** | Currency.Create() with isSystem param | ✅ Already existed |
| **RULE-287** | Filtered unique index with IsActive guard | ✅ |
| **RULE-288** | Controller: 404 vs 400 differentiation | ✅ |
| **RULE-289** | IDisposable on list VMs + toast for minor success | ✅ CurrenciesListViewModel + CurrencyRatesViewModel |
| **RULE-298** | CurrencyCode validation: Length == 3 | ✅ ISO 4217 enforcement |

---

## 11. Non-V1 Items (Deferred)

| Feature | Reason |
|---------|--------|
| Auto-exchange rate sync (API from central bank) | Requires external API — V1 manual entry only |
| Multi-currency reporting (any currency) | Advanced reporting — V1 reports in base currency only |
| Real-time rate conversion in invoice UI | V1 uses frozen rate at invoice time |
| Currency-specific rounding rules | Islamic finance — Phase 25+ |
| Branch-level base currency (multi-company) | Multi-branch — Phase 25+ |
| Tax in foreign currency | Complex compliance — V1 taxes in base currency only |
| EffectiveDate-to tracking (CurrencyRate periods) | ExchangeRateHistory only records point-in-time changes; no `EffectiveTo` |

---

## 12. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Deleted currency referenced by invoices** | HIGH | `DeletePermanentlyAsync` catches `DbUpdateException` → Arabic error message |
| **Two base currencies after seed** | Medium | DB filtered unique index `WHERE IsBaseCurrency = 1` |
| **Permanent delete of system currency** | Medium | `IsSystem` guard on all delete operations |
| **Rate precision loss on payment entities** | Low | `.HasPrecision(18, 2)` on ExchangeRate fields (fixed v4.6.8) |
| **BeginTransactionAsync crash** | Low | Removed all manual transactions (fixed v4.6.8) |
| **Desktop offline: no cached rates** | Low | V1 requires network for currency screens — cache planned for future |

---

## 13. Multi-Currency Deep Analysis (v4.10.8 — June 2026)

> **Source**: `docs/all new Anylysis for update system features/currencies new details.md`
> **Status**: 🔄 **Implementation in progress**

### 13.1 Multi-Currency Philosophy

The system operates with:
- **One Base Currency** (e.g., YER) — immutable after creation, used for ALL journal entries
- **Foreign Currencies** (e.g., USD, SAR) — each document stores its currency + exchange rate

**Core Principle**: Every financial document stores its original currency, but ALL journal entries (Debit/Credit) are recorded in base currency only.

### 13.2 Key Requirements

| # | Requirement | Status | Priority |
|---|-------------|--------|----------|
| 1 | Each document stores `CurrencyId` + `ExchangeRate` + `BaseNetTotal` | ❌ **MISSING BaseNetTotal** | 🔴 CRITICAL |
| 2 | Journal entries (Debit/Credit) ALWAYS in base currency | ❌ **NO CONVERSION** | 🔴 CRITICAL |
| 3 | CashBox has ONE currency (`CurrencyId` required) | ❌ **MISSING** | 🔴 CRITICAL |
| 4 | Bank has ONE currency (`CurrencyId` required) | ❌ **MISSING** | 🔴 CRITICAL |
| 5 | Exchange rate frozen at document creation — NEVER changed after posting | ⚠️ **PARTIAL** | 🟡 HIGH |
| 6 | Customer/Supplier receipts store exchange rate at payment time | ❌ **MISSING** | 🟡 HIGH |
| 7 | One currency per invoice (no mixed-currency lines) | ✅ PASS | — |
| 8 | Exchange rate gain/loss deferred to V2 | ✅ PASS | — |

### 13.3 Gap Analysis — What Needs to Change

#### 13.3.1 BaseNetTotal Field (4 entities)

`BaseNetTotal` = `NetTotal × ExchangeRate` — the base-currency equivalent stored on the document.

| Entity | Current Fields | Missing | Action |
|--------|---------------|---------|--------|
| `SalesInvoice` | CurrencyId ✅, ExchangeRate ✅, CostInBaseCurrency ✅ | **BaseNetTotal** | Add `decimal? BaseNetTotal` |
| `PurchaseInvoice` | CurrencyId ✅, ExchangeRate ✅, CostInBaseCurrency ✅ | **BaseNetTotal** | Add `decimal? BaseNetTotal` |
| `SalesReturn` | CurrencyId ✅, ExchangeRate ✅ | **BaseNetTotal** | Add `decimal? BaseNetTotal` |
| `PurchaseReturn` | CurrencyId ✅, ExchangeRate ✅ | **BaseNetTotal** | Add `decimal? BaseNetTotal` |

**Formula**: `BaseNetTotal = NetTotal × ExchangeRate`
**When set**: On document creation/update (Draft state only)
**Used by**: Journal entries, financial reports, account statements

#### 13.3.2 ExchangeRate + BaseNetTotal on Payment Entities (3 entities)

| Entity | Has CurrencyId? | Has ExchangeRate? | Has BaseNetTotal? | Action |
|--------|:-:|:-:|:-:|--------|
| `CustomerReceipt` | ✅ | ❌ | ❌ | Add `ExchangeRate` + `BaseNetTotal` |
| `SupplierPayment` | ✅ | ❌ | ❌ | Add `ExchangeRate` + `BaseNetTotal` |
| `Expense` | ✅ | ❌ | ❌ | Add `ExchangeRate` + `BaseNetTotal` |

#### 13.3.3 CurrencyId on CashBox and Bank

| Entity | Has CurrencyId? | Has Currency Nav? | Action |
|--------|:-:|:-:|--------|
| `CashBox` | ❌ | ❌ | Add `short CurrencyId` (required) + `Currency?` nav |
| `Bank` | ❌ | ❌ | Add `short CurrencyId` (required) + `Currency?` nav |

**Design**: Each cashbox/bank operates in ONE currency. Transfers between different-currency boxes require exchange rate.

#### 13.3.4 Status Guards on SetCurrency/SetExchangeRate

| Entity | Method | Has Status Guard? | Action |
|--------|--------|:-:|--------|
| `SalesInvoice` | `SetCurrency()` | ❌ | Add `if (Status != InvoiceStatus.Draft) throw` |
| `PurchaseInvoice` | `SetCurrency()` | ❌ | Add `if (Status != InvoiceStatus.Draft) throw` |
| `SalesReturn` | `SetExchangeRate()` | ❌ | Add `if (Status != InvoiceStatus.Draft) throw` |
| `PurchaseReturn` | `SetExchangeRate()` | ✅ | Already protected |

#### 13.3.5 AccountingIntegrationService — Currency Conversion (CRITICAL)

**Current Problem**: ALL 12+ journal entry methods use raw document amounts WITHOUT converting to base currency. The `CreateJournalEntryRequest` defaults to `CurrencyId=1, ExchangeRate=1m` and is NEVER overridden.

**Required Fix**: Every journal entry method MUST:
1. Pass `invoice.CurrencyId` and `invoice.ExchangeRate` to `CreateJournalEntryRequest`
2. Multiply ALL Debit/Credit amounts by `ExchangeRate` before creating journal lines

**Example — Sales Invoice (100 USD, ExchangeRate=700)**:
```
Before (WRONG):  Dr Cash 100 / Cr Revenue 100  (records USD as if YER)
After (CORRECT): Dr Cash 70,000 / Cr Revenue 70,000  (converts to YER)
```

**Methods to fix**:
- `CreateSalesPostEntryAsync` — multiply by `invoice.ExchangeRate`
- `CreatePurchasePostEntryAsync` — multiply by `invoice.ExchangeRate`
- `CreateCustomerPaymentEntryAsync` — multiply by `receipt.ExchangeRate`
- `CreateSupplierPaymentEntryAsync` — multiply by `payment.ExchangeRate`
- `CreateSalesReturnEntryAsync` — multiply by `return.ExchangeRate`
- `CreatePurchaseReturnEntryAsync` — multiply by `return.ExchangeRate`
- `CreateExpenseEntryAsync` — multiply by `expense.ExchangeRate`
- All reversal methods — use the same ExchangeRate as the original entry

### 13.4 Implementation Plan

| Step | Layer | Files | Description |
|------|-------|-------|-------------|
| 1 | Domain | 4 invoice entities | Add `BaseNetTotal` property |
| 2 | Domain | 3 payment entities | Add `ExchangeRate` + `BaseNetTotal` |
| 3 | Domain | CashBox, Bank | Add `CurrencyId` + nav property |
| 4 | Domain | 3 invoice entities | Add status guards to SetCurrency/SetExchangeRate |
| 5 | Infrastructure | 7 EF configs | Map new fields |
| 6 | Contracts | 7 DTOs | Add new fields to response DTOs |
| 7 | Contracts | 5 Request DTOs | Add ExchangeRate/BaseNetTotal to requests |
| 8 | Application | AccountingIntegrationService | Convert all amounts to base currency |
| 9 | Application | Service DTOs | Update InvoicePrintDto, etc. |
| 10 | API | Controllers | Pass new fields in responses |
| 11 | Desktop | ViewModels | Show BaseNetTotal in editors |
| 12 | Docs | database-schema.md | Update schema documentation |
| 13 | Docs | Phase 20 plan | Update this document |

### 13.5 Migration Strategy

Since this is a schema change (new columns), we need an EF Core migration:
- `BaseNetTotal` columns: nullable (`decimal?`) — existing rows get `NULL`
- `ExchangeRate` on payments: nullable (`decimal?`) — existing rows get `NULL`
- `CurrencyId` on CashBox/Bank: **required** — must seed default currency (YER) for existing rows
- Backfill script: Calculate `BaseNetTotal` for existing posted invoices using their `ExchangeRate`

### 13.6 Deferred to V2

| Feature | Reason |
|---------|--------|
| Exchange rate gain/loss calculation | Requires unrealized gain/loss revaluation — complex accounting |
| Multi-currency account statements | Needs dual-currency display (doc currency + base) |
| Currency-specific rounding rules | Islamic finance requirements — not V1 |
| Auto-exchange rate sync from central bank | External API dependency |
