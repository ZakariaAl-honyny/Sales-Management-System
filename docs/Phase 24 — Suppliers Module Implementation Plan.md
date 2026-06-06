# Phase 24 — Suppliers Module: Comprehensive Implementation Plan

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

```csharp
public record SupplierDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance,
    decimal CreditLimit, bool IsActive
);
```
✅ 10 fields — includes TaxNumber, CreditLimit, CurrentBalance.

**File**: `Contracts.Responses.SupplierResponse`
```csharp
public record SupplierResponse(
    int Id, string Name, string? Phone, string? Address, string? Email,
    decimal CurrentBalance, bool IsActive
);
```

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

```csharp
var defaultSupplier = Supplier.Create(
    name: "المورد الافتراضي في النظام",  // ❌ NEEDS RENAME
    openingBalance: 0m,
    createdByUserId: null
);
db.Suppliers.Add(defaultSupplier);
```

**Issues:**
1. Name `"المورد الافتراضي في النظام"` should be `"مورد نقدي"` per analysis (Part 3 line 718-723, Part 2 line 518)
2. No `AccountId` assigned — default supplier should link to `Accounts Payable (2100)` account
3. No `SupplierType` set
4. No `SupplierGroupId` set

**SystemSetting reference** (also in DbSeeder line 47):
```csharp
SystemSetting.Create("DefaultCashSupplierId", "1", "int", "Purchases",
    "المورد النقدي", "المورد الافتراضي لمشتريات النقد"),
```
✅ Already references "المورد النقدي" in DisplayName.

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: Default Supplier Name — Rename to "مورد نقدي"

**Problem**: The default supplier seeded in `DbSeeder.cs` line 130 is named `"المورد الافتراضي في النظام"`. The analysis (Part 2 line 518, Part 3 line 718-723) explicitly requires the name `"مورد نقدي"`. This name is used:
- As default supplier for cash purchases (SystemSetting `DefaultCashSupplierId` = 1)
- Displayed in Purchase Invoice editor when user selects "Cash Purchase"
- Referenced in Apple `Accounts Payable` subtree: `الموردون → مورد نقدي`

**Impact**: User sees incorrect name in purchase invoices and supplier list. Inconsistent with SystemSetting display name `"المورد النقدي"`.

**Fix**: Single line change in `DbSeeder.cs`:
```csharp
// BEFORE (line 130):
name: "المورد الافتراضي في النظام",
// AFTER:
name: "مورد نقدي",
```

**Files changed**: `DbSeeder.cs`

### 3.2 Blocker 2: Supplier — AccountId FK Missing

**Problem**: Analysis Part 3 (line 940) requires: `"المورد ينشئ حساباً محاسبياً تلقائياً"`. Currently `Supplier` entity has no `AccountId` FK to link it to a Chart of Accounts `Account`. This blocks:
- Automatic journal entry creation for supplier transactions
- Supplier balance visibility in account statements
- Integration with accounting modules (Phase 25+)

**Current workaround**: `AccountingSeeder` creates `"2100 — حسابات الموردين" (Accounts Payable)` as a **single liability account** for all suppliers. This is insufficient for per-supplier tracking — each supplier needs their own sub-account under `2100`.

**Fix**: Add `int? AccountId` FK to Supplier entity:
```csharp
public int? AccountId { get; private set; }
public Account.Account? Account { get; private set; }
```

**Auto-creation strategy**:
- On `Supplier.Create()` → auto-create `Account` under parent `"2100 — حسابات الموردين"`
- AccountCode pattern: supplier ID padded (e.g., `2101`, `2102`)
- Account naming: `NameAr = supplier.Name`, `NameEn = ""`
- Use `IAccountRepository` or direct `DbContext.Accounts.Add()` via `IUnitOfWork`

**Migration**:
```sql
ALTER TABLE Suppliers ADD AccountId int NULL;
ALTER TABLE Suppliers ADD CONSTRAINT FK_Suppliers_Accounts_AccountId
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE NO ACTION;
```

**Files changed**: `Supplier.cs`, `SupplierConfiguration.cs`, `SupplierService.cs`, `ISupplierService.cs`, `SupplierDto.cs`, `SupplierResponse.cs`, `SupplierRequests.cs`, migrations, `DbSeeder.cs`

### 3.3 Blocker 3: Manual try/catch/finally in SupplierListViewModel

**Problem**: `SupplierListViewModel.LoadSuppliersAsync()` (lines 154-196) uses manual `try/catch/finally` with `IsBusy = true/false` and `HandleException()` call. This violates RULE-141 which requires `ExecuteAsync()` wrapper for ALL async ViewModel operations.

**Current code** (lines 154-196):
```csharp
public async Task LoadSuppliersAsync()
{
    IsBusy = true;           // ❌ manual
    ErrorMessage = null;
    try                      // ❌ manual
    {
        var result = await _supplierService.GetAllAsync(IncludeInactive);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() => { /* ... */ });
        }
    }
    catch (Exception ex)     // ❌ manual
    {
        ErrorMessage = HandleException(ex, ...);
    }
    finally                  // ❌ manual
    {
        IsBusy = false;      // ❌ manual
    }
}
```

**Fix**: Move load logic into operation method, use ExecuteAsync wrapper:
```csharp
// CORRECT — use ExecuteAsync
RefreshCommand = new AsyncRelayCommand(
    (Func<Task>)(async () => await ExecuteAsync(LoadSuppliersOperationAsync)));

private async Task LoadSuppliersOperationAsync()
{
    ErrorMessage = null;
    var result = await _supplierService.GetAllAsync(IncludeInactive);
    if (result.IsSuccess && result.Value != null)
    {
        InvokeOnUIThread(() =>
        {
            Suppliers.Clear();
            foreach (var item in result.Value.OrderByDescending(x => x.Id))
                Suppliers.Add(item);
            SetupCollectionView();
            IsEmpty = !Suppliers.Any();
        });
    }
    else
    {
        ErrorMessage = HandleFailure(result.Error ?? "فشل تحميل الموردين", "LoadSuppliers");
    }
}
```

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

```csharp
public enum SupplierType : byte
{
    Cash = 0,    // Cash-on-delivery supplier (default)
    Credit = 1   // Credit-based supplier with payment terms
}
```

**Default**: `Cash (0)` — most retail suppliers are cash-on-delivery.

**Usage**:
- Control purchase invoice flow: Credit suppliers require CreditLimit check
- Filter suppliers in purchase invoice: show Cash suppliers by default
- Report segmentation: supplier aging by cash/credit

### 4.3 SupplierGroup Entity (Deferred to Non-V1)

```csharp
public class SupplierGroup : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    // CreatedAt, IsActive from BaseEntity
}
```

**V1 Decision**: **DEFERRED** — analysis Part 3 line 952 explicitly states:
> "لا توجد مجموعات عملاء أو موردين في V1"

Supplier filtering in V1 uses search only (name, phone). Group reports deferred.

### 4.4 SupplierDto (Enhanced)

```csharp
// Current — 10 fields:
public record SupplierDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance,
    decimal CreditLimit, bool IsActive
);

// Enhanced — add AccountId, SupplierType, AccountName:
public record SupplierDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance,
    decimal CreditLimit, bool IsActive,
    int? AccountId,              // NEW — Chart of Accounts FK
    string? AccountName,         // NEW — denormalized for UI display
    SupplierType SupplierType    // NEW — Cash=0, Credit=1
);
```

### 4.5 SupplierBalanceReportDto (New)

```csharp
public record SupplierBalanceReportDto(
    int SupplierId,
    string SupplierName,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal CreditLimitUsagePercent,  // (CurrentBalance / CreditLimit) * 100
    decimal TotalPurchases,
    decimal TotalPayments,
    int InvoiceCount,
    int OverdueDays
);
```

### 4.6 CreditLimitUsageDto (New)

```csharp
public record CreditLimitUsageDto(
    int SupplierId,
    string SupplierName,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal AvailableCredit,            // CreditLimit - CurrentBalance
    decimal UsagePercentage,            // (CurrentBalance / CreditLimit) * 100
    string Status,                      // "ضمن الحد المسموح" / "تجاوز الحد الائتماني"
    bool IsExceeded
);
```

### 4.7 Requests (Enhanced)

```csharp
// Current — 6 params:
public record CreateSupplierRequest(
    string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CreditLimit = 0
);

// Enhanced — add SupplierType, AccountId:
public record CreateSupplierRequest(
    string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CreditLimit = 0,
    SupplierType SupplierType = SupplierType.Cash,  // NEW
    int? AccountId = null                            // NEW (null = auto-create)
);

// Enhanced Update:
public record UpdateSupplierRequest(
    string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal CreditLimit, bool IsActive,
    SupplierType SupplierType = SupplierType.Cash,  // NEW
    int? AccountId = null                            // NEW
);
```

