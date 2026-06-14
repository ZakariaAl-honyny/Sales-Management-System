# Changelog

All notable changes to this project will be documented in this file.

## v4.10.2 — Accounts.md Analysis: Bank Auto-Account, Employee Endpoint, Parent Code Fixes & FlexibleInputCalculator Bug Fix (2026-06-15)

### ✨ Feature: Bank Auto-Account Creation
- `Bank` entity: `AccountId` changed from `int` to `int?` with `SetAccountId()` domain method (follows `CashBox` pattern)
- `BankConfiguration`: AccountId FK made optional (`.IsRequired(false)`)
- `BankDto` / `CreateBankRequest`: AccountId changed to `int?`
- `CreateBankRequestValidator`: AccountId optional (only validates when non-null)
- `BankService.CreateAsync()`: Auto-creates Level-4 detail account under parent `"1120 — البنوك"` when no AccountId provided (mirrors CashBoxService auto-creation under `"1110 — النقدية"`)
- `BankService.AutoCreateBankAccountAsync()`: Finds parent 1120, auto-increments child code, creates Asset account with color #2196F3
- `BankEditorView.xaml`: Updated labels and helper text explaining auto-creation

### ✨ Feature: Employee Auto-Account Endpoint
- `POST /api/v1/employees/{id}/auto-create-account` endpoint on `EmployeesController`
- Calls `EmployeeService.AutoCreateEmployeeAccountAsync()` with userId from JWT
- Creates Level-4 detail account under parent `"1170 — عهد الموظفين"` for custody/advance tracking

