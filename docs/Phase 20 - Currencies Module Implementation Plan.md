# Phase 20 — Currencies Module: Comprehensive Implementation Plan

> **Version**: 1.0 — Built from analysis of Analysis Part 1–5, Global Analysis, and full codebase audit
> **Scope**: Complete multi-currency support for V1 including Currency CRUD, ExchangeRateHistory, CurrencyId FK migration, Desktop screens, API services, and integration with invoices, cashboxes, and payments.

---

## Table of Contents

1. [Architecture — 3 Sub-Modules](#1-architecture--3-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Currencies Design Catalog](#4-currencies-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 3 Sub-Modules

Based on full codebase audit + analysis of user requirements, the Currencies module for V1 is divided into **3 main sub-modules**:

| # | Sub-Module | Storage | Impact |
|---|------------|---------|--------|
| 💱 | **Currencies CRUD** | `Currency` entity (new) | Currency list with IsBaseCurrency, exchange rates |
| 📊 | **Exchange Rate History** | `ExchangeRateHistory` entity (new) | Track rate changes over time |
| 🔗 | **CurrencyId FK Integration** | FK columns on 5+ entities | Link invoices, cashboxes, payments to currencies |

---

## 2. Full Inventory — What Already Exists

### 2.1 Currency Entity ❌ (Does NOT exist)

| Component | Status |
|-----------|--------|
| `Currency` entity | **❌ MISSING** — no Currency.cs anywhere in Domain |
| `CurrencyConfiguration` | **❌ MISSING** |
| `CurrencyDto` | **❌ MISSING** |
| `CurrencyService` / `ICurrencyService` | **❌ MISSING** |
| `CurrenciesController` | **❌ MISSING** |
| Currency desktop screens (List + Editor) | **❌ MISSING** |

### 2.2 ExchangeRateHistory Entity ❌ (Does NOT exist)

| Component | Status |
|-----------|--------|
| `ExchangeRateHistory` entity | **❌ MISSING** |
| `ExchangeRateHistoryConfiguration` | **❌ MISSING** |
| Rate change audit tracking | **❌ MISSING** |

### 2.3 CashBox — Has CurrencyCode (string) ⚠️

```csharp
// CashBox.cs — CURRENT
public string CurrencyCode { get; private set; } = "SAR";  // String, no FK
```

CashBox currently uses a **string** `CurrencyCode` (default "SAR") rather than an `int? CurrencyId` FK. This needs to be migrated.

### 2.4 Invoice Entities — NO Currency Support ❌

| Entity | CurrencyId | ExchangeRate |
|--------|-----------|--------------|
| `SalesInvoice` | ❌ Missing | ❌ Missing |
| `PurchaseInvoice` | ❌ Missing | ❌ Missing |
| `SalesReturn` | ❌ Missing | ❌ Missing |
| `PurchaseReturn` | ❌ Missing | ❌ Missing |

### 2.5 Payment Entities — NO Currency Support ❌

| Entity | CurrencyId | ExchangeRate |
|--------|-----------|--------------|
| `CustomerPayment` | ❌ Missing | ❌ Missing |
| `SupplierPayment` | ❌ Missing | ❌ Missing |

### 2.6 CashTransaction — NO Currency Support ❌

| Entity | CurrencyId |
|--------|-----------|
| `CashTransaction` | ❌ Missing |

### 2.7 Customer / Supplier — Already Has CreditLimit ✅

| Field | Entity | Status |
|-------|--------|--------|
| `CreditLimit` (decimal) | Customer | ✅ Exists — `Customer.Create()` validates `creditLimit >= 0` |
| `CreditLimit` (decimal) | Supplier | ✅ Exists — `Supplier.Create()` validates `creditLimit >= 0` |

Analysis Part 1 noted that "سقف الحساب" (Credit Limit) was placed inside the Currencies section in the reference app. **This is a design flaw in the reference app** — it correctly belongs on Customer/Supplier entities, which is already the case in our codebase.

### 2.8 StoreSettings — Has CurrencyCode (string) ⚠️

```csharp
// StoreSettings — CURRENT
string CurrencyCode = "SAR"  // String, will be deprecated → use BaseCurrency lookup
```

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: CashBox Uses String CurrencyCode Instead of FK

**Problem**: `CashBox.CurrencyCode` is a `string` field with default `"SAR"`. There is no FK relationship to a `Currency` table. This means:
- Cannot enforce referential integrity — if a Currency is deleted, CashBox still has its code
- Cannot report cashbox balances by currency reliably
- When CashBox shows "صندوق الدولار", the linked CurrencyId is missing

**Fix**: Replace `string CurrencyCode` with `int? CurrencyId` FK in CashBox:

```csharp
// CashBox.cs — AFTER FIX
public int? CurrencyId { get; private set; }
public Currency? Currency { get; private set; }

// Updated Create signature:
public static CashBox Create(
    string boxName,
    int? currencyId = null,   // NEW — replaces currencyCode string
    int? branchId = null,
    int? assignedUserId = null,
    decimal initialBalance = 0)
```

**Files changed**: `CashBox.cs`, `CashBoxConfiguration.cs`, migration, DTOs, ViewModels

### 3.2 Blocker 2: Invoice Entities Must Support Multi-Currency

**Problem**: SalesInvoice and PurchaseInvoice have no `CurrencyId` or `ExchangeRate`. If a purchase is made in USD (while system base is YER), there is no exchange rate to convert to base currency for costing.

**Fix**: Add `int? CurrencyId` + `decimal? ExchangeRate` to both invoice entities:

```csharp
// SalesInvoice.cs — ADD
public int? CurrencyId { get; private set; }
public decimal? ExchangeRate { get; private set; }  // Rate to base currency at transaction time
public Currency? Currency { get; private set; }

// PurchaseInvoice.cs — ADD (same fields)

// In Create() method — add optional parameters:
int? currencyId = null,
decimal? exchangeRate = null,
```

**Files changed**: `SalesInvoice.cs`, `PurchaseInvoice.cs`, `SalesInvoiceConfiguration.cs`, `PurchaseInvoiceConfiguration.cs`, DTOs, services, migrations

---

## 4. Currencies Design Catalog

### 4.1 Currency Entity (New)

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `Name` | `nvarchar(100)` | — | ✅ | **UNIQUE Index** |
| 3 | `Code` | `nvarchar(10)` | — | ✅ | **UNIQUE Index** (e.g., "YER", "USD") |
| 4 | `Symbol` | `nvarchar(10)` | — | ✅ | e.g., "﷼", "$", "€" |
| 5 | `ExchangeRateToBase` | `decimal(18,6)` | `1` | ✅ | Rate against base currency (base=1.0) |
| 6 | `IsBaseCurrency` | `bit` | `false` | ✅ | **Filtered Unique Index WHERE IsBaseCurrency = 1** |
| 7 | `FractionName` | `nvarchar(20)` | `null` | ❌ | Name of fractional unit (e.g., "فلس", "سنت") |
| 8 | `IsSystem` | `bit` | `false` | ❌ | Protects base currencies from deletion — cannot delete when true |
| 9 | `IsActive` | `bit` | `true` | ❌ | Global query filter |

**Precision note**: `ExchangeRateToBase` uses `decimal(18,6)` because currency exchange rates can require up to 6 decimal places for precision (e.g., 1 YER = 0.000040 USD).

**Seed data**:

| Name | Code | Symbol | ExchangeRateToBase | IsBaseCurrency | FractionName | IsSystem |
|------|------|--------|-------------------|----------------|-------------|---------|
| ريال يمني | YER | ﷼ | 1.0 | ✅ | فلس | ✅ |
| دولار أمريكي | USD | $ | 0.004 (example) | ❌ | سنت | ✅ |
| ريال سعودي | SAR | ﷼ | 0.014 (example) | ❌ | — | ✅ |

### 4.2 ExchangeRateHistory Entity (New)

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `CurrencyId` | `int FK` | — | ✅ | `DeleteBehavior.Restrict` |
| 3 | `OldRate` | `decimal(18,6)` | — | ✅ | Previous exchange rate |
| 4 | `NewRate` | `decimal(18,6)` | — | ✅ | New exchange rate |
| 5 | `EffectiveDate` | `date` | — | ✅ | Start date for this rate |
| 6 | `RateType` | `nvarchar(20)` | `"Daily"` | ❌ | Daily / Monthly / Yearly |
| 7 | `ChangedByUserId` | `int?` | `null` | ❌ | User who changed the rate |
| 8 | `Notes` | `nvarchar(500)` | `null` | ❌ | Reason for change |

**Audit trigger**: `ExchangeRateHistory` is recorded on EVERY rate change (similar to `ProductPriceHistory` — RULE-084/085). Never update a rate without audit.

### 4.3 CurrencyId FK Migration Plan

| Entity | Current | New Field | Migration |
|--------|---------|-----------|-----------|
| `CashBox` | `string CurrencyCode` (default "SAR") | `int? CurrencyId` FK | **MIGRATE**: Find Currency by Code → set FK; DROP old column |
| `SalesInvoice` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |
| `PurchaseInvoice` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |
| `CustomerPayment` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |
| `SupplierPayment` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |
| `CashTransaction` | *(no currency field)* | `int? CurrencyId` | ADD nullable FK |
| `SalesReturn` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |
| `PurchaseReturn` | *(no currency field)* | `int? CurrencyId`, `decimal? ExchangeRate` | ADD nullable FK + rate |

### 4.4 StoreSettings.CurrencyCode Deprecation

`StoreSettings.CurrencyCode` (string, default `"SAR"`) currently exists but overlaps with the new `Currency` entity:

| Field | Status | Action |
|-------|--------|--------|
| `StoreSettings.CurrencyCode` | ⚠️ Exists | **DEPRECATE** — hide from Settings UI. System reads `IsBaseCurrency` from Currency table instead. |

**Decision**: Deprecate (not delete) in V1:
- Keep column in database (backwards compat)
- Hide from SettingsViewModel UI
- System determines base currency from `currencies.First(c => c.IsBaseCurrency)`
- Document: Remove column in Phase 25+

---

## 5. Gap Analysis

### 5.1 Currency Entity

| Component | Status | Action |
|-----------|--------|--------|
| `Currency.cs` Domain entity | ❌ Missing | Create from scratch |
| `CurrencyConfiguration.cs` | ❌ Missing | Create with FK configs |
| EF Core Migration | ❌ Missing | New migration: CREATE TABLE Currencies |
| Seed data (YER, USD, SAR) | ❌ Missing | Add to DbSeeder |
| `CurrencyDto` | ❌ Missing | Add to AllDtos.cs |
| `CreateCurrencyRequest` / `UpdateCurrencyRequest` | ❌ Missing | Add new request file |
| `ICurrencyService` / `CurrencyService` | ❌ Missing | Create with Result<T> + IUnitOfWork |
| `CurrenciesController` | ❌ Missing | Create with auth policies |
| `CurrenciesListViewModel` + View | ❌ Missing | List screen |
| `CurrencyEditorViewModel` + View | ❌ Missing | Editor screen |
| `ICurrencyApiService` + HTTP client | ❌ Missing | Desktop API service |
| `CurrencyChangedMessage` | ❌ Missing | EventBus message |
| FluentValidators for requests | ❌ Missing | CreateCurrencyValidator + UpdateCurrencyValidator |

### 5.2 ExchangeRateHistory

| Component | Status | Action |
|-----------|--------|--------|
| `ExchangeRateHistory.cs` Domain entity | ❌ Missing | Create from scratch |
| `ExchangeRateHistoryConfiguration.cs` | ❌ Missing | Create with FK config |
| Audit recording on rate change | ❌ Missing | Service method: `RecordRateChangeAsync()` |
| Desktop screen for rate history | ❌ Missing | Read-only list in CurrencyEditor |

### 5.3 FK Integration (5+ Entities)

| Entity | Current | Gap | Action |
|--------|---------|-----|--------|
| `CashBox` | `string CurrencyCode` | No FK | Migrate to `int? CurrencyId` |
| `SalesInvoice` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |
| `PurchaseInvoice` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |
| `CustomerPayment` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |
| `SupplierPayment` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |
| `CashTransaction` | — | No currency support | Add `int? CurrencyId` |
| `SalesReturn` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |
| `PurchaseReturn` | — | No currency support | Add `int? CurrencyId` + `decimal? ExchangeRate` |

### 5.4 StoreSettings.CurrencyCode Deprecation

| Issue | Status | Action |
|-------|--------|--------|
| Two sources of truth for base currency | ⚠️ Conflicting | **DEPRECATE** — hide from UI, Currency table is source of truth |

### 5.5 CashBox Migration (String → FK)

| Issue | Status | Action |
|-------|--------|--------|
| CashBox uses string CurrencyCode | ❌ Needs migration | Replace with int? CurrencyId FK + data migration |

---

## 6. Architectural Decisions

### 6.1 Base Currency: YER (Default for Yemen Market)

Analysis Part 2 explicitly recommends YER as the default base currency because:
- The system targets the Yemeni market primarily
- YER is the local currency for Yemen
- User can change later via Currency editor (set another currency as IsBaseCurrency)
- Reduces setup burden for users

**Decision**: Seed YER as `IsBaseCurrency = true`, USD and SAR as secondary currencies. User can change base currency at any time via the Currencies screen (by setting another currency's `IsBaseCurrency = true`, which automatically unsets the current base).

**Important**: Only ONE currency can have `IsBaseCurrency = true`. DB constraint enforces via filtered unique index:
```sql
CREATE UNIQUE INDEX IX_Currencies_IsBaseCurrency ON Currencies(IsBaseCurrency) WHERE IsBaseCurrency = 1;
```

### 6.2 ExchangeRateToBase: Store Against Base Currency

All non-base currencies store their rate as `ExchangeRateToBase`:
- Base currency (YER): `ExchangeRateToBase = 1.0`
- USD: `ExchangeRateToBase = 0.004` (meaning 1 YER = 0.004 USD, or 1 USD = 250 YER)
- SAR: `ExchangeRateToBase = 0.014` (meaning 1 YER = 0.014 SAR, or 1 SAR ≈ 71.4 YER)

**Conversion formula**:
```
AmountInBaseCurrency = AmountInForeignCurrency × (ExchangeRateToBase of foreign / ExchangeRateToBase of base)
```

Since base currency always has `ExchangeRateToBase = 1.0`, this simplifies to:
```
AmountInBaseCurrency = AmountInNonBase / NonBaseCurrency.ExchangeRateToBase
```

Wait — let me reconsider. The analysis says:
```
YER = 1
USD = 550
```
Meaning: 1 USD = 550 YER. So ExchangeRateToBase for USD = 550 (how many units of base currency for 1 unit of this currency).

Actually, looking at this more carefully, there are two conventions:
1. **Direct quote** (USD→YER): ExchangeRateToBase = how many base currency units per 1 unit of this currency
   - USD: 550 (1 USD = 550 YER)
   - Base: 1.0

So:
```
AmountInBaseCurrency = AmountInForeign × ExchangeRateToBase
AmountInYER = 100 USD × 550 = 55,000 YER
```

This is the convention we'll use.

**Decision**: `ExchangeRateToBase` = the amount of base currency equivalent to 1 unit of this currency. Base currency always has `ExchangeRateToBase = 1.0`.

### 6.3 ExchangeRateHistory Precision: decimal(18,6)

Exchange rates can require high precision for small currencies (YER vs USD). Using `decimal(18,6)` provides 6 decimal places of precision, sufficient for all common currency pairs.

### 6.4 CurrencyId FK: Nullable on Invoices

A SalesInvoice or PurchaseInvoice can be in base currency (no explicit currency needed), so `CurrencyId` is **nullable**. When null, the system assumes base currency.

Similarly, `ExchangeRate` is nullable (null = use current rate from Currency table at transaction time). If explicitly set, that rate is frozen for the invoice (historical accuracy).

### 6.5 ExchangeRate at Invoice Line Level vs Header Level

**Decision**: Exchange rate is stored at the **invoice header level**, not per line item. All line items on a single invoice share the same currency and exchange rate. This matches the analysis requirements and is simpler.

### 6.6 CashBox Migration: String → FK

The cashbox migration from `string CurrencyCode` to `int? CurrencyId` will:
1. Add `CurrencyId` nullable FK column
2. Populate FK by matching `CurrencyCode` to `Currency.Code` in seed data
3. NOT drop `CurrencyCode` column in V1 (backwards compat) — document for removal in Phase 25+

### 6.7 Credit Limit (سقف الحساب) Belongs on Customer/Supplier, NOT Currency

Analysis Part 1 noted that the reference app places "سقف الحساب" inside the Currencies section. This is a **design flaw** in the reference app. `CreditLimit` is a per-customer or per-supplier limit, not a currency-level property. Our codebase already correctly places it on Customer and Supplier entities.

**Decision**: NO action needed — CreditLimit already on Customer/Supplier. Do not add to Currency.

### 6.8 StoreSettings.CurrencyCode → Currency.IsBaseCurrency

`StoreSettings.CurrencyCode` used to be the source of truth for the system's currency. Now `Currency.IsBaseCurrency` replaces this. The system will:
1. Query `currencies.First(c => c.IsBaseCurrency)` to determine base currency
2. Hide `CurrencyCode` field from Settings UI
3. Keep column in DB for backwards compatibility

### 6.9 Why NOT a Tab-Based Currencies UI

The Currency screen will be a simple standalone page (like Tax module) — not a tab within Settings. Reason:
- Currency management is an admin function with its own CRUD lifecycle
- Exchange rate history deserves its own sub-section in the editor
- Separation of concerns — Settings focuses on config, Currencies focuses on master data

---

## 7. Non-V1 Items (Deferred)

These features appeared in analysis but are **deferred** to future versions:

| Feature | Reason |
|---------|--------|
| Auto-exchange rate sync (API from central bank) | Requires external API integration — V1 supports manual entry only |
| Multi-currency reporting (report in any currency) | Advanced reporting feature — V1 reports in base currency only |
| Real-time rate conversion in invoice UI | Live rate fetching adds complexity — V1 uses frozen rate at invoice time |
| Currency-specific decimal places (e.g., JPY has 0 decimals) | Localization detail — V1 uses system-level decimal places setting |
| Currency-specific rounding rules | Islamic finance requirement — deferred to Phase 25+ |
| Cryptocurrency support (BTC, ETH) | Not relevant for retail/wholesale market |
| Blockchain-based rate verification | Out of scope for V1 |
| Branch-level base currency (multi-company) | Multi-branch feature deferred to Phase 25+ |
| Tax in foreign currency (VAT in USD) | Complex tax compliance scenario — V1 taxes in base currency only |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

---

### Task 1 — Create Currency Domain Entity

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Currency.cs` | **CREATE** — new entity with Name, Code, Symbol, ExchangeRateToBase, IsBaseCurrency, IsActive |
| `Infrastructure/Data/Configurations/CurrencyConfiguration.cs` | **CREATE** — Fluent API config with FK constraints |
| `Infrastructure/Data/Migrations/` | New migration: `CREATE TABLE Currencies` |

**Currency.cs**:

```csharp
namespace SalesSystem.Domain.Entities;

public class Currency : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string Symbol { get; private set; } = null!;
    public decimal ExchangeRateToBase { get; private set; }
    public bool IsBaseCurrency { get; private set; }
    public string? FractionName { get; private set; }  // e.g., "فلس", "سنت"
    public bool IsSystem { get; private set; }          // Protects from deletion

    private Currency() { } // EF Core

    public static Currency Create(
        string name,
        string code,
        string symbol,
        decimal exchangeRateToBase = 1.0m,
        bool isBaseCurrency = false,
        string? fractionName = null,
        bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العملة مطلوب");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("رمز العملة مطلوب");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز العملة (Symbol) مطلوب");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من الصفر");
        if (code.Length > 10)
            throw new DomainException("رمز العملة لا يتجاوز 10 أحرف");
        if (fractionName?.Length > 20)
            throw new DomainException("اسم الجزء لا يتجاوز 20 حرفاً");

        return new Currency
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Symbol = symbol.Trim(),
            ExchangeRateToBase = exchangeRateToBase,
            IsBaseCurrency = isBaseCurrency,
            FractionName = fractionName?.Trim(),
            IsSystem = isSystem,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string symbol,
        decimal exchangeRateToBase,
        bool isBaseCurrency,
        string? fractionName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العملة مطلوب");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز العملة (Symbol) مطلوب");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من الصفر");
        if (fractionName?.Length > 20)
            throw new DomainException("اسم الجزء لا يتجاوز 20 حرفاً");

        Name = name.Trim();
        Symbol = symbol.Trim();
        ExchangeRateToBase = exchangeRateToBase;
        IsBaseCurrency = isBaseCurrency;
        FractionName = fractionName?.Trim();
        UpdateTimestamp();
    }

    public void UpdateExchangeRate(decimal newRate)
    {
        if (newRate <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من الصفر");
        ExchangeRateToBase = newRate;
        UpdateTimestamp();
    }

    public void MarkAsDeleted() => IsActive = false; // Soft delete
}
```

**CurrencyConfiguration.cs**:

```csharp
public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(10);
        builder.Property(c => c.Symbol).IsRequired().HasMaxLength(10);
        builder.Property(c => c.ExchangeRateToBase).HasPrecision(18, 6).HasDefaultValue(1.0m);
        builder.Property(c => c.IsBaseCurrency).HasDefaultValue(false);
        builder.Property(c => c.FractionName).HasMaxLength(20).IsRequired(false);
        builder.Property(c => c.IsSystem).HasDefaultValue(false);
        builder.HasQueryFilter(c => c.IsActive);

        // UNIQUE indexes
        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.IsBaseCurrency)
            .IsUnique()
            .HasFilter("[IsBaseCurrency] = 1");

        // CHECK constraint
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_Currencies_ExchangeRate",
            "[ExchangeRateToBase] > 0"));
    }
}
```

**DeleteStrategy** (RULE-050): Three options when deleting a Currency:
- **Cancel** (`DeleteStrategy.Cancel`) — abort, do nothing
- **Deactivate** (`DeleteStrategy.Deactivate`) — `MarkAsDeleted()` → `IsActive = false`
- **Permanent** (`DeleteStrategy.Permanent`) — physical removal; **must catch `DbUpdateException`** if currency is referenced by invoices, cashboxes, or payments (RULE-200)

**IsSystem guard**: Currencies with `IsSystem = true` (e.g., YER, USD, SAR seeded by system) CANNOT be deleted — both soft and permanent delete return `Result.Failure("لا يمكن حذف عملة النظام. هذه العملة محمية بواسطة النظام")`. Only the `IsSystem` flag can be cleared manually in the database.

**Logging** (RULE-035/036):
- `Log.Information("Currency {Code} created: {Name}, Rate: {Rate}", code, name, exchangeRateToBase)`
- `Log.Warning("Cannot permanently delete Currency {Id}: referenced by other records")` (on FK violation)

**Estimate**: ~1 hour

---

### Task 2 — Create ExchangeRateHistory Domain Entity

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/ExchangeRateHistory.cs` | **CREATE** — new entity tracking rate changes |
| `Infrastructure/Data/Configurations/ExchangeRateHistoryConfiguration.cs` | **CREATE** — Fluent API config |
| `Infrastructure/Data/Migrations/` | New migration: `CREATE TABLE ExchangeRateHistories` |

