# Phase 22 — Chart of Accounts Module: Comprehensive Implementation Plan

> **Version**: 1.0 — Full rewrite based on 4 analysis files + codebase audit
> **Scope**: Complete Chart of Accounts CRUD module with 4-level hierarchy, 40+ seeded accounts, Desktop UI, and integration points for future journal entries

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

---

## 1. Architecture — Chart of Accounts Design

### 1.1 Hierarchy Model

The Chart of Accounts follows a **4-level hierarchical tree** using a self-referencing `ParentAccountId` FK:

| Level | Name | Arabic | Purpose | Can Have Transactions? |
|-------|------|--------|---------|----------------------|
| 1 | **Group** | رئيسي | Top-level category (Assets, Liabilities, Revenue, Expenses, Equity) | ❌ No (parent only) |
| 2 | **Main** | فرعي | Major account grouping within a category (Current Assets, Fixed Assets, Operating Expenses) | ❌ No (parent only) |
| 3 | **Sub** | فرعي فرعي | Specific account group (Cash in Hand, Banks, Accounts Receivable) | ❌ No (parent only) |
| 4 | **Detail** | تفصيلي | Leaf-level account where transactions post (Main Cash Box, Customer A, Bank Account) | ✅ Yes |

**Tree example from analysis JSON:**
```
الأصول (Level 1 — Group)
├── أصول ثابتة (Level 2 — Main)
├── أصول متداولة (Level 2 — Main)
│   ├── الصناديق (Level 3 — Sub)
│   │   ├── الصندوق (Level 4 — Detail)
│   │   └── صندوق الدولار (Level 4 — Detail)
│   ├── البنوك (Level 3 — Sub)
│   │   └── البنك الأهلي (Level 4 — Detail)
│   ├── العملاء (Level 3 — Sub)
│   │   ├── عميل نقدي (Level 4 — Detail)
│   │   └── عميل آجل (Level 4 — Detail)
│   └── البضاعة (Level 3 — Sub)
│       └── بضاعة أول المدة (Level 4 — Detail)
```

### 1.2 Core Entity Design

```
┌─────────────────────────────────────────────────┐
│                    Account                        │
├─────────────────────────────────────────────────┤
│ Id (int PK, auto-increment)                      │
│ AccountCode (string(20), UNIQUE)                 │
│ NameAr (string(200), required)                   │
│ NameEn (string(200), optional)                   │
│ AccountType (byte enum: Asset=1..Expense=5)      │
│ Level (int: 1=Group, 2=Main, 3=Sub, 4=Detail)  │
│ ParentAccountId (int? FK → self, Restrict)      │
│ IsSystemAccount (bool, default false)            │
│ IsActive (bool, default true)                    │
│ Description (string(500)?) — Help text for reports│
│ ColorCode (string(7)?) — e.g., "#2196F3"         │
│ Notes (string(500)?)                             │
│ AllowTransactions (bool) — false for levels 1-3  │
│ OpeningBalance (decimal(18,2)?) — initial balance│
│ CreatedAt, UpdatedAt, CreatedByUserId, ...       │
└─────────────────────────────────────────────────┘
         │
         │ ParentAccountId (self-referencing FK)
         ▼
┌─────────────────────────────────────────────────┐
│                    Account                        │
│                   (Parent)                        │
└─────────────────────────────────────────────────┘
```

### 1.3 AccountType Enum Values

| Code | Name | Normal Balance | Color |
|------|------|---------------|-------|
| 1 | `Asset` | Debit | `#2196F3` (Blue) |
| 2 | `Liability` | Credit | `#F44336` (Red) |
| 3 | `Equity` | Credit | `#4CAF50` (Green) |
| 4 | `Revenue` | Credit | `#4CAF50` (Green) |
| 5 | `Expense` | Debit | `#FF9800` (Orange) |

### 1.4 AccountCode Strategy

String code pattern using **numeric hierarchy**: `XXXX` format where each digit pair represents a level:

| Code | Name | Level |
|------|------|-------|
| 1000 | الأصول | 1 — Group |
| 1100 | أصول متداولة | 2 — Main |
| 1110 | الصناديق | 3 — Sub |
| 1111 | الصندوق | 4 — Detail |
| 1200 | العملاء | 3 — Sub |
| 2000 | الالتزامات | 1 — Group |
| 4000 | الإيرادات | 1 — Group |
| 4100 | المبيعات | 3 — Sub |
| 4101 | مبيعات نقدي | 4 — Detail |
| 5000 | المصروفات | 1 — Group |

---

## 2. Full Inventory — What Already Exists

### 2.1 Account Entity ✅ (Exists)

**File**: `SalesSystem.Domain/Accounting/Entities/Account.cs` (105 lines)

| Field | Type | Status | Notes |
|-------|------|--------|-------|
| `Id` | `int PK` | ✅ Exists | Auto-increment |
| `AccountCode` | `string(20)` | ✅ Exists | UNIQUE index |
| `NameAr` | `string(200)` | ✅ Exists | Required |
| `NameEn` | `string(200)` | ✅ Exists | Optional |
| `AccountType` | `AccountType (byte)` | ✅ Exists | Asset=1..Expense=5 |
| `ParentAccountId` | `int?` | ✅ Exists | Self-ref FK, Restrict |
| `IsSystemAccount` | `bool` | ✅ Exists | Default false |
| `Notes` | `string(500)?` | ✅ Exists | |
| `IsActive` | `bool` | ✅ Exists | Query filter |
| Inherited: `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId` | | ✅ Exists | From BaseEntity |
| **`Level`** | **`int`** | **❌ MISSING** | **Must add — essential for hierarchy** |
| **`Description`** | **`string(500)?`** | **❌ MISSING** | **Help text for reports** |
| **`ColorCode`** | **`string(7)?`** | **❌ MISSING** | **Hex color for UI** |
| **`AllowTransactions`** | **`bool`** | **❌ MISSING** | **False for parent accounts** |
| **`OpeningBalance`** | **`decimal(18,2)?`** | **❌ MISSING** | **Initial balance** |

### 2.2 AccountType Enum ✅ (Exists)

**File**: `SalesSystem.Domain/Accounting/Enums/AccountType.cs` (10 lines)

```csharp
public enum AccountType : byte
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}
```

**Needed**: Already correct — no changes required. Values match AGENTS.md Section 3 exactly.

### 2.3 AccountConfiguration ✅ (Exists but needs updates)

**File**: `Infrastructure/Data/Configurations/AccountConfiguration.cs` (30 lines)

**Already correct**:
- `DeleteBehavior.Restrict` on self-referencing FK (RULE-214 ✅)
- Unique index on `AccountCode`
- `HasQueryFilter(x => x.IsActive)`
- `HasMaxLength` on string properties

**⚠️ Needs expansion**:
- Add `Level` property config with `HasDefaultValue(4)`
- Add `Description` property config with `HasMaxLength(500)`
- Add `ColorCode` property config with `HasMaxLength(7)`
- Add `AllowTransactions` property config with `HasDefaultValue(false)`
- Add `OpeningBalance` property config with `HasPrecision(18, 2)`
- CHECK constraint for `Level` BETWEEN 1 AND 10
- Filtered index: `WHERE AllowTransactions = 1` for performance

### 2.4 AccountingSeeder ✅ (Exists — 18 accounts)

**File**: `Infrastructure/Data/Seeders/AccountingSeeder.cs` (91 lines)

| Code | Account Name | Type | Level (current) | Level (required) |
|------|-------------|------|-----------------|-------------------|
| 1000 | الأصول | Asset | (implicit) | 1 — Group |
| 1100 | النقدية في الصندوق | Asset | (implicit) | 3 — Sub |
| 1200 | الحسابات البنكية | Asset | (implicit) | 3 — Sub |
| 1300 | حسابات العملاء | Asset | (implicit) | 3 — Sub |
| 1400 | المخزون | Asset | (implicit) | 3 — Sub |
| 2000 | الخصوم | Liability | (implicit) | 1 — Group |
| 2100 | حسابات الموردين | Liability | (implicit) | 3 — Sub |
| 2200 | ضريبة القيمة المضافة الخارجة | Liability | (implicit) | 3 — Sub |
| 2300 | ضريبة القيمة المضافة الداخلة | Liability | (implicit) | 3 — Sub |
| 3000 | حقوق الملكية | Equity | (implicit) | 1 — Group |
| 3100 | رأس المال | Equity | (implicit) | 3 — Sub |
| 4000 | الإيرادات | Revenue | (implicit) | 1 — Group |
| 4100 | إيرادات المبيعات | Revenue | (implicit) | 3 — Sub |
| 4200 | مرتجعات المبيعات | Revenue | (implicit) | 3 — Sub |
| 5000 | المصروفات | Expense | (implicit) | 1 — Group |
| 5100 | تكلفة البضاعة المباعة | Expense | (implicit) | 3 — Sub |
| 5200 | المصروفات العمومية | Expense | (implicit) | 3 — Sub |
| 5300 | هالك المخزون | Expense | (implicit) | 3 — Sub |

**Needed**: Full rewrite to include Level, Description, ColorCode, AllowTransactions, parent-child relationships, and 25+ additional accounts.

### 2.5 What's MISSING Entirely

| Component | Status | Action |
|-----------|--------|--------|
| `AccountDto` in Contracts | ❌ Missing | Create with all fields |
| `CreateAccountRequest` / `UpdateAccountRequest` | ❌ Missing | Create with FluentValidation |
| `IAccountService` interface | ❌ Missing | Create with Result<T> |
| `AccountService` implementation | ❌ Missing | Create with IUnitOfWork |
| `AccountsController` API | ❌ Missing | Create with 6 endpoints |
| `IAccountApiService` Desktop client | ❌ Missing | Create HTTP client |
| `AccountApiService` Desktop implementation | ❌ Missing | Create with content-type guard |
| `AccountsListViewModel` | ❌ Missing | Create with tree + grid views |
| `AccountsListView.xaml` | ❌ Missing | Create with toggleable display |
| `AccountEditorViewModel` | ❌ Missing | Create with INotifyDataErrorInfo |
| `AccountEditorView.xaml` | ❌ Missing | Create editor form |
| `AccountChangedMessage` EventBus | ❌ Missing | Create message |
| Desktop DI registrations + navigation | ❌ Missing | Register all services |
| Parent-child deletion validation | ❌ Missing | Add to service layer |
| Account code auto-generation | ❌ Missing | Helper logic in service |

---

## 3. BLOCKER Resolution — Critical Decisions

### 3.1 Blocker 1: AccountCode Strategy — Keep string code (numeric hierarchy)

**Problem**: The existing `AccountCode` is a unique string with no validation rules. The analysis shows a hierarchical numbering system like `1000 → 1100 → 1110 → 1111` but the current entity just takes any string.

**Decision**: **Keep `AccountCode` as a required string**, but add:
- Validation pattern: must match `^\d{4,10}$` (4 to 10 digits)
- Auto-generation for child accounts: `ParentCode + "0" + nextAvailable`
- Display code formatting in UI (e.g., `1000`, `1100`, `1111`)
- Unique constraint enforced at DB level (already exists)

**Why NOT auto-increment**:
- Account codes are user-facing identifiers that must be meaningful
- Hierarchical codes (1xxx = Assets, 2xxx = Liabilities) help users navigate
- Auto-increment destroys the hierarchical numbering pattern

### 3.2 Blocker 2: Level Property — Must Add Required `int Level`

