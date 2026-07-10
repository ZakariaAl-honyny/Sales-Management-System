# Phase 19 — Settings Module: Comprehensive Settings Catalog & Implementation Plan

> **Version**: 4.0 — Settings fully wired: 6 dead settings deleted, 14 new settings added per settings details.md, business logic wired, print engine extended
> **Scope**: Complete System Settings Catalog for V1 with 5-category structure, 34 wired settings, 8 deferred (V2)
> **Last Updated**: v4.0 — All Phase 5 changes complete (AutoPost, AllowDrafts, DefaultCash, AutoCreateJournalEntry, EnableFefo, ShowLogo, PrintCopies, StockAlertDays, AllowNegativeCash, AllowDuplicateBarcode, DefaultWarehouse, AutoPrintAfterPosting, ShowProfit, HideTax, AutoGenerateBarcode, ShowBalanceOnPrint, PrintSignature, FooterNote, ShowExpiry, PaperSize, PrintBarcode, PrintQRCode, PrintCompanyAddress, EnableNotifications)

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

### 2.2 System Settings — Key-Value ✅ (Implemented + Wired)

**Entity**: `SalesSystem.Domain.Entities.SystemSetting`

| Field | Type | Default |
|-------|------|---------|
| `SettingKey` | `string(100)` | — (Unique Index) |
| `SettingValue` | `string(500)` | — |
| `DataType` | `string(50)` | `"string"` |
| `Category` | `string(100)` | — |
| `DisplayName` | `string(200)` | — (Required) |
| `Description` | `string(1000)?` | `null` |

**✅ ALL business logic settings now seeded, wired, and functional (v4.0):**

| Key | Category | Default | Wired To | Status |
|-----|----------|---------|----------|--------|
| `AllowNegativeStock` | Inventory | `false` | SalesService (stock validation) | ✅ Seeded + Wired |
| `EnableFefo` | Inventory | `false` | FifoAllocationService (batch selection) | ✅ Seeded + Wired |
| `StockAlertDays` | Inventory | `5` | MinStockAlertWorker (3-tier alert) | ✅ Seeded + Wired |
| `AutoPostInvoices` | Sales | `true` | SalesService.CreateAsync (auto-post) | ✅ Seeded + Wired |
| `AllowDrafts` | Sales | `true` | SalesService.CreateAsync (draft gate) | ✅ Seeded + Wired |
| `ShowProfitInInvoice` | Sales | `true` | SalesInvoiceEditorViewModel (UI) | ✅ Seeded + Wired |
| `PreventBelowRetailPrice` | Sales | `false` | SalesService.PostAsync (price guard) | ✅ Seeded + Wired |
| `AllowBelowCostSale` | Sales | `true` | SalesService.PostAsync (warning only) | ✅ Seeded + Wired |
| `HideTaxInSales` | Sales | `false` | SalesInvoiceEditorViewModel (UI) | ✅ Seeded + Wired |
| `ShowExpiryInInvoices` | Sales | `false` | PrintDataService + A4 + Thermal | ✅ Seeded + Wired |
| `DefaultCashCustomerId` | Sales | `1` | SalesService.CreateAsync | ✅ Seeded + Wired |
| `CreditLimitAlert` | Sales | `true` | SalesService.PostAsync (gate) | ✅ Seeded + Wired |
| `PurchaseAutoPost` | Purchases | `true` | PurchaseService.CreateAsync | ✅ Seeded + Wired |
| `HideTaxInPurchases` | Purchases | `false` | PurchaseInvoiceEditorViewModel | ✅ Seeded + Wired |
| `DefaultCashSupplierId` | Purchases | `1` | PurchaseService.CreateAsync | ✅ Seeded + Wired |
| `AutoGenerateBarcode` | Barcode | `true` | ProductEditorViewModel (UI) | ✅ Seeded + Wired |
| `AutoCreateJournalEntry` | Accounting | `true` | AccountingIntegrationService (17 methods) | ✅ Seeded + Wired |
| `AutoPrintAfterPosting` | Sales | `false` | SalesService + PurchaseService (Post) | ✅ Seeded + Wired |
| `AllowNegativeCash` | CashBox | `false` | CashBoxService (balance check) | ✅ Seeded + Wired |
| `AllowDuplicateBarcode` | Barcode | `false` | ProductService (barcode unique check) | ✅ Seeded + Wired |
| `DefaultWarehouse` | Inventory | `0` | SalesService + PurchaseService | ✅ Seeded + Wired |
| `EnableAttachments` | General | `true` | (feature gate) | ✅ Seeded |
| `EnableNotifications` | Notifications | `false` | MinStockAlertWorker (gate) | ✅ Seeded + Wired |
| `LowStockAlert` | Notifications | `true` | MinStockAlertWorker (3-tier) | ✅ Seeded + Wired |
| `ExpiryAlert` | Notifications | `true` | (deferred ExpiryAlertWorker) | ✅ Seeded |
| `ExpiryAlertDays` | Notifications | `30` | (deferred ExpiryAlertWorker) | ✅ Seeded |
| `RequireBatchOnPurchase` | Purchases | `false` | (V2 — DTO fields needed) | ✅ Seeded, ⏳ Deferred |
| `RequireExpiryOnPurchase` | Purchases | `false` | (V2 — DTO fields needed) | ✅ Seeded, ⏳ Deferred |
| `DefaultBranch` | General | `0` | (V2 — BranchId on invoice DTOs) | ✅ Seeded, ⏳ Deferred |
| `DefaultSalesTax` | Sales | `0` | (V2 — per-invoice tax) | ✅ Seeded |
| `DefaultPurchaseTax` | Purchases | `0` | (V2 — per-invoice tax) | ✅ Seeded |
| `PrintBarcode` | Print | `false` | A4InvoiceDocument + ThermalReceipt | ✅ Seeded + Wired |
| `PrintQRCode` | Print | `false` | A4InvoiceDocument (QR box) | ✅ Seeded + Wired |
| `PrintCompanyAddress` | Print | `true` | A4InvoiceDocument (address line) | ✅ Seeded + Wired |
| `Backup.RetentionDays` | Backup | `30` | ScheduledBackupWorker | ✅ Exists |
| `Backup.ScheduleTime` | Backup | `02:00` | ScheduledBackupWorker | ✅ Exists |

