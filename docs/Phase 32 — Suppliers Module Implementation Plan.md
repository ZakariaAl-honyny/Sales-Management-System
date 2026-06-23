# Phase 32 — Suppliers Module: Comprehensive Implementation Plan

> **Version**: 2.0 — Architecture update: DIRECT contact fields on Supplier (NO Parties, NO SupplierGroup, NO SupplierType), AccountId mandatory under parent "1320"
> **Scope**: Complete Suppliers Module enhancement — AccountId FK mandatory (auto-created under 1320), direct contact fields, CreditLimit validation, UI balance display, reports, and 12 implementation tasks

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
│  │  │  direct flds)│  │   AccountId) │             │                │
│  │  └─────────────┘  └──────────────┘             │                │
│  │  ┌─────────────┐                               │                │
│  │  │ PurchaseInvoices│                            │                │
│  │  │ (FK: SupplierId)│                            │                │
│  │  └─────────────┘                               │                │
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
| **Domain** (Supplier entity) | Business rules: Name required, CreditLimit >= 0, Guard Clauses, direct contact fields | ⛔ No DB access, no UI logic |
| **Application** (SupplierService) | Orchestration: Result<T>, IUnitOfWork, logging, CreditLimit validation on purchase, auto-account creation under 1320 | ⛔ No direct DB access, no Domain duplication |
| **API** (SuppliersController) | HTTP translation: map Result → StatusCode, secure with [Authorize] | ⛔ No business logic, no DbContext |
| **Desktop** (ViewModels + Views) | UI: ExecuteAsync(), INotifyDataErrorInfo, DialogService, Arabic ToolTips | ⛔ No DB access, no business rules |

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Layer ✅

**File**: `SalesSystem.Domain.Entities.Supplier`

| Field | Type | Default | Required | Status |
|-------|------|---------|----------|--------|
| `Id` | `int PK` | Auto | ✅ | ✅ Exists |
| `Name` | `string` | `""` | ✅ | **🔧 TO ADD (direct field)** |
| `Phone` | `string?` | `null` | ❌ | **🔧 TO ADD (direct field)** |
| `Email` | `string?` | `null` | ❌ | **🔧 TO ADD (direct field)** |
| `Address` | `string?` | `null` | ❌ | **🔧 TO ADD (direct field)** |
| `TaxNumber` | `string(30)?` | `null` | ❌ | **🔧 TO ADD (direct field)** |
| `Notes` | `string(500)?` | `null` | ❌ | **🔧 TO ADD (direct field)** |
| `PartyId` | `int FK` | — | ✅ | **🔧 TO REMOVE** (contact data now direct) |
| `AccountId` | `int FK` | — | ✅ | ✅ Exists (non-nullable, mandatory) |
| `CategoryId` | `int? FK` | `null` | ❌ | ✅ Exists |
| `CreditLimit` | `decimal(18,2)` | `0` | ❌ | **🔧 TO ADD** |
| `IsActive` | `bool` | `true` | ❌ | ✅ Exists (from BaseEntity) |
| `CreatedAt` | `DateTime` | — | ✅ | ✅ Exists (from BaseEntity) |
| `CreatedByUserId` | `int?` | — | ❌ | ✅ Exists (from BaseEntity) |

**Key architectural decisions (already implemented):**
- ✅ `AccountId` is **mandatory, non-nullable** `int` FK → `Account` — balance lives on linked GL Account
- ✅ **NO** `OpeningBalance` or `CurrentBalance` on Supplier entity — balance comes from JournalEntryLines
- ✅ **NO** `SupplierType` — payment terms are per-invoice (SalesInvoice.PaymentType), NOT per-supplier
- ✅ **NO** `SupplierGroupId` — SupplierGroup deferred to V2
- ✅ **NO** `CurrencyId` — currency is per-transaction, not per-supplier
- ✅ **NO** `PartyId` — contact fields (Name, Phone, Email, Address, TaxNumber, Notes) are DIRECT on Supplier

**Domain methods** (all exist + planned additions):
- `Supplier.Create(name, phone, email, address, taxNumber, notes, accountId, creditLimit, categoryId, createdByUserId)` — factory with Guard Clauses **🔧 TO UPDATE (direct fields instead of PartyId)**
- `Supplier.Update(name, phone, email, address, taxNumber, notes, creditLimit, categoryId, updatedByUserId)` — mutation with Guard Clauses **🔧 TO UPDATE**
- `CheckCreditLimit(decimal additionalAmount)` — **NEW**: non-throwing bool domain method (soft warning per RULE-448)

**Balance Direction** (RULE-008 convention):
```
Supplier's Account balance > 0 = We owe the supplier (liability)
Supplier's Account balance < 0 = Supplier owes us (asset/prepayment)
```

### 2.2 Infrastructure Layer ✅

**File**: `Infrastructure.Data.Configurations.SupplierConfiguration`

| Configuration | Value | Status |
|--------------|-------|--------|
| Table name | `Suppliers` | ✅ Exists |
| PK | `Id` (int, auto-increment) | ✅ Exists |
| Name | `nvarchar(150)` required | **🔧 TO ADD (direct field)** |
| Phone | `nvarchar(20)` nullable | **🔧 TO ADD (direct field)** |
| Email | `nvarchar(100)` nullable | **🔧 TO ADD (direct field)** |
| Address | `nvarchar(250)` nullable | **🔧 TO ADD (direct field)** |
| TaxNumber | `nvarchar(30)` nullable | **🔧 TO ADD (direct field)** |
| Notes | `nvarchar(500)` nullable | **🔧 TO ADD (direct field)** |
| PartyId | `int FK → Parties` Restrict | **🔧 TO REMOVE** |
| AccountId | `int FK → Accounts` Restrict | ✅ Exists |
| CategoryId | `int?` | ✅ Exists (no FK — type mismatch) |
| CreditLimit | `decimal(18,2)` | **🔧 TO ADD** |
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
- `MapToDto` maps all fields ✅
- Serilog logging on CRUD operations ✅
- **🔧 UPDATE**: `CreateAsync` must accept direct contact fields (Name, Phone, Email, Address, TaxNumber, Notes) — NOT PartyId
- **🔧 UPDATE**: `CreateAsync` must auto-create Account under parent "1320" (Accounts Payable/الموردون) when AccountId not provided
- **🔧 UPDATE**: `GetByIdAsync` must `.Include(s => s.Account)` for AccountName

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
| `ViewModels.Suppliers.SupplierEditorViewModel` | 236 | ✅ Exists (**🔧 UPDATE**: direct fields instead of Party) |
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
- **🔧 UPDATE**: Direct fields (Name, Phone, Email, Address, TaxNumber, Notes) on VM instead of nested Party
- **🔧 UPDATE**: Remove all Party-related loading/validation
- **🔧 ADD**: CreditLimit field + AccountName display-only

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
- ❌ Missing: AccountName column, CreditLimit column
- ❌ Missing: Balance display widget/indicator

**SupplierEditorView.xaml features:**
- Header with icon + title ✅
- Form fields: Name*, Phone, Address, TaxNumber, Email, IsActive ✅
- Arabic ToolTips (RULE-185-190) ✅
- Helper text beneath fields ✅
- Footer with Save + Cancel buttons ✅
- Loading overlay ✅
- ❌ Missing: CreditLimit field
- ❌ Missing: Notes field
- ❌ Missing: AccountName display (read-only, show after save)
- ❌ Missing: Current balance display in editor (read-only)

#### Services

| File | Status |
|------|--------|
| `Services.Api.ISupplierApiService` — 6 methods | ✅ Exists |
| `Services.Api.SupplierApiService` — HTTP implementation | ✅ Exists |

### 2.6 Contracts Layer ✅

**File**: `Contracts.DTOs.AllDtos`

> `SupplierDto` — current fields: Id, Name, Phone, Email, Address, TaxNumber, Notes, AccountId (int, non-nullable), AccountName, CategoryId, CreditLimit, IsActive. **NO** `PartyId`, **NO** `SupplierType`, **NO** `SupplierGroupId`, **NO** `OpeningBalance`/`CurrentBalance`.