**ExchangeRateHistory.cs**:

```csharp
namespace SalesSystem.Domain.Entities;

public class ExchangeRateHistory : BaseEntity
{
    public int CurrencyId { get; private set; }
    public decimal OldRate { get; private set; }
    public decimal NewRate { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string? RateType { get; private set; } // "Daily", "Monthly", "Yearly"
    public string? Notes { get; private set; }

    // Navigation
    public Currency Currency { get; private set; } = null!;

    private ExchangeRateHistory() { } // EF Core

    public static ExchangeRateHistory Create(
        int currencyId,
        decimal oldRate,
        decimal newRate,
        DateOnly? effectiveDate = null,
        string? rateType = "Daily",
        string? notes = null,
        int? changedByUserId = null)
    {
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة");
        if (oldRate <= 0)
            throw new DomainException("السعر القديم يجب أن يكون أكبر من الصفر");
        if (newRate <= 0)
            throw new DomainException("السعر الجديد يجب أن يكون أكبر من الصفر");

        return new ExchangeRateHistory
        {
            CurrencyId = currencyId,
            OldRate = oldRate,
            NewRate = newRate,
            EffectiveDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            RateType = rateType ?? "Daily",
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = changedByUserId,
            IsActive = true
        };
    }
}
```

**ExchangeRateHistoryConfiguration.cs**:

```csharp
public class ExchangeRateHistoryConfiguration : IEntityTypeConfiguration<ExchangeRateHistory>
{
    public void Configure(EntityTypeBuilder<ExchangeRateHistory> builder)
    {
        builder.ToTable("ExchangeRateHistories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OldRate).HasPrecision(18, 6);
        builder.Property(e => e.NewRate).HasPrecision(18, 6);
        builder.Property(e => e.RateType).HasMaxLength(20).HasDefaultValue("Daily");
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.EffectiveDate);

        // FK to Currency
        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);  // RULE-214

        // Index for fast lookups
        builder.HasIndex(e => new { e.CurrencyId, e.EffectiveDate });
    }
}
```

**Estimate**: ~45 minutes

---

### Task 3 — Seed Currency Data (YER, USD, SAR)

**File**: `Infrastructure/Data/DbSeeder.cs`

Add AFTER the Tax seed block, with **independent `AnyAsync()` guard**:

```csharp
// ═══════════════════════════════════════════
// Seed Currencies
// ═══════════════════════════════════════════
if (!await db.Set<Currency>().AnyAsync())
{
    var currencies = new List<Currency>
    {
        Currency.Create("ريال يمني", "YER", "﷼", exchangeRateToBase: 1.0m, isBaseCurrency: true,
            fractionName: "فلس", isSystem: true),
        Currency.Create("دولار أمريكي", "USD", "$", exchangeRateToBase: 250.0m,  // 1 USD = 250 YER (example)
            fractionName: "سنت", isSystem: true),
        Currency.Create("ريال سعودي", "SAR", "﷼", exchangeRateToBase: 66.5m,    // 1 SAR = 66.5 YER (example)
            isSystem: true),
    };
    db.Set<Currency>().AddRange(currencies);
    logger?.LogInformation("Seeded {Count} currencies.", currencies.Count);
}
```

**Logging** (RULE-035):
- `Log.Information("Seeded {Count} currencies.", currencies.Count)` on success
- `Log.Warning("Currencies already seeded — skipping.")` if guard prevents

**Estimate**: ~10 minutes

---

### Task 4 — Contracts: DTOs and Requests

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `CurrencyDto`, `ExchangeRateHistoryDto` |
| `Contracts/Requests/CurrencyRequests.cs` | **CREATE** — `CreateCurrencyRequest`, `UpdateCurrencyRequest` |

**DTOs** (in `AllDtos.cs`):

```csharp
public record CurrencyDto(
    int Id,
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName,
    bool IsSystem,
    bool IsActive);

public record ExchangeRateHistoryDto(
    int Id,
    int CurrencyId,
    decimal OldRate,
    decimal NewRate,
    DateOnly EffectiveDate,
    string? RateType,
    string? Notes,
    DateTime CreatedAt);
```

**Requests** (new file `Contracts/Requests/CurrencyRequests.cs`):

```csharp
namespace SalesSystem.Contracts.Requests;

public record CreateCurrencyRequest(
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase = 1.0m,
    bool IsBaseCurrency = false,
    string? FractionName = null);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName = null);

public record UpdateExchangeRateRequest(
    decimal NewRate,
    DateOnly? EffectiveDate = null,
    string? RateType = "Daily",
    string? Notes = null);
```

**FluentValidation** (RULE-044):

```csharp
// CurrencyRequestsValidator.cs
public class CreateCurrencyRequestValidator : AbstractValidator<CreateCurrencyRequest>
{
    public CreateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم العملة مطلوب")
            .MaximumLength(100).WithMessage("اسم العملة لا يتجاوز 100 حرف");
        RuleFor(x => x.Code).NotEmpty().WithMessage("رمز العملة مطلوب")
            .MaximumLength(10).WithMessage("رمز العملة لا يتجاوز 10 أحرف");
        RuleFor(x => x.Symbol).NotEmpty().WithMessage("Symbol مطلوب")
            .MaximumLength(10).WithMessage("Symbol لا يتجاوز 10 أحرف");
        RuleFor(x => x.ExchangeRateToBase)
            .GreaterThan(0).WithMessage("سعر الصرف يجب أن يكون أكبر من الصفر")
            .PrecisionScale(18, 6, false).WithMessage("سعر الصرف غير صحيح");
        RuleFor(x => x.FractionName)
            .MaximumLength(20).WithMessage("اسم الجزء لا يتجاوز 20 حرفاً");
    }
}

public class UpdateCurrencyRequestValidator : AbstractValidator<UpdateCurrencyRequest>
{
    public UpdateCurrencyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم العملة مطلوب");
        RuleFor(x => x.Symbol).NotEmpty().WithMessage("Symbol مطلوب");
        RuleFor(x => x.ExchangeRateToBase)
            .GreaterThan(0).WithMessage("سعر الصرف يجب أن يكون أكبر من الصفر");
        RuleFor(x => x.FractionName)
            .MaximumLength(20).WithMessage("اسم الجزء لا يتجاوز 20 حرفاً");
    }
}
```

**Estimate**: ~30 minutes

---