**❌ 6 settings DELETED from system** (confirmed dead, no consumers):
- `EnableBarcode`, `BarcodeInputType`, `DecimalPlaces`, `Language`, `DateFormat`, `Store.AutoUpdatePrices`

**❌ CostingMethod REMOVED from V1** — FIFO is the only costing method

**Existing Repository**:
- `ISystemSettingsRepository` / `SystemSettingsRepository` — with `GetBoolAsync()`, `GetIntAsync()`, `GetStringAsync()`, `GetDecimalAsync()`, `GetAllSystemSettingsAsync()`, `SetBatchSystemSettingsAsync()`, `InvalidateCache()`
- **IMemoryCache** with 5-min sliding expiration

**✅ BLOCKER 1 RESOLVED** — Repository no longer calls SaveChangesAsync directly (RULE-291)

### 2.3 Print Settings ✅ (Fully Implemented — 15 keys)

**Storage**: `SystemSettings` with `Category = "Print"`

**All 15 keys seeded, wired, and functional:**

| Key | Type | Default | Wired To | Status |
|-----|------|---------|----------|--------|
| `ThermalPrinterName` | `string(100)` | `"EPSON"` | PrintService | ✅ Exists |
| `A4PrinterName` | `string(100)` | `""` | PrintService | ✅ Exists |
| `LogoPath` | `string(255)` | `""` | PrintDataService → A4 + Thermal | ✅ Exists |
| `StoreTaxNumber` | `string(50)` | `""` | PrintDataService → A4 + Thermal | ✅ Exists |
| `TaxRate` | `decimal` | `0` | (deprecated — Tax entity is source of truth) | ✅ Exists |
| `AutoPrintOnPost` | `bool` | `false` | (replaced by AutoPrintAfterPosting) | ✅ Exists |
| `ReceiptHeader` | `string(500)` | `""` | ThermalReceiptGenerator | ✅ Exists |
| `ReceiptFooter` | `string(500)` | `""` | ThermalReceiptGenerator | ✅ Exists |
| `EscPosCodePage` | `int` | `22` | ThermalReceiptGenerator | ✅ Exists |
| `PaperSize` | `string(20)` | `"A4"` | A4InvoiceDocument (A4/Letter) | ✅ Seeded + Wired |
| `PrintCopies` | `int` | `1` | PrintService (loop N copies) | ✅ Seeded + Wired |
| `ShowLogo` | `bool` | `true` | PrintDataService → A4Document | ✅ Seeded + Wired |
| `ShowBalanceOnPrint` | `bool` | `true` | PrintDataService → A4Document + Thermal | ✅ Seeded + Wired |
| `PrintSignature` | `bool` | `false` | PrintDataService → A4Document + Thermal | ✅ Seeded + Wired |
| `FooterNote` | `string(500)` | `""` | PrintDataService → A4Document + Thermal | ✅ Seeded + Wired |

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

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 1 | `AllowNegativeStock` | `bool` | `false` | Allow negative inventory | ✅ Wired |
| 2 | `EnableFefo` | `bool` | `false` | Use FEFO when expiry dates exist | ✅ Wired |
| 3 | `StockAlertDays` | `int` | `5` | Low stock warning (days) | ✅ Wired |
| 4 | `DefaultWarehouse` | `int` | `0` | Default warehouse for auto-assignment | ✅ Wired |

