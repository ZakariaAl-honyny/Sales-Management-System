# Phase 23 — Customers Module: Comprehensive Implementation Plan

> **Version**: 2.1 — Updated per Phase 18 Accounting Foundation: ❌ Parties removed, ❌ CustomerGroup removed, ❌ CustomerType removed, ✅ AccountId mandatory auto-created under "1103", ✅ Contact fields direct on Customer, ✅ Balance via JournalEntryLines, ✅ Account.Create() uses 13-param signature (Phase 18 aligned)
> **Scope**: Complete Customer CRUD enhancement with Account auto-creation (parent code "1103"), CategoryId classification, CreditLimit enforcement, enhanced validation, customer reports, and UI improvements
> **Phase 18 Cross-References**: RULE-321→340 (Account entity/hierarchy), RULE-506 (allowTransactions for Level 4), RULE-504 (parent code "1103")

### v2.1 Changelog (Phase 18 Alignment)
- Fixed `Account.Create()` signature from 14 params to 13 (removed `allowTransactions` — now computed `=> IsLeaf`)
- Fixed `nature` type from `AccountNature.Debit` (enum) to `byte 1` (Asset)
- Fixed `ColorCode` from hardcoded `#2196F3` to `IAccountCodeGeneratorService.GetColorCode(nature)` → `#2196F3` (auto-generated, matching codebase)
- Fixed `AccountCode` generation from manual `GetMaxChildCodeAsync` to `AccountCodeGeneratorService.GenerateCodeAsync()` (Phase 18 hierarchical numbering)
- Added Phase 18 cross-reference rules (RULE-321→340, RULE-353) to compliance matrix
- Added FORBIDDEN patterns for hardcoded ColorCode, `allowTransactions` param, and `AccountNature` enum
- Updated test assertions to match Phase 18 entity signatures

---

## Table of Contents