**File**: `Contracts.Responses.SupplierResponse`

> `SupplierResponse` — mirrors SupplierDto fields. **NO** PartyId, SupplierType, OpeningBalance, CurrentBalance.

### 2.7 Validators (FluentValidation) ✅

**File**: `Api.Validators.SupplierRequestValidators`

| Rule | Create | Update |
|------|--------|--------|
| Name required + max 150 | ✅ | ✅ |
| Phone max 20 | ✅ | ✅ |
| Email format + max 100 | ✅ | ✅ |
| TaxNumber max 30 | ✅ | ✅ |
| Notes max 500 | **🔧 TO ADD** | **🔧 TO ADD** |
| CreditLimit >= 0 | **🔧 TO ADD** | **🔧 TO ADD** |

**Missing:**
- ❌ TaxNumber format validation (Saudi: 15 digits) — optional regex
- ❌ CreditLimit >= 0 validation
- ❌ Notes max length validation

### 2.8 Seed Data ⚠️

**File**: `Infrastructure.Data.DbSeeder`

> The default supplier `"مورد نقدي"` is created with auto-created account linked to COA under parent "1320" (Accounts Payable/الموردون).

**Issues (fixed):**
1. ✅ Name changed to `"مورد نقدي"` per AGENTS.md RULE-453
2. ✅ `AccountId` auto-created via service — linked to Accounts Payable (1320)
3. ✅ **NO** `SupplierType` — NOT in V1 (deferred per RULE-443)
4. ✅ **NO** `SupplierGroup` — NOT in V1

**🔧 UPDATE**: Direct contact fields (Name, Phone, Email, Address, TaxNumber, Notes) on Supplier entity — no Party record needed.

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: Direct Contact Fields (Remove PartyId)

**Problem**: Supplier currently stores contact data (Name, Phone, Email, Address, TaxNumber, Notes) on a linked `Party` record via `PartyId` FK. Per the accounts Details.md analysis conclusion:

```
✅ حذف Parties
✅ جعل Customers, Suppliers, Employees جداول مستقلة
✅ لكل مورد حساب محاسبي تلقائياً
✅ الرصيد الحقيقي يأتي من JournalEntryLines
```

The Party entity adds unnecessary complexity — contact data should be DIRECT fields on Supplier.

**Impact**: Every Supplier lookup requires a JOIN to Parties table. UI code must navigate through `supplier.Party.Name` instead of direct `supplier.Name`. Extra layer of indirection with no benefit for V1.

**Fix**: 
1. Add direct fields to `Supplier` entity: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`
2. Remove `PartyId` FK and `Party` navigation property
3. Remove `Party` entity reference from Supplier aggregate
4. Update `SupplierConfiguration.cs` — direct column mappings, remove Party FK
5. Update `Supplier.Create()` and `Supplier.Update()` — accept direct field params
6. Update DB migration: add columns, drop FK to Parties, drop PartyId column
7. Update all DTOs — remove PartyId, use direct fields
8. Update Desktop ViewModels — direct property bindings (no nested Party access)
9. Update FluentValidators — direct field validation
10. Update Seed Data — create supplier with direct fields

**Files changed**: `Supplier.cs`, `SupplierConfiguration.cs`, `SupplierService.cs`, `ISupplierService.cs`, `SupplierDto.cs`, `SupplierResponse.cs`, `SupplierRequests.cs`, `SupplierEditorViewModel.cs`, `SupplierListViewModel.cs`, XAML views, migrations, `DbSeeder.cs`

### 3.2 Blocker 2: Supplier — AccountId FK Missing (Fixed)

**Problem (previously)**: `Supplier` entity had no `AccountId` FK. **SOLUTION**: Already implemented. AccountId is mandatory `int` FK. Service auto-creates Level-4 detail account under parent `"1320 — الموردون"` (Accounts Payable).

**Current state**: ✅ Already implemented correctly.

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
| 2 | `Name` | `nvarchar(150)` | — | ✅ | ✅ | Direct field on Supplier |
| 3 | `Phone` | `nvarchar(20)` | — | ❌ | ✅ | Direct field on Supplier |
| 4 | `Email` | `nvarchar(100)` | — | ❌ | ✅ | Direct field on Supplier |
| 5 | `Address` | `nvarchar(250)` | — | ❌ | ✅ | Direct field on Supplier |
| 6 | `TaxNumber` | `nvarchar(30)` | — | ❌ | ✅ | Direct field on Supplier |
| 7 | `Notes` | `nvarchar(500)` | — | ❌ | ✅ | Direct field on Supplier |
| 8 | `AccountId` | `int FK` | → Accounts(Id) Restrict | ✅ | ✅ | Chart of Accounts link (mandatory) |
| 9 | `CategoryId` | `int?` | Optional classification | ❌ | ✅ | Supplier category grouping |
| 10 | `CreditLimit` | `decimal(18,2)` | >= 0 | ❌ | ✅ | Max credit allowed |
| 11 | `IsActive` | `bit` | Global query filter | ❌ | ✅ | Soft delete flag |

**REMOVED from V1 (deferred or eliminated):**
- ❌ `PartyId` — eliminated. Contact data now DIRECT on Supplier.
- ❌ `SupplierType` — eliminated. Payment type is per-invoice (SalesInvoice.PaymentType), not per-supplier.
- ❌ `SupplierGroupId` — deferred to V2. SupplierGroup entity NOT in V1.
- ❌ `OpeningBalance` — eliminated. Balance lives on linked GL Account via JournalEntryLines.
- ❌ `CurrentBalance` — eliminated. Balance lives on linked GL Account via JournalEntryLines.
- ❌ `CurrencyId` — eliminated. Currency is per-transaction (invoice/payment), not per-supplier.

### 4.2 SupplierDto (Enhanced)

> Canonical definition in `SalesSystem.Contracts/`. Fields: `Id`, `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `AccountId` (int, non-nullable), `AccountName` (string?), `CategoryId` (int?), `CreditLimit` (decimal), `IsActive`. **NO** `PartyId`, **NO** `SupplierType`, **NO** `SupplierGroupId`, **NO** `OpeningBalance`/`CurrentBalance`.

### 4.3 SupplierBalanceReportDto (New)

| Field | Type | Description |
|-------|------|-------------|
| `SupplierId` | `int` | Supplier ID |
| `SupplierName` | `string` | Supplier name |
| `AccountId` | `int` | Linked GL Account ID |
| `AccountName` | `string?` | Linked GL Account name |
| `CurrentBalance` | `decimal` | Balance from JournalEntryLines (positive = we owe) |
| `CreditLimit` | `decimal` | Maximum credit limit |
| `CreditLimitUsage` | `decimal` | Current balance / Credit limit % |

> Balance is sourced from linked Account's JournalEntryLines, not from Supplier entity per RULE-446.

### 4.4 CreditLimitUsageDto (New)

| Field | Type | Description |
|-------|------|-------------|
| `SupplierId` | `int` | Supplier ID |
| `SupplierName` | `string` | Supplier name |
| `CurrentBalance` | `decimal` | Balance from Account |
| `CreditLimit` | `decimal` | Supplier's credit limit |
| `UsagePercent` | `decimal` | (CurrentBalance / CreditLimit) × 100 |
| `IsExceeded` | `bool` | CurrentBalance > CreditLimit |

### 4.5 Requests (Enhanced)

> Canonical request definitions in `SalesSystem.Contracts/Requests/`.

**CreateSupplierRequest:**
```csharp
public record CreateSupplierRequest(
    string Name,              // Required, max 150
    string? Phone,            // Optional, max 20
    string? Email,            // Optional, max 100, email format
    string? Address,          // Optional, max 250
    string? TaxNumber,        // Optional, max 30
    string? Notes,            // Optional, max 500
    decimal CreditLimit,      // Optional, >= 0
    int? CategoryId           // Optional
);
// NO AccountId (auto-created by service under parent "1320")
// NO PartyId
// NO SupplierType
// NO SupplierGroupId
```