### 4.8 SupplierResponse (Enhanced)

```csharp
// Current — 7 fields:
public record SupplierResponse(
    int Id, string Name, string? Phone, string? Address, string? Email,
    decimal CurrentBalance, bool IsActive
);

// Enhanced — add AccountId, SupplierType, CreditLimit:
public record SupplierResponse(
    int Id, string Name, string? Phone, string? Address, string? Email,
    decimal CurrentBalance, bool IsActive,
    decimal CreditLimit,
    int? AccountId,
    SupplierType SupplierType
);
```

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

**Update**: If user explicitly requests SupplierGroup in V1, it becomes a Phase 24.5 addition with estimated +2 hours.

### 6.2 AccountId — Add NOW (Non-Fatal Blocker)

**Decision**: **Add `int? AccountId` FK NOW** as part of Phase 24. The FK is nullable (existing suppliers have no account link). This enables:

1. Auto-creation of accounting accounts for new suppliers (future Phase 25 integration)
2. Tracking supplier balance in general ledger
3. Account statements per supplier

**But**: Full auto-creation logic (`Supplier.Create` → `Account.Create`) is **deferred** to Phase 25 (Accounting Integration). In Phase 24, the FK exists but auto-creation is manual — the API accepts `AccountId` as optional. When null, no account is created.

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

**Note**: Full purchase-time credit limit validation is **Phase 25** (Purchase Invoice Module). Phase 24 creates the `IsCreditLimitExceeded()` method only.

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

**Code change** (DbSeeder.cs lines 128-134):
```csharp
// BEFORE — line 130:
name: "المورد الافتراضي في النظام",

// AFTER:
name: "مورد نقدي",
```

**Additionally** — verify SystemSetting `DefaultCashSupplierId` value matches the seeded supplier's Id:
```csharp
// After saving, ensure the default supplier has known ID
// The existing code seeds supplier FIRST, then accounting Data
// SystemSetting is seeded AFTER DbSeeder — the SystemSetting seed
// already references "DefaultCashSupplierId" = "1" which should match
```

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
```csharp
// New property
public int? AccountId { get; private set; }
public Account.Account? Account { get; private set; }

// Updated Create — optional accountId:
public static Supplier Create(
    string name,
    decimal openingBalance = 0,
    string? phone = null,
    string? email = null,
    string? address = null,
    string? taxNumber = null,
    decimal creditLimit = 0,
    int? accountId = null,           // NEW
    int? createdByUserId = null)
{
    // ... existing guards ...
    var supplier = new Supplier
    {
        // ... existing fields ...
        AccountId = accountId,         // NEW — optional link
    };
    supplier.SetCreatedBy(createdByUserId);
    return supplier;
}

// Updated Update:
public void Update(
    string name, string? phone, string? email, string? address,
    string? taxNumber, decimal creditLimit, int? accountId,  // NEW
    int? updatedByUserId)
{
    // ... existing guards ...
    AccountId = accountId;            // NEW
    // ...
}
```

**Configuration change** (SupplierConfiguration.cs):
```csharp
// Add AFTER existing property configs:
builder.Property(s => s.AccountId).IsRequired(false);

// Add FK relationship:
builder.HasOne(s => s.Account)
    .WithMany()
    .HasForeignKey(s => s.AccountId)
    .OnDelete(DeleteBehavior.Restrict);  // RULE-214
```

**DTO change** (AllDtos.cs):
```csharp
public record SupplierDto(
    int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance,
    decimal CreditLimit, bool IsActive,
    int? AccountId = null,                  // NEW
    string? AccountName = null,             // NEW — denormalized for display
    SupplierType SupplierType = SupplierType.Cash  // NEW (default Cash)
) : IMapFrom<Supplier>;
```

**Request changes** (SupplierRequests.cs):
```csharp
public record CreateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal OpeningBalance,
    decimal CreditLimit = 0,
    SupplierType SupplierType = SupplierType.Cash,  // NEW
    int? AccountId = null                            // NEW
);

public record UpdateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    bool IsActive,
    SupplierType SupplierType = SupplierType.Cash,  // NEW
    int? AccountId = null                            // NEW
);
```

**Service mapping** (SupplierService.cs MapToDto):
```csharp
private static SupplierDto MapToDto(Supplier s)
{
    return new SupplierDto(
        s.Id, s.Name, s.Phone, s.Email, s.Address,
        s.TaxNumber, s.OpeningBalance, s.CurrentBalance,
        s.CreditLimit, s.IsActive,
        s.AccountId,                                // NEW
        s.Account?.NameAr,                          // NEW — eager load Account
        s.SupplierType                              // NEW
    );
}
```

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
```csharp
private async Task<Result> CreateOpeningBalanceJournalEntryAsync(
    Supplier supplier, decimal openingBalance, int? createdByUserId, CancellationToken ct)
{
    // 1. Ensure supplier has an AccountId (auto-create if missing)
    int? supplierAccountId = supplier.AccountId;
    if (supplierAccountId == null)
    {
        var accountResult = await CreateSupplierAccountAsync(supplier, createdByUserId, ct);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);
        supplierAccountId = accountResult.Value.Id;
        supplier.LinkToAccount(supplierAccountId.Value, createdByUserId);
    }

    // 2. Find or use default inventory/stock account (e.g., 1500 — المخزون)
    var inventoryAccount = await _uow.Accounts.FirstOrDefaultAsync(
        a => a.AccountCode == "1500", ct);
    if (inventoryAccount == null)
        return Result.Failure(
            "حساب المخزون (1500) غير موجود في دليل الحسابات. يرجى التأكد من تشغيل Phase 22",
            ErrorCodes.NotFound);

    // 3. Create journal entry via JournalEntryService (Phase 30 contract)
    var journalEntry = JournalEntry.Create(
        entryDate: DateTime.UtcNow,
        description: $"رصيد افتتاحي للمورد: {supplier.Name}",
        entryType: JournalEntryType.OpeningBalance, // = 9
        createdByUserId: createdByUserId
    );

    // Debit: Inventory account
    journalEntry.AddLine(
        accountId: inventoryAccount.Id,
        debitAmount: openingBalance,
        creditAmount: 0,
        description: $"رصيد افتتاحي — بضاعة مورد: {supplier.Name}"
    );

    // Credit: Supplier account
    journalEntry.AddLine(
        accountId: supplierAccountId.Value,
        debitAmount: 0,
        creditAmount: openingBalance,
        description: $"رصيد افتتاحي — المورد: {supplier.Name}"
    );

    await _uow.JournalEntries.AddAsync(journalEntry, ct);
    return Result.Success();
}
```

**Integration into CreateAsync flow**:
```csharp
public async Task<Result<SupplierDto>> CreateAsync(
    CreateSupplierRequest request, CancellationToken ct)
{
    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        var supplier = Supplier.Create(/* ... */);

        // Step 1: Auto-create sub-account under 2100 — حسابات الموردين
        var accountResult = await CreateSupplierAccountAsync(supplier, request.CreatedByUserId, ct);
        if (!accountResult.IsSuccess)
            return Result<SupplierDto>.Failure(accountResult.Error!);

        var accountId = accountResult.Value.Id;
        supplier.LinkToAccount(accountId, request.CreatedByUserId);
        supplier.SetSupplierType(request.SupplierType);

        await _uow.Suppliers.AddAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        // Step 2: If OpeningBalance > 0, create automatic journal entry
        if (request.OpeningBalance > 0)
        {
            var journalResult = await CreateOpeningBalanceJournalEntryAsync(
                supplier, request.OpeningBalance, request.CreatedByUserId, ct);
            if (!journalResult.IsSuccess)
                return Result<SupplierDto>.Failure(journalResult.Error!);
            await _uow.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Supplier {Id} created with OpeningBalance={Balance}, AccountId={AccountId}",
            supplier.Id, request.OpeningBalance, accountId);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        _logger.LogError(ex, "Failed to create supplier with opening balance entry");
        return Result<SupplierDto>.Failure("حدث خطأ أثناء إنشاء المورد");
    }
}
```