1. [Customer Module Architecture — Scope](#1-customer-module-architecture--scope)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Issues](#3-blocker-resolution--critical-issues)
4. [Customer Design Catalog](#4-customer-design-catalog)
5. [Gap Analysis — Existing vs Target](#5-gap-analysis--existing-vs-target)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix](#9-compliance-matrix)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)
12. [FORBIDDEN Patterns](#12-forbidden-patterns)

---

## 1. Customer Module Architecture — Scope

The Customers Module is a **single focused scope**: managing customer records with direct contact fields, mandatory Account linking for balance tracking, optional CategoryId classification, and credit limit enforcement.

| # | Aspect | Purpose | Existing Status |
|---|--------|---------|-----------------|
| 🧑‍🤝‍🧑 | **Customer CRUD** | Create, Read, Update, Soft-Delete, Permanent-Delete with DeleteStrategy | ✅ Core CRUD exists — needs enhancement |
| 🔗 | **Customer-Account Link** | Auto-create a Level-4 Account under "1103 — العملاء" parent on creation | ✅ Already in code — parent code must be "1103" |
| 🚫 | **CustomerGroup** | ❌ NOT IN V1 — removed per accounts Details.md | ✅ Already absent from code |
| 🚫 | **CustomerType** | ❌ NOT IN V1 — payment type is per-invoice (SalesInvoice.PaymentType), not per-customer | ✅ Already absent from code |
| 🚫 | **Party entity** | ❌ NOT USED — contact fields go DIRECTLY on Customer entity | ✅ Removed from code |

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

### 1.2 Customer-Account Link Flow (Mandatory)

```text
Customer.Entity
    ├── Name (string)             ← DIRECT on Customer (NOT via Party)
    ├── Phone (string?)           ← DIRECT on Customer
    ├── Email (string?)           ← DIRECT on Customer
    ├── Address (string?)         ← DIRECT on Customer
    ├── TaxNumber (string?)       ← DIRECT on Customer
    ├── Notes (string?)           ← DIRECT on Customer
    ├── AccountId (int FK → Account)  ← MANDATORY, auto-created
    ├── CategoryId (int? FK → AccountCategories)
    └── CreditLimit (decimal)     ← Soft warning only (CheckCreditLimit returns bool)

Account (Chart of Accounts — Phase 22)
    — Balance tracked via JournalEntryLines
    — Enables: balance aging reports, transaction history
    — Enables: automatic journal entries when creating customer payments
```

### 1.3 Key Architecture Principles (from accounts Details.md)

```text
✅ حذف Parties → Contact fields directly on Customer entity
✅ لكل عميل حساب محاسبي تلقائياً تحت 1103
✅ الرصيد الحقيقي يأتي من JournalEntryLines وليس من جدول العميل
✅ No CustomerGroup in V1
✅ No CustomerType in V1 — payment classification per-invoice
✅ CustomerReceiptApplications لا تستخدم لحساب الرصيد بل لمعرفة ما الفواتير التي تم تسديدها
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Entity ⚠️ (Core exists — PartyId must be removed)

**File**: `SalesSystem.Domain.Entities.Customer`

**Current (with PartyId) → Target (direct contact fields)**:

| Field | Type | Required | Target Status |
|-------|------|----------|---------------|
| `Id` | `int PK` | ✅ | ✅ Keep |
| `Name` | `nvarchar(150)` | ✅ | ⬜ **MOVE from Party to Customer directly** |
| `Phone` | `nvarchar(20)?` | ❌ | ⬜ **MOVE from Party to Customer directly** |
| `Email` | `nvarchar(100)?` | ❌ | ⬜ **MOVE from Party to Customer directly** |
| `Address` | `nvarchar(250)?` | ❌ | ⬜ **MOVE from Party to Customer directly** |
| `TaxNumber` | `nvarchar(30)?` | ❌ | ⬜ **MOVE from Party to Customer directly** |
| `Notes` | `nvarchar(500)?` | ❌ | ⬜ **MOVE from Party to Customer directly (new)** |
| `PartyId` | `int FK → Party` | ✅ | ❌ **REMOVE — contact fields now direct** |
| `AccountId` | `int FK → Account` | ✅ (mandatory) | ✅ **Keep — already non-nullable** |
| `CategoryId` | `int? FK → AccountCategories` | ❌ | ✅ Keep |
| `CreditLimit` | `decimal(18,2)` | ❌ | ✅ Keep |
| `IsActive` | `bit` | ✅ | ✅ Keep (BaseEntity) |
| `CreatedAt` | `datetime2` | ✅ | ✅ Keep |
| `UpdatedAt` | `datetime2?` | ❌ | ✅ Keep |
| `CreatedByUserId` | `int? FK` | ❌ | ✅ Keep |
| `UpdatedByUserId` | `int? FK` | ❌ | ✅ Keep |

**REMOVED fields (confirmed absent from codebase):**
- ❌ `CustomerGroupId` — NOT in V1
- ❌ `CustomerType` — NOT in V1
- ❌ `OpeningBalance` — handled via Journal Entry (Dr AR / Cr OpeningBalanceEquity)
- ❌ `CurrentBalance` — balance lives on linked Account, queried from JournalEntryLines

**Domain methods (current):**
- `Customer.Create(partyId, accountId, creditLimit, categoryId, createdByUserId)` ✅ → ⬜ Change to direct contact fields
- `Customer.Update(creditLimit, categoryId, updatedByUserId)` ✅ → ⬜ Add contact field parameters
- `CheckCreditLimit(decimal additionalAmount)` ✅ — returns bool, non-throwing

**Domain methods (target after Party removal):**
- `Customer.Create(name, phone, email, address, taxNumber, notes, accountId, creditLimit, categoryId, createdByUserId)` ⬜ **NEW signature with direct contact fields**
- `Customer.Update(name, phone, email, address, taxNumber, notes, creditLimit, categoryId, updatedByUserId)` ⬜ **NEW signature with direct contact fields**
- `Customer.CheckCreditLimit(decimal additionalAmount)` ✅ Keep as-is

### 2.2 Party Entity ❌ — Must Be Removed

**File**: `SalesSystem.Domain.Entities.Party`

| Field | Type | Action |
|-------|------|--------|
| `Id` | `int PK` | ❌ Remove entire entity |
| `Name` | `nvarchar(150)` | ➡️ Move to Customer directly |
| `Phone` | `nvarchar(20)?` | ➡️ Move to Customer directly |
| `Email` | `nvarchar(100)?` | ➡️ Move to Customer directly |
| `Address` | `nvarchar(250)?` | ➡️ Move to Customer directly |
| `TaxNumber` | `nvarchar(30)?` | ➡️ Move to Customer directly |
| `Notes` | `nvarchar(500)?` | ➡️ Move to Customer directly |

**Impact**: Removing Party affects Supplier and Employee too — this plan covers ONLY Customer. Follow-up phases will handle Supplier and Employee Party removal.

### 2.3 Infrastructure Layer ⚠️ (Needs Party→Direct migration)

**File**: `Infrastructure/Data/Configurations/CustomerConfiguration.cs`

| Setting | Status |
|---------|--------|
| `ToTable("Customers")` | ✅ Exists |
| `HasKey(c => c.Id)` | ✅ Exists |
| PartyId FK config (restrict) | ✅ Exists — to be removed |
| AccountId FK config (restrict) | ✅ Exists |
| `HasIndex(c => c.AccountId)` | ✅ Exists |
| `HasQueryFilter(c => c.IsActive)` | ✅ Exists |

**Changes needed:**
- ❌ Remove `PartyId` FK configuration entirely
- ✅ Add direct contact field configs: `Name` (HasMaxLength 150, IsRequired), `Phone` (HasMaxLength 20), `Email` (HasMaxLength 100), `Address` (HasMaxLength 250), `TaxNumber` (HasMaxLength 30), `Notes` (HasMaxLength 500, optional)
- ✅ Add `HasIndex(c => c.TaxNumber).IsUnique().HasFilter("[TaxNumber] IS NOT NULL")` — optional unique tax number
- ⬜ New migration: DROP FK to Parties, DROP PartyId column, ADD direct contact columns, DROP Parties table (after all references removed)

**File**: `Infrastructure/Data/Configurations/PartyConfiguration.cs`

| Setting | Action |
|---------|--------|
| Entire file | ❌ **DELETE** — Party entity removed |

**File**: `Infrastructure/Data/DbSeeder.cs`

| Seed Data | Status | Action |
|-----------|--------|--------|
| Default customer "عميل نقدي" | ✅ Exists | Keep — but seed with direct fields, not via Party |
| Default customer Account linking | ✅ Exists | Keep — link to auto-created Account under "1103" |
| CustomerGroup seeds | ❌ Already absent | ✅ Nothing to do |
| CustomerType references | ❌ Already absent | ✅ Nothing to do |

### 2.4 Application Layer ✅ (Core exists — Party methods to refactor)

**File**: `Application/Interfaces/Services/ICustomerService.cs`

| Method | Return Type | Status | Action |
|--------|-------------|--------|--------|
| `GetByIdAsync(id, ct)` | `Result<CustomerDto>` | ✅ Exists | Fix mapping (Contact fields from Customer directly) |
| `GetAllAsync(search, page, pageSize, includeInactive, ct)` | `Result<PagedResult<CustomerDto>>` | ✅ Exists | Fix mapping, fix sort order (Id desc — RULE-220) |
| `CreateAsync(request, ct)` | `Result<CustomerDto>` | ✅ Exists | Auto-create Party replaced by direct fields + auto-create Account |
| `UpdateAsync(id, request, ct)` | `Result<CustomerDto>` | ✅ Exists | Fix mapping (no Party update) |
| `DeleteAsync(id, ct)` | `Result` | ✅ Exists | Soft delete — keep as-is |
| `PermanentDeleteAsync(id, ct)` | `Result` | ✅ Exists | Will change: no Party to cascade delete |

**Key service behavior changes:**
- `CreateAsync()`: No longer creates a Party record. Creates Customer directly with Name, Phone, Email, Address, TaxNumber, Notes. Auto-creates Account under parent "1103" if AccountId not provided.
- `UpdateAsync()`: No longer calls Party.Update(). Updates fields directly on Customer entity.
- `PermanentDeleteAsync()`: No Party FK to check — simpler cleanup. Still catches FK exceptions from SalesInvoice/Payment references.

**File**: `Application/Services/CustomerService.cs`

Observations:
- ✅ Uses `IUnitOfWork` (RULE-024)
- ✅ Returns `Result<T>` (RULE-006)
- ✅ Has proper logging (RULE-035)
- ✅ `PermanentDeleteAsync` catches FK exceptions (RULE-200)
- ❌ `GetAllAsync` sorts by Name ascending — should sort by Id descending (RULE-220)
- ⬜ Currently creates Party record before Customer — must change to direct Customer creation

### 2.5 Contracts Layer ⚠️ (Needs enhancement — CustomerDto)

**File**: `Contracts/DTOs/AllDtos.cs`

**CustomerDto — Current vs Target**:

| Field | Current Status | Target | Action |
|-------|---------------|--------|--------|
| `Id` | ✅ Exists | Keep | ✅ |
| `Name` | ✅ From Party (via navigation) | **DIRECT on CustomerDto** | ⬜ Fix mapping |
| `Phone` | ✅ From Party (via navigation) | **DIRECT on CustomerDto** | ⬜ Fix mapping |
| `Email` | ✅ From Party (via navigation) | **DIRECT on CustomerDto** | ⬜ Fix mapping |
| `Address` | ✅ From Party (via navigation) | **DIRECT on CustomerDto** | ⬜ Fix mapping |
| `TaxNumber` | ✅ From Party (via navigation) | **DIRECT on CustomerDto** | ⬜ Fix mapping |
| `Notes` | ❌ Missing | **ADD** — string? | ⬜ NEW |
| `AccountId` | ✅ Exists (mandatory int) | Keep | ✅ |
| `AccountName` | ✅ Exists | Keep | ✅ |
| `CategoryId` | ✅ Exists | Keep | ✅ |
| `CreditLimit` | ✅ Exists | Keep | ✅ |
| `CreatedAt` | ✅ Exists | Keep | ✅ |
| `IsActive` | ✅ Exists | Keep | ✅ |
| ~~PartyId~~ | ❌ Should NOT be exposed | **REMOVE** | ⬜ NEW |
| ~~CustomerType~~ | ❌ Already absent | NOT IN V1 | ✅ |
| ~~CustomerGroupId~~ | ❌ Already absent | NOT IN V1 | ✅ |
| ~~CustomerGroupName~~ | ❌ Already absent | NOT IN V1 | ✅ |

**File**: `Contracts/Requests/CustomerRequests.cs`

| Field | Status | Action |
|-------|--------|--------|
| `Name` | ✅ Exists | Keep — required |
| `Phone` | ✅ Exists | Keep |
| `Email` | ✅ Exists | Keep |
| `Address` | ✅ Exists | Keep |
| `TaxNumber` | ✅ Exists | Keep |
| `CreditLimit` | ✅ Exists | Keep |
| `CategoryId` | ✅ Exists | Keep |
| `AccountId` | NOT in request | ❌ **NOT added** — auto-created by service |
| ~~PartyId~~ | ❌ Not in request already | ✅ Nothing to do |
| ~~CustomerGroupId~~ | ❌ Already absent | ✅ |
| ~~CustomerType~~ | ❌ Already absent | ✅ |

**Key insight**: Since AccountId is auto-created (mandatory, non-nullable, system-generated), it MUST NOT be in Create/Update requests. The service always generates it.
- `CreateCustomerRequest`: No AccountId field — service creates Account under "1103"
- `UpdateCustomerRequest`: No AccountId field — cannot change the linked account

### 2.6 API Layer ✅ (Core exists — minor enhancements)

**File**: `Api/Controllers/CustomersController.cs`

| Method | Endpoint | Policy | Status |
|--------|----------|--------|--------|
| GET | `/api/v1/customers` | `AllStaff` | ✅ Exists |
| GET | `/api/v1/customers/{id}` | `AllStaff` | ✅ Exists |
| POST | `/api/v1/customers` | `ManagerAndAbove` | ✅ Exists |
| PUT | `/api/v1/customers/{id}` | `ManagerAndAbove` | ✅ Exists |
| DELETE | `/api/v1/customers/{id}` | `ManagerAndAbove` | ✅ Exists (soft) |
| DELETE | `/api/v1/customers/permanent/{id}` | `ManagerAndAbove` | ✅ Exists (permanent) |
| GET | `/api/v1/customers/{id}/balance-report` | `AllStaff` | ⬜ NEW (report endpoint) |
| GET | `/api/v1/customers/aging` | `AllStaff` | ⬜ NEW (report endpoint) |

**REMOVED from scope (NOT in V1):**
- ❌ No `/api/v1/customers/groups` endpoints (CustomerGroup not in V1)
- ❌ No `CustomerGroupsController`

**File**: `Api/Validators/CustomerRequestValidators.cs`

| Validator | Status |
|-----------|--------|
| `CreateCustomerRequestValidator` | ✅ Exists — update contact fields, add Notes + Phone regex |
| `UpdateCustomerRequestValidator` | ✅ Exists — same updates |

**Missing validation to add:**
- ✅ Phone regex `^05\d{8}$` with Arabic error message "رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام"
- ✅ Email format enhancement via `.EmailAddress()`
- ✅ Notes max length 500
- ✅ Name max length 150 with Arabic message "اسم العميل مطلوب"
- ✅ Remove any Party-related validation (none should exist currently)

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
- ❌ Party.Name reference in display — change to direct Customer.Name
- ✅ Sort by Id descending (RULE-220)
- ✅ EventBus subscription + cleanup
- ✅ DeleteStrategy dialog with 3 options
- ✅ IncludeInactive property for soft-deleted entities
- ⬜ Add AccountId, AccountName display columns
- ⬜ ErrorMessage bar (RULE-540)

**Customer Editor**:

| File | Status |
|------|--------|
| `ViewModels/Customers/CustomerEditorViewModel.cs` | ✅ Exists — 244 lines |
| `Views/Customers/CustomerEditorView.xaml` | ✅ Exists — 185 lines |
| `Views/Customers/CustomerEditorView.xaml.cs` | ✅ Exists (code-behind) |

**Issues in CustomerEditorViewModel:**
- ✅ Uses `SetDialogService()` ✅
- ✅ Uses `INotifyDataErrorInfo` (RULE-228)
- ✅ ValidateAsync calls ClearAllErrors + AddError + ValidateAllAsync (RULE-229)
- ❌ Currently loads Party data via separate API call — change to direct fields
- ❌ Missing `Notes` property binding
- ❌ SaveAsync creates Party before Customer — change to create Customer directly
- ⬜ Add loading overlay (RULE-548)
- ⬜ Success/failure feedback (RULE-536-539)

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
- ❌ Party.Name reference — change to direct Customer.Name

**Customer API Service**:

| File | Status |
|------|--------|
| `Services/Api/CustomerApiService.cs` | ✅ Exists — 60 lines |
| `Services/Api/IApiService.cs` (ICustomerApiService) | ✅ Exists |

**Missing API methods:**
- ⬜ `GetBalanceReportAsync(id, from, to)` — NEW
- ⬜ `GetAgingReportAsync()` — NEW (from JournalEntryLines)

**REMOVED API methods (CustomerGroup gone):**
- ❌ No GetGroupsAsync — NOT needed
- ❌ No CreateGroupAsync — NOT needed
- ❌ No UpdateGroupAsync — NOT needed
- ❌ No DeleteGroupAsync — NOT needed

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
| Dialog icons 44×44 max | N/A | ✅ 24×24 |

---

## 3. BLOCKER Resolution — Critical Issues

### 3.1 Blocker 1: Party Entity Removal — Contact Fields Must Move to Customer

**Problem**: The current Customer entity stores all contact data (Name, Phone, Email, Address, TaxNumber) on a **Party** record linked via `PartyId` FK. The accounts Details.md analysis explicitly mandates:
```
✅ حذف Parties
✅ جعل Customers, Suppliers, Employees جداول مستقلة
```

**Solution**: Remove `PartyId` from Customer. Add all contact fields directly to Customer entity:
- `Name` (nvarchar(150), required) — moved from Party
- `Phone` (nvarchar(20)?) — moved from Party
- `Email` (nvarchar(100)?) — moved from Party
- `Address` (nvarchar(250)?) — moved from Party
- `TaxNumber` (nvarchar(30)?) — moved from Party
- `Notes` (nvarchar(500)?) — moved from Party

**Impact**:
- Requires data migration: `INSERT INTO Customers (Name, Phone, Email, Address, TaxNumber) SELECT p.Name, p.Phone, p.Email, p.Address, p.TaxNumber FROM Parties p JOIN Customers c ON p.Id = c.PartyId`
- Requires EF migration: DROP FK, DROP PartyId, ADD direct columns
- CustomerService.CreateAsync() no longer creates Party first
- CustomerService.UpdateAsync() no longer calls Party.Update()
- Parties table can be dropped after ALL references (Supplier, Employee) are migrated

**⚠️ Multi-module dependency**: Supplier and Employee also reference Party. This plan covers ONLY Customer. The Party entity and Parties table will remain until Supplier and Employee are migrated in their respective phases. The Customer migration simply removes its PartyId FK and adds direct contact fields.

**Files changed**:
- `Domain/Entities/Customer.cs` — Remove PartyId, add direct fields
- `Domain/Entities/Party.cs` — No change (still used by Supplier/Employee)
- `Infrastructure/Data/Configurations/CustomerConfiguration.cs` — Remove PartyId FK, add direct field configs
- `Infrastructure/Data/Migrations/` — NEW migration

### 3.2 Blocker 2: AccountId Mandatory — Auto-Creation Under Parent "1103"

**Problem**: AccountId is already mandatory (non-nullable `int` FK) on Customer entity. The service must auto-create a Level-4 detail account under parent code "1103" (Accounts Receivable/العملاء) when creating a customer.

> ⚠️ **CRITICAL**: The parent account code is "1103" — NOT "1210" as previously documented. RULE-504 stipulates: "CustomerService.AutoCreateCustomerAccountAsync() MUST look up parent account by code '1103' (Accounts Receivable/العملاء) — NOT '1210' (Fixed Assets)."

**Solution**: Auto-create Account in `CustomerService.CreateAsync()`:
1. Look up parent account by code `"1103"` (seeded in Phase 22)
2. Generate next child code via `AccountCodeGeneratorService.GenerateCodeAsync()` (Phase 18: SemaphoreSlim thread-safe)
3. Create Level-4 Account with `isLeaf: true` (AllowTransactions is computed `=> IsLeaf`)
4. Link Customer to the new Account via `AccountId`
5. Wrap in `ExecuteTransactionAsync()` — if customer creation fails, account creation rolls back

**Auto-creation logic** (aligned with Phase 18 Account.Create — 13 params):
```csharp
// Inside CustomerService.CreateAsync() — Phase 18 aligned
var parentAccount = await _uow.Accounts.GetByCodeAsync("1103", ct);
// Phase 18: AccountCodeGeneratorService generates hierarchical codes (SemaphoreSlim)
var newCode = await _accountCodeGenerator.GenerateCodeAsync(parentAccount.Id, level: 4, ct);
// Phase 18: ColorCode auto-generated from Nature via IAccountCodeGeneratorService
var colorCode = IAccountCodeGeneratorService.GetColorCode(parentAccount.Nature);
var account = Account.Create(
    accountCode: newCode,
    nameAr: customer.Name,
    nameEn: customer.Name,
    nature: parentAccount.Nature,    // byte (1=Asset) from parent, NOT enum class
    isLeaf: true,                    // Level 4 detail (AllowTransactions computed => IsLeaf)
    parentId: parentAccount.Id,
    isSystem: false,                 // Not a system account
    categoryId: null,
    level: 4,                        // Detail/leaf level
    description: $"حساب العميل: {customer.Name}",
    colorCode: colorCode,            // Auto-generated, NOT hardcoded
    notes: null,
    createdByUserId: userId
);
await _uow.Accounts.AddAsync(account, ct);
```

**Validation**: AccountId is NEVER accepted from the user in Create/Update requests. The service generates it.
- `CreateCustomerRequest`: No `AccountId` field
- `UpdateCustomerRequest`: No `AccountId` field (cannot change linked account)
- `CustomerDto`: Exposes `AccountId` and `AccountName` as read-only outputs

**Migration**: No migration needed for this — AccountId FK already exists. Only the auto-creation logic needs implementation.

### 3.3 Blocker 3: OpeningBalance/CurrentBalance — ALREADY REMOVED

**Problem**: None — the Customer entity already has NO OpeningBalance or CurrentBalance fields. The code already matches the target architecture.

**Verification**: Customer.cs confirms:
- ❌ No `OpeningBalance` property
- ❌ No `CurrentBalance` property
- ✅ Balance tracked via JournalEntryLines on linked Account

---

## 4. Customer Design Catalog

### 4.1 Customer Entity (Target — After Party Removal)

| # | Field | Type | Required | Default | Constraints | Source |
|---|-------|------|----------|---------|-------------|--------|
| 1 | `Id` | `int PK` | ✅ | Auto | — | Existing |
| 2 | `Name` | `nvarchar(150)` | ✅ | — | — | ⬜ Moved from Party |
| 3 | `Phone` | `nvarchar(20)?` | ❌ | `null` | Regex: `^05\d{8}$` | ⬜ Moved from Party |
| 4 | `Email` | `nvarchar(100)?` | ❌ | `null` | Email format | ⬜ Moved from Party |
| 5 | `Address` | `nvarchar(250)?` | ❌ | `null` | — | ⬜ Moved from Party |
| 6 | `TaxNumber` | `nvarchar(30)?` | ❌ | `null` | UNIQUE INDEX (optional) | ⬜ Moved from Party |
| 7 | `Notes` | `nvarchar(500)?` | ❌ | `null` | — | ⬜ Moved from Party |
| 8 | `AccountId` | `int FK` | ✅ | Auto | FK → Accounts, Restrict | ✅ Exists (non-nullable) |
| 9 | `CategoryId` | `int? FK` | ❌ | `null` | FK → AccountCategories, Restrict | ✅ Exists |
| 10 | `CreditLimit` | `decimal(18,2)` | ❌ | `0` | `CHECK >= 0` (0 = no limit) | ✅ Exists |
| 11 | `IsActive` | `bit` | ✅ | `true` | Global query filter | ✅ Existing (BaseEntity) |
| 12 | `CreatedAt` | `datetime2` | ✅ | `GETUTCDATE()` | — | ✅ Existing (BaseEntity) |
| 13 | `UpdatedAt` | `datetime2?` | ❌ | `null` | — | ✅ Existing (BaseEntity) |
| 14 | `CreatedByUserId` | `int? FK` | ❌ | `null` | FK → Users, Restrict | ✅ Existing (BaseEntity) |
| 15 | `UpdatedByUserId` | `int? FK` | ❌ | `null` | FK → Users, Restrict | ✅ Existing (BaseEntity) |

**REMOVED (NOT IN V1):**
- ~~`CustomerGroupId`~~ ❌ NOT IN V1
- ~~`CustomerType`~~ ❌ NOT IN V1 (payment type per-invoice)
- ~~`PartyId`~~ ❌ Replaced by direct contact fields
- ~~`OpeningBalance`~~ ❌ Handled via Journal Entry
- ~~`CurrentBalance`~~ ❌ Balance on linked Account

### 4.2 CustomerDto (Target)

| # | Field | Type | Notes |
|---|-------|------|-------|
| 1 | `Id` | `int` | Auto-increment PK |
| 2 | `Name` | `string` | Direct from Customer (not Party) |
| 3 | `Phone` | `string?` | Direct from Customer |
| 4 | `Email` | `string?` | Direct from Customer |
| 5 | `Address` | `string?` | Direct from Customer |
| 6 | `TaxNumber` | `string?` | Direct from Customer |
| 7 | `Notes` | `string?` | Direct from Customer |
| 8 | `AccountId` | `int` | Mandatory FK to Account |
| 9 | `AccountName` | `string?` | Read from Account.NameAr |
| 10 | `CategoryId` | `int?` | FK to AccountCategories |
| 11 | `CategoryName` | `string?` | Read from Category NameAr |
| 12 | `CreditLimit` | `decimal` | 0 = no limit |
| 13 | `IsActive` | `bool` | Soft-delete flag |
| 14 | `CreatedAt` | `DateTime` | BaseEntity |

### 4.3 CustomerBalanceReportDto (New — for Reports)

| # | Field | Type | Source |
|---|-------|------|--------|
| 1 | `CustomerId` | `int` | Customer.Id |
| 2 | `CustomerName` | `string` | Customer.Name |
| 3 | `AccountId` | `int` | Customer.AccountId |
| 4 | `AccountCode` | `string` | Account.AccountCode |
| 5 | `CurrentBalance` | `decimal` | Sum of JournalEntryLines (Debit - Credit) |
| 6 | `CreditLimit` | `decimal` | Customer.CreditLimit |
| 7 | `AvailableCredit` | `decimal` | CreditLimit - CurrentBalance (floored to 0) |
| 8 | `TotalSales` | `decimal` | Sum of posted SalesInvoice TotalAmount |
| 9 | `TotalPayments` | `decimal` | Sum of CustomerPayment amounts |
| 10 | `LastTransactionDate` | `DateTime?` | Max of JournalEntry.CreatedAt |

### 4.4 CustomerAgingReportDto (New — for Reports)

| # | Field | Type | Source |
|---|-------|------|--------|
| 1 | `CustomerId` | `int` | Customer.Id |
| 2 | `CustomerName` | `string` | Customer.Name |
| 3 | `CurrentBalance` | `decimal` | From Account balance |
| 4 | `Current0_30` | `decimal` | Unpaid invoices 0-30 days |
| 5 | `Current31_60` | `decimal` | Unpaid invoices 31-60 days |
| 6 | `Current61_90` | `decimal` | Unpaid invoices 61-90 days |
| 7 | `Current91Plus` | `decimal` | Unpaid invoices 91+ days |
| 8 | `TotalDue` | `decimal` | Sum of all aging buckets |

---

## 5. Gap Analysis — Existing vs Target

### 5.1 Domain Entities

| Component | Status | Action |
|-----------|--------|--------|
| Customer entity with PartyId | ✅ Exists | ❌ Remove PartyId |
| Customer with direct Name, Phone, Email, Address | ❌ On Party entity | ⬜ Move fields to Customer |
| Customer with direct TaxNumber, Notes | ❌ On Party entity | ⬜ Move fields to Customer |
| AccountId FK (mandatory, non-nullable) | ✅ Already exists | Keep — auto-creation under "1103" |
| CategoryId FK (optional) | ✅ Already exists | Keep |
| CreditLimit field | ✅ Already exists | Keep |
| CheckCreditLimit() domain method | ✅ Already exists | Keep — returns bool (non-throwing) |
| CustomerGroup entity | ❌ Already absent | NOT IN V1 |
| CustomerType enum | ❌ Already absent | NOT IN V1 |
| Party entity (for Customer) | ✅ Exists | ❌ Remove Customer's dependency on Party (keep entity for Supplier/Employee) |

### 5.2 Infrastructure

| Component | Status | Action |
|-----------|--------|--------|
| CustomerConfiguration (Fluent API) | ✅ Exists | Remove PartyId FK, add direct field configs |
| PartyConfiguration | ✅ Exists | No change (Supplier/Employee still use Party) |
| DbSeeder — default customer name | ✅ "عميل نقدي" | Keep as-is |
| DbSeeder — CustomerGroup seeds | ❌ Already absent | NOT IN V1 |
| DbSeeder — CustomerType seeds | ❌ Already absent | NOT IN V1 |
| Migration — PartyId removal + direct fields | ❌ Missing | ⬜ NEW migration |

### 5.3 Application Services

| Component | Status | Action |
|-----------|--------|--------|
| ICustomerService — 6 CRUD methods | ✅ Exists | Update signatures |
| CustomerService — CRUD implementation | ✅ Exists | Remove Party creation, add Account auto-creation, fix mapping |
| CustomerService — Account auto-creation | ❌ Missing | ⬜ Add auto-creation under parent "1103" |
| Credit limit check in SalesService | ❌ Missing | ⬜ Add CheckCreditLimitAsync |
| Customer reports (balance, aging) | ❌ Missing | ⬜ Add report services |
| CustomerService — fix GetAllAsync sort order | ❌ Sorted A-Z | ⬜ Change to Id desc (RULE-220) |

### 5.4 API Layer

| Component | Status | Action |
|-----------|--------|--------|
| CustomersController — 6 endpoints | ✅ Exists | Minor updates only |
| Customer reports endpoints | ❌ Missing | ⬜ Add balance + aging endpoints |
| CustomerGroupsController | ❌ Already absent | NOT IN V1 |
| FluentValidators — current fields | ✅ Exists | Add Phone regex, Notes validation |
| FluentValidators — CustomerGroup | ❌ Already absent | NOT IN V1 |

### 5.5 Desktop Layer

| Component | Status | Action |
|-----------|--------|--------|
| CustomerListViewModel (371 lines) | ✅ Exists | Fix async patterns, remove Party references |
| CustomerEditorViewModel (244 lines) | ✅ Exists | Remove PartyId, add direct fields + Notes |
| CustomerSelectionViewModel (133 lines) | ✅ Exists | Fix async patterns + empty catch |
| CustomersListView.xaml (270 lines) | ✅ Exists | Remove Party references, add Notes column |
| CustomerEditorView.xaml (185 lines) | ✅ Exists | Remove PartyId, add Notes field, fix FontSize=20 |
| CustomerApiService (60 lines) | ✅ Exists | Add report methods |
| ICustomerApiService (in IApiService.cs) | ✅ Exists | Add report method signatures |
| CustomerGroup VMs/Views | ❌ Already absent | NOT IN V1 |

### 5.6 Validation & Business Rules

| Rule | Status | Action |
|------|--------|--------|
| Customer.Name required | ✅ Exists | Arabic message: "اسم العميل مطلوب" |
| Phone regex pattern `^05\d{8}$` | ❌ Missing | ⬜ ADD |
| Email format validation | ✅ Exists | Keep `.EmailAddress()` |
| Notes max length 500 | ❌ Missing | ⬜ ADD |
| CreditLimit ≥ 0 | ❌ Missing in validator | ⬜ ADD (exists in domain guard) |
| CreditLimit enforcement during sales | ❌ Missing | ⬜ ADD pre-transaction check in SalesService |
| TaxNumber uniqueness | ❌ Missing | ⬜ ADD unique index (optional) |
| Account auto-creation on create | ❌ Missing | ⬜ ADD in CustomerService |
| No AccountId in requests (auto-created) | ✅ Already absent | Keep |

---

## 6. Architectural Decisions

### 6.1 Direct Contact Fields (NOT Party Entity)

**Decision**: Customer contact fields (Name, Phone, Email, Address, TaxNumber, Notes) are stored **directly on the Customer entity** — NOT on a shared Party table.

**Rationale** (from accounts Details.md):
```
✅ حذف Parties → Contact fields directly on Customer entity
✅ جعل Customers, Suppliers, Employees جداول مستقلة
```

**Architectural Pattern** (from `accounts summry.md` — Source of Truth):
```
الكيان التشغيلي (Customer) هو المالك لبياناته.
الحساب المحاسبي (Account) هو انعكاس محاسبي له.

Customer → Account (mirrors Name only)
Phone/Email/Address → NOT synced to Account (Account has no such fields)
Name change → synced to Account.Name
IsActive change → synced to Account.IsActive
```

| Option | Pros | Cons |
|--------|------|------|
| **Direct fields on Customer** ✅ | Simple queries, no joins, direct API mapping | Duplication if fields shared with Supplier |
| Shared Party entity (current) | Shared contact data model | Complex queries, extra FK, unnecessary abstraction |
| Separate shared base class | Code reuse without FK | Over-engineering for this scope |

**Why not keep Party**: The Party entity adds unnecessary complexity (extra FK, extra join, extra CRUD) with minimal benefit. Customer and Supplier don't actually share contact data at runtime — they just have similar field shapes. Direct fields are simpler, faster, and match the accounts Details.md analysis.

**⚠️ Impact on Supplier/Employee**: The Party entity will remain for Supplier and Employee until their respective phases migrate to direct fields. This plan only covers Customer.

### 6.2 AccountId: MANDATORY, Auto-Created, NOT User-Supplied

**Decision**: `AccountId` is a mandatory non-nullable `int` FK to Account. The service auto-creates a Level-4 detail account under parent code "1103" (Accounts Receivable/العملاء) when creating a customer. AccountId is NEVER accepted from the user in Create/Update requests.

**Key design points**:
- Parent account code: **"1103"** (NOT "1210" — RULE-504)
- Account level: 4 (detail/leaf — allowTransactions: true)
- Account code: auto-generated hierarchically via `AccountCodeGeneratorService` (1131, 1132, 1133... — Phase 18 hierarchical expanding numbering)
- Account name: matches Customer.Name in both Arabic and English
- Nature: **1** (Asset — Accounts Receivable) — byte value, not enum class
- Color: auto-generated from ``IAccountCodeGeneratorService.GetColorCode(nature: 1)` → `#2196F3` (Asset blue — actual codebase value)
- Wrapped in `ExecuteTransactionAsync()` — atomic with customer creation
- If parent "1103" doesn't exist (Phase 22 not deployed), return clear error message

**Why mandatory (not nullable)**:
- Every customer needs a balance tracking mechanism
- Account-based reports (aging, balance, transaction history) require the link
- Journal entries for payments and invoices reference the account
- Without an Account, customer credit limit enforcement is impossible

**Why not user-supplied**:
- Account code numbering must be system-controlled for integrity
- Users should not choose which AR sub-account to use
- Prevents accidental linking to wrong account types (e.g., expense accounts)

### 6.3 NO CustomerType (Payment Per-Invoice)

**Decision**: CustomerType is **NOT in V1**. The payment classification (Cash/Credit) is determined per-invoice via `SalesInvoice.PaymentType`.

**Rationale**:
- A customer may pay cash for one invoice and credit for another
- Per-customer type is a false constraint — it's a business flow choice, not a customer attribute
- Per-invoice PaymentType already exists on SalesInvoice entity
- AGENTS.md §3 already defines `PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }` on SalesInvoice

**What this means for the module**:
- No CustomerType enum
- No CustomerType field on Customer entity
- No CustomerType in DTOs, requests, or responses
- No CustomerType in validators
- No CustomerType in UI (no radio buttons, no dropdown)

### 6.4 NO CustomerGroup in V1

**Decision**: CustomerGroup is **NOT in V1**. No grouping entity, service, controller, DTO, or UI.

**Rationale** (from accounts Details.md):
- CustomerGroup was originally planned but determined to be out of V1 scope
- No existing CustomerGroup code in the codebase
- Deferred to a future phase if needed for reporting

**What this means for the module**:
- No CustomerGroup entity
- No CustomerGroupDto
- No CustomerGroupService / ICustomerGroupService
- No CustomerGroupsController
- No CustomerGroup API endpoints
- No CustomerGroup VMs or Views
- No CustomerGroup seed data
- No CustomerGroupId FK on Customer

### 6.5 Balance: Via JournalEntryLines (NOT on Customer)

**Decision**: Customer balance is computed from the linked Account's JournalEntryLines — NOT stored on Customer entity.

**Rationale** (from accounts Details.md):
```
✅ الرصيد الحقيقي يأتي من JournalEntryLines وليس من جدول العميل
```

- No `OpeningBalance` or `CurrentBalance` on Customer entity (already correct)
- Balance = `SUM(Debit) - SUM(Credit)` from `JournalEntryLine` where `AccountId = Customer.AccountId`
- Balance is computed when needed for reports (not cached on entity)
- CustomerPayment and SalesInvoice create journal entries that affect this balance

### 6.6 CreditLimit Enforcement (Soft Warning Only)

**Decision**: Credit limit is a **soft warning** — `CheckCreditLimit()` returns `bool` (never throws). The caller decides whether to block.

**Flow**:
1. `SalesService.PostAsync()` calls `CheckCreditLimitAsync(customerId, invoice.TotalAmount)`
2. Inside: loads customer, computes projected balance via Account, calls `customer.CheckCreditLimit(projectedBalance)`
3. If exceeds limit → log warning (RULE-183), return `Result.Failure` with Arabic message
4. Admin users can override in future phases ("Force Post" permission)

### 6.7 CategoryId: Optional Classification (Keep)

**Decision**: `CategoryId` (int?, FK → AccountCategories) is kept for customer classification — already exists in code.

- Optional field for grouping customers by type (e.g., Retail, Wholesale, VIP)
- Distinguishable from CustomerGroup (which is removed) — CategoryId links to AccountCategories which is a general classification system
- Not required for V1 core functionality
- Displayed in CustomerDto and CustomerEditorView if populated

### 6.8 Why Not A Combined "Customers + Suppliers" Module

**Decision**: Customers and Suppliers remain fully separate modules (no shared "Parties" approach).

**Rationale** (from accounts Details.md):
- Different behavior: Customers have credit limits (due to us), suppliers have credit limits (we owe them)
- Different permissions: Cashiers can view customers but not suppliers (AGENTS.md §6)
- Different reports: Customer aging vs. Supplier aging
- Different COA parents: Customers under "1103" (AR), Suppliers under "2101" (AP)
- Existing codebase: Both modules already exist and are well-separated

---

## 7. Non-V1 Items (Deferred)

These customer features are explicitly **deferred** to future phases or **excluded** from V1:

| Feature | Reason | Proposed Phase |
|---------|--------|----------------|
| CustomerGroup | Removed from V1 per accounts Details.md | 🚫 Not planned |
| CustomerType | Payment type is per-invoice (SalesInvoice.PaymentType) | 🚫 Not planned |
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
| Default Cash Customer setting (`DefaultCashCustomerId`) | Already exists as key-value setting | ✅ Already exists |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), Arabic ToolTips (RULE-185-190), UI Compact styles (RULE-262-274), and INotifyDataErrorInfo validation (RULE-228).

---

### Task 1 — Remove PartyId from Customer Entity, Add Direct Contact Fields

**Description**: Remove the `PartyId` FK from Customer entity. Add all contact fields (Name, Phone, Email, Address, TaxNumber, Notes) **directly** on Customer. Update the `Create()` and `Update()` factory methods to accept direct contact fields instead of a PartyId.

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Customer.cs` | Remove `PartyId`, `Party` nav property. Add `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes` direct fields. Update `Create()` signature. Update `Update()` signature. |
| `Infrastructure/Data/Configurations/CustomerConfiguration.cs` | Remove PartyId FK config. Add direct field configs: `Name` (HasMaxLength 150, IsRequired), `Phone` (HasMaxLength 20), `Email` (HasMaxLength 100), `Address` (HasMaxLength 250), `TaxNumber` (HasMaxLength 30), `Notes` (HasMaxLength 500). Add `HasIndex(c => c.TaxNumber).IsUnique().HasFilter("[TaxNumber] IS NOT NULL")` |
| `Infrastructure/Data/Migrations/` | NEW migration: `DROP FK_Customers_Parties_PartyId`, `DROP COLUMN PartyId`, `ADD Name nvarchar(150) NOT NULL`, `ADD Phone nvarchar(20) NULL`, `ADD Email nvarchar(100) NULL`, `ADD Address nvarchar(250) NULL`, `ADD TaxNumber nvarchar(30) NULL`, `ADD Notes nvarchar(500) NULL`. If production data exists, include data migration: `UPDATE Customers SET Name = p.Name, Phone = p.Phone, ... FROM Parties p WHERE Customers.PartyId = p.Id` |

**Domain entity changes**:

```csharp
// REMOVED:
// public int PartyId { get; private set; }
// public virtual Party Party { get; private set; } = null!;

// ADDED:
public string Name { get; private set; } = string.Empty;
public string? Phone { get; private set; }
public string? Email { get; private set; }
public string? Address { get; private set; }
public string? TaxNumber { get; private set; }
public string? Notes { get; private set; }

// Updated Create() — NO more partyId parameter:
public static Customer Create(
    string name,                    // NEW — direct
    string? phone,                  // NEW — direct
    string? email,                  // NEW — direct
    string? address,                // NEW — direct
    string? taxNumber,              // NEW — direct
    string? notes,                  // NEW — direct
    int accountId,                  // Mandatory
    decimal creditLimit = 0,
    int? categoryId = null,
    int? createdByUserId = null)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new DomainException("اسم العميل مطلوب.");
    // ... guard clauses for other fields ...

    return new Customer
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
        CreatedAt = DateTime.UtcNow
    };
}

// Updated Update() — now accepts contact fields:
public void Update(
    string name,
    string? phone = null,
    string? email = null,
    string? address = null,
    string? taxNumber = null,
    string? notes = null,
    decimal creditLimit = 0,
    int? categoryId = null,
    int? updatedByUserId = null)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new DomainException("اسم العميل مطلوب.");
    if (creditLimit < 0)
        throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

    Name = name.Trim();
    Phone = phone?.Trim();
    Email = email?.Trim();
    Address = address?.Trim();
    TaxNumber = taxNumber?.Trim();
    Notes = notes?.Trim();
    CreditLimit = creditLimit;
    if (categoryId.HasValue)
        CategoryId = categoryId;
    SetUpdatedBy(updatedByUserId);
    UpdateTimestamp();
}
```

**Logging**: `Log.Information("Customer {CustomerId} contact fields updated: Name={Name}, Phone={Phone}", id, name, maskedPhone)`

**Validation**: All field guards in domain entity with Arabic messages:
- `"اسم العميل مطلوب"`
- `"رقم الجوال يجب أن يتكون من 10 أرقام ويبدأ بـ 05"`
- `"البريد الإلكتروني غير صالح"`
- `"الحد الائتماني لا يمكن أن يكون سالباً"`

**Estimate**: ~2 hours

---

### Task 2 — Add Account Auto-Creation Under Parent "1103" in CustomerService

**Description**: When `CustomerService.CreateAsync()` is called, auto-create a Level-4 detail account under parent account code "1103" (Accounts Receivable/العملاء). The AccountId is NEVER supplied by the user — it is always system-generated.

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ICustomerService.cs` | Add `AutoCreateCustomerAccountAsync(int customerId, int userId, CancellationToken ct)` — returns `Result<int>` (AccountId) |
| `Application/Services/CustomerService.cs` | Add `AutoCreateCustomerAccountAsync()` implementation. Update `CreateAsync()` to call account creation as part of the transactional flow. |
| `Contracts/DTOs/AllDtos.cs` — `CustomerDto` | Ensure `AccountId` and `AccountName` are included in response after creation |
| `Contracts/Requests/CustomerRequests.cs` | Ensure NO `AccountId` field in Create/Update — it is system-generated |