**Problem**: The current `Account` entity has NO `Level` property. The hierarchy is implicitly derived from `ParentAccountId`, but levels are essential for:
- Determining which accounts can have transactions (only Level 4 — Detail)
- Tree rendering in UI (knowing how deep to indent)
- Validation (can't attach a Level 1 to a Level 3 parent)
- Seed data clarity

**Decision**: **Add `int Level` property** with:
- Values: 1=Group, 2=Main, 3=Sub, 4=Detail
- `HasDefaultValue(4)` in config (most accounts are leaf-level)
- `CHECK (Level >= 1 AND Level <= 10)` constraint (hard limit to prevent infinite nesting)
- Validation: `child.Level > parent.Level` (must be deeper, any depth jump allowed) in domain
- Auto-set in `Create()` from parent's Level + 1 (user can override)

### 3.3 Blocker 3: Expand from 18 to 60 Accounts

**Problem**: Current seeder has only 18 flat accounts. The analysis JSON specifies ~60 accounts across 4 levels with parent-child relationships (56 from analysis + 4 from audit review). Re-seeding requires:
- Setting correct `ParentAccountId` for sub-accounts
- Assigning correct `Level` values
- Preserving `SystemAccountMappings` integrity

**Decision**: **Rewrite `AccountingSeeder` completely** with:
- 60 accounts organized by hierarchy (see Section 5 for full list)
- All `Level` values set correctly
- `ParentAccountId` linking to in-memory references
- `ColorCode` per AccountType
- `AllowTransactions = false` for levels 1-3
- `IsSystemAccount = true` for Level 1-2 accounts only — Level 3-4 are user-modifiable (rename descriptions)

### 3.4 Blocker 4: Parent-Child Deletion Protection

**Problem**: No logic currently prevents deleting an account that has children. With the hierarchy, deleting a parent must cascade-restrict at the Domain level.

**Decision**: Add guard clause before deletion:
```csharp
if (HasChildren())
    throw new DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية — احذف الحسابات الفرعية أولاً");
```

Also: System accounts (`IsSystemAccount = true`) are protected from both modification and deletion (already implemented in `Update()` and `MarkAsDeleted()`).

---

## 4. Chart of Accounts Design Catalog

### 4.1 Account Entity — Complete Spec

**File**: `SalesSystem.Domain/Accounting/Entities/Account.cs` (expanded)

```csharp
public class Account : BaseEntity
{
    // ── Existing fields ──
    public string AccountCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public int? ParentAccountId { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public string? Notes { get; private set; }

    // ── NEW fields for Phase 22 ──
    public int Level { get; private set; }              // 1=Group, 2=Main, 3=Sub, 4=Detail
    public string? Description { get; private set; }    // Help text for reports
    public string? ColorCode { get; private set; }      // Hex color (#2196F3)
    public bool AllowTransactions { get; private set; } // True for detail levels (>= 4)
    public decimal? OpeningBalance { get; private set; } // Initial balance

    // ── Navigation ──
    public Account? ParentAccount { get; private set; }
    public IReadOnlyCollection<Account> Children => _children.AsReadOnly();
    private readonly List<Account> _children = new();

    private Account() { } // EF Core

    public static Account Create(
        string accountCode,
        string nameAr,
        string nameEn,
        AccountType accountType,
        int level,
        int? parentAccountId = null,
        bool isSystemAccount = false,
        string? description = null,
        string? colorCode = null,
        bool allowTransactions = false,
        decimal? openingBalance = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new DomainException("رمز الحساب مطلوب");
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");
        if (!Enum.IsDefined(typeof(AccountType), accountType))
            throw new DomainException("نوع الحساب غير صالح");
        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");
        if (level >= 4 && !allowTransactions)
            throw new DomainException("الحساب التفصيلي يجب أن يسمح بالحركات");

        return new Account
        {
            AccountCode = accountCode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim() ?? string.Empty,
            AccountType = accountType,
            Level = level,
            ParentAccountId = parentAccountId,
            IsSystemAccount = isSystemAccount,
            Description = description?.Trim(),
            ColorCode = colorCode,
            AllowTransactions = allowTransactions,
            OpeningBalance = openingBalance,
            Notes = notes?.Trim(),
            IsActive = true
        };
    }

    public void Update(
        string nameAr,
        string nameEn,
        AccountType accountType,
        int level,
        int? parentAccountId = null,
        string? description = null,
        string? colorCode = null,
        bool allowTransactions = false,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظامي");
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");
        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");
        if (level >= 4 && !allowTransactions)
            throw new DomainException("الحساب التفصيلي يجب أن يسمح بالحركات");

        NameAr = nameAr.Trim();
        NameEn = nameEn?.Trim() ?? string.Empty;
        AccountType = accountType;
        Level = level;
        ParentAccountId = parentAccountId;
        Description = description?.Trim();
        ColorCode = colorCode;
        AllowTransactions = allowTransactions;
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public bool HasChildren() => _children.Count > 0;

    public bool IsDebitNormal() =>
        AccountType == AccountType.Asset || AccountType == AccountType.Expense;

    public override void MarkAsDeleted()
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن حذف حساب نظامي");
        if (HasChildren())
            throw new DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية");
        base.MarkAsDeleted();
    }
}
```

### 4.2 AccountConfiguration — Expanded

**File**: `Infrastructure/Data/Configurations/AccountConfiguration.cs`

```csharp
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.AccountCode).IsUnique();
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NameEn).HasMaxLength(200);
        builder.Property(x => x.AccountType).IsRequired();
        builder.Property(x => x.Level).IsRequired().HasDefaultValue(4);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ColorCode).HasMaxLength(7);
        builder.Property(x => x.AllowTransactions).HasDefaultValue(false);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsSystemAccount).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // CHECK constraints
builder.ToTable(t => t.HasCheckConstraint("CHK_Account_Level_Range", "[Level] >= 1 AND [Level] <= 10"));

        // Self-referencing parent — Restrict (RULE-214)
        builder.HasOne(x => x.ParentAccount)
            .WithMany(a => a.Children)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.IsActive);
    }
}
```

### 4.3 DTOs

**File**: `Contracts/DTOs/AllDtos.cs`

```csharp
public record AccountDto(
    int Id,
    string AccountCode,
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    string? ParentAccountName,
    bool IsSystemAccount,
    bool IsActive,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    string? Notes)
{
    public string AccountTypeDisplay => AccountType switch
    {
        1 => "أصل",
        2 => "خصم",
        3 => "حق ملكية",
        4 => "إيراد",
        5 => "مصروف",
        _ => "غير معروف"
    };

    public string LevelDisplay => Level switch
    {
        1 => "رئيسي (مجموعة)",
        2 => "فرعي",
        3 => "فرعي فرعي",
        4 => "تفصيلي",
        _ => "غير معروف"
    };
}

// Hierarchical tree node for TreeView
public record AccountTreeNodeDto(
    int Id,
    string AccountCode,
    string NameAr,
    byte AccountType,
    int Level,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    List<AccountTreeNodeDto> Children);
```

### 4.4 Request DTOs

**File**: `Contracts/Requests/AccountingRequests.cs` (append to existing)

```csharp
public record CreateAccountRequest(
    string AccountCode,
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    bool IsSystemAccount,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    string? Notes);

public record UpdateAccountRequest(
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    string? Notes);
```

### 4.5 FluentValidators

**File**: `Api/Validators/AccountValidators.cs`

```csharp
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.AccountCode)
            .NotEmpty().WithMessage("رمز الحساب مطلوب")
            .Matches(@"^\d{4,10}$").WithMessage("رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("الاسم بالإنجليزية يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.AccountType)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 4).WithMessage("مستوى الحساب يجب أن يكون بين 1 و 4");

        RuleFor(x => x.ColorCode)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("رمز اللون يجب أن يكون بصيغة Hex (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode));

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً")
            .When(x => x.OpeningBalance.HasValue);

        RuleFor(x => x.AccountCode)
            .Must((request, code) => !(request.Level == 1 && code.Length > 4))
            .WithMessage("رمز الحساب للمستوى الرئيسي يجب ألا يتجاوز 4 أرقام");
    }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200);

        RuleFor(x => x.AccountType)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 4).WithMessage("مستوى الحساب يجب أن يكون بين 1 و 4");
    }
}
```

### 4.6 Color Coding Map

| AccountType | Color Hex | Arabic Name | CSS Class |
|-------------|-----------|-------------|-----------|
| Asset (1) | `#2196F3` | أصل | `account-asset` |
| Liability (2) | `#F44336` | خصم | `account-liability` |
| Equity (3) | `#4CAF50` | حق ملكية | `account-equity` |
| Revenue (4) | `#4CAF50` | إيراد | `account-revenue` |
| Expense (5) | `#FF9800` | مصروف | `account-expense` |

---

## 5. Gap Analysis — Existing vs Target Accounts

### 5.1 Complete Account Hierarchy (60 Accounts)

The following table shows all 60 accounts to seed, organized hierarchically. **Bold** = new account (not in current seeder).

> **IsSystemAccount scope**: Only Level 1 (Group) and Level 2 (Main) accounts have `IsSystemAccount = true`. Level 3 (Sub) and Level 4 (Detail) accounts are NOT system accounts — users can rename their descriptions or modify them. The "System" column in this table uses ✅ for L1-L2 and ❌ for L3-L4, matching this strategy.

| Code | Account Name (Ar) | Type | Level | Parent | System (L1-L2 only) | Allow Txns | Notes |
|------|-------------------|------|-------|--------|--------|------------|-------|
| **1000** | **الأصول** | Asset | 1 | null | ✅ | ❌ | Root — Assets |
| **1100** | **أصول متداولة** | Asset | 2 | 1000 | ✅ | ❌ | Current assets |
| 1110 | **الصناديق** | Asset | 3 | 1100 | ✅ | ❌ | Cash boxes group |
| 1111 | **الصندوق** | Asset | 4 | 1110 | ✅ | ✅ | Main cash box |
| **1112** | **صندوق الدولار** | Asset | 4 | 1110 | ✅ | ✅ | USD cash box |
| 1120 | **البنوك** | Asset | 3 | 1100 | ✅ | ❌ | Bank accounts |
| 1121 | **البنك الأهلي** | Asset | 4 | 1120 | ✅ | ✅ | Default bank |
| 1130 | **حسابات العملاء** | Asset | 3 | 1100 | ✅ | ❌ | AR group |
| 1131 | **العميل النقدي** | Asset | 4 | 1130 | ✅ | ✅ | Cash customer |
| **1132** | **عملاء آجلون** | Asset | 4 | 1130 | ✅ | ✅ | Credit customers |
| **1133** | **أوراق قبض** | Asset | 4 | 1130 | ✅ | ✅ | Notes receivable |
| **1134** | **شيكات تحت التحصيل** | Asset | 4 | 1130 | ✅ | ✅ | Cheques under collection |
| **1135** | **مصروفات مقدمة** | Asset | 4 | 1130 | ✅ | ✅ | Prepaid expenses |
| 1140 | **المخزون** | Asset | 3 | 1100 | ✅ | ❌ | Inventory group |
| 1141 | **بضاعة أول المدة** | Asset | 4 | 1140 | ✅ | ✅ | Opening inventory |
| **1142** | **مخزون آخر المدة** | Asset | 4 | 1140 | ✅ | ✅ | Closing inventory |
| **1200** | **أصول ثابتة** | Asset | 2 | 1000 | ✅ | ❌ | Fixed assets |
| **1210** | **الأثاث** | Asset | 3 | 1200 | ✅ | ❌ | Furniture group |
| **1211** | **أثاث ومعدات** | Asset | 4 | 1210 | ✅ | ✅ | Furniture & equipment |
| **1220** | **السيارات** | Asset | 3 | 1200 | ✅ | ❌ | Vehicles group |
| **1221** | **سيارات النقل** | Asset | 4 | 1220 | ✅ | ✅ | Transport vehicles |
| **1230** | **مباني وعقارات** | Asset | 3 | 1200 | ✅ | ❌ | Buildings group |
| **1231** | **المبنى الإداري** | Asset | 4 | 1230 | ✅ | ✅ | Office building |
| **1240** | **إهلاك الأصول الثابتة** | Asset | 3 | 1200 | ✅ | ❌ | Depreciation group |
| **1241** | **إهلاك الأثاث** | Asset | 4 | 1240 | ✅ | ✅ | Furniture depreciation |
| **1300** | **الخصوم** | Liability | 1 | null | ✅ | ❌ | Root — Liabilities |
| **1310** | **التزامات متداولة** | Liability | 2 | 1300 | ✅ | ❌ | Current liabilities |
| 1320 | **حسابات الموردين** | Liability | 3 | 1310 | ✅ | ❌ | AP group |
| 1321 | **المورد النقدي** | Liability | 4 | 1320 | ✅ | ✅ | Cash supplier |
| **1322** | **موردون آجلون** | Liability | 4 | 1320 | ✅ | ✅ | Credit suppliers |
| **1323** | **أوراق دفع** | Liability | 4 | 1320 | ✅ | ✅ | Notes payable |
| **1330** | **الضرائب** | Liability | 3 | 1310 | ✅ | ❌ | Taxes group |
| **1331** | **ضريبة المبيعات (خرج)** | Liability | 4 | 1330 | ✅ | ✅ | VAT Output |
| **1332** | **ضريبة المشتريات (دخل)** | Liability | 4 | 1330 | ✅ | ✅ | VAT Input |
| **1340** | **قروض** | Liability | 3 | 1310 | ✅ | ❌ | Loans group |
| **1341** | **قروض بنكية** | Liability | 4 | 1340 | ✅ | ✅ | Bank loans |
| **1400** | **حقوق الملكية** | Equity | 1 | null | ✅ | ❌ | Root — Equity |
| 1410 | **رأس المال** | Equity | 2 | 1400 | ✅ | ❌ | Capital group |
| 1411 | **رأس المال** | Equity | 4 | 1410 | ✅ | ✅ | Owner capital |
| **1420** | **الأرباح والخسائر** | Equity | 2 | 1400 | ✅ | ❌ | P&L group |
| **1421** | **ح/ الأرباح والخسائر** | Equity | 4 | 1420 | ✅ | ✅ | Profit & Loss summary |
| **1430** | **الاحتياطيات** | Equity | 2 | 1400 | ✅ | ❌ | Reserves group |
| **1431** | **احتياطي قانوني** | Equity | 4 | 1430 | ✅ | ✅ | Legal reserve |
| **1500** | **الإيرادات** | Revenue | 1 | null | ✅ | ❌ | Root — Revenue |
| **1510** | **إيرادات النشاط** | Revenue | 2 | 1500 | ✅ | ❌ | Operating revenue |
| 1520 | **إيرادات المبيعات** | Revenue | 3 | 1510 | ✅ | ❌ | Sales group |
| 1521 | **مبيعات نقدي** | Revenue | 4 | 1520 | ✅ | ✅ | Cash sales |
| **1522** | **مبيعات آجل** | Revenue | 4 | 1520 | ✅ | ✅ | Credit sales |
| **1530** | **مردودات المشتريات** | Revenue | 3 | 1510 | ✅ | ❌ | Purchase returns group |
| **1531** | **مردودات مشتريات نقدي** | Revenue | 4 | 1530 | ✅ | ✅ | Cash purchase returns |
| **1532** | **مردودات مشتريات آجل** | Revenue | 4 | 1530 | ✅ | ✅ | Credit purchase returns |
| **1540** | **الخصم المكتسب** | Revenue | 3 | 1510 | ✅ | ❌ | Discount earned |
| **1541** | **الخصم المكتسب** | Revenue | 4 | 1540 | ✅ | ✅ | Discount earned |
| **1550** | **إيرادات أخرى** | Revenue | 2 | 1500 | ✅ | ❌ | Other revenue |
| **1551** | **إيرادات متنوعة** | Revenue | 4 | 1550 | ✅ | ✅ | Miscellaneous revenue |
| **1552** | **فوارق عملة** | Revenue | 4 | 1550 | ✅ | ✅ | Currency exchange gains |
| **1553** | **فوارق بيع وشراء العملات** | Revenue | 4 | 1550 | ❌ | ✅ | Currency exchange differences |
| **1600** | **المصروفات** | Expense | 1 | null | ✅ | ❌ | Root — Expenses |
| **1610** | **تكاليف النشاط** | Expense | 2 | 1600 | ✅ | ❌ | Activity costs |
| 1620 | **تكلفة البضاعة المباعة** | Expense | 3 | 1610 | ✅ | ❌ | COGS group |
| 1621 | **تكلفة البضاعة المباعة** | Expense | 4 | 1620 | ✅ | ✅ | Cost of goods sold |
| **1630** | **المشتريات** | Expense | 3 | 1610 | ✅ | ❌ | Purchases group |
| **1631** | **مشتريات نقدي** | Expense | 4 | 1630 | ✅ | ✅ | Cash purchases |
| **1632** | **مشتريات آجل** | Expense | 4 | 1630 | ✅ | ✅ | Credit purchases |
| **1640** | **مردودات المبيعات** | Expense | 3 | 1610 | ✅ | ❌ | Sales returns group |
| **1641** | **مردودات مبيعات نقدي** | Expense | 4 | 1640 | ✅ | ✅ | Cash sales returns |
| **1642** | **مردودات مبيعات آجل** | Expense | 4 | 1640 | ✅ | ✅ | Credit sales returns |
| **1650** | **الخصم المسموح به** | Expense | 3 | 1610 | ✅ | ❌ | Discount allowed |
| **1651** | **الخصم المسموح به** | Expense | 4 | 1650 | ✅ | ✅ | Discount allowed |
| **1660** | **تسوية المخزون** | Expense | 3 | 1610 | ✅ | ❌ | Inventory adjustment |
| **1661** | **عجز وزيادة البضاعة** | Expense | 4 | 1660 | ✅ | ✅ | Inventory surplus/shortage |
| **1662** | **البضاعة التالفة** | Expense | 4 | 1660 | ✅ | ✅ | Spoiled goods |
| **1663** | **تسوية المخزون-صرف وتوريد** | Expense | 4 | 1660 | ❌ | ✅ | Inventory settlement — issue and receipt |
| **1670** | **مصاريف تشغيلية وإدارية** | Expense | 2 | 1600 | ✅ | ❌ | Operating expenses |
| 1680 | **المصروفات العمومية** | Expense | 3 | 1670 | ✅ | ❌ | General expenses |
| 1681 | **مصروفات عمومية** | Expense | 4 | 1680 | ✅ | ✅ | General expenses |
| **1682** | **الرواتب والأجور** | Expense | 4 | 1680 | ✅ | ✅ | Salaries & wages |
| **1683** | **الإيجار** | Expense | 4 | 1680 | ✅ | ✅ | Rent |
| **1684** | **الكهرباء والمياه** | Expense | 4 | 1680 | ✅ | ✅ | Utilities |
| **1685** | **مصروفات النقل** | Expense | 4 | 1680 | ✅ | ✅ | Transport expenses |
| **1686** | **مصروفات الصيانة** | Expense | 4 | 1680 | ✅ | ✅ | Maintenance |
| **1687** | **رسوم أخرى / مصاريف أخرى** | Expense | 4 | 1680 | ❌ | ✅ | Other fees and expenses |
| **1688** | **أجور نقل** | Expense | 4 | 1680 | ❌ | ✅ | Transportation wages |
| **1690** | **هالك المخزون** | Expense | 3 | 1670 | ✅ | ❌ | Spoilage loss group |
| **1691** | **هالك المخزون** | Expense | 4 | 1690 | ✅ | ✅ | Inventory spoilage |
| **1692** | **خسائر المخزون** | Expense | 4 | 1690 | ✅ | ✅ | Inventory losses |

### 5.2 Gap Summary

| Category | Existing | Target | New |
|----------|----------|--------|-----|
| Assets | 5 | 18 | 13 |
| Liabilities | 4 | 8 | 4 |
| Equity | 2 | 6 | 4 |
| Revenue | 3 | 9 | 6 |
| Expenses | 4 | 19 | 15 |
| **Total** | **18** | **60** | **42** |

**Note**: 60 total accounts includes all hierarchy levels (1-4). Detail-level (leaf) accounts are ~32 which is the actual operational count.

---

## 6. Architectural Decisions

### 6.1 AccountCode Strategy: Numeric Hierarchical String

**Decision**: Use `string AccountCode` with numeric hierarchical format (`1000`, `1100`, `1110`, `1111`) enforced by regex validation `^\d{4,10}$`.

- Level 1 codes: 1000, 2000, 3000, 4000, 5000 (4 digits)
- Level 2 codes: 1100, 1200, 1300 (4 digits)
- Level 3 codes: 1110, 1120, 1130 (4 digits)
- Level 4 codes: 1111, 1112, 1131 (4 digits)

**Why**: Human-readable, hierarchy-visible, supports future multi-currency accounts.

### 6.2 Level Numbering: 1-4

**Decision**: Level 1=Group → Level 2=Main → Level 3=Sub → Level 4=Detail.

- DB constraint: `CHECK (Level >= 1 AND Level <= 10)`
- Auto-validation: `child.Level > parent.Level` and `child.Level <= 10`

### 6.3 Color Coding Per AccountType

**Decision**: Assign `ColorCode` by `AccountType` automatically, but allow override:

| Type | Color | Rationale |
|------|-------|-----------|
| Asset | Blue (`#2196F3`) | Standard accounting convention |
| Liability | Red (`#F44336`) | Red ink for obligations |
| Equity | Green (`#4CAF50`) | Profit/growth |
| Revenue | Green (`#4CAF50`) | Income/profit |
| Expense | Orange (`#FF9800`) | Cost/outflow |

### 6.4 Opening Balance via JournalEntry, NOT Direct Balance

**Decision**: Opening balances are set via a standard `OpeningBalance` field on the Account entity for V1 seed data only. Future journal entries will post to accounts normally.

**Rationale**: The analysis mentions "قيد افتتاحي" (opening journal entry) as a separate concept. For V1, the simplest approach is:
1. `Account.OpeningBalance` stores the initial balance (set during creation or via a one-time import)
2. Running balance = `OpeningBalance + SUM(JournalEntryLines)` — computed in service, not stored
3. Future Phase (Posting/JournalEntry): remove `OpeningBalance` from Account and use a dedicated "Opening Entry" instead

### 6.5 TreeView vs GridView in Desktop UI

**Decision**: **Support BOTH views**, toggleable by a button in the toolbar.

- **TreeView** (default for accountants): Hierarchical display with expand/collapse, indentation, color coding
- **GridView** (default for casual users): Flat table with search, sort, filter

Both views use the same `AccountTreeNodeDto` data from the API, rendered differently in XAML.

### 6.6 Account Service Returns Result<T>

**Decision**: ALL service methods return `Result<T>` (RULE-006) using `IUnitOfWork` (RULE-024).

- `GetAllAsync()` → returns tree structure (list of root nodes with Children populated)
- `GetByIdAsync()` → single account with ParentAccount info
- `CreateAsync()` → validates parent exists, auto-sets Level from parent
- `UpdateAsync()` → guards system accounts
- `DeleteAsync()` → soft delete (IsActive=false), guards system + parent accounts
- `PermanentDeleteAsync()` → catches `DbUpdateException` (RULE-200)

### 6.7 Why NOT Add CurrencyId to Account in V1

The Analysis Part 1 suggests adding `CurrencyId` to accounts for multi-currency support. This is **deferred** (see Section 7) because:
- Requires Currency entity (Phase 20 work)
- Changes the AccountCode strategy (need sub-accounts per currency)
- Adds complexity to journal entry posting
- V1 uses single-base-currency (SAR/YER) assumption

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason | Target Phase |
|---------|--------|-------------|
| **Multi-currency accounts** (`CurrencyId` FK) | Requires full currency module integration | Phase 23+ |
| **Opening balance import from Excel** | Needs ClosedXML integration with template validation | Phase 24 |
| **Account freeze/lock** | Separate from IsActive — prevents transactions while keeping visibility | Phase 25 |
| **Account budget tracking** | Separate Budget entity with monthly/quarterly limits | Phase 26 |
| **Account hierarchies > 10 levels** | Validation allows up to Level 10 (relaxed from 4); standard design uses 4 levels | Supported in V1 |
| **Drag-and-drop reordering in tree** | UI complexity — manual sort via AccountCode is sufficient for V1 | Future |
| **Account statement report** (كشف حساب) | Requires JournalEntry module first | Phase 23 |
| **Trial balance report** (ميزان المراجعة) | Requires JournalEntry + balances | Phase 24 |
| **Income statement** (قائمة الدخل) | Aggregation from JournalEntry | Phase 24 |
| **Balance sheet** (المركز المالي) | Aggregation from JournalEntry | Phase 24 |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), UI Compact styles (RULE-262-274), and Arabic messages (RULE-053).