#### Sales (Category = "Sales")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 5 | `AutoPostInvoices` | `bool` | `true` | Auto-post invoice on save | ✅ Wired |
| 6 | `AllowDrafts` | `bool` | `true` | Allow saving drafts | ✅ Wired |
| 7 | `ShowProfitInInvoice` | `bool` | `true` | Show profit in sales screen | ✅ Wired |
| 8 | `PreventBelowRetailPrice` | `bool` | `false` | Prevent sale below official price | ✅ Wired |
| 9 | `AllowBelowCostSale` | `bool` | `true` | Allow sale below cost (warning only) | ✅ Wired |
| 10 | `HideTaxInSales` | `bool` | `false` | Hide tax columns in sales screens | ✅ Wired |
| 11 | `ShowExpiryInInvoices` | `bool` | `false` | Show expiry date column in invoices | ✅ Wired |
| 12 | `DefaultCashCustomerId` | `int` | `1` | Default cash customer | ✅ Wired |
| 13 | `CreditLimitAlert` | `bool` | `true` | Warn when credit limit exceeded | ✅ Wired |
| 14 | `AutoPrintAfterPosting` | `bool` | `false` | Auto-print after posting invoice | ✅ Wired |
| 15 | `DefaultSalesTax` | `decimal` | `0` | Default tax for sales | ✅ Seeded, ⏳ V2 |

#### Purchases (Category = "Purchases")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 16 | `PurchaseAutoPost` | `bool` | `true` | Auto-post purchase invoice | ✅ Wired |
| 17 | `HideTaxInPurchases` | `bool` | `false` | Hide tax columns in purchase screens | ✅ Wired |
| 18 | `DefaultCashSupplierId` | `int` | `1` | Default cash supplier | ✅ Wired |
| 19 | `RequireBatchOnPurchase` | `bool` | `false` | Require batch number on purchase | ✅ Seeded, ⏳ V2 (DTO) |
| 20 | `RequireExpiryOnPurchase` | `bool` | `false` | Require expiry date on purchase | ✅ Seeded, ⏳ V2 (DTO) |
| 21 | `DefaultPurchaseTax` | `decimal` | `0` | Default tax for purchases | ✅ Seeded, ⏳ V2 |

#### Barcode (Category = "Barcode")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 22 | `AutoGenerateBarcode` | `bool` | `true` | Auto-generate barcode for new products | ✅ Wired |
| 23 | `AllowDuplicateBarcode` | `bool` | `false` | Block duplicate barcodes | ✅ Wired |

#### Accounting (Category = "Accounting")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 24 | `AutoCreateJournalEntry` | `bool` | `true` | Auto-create journal entry on invoice post | ✅ Wired |

#### CashBox (Category = "CashBox")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 25 | `AllowNegativeCash` | `bool` | `false` | Block negative cash balance | ✅ Wired |

#### General (Category = "General")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 26 | `EnableAttachments` | `bool` | `true` | Enable attachment feature | ✅ Seeded |
| 27 | `DefaultBranch` | `int` | `0` | Default branch for auto-assignment | ✅ Seeded, ⏳ V2 |

#### Notifications (Category = "Notifications")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 28 | `LowStockAlert` | `bool` | `true` | Warn when stock falls below min | ✅ Wired |
| 29 | `ExpiryAlert` | `bool` | `true` | Warn when items near expiry | ✅ Seeded |
| 30 | `ExpiryAlertDays` | `int` | `30` | Days before expiry to trigger alert | ✅ Seeded |
| 31 | `EnableNotifications` | `bool` | `false` | Gate ALL notifications | ✅ Wired |