**Auto-creation logic** (in CustomerService.CreateAsync — aligned with Phase 18 Account.Create 13-param signature):
```csharp
// Step 1: Create Customer entity (no AccountId yet)
var customer = Customer.Create(
    name: request.Name,
    phone: request.Phone,
    email: request.Email,
    address: request.Address,
    taxNumber: request.TaxNumber,
    notes: request.Notes,
    accountId: 0,  // Placeholder — will update after account creation
    creditLimit: request.CreditLimit,
    categoryId: request.CategoryId,
    createdByUserId: userId
);
await _uow.Customers.AddAsync(customer, ct);
await _uow.SaveChangesAsync(ct);  // Get customer.Id

// Step 2: Auto-create Account under parent "1103"
var result = await AutoCreateCustomerAccountAsync(customer.Id, userId, ct);

// Step 3: Update Customer with AccountId
customer.SetAccountId(result.Value);
await _uow.SaveChangesAsync(ct);
```

**AutoCreateCustomerAccountAsync** (aligned with Phase 18 Account.Create — 13 params, no `allowTransactions`):
```csharp
public async Task<Result<int>> AutoCreateCustomerAccountAsync(
    int customerId, int userId, CancellationToken ct)
{
    var customer = await _uow.Customers.GetByIdAsync(customerId, ct);
    if (customer == null)
        return Result<int>.Failure("العميل غير موجود", ErrorCodes.NotFound);
    if (customer.AccountId > 0)
        return Result<int>.Failure("العميل لديه حساب محاسبي بالفعل");

    var parentAccount = await _uow.Accounts.GetByCodeAsync("1103", ct);
    if (parentAccount == null)
        return Result<int>.Failure(
            "الحساب الرئيسي 1103 (العملاء) غير موجود. تأكد من اكتمال مرحلة دليل الحسابات.");

    // Phase 18: AccountCodeGeneratorService generates hierarchical codes (SemaphoreSlim thread-safe)
    var newCode = await _accountCodeGenerator.GenerateCodeAsync(
        parentAccount.Id, level: 4, ct);

    // Phase 18: ColorCode auto-generated from Nature via IAccountCodeGeneratorService
    var colorCode = IAccountCodeGeneratorService.GetColorCode(parentAccount.Nature);

    // Phase 18: Account.Create() 13-param signature — NO `allowTransactions` param
    // `isLeaf: true` means AllowTransactions=true (computed property per Phase 18)
    var account = Account.Create(
        accountCode: newCode,
        nameAr: customer.Name,
        nameEn: customer.Name,
        nature: parentAccount.Nature, // byte (1=Asset) inherited from parent account
        isLeaf: true,                 // Level 4 detail account (AllowTransactions computed => IsLeaf)
        parentId: parentAccount.Id,
        isSystem: false,              // Not a system account (user-modifiable)
        categoryId: null,
        level: 4,                     // Detail/leaf level
        description: $"حساب العميل: {customer.Name}",
        colorCode: colorCode,         // Auto-generated from nature, NOT hardcoded
        notes: null,
        createdByUserId: userId
    );
    await _uow.Accounts.AddAsync(account, ct);
    await _uow.SaveChangesAsync(ct);

    return Result<int>.Success(account.Id);
}
```

