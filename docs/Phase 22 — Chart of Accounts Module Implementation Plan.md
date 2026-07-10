# Phase 22 — Chart of Accounts Module: Comprehensive Implementation Plan

> **Version**: 2.0 — UPDATED to reflect Phase 18 actual implementation
> **Scope**: Complete Chart of Accounts CRUD module with 4-level hierarchy, 81 seeded accounts, Desktop UI, and integration points for future journal entries
> **Key Change**: Phase 18 implemented the foundation (entity, seeder, service, controller, Desktop UI) — this plan aligns with what actually exists in the codebase

---

## Table of Contents

1. [Architecture — Chart of Accounts Design](#1-architecture--chart-of-accounts-design)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Decisions](#3-blocker-resolution--critical-decisions)
4. [Chart of Accounts Design Catalog](#4-chart-of-accounts-design-catalog)
5. [Gap Analysis — Existing vs Target Accounts](#5-gap-analysis--existing-vs-target-accounts)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)
12. [Self-Explanation ◉ Tooltips for All 81 Accounts](#12-self-explanation--tooltips-for-all-81-accounts)

---

## 1. Architecture — Chart of Accounts Design

### 1.1 Hierarchy Model

The Chart of Accounts follows a **4-level hierarchical tree** using a self-referencing `ParentId` FK:

| Level | Name | Arabic | Digits | Code Pattern | Purpose | Allow Transactions |
|-------|------|--------|--------|-------------|---------|-------------------|
| 1 | **Group** | رئيسي | 1 | `1` | Top-level category (Assets, Liabilities, Revenue, Expenses, Equity) | ❌ No (parent only) |
| 2 | **Main** | فرعي | 2 | `11` | Major account grouping within a category (Current Assets, Fixed Assets, Operating Expenses) | ❌ No (parent only) |
| 3 | **Sub** | فرعي فرعي | 4 | `1101` | Specific account group (Cash in Hand, Banks, Accounts Receivable) | ❌ No (parent only) |
| 4 | **Detail** | تفصيلي | 8 | `11010001` | Leaf-level account where transactions post (Main Cash Box, Customer A, Bank Account) | ✅ Yes |

**Tree example from actual AccountingSeeder (Phase 18):**
```
1 — الأصول (Level 1 — Group)
├── 11 — أصول متداولة (Level 2 — Main)
│   ├── 1101 — النقدية (Level 3 — Sub)
│   │   ├── 11010001 — الصندوق (Level 4 — Detail) ✅ Transactions
│   │   └── 11010002 — صندوق المصروفات النثرية (Level 4 — Detail) ✅ Transactions
│   ├── 1102 — البنوك (Level 3 — Sub)
│   │   ├── 11020001 — البنك الأهلي (Level 4 — Detail) ✅ Transactions
│   │   └── 11020002 — بنك الرياض (Level 4 — Detail) ✅ Transactions
│   ├── 1103 — العملاء (Level 3 — Sub)
│   │   ├── 11030001 — العميل النقدي (Level 4 — Detail) ✅ Transactions
│   │   └── 11030002 — عملاء آجلون (Level 4 — Detail) ✅ Transactions
│   └── 1104 — المخزون (Level 3 — Sub)
│       ├── 11040001 — بضاعة أول المدة (Level 4 — Detail) ✅ Transactions
│       └── 11040002 — مخزون آخر المدة (Level 4 — Detail) ✅ Transactions
├── 12 — أصول ثابتة (Level 2 — Main)
│   ├── 1201 — أصول ثابتة ملموسة (Level 3 — Sub)
│   │   ├── 12010001 — أثاث ومعدات (Level 4 — Detail) ✅ Transactions
│   │   └── 12010002 — أجهزة حاسب آلي (Level 4 — Detail) ✅ Transactions
│   └── 1203 — مجمع الإهلاك (Level 3 — Sub)
│       └── 12030001 — مخصص إهلاك الأثاث (Level 4 — Detail) ✅ Transactions
2 — الخصوم (Level 1 — Group)
└── 21 — التزامات متداولة (Level 2 — Main)
    ├── 2101 — الموردون (Level 3 — Sub)
    │   ├── 21010001 — المورد النقدي (Level 4 — Detail) ✅ Transactions
    │   └── 21010002 — موردون آجلون (Level 4 — Detail) ✅ Transactions
    └── 2102 — الضرائب (Level 3 — Sub)
        ├── 21020001 — ضريبة المبيعات (خرج) (Level 4 — Detail) ✅ Transactions
        └── 21020002 — ضريبة المشتريات (دخل) (Level 4 — Detail) ✅ Transactions
```

### 1.2 Core Entity Design

**Actual Account entity (fully implemented in Phase 18) at `Domain/Accounting/Entities/Account.cs`:**

```
┌─────────────────────────────────────────────────┐
│                    Account                        │
├─────────────────────────────────────────────────┤
│ Id (int PK, auto-increment)                      │
│ AccountCode (string(20), UNIQUE filtered)         │
│ NameAr (string(200), required)                   │
│ NameEn (string(200), optional)                   │
│ Nature (byte: 1=Asset..5=Expense)               │
│ Level (byte: 1-10, DB CHECK constraint)         │
│ IsLeaf (bool, default true)                      │
│ AllowTransactions (bool) — COMPUTED => IsLeaf   │
│ ParentId (int? FK → self, Restrict)             │
│ IsSystem (bool, default false)                   │
│ IsActive (bool, default true)                    │
│ CategoryId (short? FK → AccountCategories)      │
│ Description (string(500)?)                      │
│ ColorCode (string(7)?) — Auto-set in seeder     │
│ Notes (string(300)?)                             │
│ CreatedByUserId, CreatedAt, UpdatedAt, ...      │
│ Read-Only: Activate(), Deactivate(),             │
│            MarkAsDeleted(), Update()             │
│ Navigation: ParentAccount, SubAccounts,          │
│             JournalLines, Category               │
└─────────────────────────────────────────────────┘
         │
         │ ParentId (self-referencing FK, Restrict)
         ▼
┌─────────────────────────────────────────────────┐
│                    Account                        │
│                   (Parent)                        │
└─────────────────────────────────────────────────┘
```

**CRITICAL differences from pre-Phase 18 plan:**
- ❌ **OpeningBalance column REMOVED** — not on Account entity. Opening balance creates a Journal Entry.
- ❌ **No `AllowTransactions` stored column** — computed property `=> IsLeaf`
- ✅ **ColorCode** exists as nullable string(7). Set in seeder/service, NEVER user-supplied.
- ✅ **Level** exists as `byte` with DB CHECK `[Level] >= 1 AND [Level] <= 10`
- ✅ **Description** exists as `string(500)?`
- ✅ **AccountCode** system-generated via `AccountCodeGeneratorService` (SemaphoreSlim thread-safe)

### 1.3 Nature (AccountType) Enum Values

| Code | Name | Normal Balance | Color (seeder) |
|------|------|---------------|-------|
| 1 | `Asset` | Debit | `#2196F3` (Blue) |
| 2 | `Liability` | Credit | `#F44336` (Red) |
| 3 | `Equity` | Credit | `#4CAF50` (Green) |
| 4 | `Revenue` | Credit | `#4CAF50` (Green) |
| 5 | `Expense` | Debit | `#FF9800` (Orange) |

Enum file: `Domain/Accounting/Enums/AccountType.cs`

### 1.4 AccountCode Strategy (HIERARCHICAL — Fully Implemented)

String code pattern using **hierarchical expanding numbering**:

| Level | Digits | Format | Example | Max Accounts |
|-------|--------|--------|---------|-------------|
| 1 | 1 | Single digit | `1` (Assets) | 9 |
| 2 | 2 | Parent + 1 digit | `11` (Current Assets) | 9 per L1 |
| 3 | 4 | Parent + 2 digits | `1101` (Cash) | 99 per L2 |
| 4 | 8 | Parent + 4 digits | `11010001` (Cash on Hand) | 9,999 per L3 |

**Implementation**: `AccountCodeGeneratorService` at `Application/Services/AccountCodeGeneratorService.cs`:
- Uses `SemaphoreSlim` for thread safety
- Level 1: queries max existing L1 code, increments by 1
- Level 2+: queries parent's children, finds max suffix, increments
- Pattern: `parentCode + nextSuffix.ToString("0"/"00"/"0000")`

**System-generated**: AccountCode is NEVER user-supplied. `CreateAccountRequest` has NO `AccountCode` field.

---

## 2. Full Inventory — What Already Exists

### 2.1 Account Entity ✅ (Fully Implemented in Phase 18)

**File**: `SalesSystem.Domain/Accounting/Entities/Account.cs` (245 lines)

| Field | Type | Status | Notes |
|-------|------|--------|-------|
| `Id` | `int PK` | ✅ Exists | Auto-increment |
| `AccountCode` | `string(20)` | ✅ Exists | UNIQUE filtered index WHERE IsActive = 1 |
| `NameAr` | `string(200)` | ✅ Exists | Required |
| `NameEn` | `string(200)` | ✅ Exists | Optional |
| `Nature` | `byte` | ✅ Exists | 1=Asset..5=Expense — mapped to AccountType enum |
| `Level` | `byte` | ✅ Exists | CHECK constraint [Level] >= 1 AND [Level] <= 10 |
| `IsLeaf` | `bool` | ✅ Exists | Default true |
| `AllowTransactions` | `bool` | ✅ Exists | COMPUTED property `=> IsLeaf` — NOT stored |
| `ParentId` | `int?` | ✅ Exists | Self-ref FK, Restrict delete |
| `IsSystem` | `bool` | ✅ Exists | Default false |
| `IsActive` | `bool` | ✅ Exists | Query filter |
| `CategoryId` | `short?` | ✅ Exists | FK to AccountCategories |
| `Description` | `string(500)?` | ✅ Exists | Help text for reports |
| `ColorCode` | `string(7)?` | ✅ Exists | Hex color — auto-set in seeder |
| `Notes` | `string(300)?` | ✅ Exists | Additional notes |
| Inherited: `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId` | | ✅ Exists | From BaseEntity |
| **`OpeningBalance`** | **`decimal(18,2)?`** | **❌ REMOVED** | Handled via Journal Entry in service layer |
| **`Explanation`** | **`string(500)?`** | **❌ NOT YET** | Needed for ◉ tooltips (Section 12) |

**Domain methods** (all implemented):
- `Account.Create(13 params)` — no OpeningBalance param, ColorCode param optional (set by service/seeder)
- `Account.Update(10 params)` — no AccountCode/IsSystem changes allowed
- `MarkAsDeleted()` — guards IsSystem + HasChildren()
- `Activate()` / `Deactivate()` — guards IsSystem
- `HasChildren()` — defense-in-depth (service uses DB AnyAsync as primary guard)
- `IsDebitNormal()` — true for Asset(1) and Expense(5)
- `GetAccountType()` — returns `(AccountType)Nature`

### 2.2 AccountType Enum ✅ (Exists)

**File**: `SalesSystem.Domain/Accounting/Enums/AccountType.cs`

| Value | Name |
|-------|------|
| 1 | Asset |
| 2 | Liability |
| 3 | Equity |
| 4 | Revenue |
| 5 | Expense |

Values match AGENTS.md Section 3 exactly.

### 2.3 AccountConfiguration ✅ (Fully Implemented in Phase 18)

**File**: `Infrastructure/Data/Configurations/AccountConfiguration.cs` (76 lines)

**Already correct**:
- `DeleteBehavior.Restrict` on self-referencing FK (RULE-214 ✅)
- Unique filtered index on `AccountCode` WHERE `[IsActive] = 1`
- `HasQueryFilter(x => x.IsActive)`
- `HasMaxLength` on all string properties
- CHECK constraint `CHK_Account_Level_Range` — `[Level] >= 1 AND [Level] <= 10`
- `Level` mapped as `tinyint` with default value 1
- `IsLeaf` default value true
- `Description` max length 500
- `ColorCode` max length 7
- `Notes` max length 300
- Category FK with `DeleteBehavior.Restrict`

**NOT present** (and not needed):
- ❌ No `OpeningBalance` config — property doesn't exist on entity
- ❌ No `AllowTransactions` stored column — computed property only
- ❌ No `Explanation` config — entity field not yet added (Section 12 task)

### 2.4 AccountingSeeder ✅ (81 accounts — Fully Implemented in Phase 18)

**File**: `Infrastructure/Data/Seeders/AccountingSeeder.cs` (615 lines)

| Level | Count | isSystem | Allow Txns | Code Pattern |
|-------|-------|----------|-----------|-------------|
| 1 — Group | 5 | ✅ true | ❌ | `1`, `2`, `3`, `4`, `5` |
| 2 — Main | 8 | ✅ true | ❌ | `11`, `12`, `21`, `31`, `32`, `41`, `51`, `52` |
| 3 — Sub | 24 | ❌ false | ❌ | `1101`, `1102`, ... `5202` |
| 4 — Detail | 44 | ❌ false (except 2 system) | ✅ | `11010001`, ... `52020003` |
| **Total** | **81** | **15 system** | **44 leaf** | Hierarchical |

**Two special Level-4 system accounts**: `32020001` (Opening Balance Equity) and `32030001` (Undistributed Profits) have `isSystem: true` — protected from modification.

**Seeding approach**: Four-pass:
1. Create Level 1 → `SaveChangesAsync` → query IDs
2. Create Level 2 with parent IDs → `SaveChangesAsync` → query IDs
3. Create Level 3 with parent IDs → `SaveChangesAsync` → query IDs
4. Create Level 4 with parent IDs → `SaveChangesAsync` → query IDs

**Color codes** (hardcoded constants in seeder):
- Asset → `#2196F3` (blue)
- Liability → `#F44336` (red)
- Equity → `#4CAF50` (green)
- Revenue → `#4CAF50` (green)
- Expense → `#FF9800` (orange)

**SystemAccountMappings**: 21 key-value pairs seeded after accounts (see seeder lines 524-613):
- DefaultCash, DefaultBank, AccountsReceivable, AccountsPayable, Inventory
- CostOfGoodsSold, SalesRevenue, SalesReturns, PurchaseReturns
- VatOutput, VatInput, Capital, OpeningBalanceEquity, RetainedEarnings
- UndistributedProfits, InventoryShortage, InventorySurplus
- GeneralExpense, SpoilageLoss, EmployeeCustody, DeliveryChargesRevenue

### 2.5 What Already Exists (Implemented in Phase 18)

| Component | Status | File |
|-----------|--------|------|
| `AccountDto` | ✅ Exists | `Contracts/DTOs/AllDtos.cs` |
| `AccountTreeNodeDto` | ✅ Exists | `Contracts/DTOs/AllDtos.cs` |
| `CreateAccountRequest` | ✅ Exists | `Contracts/Requests/AccountingRequests.cs` — NO AccountCode, has OpeningBalance |
| `UpdateAccountRequest` | ✅ Exists | `Contracts/Requests/AccountingRequests.cs` |
| `CreateAccountRequestValidator` | ✅ Exists | `Api/Validators/` |
| `UpdateAccountRequestValidator` | ✅ Exists | `Api/Validators/` |
| `IAccountService` interface | ✅ Exists | `Application/Interfaces/Services/IAccountService.cs` |
| `AccountService` implementation | ✅ Exists | `Application/Services/AccountService.cs` |
| `IAccountCodeGeneratorService` | ✅ Exists | `Application/Interfaces/Services/` |
| `AccountCodeGeneratorService` | ✅ Exists | `Application/Services/AccountCodeGeneratorService.cs` — SemaphoreSlim thread-safe |
| `AccountsController` API | ✅ Exists | `Api/Controllers/AccountsController.cs` — 8 endpoints |
| `IAccountApiService` Desktop client | ✅ Exists | `DesktopPWF/Services/Api/IAccountApiService.cs` |
| `AccountApiService` Desktop impl | ✅ Exists | `DesktopPWF/Services/Api/AccountApiService.cs` — content-type guard |
| `AccountsListViewModel` | ✅ Exists | `DesktopPWF/ViewModels/Accounts/AccountsListViewModel.cs` |
| `AccountsListView.xaml` | ✅ Exists | `DesktopPWF/Views/Accounts/AccountsListView.xaml` |
| `AccountEditorViewModel` | ✅ Exists | `DesktopPWF/ViewModels/Accounts/AccountEditorViewModel.cs` |
| `AccountEditorView.xaml` | ✅ Exists | `DesktopPWF/Views/Accounts/AccountEditorView.xaml` |
| `AccountChangedMessage` EventBus | ✅ Exists | `DesktopPWF/Messaging/Messages/AppMessages.cs` |
| Desktop DI + navigation | ✅ Exists | `App.xaml.cs` + `MainWindow.xaml` |

### 2.6 What's MISSING (Needs Implementation)

| Component | Status | Priority |
|-----------|--------|----------|
| `Explanation` field on Account entity | ❌ Missing | **HIGH** — needed for ◉ tooltips |
| `Explanation` seeding in AccountingSeeder | ❌ Missing | **HIGH** — 81 explanation strings |
| `SetExplanation()` domain method | ❌ Missing | **HIGH** |
| `InfoTooltip` binding in tree view / editor | ❌ Missing | **MEDIUM** |
| `AccountEditorView` code-behind completeness | ❌ Partially done | **LOW** — verify all bindings work |
| `Explanation` API field in DTO | ❌ Missing | **MEDIUM** |
| `HasExplanation` computed property | ❌ Missing | **MEDIUM** |
| `AccountColorHelper` static class | ❌ Missing | **LOW** — colors currently hardcoded; could refactor |
| Unit tests for Account entity/service/controller | ❌ Not verified | **MEDIUM** — check existing test coverage |

---

## 3. BLOCKER Resolution — Critical Decisions

### 3.1 Blocker 1: AccountCode Strategy — ✅ RESOLVED: System-Generated Hierarchical

**Decision**: AccountCode is **system-generated** via `AccountCodeGeneratorService` (SemaphoreSlim thread-safe). Codes follow hierarchical pattern: Level 1 = 1 digit, Level 2 = 2 digits, Level 3 = 4 digits, Level 4 = 8 digits.

**Implementation**: `CreateAccountRequest` has NO `AccountCode` field. The service auto-generates the code.

### 3.2 Blocker 2: Level Property — ✅ RESOLVED: Added as byte(1-10)

**Decision**: `Level` exists as `byte` with DB CHECK constraint `[Level] >= 1 AND [Level] <= 10`.
- Level 1=Group, 2=Main, 3=Sub, 4=Detail (standard)
- Can go up to 10 (future expansion)
- Validation: `child.Level > parent.Level` in domain
- `HasDefaultValue((byte)1)` in config

### 3.3 Blocker 3: Expand from 18 to 81 Accounts — ✅ RESOLVED: 81 accounts seeded

**Decision**: AccountingSeeder now seeds 81 accounts (5 L1 + 8 L2 + 24 L3 + 44 L4) with hierarchical codes, color codes, descriptions, parent-child relationships.

### 3.4 Blocker 4: Parent-Child Deletion Protection — ✅ RESOLVED: Dual guard

**Decision**: Two-layer protection:
1. **Domain**: `MarkAsDeleted()` has `HasChildren()` guard (defense-in-depth)
2. **Service**: `DeleteAsync()` checks `AnyAsync(a => a.ParentAccountId == id)` BEFORE calling `MarkAsDeleted()`

System accounts (`IsSystem = true`) are also protected at both layers.

---

## 4. Chart of Accounts Design Catalog

### 4.1 Account Entity — Complete Spec

See `Domain/Accounting/Entities/Account.cs` for the canonical entity. Key design decisions:

- **Nature vs AccountType**: Entity uses `Nature` (byte). `AccountType` enum exists for type safety in API/DTO layers.
- **AllowTransactions**: Computed property `=> IsLeaf` — NOT stored in DB.
- **ColorCode**: Nullable string(7), set in seeder/seervice. NEVER user-supplied.
- **OpeningBalance**: NOT on entity. Handled via Journal Entry in service layer.
- **AccountCode**: System-generated. Max 20 chars. Unique filtered WHERE IsActive = 1.

### 4.2 AccountConfiguration — Complete Spec

See `Infrastructure/Data/Configurations/AccountConfiguration.cs`. All Fluent API config is in place.

### 4.3 DTOs

**File**: `Contracts/DTOs/AllDtos.cs`

```csharp
public record AccountDto(
    int Id, string AccountCode, string NameAr, string? NameEn,
    byte Nature, bool IsLeaf, int? ParentId, string? ParentAccountName,
    bool IsSystem, bool IsActive, short? CategoryId, byte Level,
    string? Description, string? ColorCode, string? Notes)
{
    public string NatureDisplay => ...;  // "أصل", "خصم", etc.
    public string LevelDisplay => ...;   // "مجموعة رئيسية", etc.
    public byte AccountType => Nature;   // Backward-compatible
    public string AccountTypeDisplay => NatureDisplay;
    public bool AllowTransactions => IsLeaf;
    public int? ParentAccountId => ParentId;
}
```

`AccountTreeNodeDto` (for tree view) — list of children populated recursively by `AccountService.GetTreeAsync()`.

### 4.4 Request DTOs

**File**: `Contracts/Requests/AccountingRequests.cs`

```csharp
public record CreateAccountRequest(
    string NameAr, string? NameEn, byte Nature, bool IsLeaf,
    int? ParentId, bool IsSystem, short? CategoryId,
    string? Description, string? Notes, decimal? OpeningBalance);

public record UpdateAccountRequest(
    string NameAr, string? NameEn, byte Nature, bool IsLeaf,
    int? ParentId, short? CategoryId,
    string? Description, string? Notes);
```

### 4.5 FluentValidators

Validators exist in `Api/Validators/`. Create validator validates:
- NameAr required
- Nature 1-5
- Level 1-10
- OpeningBalance >= 0 (transient — creates JE)

### 4.6 Color Coding Map

| Nature | Color Hex | Arabic Name |
|--------|-----------|-------------|
| Asset (1) | `#2196F3` | أصل |
| Liability (2) | `#F44336` | خصم |
| Equity (3) | `#4CAF50` | حق ملكية |
| Revenue (4) | `#4CAF50` | إيراد |
| Expense (5) | `#FF9800` | مصروف |

Colors are set in seeder constants. A future `AccountColorHelper` static class could centralize this mapping.

---

## 5. Gap Analysis — Existing vs Target Accounts

### 5.1 Complete Account Hierarchy (81 Accounts — Already Seeded)

The AccountingSeeder at `Infrastructure/Data/Seeders/AccountingSeeder.cs` seeds all 81 accounts below.

> **IsSystemAccount scope**: Level 1 (Group) and Level 2 (Main) accounts have `isSystem = true`. Level 3 (Sub) accounts have `isSystem = false`. Level 4 (Detail) accounts have `isSystem = false` EXCEPT `32020001` (Opening Balance Equity) and `32030001` (Undistributed Profits) which are `isSystem = true` for data integrity.

#### Level 1 — Groups (5 accounts, isSystem=true)

| Code | Name (Ar) | Nature | Parent |
|------|-----------|--------|--------|
| 1 | الأصول | Asset | null |
| 2 | الخصوم | Liability | null |
| 3 | حقوق الملكية | Equity | null |
| 4 | الإيرادات | Revenue | null |
| 5 | المصروفات | Expense | null |

#### Level 2 — Main Categories (8 accounts, isSystem=true)

| Code | Name (Ar) | Nature | Parent |
|------|-----------|--------|--------|
| 11 | أصول متداولة | Asset | 1 |
| 12 | أصول ثابتة | Asset | 1 |
| 21 | التزامات متداولة | Liability | 2 |
| 31 | رأس المال والاحتياطيات | Equity | 3 |
| 32 | الأرباح والخسائر | Equity | 3 |
| 41 | إيرادات النشاط | Revenue | 4 |
| 51 | تكاليف النشاط | Expense | 5 |
| 52 | مصاريف تشغيلية وإدارية | Expense | 5 |

#### Level 3 — Sub Categories (24 accounts, isSystem=false)

| Code | Name (Ar) | Nature | Parent |
|------|-----------|--------|--------|
| 1101 | النقدية | Asset | 11 |
| 1102 | البنوك | Asset | 11 |
| 1103 | العملاء | Asset | 11 |
| 1104 | المخزون | Asset | 11 |
| 1105 | أصول متداولة أخرى | Asset | 11 |
| 1106 | تسوية المخزون | Asset | 11 |
| 1107 | عهد الموظفين | Asset | 11 |
| 1201 | أصول ثابتة ملموسة | Asset | 12 |
| 1202 | أصول ثابتة غير ملموسة | Asset | 12 |
| 1203 | مجمع الإهلاك | Asset | 12 |
| 2101 | الموردون | Liability | 21 |
| 2102 | الضرائب | Liability | 21 |
| 2103 | التزامات متداولة أخرى | Liability | 21 |
| 3101 | رأس المال | Equity | 31 |
| 3102 | المسحوبات | Equity | 31 |
| 3201 | أرباح مدورة | Equity | 32 |
| 3202 | أرصدة افتتاحية | Equity | 32 |
| 3203 | أرباح غير موزعة | Equity | 32 |
| 4101 | إيرادات المبيعات | Revenue | 41 |
| 4102 | إيرادات أخرى | Revenue | 41 |
| 5101 | تكلفة المبيعات | Expense | 51 |
| 5102 | المردودات | Expense | 51 |
| 5201 | مصروفات عمومية وإدارية | Expense | 52 |
| 5202 | مصروفات أخرى | Expense | 52 |

#### Level 4 — Detail Accounts (44 accounts, isSystem=false except 2)

| Code | Name (Ar) | Nature | Parent | isSystem |
|------|-----------|--------|--------|----------|
| 11010001 | الصندوق | Asset | 1101 | ❌ |
| 11010002 | صندوق المصروفات النثرية | Asset | 1101 | ❌ |
| 11020001 | البنك الأهلي | Asset | 1102 | ❌ |
| 11020002 | بنك الرياض | Asset | 1102 | ❌ |
| 11030001 | العميل النقدي | Asset | 1103 | ❌ |
| 11030002 | عملاء آجلون | Asset | 1103 | ❌ |
| 11030003 | مخصص الديون المشكوك فيها | Asset | 1103 | ❌ |
| 11040001 | بضاعة أول المدة | Asset | 1104 | ❌ |
| 11040002 | مخزون آخر المدة | Asset | 1104 | ❌ |
| 11050001 | مصروفات مدفوعة مقدماً | Asset | 1105 | ❌ |
| 11050002 | أوراق قبض | Asset | 1105 | ❌ |
| 11070001 | عهدة الموظفين | Asset | 1107 | ❌ |
| 12010001 | أثاث ومعدات | Asset | 1201 | ❌ |
| 12010002 | أجهزة حاسب آلي | Asset | 1202 | ❌ |
| 12020001 | برمجيات | Asset | 1202 | ❌ |
| 12030001 | مخصص إهلاك الأثاث | Asset | 1203 | ❌ |
| 12030002 | مخصص إهلاك الحاسب | Asset | 1203 | ❌ |
| 21010001 | المورد النقدي | Liability | 2101 | ❌ |
| 21010002 | موردون آجلون | Liability | 2101 | ❌ |
| 21020001 | ضريبة المبيعات (خرج) | Liability | 2102 | ❌ |
| 21020002 | ضريبة المشتريات (دخل) | Liability | 2102 | ❌ |
| 21030001 | أوراق دفع | Liability | 2103 | ❌ |
| 31010001 | رأس المال | Equity | 3101 | ❌ |
| 31020001 | المسحوبات الشخصية | Equity | 3102 | ❌ |
| 32010001 | أرباح مدورة | Equity | 3201 | ❌ |
| 32020001 | رصيد افتتاحي | Equity | 3202 | **✅ true** |
| 32030001 | أرباح غير موزعة | Equity | 3203 | **✅ true** |
| 41010001 | إيرادات المبيعات | Revenue | 4101 | ❌ |
| 41020001 | إيراد النقل | Revenue | 4102 | ❌ |
| 41020002 | الخصم المكتسب | Revenue | 4102 | ❌ |
| 41020003 | إيرادات التوصيل | Revenue | 4102 | ❌ |
| 51010001 | تكلفة البضاعة المباعة | Expense | 5101 | ❌ |
| 51020001 | مردودات مبيعات | Expense | 5102 | ❌ |
| 51020002 | مردودات مشتريات | Expense | 5102 | ❌ |
| 52010001 | مصروفات عمومية | Expense | 5201 | ❌ |
| 52010002 | الرواتب والأجور | Expense | 5201 | ❌ |
| 52010003 | الكهرباء | Expense | 5201 | ❌ |
| 52010004 | المياه | Expense | 5201 | ❌ |
| 52010005 | الإيجارات | Expense | 5201 | ❌ |
| 52010006 | النقل | Expense | 5201 | ❌ |
| 52010007 | الخصم المسموح به | Expense | 5201 | ❌ |
| 52020001 | هالك المخزون | Expense | 5202 | ❌ |
| 52020002 | عجز مخزون | Expense | 5202 | ❌ |
| 52020003 | زيادة مخزون | **Revenue** | 5202 | ❌ |

### 5.2 Gap Summary

| Category | Level 1 | Level 2 | Level 3 | Level 4 | Total |
|----------|---------|---------|---------|---------|-------|
| Assets | 1 | 2 | 7 | 15 | 25 |
| Liabilities | 1 | 1 | 3 | 5 | 10 |
| Equity | 1 | 2 | 5 | 4 | 12 |
| Revenue | 1 | 1 | 2 | 4 | 8 |
| Expenses | 1 | 2 | 7 | 16 | 26 |
| **Total** | **5** | **8** | **24** | **44** | **81** |

**Note**: 81 total accounts are already seeded. No gap exists between target and actual — Phase 18 fully implemented the chart of accounts.

---

## 6. Architectural Decisions

### 6.1 AccountCode Strategy: System-Generated Hierarchical String

**Decision**: AccountCode is system-generated via `AccountCodeGeneratorService` with SemaphoreSlim thread safety. Codes follow hierarchical pattern:
- Level 1: 1 digit (`1`, `2`, `3`, `4`, `5`)
- Level 2: 2 digits (`11`, `12`, `21`, `31`, `41`, `51`, `52`)
- Level 3: 4 digits (`1101`, `1102`, ...)
- Level 4: 8 digits (`11010001`, `11010002`, ...)

**User NEVER supplies AccountCode** — it is auto-generated. The `CreateAccountRequest` has no `AccountCode` field.

### 6.2 Level Numbering: 1-4

**Decision**: Level 1=Group → Level 2=Main → Level 3=Sub → Level 4=Detail.
- DB constraint: `CHECK ([Level] >= 1 AND [Level] <= 10)`
- Auto-validation: `child.Level > parent.Level` in domain

### 6.3 Color Coding Per AccountType

**Decision**: ColorCode assigned by Nature in the Seeder:

| Type | Color | Rationale |
|------|-------|-----------|
| Asset | Blue (`#2196F3`) | Standard accounting convention |
| Liability | Red (`#F44336`) | Red ink for obligations |
| Equity | Green (`#4CAF50`) | Profit/growth |
| Revenue | Green (`#4CAF50`) | Income/profit |
| Expense | Orange (`#FF9800`) | Cost/outflow |

ColorCode is NEVER user-supplied. Set in seeder; a future `AccountColorHelper` static class could centralize mapping.

### 6.4 Opening Balance — ✅ REMOVED from Account Entity

**Decision**: Opening balance is NOT on the Account entity. Opening balances are handled via Journal Entry in the service layer:
- `AccountService.CreateAsync()` accepts optional `OpeningBalance` (decimal?)
- If > 0, creates a Journal Entry (Dr Account / Cr OpeningBalanceEquity for Asset/Expense, vice versa for Liability/Equity/Revenue)
- Wrapped in `BeginTransactionAsync()` for atomicity

### 6.5 TreeView vs GridView in Desktop UI

**Decision**: **Support BOTH views** — toggleable by a button in the toolbar (already implemented).
- **TreeView** (default): Hierarchical display with expand/collapse, indentation, color coding
- **GridView** (flat table): Search, sort, filter

### 6.6 Account Service Returns Result<T>

**Decision**: ALL service methods return `Result<T>` (RULE-006) using `IUnitOfWork` (RULE-024). Already implemented in `IAccountService`.

### 6.7 Why NOT Add CurrencyId to Account in V1

Deferred — requires full currency module integration.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason | Target Phase |
|---------|--------|-------------|
| **Multi-currency accounts** (`CurrencyId` FK) | Requires full currency module integration | Future |
| **Opening balance import from Excel** | Needs ClosedXML integration | Future |
| **Account freeze/lock** | Separate from IsActive — prevents transactions | Future |
| **Account budget tracking** | Separate Budget entity | Future |
| **Account hierarchies > 4 levels** | Validation allows up to Level 10 | Future |
| **Drag-and-drop reordering in tree** | Manual sort via AccountCode is sufficient | Future |
| **Account statement report** (كشف حساب) | Already implemented in AccountsController | ✅ **Done** |
| **Trial balance report** (ميزان المراجعة) | Query infrastructure exists | Future |
| **Income statement** (قائمة الدخل) | Aggregation from JournalEntry | Future |
| **Balance sheet** (المركز المالي) | Aggregation from JournalEntry | Future |

---

## 8. Implementation Tasks

**CRITICAL NOTE**: Most tasks in this section were already completed in Phase 18. Remaining work focuses on the `Explanation` field for ◉ tooltips (Section 12) and unit test verification.

---

### Task 1 — Account Entity — ✅ ALREADY DONE (Phase 18)

**Status**: Fully implemented with:
- `Level` (byte) — ✅
- `Description` (string?) — ✅
- `ColorCode` (string?) — ✅
- `AllowTransactions` computed (`=> IsLeaf`) — ✅
- No `OpeningBalance` — ✅ (handled via JE)
- `MarkAsDeleted()` with `HasChildren()` guard — ✅
- `IsDebitNormal()` for Asset/Expense — ✅

**Remaining**: Add `Explanation` property (string?) for ◉ tooltips — see Task 13.

---

### Task 2 — AccountConfiguration — ✅ ALREADY DONE (Phase 18)

**Status**: Fully implemented with:
- CHECK constraint `CHK_Account_Level_Range` — ✅
- Filtered unique index on AccountCode — ✅
- All string max lengths — ✅
- No OpeningBalance config — ✅
- Self-referencing FK with Restrict — ✅

---

### Task 3 — AccountingSeeder — ✅ ALREADY DONE (Phase 18)

**Status**: 81 accounts seeded with four-pass approach + 21 SystemAccountMappings.
- Hierarchical codes (1, 11, 1101, 11010001) — ✅
- Color codes per Nature — ✅
- Descriptions for all accounts — ✅
- isSystem correctly scoped — ✅
- Two special Level-4 system accounts — ✅

**Remaining**: Seed `Explanation` strings for all 81 accounts — see Section 12.

---

### Task 4 — Account DTOs + Request DTOs + Validators — ✅ ALREADY DONE

**Status**: All DTOs, requests, and validators exist in `Contracts/` and `Api/Validators/`.
- `AccountDto` with NatureDisplay, LevelDisplay — ✅
- `AccountTreeNodeDto` with recursive Children — ✅
- `CreateAccountRequest` (no AccountCode, has OpeningBalance) — ✅
- `UpdateAccountRequest` (no AccountCode, no OpeningBalance) — ✅

---

### Task 5 — AccountService — ✅ ALREADY DONE (Phase 18)

**Status**: `IAccountService` + `AccountService` fully implemented.
- `GetTreeAsync()` — builds recursive tree from flat list — ✅
- `GetAllAsync()` — flat list ordered by AccountCode — ✅
- `GetByIdAsync()` — single account — ✅
- `GetByTypeAsync()` — filter by Nature — ✅
- `CreateAsync()` — auto-generates code, auto-sets color, creates OB JE if needed — ✅
- `UpdateAsync()` — guards system accounts — ✅
- `DeleteAsync()` — soft delete with AnyAsync guard — ✅
- `PermanentDeleteAsync()` — catches DbUpdateException — ✅
- Inject `IAccountCodeGeneratorService` — ✅

---

### Task 6 — AccountsController — ✅ ALREADY DONE (Phase 18)

**Status**: Fully implemented at `Api/Controllers/AccountsController.cs`:

| Method | Route | Policy | Status |
|--------|-------|--------|--------|
| `GET` | `/api/v1/accounts/tree` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts/{id}` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts/by-type/{type}` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts/{id}/balance` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts/{id}/ledger` | `AllStaff` | ✅ |
| `GET` | `/api/v1/accounts/mappings` | `AllStaff` | ✅ |
| `POST` | `/api/v1/accounts` | `ManagerAndAbove` | ✅ |
| `PUT` | `/api/v1/accounts/{id}` | `ManagerAndAbove` | ✅ |
| `DELETE` | `/api/v1/accounts/{id}` | `ManagerAndAbove` | ✅ |
| `DELETE` | `/api/v1/accounts/permanent/{id}` | `AdminOnly` | ✅ |

---

### Task 7 — DI Registration — ✅ ALREADY DONE

**Status**: All services registered in API `Program.cs` and Desktop `App.xaml.cs`.

---

### Task 8 — Desktop API Client — ✅ ALREADY DONE

**Status**: `IAccountApiService` + `AccountApiService` exist with content-type guard.

---

### Task 9 — EventBus Message — ✅ ALREADY DONE

**Status**: `AccountChangedMessage` exists in `DesktopPWF/Messaging/`.

---

### Task 10 — Desktop ViewModels + Views — ✅ ALREADY DONE

**Status**: `AccountsListViewModel`, `AccountEditorViewModel`, `AccountsListView.xaml`, `AccountEditorView.xaml` all exist with:
- Tree-view/grid toggle — ✅
- INotifyDataErrorInfo validation — ✅
- ExecuteAsync wrappers — ✅
- EventBus subscriptions with Cleanup() — ✅
- DialogService for errors — ✅
- ToolTips on all controls — ✅

---

### Task 11 — Desktop DI + Navigation — ✅ ALREADY DONE

**Status**: Registrations in `App.xaml.cs` + navigation entry in `MainWindow.xaml`.

---

### Task 12 — Search/Filter — ✅ ALREADY DONE

**Status**: Search functionality exists in `AccountsListViewModel` with dual-mode filtering.

---

### Task 13 — Add Explanation Field for ◉ Tooltips

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/Account.cs` | Add `Explanation` property + `SetExplanation()` method |
| `Infrastructure/Data/Configurations/AccountConfiguration.cs` | Add `HasMaxLength(500)` for Explanation |
| `Contracts/DTOs/AllDtos.cs` | Add `Explanation` + `HasExplanation` to AccountDto |
| `Infrastructure/Data/Seeders/AccountingSeeder.cs` | Add all 81 explanation strings from Section 12 |
| `DesktopPWF/Views/Accounts/AccountsListView.xaml` | Add ◉ InfoTooltip in tree/grid templates |
| `DesktopPWF/Views/Accounts/AccountEditorView.xaml` | Add Explanation field |
| `ViewModels/Accounts/AccountEditorViewModel.cs` | Add Explanation property + validation |
| DB Migration | `ALTER TABLE Accounts ADD Explanation nvarchar(500) NULL` |

**Estimate**: ~2 hours

---

### Task 14 — Unit Tests (Verify Existing + Add Explanation Tests)

**Files to verify**:
- `Tests/Domain/AccountTests.cs` — test Explaination
- `Tests/Application/AccountServiceTests.cs` — test CreateAsync flow
- `Tests/Application/AccountCodeGeneratorServiceTests.cs` — test hierarchical codes
- `Tests/Api/AccountsControllerTests.cs` — test all endpoints

**Estimate**: ~2 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | Journal Entry lines, no money on Account entity | ✅ N/A |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | `AccountService.CreateAsync` wraps account + OB JE in transaction | ✅ |
| **RULE-006** | ALL services return `Result<T>` | `IAccountService` — all 7 methods | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | `NameAr`, `NameEn`, `Description`, `ColorCode`, `Notes` | ✅ |
| **RULE-016** | BaseEntity audit fields | Account inherits BaseEntity | ✅ |
| **RULE-023** | Domain has ZERO NuGet packages | Account entity — no dependencies | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | `AccountService` injects `IUnitOfWork` | ✅ |
| **RULE-035** | Serilog for logging | `AccountService` + `AccountCodeGeneratorService` log | ✅ |
| **RULE-036** | Log critical operations | Account create/update/delete, seed data | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All endpoints protected | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | `Create()`, `Update()`, `MarkAsDeleted()`, `HasChildren()` | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | `CreateAccountRequestValidator`, `UpdateAccountRequestValidator` | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Soft delete + Permanent delete | ✅ |
| **RULE-052** | Guard Clauses on all entities | 5+ guard clauses in Account.Create/Update | ✅ |
| **RULE-053** | DomainException in Arabic | All guards use Arabic messages | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use `IDialogService` | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified | ✅ |
| **RULE-058** | INotifyDataErrorInfo | `AccountEditorViewModel` | ✅ |
| **RULE-059** | Save always enabled, validate on click | No CanExecute predicate on Save | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All commands use `ExecuteAsync()` | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Account editor opens non-modally | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use `LogSystemError()` | ✅ |
| **RULE-172** | HandleFailure() transforms errors | `AccountsListViewModel` | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ الحساب"`, etc. | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | Verified | ✅ |
| **RULE-175** | Use Async suffix for dialogs | `ShowErrorAsync`, etc. | ✅ |
| **RULE-182** | Log.Error for system errors only | Verified | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | `AccountApiService` | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, inputs in Accounts views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | Verified | ✅ |
| **RULE-187** | Action buttons explain consequences | Delete button tooltip | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | MainWindow sidebar | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels | ✅ |
| **RULE-200** | Hard-delete catch DbUpdateException → Result.Failure | `AccountService.PermanentDeleteAsync` | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | `IAccountService` | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | `AccountsController` injects services only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | Self-ref FK, Category FK | ✅ |
| **RULE-220** | Newest-first sorting on lists | Sorted by AccountCode (appropriate for COA) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | `AccountEditorViewModel` | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | Uses AddError/ClearErrors | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation | ✅ |
| **RULE-262** | No hardcoded Height=36 on buttons | Compact 28px via styles | ✅ |
| **RULE-267** | Section headers FontSize=14 max | Account title: FontSize=14 | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | Account editor screen | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Seeder already run — can't re-seed** | **LOW** — seeder is idempotent via `AnyAsync()` check | Seeder skips if accounts exist; migrations handle new columns |
| **Existing JournalEntry references to Account IDs** | **LOW** — accounts are stable (first seed) | IDs are stable; no re-seeding needed |
| **Explanation field null for existing accounts** | **LOW** | Nullable column — `HasExplanation` in DTO handles visibility |
| **Arabic text encoding for 81 explanations** | **MEDIUM** | Verify UTF-8 with BOM before commit (RULE-250) |
| **InfoTooltip overlay crowds tree view** | **LOW** | Show ◉ only on hover or selected state |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **Explanation migration fails** | `ALTER TABLE Accounts DROP COLUMN Explanation` |
| **Explanation strings contain encoding errors** | Fix and re-run migration |
| **Unit tests fail** | Fix domain/service logic; rerun |

---

## 12. Self-Explanation ◉ Tooltips for All 81 Accounts

**Goal**: Every account in the Chart of Accounts shows a ◉ icon next to its name. On hover/click, a tooltip explains what this account means in plain Arabic.

**Implementation**:
1. Add `Explanation` column (`nvarchar(500)`) to Account entity — nullable
2. Seed all 81 accounts with explanations
3. Display ◉ icon next to account name in tree view + dropdown selections
4. API returns `Explanation` field in `AccountDto`

### 12.1 Complete Explanation Mapping — All 81 Accounts

#### Level 1 — Groups (5)

| Code | Name | Explanation |
|------|------|-------------|
| 1 | الأصول | "مجموعة الأصول — تمثل كل ما تملكه الشركة من ممتلكات وحقوق ذات قيمة مالية. تشمل الأصول المتداولة (النقد والمخزون) والأصول الثابتة (المباني والآلات)." |
| 2 | الخصوم | "مجموعة الخصوم — تمثل الالتزامات المالية التي تدين بها الشركة للغير. تشمل الخصوم المتداولة (الموردين، الضرائب) والخصوم طويلة الأجل (القروض)." |
| 3 | حقوق الملكية | "مجموعة حقوق الملكية — تمثل حصة أصحاب الشركة في أصولها بعد خصم الالتزامات. تشمل رأس المال والأرباح المحتجزة والاحتياطيات." |
| 4 | الإيرادات | "مجموعة الإيرادات — تمثل الدخل الذي تحققه الشركة من أنشطتها التجارية المختلفة. تشمل إيرادات المبيعات والإيرادات الأخرى." |
| 5 | المصروفات | "مجموعة المصروفات — تمثل التكاليف التي تتحملها الشركة لتحقيق الإيرادات. تشمل تكلفة المبيعات والمصروفات التشغيلية والإدارية." |

#### Level 2 — Main Categories (8)

| Code | Name | Explanation |
|------|------|-------------|
| 11 | أصول متداولة | "مجموعة الأصول التي يمكن تحويلها إلى نقد خلال سنة واحدة. تشمل: النقد بالصندوق، البنوك، حسابات العملاء، والمخزون." |
| 12 | أصول ثابتة | "مجموعة الأصول التي تستخدمها الشركة لأكثر من سنة وتستخدم في النشاط التشغيلي مثل: الأثاث، السيارات، المباني، والأجهزة." |
| 21 | التزامات متداولة | "مجموعة الالتزامات التي يجب سدادها خلال سنة واحدة. تشمل: حسابات الموردين، الضرائب المستحقة، والقروض قصيرة الأجل." |
| 31 | رأس المال والاحتياطيات | "مجموعة حسابات رأس المال — تشمل رأس المال المستثمر والمسحوبات الشخصية للمالك." |
| 32 | الأرباح والخسائر | "مجموعة حسابات الأرباح والخسائر — تشمل الأرباح المدورة والأرصدة الافتتاحية والأرباح غير الموزعة المستخدمة في الإقفال السنوي." |
| 41 | إيرادات النشاط | "مجموعة إيرادات النشاط التجاري الأساسي — تشمل إيرادات المبيعات والإيرادات الأخرى." |
| 51 | تكاليف النشاط | "مجموعة تكاليف النشاط التجاري — تشمل تكلفة البضاعة المباعة والمردودات." |
| 52 | مصاريف تشغيلية وإدارية | "مجموعة المصروفات التشغيلية والإدارية للمنشأة كالرواتب والإيجارات والمرافق العامة." |

#### Level 3 — Sub Categories (24)

| Code | Name | Explanation |
|------|------|-------------|
| 1101 | النقدية | "يشمل النقدية الموجودة في صندوق المنشأة والبنوك وما في حكمها من أموال سائلة." |
| 1102 | البنوك | "يشمل الحسابات الجارية والتوفير لدى البنوك المختلفة." |
| 1103 | العملاء | "يشمل المبالغ المستحقة للمنشأة لدى العملاء مقابل مبيعات آجلة." |
| 1104 | المخزون | "يشمل قيمة البضاعة الموجودة لدى المنشأة في بداية ونهاية الفترة المحاسبية." |
| 1105 | أصول متداولة أخرى | "يشمل الأصول المتداولة الأخرى غير المصنفة ضمن النقدية والبنوك والعملاء والمخزون كمصروفات مدفوعة مقدماً." |
| 1106 | تسوية المخزون | "حساب مؤقت يستخدم لتسوية فروق المخزون بين النظام والجرد الفعلي." |
| 1107 | عهد الموظفين | "يشمل المبالغ الممنوحة للموظفين كعهد أو سلف لاغراض العمل." |
| 1201 | أصول ثابتة ملموسة | "يشمل الأصول الثابتة المادية كالأثاث والمعدات وأجهزة الحاسب." |
| 1202 | أصول ثابتة غير ملموسة | "يشمل الأصول غير المادية كالبرمجيات وحقوق الملكية الفكرية." |
| 1203 | مجمع الإهلاك | "يشمل إهلاك الأصول الثابتة الملموسة المتراكم عبر عمرها الإنتاجي مع مخصصات الإهلاك." |
| 2101 | الموردون | "يشمل المبالغ المستحقة للموردين مقابل مشتريات آجلة." |
| 2102 | الضرائب | "يشمل الضرائب المستحقة للجهات الحكومية كضريبة المبيعات وضريبة المشتريات." |
| 2103 | التزامات متداولة أخرى | "يشمل الالتزامات المتداولة الأخرى غير المصنفة كثمن الأوراق الدائنة." |
| 3101 | رأس المال | "يشمل حساب رأس المال المستثمر في المنشأة." |
| 3102 | المسحوبات | "يشمل مسحوبات المالك من المنشأة للاستخدام الشخصي." |
| 3201 | أرباح مدورة | "يشمل الأرباح المتراكمة للمنشأة من الفترات السابقة." |
| 3202 | أرصدة افتتاحية | "حساب مؤقت يستخدم في بداية النظام لترحيل الأرصدة الافتتاحية قبل بدء التشغيل." |
| 3203 | أرباح غير موزعة | "يشمل الأرباح غير الموزعة المستخدمة في الإقفال السنوي للحسابات." |
| 4101 | إيرادات المبيعات | "يشمل الإيرادات الناتجة عن بيع المنتجات والسلع للعملاء." |
| 4102 | إيرادات أخرى | "يشمل الإيرادات غير التشغيلية كإيرادات النقل والخصم المكتسب وإيرادات التوصيل." |
| 5101 | تكلفة المبيعات | "يشمل تكلفة البضاعة المباعة خلال الفترة المحاسبية ويمثل المصروف الرئيسي لنشاط المنشأة." |
| 5102 | المردودات | "يشمل مردودات المبيعات ومردودات المشتريات من العملاء والموردين." |
| 5201 | مصروفات عمومية وإدارية | "يشمل المصروفات الإدارية والتشغيلية كالرواتب والإيجارات والكهرباء والمياه." |
| 5202 | مصروفات أخرى | "يشمل المصروفات غير التشغيلية كهالك المخزون وعجز المخزون وزيادة المخزون." |

#### Level 4 — Detail Accounts (44)

| Code | Name | Explanation |
|------|------|-------------|
| 11010001 | الصندوق | "النقد الموجود فعلياً في خزينة الشركة بالعملة المحلية. يزداد عند المقبوضات النقدية (المبيعات النقدية، تحصيل العملاء) وينقص عند المدفوعات النقدية (المشتريات النقدية، المصروفات)." |
| 11010002 | صندوق المصروفات النثرية | "المبلغ المخصص للمصروفات اليومية الصغيرة والنثرية كالقرطاسية ومواصلات الموظفين." |
| 11020001 | البنك الأهلي | "الحساب الجاري للشركة في البنك الأهلي. يزداد عند إيداع النقود أو استلام حوالات وينقص عند سحب النقود أو إصدار الشيكات." |
| 11020002 | بنك الرياض | "الحساب الجاري للشركة في بنك الرياض. يزداد عند إيداع النقود أو استلام حوالات وينقص عند سحب النقود أو إصدار الشيكات." |
| 11030001 | العميل النقدي | "حساب العميل الذي يشتري نقداً — يستخدم لتسجيل حركات العميل النقدي." |
| 11030002 | عملاء آجلون | "الأموال المستحقة للشركة من العملاء الذين اشتروا بضاعة بالأجل. يزداد عند بيع البضاعة بالأجل وينقص عند تحصيل المبالغ." |
| 11030003 | مخصص الديون المشكوك فيها | "مخصص للديون التي يشك في تحصيلها مستقبلاً ويظهر كحساب مقابل للأصول." |
| 11040001 | بضاعة أول المدة | "قيمة المخزون الموجود في بداية الفترة المالية قبل إجراء أي مشتريات أو مبيعات." |
| 11040002 | مخزون آخر المدة | "قيمة المخزون الموجود في نهاية الفترة المالية بعد إجراء الجرد الفعلي للمخازن." |
| 11050001 | مصروفات مدفوعة مقدماً | "مبالغ دفعتها الشركة مقدماً مقابل خدمات أو منافع ستحصل عليها في المستقبل مثل إيجار مدفوع مقدماً." |
| 11050002 | أوراق قبض | "كمبيالات وأوراق تجارية مستحقة للمنشأة لدى الغير قابلة للتحصيل." |
| 11070001 | عهدة الموظفين | "المبالغ الممنوحة للموظفين كعهد أو سلف لاغراض العمل على أن يتم تسويتها لاحقاً." |
| 12010001 | أثاث ومعدات | "قيمة الأثاث والتجهيزات المكتبية مثل المكاتب والكراسي والخزائن وأجهزة الكمبيوتر." |
| 12010002 | أجهزة حاسب آلي | "قيمة أجهزة الحاسب الآلي وملحقاتها المملوكة للشركة." |
| 12020001 | برمجيات | "قيمة البرمجيات وبرامج الحاسب وتراخيصها." |
| 12030001 | مخصص إهلاك الأثاث | "مجمع إهلاك الأثاث والمعدات المتراكم عبر عمرها الإنتاجي." |
| 12030002 | مخصص إهلاك الحاسب | "مجمع إهلاك أجهزة الحاسب الآلي المتراكم عبر عمرها الإنتاجي." |
| 21010001 | المورد النقدي | "حساب الموردين الذين يتم السداد لهم نقداً فور استلام البضاعة." |
| 21010002 | موردون آجلون | "الأموال المستحقة للموردين الذين اشترينا منهم بضاعة بالأجل. يزداد عند الشراء وينقص عند السداد." |
| 21020001 | ضريبة المبيعات (خرج) | "ضريبة القيمة المضافة التي تحصّلها الشركة من العملاء على المبيعات لحساب الحكومة." |
| 21020002 | ضريبة المشتريات (دخل) | "ضريبة القيمة المضافة التي تدفعها الشركة للموردين على المشتريات وتخصم من المستحق للهيئة." |
| 21030001 | أوراق دفع | "كمبيالات وأوراق تجارية مستحقة على المنشأة للغير." |
| 31010001 | رأس المال | "الأموال التي دفعها أصحاب الشركة لتأسيس النشاط التجاري وتمويل عملياته." |
| 31020001 | المسحوبات الشخصية | "المبالغ أو الأصول التي يسحبها المالك من المنشأة للاستخدام الشخصي." |
| 32010001 | أرباح مدورة | "الأرباح المتراكمة للمنشأة من الفترات السابقة التي لم توزع على الملاك." |
| 32020001 | رصيد افتتاحي | "حساب مؤقت يستخدم لترحيل الأرصدة الافتتاحية عند بدء استخدام النظام المحاسبي — لا يمكن تعديله أو حذفه." |
| 32030001 | أرباح غير موزعة | "الأرباح غير الموزعة التي ترحل نهاية السنة المالية في عملية الإقفال السنوي — لا يمكن تعديله أو حذفه." |
| 41010001 | إيرادات المبيعات | "إيرادات بيع المنتجات والسلع للعملاء سواء نقداً أو آجلاً. يمثل المصدر الرئيسي لدخل الشركة." |
| 41020001 | إيراد النقل | "الإيرادات الناتجة عن خدمات النقل والتوصيل التي تقدمها الشركة للعملاء." |
| 41020002 | الخصم المكتسب | "الخصومات التي تحصل عليها المنشأة من الموردين عند السداد المبكر لفواتير المشتريات." |
| 41020003 | إيرادات التوصيل | "الإيرادات الناتجة عن رسوم التوصيل التي يتحملها العملاء عند توصيل الطلبات." |
| 51010001 | تكلفة البضاعة المباعة | "التكلفة المباشرة للبضاعة التي تم بيعها خلال الفترة المحاسبية — تقابل إيراد المبيعات في قائمة الدخل." |
| 51020001 | مردودات مبيعات | "قيمة البضاعة التي أعادها العملاء بعد بيعها — تقلل من صافي إيراد المبيعات." |
| 51020002 | مردودات مشتريات | "قيمة البضاعة التي أعادتها المنشأة للموردين بعد شرائها — تقلل من صافي المشتريات." |
| 52010001 | مصروفات عمومية | "المصاريف اليومية لإدارة الشركة مثل: القرطاسية، فواتير البنك، الاشتراكات." |
| 52010002 | الرواتب والأجور | "المرتبات والأجور التي تدفعها الشركة للموظفين والعمال مقابل عملهم." |
| 52010003 | الكهرباء | "فواتير استهلاك الكهرباء الخاصة بمقر الشركة ومخازنها." |
| 52010004 | المياه | "فواتير استهلاك المياه الخاصة بمقر الشركة." |
| 52010005 | الإيجارات | "إيجار المباني والمكاتب والمستودعات التي تستخدمها الشركة." |
| 52010006 | النقل | "مصروفات النقل والمواصلات المتعلقة بنشاط المنشأة مثل شحن البضاعة." |
| 52010007 | الخصم المسموح به | "الخصومات التي تمنحها المنشأة للعملاء عند السداد المبكر أو بمناسبة عروض خاصة." |
| 52020001 | هالك المخزون | "الخسائر الناتجة عن تلف أو انتهاء صلاحية المخزون." |
| 52020002 | عجز مخزون | "الخسائر الناتجة عن نقص المخزون بين الرصيد المسجل في النظام والجرد الفعلي." |
| 52020003 | زيادة مخزون | "الإيرادات الناتجة عن زيادة المخزون بين الرصيد المسجل في النظام والجرد الفعلي." |

### 12.2 Implementation Details

**Account Entity Update**:
Add `Explanation` property (string?, nvarchar(500)) with `SetExplanation()` guard.

**Fluent API Configuration**:
`HasMaxLength(500)` for Explanation.

**AccountDto Update**:
Add `Explanation` and `HasExplanation` (bool => !string.IsNullOrEmpty(Explanation)).

**Seeder Update**:
Add Explanation column to the seed data in `AccountingSeeder.cs` — include all 81 explanation strings.

**Desktop UI — InfoTooltip Binding**:
Same pattern as Phase 18 InfoTooltip UserControl. Show ◉ icon next to account name in tree view, bound to `Explanation` with visibility gated by `HasExplanation`.

**Estimate**: ~2 hours
**Files**: 6-7 files modified

---

> **End of Phase 22 — Chart of Accounts Module Implementation Plan (v2.0)**
>
> **Key updates in v2.0**: Aligned with Phase 18 actual implementation.
> - Account entity already has Level, Description, ColorCode, Notes, AllowTransactions (computed)
> - No OpeningBalance on entity — handled via Journal Entry
> - 81 accounts already seeded (not 60), with hierarchical codes (1, 11, 1101, 11010001)
> - AccountDto, Service, Controller, Desktop UI all fully implemented in Phase 18
> - Remaining work: Explanation field for ◉ tooltips + unit test verification
>
> Total estimated remaining effort: **~4 hours** (Explanation field + tests)