#### Backup (Category = "Backup")

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 32 | `Backup.RetentionDays` | `int` | `30` | Backup retention in days | ✅ Wired |
| 33 | `Backup.ScheduleTime` | `string` | `"02:00"` | Daily backup schedule time | ✅ Wired |

#### Print (Category = "Print") — see Section 4.3

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 34-48 | *(15 keys)* | mixed | — | Print settings | ✅ All Seeded + Wired |

**Total: 48 keys across 10 categories**

#### ❌ Deleted Settings (6 — confirmed dead, no consumers)

| Key | Category | Reason Deleted |
|-----|----------|---------------|
| `EnableBarcode` | Barcode | No service reads it |
| `BarcodeInputType` | Barcode | No service reads it |
| `DecimalPlaces` | General | No service reads it |
| `Language` | General | No service reads it |
| `DateFormat` | General | No service reads it |
| `Store.AutoUpdatePrices` | Store | No service reads it |

### 4.3 Print Settings (`SystemSettings` — Category = "Print")

| # | Key | Type | Default | V1 | Status |
|---|-----|------|---------|-----|--------|
| 1 | `ThermalPrinterName` | `string(100)` | `"EPSON"` | ✅ | ✅ Exists |
| 2 | `A4PrinterName` | `string(100)` | `""` | ✅ | ✅ Exists |
| 3 | `LogoPath` | `string(255)` | `""` | ✅ | ✅ Exists |
| 4 | `StoreTaxNumber` | `string(50)` | `""` | ✅ | ✅ Exists |
| 5 | `TaxRate` | `decimal` | `0` | ✅ | ✅ Exists |
| 6 | `AutoPrintOnPost` | `bool` | `false` | ✅ | ✅ Exists |
| 7 | `ReceiptHeader` | `string(500)` | `""` | ✅ | ✅ Exists |
| 8 | `ReceiptFooter` | `string(500)` | `""` | ✅ | ✅ Exists |
| 9 | `EscPosCodePage` | `int` | `22` | ✅ | ✅ Exists |
| 10 | `PaperSize` | `string(20)` | `"A4"` | ✅ | ✅ Seeded + Wired |
| 11 | `PrintCopies` | `int` | `1` | ✅ | ✅ Seeded + Wired |
| 12 | `ShowLogo` | `bool` | `true` | ✅ | ✅ Seeded + Wired |
| 13 | `ShowBalanceOnPrint` | `bool` | `true` | ✅ | ✅ Seeded + Wired |
| 14 | `PrintSignature` | `bool` | `false` | ✅ | ✅ Seeded + Wired |
| 15 | `FooterNote` | `string(500)` | `""` | ✅ | ✅ Seeded + Wired |

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

### 4.6 Notification Settings ✅ (Seeded + Wired)

Stored in `SystemSettings` table with `Category = "Notifications"`. These control system-wide alert behavior.

| # | Key | Type | Default | Description | Status |
|---|-----|------|---------|-------------|--------|
| 1 | `LowStockAlert` | `bool` | `true` | Warn when stock falls below minimum | ✅ Wired (MinStockAlertWorker) |
| 2 | `ExpiryAlert` | `bool` | `true` | Warn when items near expiry | ✅ Seeded (ExpiryAlertWorker V2) |
| 3 | `ExpiryAlertDays` | `int` | `30` | Days before expiry to trigger alert | ✅ Seeded (ExpiryAlertWorker V2) |
| 4 | `CreditLimitAlert` | `bool` | `true` | Warn when credit limit is exceeded | ✅ Wired (SalesService.PostAsync) |
| 5 | `EnableNotifications` | `bool` | `false` | Gate ALL notifications system-wide | ✅ Wired (MinStockAlertWorker) |

**Arabic descriptions for seed**:
- `LowStockAlert` → `"تنبيه عند انخفاض المخزون عن الحد الأدنى"`
- `ExpiryAlert` → `"تنبيه عند قرب انتهاء صلاحية الأصناف"`
- `ExpiryAlertDays` → `"عدد الأيام للتنبيه قبل انتهاء الصلاحية"`
- `CreditLimitAlert` → `"تنبيه عند تجاوز السقف الائتماني"`

**Impact**: These feed into `DashboardViewModel` alert counters and `ScheduledBackupWorker`-style background services.

**Note**: Full notification engine (background workers, push alerts) is deferred to Phase 22. Phase 19 seeds the settings and wires them into existing Dashboard alerts.
---