**⚠️ Phase 18 Alignment Notes**:
- `Account.Create()` has **13 parameters** (Phase 18 Task 1.2) — NO `allowTransactions` param (it's a computed property `=> IsLeaf`)
- `nature` is `byte` (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense) — NOT an enum class
- `ColorCode` is auto-generated from `IAccountCodeGeneratorService.GetColorCode()` — NOT hardcoded
- `AccountCode` is generated by `AccountCodeGeneratorService` using hierarchical expanding numbering (1→11→1101→11010001) — NOT simple increment
- The parent account "1103" is a Level 3 sub-account seeded by `AccountingSeeder` in Phase 22

**⚠️ Dependency**: Phase 22 (Chart of Accounts) must be complete — account "1103" must exist. If Phase 22 is not deployed, return clear error message.

**Logging**:
- `Log.Information("Account auto-created for Customer {CustomerId}: AccountCode={Code}, AccountId={Id}", customerId, newCode, account.Id)`
- `Log.Warning("Cannot auto-create account for customer {Id}: parent account 1103 not found", customerId)`

**Estimate**: ~1.5 hours

---

### Task 3 — Add CreditLimit Validation + SalesService Enforcement

**Description**: Add `CheckCreditLimit()` domain method (already exists in simplified form) and wire it into `SalesService.PostAsync()` as a pre-transaction check. Add `CheckCreditLimitAsync()` to `CustomerService`.

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Customer.cs` | `CheckCreditLimit(decimal additionalAmount)` — Already exists. Enhance to accept `decimal currentBalance` for proper comparison. |
| `Application/Services/CustomerService.cs` | Add `CheckCreditLimitAsync(int customerId, decimal amount, CancellationToken ct)` |
| `Application/Interfaces/Services/ICustomerService.cs` | Add `CheckCreditLimitAsync` signature |
| `Application/Services/SalesService.cs` | Add credit limit check BEFORE `BeginTransactionAsync` in `PostAsync()` |
| `Contracts/DTOs/AllDtos.cs` | Add `CreditLimitCheckResultDto` record |

**Domain method (enhanced)**:
```csharp
/// <summary>
/// Checks whether adding an additional amount would exceed the credit limit.
/// Non-throwing — the caller decides whether to block.
/// </summary>
public bool CheckCreditLimit(decimal currentBalance, decimal additionalAmount)
{
    if (CreditLimit <= 0)
        return true; // No limit set — always allowed
    return (currentBalance + additionalAmount) <= CreditLimit;
}
```

**SalesService integration**:
```csharp
// Inside SalesService.PostAsync(), BEFORE transaction:
if (invoice.PaymentType != PaymentType.Cash && invoice.CustomerId.HasValue)
{
    var creditCheck = await _customerService.CheckCreditLimitAsync(
        invoice.CustomerId.Value, invoice.TotalAmount, ct);
    if (!creditCheck.IsSuccess)
        return Result<SalesInvoiceDto>.Failure(
            $"تجاوز الحد الائتماني للعميل: {creditCheck.Error}", 
            ErrorCodes.CreditLimitExceeded);
}
```

**Logging**:
- `Log.Warning("Credit limit exceeded for Customer {Id}: Current={Current}, Additional={Additional}, Limit={Limit}", ...)` — RULE-183 (user mistake)
- `Log.Information("Credit limit check passed for Customer {Id}: Projected={Projected}, Limit={Limit}", ...)`

**Estimate**: ~1 hour

---

### Task 4 — Update CustomerService CreateAsync/UpdateAsync (Remove Party Logic)

**Description**: Refactor `CustomerService.CreateAsync()` and `UpdateAsync()` to work with direct contact fields instead of Party records. Remove all Party creation/update logic.

**Files**:

| File | Change |
|------|--------|
| `Application/Services/CustomerService.cs` | `CreateAsync()` — No longer creates Party. Creates Customer directly with contact fields + auto-creates Account. `UpdateAsync()` — Updates Customer directly, no Party.Update(). |
| `Application/Services/CustomerService.cs` | `MapToDto()` — Map contact fields from Customer directly (not from Customer.Party) |
| `Application/Services/CustomerService.cs` | `PermanentDeleteAsync()` — No Party FK to check |

**Mapping changes**:
```csharp
// BEFORE (with Party):
private static CustomerDto MapToDto(Customer customer) => new()
{
    Id = customer.Id,
    Name = customer.Party?.Name ?? string.Empty,
    Phone = customer.Party?.Phone,
    Email = customer.Party?.Email,
    // ...
};

// AFTER (direct fields):
private static CustomerDto MapToDto(Customer customer) => new()
{
    Id = customer.Id,
    Name = customer.Name,
    Phone = customer.Phone,
    Email = customer.Email,
    Address = customer.Address,
    TaxNumber = customer.TaxNumber,
    Notes = customer.Notes,
    AccountId = customer.AccountId,
    AccountName = customer.Account?.NameAr,
    CategoryId = customer.CategoryId,
    CreditLimit = customer.CreditLimit,
    IsActive = customer.IsActive,
    CreatedAt = customer.CreatedAt
};
```

**Logging**: `Log.Information("Customer {Id} created with direct contact fields: Name={Name}, AccountId={AccountId}", customer.Id, customer.Name, customer.AccountId)`

**Estimate**: ~1.5 hours

---

### Task 5 — Update Contracts Layer (DTOs, Requests, Responses)

**Description**: Update DTOs to reflect direct contact fields. Remove any Party-related fields. Add Notes. Remove CustomerGroup/CustomerType fields (already absent).

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` — `CustomerDto` | Remove `PartyId`. Add `Notes` (string?). Keep `Name`, `Phone`, `Email`, `Address`, `TaxNumber` as direct fields (not via Party). Remove `PartyName`/`PartyPhone` aliases if any. |
| `Contracts/DTOs/AllDtos.cs` — `CustomerBalanceReportDto` | NEW record |
| `Contracts/DTOs/AllDtos.cs` — `CustomerAgingReportDto` | NEW record |
| `Contracts/DTOs/AllDtos.cs` — `CreditLimitCheckResultDto` | NEW record |
| `Contracts/Requests/CustomerRequests.cs` — `CreateCustomerRequest` | Remove `AccountId` (auto-created). Add `Notes`. Keep existing fields. |
| `Contracts/Requests/CustomerRequests.cs` — `UpdateCustomerRequest` | Remove `AccountId` (cannot change). Add `Notes`. Keep existing fields. |
| `Contracts/Responses/CustomerResponse.cs` | Remove `PartyId`. Add `Notes` (string?). Keep contact fields direct. |

**CustomerDto target**:
```csharp
public record CustomerDto(
    int Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? Notes,
    int AccountId,
    string? AccountName,
    int? CategoryId,
    decimal CreditLimit,
    bool IsActive,
    DateTime CreatedAt
);
```

**Estimate**: ~1 hour

---

### Task 6 — Update FluentValidation for Customer Requests

**Description**: Update validators to add Phone regex, Notes maxlength, and remove any Party-related validation. Remove CustomerGroup/CustomerType validation (none existed, but ensure none added).

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/CustomerRequestValidators.cs` | Add Phone regex, Notes maxlength, Name required Arabic message, CreditLimit range |

**Validator additions**:
```csharp
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MaximumLength(150).WithMessage("اسم العميل يجب أن لا يتجاوز 150 حرفاً");

        RuleFor(x => x.Phone)
            .Matches("^05\\d{8}$").WithMessage("رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(250).WithMessage("العنوان يجب أن لا يتجاوز 250 حرفاً");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(30).WithMessage("الرقم الضريبي يجب أن لا يتجاوز 30 حرفاً");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الائتماني لا يمكن أن يكون سالباً");
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    // Same rules as Create
}
```

**Estimate**: ~30 minutes

---

### Task 7 — Update CustomerEditorViewModel (Remove Party Logic, Add Direct Fields + Notes)

**Description**: Refactor CustomerEditorViewModel to work with direct contact fields instead of Party. Add Notes property. Remove any CustomerGroup/CustomerType-related properties.

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerEditorViewModel.cs` | Replace Party-based loading/saving with direct field operations. Add `Notes` property. Remove `PartyId`. Remove `CustomerGroupId`/`CustomerType` stubs if any. |

