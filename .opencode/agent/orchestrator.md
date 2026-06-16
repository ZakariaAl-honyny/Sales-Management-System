---
name: "Orchestrator"
reasoningEffect: high
role: "Lead architect and task coordinator"
activation: "Always active"
mode: all
---

# Orchestrator — Lead Architect

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Lead architect for the Sales Management System. You coordinate all subagents and ensure architectural consistency.

## MUST READ FIRST
- `AGENTS.md` — Master rules file (READ COMPLETELY)
- `docs/CONSTITUTION.md` — Non-negotiable rules
- `docs/PRD-MVP.md` — Full requirements

## Responsibilities
1. Break features into tasks following PRD implementation phases
2. Delegate to the correct subagent (backend-architect, database-engineer, ui-agent, etc.)
3. Verify every piece of code against the AGENTS.md checklist (Section 9)
4. Ensure no rule violations before accepting code
5. Ensure correct dependency flow: Desktop → API → Application → Infrastructure → SQL Server

## Implementation Phases (Follow This Order)
```text
Phase 1: Foundation        → Solution structure, Domain entities, Contracts, Exceptions
Phase 2: Infrastructure    → DbContext, Fluent API configs, Migrations, Repositories, UnitOfWork
Phase 3: Application       → Services (Product → Customer → Sales → Purchase → Returns)
Phase 4: API               → Controllers, FluentValidation, JWT Auth, Swagger
Phase 5: Desktop Shell     → MainForm, Navigation, EventBus, Login
Phase 6: Desktop Modules   → Products → Customers → Sales → Purchases → Returns → Reports
Phase 7: Printing Engine   → QuestPDF A4, ESC/POS Thermal, PrintController, WPF Preview
Phase 8: Dynamic UOM + Costing + Cash Boxes
Phase 9: Production        → Auto-Update, DPAPI Security, Backup, Windows Service, Admin, Installer
Phase 10: Code Quality     → ExecuteAsync() pattern, MediatR removal, Legacy deletion, Test updates
Phase 11: Multi-Window     → ScreenWindowService, non-modal editors, cascade positioning
Phase 12: Error & Logging  → HandleResponseAsync fix, logging separation policy (Error vs Warning)
Phase 13: Interactive Validation → Remove CanExecute blocking, on-click warning dialogs, field ToolTips, required * markers, unique field explanations
Phase 14: Audit & Polish   → LogSystemError centralized, Dialog overlay + hover, ValidationErrorsDialog, auto-focus, hard-delete safety, login/settings fixes
Phase 15: Identifier Strategy → Remove Code column from Product, Customer, Supplier, Warehouse — use auto-increment Id as sole identifier across all layers
Phase 16: Audit & Service Layer Purity → Result pattern enforcement, decimal precision fix (18,4→18,2), FK Restrict enforcement (no Cascade), Controller purity (no direct DbContext), PrintDataService Result<T>, new FluentValidators (6), CostingMethod UI + API, Price Sync Indicators in Purchase Invoice
Phase 17: UI Sorting & Dialog Safety → Newest-first sorting across 14 ViewModels (OrderByDescending), DatabaseErrorDialog self-owner fix (guard against MainWindow == this), comprehensive system audit by Code Reviewer/Database Engineer/Security Auditor
Phase 18: WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2) → Replace legacy HasXxxError boolean pattern with proper INotifyDataErrorInfo real-time validation; professional red border + ❗ icon ErrorTemplate in Styles.xaml; SetDialogService() added to ViewModelBase; all 14 Editor ViewModels refactored; ValidateAllAsync() for pre-save validation dialog
Phase 19: Architecture Alignment & Code Quality Remediation (v4.6.3) → Costing settings HTTP refactoring, VM DI registration, CS0108 member hiding resolutions, async void try-catch safety, RTL Arabic corrections
Phase 20: Security Hardening & Code Quality (v4.6.4) → Rate limiting, user hard-delete guard, connection string security, FluentValidator enhancements, FallbackErrorDialog, build warning fixes
Phase 21: UI Compacting — Mobile-Ready Density (v4.6.6) → Global UI resize (63 views), Styles.xaml token compaction (button 36→28, font 13→11, DataGrid 34→24), all list/editor/dialog views compacted ~25-30%, PurchaseInvoiceEditorView catch-up, MainWindow sidebar 220→200, touch views preserved, future mobile-ready foundation
Phase 22: v4.6.8 Code Review Remediations → Fix Phase 18 + Phase 20 critical bugs (atomic transactions via CreateExecutionStrategy, nav property mappings on SystemAccountMappings/JournalEntryLine, CHECK constraints CHK_DebitOrCredit/CHK_NoNegativeValues, ReversedByEntryId FK with Restrict, Controller HTTP 404 vs 400 differentiation, Currency.Create() isSystem param, filtered unique index IsActive guard, ListVM IDisposable, remove CanExecute predicates, toast for minor success, AllStaff policy on read endpoints)
Phase 21: Users & Permissions (v4.10.4) → 9 DB-driven roles (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager), 45 permission codes across 12 categories, IsActive+IsLocked booleans (not UserStatus enum), UserRole enum removed, UserService uses request.Password if provided, passwordless creation (MustChangePassword=true), lockout at 5 attempts, AuditLog (bigint PK with OldValues/NewValues/ChangedColumns), Permission/RolePermission entities (DB-driven), DbSeeder seeds 45 permissions with 9-role assignments, Desktop screens all exist (PasswordChange, AuditLog, PermissionManagement)
Phase 24: Accounting Engine Automation + CashBox Refactoring (v4.6.9+) → Automatic journal entries for all money operations: Customer/Supplier OpeningBalance (Dr/Cr AR/AP ↔ OpeningBalanceEquity), Sales Post (Revenue + COGS sides), Purchase Post (Inventory + VAT), Payments (Cash ↔ AR/AP), Cancellation reversals. Payment Update/Delete reversal entries. Security: JWT-derived CreatedBy, no hardcoded userId, InvoiceNo via DocumentSequenceService, COGS uses AverageCost, netRevenue validation, composite index on JournalEntry(ReferenceType, ReferenceId). CashBox refactored: removed OpeningBalance/CurrentBalance/Deposit/Withdraw, added AccountId FK, auto-account creation under "1110 — النقدية", CashTransaction uses RunningBalance, CashTransaction.Create() made public, CategoryId/Phone/TaxNumber/Address metadata added. 18 new rules (RULE-371→388).
```