**UpdateSupplierRequest:**
```csharp
public record UpdateSupplierRequest(
    string Name,              // Required, max 150
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? Notes,
    decimal CreditLimit,
    int? CategoryId,
    bool IsActive
);
// NO AccountId (cannot change account after creation)
// NO PartyId
// NO SupplierType
// NO SupplierGroupId
```

### 4.6 SupplierResponse (Enhanced)

> `SupplierResponse` — mirrors SupplierDto. Includes `AccountId` (int, non-nullable) and `AccountName` (string?) for balanced display. **NO** `SupplierType`, **NO** `SupplierGroupId`, **NO** `PartyId`, **NO** `CurrentBalance` (sourced from Account), **NO** `OpeningBalance`.

### 4.7 API Endpoints (Full Set)

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
| Direct `Name` field | ❌ Currently via PartyId | Add `Name` string property to Supplier |
| Direct `Phone` field | ❌ Currently via PartyId | Add `Phone` string? property |
| Direct `Email` field | ❌ Currently via PartyId | Add `Email` string? property |
| Direct `Address` field | ❌ Currently via PartyId | Add `Address` string? property |
| Direct `TaxNumber` field | ❌ Currently via PartyId | Add `TaxNumber` string? property |
| Direct `Notes` field | ❌ Currently via PartyId | Add `Notes` string? property |
| `PartyId` FK | ✅ Currently exists | **REMOVE** — contact data now direct |
| `AccountId` FK | ✅ Exists | Already mandatory non-nullable |
| `CreditLimit` | ❌ Missing | Add `decimal` property + Guard Clause >= 0 |
| `CheckCreditLimit()` | ❌ Missing | Add domain method returning bool |
| Guard Clauses (all fields) | ⚠️ Partial | Add Name empty, CreditLimit >= 0, direct field validations |
| Balance direction convention | ✅ Exists | Positive = we owe supplier |

### 5.2 Configuration Gaps

| Configuration | Status | Action |
|--------------|--------|--------|
| MaxLength(150) for Name | **🔧 TO ADD** | Add direct column mapping |
| MaxLength(20) for Phone | **🔧 TO ADD** | Add direct column mapping |
| MaxLength(100) for Email | **🔧 TO ADD** | Add direct column mapping |
| MaxLength(250) for Address | **🔧 TO ADD** | Add direct column mapping |
| MaxLength(30) for TaxNumber | ✅ Exists | — |
| MaxLength(500) for Notes | **🔧 TO ADD** | Add direct column mapping |
| HasPrecision(18,2) for CreditLimit | **🔧 TO ADD** | — |
| AccountId FK + Restrict | ✅ Exists | — |
| PartyId FK config | ✅ Exists | **REMOVE** entire Party relationship configuration |
| IsActive query filter | ✅ Exists | — |

### 5.3 Service Gaps

| Feature | Status | Action |
|---------|--------|--------|
| Create with direct fields (not Party) | ❌ Wrong | Update `CreateAsync` to accept direct Name/Phone/Email/Address/TaxNumber/Notes |
| Account auto-creation on Supplier.Create | ✅ Exists | Create Account under 1320 parent, assign to supplier |
| CreditLimit validation on purchase | ❌ Missing | Add `IsCreditLimitExceeded(supplierId, amount)` method |
| Balance report service method | ❌ Missing | Add `GetBalanceReportAsync()` |
| CreditLimit usage service method | ❌ Missing | Add `GetCreditLimitUsageAsync()` |
| Transaction history service method | ❌ Missing | Add `GetTransactionHistoryAsync()` |
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
| Direct Name/Phone/Email/Address/TaxNumber/Notes fields | ❌ Via Party | Remove Party navigation, bind direct fields |
| AccountName display in Editor | ❌ Missing | Add read-only AccountName field |
| Current balance display in Editor | ❌ Missing | Add read-only balance field |
| CreditLimit field in XAML | ❌ Missing | Add to editor form |
| Balance usage indicator in list | ❌ Missing | Color-code CurrentBalance vs CreditLimit |
| AccountName column in DataGrid | ❌ Missing | Add column |
| CreditLimit column in DataGrid | ❌ Missing | Add column |
| Search by Account | ❌ Missing | Optional search by account name |
| ToolTips on new fields | ❌ Missing | Add Arabic ToolTips (RULE-185-190) |
| ExecuteAsync() refactor | ❌ Manual try/catch | **BLOCKER 3** |
| Compact UI styles | ⚠️ Partial | Check SupplierEditorView for hardcoded sizes |

### 5.6 Seed Data Gaps

| Data | Status | Action |
|------|--------|--------|
| Default supplier name | ✅ Correct | `"مورد نقدي"` |
| Default supplier AccountId | ✅ Correct | Auto-created under 1320 — حسابات الموردين |
| Default supplier direct fields | ✅ | Create with direct Name/Phone etc. — no Party needed |
| Supplier groups seed | ❌ Not needed | SupplierGroup NOT in V1 |

### 5.7 Validator Gaps

| Rule | Status | Action |
|------|--------|--------|
| Name required + Arabic message | **🔧 TO UPDATE** | Currently validates via Party — validate direct |
| TaxNumber format (15 digits) | ❌ Missing | Add regex validation for Saudi VAT (optional) |
| CreditLimit >= 0 | ❌ Missing | Add `GreaterThanOrEqualTo(0)` rule |
| Notes max 500 | ❌ Missing | Add `MaximumLength(500)` rule |

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

### 6.1 Direct Contact Fields on Supplier (NO Parties)

Based on the accounts Details.md analysis conclusion:

```
✅ حذف Parties
✅ جعل Customers, Suppliers, Employees جداول مستقلة
✅ لكل مورد حساب محاسبي تلقائياً
```

**Decision**: Remove the `Party` entity layer. Supplier stores contact data (Name, Phone, Email, Address, TaxNumber, Notes) as DIRECT fields on its own table. No more `PartyId` FK, no more JOINs to a Party table for basic contact information.

**Rationale**:
- Eliminates unnecessary JOIN for every supplier query
- Simplifies the entity model — Supplier IS a complete record
- Reduces database complexity (one fewer table, one fewer FK relationship)
- No shared contact data scenario that would benefit from a Party abstraction
- Supplier's contact fields are conceptually owned by the Supplier, not shared

**Impact on code**:
- `Supplier.cs`: Remove `PartyId`, `Party` nav property. Add `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes` as direct properties
- `SupplierConfiguration.cs`: Remove HasOne/WithMany for Party. Add direct column mappings for all contact fields
- `SupplierService.cs`: `CreateAsync` accepts direct fields, `MapToDto` maps directly
- `SupplierEditorViewModel.cs`: Direct property bindings (no `.Party.Name` nesting)
- Migration: Add contact columns, drop PartyId FK

### 6.2 SupplierGroup — DEFERRED to V2

**Decision**: **Defer `SupplierGroup` to V2**. No SupplierGroup entity, DTO, service, controller, migration, or Desktop UI exists in V1 codebase.

**Rationale**:
- Analysis Part 3 (line 952) explicitly states: "لا توجد مجموعات عملاء أو موردين في V1"
- Supplier filtering in V1 uses search (name, phone) only — no group filter
- Adds migration + FK + UI combo + filter complexity with no V1 requirement
- Can be added later as additive change (nullable FK)

### 6.3 AccountId — Already Implemented (Mandatory, Auto-Created)

**Decision**: `AccountId` is **mandatory** (non-nullable `int` FK → Account). Already implemented in:
- `Supplier.cs`: `public int AccountId { get; private set; }` — non-nullable
- `SupplierConfiguration.cs`: FK → Accounts with `DeleteBehavior.Restrict`
- `SupplierService.cs`: Auto-creates Level-4 detail account under parent `"1320 — الموردون"` (Accounts Payable)

**Auto-creation pattern** (follows CashBoxService):
1. Look up parent account by code `"1320"` (Accounts Payable/الموردون)
2. Find max child code: `GetMaxChildCodeAsync(parentAccount.Id)`
3. Increment code: `int.Parse(maxCode) + 1`
4. Create `Account` with: `allowTransactions = true`, `level = 4`, `isSystem = false`
5. Set `supplier.AccountId = account.Id`
6. All wrapped in `ExecuteTransactionAsync()` for atomicity