**Property changes**:
- Keep: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `CreditLimit`, `CategoryId`, `SelectedCategory`
- Add: `Notes` (string?, with INotifyDataErrorInfo validation)
- Remove: `PartyId` (was kept in VM)
- Remove: Any `CustomerGroupId`, `CustomerGroupList`, `SelectedCustomerGroup` if referenced
- Remove: Any `CustomerType`, `SelectedCustomerType` if referenced

**SaveAsync flow change**:
```csharp
// BEFORE: Create Party → Create Customer with PartyId
// AFTER: Create Customer directly with contact fields + auto-create Account

private async Task SaveAsync()
{
    if (!Validate()) return;
    await ExecuteAsync(async () =>
    {
        var request = new CreateCustomerRequest
        {
            Name = Name,
            Phone = Phone,
            Email = Email,
            Address = Address,
            TaxNumber = TaxNumber,
            Notes = Notes,
            CreditLimit = CreditLimit,
            CategoryId = SelectedCategory?.Key
            // NO AccountId — auto-created by service
        };
        var result = await _customerApiService.CreateAsync(request);
        // ...
    });
}
```

**Logging**: `LogSystemError("فشل حفظ العميل", "CustomerEditorViewModel.SaveAsync", ex)`

**Arabic ToolTips (new)**:
- Name: `"اسم العميل — إلزامي"`
- Phone: `"رقم الجوال — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام"`
- Email: `"البريد الإلكتروني — اختياري"`
- Address: `"العنوان — اختياري"`
- TaxNumber: `"الرقم الضريبي — اختياري وفريد"`
- Notes: `"ملاحظات إضافية عن العميل"`
- Save button: `"حفظ بيانات العميل — سيتم إنشاء حساب محاسبي تلقائياً"`

