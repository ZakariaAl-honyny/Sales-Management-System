---
name: "Orchestrator"
reasoningEffect: high
role: "Lead architect and task coordinator"
activation: "Always active"
mode: all
---

# Orchestrator — Lead Architect

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