### Task 5 — Application Service Layer: ICurrencyService + CurrencyService

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/ICurrencyService.cs` | **CREATE** — 7 methods |
| `Application/Services/CurrencyService.cs` | **CREATE** — full implementation |
| `Application/Interfaces/Services/IExchangeRateService.cs` | **CREATE** — 3 methods |
| `Application/Services/ExchangeRateService.cs` | **CREATE** — rate change + history |

**ICurrencyService.cs**:

```csharp
public interface ICurrencyService
{
    Task<Result<List<CurrencyDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CurrencyDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CurrencyDto>> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Result<CurrencyDto>> GetBaseCurrencyAsync(CancellationToken ct = default);
    Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default);
    Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);                    // Soft delete
    Task<Result> DeletePermanentlyAsync(int id, CancellationToken ct = default);          // With DbUpdateException catch
}
```

**IExchangeRateService.cs**:

```csharp
public interface IExchangeRateService
{
    Task<Result<List<ExchangeRateHistoryDto>>> GetHistoryAsync(int currencyId, CancellationToken ct = default);
    Task<Result<ExchangeRateHistoryDto>> UpdateRateAsync(int currencyId, UpdateExchangeRateRequest request, CancellationToken ct = default);
    Task<Result<decimal>> ConvertToBaseAsync(int currencyId, decimal amount, CancellationToken ct = default);
}
```

**CurrencyService.cs** key behaviors:

1. **CreateAsync**: if `IsBaseCurrency = true`, unset all other base currencies first (only one base allowed)
2. **UpdateAsync**: if `IsBaseCurrency = true`, unset all other base currencies
3. **DeleteAsync (soft) + DeletePermanentlyAsync**: check `currency.IsSystem` first — if true, return `Result.Failure("لا يمكن حذف عملة النظام. هذه العملة محمية بواسطة النظام")`
4. **DeletePermanentlyAsync**: after IsSystem guard, catch `DbUpdateException` → `Result.Failure("لا يمكن حذف هذه العملة لأنها مرتبطة بفواتير أو صناديق أو مدفوعات")` (RULE-200)
5. **ConvertToBaseAsync**: `return amount * currency.ExchangeRateToBase;` (simple multiplication)
6. **Logging**: `Log.Information("Currency {Code} created/updated/deleted", ...)` on every CRUD (RULE-035)

**ExchangeRateService.cs** key behaviors:

1. **UpdateRateAsync**: Records current rate in `ExchangeRateHistory` before changing
2. **GetHistoryAsync**: Returns newest-first sorted history (RULE-220)
3. **ConvertToBaseAsync**: Lookup rate at time (or use current if no date specified)

**Estimate**: ~2 hours

---

### Task 6 — API Layer: CurrenciesController

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/CurrenciesController.cs` | **CREATE** — 8 endpoints |
| `Api/Validators/CurrencyRequestsValidator.cs` | **CREATE** — FluentValidation |

**CurrenciesController.cs**:

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| GET | `/api/v1/currencies` | `AllStaff` | List all active currencies |
| GET | `/api/v1/currencies/{id}` | `AllStaff` | Get by ID |
| GET | `/api/v1/currencies/code/{code}` | `AllStaff` | Get by code (e.g., "USD") |
| GET | `/api/v1/currencies/base` | `AllStaff` | Get base currency |
| GET | `/api/v1/currencies/{id}/history` | `ManagerAndAbove` | Get exchange rate history |
| POST | `/api/v1/currencies` | `AdminOnly` | Create currency |
| PUT | `/api/v1/currencies/{id}` | `AdminOnly` | Update currency |
| PUT | `/api/v1/currencies/{id}/rate` | `AdminOnly` | Update exchange rate only |
| DELETE | `/api/v1/currencies/{id}` | `AdminOnly` | Soft delete |
| DELETE | `/api/v1/currencies/permanent/{id}` | `AdminOnly` | Hard delete (with FK guard) |

**Controller purity** (RULE-203): Controller injects `ICurrencyService` and `IExchangeRateService` only — NO `DbContext` or `IUnitOfWork` injection.

**Estimate**: ~1 hour

---

### Task 7 — Desktop API Service

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Services/Api/IApiService.cs` | Add `ICurrencyApiService` interface |
| `DesktopPWF/Services/Api/CurrencyApiService.cs` | **CREATE** — HTTP client |

**ICurrencyApiService methods**:
- `GetAllAsync(bool includeInactive = false)`
- `GetByIdAsync(int id)`
- `GetByCodeAsync(string code)`
- `GetBaseCurrencyAsync()`
- `GetHistoryAsync(int currencyId)`
- `CreateAsync(CreateCurrencyRequest request)`
- `UpdateAsync(int id, UpdateCurrencyRequest request)`
- `UpdateRateAsync(int id, UpdateExchangeRateRequest request)`
- `DeleteAsync(int id)` (soft)
- `DeletePermanentlyAsync(int id)`

**Error handling** (RULE-184): All HTTP responses check `ContentType == "application/json"` before parsing error JSON.

**Estimate**: ~1 hour

---

### Task 8 — Desktop ViewModels

**Files**:

| File | Content |
|------|---------|
| `ViewModels/Currencies/CurrenciesListViewModel.cs` | **CREATE** — List with newest-first sort (RULE-220), DeleteStrategy dialog (RULE-050) |
| `ViewModels/Currencies/CurrencyEditorViewModel.cs` | **CREATE** — Editor with INotifyDataErrorInfo (RULE-228) |
| `Messaging/Messages/AppMessages.cs` | Add `CurrencyChangedMessage` + `ExchangeRateChangedMessage` |

**CurrenciesListViewModel.cs**:

```csharp
public class CurrenciesListViewModel : ViewModelBase, IDisposable
{
    private readonly ICurrencyApiService _currencyService;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;
    private IDisposable? _subscription;

    public ObservableCollection<CurrencyDto> Currencies { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ShowHistoryCommand { get; }
    public ICommand SetBaseCurrencyCommand { get; }

    public CurrenciesListViewModel(
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IEventBus eventBus)
    {
        _currencyService = currencyService;
        _dialogService = dialogService;
        _screenWindowService = screenWindowService;
        _eventBus = eventBus;
        _subscription = eventBus.Subscribe<CurrencyChangedMessage>(_ => _ = LoadCurrenciesAsync());

        AddCommand = new RelayCommand(AddCurrency);                          // NO CanExecute (RULE-059)
        EditCommand = new RelayCommand<CurrencyDto>(EditCurrency);           // NO CanExecute
        DeleteCommand = new AsyncRelayCommand<CurrencyDto>(DeleteCurrencyAsync);
        ShowHistoryCommand = new RelayCommand<CurrencyDto>(ShowHistory);
        SetBaseCurrencyCommand = new AsyncRelayCommand<CurrencyDto>(SetBaseCurrencyAsync);
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCurrenciesOperationAsync)));

        _ = LoadCurrenciesAsync();
    }

    private async Task LoadCurrenciesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _currencyService.GetAllAsync(IncludeInactive);
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Currencies.Clear();
                foreach (var c in result.Value.OrderByDescending(x => x.Id))  // RULE-220
                    Currencies.Add(c);
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العملات", "LoadCurrencies");
        }
    }

    private void AddCurrency()
    {
        var editorVm = App.GetService<CurrencyEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة عملة جديدة",
            OnClosed = (vm) =>
            {
                if (vm is CurrencyEditorViewModel editor && editor.SavedSuccessfully)
                    _eventBus.Publish(new CurrencyChangedMessage(editor.CurrencyId!.Value));
            }
        });
    }

    // ... EditCurrency, DeleteCurrencyAsync, ShowHistory follow same pattern

    public void Dispose() => _subscription?.Dispose();
}
```

**CurrencyEditorViewModel.cs** — key features:
- INotifyDataErrorInfo validation (RULE-228)
- `SetDialogService()` in every constructor (RULE-227)
- Save always enabled — `Validate()` on click shows warning dialog with all errors (RULE-059)
- `ValidateAllAsync()` called in pre-save (RULE-229)
- Base currency toggle: if setting `IsBaseCurrency = true`, warning dialog confirms action

**ViewModel patterns** (RULE-141):
- All async commands wrapped in `ExecuteAsync()`
- Error messages via `LogSystemError()` (RULE-199) — NEVER `ex.Message` in user dialogs (RULE-171)
- Dialog titles are screen-specific: `"خطأ في حفظ العملة"` (RULE-173)
- All user messages via `IDialogService` — NO `MessageBox.Show` (RULE-174)
- Async suffix on all dialog calls: `ShowErrorAsync` (RULE-175)

**Arabic validation messages**:
- `"اسم العملة مطلوب"` (Currency name required)
- `"رمز العملة مطلوب"` (Currency code required)
- `"رمز العملة (Symbol) مطلوب"` (Symbol required)
- `"سعر الصرف يجب أن يكون أكبر من صفر"` (Exchange rate must be > 0)
- `"رمز العملة موجود بالفعل"` (Currency code already exists — from API)
- `"لا يمكن حذف العملة الأساسية. قم بتعيين عملة أخرى كقاعدة أولاً"` (Cannot delete base currency)

**EventBus Messages**:

```csharp
public record CurrencyChangedMessage(int CurrencyId);
public record ExchangeRateChangedMessage(int CurrencyId);
```

**Estimate**: ~3 hours

---

### Task 9 — Desktop Views (XAML)

**Files**:

| File | Content |
|------|---------|
| `Views/Currencies/CurrenciesListView.xaml` | **CREATE** — DataGrid with columns |
| `Views/Currencies/CurrenciesListView.xaml.cs` | **CREATE** — Code-behind |
| `Views/Currencies/CurrencyEditorView.xaml` | **CREATE** — Editor form |
| `Views/Currencies/CurrencyEditorView.xaml.cs` | **CREATE** — Code-behind |
| `Views/Currencies/ExchangeRateHistoryView.xaml` | **CREATE** — Read-only history list |
| `Views/Currencies/ExchangeRateHistoryView.xaml.cs` | **CREATE** — Code-behind |

**CurrenciesListView.xaml** columns:
- `Id` (int)
- `Code` (string — bold, e.g., "USD")
- `Name` (string — Arabic, e.g., "دولار أمريكي")
- `Symbol` (string)
- `ExchangeRateToBase` (decimal formatted)
- `IsBaseCurrency` (bool — displayed as badge "الأساسية" in green)
- `IsActive` (bool — soft delete status)

**Actions column**:
- Edit: `"تعديل بيانات العملة"`
- Delete: `"حذف العملة — سيتم إلغاء تنشيطها"`
- Set as Base: `"تعيين كعملة أساسية للنظام"`
- Rate History: `"عرض سجل أسعار الصرف"`

**Arabic ToolTips** (RULE-185-190):
- Add button: `"إضافة عملة جديدة"`
- Edit button: `"تعديل بيانات العملة — الاسم، الرمز، سعر الصرف"`
- Delete button: `"حذف العملة — سيتم إلغاء تنشيطها مع الاحتفاظ بالسجلات"`
- Permanent Delete: `"حذف العملة بشكل نهائي — لا يمكن التراجع"`
- Set as Base: `"تعيين هذه العملة كعملة أساسية للنظام — سيتم تحويل جميع العمليات إليها"`
- Rate History: `"عرض سجل تغييرات سعر الصرف لهذه العملة"`
- Save button: `"حفظ بيانات العملة"`
- Cancel button: `"إلغاء التعديل والعودة"`
- Error dismiss: `"إخفاء رسالة الخطأ"`
- Empty-state button: `"➕ إضافة أول عملة — أضف عملة جديدة للنظام"`

**UI Compact** (RULE-262-274):
- Button/TextBox heights: via style (28px default) — no hardcoded `Height="36"`
- Padding: `10,4` via style — no hardcoded `Padding="16,0"`
- Header: `Padding="12,6"`, Footer: `Padding="12,8"`
- Section margins: `Margin="0,0,0,6"` between fields
- Dialog title font: `FontSize="16"`, section headers: `FontSize="14"`
- Empty-state buttons: `Margin="0,12,0,0"` Width="140"
- Dialog icons: `Width="44" Height="44"` max

**ExchangeRateHistoryView.xaml**:
- DataGrid: CurrencyId, OldRate, NewRate, EffectiveDate, RateType, CreatedAt
- Read-only (no add/edit/delete)
- ToolTip: `"سجل تغييرات أسعار الصرف — للإطلاع فقط"`

**Estimate**: ~3.5 hours

---

### Task 10 — Blocker 1: CashBox Migration (string CurrencyCode → int? CurrencyId FK)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/CashBox.cs` | Replace `string CurrencyCode` with `int? CurrencyId` + navigation property |
| `Infrastructure/Data/Configurations/CashBoxConfiguration.cs` | Update FK config + remove old column config |
| `Contracts/DTOs/AllDtos.cs` — CashBox DTO objects | Update any CashBox-related DTOs |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE + FK + data migration |
| Desktop CashBox ViewModels | Update to use CurrencyId instead of CurrencyCode |