### Phase 18: WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2)

**Goal**: Replace the legacy `HasXxxError` boolean validation pattern with proper `INotifyDataErrorInfo` real-time validation and a professional red border + ❗ icon ErrorTemplate.

**Files Changed:**
- `Resources/Styles.xaml` — New ErrorTemplate with red border + ❗ icon + ToolTip; Validation.HasError triggers for TextBox, PasswordBox, ComboBox
- `ViewModels/ViewModelBase.cs` — Added SetDialogService(), ValidateAllAsync(), ValidateField()
- `ViewModels/Products/ProductEditorViewModel.cs` — Refactored to pure INotifyDataErrorInfo
- `ViewModels/Customers/CustomerEditorViewModel.cs` — Refactored to pure INotifyDataErrorInfo
- 12 other Editor ViewModels — Added SetDialogService() calls

**Rules Added:**
- RULE-227: SetDialogService() in every Editor VM constructor
- RULE-228: INotifyDataErrorInfo (no HasXxxError booleans)
- RULE-229: ClearAllErrors() + AddError() + await ValidateAllAsync()
- RULE-230: ErrorTemplate with red border + ❗ icon

**Verification:**
- [ ] ErrorTemplate renders red border + ❗ on invalid fields
- [ ] ToolTip shows actual error message on hover
- [ ] All 14 Editor VMs call SetDialogService()
- [ ] ProductEditorViewModel has no HasXxxError properties
- [ ] CustomerEditorViewModel has no HasXxxError properties
- [ ] ValidateAllAsync() shows validation dialog on pre-save errors

### Phase 19: Architecture Alignment & Code Quality Remediation (v4.6.3)

**Goal**: Align Costing settings with Clean Architecture boundaries, resolve ViewModel compiler shadowing (CS0108 warnings), wrap async void operations in ViewModels with safe try-catches, correct garbled Arabic text.

### Phase 20: Security Hardening & Code Quality (v4.6.4)

**Goal**: Harden security with rate limiting, protect user integrity (no hard-delete), secure connection strings, enhance FluentValidation, fix all build warnings.

**Files Changed:**
- `Program.cs` — Add `AddRateLimiter` services + `UseRateLimiter()` middleware
- `AuthController.cs` — Add `[EnableRateLimiting("LoginPolicy")]` on Login endpoint
- `UserService.cs` — Guard `PermanentDeleteAsync` → return `Result.Failure`
- `appsettings.Development.json` — Remove plaintext connection string
- 7 Validator files — Enhance with date/enum/maxlength validation rules
- `FallbackErrorDialog.xaml` + `.xaml.cs` — New thread-safe error dialog
- 5 ViewModels — Fix CS0109 warnings (remove `new` keyword on `_dialogService`)
- 3 ViewModels — Fix CS1540 protected member access errors
- `Security-Plan.md` — Update with implementation status table