## 5. Gap Analysis — Updated v4.0

### 5.1 Company Settings

| Setting | Status | Action |
|---------|--------|--------|
| `SignaturePath` | ✅ Implemented | Added to entity + config + DTO + SettingsViewModel + PrintDataService |
| `DefaultTaxRate` | ⬜ Deprecated | Hidden from UI — Tax.IsDefault is source of truth |
| `IsTaxEnabled` | ⬜ Deprecated | Hidden from UI |
| `InvoicePrefix` | ⬜ Deprecated | RULE-254: InvoiceNo is int with no prefix |
| All other fields | ✅ Exist | Nothing needed |

### 5.2 System Settings

| Setting | Status | Action |
|---------|--------|--------|
| ALL 48 keys | ✅ Seeded | DbSeeder seeds all keys (6 dead removed, 14 new added) |
| ALL 34 business logic keys | ✅ Wired | Services consume settings and affect behavior |
| 8 V2-deferred keys | ⏳ Seeded | Seeded but no wiring (need DTO/schema changes) |

### 5.3 Print Settings

| Setting | Status | Action |
|---------|--------|--------|
| All 15 keys | ✅ Seeded + Wired | PaperSize, PrintCopies, ShowLogo, ShowBalanceOnPrint, PrintSignature, FooterNote all wired to print engine |

### 5.4 Tax Settings

| Component | Status | Action |
|-----------|--------|--------|
| Tax Entity + Config | ✅ Exists | Fully implemented with CRUD |
| Tax Service + Controller | ✅ Exists | TaxService + TaxesController with Result<T> |
| Tax Desktop screens | ✅ Exists | TaxesListView + TaxEditorView |
| TaxId FK on Invoices | ✅ Exists | smallint null FK → Taxes(Id) |
| Tax seed data | ✅ Seeded | 3 tax records: No Tax, VAT 5%, VAT 15% |

### 5.5 Notification Settings

| Setting | Status | Action |
|---------|--------|--------|
| `LowStockAlert` | ✅ Wired | MinStockAlertWorker 3-tier alert |
| `ExpiryAlert` | ✅ Seeded | ExpiryAlertWorker deferred to V2 |
| `ExpiryAlertDays` | ✅ Seeded | ExpiryAlertWorker deferred to V2 |
| `CreditLimitAlert` | ✅ Wired | SalesService.PostAsync |
| `EnableNotifications` | ✅ Wired | Gates MinStockAlertWorker |

### 5.6 Settings Implementation (Phases 1-5)

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Core Business | ✅ Complete | AutoPostInvoices, AllowDrafts, DefaultCash, CreditLimitAlert, PurchaseAutoPost, DefaultCashSupplier |
| Phase 2: UI Settings | ✅ Complete | ShowProfit, HideTax, AutoGenerateBarcode |
| Phase 3: Print + Alerts | ✅ Complete | ShowBalanceOnPrint, PrintSignature, StockAlertDays (3-tier) |
| Phase 4: Print Extended | ✅ Complete | ShowExpiry, PaperSize, PrintBarcode, PrintQRCode, PrintCompanyAddress, Signature image |
| Phase 5: New Settings | ✅ Complete | 6 deleted, 14 added, AutoPrint, AllowNegativeCash, AllowDuplicateBarcode, DefaultWarehouse, EnableNotifications |
| Phase 6: ExpiryAlertWorker | ⬜ V2 | ExpiryAlert + ExpiryAlertDays need background worker |
| Phase 7: Purchase DTO fields | ⬜ V2 | RequireBatchOnPurchase, RequireExpiryOnPurchase need line item BatchNo/ExpiryDate |

---

## 6. Architectural Decisions

### 6.1 Costing Method: FIFO Only (No User Selection)

**Analysis from `a.md` + codebase audit confirmed:**

The system uses `InventoryBatches` with `QuantityRemaining` and `UnitCost` for FIFO/FEFO batch tracking. The `FifoAllocationService` (341 lines) is fully implemented and called by:
- `SalesService.PostAsync()` — FIFO batch deduction on every sale
- `PurchaseService.PostAsync()` — batch creation on every purchase
- `SalesReturnService.PostAsync()` — return to batch
- `PurchaseReturnService.PostAsync()` — deduct from batches