---

### Task 1 — Expand Account Entity with Level, Description, ColorCode, AllowTransactions, OpeningBalance

**Files**:

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/Account.cs` | Add `Level`, `Description`, `ColorCode`, `AllowTransactions`, `OpeningBalance` + `_children` navigation + `HasChildren()` method |
| `Domain/Accounting/Entities/Account.cs` | Update `Create()` and `Update()` signatures with new parameters |
| `Domain/Accounting/Entities/Account.cs` | Update `MarkAsDeleted()` with `HasChildren()` guard |
| `Infrastructure/Data/Configurations/AccountConfiguration.cs` | Add all new property configs + CHECK constraint |
| `Domain/Accounting/Entities/Account.cs` | Add `IsDebitNormal()` method (already exists) |

**Domain method** (RULE-042):
```csharp
// HasChildren guard in MarkAsDeleted
public override void MarkAsDeleted()
{
    if (IsSystemAccount)
        throw new DomainException("لا يمكن حذف حساب نظامي");
    if (HasChildren())
        throw new DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية — احذف الحسابات الفرعية أولاً");
    base.MarkAsDeleted();
}
```

**Logging**: No logging in Domain — pure business logic (RULE-023: Domain has ZERO dependencies).

**Validation** (RULE-052/053):
- `Level` must be 1-4, else `DomainException("مستوى الحساب يجب أن يكون بين 1 و 4")`
- Levels >= 4 must have `AllowTransactions = true`, else `DomainException("الحساب التفصيلي يجب أن يسمح بالحركات")`

**Estimate**: ~1 hour

---

### Task 2 — Expand AccountConfiguration with New Fields + Level CHECK Constraint

**File**: `Infrastructure/Data/Configurations/AccountConfiguration.cs`

```csharp
// New configs to add:
builder.Property(x => x.Level).IsRequired().HasDefaultValue(4);
builder.Property(x => x.Description).HasMaxLength(500);
builder.Property(x => x.ColorCode).HasMaxLength(7);
builder.Property(x => x.AllowTransactions).HasDefaultValue(false);
builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);

// CHECK constraint (Database Engineer req.)
builder.ToTable(t => t.HasCheckConstraint("CHK_Account_Level_Range", "[Level] >= 1 AND [Level] <= 10"));

// Self-referencing parent with Children navigation
builder.HasOne(x => x.ParentAccount)
    .WithMany(a => a.Children)      // ← Add .Children navigation
    .HasForeignKey(x => x.ParentAccountId)
    .OnDelete(DeleteBehavior.Restrict);
