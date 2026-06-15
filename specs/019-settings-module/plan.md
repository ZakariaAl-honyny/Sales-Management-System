# Phase 19 — Settings Module Implementation Plan

> **Version**: 3.0 — Rewritten for new schema (smallint PKs, TaxType, SettingType, Branch/Warehouse hierarchy)
> **Scope**: Organization, Currencies & Settings — 11 tables (Module 2 of database-schema.md)
> **Previous work**: v2.1 plan at `docs/Phase 19 — Settings Module Implementation Plan.md`; review at `docs/phase19_settings_review.md`

---

## 1. Summary

Implement the full Settings & Organization module covering 11 database tables across 7 sub-modules: Branches, Warehouses, Currencies + CurrencyRates, Taxes, CompanySettings (singleton), SystemSettings (key-value with categories), DocumentSequences, FiscalYears, Notifications, and Attachments. All lookup tables use **smallint PKs** for compact storage. Seed 29+ system settings across 8 categories, 7 base units, 2 currencies, 3 tax records, 1 default branch + warehouse, and 9 document sequence types.

---

## 2. Key Entities (Aligned with New Schema)

### 2.1 Branches (`smallint` PK)
Single branch per company in V1. Columns: Name (unique), Phone, Address, ManagerName, Notes. Soft-deletable via `IsActive`. Future: multi-branch support.

### 2.2 Warehouses (`smallint` PK, BranchId FK)
Linked to Branches via `BranchId smallint FK`. Name, Phone, Address, Notes. One default warehouse ("المخزن الرئيسي") seeded.

### 2.3 Currencies (`smallint` PK)
ISO 4217 compliance: Code `char(3)`, Name, FractionName (e.g., "هللة"), Symbol. `IsBaseCurrency` bit with **filtered unique index** (only one active base currency). `IsSystem` bit protects system currencies from deletion. Two seeded: SAR (base) and USD.

### 2.4 CurrencyRates (`int` PK, CurrencyId FK)
Effective-dated rates: `RateToBase decimal(18,2)`, `EffectiveFrom`/`EffectiveTo` datetimes. Indexed on `(CurrencyId, EffectiveFrom)` for date-lookup. FK references Currencies via `smallint`.

### 2.5 Taxes (`smallint` PK)
`Rate decimal(5,2)` with CHECK 0-100. `TaxType tinyint`: Standard=1, ZeroRated=2, Exempt=3. `IsDefault` with **filtered unique index** WHERE `IsDefault=1 AND IsActive=1`. Seed: No Tax (0%, default), VAT 5%, VAT 15%.

### 2.6 CompanySettings (singleton row, `tinyint PK = 1`)
Single-row pattern enforced at DB/API level. Columns: CompanyName, Phone, Email, Address, TaxNumber, LogoPath, `DefaultCurrencyId smallint FK`. No `DefaultTaxRate`/`IsTaxEnabled`/`InvoicePrefix` (deprecated — Tax entity + int InvoiceNo are sources of truth).

### 2.7 SystemSettings (key-value, `int` PK)
Flexible catalog with `SettingKey` (unique), `SettingValue nvarchar(500)`, `SettingType tinyint` (String=1, Integer=2, Decimal=3, Boolean=4), `Category nvarchar(100)`, `DisplayName`, `Description`. Guard clauses on `Create()` enforce non-empty Category + valid DataType. Eight categories: Inventory, Sales, Purchases, Barcode, Accounting, Print, Notifications, General.

### 2.8 DocumentSequences (`int` PK)
Thread-safe auto-numbering per `DocumentType` (e.g., "SalesInvoice", "PurchaseInvoice"). `NextNumber int` incremented via `DocumentSequenceService` with `SemaphoreSlim` lock. 9 document types seeded.

### 2.9 FiscalYears (`int` PK)
YearName, StartDate, EndDate (CHECK `EndDate > StartDate`), IsClosed. Used by accounting engine for fiscal period validation.

### 2.10 Notifications (`int` PK)
`UserId int FK`, `Type tinyint` (LowStock=1, ExpirySoon=2, CreditLimitExceeded=3, System=4, Reminder=5), Title, Message, ReferenceType/ReferenceId, IsRead. Indexed on `(UserId, IsRead, CreatedAt DESC)` for unread queries.

