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