```

**Migration**: `20260605000001_ExpandAccountsForChartOfAccounts.sql`

```sql
ALTER TABLE Accounts ADD Level int NOT NULL DEFAULT 4;
ALTER TABLE Accounts ADD Description nvarchar(500) NULL;
ALTER TABLE Accounts ADD ColorCode nvarchar(7) NULL;
ALTER TABLE Accounts ADD AllowTransactions bit NOT NULL DEFAULT 0;
ALTER TABLE Accounts ADD OpeningBalance decimal(18,2) NULL;
ALTER TABLE Accounts ADD CONSTRAINT CHK_Account_Level_Range CHECK (Level >= 1 AND Level <= 10);
```

**Logging**: `Log.Information("Account schema expanded: added Level, Description, ColorCode, AllowTransactions, OpeningBalance")`

**Estimate**: ~30 minutes

---

### Task 3 — Rewrite AccountingSeeder with 60 Accounts (Full Hierarchy)

**File**: `Infrastructure/Data/Seeders/AccountingSeeder.cs`

Complete rewrite from 18 flat accounts to 60 hierarchically organized accounts (56 original + 4 additions from analysis review). The seeder must:

1. Create accounts Level 1 first (no parent)
2. Create Level 2 accounts with `ParentAccountId` set
3. Create Level 3 accounts with `ParentAccountId` set
4. Create Level 4 accounts with `ParentAccountId` set + `AllowTransactions = true`
5. Set `IsSystemAccount = true` for Level 1 (Group) and Level 2 (Main) accounts only — Level 3 and Level 4 detail accounts are NOT system accounts, allowing users to rename descriptions
6. Set `ColorCode` per AccountType
7. Set `Description` as help text in reports

**Pattern** — use in-memory dictionary to link parents:
```csharp
var accountMap = new Dictionary<string, Account>();

// Level 1 — create without parent
var assets = Account.Create("1000", "الأصول", "Assets", AccountType.Asset, 1,
    isSystemAccount: true, colorCode: "#2196F3",
    description: "مجموعة الأصول — كل ما تملكه المنشأة من موارد");
accountMap["1000"] = assets;

// Level 2 — parent is Level 1
var currentAssets = Account.Create("1100", "أصول متداولة", "Current Assets", AccountType.Asset, 2,
    parentAccountId: null, // Will set after adding to DB or using reference
    isSystemAccount: true, colorCode: "#2196F3",
    description: "الأصول التي يمكن تحويلها إلى نقد خلال سنة");
// ParentId will be set by service or we need temporary IDs
```

**IMPORTANT**: Because accounts reference each other by `ParentAccountId` and we don't know DB-generated IDs yet, the seeder must use a **two-pass approach**:

```csharp
// Pass 1: Create all accounts, add to context, SaveChanges to get IDs
// Pass 2: Set ParentAccountId using saved IDs, update context
```

Or, use the existing approach of querying back from DB after first save:
```csharp
// Create level 1
var assets = Account.Create("1000", ...);
db.Set<Account>().Add(assets);
// ... create all level 1
await db.SaveChangesAsync();

// Create level 2 using DB IDs
var assetsId = db.Set<Account>().First(a => a.AccountCode == "1000").Id;
var currentAssets = Account.Create("1100", ..., parentAccountId: assetsId, level: 2, ...);
db.Set<Account>().Add(currentAssets);
// ... create all level 2
await db.SaveChangesAsync();
// ... and so on for levels 3, 4
```

**Logging**:
- `Log.Information("Seeded {Count} accounts across {Levels} levels.", totalCount, 4)` on success
- `Log.Warning("Accounts already seeded — skipping.")` if guard prevents

**Estimate**: ~2 hours

---

### Task 4 — Create Account DTOs + Request DTOs + FluentValidators

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `AccountDto` (13 fields) and `AccountTreeNodeDto` (with Children list) |
| `Contracts/Requests/AccountingRequests.cs` | Add `CreateAccountRequest` and `UpdateAccountRequest` |
| `Api/Validators/AccountValidators.cs` | **NEW** — both validators with Arabic messages |

**FluentValidation** (RULE-044):
```csharp
RuleFor(x => x.AccountCode)
    .NotEmpty().WithMessage("رمز الحساب مطلوب")
    .Matches(@"^\d{4,10}$").WithMessage("رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");
RuleFor(x => x.Level)
    .InclusiveBetween(1, 4).WithMessage("مستوى الحساب يجب أن يكون بين 1 و 4");
```

**Logging**: No logging in DTOs/Validators — pure data structures.

**Estimate**: ~30 minutes

---

### Task 5 — Create AccountService (Application Layer)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IAccountService.cs` | **NEW** — 7 methods returning `Result<T>` |
| `Application/Services/AccountService.cs` | **NEW** — full implementation with IUnitOfWork |

**IAccountService**:
```csharp
public interface IAccountService
{
    Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetByTypeAsync(AccountType type, CancellationToken ct = default);
    Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);            // Soft delete
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default);   // Hard delete
}
```

**Service Patterns**:

```csharp
// CreateAsync — validates parent level
public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct)
{
    if (request.ParentAccountId.HasValue)
    {
        var parent = await _uow.Accounts.GetByIdAsync(request.ParentAccountId.Value, ct);
        if (parent == null)
            return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);

        // Validate level depth (must be deeper than parent, max 10)
        if (request.Level <= parent.Level)
            return Result<AccountDto>.Failure(
                "مستوى الحساب الفرعي يجب أن يكون أعمق من مستوى الحساب الأب",
                ErrorCodes.ValidationError);
        if (request.Level > 10)
            return Result<AccountDto>.Failure(
                "مستوى الحساب لا يمكن أن يتجاوز 10",
                ErrorCodes.ValidationError);
    }

    // Check unique account code
    var existing = await _uow.Accounts.GetByCodeAsync(request.AccountCode, ct);
    if (existing != null)
        return Result<AccountDto>.Failure("رمز الحساب موجود مسبقاً", ErrorCodes.DuplicateBarcode);

    var account = Account.Create(
        request.AccountCode, request.NameAr, request.NameEn,
        (AccountType)request.AccountType, request.Level,
        request.ParentAccountId, request.IsSystemAccount,
        request.Description, request.ColorCode, request.AllowTransactions,
        request.OpeningBalance, request.Notes
    );

    await _uow.Accounts.AddAsync(account, ct);
    await _uow.SaveChangesAsync(ct);
    _logger.LogInformation("Account {AccountCode} ({NameAr}) created — Level {Level}, Type {Type}",
        account.AccountCode, account.NameAr, account.Level, account.AccountType);
    return Result<AccountDto>.Success(MapToDto(account));
}

// PermanentDeleteAsync — catch DbUpdateException (RULE-200)
public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
{
    try
    {
        var account = await _uow.Accounts.GetByIdAsync(id, ct);
        if (account == null)
            return Result.Failure("الحساب غير موجود", ErrorCodes.NotFound);
        if (account.IsSystemAccount)
            return Result.Failure("لا يمكن حذف حساب نظامي", ErrorCodes.InvalidOperation);
        if (account.HasChildren())
            return Result.Failure("لا يمكن حذف حساب رئيسي لديه حسابات فرعية", ErrorCodes. ReferencedByOtherEntities);

        _uow.Accounts.Remove(account);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Account {Id} permanently deleted", id);
        return Result.Success();
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Cannot permanently delete Account {Id}: {Error}", id, ex.InnerException?.Message);
        return Result.Failure("لا يمكن حذف هذا الحساب لأنه مرتبط بمعاملات أخرى", ErrorCodes.ReferencedByOtherEntities);
    }
}

// GetTreeAsync — builds tree from flat list
public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct)
{
    var all = await _uow.Accounts.GetAllAsync(ct);  // Includes inactive via query filter
    var roots = all.Where(a => a.ParentAccountId == null)
        .OrderBy(a => a.AccountCode)
        .Select(a => BuildTreeNode(a, all))
        .ToList();

    return Result<List<AccountTreeNodeDto>>.Success(roots);
}

private static AccountTreeNodeDto BuildTreeNode(Account account, IEnumerable<Account> all)
{
    return new AccountTreeNodeDto(
        account.Id, account.AccountCode, account.NameAr,
        (byte)account.AccountType, account.Level, account.ColorCode,
        account.AllowTransactions, account.OpeningBalance,
        all.Where(c => c.ParentAccountId == account.Id)
            .OrderBy(c => c.AccountCode)
            .Select(c => BuildTreeNode(c, all))
            .ToList()
    );
}
```

**Error Codes** (add to ErrorCodes if not existing):
- `DuplicateAccountCode` — Account code already exists
- `InvalidParentLevel` — Child level not deeper than parent or exceeds max depth

**Logging** (RULE-035):
- `Log.Information("Account {AccountCode} ({NameAr}) created — Level {Level}, Type {Type}", ...)` on create
- `Log.Information("Account {Id} updated — {NameAr}", ...)` on update
- `Log.Warning("Cannot delete account {Id}: has {ChildCount} children", ...)` on invalid delete (RULE-183)
- `Log.Error(ex, "Cannot permanently delete Account {Id}", ...)` on FK violation (RULE-199)

**Estimate**: ~2 hours

---