### 2.11 Attachments (`int` PK)
Polymorphic document attachment via `ReferenceType`/`ReferenceId`. FileName, FilePath, FileSize, ContentType. Indexed on `(ReferenceType, ReferenceId)`.

---

## 3. Business Rules

### 3.1 PK Strategy
- **smallint PKs**: Branches, Warehouses, Currencies, Taxes, Units, AccountCategories — all use `smallint IDENTITY` for compact storage (max 32,767 records, sufficient for lookup tables).
- **int PKs**: All document and transaction entities.
- **bigint PKs**: AuditLogs, SystemLogs (high-volume).

### 3.2 Soft Delete & Filtered Unique Indexes
All soft-deletable entities (ActivatableEntity inheritors) have global query filter `IsActive == true`. Filtered unique indexes on soft-deletable entities MUST include `AND [IsActive] = 1` — a soft-deleted record must not prevent creating a new record with the same unique value. Applies to:
- `Tax.IsDefault` (only one active default)
- `Currency.IsBaseCurrency` (only one active base)
- `Currency.Code` and `Currency.Name` (unique among active records)
- `Branches.Name`

### 3.3 FK Delete Behavior
ALL foreign keys use `DeleteBehavior.Restrict` — zero exceptions. No cascade delete anywhere in the system.

### 3.4 Singleton Row Pattern
`CompanySettings` uses `Id = 1` as a fixed tinyint PK. The API ensures GET returns the single row; PUT updates it; POST is not exposed. EF migration seeds it if empty.

### 3.5 Currency Rate History
CurrencyRates are immutable after creation — no editing, only add new rates with future effective dates. Current rate is the one with `EffectiveFrom <= today AND (EffectiveTo IS NULL OR EffectiveTo >= today)` ordered by `EffectiveFrom DESC`.

### 3.6 Tax on Invoices
Both `SalesInvoice` and `PurchaseInvoice` have `int? TaxId` FK referencing `Taxes(Id)`. This provides full audit trail of which tax rate applied to historical invoices. The Tax entity's `IsDefault` record is the single source of truth for invoice tax computation.

### 3.7 Costing Method
Stored as `SystemSetting("CostingMethod")` with integer value: WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3. Seeded as WeightedAverage. Exposed in Settings UI as RadioButton group with Arabic labels.

### 3.8 Thread-Safe Document Sequencing
`DocumentSequenceService` uses `SemaphoreSlim(1,1)` to serialize all `GetNextIntAsync()` calls. The DB sequence is updated inside an EF Core transaction. This guarantees no duplicate invoice numbers under concurrent access.

### 3.9 IUnitOfWork Compliance
SystemSettingsRepository MUST NOT own `SaveChangesAsync()` — all writes go through `IUnitOfWork.SaveChangesAsync()` in the service layer. The `SetBatchSystemSettingsAsync()` method prepares entities only; the caller commits.

---

## 4. Seed Data

### 4.1 Branches (1 record)
- فرع الرئيسي (Main Branch) — default branch, Id=1

### 4.2 Warehouses (1 record)
- المخزن الرئيسي (Main Warehouse) — links to Branch Id=1

### 4.3 Currencies (2 records)
| Name | Code | Symbol | FractionName | IsBaseCurrency | IsSystem |
|------|------|--------|-------------|---------------|----------|
| ريال سعودي | SAR | ﷼ | هللة | `true` | `true` |
| دولار أمريكي | USD | $ | سنت | `false` | `true` |

### 4.4 Currencies Rates (1 record)
- USD to SAR: RateToBase = 3.75, EffectiveFrom = seed date, EffectiveTo = null

### 4.5 Taxes (3 records)
| Name | Rate | TaxType | IsDefault |
|------|------|---------|-----------|
| غير خاضع للضريبة | 0% | Exempt (3) | `true` |
| ضريبة القيمة المضافة 5% | 5% | Standard (1) | `false` |
| ضريبة القيمة المضافة 15% | 15% | Standard (1) | `false` |

### 4.6 CompanySettings (1 singleton row)
CompanyName="متجري", DefaultCurrencyId=SAR, DefaultTaxRate=0m (deprecated via RULE-297), IsTaxEnabled=false, all other fields null.

### 4.7 SystemSettings (29+ key-value pairs across 8 categories)