### 6.4 CreditLimit Validation — Layer Location

**Decision**: Primary validation in **Domain** (Guard Clause: `CreditLimit >= 0`), secondary validation in **Application** service (`Create/Update`), **operational** validation in `SupplierService.IsCreditLimitExceeded()` (called from PurchaseInvoiceService during credit purchases).

**Where to validate credit limit during purchase**:
1. **Domain** (Supplier.CheckCreditLimit) — non-throwing bool method returning warning
2. **Application** (PurchaseInvoiceService) — validate before opening transaction
3. **API** (FluentValidation) — CreditLimit >= 0 on create/update
4. **Desktop** — show warning before saving

**Note**: Full purchase-time credit limit validation is **Phase 27** (Purchase Invoice Module). Phase 32 creates the `CheckCreditLimit()` method only.

### 6.5 SupplierResponse — Enhance vs. Deprecate

**Decision**: **Enhance existing `SupplierResponse`** by adding `CreditLimit`, `AccountId`, `AccountName` — removing `PartyId`. Breaking change but all callers use deserialized DTO.

### 6.6 Why NOT a Separate Supplier Account Screen

**Decision**: Keep supplier and account creation conceptually integrated. Account auto-creation happens transparently when a supplier is created. User never sees account management in the supplier screen.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason | Target |
|---------|--------|--------|
| **SupplierGroup** entity + CRUD | Analysis says no groups in V1 | V2 |
| **Aging Report** (supplier debt aging) | Requires invoice date analysis + custom SQL | V2 |
| **Full Account Statement** (debit/credit per supplier) | Requires journal entry aggregation | Phase 30 |
| **Bulk supplier import** (Excel/CSV) | Advanced feature, not in V1 scope | V2 |
| **Supplier contract management** | Out of V1 scope | V3 |
| **Supplier purchase history dashboard** | Requires Phase 27 purchase module integration | Phase 31 |
| **Supplier communication log** | Out of V1 scope | V3 |
| **Supplier document attachments** | Invoice attachment — not V1 | V2 |
| **Purchase order → supplier integration** | PO module is deferred | V2 |
| **Supplier self-service portal** | Out of scope entirely | Future |
| **Multi-currency supplier balances** | Requires currency module full integration | Phase 30 |

---

## 8. Implementation Tasks

All tasks include:
- **Logging** (RULE-035/036): `Log.Information` on CRUD, `Log.Warning` on validation failures (RULE-183)
- **Error handling** (RULE-199/200/201): `LogSystemError()` in ViewModels, catch `DbUpdateException` in services
- **ToolTips** (RULE-185-190): Arabic action-oriented ToolTips on all interactive controls
- **UI Compact** (RULE-262-274): No hardcoded heights/paddings, 28px default, 12,6 header, 12,8 footer
- **INotifyDataErrorInfo** (RULE-228): All editor properties use `AddError`/`ClearErrors`
- **ValidateAllAsync** (RULE-229): Pre-save validation calls `ClearAllErrors()` + `AddError()` + `await ValidateAllAsync()`
- **Success/Error feedback** (RULE-536-542): Toast for minor success, dialog for major, ErrorMessage bar in list views

---

### Task 1 — Refactor Supplier Entity: Direct Contact Fields (Remove PartyId)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Supplier.cs` | Remove `PartyId`, `Party` nav property. Add direct fields: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `CreditLimit` |
| `Domain/Entities/Supplier.cs` | Update `Create()` factory: accept direct contact field params (no partyId), add Name guard, CreditLimit guard |
| `Domain/Entities/Supplier.cs` | Update `Update()` method: accept direct contact field params |
| `Domain/Entities/Supplier.cs` | Add `CheckCreditLimit(decimal additionalAmount)` — non-throwing bool method |
| `Infrastructure/Data/Configurations/SupplierConfiguration.cs` | Remove Party FK mapping. Add direct column mappings for Name(150), Phone(20), Email(100), Address(250), TaxNumber(30), Notes(500), CreditLimit(18,2) |
| `Infrastructure/Data/Migrations/` | New migration: Add direct columns, drop PartyId FK and column |
| `Contracts/DTOs/AllDtos.cs` — `SupplierDto` | Remove `PartyId`. Ensure direct fields: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `AccountId`, `AccountName`, `CreditLimit` |
| `Contracts/Responses/SupplierResponse.cs` | Remove `PartyId`. Add direct fields + `AccountId`, `AccountName`, `CreditLimit` |
| `Contracts/Requests/SupplierRequests.cs` | Remove `PartyId` from Create/Update requests. Use direct fields |
| `Application/Services/SupplierService.cs` | Update `CreateAsync`/`UpdateAsync`/`MapToDto` — direct fields, no Party |
| `Application/Interfaces/Services/ISupplierService.cs` | No interface change needed (dto change only) |

**Domain entity change** (Supplier.cs):

```csharp
public class Supplier : ActivatableEntity
{
    // DIRECT contact fields (NO PartyId)
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Notes { get; private set; }

    // Mandatory FK to Chart of Accounts
    public int AccountId { get; private set; }
    public virtual Account? Account { get; private set; }

    // Optional classification
    public int? CategoryId { get; private set; }

    // Credit management
    public decimal CreditLimit { get; private set; }

    private Supplier() { } // EF Core

    public static Supplier Create(
        string name,
        string? phone,
        string? email,
        string? address,
        string? taxNumber,
        string? notes,
        int accountId,
        decimal creditLimit = 0,
        int? categoryId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب");
        if (creditLimit < 0)
            throw new DomainException("الحد الائتماني لا يمكن أن يكون سالباً");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        return new Supplier
        {
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Notes = notes?.Trim(),
            AccountId = accountId,
            CreditLimit = creditLimit,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }

    public void Update(
        string name,
        string? phone,
        string? email,
        string? address,
        string? taxNumber,
        string? notes,
        decimal creditLimit,
        int? categoryId = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب");
        if (creditLimit < 0)
            throw new DomainException("الحد الائتماني لا يمكن أن يكون سالباً");

        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        Notes = notes?.Trim();
        CreditLimit = creditLimit;
        CategoryId = categoryId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Non-throwing SOFT WARNING check. Returns true if additional amount would exceed limit.
    /// Caller decides whether to block.
    /// </summary>
    public bool CheckCreditLimit(decimal additionalAmount, decimal currentBalance)
    {
        if (CreditLimit <= 0) return false; // No limit = always OK
        return (currentBalance + additionalAmount) > CreditLimit;
    }
}
```

**Configuration change** (SupplierConfiguration.cs):

```csharp
builder.ToTable("Suppliers");
builder.HasKey(s => s.Id);
builder.Property(s => s.Id).ValueGeneratedOnAdd();

// Direct contact fields (NO PartyId)
builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
builder.Property(s => s.Phone).HasMaxLength(20).IsRequired(false);
builder.Property(s => s.Email).HasMaxLength(100).IsRequired(false);
builder.Property(s => s.Address).HasMaxLength(250).IsRequired(false);
builder.Property(s => s.TaxNumber).HasMaxLength(30).IsRequired(false);
builder.Property(s => s.Notes).HasMaxLength(500).IsRequired(false);
builder.Property(s => s.CreditLimit).HasPrecision(18, 2).HasDefaultValue(0);

// FK to Account (mandatory)
builder.HasOne(s => s.Account)
    .WithMany()
    .HasForeignKey(s => s.AccountId)
    .OnDelete(DeleteBehavior.Restrict)
    .IsRequired();

// CategoryId is optional — no FK constraint (type mismatch with smallint PK)
builder.Property(s => s.CategoryId).IsRequired(false);

// Indexes
builder.HasIndex(s => s.AccountId).HasDatabaseName("IX_Suppliers_AccountId");
builder.HasIndex(s => s.CategoryId).HasDatabaseName("IX_Suppliers_CategoryId");

// Global query filter — soft delete
builder.HasQueryFilter(s => s.IsActive);
```