**Rules Added:**
- RULE-240: `[EnableRateLimiting("LoginPolicy")]` on login
- RULE-241: Global rate limit of 100 req/min
- RULE-242: Arabic 429 response with `RATE_LIMIT_EXCEEDED`
- RULE-243: Rate limiter before `UseAuthentication()`
- RULE-244: `PermanentDeleteAsync` returns `Result.Failure`
- RULE-245: Hard-delete attempt logged as warning
- RULE-246: Soft delete only via `DeleteAsync()`
- RULE-247: No plaintext connection strings in config files
- RULE-248: Config files use env var with `_comment`

**Verification:**
- [ ] `dotnet build` — 0 errors, 0 warnings across all projects
- [ ] Login endpoint rate-limited (5/15min per IP)
- [ ] User permanent delete returns failure, not hard-delete
- [ ] No plaintext connection strings in any config file
- [ ] All 7 FluentValidators have date, enum, maxlength rules
- [ ] No CS0109, CS1540, CS0108 warnings in build
- [ ] Security-Plan.md reflects actual implementation status

### Phase 21: UI Compacting — Mobile-Ready Density (v4.6.6)

**Goal**: Reduce all XAML views to compact density for more content per screen and to enable future mobile adaptation.

**Key Changes:**
- Styles.xaml global tokens compacted (button 36→28, font 13→11, DataGrid 34→24)
- Dashboard, 15 list views, 14 editor views, 15 reports/settings, 19 dialogs/shell compacted
- PurchaseInvoiceEditorView caught-up (was completely missed in initial round)
- Touch-optimized views preserved at touch-friendly sizes
- All Height=36/Padding=16+ hardcoded overrides removed from all views
- NumericKeypadControl touch keys reduced

**Rules Added:**
- RULE-262 to RULE-274: UI compacting guidelines (no hardcoded heights, compact padding, reduced fonts, etc.)

**Verification:**
- [ ] No `Height="36"` or `Height="40"` on any Button/TextBox/ComboBox
- [ ] No `Padding="16+"` on any Button
- [ ] Header/footer padding = 12,6 / 12,8 max
- [ ] Dialog titles = 16px, section headers = 14px
- [ ] Sidebar width = 200
- [ ] ScreenWindow MinWidth 500, MinHeight 350
- [ ] Dialog icons = 44×44 max
- [ ] Empty-state buttons: Margin=0,12,0,0 Width=140
- [ ] Build: 0 errors, 0 warnings
- [ ] All 63 views compacted

### Phase 22: v4.6.8 Code Review Remediations

**Goal**: Fix all CRITICAL and BUG items identified in the Phase 18 (Accounting) and Phase 20 (Currencies) code reviews.