**Estimate**: ~1.5 hours

---

### Task 8 — Update CustomerEditorView.xaml (Remove Party Fields, Add Notes + Account Display)

**Description**: Update editor XAML to show direct contact fields, remove Party-related controls, add Notes multiline input, fix FontSize=20 violation, add loading overlay.

**Files**:

| File | Change |
|------|--------|
| `Views/Customers/CustomerEditorView.xaml` | Replace any Party-related template with direct fields. Add Notes TextBox (multiline). Add AccountName display (read-only). Fix FontSize="20" → "16". Add loading overlay. |

**XAML layout (target)**:

```xml
<!-- Account Info (Read-Only Display) -->
<Border Style="{StaticResource SectionBorder}">
    <Grid>
        <TextBlock Text="معلومات الحساب المحاسبي" Style="{StaticResource SectionHeaderStyle}"/>
        <StackPanel Grid.Row="1" Margin="0,6,0,0">
            <TextBlock Text="رقم الحساب: يتم إنشاؤه تلقائياً" 
                       Style="{StaticResource ReadOnlyFieldStyle}"
                       ToolTip="الحساب المحاسبي للعميل — يتم إنشاؤه تلقائياً تحت 1103"/>
            <TextBlock Text="{Binding AccountName, StringFormat='اسم الحساب: {0}'}"
                       Visibility="{Binding AccountName, Converter={StaticResource StringNotEmptyToVisibility}}"
                       Style="{StaticResource ReadOnlyFieldStyle}"/>
        </StackPanel>
    </Grid>
</Border>

<!-- Contact Information (Direct Fields) -->
<Border Style="{StaticResource SectionBorder}">
    <Grid>
        <TextBlock Text="معلومات الاتصال" Style="{StaticResource SectionHeaderStyle}"/>
        <StackPanel Grid.Row="1" Margin="0,6,0,0">
            <!-- Name -->
            <TextBlock Text="اسم العميل *" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="اسم العميل — إلزامي"/>
            
            <!-- Phone -->
            <TextBlock Text="رقم الجوال" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="رقم الجوال — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام"/>

            <!-- Email -->
            <TextBlock Text="البريد الإلكتروني" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="البريد الإلكتروني — اختياري"/>

            <!-- Address -->
            <TextBlock Text="العنوان" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <TextBox Text="{Binding Address, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="العنوان — اختياري"/>

            <!-- TaxNumber -->
            <TextBlock Text="الرقم الضريبي" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <TextBox Text="{Binding TaxNumber, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="الرقم الضريبي — اختياري وفريد لكل عميل"/>

            <!-- Notes -->
            <TextBlock Text="ملاحظات" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <TextBox Text="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" TextWrapping="Wrap" MaxHeight="100"
                     ToolTip="ملاحظات إضافية — اختيارية"/>
        </StackPanel>
    </Grid>
</Border>

<!-- Financial Info -->
<Border Style="{StaticResource SectionBorder}">
    <Grid>
        <TextBlock Text="المعلومات المالية" Style="{StaticResource SectionHeaderStyle}"/>
        <StackPanel Grid.Row="1" Margin="0,6,0,0">
            <!-- CreditLimit -->
            <TextBlock Text="الحد الائتماني" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding CreditLimit, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="الحد الائتماني — صفر يعني لا يوجد حد"/>
            
            <!-- Category -->
            <TextBlock Text="التصنيف" Style="{StaticResource LabelStyle}" Margin="0,6,0,0"/>
            <ComboBox ItemsSource="{Binding CategoryOptions}"
                      SelectedValuePath="Key" DisplayMemberPath="Value"
                      SelectedValue="{Binding SelectedCategoryId}"
                      Style="{StaticResource ModernComboBox}"
                      ToolTip="تصنيف العميل — اختياري"/>
        </StackPanel>
    </Grid>
</Border>

<!-- Loading Overlay -->
<Border Grid.RowSpan="10" 
        Background="#80FFFFFF"
        Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibility}}"
        Panel.ZIndex="1000">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <ProgressBar IsIndeterminate="True" Width="200"/>
        <TextBlock Text="جاري المعالجة..." Margin="0,8,0,0" 
                   HorizontalAlignment="Center"/>
    </StackPanel>
</Border>
```

**FontSize fix**: `FontSize="20"` on line 28 → change to `FontSize="16"`

**Estimate**: ~1.5 hours

---

### Task 9 — Update CustomerListViewModel + SelectionViewModel (Remove Party + Fix Async Patterns)

**Description**: Fix async pattern violations (RULE-141), remove CanExecute predicates (RULE-059), remove Party references. Add ErrorMessage bar.

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Customers/CustomerListViewModel.cs` | Refactor 3 methods to `ExecuteAsync()` wrapper: `LoadCustomersAsync`, `DeleteCustomerAsync`, `RestoreCustomerAsync`. Remove CanExecute predicates. Remove Party.Name references. Add ErrorMessage property + display. |
| `ViewModels/Customers/CustomerSelectionViewModel.cs` | Refactor `LoadCustomersAsync` to `ExecuteAsync()` wrapper. Fix empty catch block. Remove CanExecute predicates. Remove Party.Name references. |

**Key changes in CustomerListViewModel**:
```csharp
// BEFORE (with Party):
private async Task LoadCustomersAsync()
{
    IsBusy = true;
    try
    {
        var result = await _customerApiService.GetAllAsync(SearchText, IncludeInactive);
        if (result.IsSuccess)
            Customers = new ObservableCollection<CustomerDto>(result.Value!.Items);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error loading customers");
    }
    finally
    {
        IsBusy = false;
    }
}