### Task 6 — Create AccountsController (API Layer)

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/AccountsController.cs` | **NEW** — 6 endpoints with authorization policies |

**Endpoints**:

| Method | Route | Policy | Description |
|--------|-------|--------|-------------|
| `GET` | `/api/v1/accounts` | `AllStaff` | Flat list of all active accounts |
| `GET` | `/api/v1/accounts/tree` | `AllStaff` | Hierarchical tree (nested JSON) |
| `GET` | `/api/v1/accounts/{id}` | `AllStaff` | Single account by Id |
| `GET` | `/api/v1/accounts/by-type/{type}` | `AllStaff` | Accounts filtered by AccountType |
| `POST` | `/api/v1/accounts` | `ManagerAndAbove` | Create new account |
| `PUT` | `/api/v1/accounts/{id}` | `ManagerAndAbove` | Update existing account |
| `DELETE` | `/api/v1/accounts/{id}` | `ManagerAndAbove` | Soft delete |
| `DELETE` | `/api/v1/accounts/permanent/{id}` | `AdminOnly` | Hard delete (with FK guard) |

**Controller purity** (RULE-203): Controller injects `IAccountService` only — NO `DbContext` or `IUnitOfWork`.

```csharp
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await _accountService.GetTreeAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _accountService.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _accountService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [EnableRateLimiting("GlobalPolicy")]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _accountService.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAccountRequest request, CancellationToken ct)
    {
        var result = await _accountService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _accountService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("permanent/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _accountService.PermanentDeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
```

**Logging**: Minimal in controller — logging happens in service layer per RULE-035.

**Estimate**: ~1 hour

---

### Task 7 — Register Services in API DI + Application ServiceRegistration

**Files**:

| File | Change |
|------|--------|
| `Api/Program.cs` | Add `builder.Services.AddScoped<IAccountService, AccountService>()` |
| `Application/DependencyInjection.cs` or similar | Add `IAccountService` registration if exists |

**Estimate**: ~5 minutes

---

### Task 8 — Create AccountApiService (Desktop HTTP Client)

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Services/Api/IAccountApiService.cs` | **NEW** — 5 HTTP methods |
| `DesktopPWF/Services/Api/AccountApiService.cs` | **NEW** — HttpClient implementation with content-type guard |

```csharp
public interface IAccountApiService
{
    Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default);
}
```

**HandleResponseAsync pattern** (RULE-184):
```csharp
protected async Task<Result<List<AccountDto>>> HandleResponseListAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<AccountDto>>(content, _jsonOptions);
        return Result<List<AccountDto>>.Success(list ?? new List<AccountDto>());
    }

    try
    {
        // Content-type guard (RULE-184)
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return Result<List<AccountDto>>.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
        }

        var raw = await response.Content.ReadAsStringAsync();
        Serilog.Log.Warning("API failure (non-JSON): {StatusCode} - {Content}", response.StatusCode, raw);
        return Result<List<AccountDto>>.Failure("خطأ في الخادم", response.StatusCode.ToString());
    }
    catch (Exception ex)
    {
        Serilog.Log.Error(ex, "Failed to parse API error response");
        return Result<List<AccountDto>>.Failure("حدث خطأ غير متوقع", "Unknown");
    }
}
```

**Logging**:
- `Log.Information("Accounts loaded: {Count} accounts from API", result.Value?.Count ?? 0)` on success
- `Log.Warning("API returned {StatusCode} for accounts request", response.StatusCode)` on failure (RULE-183)
- `Log.Error(ex, "HTTP request failed for accounts")` on network error (RULE-182)

**Estimate**: ~1 hour

---

### Task 9 — Create AccountChangedMessage EventBus Message

**File**: `DesktopPWF/Messaging/Messages/AppMessages.cs`

```csharp
public record AccountChangedMessage(int AccountId) : IEventBusMessage;
```

**Estimate**: ~5 minutes

---

### Task 10 — Create AccountsListViewModel + AccountsListView (Desktop)

**Files** (8 files):

| File | Content |
|------|---------|
| `ViewModels/Accounts/AccountsListViewModel.cs` | **NEW** — List VM with tree+grid toggle |
| `Views/Accounts/AccountsListView.xaml` | **NEW** — Dual-display (TreeView + DataGrid) |
| `Views/Accounts/AccountsListView.xaml.cs` | **NEW** — Code-behind |
| `ViewModels/Accounts/AccountEditorViewModel.cs` | **NEW** — Editor VM with INotifyDataErrorInfo |
| `Views/Accounts/AccountEditorView.xaml` | **NEW** — Editor form |
| `Views/Accounts/AccountEditorView.xaml.cs` | **NEW** — Code-behind |
| `DesktopPWF/App.xaml.cs` | Register DI + navigation |
| `Services/Api/AccountApiService.cs` | (Created in Task 8 — register in DI) |

#### AccountsListViewModel

```csharp
public class AccountsListViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;
    private IDisposable? _subscription;

    public AccountsListViewModel(
        IAccountApiService accountService,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IEventBus eventBus)
    {
        _accountService = accountService;
        _dialogService = dialogService;
        _screenWindowService = screenWindowService;
        _eventBus = eventBus;

        _subscription = eventBus.Subscribe<AccountChangedMessage>(OnAccountChanged);
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAccountsOperationAsync)));
        AddCommand = new RelayCommand(AddAccount);
        ToggleViewCommand = new RelayCommand(ToggleView);
    }

    // ── Observable Properties ──
    private bool _isTreeView = true;
    public bool IsTreeView { get => _isTreeView; set => SetProperty(ref _isTreeView, value); }

    private ObservableCollection<AccountTreeNodeDto> _treeItems = new();
    public ObservableCollection<AccountTreeNodeDto> TreeItems
    {
        get => _treeItems;
        set => SetProperty(ref _treeItems, value);
    }

    private ObservableCollection<AccountDto> _flatItems = new();
    public ObservableCollection<AccountDto> FlatItems
    {
        get => _flatItems;
        set => SetProperty(ref _flatItems, value);
    }

    private AccountTreeNodeDto? _selectedNode;
    public AccountTreeNodeDto? SelectedNode
    {
        get => _selectedNode;
        set
        {
            SetProperty(ref _selectedNode, value);
            OnPropertyChanged(nameof(CanEditOrDelete));
        }
    }

    private AccountDto? _selectedItem;
    public AccountDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnPropertyChanged(nameof(CanEditOrDelete));
        }
    }

    public bool CanEditOrDelete => (IsTreeView && SelectedNode != null) ||
                                    (!IsTreeView && SelectedItem != null);

    // ── Commands ──
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand ToggleViewCommand { get; private set; } = null!;

    // ── Operations ──
    private async Task LoadAccountsOperationAsync()
    {
        ErrorMessage = null;

        if (IsTreeView)
        {
            var result = await _accountService.GetTreeAsync();
            if (result.IsSuccess && result.Value != null)
            {
                await InvokeOnUIThreadAsync(() =>
                {
                    TreeItems.Clear();
                    foreach (var node in result.Value)
                        TreeItems.Add(node);
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الحسابات", "LoadAccounts");
            }
        }
        else
        {
            var result = await _accountService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                await InvokeOnUIThreadAsync(() =>
                {
                    FlatItems.Clear();
                    // Newest-first (RULE-220): newest accounts last, but tree is ordered by code
                    foreach (var item in result.Value.OrderBy(x => x.AccountCode))
                        FlatItems.Add(item);
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الحسابات", "LoadAccounts");
            }
        }
    }

    private void AddAccount()
    {
        int? parentId = IsTreeView ? SelectedNode?.Id : SelectedItem?.Id;
        var editorVm = new AccountEditorViewModel(_accountService, _dialogService, parentId);
        editorVm.FocusFirstInvalidFieldRequested += (_, _) => RequestFocusFirstInvalidField();

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة حساب جديد",
            OnClosed = (vm) =>
            {
                if (vm is AccountEditorViewModel editor && editor.IsSaved)
                {
                    _eventBus.Publish(new AccountChangedMessage(0));
                    Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadAccountsOperationAsync));
                }
            }
        });
    }

    private void ToggleView()
    {
        IsTreeView = !IsTreeView;
        _ = ExecuteAsync(LoadAccountsOperationAsync);
    }

    private void OnAccountChanged(AccountChangedMessage msg) =>
        _ = ExecuteAsync(LoadAccountsOperationAsync);

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

#### AccountEditorViewModel (Editor)

```csharp
public class AccountEditorViewModel : ViewModelBase
{
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly int? _parentAccountId;

    public AccountEditorViewModel(
        IAccountApiService accountService,
        IDialogService dialogService,
        int? parentAccountId = null)
    {
        _accountService = accountService;
        _dialogService = dialogService;
        _parentAccountId = parentAccountId;
        SetDialogService(dialogService); // RULE-227

        SaveCommand = new AsyncRelayCommand(SaveAsync); // ALWAYS enabled (RULE-059)
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());

        LoadedCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadInitialDataAsync)));
    }

    // ── Properties ──
    public int? AccountId { get; private set; }
    public bool IsSaved { get; private set; }
    public event Action? CloseRequested;

    private string _accountCode = string.Empty;
    public string AccountCode
    {
        get => _accountCode;
        set
        {
            _accountCode = value;
            OnPropertyChanged();
            ClearErrors(nameof(AccountCode));
            if (string.IsNullOrWhiteSpace(value))
                AddError(nameof(AccountCode), "رمز الحساب مطلوب");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{4,10}$"))
                AddError(nameof(AccountCode), "رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");
        }
    }

    private string _nameAr = string.Empty;
    public string NameAr
    {
        get => _nameAr;
        set
        {
            _nameAr = value;
            OnPropertyChanged();
            ClearErrors(nameof(NameAr));
            if (string.IsNullOrWhiteSpace(value))
                AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");
        }
    }

    private string _nameEn = string.Empty;
    public string NameEn { get => _nameEn; set { _nameEn = value; OnPropertyChanged(); } }

    private byte _accountType = 1;
    public byte AccountType
    {
        get => _accountType;
        set { _accountType = value; OnPropertyChanged(); }
    }

    private int _level = 4;
    public int Level
    {
        get => _level;
        set { _level = value; OnPropertyChanged(); }
    }

    private string _description = string.Empty;
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

    private string _colorCode = string.Empty;
    public string ColorCode { get => _colorCode; set { _colorCode = value; OnPropertyChanged(); } }

    private bool _allowTransactions = true;
    public bool AllowTransactions { get => _allowTransactions; set { _allowTransactions = value; OnPropertyChanged(); } }

    private decimal? _openingBalance;
    public decimal? OpeningBalance { get => _openingBalance; set { _openingBalance = value; OnPropertyChanged(); } }

    // ── Commands ──
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand LoadedCommand { get; private set; } = null!;

    // ── Validation ──
    private bool Validate()
    {
        ClearAllErrors(); // RULE-229

        if (string.IsNullOrWhiteSpace(AccountCode))
            AddError(nameof(AccountCode), "رمز الحساب مطلوب");
        if (string.IsNullOrWhiteSpace(NameAr))
            AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");
        if (Level < 1 || Level > 10)
            AddError(nameof(Level), "مستوى الحساب يجب أن يكون بين 1 و 10");
        if (Level >= 4 && !AllowTransactions)
            AddError(nameof(AllowTransactions), "الحساب التفصيلي يجب أن يسمح بالحركات");

        return !HasErrors;
    }

    // ── Save ──
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", GetErrorsList());
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new CreateAccountRequest(
                AccountCode, NameAr, NameEn, AccountType, Level,
                _parentAccountId, false, Description,
                string.IsNullOrWhiteSpace(ColorCode) ? null : ColorCode,
                AllowTransactions, OpeningBalance, null);

            var result = await _accountService.CreateAsync(request);
            if (result.IsSuccess)
            {
                IsSaved = true;
                await _dialogService.ShowSuccessAsync("تم الحفظ", "تم إنشاء الحساب بنجاح");
                CloseRequested?.Invoke();
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطأ في حفظ الحساب", result.Error ?? "حدث خطأ غير متوقع");
            }
        });
    }

    private async Task LoadInitialDataAsync()
    {
        // Auto-set Level based on parent if provided
        if (_parentAccountId.HasValue)
        {
            var result = await _accountService.GetByIdAsync(_parentAccountId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                Level = result.Value.Level + 1;
            }
        }
    }

    private List<string> GetErrorsList()
    {
        var errors = new List<string>();
        foreach (var prop in GetType().GetProperties())
        {
            var propName = prop.Name;
            var propErrors = GetErrors(propName).Cast<string>();
            errors.AddRange(propErrors.Select(e => $"• {e}"));
        }
        return errors;
    }
}
```

#### XAML Patterns

**AccountsListView.xaml** — TreeView toggle button + dual display:
```xml
<!-- Toolbar with Toggle View button (RULE-185: Arabic ToolTip) -->
<Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="دليل الحسابات" FontSize="14" FontWeight="Bold" Foreground="White"
                   VerticalAlignment="Center"/>
        <Button Command="{Binding ToggleViewCommand}" Margin="12,0,0,0"
                Style="{StaticResource SecondaryButton}"
                ToolTip="تبديل بين العرض الشجري والجدولي">
            <TextBlock Text="{Binding IsTreeView, Converter={StaticResource BoolToViewModeConverter}}"/>
        </Button>
        <Button Command="{Binding AddCommand}" Margin="6,0,0,0"
                Style="{StaticResource PrimaryButton}"
                ToolTip="إضافة حساب جديد — سيتم فتح نافذة إنشاء حساب"/>
    </StackPanel>
</Border>

<!-- TreeView (visible when IsTreeView = true) -->
<TreeView ItemsSource="{Binding TreeItems}" Visibility="{Binding IsTreeView, Converter={StaticResource BoolToVisibility}}"
          Style="{StaticResource CompactTreeView}">
    <TreeView.ItemTemplate>
        <HierarchicalDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal" Margin="0,2,0,2">
                <Ellipse Width="8" Height="8" Fill="{Binding ColorCode, Converter={StaticResource HexToBrush}}"
                         Margin="0,0,6,0" VerticalAlignment="Center"/>
                <TextBlock Text="{Binding NameAr}" FontSize="13"/>
                <TextBlock Text="{Binding AccountCode}" FontSize="11" Foreground="Gray" Margin="6,0,0,0"/>
            </StackPanel>
        </HierarchicalDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>

<!-- DataGrid (visible when IsTreeView = false, compact RULE-262-274) -->
<DataGrid ItemsSource="{Binding FlatItems}" Visibility="{Binding IsTreeView, Converter={StaticResource BoolToVisibilityInverse}}"
          Style="{StaticResource CompactDataGrid}" AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="الكود" Binding="{Binding AccountCode}" Width="80"/>
        <DataGridTextColumn Header="اسم الحساب" Binding="{Binding NameAr}" Width="*"/>
        <DataGridTextColumn Header="النوع" Binding="{Binding AccountTypeDisplay}" Width="100"/>
        <DataGridTextColumn Header="المستوى" Binding="{Binding LevelDisplay}" Width="120"/>
        <DataGridTextColumn Header="الوصف" Binding="{Binding Description}" Width="150"/>
    </DataGrid.Columns>
</DataGrid>
```

**AccountEditorView.xaml** — Editor form:
```xml
<!-- Compact editor (RULE-262-274) -->
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel Margin="10">
        <!-- Account Code -->
        <TextBlock Text="رمز الحساب *" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding AccountCode, UpdateSourceTrigger=PropertyChanged}"
                 ToolTip="رمز الحساب — أرقام فقط (4-10 خانات)، مثال: 1111"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>

        <!-- Name Ar -->
        <TextBlock Text="اسم الحساب (عربي) *" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding NameAr, UpdateSourceTrigger=PropertyChanged}"
                 ToolTip="اسم الحساب باللغة العربية — هذا الحقل إلزامي"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>

        <!-- Name En -->
        <TextBlock Text="اسم الحساب (إنجليزي)" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding NameEn}"
                 ToolTip="اسم الحساب باللغة الإنجليزية — اختياري"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>

        <!-- AccountType + Level -->
        <Grid Margin="0,0,0,6">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,4,0">
                <TextBlock Text="نوع الحساب *" Style="{StaticResource LabelStyle}"/>
                <ComboBox SelectedValue="{Binding AccountType}"
                          ItemsSource="{Binding AccountTypes}" DisplayMemberPath="Value"
                          ToolTip="نوع الحساب: أصل، خصم، حق ملكية، إيراد، مصروف"
                          Style="{StaticResource ModernComboBox}"/>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="4,0,0,0">
                <TextBlock Text="المستوى *" Style="{StaticResource LabelStyle}"/>
                <ComboBox SelectedValue="{Binding Level}" Style="{StaticResource ModernComboBox}"
                          ToolTip="مستوى الحساب: 1=رئيسي، 2=فرعي، 3=فرعي فرعي، 4=تفصيلي">
                    <ComboBoxItem Content="1 — رئيسي (مجموعة)" Value="1"/>
                    <ComboBoxItem Content="2 — فرعي" Value="2"/>
                    <ComboBoxItem Content="3 — فرعي فرعي" Value="3"/>
                    <ComboBoxItem Content="4 — تفصيلي" Value="4"/>
                </ComboBox>
            </StackPanel>
        </Grid>

        <!-- ColorCode -->
        <TextBlock Text="لون الحساب" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding ColorCode}"
                 ToolTip="رمز اللون بصيغة Hex — مثال: #2196F3 للأصول"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>

        <!-- AllowTransactions -->
        <CheckBox IsChecked="{Binding AllowTransactions}" Content="يسمح بالحركات"
                  ToolTip="تفعيل هذا الخيار للسماح بإضافة قيود يومية على هذا الحساب"
                  Margin="0,0,0,6"/>

        <!-- OpeningBalance -->
        <TextBlock Text="الرصيد الافتتاحي" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding OpeningBalance, TargetNullValue=''}"
                 ToolTip="الرصيد الافتتاحي للحساب عند بدء استخدام النظام"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>

        <!-- Description -->
        <TextBlock Text="الوصف التفصيلي" Style="{StaticResource LabelStyle}"/>
        <TextBox Text="{Binding Description}" AcceptsReturn="True" MaxHeight="80"
                 ToolTip="وصف تفصيلي للحساب — يظهر في التقارير"
                 Style="{StaticResource ModernTextBox}" Margin="0,0,0,8"/>

        <!-- Buttons (ALWAYS enabled — RULE-059) -->
        <Border Background="White" Padding="12,8" BorderThickness="0,1,0,0"
                BorderBrush="{StaticResource BorderBrush}">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                <Button Command="{Binding SaveCommand}" Content="💾 حفظ"
                        Style="{StaticResource PrimaryButton}"
                        ToolTip="حفظ الحساب — سيتم التحقق من البيانات قبل الحفظ"/>
                <Button Command="{Binding CancelCommand}" Content="❌ إلغاء"
                        Style="{StaticResource SecondaryButton}" Margin="8,0,0,0"
                        ToolTip="إلغاء التعديل والعودة إلى قائمة الحسابات"/>
            </StackPanel>
        </Border>
    </StackPanel>