**Helper — CreateSupplierAccountAsync()**:
```csharp
private async Task<Result<Account>> CreateSupplierAccountAsync(
    Supplier supplier, int? createdByUserId, CancellationToken ct)
{
    // Find parent: 2100 — حسابات الموردين
    var parentAccount = await _uow.Accounts.FirstOrDefaultAsync(
        a => a.AccountCode == "2100", ct);
    if (parentAccount == null)
        return Result<Account>.Failure(
            "حساب الموردين (2100) غير موجود في دليل الحسابات",
            ErrorCodes.NotFound);

    var maxCode = await _uow.Accounts.GetMaxCodeUnderParentAsync(2100, ct);
    var newCode = $"210{maxCode + 1}"; // e.g., 2101, 2102, ...

    var account = Account.Create(
        nameAr: supplier.Name,
        nameEn: "",
        accountCode: newCode,
        parentId: parentAccount.Id,
        accountType: AccountType.Liability,
        level: 4,
        isSystemAccount: false,
        createdByUserId: createdByUserId
    );

    await _uow.Accounts.AddAsync(account, ct);
    return Result<Account>.Success(account);
}
```

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
```csharp
namespace SalesSystem.Domain.Enums;

public enum SupplierType : byte
{
    Cash = 0,
    Credit = 1
}
```

**Entity change**:
```csharp
public SupplierType SupplierType { get; private set; } = SupplierType.Cash;

// Create() update:
public static Supplier Create(..., SupplierType supplierType = SupplierType.Cash, ...)
{
    // ...
    SupplierType = supplierType;
    // ...
}

// Update() update:
public void Update(..., SupplierType supplierType, ...)
{
    // ...
    SupplierType = supplierType;
    // ...
}
```

**Configuration change**:
```csharp
builder.Property(s => s.SupplierType)
    .HasConversion<int>()
    .HasDefaultValue(0);            // SupplierType.Cash
```

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
```csharp
public interface ISupplierService
{
    // Existing methods...
    Task<Result<List<SupplierBalanceReportDto>>> GetBalanceReportAsync(CancellationToken ct = default);
    Task<Result<CreditLimitUsageDto>> GetCreditLimitUsageAsync(int supplierId, CancellationToken ct = default);
}
```

**Balance report implementation**:
```csharp
public async Task<Result<List<SupplierBalanceReportDto>>> GetBalanceReportAsync(CancellationToken ct)
{
    try
    {
        var suppliers = await _uow.Suppliers.GetAllAsync(ct);
        
        var report = suppliers.Select(s => new SupplierBalanceReportDto(
            s.Id,
            s.Name,
            s.CurrentBalance,
            s.CreditLimit,
            s.CreditLimit > 0 
                ? Math.Round((s.CurrentBalance / s.CreditLimit) * 100, 1)
                : 0m,
            0,  // TotalPurchases — requires purchase invoice aggregation (Phase 26)
            0,  // TotalPayments — requires payment aggregation
            0,  // InvoiceCount
            0   // OverdueDays
        )).OrderByDescending(r => r.CurrentBalance).ToList();

        return Result<List<SupplierBalanceReportDto>>.Success(report);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to generate supplier balance report");
        return Result<List<SupplierBalanceReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير أرصدة الموردين");
    }
}
```

**Credit limit usage implementation**:
```csharp
public async Task<Result<CreditLimitUsageDto>> GetCreditLimitUsageAsync(int supplierId, CancellationToken ct)
{
    var supplier = await _uow.Suppliers.GetByIdAsync(supplierId, ct);
    if (supplier == null)
        return Result<CreditLimitUsageDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

    if (supplier.SupplierType == Domain.Enums.SupplierType.Cash)
        return Result<CreditLimitUsageDto>.Success(new CreditLimitUsageDto(
            supplier.Id, supplier.Name,
            supplier.CurrentBalance, supplier.CreditLimit,
            0, 0, "المورد نقدي — لا ينطبق حد ائتماني", false));

    var availableCredit = supplier.CreditLimit - supplier.CurrentBalance;
    var usagePercent = supplier.CreditLimit > 0
        ? Math.Round((supplier.CurrentBalance / supplier.CreditLimit) * 100, 1)
        : 0m;
    var isExceeded = supplier.CurrentBalance > supplier.CreditLimit && supplier.CreditLimit > 0;
    var status = isExceeded
        ? "⚠️ تجاوز الحد الائتماني"
        : "✅ ضمن الحد المسموح";

    return Result<CreditLimitUsageDto>.Success(new CreditLimitUsageDto(
        supplier.Id, supplier.Name,
        supplier.CurrentBalance, supplier.CreditLimit,
        Math.Max(0, availableCredit), usagePercent,
        status, isExceeded));
}

/// <summary>
/// Check if purchase would exceed supplier credit limit.
/// Called from PurchaseInvoiceService during purchase creation.
/// </summary>
public async Task<bool> IsCreditLimitExceededAsync(int supplierId, decimal invoiceTotal, CancellationToken ct)
{
    var supplier = await _uow.Suppliers.GetByIdAsync(supplierId, ct);
    if (supplier == null || supplier.SupplierType == Domain.Enums.SupplierType.Cash)
        return false;  // Cash suppliers have no credit limit check
    
    if (supplier.CreditLimit <= 0)
        return true;   // No credit limit set = cannot use credit
    
    return (supplier.CurrentBalance + invoiceTotal) > supplier.CreditLimit;
}
```

**⚠️ Note**: `IsCreditLimitExceededAsync` is created here but **called** from Phase 26 (Purchase Invoice Module). Phase 24 creates the method + wires it into the service.

**DTOs**:
```csharp
public record SupplierBalanceReportDto(
    int SupplierId, string SupplierName, decimal CurrentBalance,
    decimal CreditLimit, decimal CreditLimitUsagePercent,
    decimal TotalPurchases, decimal TotalPayments,
    int InvoiceCount, int OverdueDays
);

public record CreditLimitUsageDto(
    int SupplierId, string SupplierName,
    decimal CurrentBalance, decimal CreditLimit,
    decimal AvailableCredit, decimal UsagePercentage,
    string Status, bool IsExceeded
);
```

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
```csharp
[HttpGet("reports/balance-summary")]
[Authorize(Policy = "ManagerAndAbove")]
[ProducesResponseType(typeof(List<SupplierBalanceReportDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetBalanceSummary(CancellationToken ct)
{
    var result = await _supplierService.GetBalanceReportAsync(ct);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
}

[HttpGet("{id:int}/credit-limit")]
[Authorize(Policy = "ManagerAndAbove")]
[ProducesResponseType(typeof(CreditLimitUsageDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetCreditLimitUsage(int id, CancellationToken ct)
{
    var result = await _supplierService.GetCreditLimitUsageAsync(id, ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
}

[HttpGet("{id:int}/transactions")]
[Authorize(Policy = "ManagerAndAbove")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetTransactionHistory(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
{
    var result = await _supplierService.GetTransactionHistoryAsync(id, from, to, ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
}
```

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
```csharp
// Constructor:
RefreshCommand = new AsyncRelayCommand(
    (Func<Task>)(async () => await ExecuteAsync(LoadSuppliersOperationAsync)));

// Operation method:
private async Task LoadSuppliersOperationAsync()
{
    ErrorMessage = null;
    var result = await _supplierService.GetAllAsync(IncludeInactive);
    if (result.IsSuccess && result.Value != null)
    {
        InvokeOnUIThread(() =>
        {
            Suppliers.Clear();
            foreach (var item in result.Value.OrderByDescending(x => x.Id))
                Suppliers.Add(item);
            SetupCollectionView();
            IsEmpty = !Suppliers.Any();
        });
    }
    else
    {
        ErrorMessage = HandleFailure(result.Error ?? "فشل تحميل الموردين", "LoadSuppliers");
    }
}
```

**Same pattern for DeleteSupplierAsync and RestoreSupplierAsync**.

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
```csharp
private SupplierType _supplierType = SupplierType.Cash;
public SupplierType SelectedSupplierType
{
    get => _supplierType;
    set => SetProperty(ref _supplierType, value);
}

private int? _accountId;
public int? AccountId
{
    get => _accountId;
    set => SetProperty(ref _accountId, value);
}

private string _accountName = string.Empty;
public string AccountName
{
    get => _accountName;
    set => SetProperty(ref _accountName, value);
}

public ObservableCollection<AccountDto> Accounts { get; } = new();
public bool IsCreditSupplier => SelectedSupplierType == SupplierType.Credit;
```

**Validation additions**:
```csharp
// In ValidateAsync():
if (SelectedSupplierType == SupplierType.Credit && CreditLimit <= 0)
    AddError(nameof(CreditLimit), "حد الائتمان مطلوب للموردين الآجلين");
```

**SaveOperationAsync updates**:
```csharp
var createRequest = new CreateSupplierRequest(
    Name, Phone, Email, Address, TaxNumber,
    OpeningBalance, CreditLimit,
    SelectedSupplierType,         // NEW
    AccountId                     // NEW
);
```