**CashBox.cs** changes:

```csharp
// BEFORE:
public string CurrencyCode { get; private set; } = "SAR";

// AFTER:
public int? CurrencyId { get; private set; }
public Currency? Currency { get; private set; }

// Updated Create signature:
public static CashBox Create(
    string boxName,
    int? currencyId = null,    // NEW — FK to Currency
    int? branchId = null,
    int? assignedUserId = null,
    decimal initialBalance = 0)
{
    // ... same validation ...
    return new CashBox
    {
        BoxName = boxName.Trim(),
        CurrencyId = currencyId,    // Was: CurrencyCode = currencyCode,
        BranchId = branchId,
        AssignedUserId = assignedUserId,
        CurrentBalance = initialBalance,
        IsActive = true
    };
}
```

**CashBoxConfiguration.cs** — update FK:

```csharp
// REPLACE old CurrencyCode config:
// builder.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("SAR");

// WITH new FK config:
builder.Property(x => x.CurrencyId);
builder.HasOne(x => x.Currency)
    .WithMany()
    .HasForeignKey(x => x.CurrencyId)
    .OnDelete(DeleteBehavior.Restrict);  // RULE-214
```

**Migration SQL**:

```sql
-- Step 1: Add nullable FK column
ALTER TABLE CashBoxes ADD CurrencyId int NULL;

-- Step 2: Data migration — match CurrencyCode to Currency.Code
UPDATE c SET c.CurrencyId = cur.Id
FROM CashBoxes c
INNER JOIN Currencies cur ON cur.Code = c.CurrencyCode AND cur.IsActive = 1;

-- Step 3: Add FK constraint
ALTER TABLE CashBoxes ADD CONSTRAINT FK_CashBoxes_Currencies
    FOREIGN KEY (CurrencyId) REFERENCES Currencies(Id) ON DELETE NO ACTION;

-- Step 4: Add index for FK lookups
CREATE INDEX IX_CashBoxes_CurrencyId ON CashBoxes(CurrencyId);

-- NOTE: CurrencyCode column is NOT dropped in V1 (backwards compat).
-- Document for removal in Phase 25+.
```

**Logging**:
- `Log.Information("CashBox {Id} currency migrated: {OldCode} → CurrencyId {NewId}", id, oldCode, newId)` for each migrated record
- `Log.Warning("CashBox {Id} has unmapped CurrencyCode {Code} — no matching Currency found", id, code)` for unmapped codes

**Estimate**: ~2 hours

---

### Task 11 — Blocker 2: Add CurrencyId + ExchangeRate to Invoices

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoice.cs` | Add `int? CurrencyId`, `decimal? ExchangeRate`, `Currency? Currency` nav property + `SetCurrency()` method |
| `Domain/Entities/PurchaseInvoice.cs` | Same |
| `Domain/Entities/SalesReturn.cs` | Same |
| `Domain/Entities/PurchaseReturn.cs` | Same |
| `Infrastructure/Data/Configurations/SalesInvoiceConfiguration.cs` | Add FK config |
| `Infrastructure/Data/Configurations/PurchaseInvoiceConfiguration.cs` | Add FK config |
| `Infrastructure/Data/Configurations/ReturnsTransfersConfiguration.cs` | Add FK config for returns |
| `Contracts/DTOs/AllDtos.cs` | Add `CurrencyId`, `CurrencyCode`, `CurrencyName`, `ExchangeRate` to invoice DTOs |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE + FK |
| Application invoice services | Update DTO mapping to include currency info |

**SalesInvoice.cs** additions:

```csharp
// New properties:
public int? CurrencyId { get; private set; }
public decimal? ExchangeRate { get; private set; }
public Currency? Currency { get; private set; }

// New domain method:
public void SetCurrency(int? currencyId, decimal? exchangeRate)
{
    CurrencyId = currencyId;
    ExchangeRate = exchangeRate;
}

// Updated Create() — add optional parameters:
int? currencyId = null,
decimal? exchangeRate = null,
```

**PurchaseInvoice.cs** — same pattern.

**SalesInvoiceConfiguration.cs** — add FK:

```csharp
builder.HasOne(i => i.Currency)
    .WithMany()
    .HasForeignKey(i => i.CurrencyId)
    .OnDelete(DeleteBehavior.Restrict);  // RULE-214
```

**DTO updates**:

```csharp
// SalesInvoiceDto — ADD:
int? CurrencyId,
string? CurrencyCode,
string? CurrencyName,
decimal? ExchangeRate,

// PurchaseInvoiceDto — ADD same fields
```

**Estimate**: ~2 hours

---

### Task 12 — Add CurrencyId to Payment Entities

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/CustomerPayment.cs` | Add `int? CurrencyId`, `decimal? ExchangeRate`, `Currency? Currency` nav + `SetCurrency()` |
| `Domain/Entities/SupplierPayment.cs` | Same |
| `Domain/Entities/CashTransaction.cs` | Add `int? CurrencyId`, `Currency? Currency` nav |
| Infrastructure configs for each | Add FK configs with `DeleteBehavior.Restrict` |
| `Contracts/DTOs/AllDtos.cs` | Add currency fields to payment DTOs |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE + FK |

**Estimate**: ~1.5 hours

---

### Task 13 — Deprecate StoreSettings.CurrencyCode

**Files**:

| File | Change |
|------|--------|
| `ViewModels/SettingsViewModel.cs` | Hide `CurrencyCode` from save logic |
| `Views/Settings/SettingsView.xaml` | Hide CurrencyCode field from Company Info card |
| `Application/Services/StoreSettingsService.cs` | Map `CurrencyCode` from `Currency.IsBaseCurrency` lookup when reading SettingsDto |