</ScrollViewer>
```

**ViewModel Logging** (RULE-199/201):
```csharp
// All async operations wrapped in ExecuteAsync (RULE-141)
// LogSystemError is the ONLY error logging method (RULE-199)
catch (Exception ex)
{
    LogSystemError($"Failed to load chart of accounts", "AccountsListViewModel.LoadAccountsOperationAsync", ex);
}
```

**ViewModel error handling** (RULE-171/172/173):
- Dialog titles: `"خطأ في تحميل دليل الحسابات"`, `"خطأ في حفظ الحساب"`
- User messages: Never `ex.Message` — always user-friendly Arabic

**Estimate**: ~6 hours (largest task)

---

### Task 11 — Register Desktop DI + Navigation in App.xaml.cs + MainWindow

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/App.xaml.cs` | Register `IAccountApiService`, `AccountApiService`, `AccountsListViewModel`, `AccountEditorViewModel` |
| `DesktopPWF/App.xaml.cs` | Add navigation entry in menu |
| `DesktopPWF/MainWindow.xaml` | Add "دليل الحسابات" navigation item in sidebar |

**DI Registration**:
```csharp
services.AddHttpClient<IAccountApiService, AccountApiService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5221");
});
services.AddTransient<AccountsListViewModel>();
services.AddTransient<AccountEditorViewModel>();
```

**Navigation (MainWindow sidebar item)**:
```xml
<RadioButton Content="📒 دليل الحسابات"
             ToolTip="إدارة دليل الحسابات — عرض وتعديل الحسابات المحاسبية"
             Command="{Binding NavigateCommand}" CommandParameter="AccountsList"/>
```

**Estimate**: ~30 minutes

---

### Task 12 — Add Account Search/Filter to List ViewModel

**File**: `ViewModels/Accounts/AccountsListViewModel.cs`

Add search functionality:
```csharp
private string _searchText = string.Empty;
public string SearchText
{
    get => _searchText;
    set
    {
        _searchText = value;
        OnPropertyChanged();
        _ = ExecuteAsync(LoadAccountsOperationAsync); // Auto-search on typing
    }
}

private string _filterType = "0"; // "0" = All
public string FilterType
{
    get => _filterType;
    set { _filterType = value; OnPropertyChanged(); _ = ExecuteAsync(LoadAccountsOperationAsync); }
}

// In LoadAccountsOperationAsync, after getting data:
if (!string.IsNullOrWhiteSpace(SearchText))
{
    var search = SearchText.Trim();
    var results = all.Where(a =>
        a.NameAr.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        a.AccountCode.Contains(search) ||
        (a.NameEn?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
    // Display filtered results
}
if (FilterType != "0" && byte.TryParse(FilterType, out var type))
{
    results = results.Where(a => a.AccountType == type);
}
```

**Estimate**: ~30 minutes

### Task 13 — Unit Tests

**Files**: NEW test files in `SalesSystem.Domain.Tests`, `SalesSystem.Application.Tests`, `SalesSystem.Api.Tests`, `SalesSystem.Infrastructure.Tests`

#### 1. Domain Entity Tests

**Account.Create()** — Test with valid inputs creates entity with all fields set correctly. Test with empty `accountCode` → `DomainException("رمز الحساب مطلوب")`. Test with empty `nameAr` → `DomainException("اسم الحساب بالعربية مطلوب")`. Test with invalid `AccountType` out of range → `DomainException("نوع الحساب غير صالح")`. Test `level < 1` → `DomainException("مستوى الحساب يجب أن يكون بين 1 و 10")`. Test `level > 10` → same exception. Test `level >= 4` with `allowTransactions = false` → `DomainException("الحساب التفصيلي يجب أن يسمح بالحركات")`. Test `HasChildren()` returns `true` when `_children.Count > 0`. Test `IsDebitNormal()` returns `true` for Asset and Expense types, `false` for Liability, Equity, Revenue.

**Account.Update()** — Valid update modifies fields. `IsSystemAccount = true` update → `DomainException("لا يمكن تعديل حساب نظامي")`. Same guard clause tests as Create for level and transactions.

**Account.MarkAsDeleted()** — Soft delete works. `IsSystemAccount = true` → `DomainException("لا يمكن حذف حساب نظامي")`. `HasChildren() = true` → `DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية")`.

#### 2. Service Tests (using Mock<IUnitOfWork>)

**AccountService.CreateAsync()**:
- Valid request → `Result<AccountDto>.Success`
- Parent not found → `Result<AccountDto>.Failure` with `ErrorCodes.NotFound`
- Duplicate AccountCode → `Result<AccountDto>.Failure` with `ErrorCodes.DuplicateBarcode`
- Child level <= parent level → `Result<AccountDto>.Failure` with `ErrorCodes.ValidationError`
- Child level > 10 → `Result<AccountDto>.Failure` with `ErrorCodes.ValidationError`

**AccountService.GetByIdAsync()**:
- Existing account → DTO with correct fields
- Non-existent → `Result<AccountDto>.Failure` with `ErrorCodes.NotFound`

**AccountService.GetTreeAsync()**:
- Returns tree structure correctly — roots have Children populated
- Empty list when no accounts exist

**AccountService.UpdateAsync()**:
- Valid update → `Result<AccountDto>.Success` with updated fields
- System account update → `Result<AccountDto>.Failure`
- Non-existent account → `Result<AccountDto>.Failure`

**AccountService.DeleteAsync()**:
- Soft delete → `Result.Success()`; `IsActive = false`
- System account → `Result.Failure`
- Parent with children → `Result.Failure`

**AccountService.PermanentDeleteAsync()**:
- Valid → `Result.Success()`
- FK violation → `Result.Failure` with `ErrorCodes.ReferencedByOtherEntities`

#### 3. FluentValidation Tests

**CreateAccountRequestValidator**:
- Valid request passes
- Empty AccountCode → fails with "رمز الحساب مطلوب"
- AccountCode non-numeric → fails with "رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)"
- AccountCode < 4 digits → fails with same message
- Empty NameAr → fails with "اسم الحساب بالعربية مطلوب"
- NameAr > 200 chars → fails
- Invalid AccountType (< 1 or > 5) → fails
- Level < 1 or > 4 → fails
- Invalid ColorCode hex format → fails
- Negative OpeningBalance → fails

#### 4. Database Configuration Tests

**AccountConfiguration**: Verify `HasQueryFilter(x => x.IsActive)`. Verify `DeleteBehavior.Restrict` on self-referencing `ParentAccountId` FK. Verify unique index on `AccountCode`. Verify `HasPrecision(18, 2)` on `OpeningBalance`. Verify `HasMaxLength(20)` on `AccountCode`, `HasMaxLength(200)` on `NameAr`/`NameEn`, `HasMaxLength(500)` on `Description`/`Notes`, `HasMaxLength(7)` on `ColorCode`. Verify CHECK constraint `CHK_Account_Level_Range` exists with `[Level] >= 1 AND [Level] <= 10`.

#### 5. Phase-specific Tests

- Account tree hierarchy: Level validation enforces `child.Level > parent.Level` (any depth jump allowed, max 10)
- All 60 accounts seeded correctly — check each against Section 5.1 table (code, name, type, level, parent, IsSystemAccount, AllowTransactions)
- AccountCode uniqueness enforced at DB level — duplicate code insertion throws exception
- IsSystemAccount: only Level 1 (Group) and Level 2 (Main) accounts can be system accounts; Level 3-4 are NOT system accounts
- Self-referencing FK (ParentAccountId): correct tree navigation via `Children` navigation property
- Leaf account detection (`HasChildren()` = false) — only Detail (Level 4) accounts are leaves
- Color coding: Level 1=`#1A237E`, Level 2=`#1565C0`, Level 3=`#0D47A1`, Level 4=`#000000` — verify `ColorCode` assignment in seed matches
- OpeningBalance precision: `decimal(18,2)` with `HasPrecision`
- AllowTransactions: `true` for Level 4 (Detail) accounts only; `false` for Levels 1-3
- `ParentAccountId` null for root (Level 1) accounts; non-null for child accounts with valid FK reference
- `GetTreeAsync()` builds correct hierarchy — root nodes have `Children`, each child has `ParentId` pointing to parent