| Category | Keys | Count |
|----------|------|-------|
| Inventory | CostingMethod, AllowNegativeStock, EnableFefo, StockAlertDays | 4 |
| Sales | AutoPostInvoices, AllowDrafts, ShowProfitInInvoice, PreventBelowRetailPrice, AllowBelowCostSale, HideTaxInSales, ShowExpiryInInvoices, DefaultCashCustomerId | 8 |
| Purchases | PurchaseAutoPost, HideTaxInPurchases, DefaultCashSupplierId | 3 |
| Barcode | EnableBarcode, BarcodeInputType, AutoGenerateBarcode | 3 |
| Accounting | AutoCreateJournalEntry | 1 |
| Print | ThermalPrinterName, A4PrinterName, LogoPath, StoreTaxNumber, TaxRate, AutoPrintOnPost, ReceiptHeader, ReceiptFooter, EscPosCodePage, PaperSize, PrintCopies, ShowLogo, ShowBalanceOnPrint, PrintSignature, FooterNote | 15 |
| Notifications | LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert | 4 |
| General | DecimalPlaces, Language, DateFormat | 3 |

**Total: 41 settings** (some pre-existing, ~29 new seeds).

### 4.8 DocumentSequences (9 types)
SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, CustomerReceipt, SupplierPayment, WarehouseTransfer, InventoryAdjustment, JournalEntry — each starting at NextNumber=1.

### 4.9 Units (7 base units — for future Phase 25)
حبة (Piece), كرتون (Carton), علبة (Box/Can), كيلو (Kilogram), جرام (Gram), لتر (Liter), متر (Meter).

### 4.10 Default Parties (for system reference)
- Default Customer: "عميل نقدي" (cash customer, Id=1) — referenced by `DefaultCashCustomerId` setting
- Default Supplier: "مورد نقدي" (cash supplier, Id=1) — referenced by `DefaultCashSupplierId` setting

---

## 5. Design Decisions

### 5.1 smallint for Lookup Tables
Branches, Warehouses, Currencies, Taxes, Units, Roles, Departments all use `smallint IDENTITY` PK. This aligns with `database-schema.md` §1 (line 11) and reduces index size on frequently joined entities. Max practical limit is 32,767 — far exceeding any lookup table in this system.

### 5.2 TaxType Enum on Tax Entity
The Tax entity has a `TaxType` column (Standard/ZeroRated/Exempt) enabling proper treatment of zero-rated supplies (common in export transactions) and exempt supplies (banking, insurance). This replaces the old binary "tax on/off" approach.

### 5.3 SettingType on SystemSettings
Instead of storing everything as strings, `SettingType` (String/Integer/Decimal/Boolean) enables typed accessors: `GetBoolAsync()`, `GetIntAsync()`, `GetDecimalAsync()` with safe parsing and fallback defaults. The `SystemSettingsViewModel` maps these to strongly-typed properties.

### 5.4 System Account Mapping Is NOT in This Phase
Although `SystemAccountMappings` (4.10 in the schema) stores default account IDs for system operations, it is created and seeded as part of **Phase 22 (Chart of Accounts)** — not Phase 19. The Phase 19 seeder skips it.

### 5.5 Notification Settings Only (No Engine)
Phase 19 seeds the 4 notification settings (LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert) and wires them into the SettingsViewModel. The notification engine (background workers, push alerts) is deferred to Phase 28+.

### 5.6 FiscalYear Seeded via Accounting Phase
FiscalYear table structure is created in Phase 19 migrations, but seed data (creating the current year and opening it) is deferred to **Phase 30 (Journal Entries)** which handles the annual closing lifecycle.

### 5.7 CostingMethod: WeightedAverage Is V1 Default
WeightedAverage is the seeded default per AGENTS.md RULE-068/069. It is the retail standard, already implemented in `UpdateProductPricingService`, and IFRS-compliant. FIFO/FEFO can be added as `CostingMethod.FIFO = 4` in a future phase if pharmacy/expiry tracking is required.

---

## 6. Implementation Tasks

### Task 1 — Domain Entities
Create/update entities: Branch, Warehouse, Currency, CurrencyRate, Tax, CompanySettings, SystemSetting, DocumentSequence, FiscalYear, Notification, Attachment. All lookup entities use `smallint` PKs and ActivatableEntity base class (IsActive, CreatedAt, etc.). Domain guards enforce: non-empty names, positive rates, valid SettingType values, Rate 0-100 CHECK.