**Migration path** (don't delete column):
- Keep `CurrencyCode` in entity and DB
- Just stop using it in UI and service logic
- Document: Remove column in Phase 25+

**Estimate**: ~30 minutes

---

### Task 14 — DI Registrations + Navigation

**Files**:

| File | Change |
|------|--------|
| `Api/Program.cs` | Register `ICurrencyService`, `IExchangeRateService`, `CurrenciesController` |
| `DesktopPWF/App.xaml.cs` | Register ViewModels, Views, API service + navigation entry in MainWindow |

**Desktop DI registration**:

```csharp
// Services
services.AddHttpClient<ICurrencyApiService, CurrencyApiService>(client => {
    client.BaseAddress = new Uri("http://localhost:5221");
});

// ViewModels
services.AddTransient<CurrenciesListViewModel>();
services.AddTransient<CurrencyEditorViewModel>();
services.AddTransient<ExchangeRateHistoryViewModel>();

// Views
services.AddTransient<CurrenciesListView>();
services.AddTransient<CurrencyEditorView>();
services.AddTransient<ExchangeRateHistoryView>();
```

**MainWindow navigation menu item**:
```xml
<MenuItem Header="العملات" Command="{Binding NavigateCommand}" CommandParameter="CurrenciesListView"
          ToolTip="إدارة العملات وأسعار الصرف — إضافة وتعديل العملات"/>
```

**Estimate**: ~30 minutes

---

### Task 15 — Unit Tests (Expanded)

**Test Infrastructure:**
- Use xUnit + Moq + FluentAssertions
- `SalesSystem.Domain.Tests` for entity tests
- `SalesSystem.Application.Tests` for service tests
- `SalesSystem.Api.Tests` for API controller tests
- `SalesSystem.Arch.Tests` for configuration tests
- `SalesSystem.DesktopPWF.Tests` for ViewModel tests

**Files to create/modify:**

| File | Change |
|------|--------|
| `Tests/Domain/CurrencyTests.cs` | **CREATE** — Entity factory guards, Update, MarkAsDeleted |
| `Tests/Domain/ExchangeRateHistoryTests.cs` | **CREATE** — Entity factory guards |
| `Tests/Application/CurrencyServiceTests.cs` | **CREATE** — CRUD, base currency, FK guard |
| `Tests/Application/ExchangeRateServiceTests.cs` | **CREATE** — Rate change, history recording |
| `Tests/Api/CurrenciesControllerTests.cs` | **CREATE** — Endpoint tests |
| `Tests/Desktop/CurrenciesListViewModelTests.cs` | **CREATE** — List load, add, edit, delete |
| `Tests/Desktop/CurrencyEditorViewModelTests.cs` | **CREATE** — Validation, save |
| `Tests/Arch/CurrencyConfigurationTests.cs` | **CREATE** — Precision, Restrict, indexes |
| `Tests/Arch/ExchangeRateHistoryConfigurationTests.cs` | **CREATE** — Config checks |

**Estimate**: ~3.5 hours

---

### 1. Domain Entity Tests

#### Currency Entity (`CurrencyTests.cs`)

```csharp
[Fact]
public void Create_ValidInput_CreatesCurrencyCorrectly()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 2, isSystem: true);
    Assert.Equal("ريال يمني", currency.Name);
    Assert.Equal("YER", currency.Code);
    Assert.Equal("﷼", currency.Symbol);
    Assert.Equal(2, currency.DecimalPlaces);
    Assert.True(currency.IsSystem);
    Assert.True(currency.IsActive);
}

[Fact]
public void Create_EmptyName_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("", "YER", "﷼", 2));
    Assert.Contains("مطلوب", ex.Message);
}

[Fact]
public void Create_EmptyCode_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("ريال", "", "﷼", 2));
    Assert.Contains("مطلوب", ex.Message);
}

[Fact]
public void Create_CodeTooLong_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("ريال", "ABCDE", "﷼", 2));
    Assert.Contains("3 أحرف", ex.Message);
}

[Fact]
public void Create_NegativeDecimalPlaces_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("ريال", "YER", "﷼", -1));
    Assert.Contains("سال", ex.Message);
}

[Fact]
public void Create_FractionNameMax20Chars_ValidatesLength()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 2, fractionName: "فلس");
    Assert.Equal("فلس", currency.FractionName);

    var longName = new string('ا', 21);
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("ريال", "YER", "﷼", 2, fractionName: longName));
    Assert.Contains("20", ex.Message);
}

[Fact]
public void Create_IsSystemDefaultsToFalse()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    Assert.False(currency.IsSystem);
}

[Fact]
public void Update_ValidInput_UpdatesCorrectly()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2);
    currency.Update("ريال يمني", "YER", "﷼", 2, "فلس");
    Assert.Equal("ريال يمني", currency.Name);
    Assert.Equal("فلس", currency.FractionName);
}

[Fact]
public void Update_SystemCurrency_ThrowsDomainException()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true, isBaseCurrency: true);
    var ex = Assert.Throws<DomainException>(() =>
        currency.Update("اسم جديد", "ABC", "$", 2));
    Assert.Contains("عملة نظام", ex.Message);
}

[Fact]
public void UpdateExchangeRate_ValidRate_UpdatesRateAndRecordsHistory()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2);
    currency.UpdateExchangeRate(250.00m, DateTime.Today, 1);
    Assert.Equal(250.00m, currency.ExchangeRateToBase);
}

[Fact]
public void UpdateExchangeRate_ZeroRate_ThrowsDomainException()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2);
    var ex = Assert.Throws<DomainException>(() =>
        currency.UpdateExchangeRate(0, DateTime.Today, 1));
    Assert.Contains("أكبر من", ex.Message);
}

[Fact]
public void MarkAsDeleted_SystemCurrency_ThrowsDomainException()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true);
    var ex = Assert.Throws<DomainException>(() =>
        currency.MarkAsDeleted());
    Assert.Contains("عملة نظام", ex.Message);
}

[Fact]
public void MarkAsDeleted_NonSystemCurrency_Succeeds()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    currency.MarkAsDeleted();
    Assert.False(currency.IsActive);
}

[Fact]
public void SetAsBaseCurrency_UpdatesFlag()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2);
    Assert.False(currency.IsBaseCurrency);
    currency.SetAsBaseCurrency();
    Assert.True(currency.IsBaseCurrency);
    currency.UnsetBaseCurrency();
    Assert.False(currency.IsBaseCurrency);
}
```

#### ExchangeRateHistory Entity (`ExchangeRateHistoryTests.cs`)

```csharp
[Fact]
public void Create_ValidInput_CreatesHistoryRecord()
{
    var history = ExchangeRateHistory.Create(1, 250.00m, 260.00m, 1, "Update");
    Assert.Equal(1, history.CurrencyId);
    Assert.Equal(250.00m, history.OldRate);
    Assert.Equal(260.00m, history.NewRate);
    Assert.Equal("Update", history.ChangeReason);
}

[Fact]
public void Create_ZeroOldRate_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        ExchangeRateHistory.Create(1, 0, 260.00m, 1, "Update"));
    Assert.Contains("أكبر من", ex.Message);
}

[Fact]
public void Create_ZeroNewRate_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        ExchangeRateHistory.Create(1, 250.00m, 0, 1, "Update"));
    Assert.Contains("أكبر من", ex.Message);
}

[Fact]
public void Create_EmptyReason_DefaultsToAutoGenerated()
{
    var history = ExchangeRateHistory.Create(1, 250.00m, 260.00m, 1, "");
    Assert.NotNull(history.ChangeReason);
}

[Fact]
public void Create_NegativeRate_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        ExchangeRateHistory.Create(1, 250.00m, -10.00m, 1, "Error"));
    Assert.Contains("أكبر من", ex.Message);
}
```

---

### 2. Service Tests (using `Mock<IUnitOfWork>`)

#### CurrencyServiceTests.cs

```csharp
public class CurrencyServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICurrencyRepository> _repoMock;
    private readonly CurrencyService _service;

    public CurrencyServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repoMock = new Mock<ICurrencyRepository>();
        _uowMock.Setup(x => x.Currencies).Returns(_repoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _service = new CurrencyService(_uowMock.Object, Mock.Of<ILogger<CurrencyService>>());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        var result = await _service.CreateAsync(
            new CreateCurrencyRequest { Name = "ريال", Code = "YER", Symbol = "﷼", DecimalPlaces = 2 },
            CancellationToken.None);
        Assert.True(result.IsSuccess);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        _repoMock.Setup(x => x.AnyAsync(c => c.Code == "YER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var result = await _service.CreateAsync(
            new CreateCurrencyRequest { Name = "ريال", Code = "YER", Symbol = "﷼", DecimalPlaces = 2 },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var currency = Currency.Create("ريال", "YER", "﷼", 2);
        _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("ريال", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        _repoMock.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Currency?)null);
        var result = await _service.GetByIdAsync(99, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_SystemCurrency_ReturnsFailure()
    {
        var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true);
        _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Contains("عملة نظام", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_NonSystemCurrency_ReturnsSuccess()
    {
        var currency = Currency.Create("دولار", "USD", "$", 2);
        _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeletePermanentlyAsync_BaseCurrency_ReturnsFailure()
    {
        var currency = Currency.Create("ريال", "YER", "﷼", 2, isBaseCurrency: true);
        _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);
        var result = await _service.DeletePermanentlyAsync(1, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Contains("عملة قاعدة", result.Error);
    }

    [Fact]
    public async Task SetAsBaseCurrency_UnsetsPreviousBase()
    {
        var oldBase = Currency.Create("ريال", "YER", "﷼", 2, isBaseCurrency: true);
        var newBase = Currency.Create("دولار", "USD", "$", 2);
        _repoMock.Setup(x => x.GetBaseCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldBase);
        _repoMock.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newBase);
        var result = await _service.SetAsBaseCurrencyAsync(2, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.False(oldBase.IsBaseCurrency);
        Assert.True(newBase.IsBaseCurrency);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveCurrencies()
    {
        var currencies = new List<Currency>
        {
            Currency.Create("ريال", "YER", "﷼", 2, isSystem: true),
            Currency.Create("دولار", "USD", "$", 2)
        };
        _repoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);
        var result = await _service.GetAllAsync(CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }
}
```

#### ExchangeRateServiceTests.cs

```csharp
[Fact]
public async Task UpdateRateAsync_Valid_RecordsHistory()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    currency.SetAsBaseCurrency();
    currency.UpdateExchangeRate(250.00m, DateTime.Today, 1);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(currency);
    var rateRepoMock = new Mock<IExchangeRateHistoryRepository>();
    _uowMock.Setup(x => x.ExchangeRateHistories).Returns(rateRepoMock.Object);
    var result = await _exchangeService.UpdateRateAsync(1, 260.00m, 1, "تحديث السعر", CancellationToken.None);
    Assert.True(result.IsSuccess);
    rateRepoMock.Verify(x => x.AddAsync(It.IsAny<ExchangeRateHistory>(), It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task UpdateRateAsync_BaseCurrency_ReturnsSuccess()
{
    var baseCurrency = Currency.Create("ريال", "YER", "﷼", 2, isBaseCurrency: true);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(baseCurrency);
    var result = await _exchangeService.UpdateRateAsync(1, 1.00m, 1, "تحديث", CancellationToken.None);
    Assert.True(result.IsSuccess);
}

[Fact]
public async Task GetHistoryAsync_ReturnsOrderedHistory()
{
    var histories = new List<ExchangeRateHistory> { /* ... */ };
    _historyRepoMock.Setup(x => x.GetByCurrencyIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(histories);
    var result = await _exchangeService.GetHistoryAsync(1, CancellationToken.None);
    Assert.True(result.IsSuccess);
}

[Fact]
public async Task DeletePermanentlyAsync_CurrencyWithHistory_FailsWithFKGuard()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(currency);
    _historyRepoMock.Setup(x => x.HasHistory(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
    var result = await _currencyService.DeletePermanentlyAsync(1, CancellationToken.None);
    Assert.False(result.IsSuccess);
}

[Fact]
public async Task Transaction_Rollback_OnFailure()
{
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(Mock.Of<IDbContextTransaction>());
    _repoMock.Setup(x => x.AddAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("DB error"));
    var result = await _service.CreateAsync(
        new CreateCurrencyRequest { Name = "ريال", Code = "YER", Symbol = "﷼", DecimalPlaces = 2 },
        CancellationToken.None);
    Assert.False(result.IsSuccess);
    _uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

---

### 3. FluentValidation Tests

```csharp
public class CreateCurrencyRequestValidatorTests
{
    private readonly CreateCurrencyRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateCurrencyRequest { Name = "ريال", Code = "YER", Symbol = "﷼", DecimalPlaces = 2 };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(new CreateCurrencyRequest { Name = "", Code = "YER", Symbol = "﷼", DecimalPlaces = 2 });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void CodeLongerThan3_Fails()
    {
        var result = _validator.Validate(new CreateCurrencyRequest { Name = "Test", Code = "ABCD", Symbol = "$", DecimalPlaces = 2 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NegativeDecimalPlaces_Fails()
    {
        var result = _validator.Validate(new CreateCurrencyRequest { Name = "Test", Code = "USD", Symbol = "$", DecimalPlaces = -1 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void FractionNameTooLong_Fails()
    {
        var result = _validator.Validate(new CreateCurrencyRequest { Name = "Test", Code = "USD", Symbol = "$", DecimalPlaces = 2, FractionName = new string('ا', 21) });
        Assert.False(result.IsValid);
    }
}

public class UpdateExchangeRateRequestValidatorTests
{
    private readonly UpdateExchangeRateRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.Validate(new UpdateExchangeRateRequest { NewRate = 250.00m, ChangeReason = "Update" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ZeroRate_Fails()
    {
        var result = _validator.Validate(new UpdateExchangeRateRequest { NewRate = 0m, ChangeReason = "Update" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NegativeRate_Fails()
    {
        var result = _validator.Validate(new UpdateExchangeRateRequest { NewRate = -10m, ChangeReason = "Update" });
        Assert.False(result.IsValid);
    }
}
```

---

### 4. API Controller Tests (Integration)

```csharp
public class CurrenciesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CurrenciesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Returns200WithData()
    {
        var response = await _client.GetAsync("/api/v1/currencies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/currencies/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/currencies/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new { name = "ريال", code = "YER", symbol = "﷼", decimalPlaces = 2 };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/currencies", json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400()
    {
        var request = new { name = "", code = "", symbol = "", decimalPlaces = -1 };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/currencies", json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SystemCurrency_Returns400()
    {
        var response = await _client.DeleteAsync("/api/v1/currencies/1"); // YER is system
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/v1/currencies/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExchangeRate_Valid_Returns200()
    {
        var request = new { newRate = 260.00m, changeReason = "تحديث السعر" };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/api/v1/currencies/1/exchange-rate", json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/currencies");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Forbidden_Returns403()
    {
        // Cashier role cannot manage currencies
        var response = await _client.GetAsync("/api/v1/currencies");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

---

### 5. Database Configuration Tests

```csharp
public class CurrencyConfigurationTests
{
    [Fact]
    public void CurrencyConfiguration_DecimalPlaces_IsRequired()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new CurrencyConfiguration());
        var entity = builder.Entity<Currency>();
        var prop = entity.Metadata.FindProperty(nameof(Currency.DecimalPlaces));
        Assert.False(prop.IsNullable);
    }

    [Fact]
    public void CurrencyConfiguration_CodeHasMaxLength3()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new CurrencyConfiguration());
        var entity = builder.Entity<Currency>();
        var codeProp = entity.Metadata.FindProperty(nameof(Currency.Code));
        Assert.Equal(3, codeProp.GetMaxLength());
    }

    [Fact]
    public void CurrencyConfiguration_ForeignKeysUseRestrict()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new CurrencyConfiguration());
        var entity = builder.Entity<Currency>();
        var foreignKeys = entity.Metadata.GetForeignKeys();
        foreach (var fk in foreignKeys)
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void CurrencyConfiguration_UniqueIndexOnCode()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new CurrencyConfiguration());
        var entity = builder.Entity<Currency>();
        var index = entity.Metadata.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Code"));
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }
}

public class ExchangeRateHistoryConfigurationTests
{
    [Fact]
    public void ExchangeRateHistory_RatePrecision_18_6()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new ExchangeRateHistoryConfiguration());
        var entity = builder.Entity<ExchangeRateHistory>();
        var oldRateProp = entity.Metadata.FindProperty(nameof(ExchangeRateHistory.OldRate));
        Assert.Equal("decimal(18,6)", oldRateProp.GetColumnType());
        var newRateProp = entity.Metadata.FindProperty(nameof(ExchangeRateHistory.NewRate));
        Assert.Equal("decimal(18,6)", newRateProp.GetColumnType());
    }

    [Fact]
    public void ExchangeRateHistory_ForeignKeyToCurrency_Restrict()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new ExchangeRateHistoryConfiguration());
        var entity = builder.Entity<ExchangeRateHistory>();
        var fk = entity.Metadata.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Currency));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }
}
```

---

### 6. Phase 20-Specific Tests

#### Currency.Create(): FractionName max 20 chars

```csharp
[Fact]
public void Create_FractionNameMax20Chars_ValidatesBoundary()
{
    var valid = Currency.Create("ريال", "YER", "﷼", 2, fractionName: new string('ا', 20));
    Assert.NotNull(valid);
    var ex = Assert.Throws<DomainException>(() =>
        Currency.Create("ريال", "YER", "﷼", 2, fractionName: new string('ا', 21)));
    Assert.Contains("20", ex.Message);
}
```

#### Currency.Create(): IsSystem default false

```csharp
[Fact]
public void Create_IsSystemDefaultsToFalse()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    Assert.False(currency.IsSystem);
}

[Fact]
public void Create_IsSystemExplicitlyTrue()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true);
    Assert.True(currency.IsSystem);
}
```

#### Delete guard: IsSystem returns Result.Failure

```csharp
[Fact]
public async Task DeleteAsync_SystemCurrency_ReturnsFailureWithArabicMessage()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(currency);
    var result = await _service.DeleteAsync(1, CancellationToken.None);
    Assert.False(result.IsSuccess);
    Assert.Contains("لا يمكن حذف", result.Error);
}

[Fact]
public async Task DeletePermanentlyAsync_SystemCurrency_ReturnsFailure()
{
    var currency = Currency.Create("ريال", "YER", "﷼", 2, isSystem: true);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(currency);
    var result = await _service.DeletePermanentlyAsync(1, CancellationToken.None);
    Assert.False(result.IsSuccess);
}
```

#### ExchangeRateHistory: FK integrity on Currency delete (Restrict)

```csharp
[Fact]
public void ExchangeRateHistory_CurrencyFk_RestrictDeleteBehavior()
{
    var builder = new ModelBuilder();
    builder.ApplyConfiguration(new ExchangeRateHistoryConfiguration());
    var entity = builder.Entity<ExchangeRateHistory>();
    var fk = entity.Metadata.GetForeignKeys()
        .First(f => f.Properties.Any(p => p.Name == "CurrencyId"));
    Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
}

[Fact]
public async Task DeletePermanentlyAsync_CurrencyWithExchangeHistory_ReturnsFailure()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(currency);
    _historyRepoMock.Setup(x => x.HasHistory(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
    var result = await _service.DeletePermanentlyAsync(1, CancellationToken.None);
    Assert.False(result.IsSuccess);
    Assert.Contains("مرتبط", result.Error);
}
```

#### Exchange rate conversion: decimal(18,6) precision

```csharp
[Fact]
public void ExchangeRate_Precision_18_6()
{
    var builder = new ModelBuilder();
    builder.ApplyConfiguration(new CurrencyConfiguration());
    var entity = builder.Entity<Currency>();
    var rateProp = entity.Metadata.FindProperty(nameof(Currency.ExchangeRateToBase));
    Assert.Equal("decimal(18,6)", rateProp.GetColumnType());
}

[Fact]
public void ExchangeRate_AcceptsSixDecimalPlaces()
{
    var currency = Currency.Create("دولار", "USD", "$", 2);
    currency.UpdateExchangeRate(250.123456m, DateTime.Today, 1);
    Assert.Equal(250.123456m, currency.ExchangeRateToBase);
}
```

---

**Test count target:** 70+ tests across all test categories.

**Estimate:** ~3.5 hours

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | ExchangeRate uses `decimal(18,6)` — 6 places needed for precise FX rates. Money fields like `Amount` on invoices remain `decimal(18,2)`. Rate field config is explicit exception. | ✅ (18,6 documented) |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | CurrencyService — base currency switch unsets other base within transaction | ✅ |
| **RULE-006** | ALL services return `Result<T>` | CurrencyService, ExchangeRateService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Currency.Name, Code, Symbol all `nvarchar` | ✅ |
| **RULE-016** | BaseEntity audit fields | Currency, ExchangeRateHistory inherit BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | CurrencyService, ExchangeRateService | ✅ |
| **RULE-035** | Serilog for logging | All services: `Log.Information` on CRUD + rate changes | ✅ |
| **RULE-036** | Log critical operations | Rate changes, base currency switches, currency CRUD | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | CurrenciesController — all endpoints require auth | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Currency entity: `Create()`, `Update()`, `UpdateExchangeRate()`, `MarkAsDeleted()` — `FractionName`, `IsSystem` via private set | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateCurrencyRequestValidator, UpdateCurrencyRequestValidator, UpdateExchangeRateRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Currency: Cancel/Deactivate/Permanent + ShowDeleteConfirmationAsync dialog | ✅ |
| **RULE-052** | Guard Clauses on all entities | Currency.Create/Update — Arabic DomainException | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "اسم العملة مطلوب", "سعر الصرف يجب أن يكون أكبر من الصفر" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | CurrencyEditorViewModel (RULE-228) | ✅ |
| **RULE-059** | Save always enabled, validate on click | CurrencyEditorViewModel — no CanExecute blocking | ✅ |
| **RULE-084** | Record EVERY price/cost change | ExchangeRateHistory records every rate change — never update without audit | ✅ |
| **RULE-085** | Old/new values in audit record | ExchangeRateHistory stores OldRate, NewRate, ChangedByUserId, ChangeReason | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModels in Tasks 8-9 | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Editor opens via `OpenScreen()` — not `ShowDialog()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ العملة"`, `"خطأ في تحميل العملات"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, JSON parse crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, unmapped CashBox CurrencyCode, "not found" | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | CurrencyApiService — content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, MenuItems, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إضافة عملة جديدة" ✅, not "عملة" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | "تعيين كعملة أساسية — سيتم تحويل جميع الحسابات والتقارير إلى هذه العملة" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة العملات — إضافة وتعديل العملات وأسعار الصرف" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول عملة — أضف عملة جديدة للنظام" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | CurrencyService.DeletePermanentlyAsync catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | CurrencyService, ExchangeRateService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | CurrenciesController — service only | ✅ |
| **RULE-210** | CHECK constraints at DB level | `CHK_Currencies_ExchangeRate` (ExchangeRateToBase > 0) | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | CurrencyId on: CashBox, SalesInvoice, PurchaseInvoice, CustomerPayment, SupplierPayment, CashTransaction, ExchangeRateHistory | ✅ |
| **RULE-220** | Newest-first sorting on lists | CurrenciesListViewModel: OrderByDescending(Id), ExchangeRateHistory: OrderByDescending(EffectiveDate) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | CurrencyEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | CurrencyEditorViewModel | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in CurrencyEditorViewModel | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | Already exists — not in scope | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not affected by this phase | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected — InvoiceNo stays int | ✅ N/A |
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

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **CashBox CurrencyCode→FK migration loses data** | **HIGH** — production CashBoxes lose currency reference | Migration JOINs on Currency.Code — ensure all string codes match FK codes exactly. Log unmapped records. |
| **Two base currencies after seed** | Medium | DB filtered unique index `WHERE IsBaseCurrency = 1` enforces single base currency at DB level |
| **Service sets base currency without unsetting old base** | Medium | `CurrencyService.CreateAsync()` and `UpdateAsync()` explicitly unset other base currencies before setting new one, inside a transaction (RULE-003) |
| **DbUpdateException on Currency permanent delete** | Medium | CurrencyService catches `DbUpdateException` → `Result.Failure` with Arabic message (RULE-200) |
| **Invoice Create method signature change breaks callers** | Medium | Add `currencyId` and `exchangeRate` as **optional** parameters at the end — backward compatible for all existing callers |
| **Old StoreSettings.CurrencyCode still used somewhere** | Medium | Search all code for `.CurrencyCode` references — replace with `Currency.IsBaseCurrency` lookup |
| **ExchangeRate precision mismatch** | Low | Explicit `decimal(18,6)` on rate fields, explicit `decimal(18,2)` on money fields — no ambiguity |
| **Permanent delete of base currency** | Low | Domain guard: check `IsBaseCurrency` in `DeletePermanentlyAsync` and return `Result.Failure("لا يمكن حذف العملة الأساسية. قم بتعيين عملة أخرى كقاعدة أولاً")` |
| **User accidentally deletes system-seeded currency** | Low | `IsSystem` flag on YER/USD/SAR — `DeleteAsync` and `DeletePermanentlyAsync` both check `IsSystem` first and return `Result.Failure` with Arabic message |
| **Desktop offline: no currency data cached** | Low | Future enhancement: cache base currency locally. V1: requires network for Currency screens |
| **New migration conflicts with existing DB** | Low | Always nullable, additive columns — no breaking changes for invoice/payment FKs |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Currency seed data causes issues | `DELETE FROM Currencies;` — re-seed with corrected data |
| ExchangeRateHistory migration causes issues | `DROP TABLE ExchangeRateHistories;` |
| CashBox CurrencyId migration causes issues | `ALTER TABLE CashBoxes DROP COLUMN CurrencyId;` — falls back to CurrencyCode string |
| Invoice CurrencyId migration causes issues | `ALTER TABLE SalesInvoices DROP COLUMN CurrencyId; ALTER TABLE SalesInvoices DROP COLUMN ExchangeRate;` same for PurchaseInvoices |
| Payment CurrencyId migration causes issues | `ALTER TABLE CustomerPayments DROP COLUMN CurrencyId;` same for SupplierPayments |
| Currency UI not needed | Remove DI registration + navigation entry — no data impact |
| Desktop CurrencyApiService not working | Catch HTTP errors gracefully — system works without currency screen (invoices default to base currency) |
| StoreSettings.CurrencyCode deprecation causes issues | Restore field visibility in SettingsView.xaml — no data loss |

---

## 12. v4.6.8 — Stabilization Fixes

### 12.1 Problem: CurrencyService Uses Manual Transactions — CRASHES with SqlServerRetryingExecutionStrategy

**Root Cause**: `CurrencyService.CreateAsync()`, `UpdateAsync()`, and `UpdateExchangeRateAsync()` all used `await using var transaction = await _uow.BeginTransactionAsync(ct)`. This is incompatible with EF Core's `SqlServerRetryingExecutionStrategy`, which throws `InvalidOperationException` when it detects a user-initiated transaction.

**Error in API Log**:
```
System.InvalidOperationException: The configured execution strategy
'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions.
```

This exception was caught by the generic `catch (Exception ex)` in CurrencyService and replaced with the generic message `"حدث خطأ أثناء إضافة العملة."`. The user saw a confusing error dialog with no actionable information.

**Fix**: Removed ALL manual `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` calls from all three methods. Each method now uses a single `SaveChangesAsync()` call, which EF Core automatically wraps in an implicit database transaction via `IDbContextTransaction`.

### 12.2 Problem: ExchangeRate on Payment Entities Missing Precision

**Root Cause**: `CustomerPayment.ExchangeRate` (decimal?) and `SupplierPayment.ExchangeRate` (decimal?) had no `.HasPrecision()` in their Fluent API configuration. EF Core defaults to `decimal(18,2)`, which silently truncates exchange rate values like `250.50` (fine) but would lose precision on rates with more decimal places (e.g., `0.000123`).

**Fix**: Added `.HasPrecision(18, 2)` to both properties in `SystemConfigurations.cs`.

### 12.3 Problem: JournalEntryId1 Shadow FK

**Root Cause**: `JournalEntryConfiguration.cs` used bare `.WithOne()` without specifying the navigation property on `JournalEntryLine`. EF Core created a shadow FK property `JournalEntryId1` because it couldn't resolve whether to use the existing `JournalEntryId` property or create a new one for the relationship. The existing `JournalEntryId` was also an explicit property but not fully mapped.

**Fix**: Changed `.WithOne()` to `.WithOne(x => x.JournalEntry)` — explicitly tells EF Core to use the `JournalEntry` navigation property on `JournalEntryLine`, which resolves to the existing `JournalEntryId` FK.

### 12.4 Problem: CurrencyEditorViewModel ValidateAsync() Bypasses RULE-229

**Root Cause**: The private `ValidateAsync()` method built its own `List<string>` and called `_dialogService.ShowValidationErrorsAsync(...)` directly. This completely bypassed the `INotifyDataErrorInfo` infrastructure (RULE-229 requirement to use `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()`).

**Fix**: Replaced with `ClearAllErrors()` → `AddError()` for each field → `return await ValidateAllAsync()` pattern matching `CashBoxEditorViewModel`.

### 12.5 Problem: LogSystemError Used for User Validation Failures

**Root Cause**: The `else` block in `SaveOperationAsync` called `LogSystemError(...)` (which logs at `Serilog.Log.Error` level) when the API returned business validation errors (e.g., duplicate currency name). Per RULE-182/183, user validation mistakes should be logged at `Warning` level, not `Error`.

**Fix**: Removed `LogSystemError` call from the `else` block. `HandleFailure()` already logs at Warning level — no additional logging needed.

### 12.6 Problem: Missing Success Toast

**Root Cause**: The editor ViewModel didn't inject `IToastNotificationService`. Users received no visual feedback when a currency was successfully created or updated — the window just closed.

**Fix**: Added `IToastNotificationService` via dual constructor pattern, calling `_toastService.ShowSuccess("تم إضافة العملة بنجاح" / "تم تعديل العملة بنجاح")` before `RequestClose()`.

### New Rules Added to AGENTS.md

| Rule | Directive |
|------|-----------|
| RULE-275 | NEVER use `BeginTransactionAsync` when `SqlServerRetryingExecutionStrategy` is configured |
| RULE-276 | For multi-write atomicity, use `CreateExecutionStrategy().ExecuteAsync()` not `BeginTransactionAsync` |
| RULE-277 | `ExchangeRate` on payment entities MUST have `.HasPrecision(18, 2)` |
| RULE-278 | `JournalEntry` → `JournalEntryLine` MUST use `.WithOne(x => x.JournalEntry)` |
| RULE-279 | Editor VMs MUST follow CashBoxEditorViewModel pattern (ClearAllErrors → AddError → ValidateAllAsync, toast, dual constructor) |
| RULE-280 | LogSystemError reserved for system errors — NEVER for API business validation errors |