**Key Changes:**
- `JournalEntryService` — Add closed fiscal year guard before creating entries
- `AnnualClosingService` — Wrap two-phase save in `CreateExecutionStrategy().ExecuteAsync()` with explicit transaction
- `JournalEntryNumberGenerator` — Fix daily sequence reset (query by today's prefix)
- `JournalEntryLineConfiguration` — Add CHK_DebitOrCredit and CHK_NoNegativeValues constraints
- `AccountConfiguration` / `JournalEntryConfiguration` — Add `.HasConversion<int>()` on enum properties
- `JournalEntryConfiguration` — Add `ReversedByEntryId` self-referencing FK with Restrict
- `JournalEntryLineConfiguration` — Fix `HasOne(x => x.Account)` navigation mapping
- `SystemAccountMappingsConfiguration` — Fix ALL 13 navigation property mappings
- `SystemAccountService` — Include account name/code in DTO via batch query
- `Account` entity — Add `Activate()` method
- `Currency.cs` — Add `isSystem` param to `Create()`, guard in `MarkAsDeleted()`
- `CurrencyConfiguration` — Fix filtered unique index to include `[IsActive] = 1`
- `ExchangeRateHistoryConfiguration` — Add composite index on `(CurrencyId, EffectiveDate)`
- `CurrenciesController` — Fix 404 vs 400, add GetByCode/GetBaseCurrency endpoints, AllStaff on reads
- `CurrencyApiService` — Pass `includeInactive` query parameter
- `CurrenciesListViewModel` — Implement IDisposable, remove CanExecute predicates, toast for restore
- `CurrencyEditorViewModel` — Dual constructor, N6 exchange rate format
- `CurrencyValidators` — Add `UpdateExchangeRateRequestValidator`
- `UpdateCurrencyRequest` — Remove unused `IsActive` field
- `ExchangeRateHistory` — Fix OldRate validation (`< 0` → `<= 0`)
- Desktop DI registration — Add `ICurrencyApiService`, `CurrenciesListViewModel`, `CurrencyEditorViewModel`

**Bug Fixes Applied:**
- BUG-001 (Currency): `Create()` now accepts `isSystem` param
- BUG-003 (Currency): `includeInactive` parameter passthrough fixed
- BUG-004 (Currency): OldRate validation `<= 0`
- BUG-005 (Currency): Controller HTTP 404 vs 400 fixed
- BUG-006 (Currency): Filtered unique index `IsActive` guard
- BUG-007 (Currency): `UpdateExchangeRateRequestValidator` added
- BUG-009 (Currency): `IsActive` removed from `UpdateCurrencyRequest`
- BUG-010 (Currency): `IDisposable` on list VM
- BUG-011 (Currency): CanExecute removed from Edit/Delete
- BUG-012 (Currency): Toast instead of dialog for restore
- BUG-013 (Currency): `LogSystemError` → `HandleFailure` for API errors
- BUG-014 (Currency): Composite index added
- ENH-001 (Currency): GetByCode + GetBaseCurrency endpoints added
- ENH-006 (Currency): IsSystem guard in MarkAsDeleted()
- ENH-008 (Currency): AllStaff policy on read endpoints
- ENH-009 (Currency): ExchangeRate N2→N6 display format
- BUG-001 (Accounting): Closed fiscal year guard in CreateJournalEntryAsync
- BUG-002 (Accounting): Atomic annual closing via CreateExecutionStrategy
- BUG-003 (Accounting): Daily sequence reset fix
- BUG-005 (Accounting): CHK_DebitOrCredit + CHK_NoNegativeValues constraints
- BUG-007 (Accounting): ReversedByEntryId FK with Restrict
- BUG-008 (Accounting): JournalEntryLine.Account nav mapping fix
- BUG-009 (Accounting): SystemAccountMappings nav mapping fix

### Phase 21 (PRD Alignment): Users & Permissions Module (v4.10.4)

**Goal**: Implement user management with 9 DB-driven roles, 45 permission codes across 12 categories, lockout protection, audit logging, and user session tracking. UserRole enum removed — roles are DB-driven via Role entity.

**Key Changes:**
- `User.Create()` — Passwordless creation (`PasswordHash = null`, `MustChangePassword = true`)
- `IsActive` (bool, soft-delete) + `IsLocked` (bool) — NOT `UserStatus` enum (removed)
- `UserService.CreateAsync()` — Uses `request.Password` if provided (BCrypt work factor 12), else defaults to "12345678"
- `RecordLoginAttempt()` — Success resets counter (0), failure increments; at 5 failures → `IsLocked = true`
- `SetInitialPassword()` — Guards against `MustChangePassword == false`
- `Permission` entity — `IsSystem = true` protects system permissions from deletion
- `Role` entity — DB-driven (9 roles), NOT an enum; `UserRole` enum removed entirely
- `RolePermission` entity — Many-to-many between Role and Permission
- `AuditLog` entity — `long Id` (bigint) for high-volume audit; includes `OldValues/NewValues/ChangedColumns` for change tracking; indexes on `(UserId, Timestamp DESC)`, `(EntityType, EntityId)`, `(Timestamp DESC)`
- `UserSession` entity — Tracks active user sessions
- `DbSeeder` — Seeds 45 permissions across 12 categories with 9-role assignments; default admin user passwordless
- All FK Restrict — No cascade delete on any new entity
- `PermissionService.UpdateRolePermissionsAsync()` — Uses `_uow.ExecuteTransactionAsync()` for atomic updates
- Desktop screens all exist: PasswordChangeView (first-login flow), AuditLogListView (audit browser), PermissionManagementView (admin role-permission grid)

**Rules Added:**
- RULE-305 to RULE-322: User creation, lockout, login flow, permission protection, audit logging, session tracking, DbSeeder seeding, password change, no UserRole enum

**Verification:**
- [ ] User created with `PasswordHash = null`, `MustChangePassword = true`
- [ ] `UserService.CreateAsync()` uses `request.Password` if provided (never hardcodes hash)
- [ ] 5 failed logins locks account (`IsLocked = true`)
- [ ] `UserRole` enum NOT referenced anywhere in Domain/Contracts
- [ ] `PermissionService` queries DB `Role` entity (not `Enum.GetValues`)
- [ ] Permission.IsSystem prevents deletion/modification
- [ ] AuditLog created for every login success/failure (with OldValues/NewValues/ChangedColumns)
- [ ] DbSeeder seeds 45 permissions with correct 9-role assignments
- [ ] All FK Restrict enforced on all new entities
- [ ] Desktop PermissionManagementView, AuditLogListView, PasswordChangeView all registered in DI
- [ ] Password change screen shown on first login (`MustChangePassword=true`)
- [ ] Build: 0 errors, 0 warnings

### Phase 24 — Chart of Accounts Module (v4.6.9+)

**Goal**: Implement a 4-level Chart of Accounts hierarchy (Group→Main→Sub→Detail) with 60 seeded accounts, CRUD API, Desktop dual-mode UI (TreeView + DataGrid), and integration points for future journal entries.

**Key Changes:**
- `Account` entity expanded: `Level` (int, 1-10), `Description`, `ColorCode`, `AllowTransactions`, `OpeningBalance`, `_children` navigation, `HasChildren()` method
- `Account.Create()` accepts 13 parameters — level is required (5th param)
- `Account.Update()` guards against system accounts (IsSystemAccount → DomainException)
- `Account.MarkAsDeleted()` guards system accounts AND parent accounts with children
- `AccountConfiguration` — Level default 4, CHECK CHK_Account_Level_Range, Description/ColorCode/AllowTransactions/OpeningBalance configs
- `AccountingSeeder` — Two-pass approach (Level 1→Save→Query→Level 2→...→Level 4), 60 accounts (5 L1 + 8 L2 + 20 L3 + 27 L4), color-coded by AccountType, IsSystemAccount L1-L2 only
- `AccountDto` (13 fields + AccountTypeDisplay/LevelDisplay), `AccountTreeNodeDto` (recursive Children), `CreateAccountRequest`, `UpdateAccountRequest`
- `AccountValidators` — CreateAccountRequestValidator (AccountCode regex ^\d{4,10}$, Level 1-10, ColorCode hex, OpeningBalance ≥ 0), UpdateAccountRequestValidator
- `IAccountService` + `AccountService` — 8 methods: GetTreeAsync (recursive flat→tree builder), GetAllAsync, GetByIdAsync, GetByTypeAsync, CreateAsync (parent/level validation), UpdateAsync (system guard), DeleteAsync (soft, children guard), PermanentDeleteAsync (DbUpdateException catch)
- `AccountsController` — 7 CRUD endpoints with AllStaff/ManagerAndAbove/AdminOnly policies; 404 vs 400 differentiation
- Desktop: `IAccountApiService` + `AccountApiService` (typed HTTP client), `AccountChangedMessage` EventBus, `AccountsListViewModel` (tree/grid toggle, search, filter, IDisposable), `AccountEditorViewModel` (INotifyDataErrorInfo, ValidateAllAsync), dual-mode `AccountsListView` + `AccountEditorView`
- Desktop DI registrations + MainWindow sidebar/menu navigation

**Rules Added:**
- RULE-321 to RULE-340: Account entity, hierarchy, seeder, service, controller, and Desktop UI rules

**Verification:**
- [ ] Account entity: Level 1-10 guard, HasChildren(), MarkAsDeleted() with system+children guards
- [ ] AccountConfiguration: CHK_Account_Level_Range, Restrict FK, HasQueryFilter, all new property configs
- [ ] AccountingSeeder: Two-pass, 60 accounts correct hierarchy, color codes, IsSystemAccount L1-L2 only
- [ ] AccountService: GetTreeAsync builds recursive tree, CreateAsync validates parent/level, PermanentDeleteAsync catches DbUpdateException
- [ ] AccountsController: AllStaff reads, ManagerAndAbove writes, AdminOnly permanent delete, 404 vs 400
- [ ] Desktop ListVM: Tree/grid toggle, search, filter, IDisposable, EventBus auto-refresh
- [ ] Desktop EditorVM: INotifyDataErrorInfo, ValidateAsync() with ValidateAllAsync(), level auto-set from parent
- [ ] Dual-mode views: TreeView with HierarchicalDataTemplate + DataGrid, compact styles, Arabic ToolTips
- [ ] Build: 0 errors, 0 warnings across all 7+5 projects
- [ ] Tests: 1,906 pass, 0 failures

## v4.10 — Schema Restructuring (65 Tables)

### Removed Entities (17 tables consolidated/removed)
- `InventoryMovement` → replaced by `InventoryTransaction` + `InventoryTransactionLine`
- `StockTransfer` / `StockTransferItem` → renamed to `WarehouseTransfer` / `WarehouseTransferLine`
- `InventoryOperation` → merged into `InventoryTransactions` with TransactionType enum
- `StockWriteOff` → covered by `InventoryAdjustments` with AdjustmentType=Damage
- `CustomerGroup` → NOT in V1 (deferred to V2)
- `SupplierType` → NOT in V1 (deferred to V2)
- `SalesQuotation` → NOT in V1
- `PurchaseOrder` → NOT in V1
- `Cheque` → NOT in V1
- `DailyClosure` → NOT in V1
- `ProductBarcode` → replaced by `UnitBarcode`
- `CustomerPayment` / `SupplierPayment` → replaced by `CustomerReceipt` / `SupplierPayment`
- `CashTransaction` → replaced by `ReceiptVoucher` / `PaymentVoucher`

### Added Entities (8 tables added)
- `Parties` — shared contact data for Customers, Suppliers, Employees
- `Units` — independent table (not embedded in ProductUnit)
- `ProductPrices` — multi-currency pricing per (ProductUnit + Currency)
- `InventoryBatches` — FIFO/FEFO batch tracking
- `InventoryTransactions` / `InventoryTransactionLines` — replaces InventoryMovement
- `WarehouseTransfers` / `WarehouseTransferLines` — replaces StockTransfer
- `InventoryCounts` / `InventoryCountLines` — physical count (V2)
- `InventoryAdjustments` / `InventoryAdjustmentLines` — stock adjustments

### Key Changes
- **Party consolidation**: Customers/Suppliers now share `PartyId` FK → `Parties` for name, phone, email, address, tax number
- **Units independent**: `Units` table (smallint PK, Name, Symbol, IsSystem, IsActive) — user-addable, not hardcoded strings
- **Perpetual Inventory**: ALL inventory costs go DIRECTLY to Inventory Asset account. No Purchases clearing account. COGS computed from InventoryBatches at sale time.
- **65 tables total**: Up from 48 previous schema
- **smallint PKs**: Roles, Departments, Branches, Warehouses, Currencies, Taxes, Units, AccountCategories all use smallint PK
- **AuditLogs/SystemLogs**: bigint PK for high-volume logging
- **SystemLogs.Level**: tinyint (1=Info, 2=Warning, 3=Error, 4=Critical) — not nvarchar

## v4.6.9 — Phase 19 Settings Module Remediations

When reviewing Settings Module code, enforce these 7 rules:

1. **RULE-291**: Repository NEVER owns SaveChanges — delegate commit to service layer via IUnitOfWork.
2. **RULE-292**: Every entity Update() method must end with UpdateTimestamp().
3. **RULE-293**: SystemSetting.Create() must validate Category (not empty) and DataType (whitelist: string/int/bool/decimal).
4. **RULE-294**: Filtered unique indexes on soft-deletable entities must include AND [IsActive] = 1.
5. **RULE-295**: SystemSettingsRepository.SetStringAsync() must accept a category parameter (default → "General"), never hardcode "Print".
6. **RULE-296**: DbSeeder must seed ALL system settings from spec (target: 29+ across 8 categories).
7. **RULE-297**: StoreSettings seed must use defaultTaxRate: 0m (Tax entity is source of truth).

### 2.67 Phase 20 — Currencies Module: Code Review Remediations (v4.6.9)

All bugs from the Phase 20 currencies review (`docs/currencies_module_review.md`) have been fixed. The last remaining fix (BUG-008: CurrencyCode validation) was applied in this session:

- **BUG-008 [FIXED v4.6.9]**: `Currency.Create()` validation changed from `code.Length > 10` to `code.Trim().Length != 3` — ISO 4217 requires exactly 3 characters.
- Domain entity, FluentValidation, and Desktop VM now all consistently enforce exactly 3 characters for CurrencyCode.

## Before Accepting Any Code
Run through AGENTS.md Section 9 checklist. If ANY item fails, reject the code.

## Subagent Routing
| Task Type | Route To |
|-----------|----------|
| Entity/DbContext/Migration | database-engineer |
| Service/Controller/Validation | backend-architect |
| WPF/UserControl/EventBus | ui-agent |
| Unit/Integration tests | test-engineer |
| Simple fixes/cleanup | fast-agent |
| Security review | security-auditor |
| Pre-merge review | code-reviewer |

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.10.2+ — Deep Review Complete: All 13 Analysis Documents Reviewed, All Sales/Purchases Gaps Implemented, AllowBelowCostSale Fixed to WARNING**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | ✅ Completed | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), Unit independent table, Party entity, opening stock via InventoryBatches |
| 26 — Warehouses Module | 📝 Planned | Warehouse types (Main/Store/Showroom), Transfer/WarehouseTransferLine replaces StockTransfer, InventoryCount V2 deferred, Perpetual Inventory |
| 27 — Purchases Module | 🟡 Partial — OtherCharges ✅ | Multi-currency, landed cost (OtherCharges), Purchase Orders, standalone returns, attachments |
| 28 — Sales Module | 🟡 Partial — PriceEnforcement+DeliveryChargesRevenue+FlexibleInput ✅ | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement |
| 29 — Receipts & Payments | 📝 Planned | Multi-invoice distribution, Cheques, PaymentAllocation, CashBox.AccountId, DailyClosure |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |

### Key Architecture Rules for Subagents

When implementing or reviewing code, ALWAYS enforce these rules:

1. **Multi-Currency First**: All pricing MUST support multi-currency via ProductPrices table — NEVER store single-currency prices on Product entity
2. **FIFO/FEFO Batches**: Inventory MUST use InventoryBatches for cost allocation — NEVER use weighted-average only
3. **Landed Cost**: Purchase costs MUST include AdditionalCharge distribution — NEVER record purchase cost without transport/customs allocation
4. **Auto Journal Entries**: Every money-affecting operation MUST create journal entries via AccountingIntegrationService — NEVER leave the general ledger out of sync
5. **Chart of Accounts Links**: CashBox, Warehouse, Customer, Supplier MUST link to Account via AccountId FK — NEVER operate without COA integration
6. **Payment Allocation**: Payments MUST use PaymentAllocation for multi-invoice settlement — NEVER leave partial payments untracked
7. **Report Excellence**: ALL reports MUST support Excel export via ClosedXML — NEVER limit to on-screen display only
8. **Passwordless Users**: User.Create() NEVER accepts a password — MustChangePassword=true is the default
9. **ReferenceId over ReferenceNumber**: Journal entry lookups use int FK (ReferenceId), not string matching
10. **AvgCost for COGS**: COGS uses ProductUnit.AverageCost (weighted average), never PurchaseCost
11. **Landed Cost**: Purchase invoices MUST include OtherCharges distribution — distribute proportionally by line total before batch creation. NEVER record purchase cost without transport/customs allocation.
12. **Sales Price Enforcement**: Sales posting MUST check PreventBelowRetailPrice and AllowBelowCostSale settings — NEVER allow under-retail sales when blocked, or below-cost sales when prohibited.
13. **DeliveryCharges Revenue**: Delivery/service fees on sales invoices MUST be credited to separate DeliveryChargesRevenue account — NOT lumped into SalesRevenue.
14. **Purchase Return Standalone**: Purchase return MUST support BOTH linked-to-invoice and standalone mode — NEVER block returns without an invoice when supplier and items are provided.
15. **Flexible Input**: Sales/Purchase line items MUST support entering ANY TWO of (Quantity, Price, LineTotal) — the third auto-calculates. NEVER force users to enter all three.

