# Phase 19 — Settings Module: Comprehensive Settings Catalog & Implementation Plan

> **Version**: 2.1 — Full rewrite after 3 subagent reviews (Code Reviewer, Backend Architect, Database Engineer)
> **Scope**: Complete System Settings Catalog for V1 with 5-category structure + 15 review issues resolved

---

## Table of Contents
1. [Settings Architecture — 5 Categories](#1-settings-architecture--5-categories)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [System Settings Catalog](#4-system-settings-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Settings (Deferred)](#7-non-v1-settings-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (40+ Rules)](#9-compliance-matrix-40-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Settings Architecture — 5 Categories

Based on full codebase audit + user requirements from reference images, system settings for V1 are divided into **5 main categories**:

| # | Category | Storage | Impact |
|---|----------|---------|--------|
| 🏢 | **Company Settings** | `StoreSettings` (single row entity) | Company data on invoices and reports |
| ⚙️ | **System Settings** | `SystemSettings` (key-value pairs) | System logic — inventory, sales, barcode |
| 🖨️ | **Print Settings** | `SystemSettings` Category="Print" | Printed invoice format and content |
| 💰 | **Tax Settings** | `Taxes` (standalone entity + CRUD) | Multi-tax rate management |
| 🔐 | **Security** | `User` + `Role` + `Permission` | Access control for the system |

---

## 2. Full Inventory — What Already Exists

### 2.1 Company Settings ✅ (Fully exists)

**Entity**: `SalesSystem.Domain.Entities.StoreSettings`

| Field | Type | Default | Required | Status |
|-------|------|---------|----------|--------|
| `StoreName` | `string(150)` | `""` | ✅ | ✅ Exists |
| `Phone` | `string(20)?` | `null` | ❌ | ✅ Exists |
| `Address` | `string(250)?` | `null` | ❌ | ✅ Exists |
| `Email` | `string(100)?` | `null` | ❌ | ✅ Exists |
| `LogoPath` | `string(255)?` | `null` | ❌ | ✅ Exists |
| `SignaturePath` | `string(255)?` | `null` | ❌ | ⬜ **NEW — add** |
| `TaxNumber` | `string(50)?` | `null` | ❌ | ✅ Exists |
| `CurrencyCode` | `string(10)` | `"SAR"` | ✅ | ✅ Exists |

**⚠️ CRITICAL — Fields scheduled for deprecation:**

| Field | Issue | Action |
|-------|-------|--------|
| `DefaultTaxRate` | Overlaps with new `Tax` entity — two sources of truth | **DEPRECATED** — hide from Settings UI, keep in DB for backwards compat |
| `IsTaxEnabled` | Same overlap with Tax entity | **DEPRECATED** — hide from Settings UI |
| `InvoicePrefix` | Conflicts with RULE-254 (InvoiceNo is int with no prefix) | **DEPRECATED** — document for removal in next phase |

**Associated services** (all exist):
- `IStoreSettingsService` / `StoreSettingsService` — with `Result<T>` and `IUnitOfWork`
- `ISettingsApiService` / `SettingsApiService` — HTTP client + cache
- `SettingsViewModel` (524 lines) — displays Company Info in first card
- `SettingsView.xaml` (386 lines) — single page with 7 cards
- `SettingsController` — 6 endpoints with `[Authorize]`

### 2.2 System Settings — Key-Value ⚠️ (Partially exists)

**Entity**: `SalesSystem.Domain.Entities.SystemSetting`

| Field | Type | Default |
|-------|------|---------|
| `SettingKey` | `string(100)` | — (Unique Index) |
| `SettingValue` | `string(500)` | — |
| `DataType` | `string(50)` | `"string"` |
| `Category` | `string(100)` | — |
| `DisplayName` | `string(200)` | — (Required) |
| `Description` | `string(1000)?` | `null` |

**Existing keys (3 only)**:

| Key | Category | Default Value | Status |
|-----|----------|---------------|--------|
| `CostingMethod` | (empty) | `1` (WeightedAverage) | ✅ Exists |
| `Backup.RetentionDays` | (empty) | `30` | ✅ Exists |
| `Backup.ScheduleTime` | (empty) | `02:00` | ✅ Exists |
| Remaining 22 settings | — | — | **❌ NOT YET SEEDED** |

**Existing Repository**:
- `ISystemSettingsRepository` / `SystemSettingsRepository` — with `GetCostingMethodAsync()`, `SetCostingMethodAsync()`, `GetStringAsync()`, `SetStringAsync()`

**🔴 BLOCKER — Repository bypasses IUnitOfWork:**

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

This causes **two separate commits** when mixing StoreSettings and SystemSettings writes in one operation (e.g., `StoreSettingsService.UpdateSettingsAsync`). See [Section 3 — Blocker 1](#31-blocker-1-systemsettingsrepository-bypasses-iunitofwork).

### 2.3 Print Settings ✅ (Exists — Category="Print")

**Storage**: `SystemSettings` with `Category = "Print"`

**Existing keys (9)**:

| Key | Default | Status |
|-----|---------|--------|
| `ThermalPrinterName` | `""` | ✅ Exists |
| `A4PrinterName` | `""` | ✅ Exists |
| `LogoPath` | `""` | ✅ Exists |
| `StoreTaxNumber` | `""` | ✅ Exists |
| `TaxRate` | `0` | ✅ Exists |
| `AutoPrintOnPost` | `false` | ✅ Exists |
| `ReceiptHeader` | `""` | ✅ Exists |
| `ReceiptFooter` | `""` | ✅ Exists |
| `EscPosCodePage` | `22` | ✅ Exists |

**New keys to add (6)**:

| Key | Type | Default |
|-----|------|---------|
| `PaperSize` | `string` | `"A4"` |
| `PrintCopies` | `int` | `1` |
| `ShowLogo` | `bool` | `true` |
| `ShowBalanceOnPrint` | `bool` | `true` |
| `PrintSignature` | `bool` | `false` |
| `FooterNote` | `string` | `""` |

### 2.4 Tax Settings ❌ (Does NOT exist — needs full build)

- No `Tax` entity
- No `TaxConfiguration`
- No `TaxService` / `TaxController`
- No Tax screens in DesktopPWF
- **No `TaxId` FK on SalesInvoice or PurchaseInvoice** (see Blocker 2)

### 2.5 Security ✅ (Exists)

- **User entity** with `UserRole` (Admin=1, Manager=2, Cashier=3)
- **3 fixed roles** (not user-customizable in V1)
- **Policy-based authorization**: `AdminOnly`, `ManagerAndAbove`, `AllStaff`
- **User CRUD screen**: exists with soft-delete only (RULE-246)
- **Passwords**: BCrypt work factor 12 (RULE-039)
- **Rate limiting**: 5 attempts / 15 min per IP on login (RULE-240)

### 2.6 Notification Settings ⬜ (New — Phase 19 Addition)

**Storage**: `SystemSettings` with `Category = "Notifications"`

| Key | Default | Status |
|-----|---------|--------|
| `LowStockAlert` | `true` | ⬜ **NEW** |
| `ExpiryAlert` | `true` | ⬜ **NEW** |
| `ExpiryAlertDays` | `30` | ⬜ **NEW** |
| `CreditLimitAlert` | `true` | ⬜ **NEW** |

**Note**: Settings only — no alert engine in Phase 19. See Section 4.6 and Task 17.

---

## 3. BLOCKER Resolution — Critical Fixes

These 3 issues were identified by the Backend Architect as **blocking** — they must be resolved before any Phase 19 implementation begins.

### 3.1 Blocker 1: SystemSettingsRepository bypasses IUnitOfWork

**Problem**: `SystemSettingsRepository.SetStringAsync()` and `SetCostingMethodAsync()` call `_context.SaveChangesAsync()` internally, bypassing the Unit of Work pattern (RULE-024). This means when `StoreSettingsService.UpdateSettingsAsync()` calls both `_uow.SaveChangesAsync()` AND `_systemSettingsRepo.SetStringAsync()`, there are **separate commits** — transaction integrity is lost.

**Root cause**: The repository pattern should NOT own transaction commit. That's the service layer's responsibility via `IUnitOfWork`.

**Fix**:
1. `SystemSettingsRepository` — remove all `SaveChangesAsync()` calls from `SetStringAsync()`, `SetCostingMethodAsync()`
2. Add `SaveBatchAsync()` or have callers use `_uow.SaveChangesAsync()`
3. Add `ISystemSettingsService` in Application layer that manages the transactional boundary

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Also needed**: Add typed accessor extension methods to `ISystemSettingsRepository`:

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Files changed**: `SystemSettingsRepository.cs`, `ISystemSettingsRepository.cs`, `StoreSettingsService.cs`

### 3.2 Blocker 2: TaxId FK missing from SalesInvoice & PurchaseInvoice

**Problem**: The plan creates a `Tax` entity and CRUD screen, but neither `SalesInvoice` nor `PurchaseInvoice` have a `TaxId` FK. They store `TaxAmount` as a denormalized decimal only. Without the FK:
- Cannot trace which tax rate applied to a historical invoice
- If tax rates change, old invoices lose audit trail
- Tax table becomes a "display-only" reference with no operational impact

**Root cause**: Invoices were designed before the Tax entity existed. Tax was just a computed decimal.

**Fix**: Add `int? TaxId` FK to both invoice entities.

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the table definition.

**Files changed**: `SalesInvoice.cs`, `PurchaseInvoice.cs`, `SalesInvoiceConfiguration.cs`, `PurchaseInvoiceConfiguration.cs`, `SalesInvoiceDto.cs`, `PurchaseInvoiceDto.cs`, invoice services, migration

### 3.3 Blocker 3: StoreSettings.DefaultTaxRate/IsTaxEnabled overlap with Tax entity

**Problem**: `StoreSettings` has `DefaultTaxRate` (decimal) and `IsTaxEnabled` (bool). The new Tax entity has `IsDefault` (bool), `Rate` (decimal). This creates **two sources of truth**:
- If `StoreSettings.IsTaxEnabled = false` but Tax entity exists with records — which wins?
- If `StoreSettings.DefaultTaxRate = 15` but Tax.IsDefault is "VAT 5%" — which is used?

**Decision**: **Deprecate `StoreSettings.DefaultTaxRate` and `IsTaxEnabled`** in favor of the Tax entity. The `Tax.IsDefault` record will be the single source of truth.

**Migration path** (backwards-compatible):
1. Keep columns in `StoreSettings` entity (don't remove — breaking change)
2. Hide them from `SettingsViewModel` and `SettingsView.xaml` (front-end deprecation)
3. When computing invoice tax, always read from `Tax` table: `taxes.FirstOrDefault(t => t.IsDefault)`
4. In the new Tax CRUD screen, the first tax created with `IsDefault = true` wins
5. Document: Remove columns in Phase 20 after data migration

**Files changed**: `SettingsViewModel.cs`, `SettingsView.xaml`, `StoreSettingsService.cs`, invoice services

---

## 4. System Settings Catalog

### 4.1 Company Settings (`StoreSettings` — single row)

| # | Key | Field | Type | Default | Required | V1 |
|---|-----|-------|------|---------|----------|----|
| 1 | — | `StoreName` | `nvarchar(150)` | `"My Store"` | ✅ | ✅ |
| 2 | — | `Phone` | `nvarchar(20)` | `null` | ❌ | ✅ |
| 3 | — | `Address` | `nvarchar(250)` | `null` | ❌ | ✅ |
| 4 | — | `Email` | `nvarchar(100)` | `null` | ❌ | ✅ |
| 5 | — | `LogoPath` | `nvarchar(255)` | `null` | ❌ | ✅ |
| 6 | **NEW** | `SignaturePath` | `nvarchar(255)` | `null` | ❌ | ✅ |
| 7 | — | `TaxNumber` | `nvarchar(50)` | `null` | ❌ | ✅ |
| 8 | — | `CurrencyCode` | `nvarchar(10)` | `"SAR"` | ✅ | ✅ |
| — | DEPRECATED | `DefaultTaxRate` | `decimal(18,2)` | `0` | — | → Tax entity |
| — | DEPRECATED | `IsTaxEnabled` | `bit` | `false` | — | → Tax entity |
| — | DEPRECATED | `InvoicePrefix` | `nvarchar(20)` | `"INV"` | — | → RULE-254 |

### 4.2 System Settings (`SystemSettings` — Key-Value, Category column)

All stored in `SystemSettings` table with following keys:

#### Inventory (Category = "Inventory")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 1 | `CostingMethod` | `int` (1-3) | `1` (WeightedAverage) | Inventory costing method |
| 2 | `AllowNegativeStock` | `bool` | `false` | Allow negative inventory |
| 3 | `EnableFefo` | `bool` | `false` | Use FEFO when expiry dates exist |
| 4 | `StockAlertDays` | `int` | `5` | Low stock warning (days) |

#### Sales (Category = "Sales")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 5 | `AutoPostInvoices` | `bool` | `true` | Auto-post invoice on save |
| 6 | `AllowDrafts` | `bool` | `true` | Allow saving drafts |
| 7 | `ShowProfitInInvoice` | `bool` | `true` | Show profit in sales screen |
| 8 | `PreventBelowRetailPrice` | `bool` | `false` | Prevent sale below official price |
| 9 | `AllowBelowCostSale` | `bool` | `false` | Allow sale below cost |
| 10 | `HideTaxInSales` | `bool` | `false` | Hide tax columns in sales screens |
| 11 | `ShowExpiryInInvoices` | `bool` | `false` | Show expiry date column in invoices |
| 12 | `DefaultCashCustomerId` | `int` | `1` | Default cash customer |

#### Purchases (Category = "Purchases")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 13 | `PurchaseAutoPost` | `bool` | `true` | Auto-post purchase invoice |
| 14 | `HideTaxInPurchases` | `bool` | `false` | Hide tax columns in purchase screens |
| 15 | `DefaultCashSupplierId` | `int` | `1` | Default cash supplier |

#### Barcode (Category = "Barcode")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 16 | `EnableBarcode` | `bool` | `true` | Enable barcode system-wide |
| 17 | `BarcodeInputType` | `string` | `"Scanner"` | Input type: Scanner / Camera |
| 18 | `AutoGenerateBarcode` | `bool` | `true` | Auto-generate barcode for new products |

#### Accounting (Category = "Accounting")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 19 | `AutoCreateJournalEntry` | `bool` | `true` | Auto-create journal entry on invoice post |

#### General (Category = "General")

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 20 | `DecimalPlaces` | `int` | `2` | Number of decimal places for prices |
| 21 | `Language` | `string` | `"ar"` | System language |
| 22 | `DateFormat` | `string` | `"dd/MM/yyyy"` | Date format |

### 4.3 Print Settings (`SystemSettings` — Category = "Print")

| # | Key | Type | Default | V1 |
|---|-----|------|---------|----|
| 1 | `ThermalPrinterName` | `string(100)` | `"EPSON"` | ✅ |
| 2 | `A4PrinterName` | `string(100)` | `""` | ✅ |
| 3 | `LogoPath` | `string(255)` | `""` | ✅ |
| 4 | `StoreTaxNumber` | `string(50)` | `""` | ✅ |
| 5 | `TaxRate` | `decimal` | `0` | ✅ |
| 6 | `AutoPrintOnPost` | `bool` | `false` | ✅ |
| 7 | `ReceiptHeader` | `string(500)` | `""` | ✅ |
| 8 | `ReceiptFooter` | `string(500)` | `""` | ✅ |
| 9 | `EscPosCodePage` | `int` | `22` | ✅ |
| 10 | **NEW** `PaperSize` | `string(20)` | `"A4"` | ✅ |
| 11 | **NEW** `PrintCopies` | `int` | `1` | ✅ |
| 12 | **NEW** `ShowLogo` | `bool` | `true` | ✅ |
| 13 | **NEW** `ShowBalanceOnPrint` | `bool` | `true` | ✅ |
| 14 | **NEW** `PrintSignature` | `bool` | `false` | ✅ |
| 15 | **NEW** `FooterNote` | `string(500)` | `""` | ✅ |

### 4.4 Tax Settings (Standalone Entity — New)

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `Name` | `nvarchar(100)` | — | ✅ | **UNIQUE Index** |
| 3 | `Rate` | `decimal(18,2)` | `0` | ✅ | **CHECK (Rate >= 0 AND Rate <= 100)** |
| 4 | `IsDefault` | `bit` | `false` | ❌ | **Filtered Unique Index WHERE IsDefault = 1** |
| 5 | `IsActive` | `bit` | `true` | ❌ | Global query filter |

**Seed data**:

| Name | Rate | IsDefault |
|------|------|-----------|
| No Tax | 0% | ✅ |
| VAT 5% | 5% | ❌ |
| VAT 15% | 15% | ❌ |

### 4.5 Security (Already Exists — No Changes)

| Component | Status |
|-----------|--------|
| Users CRUD | ✅ Exists — Soft Delete only |
| Roles (3 fixed) | ✅ Admin, Manager, Cashier (not customizable in V1) |
| Permissions | ✅ Policy-based: `AdminOnly`, `ManagerAndAbove`, `AllStaff` |
| JWT Auth | ✅ With BCrypt + Rate Limiting |
| User hard-delete | ❌ Guarded — `PermanentDeleteAsync` returns `Result.Failure` (RULE-244) |

### 4.6 Notification Settings (New — SystemSettings Category = "Notifications")

Stored in `SystemSettings` table with `Category = "Notifications"`. These control system-wide alert behavior.

| # | Key | Type | Default | Description |
|---|-----|------|---------|-------------|
| 1 | `LowStockAlert` | `bool` | `true` | Warn when stock falls below minimum |
| 2 | `ExpiryAlert` | `bool` | `true` | Warn when items near expiry |
| 3 | `ExpiryAlertDays` | `int` | `30` | Days before expiry to trigger alert |
| 4 | `CreditLimitAlert` | `bool` | `true` | Warn when credit limit is exceeded |

**Arabic descriptions for seed**:
- `LowStockAlert` → `"تنبيه عند انخفاض المخزون عن الحد الأدنى"`
- `ExpiryAlert` → `"تنبيه عند قرب انتهاء صلاحية الأصناف"`
- `ExpiryAlertDays` → `"عدد الأيام للتنبيه قبل انتهاء الصلاحية"`
- `CreditLimitAlert` → `"تنبيه عند تجاوز السقف الائتماني"`

**Impact**: These feed into `DashboardViewModel` alert counters and `ScheduledBackupWorker`-style background services.

**Note**: Full notification engine (background workers, push alerts) is deferred to Phase 22. Phase 19 seeds the settings and wires them into existing Dashboard alerts.
---

## 5. Gap Analysis

### 5.1 Company Settings

| Setting | Status | Action |
|---------|--------|--------|
| `SignaturePath` | ❌ Missing | Add to entity + config + migration + DTO + SettingsVM |
| `DefaultTaxRate` | ❌ Conflicting | **DEPRECATE** — hide from UI, Tax entity is source of truth |
| `IsTaxEnabled` | ❌ Conflicting | **DEPRECATE** — hide from UI |
| `InvoicePrefix` | ❌ Legacy | **DEPRECATE** — RULE-254: InvoiceNo is int with no prefix |
| All other fields | ✅ Exist | Nothing needed |

### 5.2 System Settings

| Setting | Status | Action |
|---------|--------|--------|
| `CostingMethod` | ✅ Exists, default=1 | Needs data seed only |
| 18 other settings | ❌ Missing | Add data seed in DbSeeder |
| `HideTaxInSales` | ❌ Missing | Add to catalog + seed + SettingsViewModel |
| `HideTaxInPurchases` | ❌ Missing | Add to catalog + seed + SettingsViewModel |
| `ShowExpiryInInvoices` | ❌ Missing | Add to catalog + seed + SettingsViewModel |
| `AutoCreateJournalEntry` | ✅ In Accounting plan | Needs data seed |

### 5.3 Print Settings

| Setting | Status | Action |
|---------|--------|--------|
| 9 existing keys | ✅ Exist | Nothing |
| `PaperSize`, `PrintCopies`, `ShowLogo`, `ShowBalanceOnPrint`, `PrintSignature`, `FooterNote` | ❌ Missing | Add data seed + DTO + PrintDataService mapping |

### 5.4 Tax Settings

| Component | Status | Action |
|-----------|--------|--------|
| Tax Entity + Config | ❌ Missing | Create from scratch |
| Tax Service + Controller | ❌ Missing | Create with Result<T> + IUnitOfWork |
| Tax Desktop screens | ❌ Missing | List + Editor ViewModels/Views |
| **TaxId FK on Invoices** | ❌ Missing | **BLOCKER 2** — add FK to SalesInvoice + PurchaseInvoice |
| Tax seed data | ❌ Missing | Add to DbSeeder (3 tax records) |

### 5.5 Notification Settings

| Setting | Status | Action |
|---------|--------|--------|
| `LowStockAlert` | ❌ Missing | Add Section 4.6 + seed + SettingsViewModel |
| `ExpiryAlert` | ❌ Missing | Same |
| `ExpiryAlertDays` | ❌ Missing | Same |
| `CreditLimitAlert` | ❌ Missing | Same |

### 5.6 Backup Module Cross-Reference

| Item | Status | Action |
|------|--------|--------|
| Backup module (Phase 4.4) | ✅ Already exists | Add cross-reference in Task 18 |
| ScheduledBackupWorker | ✅ Exists | No changes needed |
| Backup retention settings | ✅ Seeded | Already handled in Task 5 |

### 5.7 SystemSettingsRepository IUnitOfWork violation

| Issue | Status | Action |
|-------|--------|--------|
| Repo calls SaveChangesAsync directly | ❌ VIOLATION | **BLOCKER 1** — remove SaveChanges from repo, use UoW in service |

---

## 6. Architectural Decisions

### 6.1 Costing Method: WeightedAverage (NOT FIFO)

The user's analysis file (`docs/a.md`) mentions "FIFO" as a previous decision. However:

- **AGENTS.md RULE-068**: Costing method seeded as `WeightedAverage (1)`
- **AGENTS.md RULE-069**: Three methods: WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3
- **Current codebase**: `UpdateProductPricingService` already implements all 3 methods
- **FIFO would require**: ~400+ lines of new Domain (PurchaseLot entity) + Service + Migration code

**Decision**: **Keep WeightedAverage for V1**. It is the retail standard, already implemented, and IFRS-compliant. If FIFO is truly required (e.g., pharmacy with strict expiry tracking), it can be added as `CostingMethod.FIFO = 4` in a future phase.

### 6.2 TaxId FK — Add NOW, Not Later

Although the original plan deferred `TaxId` FK on invoices to "a future phase", the Backend Architect flagged this as a **BLOCKER**. Adding it later would require:
- A separate migration to add nullable FK columns
- Backfilling historical invoice data
- Two changes to invoice services (one now, one later)

**Decision**: **Add `int? TaxId` NOW** as part of Phase 19. The FK is nullable (existing invoices have no tax reference), uses `DeleteBehavior.Restrict`, and requires no data migration.

### 6.3 StoreSettings.DefaultTaxRate/IsTaxEnabled — Deprecate

These two fields on `StoreSettings` directly overlap with the new `Tax` entity.

**Decision**: Deprecate (not delete) in V1:
- Keep columns in database (don't remove — backwards compat)
- Hide from SettingsViewModel UI
- Invoice tax computation reads from `Tax` table: `firstOrDefault(t => t.IsDefault)`
- Remove columns in Phase 20

### 6.4 Why NOT a Tab-Based Settings UI

The existing single-page `SettingsView` (386 lines) with 7 section cards is working and comprehensive. Tab ViewModels exist separately (`CostingMethodSettingsViewModel`, `BackupViewModel`).

**Decision**: Keep single-page design. No tab refactor.

### 6.5 Why StoreSettings for Some Settings Instead of Key-Value

Settings like `AllowNegativeStock`, `EnableStockAlerts`, `AutoUpdatePrices` live as columns in `StoreSettings` rather than key-value in `SystemSettings`. This is intentional because:
- They are store-level policies tied to the single store identity
- Strong typing at the DB level (bool columns, not parsed strings)
- Simplifies the Settings UI (fewer key-value entries)

---

## 7. Non-V1 Settings (Deferred)

These settings appeared in reference images but are **deferred** to future versions:

| Setting | Reason |
|---------|--------|
| Digit Grouping (comma separators) | Report formatting detail |
| Hide Document Column in Reports | Report display detail |
| Total Operations Below Account | Advanced report feature |
| Print Each Invoice on Separate Page | Print detail — low priority |
| Update Sale Price When Entering Invoice | **REJECTED** — official price changes only from Price Screen |
| System-wide shared password | **REJECTED** — system uses Users + Roles |
| Show Time in Account Screen | User Preference — future phase |
| Hide Barcode When Printing | User Preference — future phase |
| User Preferences (general category) | Dedicated User Preferences screen in future version |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 0 — WeightedAverage vs FIFO Decision Confirmation

**Before any code is written**, the user must confirm: **WeightedAverage (current AGENTS.md default) or FIFO?**

If FIFO: requires Constitution amendment (RULE-068/069/071) + UpdateProductPricingService rewrite + CostingMethod enum update + migration.

**Default**: Proceed with WeightedAverage unless explicitly told otherwise.

---

### Task 1 — BLOCKER 1: Fix SystemSettingsRepository IUnitOfWork Violation

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Repositories/SystemSettingsRepository.cs` | Remove all `SaveChangesAsync()` calls from `SetStringAsync()` and `SetCostingMethodAsync()` |
| `Application/Interfaces/Repositories/ISystemSettingsRepository.cs` | Add typed accessors: `GetBoolAsync()`, `GetIntAsync()`, `GetDecimalAsync()` |
| `Application/Services/StoreSettingsService.cs` | Wrap mixed StoreSettings + SystemSettings writes in `BeginTransactionAsync()` |
| `Infrastructure/Repositories/SystemSettingsRepository.cs` | Implement typed accessors with safe parsing + fallback to default |

**Logging** (RULE-035/036):
- `Log.Information("SystemSetting {Key} updated to {Value}", key, value)` on every write
- `Log.Warning("SystemSetting {Key} parse failed, using default {Default}", key, defaultValue)` on parse failure (RULE-183)

**Validation** (RULE-044):
- `UpdateSettingsRequestValidator` — ensure CostingMethod is 1-3, DecimalPlaces is 0-6, StockAlertDays is 1-365

**Estimate**: ~1 hour

---

### Task 2 — BLOCKER 2: Add TaxId FK to SalesInvoice & PurchaseInvoice

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoice.cs` | Add `int? TaxId` + `Tax? Tax` nav property + `SetTax(Tax tax, decimal taxAmount)` method |
| `Domain/Entities/PurchaseInvoice.cs` | Same |
| `Infrastructure/Data/Configurations/SalesInvoiceConfiguration.cs` | Add FK config: `HasForeignKey(t => t.TaxId).OnDelete(DeleteBehavior.Restrict)` |
| `Infrastructure/Data/Configurations/PurchaseInvoiceConfiguration.cs` | Same |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE + FK |
| `Contracts/DTOs/AllDtos.cs` | Add `TaxId`, `TaxName`, `TaxRate` to `SalesInvoiceDto` and `PurchaseInvoiceDto` |
| Application invoice services | Update DTO mapping to include Tax info |
| Desktop invoice ViewModels | Show Tax name in invoice header |

**Domain method** (RULE-042):

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the table definition.

**Logging**: `Log.Information("Tax {TaxId} applied to Invoice {InvoiceId}, Amount: {Amount}", taxId, id, taxAmount)`

**Estimate**: ~2 hours

---

### Task 3 — BLOCKER 3: Deprecate DefaultTaxRate, IsTaxEnabled, InvoicePrefix

**Files**:

| File | Change |
|------|--------|
| `ViewModels/SettingsViewModel.cs` | Remove/hide `DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix` from save logic |
| `Views/Settings/SettingsView.xaml` | Hide these fields from the Company Info card |
| `Application/Services/StoreSettingsService.cs` | Map `DefaultTaxRate` from Tax.IsDefault when writing SettingsDto |
| Invoice services (Sales + Purchase) | Read tax rate from `Tax` table, NOT from `StoreSettings.DefaultTaxRate` |

**Migration path** (don't delete columns):
- Keep `DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix` in the entity and DB
- Just stop using them in UI and service logic
- Document: Remove columns in Phase 20 migration

**Estimate**: ~30 minutes

---

### Task 4 — Add SignaturePath to StoreSettings

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/StoreSettings.cs` | Add `public string? SignaturePath { get; private set; }` |
| `Domain/Entities/StoreSettings.cs` | Update `Create()` and `Update()` to accept optional `signaturePath` |
| `Infrastructure/Data/Configurations/SystemConfigurations.cs` | Add `.Property(ss => ss.SignaturePath).HasMaxLength(255)` |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE StoreSettings ADD SignaturePath nvarchar(255) NULL` |
| `Contracts/DTOs/AllDtos.cs` — `StoreSettingsDto` | Add `string? SignaturePath` |
| `Contracts/Requests/MiscRequests.cs` — `UpdateSettingsRequest` | Add `string? SignatureUrl` |
| `Application/Services/StoreSettingsService.cs` | Update mapping |
| `ViewModels/SettingsViewModel.cs` | Add `SignaturePath` property + Browse command |
| `Views/Settings/SettingsView.xaml` | Add file picker field in Company Info card + Arabic ToolTip (RULE-185) |
| `Infrastructure/Printing/A4InvoiceDocument.cs` | Render signature image if `SignaturePath` is not null |
| `Infrastructure/Printing/PrintDataService.cs` | Map `SignaturePath` from StoreSettings for print DTOs |

**UI Compact** (RULE-262-274):
- File picker button: `Height="28"` (via style), `Padding="10,4"`
- Field margin: `Margin="0,0,0,6"`

**ToolTips** (RULE-185-190):
- Browse button: `"اختيار ملف التوقيع لطباعته على الفواتير"`
- Clear button: `"إزالة التوقيع المحدد"`

**Estimate**: ~30 minutes

---

### Task 5 — Seed SystemSettings (22 Key-Value Pairs)

**File**: `Infrastructure/Data/DbSeeder.cs`

Add AFTER the accounting seed block, with **independent `AnyAsync()` guard** (NOT relying on Users guard):

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.


**Logging** (RULE-035):
- `Log.Information("Seeded {Count} SystemSettings key-value pairs.", settings.Count)` on success
- `Log.Warning("SystemSettings already seeded — skipping.")` if guard prevents

**Estimate**: ~15 minutes

---

### Task 6 — Seed Master Data Entities (Warehouse, CashBox, Units, DocumentTypes, Default Customer/Supplier, Category)

**File**: `Infrastructure/Data/DbSeeder.cs`

Add AFTER the SystemSettings seed block (Task 5), with **independent `AnyAsync()` guards** for each entity type:

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.

**IMPORTANT — Seed Order**: These seeds must run BEFORE `SystemSettings` seed if `DefaultCashCustomerId` and `DefaultCashSupplierId` reference them. The Customer/Supplier seeds set Id=1 (first record). The SystemSettings seed references `DefaultCashCustomerId = "1"` and `DefaultCashSupplierId = "1"`.

**Entity assumptions**:
- `Warehouse.Create(name)` — single-param factory, already exists in codebase
- `CashBox.Create(boxName)` — single-param factory, already exists
- `Category.Create(name)` — single-param factory, already exists
- `Unit.Create(name)` — single-param factory, already exists
- `DocumentType` entity — assumed to exist with `Create(name)` factory. If not yet created, add in this phase.
- `Customer.Create(name, phone, address, creditLimit, createdByUserId)` — existing factory
- `Supplier.Create(name, phone, address, creditLimit, createdByUserId)` — existing factory

**Guard clauses**: Each entity has its own `AnyAsync()` check — independent seeding. If one entity was seeded previously, only the missing ones are created.

**Logging** (RULE-035):
- `Log.Information("Seeded default warehouse: {Name}", warehouse.Name)` on success
- `Log.Warning("{EntityName} already seeded — skipping.")` if guard prevents (RULE-183)

**Estimate**: ~20 minutes

---

### Task 7 — CostingMethod RadioButton in SettingsView

**Files**:

| File | Change |
|------|--------|
| `Views/Settings/SettingsView.xaml` | Add RadioButton group for `CostingMethod` in the System Settings section with 3 options: WeightedAverage, LastPurchasePrice, SupplierPrice |
| `ViewModels/SettingsViewModel.cs` | Add `CostingMethod` enum property + `IsWeightedAverageSelected`, `IsLastPriceSelected`, `IsSupplierPriceSelected` bool properties |
| `Contracts/DTOs/AllDtos.cs` — `StoreSettingsDto` | Add `CostingMethod` field (int 1-3) |
| `Contracts/Requests/MiscRequests.cs` — `UpdateSettingsRequest` | Add `CostingMethod` field |

**XAML Pattern**:

> See `docs/ui-screens.md` for WPF UI patterns and `docs/AGENTS.md` for ViewModel patterns.

**ToolTips** (RULE-185-190):
- WeightedAverage: `"المتوسط المرجح — طريقة تقييم المخزون الافتراضية"`
- LastPurchasePrice: `"استخدام آخر سعر شراء كتكلفة للمخزون"`
- SupplierPrice: `"استخدام سعر المورد المدرج في كرت الصنف"`

**UI Compact** (RULE-262-274):
- RadioButton margins: `Margin="0,4,0,2"` for first, `Margin="0,2,0,6"` for last
- Group uses compact spacing between options

**Estimate**: ~30 minutes

---

### Task 8 — StoreSettingsChangedMessage EventBus Message

**File**: `DesktopPWF/Messaging/Messages/AppMessages.cs`

> See `docs/AGENTS.md` for DTO patterns and `SalesSystem.Contracts/` for canonical DTO definitions.

This is a forward-looking addition. Currently no code publishes it, but it completes the convention of 18 existing EventBus message types. Future subscribers (Dashboard, MainWindow) can react to settings changes.

**Estimate**: ~5 minutes

---

### Task 9 (was Task 7) — Tax Module (Full Build — New)

#### 9.1 Domain Layer

**File**: `Domain/Entities/Tax.cs`

> See `docs/database-schema.md` Module 2.5 (Taxes table) for the canonical Tax definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns (private set, Guard Clauses, domain methods).

**DeleteStrategy** (RULE-050): Three options when deleting:
- **Cancel** (`DeleteStrategy.Cancel`) — abort, do nothing
- **Deactivate** (`DeleteStrategy.Deactivate`) — `MarkAsDeleted()` → `IsActive = false`
- **Permanent** (`DeleteStrategy.Permanent`) — physical removal; **must catch `DbUpdateException`** if Tax is referenced by invoices (RULE-200)

> See `docs/database-schema.md` Module 2.5 for the TaxConfiguration (Fluent API) and canonical SQL CREATE TABLE definition.

#### 9.3 Seed Data

**File**: `Infrastructure/Data/DbSeeder.cs`

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.

#### 9.4 Application Layer

**File**: `Application/Interfaces/Services/ITaxService.cs`

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**File**: `Application/Services/TaxService.cs`

- Uses `IUnitOfWork` (RULE-024)
- Returns `Result<T>` (RULE-006)
- `DeletePermanentlyAsync` catches `DbUpdateException` → returns `Result.Failure("لا يمكن حذف هذه الضريبة لأنها مرتبطة بفواتير")` (RULE-200)
- `CreateAsync`: if `IsDefault = true`, unset all other defaults first
- **Logging**: `Log.Information("Tax {Id} created: {Name} @ {Rate}%", ...)` on every CRUD (RULE-035)

#### 9.5 Contracts Layer

**File**: `Contracts/DTOs/AllDtos.cs`

> See `docs/AGENTS.md` for DTO patterns and `SalesSystem.Contracts/` for canonical DTO definitions.

**File**: `Contracts/Requests/TaxRequests.cs`

> See `docs/AGENTS.md` for DTO patterns and `SalesSystem.Contracts/` for canonical DTO definitions.

#### 9.6 API Layer

**File**: `Api/Controllers/TaxesController.cs`

| Method | Endpoint | Policy |
|--------|----------|--------|
| GET | `/api/v1/taxes` | `AllStaff` |
| GET | `/api/v1/taxes/{id}` | `AllStaff` |
| POST | `/api/v1/taxes` | `ManagerAndAbove` |
| PUT | `/api/v1/taxes/{id}` | `ManagerAndAbove` |
| DELETE | `/api/v1/taxes/{id}` | `ManagerAndAbove` (soft) |
| DELETE | `/api/v1/taxes/permanent/{id}` | `AdminOnly` (permanent) |

**Controller purity** (RULE-203): Controller injects `ITaxService` only — NO `DbContext` or `IUnitOfWork` injection.

**FluentValidation** (RULE-044):
- `CreateTaxRequestValidator`: Name required (max 100), Rate between 0 and 100
- `UpdateTaxRequestValidator`: Same

#### 9.7 Desktop Layer

**Files** (8 files):

| File | Content |
|------|---------|
| `Services/Api/IApiService.cs` — `ITaxesApiService` | 5 methods: GetAll, GetById, Create, Update, Delete |
| `Services/Api/TaxesApiService.cs` | HTTP client (with content-type guard RULE-184) |
| `ViewModels/Taxes/TaxesListViewModel.cs` | List with newest-first sort (RULE-220), DeleteStrategy dialog (RULE-050) |
| `Views/Taxes/TaxesListView.xaml` | DataGrid with ToolTips on all buttons (RULE-185-190), compact styles (RULE-262-274) |
| `Views/Taxes/TaxesListView.xaml.cs` | Code-behind |
| `ViewModels/Taxes/TaxEditorViewModel.cs` | INotifyDataErrorInfo (RULE-228), SetDialogService() (RULE-227), ValidateAllAsync() (RULE-229) |
| `Views/Taxes/TaxEditorView.xaml` | Editor form — Save always enabled, validate on click (RULE-059), compact styles |
| `Views/Taxes/TaxEditorView.xaml.cs` | Code-behind |
| `Messaging/Messages/AppMessages.cs` | Add `TaxChangedMessage` |
| `App.xaml.cs` | DI registrations + navigation |

**ViewModel patterns** (RULE-141):
- All async commands wrapped in `ExecuteAsync()`
- Error messages via `LogSystemError()` (RULE-199) — NEVER `ex.Message` in user dialogs (RULE-171)
- Dialog titles are screen-specific: `"خطأ في حفظ الضريبة"` (RULE-173)
- All user messages via `IDialogService` — NO `MessageBox.Show` (RULE-174)
- Async suffix on all dialog calls: `ShowErrorAsync` (RULE-175)

**UI Compact** (RULE-262-274):
- Button/TextBox heights: via style (28px default) — no hardcoded `Height="36"`
- Padding: `10,4` via style — no hardcoded `Padding="16,0"`
- Header: `Padding="12,6"`, Footer: `Padding="12,8"`
- Section margins: `Margin="0,0,0,6"` between fields
- Dialog title font: `FontSize="16"`, section headers: `FontSize="14"`
- Empty-state buttons: `Margin="0,12,0,0"` Width="140"
- Dialog icons: `Width="44" Height="44"` max

**Arabic ToolTips** (RULE-185-190):
- Add button: `"إضافة ضريبة جديدة"`
- Edit button: `"تعديل بيانات الضريبة"`
- Delete button: `"حذف الضريبة — سيتم إلغاء تنشيطها"`
- Save button: `"حفظ بيانات الضريبة"`
- Cancel button: `"إلغاء التعديل والعودة"`
- Error dismiss: `"إخفاء رسالة الخطأ"`
- Empty-state button: `"➕ إضافة أول ضريبة — أضف نسبة ضريبة جديدة"`

**Estimate**: ~4 hours

---

### Task 10 — System Settings UI Screen (New — Standalone Page)

**Files**:

| File | Content |
|------|---------|
| `ViewModels/Settings/SystemSettingsViewModel.cs` | ViewModel with ~22 properties + Save (grouped by category) |
| `Views/Settings/SystemSettingsView.xaml` | Form with 5 sections (Inventory, Sales, Purchases, Barcode, General) |
| `Views/Settings/SystemSettingsView.xaml.cs` | Code-behind |
| `Services/Api/IApiService.cs` | Extend `ISettingsApiService` with `GetAllSystemSettingsAsync()` + `SetSystemSettingAsync(key, value)` |
| `Services/Api/SettingsApiService.cs` | Implement HTTP methods |
| `App.xaml.cs` | DI registration + navigation from MainWindow |

**Logging**: `Log.Information("SystemSetting {Key} set to {Value}")` (RULE-035)
**ToolTips**: All buttons and inputs have Arabic ToolTips (RULE-185-190)
**UI Compact**: Follow same compact style rules as Task 7.7 (RULE-262-274)

**Opened via**: `ScreenWindowService.OpenScreen()` (RULE-160) — non-modal, not ShowDialog

**Estimate**: ~3 hours

---

### Task 11 — Add 4 New Print Settings to DTOs + Services

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` — `PrintSettingsDto` | Add `string PaperSize`, `int PrintCopies`, `bool ShowLogo`, `bool ShowBalanceOnPrint`, `bool PrintSignature`, `string FooterNote` |
| `Contracts/Requests/MiscRequests.cs` — `UpdatePrintSettingsRequest` | Add same 6 fields |
| `Infrastructure/Printing/PrintDataService.cs` | Map all 6 new keys in `GetPrintSettingsAsync()` + `UpdatePrintSettingsAsync()` |
| `ViewModels/SettingsViewModel.cs` | Add 6 properties with binding |
| `Views/Settings/SettingsView.xaml` | Add 6 input fields in Print Settings card (compact style) |
| `Infrastructure/Printing/A4InvoiceDocument.cs` | Use `ShowLogo`, `ShowBalanceOnPrint`, `PrintSignature`, and `FooterNote` |
| `Infrastructure/Printing/ThermalReceiptGenerator.cs` | Use `PaperSize`, `PrintCopies`, `ShowLogo`, and `FooterNote` |

**Estimate**: ~1 hour

---

### Task 12 — Add IMemoryCache for SystemSettings (Performance)

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Repositories/SystemSettingsRepository.cs` | Inject `IMemoryCache`, implement 5-min sliding expiration |
| `Application/Interfaces/Repositories/ISystemSettingsRepository.cs` | Add `InvalidateCache()` method |
| `Infrastructure/ServiceRegistration.cs` or `Program.cs` | Register `IMemoryCache` |

**Pattern**:
- Read: `cache.GetOrCreateAsync($"sys:{key}", async entry => { entry.SlidingExpiration = TimeSpan.FromMinutes(5); return await queryDb; })`
- Invalidate on write: `cache.Remove($"sys:{key}")`
- Bulk invalidate on settings screen save: `cache.RemoveByPrefix("sys:")`

**Estimate**: ~30 minutes

---

### Task 13 — Expanded Compliance Matrix

**Expand Section 9** to cover all 40+ rules (see [Section 9](#9-compliance-matrix-40-rules) — already expanded below).

**Estimate**: ~15 minutes (documentation only)

---

## 9. Compliance Matrix (40+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | Tax.Rate = `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | StoreSettingsService mixed writes — FIXED via BeginTransactionAsync (Task 1) | ✅ |
| **RULE-006** | ALL services return `Result<T>` | TaxService, StoreSettingsService, SystemSettingsService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All settings tables | ✅ |
| **RULE-016** | BaseEntity audit fields | Tax inherits BaseEntity (CreatedAt, CreatedByUserId, UpdatedAt, IsActive) | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | TaxService, StoreSettingsService — SystemSettingsRepository FIXED (Task 1) | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on CRUD + settings changes | ✅ |
| **RULE-036** | Log critical operations | Invoice tax assignment, settings changes, seed data | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | TaxesController, SettingsController | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Tax entity: `Create()`, `Update()`, `MarkAsDeleted()` | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateTaxRequestValidator, UpdateTaxRequestValidator, UpdateSettingsRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Tax: Cancel/Deactivate/Permanent + ShowDeleteConfirmationAsync dialog | ✅ |
| **RULE-052** | Guard Clauses on all entities | Tax.Create/Update — Arabic DomainException | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "اسم الضريبة مطلوب", "نسبة الضريبة لا يمكن أن تتجاوز 100%" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | TaxEditorViewModel (RULE-228) | ✅ |
| **RULE-059** | Save always enabled, validate on click | TaxEditorViewModel — no CanExecute blocking | ✅ |
| **RULE-068** | CostingMethod default = WeightedAverage (1) | Seed: `CostingMethod` = `"1"` | ✅ |
| **RULE-069** | 3 methods: WA=1, LPP=2, SP=3 | Enum + Service alignment | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModels in Tasks 7-8 | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | SystemSettings screen opens via `OpenScreen()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ الضريبة"`, `"خطأ في تحميل الإعدادات"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, JSON parse crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, "not found", parse fallbacks | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | SettingsApiService, TaxesApiService — content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, MenuItems, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إضافة ضريبة جديدة" ✅, not "ضريبة" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة الضرائب — إضافة وتعديل نسب الضريبة" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول ضريبة — أضف نسبة ضريبة جديدة" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | TaxService.DeletePermanentlyAsync catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | TaxService, StoreSettingsService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | TaxesController, SettingsController — service only | ✅ |
| **RULE-207** | CostingMethod: 1=WA, 2=LPP, 3=SP | Seed + Service + Enum aligned | ✅ |
| **RULE-210** | CHECK constraints at DB level | `CHK_Taxes_Rate_Range` (Rate >= 0 AND <= 100) | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | TaxId on invoices: Restrict (Task 2) | ✅ |
| **RULE-220** | Newest-first sorting on lists | TaxesListViewModel: OrderByDescending(Id) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | TaxEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | TaxEditorViewModel | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in TaxEditorViewModel | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | Already exists — not in scope | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not affected by this phase | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected — InvoicePrefix deprecated | ✅ |
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
| **FIFO vs WeightedAverage unresolved** | **HIGH** — could block entire plan | Task 0: confirm decision before starting (default: WeightedAverage) |
| **SystemSettingsRepository IUnitOfWork fix breaks existing code** | Medium | All callers must use `_uow.SaveChangesAsync()` after repo write methods — verify all 4 call sites |
| **Two Tax defaults after seed** | Medium | DB filtered unique index `WHERE IsDefault = 1` enforces single default |
| **DbUpdateException on Tax permanent delete** | Medium | TaxService catches `DbUpdateException` → `Result.Failure` (RULE-200) |
| **New migration conflicts with existing DB** | Low | Always nullable, additive columns — no breaking changes |
| **Old StoreSettings.DefaultTaxRate still used somewhere** | Medium | Search all code for `.DefaultTaxRate` references — replace with Tax entity lookup |
| **22 SystemSettings as key-value = no type safety** | Low | Typed accessors (GetBoolAsync, GetIntAsync) with safe parsing + fallback |
| **IMemoryCache serving stale settings** | Low | Invalidate cache on every write + 5-min sliding expiration |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Seed data causes issues | `DELETE FROM SystemSettings; DELETE FROM StoreSettings WHERE Id > 1; DELETE FROM Taxes;` |
| Tax FK migration causes issues | `ALTER TABLE SalesInvoices DROP COLUMN TaxId; ALTER TABLE PurchaseInvoices DROP COLUMN TaxId; DROP TABLE Taxes;` |
| SignaturePath migration causes issues | `ALTER TABLE StoreSettings DROP COLUMN SignaturePath;` |
| SystemSettings UI not needed | Remove DI registration + navigation entry — no data impact |
| Tax module not needed | Remove all Tax files + revert migration |
| IMemoryCache causes issues | Remove cache registration — settings work without cache |

---

### Task 14 — Unit Tests

**Test Infrastructure:**
- Use xUnit + Moq + FluentAssertions
- `SalesSystem.Domain.Tests` for entity tests
- `SalesSystem.Application.Tests` for service tests
- `SalesSystem.Api.Tests` for API controller tests
- `SalesSystem.Arch.Tests` for configuration tests

**Files to create/modify:**

| File | Change |
|------|--------|
| `Tests/Domain/TaxTests.cs` | **CREATE** — Tax entity factory guards |
| `Tests/Domain/SystemSettingTests.cs` | **CREATE** — Key-value guard clauses |
| `Tests/Application/TaxServiceTests.cs` | **CREATE** — CRUD + FK guard |
| `Tests/Application/StoreSettingsServiceTests.cs` | **CREATE** — Mixed write + transaction |
| `Tests/Application/SystemSettingsServiceTests.cs` | **CREATE** — 22 key-value CRUD |
| `Tests/Api/SettingsControllerTests.cs` | **CREATE** — Get/Update CostingMethod |
| `Tests/Api/TaxesControllerTests.cs` | **CREATE** — Full CRUD endpoints |
| `Tests/Arch/SystemSettingConfigurationTests.cs` | **CREATE** — Config checks |
| `Tests/Arch/TaxConfigurationTests.cs` | **CREATE** — Precision, Restrict |
| `Tests/Desktop/SettingsViewModelTests.cs` | **CREATE** — CostingMethod binding |
| `Tests/Desktop/TaxEditorViewModelTests.cs` | **CREATE** — Validation, save |

**Estimate:** ~3 hours

---

### 1. Domain Entity Tests

#### Tax Entity (`TaxTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### SystemSetting Entity (`SystemSettingTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 2. Service Tests (using `Mock<IUnitOfWork>`)

#### TaxServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### SystemSettingsServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### StoreSettingsServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 3. FluentValidation Tests

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 4. API Controller Tests (Integration)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 5. Database Configuration Tests

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 6. Phase 19-Specific Tests

#### Seed Data: 8 Entities Created Correctly

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### SystemSetting CRUD — 22 Key-Value Pairs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### CostingMethod RadioButton Binding

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### Settings ViewModel: Save Validates Required Fields

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### SettingsController: Get/Update CostingMethod

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

**Test count target:** 65+ tests across all test categories.

**Estimate:** ~3 hours

---

### Task 15 — Add Missing System Settings (HideTaxInSales, HideTaxInPurchases, ShowExpiryInInvoices)

These 3 settings were identified in Analysis Part 5 as required but missing from the original catalog.

#### Catalog Updates (Already Applied Above)

Section 4.2 updated with rows:
- `HideTaxInSales` (#10, Sales, bool, default=false)
- `ShowExpiryInInvoices` (#11, Sales, bool, default=false)
- `HideTaxInPurchases` (#14, Purchases, bool, default=false)

#### Seed Data (Already Applied in Task 5)

Added to DbSeeder SystemSettings block:

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.

#### DTO + ViewModel Wiring

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` — `SystemSettingsDto` or separate DTO | Add `bool HideTaxInSales`, `bool HideTaxInPurchases`, `bool ShowExpiryInInvoices` |
| `Contracts/Requests/MiscRequests.cs` | Add 3 fields to update request |
| `ViewModels/Settings/SystemSettingsViewModel.cs` | Add 3 bool properties + binding + save mapping |
| `Views/Settings/SettingsView.xaml` or `SystemSettingsView.xaml` | Add 3 toggle switches in Sales/Purchases sections |
| `Services/Api/ISettingsApiService.cs` | Already handles all key-value pairs — no change needed |
| Invoice ViewModels (Sales + Purchase) | Consume `HideTaxInSales`/`HideTaxInPurchases` to toggle column visibility |
| Sales invoice screens | Consume `ShowExpiryInInvoices` to toggle expiry column |

#### Logging (RULE-035/036):
- `Log.Information("SystemSetting {Key} set to {Value}", key, value)` on save — already covered
- `Log.Warning("Tax column hidden in Sales — user {UserId}")` when hiding tax (RULE-183)

#### ToolTips (RULE-185-190):
- HideTaxInSales toggle: `"إخفاء أعمدة وحقول الضريبة في شاشات البيع"`
- HideTaxInPurchases toggle: `"إخفاء أعمدة وحقول الضريبة في شاشات الشراء"`
- ShowExpiryInInvoices toggle: `"عرض عمود تاريخ الانتهاء في فواتير البيع والشراء"`

**Estimate**: ~20 minutes

---

### Task 16 — Add Missing Print Settings (ShowLogo, FooterNote)

These 2 settings were identified in Analysis Part 5 as required but missing from the original print settings catalog.

#### Catalog Updates (Already Applied Above)

Section 4.3 updated with rows:
- `ShowLogo` (#12, bool, default=true)
- `FooterNote` (#15, string(500), default="")

#### Seed Data (Already Applied in Task 5)

Added to DbSeeder:

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.

#### DTO + Service Wiring (Updated Task 11)

PrintSettingsDto now includes:
| Field | Type | Default |
|-------|------|---------|
| `PaperSize` | `string` | `"A4"` |
| `PrintCopies` | `int` | `1` |
| `ShowLogo` | `bool` | `true` |
| `ShowBalanceOnPrint` | `bool` | `true` |
| `PrintSignature` | `bool` | `false` |
| `FooterNote` | `string` | `""` |

#### Print Engine Integration

| File | Change |
|------|--------|
| `Infrastructure/Printing/A4InvoiceDocument.cs` | **ShowLogo**: Check `ShowLogo` before rendering logo image. **FooterNote**: Render `FooterNote` text at document bottom. **PrintSignature**: Already wired. |
| `Infrastructure/Printing/ThermalReceiptGenerator.cs` | **ShowLogo**: Check before sending logo ESC/POS commands. **FooterNote**: Append `FooterNote` after signature block. |
| `Infrastructure/Printing/PrintDataService.cs` | Map `ShowLogo` and `FooterNote` from SystemSettings in both read/write paths. |

#### Validation (RULE-044):
- `FooterNote` max length: 500 characters (enforced in config + FluentValidation)

#### ToolTips (RULE-185-190):
- ShowLogo toggle: `"طباعة شعار المتجر في رأس الفاتورة"`
- FooterNote field: `"نص إضافي يظهر في أسفل جميع الفواتير المطبوعة"`

**Estimate**: ~15 minutes

---

### Task 17 — Notification Settings (New — SystemSettings Category="Notifications")

#### Catalog (Already Added as Section 4.6)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `LowStockAlert` | `bool` | `true` | Warn when stock falls below min |
| `ExpiryAlert` | `bool` | `true` | Warn when items near expiry |
| `ExpiryAlertDays` | `int` | `30` | Days before expiry to alert |
| `CreditLimitAlert` | `bool` | `true` | Warn when credit limit exceeded |

#### Seed Data (Already Applied in Task 5)

Added to DbSeeder SystemSettings block:

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.


#### Service Wiring

| File | Change |
|------|--------|
| `Application/Interfaces/Repositories/ISystemSettingsRepository.cs` | Typed accessors (`GetBoolAsync`, `GetIntAsync`) already added in Task 1 — no new methods needed |
| `Application/Services/SystemSettingsService.cs` | Add `GetNotificationSettingsAsync()` that returns `NotificationSettingsDto` with 4 mapped fields |
| `Contracts/DTOs/AllDtos.cs` | Add `NotificationSettingsDto` record |
| `ViewModels/Settings/SystemSettingsViewModel.cs` | Add 4 notification properties + save logic in "Notifications" section |
| `Views/Settings/SystemSettingsView.xaml` or add to `SettingsView.xaml` | Add "الإشعارات" card with 3 toggles + ExpiryAlertDays slider |
| `Services/Api/ISettingsApiService.cs` | Extend with `GetNotificationSettingsAsync()` / `UpdateNotificationSettingsAsync()` IF separate endpoint needed |

**Recommended approach**: Since all 4 are key-value in `SystemSettings`, they can be GET/PUT as a group through the existing `SystemSettings` API (`GET /api/v1/settings/all` + `PUT /api/v1/settings`). No separate controller needed.

#### Dashboard Integration (Deferred)

The actual alert engine logic (background queries for low stock, expiring items, credit limit) is deferred to **Phase 22 — Notification Engine**. Phase 19 only:
1. Seeds defaults ✅ (done)
2. Provides Settings UI toggles ✅ (this task)
3. Documents the key names for future consumption ✅ (Section 4.6)

#### Validation (RULE-044):
- `ExpiryAlertDays`: 1–365 range (FluentValidation)

#### ToolTips (RULE-185-190):
- LowStockAlert toggle: `"تنبيه عند انخفاض المخزون عن الحد الأدنى"`
- ExpiryAlert toggle: `"تنبيه عند قرب انتهاء صلاحية الأصناف"`
- ExpiryAlertDays input: `"عدد الأيام للتنبيه قبل انتهاء الصلاحية (1-365)"`
- CreditLimitAlert toggle: `"تنبيه عند تجاوز السقف الائتماني للعميل أو المورد"`

#### UI Compact (RULE-262-274):
- Toggle switches: `Height="28"` via style, margin `0,0,0,6`
- ExpiryAlertDays: compact `NumericUpDown` or `TextBox` (int input)
- Card header: `FontSize="14"`, padding `12,6`

**Estimate**: ~30 minutes

---

### Task 18 — Backup Module Cross-Reference & Notification Future-Phase Note

#### Backup Module Reference

The backup system was fully implemented in **Phase 4.4** (Auto-Update & Backup) and is NOT re-implemented here. Existing components:

| Component | Phase | File |
|-----------|-------|------|
| Scheduled Backup Worker | 4.4 | `Infrastructure/BackgroundJobs/ScheduledBackupWorker.cs` |
| Backup/Restore Service | 4.4 | `Infrastructure/Services/Backup/BackupService.cs` |
| Backup Settings (RetentionDays, ScheduleTime) | 4.4 | Seeded in `SystemSettings` with keys `Backup.RetentionDays` and `Backup.ScheduleTime` |
| Backup ViewModel | 4.4 | `ViewModels/Settings/BackupViewModel.cs` |
| Backup View | 4.4 | `Views/Settings/BackupView.xaml` |

**Phase 19 impact**: Task 5 already seeds `Backup.RetentionDays` and `Backup.ScheduleTime` as part of the 27 key-value pairs. No other backup changes needed.

#### Notification Future-Phase Note

The notification settings seeded in Task 17 (`LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert`) are **reserved for Phase 22 — Notification Engine**. No background workers should be built in Phase 19. The only Phase 19 scope is:
1. ✅ Seed defaults
2. ✅ Settings UI toggles
3. ✅ Document key names for future consumption

#### Updated Seed Data Cross-Reference

| Category | Keys | Phase | Notes |
|----------|------|-------|-------|
| Inventory | `CostingMethod`, `AllowNegativeStock`, `EnableFefo`, `StockAlertDays` | 19 → Task 5 | ✅ Seeded |
| Sales | `AutoPostInvoices`, `AllowDrafts`, `ShowProfitInInvoice`, `PreventBelowRetailPrice`, `AllowBelowCostSale`, `HideTaxInSales`, `ShowExpiryInInvoices`, `DefaultCashCustomerId` | 19 → Task 5 | ✅ Seeded |
| Purchases | `PurchaseAutoPost`, `HideTaxInPurchases`, `DefaultCashSupplierId` | 19 → Task 5 | ✅ Seeded |
| Barcode | `EnableBarcode`, `BarcodeInputType`, `AutoGenerateBarcode` | 19 → Task 5 | ✅ Seeded |
| Accounting | `AutoCreateJournalEntry` | 19 → Task 5 | ✅ Seeded |
| General | `DecimalPlaces`, `Language`, `DateFormat` | 19 → Task 5 | ✅ Seeded |
| Print | 15 keys (9 existing + 6 new) | 19 → Task 5 | ✅ All seeded |
| Notifications | `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` | 19 → Task 17 | ✅ All seeded |
| Backup | `Backup.RetentionDays`, `Backup.ScheduleTime` | 4.4 + 19 → Task 5 | ✅ Retained |

#### Compliance Matrix Update

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-254** | InvoiceNo as int (Section 2.45) | No InvoiceNo changes in this task | ✅ N/A |
| **RULE-258** | SupplierInvoiceNo kept as supplier ref | InvoiceNo seed does not conflict | ✅ N/A |

**Estimate**: ~10 minutes (documentation only)
