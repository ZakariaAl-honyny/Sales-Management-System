# Phase 32 — Suppliers Module: Comprehensive Implementation Plan

> **Version**: 1.0 — Full codebase audit + design analysis (Analysis Parts 2, 3, 4)
> **Scope**: Complete Suppliers Module enhancement — AccountId FK, SupplierType, CreditLimit validation, UI balance display, reports, and 9 implementation tasks

---

## Table of Contents

1. [Architecture — Suppliers Module Design](#1-architecture--suppliers-module-design)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Supplier Design Catalog](#4-supplier-design-catalog)
5. [Gap Analysis — Existing vs Target](#5-gap-analysis--existing-vs-target)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — Suppliers Module Design

### 1.1 Context Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                     SUPPLIERS MODULE BOUNDARY                     │
│                                                                   │
│  ┌──────────────────┐    ┌─────────────────────┐                  │
│  │   Desktop (WPF)   │    │    API (ASP.NET)     │                 │
│  │  ┌──────────────┐│    │  ┌─────────────────┐ │                 │
│  │  │SupplierListView│──►│  │SuppliersController│ │                 │
│  │  │ (List + Search)│    │  │ (6 endpoints)    │ │                 │
│  │  └──────────────┘│    │  └────────┬────────┘ │                 │
│  │  ┌──────────────┐│    │           │            │                 │
│  │  │SupplierEditor │──►│  ┌────────▼────────┐ │                 │
│  │  │ View (CRUD)   │    │  │  SupplierService │ │                 │
│  │  └──────────────┘│    │  │  (Result<T>)     │ │                 │
│  │  ┌──────────────┐│    │  └────────┬────────┘ │                 │
│  │  │SupplierBalance││    │           │            │                 │
│  │  │ Widget (NEW)  ││    │  ┌────────▼────────┐ │                 │
│  │  └──────────────┘│    │  │   IUnitOfWork     │ │                 │
│  └──────────────────┘    │  └────────┬────────┘ │                 │
│           │              │           │            │                 │
│           ▼              │  ┌────────▼────────┐ │                 │
│  ┌──────────────────┐    │  │    EF Core       │ │                 │
│  │  SupplierApi      │──►│  │  SalesDbContext   │ │                 │
│  │  Service (HTTP)   │    │  └─────────────────┘ │                 │
│  └──────────────────┘    └────────────────────▲──┘                 │
│                                                │                    │
│  ┌─────────────────────────────────────────────┴──┐                │
│  │              SQL Server Database                │                │
│  │  ┌─────────────┐  ┌──────────────┐             │                │
│  │  │  Suppliers   │  │   Accounts   │(Chart of Accts)             │
│  │  │  (enhanced)  │──│  (FK:       │             │                │
│  │  │              │  │   AccountId) │             │                │
│  │  └─────────────┘  └──────────────┘             │                │
│  │  ┌─────────────┐  ┌──────────────┐             │                │
│  │  │ PurchaseInvoices │ SupplierGroups(NEW)      │                │
│  │  │ (FK: SupplierId)│(FK: SupplierGroupId)     │                │
│  │  └─────────────┘  └──────────────┘             │                │
│  └────────────────────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────┘
```

### 1.2 Data Flow

| Direction | Flow | Protocol |
|-----------|------|----------|
| Desktop → API | List/CRUD operations | HTTP JSON (HttpClient) |
| API → Application | Business logic + validation | In-process (DI) |
| Application → Domain | Entity state changes | In-process (private set + methods) |
| Application → Infrastructure | Persistence | IUnitOfWork → EF Core |
| Infrastructure → SQL Server | SQL queries | EF Core SQL Server Provider |

### 1.3 Module Boundaries

| Layer | Responsibilities | Forbidden |
|-------|-----------------|-----------|
| **Domain** (Supplier entity) | Business rules: CreditLimit >= 0, balance management, name required, Guard Clauses | ⛔ No DB access, no UI logic |
| **Application** (SupplierService) | Orchestration: Result<T>, IUnitOfWork, logging, CreditLimit validation on purchase | ⛔ No direct DB access, no Domain duplication |
| **API** (SuppliersController) | HTTP translation: map Result → StatusCode, secure with [Authorize] | ⛔ No business logic, no DbContext |
| **Desktop** (ViewModels + Views) | UI: ExecuteAsync(), INotifyDataErrorInfo, DialogService, Arabic ToolTips | ⛔ No DB access, no business rules |

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Layer ✅

**File**: `SalesSystem.Domain.Entities.Supplier`

| Field | Type | Default | Required | Status |
|-------|------|---------|----------|--------|
| `Id` | `int PK` | Auto | ✅ | ✅ Exists |
| `Name` | `string` | `""` | ✅ | ✅ Exists |
| `Phone` | `string?` | `null` | ❌ | ✅ Exists |
| `Email` | `string?` | `null` | ❌ | ✅ Exists |
| `Address` | `string?` | `null` | ❌ | ✅ Exists |
| `OpeningBalance` | `decimal(18,2)` | `0` | ❌ | ✅ Exists |
| `CurrentBalance` | `decimal(18,2)` | `0` | ✅ (computed) | ✅ Exists |
| `CreditLimit` | `decimal(18,2)` | `0` | ❌ | ✅ Exists |
| `TaxNumber` | `string(30)?` | `null` | ❌ | ✅ Exists |
| `IsActive` | `bool` | `true` | ❌ | ✅ Exists (from BaseEntity) |

**Domain methods** (all exist):
- `Supplier.Create(name, openingBalance, phone, email, address, taxNumber, creditLimit, createdByUserId)` — factory with Guard Clauses
- `Supplier.Update(name, phone, email, address, taxNumber, creditLimit, updatedByUserId)` — mutation with Guard Clauses
- `IncreaseBalance(decimal amount)` — domain method (applies when we owe more)
- `DecreaseBalance(decimal amount)` — domain method (applies when we pay)

### 2.2 Infrastructure Layer ✅

**File**: `Infrastructure.Data.Configurations.SupplierConfiguration`

| Configuration | Value | Status |
|--------------|-------|--------|
| Table name | `Suppliers` | ✅ Exists |
| PK | `Id` (int, auto-increment) | ✅ Exists |
| Name | `nvarchar(150)` required | ✅ Exists |
| Phone | `nvarchar(20)` nullable | ✅ Exists |
| Email | `nvarchar(100)` nullable | ✅ Exists |
| Address | `nvarchar(250)` nullable | ✅ Exists |
| OpeningBalance | `decimal(18,2)` | ✅ Exists |
| CurrentBalance | `decimal(18,2)` | ✅ Exists |
| CreditLimit | `decimal(18,2)` | ✅ Exists |
| TaxNumber | `nvarchar(30)` nullable | ✅ Exists |
| DeleteBehavior | `Restrict` on all FKs (RULE-214) | ✅ Exists |
| Query filter | `IsActive == true` | ✅ Exists |

### 2.3 Application Layer ✅

**File**: `Application.Interfaces.Services.ISupplierService`

| Method | Returns | Status |
|--------|---------|--------|
| `GetByIdAsync(int, CancellationToken)` | `Result<SupplierDto>` | ✅ Exists |
| `GetAllAsync(string?, int, int, bool, CancellationToken)` | `Result<PagedResult<SupplierDto>>` | ✅ Exists |
| `CreateAsync(CreateSupplierRequest, CancellationToken)` | `Result<SupplierDto>` | ✅ Exists |
| `UpdateAsync(int, UpdateSupplierRequest, CancellationToken)` | `Result<SupplierDto>` | ✅ Exists |
| `DeleteAsync(int, CancellationToken)` | `Result` (soft) | ✅ Exists |
| `PermanentDeleteAsync(int, CancellationToken)` | `Result` (with FK guard) | ✅ Exists |
| ⬜ **GetBalanceReportAsync(int, CancellationToken)** | `Result<SupplierBalanceReportDto>` | **NEW** |
| ⬜ **GetCreditLimitUsageAsync(int, CancellationToken)** | `Result<CreditLimitUsageDto>` | **NEW** |

**File**: `Application.Services.SupplierService`

- Uses `IUnitOfWork` (RULE-024) ✅
- Returns `Result<T>` (RULE-006) ✅
- `PermanentDeleteAsync` checks `PurchaseInvoices` and `SupplierPayments` references before deletion ✅
- `MapToDto` maps all 10 fields ✅
- Serilog logging on CRUD operations ✅

### 2.4 API Layer ✅

**File**: `Api.Controllers.SuppliersController`

| Method | Endpoint | Policy | Status |
|--------|----------|--------|--------|
| GET | `/api/v1/suppliers` | `ManagerAndAbove` | ✅ Exists |
| GET | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | ✅ Exists |
| POST | `/api/v1/suppliers` | `ManagerAndAbove` | ✅ Exists |
| PUT | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | ✅ Exists |
| DELETE | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | ✅ Exists (soft) |
| DELETE | `/api/v1/suppliers/permanent/{id}` | `AdminOnly` | ✅ Exists |
| ⬜ **GET** | `/api/v1/suppliers/reports/balance` | `ManagerAndAbove` | **NEW** |
| ⬜ **GET** | `/api/v1/suppliers/{id}/credit-limit` | `ManagerAndAbove` | **NEW** |

**Controller purity** (RULE-203): ✅ — injects `ISupplierService` only, no DbContext or IUnitOfWork.

### 2.5 Desktop Layer ✅

#### ViewModels

| File | Lines | Status |
|------|-------|--------|
| `ViewModels.Suppliers.SupplierEditorViewModel` | 236 | ✅ Exists |
| `ViewModels.Suppliers.SupplierListViewModel` | 374 | ✅ Exists |
| `ViewModels.Suppliers.SupplierSelectionViewModel` | — | ✅ Exists |

**SupplierEditorViewModel features:**
- `SetDialogService()` called in constructor (RULE-227) ✅
- `INotifyDataErrorInfo` with `AddError`/`ClearErrors` (RULE-228) ✅
- Save always enabled — validate on click with `ValidateAllAsync()` (RULE-229) ✅
- `ExecuteAsync()` wrapper for save (RULE-141) ✅
- `LogSystemError()` for system errors (RULE-199) ✅
- Arabic dialog titles: `"خطأ في حفظ المورد"` (RULE-173) ✅
- `IDialogService` with Async suffix (RULE-174/175) ✅
- EventBus publish on save: `SupplierChangedMessage` ✅

**SupplierListViewModel features:**
- Newest-first sort by Id descending (RULE-220) ✅
- DeleteStrategy dialog (RULE-050) ✅
- `RestoreSupplierAsync()` via `UpdateAsync(IsActive=true)` ✅
- `IncludeInactive` filter toggle ✅
- `EditSupplierFromDoubleClick()` for DataGrid double-click ✅
- Toast notifications on success ✅
- EventBus subscription with Cleanup() ✅
- ❌ Manual try/catch/finally in `LoadSuppliersAsync()` — should use `ExecuteAsync()`

#### Views

| File | Status |
|------|--------|
| `Views.Suppliers.SuppliersListView.xaml` | ✅ Exists (266 lines) |
| `Views.Suppliers.SupplierEditorView.xaml` | ✅ Exists (185 lines) |
| `Views.Suppliers.SupplierSelectionView.xaml` | ✅ Exists |

**SuppliersListView.xaml features:**
- Search TextBox with Enter key binding ✅
- IncludeInactive checkbox filter ✅
- CRUD buttons (Add, Edit, Delete, Restore, Refresh) ✅
- Arabic ToolTips on all buttons (RULE-185-190) ✅
- Error message bar ✅
- Empty-state view with Add button ✅
- Footer with supplier count ✅
- Loading overlay with ProgressBar ✅
- DataGrid columns: Id, Name, Phone, CurrentBalance, Status ✅
- ContextMenu with Edit/Delete/Restore ✅
- ❌ Missing: AccountId, SupplierType, SupplierGroup columns
- ❌ Missing: Balance display widget/indicator

**SupplierEditorView.xaml features:**
- Header with icon + title ✅
- Form fields: Name*, Phone, Address, TaxNumber, Email, OpeningBalance, IsActive ✅
- Arabic ToolTips (RULE-185-190) ✅
- Helper text beneath fields ✅
- Footer with Save + Cancel buttons ✅
- Loading overlay ✅
- ❌ Missing: AccountId selector (ComboBox for Chart of Accounts)
- ❌ Missing: SupplierType radio (Cash/Credit)
- ❌ Missing: SupplierGroup combo
- ❌ Missing: CreditLimit field prominently displayed
- ❌ Missing: Current balance display in editor (read-only)

#### Services

| File | Status |
|------|--------|
| `Services.Api.ISupplierApiService` — 6 methods | ✅ Exists |
| `Services.Api.SupplierApiService` — HTTP implementation | ✅ Exists |

### 2.6 Contracts Layer ✅

**File**: `Contracts.DTOs.AllDtos`

> See `SalesSystem.Contracts/` for the canonical `SupplierDto` definition. Current fields: Id, Name, Phone, Email, Address, TaxNumber, CreditLimit, IsActive, plus AccountId (int, non-nullable — added per RULE-444). OpeningBalance and CurrentBalance REMOVED — balance lives on linked Account only.

**File**: `Contracts.Responses.SupplierResponse`
> See `SalesSystem.Contracts/Responses/` for the canonical `SupplierResponse` definition.

### 2.7 Validators (FluentValidation) ✅

**File**: `Api.Validators.SupplierRequestValidators`

| Rule | Create | Update |
|------|--------|--------|
| Name required + max 150 | ✅ | ✅ |
| Phone max 20 | ✅ | ✅ |
| Email format + max 100 | ✅ | ✅ |
| OpeningBalance >= 0 | ✅ | ❌ Update doesn't allow balance change |
| CreditLimit >= 0 | ✅ | ✅ |

**Missing:**
- ❌ TaxNumber max length validation
- ❌ TaxNumber format validation (Saudi: 15 digits)
- ❌ AccountId validation (must exist in Accounts)
- ❌ SupplierType validation

### 2.8 Seed Data ⚠️

**File**: `Infrastructure.Data.DbSeeder` (line 129-133)

> See `docs/AGENTS.md` §2.84 (RULE-442 to RULE-454) for the canonical supplier seeding pattern. The default supplier `"مورد نقدي"` is created with auto-created account linked to COA under 2100 parent.

**Issues (fixed):**
1. ✅ Name changed to `"مورد نقدي"` per AGENTS.md RULE-453
2. ✅ `AccountId` auto-created via service — linked to Accounts Payable (2100)
3. ❌ `SupplierType` — NOT in V1 (deferred — payment type is per-invoice per RULE-443)
4. ❌ `SupplierGroup` — NOT in V1 (per RULE-443)

**SystemSetting reference** (also in DbSeeder line 47):
> See `docs/database-schema.md` for the `SystemSetting` table schema and `docs/AGENTS.md` §2.67 (RULE-291 to RULE-297) for system setting creation patterns.
✅ Already references "المورد النقدي" in DisplayName.

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: Default Supplier Name — Rename to "مورد نقدي"

**Problem**: The default supplier seeded in `DbSeeder.cs` line 130 is named `"المورد الافتراضي في النظام"`. The analysis (Part 2 line 518, Part 3 line 718-723) explicitly requires the name `"مورد نقدي"`. This name is used:
- As default supplier for cash purchases (SystemSetting `DefaultCashSupplierId` = 1)
- Displayed in Purchase Invoice editor when user selects "Cash Purchase"
- Referenced in Apple `Accounts Payable` subtree: `الموردون → مورد نقدي`

**Impact**: User sees incorrect name in purchase invoices and supplier list. Inconsistent with SystemSetting display name `"المورد النقدي"`.

**Fix**: Single line change in `DbSeeder.cs` — rename the default supplier name string literal from `"المورد الافتراضي في النظام"` to `"مورد نقدي"`.

**Files changed**: `DbSeeder.cs`

### 3.2 Blocker 2: Supplier — AccountId FK Missing

**Problem**: Analysis Part 3 (line 940) requires: `"المورد ينشئ حساباً محاسبياً تلقائياً"`. Currently `Supplier` entity has no `AccountId` FK to link it to a Chart of Accounts `Account`. This blocks:
- Automatic journal entry creation for supplier transactions
- Supplier balance visibility in account statements
- Integration with accounting modules (Phase 25+)

**Current workaround**: `AccountingSeeder` creates `"2100 — حسابات الموردين" (Accounts Payable)` as a **single liability account** for all suppliers. This is insufficient for per-supplier tracking — each supplier needs their own sub-account under `2100`.

**Fix**: Add `int AccountId` (MANDATORY non-nullable FK) to Supplier entity — see `docs/AGENTS.md` §2.84 (RULE-442) for the canonical definition. AccountId is auto-created by service (NEVER user-supplied). Balance tracks on the linked GL Account — Supplier entity has NO OpeningBalance/CurrentBalance fields (RULE-446).

**Migration**:

> See `docs/database-schema.md` for the canonical Suppliers table schema — AccountId FK added with `DeleteBehavior.Restrict` per RULE-214.

**Files changed**: `Supplier.cs`, `SupplierConfiguration.cs`, `SupplierService.cs`, `ISupplierService.cs`, `SupplierDto.cs`, `SupplierResponse.cs`, `SupplierRequests.cs`, migrations, `DbSeeder.cs`

### 3.3 Blocker 3: Manual try/catch/finally in SupplierListViewModel

**Problem**: `SupplierListViewModel.LoadSuppliersAsync()` (lines 154-196) uses manual `try/catch/finally` with `IsBusy = true/false` and `HandleException()` call. This violates RULE-141 which requires `ExecuteAsync()` wrapper for ALL async ViewModel operations.

**Current code** (lines 154-196): ❌ Outdated pattern — uses manual try/catch/finally with IsBusy (violates RULE-141).

**Fix**: Replace with `ExecuteAsync()` wrapper pattern per `docs/AGENTS.md` §2.36 (RULE-141 to RULE-146):

> See `docs/AGENTS.md` §2.36 for the canonical `ExecuteAsync()` pattern. All async ViewModel operations MUST use `ExecuteAsync()` wrapper — NO manual try/catch/finally, NO manual IsBusy, NO direct Serilog calls.

**Files changed**: `SupplierListViewModel.cs`

---

## 4. Supplier Design Catalog

### 4.1 Enhanced Supplier Entity

| # | Field | Type | Constraints | Required | V1 | Notes |
|---|-------|------|-------------|----------|----|-------|
| 1 | `Id` | `int PK` | Auto-increment | ✅ | ✅ | — |
| 2 | `Name` | `nvarchar(150)` | — | ✅ | ✅ | Unique per supplier |
| 3 | `Phone` | `nvarchar(20)` | — | ❌ | ✅ | Contact number |
| 4 | `Email` | `nvarchar(100)` | — | ❌ | ✅ | Official email |
| 5 | `Address` | `nvarchar(250)` | — | ❌ | ✅ | Physical address |
| 6 | `OpeningBalance` | `decimal(18,2)` | >= 0 | ❌ | ✅ | Initial balance when added |
| 7 | `CurrentBalance` | `decimal(18,2)` | Computed | ✅ | ✅ | Positive = we owe supplier |
| 8 | `CreditLimit` | `decimal(18,2)` | >= 0 | ❌ | ✅ | Max credit allowed |
| 9 | `TaxNumber` | `nvarchar(30)` | — | ❌ | ✅ | VAT registration |
| 10 | **NEW** `AccountId` | `int FK?` | → Accounts(Id) Restrict | **IF** accounting enabled | ✅ | Chart of Accounts link |
| 11 | **NEW** `SupplierType` | `tinyint` | 0=Cash, 1=Credit | ❌ | ✅ | Supplier classification |
| 12 | **NEW** `SupplierGroupId` | `int FK?` | → SupplierGroups(Id) Restrict | ❌ | **Deferred** | Group for reporting |
| 13 | `IsActive` | `bit` | Global query filter | ❌ | ✅ | Soft delete flag |

**Balance Direction** (RULE-008 convention):
```
Supplier.CurrentBalance > 0 = We owe the supplier (liability)
Supplier.CurrentBalance < 0 = Supplier owes us (asset/prepayment)
```

### 4.2 SupplierType Enum

> **NOT in V1** — deferred per RULE-443. `SupplierType` enum is not implemented. Payment type is per-invoice (`SalesInvoice.PaymentType`), not per-supplier.

### 4.3 SupplierGroup Entity (Deferred to Non-V1)

> **NOT in V1** — deferred per analysis Part 3 line 952. `SupplierGroup` entity is not implemented.

> Supplier filtering in V1 uses search only (name, phone). Group reports deferred.

### 4.4 SupplierDto (Enhanced)

> Canonical definition in `SalesSystem.Contracts/`. See `docs/AGENTS.md` §2.84 for current SupplierDto fields — AccountId (int, non-nullable), AccountName. OpeningBalance/CurrentBalance REMOVED — balance lives on linked Account only. SupplierType NOT in V1.

### 4.5 SupplierBalanceReportDto (New)

> Report DTO pattern: see `SalesSystem.Contracts.DTOs.Reports/` for canonical report DTO definitions. Balance is sourced from linked Account, not from Supplier entity.

### 4.6 CreditLimitUsageDto (New)

> Report DTO pattern: see `SalesSystem.Contracts.DTOs.Reports/` for canonical report DTO definitions. `CurrentBalance` computed from Account balance, not stored on Supplier.

### 4.7 Requests (Enhanced)

> Canonical request definitions in `SalesSystem.Contracts/Requests/`. See `docs/AGENTS.md` §2.84 — `CreateSupplierRequest`/`UpdateSupplierRequest` MUST NOT have `AccountId`, `SupplierType`, or `OpeningBalance` (auto-created or removed). Account is auto-created under 2100 parent by service.

### 4.8 SupplierResponse (Enhanced)

> Canonical response definition in `SalesSystem.Contracts/Responses/`. `SupplierResponse` includes `AccountId` (int, non-nullable) and `AccountName` (string?). NO `SupplierType`, NO `CurrentBalance` (sourced from linked Account).

### 4.9 API Endpoints (Full Set)

| Method | Endpoint | Policy | Action | Status |
|--------|----------|--------|--------|--------|
| GET | `/api/v1/suppliers` | `ManagerAndAbove` | List (search, paged) | ✅ Exists |
| GET | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | Get by ID | ✅ Exists |
| POST | `/api/v1/suppliers` | `ManagerAndAbove` | Create | ✅ Exists |
| PUT | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | Update | ✅ Exists |
| DELETE | `/api/v1/suppliers/{id}` | `ManagerAndAbove` | Soft delete | ✅ Exists |
| DELETE | `/api/v1/suppliers/permanent/{id}` | `AdminOnly` | Hard delete (with FK guard) | ✅ Exists |
| **NEW** GET | `/api/v1/suppliers/reports/balance-summary` | `ManagerAndAbove` | Balance summary report | **NEW** |
| **NEW** GET | `/api/v1/suppliers/{id}/credit-limit` | `ManagerAndAbove` | Credit limit usage | **NEW** |
| **NEW** GET | `/api/v1/suppliers/{id}/transactions` | `ManagerAndAbove` | Transaction history | **NEW** |

---

## 5. Gap Analysis — Existing vs Target

### 5.1 Domain Entity Gaps

| Feature | Status | Action |
|---------|--------|--------|
| `AccountId` FK | ❌ Missing | Add FK → Account entity, Restrict delete |
| `SupplierType` enum | ❌ Missing | Add Cash=0, Credit=1 enum + property |
| `SupplierGroupId` FK | ❌ Missing | **DEFERRED** — not in V1 per analysis |
| Balance direction convention | ✅ Exists | Positive = we owe supplier |
| Guard Clauses (all fields) | ✅ Exists | Name, OpeningBalance, CreditLimit |
| `IncreaseBalance` / `DecreaseBalance` | ✅ Exists | Domain methods |

### 5.2 Configuration Gaps

| Configuration | Status | Action |
|--------------|--------|--------|
| MaxLength(150) for Name | ✅ Exists | — |
| MaxLength(30) for TaxNumber | ✅ Exists | — |
| HasPrecision(18,2) for decimals | ✅ Exists | — |
| AccountId FK + Restrict | ❌ Missing | Add HasForeignKey + OnDelete(DeleteBehavior.Restrict) |
| SupplierType conversion | ❌ Missing | Add .Property(s => s.SupplierType).HasConversion<int>() |

### 5.3 Service Gaps

| Feature | Status | Action |
|---------|--------|--------|
| Account auto-creation on Supplier.Create | ❌ Missing | Create Account under 2100 parent, assign to supplier |
| CreditLimit validation on purchase | ❌ Missing | Add `IsCreditLimitExceeded(supplierId, amount)` method |
| Balance report service method | ❌ Missing | Add `GetBalanceReportAsync()` |
| CreditLimit usage service method | ❌ Missing | Add `GetCreditLimitUsageAsync()` |
| Transaction history service method | ❌ Missing | Add `GetTransactionHistoryAsync()` |
| Search by SupplierType | ❌ Missing | Extend GetAllAsync search predicate |
| ExecuteAsync() in ViewModel | ❌ Manual try/catch | **BLOCKER 3** — fix pattern |

### 5.4 Controller Gaps

| Feature | Status | Action |
|---------|--------|--------|
| 3 new report endpoints | ❌ Missing | Add to SuppliersController |
| Rate limiting on all | ✅ Exists (global 100/min + login 5/15min) | — |
| Policy `ManagerAndAbove` | ✅ Correct | Suppliers are manager-only |

### 5.5 Desktop Gaps

| Feature | Status | Action |
|---------|--------|--------|
| SupplierType ComboBox/Radio | ❌ Missing | Add to SupplierEditorView |
| AccountId ComboBox (Chart of Accounts) | ❌ Missing | Add selector linked to AccountApiService |
| Current balance display in Editor | ❌ Missing | Add read-only balance field |
| CreditLimit field in XAML | ❌ Missing | Add to editor form |
| Balance usage indicator in list | ❌ Missing | Color-code CurrentBalance vs CreditLimit |
| SupplierType column in DataGrid | ❌ Missing | Add template column |
| AccountId filter in search | ❌ Missing | Optional search by account |
| ToolTips on new fields | ❌ Missing | Add Arabic ToolTips (RULE-185-190) |
| ExecuteAsync() refactor | ❌ Manual try/catch | **BLOCKER 3** |
| Compact UI styles | ⚠️ Partial | Check SupplierEditorView for hardcoded sizes |

### 5.6 Seed Data Gaps

| Data | Status | Action |
|------|--------|--------|
| Default supplier name | ❌ Wrong | `"المورد الافتراضي في النظام"` → `"مورد نقدي"` (**Blocker 1**) |
| Default supplier AccountId | ❌ Missing | Link to `2100 — حسابات الموردين` account |
| Default supplier SupplierType | ❌ Missing | Set to `Cash (0)` |
| Supplier groups seed | ❌ Missing | **DEFERRED** — not needed in V1 |

### 5.7 Validator Gaps

| Rule | Status | Action |
|------|--------|--------|
| TaxNumber format (15 digits) | ❌ Missing | Add regex validation for Saudi VAT |
| AccountId exists validation | ❌ Missing | Add `MustExistInDatabase<Account>()` rule |
| SupplierType range validation | ❌ Missing | Add `IsInEnum()` rule |

### 5.8 Report Gaps

| Report | Status | Action |
|--------|--------|--------|
| Supplier Balance Summary | ❌ Missing | Total balance, count, credit limit usage |
| Credit Limit Exceeded List | ❌ Missing | Filter suppliers exceeding limit |
| Supplier Transaction History | ❌ Missing | Purchase invoices + payments per supplier |
| **Deferred**: Aging Report | ❌ Deferred | Supplier debt aging (invoice date analysis) |
| **Deferred**: Supplier Statement | ❌ Deferred | Full account statement with debit/credit |

---

## 6. Architectural Decisions

### 6.1 SupplierGroup — DEFERRED to V2

Analysis Part 3 (line 952) explicitly states:

> "لا توجد مجموعات عملاء أو موردين في V1"

**Decision**: **Defer `SupplierGroup` to V2**. Despite the Task Requirements mentioning Supplier Group, the canonical analysis document is the primary source for V1 scope. Supplier filtering in V1 uses search (name, phone, email, address) only.

**Rationale**:
- User explicitly stated no groups in V1
- Adds migration + FK + UI combo + filter complexity
- Can be added later as additive change (nullable FK)
- No existing screens depend on groups

**Update**: If user explicitly requests SupplierGroup in V1, it becomes a Phase 32.5 addition with estimated +2 hours.

### 6.2 AccountId — Add NOW (Non-Fatal Blocker)

**Decision**: **Add `int? AccountId` FK NOW** as part of Phase 32. The FK is nullable (existing suppliers have no account link). This enables:

1. Auto-creation of accounting accounts for new suppliers (future Phase 25 integration)
2. Tracking supplier balance in general ledger
3. Account statements per supplier

**But**: Full auto-creation logic (`Supplier.Create` → `Account.Create`) is **deferred** to Phase 25 (Accounting Integration). In Phase 32, the FK exists but auto-creation is manual — the API accepts `AccountId` as optional. When null, no account is created.

**Why nullable for V1**:
- Backwards-compatible with existing suppliers
- Accounting integration not yet fully built
- User can manually link suppliers to accounts via the editor

### 6.3 SupplierType — Add NOW (Required for Purchases)

**Decision**: **Add `SupplierType` property NOW** with Cash=0 as default. This directly impacts:
- Purchase invoice credit limit validation
- UI filtering (show/hide CreditLimit for Cash suppliers)
- Report segmentation

**Backwards compat**: Existing suppliers default to `Cash (0)` — additive change, no data migration needed.

### 6.4 CreditLimit Validation — Layer Location

**Decision**: Primary validation in **Domain** (Guard Clause: `CreditLimit >= 0`), secondary validation in **Application** service (`Create/Update`), **operational** validation in `SupplierService.IsCreditLimitExceeded()` (called from PurchaseInvoiceService during credit purchases).

**Where to validate credit limit during purchase**:
1. **Domain** (PurchaseInvoice.Create) — check `supplier.CreditLimit >= (supplier.CurrentBalance + invoice.TotalAmount)`
2. **Application** (PurchaseInvoiceService) — validate before opening transaction
3. **API** (FluentValidation) — request-level validation
4. **Desktop** — show warning before saving

**Note**: Full purchase-time credit limit validation is **Phase 25** (Purchase Invoice Module). Phase 32 creates the `IsCreditLimitExceeded()` method only.

### 6.5 SupplierResponse — Enhance vs. Deprecate

**Decision**: **Enhance existing `SupplierResponse`** by adding `AccountId`, `SupplierType`, `CreditLimit` fields rather than creating a new response type. Breaking change but all callers in Desktop use `SupplierDto` (which already has these fields conceptually through the service layer). The Response is used by API JSON serialization.

### 6.6 Why NOT a Separate Supplier Account Screen

The analysis (Part 3) shows suppliers auto-creating accounts. However, in V1:
- Supplier management is a standalone module
- Account management is a separate module (Phase 22)
- The link between them is a simple FK

**Decision**: Keep supplier and account creation conceptually separate in V1. The `AccountId` FK exists but isn't automatically populated. Supplier creation does NOT automatically create an Account in V1 — this is deferred to Phase 25 where the accounting integration wires them together.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason | Target |
|---------|--------|--------|
| **SupplierGroup** entity + CRUD | Analysis says no groups in V1 (Part 3, line 952) | V2 |
| **Auto-account creation** on supplier create | Requires Phase 25 accounting integration | Phase 25 |
| **Aging Report** (supplier debt aging) | Requires invoice date analysis + custom SQL | V2 |
| **Full Account Statement** (debit/credit per supplier) | Requires journal entry integration | Phase 25 |
| **Bulk supplier import** (Excel/CSV) | Advanced feature, not in V1 scope | V2 |
| **Supplier contract management** | Out of V1 scope | V3 |
| **Supplier purchase history dashboard** | Requires Phase 26 purchase module integration | Phase 30 |
| **Supplier communication log** | Out of V1 scope | V3 |
| **Supplier document attachments** | Analysis Part 4 (invoice attachment) — not V1 | V2 |
| **Purchase order → supplier integration** | PO module is deferred | V2 |
| **Supplier self-service portal** | Out of scope entirely | Future |
| **Multi-currency supplier balances** | Requires currency module full integration | Phase 28 |

---

## 8. Implementation Tasks

All tasks include:
- **Logging** (RULE-035/036): `Log.Information` on CRUD, `Log.Warning` on validation failures (RULE-183)
- **Error handling** (RULE-199/200/201): `LogSystemError()` in ViewModels, catch `DbUpdateException` in services
- **ToolTips** (RULE-185-190): Arabic action-oriented ToolTips on all interactive controls
- **UI Compact** (RULE-262-274): No hardcoded heights/paddings, 28px default, 12,6 header, 12,8 footer
- **INotifyDataErrorInfo** (RULE-228): All editor properties use `AddError`/`ClearErrors`
- **ValidateAllAsync** (RULE-229): Pre-save validation calls `ClearAllErrors()` + `AddError()` + `await ValidateAllAsync()`

### Task 0 — Default Supplier Name Confirmation

**Before any code is written**, confirm the default supplier seed name:

| Option | Value | Source |
|--------|-------|--------|
| **Recommended** | `"مورد نقدي"` | Analysis Part 2 (line 518), Part 3 (line 723) |
| Current | `"المورد الافتراضي في النظام"` | DbSeeder.cs line 130 |

**Decision**: Proceed with rename to `"مورد نقدي"` unless user explicitly requests otherwise. The name `"مورد نقدي"` (Cash Supplier) is:
- Standard in retail systems (محاسب سوفت, etc.)
- Clear and concise — fits in dropdown selects
- Consistent with SystemSetting display name `"المورد النقدي"`

**Estimate**: 0 minutes (decision only)

---

### Task 1 — BLOCKER 1: Rename Default Supplier + Add Defaults

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Rename default supplier from `"المورد الافتراضي في النظام"` to `"مورد نقدي"` |
| `Infrastructure/Data/DbSeeder.cs` | Reset `DefaultCashSupplierId` SystemSetting after new seed ID (verify Id=1) |

**Code change** (DbSeeder.cs lines 128-134): Rename default supplier string literal from `"المورد الافتراضي في النظام"` to `"مورد نقدي"`.

**Additionally** — verify SystemSetting `DefaultCashSupplierId` value matches the seeded supplier's Id:

> The default supplier seeds at Id=1 — SystemSetting `DefaultCashSupplierId` must reference the correct ID. See `docs/database-schema.md` for DbSeeder seeding order.

**Logging** (RULE-035):
- `Log.Information("Default supplier seeded: {SupplierName} (ID: {SupplierId})", supplier.Name, supplier.Id)`

**Validation**: Verify seed order — supplier must be seeded before SystemSetting references it.

**Estimate**: ~10 minutes

---

### Task 2 — BLOCKER 2: Add AccountId FK to Supplier

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Supplier.cs` | Add `int? AccountId` + `Account? Account` nav property |
| `Domain/Entities/Supplier.cs` | Update `Create()` factory to accept optional `accountId` parameter |
| `Domain/Entities/Supplier.cs` | Update `Update()` method to accept optional `accountId` parameter |
| `Domain/Entities/Supplier.cs` | Add Guard: no change for AccountId (optional FK, no guard needed) |
| `Infrastructure/Data/Configurations/SupplierConfiguration.cs` | Add FK config: `.Property(s => s.AccountId).IsRequired(false)` + `.HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Restrict)` |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE Suppliers ADD AccountId int NULL` + FK |
| `Contracts/DTOs/AllDtos.cs` — `SupplierDto` | Add `int? AccountId`, `string? AccountName` |
| `Contracts/Responses/SupplierResponse.cs` | Add `int? AccountId`, `decimal CreditLimit`, `SupplierType SupplierType` |
| `Contracts/Requests/SupplierRequests.cs` | Add `int? AccountId` to both Create and Update requests |
| `Application/Services/SupplierService.cs` | Update `MapToDto()` to map Account information |
| `Application/Interfaces/Services/ISupplierService.cs` | No interface change needed (dto change only) |

**Domain entity change** (Supplier.cs):

> Canonical Supplier entity pattern in `docs/AGENTS.md` §2.84 (RULE-442 to RULE-454). `AccountId` is non-nullable int (MANDATORY per RULE-442). `Supplier.Create()` accepts `accountId` as required parameter per RULE-444. `Supplier.Update()` accepts `accountId` as required parameter per RULE-445.

**Configuration change** (SupplierConfiguration.cs):

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions. AccountId FK uses `DeleteBehavior.Restrict` per RULE-214.

**Configuration change** (SupplierConfiguration.cs):

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions. AccountId FK uses `DeleteBehavior.Restrict` per RULE-214.

**DTO change** (AllDtos.cs):

> Canonical `SupplierDto` in `SalesSystem.Contracts/`. See `docs/AGENTS.md` §2.84 — NO `OpeningBalance`/`CurrentBalance` (sourced from linked Account). NO `SupplierType` (not in V1). `AccountId` is non-nullable int.

**Request changes** (SupplierRequests.cs):

> See `SalesSystem.Contracts/Requests/` for canonical definitions. MUST NOT have `AccountId` (auto-created by service per RULE-450). MUST NOT have `SupplierType` (not in V1 per RULE-443).

**Service mapping** (SupplierService.cs MapToDto):

> See `docs/AGENTS.md` §2.84 for canonical mapping pattern. `AccountName` sourced from linked Account entity via `.Include(s => s.Account)`.

**⚠️ Eager loading**: `GetByIdAsync` must `.Include(s => s.Account)` to populate Account navigation property.

**Logging**: `Log.Information("Supplier {Id} linked to Account {AccountId}", supplier.Id, accountId)` on create/update

**Estimate**: ~2 hours

---

### Task 2.1 — Auto-Create Journal Entry When OpeningBalance > 0

**Problem**: Analysis Part 3 (lines 946-948) requires that when creating a supplier with `OpeningBalance > 0`, the system must automatically create a journal entry to record the opening balance in the general ledger. This ensures the supplier's balance is reflected in the Chart of Accounts from day one.

**Design**:
- Debit: المخزون / بضاعة (Inventory/Stock account — or a configurable default account)
- Credit: حساب المورد (the supplier's auto-created sub-account under 2100 — حسابات الموردين)
- Entry type: `OpeningBalance` (value 9 — must be added to `JournalEntryType` enum in Phase 30)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/SupplierService.cs` | Add `CreateOpeningBalanceJournalEntryAsync()` method |
| `Application/Interfaces/Services/ISupplierService.cs` | Add method signature |
| `Contracts/DTOs/AllDtos.cs` — `SupplierDto` | Ensure `AccountId`, `AccountName` returned after creation |
| `Domain/Enums/` (Phase 30) | Ensure `JournalEntryType` has `OpeningBalance = 9` — **cross-reference** |

**Service method — CreateOpeningBalanceJournalEntryAsync()**:

> Canonical accounting integration pattern in `docs/AGENTS.md` §2.76 (Phase 24 — Accounting Integration Rules, RULE-371 to RULE-388). Opening balance creates journal entry: Dr Inventory / Cr Supplier Account. Uses `AccountingIntegrationService` — NEVER write raw journal entry logic in SupplierService.

**Integration into CreateAsync flow**:

> See `docs/AGENTS.md` §2.3 (Transaction Pattern — RULE-003, RULE-005) for the canonical transaction flow. Account auto-creation uses the CashBoxService pattern (auto-create Level-4 detail account under parent). Opening balance deferred — balance tracked on linked GL Account per RULE-446.

**Helper — CreateSupplierAccountAsync()**:

> Account auto-creation follows the CashBox pattern in `docs/AGENTS.md` §2.26 (RULE-077 to RULE-083). Auto-creates Level-4 detail account under parent `"2100 — حسابات الموردين"` per RULE-452.

**⚠️ Cross-Phase Dependencies**:
1. **Phase 22** (Chart of Accounts): Parent account `2100 — حسابات الموردين` and `1500 — المخزون` must be seeded
2. **Phase 30** (Journal Entry module): `JournalEntry`, `JournalEntryLine`, and `JournalEntryType.OpeningBalance = 9` must exist
3. If Phase 30 is not yet deployed, the journal entry creation step is **skipped gracefully** — supplier is created without the entry, and a warning is logged

**Logging**:
- `Log.Information("Opening balance journal entry created for Supplier {Id}: Amount={Amount}, AccountId={AccountId}", ...)`
- `Log.Warning("Could not create opening balance journal entry for Supplier {Id}: inventory account 1500 not found", ...)`
- `Log.Warning("Could not create opening balance journal entry for Supplier {Id}: parent account 2100 not found", ...)`

**Estimate**: ~2 hours

---

### Task 3 — Add SupplierType Enum + Property

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/SupplierType.cs` | **NEW** — SupplierType enum (Cash=0, Credit=1) |
| `Domain/Entities/Supplier.cs` | Add `SupplierType SupplierType { get; private set; }` property |
| `Domain/Entities/Supplier.cs` | Update `Create()` to accept `supplierType` param |
| `Domain/Entities/Supplier.cs` | Update `Update()` to accept `supplierType` param |
| `Infrastructure/Data/Configurations/SupplierConfiguration.cs` | Add `.Property(s => s.SupplierType).HasConversion<int>().HasDefaultValue(0)` |
| `Contracts/DTOs/AllDtos.cs` | Already added in Task 2 |
| `Contracts/Requests/SupplierRequests.cs` | Already added in Task 2 |
| `Contracts/Responses/SupplierResponse.cs` | Already added in Task 2 |
| `Api/Validators/SupplierRequestValidators.cs` | Add `IsInEnum()` rule for SupplierType |
| `Application/Services/SupplierService.cs` | Map SupplierType in MapToDto + search filtering |
| `Infrastructure/Data/DbSeeder.cs` | Set `SupplierType.Cash` for default supplier |

**Enum definition** — file: `Domain/Enums/SupplierType.cs`:

> See `docs/AGENTS.md` §3 for canonical enum values. `SupplierType` is NOT in V1 — deferred per RULE-443. Payment type is per-invoice (`SalesInvoice.PaymentType`), not per-supplier.

**Logging**: `Log.Information("Supplier {Id} type changed to {SupplierType}", id, supplierType)`

**Estimate**: ~30 minutes

---

### Task 4 — Add CreditLimit Validation + Balance Report Methods

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ISupplierService.cs` | Add `GetBalanceReportAsync()` and `GetCreditLimitUsageAsync()` methods |
| `Application/Services/SupplierService.cs` | Implement balance report + credit limit methods |
| `Contracts/DTOs/AllDtos.cs` | Add `SupplierBalanceReportDto` and `CreditLimitUsageDto` |

**Service interface additions**:

> Service interface pattern per `docs/AGENTS.md` §2.5 (RULE-006 — ALL service methods return Result<T>). New methods: `GetBalanceReportAsync()`, `GetCreditLimitUsageAsync(int)`. Balance sourced from linked Account per RULE-446 — see `docs/AGENTS.md` §2.84 for canonical service pattern.

/// <summary>
/// Check if purchase would exceed supplier credit limit.
/// Called from PurchaseInvoiceService during purchase creation.
/// </summary>

> Credit limit validation pattern: see `docs/AGENTS.md` §2.84 (RULE-448 — `Supplier.CheckCreditLimit(decimal additionalAmount)` is a non-throwing domain method returning bool). Validation is a SOFT WARNING only — caller decides whether to block.

**⚠️ Note**: `IsCreditLimitExceededAsync` is created here but **called** from Phase 26 (Purchase Invoice Module). Phase 32 creates the method + wires it into the service.

**DTOs**:

> See `SalesSystem.Contracts.DTOs.Reports/` for canonical report DTO definitions. Balance is sourced from linked Account (not Supplier entity per RULE-446).

**Logging**:
- `Log.Information("Supplier balance report generated — {Count} suppliers", count)`
- `Log.Warning("Supplier {Id} exceeds credit limit: balance={Balance}, limit={Limit}", id, balance, limit)` (RULE-183 — user data, warning level)

**Estimate**: ~1 hour

---

### Task 5 — Add 3 New Report Endpoints to API

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/SuppliersController.cs` | Add 3 new GET endpoints |

**New endpoints**:

> Controller pattern per `docs/AGENTS.md` §2.5 (RULE-025 — Controllers translate Result to HTTP status codes). All controllers inject service interfaces only per RULE-203. Use `NotFound` for `ErrorCodes.NotFound` and `BadRequest` for business validation errors per RULE-288.

**Controller purity** (RULE-203): All endpoints inject `ISupplierService` only — no DbContext.

**Estimate**: ~30 minutes

---

### Task 6 — BLOCKER 3: Fix SupplierListViewModel ExecuteAsync Pattern

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Suppliers/SupplierListViewModel.cs` | Replace manual try/catch/finally with ExecuteAsync wrapper |

**Changes required**:
1. `LoadSuppliersAsync()` — rename to `LoadSuppliersOperationAsync()`, remove `IsBusy`/`try/catch/finally`
2. `RefreshCommand` — wrap in `ExecuteAsync()`
3. `DeleteSupplierAsync()` — wrap in `ExecuteAsync()`
4. `RestoreSupplierAsync()` — wrap in `ExecuteAsync()`
5. Remove `HandleException()` call — replace with `LogSystemError()` + `HandleFailure()`

**Pattern**:

> See `docs/AGENTS.md` §2.36 (RULE-141 to RULE-146 — ExecuteAsync Pattern) for the canonical ViewModel command wrapper. All async commands wrapped in `ExecuteAsync()` — NO manual try/catch, NO manual `IsBusy`.

**⚠️ Note**: RULE-059 says commands must NOT have CanExecute predicates. The existing `DeleteCommand` and `RestoreCommand` use `() => SelectedSupplier != null` predicates — these must be removed. Buttons remain enabled, validation happens on click.

**Estimate**: ~1.5 hours

---

### Task 7 — Enhance SupplierEditorViewModel with New Fields

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add `_supplierType`, `_selectedAccountId`, `_accountName`, `_accountSearchText` fields |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add properties: `SelectedSupplierType`, `AccountId`, `AccountName`, `AccountSearchText` |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add `Accounts` ObservableCollection + `LoadAccountsAsync()` |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add Arabic ToolTip constants for new fields |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update `ValidateAsync()` for new fields |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update `SaveOperationAsync()` to include new fields in request |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update constructor to accept `IAccountApiService` |

**New properties**:

> See `docs/AGENTS.md` §2.84 for canonical supplier editor ViewModel pattern. Key updates per RULE-451: Desktop Supplier Editor MUST NOT have OpeningBalance input, AccountId selection, or SupplierType radio. AccountName is display-only after save. NO `SupplierType` in V1 (deferred per RULE-443).

**Validation additions**:

> See `docs/AGENTS.md` §2.23 (RULE-059 — Save always enabled, validate on click). Editor uses `ClearAllErrors()` + `AddError()` + `await ValidateAllAsync()` per RULE-229.

**⚠️ Note**: `IAccountApiService` is a new dependency. Register in DI. If Chart of Accounts module is not yet available, make account selection optional (hide when service returns no data).

**Logging**: `Log.Information("Supplier editor loaded {Count} accounts for selection", accounts.Count)`

**Estimate**: ~2 hours

---

### Task 8 — Update SupplierEditorView.xaml (Compact + New Fields)

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SupplierEditorView.xaml` | Add SupplierType RadioButtons (Cash/Credit) |
| `Views/Suppliers/SupplierEditorView.xaml` | Add AccountId ComboBox (Chart of Accounts selection) |
| `Views/Suppliers/SupplierEditorView.xaml` | Add CreditLimit field (visible only for Credit suppliers) |
| `Views/Suppliers/SupplierEditorView.xaml` | Add CurrentBalance read-only display (for edit mode) |
| `Views/Suppliers/SupplierEditorView.xaml` | Fix hardcoded sizes to use compact styles (RULE-262-274) |

**New XAML sections to add**:

**New XAML sections to add**:

> XAML patterns follow compact global styles in `docs/AGENTS.md` §2.64 (RULE-262-274). Per RULE-451: Desktop Supplier Editor MUST NOT have SupplierType radio, AccountId selection combo, or OpeningBalance input. AccountName is display-only after save.

**Compact UI fixes** (RULE-262-274):
- Remove `Height="600" Width="550"` from Window element (use `MinHeight/MinWidth` only)
- Verify no hardcoded `Height="36"` or `Height="40"` on TextBox/Button elements
- Remove `Border Height="12"` spacers — replace with `Margin="0,0,0,6"` on fields
- Header: already uses `Padding="12,6"` ✅
- Footer: already uses `Padding="12,8"` ✅

**ToolTips** (RULE-185-190):
- Cash radio: `"مورد نقدي — يتم الدفع عند استلام البضاعة"`
- Credit radio: `"مورد آجل — له حد ائتماني ويتم السداد لاحقاً"`
- Account combo: `"اختيار حساب محاسبي من دليل الحسابات — اختياري"`
- CreditLimit field: `"الحد الأقصى للائتمان — سيتم منع إضافة فواتير تتجاوز هذا الحد"`

**Estimate**: ~2 hours

---

### Task 9 — Update SupplierEditorView Code-Behind

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SupplierEditorView.xaml.cs` | Add Loaded event handler for Accounts loading |
| `Views/Suppliers/SupplierEditorView.xaml.cs` | Wire `FocusFirstInvalidFieldRequested` |

**Code-behind**:

> Window code-behind pattern follows `docs/AGENTS.md` §2.53 (RULE-224 — PositionOverOwner guards against self-ownership) and §2.40 (ScreenWindowService for non-modal screens).

**Estimate**: ~15 minutes

---

### Task 10 — Enhance SuppliersListView with Balance Display + New Columns

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SuppliersListView.xaml` | Add SupplierType column, AccountId column |
| `Views/Suppliers/SuppliersListView.xaml` | Enhance CurrentBalance column with color coding |
| `Views/Suppliers/SuppliersListView.xaml` | Add CreditLimit column |
| `ViewModels/Suppliers/SupplierListViewModel.cs` | Add `SupplierType`, `AccountName` to search/filter predicates |

**New DataGrid columns**:

> XAML DataGrid patterns use compact global styles per `docs/AGENTS.md` §2.64 (RULE-262-274). Balance display sourced from linked Account — not from Supplier entity per RULE-446.

**⚠️ Note**: Negative balance DataTrigger won't work directly with decimal bindings (WPF limitation). Use a `BalanceColor` computed property in the ViewModel or a value converter instead. Balance direction per `docs/AGENTS.md` §2.8 (RULE — Supplier balance > 0 = We owe the supplier).

**Estimate**: ~1.5 hours

---

### Task 11 — Update FluentValidators + Validator Tests

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/SupplierRequestValidators.cs` | Add TaxNumber format, AccountId exists, SupplierType enum rules |
| `Tests/SalesSystem.Api.Tests/Validators/SupplierRequestValidatorTests.cs` | Update tests for new rules |

**Validator enhancements**:

> FluentValidation pattern per `docs/AGENTS.md` §2.13 (RULE-044 — FluentValidation for EVERY Command). Phone number uses regex `^05\d{8}$` per RULE-454. TaxNumber uses `MaximumLength(30)`. `nameof` operator used for property references per RULE-351.

**Estimate**: ~30 minutes

---

### Task 12 — Update Seed Data with Defaults

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Set AccountId for default supplier (link to Accounts Payable) |
| `Infrastructure/Data/DbSeeder.cs` | Set SupplierType = Cash for default supplier |

**Seed update**:

> DbSeeder pattern per `docs/AGENTS.md` §2.84 (RULE-452 — Account auto-created under 2100 parent). Uses two-pass approach: seed supplier first (without AccountId), then update after AccountingSeeder with the account FK.

**Estimate**: ~20 minutes

---

### Task 13 — Add ISupplierApiService Methods for New Endpoints

**Files**:

| File | Change |
|------|--------|
| `Services/Api/IApiService.cs` — `ISupplierApiService` | Add `GetBalanceReportAsync()`, `GetCreditLimitUsageAsync(int id)`, `GetTransactionHistoryAsync(int id, DateTime?, DateTime?)` |
| `Services/Api/SupplierWarehouseApiService.cs` | Implement the 3 new HTTP methods |

**Interface additions**:

> API service interfaces follow `SalesSystem.DesktopPWF/Services/Api/ISupplierApiService.cs` pattern with `ExecuteAsync<T>()` wrapper (shared from `ApiServiceBase`). See `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern. DTOs defined in `SalesSystem.Contracts.DTOs.Suppliers/`.

**Estimate**: ~30 minutes

---

### Task 14 — Enhanced Search (By SupplierType + Account)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/SupplierService.cs` | Extend `GetAllAsync` search predicate with SupplierType filtering |

**Search predicate extension**:

> Search predicate pattern follows existing `GetAllAsync` implementation in `SupplierService`. Per RULE-192: search/filter by Id or Name only — NO Code column. Enhanced search extends the current predicate with AccountId integer matching and Account.NameAr string matching. See `docs/AGENTS.md` §2.45 for the identifier strategy (RULE-191 to RULE-198).

**Estimate**: ~30 minutes

---

### Task 15 — Add SupplierChangedMessage + Account Integration Events

**Files**:

| File | Change |
|------|--------|
| `Messaging/Messages/AppMessages.cs` | Verify `SupplierChangedMessage` exists (already created) |

**Already exists**: The EventBus message `SupplierChangedMessage` is already defined and used in:
- `SupplierEditorViewModel` — publishes on save
- `SupplierListViewModel` — subscribes + triggers reload
- `SupplierPaymentsListViewModel` — subscribes for balance refresh

**No changes needed** — verify EventBus integration is complete.

**Estimate**: 0 minutes (verification only)

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | Supplier.CreditLimit, CurrentBalance, OpeningBalance — all `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | Supplier.Create + Account.Create (future Phase 25) | ✅ Deferred |
| **RULE-006** | ALL services return `Result<T>` | SupplierService: all methods return `Result<T>` or `Result` | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Name, Phone, Email, Address, TaxNumber | ✅ |
| **RULE-016** | BaseEntity audit fields | Supplier inherits BaseEntity (CreatedAt, CreatedByUserId, IsActive) | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | SupplierService uses `_uow` for all DB operations | ✅ |
| **RULE-035** | Serilog for logging | All CRUD operations logged via `_logger.LogInformation` | ✅ |
| **RULE-036** | Log critical operations | Supplier create/update/delete + CreditLimit exceeded | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | SuppliersController: `[Authorize(Policy = "ManagerAndAbove")]` | ✅ |
| **RULE-039** | BCrypt work factor 12 | Not in supplier scope | ✅ N/A |
| **RULE-042** | Rich Domain — `private set` + methods | Supplier: Create(), Update(), IncreaseBalance(), DecreaseBalance(), LinkToAccount() | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateSupplierRequestValidator + UpdateSupplierRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | SupplierListViewModel: `ShowDeleteConfirmationAsync()` → Cancel/Deactivate/Permanent | ✅ |
| **RULE-052** | Guard Clauses on all entities | Supplier.Create: Name guard, OpeningBalance guard, CreditLimit guard | ✅ |
| **RULE-053** | DomainException in Arabic | "اسم المورد مطلوب", "الرصيد الافتتاحي لا يمكن أن يكون سالباً" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all supplier ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | SupplierEditorViewModel — AddError/ClearErrors (RULE-228) | ✅ |
| **RULE-059** | Save always enabled, validate on click | SupplierEditorViewModel — NO CanExecute blocking | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | **BLOCKER 3** — SupplierListViewModel needs refactor (Task 6) | ⚠️ **Fix in Task 6** |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | SupplierEditor opens via `_screenWindowService.OpenScreen()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | `HandleFailure(result.Error, "SupplierListViewModel.Delete")` | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ المورد"`, `"خطأ في تحميل الموردين"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | ALL dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync`, `ShowConfirmationAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, unexpected exceptions | ✅ |
| **RULE-183** | Log.Warning for user mistakes | CreditLimit exceeded, validation failures, supplier not found | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | SupplierApiService inherits ApiServiceBase with content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, inputs across SuppliersListView + EditorView | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "فتح شاشة إضافة مورد جديد" ✅, not "مورد جديد" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Delete: "حذف أو إلغاء تنشيط العنصر المحدد" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة الموردين — إضافة وتعديل بيانات الموردين" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة مورد — فتح شاشة إضافة مورد جديد" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModel catch blocks use LogSystemError() | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | SupplierService.PermanentDeleteAsync catches FK violations | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All supplier ViewModels | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | SupplierService — all CRUD + new report methods | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | SuppliersController — injects ISupplierService only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | Supplier.AccountId FK → Restrict (Task 2) | ✅ |
| **RULE-220** | Newest-first sorting on lists | SupplierListViewModel: `OrderByDescending(x => x.Id)` | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | SupplierEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | SupplierEditorViewModel — AddError/ClearErrors | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | Not in supplier scope | ✅ N/A |
| **RULE-244** | User hard-delete guarded | Not in supplier scope | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not in supplier scope | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not in supplier scope | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | Task 8: remove hardcoded sizes from SupplierEditorView | ⚠️ **Fix in Task 8** |
| **RULE-263** | No hardcoded Padding="16+" on buttons | Already fixed — styles provide 10,4 | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | Already correct in SupplierEditorView | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Task 8: replace `Border Height="12"` with `Margin="0,0,0,6"` | ⚠️ **Fix in Task 8** |
| **RULE-266** | Dialog titles FontSize=16 max | Title = `FontSize="20"` in SupplierEditorView header ❌ → fix to 16 | ⚠️ **Fix in Task 8** |
| **RULE-267** | Section headers FontSize=14 max | No section headers ❌ | ✅ N/A |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | In SuppliersListView ✅ | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | Emoji icon in header = FontSize 20 ✅ | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | SupplierEditorWindow: MinWidth=500 MinHeight=500 | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | Save button `Width="140"` ❌ → use `MinWidth="100"` | ⚠️ **Fix in Task 8** |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | `Height="600" Width="550"` on Window ❌ → remove | ⚠️ **Fix in Task 8** |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **AccountId FK blocks migration if Accounts table doesn't exist** | **HIGH** — migration fails if Accounting module (Phase 22) not deployed | Make FK nullable + ensure Phase 22 accounts seeded first. Add FK as additive column only |
| **Supplier seed order conflict with AccountingSeeder** | Medium — supplier seeded before accounts exist | Implement two-pass seed: first pass (null AccountId), second pass (update with AccountId) |
| **SupplierType enum default value mismatch** | Low — existing suppliers get Cash=0 | Explicitly map `HasDefaultValue(0)` in configuration |
| **ExecuteAsync() refactor breaks existing behavior** | Medium — `DeleteSupplierAsync` and `RestoreSupplierAsync` also need refactoring | Test all 3 async methods after refactor (Task 6) |
| **Chart of Accounts module not yet available** | Low — Account selection hidden/disablded | Make AccountId editor conditional: hide when `Accounts.Count == 0` |
| **TaxNumber format validation too strict** | Low — some Saudi suppliers have different VAT formats | Use regex `^\d{15}$` for KSA VAT, make format validation optional (warning level) |
| **Balance color coding in DataGrid** | Low — WTF limitation with decimal DataTriggers | Use `IValueConverter` or computed string property on DTO wrapper |
| **Two-pass seed complexity** | Low — increases seed code complexity | Keep it simple: default supplier seeded with null AccountId, second update in same method |
| **UI compact fixes break existing layout** | Low — removing `Border Height="12"` spacers changes vertical spacing | Test all 5 editor forms visually after compact fixes |
| **SupplierReports query performance** | Low — balance report aggregates across invoices/payments (future) | Add pagination to report endpoints, defer full aggregation to Phase 26 |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **Default supplier rename causes issues** | `UPDATE Suppliers SET Name = N'المورد الافتراضي في النظام' WHERE Id = 1 AND Name = N'مورد نقدي'` |
| **AccountId FK migration fails** | `ALTER TABLE Suppliers DROP CONSTRAINT FK_Suppliers_Accounts_AccountId; ALTER TABLE Suppliers DROP COLUMN AccountId;` |
| **SupplierType column causes issues** | `ALTER TABLE Suppliers DROP COLUMN SupplierType;` |
| **New FluentValidators too strict** | Remove or relax TaxNumber regex, make AccountId rule conditional |
| **SupplierListViewModel ExecuteAsync refactor breaks** | Revert to original `try/catch/finally` pattern, re-apply with full testing |
| **New endpoints cause routing conflicts** | Remove 3 new endpoints from SuppliersController — no breaking change to existing endpoints |
| **UI compact changes break layout** | Revert SupplierEditorView.xaml and SuppliersListView.xaml to original versions |
| **Two-pass seed logic faulty** | Simplify: seed default supplier with null AccountId, skip second pass |
| **Report DTOs cause serialization errors** | Remove `SupplierBalanceReportDto` and `CreditLimitUsageDto` from API responses |
| **Entire Phase**

To be rolled back as group:

> See `docs/database-schema.md` for rollback SQL reverting AccountId and SupplierType column additions. Code revert via `git revert <phase-merge-commit>`.

---

## Implementation Summary

| Task | Description | Files Changed | Estimate |
|------|-------------|---------------|----------|
| **Task 0** | Default supplier name confirmation | 0 (decision only) | 0 min |
| **Task 1** | Rename default supplier in DbSeeder | 1 | 10 min |
| **Task 2** | Add AccountId FK to Supplier + migration | 8+ | 2 hours |
| **Task 3** | Add SupplierType enum + property | 8+ | 30 min |
| **Task 4** | CreditLimit validation + balance report methods | 3 | 1 hour |
| **Task 5** | Add 3 new report endpoints to API | 1 | 30 min |
| **Task 6** | Fix SupplierListViewModel ExecuteAsync pattern | 1 | 1.5 hours |
| **Task 7** | Enhance SupplierEditorViewModel with new fields | 1 | 2 hours |
| **Task 8** | Update SupplierEditorView.xaml (compact + new fields) | 1 | 2 hours |
| **Task 9** | Update SupplierEditorView code-behind | 1 | 15 min |
| **Task 10** | Enhance SuppliersListView with balance + columns | 2 | 1.5 hours |
| **Task 11** | Update FluentValidators + tests | 2 | 30 min |
| **Task 12** | Update seed data with AccountId + SupplierType | 1 | 20 min |
| **Task 13** | Add API service methods for new endpoints | 2 | 30 min |
| **Task 14** | Enhanced search by SupplierType + Account | 1 | 30 min |
| **Task 15** | Verify EventBus integration | 0 | 0 min |
| **Total** | **15 tasks** | **~33 files** | **~13 hours** |

### Key Metrics

- **New enums**: 1 (`SupplierType`)
- **New DTOs**: 3 (`SupplierBalanceReportDto`, `CreditLimitUsageDto`, `SupplierTransactionDto`)
- **New properties on Supplier**: 2 (`AccountId`, `SupplierType`)
- **New API endpoints**: 3 (balance-summary, credit-limit, transactions)
- **New ViewModel properties**: ~6 (SelectedSupplierType, Accounts list, AccountId, etc.)
- **New XAML fields**: SupplierType radio, AccountId combo, CreditLimit, CurrentBalance display
- **Critical bugs fixed**: 3 (Blocker 1: rename, Blocker 2: AccountId FK, Blocker 3: ExecuteAsync)
- **Validation rules enhanced**: 3 (TaxNumber format, AccountId exists, SupplierType enum)
- **Search enhanced**: By SupplierType + Account name

### Execution Order

```
Task 0  (Decision)
  ↓
Task 1  (Rename seed) ───────┐
Task 3  (SupplierType enum) ──┤
                              ├──→ Task 2 (AccountId FK — depends on Tasks 1,3)
                              │       ↓
Task 12 (Seed updates) ←──────┘       ↓
                              Task 4-5 (Service + API methods)
                                      ↓
                              Task 6-10 (Desktop UI — ViewModels + Views)
                                      ↓
                              Task 11 (Validators)
                                      ↓
                              Task 13 (API service methods)
                                      ↓
                              Task 14 (Enhanced search)
                                      ↓
                              Task 15 (Verify EventBus)
```

**Parallelizable**:
- Tasks 1 + 3 (independent) → then Task 2 (depends on both)
- Tasks 4 + 5 (Service + Controller) → parallel
- Tasks 7 + 10 (ViewModels) → parallel
- Tasks 8 + 9 (Views) → depend on Task 7

---

### Task 16 — Comprehensive Unit Tests: Domain + Service + Validation + Config

**Files**:

| File | Change |
|------|--------|
| `Tests/SalesSystem.Domain.Tests/Entities/SupplierTests.cs` | **NEW** — entity factory tests |
| `Tests/SalesSystem.Application.Tests/Services/SupplierServiceTests.cs` | **NEW** — service layer tests |
| `Tests/SalesSystem.Api.Tests/Validators/SupplierRequestValidatorTests.cs` | **Update** — add new field rules |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/SupplierConfigurationTests.cs` | **NEW** — EF config tests |

---

#### 16.1 — Domain Entity Tests (Supplier.Create)
Test every `Create()` factory method with valid/invalid inputs:

> Test patterns follow existing entity tests in `SalesSystem.Domain.Tests/Entities/`. See `docs/AGENTS.md` §2.20 (RULE-052/053 — Guard Clauses with DomainException) for the canonical domain entity test pattern. Key tests: factory method invariants, guard clause coverage (empty name, negative balance, max length), balance methods, AccountId linking.

---

#### 16.2 — Service Tests (Mock\<IUnitOfWork\>)

> Service test patterns follow existing mock-based tests in `SalesSystem.Application.Tests/Services/`. See `docs/AGENTS.md` §2.5 (Result<T> pattern) and §2.3 (Transaction pattern) for the canonical test setup. Tests cover: CreateAsync (success/empty name/negative balance), GetByIdAsync (found/not found), transaction rollback on exception, GetAllAsync paged results, soft delete.

---

#### 16.3 — FluentValidation Tests

> FluentValidation test pattern follows `SalesSystem.Api.Tests/Validators/` conventions. Per RULE-044: EVERY Command MUST have associated validator tests. Key validation rules per RULE-454: Phone regex `^05\d{8}$`, Email `.EmailAddress()`, Name required with Arabic message. See `docs/AGENTS.md` §2.13 for the canonical validation layer pattern.

---

#### 16.4 — Database Configuration Tests

> EF Core configuration test pattern follows `SalesSystem.Infrastructure.Tests/Configurations/`. Tests cover: table name, MaxLength constraints, precision (RULE-001/002 — decimal(18,2) for money, decimal(18,3) for quantities), DeleteBehavior.Restrict on ALL FKs (RULE-214), AccountId non-nullable (RULE-442), IsActive query filter (RULE-016). See `docs/AGENTS.md` §2.16 for EF Core conventions.

---

#### 16.5 — Phase-Specific Integration Tests

> Integration test patterns follow `SalesSystem.Application.Tests/Services/SupplierServiceTests.cs`. Tests cover: account auto-creation under 2100 parent (RULE-452), journal entry for opening balance (Dr OpeningBalanceEquity / Cr AccountsPayable per RULE-372), transactional integrity with rollback on failure (RULE-281 — ExecuteTransactionAsync), AccountId FK eager loading (RULE-442). See `docs/AGENTS.md` §2.84 for canonical Supplier entity rules.

---

#### 16.6 — Update Test

> Update test pattern follows existing service test conventions. Key tests: UpdateAsync with valid/invalid request (Delegate validation to FluentValidation per RULE-044), UpdateAsync when entity not found returns `Result.Failure` with `ErrorCodes.NotFound` per RULE-202/288. See `docs/AGENTS.md` §2.5 for canonical Result<T> pattern.

| Test Sub-Task | Focus | Files | Estimate |
|----------------|-------|-------|----------|
| 16.1 Domain Entity | `Supplier.Create` factory + guards + balance methods | `SupplierTests.cs` | 2 hours |
| 16.2 Service Layer | `CreateAsync`, `GetByIdAsync`, transactions, rollback | `SupplierServiceTests.cs` | 3 hours |
| 16.3 FluentValidation | Name, TaxNumber, OpeningBalance, SupplierType rules | `SupplierRequestValidatorTests.cs` | 1.5 hours |
| 16.4 DB Config | Precision, MaxLength, Restrict FK, nullable AccountId | `SupplierConfigurationTests.cs` | 1.5 hours |
| 16.5 Phase-Specific | Account auto-creation, journal entry, transactional flow | `SupplierServiceTests.cs` | 2 hours |
| 16.6 Update Tests | UpdateAsync valid/invalid scenarios | `SupplierServiceTests.cs` | 1 hour |
| **Total** | **6 sub-tasks** | **4 test files** | **~11 hours** |