**⚠️ Eager loading**: `GetByIdAsync` must `.Include(s => s.Account)` to populate Account navigation property for AccountName.

**Logging**:
- `Log.Information("Supplier {Id} created with Account {AccountId}: Name={Name}", id, accountId, name)`
- `Log.Information("Supplier {Id} updated: Name={Name}", id, name)`

**Estimate**: ~3 hours

---

### Task 2 — Auto-Create Account on Supplier Create (Parent "1320")

**Problem**: Currently `AccountId` is mandatory non-nullable. The service must auto-create a Level-4 detail account under parent `"1320 — الموردون"` (Accounts Payable) when creating a supplier.

**Note**: This is already implemented. Verify and ensure the auto-creation logic follows the correct parent code `"1320"` (not old `"2100"`).

**Parent code verification**:
```
✅ CORRECT parent code: "1320" (Accounts Payable — الموردون)
❌ WRONG parent code:   "2100" (doesn't exist in COA)
```

**Files**:

| File | Change |
|------|--------|
| `Application/Services/SupplierService.cs` | Verify `CreateAsync` auto-creates account under "1320". If not, update. |
| `Application/Interfaces/Services/ISupplierService.cs` | No change needed |

**Auto-creation pattern** (mirrors CashBoxService):

```csharp
private async Task<int> AutoCreateAccountAsync(string supplierName, CancellationToken ct)
{
    var parentAccount = await _uow.Accounts.GetByCodeAsync("1320", ct);
    if (parentAccount == null)
        throw new InvalidOperationException("الحساب الأب 1320 (الموردون) غير موجود");

    var maxCode = await _uow.Accounts.GetMaxChildCodeAsync(parentAccount.Id, ct);
    var newCode = (int.Parse(maxCode ?? "13200000") + 1).ToString().PadLeft(8, '0');

    var account = Account.Create(
        accountCode: newCode,
        nameAr: supplierName,
        nameEn: supplierName,
        nature: AccountNature.Credit,     // Liability-nature account
        isLeaf: true,
        parentId: parentAccount.Id,
        isSystem: false,
        categoryId: null,
        level: 4,
        description: $"حساب المورد: {supplierName}",
        colorCode: "#F44336",              // Red (Liability)
        notes: null,
        createdByUserId: null,
        allowTransactions: true            // Level 4 must allow transactions (RULE-506)
    );

    await _uow.Accounts.AddAsync(account, ct);
    await _uow.SaveChangesAsync(ct);
    return account.Id;
}
```

**Integration into CreateAsync flow**:

```csharp
public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request, CancellationToken ct)
{
    // Validate
    if (string.IsNullOrWhiteSpace(request.Name))
        return Result<SupplierDto>.Failure("اسم المورد مطلوب");

    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        // 1. Auto-create account under parent "1320"
        var accountId = await AutoCreateAccountAsync(request.Name, ct);

        // 2. Create supplier with direct fields
        var supplier = Supplier.Create(
            request.Name, request.Phone, request.Email,
            request.Address, request.TaxNumber, request.Notes,
            accountId, request.CreditLimit, request.CategoryId, currentUserId);

        await _uow.Suppliers.AddAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result<SupplierDto>.Success(MapToDto(supplier));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        _logger.LogError(ex, "Failed to create supplier {Name}", request.Name);
        return Result<SupplierDto>.Failure("حدث خطأ أثناء إنشاء المورد");
    }
}
```

**⚠️ Cross-Phase Dependencies**:
1. **Phase 22** (Chart of Accounts): Parent account `"1320 — الموردون"` must be seeded in AccountingSeeder
2. If 1320 is not seeded, supplier creation returns `Result.Failure("الحساب الأب 1320 (الموردون) غير موجود")`

**Estimate**: ~1 hour (verification + any fixes)

---

### Task 3 — Add CreditLimit Validation + Balance Report Methods

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ISupplierService.cs` | Add `GetBalanceReportAsync()` and `GetCreditLimitUsageAsync()` methods |
| `Application/Services/SupplierService.cs` | Implement balance report + credit limit methods |
| `Contracts/DTOs/AllDtos.cs` | Add `SupplierBalanceReportDto` and `CreditLimitUsageDto` |

**Service interface additions**:

```csharp
Task<Result<SupplierBalanceReportDto>> GetBalanceReportAsync(int supplierId, CancellationToken ct);
Task<Result<CreditLimitUsageDto>> GetCreditLimitUsageAsync(int supplierId, CancellationToken ct);
```

> Balance sourced from linked Account's JournalEntryLines per RULE-446. `Supplier.CheckCreditLimit()` returns bool (non-throwing per RULE-448).

**⚠️ Note**: `CheckCreditLimit` is created here but **called** from Phase 27 (Purchase Invoice Module). Phase 32 creates the method + wires it into the service.

**Logging**:
- `Log.Information("Supplier balance report generated — {Count} suppliers", count)`
- `Log.Warning("Supplier {Id} exceeds credit limit: balance={Balance}, limit={Limit}", id, balance, limit)` (RULE-183 — user data, warning level)

**Estimate**: ~1 hour

---

### Task 4 — Add 3 New Report Endpoints to API

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/SuppliersController.cs` | Add 3 new GET endpoints |

**New endpoints**:

```csharp
[HttpGet("reports/balance-summary")]
[Authorize(Policy = "ManagerAndAbove")]
public async Task<IActionResult> GetBalanceSummary(CancellationToken ct)
{
    var result = await _supplierService.GetBalanceReportAsync(ct);
    if (!result.IsSuccess)
        return result.Error == ErrorCodes.NotFound ? NotFound() : BadRequest(new { error = result.Error });
    return Ok(result.Value);
}

[HttpGet("{id}/credit-limit")]
[Authorize(Policy = "ManagerAndAbove")]
public async Task<IActionResult> GetCreditLimitUsage(int id, CancellationToken ct)
{
    var result = await _supplierService.GetCreditLimitUsageAsync(id, ct);
    if (!result.IsSuccess)
        return result.Error == ErrorCodes.NotFound ? NotFound() : BadRequest(new { error = result.Error });
    return Ok(result.Value);
}
```

> Controller pattern per RULE-025 (translate Result to HTTP status codes). Inject `ISupplierService` only per RULE-203. Use `NotFound` for `ErrorCodes.NotFound` and `BadRequest` for business validation errors per RULE-288.

**Estimate**: ~30 minutes

---

### Task 5 — Fix SupplierListViewModel ExecuteAsync Pattern

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

> See `docs/AGENTS.md` §2.36 (RULE-141 to RULE-146 — ExecuteAsync Pattern) for the canonical ViewModel command wrapper.

**⚠️ Note**: RULE-059 says commands must NOT have CanExecute predicates. The existing `DeleteCommand` and `RestoreCommand` use `() => SelectedSupplier != null` predicates — these must be removed. Buttons remain enabled, validation happens on click.

**Estimate**: ~1.5 hours

---

### Task 6 — Enhance SupplierEditorViewModel with Direct + New Fields

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Remove `_partyId`/`_partyName` fields. Add direct fields: `_name`, `_phone`, `_email`, `_address`, `_taxNumber`, `_notes`, `_creditLimit` |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add properties: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `CreditLimit`, `AccountName` (read-only) |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update `ValidateAsync()` for direct fields: Name required, CreditLimit >= 0 |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update `SaveOperationAsync()` to use direct fields in CreateSupplierRequest |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Update `LoadSupplierAsync()` — map direct fields from DTO (no Party navigation) |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Add Arabic ToolTip constant strings for new fields |

**ViewModel property pattern**:

```csharp
private string? _name;
public string? Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value))
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(value))
                AddError(nameof(Name), "اسم المورد مطلوب");
        }
    }
}

private decimal _creditLimit;
public decimal CreditLimit
{
    get => _creditLimit;
    set
    {
        if (SetProperty(ref _creditLimit, value))
        {
            ClearErrors(nameof(CreditLimit));
            if (value < 0)
                AddError(nameof(CreditLimit), "الحد الائتماني لا يمكن أن يكون سالباً");
        }
    }
}
```