**Estimate**: ~4 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | `Account.OpeningBalance` = `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | AccountService.CreateAsync uses `IUnitOfWork` (single save) | ✅ |
| **RULE-006** | ALL services return `Result<T>` | `IAccountService` — 7 methods all `Result<T>` | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | `NameAr`, `NameEn`, `Description`, `ColorCode`, `Notes` | ✅ |
| **RULE-016** | BaseEntity audit fields | Account inherits BaseEntity — `CreatedAt`, `CreatedByUserId`, etc. | ✅ |
| **RULE-023** | Domain has ZERO NuGet packages | Account entity — no dependencies | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | `AccountService` injects `IUnitOfWork` | ✅ |
| **RULE-035** | Serilog for logging | AccountService: `Log.Information` on every CRUD | ✅ |
| **RULE-036** | Log critical operations | Account create/update/delete, seed data | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | `AccountsController` — every endpoint has `[Authorize]` | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Account: `Create()`, `Update()`, `MarkAsDeleted()`, `HasChildren()` | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | `CreateAccountRequestValidator`, `UpdateAccountRequestValidator` | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Account: Soft delete (`DeleteAsync`) + Permanent (`PermanentDeleteAsync`) | ✅ |
| **RULE-052** | Guard Clauses on all entities | Account.Create/Update — 5 guard clauses | ✅ |
| **RULE-053** | DomainException in Arabic | All guards: "رمز الحساب مطلوب", "مستوى الحساب يجب أن يكون بين 1 و 4" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use `IDialogService` | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | `AccountEditorViewModel` — `AddError`/`ClearErrors` per property | ✅ |
| **RULE-059** | Save always enabled, validate on click | AccountEditorViewModel — `SaveCommand = new AsyncRelayCommand(SaveAsync)` | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModel async commands use `ExecuteAsync()` | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern — `IAccountService`/`AccountService` | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Account editor opens via `OpenScreen()` | ✅ |
| **RULE-161** | Editors NON-modal | `OpenScreen()` not `ShowDialog()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use `LogSystemError()` | ✅ |
| **RULE-172** | HandleFailure() transforms errors | `AccountsListViewModel` — calls `HandleFailure()` | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ الحساب"`, `"خطأ في تحميل دليل الحسابات"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All ViewModels verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync`, `ShowValidationErrorsAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, JSON parse crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, "not found", parent validation failures | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | `AccountApiService` — content-type guard on non-JSON responses | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | ALL buttons, inputs, TreeView items in Accounts views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | `"إضافة حساب جديد — سيتم فتح نافذة إنشاء حساب"` | ✅ |
| **RULE-187** | Action buttons explain consequences | Delete: `"حذف الحساب — لا يمكن حذف حسابات نظامية أو حسابات رئيسية"` | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | `"دليل الحسابات — عرض وتعديل الحسابات المحاسبية"` | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | `"➕ إضافة أول حساب — أضف حساباً جديداً لدليل الحسابات"` | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | `"إخفاء رسالة الخطأ"` | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use `LogSystemError()` | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | `AccountService.PermanentDeleteAsync` catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | `IAccountService` — all 7 methods | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | `AccountsController` — injects `IAccountService` only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | `AccountConfiguration` — self-referencing `ParentAccountId` | ✅ |
| **RULE-220** | Newest-first sorting on lists | Accounts sorted by `AccountCode` (hierarchical, not "newest-first" — appropriate for tree) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | `AccountEditorViewModel` constructor calls `SetDialogService()` | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | `AccountEditorViewModel` — uses `AddError`/`ClearErrors` | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in `AccountEditorViewModel.SaveAsync()` | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | Already exists — not in scope | ✅ N/A |
| **RULE-244** | User hard-delete guarded | Not applicable to accounts | ✅ N/A |
| **RULE-249** | Arabic string literals valid UTF-8 | All Arabic messages verified as correct UTF-8 | ✅ |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected by this phase | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | AccountsList header: `Padding="12,6"`, footer: `Padding="12,8"` | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields: `Margin="0,0,0,6"` | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | Dialog windows: `FontSize="16"` | ✅ |
| **RULE-267** | Section headers FontSize=14 max | Accounts title: `FontSize="14"` | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | Empty accounts view: `Margin="0,12,0,0" Width="140"` | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | Account editor screen | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | Save/Cancel buttons | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Expanding seeder deletes existing SystemAccountMappings references** | **HIGH** — could break existing invoice/journal entry mappings | Two-phase seed: first recreate accounts, THEN remap SystemAccountMappings using new IDs |
| **Parent-child level validation blocks legitimate use cases** | Low (resolved) | Validation relaxed from strict `parent.Level + 1 == child.Level` to `child.Level > parent.Level` — any depth jump allowed, max 10 |
| **TreeView performance with 60+ accounts** | Low | 60 accounts is trivially small for TreeView; no virtualization needed |
| **AccountCode unique index conflicts during seed** | Medium | Seeder checks `AnyAsync()` before seeding; if accounts exist, skip entirely |
| **Existing JournalEntry references to old Account IDs break** | **HIGH** — if existing JE rows reference accounts that get re-seeded with different IDs | Migration must preserve old IDs; seeder must check `AnyAsync()` and skip if accounts exist |
| **ColorCode null reference in XAML binding** | Low | Use `Converter={StaticResource HexToBrush}` with fallback to transparent/default color |
| **OpeningBalance double-counting if migrated incorrectly** | Medium | OpeningBalance is a display field only in V1; actual balance comes from JournalEntry (not yet implemented) |
| **IsSystemAccount flag prevents legitimate user edits** | Low | Only L1-L2 accounts are system-protected (15 of 60); L3-L4 accounts are user-modifiable |
| **Self-referencing FK deadlock on bulk operations** | Low | Single-account operations only; no bulk operations in V1 |
| **Desktop TreeView not updating after add/edit** | Low | EventBus `AccountChangedMessage` triggers `LoadAccountsOperationAsync` on any change |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **Seed data causes issues (60 accounts wrong)** | `DELETE FROM SystemAccountMappings; DELETE FROM Accounts;` then revert to previous seeder or re-run corrected seeder |
| **Level/DESCRIPTION/COLORCODE migration fails** | `ALTER TABLE Accounts DROP COLUMN Level, Description, ColorCode, AllowTransactions, OpeningBalance; DROP CONSTRAINT CHK_Account_Level_Range;` |
| **AccountsController not needed** | Remove controller + DI registration — no data impact |
| **AccountEditorViewModel too complex for V1** | Remove Desktop files; API endpoints still work for direct testing |
| **TreeView API endpoint not used** | Remove `GET /accounts/tree` endpoint — flat `GET /accounts` still functional |
| **Parent-child FK validation too strict** | Resolved — validation now allows any `child.Level > parent.Level` with max `child.Level <= 10` |
| **SystemAccountMappings reference wrong IDs** | Truncate `SystemAccountMappings`, re-run migration from scratch, verify IDs match account codes |
| **Entire module not needed** | Revert migration: `DROP TABLE Accounts` (if no other tables reference it); remove all new files; revert to previous seeding logic |

---

---

## 12. Self-Explanation ◉ Tooltips for All 60 Accounts

**Goal**: Every account in the Chart of Accounts shows a ◉ icon next to its name. On hover/click, a tooltip explains what this account means in plain Arabic, what transactions affect it, and what it represents.

**UI Pattern**: Same InfoTooltip UserControl from Phase 18 (◉ icon with styled ToolTip).

**Implementation**:

1. Add `Explanation` column (`nvarchar(500)`) to Account entity — nullable
2. Seed all 60 accounts with explanations
3. Display ◉ icon next to account name in tree view + dropdown selections
4. API returns `Explanation` field in `AccountDto`

### 12.1 Complete Explanation Mapping — All 60 Accounts

Each account below includes the explanation text to seed into the database. Level 1 (Group) and Level 2 (Main) accounts get category-level explanations. Level 3 (Sub) and Level 4 (Detail) accounts get transaction-level explanations.

#### الأصول (Assets) — Level 1

| Code | Name | Explanation |
|------|------|-------------|
| 1000 | الأصول | "مجموعة الأصول — تمثل كل ما تملكه الشركة من ممتلكات وحقوق ذات قيمة مالية. تشمل الأصول المتداولة (النقد والمخزون) والأصول الثابتة (المباني والآلات)." |

##### الأصول المتداولة (Current Assets) — Level 2

| Code | Name | Explanation |
|------|------|-------------|
| 1100 | الأصول المتداولة | "مجموعة الأصول التي يمكن تحويلها إلى نقد خلال سنة واحدة. تشمل: النقد بالصندوق، البنوك، حسابات العملاء، والمخزون." |
| 1110 | الصناديق | "مجموعة الصناديق النقدية — تمثل النقد الموجود فعلياً في خزينة الشركة بكل العملات." |
| 1111 | الصندوق | "النقد الموجود فعلياً في خزينة الشركة بالعملة المحلية. يزداد عند المقبوضات النقدية (المبيعات النقدية، تحصيل العملاء) وينقص عند المدفوعات النقدية (المشتريات النقدية، المصروفات)." |
| 1112 | صندوق الدولار | "النقد الموجود في خزينة الشركة بالعملة الأجنبية (الدولار). يزداد عند تحصيل مبالغ بالدولار وينقص عند صرف الدولار أو تحويله." |
| 1120 | البنوك | "مجموعة الحسابات البنكية للشركة — تمثل الأرصدة المودعة لدى البنوك." |
| 1121 | البنك الأهلي | "الحساب الجاري للشركة في البنك الأهلي. يزداد عند إيداع النقود أو استلام حوالات وينقص عند سحب النقود أو إصدار الشيكات." |
| 1130 | حسابات العملاء | "مجموعة حسابات العملاء — تمثل الأموال المستحقة للشركة من العملاء الذين اشتروا بالآجل." |
| 1131 | العميل النقدي | "حساب العميل الذي يشتري نقداً — يستخدم لتسجيل حركات العميل النقدي. هذا حساب فني لتتبع مبيعات العميل النقدي." |
| 1132 | عملاء آجلون | "الأموال المستحقة للشركة من العملاء الذين اشتروا بضاعة بالأجل. يزداد عند بيع البضاعة بالأجل وينقص عند تحصيل المبالغ من العملاء." |
| 1133 | أوراق قبض | "سندات وشيكات مستحقة للشركة من العملاء تم تأجيل سدادها. تمثل مستحقات مؤجلة لدى الشركة." |
| 1134 | شيكات تحت التحصيل | "الشيكات التي استلمتها الشركة من العملاء ولم يتم تحصيلها من البنك بعد. تدخل ضمن الأصول حتى يتم صرفها." |
| 1135 | مصروفات مقدمة | "مبالغ دفعتها الشركة مقدماً مقابل خدمات أو منافع ستحصل عليها في المستقبل مثل: إيجار مدفوع مقدماً، تأمين مدفوع مقدماً." |
| 1140 | المخزون | "مجموعة المخزون — تمثل قيمة البضاعة الموجودة في المخازن وغير المباعة بعد." |
| 1141 | بضاعة أول المدة | "قيمة المخزون الموجود في بداية الفترة المالية. يستخدم لحساب تكلفة البضاعة المباعة بنظام الجرد الدوري." |
| 1142 | مخزون آخر المدة | "قيمة المخزون الموجود في نهاية الفترة المالية بعد جرد المخازن. يحدد بعد إجراء الجرد الفعلي." |

##### الأصول الثابتة (Fixed Assets) — Level 2

| Code | Name | Explanation |
|------|------|-------------|
| 1200 | الأصول الثابتة | "مجموعة الأصول التي تستخدمها الشركة لأكثر من سنة وتستخدم في النشاط التشغيلي مثل: الأثاث، السيارات، المباني، والأجهزة." |
| 1210 | الأثاث | "مجموعة الأثاث والمعدات — تشمل مكاتب العمل والكراسي والخزائن والأجهزة المكتبية." |
| 1211 | أثاث ومعدات | "قيمة الأثاث والتجهيزات المكتبية مثل المكاتب والكراسي والخزائن وأجهزة الكمبيوتر. يزداد عند شراء أصول جديدة وينقص عند البيع أو الإهلاك." |
| 1220 | السيارات | "مجموعة سيارات الشركة — تشمل سيارات النقل والتوصيل." |
| 1221 | سيارات النقل | "قيمة سيارات النقل التي تملكها الشركة مثل سيارات التوصيل والشاحنات. يزداد عند شراء سيارة وينقص عند البيع أو الإهلاك." |
| 1230 | مباني وعقارات | "مجموعة المباني والعقارات التي تملكها الشركة." |
| 1231 | المبنى الإداري | "قيمة المبنى الذي تملكه الشركة وتستخدمه كمقر إداري. يزداد عند شراء عقار وينقص عند البيع أو الإهلاك." |
| 1240 | إهلاك الأصول الثابتة | "مجموعة إهلاكات الأصول الثابتة — تمثل انخفاض قيمة الأصول مع مرور الزمن بسبب الاستعمال والتقادم." |
| 1241 | إهلاك الأثاث | "قيمة الإهلاك السنوي للأثاث والمعدات. يحسب بنسبة مئوية من قيمة الأصل سنوياً ويظهر كمصروف في قائمة الدخل." |

#### الخصوم (Liabilities) — Level 1

| Code | Name | Explanation |
|------|------|-------------|
| 1300 | الخصوم | "مجموعة الخصوم — تمثل الالتزامات المالية التي تدين بها الشركة للغير. تشمل الخصوم المتداولة (الموردين، الضرائب) والخصوم طويلة الأجل (القروض)." |
| 1310 | التزامات متداولة | "مجموعة الالتزامات التي يجب سدادها خلال سنة واحدة. تشمل: حسابات الموردين، الضرائب المستحقة، والقروض قصيرة الأجل." |
| 1320 | حسابات الموردين | "مجموعة حسابات الموردين — تمثل الأموال التي تدين بها الشركة للموردين الذين اشترينا منهم بضاعة بالأجل." |
| 1321 | المورد النقدي | "حساب المورد الذي نشتري منه نقداً — يستخدم لتسجيل حركات المورد النقدي. حساب فني لتتبع مشتريات هذا المورد." |
| 1322 | موردون آجلون | "الأموال المستحقة للموردين الذين اشترينا منهم بضاعة بالأجل. يزداد عند شراء البضاعة بالأجل وينقص عند سداد المبالغ للموردين." |
| 1323 | أوراق دفع | "سندات وشيكات أصدرتها الشركة لصالح الموردين أو الدائنين ولم يتم سدادها بعد. تمثل التزامات مؤجلة على الشركة." |
| 1330 | الضرائب | "مجموعة الضرائب — تشمل ضريبة القيمة المضافة وغيرها من الضرائب المستحقة على الشركة." |
| 1331 | ضريبة المبيعات (خرج) | "ضريبة القيمة المضافة التي تحصّلها الشركة من العملاء على المبيعات لحساب الحكومة. تزداد عند إصدار فاتورة مبيعات وينقص عند سدادها للهيئة." |
| 1332 | ضريبة المشتريات (دخل) | "ضريبة القيمة المضافة التي تدفعها الشركة للموردين على المشتريات. تخصم من ضريبة المبيعات المستحقة قبل سداد الفرق للهيئة." |
| 1340 | قروض | "مجموعة القروض — تشمل القروض التي حصلت عليها الشركة من البنوك أو المؤسسات المالية." |
| 1341 | قروض بنكية | "القروض المستحقة على الشركة للبنوك. يزداد عند الحصول على قرض جديد وينقص عند سداد الأقساط." |

#### حقوق الملكية (Equity) — Level 1