**Filter for account selection**:
```csharp
// Load only liability accounts (AccountType = 2 / Liability)
// Under parent "2100 — حسابات الموردين"
private async Task LoadAccountsAsync()
{
    var result = await _accountService.GetAllAsync();
    if (result.IsSuccess)
    {
        Accounts.Clear();
        foreach (var acct in result.Value
            .Where(a => a.AccountType == AccountType.Liability)
            .OrderBy(a => a.AccountCode))
        {
            Accounts.Add(acct);
        }
    }
}
```

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

**SupplierType section**:
```xml
<!-- Supplier Type -->
<TextBlock Text="نوع المورد *" Style="{StaticResource LabelStyle}"/>
<StackPanel Orientation="Horizontal" Margin="0,0,0,6">
    <RadioButton Content="نقدي" 
                 IsChecked="{Binding SelectedSupplierType, Converter={StaticResource EnumToBoolConverter}, ConverterParameter=Cash}"
                 GroupName="SupplierType"
                 ToolTip="مورد نقدي — الدفع عند الاستلام"/>
    <RadioButton Content="آجل" 
                 IsChecked="{Binding SelectedSupplierType, Converter={StaticResource EnumToBoolConverter}, ConverterParameter=Credit}"
                 GroupName="SupplierType"
                 Margin="20,0,0,0"
                 ToolTip="مورد آجل — له حد ائتماني وفترة سداد"/>
</StackPanel>
```

**CreditLimit section**:
```xml
<!-- Credit Limit (visible only for Credit suppliers) -->
<StackPanel Visibility="{Binding IsCreditSupplier, Converter={StaticResource BoolToVisibility}}">
    <TextBlock Text="حد الائتمان" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding CreditLimit, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
             Style="{StaticResource ModernTextBox}"
             ToolTip="الحد الأقصى للائتمان المسموح به لهذا المورد"/>
    <Border Height="6"/>
</StackPanel>
```

**Account selection**:
```xml
<!-- الحساب المحاسبي (اختياري) -->
<TextBlock Text="الحساب المحاسبي" Style="{StaticResource LabelStyle}"/>
<ComboBox ItemsSource="{Binding Accounts}"
          SelectedValue="{Binding AccountId}"
          SelectedValuePath="Id"
          DisplayMemberPath="NameAr"
          Style="{StaticResource ModernComboBox}"
          ToolTip="ربط المورد بحساب في دليل الحسابات — اختياري"
          IsEditable="True"
          Text="{Binding AccountSearchText}"/>
<TextBlock Text="ربط المورد بحساب في دليل الحسابات (اختياري)" 
           Style="{StaticResource HelperTextStyle}"/>
```

**Current Balance display (edit mode)**:
```xml
<!-- الرصيد الحالي (للقراءة فقط) -->
<StackPanel Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibility}}">
    <TextBlock Text="الرصيد الحالي" Style="{StaticResource LabelStyle}"/>
    <Border Background="{StaticResource InfoLightBrush}" 
            CornerRadius="6" Padding="12,8" Margin="0,0,0,8">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="💳" Margin="0,0,8,0" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding CurrentBalance, StringFormat=N2}" 
                       FontSize="18" FontWeight="Bold" 
                       Foreground="{StaticResource PrimaryBrush}"/>
        </StackPanel>
    </Border>
</StackPanel>
```

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
```csharp
public partial class SupplierEditorView : Window
{
    public SupplierEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SupplierEditorViewModel vm)
        {
            vm.FocusFirstInvalidFieldRequested += (_, _) =>
                ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
            
            // For new window threads — position overlays
            PositionOverOwner();
        }
    }

    private void PositionOverOwner()
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow != null && mainWindow != this)
        {
            Owner = mainWindow;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
```

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
```xml
<!-- نوع المورد -->
<DataGridTemplateColumn Header="النوع" Width="80" CellStyle="{StaticResource DataGridCellCenterStyle}">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBlock>
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
                        <Setter Property="Text" Value="نقدي"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding SupplierType}" Value="1">
                                <Setter Property="Foreground" Value="{StaticResource WarningBrush}"/>
                                <Setter Property="Text" Value="آجل"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<!-- حد الائتمان -->
<DataGridTextColumn Header="حد الائتمان" 
                    Binding="{Binding CreditLimit, StringFormat=N2}" 
                    Width="110" 
                    CellStyle="{StaticResource DataGridCellCenterStyle}"/>

<!-- الحساب المحاسبي -->
<DataGridTextColumn Header="الحساب" 
                    Binding="{Binding AccountName}" 
                    Width="*" 
                    CellStyle="{StaticResource DataGridCellStyle}"/>
```

**Enhanced CurrentBalance with color coding**:
```xml
<DataGridTemplateColumn Header="الرصيد الحالي" Width="130" CellStyle="{StaticResource DataGridCellCenterStyle}">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding CurrentBalance, StringFormat=N2}">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="FontWeight" Value="SemiBold"/>
                        <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding CurrentBalance}" Value="0">
                                <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
                            </DataTrigger>
                            <!-- Negative balance = supplier owes us (asset) = orange -->
                            <DataTrigger Binding="{Binding CurrentBalance}" Value="0">
                                <Setter Property="Foreground" Value="{StaticResource WarningBrush}"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**⚠️ Note**: Negative balance DataTrigger won't work directly with decimal bindings (WPF limitation). Use a `BalanceColor` computed property in the ViewModel or a value converter instead:
```csharp
// In ViewModel (per-supplier computed property on a wrapper):
public string BalanceColor => CurrentBalance > 0 
    ? "#F44336"   // Red — we owe them (liability)
    : CurrentBalance < 0 
        ? "#FF9800" // Orange — they owe us
        : "#9E9E9E";  // Gray — settled
```

**Search filter update**:
```csharp
private bool FilterSuppliers(object obj)
{
    if (obj is not SupplierDto supplier) return false;
    if (string.IsNullOrWhiteSpace(SearchText)) return true;

    var searchLower = SearchText.Trim().ToLower();
    return supplier.Name.ToLower().Contains(searchLower) ||
           (supplier.Phone?.ToLower().Contains(searchLower) ?? false) ||
           (supplier.Email?.ToLower().Contains(searchLower) ?? false) ||
           (supplier.Address?.ToLower().Contains(searchLower) ?? false) ||
           (supplier.AccountName?.ToLower().Contains(searchLower) ?? false) ||  // NEW
           supplier.SupplierType.ToString().ToLower().Contains(searchLower);    // NEW
}
```

**Estimate**: ~1.5 hours

---

### Task 11 — Update FluentValidators + Validator Tests

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/SupplierRequestValidators.cs` | Add TaxNumber format, AccountId exists, SupplierType enum rules |
| `Tests/SalesSystem.Api.Tests/Validators/SupplierRequestValidatorTests.cs` | Update tests for new rules |

**Validator enhancements**:
```csharp
// CreateSupplierRequestValidator additions:
RuleFor(x => x.TaxNumber)
    .MaximumLength(30).WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 30 حرف")
    .Matches(@"^\d{15}$").WithMessage("الرقم الضريبي يجب أن يتكون من 15 رقم")
    .When(x => !string.IsNullOrEmpty(x.TaxNumber));

RuleFor(x => x.SupplierType)
    .IsInEnum().WithMessage("نوع المورد غير صالح");

RuleFor(x => x.CreditLimit)
    .GreaterThanOrEqualTo(0).WithMessage("حد الائتمان لا يمكن أن يكون سالباً")
    .GreaterThan(0).When(x => x.SupplierType == SupplierType.Credit)
        .WithMessage("حد الائتمان مطلوب للموردين الآجلين");

// UpdateSupplierRequestValidator additions: same rules minus OpeningBalance
```

**Estimate**: ~30 minutes

---

### Task 12 — Update Seed Data with Defaults

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Set AccountId for default supplier (link to Accounts Payable) |
| `Infrastructure/Data/DbSeeder.cs` | Set SupplierType = Cash for default supplier |

**Seed update**:
```csharp
// 7. Default supplier — for cash purchases
// Get the "حسابات الموردين" account (AccountCode = "2100")
var apAccount = await db.Set<Account>().FirstOrDefaultAsync(a => a.AccountCode == "2100");

var defaultSupplier = Supplier.Create(
    name: "مورد نقدي",
    openingBalance: 0m,
    supplierType: SupplierType.Cash,
    accountId: apAccount?.Id,          // Link to Accounts Payable
    createdByUserId: null
);
db.Suppliers.Add(defaultSupplier);
```

**⚠️ Note**: Seed order matters. `Supplier` is seeded BEFORE `AccountingSeeder` in the current `DbSeeder` (see lines 111-134). The accounting seed happens at line 178. We need to:
1. Either move supplier seed AFTER accounting seed
2. Or use a two-pass approach: seed supplier first (without AccountId), then update after accounting