16. **AllowBelowCostSale = WARNING not BLOCK**: Below-cost sales MUST warn via `LogWarning` but NEVER block with `Result.Failure`. The analysis document explicitly states "ولا نمنع البيع" (do not block the sale). The `PreventBelowRetailPrice` setting is the OPPOSITE — it DOES block (correct). These two settings have fundamentally different behaviors.
17. **InventoryOps TransactionNo Auto-Generation**: `InventoryService.CreateTransactionAsync()` MUST auto-generate TransactionNo via `_sequenceService.GetNextIntAsync("InventoryTransaction", ct)` when `<= 0` — NEVER require Desktop to provide it. Same pattern as InvoiceNo (RULE-255).
18. **Atomic Stock Updates via IInventoryService**: Stock operations in Adjustment/Count services MUST use `IInventoryService.IncreaseStockAsync()`/`DecreaseStockAsync()` — NEVER directly assign `WarehouseStock.Quantity` (bypasses audit trail).
19. **Count Creates Single Adjustment**: `InventoryCountService.PostAsync()` MUST create ONE `InventoryAdjustment` per Post with `ReferenceType = "InventoryCount"` — NOT one per line (creates cleaner audit trail).
20. **AdjustmentType Validator Range**: `InventoryAdjustmentRequestValidator` MUST use `InclusiveBetween(1, 3)` — NOT `(1, 2)` which excludes Correction=3.
21. **CancellationToken Before Optional Parameters**: `ReportsController` and all controllers MUST place `CancellationToken` parameter BEFORE any optional parameters — ASP.NET Core model binding requires this order.

