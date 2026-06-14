# Phase 23 — Customers Module: Comprehensive Implementation Plan

> **Version**: 1.0 — Based on 5 analysis files + full codebase audit (Domain, API, Desktop, Infrastructure)
> **Scope**: Complete Customer CRUD enhancement with Account linking, CustomerType, CustomerGroup, TaxNumber, CreditLimit enforcement, enhanced validation, customer reports, and UI improvements

---

## Table of Contents

1. [Customer Module Architecture — 2 Sub-Modules](#1-customer-module-architecture--2-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Issues](#3-blocker-resolution--critical-issues)
4. [Customer Design Catalog](#4-customer-design-catalog)
5. [Gap Analysis — Existing vs Target](#5-gap-analysis--existing-vs-target)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Customer Module Architecture — 2 Sub-Modules

The Customers Module is divided into **2 main sub-modules**:

| # | Sub-Module | Purpose | Existing Status |
|---|------------|---------|-----------------|
| 🧑‍🤝‍🧑 | **Customer CRUD** | Create, Read, Update, Soft-Delete, Permanent-Delete with DeleteStrategy | ✅ Core CRUD exists — needs enhancement |
| 🔗 | **Customer-Account Link** | Link each Customer to an Account in Chart of Accounts + Customer Type (Cash/Credit) + Customer Group | ❌ Missing entirely — needs full build |

### 1.1 CRUD Flow (Enhanced)

```text
Desktop CustomerEditorView
    → CustomerEditorViewModel (INotifyDataErrorInfo, ExecuteAsync wrapper)
        → CustomerApiService (HttpClient)
            → CustomersController ([Authorize], FluentValidation)
                → CustomerService (Result<T>, IUnitOfWork)
                    → Customer Entity (Domain Guard Clauses)
                        → CustomerConfiguration (Fluent API, Restrict FKs)
```

### 1.2 Customer-Account Link Flow

```text
Customer.Entity
    ├── AccountId (int? FK → Account) — Links to Chart of Accounts
    ├── CustomerType (enum: Cash=1, Credit=2) — Payment classification
    ├── CustomerGroupId (int? FK → CustomerGroup) — Grouping for reporting
    └── TaxNumber (string?) — Existing field, already present

Customer.Entity.AccountId
    ↓ FK (Restrict)
Account.Entity (Chart of Accounts — Phase 22)
    — Enables: balance aging reports, transaction history by account
    — Enables: automatic journal entries when creating customer payments
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Entity ✅ (Core exists — needs enhancement)

**File**: `SalesSystem.Domain.Entities.Customer`

| Field | Type | Required | Status |
|-------|------|----------|--------|
| `Id` | `int PK` | ✅ | ✅ Exists — auto-increment |
| `Name` | `string(150)` | ✅ | ✅ Exists |
| `Phone` | `string(20)?` | ❌ | ✅ Exists |
| `Email` | `string(100)?` | ❌ | ✅ Exists |
| `Address` | `string(250)?` | ❌ | ✅ Exists |
| `OpeningBalance` | `decimal(18,2)` | ❌ | ✅ Exists |
| `CurrentBalance` | `decimal(18,2)` | ❌ | ✅ Exists (computed from transactions) |
| `CreditLimit` | `decimal(18,2)` | ❌ | ✅ Exists |
| `TaxNumber` | `string(30)?` | ❌ | ✅ Exists |
| `IsActive` | `bool` | ✅ | ✅ Exists (BaseEntity) |
| `CreatedAt` | `datetime2` | ✅ | ✅ Exists (BaseEntity) |
| `AccountId` | `int? FK` | ❌ | ⬜ **NEW — add FK to Accounts table** |
| `CustomerType` | `byte?` | ❌ | ⬜ **NEW — Cash/Credit enum** |
| `CustomerGroupId` | `int? FK` | ❌ | ⬜ **NEW — FK to CustomerGroup table** |

**Domain methods** (all exist):
- `Customer.Create(name, openingBalance, phone, email, address, taxNumber, creditLimit, createdByUserId)` ✅
- `Customer.Update(name, phone, email, address, taxNumber, creditLimit, updatedByUserId)` ✅
- `IncreaseBalance(amount)` ✅
- `DecreaseBalance(amount)` ✅

**Missing domain methods:**
- `SetCustomerType(CustomerType type)` — ⬜ NEW
- `LinkToAccount(int accountId)` — ⬜ NEW
- `SetCustomerGroup(int? customerGroupId)` — ⬜ NEW
- `CheckCreditLimit(decimal additionalAmount)` — ⬜ NEW (returns bool, not DomainException)

### 2.2 Infrastructure Layer ⚠️ (Partially exists)

**File**: `Infrastructure/Data/Configurations/CustomerConfiguration.cs`

| Setting | Status |
|---------|--------|
| `ToTable("Customers")` | ✅ Exists |
| `HasKey(c => c.Id)` | ✅ Exists |
| `Name — HasMaxLength(150)` | ✅ Exists |
| `Phone — HasMaxLength(20)` | ✅ Exists |
| `Email — HasMaxLength(100)` | ✅ Exists |
| `Address — HasMaxLength(250)` | ✅ Exists |
| `OpeningBalance — HasPrecision(18, 2)` | ✅ Exists |
| `CurrentBalance — HasPrecision(18, 2)` | ✅ Exists |
| `CreditLimit — HasPrecision(18, 2)` | ✅ Exists |
| `TaxNumber — HasMaxLength(30)` | ✅ Exists |
| `HasQueryFilter(c => c.IsActive)` | ✅ Exists |

**Missing configuration:**
- `AccountId` FK configuration with `DeleteBehavior.Restrict` — ⬜ NEW
- `CustomerType` property config — ⬜ NEW
- `CustomerGroupId` FK configuration with `DeleteBehavior.Restrict` — ⬜ NEW
- `CustomerGroupConfiguration` — ⬜ NEW (entirely new entity)
- Index on `AccountId` for faster joins — ⬜ NEW
- Index on `CustomerGroupId` for faster filtering — ⬜ NEW

### 2.3 DbSeeder ⚠️ (Needs update)

**File**: `Infrastructure/Data/DbSeeder.cs` — Lines 120-126

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and see DbSeeder.cs for seed data patterns.

Also needs to seed:
- Default CustomerGroup (e.g., "عام") — ⬜ NEW
- AccountId link for the default customer — ⬜ NEW

### 2.4 Application Layer ✅ (Core exists)

**File**: `Application/Interfaces/Services/ICustomerService.cs`

| Method | Return Type | Status |
|--------|-------------|--------|
| `GetByIdAsync(id, ct)` | `Result<CustomerDto>` | ✅ Exists |
| `GetAllAsync(search, page, pageSize, includeInactive, ct)` | `Result<PagedResult<CustomerDto>>` | ✅ Exists |
| `CreateAsync(request, ct)` | `Result<CustomerDto>` | ✅ Exists |
| `UpdateAsync(id, request, ct)` | `Result<CustomerDto>` | ✅ Exists |
| `DeleteAsync(id, ct)` | `Result` | ✅ Exists (soft delete) |
| `PermanentDeleteAsync(id, ct)` | `Result` | ✅ Exists (with DbUpdateException catch) |

**Missing methods:**
- `GetByGroupAsync(int customerGroupId, ct)` — ⬜ NEW
- `GetCreditLimitSummaryAsync(int id, ct)` — ⬜ NEW
- `CheckCreditLimitAsync(int customerId, decimal amount, ct)` — ⬜ NEW (called from SalesService)
- `GetCustomerBalanceReportAsync(int id, DateTime from, DateTime to, ct)` — ⬜ NEW
- `GetCustomerAgingReportAsync(ct)` — ⬜ NEW

**File**: `Application/Services/CustomerService.cs`

Observations:
- ✅ Uses `IUnitOfWork` (RULE-024)
- ✅ Returns `Result<T>` (RULE-006)
- ✅ Has proper logging (RULE-035)
- ✅ `PermanentDeleteAsync` catches FK exceptions (RULE-200)
- ❌ `GetAllAsync` sorts by Name ascending — should sort by Id descending (RULE-220) — needs fix
- ❌ Missing `try-catch` in `LoadCustomersAsync` uses manual try/finally instead of `ExecuteAsync()` wrapper (RULE-141)

### 2.5 Contracts Layer ⚠️ (Needs enhancement)

**File**: `Contracts/DTOs/AllDtos.cs` — Line 51
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Missing fields in CustomerDto:**
- `int? AccountId` — ⬜ NEW
- `string? AccountName` — ⬜ NEW
- `byte? CustomerType` — ⬜ NEW
- `int? CustomerGroupId` — ⬜ NEW
- `string? CustomerGroupName` — ⬜ NEW

**File**: `Contracts/Requests/CustomerRequests.cs`
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Missing fields in requests:**
- `int? AccountId` — ⬜ NEW
- `byte? CustomerType` — ⬜ NEW
- `int? CustomerGroupId` — ⬜ NEW

**File**: `Contracts/Responses/CustomerResponse.cs`
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Missing:**
- `string? TaxNumber` — ⬜ NEW (needed for invoice printing)
- `decimal OpeningBalance` — ⬜ NEW
- `int? AccountId` — ⬜ NEW
- `string? AccountName` — ⬜ NEW
- `byte? CustomerType` — ⬜ NEW
- `string? CustomerGroupName` — ⬜ NEW

### 2.6 API Layer ✅ (Core exists — needs enhancement)

**File**: `Api/Controllers/CustomersController.cs`

| Method | Endpoint | Policy | Status |
|--------|----------|--------|--------|
| GET | `/api/v1/customers` | `AllStaff` | ✅ Exists |
| GET | `/api/v1/customers/{id}` | `AllStaff` | ✅ Exists |
| POST | `/api/v1/customers` | `ManagerAndAbove` | ✅ Exists |
| PUT | `/api/v1/customers/{id}` | `ManagerAndAbove` | ✅ Exists |
| DELETE | `/api/v1/customers/{id}` | `ManagerAndAbove` | ✅ Exists (soft) |
| DELETE | `/api/v1/customers/permanent/{id}` | `ManagerAndAbove` | ✅ Exists (permanent) |

**Missing endpoints:**
- `GET /api/v1/customers/groups` — List customer groups — ⬜ NEW
- `GET /api/v1/customers/{id}/balance-report` — Balance report — ⬜ NEW
- `GET /api/v1/customers/aging` — Aging report — ⬜ NEW
- `POST /api/v1/customers/groups` — Create group — ⬜ NEW
- `PUT /api/v1/customers/groups/{id}` — Update group — ⬜ NEW
- `DELETE /api/v1/customers/groups/{id}` — Delete group — ⬜ NEW

**File**: `Api/Validators/CustomerRequestValidators.cs`

| Validator | Status |
|-----------|--------|
| `CreateCustomerRequestValidator` | ✅ Exists — Name required, Phone/Email length, OpeningBalance ≥ 0, CreditLimit ≥ 0 |
| `UpdateCustomerRequestValidator` | ✅ Exists — Same rules |

**Missing validation:**
- Phone regex pattern validation (e.g., Saudi/Yemeni mobile format) — ⬜ NEW
- Email format enhancement — ⬜ NEW
- New fields (AccountId, CustomerType, CustomerGroupId) — ⬜ NEW

### 2.7 Desktop Layer ⚠️ (Core exists — needs enhancement)

**Customer List**:

| File | Status |
|------|--------|
| `ViewModels/Customers/CustomerListViewModel.cs` | ✅ Exists — 371 lines |
| `Views/Customers/CustomersListView.xaml` | ✅ Exists — 270 lines |
| `Views/Customers/CustomersListView.xaml.cs` | ✅ Exists (code-behind) |

**Issues in CustomerListViewModel:**
- ❌ `LoadCustomersAsync()` uses manual `try/catch/finally` — should use `ExecuteAsync()` wrapper (RULE-141)
- ❌ `DeleteCustomerAsync()` uses manual `try/catch/finally` — should use `ExecuteAsync()` wrapper (RULE-141)
- ❌ `RestoreCustomerAsync()` uses manual `try/catch/finally` — should use `ExecuteAsync()` wrapper (RULE-141)
- ❌ `EditCommand` has `CanExecute` predicate `() => SelectedCustomer != null` — should always be enabled (RULE-059)
- ❌ `DeleteCommand` has `CanExecute` predicate — should always be enabled (RULE-059)
- ❌ `RestoreCommand` has `CanExecute` predicate — should always be enabled (RULE-059)
- ✅ Sort by Id descending (RULE-220)
- ✅ EventBus subscription + cleanup
- ✅ DeleteStrategy dialog with 3 options

**Customer Editor**:

| File | Status |
|------|--------|
| `ViewModels/Customers/CustomerEditorViewModel.cs` | ✅ Exists — 244 lines |
| `Views/Customers/CustomerEditorView.xaml` | ✅ Exists — 185 lines |
| `Views/Customers/CustomerEditorView.xaml.cs` | ✅ Exists (code-behind) |

**Issues in CustomerEditorViewModel:**
- ❌ Missing `AccountId`, `CustomerType`, `CustomerGroupId` properties — ⬜ NEW
- ❌ `SaveCommand` uses `ExecuteAsync` correctly ✅
- ❌ Missing `CustomerGroup` related properties for dropdown — ⬜ NEW
- ✅ Uses `SetDialogService()` ✅
- ✅ Uses `INotifyDataErrorInfo` (RULE-228)
- ✅ ValidateAsync calls ClearAllErrors + AddError + ValidateAllAsync (RULE-229)

**Customer Selection**:

| File | Status |
|------|--------|
| `ViewModels/Customers/CustomerSelectionViewModel.cs` | ✅ Exists — 133 lines |
| `Views/Customers/CustomerSelectionView.xaml` | ✅ Exists |
| `Views/Customers/CustomerSelectionView.xaml.cs` | ✅ Exists (code-behind) |

**Issues in CustomerSelectionViewModel:**
- ❌ `LoadCustomersAsync()` uses manual `try/catch/finally` — should use `ExecuteAsync()` wrapper (RULE-141)
- ❌ `SelectCommand` has `CanExecute` predicate — should always be enabled (RULE-059)
- ❌ Empty `catch` block — should log via `LogSystemError()` (RULE-199)

**Customer API Service**:

| File | Status |
|------|--------|
| `Services/Api/CustomerApiService.cs` | ✅ Exists — 60 lines |
| `Services/Api/IApiService.cs` (ICustomerApiService) | ✅ Exists |

**Missing API methods:**
- `GetGroupsAsync()` — ⬜ NEW
- `CreateGroupAsync(request)` — ⬜ NEW
- `UpdateGroupAsync(id, request)` — ⬜ NEW
- `DeleteGroupAsync(id)` — ⬜ NEW
- `GetBalanceReportAsync(id, from, to)` — ⬜ NEW
- `GetAgingReportAsync()` — ⬜ NEW

**Customer XAML Views — UI Compact Compliance (RULE-262-274):**

| Rule | CustomersListView | CustomerEditorView |
|------|-------------------|-------------------|
| No hardcoded Height="36" on buttons | ✅ All use styles | ✅ All use styles |
| No hardcoded Padding="16+" on buttons | ✅ All use styles | ✅ All use styles |
| Header padding 12,6 max | ✅ Border Padding="12,6" | ✅ Border Padding="12,6" |
| Footer padding 12,8 max | ✅ FooterBarStyle handles | ✅ Border Padding="12,8" |
| Section margins ≤ 8px | ✅ StandardSectionMargin | ✅ Uses Border Height="12" (excessive — should be 8) |
| Dialog title FontSize ≤ 16 | N/A (list view) | ❌ Line 28: FontSize="20" — **MUST FIX** |
| Empty-state button Width=140 | ✅ Width="140" | N/A |
| Dialog icons 44×44 max | N/A | ❌ Line 26-27: IconUser 24×24 ✅ (but icon is Path not emoji) |

---

## 3. BLOCKER Resolution — Critical Issues

### 3.1 Blocker 1: Default Customer Name — Must Rename to "عميل نقدي"

**Problem**: The seed default customer is named "العميل الافتراضي في النظام" which is not a user-friendly name. The analysis explicitly states the name should be "عميل نقدي" (Cash Customer) — this is the standard name used in comparable systems (محاسب سوفت) and is what the codebase already references in 6+ places as a fallback string.

**Evidence from codebase** (all reference "عميل نقدي" as the default display name):
- `SalesService.cs` line 470: `i.Customer?.Name ?? "عميل نقدي"`
- `PrintDtoExtensions.cs` line 29: `invoice.CustomerName ?? "عميل نقدي"`
- `ReportRepository.cs` line 33: `i.Customer.Name ?? "عميل نقدي"`
- `SalesInvoiceEditorViewModel.cs` line 589: filter includes `"عميل نقدي"`
- `DashboardViewModel.cs` line 249: `inv.CustomerName ?? "عميل نقدي"`

> See `Infrastructure/Data/DbSeeder.cs` for seed data patterns.

**Additionally**, the default supplier must also be renamed from "المورد الافتراضي في النظام" to "مورد نقدي" (though this is the Supplier module scope — include here for cross-reference).

**Impact**: Low — name change only, no data migration needed if the DB hasn't been deployed to production. If production data exists, an UPDATE script is needed.

**Files changed**: `DbSeeder.cs` only.

### 3.2 Blocker 2: AccountId FK — New Migration Required

**Problem**: The Customer entity needs to link to the Chart of Accounts (Account entity from Phase 22). Currently there is no `AccountId` FK. Adding it requires:
1. Add `int? AccountId` to Customer entity
2. Add FK configuration with `DeleteBehavior.Restrict`
3. Create a new migration
4. Update seed data to link the default customer to the correct account

**Decision**: Make `AccountId` nullable (optional). If no account is linked, the customer still works functionally but won't appear in account-based reports.

> See `docs/database-schema.md` for the canonical table definitions and migration patterns.

### 3.3 Blocker 3: CustomerType & CustomerGroup — New Entities

**Problem**: The current Customer entity has no `CustomerType` or `CustomerGroupId`. The analysis explicitly mentions:
- "نوع العميل: نقدي / آجل" (Customer Type: Cash / Credit)
- "مجموعة العملاء" (Customer Group) — for categorizing customers (e.g., Wholesale, Retail, VIP)

> See `Domain/Enums/` for enum definitions and `docs/AGENTS.md` §3 for canonical enum values.

**CustomerGroup Entity** (new standalone entity):

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `Id` | `int PK` | ✅ | Auto-increment |
| `Name` | `nvarchar(100)` | ✅ | **UNIQUE Index** — e.g., "جملة", "قطاعي", "VIP" |
| `Description` | `nvarchar(250)?` | ❌ | Optional description |
| `IsActive` | `bit` | ✅ | Global query filter |

**Seed data for CustomerGroup**:
| Name | Description |
|------|-------------|
| عام | المجموعة الافتراضية — لجميع العملاء غير المصنفين |
| جملة | عملاء الجملة |
| قطاعي | عملاء القطاعي |
| VIP | عملاء مميزون |

---

## 4. Customer Design Catalog

### 4.1 Customer Entity (Enhanced)

| # | Field | Type | Required | Default | Constraints | Status |
|---|-------|------|----------|---------|-------------|--------|
| 1 | `Id` | `int PK` | ✅ | Auto | — | ✅ Exists |
| 2 | `Name` | `nvarchar(150)` | ✅ | — | **UNIQUE Index** (optional — discuss) | ✅ Exists |
| 3 | `Phone` | `nvarchar(20)?` | ❌ | `null` | — | ✅ Exists |
| 4 | `Email` | `nvarchar(100)?` | ❌ | `null` | — | ✅ Exists |
| 5 | `Address` | `nvarchar(250)?` | ❌ | `null` | — | ✅ Exists |
| 6 | `OpeningBalance` | `decimal(18,2)` | ❌ | `0` | `CHECK >= 0` | ✅ Exists |
| 7 | `CurrentBalance` | `decimal(18,2)` | ❌ | `0` | — (computed) | ✅ Exists |
| 8 | `CreditLimit` | `decimal(18,2)` | ❌ | `0` | `CHECK >= 0` | ✅ Exists |
| 9 | `TaxNumber` | `nvarchar(30)?` | ❌ | `null` | **UNIQUE Index** (optional) | ✅ Exists |
| 10 | `AccountId` | `int? FK` | ❌ | `null` | FK → Accounts, Restrict | ⬜ **NEW** |
| 11 | `CustomerType` | `byte?` | ❌ | `null` | 1=Cash, 2=Credit | ⬜ **NEW** |
| 12 | `CustomerGroupId` | `int? FK` | ❌ | `null` | FK → CustomerGroups, Restrict | ⬜ **NEW** |
| 13 | `IsActive` | `bit` | ✅ | `true` | Global query filter | ✅ Exists (BaseEntity) |
| 14 | `CreatedAt` | `datetime2` | ✅ | `GETUTCDATE()` | — | ✅ Exists (BaseEntity) |
| 15 | `UpdatedAt` | `datetime2?` | ❌ | `null` | — | ✅ Exists (BaseEntity) |
| 16 | `CreatedByUserId` | `int? FK` | ❌ | `null` | FK → Users, Restrict | ✅ Exists (BaseEntity) |
| 17 | `UpdatedByUserId` | `int? FK` | ❌ | `null` | FK → Users, Restrict | ✅ Exists (BaseEntity) |

### 4.2 CustomerGroup Entity (New)

| # | Field | Type | Required | Default | Constraints |
|---|-------|------|----------|---------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto | — |
| 2 | `Name` | `nvarchar(100)` | ✅ | — | **UNIQUE Index** |
| 3 | `Description` | `nvarchar(250)?` | ❌ | `null` | — |
| 4 | `IsActive` | `bit` | ✅ | `true` | Global query filter |

### 4.3 CustomerType Enum (New)

> See `Domain/Enums/` for enum definitions and `docs/AGENTS.md` §3 for canonical enum values.

### 4.4 CustomerBalanceReportDto (New — for reports)

> See `SalesSystem.Contracts/` for canonical DTO definitions.

### 4.5 CustomerAgingReportDto (New — for aging reports)

> See `SalesSystem.Contracts/` for canonical DTO definitions.

---

## 5. Gap Analysis — Existing vs Target

### 5.1 Domain Entities

| Component | Status | Action |
|-----------|--------|--------|
| Customer entity with Name, Phone, Email, Address | ✅ Exists | Nothing |
| OpeningBalance, CurrentBalance, CreditLimit, TaxNumber | ✅ Exists | Nothing |
| AccountId FK (link to Chart of Accounts) | ❌ Missing | Add FK + config + migration |
| CustomerType (Cash/Credit enum) | ❌ Missing | Add enum + property + migration |
| CustomerGroupId FK + CustomerGroup entity | ❌ Missing | Create from scratch |
| Customer.CheckCreditLimit() domain method | ❌ Missing | Add domain method |
| CreditLimit validation in SalesService | ❌ Missing | Add pre-transaction check |

### 5.2 Infrastructure

| Component | Status | Action |
|-----------|--------|--------|
| CustomerConfiguration (Fluent API) | ✅ Exists | Add new FK configurations |
| CustomerGroupConfiguration | ❌ Missing | Create from scratch |
| DbSeeder — default customer name | ❌ Wrong | Rename to "عميل نقدي" |
| DbSeeder — CustomerGroup seed data | ❌ Missing | Add 4 groups |
| Migration — new columns | ❌ Missing | Create migration |

### 5.3 Application Services

| Component | Status | Action |
|-----------|--------|--------|
| ICustomerService — 6 CRUD methods | ✅ Exists | Add new methods |
| CustomerService — CRUD implementation | ✅ Exists | Add new methods + mapping |
| Customer group service (ICustomerGroupService) | ❌ Missing | Create from scratch |
| Credit limit validation in CRM | ❌ Missing | Add CheckCreditLimitAsync |
| Customer reports (balance, aging) | ❌ Missing | Add report services |
| CustomerService — fix GetAllAsync sort order | ❌ Sorted A-Z | Change to Id desc (RULE-220) |

### 5.4 API Layer

| Component | Status | Action |
|-----------|--------|--------|
| CustomersController — 6 endpoints | ✅ Exists | Add new endpoints |
| CustomerGroupsController | ❌ Missing | Create |
| CustomerReportsController | ❌ Missing | Create (or add to CustomersController) |
| FluentValidators — current fields | ✅ Exists | Add new field validation |
| FluentValidators — CustomerGroup | ❌ Missing | Create |

### 5.5 Desktop Layer

| Component | Status | Action |
|-----------|--------|--------|
| CustomerListViewModel (371 lines) | ✅ Exists | Fix async patterns + add group filter |
| CustomerEditorViewModel (244 lines) | ✅ Exists | Add AccountId, CustomerType, CustomerGroupId |
| CustomerSelectionViewModel (133 lines) | ✅ Exists | Fix async patterns + empty catch |
| CustomersListView.xaml (270 lines) | ✅ Exists | Add group filter combo + balance indicator |
| CustomerEditorView.xaml (185 lines) | ✅ Exists | Add new fields + fix FontSize=20 |
| CustomerGroup list/editor VMs | ❌ Missing | Create |
| CustomerGroup XAML views | ❌ Missing | Create |
| CustomerApiService (60 lines) | ✅ Exists | Add group + report methods |
| ICustomerApiService (in IApiService.cs) | ✅ Exists | Add new method signatures |

### 5.6 Validation & Business Rules

| Rule | Status | Action |
|------|--------|--------|
| Customer.Name required | ✅ Exists | Nothing |
| Phone max length 20 | ✅ Exists | Add regex pattern validation |
| Email format validation | ✅ Exists | Nothing |
| OpeningBalance ≥ 0 | ✅ Exists | Nothing |
| CreditLimit ≥ 0 | ✅ Exists | Nothing |
| CreditLimit enforcement during sales | ❌ Missing | Add in SalesService post-transaction |
| CustomerType required for credit customers | ❌ Missing | Add validation |
| CustomerGroupId must exist | ❌ Missing | Add FK validator |

---

## 6. Architectural Decisions

### 6.1 CustomerType: Enum (NOT Bool)

Although CustomerType could be represented as a `bool IsCreditCustomer`, an enum is preferred because:
- **Extensibility**: Future types may include `COD` (Cash on Delivery), `Prepaid`, `Installment`
- **Clarity**: `CustomerType.Cash` is more readable than `IsCreditCustomer = false`
- **Consistency**: Matches AGENTS.md convention of byte enums (UserRole, InvoiceStatus, PaymentType)

**Decision**: Add `CustomerType` as `byte?` (nullable — defaults to null which means "not specified") with values 1=Cash, 2=Credit.

### 6.2 CustomerGroup: Standalone Entity (NOT String)

Several options were considered:

| Option | Pros | Cons |
|--------|------|------|
| **String column** `GroupName` | Simple, no new table | No reporting, no consistency, typos |
| **Standalone Entity** with CRUD | Full control, reporting, dropdown UI | More files, more code |
| **Enum** | Simple, type-safe | Not user-extensible |

**Decision**: **Standalone Entity** (`CustomerGroup`) with full CRUD. Rationale:
- Users need to create custom groups dynamically (Wholesale, Retail, VIP, Agent, etc.)
- Reporting requires filtering by group
- Dropdown in UI requires a reliable options source
- 4 seed groups will be provided (عام, جملة, قطاعي, VIP)

### 6.3 AccountId: Nullable FK (NOT Required)

The customer may be created without an Account link if the Chart of Accounts module isn't fully set up yet.

**Decision**: `int? AccountId` — nullable FK. When null:
- Customer works normally for cash sales
- Customer won't appear in account-based reports
- A warning icon shows on the customer editor if Account is not linked
- The SalesInvoice and CustomerPayment modules handle the null case gracefully

### 6.4 CreditLimit Enforcement Strategy

**Decision**: Credit limit is checked **during sales invoice posting**, not during draft save.

**Flow**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Note**: This is a soft check — admin users can override (phase 24 feature — "Force Post" permission). In V1, the check blocks the post with a clear Arabic message.

### 6.5 Customer Balance: Computed (NOT Stored Manually)

**Decision**: `CurrentBalance` is updated only through domain methods `IncreaseBalance()` and `DecreaseBalance()`:
- Called from `SalesService.PostAsync()` (increases balance by DueAmount)
- Called from `SalesService.CancelAsync()` (decreases balance by DueAmount)
- Called from `CustomerPaymentService.PostAsync()` (decreases balance by payment amount)
- Called from `SalesReturnService.PostAsync()` (decreases balance)
- NEVER updated directly by the user

The `OpeningBalance` field is set once during customer creation and never changes.

### 6.6 Why Not A Combined "Customers + Suppliers" Module

Some systems combine Customers and Suppliers into a single "Parties" module. We keep them separate because:
- **Different behavior**: Customers have credit limits (due to us), suppliers have credit limits (we owe them)
- **Different permissions**: Cashiers can view customers but not suppliers (AGENTS.md §6)
- **Different reports**: Customer aging vs. Supplier aging
- **Existing codebase**: Both modules already exist and are well-separated

---

## 7. Non-V1 Items (Deferred)

These customer features are explicitly **deferred** to future phases:

| Feature | Reason | Proposed Phase |
|---------|--------|----------------|
| Customer Loyalty Points / Rewards Program | Requires new entity + business logic + UI | Phase 30+ |
| Customer Statements (monthly PDF statements) | Requires background job + email integration | Phase 28+ |
| Bulk SMS / Email to customer groups | Requires external API integration (Twilio, etc.) | Phase 29+ |
| Customer Document Upload (attachments) | Requires file storage infrastructure | Phase 27+ |
| Customer Portal (self-service) | Requires web frontend + authentication | Phase 35+ |
| Customer Price Levels (per-customer pricing) | Complex pricing engine — needs dedicated phase | Phase 31+ |
| Customer Activity Log (audit trail per customer) | Already partially covered by BaseEntity audit fields | Phase 32 |
| Customer Import from Excel/CSV | File parsing + validation + batch processing | Phase 28+ |
| Duplicate Customer Detection (AI/ML) | Complex matching algorithm — not V1 | Phase 33+ |
| Customer Barcode / QR on statements | Print design detail — low priority | Phase 26+ |
| Default Cash Customer setting in SystemSettings (`DefaultCashCustomerId`) | Already exists as key-value setting — just needs UI toggle | ✅ Already exists |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), Arabic ToolTips (RULE-185-190), UI Compact styles (RULE-262-274), and INotifyDataErrorInfo validation (RULE-228).

---

### Task 1 — Rename Default Customer Seed to "عميل نقدي"

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` (line 122) | Change `"العميل الافتراضي في النظام"` → `"عميل نقدي"` |
| `Infrastructure/Data/DbSeeder.cs` (line 130) | Change `"المورد الافتراضي في النظام"` → `"مورد نقدي"` (cross-reference for Supplier module) |

**Code change**:
> See `Infrastructure/Data/DbSeeder.cs` for seed data patterns.

**Logging**: `Log.Information("Default customer seeded: عميل نقدي (ID: {CustomerId})")`

**Validation**: None — name change only

**Estimate**: ~5 minutes

---

### Task 2 — Add AccountId FK to Customer + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Customer.cs` | Add `public int? AccountId { get; private set; }` + `Account? Account` nav property + `LinkToAccount(int accountId)` method |
| `Infrastructure/Data/Configurations/CustomerConfiguration.cs` | Add FK config: `.Property(c => c.AccountId)`, `.HasIndex(c => c.AccountId)`, `.HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Restrict)` |
| `Infrastructure/Data/Migrations/` | New migration: ADD column + FK + INDEX |
| `Contracts/DTOs/AllDtos.cs` — `CustomerDto` | Add `int? AccountId`, `string? AccountName` |
| `Contracts/Requests/CustomerRequests.cs` | Add `int? AccountId` to Create/Update |
| `Contracts/Responses/CustomerResponse.cs` | Add `int? AccountId`, `string? AccountName` |
| `Application/Services/CustomerService.cs` | Map `AccountId` + `AccountName` in `MapToDto()` — eager load `Account` nav property |
| `Api/Validators/CustomerRequestValidators.cs` | Add `AccountId must exist if provided` rule |

**Domain method**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

**SQL migration**:
> See `docs/database-schema.md` for the canonical table definitions and migration patterns.

**Logging**: `Log.Information("Customer {CustomerId} linked to Account {AccountId}", customerId, accountId)`

**Validation**: `RuleFor(x => x.AccountId).GreaterThan(0).When(x => x.AccountId.HasValue)`

**Estimate**: ~1 hour

---

### Task 3 — Add CustomerType Enum + CustomerGroup Entity + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/CustomerType.cs` | **NEW** — `public enum CustomerType : byte { Cash = 1, Credit = 2 }` |
| `Domain/Entities/CustomerGroup.cs` | **NEW** — entity with Name, Description, IsActive |
| `Domain/Entities/Customer.cs` | Add `public byte? CustomerType { get; private set; }` + `public int? CustomerGroupId { get; private set; }` + nav properties + `SetCustomerType()`, `SetCustomerGroup()` methods |
| `Infrastructure/Data/Configurations/CustomerConfiguration.cs` | Add `.Property(c => c.CustomerType)`, `.HasIndex(c => c.CustomerGroupId)`, `.HasForeignKey(c => c.CustomerGroupId).OnDelete(DeleteBehavior.Restrict)` |
| `Infrastructure/Data/Configurations/CustomerGroupConfiguration.cs` | **NEW** — Fluent API config |
| `Infrastructure/Data/DbSeeder.cs` | Add seed data for 4 CustomerGroups + link default customer to "عام" group + set CustomerType = Cash |
| `Infrastructure/Data/Migrations/` | New migration: ADD columns + FK + tables |
| `Contracts/DTOs/AllDtos.cs` — `CustomerDto` | Add `byte? CustomerType`, `int? CustomerGroupId`, `string? CustomerGroupName` |
| `Contracts/DTOs/AllDtos.cs` — `CustomerGroupDto` | **NEW** — record |
| `Contracts/Requests/CustomerRequests.cs` | Add `byte? CustomerType`, `int? CustomerGroupId` to Create/Update |
| `Contracts/Requests/CustomerGroupRequests.cs` | **NEW** — CreateCustomerGroupRequest, UpdateCustomerGroupRequest |
| `Contracts/Responses/CustomerResponse.cs` | Add `byte? CustomerType`, `string? CustomerGroupName` |
| `Application/Services/CustomerService.cs` | Map new fields in `MapToDto()` + eager load CustomerGroup nav property |

**CustomerGroup entity**:

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Customer configuration additions**:

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

> See `Infrastructure/Data/DbSeeder.cs` for seed data patterns.

**SQL migration**:
> See `docs/database-schema.md` for the canonical table definitions and migration patterns.

**Logging**: `Log.Information("Customer {Id} type set to {CustomerType}", id, customerType)`

**Validation**: `RuleFor(x => x.CustomerType).InclusiveBetween(1, 2).When(x => x.CustomerType.HasValue)`

**Estimate**: ~2 hours

---

### Task 3.1 — Auto-Create Sub-Account When CustomerType = Credit

**Problem**: Analysis Part 2 (lines 740-821) requires that when creating a customer with `CustomerType = Credit`, the system must automatically create a sub-account in the Chart of Accounts under the `العملاء` parent account (Account Code 1210, seeded in Phase 22).

**Files**:

| File | Change |
|------|--------|
| `Application/Services/CustomerService.cs` | Add `CreateCustomerAccountAsync()` method as part of `CreateAsync()` flow |
| `Application/Interfaces/Services/ICustomerService.cs` | Add `CreateCustomerAccountAsync` signature |
| `Application/Services/CustomerService.cs` | Update `CreateAsync()` to call account creation when CustomerType is Credit |
| `Infrastructure/Data/Migrations/` | Ensure Account sequence exists for auto-code generation |
| `Contracts/DTOs/AllDtos.cs` — `CustomerDto` | Ensure `AccountId`, `AccountName` returned after creation |

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer + transaction patterns.

**⚠️ Dependency**: Phase 22 must be complete — the parent account `1210 — العملاء` must exist in the seeded Chart of Accounts. If Phase 22 is not yet deployed, this feature will skip account creation and return a clear error message.

**Logging**:
- `Log.Information("Sub-account created for Customer {CustomerId}: AccountCode={Code}, AccountId={Id}", ...)`
- `Log.Warning("Could not auto-create account for credit customer {Id}: parent account 1210 not found", ...)`

**Estimate**: ~1.5 hours

---

### Task 4 — Add CreditLimit Validation Domain Method + Service Check

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Customer.cs` | Add `CheckCreditLimit(decimal additionalAmount)` method + `IsCreditLimitExceeded(decimal projectedBalance)` method |
| `Application/Services/CustomerService.cs` | Add `CheckCreditLimitAsync(int customerId, decimal amount, CancellationToken ct)` method |
| `Application/Services/SalesService.cs` | Add credit limit check before BeginTransactionAsync in PostAsync |
| `Application/Interfaces/Services/ICustomerService.cs` | Add `CheckCreditLimitAsync` signature |
| `Contracts/DTOs/AllDtos.cs` | Add `CreditLimitCheckResultDto` record |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Logging**: 
- `Log.Warning("Credit limit exceeded for Customer {Id}: Current={Current}, Limit={Limit}", ...)` (RULE-183 — user mistake)
- `Log.Information("Credit limit check passed for Customer {Id}", ...)`

**Validation**: N/A — this is business logic validation, not request validation

**Estimate**: ~1 hour

---

### Task 5 — Enhance CustomerEditorViewModel with New Fields + INotifyDataErrorInfo

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerEditorViewModel.cs` | Add AccountId, CustomerType, CustomerGroupId properties + load groups + validation |

> See `docs/AGENTS.md` for ViewModel patterns (INotifyDataErrorInfo, ExecuteAsync wrapper). See existing editor ViewModels for property binding conventions.

> See `docs/AGENTS.md` §2.23 for INotifyDataErrorInfo validation patterns.

> See existing ViewModel constructors in `DesktopPWF/ViewModels/` for initialization patterns.

**Logging**: `LogSystemError("Failed to load customer groups", "CustomerEditorViewModel.LoadCustomerGroupsAsync", ex)`

**Estimate**: ~1.5 hours

---

### Task 6 — Update CustomerEditorView.xaml with New Fields + Arabic ToolTips + Compact Styles

**Files**:

| File | Change |
|------|--------|
| `Views/Customers/CustomerEditorView.xaml` | Add CustomerType radio/dropdown, CustomerGroup combo, AccountId search/combo, fix FontSize=20 violation |

**New XAML sections (in order)**:

> See XAML patterns in existing views at `DesktopPWF/Views/` and `DesktopPWF/Styles/Styles.xaml` for control styling conventions.

> See `docs/AGENTS.md` §2.64 for UI compact style rules (RULE-262–274).

> See `docs/AGENTS.md` §2.64 for UI compact style rules (RULE-262–274).

**Arabic ToolTips (RULE-185-190)**:
- CustomerType combo: `"اختيار نوع العميل — نقدي (الدفع عند الاستلام) أو آجل (فاتورة على الحساب)"`
- CustomerGroup combo: `"تصنيف العميل ضمن مجموعة — يساعد في التقارير والتصفية"`
- Account combo: `"ربط العميل بحساب في دليل الحسابات — ضروري للتقارير المحاسبية"`
- Save button: `"حفظ بيانات العميل — سيتم تحديث جميع المعلومات"`
- Cancel button: `"إلغاء وإغلاق نافذة العميل"`

**Estimate**: ~1.5 hours

---

### Task 7 — Update CustomersListView.xaml with Group Filter + Balance Indicator

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerListViewModel.cs` | Add Group filter combo + fix async patterns (RULE-141) + fix CanExecute violations (RULE-059) |
| `Views/Customers/CustomersListView.xaml` | Add group filter UI + balance indicator column + fix ToolTips |

**ViewModel changes**:

**a) Add group filter property**:
> See existing ViewModel patterns in `DesktopPWF/ViewModels/` for ObservableCollection and filter property conventions.

> See `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern (RULE-141–146).

> See `docs/AGENTS.md` §2.23 for interactive validation patterns (RULE-059 — buttons always enabled).

> See `docs/AGENTS.md` §2.23 — CanExecute predicates are never used (buttons always enabled).

> See XAML patterns in existing views at `DesktopPWF/Views/Customers/` for DataGrid/ToolBar conventions.

**Fix XAML ToolTips**:
- Edit button: `"تعديل بيانات العميل المحدد"` (not `"تعديل العنصر المحدد"`)
- Delete button: `"حذف أو إلغاء تنشيط العميل المحدد"` (not `"حذف أو إلغاء تنشيط العنصر المحدد"`)
- Restore button: `"استعادة العميل المحذوف"` (not `"استعادة العنصر المحذوف"`)
- Refresh button: `"تحديث قائمة العملاء"` (not `"تحديث قائمة البيانات"`)

**Logging**: `LogSystemError("Failed to load customer groups", "CustomerListViewModel.LoadGroupsAsync", ex)`

**Estimate**: ~2 hours

---

### Task 8 — Customer Group Module (Full Build — New)

**Files**:

| File | Content |
|------|---------|
| `Domain/Entities/CustomerGroup.cs` | ✅ Already created in Task 3 |
| `Infrastructure/Data/Configurations/CustomerGroupConfiguration.cs` | ✅ Already created in Task 3 |
| `Contracts/DTOs/AllDtos.cs` — `CustomerGroupDto` | **NEW** record |
| `Contracts/Requests/CustomerGroupRequests.cs` | **NEW** — CreateCustomerGroupRequest, UpdateCustomerGroupRequest |
| `Application/Interfaces/Services/ICustomerGroupService.cs` | **NEW** — interface with CRUD |
| `Application/Services/CustomerGroupService.cs` | **NEW** — implementation with Result<T>, IUnitOfWork |
| `Api/Controllers/CustomerGroupsController.cs` | **NEW** — 6 endpoints |
| `Api/Validators/CustomerGroupRequestValidators.cs` | **NEW** — FluentValidation |
| `Desktop/Services/Api/IApiService.cs` — `ICustomerGroupApiService` | **NEW** — add to existing interface file |
| `Desktop/Services/Api/CustomerGroupApiService.cs` | **NEW** — HTTP client |
| `Desktop/ViewModels/Customers/CustomerGroupListViewModel.cs` | **NEW** — list with ExecuteAsync |
| `Desktop/Views/Customers/CustomerGroupListView.xaml` + `.cs` | **NEW** — DataGrid + ToolTips |
| `Desktop/ViewModels/Customers/CustomerGroupEditorViewModel.cs` | **NEW** — INotifyDataErrorInfo |
| `Desktop/Views/Customers/CustomerGroupEditorView.xaml` + `.cs` | **NEW** — editor form |
| `Desktop/Messaging/Messages/AppMessages.cs` | **NEW** — `CustomerGroupChangedMessage` |
| `Desktop/App.xaml.cs` | DI registrations + navigation |

> See `SalesSystem.Contracts/` for canonical DTO definitions.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**API Endpoints** (CustomerGroupsController):

| Method | Endpoint | Policy |
|--------|----------|--------|
| GET | `/api/v1/customers/groups` | `AllStaff` |
| GET | `/api/v1/customers/groups/{id}` | `AllStaff` |
| POST | `/api/v1/customers/groups` | `ManagerAndAbove` |
| PUT | `/api/v1/customers/groups/{id}` | `ManagerAndAbove` |
| DELETE | `/api/v1/customers/groups/{id}` | `ManagerAndAbove` (soft) |
| DELETE | `/api/v1/customers/groups/permanent/{id}` | `AdminOnly` (permanent) |

**FluentValidation**:
- `CreateCustomerGroupRequestValidator`: Name required (max 100), Description max 250
- `UpdateCustomerGroupRequestValidator`: Same

**ViewModel patterns** (RULE-141):
- All async commands wrapped in `ExecuteAsync()`
- Error messages via `LogSystemError()` (RULE-199)
- Dialog titles: `"خطأ في حفظ مجموعة العملاء"` (RULE-173)
- All via `IDialogService` — NO `MessageBox.Show` (RULE-174)
- Async suffix: `ShowErrorAsync`, `ShowSuccessAsync` (RULE-175)

**UI Compact** (RULE-262-274):
- Button/TextBox: 28px via styles
- Header: Padding="12,6", Footer: Padding="12,8"
- Section margins: 6px between fields
- Dialog title: FontSize="16", Section headers: FontSize="14"
- Empty-state: Margin="0,12,0,0", Width="140"
- Dialog icons: 44×44 max

**Arabic ToolTips** (RULE-185-190):
- Add group: `"إضافة مجموعة عملاء جديدة"`
- Edit group: `"تعديل بيانات المجموعة"`
- Delete group: `"حذف المجموعة — لن يتم حذف العملاء المرتبطين بها"`
- Save group: `"حفظ بيانات مجموعة العملاء"`
- Cancel: `"إلغاء والعودة"`

**Estimate**: ~3 hours

---

### Task 9 — Fix CustomerListViewModel Async Pattern Violations (RULE-141)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerListViewModel.cs` | Refactor 3 methods to use ExecuteAsync wrapper: `LoadCustomersAsync`, `DeleteCustomerAsync`, `RestoreCustomerAsync` |
| `ViewModels/Customers/CustomerSelectionViewModel.cs` | Refactor `LoadCustomersAsync` to use ExecuteAsync wrapper + fix empty catch block |

**Changes in CustomerListViewModel**:

> See `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern (RULE-141–146).

**Changes in CustomerSelectionViewModel**:

> See `docs/AGENTS.md` §2.46 for the LogSystemError pattern (RULE-199).

**Remove manual IsBusy assignments** (no longer needed — managed by ExecuteAsync):
- Remove all `IsBusy = true` / `IsBusy = false` from the 3 operation methods
- Remove all `try/catch/finally` blocks — replace with clean operation methods

**Estimate**: ~1 hour

---

### Task 10 — Customer Reports Endpoints + FluentValidation Enhancement

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ICustomerReportService.cs` | **NEW** — Balance report + Aging report |
| `Application/Services/CustomerReportService.cs` | **NEW** — Implementation using IUnitOfWork + raw SQL via ReportRepository |
| `Contracts/DTOs/AllDtos.cs` | Add `CustomerBalanceReportDto`, `CustomerAgingReportDto` |
| `Api/Controllers/CustomersController.cs` | Add 3 new endpoints (or new CustomerReportsController) |
| `Api/Validators/CustomerRequestValidators.cs` | Add Phone regex pattern, new field validators |
| `Desktop/Services/Api/CustomerApiService.cs` | Add GetBalanceReportAsync, GetAgingReportAsync |
| `Desktop/Services/Api/IApiService.cs` | Add balance report + aging report method signatures |

> See `SalesSystem.Contracts/` for canonical DTO definitions.

> See `docs/AGENTS.md` for controller patterns (RULE-022, RULE-025) and `docs/CONSTITUTION.md` for the Result<T> pattern.

> See `SalesSystem.Api/Validators/` for FluentValidation patterns and `docs/AGENTS.md` §2.13 for validation layers.

**Logging**: 
- `Log.Information("Customer balance report generated for {CustomerId}", id)`
- `Log.Warning("Aging report requested with no customers matching criteria")` (RULE-183)

**Estimate**: ~2 hours

---

### Task 11 — Update CustomerSelectionViewModel for CustomerType Filter + Cleanup

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerSelectionViewModel.cs` | Add CustomerType filter + fix async patterns + fix empty catch block |
| `Views/Customers/CustomerSelectionView.xaml` | Add CustomerType filter dropdown + ToolTips |

> See existing selection ViewModel patterns in `DesktopPWF/ViewModels/` for filter properties.

> See `docs/AGENTS.md` §2.36 for the ExecuteAsync pattern (RULE-141–146).

**Fix empty catch block**:

> See `docs/AGENTS.md` §2.46 for the LogSystemError pattern (RULE-199).

> See XAML patterns in existing views at `DesktopPWF/Views/` for control styling conventions.

**Estimate**: ~45 minutes

---

### Task 12 — End-to-End Integration + Desktop Navigation

**Files**:

| File | Change |
|------|--------|
| `Desktop/App.xaml.cs` | Register `CustomerGroupListViewModel`, `CustomerGroupEditorViewModel`, `CustomerGroupApiService`, `ICustomerGroupService` (via API client), `CustomerReportService` |
| `Desktop/App.xaml.cs` | Add navigation menu item for Customer Groups under Customers section |
| `Desktop/MainWindow.xaml` | Add "مجموعات العملاء" MenuItem in Customers navigation section |

> See `DesktopPWF/App.xaml.cs` for DI registration patterns.

> See `DesktopPWF/MainWindow.xaml` for navigation entry patterns.

**Estimate**: ~30 minutes

### Task 13 — Unit Tests

**Files**: NEW test files in `SalesSystem.Domain.Tests`, `SalesSystem.Application.Tests`, `SalesSystem.Api.Tests`, `SalesSystem.Infrastructure.Tests`

#### 1. Domain Entity Tests

**Customer.Create()** — Test with valid inputs creates entity. Test with empty `name` → `DomainException("اسم العميل مطلوب")`. Test with `CreditLimit < 0` → `DomainException`. Test `IncreaseBalance(amount)` and `DecreaseBalance(amount)` update `CurrentBalance` correctly. Test `DecreaseBalance` with amount > CurrentBalance → `DomainException("المبلغ المدفوع أكبر من الإجمالي")`.

**Customer.Create() with CustomerType** — Validate new fields: `CustomerType = CustomerType.Credit` sets `CustomerType = 2`; `CustomerType = CustomerType.Cash` sets `CustomerType = 1`.

**Customer.LinkToAccount()** — Valid accountId links correctly. `accountId <= 0` → `DomainException("رقم الحساب المحاسبي غير صحيح")`.

**Customer.CheckCreditLimit()** — Projected balance > CreditLimit → returns `true`. Within limit → returns `false`. CreditLimit = 0 (no limit) → always `false`.

**Customer.SetCustomerType()** — Valid type values (1, 2) set correctly. Invalid values → `DomainException`.

**Customer.SetCustomerGroup()** — Valid groupId sets correctly.

**CustomerGroup.Create()** — Valid name creates group. Empty name → `DomainException("اسم المجموعة مطلوب")`. Valid description saved.

**CustomerGroup.Update()** — Valid update modifies fields. Empty name → `DomainException`.

#### 2. Service Tests (using Mock<IUnitOfWork>)

**CustomerService.CreateAsync()**:
- Valid request with CustomerType = Credit → `Result<CustomerDto>.Success`; auto-creates sub-account under 1210
- Valid request with CustomerType = Cash → `Result<CustomerDto>.Success`; NO account created
- Duplicate TaxNumber → `Result<CustomerDto>.Failure`
- Transaction rollback on failure (account created but customer save fails)
- `GetAllAsync()` sorts by `Id` descending (RULE-220)

**CustomerService.CheckCreditLimitAsync()**:
- CustomerType = Credit with projected balance <= limit → `Result.Success`
- CustomerType = Credit with projected balance > limit → `Result.Failure` with `ErrorCodes.CreditLimitExceeded` and Arabic message
- CustomerType = Cash → always `Result.Success` (no credit limit)

**CustomerService.GetByIdAsync()**:
- Existing customer → DTO with AccountId, AccountName, CustomerGroupName included
- Non-existent → `Result<CustomerDto>.Failure` with `ErrorCodes.NotFound`

**CustomerService.DeleteAsync()** / **PermanentDeleteAsync()**:
- Soft delete → `Result.Success()`
- Hard delete with FK violation → `Result.Failure` with FK error caught (RULE-200)

#### 3. FluentValidation Tests

**CreateCustomerRequestValidator**:
- Valid request passes (all fields correct)
- Empty Name → fails with "اسم العميل مطلوب"
- Phone > 20 chars → fails
- Phone invalid format (regex) → fails
- Invalid CustomerType (not 1 or 2) → fails when provided
- CustomerGroupId <= 0 → fails when provided
- AccountId <= 0 → fails when provided
- OpeningBalance < 0 → fails
- CreditLimit < 0 → fails

**CustomerGroupRequestValidator**:
- Valid request passes
- Empty Name → fails
- Name > 100 chars → fails
- Description > 250 chars → fails

#### 4. Database Configuration Tests

**CustomerConfiguration**: Verify `HasQueryFilter(c => c.IsActive)`. Verify FK for `AccountId` uses `DeleteBehavior.Restrict`. Verify FK for `CustomerGroupId` uses `DeleteBehavior.Restrict`. Verify `HasIndex(c => c.AccountId)`. Verify `HasIndex(c => c.CustomerGroupId)`. Verify `TaxNumber` has unique index. Verify `CustomerType` property configuration.

**CustomerGroupConfiguration**: Verify `HasQueryFilter(g => g.IsActive)`. Verify unique index on `Name`. Verify `HasMaxLength(100)` on `Name`, `HasMaxLength(250)` on `Description`.

#### 5. Phase-specific Tests

- Customer.Create() with `CustomerType = Credit` auto-creates sub-account under parent Account Code `1210` (العملاء)
- `CreateCustomerAccountAsync()` generates correct account code sequence: `1211`, `1212`, `1213`... (max existing code + 1 under 1210)
- TaxNumber UNIQUE INDEX enforced — duplicate TaxNumber on create/update raises DB exception
- CustomerType enum: Cash=`1`, Credit=`2` with different validation rules per type
- CreditLimit validation: must be `> 0` when `CustomerType = Credit`; can be `0` when `CustomerType = Cash`
- AccountId FK: links customer to Chart of Accounts correctly; FK with `DeleteBehavior.Restrict` — cannot delete Account if customer references it
- Auto-account creation is transactional with customer creation — if account creation fails, customer creation rolls back
- Default customer "عميل نقدي" seeded correctly with `CustomerType = Cash`, linked to "عام" group
- `CustomerGroup` seed data: 4 groups seeded (عام, جملة, قطاعي, VIP) with unique names
- Balance computation: SalesService.PostAsync() calls `IncreaseBalance(DueAmount)`; SalesService.CancelAsync() calls `DecreaseBalance(DueAmount)`
- `AvailableCredit = CreditLimit - CurrentBalance` computed correctly (never negative — floored to 0)

**Estimate**: ~4 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | CreditLimit, CurrentBalance, OpeningBalance — `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | SalesService PostAsync — credit limit check INSIDE transaction | ✅ |
| **RULE-006** | ALL services return `Result<T>` | CustomerService, CustomerGroupService, CustomerReportService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Customer.Name, Phone, Email, Address, TaxNumber — all nvarchar | ✅ |
| **RULE-016** | BaseEntity audit fields | Customer, CustomerGroup inherit BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | CustomerService, CustomerGroupService | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on CRUD + CreditLimit warnings | ✅ |
| **RULE-036** | Log critical operations | Customer create/update/delete, CreditLimit enforcement, default customer rename | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | CustomersController, CustomerGroupsController | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Customer.Create, Update, IncreaseBalance, DecreaseBalance, CheckCreditLimit, LinkToAccount | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateCustomerRequestValidator, UpdateCustomerRequestValidator, Create/Update CustomerGroup validators | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Customer delete dialog: Cancel/Deactivate/Permanent | ✅ |
| **RULE-052** | Guard Clauses on all entities | Customer.Create/Update, CustomerGroup.Create/Update | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "اسم العميل مطلوب", "المبلغ يجب أن يكون أكبر من الصفر" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | CustomerEditorViewModel, CustomerGroupEditorViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | CustomerEditorViewModel — no CanExecute blocking (Task 6 fix) | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | CustomerListViewModel (FIX — Task 9), CustomerEditorViewModel ✅, CustomerSelectionViewModel (FIX — Task 11) | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Customer editor opens via OpenScreen (already correct) | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ العميل"`, `"خطأ في تحميل العملاء"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Credit limit exceeded, validation errors, empty results | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | CustomerApiService inherits from ApiServiceBase with guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, combos, inputs across CustomersListView, CustomerEditorView, CustomerGroup views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "فتح شاشة إضافة عميل جديد" ✅, not "عميل جديد" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل العملية نهائياً — سيتم تحديث رصيد العميل" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة مجموعات العملاء — إنشاء وتعديل المجموعات" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول عميل — ابدأ بتسجيل بيانات العملاء" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-191** | Product/Customer/Supplier — NO Code column | Customer entity already has no Code column ✅ | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | CustomerService.PermanentDeleteAsync catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | CustomerService, CustomerGroupService, CustomerReportService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | CustomersController — service only (already correct) | ✅ |
| **RULE-210** | CHECK constraints at DB level | CreditLimit ≥ 0, OpeningBalance ≥ 0 (add to configuration) | ⬜ Add |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | AccountId FK, CustomerGroupId FK — both Restrict | ✅ |
| **RULE-220** | Newest-first sorting on lists | CustomerListViewModel: OrderByDescending(Id) ✅, fix CustomerService GetAllAsync sort | ⬜ Fix |
| **RULE-227** | SetDialogService() in EVERY Editor VM | CustomerEditorViewModel ✅, CustomerGroupEditorViewModel | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | CustomerEditorViewModel ✅, CustomerGroupEditorViewModel | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in CustomerEditorViewModel ✅ | ✅ |
| **RULE-246** | Users soft-deleted only | Not affected by this phase | ✅ N/A |
| **RULE-249** | Arabic strings UTF-8 encoded | All Arabic strings in new/modified files verified UTF-8 | ✅ |
| **RULE-250** | Files saved with UTF-8 encoding | All modified .cs and .xaml files | ✅ |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected — credit limit uses CurrentBalance | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new/modified XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new/modified XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new/modified XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields (FIX Border Height="12" → "6" in CustomerEditorView) | ⬜ Fix |
| **RULE-266** | Dialog titles FontSize=16 max | FIX CustomerEditorView FontSize="20" → "16" | ⬜ Fix |
| **RULE-267** | Section headers FontSize=14 max | All section headers in modified views | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | CustomerList empty-state button ✅ | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | CustomerEditorView 24×24 ✅ | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | Already set in ScreenWindow.xaml | ✅ N/A |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | CustomerEditorView buttons use Width="140" (Save) and Width="100" (Cancel) — acceptable for primary action | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new/modified XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **AccountId FK references non-existent Account** | **MEDIUM** — migration may fail if Account FK references a table that hasn't been migrated yet | Ensure Phase 22 (Chart of Accounts) migration runs BEFORE Phase 23 migration. If Phases run out of order, make AccountId nullable and add FK only after Accounts exist. |
| **CustomerGroup rename conflict** | **LOW** — "عام" group name may conflict with existing seed data from other modules | Use `AnyAsync()` guard before seeding. Name has UNIQUE index enforced at DB level. |
| **CreditLimit enforcement breaks existing sales** | **MEDIUM** — existing credit customers may have CurrentBalance > CreditLimit after adding enforcement | Add enforcement gently: first warning (not blocking), then blocking in next phase. Or add a grace period setting. |
| **CustomerListViewModel refactor (async patterns) breaks existing tests** | **MEDIUM** — changing method signatures from `async Task` to `ExecuteAsync` wrappers changes how tests invoke commands | Update 3 test files (CustomerListViewModelTests.cs, CustomerEditorViewModelTests.cs) to use new command execution pattern. Run all tests before merging. |
| **CanExecute removal changes UX behavior** | **LOW** — Edit/Delete buttons now always enabled, validation moves to click handler | This is intentional per RULE-059. Users get a warning dialog if they click without selecting. |
| **New migration conflicts with production DB** | **LOW** — additive columns (nullable) are safe. Only risk is FK to Accounts table. | Always test migration against a copy of production DB first. |
| **CustomerGroup CRUD duplicates effort (SupplierGroup in Phase 24)** | **LOW** — CustomerGroup and SupplierGroup could share a base class or interface | Keep separate for now. If they converge, refactor in Phase 25 (Consolidation phase). |
| **TaxNumber uniqueness not enforced** | **LOW** — two customers could have the same TaxNumber | Add unique index on TaxNumber at DB level. Handle gracefully in FluentValidation. |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Default customer rename causes issues | `UPDATE Customers SET Name = N'العميل الافتراضي في النظام' WHERE Id = 1 AND Name = N'عميل نقدي'` |
| AccountId FK migration fails | `ALTER TABLE Customers DROP CONSTRAINT FK_Customers_Accounts_AccountId; ALTER TABLE Customers DROP COLUMN AccountId;` |
| CustomerType columns cause issues | `ALTER TABLE Customers DROP COLUMN CustomerType; ALTER TABLE Customers DROP COLUMN CustomerGroupId;` |
| CustomerGroup table not needed | `DROP TABLE CustomerGroups;` (check FK first) |
| CustomerGroup module desktop not needed | Remove DI registrations + navigation entries — no data impact |
| CreditLimit enforcement blocks too many sales | Comment out the `CheckCreditLimitAsync()` call in SalesService — business logic only, no schema change |
| CustomerListViewModel async refactor breaks | Revert CustomerListViewModel.cs to previous version — all data access is via API (no schema impact) |
| `FontSize="16"` change disliked by user | Revert to `FontSize="20"` — cosmetic only, no data impact |