### Task 2 — EF Core Configurations
Fluent API configs for all 11 entities. Key requirements: `smallint` PK identity, `nvarchar` with MaxLength, decimal precision (18,2) for money, (18,3) for quantities, (5,2) for Tax.Rate, indexed foreign keys, filtered unique indexes WITH `[IsActive] = 1` guard, CHECK constraints on Tax.Rate, Restrict delete on ALL FKs, global query filter for IsActive on soft-deletable entities.

### Task 3 — Migrations
Single migration creating all 11 tables with proper types, constraints, indexes, and FKs.

### Task 4 — Application Services
Services: `IBranchService`, `IWarehouseService`, `ICurrencyService`, `ICurrencyRateService`, `ITaxService`, `ICompanySettingsService`, `ISystemSettingsService`, `IDocumentSequenceService`, `IFiscalYearService`, `INotificationService`, `IAttachmentService`. All return `Result<T>`. `ISystemSettingsRepository` gains typed accessors (`GetBoolAsync`, `GetIntAsync`, `GetDecimalAsync`). `StoreSettingsService.UpdateSystemSettingsAsync()` uses `ExecuteTransactionAsync` for mixed entity/key-value saves.

### Task 5 — API Controllers
CRUD controllers with `[Authorize]` and policy-based access: TaxesController, CurrenciesController, CurrencyRatesController, BranchesController, WarehousesController, CompanySettingsController, SystemSettingsController (bulk get/put), DocumentSequencesController (read-only GET), NotificationsController, AttachmentsController. FluentValidation on all write requests.

### Task 6 — Desktop ViewModels & Views
- SettingsViewModel: unified single-page view with cards for CompanyInfo, SystemSettings (categorized), PrintSettings, Notifications. CostingMethod RadioButton group.
- TaxesListViewModel + TaxEditorViewModel: full CRUD with INotifyDataErrorInfo, SetDialogService(), ValidateAsync(), EventBus auto-refresh.
- CurrenciesListViewModel + CurrencyEditorViewModel.
- BranchesListViewModel + BranchEditorViewModel.
- WarehousesListViewModel + WarehouseEditorViewModel.
- SystemSettingsViewModelEx (or inline section): strongly-typed properties for all 29+ settings, MapFromDictionary/BuildDictionary pattern.

### Task 7 — DbSeeder
Independent `AnyAsync()` guards per entity type. Seed order: Branches → Warehouses → Currencies → CurrencyRates → Taxes → CompanySettings → SystemSettings → DocumentSequences → Units → Customer/Supplier (for DefaultCashCustomerId/DefaultCashSupplierId references). All Arabic display names and descriptions.

### Task 8 — Integrations
- TaxId FK on SalesInvoice + PurchaseInvoice (existing entities extended).
- Deprecate StoreSettings.DefaultTaxRate/IsTaxEnabled/InvoicePrefix (hide from UI, keep in DB).
- Wire DefaultCurrencyId into invoice print DTOs.
- Connect notification settings to DashboardViewModel alert badges.
- `StoreSettingsChangedMessage` EventBus message for cross-VM reactivity.

### Task 9 — Tests
Unit tests for domain guards (Tax.Create with invalid rate, SystemSetting.Create with empty category/invalid datatype). Service tests for Result<T> patterns. API integration tests for endpoints. Desktop ViewModel tests for settings load/save.

---

## 7. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Smallint PK overflow on high-volume lookups | Low — lookup tables won't exceed 32K records | Monitor via DB index stats, convert to int if needed in V2 |
| DefaultCashCustomerId / DefaultCashSupplierId hardcoded as "1" | Medium — if seed changes, references break | Seed these entities BEFORE SystemSettings; hardcode stays stable |
| 41 system settings spread across 2 storage mechanisms (StoreSettings + SystemSettings) | Low — clear separation: strong-typed singleton vs flexible key-value | Document the boundary in team onboarding |
| Transaction safety for mixed entity/key-value writes | Medium — SystemSettingsRepository historically called SaveChanges directly | **RESOLVED** v4.6.9: removed all SaveChanges from repo; service uses ExecuteTransactionAsync |
| Notification settings seeded but no engine consumes them | Low — settings are invisible to users until the engine is built | Settings appear in UI; engine deferred to Phase 28+ |