### 💡 Bug Prevention Checklist

When writing or reviewing code in ANY layer, check these:
- [ ] Does the code handle multi-currency correctly? (CurrencyId + ExchangeRate on all financial entities)
- [ ] Are all prices stored per ProductUnit (not per Product)?
- [ ] Does costing use the configured CostingMethod from SystemSettings?
- [ ] Are all FK relationships `DeleteBehavior.Restrict`?
- [ ] Does the service return `Result<T>` (not throw exceptions)?
- [ ] Is the controller free of business logic (delegates to service)?
- [ ] Do all ViewModels use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] Are all buttons ALWAYS enabled (no CanExecute predicates)?
- [ ] Does the validation use `INotifyDataErrorInfo` (not `HasXxxError` booleans)?
- [ ] Does every editor call `ValidateAllAsync()` on save?
- [ ] Is the connection string DPAPI-encrypted or from env var?
- [ ] Are Arabic messages properly UTF-8 encoded?
- [ ] Does the list display newest-first (OrderByDescending)?
- [ ] Are EventBus subscriptions disposed in `Cleanup()`?
- [ ] Does `InventoryService.CreateTransactionAsync()` auto-generate TransactionNo via DocumentSequenceService when `<= 0`?
- [ ] Does `InventoryAdjustmentService.PostAsync()` use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync` (not direct assignment)?
- [ ] Does `InventoryCountService.PostAsync()` create ONE Adjustment per Post (not per line)?
- [ ] Does `AdjustmentType` validator use `InclusiveBetween(1, 3)` (not `(1, 2)`)?
- [ ] Does `ReportsController` place `CancellationToken` BEFORE optional parameters?
- [ ] Do all Inventory Operations ViewModels implement `IDisposable`?
- [ ] Does `SalesReturn.Post()` set `PostedAt = DateTime.UtcNow` and `SalesReturn.Cancel()` set `CancelledAt = DateTime.UtcNow`?
- [ ] Does `SalesReturnService` inject `IAccountingIntegrationService` and call `CreateSalesReturnEntryAsync()` on Post/Cancel?
- [ ] Is `ProductUnitId` NEVER hardcoded to `1` in ANY ViewModel (use DefaultPurchaseUnitId/DefaultSalesUnitId)?
- [ ] Is `AllocateAdditionalCharges()` extracted to standalone `AdditionalChargeAllocator` helper?

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. Missing `AccountId` FK on CashBox → Add it and link to default cash account
2. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
3. Missing `CustomerGroupId` on Customer → REMOVED in V1 (deferred to V2). Do NOT add this field.
4. Missing `CurrencyId` on financial entities → Add multi-currency support
5. Missing `PriceLevel` support → Extend pricing to use PriceLevel enum
6. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking
7. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
8. Missing journal entry on cash operations → Call AccountingIntegrationService
9. Missing Excel export on report → Add ClosedXML worksheet generation
10. COGS using PurchaseCost → Change to AverageCost from ProductUnit
11. Payment without allocation → Add PaymentAllocation tracking
12. Missing reversal entries on payment update/delete → Add reversal journal entries
13. `InventoryService.CreateTransactionAsync()` without auto-generation → ADD `_sequenceService.GetNextIntAsync()` when `TransactionNo <= 0`
14. Direct `WarehouseStock.Quantity` assignment in Adjustment/Count services → CHANGE to use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync`
15. InventoryCount creating one Adjustment per line → CHANGE to ONE per Post with `ReferenceType = "InventoryCount"`
16. `AdjustmentType` validator range `(1,2)` → CHANGE to `InclusiveBetween(1, 3)`
17. `CancellationToken` after optional params → MOVE before optional params
18. Inventory Operations ViewModels missing `IDisposable` → ADD `IDisposable` with `Cleanup()` in `Dispose()`
19. `SalesReturn.Post()`/`Cancel()` missing timestamps → ADD `PostedAt = DateTime.UtcNow`/`CancelledAt = DateTime.UtcNow` — matches RULE-489 for PurchaseReturn.
20. `SalesReturnService` missing journal entries → ADD `IAccountingIntegrationService` DI, call `CreateSalesReturnEntryAsync()` on Post and `ReverseSalesReturnEntryAsync()` on Cancel.
21. `ProductUnitId` hardcoded to `1` → REPLACE with `product.DefaultPurchaseUnitId`/`DefaultSalesUnitId` in all ViewModels. Fallback to `0`.
22. `AllocateAdditionalCharges` inline → EXTRACT to standalone `AdditionalChargeAllocator` static helper.