**The `CostingMethod` setting is DEAD CODE:**
- Seeded as `"1"` (WeightedAverage) in DbSeeder
- Stored and retrievable via API
- **NO service reads it to change behavior** — `SalesService`, `PurchaseService`, `InventoryService` all ignore it
- `UpdateProductPricingService` is a no-op stub (logs and returns success)

**Decision**: **Remove `CostingMethod` from V1 entirely.** The system uses FIFO for batch tracking + weighted average for COGS journal entries (computed on-the-fly from `InventoryBatch` data via `ProductCostService.GetAverageCostAsync()`). This is the correct retail approach.

If multi-method costing is needed in the future, it can be added as `CostingMethod.FIFO = 1, WeightedAverage = 2` with actual behavior wiring.

**Also from `a.md`**: The analysis recommended these settings instead of CostingMethod. All already exist in the codebase:
- `AllowNegativeStock` — ✅ seeded as `"false"`, used in SalesService
- `AllowBelowCostSale` — ✅ seeded as `"true"`, warning-only (correct per analysis: "ولا نمنع البيع")
- `EnableFefo` — ✅ seeded as `"false"`, wired to FifoAllocationService
- `ExpiryAlertDays` — ✅ seeded as `"30"`, wired to Notifications category
- `DefaultTaxId` — ❌ NOT needed (architecturally unnecessary — `Tax.IsDefault` flag handles default tax selection, already seeded in DbSeeder)

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

## 8. Implementation Tasks — Updated v4.0

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 1 — BLOCKER 1: Fix SystemSettingsRepository IUnitOfWork Violation ✅ DONE

**Status**: ✅ Completed in v4.6.9 (Phase 19 Settings Module Remediations)

| File | Change |
|------|--------|
| `Infrastructure/Repositories/SystemSettingsRepository.cs` | ✅ Removed `SaveChangesAsync()` from `SetStringAsync()` |
| `Application/Interfaces/Repositories/ISystemSettingsRepository.cs` | ✅ Added `GetBoolAsync()`, `GetIntAsync()`, `GetDecimalAsync()`, `InvalidateCache()` |
| `Application/Services/StoreSettingsService.cs` | ✅ Uses `_uow.SaveChangesAsync()` for mixed writes |

---

### Task 2 — BLOCKER 2: Add TaxId FK to SalesInvoice & PurchaseInvoice ✅ DONE

**Status**: ✅ Completed — TaxId FK exists on both entities (smallint null FK → Taxes)

---

### Task 3 — BLOCKER 3: Deprecate DefaultTaxRate, IsTaxEnabled, InvoicePrefix ✅ DONE

**Status**: ✅ Completed — Hidden from SettingsUI, Tax.IsDefault is source of truth

---

### Task 4 — Add SignaturePath to StoreSettings ✅ DONE

**Status**: ✅ Completed in v4.6.9 — SignaturePath added to entity + config + DTO + PrintDataService + A4Document

---

### Task 5 — Seed SystemSettings (All 48 keys) ✅ DONE

**Status**: ✅ Completed in v4.10.7 — DbSeeder seeds all 48 keys across 10 categories
- 6 dead settings removed (EnableBarcode, BarcodeInputType, DecimalPlaces, Language, DateFormat, Store.AutoUpdatePrices)
- 14 new settings added (AutoPrintAfterPosting, AllowNegativeCash, AllowDuplicateBarcode, EnableAttachments, EnableNotifications, DefaultSalesTax, DefaultPurchaseTax, DefaultBranch, DefaultWarehouse, RequireBatchOnPurchase, RequireExpiryOnPurchase, PrintBarcode, PrintQRCode, PrintCompanyAddress)

---

### Task 6 — Seed Master Data Entities ✅ DONE

**Status**: ✅ Completed — Branches, CashBox (linked to 11010001), FiscalYear, Departments, ProductCategories all seeded

---

### Task 7 — *(removed — was Task 7 in v3.0 but not documented)*

---

### Task 8 — StoreSettingsChangedMessage EventBus Message ✅ DONE

**Status**: ✅ Exists in `DesktopPWF/Messaging/Messages/AppMessages.cs`

---

### Task 9 — Tax Module (Full Build — New) ✅ DONE

**Status**: ✅ Fully implemented across all layers (Domain, Application, Contracts, API, Desktop)

---

### Task 10 — System Settings UI Screen ✅ DONE

**Status**: ✅ Completed — SystemSettingsViewModel + SystemSettingsView.xaml with 10 category cards

---