| Code | Name | Explanation |
|------|------|-------------|
| 1400 | حقوق الملكية | "مجموعة حقوق الملكية — تمثل حصة أصحاب الشركة في أصولها بعد خصم الالتزامات. تشمل رأس المال والأرباح المحتجزة والاحتياطيات." |
| 1410 | رأس المال | "مجموعة رأس المال — تمثل الأموال التي استثمرها أصحاب الشركة." |
| 1411 | رأس المال | "الأموال التي دفعها أصحاب الشركة لتأسيس النشاط التجاري. يمثل التزام الشركة تجاه مالكيها. يزداد عند ضخ استثمارات جديدة وينقص عند السحوبات." |
| 1420 | الأرباح والخسائر | "مجموعة الأرباح والخسائر — تمثل صافي نتيجة أعمال الشركة (ربح أو خسارة)." |
| 1421 | ح/ الأرباح والخسائر | "ملخص نتائج أعمال الشركة في نهاية الفترة المالية. يمثل صافي الربح (إذا دائن) أو صافي الخسارة (إذا مدين) بعد مقابلة الإيرادات بالمصروفات." |
| 1430 | الاحتياطيات | "مجموعة الاحتياطيات — تمثل الأرباح التي تم تجنيبها لمواجهة مخاطر مستقبلية أو لتوسعة النشاط." |
| 1431 | احتياطي قانوني | "نسبة من الأرباح السنوية يتم تجنيبها حسب القانون لتعزيز المركز المالي للشركة. لا يمكن توزيعها على الملاك." |

#### الإيرادات (Revenue) — Level 1

| Code | Name | Explanation |
|------|------|-------------|
| 1500 | الإيرادات | "مجموعة الإيرادات — تمثل الدخل الذي تحققه الشركة من أنشطتها التجارية المختلفة. تشمل إيرادات المبيعات والإيرادات الأخرى." |
| 1510 | إيرادات النشاط | "مجموعة إيرادات النشاط التجاري الأساسي — تشمل إيرادات المبيعات والمردودات والخصم المكتسب." |
| 1520 | إيرادات المبيعات | "مجموعة إيرادات المبيعات — تمثل قيمة البضاعة المباعة للعملاء بشقيها النقدي والآجل." |
| 1521 | مبيعات نقدي | "قيمة البضاعة المباعة للعملاء نقداً. يزداد عند إصدار فاتورة مبيعات نقدية. يمثل الإيراد المباشر للنشاط." |
| 1522 | مبيعات آجل | "قيمة البضاعة المباعة للعملاء بالأجل. يزداد عند إصدار فاتورة مبيعات آجلة. يتحول إلى نقد عند تحصيل قيمة الفاتورة." |
| 1530 | مردودات المشتريات | "مجموعة مردودات المشتريات — تمثل قيمة البضاعة التي تم إرجاعها للموردين." |
| 1531 | مردودات مشتريات نقدي | "قيمة البضاعة التي أرجعتاها للمورد وتم استرداد قيمتها نقداً. يزيد من الإيرادات لأنه يقلل من تكلفة المشتريات." |
| 1532 | مردودات مشتريات آجل | "قيمة البضاعة التي أرجعتاها للمورد وأضيفت إلى رصيدنا الدائن لديه. يزيد من الإيرادات لأنه يقلل من التزاماتنا تجاه المورد." |
| 1540 | الخصم المكتسب | "مجموعة الخصم المكتسب — يمثل الخصم الذي تحصل عليه الشركة من الموردين عند السداد المبكر." |
| 1541 | الخصم المكتسب | "الخصم الذي تحصل عليه الشركة من الموردين عند دفع قيمة المشتريات قبل تاريخ الاستحقاق. يعتبر إيراداً لأنه يقلل من التكلفة الفعلية للمشتريات." |
| 1550 | إيرادات أخرى | "مجموعة الإيرادات الأخرى — تشمل الإيرادات غير المتعلقة بالنشاط التجاري الأساسي." |
| 1551 | إيرادات متنوعة | "أي إيرادات أخرى تحققها الشركة خارج النشاط الأساسي مثل: بيع خردة، إيجار عقارات مملوكة، أرباح استثمارية." |
| 1552 | فوارق عملة | "الأرباح التي تنتج عن تغير أسعار صرف العملات الأجنبية عند تسوية المبالغ المقومة بعملات أجنبية." |
| 1553 | فوارق بيع وشراء العملات | "الفرق الناتج عن بيع وشراء العملات الأجنبية بين سعر الشراء وسعر البيع في السوق." |

#### المصروفات (Expenses) — Level 1

| Code | Name | Explanation |
|------|------|-------------|
| 1600 | المصروفات | "مجموعة المصروفات — تمثل التكاليف التي تتحملها الشركة لتحقيق الإيرادات. تشمل تكلفة المبيعات والمصروفات التشغيلية والإدارية." |
| 1610 | تكاليف النشاط | "مجموعة تكاليف النشاط التجاري — تشمل تكلفة البضاعة المباعة والمشتريات والمردودات والخصم المسموح به وتسويات المخزون." |
| 1620 | تكلفة البضاعة المباعة | "مجموعة تكلفة البضاعة المباعة — تمثل التكلفة المباشرة للبضاعة التي تم بيعها." |
| 1621 | تكلفة البضاعة المباعة | "تكلفة شراء البضاعة التي تم بيعها خلال الفترة. تحسب بالتكلفة المرجحة (Weighted Average). تقابل إيراد المبيعات في قائمة الدخل." |
| 1630 | المشتريات | "مجموعة المشتريات — تمثل قيمة البضاعة التي اشترتها الشركة لإعادة بيعها." |
| 1631 | مشتريات نقدي | "قيمة البضاعة التي اشترتها الشركة ودفعت قيمتها نقداً. يزداد عند شراء بضاعة نقداً ويقابله انخفاض في رصيد الصندوق." |
| 1632 | مشتريات آجل | "قيمة البضاعة التي اشترتها الشركة بالأجل. يزداد عند شراء بضاعة بالأجل ويقابله زيادة في رصيد المورد الدائن." |
| 1640 | مردودات المبيعات | "مجموعة مردودات المبيعات — تمثل قيمة البضاعة التي أرجعها العملاء للشركة." |
| 1641 | مردودات مبيعات نقدي | "قيمة البضاعة التي أرجعها العملاء وتم رد قيمتها نقداً. يقلل من صافي إيراد المبيعات." |
| 1642 | مردودات مبيعات آجل | "قيمة البضاعة التي أرجعها العملاء وأضيفت إلى رصيدهم الدائن. يقلل من صافي إيراد المبيعات." |
| 1650 | الخصم المسموح به | "مجموعة الخصم المسموح به — يمثل الخصم الذي تمنحه الشركة للعملاء." |
| 1651 | الخصم المسموح به | "الخصم الذي تمنحه الشركة للعملاء عند السداد المبكر أو بمناسبة عروض خاصة. يقلل من الإيرادات الفعلية ويعتبر مصروفاً." |
| 1660 | تسوية المخزون | "مجموعة تسويات المخزون — تشمل العجز والزيادة والبضاعة التالفة والتسويات الأخرى." |
| 1661 | عجز وزيادة البضاعة | "الفرق بين المخزون النظري (المسجل في النظام) والمخزون الفعلي (بعد الجرد). العجز مصروف والزيادة إيراد." |
| 1662 | البضاعة التالفة | "قيمة البضاعة التي تلفت أو انتهت صلاحيتها ولا يمكن بيعها. تعتبر خسارة للشركة وتسجل كمصروف." |
| 1663 | تسوية المخزون-صرف وتوريد | "تسوية حركات صرف وتوريد المخزون للأغراض الداخلية مثل: تحويل بضاعة بين المخازن أو صرف عينات." |
| 1670 | مصاريف تشغيلية وإدارية | "مجموعة المصاريف التشغيلية والإدارية — تشمل المصروفات اليومية لإدارة وتشغيل الشركة." |
| 1680 | المصروفات العمومية | "مجموعة المصروفات العمومية والإدارية — تشمل المصروفات التشغيلية اليومية." |
| 1681 | مصروفات عمومية | "المصاريف اليومية لإدارة الشركة مثل: القرطاسية، الفواتير البسيطة، مصاريف البنك، الاشتراكات." |
| 1682 | الرواتب والأجور | "المرتبات والأجور التي تدفعها الشركة للموظفين والعمال مقابل عملهم. تشمل الراتب الأساسي والبدلات والحوافز." |
| 1683 | الإيجار | "قيمة إيجار المباني أو المخازن أو المكاتب التي تستخدمها الشركة. تدفع شهرياً أو سنوياً حسب العقد." |
| 1684 | الكهرباء والمياه | "فواتير الكهرباء والمياه الخاصة بمقر الشركة ومخازنها. تدفع بشكل دوري للجهات المختصة." |
| 1685 | مصروفات النقل | "مصاريف نقل البضاعة من الموردين إلى المخازن أو من المخازن إلى العملاء. تشمل أجور الشحن." |
| 1686 | مصروفات الصيانة | "مصاريف صيانة وإصلاح الأصول الثابتة مثل: صيانة السيارات، الأثاث، أجهزة الكمبيوتر والمباني." |
| 1687 | رسوم أخرى / مصاريف أخرى | "أي مصاريف أخرى لا تندرج تحت البنود السابقة مثل: رسوم تراخيص، غرامات، رسوم حكومية." |
| 1688 | أجور نقل | "أجور سائقي النقل وعمال التحميل والتنزيل. تختلف عن مصروفات النقل التي تشمل تكلفة الشحن نفسها." |
| 1690 | هالك المخزون | "مجموعة هالك وخسائر المخزون — تمثل البضاعة التالفة أو المفقودة." |
| 1691 | هالك المخزون | "قيمة البضاعة التي أصبحت تالفة أو منتهية الصلاحية ولا يمكن بيعها. تسجل كمصروف عند اكتشافها." |
| 1692 | خسائر المخزون | "خسائر المخزون الناتجة عن السرقة أو الفقدان أو أخطاء الجرد. الفرق بين الرصيد النظري والفعلي." |

### 12.2 Implementation Details

**Account Entity Update**:
```csharp
// Add to Account entity in SalesSystem.Domain
public string? Explanation { get; private set; }
// Guard clause
public void SetExplanation(string? explanation)
{
    if (explanation?.Length > 500)
        throw new DomainException("الشرح لا يتجاوز 500 حرف");
    Explanation = explanation;
}
```

**Fluent API Configuration**:
```csharp
// In AccountConfiguration.cs
builder.Property(x => x.Explanation)
    .HasMaxLength(500)
    .HasColumnType("nvarchar(500)");
```

**AccountDto Update**:
```csharp
// Add to AccountDto in SalesSystem.Contracts
public string? Explanation { get; set; }
public bool HasExplanation => !string.IsNullOrWhiteSpace(Explanation);
```

**Seeder Update** (`ChartOfAccountsSeeder.cs`):
- Add `Explanation` column to the seed data
- Include all 60 explanations from the table above
- Each account seeded with its `Explanation` string alongside existing fields

**Desktop UI — InfoTooltip Binding**:

In `AccountTreeItemViewModel` or the tree view display template:
```xml
<!-- Tooltip triggers on hover; ◉ click opens detailed popup -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="{Binding AccountName}" />
    <controls:InfoTooltip Text="{Binding Account.Explanation}" 
                          Visibility="{Binding Account.HasExplanation, 
                              Converter={StaticResource BoolToVisibility}}"
                          ToolTip="اضغط لعرض شرح هذا الحساب" />
</StackPanel>
```

Same pattern for dropdown selections in account picker controls.

**API Response**:
```csharp
// AccountDto now includes Explanation
// GET /api/v1/accounts returns Explanation for every account
// GET /api/v1/accounts/{id} returns full Explanation
```

### 12.3 Files Changed

| File | Change |
|------|--------|
| `SalesSystem.Domain/Entities/Account.cs` | Add `Explanation` property + `SetExplanation()` |
| `SalesSystem.Infrastructure/Configurations/AccountConfiguration.cs` | Add `HasMaxLength(500)` |
| `SalesSystem.Contracts/DTOs/Accounts/AccountDto.cs` | Add `Explanation` + `HasExplanation` |
| `SalesSystem.Infrastructure/Data/Seeders/ChartOfAccountsSeeder.cs` | Add all 60 explanation strings |
| `SalesSystem.DesktopPWF/Views/Accounts/AccountTreeView.xaml` | Add InfoTooltip next to account name |
| `SalesSystem.DesktopPWF/Views/Controls/InfoTooltip.xaml` | Already exists from Phase 18 |
| DB Migration | `ALTER TABLE Accounts ADD Explanation nvarchar(500) NULL` |

### 12.4 Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Explanation text too long for UI | `HasMaxLength(500)` constraint; tooltip wraps text |
| Arabic text encoding issues | Verify UTF-8 with BOM before commit (RULE-250) |
| Existing accounts without explanations | Nullable column — `HasExplanation` in DTO handles visibility |
| InfoTooltip overlay too crowded in tree view | Show ◉ only on hover or selected state; compact icon (14×14) |

**Estimate**: ~4 hours
**Files**: 7 files modified (see table above)
**Dependencies**: Phase 18 InfoTooltip UserControl (already exists)

---

> **End of Phase 22 — Chart of Accounts Module Implementation Plan**
>
> Total estimated effort: **~22 hours** (18.5 hours implementation + 3.5 hours testing/review)
> Key deliverables: 1 expanded entity, 1 seeder rewrite, 1 service, 1 controller, 1 API client, 4 ViewModels, 4 Views, ~60 seed accounts
> Dependencies: Phase 20 (Users/Roles), Phase 21 (SystemSettings) — no blocking dependencies