**Recommended**: Keep current order (seeds before accounting), set `AccountId = null` for default supplier, and add a **second pass** after accounting seed:
```csharp
// DbSeeder — AFTER accounting seed (line 178):
// Update default supplier with AccountId
var defaultSupplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == 1);
var apAccount = await db.Set<Account>().FirstOrDefaultAsync(a => a.AccountCode == "2100");
if (defaultSupplier != null && apAccount != null)
{
    defaultSupplier.LinkToAccount(apAccount.Id, null);
}
```

This requires adding a `LinkToAccount` method to Supplier entity:
```csharp
public void LinkToAccount(int accountId, int? updatedByUserId)
{
    AccountId = accountId;
    SetUpdatedBy(updatedByUserId);
    UpdateTimestamp();
}
```

**Estimate**: ~20 minutes

---

### Task 13 — Add ISupplierApiService Methods for New Endpoints

**Files**:

| File | Change |
|------|--------|
| `Services/Api/IApiService.cs` — `ISupplierApiService` | Add `GetBalanceReportAsync()`, `GetCreditLimitUsageAsync(int id)`, `GetTransactionHistoryAsync(int id, DateTime?, DateTime?)` |
| `Services/Api/SupplierWarehouseApiService.cs` | Implement the 3 new HTTP methods |

**Interface additions**:
```csharp
public interface ISupplierApiService
{
    // Existing methods...
    Task<Result<List<SupplierBalanceReportDto>>> GetBalanceReportAsync();
    Task<Result<CreditLimitUsageDto>> GetCreditLimitUsageAsync(int supplierId);
    Task<Result<List<SupplierTransactionDto>>> GetTransactionHistoryAsync(int supplierId, DateTime? from = null, DateTime? to = null);
}
```

**HTTP implementation**:
```csharp
public async Task<Result<List<SupplierBalanceReportDto>>> GetBalanceReportAsync()
{
    return await ExecuteAsync<List<SupplierBalanceReportDto>>(
        () => _httpClient.GetAsync("api/v1/suppliers/reports/balance-summary"),
        "SupplierApiService.GetBalanceReportAsync");
}

public async Task<Result<CreditLimitUsageDto>> GetCreditLimitUsageAsync(int supplierId)
{
    return await ExecuteAsync<CreditLimitUsageDto>(
        () => _httpClient.GetAsync($"api/v1/suppliers/{supplierId}/credit-limit"),
        "SupplierApiService.GetCreditLimitUsageAsync");
}

public async Task<Result<List<SupplierTransactionDto>>> GetTransactionHistoryAsync(
    int supplierId, DateTime? from = null, DateTime? to = null)
{
    var query = $"api/v1/suppliers/{supplierId}/transactions";
    if (from.HasValue) query += $"?from={from:yyyy-MM-dd}";
    if (to.HasValue) query += $"{(from.HasValue ? "&" : "?")}to={to:yyyy-MM-dd}";
    
    return await ExecuteAsync<List<SupplierTransactionDto>>(
        () => _httpClient.GetAsync(query),
        "SupplierApiService.GetTransactionHistoryAsync");
}
```

**Dto for transaction history**:
```csharp
public record SupplierTransactionDto(
    DateTime Date,
    string ReferenceType,      // "فاتورة شراء", "سند صرف", "مرتجع شراء"
    int ReferenceId,
    string Description,
    decimal Debit,             // Amount we owe (increase)
    decimal Credit,            // Amount we paid (decrease)
    decimal Balance            // Running balance
);
```

**Estimate**: ~30 minutes

---

### Task 14 — Enhanced Search (By SupplierType + Account)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/SupplierService.cs` | Extend `GetAllAsync` search predicate with SupplierType filtering |

**Search predicate extension**:
```csharp
// Current predicate — search by Name or Phone:
predicate = sup => sup.Name.Contains(s) || (sup.Phone != null && sup.Phone.Contains(s));

// Enhanced — add SupplierType filter AND account search:
if (int.TryParse(s, out int num))
{
    predicate = sup => sup.Name.Contains(s) || 
                       (sup.Phone != null && sup.Phone.Contains(s)) ||
                       (sup.AccountId.HasValue && sup.AccountId.Value == num);
}
else
{
    predicate = sup => sup.Name.Contains(s) || 
                       (sup.Phone != null && sup.Phone.Contains(s)) ||
                       (sup.Account != null && sup.Account.NameAr.Contains(s));
}
```

**Additionally**: Add a `supplierType` query filter parameter to the API controller:
```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] bool includeInactive = false,
    [FromQuery] SupplierType? supplierType = null,  // NEW
    CancellationToken ct = default)
```

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

```sql
-- Revert all Phase 24 DB changes
ALTER TABLE Suppliers DROP CONSTRAINT FK_Suppliers_Accounts_AccountId;
ALTER TABLE Suppliers DROP COLUMN AccountId;
ALTER TABLE Suppliers DROP COLUMN SupplierType;
UPDATE Suppliers SET Name = N'المورد الافتراضي في النظام' WHERE Id = 1;
```

**Code revert**: `git revert <phase-24-merge-commit>` or revert individual files:
```bash
git checkout HEAD~1 -- SalesSystem/SalesSystem.Domain/Entities/Supplier.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.Domain/Enums/SupplierType.cs
git checkout HEAD~1 -- SalesSystem/Infrastructure/Data/DbSeeder.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.DesktopPWF/ViewModels/Suppliers/
git checkout HEAD~1 -- SalesSystem/SalesSystem.DesktopPWF/Views/Suppliers/
git checkout HEAD~1 -- SalesSystem/SalesSystem.Contracts/DTOs/AllDtos.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.Contracts/Requests/SupplierRequests.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.Contracts/Responses/SupplierResponse.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.Api/Controllers/SuppliersController.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.Application/Services/SupplierService.cs
git checkout HEAD~1 -- SalesSystem/SalesSystem.DesktopPWF/Services/Api/SupplierWarehouseApiService.cs
```

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