### Task 11 — Add Print Settings to DTOs + Services ✅ DONE

**Status**: ✅ Completed — PaperSize, PrintCopies, ShowLogo, ShowBalanceOnPrint, PrintSignature, FooterNote, PrintBarcode, PrintQRCode, PrintCompanyAddress all in DTOs + PrintDataService + A4Document + ThermalReceipt

---

### Task 12 — Add IMemoryCache for SystemSettings ✅ DONE

**Status**: ✅ Completed — IMemoryCache with 5-min sliding expiration in SystemSettingsRepository

---

### Task 13 — Expanded Compliance Matrix ✅ DONE

**Status**: ✅ Updated in v4.0

---

### Task 14 — Unit Tests ⬜ PARTIAL

**Status**: ⬜ Domain.Tests (380 pass) + Application.Tests (63 pass, 7 pre-existing failures)
- TaxServiceTests, StoreSettingsServiceTests — need verification
- AccountingIntegrationServiceTests — fixed with ISystemSettingsRepository mock

---

### Task 15 — Add Missing System Settings (HideTax, ShowExpiry) ✅ DONE

**Status**: ✅ Seeded, wired, and UI toggles in SystemSettingsView.xaml

---

### Task 16 — Add Missing Print Settings (ShowLogo, FooterNote) ✅ DONE

**Status**: ✅ Seeded, wired to print engine, UI in SystemSettingsView.xaml

---

### Task 17 — Notification Settings ✅ DONE

**Status**: ✅ Seeded (LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert, EnableNotifications)
- LowStockAlert + CreditLimitAlert + EnableNotifications → wired in MinStockAlertWorker
- ExpiryAlert + ExpiryAlertDays → deferred to V2 ExpiryAlertWorker

---

### Task 18 — Backup Module Cross-Reference ✅ DONE

**Status**: ✅ Backup.RetentionDays + Backup.ScheduleTime seeded and wired

---

### Task 19 — SystemSettings Business Logic Seed ✅ DONE (v4.0)

**Status**: ✅ All 31 business logic settings seeded (Inventory 4, Sales 10, Purchases 5, Barcode 2, Accounting 1, CashBox 1, General 2, Notifications 4, Backup 2)

---

### Task 20 — Settings Wiring: Phase 1-5 ✅ DONE (v4.0)

**Status**: ✅ All 5 wiring phases complete:
- Phase 1: AutoPostInvoices, AllowDrafts, DefaultCashCustomerId, CreditLimitAlert, PurchaseAutoPost, DefaultCashSupplierId
- Phase 2: ShowProfitInInvoice, HideTaxInSales, HideTaxInPurchases, AutoGenerateBarcode
- Phase 3: ShowBalanceOnPrint, PrintSignature, StockAlertDays (3-tier), LowStockAlert
- Phase 4: ShowExpiryInInvoices, PaperSize, PrintBarcode, PrintQRCode, PrintCompanyAddress, Signature image
- Phase 5: AutoPrintAfterPosting, AllowNegativeCash, AllowDuplicateBarcode, DefaultWarehouse, EnableNotifications

---

### Task 21 — Delete Dead Settings ✅ DONE (v4.0)

**Status**: ✅ 6 dead settings removed from DbSeeder, StoreSettingsService, SystemSettingsViewModel, SystemSettingsView.xaml

---

### Task 22 — Add 14 New Settings per settings details.md ✅ DONE (v4.0)

**Status**: ✅ All 14 new settings added to DbSeeder + SystemSettingsViewModel + SystemSettingsView.xaml + StoreSettingsService validation

---

### Task 10 — System Settings UI Screen ✅ DONE

**Status**: ✅ SystemSettingsViewModel + SystemSettingsView.xaml with 10 category cards (Inventory, Sales, Purchases, Barcode, Accounting, CashBox, General, Notifications, Print, Backup)

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
| **RULE-068** | CostingMethod — REMOVED from V1 | FIFO is the only costing method; UpdateProductPricingService is a no-op stub | ✅ N/A |
| **RULE-069** | CostingMethod enum — REMOVED from V1 | Domain enum exists but is not used in any business logic | ✅ N/A |
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
| **RULE-207** | CostingMethod — REMOVED from V1 | System uses FIFO batches + weighted average COGS (computed on-the-fly) | ✅ N/A |
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
| ~~FIFO vs WeightedAverage unresolved~~ | ~~HIGH~~ | **RESOLVED** — FIFO is the only method (a.md + codebase audit confirmed) |
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