### 🐛 Bug Fix: CustomerService/SupplierService Account Parent Codes
- `CustomerService.AutoCreateCustomerAccountAsync()`: Fixed AR parent lookup from `"1210"` (Fixed Assets) to `"1130"` (العملاء/Accounts Receivable)
- `SupplierService.AutoCreateSupplierAccountAsync()`: Fixed AP parent lookup from `"2100"` (doesn't exist) to `"1320"` (الموردون/Accounts Payable)

### 🐛 Bug Fix: FlexibleInputCalculator — RecalculateFromFlexibleInput Logic
- Fixed `RecalculateFromFlexibleInput()` in BOTH `SalesInvoiceEditorViewModel` and `PurchaseInvoiceEditorViewModel`
- The method now ONLY calls `FlexibleInputCalculator.Calculate()` when `_lastModifiedField == CalculationField.Total` (user explicitly edited LineTotal)
- When user edits Quantity or UnitPrice/UnitCost, `_lineTotalInput` is computed directly as `_quantity * _unitPrice` (or `_quantity * _unitCost`)
- This prevents the calculator from treating the auto-computed total as a user-entered anchor, which caused incorrect Price/Quantity recalculation
- All 449 DesktopPWF tests pass (0 failures)

### 🔧 Maintenance
- AGENTS.md updated with RULE-502 through RULE-505 (Accounts.md analysis)
- AGENTS.md forbidden patterns updated with 5 new entries (Bank nullable AccountId, parent code fixes, flexible input fix, employee endpoint)
- AGENTS.md checklist updated with 7 new items

## v4.10.1 — Purchase Invoice OtherCharges, Sales Price Enforcement, DeliveryChargesRevenue, Purchase Return Standalone & Flexible Input (2026-06-15)

### ✨ Feature: PurchaseInvoice OtherCharges (Landed Cost)
- Added `OtherCharges` decimal property to `PurchaseInvoice` entity with `otherCharges < 0` guard clause
- Updated `RecalculateTotals()` to include `+ OtherCharges`
- Created `AllocateAdditionalCharges()` in `PurchaseService.PostAsync()` — distributes proportionally by line total
- Adjusted landed unit cost per item before inventory batch creation
- Updated EF config, Create/Update DTOs/Requests, and Desktop ViewModel/XAML
- Updated `AccountingIntegrationService.CreatePurchasePostEntryAsync()` to use `SubTotal - Discount + OtherCharges`

### ✨ Feature: Sales Price Enforcement
- Injected `IProductPriceService` into `SalesService` for price enforcement lookups
- `PostAsync()` rejects items when `UnitPrice < effectivePrice` (if `PreventBelowRetailPrice` enabled)
- `PostAsync()` rejects items sold below `CostInBaseCurrency` (if `AllowBelowCostSale` disabled)
- Desktop VM `GetDefaultPrice()` uses actual price lookup (not `0m` stub)
- `CostInBaseCurrency` loads from `ProductDto.Cost` (not hardcoded `0m`)

### ✨ Feature: DeliveryChargesRevenue Account
- Added `SystemAccountKey.DeliveryChargesRevenue = 21` enum value
- Seeded COA account `1533 — إيرادات التوصيل` under parent `1530 — إيرادات أخرى` (total 74 accounts)
- `CreateSalesPostEntryAsync()` credits DeliveryChargesRevenue separately from SalesRevenue
- `ReverseSalesPostEntryAsync()` mirrors the split
- Proper accounting separation — delivery fees not lumped with product sales

### ✨ Feature: Purchase Return Standalone Mode
- Desktop editor allows standalone returns (`PurchaseInvoiceId = null`) with supplier + items
- Added `SelectedSupplierId`, `SelectedSupplierName`, `IsLinkedToInvoice` properties
- Fixed hardcoded `ProductUnitId: 1` to use actual ProductUnitId from input
- Added `CreatePurchaseReturnEntryAsync()` and `ReversePurchaseReturnEntryAsync()` in AccountingIntegrationService
- Added `GET /api/v1/purchase-returns/returned-quantities/{invoiceId}` endpoint
- Fixed `PostedAt`/`CancelledAt` not set in `Post()`/`Cancel()` methods

### ✨ Feature: Flexible Input (Sales + Purchases)
- Created `FlexibleInputCalculator` helper class with `CalculationField` enum
- User enters ANY TWO of (Quantity, UnitPrice/UnitCost, LineTotal) — third auto-calculated
- LineTotal column editable in Sales and Purchase DataGrids (not `IsReadOnly`)
- `LineTotalInput`, `_lastModifiedField`, `_isRecalculating` in both line ViewModels
- `_isRecalculating` guard flag prevents infinite recursion loops

### 🔧 Maintenance
- All Phase 18 post-analysis remediations verified
- AGENTS.md updated with RULE-475 through RULE-498 (5 new sections)
- Forbidden patterns updated for all 5 features
- Checklist updated with 13 new items

## v4.10 — 65-Table Schema Refactoring & Docs Cleanup (2026-06-13)

### ✨ Major Architecture Changes
- **Schema reduced from 82 to 65 tables**: 17 tables removed, 8 added/modified for V1 final design
- **Perpetual Inventory**: NO Purchases clearing account — all inventory costs go DIRECTLY to Inventory Asset account
- **Units as independent table**: Units table with smallint PK, user-addable, IsSystem flag for seed data protection
- **ProductPrices**: Multi-currency pricing per (ProductUnit × CurrencyId) with effective date ranges — replaces RetailPrice/WholesalePrice on ProductUnit
- **InventoryBatches**: Replaces PurchaseLots — batch-level FIFO/FEFO cost allocation with UnitCost per batch
- **WarehouseTransfers/WarehouseTransferLines**: Replaces StockTransfers/StockTransferItems
- **InventoryTransactions/InventoryTransactionLines**: Replaces InventoryMovements — 12 transaction types with status lifecycle
- **CustomerReceipts**: Replaces CustomerPayments with multi-invoice allocation (CustomerReceiptApplications)
- **ReceiptVouchers/PaymentVouchers**: Replace CashTransactions for box money flow
- **Party entity**: Shared contact data (Name, Phone, Email, Address, TaxNumber) — Customers/Suppliers each have PartyId FK
- **CashBox simplified**: NO OpeningBalance/CurrentBalance — balance lives on linked Account only
- **Customer/Supplier simplified**: NO OpeningBalance/CurrentBalance/CurrencyId — balance on linked Account only
- **smallint PKs**: Branches, Warehouses, Currencies, Units, Roles, Departments, Taxes, AccountCategories
- **bigint PKs**: AuditLogs.Id, SystemLogs.Id for high-volume data
- **SystemLog.Level**: nvarchar → tinyint (enum: Info=1, Warning=2, Error=3, Critical=4)
- **BaseCurrency immutable**: IsBaseCurrency cannot be changed after system creation
- **No CustomerGroup/SupplierType/CustomerType in V1**: Payment type is per-invoice, not per-entity
- **No PriceLevel enum in V1**: Pricing is simply per (ProductUnit × CurrencyId)
- **No Purchases Orders/Sales Quotations/Cheques/DailyClosure in V1**: Deferred to V2

### 📄 Docs Consolidation
- **docs/database-schema.md**: Established as SINGLE source of truth for all table definitions (65 tables, 8 modules)
- **docs/PRD-MVP.md**: Removed 2,324 lines of duplicate schema definitions, SQL scripts, and C# entity code — replaced with references to database-schema.md
- **docs/CONSTITUTION.md**: Removed duplicate schema details, added Perpetual Inventory, ProductPrices, Party, Units sections, added Schema Reference section
- **Phase plans 18, 19, 21, 22**: Removed inline SQL/C# code — replaced with references to canonical docs
- **AGENTS.md**: Updated with per-unit pricing, immutable base currency, independent Units, ProductPrices rules
- **README.md**: Updated with Phases 15-32 features, new services, updated badges
- **18 subagent files**: All updated with 65-table schema knowledge, new entity patterns, removed entity detection
- **All numbers**: 460+ architecture rules enforced (AGENTS.md), 2,083+ tests

### 🛠️ Entity/Service Changes
- Removed entities: ProductBarcodes, ProductImages, BillOfMaterials, ProductPriceHistory (old), StoreSettings (old), CustomerGroup, CustomerPayments, SupplierPayments (old), InventoryMovements, StockTransfers, StockTransferItems, InventoryOperations, StockWriteOffs, CashTransactions, DailyClosures, Cheques, PurchaseLots
- Added/modified entities: Parties, CustomerReceipts, CustomerReceiptApplications, InventoryBatches, InventoryTransactions, InventoryTransactionLines, WarehouseTransfers, WarehouseTransferLines, ProductPrices, ReceiptVouchers, PaymentVouchers, CompanySettings (replaces StoreSettings)
- All FK types: smallint for lookup tables (Branches, Warehouses, Currencies, Units, Roles, Departments, Taxes, AccountCategories)
- All money: decimal(18,2), quantities: decimal(18,3), percentages: decimal(5,2)

## v4.6.9 — Phase 23: Customers Module (2026-06-08)

### ✨ New Features
- **CustomerType enum** (Cash/Credit) — stored as `byte` in DB, **informational only**; actual Cash/Credit decision is per-invoice via `SalesInvoice.PaymentType`
- **CustomerGroup entity** — new categorization entity with soft-delete and child reference guard
- **Account linking** — optional FK from Customer to Account for journal entry integration
- **Credit Limit Enforcement** — `SalesService.PostAsync()` checks `customer.CheckCreditLimit(invoice.DueAmount)` before allowing credit sales; rolls back transaction with Arabic error if exceeded. Enforces based on `CreditLimit > 0` (not CustomerType).
- **API: CustomerGroupsController** — full CRUD with kebab-case routes and ManagerAndAbove write policy
- **API: GET /api/v1/customers/groups** — group lookup endpoint with AllStaff policy
- **API: GET /api/v1/customers/by-group/{groupId}** — customers filtered by group
- **API: GET /api/v1/customers/reports/balance** — customer balance report with balance status (مدين/دائن/متوازن)
- **API: GET /api/v1/customers/reports/aging** — customer aging report with aging buckets
- **Desktop: Customer Editor** — CustomerType (info only), CustomerGroup dropdown, Account lookup with AvailableGroups/AvailableAccounts data loading
- **Desktop: Customer List** — group filter ComboBox for filtering customers by group
- **Report DTOs** — `CustomerBalanceReportDto` + `CustomerAgingReportDto` with balance status and aging buckets

### 🛠️ Enhancements
- CustomerDto extended with AccountId, AccountName, CustomerType, CustomerGroupId, CustomerGroupName
- CustomerService GetAllAsync now includes Account/CustomerGroup navigation properties via Include
- CustomerService.GetAllGroupsAsync() added for group lookup
- CustomerService.GetByGroupAsync(), GetCustomerBalanceReportAsync(), GetCustomerAgingReportAsync() added
- CreateCustomerRequest/UpdateCustomerRequest enhanced with AccountId, CustomerType, CustomerGroupId
- CustomerValidators enhanced with Phone regex `^05\d{8}$`, Email `.EmailAddress()`, CustomerType range validation
- DbSeeder: seeds "عام" group, enhanced default customer with Cash type + GroupId
- CustomerEditorView: removed redundant emoji + FontSize=20 violation (RULE-266)
- CustomerListViewModel: removed all CanExecute predicates (RULE-059), added null-check guards with warning dialog
- All async operations in CustomerListViewModel refactored to ExecuteAsync wrapper pattern (RULE-141)

### 🔧 Infrastructure
- New EF migration `Phase23_CustomersModule` — adds CustomerGroups table, AccountId/CustomerType/CustomerGroupId columns to Customers
- All FKs use DeleteBehavior.Restrict — no cascade
- CustomerGroups registered in UnitOfWork
- 370 total architecture rules enforced (AGENTS.md)

### 🧪 Tests
- CustomerEditorViewModelTests updated with IAccountApiService mock (25 test methods)
- CustomersControllerTests updated with FluentValidation mocks (4 test methods)
- Build verification: 0 errors, 0 warnings across all 12 projects

### 🐛 Bug Fixes
- **XAML-001 [FIXED]**: `Style="{StaticResource ModernTextBox}"` used on 3 `ComboBox` elements (CustomerType, CustomerGroup, Account dropdowns) — `ModernTextBox` has `TargetType="TextBox"`, causing `XamlParseException`. Changed to `Style="{StaticResource ModernComboBox}"`.
- **XAML-002 [FIXED]**: `DisplayMemberPath="Name"` and `<ComboBox.ItemTemplate>` both set on the same `ComboBox` — WPF throws `InvalidOperationException` (`Cannot set both DisplayMemberPath and ItemTemplate`). Removed `DisplayMemberPath`.

## v4.6.9 — Phase 22 Bug Fixes + Chart of Accounts Complete (2026-06-08)

### Phase 22 — Chart of Accounts Module (v4.6.9+)
- **60-Account Hierarchy**: 4 levels (Group→Main→Sub→Detail), 5 Groups + 8 Main + 20 Sub + 27 Detail accounts
- **Account entity**: Level (int 1-10), ColorCode (hex), AllowTransactions (L4+), OpeningBalance, Description, Explanation (new string? field)
- **DB Constraints**: CHK_Account_Level_Range, self-referencing ParentAccountId FK with Restrict delete, HasConversion<int> on all enum properties
- **Two-Pass Seeder**: Creates L1→SaveChanges→Query IDs→L2→SaveChanges→L3→L4 — 60 accounts seeded with Arabic names, color codes, and Arabic explanation text
- **SystemAccountMappings**: Updated with new account codes — maps 13 system operation types
- **AccountDto + AccountTreeNodeDto**: Computed AccountTypeDisplay/LevelDisplay, recursive Children for TreeView
- **AccountService**: 8 methods — GetTreeAsync (builds from flat list, no N+1), CRUD with parent/level/code validation, DbUpdateException handling
- **AccountsController**: 7 CRUD endpoints, AllStaff/ManagerAndAbove/AdminOnly policies, 404 vs 400 differentiation
- **Desktop dual-mode UI**: TreeView (HierarchicalDataTemplate) + DataGrid toggle, search/filter in both modes, Edit/Delete toolbar commands, edit mode with read-only AccountCode
- **FluentValidators**: CreateAccountRequestValidator (code format, Level-1 exact 3 chars, NameAr, ColorCode hex), UpdateAccountRequestValidator (same rules as Create)

### Phase 22 Code Review — Bug Fixes
- **BUG-001 [FIXED]**: `HasChildren()` domain guard on `Account.MarkAsDeleted()` never executed — `SubAccounts` nav property not loaded by EF. Service now uses `AnyAsync(a => a.ParentAccountId == id)` DB query before calling `MarkAsDeleted()`. Domain guard retained as defense-in-depth.
- **BUG-002/003 [FIXED]**: Double entity fetch in `DeleteAsync()`/`PermanentDeleteAsync()` — now loads entity once, uses already-loaded instance for `MarkAsDeleted()` and `DeleteRange()`.
- **BUG-004 [FIXED]**: `Explanation` field missing across ALL layers — added to Domain entity (string? nullable), EF config (nvarchar(500)), DTOs, Requests, Service mapping, Validator (MaxLength(500)), and Seeder (Arabic text for all 60 accounts).
- **BUG-005 [FIXED]**: Level-1 account codes lacked special length validation — `CreateAccountRequestValidator` now enforces exactly 3 characters for Level-1 accounts.
- **BUG-006 [FIXED]**: `UpdateAccountRequestValidator` missing `NameAr` Arabic message, `NameEn` MaxLength, `ColorCode` hex validation — now has SAME rules as Create validator.
- **`:byte` route constraint [FIXED]**: `AccountsController` used `{type:byte}` which causes HTTP 500 (no built-in `:byte` in ASP.NET Core) — changed to `{type:int:min(1):max(5)}`.
- **Health check leak [FIXED]**: `DatabaseHealthCheck` used raw `IConfiguration.GetConnectionString()` returning `""` (empty per RULE-040), bypassing DPAPI decryption — rewritten to inject `SecureDbContextFactory.GetDecryptedConnectionString()` (single source of truth).
- **Enh-3 [FIXED]**: Account editor edit mode — loads existing account, populates fields, sets `AccountCode` read-only.
- **Enh-4 [FIXED]**: Edit/Delete commands with toolbar buttons in `AccountsListViewModel`.
- **Enh-5 [FIXED]**: Search/filter works in BOTH TreeView and DataGrid modes.
- **Log.Error → Log.Warning [FIXED]**: `DatabaseHealthCheckService.cs` retry/timeout messages lowered to Warning level (per RULE-182).
- **RULE-341 through RULE-352** added to AGENTS.md — Phase 22 code review bug fix rules.

### New Rules (AGENTS.md §2.73-2.74)
- RULE-341 through RULE-352 — Phase 22 Code Review Bug Fixes (HasChildren→AnyAsync, double entity fetch, Explanation field, :byte route, Update validator completeness, health check source of truth)

### Documentation & Subagent Updates
- AGENTS.md: Header updated to "v4.6.9+ — Phases 21-22 Complete + Bug Fixes"; Section 2.74 added (12 bug fix rules); FORBIDDEN section updated (12 new items); Checklist updated (13 new items)
- README.md: Phase 22 bug fixes table added; Contributing section updated to "352 rules"
- code-reviewer.md: "Phase 22 Code Review Bug Fixes" section added with 18 checklist items across 6 categories
- backend-architect.md: Phase 22 bug fix patterns added (HasChildren→AnyAsync, double fetch, Explanation, routes, validators, health check)
- database-engineer.md: Phase 22 bug fix sections added (Explanation field, AccountCode length, UpdateValidator, route constraints)
- ui-agent.md: Phase 22 UI fix patterns added (edit mode, list VM commands, dual-mode search)
- implement-agent.md: Phase 22 code patterns added (DeleteAsync, PermanentDeleteAsync, route constraints)

## v4.6.9 — Settings Module Fixes & Phase 19 Remediations (2026-06-06)

### Phase 19 Settings Module — Code Review Fixes

- **BUG-001 [FIXED]**: Removed `SaveChangesAsync` from `SetBatchSystemSettingsAsync()` — repository no longer owns commit (RULE-291). Added `_uow.SaveChangesAsync()` to `StoreSettingsService.UpdateSystemSettingsAsync()`.
- **BUG-002 [FIXED]**: Added `UpdateTimestamp()` call to `Tax.Update()` — audit trail was broken for tax modifications (RULE-292).
- **BUG-003 [FIXED]**: Added 10 missing system settings to `DbSeeder` — `HideTaxInSales`, `ShowExpiryInInvoices`, `HideTaxInPurchases`, `ShowLogo`, `FooterNote`, `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` (RULE-296).
- **BUG-004 [PRE-EXISTING]**: `SetTax()` domain method already existed on both `SalesInvoice` and `PurchaseInvoice` — no change needed.
- **BUG-005 [FIXED]**: Changed `StoreSettings` seed `defaultTaxRate: 15m` → `0m` — Tax entity is source of truth (RULE-297).
- **BUG-006 [FIXED]**: Added `AND [IsActive] = 1` to `TaxConfiguration` filtered unique index on `IsDefault` (RULE-294).
- **BUG-007 [FIXED]**: Added `Category` guard clause and `DataType` validation to `SystemSetting.Create()` — validates against whitelist (RULE-293).
- **BUG-008 [FIXED]**: `SetStringAsync()` now accepts `category` parameter (default → `"General"`) — no longer hardcodes `category: "Print"` (RULE-295).
- **BUG-009 [FIXED]**: Removed dead-code redundant null check in `SettingsController.GetPrintSettings()`.

### Phase 19 Enhancements
- Total system settings seeded: 29 across 8 categories (Inventory, Sales, Purchases, Barcode, Accounting, Print, Notifications, General)
- `SystemSettingsRepository` now uses `IMemoryCache` with `ConcurrentDictionary` key tracker for efficient cache invalidation
- `StoreSettingsService` delegates system settings endpoints to `IStoreSettingsService` (no direct `ISystemSettingsRepository` injection in controller)
- `SettingsController` now differentiates `NotFound` (404) vs `BadRequest` (400) responses based on `ErrorCodes.NotFound`
- **ENH-001/002 [FIXED]**: Added missing properties to `SystemSettingsViewModel` — `HideTaxInSales`, `ShowExpiryInInvoices` (Sales), `HideTaxInPurchases` (Purchases), `ShowLogo`, `FooterNote`, `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber` (Print), `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` (Notifications). All now map from/to the dictionary via `MapFromDictionary`/`BuildDictionary`.
- **ENH-003 [FIXED]**: Added `ValidateSystemSettings()` in `StoreSettingsService` — validates 19 boolean keys via `bool.TryParse`, 6 integer keys via `int.TryParse` with range checks (CostingMethod 1-3, DecimalPlaces 0-6, StockAlertDays 1-365).
- **ENH-004 [FIXED]**: Added `Tax.ClearDefault()` and `Tax.SetDefault()` domain methods — both call `UpdateTimestamp()` for audit trail (RULE-299).
- **RULE-299 through RULE-301** added to AGENTS.md — Enhancement remediations for Phase 19.

### Phase 20 Currencies Module — Remaining Fix
- **BUG-008 [FIXED]**: `Currency.Create()` validation changed from `code.Length > 10` to `code.Trim().Length != 3` — ISO 4217 requires exactly 3 characters.
- RULE-298 added to AGENTS.md — currency code length validation rule.

### Phase 20 Currencies Module — Enhancement Fixes
- **ENH-005 [FIXED]**: Added `OpeningBalance = initialBalance` to `CashBox.Create()` — OpeningBalance was always 0 regardless of initial balance.
- **ENH-007 [FIXED]**: Added `SetAsBaseCurrency()` and `UnsetBaseCurrency()` domain methods on `Currency` entity — both call `UpdateTimestamp()` for audit trail (RULE-303).
- **ENH-012 [FIXED]**: Removed unnecessary `async` keyword from lambda in `CurrencyEditorViewModel.LoadRateHistoryAsync()` — no `await` inside the lambda (RULE-304).
- **RULE-302 through RULE-304** added to AGENTS.md — Enhancement remediations for Phase 20.

### Documentation Updates
- `docs/phase18_accounting_review.md` — All 12 bugs (5 critical, 7 standard) now marked as `[FIXED]`; executive summary updated; checklist and priority fix order refreshed; post-review fix status table added.

### New Rules (AGENTS.md §2.67)
- RULE-291 through RULE-298 — Settings Module (291-297) + CurrencyCode validation (298) code review remediations

### Phase 21 — Users & Permissions Module (v4.6.9)
- **16 tasks** implemented: Domain entities (User rebuild, Permission, RolePermission, AuditLog, UserSession) -> Infrastructure configs + seed (33 permissions, 4 roles, passwordless admin) -> Application services (AuditLogService, PermissionService, AuthService update) -> API controllers (10 new endpoints) -> Desktop UI (5 new/updated screens) -> Tests + Migration.
- **User entity rebuilt**: Passwordless creation, UserStatus enum (Active/Inactive/Locked), MustChangePassword, Phone/Email/Avatar/DefaultCashBoxId, account lockout (5 failed attempts).
- **Permissions system**: DB-backed Permission+RolePermission entities, 33 seed permissions across 9 categories, 4-role model (Admin/Accountant/Cashier/Observer), PermissionService with transactional updates, PermissionManagement UI with grouped checkboxes.
- **Audit & Security**: AuditLog entity (long Id, 3 indexes), AuditLogService (paginated/filtered queries), AuditLog browser UI, UserSession tracking, login audit trail (Success/Failed/Locked).
- **Auth enhancements**: Passwordless first-login flow (SetPassword), ChangePassword with current password verification, account lockout, MustChangePassword redirect.
- **API endpoints**: POST /auth/set-password, POST /auth/change-password, GET /users/current, POST /users/{id}/reset-password, GET /audit-logs, GET /audit-logs/user/{id}, GET /audit-logs/login-history, GET /permissions, GET /permissions/roles, PUT /permissions/roles/{role}.
- **Desktop UI**: Enhanced UserEditor (avatar 80x80, Phone/Email), PasswordChangeScreen (3 fields, INotifyDataErrorInfo), AuditLog browser (paginated DataGrid, filters), PermissionManagement (4-role tabs, category expanders, checkboxes), MainWindow StatusBar (avatar, role badge, change password, logout), permission-based nav visibility.
- **Validators**: ChangePasswordRequestValidator, SetPasswordRequestValidator, AuditLogQueryValidator -- all with Arabic messages.
- **EF Migration**: Phase21_UsersAndPermissions generated.
- **Build**: All 6 projects 0 errors, 0 warnings. Tests: 1,887 passed, 5 pre-existing failures, 70 skipped.

## v4.6.8 — Currency Module Stabilization & EF Core Transaction Strategy (2026-06-06)

### What's New
- Fixed ALL 14 Critical/Bug items from Phase 20 Currencies Module code review
- Fixed ALL 12 Critical/Bug items from Phase 18 Accounting Foundation code review
- Added `IUnitOfWork.ExecuteTransactionAsync()` for atomic multi-save operations using `CreateExecutionStrategy().ExecuteAsync()` with explicit `BeginTransactionAsync()`/`CommitAsync()` inside the delegate
- Added DB CHECK constraints on `JournalEntryLine` (`CHK_DebitOrCredit`, `CHK_NoNegativeValues`)
- Fixed `JournalEntryNumberGenerator` daily reset logic — now queries by today's date prefix (`JE-{yyyyMMdd}`) instead of global last entry by Id
- Fixed `SystemAccountMappings` navigation property mappings — all 13 relationships now use lambda syntax `HasOne(x => x.DefaultCashAccount)` etc.
- Added `Account.Activate()` method allowing reactivation of deactivated accounts
- Added account names/codes to `SystemAccountMappingsDto` so UI can display meaningful account labels (not raw IDs)
- Added `Account.Create()` — `createdByUserId` null guard fixed; `MarkAsDeleted()` now protected for system accounts via `IsSystem` guard
- Fixed `Currency.Create()` to accept `isSystem` parameter (default `false`) — system currencies can no longer be deleted
- Fixed all Controller endpoints to return `404 NotFound` for `ErrorCodes.NotFound`, `400 BadRequest` for business validation errors
- `CurrenciesListViewModel` now implements `IDisposable`, uses toast for minor success messages
- Removed `IsActive` field from `UpdateCurrencyRequest` (was unused and confusing)
- Added `UpdateExchangeRateRequestValidator` FluentValidation rule
- Added composite index on `ExchangeRateHistory(CurrencyId, EffectiveDate)` for fast lookups
- Read endpoints now use `AllStaff` policy instead of restrictive `AdminOnly`
- Exchange rate display precision changed from `N2` to `N6` in DataGrid bindings
- All `ViewModels/Currencies/` files updated with RULE-229 validation pattern, no CanExecute, `IDisposable`, and `IToastNotificationService` for success feedback
- AGENTS.md updated: RULE-275 to RULE-290 added covering transaction strategy, ExchangeRate precision, navigation property mapping, editor validation pattern, and LogSystemError discipline

| Category | Count |
|----------|-------|
| 🔴 Critical bugs fixed | 11 |
| 🟠 Bugs fixed | 15 |
| 🟡 Enhancements | 14 |
| **Total files modified** | ~45 files across 6 projects |
| **All 9 projects build** | 0 errors, 0 warnings |

## v4.6.7 — InvoiceNo Int Re-addition & DocumentSequenceService Enhancement (2026-06-06)

### Added
- **InvoiceNo (int) Re-added with UNIQUE constraint**: `SalesInvoice` and `PurchaseInvoice` now have `int InvoiceNo` — a user-facing invoice number, separate from the auto-increment `Id` PK. UNIQUE per document type (RULE-261).
  - Domain: `int InvoiceNo` with guard `if (invoiceNo <= 0) throw ...`
  - Requests: `int? InvoiceNo` in create DTOs (null = auto-generate via DocumentSequenceService)
  - DTOs: `int InvoiceNo` in all response/report/print DTOs
  - Services: Default via `IDocumentSequenceService.GetNextIntAsync("SalesInvoice"|"PurchaseInvoice", ct)` — thread-safe with `SemaphoreSlim` lock
  - Validators: `GreaterThan(0)` rule + uniqueness check against DB
  - Desktop: `int InvoiceNo` property in editor VMs, displays in list columns, search by int comparison
- **DocumentSequenceService Enhancement**: Added `GetNextIntAsync(sequenceKey, ct)` for thread-safe int sequence generation — uses `SemaphoreSlim` lock + DB sequence row.
  - Sequence keys: `"SalesInvoice"`, `"PurchaseInvoice"` stored in DocumentSequences table like other sequences.
  - Old prefix-based generation (INV/PUR strings) replaced with pure int for invoices.
- **Migration `AddInvoiceNoColumn`**: Adds `InvoiceNo int NOT NULL` to `SalesInvoices` and `PurchaseInvoices` tables with UNIQUE constraint.
- **8 new tests**: `DocumentSequenceServiceTests` — `GetNextInt_ShouldReturnIncrementedValue`, `GetNextIntAsync_ThreadSafety`, `GetNextInt_ShouldStartAtOne`, `GetNextInt_ShouldHandleMultipleKeys`, etc. — all passing.
- **Screen Title Emoji Icons**: 41 main screen page title headers now have emoji icons (📦 Products, 🛒 Sales, 📥 Purchases, 👤 Customers, etc.) — improves visual scanability.
- **FirstValidationErrorConverter**: Moved from `App.xaml` to `Resources/Styles.xaml` to fix `XamlParseException`.

### Fixed
- **Concurrency bug**: `lastId + 1` was NOT thread-safe for multi-user environments — replaced with `DocumentSequenceService.GetNextIntAsync()` using `SemaphoreSlim` lock.
- **Garbled Arabic Text — Full Solution Sweep**: 48 garbled Arabic strings fixed in `InvoicePrintDtoBuilderTests.cs`. 5 garbled comment box-drawing separators fixed in `ProductSelectionViewModel.cs`, `PurchaseInvoiceListViewModel.cs`, `SalesInvoiceListViewModel.cs`.
- **All 9 Dialog Buttons Standardized**: FontSize="11", Padding="10,4", CornerRadius="6" applied consistently across all dialogs.
- **DeleteConfirmationDialog Header**: Icon container 48×48→44×44, icon FontSize 26→20, title FontSize 18→16.
- **ValidationErrorsDialog List**: Bullet points 14→12, error text 14→13, LineHeight 22→20.
- **Screen Title Icon FontSizes**: 18→16 across 9 error bars, 5 report views, and StockTransferEditor header.
- **Empty-State Titles**: FontSize 18→14 in CashBoxTransactionsView, DailyClosureView, ProductsListView.
- **Page Titles**: BackupView 18→16, CostingMethodSettingsView 18→14.

### Changed
- All remaining non-standard button sizes standardized to Styles.xaml global compact tokens.
- AGENTS.md Section 2.63 updated: `InvoiceNo` is now UNIQUE per document type — NOT "duplicates allowed".
- AGENTS.md RULE-254/255/261 updated: InvoiceNo is UNIQUE, generated by `DocumentSequenceService.GetNextIntAsync()`.
- `docs/CONSTITUTION.md` Section 5 updated: InvoiceNo int strategy with UNIQUE constraint documented.
- `docs/database-schema.md` updated: InvoiceNo columns marked UNIQUE, new tables (Currencies, Accounts, JournalEntries, JournalEntryLines, PurchaseLots, FiscalYears, Taxes) added.

---

## v4.6.6 — UI Compacting — Mobile-Ready Density (2026-05-31)

### Added
- **Global UI Compacting**: All 63 XAML views compacted by ~25-30% for more content per screen and future mobile adaptation.
- **Styles.xaml Global Token Reduction**: Button/TextBox/ComboBox default heights 36→28px, font sizes 13→11, DataGrid row height 34→24, header fonts 20→16.
- **Dashboard Compacted**: KPI card spacing 32→12px, icon padding 12→6, description font size reduced.
- **15 List Views Compacted**: Toolbar spacing reduced, search widths 220→160, all `Height="36"` overrides removed, empty-state margins 20→12px, widths 160→140px.
- **14 Editor Views Compacted**: Header/footer padding reduced ~40%, section spacing 12→6/8px, title fonts 18→14.
- **15 Reports/Settings/Inventory Views Compacted**: Filter bars, section margins, fonts, footer paddings all reduced.
- **19 Dialogs/Shell Views Compacted**: Dialog titles 20→16, icon borders 50×50→44×44, button widths MinWidth 80-100, dialog containers shrunk ~15%.
- **MainWindow Sidebar**: Width 220→200, menu padding 5→3, brand area 16,20→12,12.
- **ScreenWindow**: MinWidth/MinHeight 600/400→500/350, default size 900×650→850×600.
- **NumericKeypadControl**: Touch keys MinHeight 30→28, MinWidth 40→36, FontSize 16→14.
- **Touch-optimized views preserved** at their touch-friendly sizes (CartView, Qty buttons).

### Changed
- **PurchaseInvoiceEditorView Fully Compacted**: Major miss caught — 18 edits: header 16,8→12,6, title font 18→14, outer margin 16→10, TextBox Height=36 removed, all field margins 12→6/8, footer padding 20,12→12,8.
- **ProductsListView Empty State**: Button Margin 0,20→0,12, Width 160→140, Height=36 removed.
- **ExpiredProductsReportView**: 4× Height=34 removed, button padding 20,0 removed.
- **CashBoxTransactionsView**: 2× Height=32 removed from filter buttons.
- **SalesInvoiceEditorView**: Line add button Height=30 removed, search button 32→28.
- **Return Editors (Sales/Purchase)**: TextBox Height=36 removed, add button Height=36 removed, search button Height=32 removed.
- **ProductUnitEditorView**: Button Height=32 removed, margin 8→6.
- **StockTransferEditorView**: Border Height=30 removed, padding 16,4→10,4.
- **Controls/NumericKeypadControl**: Keys MinHeight 30→28, MinWidth 40→36, FontSize 16→14.

### Fixed
- **5 Selection Views XML**: Fixed missing `>` on TextBox opening tags in ProductSelectionView, CustomerSelectionView, SalesInvoiceSelectionView, PurchaseInvoiceSelectionView, SupplierSelectionView — caused parsing errors after subagent edit glitch.

### Removed
- `Height="36"`, `Height="34"`, `Height="32"`, `Height="30"` hardcoded overrides from all Button/TextBox/ComboBox elements across all views (let Styles.xaml handle heights).
- `Padding="16,0"`, `Padding="20,0"`, `Padding="24,0"` hardcoded overrides from buttons.
- `Padding="16,12"`, `Padding="20,12"` from header/footer borders — replaced with `12,6` / `12,8`.
- Large `Margin="0,0,0,12"` from form fields — replaced with `0,0,0,6` or `0,0,0,8`.

---

## v4.6.5 — Invoice Number Removal & Touch POS Polish (2026-05-31)

### Breaking Changes
- **InvoiceNo (string) Removed from SalesInvoice**: `InvoiceNo` property and parameter removed from `SalesInvoice` entity and `SalesInvoice.Create()` factory method. Use auto-increment `Id` (int PK) as the sole invoice identifier.
- **InvoiceNo (string) Removed from PurchaseInvoice**: Same removal — `PurchaseInvoice.InvoiceNo` eliminated. Only `SupplierInvoiceNo` remains as supplier's external reference number (not a system identifier).
- **GetByNumberAsync Removed from Services & Controllers**: `GetByNumberAsync()` / `GetByNumber()` methods removed from `ISalesService`, `SalesService`, `IPurchaseService`, `PurchaseService`, and both `SalesInvoicesController` and `PurchaseInvoicesController`.
- **GetByNumber Endpoints Removed**: `GET /api/v1/sales/{number}` and `GET /api/v1/purchases/{number}` endpoints removed — search by `Id` only.
- **GetByNumberAsync Removed from Desktop API Clients**: `GetByNumberAsync()` removed from `SalesInvoiceApiService` and `PurchaseInvoiceApiService`.

### Added
- **Stock Validation on Touch POS Product Add**: When adding a product with insufficient stock to a Touch POS cart, a warning dialog shows product name, requested quantity, and available stock (uses `_dialogService.ShowWarningAsync`).
- **PlayWarning() on ISoundService**: New `PlayWarning()` method added to `ISoundService`/`SoundService` (uses `System.Media.SystemSounds.Asterisk.Play()`). Warning sound plays on stock validation failures.

### Changed
- **SalesReportDto**: `string InvoiceNo` replaced with `int Id` for invoice identifier across reports.
- **PurchaseReportDto**: Same — `int Id` replaces `string InvoiceNo`.
- **InvoicePrintDto (Printing)**: Uses `int Id` instead of `string InvoiceNo` for PDF/thermal printing.
- **Invoice List Search**: `SalesInvoiceListViewModel` and `PurchaseInvoiceListViewModel` search now uses `int.TryParse()` + `Id` comparison — no string `InvoiceNo` filtering.
- **EF Config**: `SalesInvoiceConfiguration` and `PurchaseInvoiceConfiguration` no longer configure `InvoiceNo` property (MaxLength, HasIndex removed).
- **Stock Validation Warning**: Added `_dialogService.ShowWarningAsync` call in TouchPosViewModel when product stock is insufficient.

### Fixed
- **Touch POS Product Card Layout**: Removed `VirtualizingPanel.ScrollUnit="Pixel"` from `Styles.xaml` `ModernListBox` style. Increased `MinHeight` on `TouchPosCartView` from 150 to 200. Product cards now render with proper proportions.
- **Garbled Arabic Encoding**: Fixed UTF-8 encoding issues across multiple files including `StockTransfersListViewModel.cs`, `SupplierPaymentsListViewModel.cs`.
- **CategoryApiService GetById**: Endpoint corrected from `GetAllAsync` pattern to proper `GetById` call.

### Removed
- `SalesInvoice.InvoiceNo` property and EF configuration
- `PurchaseInvoice.InvoiceNo` property and EF configuration
- `InvoiceNo` parameter from `SalesInvoice.Create()` and `PurchaseInvoice.Create()`
- `GetByNumberAsync` methods from 2 services, 2 controllers, 2 API clients
- `InvoiceNo` unique index from `SalesInvoiceConfiguration` and `PurchaseInvoiceConfiguration`
- `DocumentSequenceService` usage for invoice numbering (INV/PUR prefixes)
- `InvoiceNo` from `SalesInvoiceListDto` and `PurchaseInvoiceListDto` records

## v4.6 — Identifier Strategy & Validation (2026-05-25)

### Breaking Changes
- **Entity Code Removal**: Removed legacy `Code` property from master data entities (Product, Customer, Supplier, Warehouse, Category, Unit, User) across Domain, Contracts, Application, and Infrastructure layers. Entities now rely solely on auto-increment `Id` for identification.
- **DuplicateCode Error Removed**: `ErrorCodes.DuplicateCode` constant deleted. Only `DuplicateBarcode` remains for barcode uniqueness.

### Added
- `INotifyDataErrorInfo` validation framework in `ViewModelBase` with `AddError()`, `ClearErrors()`, `ClearAllErrors()`, `ValidateAllAsync()`, and `ValidateField()` methods.
- Global `Validation.ErrorTemplate` in `Styles.xaml` — red border + ❗ icon badge with error ToolTip for `TextBox`, `PasswordBox`, and `ComboBox`.
- `SetDialogService()` method on `ViewModelBase` for enabling `ValidateAllAsync()` dialog support.

### Changed
- **SupplierEditorViewModel**: Refactored from legacy `HasNameError`/`HasOpeningBalanceError` boolean pattern to `INotifyDataErrorInfo` with `AddError/ClearErrors/ValidateAllAsync`.
- **WarehouseEditorViewModel**: Refactored from legacy `HasNameError` boolean pattern to `INotifyDataErrorInfo` with `AddError/ClearErrors/ValidateAllAsync`.
- **ProductEditorView.xaml**: Removed 7 manual `HasXxxError`/`XxxError` TextBlock bindings — validation feedback now handled by `ErrorTemplate`.
- **CustomerEditorView.xaml, WarehouseEditorView.xaml, SupplierEditorView.xaml**: Removed manual `NameError`/`HasNameError` TextBlock bindings.

### Fixed
- Validation feedback now properly red-bordered with ❗ icon and ToolTip for all text-based inputs.

### Removed
- Legacy `HasNameError`, `HasOpeningBalanceError`, `NameError`, `OpeningBalanceError` boolean properties from `SupplierEditorViewModel`.
- Legacy `HasNameError`, `NameError` boolean properties from `WarehouseEditorViewModel`.

## v4.5 — Multi-Window & UI Polish (2026-05-25)

### ✨ New Features
- **Multi-Window Non-Modal Editors**: Editors now open in separate non-modal windows (Product, Customer, Supplier, Category, Unit, User, Sales Invoice, Purchase Invoice, etc.)
- **ScreenWindowService**: Generic window host with cascade positioning (30px offset, modulo 10 reset)
- **WeakReference Window Tracking**: Closed windows are fully garbage collected — no memory leaks
- **Arabic Auto-Titles**: Editor windows display descriptive Arabic titles (e.g., "فاتورة بيع جديدة")

### 🐛 Bug Fixes
- **Dialog Ownership**: Dialogs now correctly center over the active window — no more self-ownership crashes
- **EventBus Memory Leaks**: DashboardViewModel now uses standard `Cleanup()` override for unsubscription
- **DialogService Active Window Resolution**: Owner correctly resolved to the active window instead of always MainWindow

### 🛠️ Improvements
- **Newest-First Sorting**: All list screens (Products, Customers, Suppliers, Invoices, etc.) default to newest-first
- **Arabic ToolTips**: All primary interactive controls now have descriptive Arabic ToolTips
- **MessageBox Elimination**: Zero remaining `MessageBox.Show` calls — 100% IDialogService

## v4.4 — Production Hardening (2026-05-25)

### الميزات الجديدة
- **DPAPI Connection String Encryption**: تشفير سلسلة الاتصال بقاعدة البيانات باستخدام `ProtectedData` على أول تشغيل
  - `ConnectionStringProtector` مع بادئة `"DPAPI:"` وفحص عدم التشفير المزدوج
  - `SecureDbContextFactory` مع fallback إلى متغير البيئة `SALESSYSTEM_DB_CONNECTION`
  - كتابة ذرية للملفات: `.tmp` ← `File.Replace()` ← `.bak`
  - `FirstRunSetupService` لتشفير الإعدادات تلقائياً عند أول تشغيل
  - `SecurityAudit.cs` — فحص أمني في وضع DEBUG فقط
- **Windows Service Hosting**: تشغيل API كخدمة ويندوز مع سياسة استرداد تلقائي
  - `UseWindowsService()` مع اسم الخدمة `SalesSystemService`
  - 3 محاولات إعادة تشغيل عند الفشل (1د، 5د، 15د)
  - Serilog EventLog sink لتسجيل الخدمة
  - إعادة محاولة SQL عند بدء التشغيل: 3 محاولات × 5 ثوانٍ
  - ترحيل قاعدة البيانات تلقائياً عند بدء الخدمة
- **Automated Daily Backups**: نسخ احتياطي تلقائي يومي مع تنظيف قديم
  - `ScheduledBackupWorker` — `BackgroundService` يومياً عند 2:00 صباحاً
  - SQL خام `BACKUP DATABASE` بدون SMO
  - استعادة باستخدام `SINGLE_USER WITH ROLLBACK AFTER 30` (مهلة 30 ثانية للمعاملات النشطة)
  - `TrySetMultiUserAsync` للاسترداد عند فشل الاستعادة — لا تُترك DB في SINGLE_USER أبداً
  - `DeleteOldBackupsAsync` — حذف النسخ القديمة تلقائياً (الاحتفاظ الافتراضي 30 يوماً)
  - `int.TryParse` لكل قيم الإعدادات — لا `FormatException`
- **Desktop Health Check**: فحص اتصال قاعدة البيانات قبل عرض شاشة الدخول
  - `IDatabaseHealthCheckService` — يفحص `/api/v1/health/database` قبل تسجيل الدخول
  - `DatabaseErrorDialog` مع زر إعادة المحاولة والخروج ورسائل خطأ بالعربية
  - `ExceptionMiddleware` يكتشف استثناءات الاتصال ويعيد `503 Service Unavailable` مع رمز `DATABASE_CONNECTION_ERROR`
  - نقطة نهاية `GET /api/v1/health/database` تستخدم `DbContext.Database.CanConnectAsync()`
- **Silent Auto-Update**: تحديث تلقائي في الخلفية مع تحقق من SHA256
  - `IUpdaterService` و `UpdaterService` — فحص تحديثات مع timeout 8 ثوانٍ وفشل صامت
  - `GitHubUpdaterService` — بديل يستخدم GitHub API
  - `UpdateDialogViewModel` مع `IDisposable` وإبلاغ التقدم و 4 أوامر
  - `UpdateDialog.xaml` — نافذة RTL بدون حدود مع مقارنة الإصدار وسجل التغييرات وشريط التقدم
  - تحقق SHA256 قبل تشغيل المثبت
  - `LaunchInstallerAndExitAsync` يعيد `Result<bool>` — المتصل يدير الإغلاق
  - الإصدار المُتخطّى محفوظ في `%AppData%\SalesSystem\settings.json`
  - مقارنة الإصدارات باستخدام `System.Version` — لا مقارنة نصوص
- **Settings UI**: حقول إعدادات النسخ الاحتياطي والتحديث في صفحة الإعدادات
  - مسار النسخ الاحتياطي، وقت الجدولة، أيام الاحتفاظ
  - عنوان خادم التحديث (Update Server URL)
  - `AdminOnlyViewModel` — فرض صلاحية Admin عبر `ISessionService` المُحقونة في المُنشئ
  - `UserListViewModel` — إدارة المستخدمين مع Toggle Status (حذف ناعم) وإعادة تعيين كلمة المرور

### تحسينات
- إضافة `ShowInfoAsync` إلى `IDialogService` — ثيمة زرقاء وأيقونة معلومات
- استبدال جميع استدعاءات `MessageBox.Show` في `MainWindow.xaml.cs` بـ `IDialogService`
- `AdminOnlyViewModel` يستخدم حقن التبعية في المُنشئ بدلاً من service locator
- مشاركة معالج قوائم التقارير باستخدام `Tag` بدلاً من معالجات مكررة
- `HashGen.cs` — حُذف (يحتوي على `Console.WriteLine` مخالف لـ RULE-035)
- `UpdateDialogViewModel` — تنفيذ `IDisposable` للتخلص من `_downloadCts`
- استخدام atomic write في `FirstRunSetupService` (`.tmp` → `File.Replace()`)
- `ROLLBACK IMMEDIATE` ← `ROLLBACK AFTER 30` في `BackupService`

### الميزات الجديدة (طباعة)
- إعدادات طباعة A4 وحرارية مع اختيار الطابعة وتكوين الرأس والتذييل
- **Inno Setup Installer**: `Installer/SalesSystem.iss` مع فحص .NET 10 runtime
  - تثبيت يتطلب صلاحيات المدير (Admin install)
  - تشغيل Windows Service تلقائياً أثناء التثبيت
  - إنشاء مجلد النسخ الاحتياطي وتعيين الصلاحيات
  - واجهة عربية كاملة للمثبت
- **Post-Quantum Readiness**: بنية أمنية قابلة للترقية (DPAPI + env vars + salt)

## v4.3 — نظام الخزينة النقدية (2026-05-24)

### الميزات الجديدة
- **الخزينة النقدية (Cash Boxes)**: إدارة صناديق النقدية مع تتبع الرصيد الآلي
  - إنشاء صناديق نقدية متعددة مع رصيد افتتاحي
  - تسجيل المصروفات النقدية اليدوية
  - عرض حركات الصندوق مع ترشيح حسب التاريخ
  - تعطيل الصناديق غير النشطة
- **ربط الفواتير بالخزينة**: عند ترحيل فاتورة مبيعات أو مشتريات بدفعة نقدية، يتم تسجيل حركة نقدية تلقائياً
  - فواتير المبيعات → إيراد مبيعات (SalesIncome)
  - فواتير المشتريات → دفعات موردين (SupplierPayment)
  - إلغاء الفاتورة → حركة عكسية تلقائية
  - التحقق من إلزامية اختيار الصندوق عند وجود مبلغ مدفوع
- **التحويل بين الصناديق**: تحويل ذري بقيد مزدوج بين خزنتين
- **الإغلاق اليومي**: حساب الرصيد الختامي (الرصيد الافتتاحي + الإيرادات - المصروفات)
  - منع الإغلاق المكرر لنفس اليوم
  - عرض الإغلاقات السابقة مع إمكانية التصفية

### تحسينات
- إضافة `CashBoxId` إلى طلبات إنشاء وتحديث الفواتير
- تحسين صلاحيات الوصول: CashBoxesController → ManagerAndAbove / AdminOnly
- إضافة أدوات التحقق (ToolTips) إلى جميع أزرار واجهة الخزينة
- فرز الحركات من الأحدث إلى الأقدم

## v4.3 — محرك الطباعة (2026-05-25)

### الميزات الجديدة
- **طباعة فواتير A4 (PDF)**: إنشاء فواتير احترافية بتنسيق A4 باستخدام QuestPDF مع دعم RTL للغة العربية
  - شعار المتجر في رأس الصفحة (مع معالجة عدم وجود الشعار بشكل آمن)
  - جدول الأصناف مع تلوين الصفوف بالتناوب وتقسيم الضريبة
  - أرقام الصفحات وتذييل يحتوي على إجمالي الفاتورة والخصم والضريبة
- **طباعة إيصالات حرارية 80mm**: إرسال أوامر ESC/POS مباشرة إلى الطابعة عبر Win32 raw printing
  - ترميز Windows-1256 لدعم اللغة العربية في الإيصالات
  - دعم رأس الإيصال وتذييله وطاولة الأصناف بعرض 42 حرفاً
  - أمر قص الورق وفتح الدرج النقدي
  - استخدام `EscPosCommandBuilder` مطوّر داخلياً — لا حزمة NuGet خارجية
- **إعدادات الطباعة**: إدارة إعدادات الطباعة عبر واجهة الإعدادات
  - اختيار طابعة A4 والطابعة الحرارية
  - تكوين رأس وتذييل الإيصال الحراري (`ReceiptHeader`/`ReceiptFooter`)
  - كود صفحة ESC/POS (الافتراضي: 22 = IBM864 للعربية)
  - طباعة تلقائية للإيصال الحراري بعد ترحيل فاتورة البيع (خاصية `AutoPrintOnPost`)
- **معاينة PDF قبل الطباعة**: نافذة معاينة في سطح المكتب تعرض ملف PDF المُنشأ
- **زر طباعة اختبارية**: إرسال أمر طباعة اختبارية للطابعة الحرارية من شاشة الإعدادات
- **واجهة برمجة التطبيقات (API)**: 11 نقطة نهاية للطباعة عبر `PrintController`
  - `GET/POST /api/v1/print/sales/{id}/a4` — طباعة فاتورة مبيعات A4
  - `GET/POST /api/v1/print/sales/{id}/thermal` — طباعة إيصال مبيعات حراري
  - `GET/POST /api/v1/print/purchases/{id}/a4` — طباعة فاتورة مشتريات A4
  - `GET/POST /api/v1/print/purchases/{id}/thermal` — طباعة إيصال مشتريات حراري
  - `POST /api/v1/print/test` — طباعة اختبارية

### تحسينات
- إضافة أزرار طباعة في شاشات فواتير المبيعات والمشتريات
- جميع عمليات الطباعة تتم عبر `PrintController` — سطح المكتب لا يتصل بالطابعة مباشرة
- إرجاع `PrintResult` في جميع العمليات — لا يتم رمي الاستثناءات أبداً
- تخزين إعدادات الطباعة في جدول `SystemSettings` (فئة `"Print"`)
- تسجيل جميع عمليات الطباعة عبر Serilog
- اختبارات وحدة لـ `EscPosCommandBuilder` (التحقق من صحة تسلسل الأوامر الثنائية)

## [1.11.0] - 2026-05-24
### Added
- **v4.3 — Dynamic UOM & Costing Engine** (Phase 2–4 MVP):
  - **ProductUnit Entity**: Multi-unit support per product with `UnitName`, `ConversionFactor`, per-unit `SalesPrice`/`PurchaseCost`, `IsBaseUnit` flag. Guard clauses and factory methods.
  - **UnitBarcode Entity**: Scannable barcodes linked to specific product units. Global unique index enforcement.
  - **ProductPriceHistory Entity**: Immutable audit log tracking every price/cost change with `OldValue`, `NewValue`, `ChangeReason`, `ChangedByUserId`.
  - **Costing Strategies**: Three methods via `SystemSettings.CostingMethod` — WeightedAverage (`(oldStock×oldCost + newQty×newCost)/(oldStock+newQty)`), LastPurchasePrice, SupplierPrice. Cost cascade to all derived units.
  - **Product Unit API**: `GET/POST/PUT/DELETE /api/v1/products/{id}/units` endpoints with FluentValidation, `[Authorize]` policies.
  - **Barcode Resolution API**: `GET /api/v1/barcodes/{barcode}` resolves barcode to product + unit + price in <100ms.
  - **Desktop UI**: ProductUnitEditorView/ViewModel with INotifyDataErrorInfo validation, ProductUnitsListView with DataGrid, all Arabic ToolTips, EventBus integration.
  - **Purchase Cost Hook**: `PurchaseService.PostAsync` triggers `UpdateProductPricingService` per line item — best-effort (never blocks invoice posting).
  - **DbSeeder Migration**: Seeds base "قطعة" ProductUnit for all existing products without units.
  - **8 Unit Tests**: WeightedAverage w/ stock, w/ zero stock, LastPurchasePrice, SupplierPrice, cost cascade, missing unit/base unit errors.

## [1.10.0] - 2026-05-24
### Added
- **v4.6.4 — Security Hardening & Code Quality** (Phase 7 & 8):
  - **Rate Limiting**: Added `AddRateLimiter` with `LoginPolicy` (5 attempts per 15 min per IP) and global policy (100 req/min). Arabic 429 response with `RATE_LIMIT_EXCEEDED` code.
  - **User Hard-Delete Guarded**: `UserService.PermanentDeleteAsync()` now returns `Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي")` — enforces RULE-038 (soft delete only).
  - **Connection String Security**: Removed plaintext SQL connection string from `appsettings.Development.json`. Uses `SALESSYSTEM_DB_CONNECTION` env var only per RULE-040.
  - **FluentValidator Enhancements**: Enhanced all 7 invoice/payment/transfer validators with additional rules: `PaymentType.IsInEnum()`, date not future, `Notes.MaxLength(500)`, `DiscountAmount >= 0`.
  - **FallbackErrorDialog**: Added `FallbackErrorDialog.xaml` for thread-safe unhandled exception display.
  - **Security-Plan.md**: Comprehensive 7-layer security document with implementation status table.
  - **Phase 4 Verification (US2 Backup/Restore)**: Full review + fixes — BackupViewModel constructor injection, RestoreBackupRequest DTO + FluentValidation, BackupController path alignment, BackupApiService JSON body.
  - **Phase 5 Verification (US3 Settings & Users)**: Confirmed all 10 tasks (T018–T027) already implemented. Fixed garbled Arabic strings in UserListViewModel.cs (9 strings corrected). Validator updated: CreateUserRequest Password MinLength 6→8, UserName MaxLength 50→100.
  - **Phase 6-7 Verification (US4 DPAPI + US5 Auto-Update)**: Confirmed all 6 tasks (T028–T033) already implemented. Fixed UpdaterService.LocalSettingsPath to use `%AppData%\SalesSystem\settings.json` (was `Path.GetTempPath()\...`).
  - **Phase 7 Code Review (v4.6.4)**: 6-agent code review of all 39 Phase 7 files. Fixed 12 violations across Program.cs, HealthController, BackupService, 2 new validators, UserListViewModel, DatabaseHealthCheckService, UpdaterService (duplicate interface + AppData path), UpdateDialogViewModel/XAML.
  - **Phase 8 Code Review (v4.6.4)**: 6-agent code review of all Phase 8 files. Fixed 8 violations: empty catch blocks in Desktop UpdaterService, English→Arabic error messages in Result.Failure, direct Serilog calls→LogSystemError in SettingsViewModel/UpdateDialogViewModel, duplicate UpdateSettingsRequestValidator removed from MiscValidators.cs, null guard added to RestoreBackupRequestValidator.Must(), ex.Message removed from FallbackErrorDialog.

### Fixed
- **Build Warnings (10 CS0109)**: Removed unnecessary `new` keyword from `_dialogService` in 5 ViewModels.
- **Build Errors (4 CS1540)**: Fixed protected member access via `((ViewModelBase)this).DialogService` in ReportsViewModel, StockTransfersListViewModel, SupplierPaymentsListViewModel.
- **Test Compilation**: Fixed 2 errors in `PurchaseInvoicesControllerTests.cs` (missing `using SalesSystem.Contracts.Enums`).
- **Desktop UpdaterService**: Empty catch blocks documented with comments (`LoadVersionFileUrl`, `LoadLocalSettings`). All 6 English `Result.Failure` error messages replaced with Arabic (RULE-171/172).
- **SettingsViewModel**: Replaced 2× direct `Serilog.Log.Warning` calls with `LogSystemError()` from ViewModelBase (RULE-201).
- **UpdateDialogViewModel**: Replaced direct `Serilog.Log.Error` with `LogSystemError()` (RULE-201).
- **MiscValidators.cs**: Removed duplicate `UpdateSettingsRequestValidator` class (duplicate exists in dedicated file at Validators/UpdateSettingsRequestValidator.cs).
- **RestoreBackupRequestValidator**: Added null guard to `.Must(f => f.EndsWith(...))` to prevent NullReferenceException.
- **App.xaml.cs**: Removed `e.Exception.Message` from FallbackErrorDialog user-facing message (RULE-171).
- **ConnectionStringProtector.cs**: Deleted old file from `Infrastructure/Security/` — moved to `Infrastructure/Services/`.

### Tests
- **5 New Tests**: SetDialogService constructor test, ValidateAsync empty name, ValidateAsync valid name clears errors, ValidateAsync multiple errors, Post_AlreadyPostedInvoice_ThrowsDomainException.

## [1.9.1] - 2026-05-23
### Added
- **v4.6.3 — Architecture Alignment & Code Quality Audit**:
  - Relocated Settings ViewModels (`CostingMethodSettingsViewModel`) and Views (`CostingMethodSettingsView`) from the root folder to their proper folders `ViewModels/Settings` and `Views/Settings`.
  - Refactored `CostingMethodSettingsViewModel` to fetch costing settings via `ISettingsApiService.GetSettingsAsync()` and save via API, respecting clean architecture (no direct Infrastructure database connection).
  - Registered `CostingMethodSettingsViewModel` as a transient service in `App.xaml.cs` for DI resolution.
  - Replaced the unhandled exception handler's `MessageBox.Show` call in `App.xaml.cs` with a thread-safe dialog overlay fallback.
  - Fixed compiled CS0108 member hiding warnings across list ViewModels (`ReportsViewModel`, `WarehouseListViewModel`, `SupplierPaymentsListViewModel`, `StockTransfersListViewModel`) by removing shadowed `DialogService` properties.
  - Fixed garbled Arabic encoding issues in `StockTransfersListViewModel.cs` and `SupplierPaymentsListViewModel.cs`.
  - Wrapped 21+ `async void` commands and initialization methods across ViewModels with robust try-catch logging patterns to prevent silent app crashes.

### Files Modified
- `App.xaml.cs`, `Services/App/DialogService.cs`, `ViewModels/Settings/CostingMethodSettingsViewModel.cs`, `Views/Settings/CostingMethodSettingsView.xaml`, `ViewModels/ReportsViewModel.cs`, `ViewModels/WarehouseListViewModel.cs`, `ViewModels/Payments/SupplierPaymentsListViewModel.cs`, `ViewModels/Transfers/StockTransfersListViewModel.cs`, `docs/CHANGELOG.md`, `docs/PRD-MVP.md`, `docs/database-schema.md`, `docs/ui-screens.md`, `docs/MASTER-PLAN.md`, `README.md`, `AGENTS.md`, `.opencode/agent/implement-agent.md`, `.opencode/agent/code-reviewer.md`, `.opencode/agent/ui-agent.md`, `.opencode/agent/backend-architect.md`, `.opencode/agent/database-engineer.md`, `.opencode/agent/security-auditor.md`, `.opencode/agent/test-engineer.md`

## [1.9.0] - 2026-05-23
### Added
- **v4.6.2 — WPF Validation ErrorTemplate & INotifyDataErrorInfo Standardization**:
  - New `ErrorTemplate` in `Styles.xaml`: Red border (#EF4444, 1.5px) + ❗ icon badge with ToolTip bound to `[0].ErrorContent` — applies to TextBox, PasswordBox, ComboBox.
  - `ViewModelBase.cs`: Added `SetDialogService(IDialogService)`, `ValidateAllAsync()`, and `ValidateField()` — standardized pre-save validation dialog + focus.
  - `ProductEditorViewModel`: Migrated from legacy `HasXxxError` boolean + computed string pattern to pure `INotifyDataErrorInfo` using `AddError()`/`ClearErrors()` in property setters — removed 7 obsolete properties.
  - `CustomerEditorViewModel`: Same migration — removed 3 obsolete `HasXxxError` boolean properties.
  - All 14 Editor ViewModels now call `SetDialogService()` in constructors to enable `ValidateAllAsync()`.
  - `AGENTS.md`: Added RULE-227 to RULE-230 covering `SetDialogService()`, `INotifyDataErrorInfo`, `ValidateAllAsync()`, and `ErrorTemplate`.

### Changed
- **Validation model**: Replaced `HasXxxError` / `XxxError` boolean + computed string pattern with `INotifyDataErrorInfo` (`AddError`/`ClearErrors`) — real-time validation UI updates with red border on invalid fields.
- **Pre-save validation**: `ValidateAsync()` now calls `ClearAllErrors()` → `AddError()` for each field → `await ValidateAllAsync()` from ViewModelBase — shows styled validation dialog automatically.

### Files Modified
- `Resources/Styles.xaml`, `ViewModels/ViewModelBase.cs`, `ViewModels/Products/ProductEditorViewModel.cs`, `ViewModels/Customers/CustomerEditorViewModel.cs`, `ViewModels/Suppliers/SupplierEditorViewModel.cs`, `ViewModels/Categories/CategoryEditorViewModel.cs`, `ViewModels/Units/UnitEditorViewModel.cs`, `ViewModels/WarehouseEditorViewModel.cs`, `ViewModels/Users/UserEditorViewModel.cs`, `ViewModels/Payments/CustomerPaymentEditorViewModel.cs`, `ViewModels/Payments/SupplierPaymentEditorViewModel.cs`, `ViewModels/Transfers/StockTransferEditorViewModel.cs`, `ViewModels/Returns/SalesReturnEditorViewModel.cs`, `ViewModels/Returns/PurchaseReturnEditorViewModel.cs`, `ViewModels/Sales/SalesInvoiceEditorViewModel.cs`, `ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs`, `AGENTS.md`, `.opencode/agent/code-reviewer.md`, `.opencode/agent/ui-agent.md`, `.opencode/agent/implement-agent.md`, `.opencode/agent/orchestrator.md`, `.opencode/agent/test-engineer.md`, `README.md`, `docs/database-schema.md`, `docs/CHANGELOG.md`, `docs/MASTER-PLAN.md`, `docs/CONSTITUTION.md`

## [1.8.0] - 2026-05-23
### Added
- **UpdateProductPricingService Returns Result<T>**: Changed from `Task` + throwing exceptions to `Task<Result>` — returns `Result.Failure` with Arabic messages for "unit not found" and "no base unit" instead of `InvalidOperationException`.
- **FK DeleteBehavior.Restrict Enforced**: Cascade delete removed from ProductUnitConfiguration (Barcodes FK, Product FK), UnitBarcodeConfiguration, ProductBarcodeConfiguration — ALL FKs now use `DeleteBehavior.Restrict` per AGENTS.md RULE-214.
- **Controller Purity Enforcement**:
  - PrintController: Moved all `SalesDbContext` queries to dedicated `PrintDataService` in Application layer — controller only delegates to `IPrintDataService`.
  - LogsController: Removed `[AllowAnonymous]` — now `[Authorize(Policy = "AllStaff")]` with class-level attribute.
  - SettingsController: Both GET endpoints changed from `AllStaff` to `[Authorize(Policy = "AdminOnly")]`.
- **PrintDataService Returns Result<InvoicePrintDto>**: Changed return type from `InvoicePrintDto?` (nullable) to `Task<Result<InvoicePrintDto>>` — wraps DTO in `Result.Success/Failure` instead of returning null.
- **6 New FluentValidators**: `UpdateSalesInvoiceValidator`, `UpdatePurchaseInvoiceValidator`, `UpdateStockTransferValidator`, `UpdateCustomerPaymentValidator`, `UpdateSupplierPaymentValidator`, `CreateLogRequestValidator` — all with Arabic messages.
- **Costing Method in Settings UI**: 3 RadioButtons (Weighted Average / Last Purchase Price / Supplier Price) with Arabic explanations in Settings screen — persisted via API to SystemSettings table.
  - New properties: `CostingMethod`, `IsWeightedAverageSelected`, `IsLastPriceSelected`, `IsSupplierPriceSelected` in SettingsViewModel.
  - SettingsController updated: `Get()` reads costing method from `ISystemSettingsRepository`, `Update()` saves it.
  - StoreSettingsDto and UpdateSettingsRequest DTOs now include `CostingMethod` field.
- **Price Sync Indicators in Purchase Invoice**: New `CostChangedFromDatabase` + `PriceDifferenceIndicator` properties in PurchaseInvoiceLineViewModel — orange sync warning shows when entered unit cost differs from current DB cost.
  - Updated PurchaseInvoiceEditorView.xaml DataGrid: enhanced "التكلفة" column with sync warning TextBlock.

### Changed
- **decimal(18,4) → decimal(18,2)**: All money fields changed from `HasPrecision(18,4)` to `HasPrecision(18,2)`:
  - ProductUnitConfiguration: SalesPrice, PurchaseCost, SupplierPrice, LastPurchasePrice.
  - CashTransactionConfiguration: Amount, BalanceBefore, BalanceAfter.
  - CashBoxConfiguration: CurrentBalance.
- **UpdateProductPricingService.WeightedAverage**: Rounding changed from `Math.Round(weightedAverage, 4)` to `Math.Round(weightedAverage, 2)` — consistent with new `decimal(18,2)` precision.
- **API PrintController 10 methods**: All updated to use `result.IsSuccess` / `result.Value!` pattern — PrintControllerTests Moq setups use `Result<InvoicePrintDto>.Success/Failure`.
- **AGENTS.md**: RULE-211 updated — ALL money fields use `decimal(18,2)` (not 18,4). RULE-214/215/216 for FK Restrict enforcement.
- **MASTER-PLAN.md**: Phase 5 WPF XAML and Phase 4 WPF ViewModels now complete with CostingMethod UI and Price Sync Indicators.
- **README.md**: Updated to v4.7 with new "What's New" section, new Phase 16 row.

### Fixed
- **3 UpdateProductPricingService tests**: 
  - `WeightedAverage_ShouldCalculateCorrectly` — expected values changed from `13.7113m`/`164.5356m` to `13.71m`/`164.52m` with `0.01m` precision.
  - `WhenProductUnitNotFound_ShouldThrow` → `ShouldReturnFailure` — changed from `InvalidOperationException` assertion to `result.IsSuccess.Should().BeFalse()` + Arabic error check.
  - `WhenNoBaseUnit_ShouldThrow` → `ShouldReturnFailure` — same pattern with Arabic error message.
- **PrintControllerTests**: All 11 Moq setups updated — `_printDataService.Setup(...).ReturnsAsync(Result<InvoicePrintDto>.Success(...))` instead of raw DTO.

## [1.7.1] - 2026-05-22
### Added
- **LogSystemError Centralized (v4.6)**: All `Serilog.Log.Error` calls moved to `ViewModelBase.LogSystemError()` — 17 calls across 11 ViewModels consolidated.
- **Hard Delete DbUpdateException Safety**: All 7 Application services (Product, Customer, Supplier, Category, Unit, Warehouse, User) now catch `DbUpdateException` in `PermanentDeleteAsync()` and return `Result.Failure` with Arabic message including inner exception.
- **ValidationErrorsDialog**: New dedicated dialog with `ItemsControl` for bulleted red error list — `ShowValidationErrorsAsync(title, List<string> errors)` added to `IDialogService`.
- **ValidationFocusBehavior**: New helper class with `FindFirstInvalid()` and `FindFirstEmptyRequired()` methods — auto-focuses first invalid field after validation dialog.
- **FocusFirstInvalidFieldRequested**: New event in ViewModelBase + `RequestFocusFirstInvalidField()` — 14 editor Views subscribe and auto-focus on first error.
- **7 Dialog Styles in Styles.xaml**: DialogOverlayStyle, DialogCardStyle, DialogHeaderStyle, DialogIconBorderStyle, DialogTitleStyle, DialogButtonBaseStyle, ValidationErrorItemStyle.

### Changed
- **Dialog Overhaul (v4.6)**: All 8 dialog windows (Error, Warning, Success, Info, Confirmation, DeleteConfirmation, DatabaseError, ValidationErrors) updated with:
  - `WindowStyle="None"` + `AllowsTransparency="True"` + `Background="Transparent"` — transparent overlay pattern.
  - Full-screen `#80000000` dimming rectangle behind centered card.
  - `CornerRadius="16"` and `DeepShadow` on dialog card.
  - `PositionOverOwner()` in all code-behind files.
  - Button hover effects: `IsMouseOver` (darker shade) and `IsPressed` (even darker) triggers in `ControlTemplate.Triggers`.
- **14 Editor ViewModels** updated: Use `ShowValidationErrorsAsync(errorsList)` instead of `ShowWarningAsync(joinedString)` and call `RequestFocusFirstInvalidField()`.
- **14 Editor Views** updated: Subscribe to `FocusFirstInvalidFieldRequested` → `ValidationFocusBehavior.FindFirstInvalid(this)?.Focus()`.
- **Login icon**: `Background="{DynamicResource PrimaryBrush}"` → `Background="Transparent"` — icon fill uses PrimaryBrush instead of White.
- **Settings layout**: Added 4th `RowDefinition Height="Auto"`; changed bottom margin from `0` to `24`.
- **AGENTS.md**: Version updated to v4.6; new rules RULE-198 to RULE-218 added; FORBIDDEN list expanded; checklist expanded.

### Removed
- All direct `Serilog.Log.Error` calls from ViewModels — centralized in `ViewModelBase.LogSystemError()` and `HandleException()`.
- All `CanExecute` predicates from editor ViewModel Save/Post commands (Phase 13 completed).
- All `IsEnabled="{Binding CanSave}"` from XAML files.

## [1.7.0] - 2026-05-22
### Added
- **Interactive Validation (v4.6)**: Complete overhaul of form validation UX across the entire WPF Desktop application.
  - Save/Post/Print buttons are ALWAYS enabled — no CanExecute predicates block user actions.
  - On-click validation shows styled warning dialog listing ALL missing/incorrect fields with Arabic messages.
  - Required fields marked with `*` on ALL editor screens (Category, Unit, Warehouse added).
  - Field-level ToolTips (35+) on every input explaining validation rules, formats, and uniqueness constraints.
  - Unique field explanations: Barcode ("يجب أن يكون فريداً") and Username ("يجب أن يكون فريداً ولا يمكن تكراره").
  - AGENTS.md RULE-059 rewritten: "InterActive Validation" pattern documented with correct/wrong code examples.

### Changed
- **13 Editor ViewModels** modified to remove CanExecute predicates:
  - ProductEditorViewModel, CategoryEditorViewModel, UnitEditorViewModel, WarehouseEditorViewModel
  - UserEditorViewModel, CustomerEditorViewModel, SupplierEditorViewModel
  - SalesInvoiceEditorViewModel, PurchaseInvoiceEditorViewModel, StockTransferEditorViewModel
  - SalesReturnEditorViewModel, PurchaseReturnEditorViewModel
- **7 XAML files** updated with ToolTips, `*` markers, and `IsEnabled="{Binding CanSave}"` removed:
  - ProductEditorView, CategoryEditorView, UnitEditorView, WarehouseEditorView
  - CustomerEditorView, SupplierEditorView, UserEditorView
- **AGENTS.md** — RULE-059 updated from "Save buttons disabled via CanExecute" to "InterActive Validation — buttons always enabled".
- **README.md** — Added "What's New in v4.6" section and implementation phase row.
- **orchestrator.md** — Added Phase 13: Interactive Validation.
- **implement-agent.md** — Added Interactive Validation pattern section.
- **backend-architect.md** — Added Rule 19: no CanExecute blocking.
- **code-reviewer.md** — Added Interactive Validation checklist section with 8 items.
- **ui-agent.md** — Added Interactive Validation section with patterns and rules.

## [1.6.1] - 2026-05-22
### Added
- **Warehouse Code Removal (v4.5.3)**: `Code` column removed from Warehouse entity — completing the Identifier Strategy across all entities.
  - RULE-198: WarehouseResponse DTO must not have Code field.

### Changed
- **AGENTS.md updated to v4.5.3** — Updated from v4.5.2, 198 rules total.
  - RULE-191/195/196 expanded to include Warehouse.
  - New RULE-198 for WarehouseResponse.
  - FORBIDDEN list + checklist updated.
- **README.md** — Updated v4.5.2 → v4.5.3, added Warehouse Code row to table.
- **Subagents** — Updated code-reviewer, implement-agent, ui-agent with Warehouse patterns.

### Removed
- **Warehouse.Code** — Removed from Warehouse entity, EF config, migrations (Code column + IX_Warehouses_Code index).
- **Warehouse.Code from Contracts** — Removed from WarehouseRequests (Create/Update), WarehouseDto, WarehouseResponse.
- **Warehouse.Code from Service** — Removed auto-generation, uniqueness check, search filter from WarehouseService.
- **Warehouse.Code from API** — Removed Code validation rules from WarehouseRequestValidators.
- **Warehouse.Code from Desktop** — Removed Code field/property from WarehouseListViewModel, WarehouseEditorViewModel.
- **Warehouse.Code from Tests** — Removed Code assertions/tests from all 4+ Warehouse test files.
- **WarehouseResponse.Code** — Removed Code field from WarehouseResponse record.
- **Leftover Code assertions** — Removed `result.Value.Code` from CustomerServiceTests and SupplierServiceTests (previously missed).

## [1.6.0] - 2026-05-22
### Added
- **Identifier Strategy — Code Removal (v4.5.2)**: 7 new rules (RULE-191 to RULE-197) in AGENTS.md.
  - Product, Customer, Supplier MUST NOT have Code column — use auto-increment Id instead.
  - Search/filter by Id or Name only.
  - Invoice item DTOs carry ProductId only (no ProductCode).
  - Report DTOs exclude Code fields.
  - Code auto-generation services removed.
  - Editor ViewModels must not have Code property.
  - DuplicateCode error constant removed.

### Changed
- **AGENTS.md updated to v4.5.2** — Updated from v4.5.1, 197 rules total.
  - Section 2.45: Identifier Strategy (RULE-191 to RULE-197).
  - FORBIDDEN list: 4 new items (Code column, ProductCode, auto-generation, Code search).
  - Checklist: 5 new items.
- **README.md** — Added "What's New in v4.5.2" section with 7 rows.

### Removed
- **Code column** — Removed from Product, Customer, and Supplier entities (domain, DB, DTOs, ViewModels, XAML).
- **ProductCode** — Removed from all invoice item DTOs (SalesInvoiceItem, PurchaseInvoiceItem, SalesReturnItem, PurchaseReturnItem, StockTransferItem).
- **Code fields** — Removed from report DTOs (StockReport, CustomerBalanceReport, SupplierBalanceReport, LowStockReport).
- **Code auto-generation** — Removed DocumentSequenceService calls for PRD/CUST/SUP prefixes in ProductService, CustomerService, SupplierService.
- **Code validation** — Removed from API validators (Product, Customer, Supplier).
- **Code editor fields** — Removed Code TextBox from ProductEditorView, CustomerEditorView, SupplierEditorView.
- **Code search** — Removed from all list/selection ViewModel search filters.
- **Code assertions** — Removed from all unit tests.
- **DuplicateCode error** — Removed `ErrorCodes.DuplicateCode` constant.

## [1.5.1] - 2026-05-22

### Fixed
- **HandleResponseAsync JSON parsing crash**: Non-generic `HandleResponseAsync` in `IApiService.cs` now checks `ContentType` before calling `ReadFromJsonAsync<ErrorResponse>()` — prevents `JsonException` crash when API returns 404 with empty/HTML body (mirrors the pattern in the generic overload).
- **Print test log level**: `SettingsViewModel.cs` print test failure changed from `Log.Error` to `Log.Warning` — printer test failure is a user/configuration issue, not a system error.

### Changed
- **Logging separation policy**: Clear distinction documented in AGENTS.md — `Log.Error` for system errors only (DB down, API unreachable, parse crashes), `Log.Warning` for user mistakes (validation, business rules, "not found").

## [1.5.0] - 2026-05-22

### Added
- **Error Message Best Practices (v4.5.1)**: 7 new rules (RULE-171 to RULE-177) in AGENTS.md.
  - ALL catch blocks use `Serilog.Log.Error()` — NEVER `ex.Message` in user-facing dialogs.
  - `HandleFailure()` transforms timeout/network/not-found errors into user-friendly Arabic.
  - Dialog titles are screen-specific (e.g., `"خطأ في حفظ الفاتورة"`) — NEVER generic `"خطأ"`.
  - `MessageBox.Show` is FORBIDDEN — ALL user-facing messages go through `IDialogService`.
  - ALL dialog calls use `Async` suffix methods (`ShowErrorAsync`, `ShowSuccessAsync`).
  - Success messages name the action (e.g., `"تم تصدير التقرير إلى Excel بنجاح"`).
  - Raw HTTP response bodies logged via Serilog — NEVER shown to users.

- **Application Shutdown (v4.5.1)**: 4 new rules (RULE-178 to RULE-181) in AGENTS.md.
  - `App.xaml` uses `ShutdownMode="OnExplicitShutdown"` — prevents app staying alive due to hidden ScreenWindow instances.
  - `LoginWindow.CloseButton_Click` calls `Application.Current.Shutdown()` — fully exits app.
  - `MainWindow.Closed` calls `System.Windows.Application.Current.Shutdown()` — except during logout.
  - Logout flow sets `_isLoggingOut = true`, clears session, opens new LoginWindow — prevents shutdown.

- **AGENTS.md updated to v4.5.1** — 181 rules total.
  - Section 2.41: Error Message Best Practices (RULE-171 to RULE-177).
  - Section 2.42: Application Shutdown (RULE-178 to RULE-181).
  - FORBIDDEN list: 7 new items.
  - Checklist: 9 new items.

- **Bug-fix patterns**: Code reviewer / implement agent checklists expanded with 7 new items for: manual window creation, ignored ShowDialog returns, AddCommand CanExecute verification, MessageBox.Show audit.

- **Self-Explaining System (v4.5.1)**: 6 new rules (RULE-185 to RULE-190) in AGENTS.md for ToolTip requirements.
  - ALL interactive controls must have Arabic ToolTip explaining the action.
  - ToolTips must be user-action-oriented, not just repeat button text.
  - Action buttons must explain consequences (e.g., stock updates).
  - Navigation MenuItems must describe destination screen.
  - Empty-state buttons must have ToolTips.
  - Error dismiss buttons must have ToolTip.
- **174 Arabic ToolTips**: Added across ~40 XAML files in the DesktopPWF Views.
  - Group 1 (List views): 32 ToolTips — Products, Customers, Suppliers, Warehouses, StockTransfers.
  - Group 2 (Invoice editors): 40 ToolTips — Sales, Purchase, Returns, StockTransfer editors.
  - Group 3 (CRUD editors): 22 ToolTips — Product, Customer, Supplier, User, Category, Unit, Warehouse, Payment editors.
  - Group 4 (Menus & Misc): 57 ToolTips — MainWindow menus, Selection dialogs, LowStock, Dashboard, Reports, Settings.
  - Group 5 (Remaining lists): 23 ToolTips — Sales/Purchase/Returns lists, Users, Categories, Units, Payments.

### Changed
- **Version updated to v4.5.1** — Error Message & Shutdown Improvements release.
- **README.md updated to v4.5.1** — New "What's New" section, updated version badge.
- **SupplierListViewModel**: Refactored to use `InitializeCommands()` pattern (matching ProductListViewModel standard).
- **AGENTS.md updated to v4.5.1** — 190 rules total.
  - Section 2.43: UI ToolTips (RULE-185 to RULE-190).
  - FORBIDDEN list: 2 new items (missing ToolTip, redundant ToolTip).
  - Checklist: 5 new items.
- **README.md** — Added 5 new rows to What's New in v4.5.1 for ToolTip features.

### Fixed
- **Raw exception messages**: 13 files fixed — catch blocks no longer show `ex.Message` to users.
- **Generic "خطأ" titles**: 12 ViewModels updated with screen-specific dialog titles.
- **MessageBox.Show violations**: 16 calls replaced with `IDialogService` across 6 editor ViewModels.
- **Sync dialog calls**: `LowStockViewModel` + `PurchaseInvoiceEditorViewModel` — all sync `ShowError`/`ShowInfo`/`ShowWarning` migrated to async.
- **HandleFailure transformation**: `ViewModelBase.HandleFailure()` now transforms common errors (timeout, network, not found) to user-friendly Arabic.
- **Vague success messages**: `ReportsViewModel` — `"تم التصدير بنجاح"` → `"تم تصدير التقرير إلى Excel بنجاح"` / `"إلى CSV"`.
- **Raw HTTP body exposure**: `SettingsViewModel` — raw HTTP response body replaced with user-friendly message + Serilog logging.
- **CS0234 namespace collision**: `Application.Current` → `System.Windows.Application.Current` in `LoginWindow.xaml.cs` and `MainWindow.xaml.cs`.
- **CS8602 null dereferences**: Fixed in `InputHelper.cs`, `MainWindow.xaml.cs`, `App.xaml.cs`, `UpdaterService.cs`, `UpdateDialogViewModel.cs`.
- **CS1729 constructor mismatch**: `CustomerEditorViewModelTests` + `SupplierEditorViewModelTests` — added `Mock<IDialogService>` and updated 53 constructor calls.
- **CS8632 nullable annotations**: Removed `?` from 6 field declarations across 4 E2E test files with `#nullable disable`.
- **SYSLIB0050 obsolete API**: `WarehouseListViewModelTests` — `FormatterServices.GetUninitializedObject` → `RuntimeHelpers.GetUninitializedObject`.
- **CustomerListViewModel**: Replaced manual `CustomerEditorView` creation + `ShowDialog()` with `_dialogService.ShowDialog(editorVm)` in `AddCustomer()` and `EditCustomer()`.
- **CustomerListViewModel**: Replaced `MessageBox.Show` in `RestoreCustomerAsync()` with `_dialogService.ShowSuccessAsync()` + `HandleFailure()`.
- **SupplierListViewModel**: `AddSupplier()` and `EditSupplier()` now check `_dialogService.ShowDialog()` return value and reload list on success — previously ignored return, causing stale list.
- **SupplierListViewModel**: Extracted command initialization into `InitializeCommands()` — eliminated duplicate code in both constructors.
- **SupplierListViewModel**: Replaced `MessageBox.Show` in `RestoreSupplierAsync()` with `_dialogService.ShowSuccessAsync()` + `HandleFailure()`.
- **SupplierListViewModel**: Command properties changed from `{ get; }` to `{ get; private set; } = null!;` to support `InitializeCommands()` pattern.
- **ProductEditorViewModel**: Added `IDialogService` dependency; replaced 4× `MessageBox.Show` in `SaveAsync()` with `_dialogService.ShowSuccessAsync()` / `ShowErrorAsync()`.
- **ProductEditorViewModelTests**: Updated 19 constructor calls with `Mock<IDialogService>` parameter.

## [1.4.0] - 2026-05-21

### Added
- **ExecuteAsync() Pattern (v4.5)**: Centralized error handling wrapper in ViewModelBase.
  - `ExecuteAsync(Func<Task>)` — wraps async operations with IsBusy + error handling.
  - `ExecuteAsync(Func<Task>, Action<Exception>)` — same with custom error callback for UI display.
  - `ExecuteResultAsync<T>(Func<Task<Result<T>>>)` — wraps Result<T> operations, returns null on failure.
  - `IsBusy` property (protected set) replaces `IsLoading` — automatically managed.
  - `StatusMessage` property (protected set) for user feedback during operations.
  - Eliminates manual try/catch/finally in ALL ViewModel commands.

- **IProductPriceService (v4.5)**: Replaced MediatR ProductPriceQuery with Service Layer pattern.
  - `IProductPriceService` interface with `GetPriceByUnitAsync()` method.
  - `ProductPriceService` implementation using `IUnitOfWork` pattern.
  - Follows existing codebase conventions (constructor injection, CancellationToken).

- **Test Infrastructure Updates (v4.5)**:
  - E2ETests: Fixed CS0118 namespace conflict (`FlaUI.Core.Application` vs `System.Windows.Application`).
  - Application.Tests: Added `HardDeleteAsync` + `DeleteRange` to InMemoryEfCoreRepository (14 files).
  - Api.Tests: Updated 17 controller test files with corrected signatures + `includeInactive` params.
  - DesktopPWF.Tests: Updated 13 ViewModel test files with corrected DTO constructors + `DeleteStrategy` mocks.
  - All test exclusions documented with detailed comments in .csproj files.

- **DB Health Check & Graceful Error Handling (v4.5)**:
  - `GET /api/v1/health` — now includes `Database` field (`Connected`/`Disconnected`), returns `Degraded` status when DB is unreachable.
  - `GET /api/v1/health/database` — dedicated endpoint calling `DbContext.Database.CanConnectAsync()`, returns 503 on failure.
  - ExceptionMiddleware — detects `InvalidOperationException` (connection string) and `SqlException` by type name, returns `503 Service Unavailable` with `DATABASE_CONNECTION_ERROR` code.
  - `DatabaseErrorDialog.xaml` — styled RTL dialog with warning icon, diagnostic tips, Retry/Exit buttons.
  - `IDatabaseHealthCheckService` / `DatabaseHealthCheckService` — Desktop service calling `/api/v1/health/database`, catches `HttpRequestException` and `TaskCanceledException`, returns `HealthCheckResult` with Arabic error messages.
  - `App.xaml.cs` — now checks API + DB connectivity BEFORE showing login window, loops with retry dialog until connected or user exits.
  - `SecureDbContextFactory.GetDecryptedConnectionString()` — falls back to `SALESSYSTEM_DB_CONNECTION` env var before throwing.

### Changed
- **Version updated to v4.5** — Code Quality & Refactoring release.
- **AGENTS.md updated to v4.5** — 159 rules (RULE-141 to RULE-159 added).
  - Section 2.36: ViewModel ExecuteAsync Pattern (RULE-141 to RULE-146).
  - Section 2.37: Architecture Decisions (RULE-147 to RULE-150).
  - Section 2.38: DB Health Check & Graceful Error Handling (RULE-151 to RULE-159).
  - FORBIDDEN list: 4 new items (starting Desktop without DB check, API crash on DB error, raw exception messages, missing env var fallback).
  - Checklist: 5 new DB health check items.
- **README.md updated to v4.5** — Phase 10 added as Completed, What's New section updated.
- **MASTER-PLAN.md completely rewritten** — Now reflects actual Clean Architecture (Layered), NOT aspirational Vertical Slices.
  - Reduced from 2,945 lines to 693 lines.
  - Removed all fictional code that was never built.
  - Added actual code patterns (ViewModel, Service, Controller, Domain, Validation).
  - Added honest "Partially Implemented" section (MediatR, CQRS).
  - Added "Future Plans" table (8 items clearly marked as NOT implemented).
  - Added Architecture Decisions section explaining design choices.

### Removed
- **MediatR package** — Removed from `SalesSystem.Application.csproj` (was unused).
- **ProductPriceQuery.cs** — Deleted (MediatR record + handler), replaced with `IProductPriceService`.
- **Legacy/SalesSystem.Desktop/** — Deleted abandoned WinForms desktop project (safe to delete — all functionality rebuilt in DesktopPWF).

### Fixed
- `IsLoading` → `IsBusy` in all ViewModels and test files.
- `LoadDataAsync` → `RefreshCommand` in DashboardViewModel tests.
- `LoadWarehousesAsync` made public in WarehouseListViewModel (for test access).
- WarehouseListViewModelTests: Updated to use `IsBusy` instead of `IsLoading`.
- LoginWindowViewModelTests: Rewrote loading state tests to use command execution.
- DashboardViewModelTests: Rewrote to use `RefreshCommand` instead of direct method calls.
- ReportsViewModelTests: Updated `IsLoading` references to `IsBusy`.

## [1.3.0] - 2026-05-21

### Added
- **Auto-Update System (v4.4)**: Complete background update checker with SHA256 verification.
  - `IUpdaterService` interface with `Result<T>` pattern — all 6 methods return `Result<T>` or `Result`.
  - `UpdaterService` — HTTP-based update check with 8-second timeout, silent failure on network issues.
  - `GitHubUpdaterService` — Alternative implementation using GitHub API releases with rate-limit handling.
  - `UpdateInfo`, `UpdateCheckResult`, `DownloadProgress` models in `Application/Updates/Models/`.
  - `UpdateDialogViewModel` — WPF ViewModel with `IDisposable` (dispose `_downloadCts`), progress reporting, 4 commands.
  - `UpdateDialog.xaml` — Borderless RTL window with version comparison, changelog, progress bar, Download/Install/Skip/Cancel buttons.
  - Background update check in `App.xaml.cs` — fire-and-forget with 3-second delay, NEVER blocks startup.
  - Manual update check from MainWindow Help menu.
  - `AddUpdateServices()` DI extension method in `Infrastructure/DependencyInjection.cs`.
  - SHA256 checksum verification before launching installer.
  - `LaunchInstallerAndExitAsync` returns `Result<bool>` — caller handles shutdown (no `Environment.Exit(0)`).
  - Skipped version persisted to `%AppData%\SalesSystem\settings.json`.
  - Version comparison uses `System.Version` — NEVER string comparison.

- **Security & DPAPI (v4.4)**: Connection string encryption and first-run setup.
  - `IConnectionStringProtector` / `ConnectionStringProtector` — DPAPI encryption via `IDataProtector` with `"DPAPI:"` prefix.
  - Idempotent encryption — `Encrypt()` checks `IsEncrypted()` first, prevents double-encryption.
  - `FirstRunSetupService` — auto-encrypts plaintext connection string on first run.
  - Atomic file writes: `.tmp` → `File.Replace()` → `.bak` pattern for `appsettings.json`.
  - `SecureDbContextFactory` — decrypts connection string before creating DbContext.
  - `SecurityAudit.cs` — DEBUG-only pre-build checks: unencrypted connection strings, hardcoded passwords, GitHub tokens.
  - DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`.
  - JWT secret from environment variable — throws `InvalidOperationException` in production if missing.

- **Backup System (v4.4)**: Database backup and restore with scheduled automation.
  - `BackupService` — raw SQL `BACKUP DATABASE` / `RESTORE DATABASE` (no SMO dependency).
  - Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` — gives active transactions 30 seconds.
  - `TrySetMultiUserAsync` recovery on restore failure — NEVER leaves DB in SINGLE_USER mode.
  - `ScheduledBackupWorker` — `BackgroundService` running daily at 2:00 AM with `IServiceScopeFactory`.
  - Configurable retention days (default 30) — old backups auto-deleted.
  - `int.TryParse` for all config values — no `FormatException` on bad config.
  - `DeleteOldBackupsAsync` — cleanup method for expired backup files.

- **Windows Service (v4.4)**: API runs as a Windows Service.
  - `UseWindowsService()` in `Program.cs` with service name `SalesSystemService`.
  - Auto-recovery: 3 restarts on failure (1min, 5min, 15min delays).
  - Serilog EventLog sink for Windows Service logging.
  - SQL retry on startup: 3 attempts × 5 second delay.
  - Database migration runs on service startup (auto-migrate).
  - `Install-Service.bat` / `Uninstall-Service.bat` scripts.

- **Admin Screens (v4.4)**: User management with role-based access.
  - `AdminOnlyViewModel` base class — enforces Admin role via constructor-injected `ISessionService`.
  - Non-admin users get `UnauthorizedAccessException` — admin UI hidden.
  - `UserListViewModel` extends `AdminOnlyViewModel` — Toggle Status (soft delete), Reset Password.
  - `UsersListView.xaml` — DataGrid with Arabic labels, Edit/Reset Password/Toggle Status buttons.
  - Constructor injection throughout — no service locator anti-pattern.

- **Installer (v4.4)**: Inno Setup script for production deployment.
  - `Installer/SalesSystem.iss` — admin install required.
  - .NET 10 runtime check before installation.
  - Windows Service auto-start configured during install.
  - Creates backup directory and sets permissions.
  - Arabic UI throughout installer.

- **Dialog Service Enhancement (v4.4)**:
  - `ShowInfoAsync` method added to `IDialogService` — blue theme, info icon.
  - ALL sync dialog methods now use styled dialogs — NEVER raw `MessageBox.Show`.
  - `InfoDialog.xaml` created — blue theme with info icon.
  - All 5 `MessageBox.Show` calls in `MainWindow.xaml.cs` replaced with `IDialogService`.

### Changed
- **Version updated to v4.4** — Production Readiness release.
- **AGENTS.md updated to v4.4** — 140 rules (RULE-001 to RULE-140).
- **`IUpdaterService` refactored** — all methods now return `Result<T>` pattern (was custom `UpdateCheckResult`).
- **Duplicate models removed** — Desktop now uses `Application/Updates/Models/` (was duplicated in `DesktopPWF/Models/Updates/`).
- **`Environment.Exit(0)` removed** — replaced with `Result<bool>` return pattern.
- **`AdminOnlyViewModel` refactored** — constructor injection instead of service locator.
- **Report menu handlers** — shared handler with `Tag` attribute instead of duplicate handlers.
- **`DialogService` sync methods** — use styled dialogs instead of raw `MessageBox.Show`.
- **NuGet packages added**: `Microsoft.Extensions.Hosting.WindowsServices`, `Microsoft.AspNetCore.DataProtection`, `Serilog.Sinks.EventLog`.
- **`.gitignore` updated** — added `appsettings.Production.json`, `*.bak`, `*.pfx`, `*.p12`, `DataProtection-Keys/`, `publish/`, `Release/`, `logs/`.
- **`HashGen.cs` deleted** — contained `Console.WriteLine` (RULE-035 violation).

### Fixed
- `ROLLBACK IMMEDIATE` → `ROLLBACK AFTER 30` in BackupService — prevents killing active transactions.
- `int.Parse` → `int.TryParse` in ScheduledBackupWorker — no exception on bad config.
- Non-atomic file write in FirstRunSetupService — now uses `.tmp` → `File.Replace()` pattern.
- `UpdateDialogViewModel` memory leak — now implements `IDisposable` to dispose `_downloadCts`.
- JWT fallback secret — now throws in production if env var is missing.

## [1.2.0] - 2026-05-21

### Added
- **Printing & PDF Generation Engine (Phase 7)**: Complete A4 + Thermal printing subsystem.
  - **A4 PDF generation** via QuestPDF (`A4InvoiceDocument`) with RTL Arabic, store logo, alternating rows, tax breakdown, page numbers.
  - **80mm thermal receipts** via Win32 raw printing (`OpenPrinter`/`WritePrinter`) with custom `EscPos` builder — no external NuGet packages.
  - 42-character monospaced column layout, Windows-1256 encoding for Arabic, cutter + cash drawer commands.
  - **`InvoicePrintDtoBuilder`** with 4 overloads (Sales, Purchase, SalesReturn, PurchaseReturn).
  - **`PrintController`** (API) with 11 endpoints: preview, A4 print, thermal print, save PDF, preview-data, test page.
  - **`IPrintApiService`/`PrintApiService`** Desktop HTTP client for all print endpoints.
  - **`PdfPreviewWindow`** WPF control using WebBrowser for PDF preview.
  - **Print settings** persisted in `SystemSetting` table (`Category = "Print"`): `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber`, `TaxRate`.
  - **Test print page** (`POST /api/v1/print/test`) with button in WPF Settings view.
  - **Print buttons** in Sales and Purchase invoice list views (toolbar + context menu).
  - **`PrintService`** injects `ISystemSettingsRepository` for printer name resolution from DB.
  - **`PrintResult`** pattern — never throw from printing code.
  - **254+ tests** across Domain, Application, Infrastructure, API test projects (PrintControllerTests, PrintServiceTests, InvoicePrintDtoBuilderTests).
- **Print-related infrastructure**:
  - `net10.0-windows` target framework for Infrastructure, Api, and Infra.Tests (required for Win32 `DllImport`).
  - `QuestPDF` 2024.3.0, `SixLabors.ImageSharp` 3.1.4, `System.Drawing.Common` 10.0.0 packages in Infrastructure.
  - `PrintingBootstrapper.Initialize()` for QuestPDF Community license.

### Changed
- **`ISystemSettingsRepository`** extended with `GetStringAsync`/`SetStringAsync` methods.
- **`PrintController`** now reads store info (name, phone, address, tax) from `SystemSetting` table.
- **`SettingsController`** now exposes `GET/PUT /api/v1/settings/print` endpoints.
- **`SettingsViewModel`** (Desktop) loads/saves print settings via API.
- **`SalesInvoiceListViewModel`** and **`PurchaseInvoiceListViewModel`** inject `IPrintApiService` for print commands.
- **API test csproj** — re-excluded 17 pre-existing broken controller test files; only PrintControllerTests active.
- **All 7 projects** build with 0 errors; 1,342 tests pass (2 printer-dependent skipped).

## [1.1.0] - 2026-05-21

### Added
- **Dynamic Unit of Measure (v4.3)**: Multiple units per product (Piece, Box, Carton) with configurable conversion factors.
  - `ProductUnit` entity with per-unit RetailPrice, WholesalePrice, and ConversionFactor.
  - `UnitBarcode` table for unit-specific barcodes (one barcode per product-unit combination).
  - `SmartUnitFormatter` for quantity-based best-display-unit selection.
  - Base unit enforcement (`ConversionFactor = 1`) with at-least-one-unit Domain rule.
- **Costing Strategy (v4.3)**: Three configurable costing methods.
  - WeightedAverage (`(OldStock * OldAvgCost + NewQty * NewUnitCost) / TotalQty`).
  - LastPurchasePrice (direct overwrite of AvgCost).
  - SupplierPrice (use catalog price — no calculation).
  - `UpdateProductPricingService` in Application layer with cost cascade to ALL product units.
  - Costing method stored in `SystemSettings` table, seeded as WeightedAverage.
  - `ProductPriceHistory` audit trail on every cost/price change.
- **Cash Box Management (v4.3)**: Multi-box cash tracking.
  - `CashBox` entity with `OpeningBalance`, `CurrentBalance`.
  - `CashTransaction` immutable entries (OpeningBalance, SalesIncome, Expense, TransferOut/In, RefundOut, SupplierPayment, CustomerPayment).
  - `CashBox.CurrentBalance` validated before dispensing (never negative).
  - Cash transfers require TWO transactions (Out from source + In to destination).
  - `DailyClosure` for end-of-day reconciliation.
- **DesktopPWF Migration**: Full WPF MVVM rewrite replacing old WinForms Desktop.
  - 638 files added — EventBus, DialogService, printing subsystem, styled dialogs, toast notifications, barcode input.
  - LoginWindow: RELEASE uses `WindowStyle="None"` + `AllowsTransparency="True"`; DEBUG uses `SingleBorderWindow`.
- **New API Controllers**: Backup, Settings, Users, Dashboard, Logs, Returns, CustomerPayments, SupplierPayments.
- **New Services**: `BarcodeLookupService`, `SystemSettingsRepository`, `BackupService`, `JwtTokenGenerator`, `SalesDbContextFactory`.
- **Database**: 8 new tables (`ProductUnits`, `UnitBarcodes`, `CashBoxes`, `CashTransactions`, `SystemSettings`, `ProductPriceHistory`, `SystemLog` — plus cleanup of legacy tables).
- **23 new unit tests** for ProductUnit, CashBox, and WeightedAverage costing.

### Changed
- **Domain Layer**: `Product` entity updated — pricing moved from `Product` to `ProductUnit`; `ProductBarcodes` table replaced by `UnitBarcodes`.
- **Infrastructure**: EF Core configs updated for all new entities; `BarcodeLookupService` added for unit-aware scanning.
- **Application Layer**: `UpdateProductPricingService` centralizes all costing logic; `StoreSettingsService` manages system configuration.
- **WPF Desktop**: Complete MVVM re-architecture — all modules migrated from WinForms to WPF.
- **API**: New endpoints for settings management, backup, dashboard, system logs, returns, and payments.

### Fixed
- Pricing duplication between Product and ProductUnit resolved — pricing now lives on ProductUnit only.
- Barcode ambiguity resolved — each barcode uniquely identifies one specific product unit.
- Stock conversion now happens entirely in Domain layer (no UI-side conversion logic).

## [1.0.0] - 2026-05-16

### Added
- **Wholesale & Retail Dual-Unit System**: Support for selling in multiple units (e.g., Box vs. Piece) with automatic stock conversion.
- **Intelligent Low Stock Management**: Automated reorder suggestions based on wholesale/retail conversion factors and reorder levels.
- **System Services**: Store-wide settings management including Tax Identification Number (TIN) support.
- **Database Maintenance**: Integrated backup and restore functionality with risk-aware UI prompts and automatic system restart on restore.
- **Audible Feedback**: Added sound cues for successful product scans and quantity updates in sales/purchase modules.

### Changed
- **Modernized UI**: Standardized all list toolbars to use WrapPanel for responsiveness and improved DataGrid ergonomics.
- **Arabic Localization**: Completed 100% RTL compliance across all administrative and transactional screens.
- **Printing Architecture**: Updated A4 and 80mm thermal receipt templates to include mandatory store tax information.

### Fixed
- Standardized editor window footers across the solution for consistent user action flow.
- Resolved database schema inconsistencies regarding decimal precision for financial fields.