```csharp
[Fact]
public void Create_WithValidInputs_CreatesSupplierCorrectly()
{
    var supplier = Supplier.Create(
        name: "مورد تجربة",
        openingBalance: 5000m,
        phone: "0555123456",
        email: "test@example.com",
        address: "الرياض، المملكة العربية السعودية",
        taxNumber: "123456789012345",
        creditLimit: 10000m,
        accountId: 15,
        createdByUserId: 1);

    Assert.Equal("مورد تجربة", supplier.Name);
    Assert.Equal(5000m, supplier.OpeningBalance);
    Assert.Equal(5000m, supplier.CurrentBalance); // OpeningBalance → CurrentBalance
    Assert.Equal("0555123456", supplier.Phone);
    Assert.Equal("test@example.com", supplier.Email);
    Assert.Equal("الرياض، المملكة العربية السعودية", supplier.Address);
    Assert.Equal("123456789012345", supplier.TaxNumber);
    Assert.Equal(10000m, supplier.CreditLimit);
    Assert.Equal(15, supplier.AccountId);
    Assert.Equal(1, supplier.CreatedByUserId);
    Assert.True(supplier.IsActive);
    Assert.Equal(SupplierType.Cash, supplier.SupplierType);
    Assert.NotEqual(default, supplier.CreatedAt);
}

[Fact]
public void Create_WithDefaultSupplierType_SetsCash()
{
    var supplier = Supplier.Create("مورد نقدي");
    Assert.Equal(SupplierType.Cash, supplier.SupplierType);
}

[Fact]
public void Create_WithCreditSupplierType_SetsCredit()
{
    var supplier = Supplier.Create("مورد آجل", supplierType: SupplierType.Credit);
    Assert.Equal(SupplierType.Credit, supplier.SupplierType);
}

[Fact]
public void Create_WithEmptyName_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() => Supplier.Create(""));
    Assert.Contains("الاسم مطلوب", ex.Message);
}

[Fact]
public void Create_WithNullName_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() => Supplier.Create(null!));
    Assert.Contains("الاسم مطلوب", ex.Message);
}

[Fact]
public void Create_WithWhitespaceName_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() => Supplier.Create("   "));
    Assert.Contains("الاسم مطلوب", ex.Message);
}

[Fact]
public void Create_WithNegativeOpeningBalance_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Supplier.Create("مورد", openingBalance: -100m));
    Assert.Contains("الرصيد الافتتاحي لا يمكن أن يكون سالباً", ex.Message);
}

[Fact]
public void Create_WithZeroOpeningBalance_IsAllowed()
{
    var supplier = Supplier.Create("مورد نقدي", openingBalance: 0m);
    Assert.Equal(0m, supplier.OpeningBalance);
    Assert.Equal(0m, supplier.CurrentBalance);
}

[Fact]
public void Create_WithPositiveOpeningBalance_SetsCurrentBalance()
{
    var supplier = Supplier.Create("مورد", openingBalance: 5000m);
    Assert.Equal(5000m, supplier.CurrentBalance);
}

[Fact]
public void Create_WithNegativeCreditLimit_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Supplier.Create("مورد", creditLimit: -500m));
    Assert.Contains("حد الائتمان لا يمكن أن يكون سالباً", ex.Message);
}

[Fact]
public void Create_WithZeroCreditLimit_IsAllowed()
{
    var supplier = Supplier.Create("مورد", creditLimit: 0m);
    Assert.Equal(0m, supplier.CreditLimit);
}

[Fact]
public void Create_WithPositiveCreditLimit_SetsCorrectly()
{
    var supplier = Supplier.Create("مورد", creditLimit: 25000m);
    Assert.Equal(25000m, supplier.CreditLimit);
}

[Fact]
public void Create_WithNameExceedingMaxLength_ThrowsDomainException()
{
    var longName = new string('ا', 151);
    var ex = Assert.Throws<DomainException>(() =>
        Supplier.Create(longName));
    Assert.Contains("الاسم لا يمكن أن يتجاوز 150 حرف", ex.Message);
}

[Fact]
public void Create_WithNullOptionalParameters_CreatesSuccessfully()
{
    var supplier = Supplier.Create("مورد", createdByUserId: null);
    Assert.Null(supplier.CreatedByUserId);
    Assert.Null(supplier.Phone);
    Assert.Null(supplier.Email);
    Assert.Null(supplier.Address);
    Assert.Null(supplier.TaxNumber);
    Assert.Null(supplier.AccountId);
}

[Fact]
public void Update_WithValidInputs_UpdatesFieldsCorrectly()
{
    var supplier = Supplier.Create("مورد قديم", openingBalance: 1000m);
    supplier.Update("مورد محدث", "0555111111", "new@test.com",
        "جدة", "987654321098765", 20000m,
        SupplierType.Credit, accountId: 20, updatedByUserId: 2);

    Assert.Equal("مورد محدث", supplier.Name);
    Assert.Equal("0555111111", supplier.Phone);
    Assert.Equal("new@test.com", supplier.Email);
    Assert.Equal("جدة", supplier.Address);
    Assert.Equal("987654321098765", supplier.TaxNumber);
    Assert.Equal(20000m, supplier.CreditLimit);
    Assert.Equal(SupplierType.Credit, supplier.SupplierType);
    Assert.Equal(20, supplier.AccountId);
}

[Fact]
public void Update_WithEmptyName_ThrowsDomainException()
{
    var supplier = Supplier.Create("مورد");
    var ex = Assert.Throws<DomainException>(() =>
        supplier.Update("", null, null, null, null, 0, SupplierType.Cash, null, null));
    Assert.Contains("الاسم مطلوب", ex.Message);
}

[Fact]
public void Update_DoesNotChangeOpeningBalanceOrCurrentBalance()
{
    var supplier = Supplier.Create("مورد", openingBalance: 5000m);
    supplier.Update("مورد محدث", null, null, null, null, 0, SupplierType.Cash, null, null);
    Assert.Equal(5000m, supplier.OpeningBalance);
    Assert.Equal(5000m, supplier.CurrentBalance);
}

[Fact]
public void IncreaseBalance_AddsToCurrentBalance()
{
    var supplier = Supplier.Create("مورد");
    supplier.IncreaseBalance(1000m);
    Assert.Equal(1000m, supplier.CurrentBalance);
}

[Fact]
public void DecreaseBalance_SubtractsFromCurrentBalance()
{
    var supplier = Supplier.Create("مورد", openingBalance: 5000m);
    supplier.DecreaseBalance(2000m);
    Assert.Equal(3000m, supplier.CurrentBalance);
}

[Fact]
public void DecreaseBalance_WithNegativeAmount_ThrowsDomainException()
{
    var supplier = Supplier.Create("مورد", openingBalance: 1000m);
    var ex = Assert.Throws<DomainException>(() =>
        supplier.DecreaseBalance(-100m));
    Assert.Contains("المبلغ يجب أن يكون أكبر من الصفر", ex.Message);
}

[Fact]
public void IncreaseBalance_WithNegativeAmount_ThrowsDomainException()
{
    var supplier = Supplier.Create("مورد");
    var ex = Assert.Throws<DomainException>(() =>
        supplier.IncreaseBalance(-100m));
    Assert.Contains("المبلغ يجب أن يكون أكبر من الصفر", ex.Message);
}

[Fact]
public void LinkToAccount_SetsAccountIdCorrectly()
{
    var supplier = Supplier.Create("مورد");
    supplier.LinkToAccount(42, 1);
    Assert.Equal(42, supplier.AccountId);
}

[Fact]
public void LinkToAccount_OverwritesPreviousAccountId()
{
    var supplier = Supplier.Create("مورد", accountId: 10);
    supplier.LinkToAccount(99, 1);
    Assert.Equal(99, supplier.AccountId);
}

[Fact]
public void SetSupplierType_ChangesTypeCorrectly()
{
    var supplier = Supplier.Create("مورد");
    Assert.Equal(SupplierType.Cash, supplier.SupplierType);
    supplier.SetSupplierType(SupplierType.Credit);
    Assert.Equal(SupplierType.Credit, supplier.SupplierType);
}
```

**Estimate**: ~2 hours

---

#### 16.2 — Service Tests (Mock\<IUnitOfWork\>)

```csharp
public class SupplierServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ISupplierRepository> _supplierRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<ILogger<SupplierService>> _loggerMock;
    private readonly SupplierService _service;

    public SupplierServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _supplierRepoMock = new Mock<ISupplierRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();
        _loggerMock = new Mock<ILogger<SupplierService>>();

        _uowMock.Setup(x => x.Suppliers).Returns(_supplierRepoMock.Object);
        _uowMock.Setup(x => x.Accounts).Returns(_accountRepoMock.Object);

        _service = new SupplierService(_uowMock.Object, _loggerMock.Object);
    }

    // ─── CreateAsync Success ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateSupplierRequest(
            "مورد جديد", "0555123456", "test@test.com", "الرياض",
            "123456789012345", 0m, 10000m,
            SupplierType.Cash, accountId: null);

        _supplierRepoMock.Setup(x => x.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("مورد جديد", result.Value.Name);
    }

    // ─── CreateAsync Failure ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithEmptyName_ReturnsFailure()
    {
        var request = new CreateSupplierRequest(
            "", "0555123456", null, null, null, 0m, 0m,
            SupplierType.Cash, null);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("الاسم مطلوب", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeOpeningBalance_ReturnsFailure()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, null, -100m, 0m,
            SupplierType.Cash, null);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("الرصيد الافتتاحي لا يمكن أن يكون سالباً", result.Error);
    }

    // ─── GetByIdAsync Finds ────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenSupplierExists_ReturnsDto()
    {
        var supplier = Supplier.Create("مورد موجود", openingBalance: 1000m);
        typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 5);

        _supplierRepoMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var result = await _service.GetByIdAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Id);
        Assert.Equal("مورد موجود", result.Value.Name);
        Assert.Equal(1000m, result.Value.CurrentBalance);
    }

    // ─── GetByIdAsync Not Found ───────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenSupplierNotFound_ReturnsFailure()
    {
        _supplierRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }

    // ─── Transaction Rollback ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_OnException_RollsBackTransaction()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        _supplierRepoMock.Setup(x => x.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var request = new CreateSupplierRequest(
            "مورد", null, null, null, null, 0m, 0m,
            SupplierType.Cash, null);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── GetAllAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        var suppliers = new List<Supplier>
        {
            Supplier.Create("مورد أ"),
            Supplier.Create("مورد ب"),
            Supplier.Create("مورد ج")
        };
        // Assign IDs
        for (int i = 0; i < suppliers.Count; i++)
            typeof(Supplier).GetProperty("Id")!.SetValue(suppliers[i], i + 1);

        _supplierRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers);

        var result = await _service.GetAllAsync(null, 1, 10, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count());
    }

    // ─── DeleteAsync (Soft) ────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SetsIsActiveFalse()
    {
        var supplier = Supplier.Create("مورد للحذف");
        typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);

        _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(supplier.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_WhenSupplierNotFound_ReturnsFailure()
    {
        _supplierRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var result = await _service.DeleteAsync(999, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }
}
```

**Additional test scenarios**:

```csharp
// ─── Account Auto-Creation (Phase-specific) ────────────────────

[Fact]
public async Task CreateAsync_WithOpeningBalance_CreatesJournalEntry()
{
    var request = new CreateSupplierRequest(
        "مورد برصيد افتتاحي", null, null, null, null,
        openingBalance: 10000m, creditLimit: 0m,
        SupplierType.Credit, accountId: null);

    var parentAccount = Account.Create("حسابات الموردين", "", "2100",
        null, AccountType.Liability, 3, false, null);
    typeof(Account).GetProperty("Id")!.SetValue(parentAccount, 100);

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);
    _accountRepoMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(parentAccount);
    _accountRepoMock.Setup(x => x.GetMaxCodeUnderParentAsync(
            2100, It.IsAny<CancellationToken>()))
        .ReturnsAsync(0);
    _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    _accountRepoMock.Verify(x => x.AddAsync(
        It.Is<Account>(a => a.AccountCode == "2101" &&
            a.NameAr == "مورد برصيد افتتاحي"),
        It.IsAny<CancellationToken>()), Times.Once);
    _uowMock.Verify(x => x.JournalEntries.AddAsync(
        It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task CreateAsync_WithZeroOpeningBalance_DoesNotCreateJournalEntry()
{
    var request = new CreateSupplierRequest(
        "مورد بدون رصيد", null, null, null, null,
        openingBalance: 0m, creditLimit: 0m,
        SupplierType.Cash, accountId: null);

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);
    _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    _uowMock.Verify(x => x.JournalEntries.AddAsync(
        It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
}

[Fact]
public async Task CreateAsync_WhenAccountCreationFails_RollsBack()
{
    var request = new CreateSupplierRequest(
        "مورد", null, null, null, null, openingBalance: 5000m,
        creditLimit: 0m, SupplierType.Cash, accountId: null);

    _accountRepoMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Account?)null); // 2100 not found — failure

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.False(result.IsSuccess);
    mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
}

// ─── Account Code Generation ─────────────────────────────────────
// Tests that supplier sub-accounts generate sequential codes: 2101, 2102...

[Theory]
[InlineData(0, "2101")]
[InlineData(1, "2102")]
[InlineData(9, "2110")]
[InlineData(99, "2199")]
public async Task CreateSupplierAccountAsync_GeneratesCorrectAccountCode(
    int maxExisting, string expectedCode)
{
    // Arrange: seed parent account 2100
    var parentAccount = Account.Create("حسابات الموردين", "", "2100",
        null, AccountType.Liability, 3, false, null);
    typeof(Account).GetProperty("Id")!.SetValue(parentAccount, 100);

    _accountRepoMock.Setup(x => x.FirstOrDefaultAsync(
            It.Is<Expression<Func<Account, bool>>>(e => e.ToString()!.Contains("2100")),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(parentAccount);
    _accountRepoMock.Setup(x => x.GetMaxCodeUnderParentAsync(
            2100, It.IsAny<CancellationToken>()))
        .ReturnsAsync(maxExisting);

    var supplier = Supplier.Create("مورد جديد", accountId: null);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 99);

    var accountResult = await InvokeCreateSupplierAccountAsync(supplier, 1, CancellationToken.None);

    Assert.True(accountResult.IsSuccess);
    Assert.Equal(expectedCode, accountResult.Value.AccountCode);
}

// ─── Balance Report ──────────────────────────────────────────────

[Fact]
public async Task GetBalanceReportAsync_ReturnsAllSuppliersWithCalculatedFields()
{
    var suppliers = new List<Supplier>
    {
        Supplier.Create("مورد أ", openingBalance: 5000m, creditLimit: 10000m),
        Supplier.Create("مورد ب", openingBalance: 0m, creditLimit: 5000m),
    };
    typeof(Supplier).GetProperty("Id")!.SetValue(suppliers[0], 1);
    typeof(Supplier).GetProperty("Id")!.SetValue(suppliers[1], 2);

    _supplierRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(suppliers);

    var result = await _service.GetBalanceReportAsync(CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.Value.Count);
    Assert.Equal(50m, result.Value[0].CreditLimitUsagePercent); // 5000/10000 * 100
    Assert.Equal(0m, result.Value[1].CreditLimitUsagePercent);   // 0/5000 * 100
}

// ─── Credit Limit Exceeded ───────────────────────────────────────

[Fact]
public async Task IsCreditLimitExceededAsync_WhenBelowLimit_ReturnsFalse()
{
    var supplier = Supplier.Create("مورد", openingBalance: 3000m, creditLimit: 10000m,
        supplierType: SupplierType.Credit);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);
    _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(supplier);

    var exceeded = await _service.IsCreditLimitExceededAsync(1, 2000m, CancellationToken.None);

    Assert.False(exceeded); // 3000 + 2000 = 5000 ≤ 10000
}

[Fact]
public async Task IsCreditLimitExceededAsync_WhenAboveLimit_ReturnsTrue()
{
    var supplier = Supplier.Create("مورد", openingBalance: 8000m, creditLimit: 10000m,
        supplierType: SupplierType.Credit);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);
    _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(supplier);

    var exceeded = await _service.IsCreditLimitExceededAsync(1, 5000m, CancellationToken.None);

    Assert.True(exceeded); // 8000 + 5000 = 13000 > 10000
}

[Fact]
public async Task IsCreditLimitExceededAsync_ForCashSupplier_ReturnsFalse()
{
    var supplier = Supplier.Create("مورد نقدي", creditLimit: 0m,
        supplierType: SupplierType.Cash);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);
    _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(supplier);

    var exceeded = await _service.IsCreditLimitExceededAsync(1, 999999m, CancellationToken.None);

    Assert.False(exceeded); // Cash suppliers skip credit check
}

// ─── Transaction History ─────────────────────────────────────────

[Fact]
public async Task GetTransactionHistoryAsync_WhenSupplierNotFound_ReturnsFailure()
{
    _supplierRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Supplier?)null);

    var result = await _service.GetTransactionHistoryAsync(
        999, null, null, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
}
```

**Estimate**: ~3 hours

---

#### 16.3 — FluentValidation Tests

```csharp
public class CreateSupplierRequestValidatorTests
{
    private readonly CreateSupplierRequestValidator _validator;

    public CreateSupplierRequestValidatorTests()
    {
        _validator = new CreateSupplierRequestValidator();
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد صالح", "0555123456", "test@test.com", "الرياض",
            "123456789012345", 5000m, 10000m,
            SupplierType.Credit, accountId: 1);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            "", null, null, null, null, 0m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void NameExceedsMaxLength_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            new string('ا', 151), null, null, null, null, 0m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void InvalidTaxNumberFormat_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, "12345", 0m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public void ValidTaxNumber_PassesValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, "123456789012345", 0m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NullTaxNumber_PassesValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, null, 0m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NegativeOpeningBalance_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, null, -100m, 0m,
            SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OpeningBalance");
    }

    [Fact]
    public void InvalidSupplierType_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد", null, null, null, null, 0m, 0m,
            (SupplierType)99, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SupplierType");
    }

    [Fact]
    public void CreditSupplierWithoutCreditLimit_FailsValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد آجل", null, null, null, null, 0m,
            creditLimit: 0m, SupplierType.Credit, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CreditLimit");
    }

    [Fact]
    public void CashSupplierWithoutCreditLimit_PassesValidation()
    {
        var request = new CreateSupplierRequest(
            "مورد نقدي", null, null, null, null, 0m,
            creditLimit: 0m, SupplierType.Cash, null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
```

**Estimate**: ~1.5 hours

---

#### 16.4 — Database Configuration Tests

```csharp
public class SupplierConfigurationTests
{
    [Fact]
    public void SupplierConfiguration_HasCorrectTableName()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Supplier));
        Assert.Equal("Suppliers", entity!.GetTableName());
    }

    [Fact]
    public void Name_HasRequiredMaxLength150()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.Name));
        Assert.False(prop!.IsNullable);
        Assert.Equal(150, prop.GetMaxLength());
    }

    [Fact]
    public void Phone_HasMaxLength20()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.Phone));
        Assert.Equal(20, prop!.GetMaxLength());
    }

    [Fact]
    public void Email_HasMaxLength100()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.Email));
        Assert.Equal(100, prop!.GetMaxLength());
    }

    [Fact]
    public void TaxNumber_HasMaxLength30()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.TaxNumber));
        Assert.Equal(30, prop!.GetMaxLength());
    }

    [Fact]
    public void MoneyFields_HavePrecision18_2()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Supplier))!;
        foreach (var propName in new[] {
            nameof(Supplier.OpeningBalance),
            nameof(Supplier.CurrentBalance),
            nameof(Supplier.CreditLimit) })
        {
            var prop = entity.FindProperty(propName);
            Assert.NotNull(prop);
            Assert.Equal(18, prop!.GetPrecision());
            Assert.Equal(2, prop.GetScale());
        }
    }

    [Fact]
    public void AccountIdFK_UsesDeleteBehaviorRestrict()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Supplier))!;
        var fk = entity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Account));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk!.DeleteBehavior);
    }

    [Fact]
    public void AccountId_IsNullable()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.AccountId));
        Assert.True(prop!.IsNullable);
    }

    [Fact]
    public void SupplierType_HasDefaultValueZero()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Supplier))!
            .FindProperty(nameof(Supplier.SupplierType));
        Assert.NotNull(prop);
        // Default value should be 0 (SupplierType.Cash)
    }

    [Fact]
    public void Supplier_HasQueryFilterForIsActive()
    {
        var modelBuilder = new ModelBuilder();
        new SupplierConfiguration().Configure(modelBuilder.Entity<Supplier>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Supplier))!;
        var queryFilter = entity.GetQueryFilter();
        Assert.NotNull(queryFilter);
    }
}
```