// AFTER (clean operation method):
private async Task LoadCustomersOperationAsync()
{
    ErrorMessage = null;
    var result = await _customerApiService.GetAllAsync(SearchText, IncludeInactive);
    if (result.IsSuccess && result.Value != null)
        Customers = new ObservableCollection<CustomerDto>(result.Value.Items);
    else
        ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العملاء", "LoadCustomers");
}

// Commands use ExecuteAsync():
RefreshCommand = new AsyncRelayCommand(
    async () => await ExecuteAsync(LoadCustomersOperationAsync));
```

**Remove CanExecute predicates**:
```csharp
// BEFORE:
EditCommand = new AsyncRelayCommand(EditCustomerAsync, () => SelectedCustomer != null);
DeleteCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null);

// AFTER (always enabled — RULE-059):
EditCommand = new AsyncRelayCommand(EditCustomerAsync);
DeleteCommand = new AsyncRelayCommand(DeleteCustomerAsync);
```

**Add ErrorMessage bar to XAML** (between header and DataGrid):
```xml
<Border Grid.Row="2" Background="{StaticResource ErrorBackground}"
        Visibility="{Binding ErrorMessage, Converter={StaticResource StringNotEmptyToVisibility}}"
        Padding="8,4">
    <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}"/>
</Border>
```

**Estimate**: ~2 hours

---

### Task 10 — Update CustomersListView.xaml (Remove Party Columns)

**Description**: Update the list view XAML to remove any Party-related columns. Keep direct contact fields. Add ErrorMessage bar. Fix ToolTips.

**Files**:

| File | Change |
|------|--------|
| `Views/Customers/CustomersListView.xaml` | Remove Party references. Add ErrorMessage bar. Verify ToolTips use customer-specific Arabic text. |

**DataGrid column updates**:
- Remove: Any `Party.Name` bound columns (should be `Name` direct)
- Keep: `Name`, `Phone`, `Email`, `Address`, `CreditLimit`, `AccountName` columns
- Add: `Notes` column (truncated) if helpful
- Ensure: `AccountName` column shows "حساب: {AccountName}" format

**ToolTip fixes**:
- Search box: `"البحث في العملاء بالاسم أو رقم الجوال"`
- IncludeInactive checkbox: `"عرض العملاء غير النشطين — الذين تم حذفهم سابقاً"`
- Edit button: `"تعديل بيانات العميل المحدد"`
- Delete button: `"حذف أو إلغاء تنشيط العميل المحدد"`
- Restore button: `"استعادة العميل المحذوف"`
- Refresh button: `"تحديث قائمة العملاء"`

**Estimate**: ~1 hour

---

### Task 11 — Customer Reports Endpoints (Balance + Aging)

**Description**: Create balance report and aging report endpoints using JournalEntryLines for balance computation (not stored Customer balance).

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ICustomerReportService.cs` | NEW — interface with `GetBalanceReportAsync`, `GetAgingReportAsync` |
| `Application/Services/CustomerReportService.cs` | NEW — implementation queries JournalEntryLines sum per AccountId |
| `Contracts/DTOs/AllDtos.cs` | Add `CustomerBalanceReportDto`, `CustomerAgingReportDto` |
| `Api/Controllers/CustomersController.cs` | Add 2 new report endpoints |
| `Desktop/Services/Api/CustomerApiService.cs` | Add `GetBalanceReportAsync`, `GetAgingReportAsync` |

**Balance report query pattern**:
```csharp
// Balance = SUM(Debit) - SUM(Credit) for the account
var balance = await _uow.JournalEntryLines
    .Where(jel => jel.AccountId == customer.AccountId && jel.JournalEntry.Status == JournalEntryStatus.Posted)
    .GroupBy(jel => jel.AccountId)
    .Select(g => new { Balance = g.Sum(jel => jel.Debit - jel.Credit) })
    .FirstOrDefaultAsync(ct);
```

**Aging report query**: Group unpaid SalesInvoice amounts by DueDate range for posted invoices with DueAmount > 0.

**Logging**: `Log.Information("Customer balance report generated for {CustomerId}", id)`

**Estimate**: ~2 hours

---

### Task 12 — End-to-End Integration + Desktop Navigation

**Description**: Wire up all changes. Update DI registrations for new/removed services.

**Files**:

| File | Change |
|------|--------|
| `Desktop/App.xaml.cs` | NO CustomerGroup registrations (removed from V1). Ensure `CustomerApiService`, `CustomerReportService` are registered. |
| `Desktop/MainWindow.xaml` | NO "مجموعات العملاء" menu item. Keep existing Customers navigation entry. Add Customer Reports menu if needed. |

**DI registration changes**:
```csharp
// REMOVE (CustomerGroup NOT in V1):
// services.AddTransient<ICustomerGroupApiService, CustomerGroupApiService>();
// services.AddTransient<CustomerGroupListViewModel>();
// services.AddTransient<CustomerGroupEditorViewModel>();

// KEEP (Customer core):
services.AddTransient<ICustomerApiService, CustomerApiService>();
services.AddTransient<CustomerListViewModel>();
services.AddTransient<CustomerEditorViewModel>();
services.AddTransient<CustomerSelectionViewModel>();
```

**Estimate**: ~30 minutes

---

### Task 13 — Unit Tests

**Files**: Updated test files in `SalesSystem.Domain.Tests`, `SalesSystem.Application.Tests`, `SalesSystem.Api.Tests`

#### 1. Domain Entity Tests

**Customer.Create()** (new signature with direct fields):
- Valid inputs → creates entity with all fields set correctly
- Empty `name` → `DomainException("اسم العميل مطلوب")`
- `name` with whitespace → trimmed correctly
- `CreditLimit < 0` → `DomainException("الحد الائتماني لا يمكن أن يكون سالباً")`
- `Phone` with valid Saudi format (05xxxxxxxx) → accepted
- `Phone` with invalid format → accepted at domain level (validated at FluentValidation layer)
- `Email` with invalid format → accepted at domain level
- Null optional fields → stored as null
- `AccountId <= 0` → DomainException

**Customer.Update()** (new signature with direct fields):
- Valid update modifies all fields
- Empty name → DomainException
- `CreditLimit < 0` → DomainException
- `Notes` updated correctly
- `UpdateTimestamp()` called after update
- Partial update (null phone) keeps existing phone

**Customer.CheckCreditLimit()**:
- `CreditLimit = 0` (no limit) → always returns `true`
- `CreditLimit > 0` with projected balance <= limit → returns `true`
- `CreditLimit > 0` with projected balance > limit → returns `false`
- Negative additional amount → still handles gracefully

#### 2. Service Tests (using Mock<IUnitOfWork>)

**CustomerService.CreateAsync()**:
- Valid request with all fields → `Result<CustomerDto>.Success`; Account auto-created under "1103"
- Valid request with minimal fields → `Result<CustomerDto>.Success`
- Duplicate TaxNumber (unique index violation caught) → `Result<CustomerDto>.Failure`
- Parent account "1103" not found → `Result.Failure` with Arabic message
- Transaction rollback if account creation succeeds but customer save fails

**CustomerService.UpdateAsync()**:
- Valid update → `Result<CustomerDto>.Success` with updated fields
- Non-existent customer → `Result.Failure` with `ErrorCodes.NotFound`
- Update with same TaxNumber as another customer → `Result.Failure`

**CustomerService.CheckCreditLimitAsync()**:
- Within limit → `Result.Success`
- Exceeds limit → `Result.Failure` with Arabic message
- Customer not found → `Result.Failure` with NotFound
- `CreditLimit = 0` (no limit) → `Result.Success`

**CustomerService.DeleteAsync()** / **PermanentDeleteAsync()**:
- Soft delete → `Result.Success`
- Hard delete with FK violation (has sales invoices) → `Result.Failure` with FK error caught (RULE-200)

**CustomerService.GetAllAsync()**:
- Returns items sorted by Id descending (RULE-220)
- `includeInactive = true` → returns active + inactive
- `includeInactive = false` → returns active only

#### 3. FluentValidation Tests

**CreateCustomerRequestValidator**:
- Valid request passes (all fields correct)
- Empty Name → fails with "اسم العميل مطلوب"
- Name > 150 chars → fails
- Phone invalid format (not starting with 05) → fails
- Phone > 20 chars → fails
- Invalid Email format → fails
- Address > 250 chars → fails
- TaxNumber > 30 chars → fails
- Notes > 500 chars → fails
- CreditLimit < 0 → fails
- NO CustomerGroup validation (not in V1)
- NO CustomerType validation (not in V1)
- NO AccountId validation (auto-created)

#### 4. Database Configuration Tests

**CustomerConfiguration**:
- Verify `HasQueryFilter(c => c.IsActive)`
- Verify `Name` has `HasMaxLength(150)` and `IsRequired()`
- Verify `Phone` has `HasMaxLength(20)`
- Verify `Email` has `HasMaxLength(100)`
- Verify `Address` has `HasMaxLength(250)`
- Verify `TaxNumber` has `HasMaxLength(30)` and `HasIndex().IsUnique().HasFilter()`
- Verify `Notes` has `HasMaxLength(500)`
- Verify `AccountId` FK uses `DeleteBehavior.Restrict`
- Verify `HasIndex(c => c.AccountId)`
- Verify `CategoryId` FK uses `DeleteBehavior.Restrict`
- Verify NO PartyId configuration (removed)
- Verify NO CustomerGroupId configuration (never existed)

#### 5. Phase-specific Tests