**⚠️ Note**: No `IAccountApiService` needed — `AccountName` is returned in `SupplierDto.AccountName` from the API (via `.Include(s => s.Account)` on the server side).

**Estimate**: ~2 hours

---

### Task 7 — Update SupplierEditorView.xaml (Compact + Direct Fields)

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SupplierEditorView.xaml` | Replace Party-dependent fields with direct Name, Phone, Email, Address, TaxNumber, Notes fields |
| `Views/Suppliers/SupplierEditorView.xaml` | Add CreditLimit field |
| `Views/Suppliers/SupplierEditorView.xaml` | Add AccountName read-only display |
| `Views/Suppliers/SupplierEditorView.xaml` | Fix hardcoded sizes to use compact styles (RULE-262-274) |

**Form layout**:

```
┌─────────────────────────────────────┐
│  ➕ إضافة مورد جديد                  │
├─────────────────────────────────────┤
│ اسم المورد *  [__________________]  │
│                                     │
│ الهاتف         [__________________]  │
│                                     │
│ البريد الإلكتروني [________________] │
│                                     │
│ العنوان        [__________________]  │
│                                     │
│ الرقم الضريبي  [__________________]  │
│                                     │
│ ملاحظات        [__________________]  │
│                                     │
│ الحد الائتماني [___________]  0.00  │
│                                     │
│ الحساب المحاسبي: 13200001 — مورد نقدي│  (read-only, display after save)
│                                     │
│ □ نشط                               │
├─────────────────────────────────────┤
│ [     حفظ     ]  [    إلغاء    ]    │
└─────────────────────────────────────┘
```

**Compact UI fixes** (RULE-262-274):
- Remove `Height="600" Width="550"` from Window element (use `MinHeight/MinWidth` only)
- Verify no hardcoded `Height="36"` or `Height="40"` on TextBox/Button elements
- Remove `Border Height="12"` spacers — replace with `Margin="0,0,0,6"` on fields
- Header: `Padding="12,6"`
- Footer: `Padding="12,8"`

**ToolTips** (RULE-185-190):
- Name: `"اسم المورد — إلزامي"`
- Phone: `"رقم هاتف المورد"`
- Email: `"البريد الإلكتروني للمورد"`
- Address: `"العنوان الفعلي للمورد"`
- TaxNumber: `"الرقم الضريبي للمورد"`
- Notes: `"ملاحظات إضافية عن المورد"`
- CreditLimit: `"الحد الأقصى للائتمان — سيتم تحذير عند تجاوز هذا الحد"`
- AccountName: `"الحساب المحاسبي المرتبط — يتم إنشاؤه تلقائياً"`

**Estimate**: ~2 hours

---

### Task 8 — Update SupplierEditorView Code-Behind

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SupplierEditorView.xaml.cs` | Remove any Party loading logic. Wire direct field loading. |

**Code-behind**:

> Window code-behind pattern follows `docs/AGENTS.md` §2.53 (RULE-224 — PositionOverOwner guards against self-ownership) and §2.40 (ScreenWindowService for non-modal screens).

**Estimate**: ~15 minutes

---

### Task 9 — Enhance SuppliersListView with Balance Display + New Columns

**Files**:

| File | Change |
|------|--------|
| `Views/Suppliers/SuppliersListView.xaml` | Add AccountName column, CreditLimit column |
| `Views/Suppliers/SuppliersListView.xaml` | Enhance CurrentBalance column with color coding |
| `ViewModels/Suppliers/SupplierListViewModel.cs` | Add AccountName to search/filter predicates |

**New DataGrid columns**:

```xml
<DataGridTextColumn Header="الحساب المحاسبي" Binding="{Binding AccountName}"/>
<DataGridTextColumn Header="الحد الائتماني" Binding="{Binding CreditLimit, StringFormat=N2}"/>

<!-- Balance with color coding via converter -->
<DataGridTextColumn Header="الرصيد الحالي" Binding="{Binding CurrentBalance, StringFormat=N2}">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="Foreground" Value="{Binding CurrentBalance, Converter={StaticResource BalanceToColorConverter}}"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

**⚠️ Note**: Negative balance DataTrigger won't work directly with decimal bindings (WPF limitation). Use a `BalanceToColorConverter` IValueConverter instead. Balance direction per `docs/AGENTS.md` §2.8 (Supplier balance > 0 = We owe the supplier).

**Estimate**: ~1.5 hours

---

### Task 10 — Update FluentValidators + Validator Tests

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/SupplierRequestValidators.cs` | Update: validate direct Name (required + Arabic message), CreditLimit >= 0, Notes max 500, TaxNumber format |
| `Tests/SalesSystem.Api.Tests/Validators/SupplierRequestValidatorTests.cs` | Update tests for new rules, remove PartyId tests |

**Validator enhancements**:

```csharp
public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المورد مطلوب")
            .MaximumLength(150);

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح")
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(250);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(30);

        RuleFor(x => x.Notes)
            .MaximumLength(500);

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الائتماني لا يمكن أن يكون سالباً");
    }
}
```

> FluentValidation pattern per `docs/AGENTS.md` §2.13 (RULE-044). Phone number uses regex `^05\d{8}$` per RULE-454. TaxNumber uses `MaximumLength(30)`. `nameof` operator used for property references per RULE-351.

**Estimate**: ~30 minutes

---

### Task 11 — Update Seed Data with Default Supplier

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Update default supplier creation with direct fields (no Party), AccountId auto-created under 1320 |

**Seed update**:

```csharp
// Default supplier "مورد نقدي" — no Party needed, direct fields
var accountId = await AutoCreateAccountAsync("مورد نقدي", ct);
var cashSupplier = Supplier.Create(
    name: "مورد نقدي",
    phone: "0500000000",
    email: null,
    address: "العنوان الرئيسي",
    taxNumber: null,
    notes: "المورد النقدي الافتراضي — يستخدم في المشتريات النقدية",
    accountId: accountId,
    creditLimit: 0,
    categoryId: null,
    createdByUserId: 1
);
```

**Estimate**: ~20 minutes

---

### Task 12 — Add ISupplierApiService Methods for New Endpoints

**Files**:

| File | Change |
|------|--------|
| `Services/Api/ISupplierApiService.cs` | Add `GetBalanceReportAsync()`, `GetCreditLimitUsageAsync(int id)`, `GetTransactionHistoryAsync(int id, DateTime?, DateTime?)` |
| `Services/Api/SupplierApiService.cs` | Implement the 3 new HTTP methods |

**Interface additions**:

> API service interfaces follow `SalesSystem.DesktopPWF/Services/Api/ISupplierApiService.cs` pattern with `ExecuteAsync<T>()` wrapper (shared from `ApiServiceBase`). See `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern.

**Estimate**: ~30 minutes

---

### Task 13 — Enhanced Search (By Account Name + Direct Fields)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/SupplierService.cs` | Extend `GetAllAsync` search predicate with Account.Name matching + direct field search |

**Search predicate extension**:

```csharp
// Current predicate (additions):
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(s =>
        s.Name.Contains(searchTerm) ||
        (s.Phone != null && s.Phone.Contains(searchTerm)) ||
        (s.Email != null && s.Email.Contains(searchTerm)) ||
        (s.Account != null && s.Account.NameAr.Contains(searchTerm))
    );
}
```

> Search predicate pattern follows existing `GetAllAsync` implementation. Per RULE-192: search/filter by Id or Name only — NO Code column.

**Estimate**: ~30 minutes

---

### Task 14 — Verify EventBus Integration

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

### Task 15 — Comprehensive Unit Tests: Domain + Service + Validation + Config

**Files**:

| File | Change |
|------|--------|
| `Tests/SalesSystem.Domain.Tests/Entities/SupplierTests.cs` | **UPDATE** — entity factory tests for direct fields |
| `Tests/SalesSystem.Application.Tests/Services/SupplierServiceTests.cs` | **UPDATE** — service layer tests for direct fields |
| `Tests/SalesSystem.Api.Tests/Validators/SupplierRequestValidatorTests.cs` | **UPDATE** — add new field rules, remove PartyId rules |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/SupplierConfigurationTests.cs` | **UPDATE** — EF config tests for direct fields |

---

#### 15.1 — Domain Entity Tests (Supplier.Create)

Test every `Create()` factory method with valid/invalid inputs:

> Test patterns follow existing entity tests in `SalesSystem.Domain.Tests/Entities/`. Key tests: factory method invariants, guard clause coverage (empty name, negative credit limit, max length), CheckCreditLimit method, AccountId linking, direct field assignments.

**Key test cases**:
- `Create_ValidInputs_SetsAllProperties`
- `Create_EmptyName_ThrowsDomainException`
- `Create_NegativeCreditLimit_ThrowsDomainException`
- `Create_InvalidAccountId_ThrowsDomainException`
- `Update_ValidInputs_UpdatesProperties`
- `Update_EmptyName_ThrowsDomainException`
- `CheckCreditLimit_WithinLimit_ReturnsFalse`
- `CheckCreditLimit_ExceedsLimit_ReturnsTrue`
- `CheckCreditLimit_NoLimit_ReturnsFalse`

**Estimate**: ~2 hours

---

#### 15.2 — Service Tests (Mock<IUnitOfWork>)

> Service test patterns follow existing mock-based tests in `SalesSystem.Application.Tests/Services/`. Key tests: CreateAsync with direct fields (success/empty name/negative credit limit), GetByIdAsync (found/not found), transaction rollback on exception, GetAllAsync paged results, soft delete, account auto-creation under 1320.

**Estimate**: ~3 hours

---

#### 15.3 — FluentValidation Tests

> FluentValidation test pattern follows `SalesSystem.Api.Tests/Validators/` conventions. Key validation rules: Name required with Arabic message, CreditLimit >= 0, Phone regex, Email format, Notes max length. NO PartyId rules.

**Estimate**: ~1.5 hours

---

#### 15.4 — Database Configuration Tests

> EF Core configuration test pattern follows `SalesSystem.Infrastructure.Tests/Configurations/`. Tests cover: table name, MaxLength constraints for direct fields, precision (decimal(18,2) for CreditLimit), DeleteBehavior.Restrict on AccountId FK, no Party FK, IsActive query filter.

**Estimate**: ~1.5 hours

---

#### 15.5 — Phase-Specific Integration Tests

> Integration test patterns follow `SalesSystem.Application.Tests/Services/SupplierServiceTests.cs`. Tests cover: account auto-creation under 1320 parent, transactional integrity with rollback on failure (RULE-281 — ExecuteTransactionAsync), AccountId FK eager loading.

**Estimate**: ~2 hours

---

| Test Sub-Task | Focus | Files | Estimate |
|----------------|-------|-------|----------|
| 15.1 Domain Entity | `Supplier.Create` factory + guards + direct fields + CheckCreditLimit | `SupplierTests.cs` | 2 hours |
| 15.2 Service Layer | `CreateAsync`, `GetByIdAsync`, transactions, rollback, auto-account | `SupplierServiceTests.cs` | 3 hours |
| 15.3 FluentValidation | Name, CreditLimit, direct field rules (no PartyId) | `SupplierRequestValidatorTests.cs` | 1.5 hours |
| 15.4 DB Config | Precision, MaxLength, Restrict FK, direct fields | `SupplierConfigurationTests.cs` | 1.5 hours |
| 15.5 Phase-Specific | Account auto-creation under 1320, transactional flow | `SupplierServiceTests.cs` | 2 hours |
| **Total** | **5 sub-tasks** | **4 test files** | **~10 hours** |

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | Supplier.CreditLimit — `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | Supplier.Create + Account.Create wrapped in ExecuteTransactionAsync | ✅ |
| **RULE-006** | ALL services return `Result<T>` | SupplierService: all methods return `Result<T>` or `Result` | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Name, Phone, Email, Address, TaxNumber, Notes — all nvarchar | ✅ |
| **RULE-016** | BaseEntity audit fields | Supplier inherits ActivatableEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | SupplierService uses `_uow` for all DB operations | ✅ |
| **RULE-035** | Serilog for logging | All CRUD operations logged via `_logger.LogInformation` | ✅ |
| **RULE-036** | Log critical operations | Supplier create/update/delete + CreditLimit exceeded | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | SuppliersController: `[Authorize(Policy = "ManagerAndAbove")]` | ✅ |
| **RULE-042** | Rich Domain — `private set` + methods | Supplier: Create(), Update(), CheckCreditLimit() | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateSupplierRequestValidator + UpdateSupplierRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | SupplierListViewModel: `ShowDeleteConfirmationAsync()` | ✅ |
| **RULE-052** | Guard Clauses on all entities | Supplier.Create: Name guard, CreditLimit guard, AccountId guard | ✅ |
| **RULE-053** | DomainException in Arabic | "اسم المورد مطلوب", "الحد الائتماني لا يمكن أن يكون سالباً" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all supplier ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | SupplierEditorViewModel — AddError/ClearErrors (RULE-228) | ✅ |
| **RULE-059** | Save always enabled, validate on click | SupplierEditorViewModel — NO CanExecute blocking | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | **BLOCKER 3** — SupplierListViewModel needs refactor (Task 5) | ⚠️ **Fix in Task 5** |
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
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | Supplier.AccountId FK → Restrict | ✅ |
| **RULE-220** | Newest-first sorting on lists | SupplierListViewModel: `OrderByDescending(x => x.Id)` | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | SupplierEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | SupplierEditorViewModel — AddError/ClearErrors | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | Not in supplier scope | ✅ N/A |
| **RULE-244** | User hard-delete guarded | Not in supplier scope | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not in supplier scope | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not in supplier scope | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | Task 7: remove hardcoded sizes from SupplierEditorView | ⚠️ **Fix in Task 7** |
| **RULE-263** | No hardcoded Padding="16+" on buttons | Already fixed — styles provide 10,4 | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | Already correct in SupplierEditorView | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Task 7: replace `Border Height="12"` with `Margin="0,0,0,6"` | ⚠️ **Fix in Task 7** |
| **RULE-266** | Dialog titles FontSize=16 max | Title = `FontSize="20"` → fix to 16 | ⚠️ **Fix in Task 7** |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | In SuppliersListView ✅ | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | Emoji icon in header = FontSize 20 ✅ | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | SupplierEditorWindow: MinWidth=500 MinHeight=500 | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | Save button `Width="140"` → use `MinWidth="100"` | ⚠️ **Fix in Task 7** |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | `Height="600" Width="550"` on Window → remove | ⚠️ **Fix in Task 7** |
| **RULE-442** | AccountId mandatory on Supplier | `int AccountId` non-nullable, auto-created | ✅ |
| **RULE-443** | NO SupplierType in V1 | Not present in code | ✅ |
| **RULE-444** | Supplier.Create accepts accountId required | In factory method | ✅ |
| **RULE-445** | Supplier.Update accepts accountId required | In Update method | ✅ |
| **RULE-446** | NO OpeningBalance/CurrentBalance on Supplier | Balance from linked Account only | ✅ |
| **RULE-447** | NO CurrencyId on Supplier | Not present in code | ✅ |
| **RULE-448** | CheckCreditLimit returns bool (non-throwing) | ✅ Added in this phase | ✅ |
| **RULE-449** | SupplierDto has AccountId + AccountName | ✅ | ✅ |
| **RULE-450** | NO AccountId in Create/Update requests | Auto-created by service | ✅ |
| **RULE-451** | NO OpeningBalance/SupplierType/AccountId in Desktop UI | Editor has no these fields | ✅ |
| **RULE-452** | Account auto-created under 1320 parent | Service auto-creates under "1320 — الموردون" | ✅ |
| **RULE-453** | Seeder seeds "مورد نقدي" with auto-created account | ✅ | ✅ |
| **RULE-454** | Phone regex `^05\d{8}$` + Email validation | In FluentValidators | ✅ |
| **RULE-536** | Success/Error feedback on all operations | Toast + dialog per operation | ✅ |
| **RULE-540** | ErrorMessage bar in list views | Present in SuppliersListView | ✅ |
| **RULE-542** | Loading overlay in editor views | Present in SupplierEditorView | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Direct field migration drops PartyId FK** | **HIGH** — data loss if existing suppliers have Party records | Migration must: (1) copy Party.Name→Supplier.Name, Party.Phone→Supplier.Phone etc., (2) then drop PartyId column and FK |
| **AccountId auto-creation fails if 1320 not seeded** | **HIGH** — supplier creation impossible | Ensure AccountingSeeder runs before SupplierSeeder. Guard with `if (parentAccount == null) return Result.Failure(...)` |
| **ExecuteAsync() refactor breaks existing behavior** | Medium — `DeleteSupplierAsync` and `RestoreSupplierAsync` also need refactoring | Test all 3 async methods after refactor (Task 5) |
| **Chart of Accounts module not yet available** | Low — Account auto-creation depends on Phase 22 | Make 1320 parent check graceful: return descriptive error if missing |
| **TaxNumber format validation too strict** | Low — some suppliers have different VAT formats | Use `MaximumLength(30)` only, make regex optional |
| **Balance color coding in DataGrid** | Low — WPF limitation with decimal DataTriggers | Use `IValueConverter` for BalanceToColorConverter |
| **UI compact fixes break existing layout** | Low — removing `Border Height="12"` spacers changes vertical spacing | Test all editor forms visually after compact fixes |
| **SupplierReports query performance** | Low — balance report aggregates across JournalEntryLines | Add pagination to report endpoints, defer full aggregation to Phase 30 |