### Task 14 — Unit Tests ⬜ PARTIAL

**Status**: ⬜ Domain.Tests (380✅), Application.Tests (63✅, 7❌ pre-existing)
- Tax entity tests: ✅ Exist
- AccountingIntegrationServiceTests: ✅ Fixed (ISystemSettingsRepository mock)
- Pre-existing failures: AuthService/UserService test constructor changes needed

---

### Task 15 — Add Missing System Settings (HideTax, ShowExpiry) ✅ DONE

**Status**: ✅ Seeded, wired, UI toggles in SystemSettingsView.xaml

---

### Task 16 — Add Missing Print Settings (ShowLogo, FooterNote) ✅ DONE

**Status**: ✅ Seeded, wired to A4Document + ThermalReceiptGenerator, PrintDataService

---

### Task 17 — Notification Settings ✅ DONE

**Status**: ✅ 5 settings seeded: LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert, EnableNotifications
- LowStockAlert + CreditLimitAlert + EnableNotifications → wired in MinStockAlertWorker
- ExpiryAlert + ExpiryAlertDays → deferred to V2 ExpiryAlertWorker

---

### Task 18 — Backup Module Cross-Reference ✅ DONE

**Status**: ✅ Backup.RetentionDays + Backup.ScheduleTime seeded and wired

---

## Seed Data Cross-Reference (Updated v4.0)

| Category | Keys Seeded | Wired In Code | Deferred (V2) |
|----------|-------------|---------------|---------------|
| **Inventory** | 4 | 3 (AllowNegativeStock, EnableFefo, StockAlertDays) | 1 (DefaultBranch) |
| **Sales** | 10 | 9 (AutoPostInvoices, AllowDrafts, ShowProfit, PreventBelowRetail, AllowBelowCostSale, HideTaxInSales, ShowExpiry, DefaultCashCustomerId, CreditLimitAlert, AutoPrintAfterPosting) | 1 (DefaultSalesTax) |
| **Purchases** | 5 | 3 (PurchaseAutoPost, HideTaxInPurchases, DefaultCashSupplierId) | 2 (RequireBatch, RequireExpiry, DefaultPurchaseTax) |
| **Barcode** | 2 | 2 (AutoGenerateBarcode, AllowDuplicateBarcode) | 0 |
| **Accounting** | 1 | 1 (AutoCreateJournalEntry) | 0 |
| **CashBox** | 1 | 1 (AllowNegativeCash) | 0 |
| **General** | 2 | 0 (EnableAttachments, DefaultBranch) | 2 (V2) |
| **Notifications** | 5 | 3 (LowStockAlert, CreditLimitAlert, EnableNotifications) | 2 (ExpiryAlert, ExpiryAlertDays) |
| **Backup** | 2 | 2 (RetentionDays, ScheduleTime) | 0 |
| **Print** | 15 | 15 (all wired to print engine) | 0 |
| **Store** | 10 | 10 (StoreName, Phone, Address, etc.) | 0 |
| **TOTAL** | **57** | **48** | **9 (V2 deferred)** |

---

## Deferred to V2 (Settings)

| Setting | Reason | Needs |
|---------|--------|-------|
| RequireBatchOnPurchase | Purchase line items don't have BatchNo field | DTO + Domain + Schema |
| RequireExpiryOnPurchase | Purchase line items don't have ExpiryDate field | DTO + Domain + Schema |
| DefaultBranch | Invoice DTOs don't have BranchId field | DTO changes |
| DefaultSalesTax | Per-invoice tax selection | Invoice-level tax UI |
| DefaultPurchaseTax | Per-invoice tax selection | Invoice-level tax UI |
| ExpiryAlert | Needs ExpiryAlertWorker background service | Background worker |
| ExpiryAlertDays | Needs ExpiryAlertWorker background service | Background worker |
| EnableAttachments | Feature gate only — no UI wiring yet | Desktop attachment UI |
| DefaultWarehouse | Wired as fallback but needs WarehouseId on more DTOs | Partial |

#### Compliance Matrix Update

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-254** | InvoiceNo as int (Section 2.45) | No InvoiceNo changes in this task | ✅ N/A |
| **RULE-258** | SupplierInvoiceNo kept as supplier ref | InvoiceNo seed does not conflict | ✅ N/A |

**Estimate**: ~10 minutes (documentation only)