- Customer.Create() with valid direct fields → no Party created
- Account auto-creation generates hierarchical code via `AccountCodeGeneratorService` (Phase 18: 1→11→1101→11010001)
- Account name matches Customer.Name in Arabic
- Account nature is 1 (Asset — byte value, not enum class)
- Account isLeaf = true (Level 4 detail — AllowTransactions is computed `=> IsLeaf`)
- Account ColorCode auto-generated from nature (#2B579A for Asset)
- Parent account lookup by code "1103" (NOT "1210")
- Auto-creation is transactional — if customer fails, account rolls back
- Balance computed as SUM(Debit - Credit) from JournalEntryLines
- Default customer "عميل نقدي" seeded with direct fields (no Party)
- CheckCreditLimit returns bool (never throws DomainException)

**Estimate**: ~4 hours

---

## 9. Compliance Matrix

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | CreditLimit — `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | Customer Create: Account creation + Customer save in one `ExecuteTransactionAsync` (Phase 28 aligned) | ✅ |
| **RULE-006** | ALL services return `Result<T>` | CustomerService, CustomerReportService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Name, Phone, Email, Address, TaxNumber, Notes — all nvarchar | ✅ |
| **RULE-016** | BaseEntity audit fields | Customer inherits BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | CustomerService | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on CRUD + CreditLimit warnings | ✅ |
| **RULE-036** | Log critical operations | Customer create/update/delete, Account auto-creation, CreditLimit enforcement | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | CustomersController | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Customer.Create, Update, CheckCreditLimit | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateCustomerRequestValidator, UpdateCustomerRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Customer delete dialog: Cancel/Deactivate/Permanent | ✅ |
| **RULE-052** | Guard Clauses on all entities | Customer.Create/Update with all field guards | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | CustomerEditorViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | CustomerEditorViewModel — no CanExecute blocking (Task 9 fix) | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | CustomerListViewModel (FIX — Task 9), CustomerEditorViewModel ✅, CustomerSelectionViewModel (FIX — Task 9) | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Customer editor opens via OpenScreen | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ العميل"`, `"خطأ في تحميل العملاء"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Credit limit exceeded, validation errors, empty results | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | CustomerApiService inherits from ApiServiceBase with guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, combos, inputs across CustomersListView, CustomerEditorView | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "فتح شاشة إضافة عميل جديد" ✅, not "عميل جديد" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Save: "حفظ بيانات العميل — سيتم إنشاء حساب محاسبي تلقائياً" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة العملاء — إنشاء وتعديل بيانات العملاء" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول عميل — ابدأ بتسجيل بيانات العملاء" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-191** | Customer — NO Code column | Customer entity has no Code column ✅ | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | CustomerService.PermanentDeleteAsync catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | CustomerService, CustomerReportService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | CustomersController — service only | ✅ |
| **RULE-210** | CHECK constraints at DB level | CreditLimit ≥ 0 (add to configuration) | ⬜ Add |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | AccountId FK, CategoryId FK — both Restrict | ✅ |
| **RULE-220** | Newest-first sorting on lists | CustomerListViewModel: OrderByDescending(Id) ✅, fix CustomerService GetAllAsync sort | ⬜ Fix |
| **RULE-227** | SetDialogService() in EVERY Editor VM | CustomerEditorViewModel ✅ | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | CustomerEditorViewModel ✅ | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in CustomerEditorViewModel ✅ | ✅ |
| **RULE-246** | Users soft-deleted only | Not affected by this phase | ✅ N/A |
| **RULE-249** | Arabic strings UTF-8 encoded | All Arabic strings in new/modified files verified UTF-8 | ✅ |
| **RULE-250** | Files saved with UTF-8 encoding | All modified .cs and .xaml files | ✅ |
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
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | CustomerEditorView buttons acceptable | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new/modified XAML uses styles only | ✅ |
| **RULE-504** | Customer parent account code = "1103" (NOT "1210") | CustomerService.AutoCreateCustomerAccountAsync | ✅ |
| **RULE-506** | Level 4 accounts must have `isLeaf: true` (AllowTransactions computed => IsLeaf) | Auto-creation uses isLeaf: true | ✅ |
| **RULE-426** | AccountId mandatory (non-nullable int FK) | Customer entity | ✅ |
| **RULE-427** | CustomerGroup NOT in V1 | NO CustomerGroup entity/service/controller/UI | ✅ |
| **RULE-428** | CustomerType REMOVED | Payment type per-invoice via SalesInvoice.PaymentType | ✅ |
| **RULE-429** | Customer.Create() accepts accountId | Domain factory method | ✅ |
| **RULE-430** | Customer.Update() accepts accountId | Domain update method (AccountId is read-only after creation) | ✅ |
| **RULE-431** | NO OpeningBalance/CurrentBalance on Customer | Balance on linked Account via JournalEntryLines | ✅ |
| **RULE-432** | NO CurrencyId on Customer | Currency per-transaction (invoice/payment) | ✅ |
| **RULE-433** | CheckCreditLimit returns bool (non-throwing) | Domain method returns bool | ✅ |
| **RULE-434** | CustomerDto includes AccountId, AccountName | DTO has both | ✅ |
| **RULE-435** | Create/Update requests NO AccountId | Auto-created by service | ✅ |
| **RULE-436** | Desktop editor NO CustomerGroup/AccountId/Type UI | No such controls | ✅ |
| **RULE-437** | Account auto-created under parent "1103" | Service creates Level-4 under AR | ✅ |
| **RULE-438** | Seeder NO CustomerGroup seeds | Only "عميل نقدي" with auto-account | ✅ |
| **RULE-439** | Phone regex `^05\d{8}$` with Arabic error | FluentValidation | ✅ |
| **RULE-440** | ModernComboBox (NOT ModernTextBox) on ComboBox | Style guide | ✅ |
| **RULE-504** | Parent code is "1103" NOT "1210" | Auto-creation logic | ✅ |
| **RULE-321** | Account.Level 1-10 with CHECK constraint | Customer auto-creates Level 4 | ✅ |
| **RULE-322** | Account.Create() 13-param signature | Auto-creation uses correct signature | ✅ |
| **RULE-327** | IsSystemAccount for L1-L2 only | Customer accounts are L4, isSystem=false | ✅ |
| **RULE-328** | ColorCode auto-generated from Nature | Uses IAccountCodeGeneratorService.GetColorCode(), not hardcoded | ✅ |
| **RULE-333** | AccountService.GetTreeAsync() recursive | Not directly used in Customer, but Account tree works | ✅ N/A |
| **RULE-334** | CreateAsync validates parent/level | Auto-creation passes correct parent and level=4 | ✅ |
| **RULE-353** | AccountCode via AccountCodeGeneratorService | Uses IAccountCodeGeneratorService, not manual code | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Party removal breaks Supplier/Employee** | **HIGH** — Customer migration removes PartyId FK but Supplier and Employee still reference Parties table | Keep Parties table and Party entity. Only remove Customer's FK. Do NOT DROP Parties table until all modules migrate. |
| **AccountId FK references non-existent Account** | **MEDIUM** — migration may fail if Account FK references a table that hasn't been migrated yet | Ensure Phase 22 (Chart of Accounts) migration runs BEFORE Phase 23 migration. AccountId is mandatory — FK must exist. |
| **Account auto-creation fails if parent "1103" missing** | **MEDIUM** — customer creation blocked if Phase 22 not complete | Return clear Arabic error: "الحساب الرئيسي 1103 (العملاء) غير موجود. تأكد من اكتمال مرحلة دليل الحسابات." |
| **Existing production data with Party references** | **MEDIUM** — data migration needed to move contact fields from Parties to Customers | Write UPDATE SQL in migration: `UPDATE Customers SET Name = p.Name, ... FROM Parties p WHERE ...` |
| **CreditLimit enforcement breaks existing sales** | **MEDIUM** — existing credit customers may have high balances | Add enforcement gently: warning first (not blocking), then blocking in next phase. Or check soft — RULE-506 states warning-only for below-cost, similar approach here. |
| **CustomerListViewModel refactor breaks existing tests** | **MEDIUM** — changing async patterns changes test invocation | Update test files to use new command execution pattern. Run all tests before merging. |
| **CanExecute removal changes UX behavior** | **LOW** — Edit/Delete buttons now always enabled | This is intentional per RULE-059. Users get a warning dialog if they click without selecting. |
| **TaxNumber unique index conflicts with existing data** | **LOW** — two customers may have null or same TaxNumber | Make unique index filtered (`WHERE TaxNumber IS NOT NULL`). Handle gracefully in UI. |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Party removal migration fails | Keep PartyId column. Revert Customer.cs to use PartyId. Don't drop FK until Supplier/Employee ready. |
| Account auto-creation causes issues | Set AccountId nullable temporarily, fall back to manual Account linking. |
| AccountId FK migration fails | `ALTER TABLE Customers DROP CONSTRAINT FK_Customers_Accounts_AccountId; ALTER TABLE Customers DROP COLUMN AccountId;` |
| CreditLimit enforcement blocks too many sales | Comment out the `CheckCreditLimitAsync()` call in SalesService — business logic only, no schema change. |
| CustomerEditorViewModel refactor breaks | Revert CustomerEditorViewModel.cs to previous version — no schema impact. |
| CustomerListViewModel async refactor breaks | Revert CustomerListViewModel.cs to previous version — all data access is via API (no schema impact). |
| `FontSize="16"` change disliked by user | Revert to `FontSize="20"` — cosmetic only, no data impact. |
| Data migration (Party→Customer fields) corrupts data | Restore from backup. Run migration script in transaction with validation step. |

---

## 12. FORBIDDEN Patterns

**These patterns are explicitly forbidden in V1 of the Customers Module:**

| Pattern | Why Forbidden |
|---------|---------------|
| ❌ **CustomerGroup** — No entity, DTO, service, controller, validator, Desktop VM/View, seed data, FK on Customer | Removed per accounts Details.md — not in V1 |
| ❌ **CustomerType** — No enum, no field on Customer, no DTO field, no radio button UI | Payment type is per-invoice via SalesInvoice.PaymentType — not per-customer |
| ❌ **Parties entity for Customer** — No PartyId FK, no Party navigation property on Customer | Contact fields (Name, Phone, Email, Address, TaxNumber, Notes) are DIRECT fields on Customer entity |
| ❌ **OpeningBalance on Customer** — No OpeningBalance field | Handled via Journal Entry (Dr AR / Cr OpeningBalanceEquity) |
| ❌ **CurrentBalance on Customer** — No CurrentBalance field | Balance tracked on linked Account via JournalEntryLines |
| ❌ **CurrencyId on Customer** — No per-customer currency | Currency is per-transaction (invoice/payment), not per-customer |
| ❌ **User-supplied AccountId** — AccountId is NEVER in Create/Update requests | Always auto-created by service under parent "1103" |
| ❌ **AccountId nullable** — AccountId is mandatory (non-nullable int FK) | Every customer needs an Account for balance tracking |
| ❌ **CustomerGroup in Desktop navigation** — No "مجموعات العملاء" menu item | CustomerGroup is NOT in V1 |
| ❌ **CustomerType in Desktop editor** — No radio buttons or dropdowns | Payment type is per-invoice, not per-customer |
| ❌ **Hardcoded parent code 1210** — Parent account code is "1103" | RULE-504 explicitly mandates "1103" (Accounts Receivable/العملاء) |
| ❌ **Hardcoded ColorCode on customer accounts** — ColorCode must be auto-generated from Nature via `IAccountCodeGeneratorService.GetColorCode()` | Phase 18 RULE-328: ColorCode is system-generated, NOT user/hardcoded |
| ❌ **`allowTransactions` param on Account.Create()** — Phase 18 removed this param; use `isLeaf: true` instead | Phase 18 Task 1.2: AllowTransactions is a computed property `=> IsLeaf` |
| ❌ **`AccountNature.Debit` enum** — Nature is a `byte` (1-5), not an enum class | Phase 18 Task 1.2: `nature` parameter is `byte` |