---

## 11. FORBIDDEN Patterns

The following patterns are FORBIDDEN in the Suppliers module:

```text
❌ Parties — Supplier contact data is DIRECT fields, NOT on a Party entity
❌ PartyId — Remove FK to Parties table
❌ Party navigation property — Remove from Supplier entity
❌ SupplierType enum — NOT in V1 (deferred forever: payment type is per-invoice)
❌ SupplierType property on Supplier — NOT in V1
❌ SupplierType field in DTOs, Requests, Responses — NOT in V1
❌ SupplierType validation rules — NOT in V1
❌ SupplierGroup entity — NOT in V1 (deferred to V2)
❌ SupplierGroupId FK — NOT in V1
❌ SupplierGroup CRUD endpoints — NOT in V1
❌ SupplierGroup DropDown in Desktop UI — NOT in V1
❌ OpeningBalance on Supplier entity — balance comes from JournalEntryLines
❌ CurrentBalance on Supplier entity — balance comes from JournalEntryLines
❌ BalanceBefore/BalanceAfter on Supplier — use RunningBalance from Account
❌ CurrencyId on Supplier — currency is per-transaction, not per-supplier
❌ ProductCode on Supplier — no such field (Product is separate entity)
❌ Hard-deleting Suppliers — soft delete only (IsActive = false)
❌ Cascade delete on AccountId FK — ALWAYS DeleteBehavior.Restrict
❌ Business logic in SuppliersController — delegate to SupplierService
❌ CanExecute predicates on Commands — buttons always enabled (RULE-059)
❌ MessageBox.Show — use IDialogService (RULE-054)
❌ Manual try/catch/finally in ViewModels — use ExecuteAsync() (RULE-141)
```

---

## 12. Rollback Plan

| Scenario | Action |
|----------|--------|
| **Direct field migration causes data loss** | Rollback migration: DROP new columns, ADD PartyId FK back. SQL: `ALTER TABLE Suppliers ADD PartyId int NOT NULL` + restore Party records |
| **AccountId auto-creation fails for existing suppliers** | `UPDATE Suppliers SET AccountId = NULL WHERE AccountId IN (SELECT Id FROM Accounts WHERE Code LIKE '1320%')` manually relink |
| **CreditLimit validation too strict** | Remove `GreaterThanOrEqualTo(0)` from validator |
| **SupplierListViewModel ExecuteAsync refactor breaks** | Revert to original `try/catch/finally` pattern, re-apply with full testing |
| **New endpoints cause routing conflicts** | Remove 3 new endpoints from SuppliersController — no breaking change to existing endpoints |
| **UI compact changes break layout** | Revert SupplierEditorView.xaml and SuppliersListView.xaml to original versions |
| **Entire Phase** | `git revert <phase-merge-commit>` |

---

## Implementation Summary

| Task | Description | Files Changed | Estimate |
|------|-------------|---------------|----------|
| **Task 1** | Refactor Supplier: direct contact fields (remove PartyId) + CreditLimit | 8+ | 3 hours |
| **Task 2** | Verify auto-account creation under parent "1320" | 1 | 1 hour |
| **Task 3** | Add CreditLimit + balance report service methods | 3 | 1 hour |
| **Task 4** | Add 3 new report endpoints to API | 1 | 30 min |
| **Task 5** | Fix SupplierListViewModel ExecuteAsync pattern | 1 | 1.5 hours |
| **Task 6** | Enhance SupplierEditorViewModel with direct + new fields | 1 | 2 hours |
| **Task 7** | Update SupplierEditorView.xaml (compact + direct fields) | 1 | 2 hours |
| **Task 8** | Update SupplierEditorView code-behind | 1 | 15 min |
| **Task 9** | Enhance SuppliersListView with balance + columns | 2 | 1.5 hours |
| **Task 10** | Update FluentValidators + tests | 2 | 30 min |
| **Task 11** | Update seed data with direct fields | 1 | 20 min |
| **Task 12** | Add API service methods for new endpoints | 2 | 30 min |
| **Task 13** | Enhanced search by Account + direct fields | 1 | 30 min |
| **Task 14** | Verify EventBus integration | 0 | 0 min |
| **Task 15** | Comprehensive unit tests (5 sub-tasks) | 4 | 10 hours |
| **Total** | **15 tasks** | **~29 files** | **~25 hours** |

### Key Metrics

- **REMOVED**: PartyId, Party entity reference, SupplierType enum, SupplierGroup references
- **ADDED**: Direct fields (Name, Phone, Email, Address, TaxNumber, Notes, CreditLimit) on Supplier
- **KEPT**: AccountId (mandatory, auto-created under 1320), CategoryId (optional)
- **New DTOs**: 3 (`SupplierBalanceReportDto`, `CreditLimitUsageDto`, `SupplierTransactionDto`)
- **New properties on Supplier**: 7 (Name, Phone, Email, Address, TaxNumber, Notes, CreditLimit) — replacing PartyId
- **New API endpoints**: 3 (balance-summary, credit-limit, transactions)
- **New ViewModel properties**: ~8 direct field properties replacing Party navigation
- **Critical bugs fixed**: 2 (Blocker 1: direct fields, Blocker 3: ExecuteAsync)
- **Validation rules enhanced**: 4 (Name required, CreditLimit >= 0, Notes max, TaxNumber format)
- **Search enhanced**: By Account name + direct fields

### Execution Order

```
Task 1  (Remove PartyId, add direct fields + CreditLimit)
  ↓
Task 2  (Verify auto-account under 1320)
  ↓
Task 3-4 (Service + API methods)
    ↓
Task 5  (ExecuteAsync refactor)
    ↓
Task 6-9 (Desktop UI — ViewModels + Views)
    ↓
Task 10 (Validators)
    ↓
Task 11 (Seed data)
    ↓
Task 12 (API service methods)
    ↓
Task 13 (Enhanced search)
    ↓
Task 14 (Verify EventBus)
    ↓
Task 15 (Tests)
```

**Parallelizable**:
- Tasks 3 + 4 (Service + Controller) → parallel
- Tasks 6 + 9 (ViewModels) → parallel
- Tasks 7 + 8 (Views) → depend on Task 6
- Tasks 10 + 11 + 12 + 13 → can run in parallel after Task 1
- Task 15 (Tests) → can start after Task 1 code is stable