**Estimate**: ~1.5 hours

---

#### 16.5 — Phase-Specific Integration Tests

```csharp
// ─── Supplier.Create() Auto-Creates Sub-Account Under 2100 ──────

[Fact]
public async Task CreateAsync_WithCreditSupplier_AutoCreatesSubAccountUnder2100()
{
    var request = new CreateSupplierRequest(
        "مورد آجل جديد", null, null, null, null,
        openingBalance: 0m, creditLimit: 50000m,
        SupplierType.Credit, accountId: null);

    var parentAccount2100 = Account.Create("حسابات الموردين", "", "2100",
        null, AccountType.Liability, 3, false, null);
    typeof(Account).GetProperty("Id")!.SetValue(parentAccount2100, 100);

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);
    _accountRepoMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(parentAccount2100);
    _accountRepoMock.Setup(x => x.GetMaxCodeUnderParentAsync(
            2100, It.IsAny<CancellationToken>()))
        .ReturnsAsync(0); // First sub-account → code 2101
    _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    // Verify account was created with correct parent and code
    _accountRepoMock.Verify(x => x.AddAsync(
        It.Is<Account>(a =>
            a.AccountCode == "2101" &&
            a.NameAr == "مورد آجل جديد" &&
            a.ParentId == 100 &&
            a.AccountType == AccountType.Liability),
        It.IsAny<CancellationToken>()), Times.Once);
    // Verify supplier was linked to the new account
    Assert.NotNull(result.Value.AccountId);
}

// ─── OpeningBalance > 0 Creates Auto-Journal Entry ──────────────

[Fact]
public async Task CreateAsync_WithOpeningBalance5000_CreatesJournalEntryDebitInventoryCreditSupplier()
{
    var request = new CreateSupplierRequest(
        "مورد برصيد", null, null, null, null,
        openingBalance: 5000m, creditLimit: 0m,
        SupplierType.Cash, accountId: null);

    var parentAccount2100 = Account.Create("حسابات الموردين", "", "2100",
        null, AccountType.Liability, 3, false, null);
    typeof(Account).GetProperty("Id")!.SetValue(parentAccount2100, 100);

    var inventoryAccount1500 = Account.Create("المخزون", "", "1500",
        null, AccountType.Asset, 2, true, null);
    typeof(Account).GetProperty("Id")!.SetValue(inventoryAccount1500, 50);

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);
    _accountRepoMock.SetupSequence(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(parentAccount2100)   // First call: find 2100
        .ReturnsAsync(inventoryAccount1500); // Second call: find 1500
    _accountRepoMock.Setup(x => x.GetMaxCodeUnderParentAsync(
            2100, It.IsAny<CancellationToken>()))
        .ReturnsAsync(0);
    _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    // Verify journal entry: debit Inventory(1500) 5000, credit supplier account 5000
    _uowMock.Verify(x => x.JournalEntries.AddAsync(
        It.Is<JournalEntry>(je =>
            je.Lines.Any(l => l.AccountId == 50 && l.DebitAmount == 5000m) &&
            je.Lines.Any(l => l.AccountId == 100 && l.CreditAmount == 5000m) &&
            je.EntryType == JournalEntryType.OpeningBalance),
        It.IsAny<CancellationToken>()), Times.Once);
}

// ─── Transactional: Supplier + Account + Journal Entry ───────────

[Fact]
public async Task CreateAsync_FullTransaction_CommitsOnceOnSuccess()
{
    var request = new CreateSupplierRequest(
        "مورد متكامل", null, null, null, null,
        openingBalance: 5000m, creditLimit: 10000m,
        SupplierType.Credit, accountId: null);

    var parent2100 = Account.Create("حسابات الموردين", "", "2100",
        null, AccountType.Liability, 3, false, null);
    typeof(Account).GetProperty("Id")!.SetValue(parent2100, 100);
    var inventory1500 = Account.Create("المخزون", "", "1500",
        null, AccountType.Asset, 2, true, null);
    typeof(Account).GetProperty("Id")!.SetValue(inventory1500, 50);

    var mockTransaction = new Mock<IDbContextTransaction>();
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockTransaction.Object);
    _accountRepoMock.SetupSequence(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(parent2100)
        .ReturnsAsync(inventory1500);
    _accountRepoMock.Setup(x => x.GetMaxCodeUnderParentAsync(
            2100, It.IsAny<CancellationToken>()))
        .ReturnsAsync(0);
    _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    // Transaction committed exactly once
    mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    // Supplier added, account added, journal entry added
    _supplierRepoMock.Verify(x => x.AddAsync(
        It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Once);
    _accountRepoMock.Verify(x => x.AddAsync(
        It.Is<Account>(a => a.AccountCode == "2101"),
        It.IsAny<CancellationToken>()), Times.Once);
    _uowMock.Verify(x => x.JournalEntries.AddAsync(
        It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    // SaveChanges called at least twice (supplier before stock, then journal entry)
    _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.AtLeast(2));
}

// ─── AccountId FK Links Supplier to CoA Correctly ───────────────

[Fact]
public async Task GetByIdAsync_EagerLoadsAccountNavigationProperty()
{
    var supplier = Supplier.Create("مورد بحساب", openingBalance: 1000m, accountId: 42);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);

    _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(supplier);

    var result = await _service.GetByIdAsync(1, CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value.AccountId);
    // AccountName populated from supplier.Account?.NameAr
    // (requires Include in repository query)
}
```

**Estimate**: ~2 hours

---

#### 16.6 — Update Test

```csharp
[Fact]
public async Task UpdateAsync_WithValidRequest_UpdatesAndReturnsDto()
{
    var supplier = Supplier.Create("مورد قديم", openingBalance: 1000m, creditLimit: 5000m,
        supplierType: SupplierType.Cash);
    typeof(Supplier).GetProperty("Id")!.SetValue(supplier, 1);

    _supplierRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(supplier);

    var request = new UpdateSupplierRequest(
        "مورد محدث", "0555999999", "updated@test.com", "جدة",
        "987654321098765", 20000m, true,
        SupplierType.Credit, accountId: 55);

    var result = await _service.UpdateAsync(1, request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("مورد محدث", result.Value.Name);
    Assert.Equal("0555999999", result.Value.Phone);
    Assert.Equal(SupplierType.Credit, result.Value.SupplierType);
    Assert.Equal(55, result.Value.AccountId);
    // OpeningBalance and CurrentBalance should NOT change via Update
    Assert.Equal(1000m, result.Value.OpeningBalance);
    Assert.Equal(1000m, result.Value.CurrentBalance);
}

[Fact]
public async Task UpdateAsync_WhenSupplierNotFound_ReturnsFailure()
{
    _supplierRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Supplier?)null);

    var request = new UpdateSupplierRequest(
        "مورد", null, null, null, null, 0m, true,
        SupplierType.Cash, null);

    var result = await _service.UpdateAsync(999, request, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
}
```

**Estimate**: ~1 hour

| Test Sub-Task | Focus | Files | Estimate |
|----------------|-------|-------|----------|
| 16.1 Domain Entity | `Supplier.Create` factory + guards + balance methods | `SupplierTests.cs` | 2 hours |
| 16.2 Service Layer | `CreateAsync`, `GetByIdAsync`, transactions, rollback | `SupplierServiceTests.cs` | 3 hours |
| 16.3 FluentValidation | Name, TaxNumber, OpeningBalance, SupplierType rules | `SupplierRequestValidatorTests.cs` | 1.5 hours |
| 16.4 DB Config | Precision, MaxLength, Restrict FK, nullable AccountId | `SupplierConfigurationTests.cs` | 1.5 hours |
| 16.5 Phase-Specific | Account auto-creation, journal entry, transactional flow | `SupplierServiceTests.cs` | 2 hours |
| 16.6 Update Tests | UpdateAsync valid/invalid scenarios | `SupplierServiceTests.cs` | 1 hour |
| **Total** | **6 sub-tasks** | **4 test files** | **~11 hours** |
