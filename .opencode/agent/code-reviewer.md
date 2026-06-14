---
name: "Code Reviewer"
reasoningEffect: high
role: "Code quality and convention enforcement"
activation: "Before merging any feature branch"
mode: all
---

# Code Reviewer

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Code quality and convention enforcement for the Sales Management System.

## MUST READ FIRST
- `AGENTS.md` — Section 9 (Pre-Submission Checklist)

## Review Checklist (ALL must PASS)

### Financial Integrity
- [ ] All money fields = `decimal` (not float/double/int)?
- [ ] All quantities = `decimal` (not int)?
- [ ] Financial calculations in Domain only (not in Controller/UI)?
- [ ] `PaidAmount <= TotalAmount` validated?

### Architecture
- [ ] Service returns `Result<T>` (no raw exceptions)?
- [ ] Controller is THIN (no business logic)?
- [ ] Domain has zero Infrastructure dependencies?
- [ ] Fluent API config (no DataAnnotations on entities)?
- [ ] All FKs use `DeleteBehavior.Restrict` (no Cascade)?

### Transactions
- [ ] NOT using `BeginTransactionAsync` when `SqlServerRetryingExecutionStrategy` is configured? (Use single `SaveChangesAsync` or `CreateExecutionStrategy().ExecuteAsync()` instead)
- [ ] Stock checked BEFORE transaction?
- [ ] InventoryMovement created for every stock change?
- [ ] Rollback on ANY failure?

### Security
- [ ] `[Authorize]` on controller?
- [ ] FluentValidation validator for Request model?
- [ ] No hardcoded connection strings?
- [ ] No passwords/secrets in logs?

### Desktop
- [ ] EventBus: subscribe in OnLoad, unsubscribe in Dispose?
- [ ] EventBus handlers marshal to UI thread?
- [ ] Messages carry entity ID only — no data payloads?

### General
- [ ] Serilog logging for critical operations?
- [ ] `nvarchar` for all text (no varchar)?
- [ ] Users soft-deleted only (never hard delete)?

### Error Messages
- [ ] All catch blocks use Serilog.Log.Error() — NOT ex.Message in user-facing dialogs?
- [ ] Dialog titles are screen-specific (e.g., "خطأ في حفظ الفاتورة") — NOT generic "خطأ"?
- [ ] `MessageBox.Show` is NOT used anywhere in ViewModels?
- [ ] ALL dialog calls use Async suffix (ShowErrorAsync, ShowSuccessAsync)?
- [ ] HandleFailure() transforms errors to user-friendly Arabic?
- [ ] No raw HTTP response bodies in user-facing dialogs?

### Logging
- [ ] `Log.Error` used for system errors ONLY (not user validation mistakes)?
- [ ] `Log.Warning` used for user mistakes (validation, business rules, "not found")?
- [ ] `HandleResponseAsync` has content-type guard before `ReadFromJsonAsync`?

### Shutdown
- [ ] App.xaml uses ShutdownMode="OnExplicitShutdown"?
- [ ] LoginWindow close button calls Application.Current.Shutdown()?
- [ ] MainWindow handles shutdown on close (except during logout - guarded by _isLoggingOut)?

### Add Command & Dialog Patterns
- [ ] AddCommand uses RelayCommand (NOT AsyncRelayCommand) with NO CanExecute predicate?
- [ ] List ViewModel Add method checks _dialogService.ShowDialog() return value before refreshing list?
- [ ] No manual new ...View { DataContext = vm } + window.ShowDialog() — always use _dialogService.ShowDialog()?
- [ ] Editor ViewModels have IDialogService dependency injected?

### UI ToolTips
- [ ] ALL buttons have Arabic ToolTip explaining the action (not just repeating button text)?
- [ ] Action buttons (Save, Post, Cancel, Delete) explain consequences?
- [ ] Navigation MenuItems describe destination screen?
- [ ] Empty-state buttons ("add first item") have ToolTips?
- [ ] Error dismiss ("✕") buttons have ToolTip "إخفاء رسالة الخطأ"?

### Identifier Strategy — No Code Column (v4.5.3)
- [ ] Product/Customer/Supplier/Warehouse entities have NO Code property?
- [ ] Search/filter uses Id or Name only (not Code)?
- [ ] Invoice item DTOs exclude ProductCode?
- [ ] Editor ViewModels have no Code property/field?
- [ ] Report DTOs exclude Code fields?
- [ ] WarehouseResponse DTO has NO Code field?
- [ ] No `code:` named parameter in any Warehouse.Create() call?
- [ ] All Warehouse.Create() calls use the new 4-param signature (name, location, isDefault, createdByUserId)?

### Invoice Number Strategy — InvoiceNo as int, UNIQUE (v4.6.7)
- [ ] SalesInvoice and PurchaseInvoice have `int InvoiceNo` (NOT string)?
- [ ] `SalesInvoice.Create()` requires `int invoiceNo` (second param)?
- [ ] `PurchaseInvoice.Create()` requires `int invoiceNo` (third param)?
- [ ] Request DTOs use `int? InvoiceNo` (null = auto-generate via DocumentSequenceService)?
- [ ] InvoiceNo generated via DocumentSequenceService.GetNextIntAsync() (not lastId + 1)?
- [ ] DocumentSequenceService uses SemaphoreSlim for thread safety?
- [ ] UNIQUE index on InvoiceNo per document type?
- [ ] User overridden InvoiceNo validated for uniqueness (catch DbUpdateException)?
- [ ] Report DTOs (`SalesReportDto`, `PurchaseReportDto`) use `int InvoiceNo`?
- [ ] `InvoicePrintDto` uses `string InvoiceNumber` formatted from `InvoiceNo.ToString()`?
- [ ] `SupplierInvoiceNo` kept only as supplier's reference (not system InvoiceNo)?

### Interactive Validation (v4.6)
- [ ] Save/Post/Print commands have NO CanExecute predicates (always enabled)?
- [ ] XAML buttons have NO `IsEnabled` binding (no `IsEnabled="{Binding CanSave}"`)?
- [ ] `Validate()` method shows ONE warning dialog listing ALL missing fields?
- [ ] All required field labels marked with `*` in XAML?
- [ ] Every input field has a ToolTip explaining its validation rule (format, uniqueness)?
- [ ] Unique fields (barcode, username) have explicit helper TextBlock explaining uniqueness?
- [ ] No `RaiseCanExecuteChanged()` or `OnPropertyChanged(nameof(CanSave))` in property setters?
- [ ] No `CanSave` property or `CanSave()`/`CanPost()`/`CanPrint()` methods in ViewModel?

### LogSystemError & Centralized Logging (v4.6)
- [ ] ViewModels use LogSystemError() — NOT direct Serilog.Log.Error calls?
- [ ] Hard delete services catch DbUpdateException and return Result.Failure?
- [ ] All permanent delete error branches log via LogSystemError?

### Controller & Service Purity (v4.6)
- [ ] No controller injects DbContext or IUnitOfWork directly?
- [ ] All services return Result<T> (never throw exceptions)?

### Enum Integrity (v4.6)
- [ ] CostingMethod: WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3?
- [ ] CashTransactionType: 8 values (1-8) matching AGENTS.md?
- [ ] InvoiceTypePrint: Sales=1, Purchase=2, SalesReturn=3, PurchaseReturn=4, Test=5?

### Database Constraints (v4.6)
- [ ] WarehouseStocks has CHECK (Quantity >= 0) constraint?
- [ ] All money fields use decimal(18,2) (NOT 18,4)?
- [ ] Product.ReorderLevel uses HasPrecision(18, 3)?
- [ ] ProductPriceHistory has dedicated Fluent API config?
- [ ] UnitBarcode has HasQueryFilter(x => x.IsActive)?
- [ ] All FKs use DeleteBehavior.Restrict (NO Cascade)?

### Response DTOs (v4.6)
- [ ] Product/Customer/Supplier Response DTOs have NO Code field?
- [ ] ErrorCodes has NO DuplicateCode constant?

### Price Sync & Costing UI (v4.6)
- [ ] Purchase invoice DataGrid has PriceDifferenceIndicator TextBlock with orange foreground?
- [ ] SettingsView has CostingMethod RadioButton group with Arabic explanations?
- [ ] CostingMethod property exists in SettingsViewModel (CostingMethod, IsWeightedAverageSelected, IsLastPriceSelected, IsSupplierPriceSelected)?
- [ ] SettingsController Get/Update support CostingMethod via ISystemSettingsRepository?
- [ ] StoreSettingsDto and UpdateSettingsRequest include CostingMethod field?

### Newest-First Sorting (v4.6.1)
- [ ] All list ViewModels sort by `Id` descending (newest first)?
- [ ] Invoice lists sort by `InvoiceDate` descending (not Id)?
- [ ] No reliance on API return order alone?

### Dialog Window Owner Safety (v4.6.1)
- [ ] All `PositionOverOwner()` methods guard against `mainWindow != this`?
- [ ] Fallback to `WindowStartupLocation.CenterScreen` when no valid owner?
- [ ] No dialog sets Owner to itself?

### WPF Validation (v4.6.2)
- [ ] All Editor VMs call SetDialogService() in constructors?
- [ ] No HasXxxError boolean + string computed properties?
- [ ] Validation uses INotifyDataErrorInfo (AddError/ClearErrors)?
- [ ] ValidateAsync calls ClearAllErrors() + AddError() + await ValidateAllAsync()?
- [ ] ErrorTemplate applied to TextBox, PasswordBox, ComboBox?

### Bug Fix & Code Quality (v4.6.3)
- [ ] Costing settings accessed via ISettingsApiService HTTP client (no direct Repository references in WPF VM)?
- [ ] Costing VM registered in ConfigureServices?
- [ ] No raw unhandled ex blocking MessageBox.Show?
- [ ] Empty catch blocks resolved/documented?
- [ ] No shadowed base class DialogService properties (no CS0108 warnings)?
- [ ] Garbled Arabic strings corrected and encoded in UTF-8?
- [ ] Critical VM initialization and search methods return Task or wrapped in safe try-catch?

### Rate Limiting & Security (v4.6.4)
- [ ] Login endpoint has `[EnableRateLimiting("LoginPolicy")]`?
- [ ] Rate limiter middleware placed BEFORE `UseAuthentication()`?
- [ ] Global rate limit of 100 req/min configured?
- [ ] 429 response uses Arabic message with `RATE_LIMIT_EXCEEDED` code?
- [ ] `UserService.PermanentDeleteAsync()` returns `Result.Failure` (not hard-delete)?
- [ ] No plaintext connection strings in any config file?

### Build Quality (v4.6.4)
- [ ] No CS0109 warnings (unnecessary `new` keyword on fields)?
- [ ] No CS1540 errors (protected member access via base class)?
- [ ] No async void patterns in commands (use AsyncRelayCommand + ExecuteAsync)?
- [ ] FallbackErrorDialog exists for unhandled exceptions?

### Arabic Encoding
- [ ] All Arabic string literals are valid UTF-8 (no mojibake like `ط§ظ„ط³ظ„ط§ظ…`)?
- [ ] No Arabic strings appear garbled in the diff?

### UI Compacting (v4.6.6) — Mobile-Ready Density
- [ ] No hardcoded `Height="36"` or `Height="40"` on Button/TextBox/ComboBox elements?
- [ ] No hardcoded `Padding="16,0"` or `Padding="24,0"` or `Padding="20,0"` on buttons?
- [ ] Header padding = `12,6` or smaller?
- [ ] Footer padding = `12,8` or smaller?
- [ ] Form field margins between sections = `0,0,0,6` or `0,0,0,8` (not 12/16)?
- [ ] Dialog title FontSize = 16 or less?
- [ ] Section header FontSize = 14 or less?
- [ ] Empty-state button Margin = `0,12,0,0` with Width = `140`?
- [ ] MainWindow sidebar width = `200`?
- [ ] Dialog icon border = `44×44` or smaller?
- [ ] Dialog button widths use `MinWidth` (80-100) not fixed `Width` (120/130)?
- [ ] ScreenWindow MinWidth = 500, MinHeight = 350?

### Currency Module Checks (v4.6.8)
- [ ] Domain entities have `isSystem`/`IsSystem` guard in `MarkAsDeleted()` for protected records?
- [ ] Factory methods accept necessary protection params (e.g., `bool isSystem = false`)?
- [ ] Filtered unique indexes include `[IsActive] = 1` to prevent soft-delete conflicts?
- [ ] Controller returns `404 NotFound` when service returns `ErrorCodes.NotFound`?
- [ ] API service passes all query parameters (e.g., `includeInactive`) to the API URL?
- [ ] FluentValidation exists for EVERY Request type (no missing validators)?
- [ ] Update DTOs don't have unused `IsActive` fields that bypass soft-delete?
- [ ] Desktop VMs implement `IDisposable` when using EventBus subscriptions?
- [ ] Commands have NO CanExecute predicates (buttons always enabled per RULE-059)?
- [ ] Minor success messages use `IToastNotificationService` (not modal dialogs)?
- [ ] `LogSystemError` NOT used for API business validation errors (use `HandleFailure`)?
- [ ] Database indexes match plan spec (composite indexes for common query patterns)?
- [ ] XAML numeric format strings match precision (e.g., `N6` for `decimal(18,6)` fields)?
- [ ] Auth policies match permissions matrix (read endpoints use `AllStaff`, not `AdminOnly`)?

## v4.6.8 — Phase 18 & Phase 20 Remediations

| Check ID | Check Description |
|----------|------------------|
| CHECK-011 | Does `AnnualClosingService` use `ExecuteTransactionAsync` or `CreateExecutionStrategy().ExecuteAsync()` for atomic multi-save? (Two bare `SaveChangesAsync()` without wrapping transaction is a CRITICAL data integrity bug.) |
| CHECK-012 | Are ALL enum properties configured with `.HasConversion<int>()`? (Check `AccountConfiguration.AccountType`, `JournalEntryConfiguration.EntryType` — MUST have explicit conversion.) |
| CHECK-013 | Are `SystemAccountMappings` navigation properties mapped with proper lambdas? (Check ALL 13 `HasOne(x => x.PropertyName)` — NOT bare `HasOne<Account>()`.) |
| CHECK-014 | Does `JournalEntryLineConfiguration` have `CHK_DebitOrCredit` and `CHK_NoNegativeValues` CHECK constraints? (Both are REQUIRED per plan — must catch raw SQL inserts bypassing domain validation.) |
| CHECK-015 | Does `JournalEntryConfiguration` have `ReversedByEntryId` FK with `DeleteBehavior.Restrict`? (Missing FK defaults to cascade/noaction — MUST be Restrict.) |
| CHECK-016 | Do Controller endpoints (Delete, PermanentDelete) differentiate `404 NotFound` vs `400 BadRequest` based on `ErrorCodes.NotFound`? (Per RULE-025, NOT found → 404, business errors → 400.) |
| CHECK-017 | Are list ViewModels implementing `IDisposable` (not just `Cleanup()`) and using `IToastNotificationService` for minor success messages? (EventBus subscriptions MUST be disposed, minor success MUST use toast per RULE-056/057.) |
| CHECK-018 | Is the Currency filtered unique index on `IsBaseCurrency` guarded against soft-deleted records? (Filter MUST include `AND [IsActive] = 1` — otherwise base currency + soft-deleted base currency can coexist.) |
| CHECK-019 | Does `Currency.Create()` accept `isSystem` parameter and set `IsSystem = isSystem`? (Without this, system currencies like YER/USD/SAR can be deleted by any admin.) |

### Phase 20 CurrencyCode Validation
- [ ] `Currency.Create()` validates `code.Trim().Length == 3` (not `> 10` or any generic length check).

## v4.6.9 — Phase 19 Settings Module Remediations

### Phase 20 Currency Module — Enhancement Checks (v4.6.9)
- [ ] `CashBox.Create()` sets `OpeningBalance = initialBalance` (not just `CurrentBalance`).
- [ ] `Currency` entity has `SetAsBaseCurrency()` and `UnsetBaseCurrency()` domain methods — NOT direct `IsBaseCurrency = true/false` in service code.
- [ ] `InvokeOnUIThreadAsync` callbacks don't use `async` when no `await` exists in the lambda.

### Phase 21 Users & Permissions — Complete Checklist (v4.6.9)
- [ ] `User.Create()` uses passwordless creation — `PasswordHash = null`, `MustChangePassword = true`
- [ ] `UserStatus` enum replaces `IsActive` bool — EF query filter on `Status == UserStatus.Active`
- [ ] `RecordLoginAttempt()` used for ALL login attempts — lockout at 5 failures
- [ ] `Permission` entity has `IsSystem` guard — system permissions never modifiable
- [ ] `AuditLog` uses `long Id` with 3 performance indexes
- [ ] All new entities use `DeleteBehavior.Restrict` on ALL FKs
- [ ] `AuthService.LoginAsync` checks `MustChangePassword` before password verification
- [ ] `AuthService.ChangePasswordAsync` validates current password via BCrypt
- [ ] Login audit entries created for every success/failure/lockout
- [ ] `PermissionService.UpdateRolePermissionsAsync` uses `ExecuteTransactionAsync`
- [ ] DbSeeder seeds 33 permissions across 9 categories with 4-role assignments
- [ ] Default admin seeded passwordless (`PasswordHash = null`, `MustChangePassword = true`)

### Key Checkpoints
- [ ] `SetBatchSystemSettingsAsync()` does NOT call `SaveChangesAsync()` — check that the repo only prepares entities.
- [ ] Every `Update()` method in Domain entities calls `UpdateTimestamp()` at the end.
- [ ] `SystemSetting.Create()` validates `Category` (not empty) and `DataType` (whitelist: string/int/bool/decimal).
- [ ] Filtered unique indexes on soft-deletable entities include `AND [IsActive] = 1`.
- [ ] `SetStringAsync()` accepts a `category` parameter — no hardcoded categories.
- [ ] DbSeeder seeds ALL system settings (target: 29+).
- [ ] StoreSettings seed uses `defaultTaxRate: 0m`, not `15m`.
- [ ] Controllers differentiate NotFound(404) vs BadRequest(400) via `ErrorCodes.NotFound`.

### Phase 19 Settings Module — Enhancement Checks
- [ ] `Tax` entity has `SetDefault()` and `ClearDefault()` domain methods — NOT direct `IsDefault = true/false` in service code.
- [ ] `SystemSettingsViewModel` has ALL seeded settings as properties (check Print + Notifications sections exist).
- [ ] `StoreSettingsService.UpdateSystemSettingsAsync()` validates known keys by type before saving (boolean → `bool.TryParse`, integer → `int.TryParse` with ranges).

## Phase 22 — Chart of Accounts Module Checklist (v4.6.9+)

### Account Entity
- [ ] `Account.Create()` accepts exactly 13 parameters including `level` (5th param, required)?
- [ ] Level range validated with guard (1-10) — `DomainException` for out of range?
- [ ] `MarkAsDeleted()` guards `IsSystemAccount` AND `HasChildren()` — throws `DomainException` for either?
- [ ] `Update()` guards `IsSystemAccount` — system accounts are read-only?
- [ ] `HasChildren()` method exists for parent account deletion guard?
- [ ] `AccountConfiguration` has `CHK_Account_Level_Range [Level] >= 1 AND [Level] <= 10` CHECK constraint?
- [ ] `AccountConfiguration` has `.HasConversion<int>()` on `AccountType` enum property?
- [ ] Self-referencing `ParentAccountId` FK uses `DeleteBehavior.Restrict` (NOT Cascade)?
- [ ] `AccountConfiguration` has `.HasQueryFilter(x => x.IsActive)` for soft delete?
- [ ] All decimal fields use correct precision (OpeningBalance 18,2; no 18,4 anywhere)?

### AccountingSeeder
- [ ] Two-pass approach: first create Level 1-2 with `SaveChangesAsync()`, query IDs, then create Level 3-4 with `ParentAccountId`?
- [ ] Exactly 60 accounts seeded (5 L1 + 8 L2 + 20 L3 + 27 L4)?
- [ ] `AllowTransactions = false` for levels < 4; `AllowTransactions = true` for level >= 4?
- [ ] `IsSystemAccount = true` for Level 1-2 only (not L3-L4)?
- [ ] Color codes match: Asset=#2196F3, Liability=#F44336, Equity/Revenue=#4CAF50, Expense=#FF9800?
- [ ] `SystemAccountMappings` updated with new account codes (Cash, Bank, AR, AP, VAT, Capital, Sales, COGS, Inventory, Expense, Revenue, Discount, RetainedEarnings)?
- [ ] Seeder is idempotent — skips if `Accounts.Any()`?
- [ ] Semantics `accountCode` uses `==` (not `Equals(..., OrdinalIgnoreCase)`) in EF predicates (SQL is case-insensitive)?

### AccountService
- [ ] `CreateAsync()` validates: parent exists, `request.Level > parent.Level`, `request.Level <= 10`, unique `AccountCode`?
- [ ] `UpdateAsync()` guards system accounts — returns `Result.Failure` (not `DomainException`)?
- [ ] `DeleteAsync()` (soft delete) checks `HasChildren()` — returns `Result.Failure` with Arabic message?
- [ ] `PermanentDeleteAsync()` catches `DbUpdateException` and returns `Result.Failure`?
- [ ] `GetTreeAsync()` builds recursive tree from flat `List<Account>` — no recursive DB queries (N+1)?
- [ ] All methods return `Result<T>` or `Result` — no raw exceptions?

### ErrorCodes
- [ ] Uses `ErrorCodes.DuplicateEntry` (not `ErrorCodes.DuplicateBarcode`) for duplicate account code?
- [ ] Uses `ErrorCodes.NotFound` for entity not found?
- [ ] Uses `ErrorCodes.SystemAccountProtected` or generic "لا يمكن تعديل/حذف حساب نظام" message?

### AccountsController
- [ ] GET endpoints use `[Authorize(Policy = "AllStaff")]`?
- [ ] POST/PUT use `[Authorize(Policy = "ManagerAndAbove")]`?
- [ ] DELETE soft uses `[Authorize(Policy = "ManagerAndAbove")]`?
- [ ] DELETE permanent uses `[Authorize(Policy = "AdminOnly")]`?
- [ ] Controller returns `404 NotFound` when service returns `ErrorCodes.NotFound`?
- [ ] Controller returns `400 BadRequest` for business validation errors (not 404)?
- [ ] Controller does NOT inject `DbContext` or `IUnitOfWork` directly?

### Phase 22 Code Review Bug Fixes (v4.6.9+)

These items MUST be checked for any code touching Chart of Accounts or Account entity:

#### HasChildren() & Entity Fetch Rules
- [ ] `HasChildren()` domain guard NOT used as sole parent-account protection — service uses `AnyAsync(a => a.ParentAccountId == id)` DB query?
- [ ] Entity NOT fetched twice in `DeleteAsync()`/`PermanentDeleteAsync()` — use already-loaded entity?
- [ ] `PermanentDeleteAsync()` catches `DbUpdateException` and returns `Result.Failure` with Arabic message?

#### Explanation Field Completeness
- [ ] `Explanation` field exists in ALL layers: Domain entity (`string?` nullable), EF config (`nvarchar(500)`), DTO, Request, Service mapping, Validator (`MaxLength(500)`), Seeder?
- [ ] All seeded accounts have Arabic `explanation` text (not null)?
- [ ] `Explanation` field is `string?` nullable (not `string` non-nullable)?

#### Controller Route Constraints
- [ ] No `:byte` or unsupported route constraints (`:sbyte`, `:short`, `:ushort`, `:uint`, `:ulong`) — use `:int`, `:int:min(1):max(N)`, `:guid`, or `:string` instead?
- [ ] Route range matches the actual enum value range (e.g., AccountType 1-5, not hardcoded `3`)?

#### Validator Completeness
- [ ] Update Validators have SAME field validations as Create Validators?
- [ ] Level-1 account code length enforced at exactly 3 characters in Create validator?
- [ ] `nameof` operator used in `RuleFor` calls (not string literals)?

#### Desktop ViewModel Integrity
- [ ] Account code is read-only during edit mode (`IsAccountCodeReadOnly = true`)?
- [ ] Edit/Delete commands implemented with toolbar buttons in ListViewModel?
- [ ] Search/filter works in BOTH TreeView and DataGrid modes?

#### Health Check
- [ ] Health check uses `SecureDbContextFactory.GetDecryptedConnectionString()` (not raw `IConfiguration`)?
- [ ] Connection string NOT leaked into logs or SqlConnection attempt before DPAPI decryption?

### AccountValidators
- [ ] `CreateAccountRequestValidator` validates: code format (4-10 digits), NameAr required, ColorCode hex format, OpeningBalance non-negative?
- [ ] `UpdateAccountRequestValidator` validates: NameAr required, AccountType in enum, Level in range?

### Contracts (DTOs)
- [ ] `AccountDto` has `AccountTypeDisplay` (string from AccountType enum) and `LevelDisplay` (e.g., "المستوى 2") computed properties?
- [ ] `AccountTreeNodeDto` has recursive `Children: List<AccountTreeNodeDto>` for TreeView rendering?

### Desktop AccountApiService
- [ ] Typed HTTP client with `try-catch` and `Serilog.Log.Error` for connection failures?
- [ ] Content-type guard before `ReadFromJsonAsync`?
- [ ] Arabic error messages for timeout/network failures?

### Desktop AccountsListViewModel
- [ ] Implements `IDisposable` (not just `Cleanup()`) — EventBus subscription disposed?
- [ ] Dual-mode toggle (`IsTreeView` / `ToggleViewCommand`)?
- [ ] Search by name or code; filter by AccountType?
- [ ] Add/Edit/Delete commands have NO CanExecute predicates (always enabled per RULE-059)?
- [ ] `AddCommand` uses `RelayCommand` (not `AsyncRelayCommand`) with no predicate?
- [ ] Edit/Delete use `_screenWindowService.OpenScreen()` (non-modal) — NOT `ShowDialog()`?
- [ ] Events: EventBus subscription with auto-refresh on `AccountChangedMessage`?
- [ ] Minor success messages use `IToastNotificationService` (not modal dialogs)?

### Desktop AccountEditorViewModel
- [ ] Dual constructor: parameterless (delegates to service locator) + parameterized (DI)?
- [ ] `SetDialogService()` called in constructor (RULE-227)?
- [ ] `ValidateAsync()` follows RULE-229/338: `ClearAllErrors()` → `AddError()` per field → `await ValidateAllAsync()`?
- [ ] `SaveCommand` has NO CanExecute predicate (RULE-059)?
- [ ] `INotifyDataErrorInfo` for real-time validation (no `HasXxxError` booleans)?
- [ ] Level auto-set from parent account when parent changes?
- [ ] `IToastNotificationService` for success feedback after save?

### Desktop Views (XAML)
- [ ] `AccountsListView` has dual-mode: TreeView with `HierarchicalDataTemplate` + DataGrid flat view toggle?
- [ ] TreeView uses `HierarchicalDataTemplate` with `ItemsSource="{Binding Children}"` for recursive rendering?
- [ ] TreeView nodes show colored indicator (`Background="{Binding ColorCode}"`)?
- [ ] Editor form has: Code, NameAr, NameEn, Type combo, Level (read-only), ColorCode, OpeningBalance, AllowTransactions checkbox?
- [ ] All interactive controls have Arabic ToolTips (RULE-185-190)?
- [ ] Compact styles per RULE-262-274 (no hardcoded Height=36/40, Padding=16+)?
- [ ] Required fields marked with `*` and have ToolTip explaining validation rule?

### Desktop DI + Navigation
- [ ] `IAccountApiService` registered as singleton in `App.xaml.cs`?
- [ ] `AccountsListViewModel` and `AccountEditorViewModel` registered as transient?
- [ ] `AccountsListView` and `AccountEditorView` registered as transient?
- [ ] MainWindow sidebar has "دليل الحسابات" button?
- [ ] MainWindow menu has "الحسابات" item under appropriate category?
- [ ] `MainViewModel` has `NavigateToChartOfAccountsCommand`?

### Build & Tests
- [ ] `dotnet build` — 0 errors, 0 warnings across all projects?
- [ ] Domain tests for Account entity: Create (guards, 13 params), Update (system guard), MarkAsDeleted (system + children guard), IsDebitNormal, Level range?
- [ ] Service tests: CreateAsync (success, parent not found, duplicate code), DeleteAsync (children guard, not found), PermanentDeleteAsync (hard delete guard)?
- [ ] Controller integration tests: all CRUD endpoints, auth policies (AllStaff vs ManagerAndAbove vs AdminOnly)?

### Phase 23 — Customers Module Checklist (65-table schema: Parties-based, No CustomerGroup)

- [ ] Customer has `PartyId` (int, non-nullable FK to Parties) — mandatory, NOT optional?
- [ ] Customer has `AccountId` (int, non-nullable FK to Account) — mandatory, auto-created by service?
- [ ] Customer has NO `CustomerGroupId`, NO `CustomerType`, NO `OpeningBalance`, NO `CurrentBalance`, NO `CurrencyId`?
- [ ] Supplier follows same pattern: `PartyId` mandatory, `AccountId` mandatory, no balance/type fields?
- [ ] `Party` entity exists with `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`?
- [ ] `Party` is `ActivatableEntity` with `.HasQueryFilter(x => x.IsActive)`?
- [ ] `Customer.CheckCreditLimit()` returns bool (never throws)?
- [ ] Service auto-creates Level-4 account under `"1210 — العملاء"` for customers?
- [ ] Service auto-creates Level-4 account under `"2100 — حسابات الموردين"` for suppliers?
- [ ] Account auto-creation uses `GetByCodeAsync("1210")` / `GetByCodeAsync("2100")` for parent lookup?
- [ ] Account code auto-increments: `(int.Parse(maxCode ?? "1210") + 1).ToString()`?
- [ ] Seeder creates default "عميل نقدي" with Party + Account under 1210?
- [ ] Seeder creates default "مورد نقدي" with Party + Account under 2100?
- [ ] Desktop Editor has NO CustomerGroup dropdown, CustomerType radio, AccountId selector, OpeningBalance input?
- [ ] Desktop Editor has Party fields: Name, Phone, Email, Address, TaxNumber (editable)?
- [ ] Desktop Editor has AccountName display-only label?
- [ ] Desktop List has NO group filter, NO CustomerType filter?
- [ ] API routes use `api/v1/customers` (no customer-groups endpoints)?
- [ ] NO CustomerGroup controller, service, or repository exists in V1?
- [ ] NO CustomerType enum exists in the codebase?
- [ ] All FK relationships use `DeleteBehavior.Restrict`?
- [ ] CustomerDto has `AccountId`, `AccountName`, `PartyId`, `PartyName` — NO `CustomerGroupId`, `CustomerType`?
- [ ] Phone regex `^05\d{8}$` + Email `.EmailAddress()` in validators?
- [ ] ComboBox uses `ModernComboBox` style (NOT `ModernTextBox`)?
- [ ] ComboBox does NOT have both `DisplayMemberPath` and `ItemTemplate` set?

### 65-Table Schema Checks (New Entity Patterns)
- [ ] `ProductPrices` table exists with `ProductUnitId`, `CurrencyId`, `Price` decimal(18,2), `EffectiveFrom`, `EffectiveTo`?
- [ ] `InventoryBatches` table exists with `ProductId`, `WarehouseId`, `BatchNo`, `ExpiryDate`, `QuantityReceived/Remaining` decimal(18,3), `UnitCost` decimal(18,2)?
- [ ] `Units` table uses `smallint` PK (not `int`) — `.HasColumnType("smallint")`?
- [ ] `Units` is an independent table with seed data (not embedded in ProductUnit)?
- [ ] `ProductUnit` has `UnitId` FK (FK to Units table), `Factor`, `IsBaseUnit`, `DefaultPurchaseUnit`, `DefaultSalesUnit` flags?
- [ ] `WarehouseTransfer`/`WarehouseTransferLine` replaces `StockTransfer`/`StockTransferItem`?
- [ ] `InventoryTransaction`/`InventoryTransactionLine` replaces `InventoryMovement`?
- [ ] `AuditLog` uses `long Id` (bigint) — `.HasColumnType("bigint")`?
- [ ] `ImportLog` uses `long Id` (bigint) for high-volume data?
- [ ] `SystemLog.Level` uses `tinyint` (not string)?
- [ ] `Currency.IsBaseCurrency` is IMMUTABLE — no public setter or domain method to change after creation?
- [ ] `Currency` entity has `IsSystem` flag protecting seed currencies from deletion?
- [ ] `Currency` entity has `FractionName` field for sub-currency display?
- [ ] All lookup tables (Units, Roles, Departments, Currencies, Branches, Taxes) use `smallint` PK?
- [ ] Perpetual Inventory: NO `Purchases` account — all inventory costs go direct to Inventory Asset?
- [ ] Purchase return reversal credits `PurchaseReturnAccountId` (not InventoryAsset directly)?

### FORBIDDEN Patterns (Customers + Schema)
- ❌ `CustomerGroupId`, `CustomerGroupName`, `CustomerType` in any V1 Customer entity, DTO, request, or service
- ❌ `SupplierType` in any V1 Supplier entity, DTO, request, or service
- ❌ `OpeningBalance`/`CurrentBalance`/`CurrencyId` on Customer or Supplier entity
- ❌ User-supplied AccountId in customer/supplier Create/Update requests (auto-created by service)
- ❌ CustomerGroupController, service, repository, or Desktop UI in V1 codebase
- ❌ `ModernTextBox` style on `ComboBox` elements (use `ModernComboBox`)
- ❌ `DisplayMemberPath` and `ItemTemplate` on same `ComboBox`
- ❌ `:byte` route constraint on any controller
- ❌ `StockTransfer`/`StockTransferItem` (use `WarehouseTransfer`/`WarehouseTransferLine`)
- ❌ `InventoryMovement` (use `InventoryTransaction`/`InventoryTransactionLine`)
- ❌ `decimal(18,4)` for any money/quantity field (use 18,2 or 18,3)
- ❌ `int` PK for AuditLog or ImportLog (use `long`/bigint)

## Output Format
For each file, report: `✅ PASS` or `❌ FAIL: [specific violation]`

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Parties-based (Party entity), no CustomerGroup/SupplierType, Account auto-created under 1210/2100, no balance fields on Customer/Supplier |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries, COGS (AverageCost), Payment reversals, per-entity account routing |
| 25 — Products Module | 📝 Planned | ProductPrices (per unit×currency×effective dates), Units independent table (smallint PK), ProductUnit with Factor/IsBaseUnit, InventoryBatches (FIFO), Perpetual Inventory, product images, opening stock |
| 26 — Warehouses Module | 📝 Planned | WarehouseTransfer/WarehouseTransferLine (replaces StockTransfer), InventoryTransaction/InventoryTransactionLine (replaces InventoryMovement), warehouse types, AccountId FK |
| 27 — Purchases Module | 📝 Planned | Multi-currency, landed cost (AdditionalCharge), Purchase Orders, standalone returns |
| 28 — Sales Module | 📝 Planned | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement |
| 29 — Receipts & Payments | 🟡 Partial — CashBox ✅ | CashBox refactored (no balance fields, AccountId FK, RunningBalance); Cheques, PaymentAllocation, DailyClosure planned |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency (CurrencyId + ExchangeRate), attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export via ClosedXML |

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

### CashBox Architecture (v4.9 — No Balance Fields)
- [ ] CashBox has NO OpeningBalance/CurrentBalance fields (removed)?
- [ ] CashBox has AccountId FK linking to Chart of Accounts Account?
- [ ] CashTransaction uses RunningBalance (not BalanceBefore/BalanceAfter)?
- [ ] CashTransaction.Create() is public (not internal)?
- [ ] CashBox auto-creates Level-4 sub-account under "1110 — النقدية" when AccountId is null?
- [ ] No Deposit()/Withdraw() methods on CashBox entity?
- [ ] CashTransfer has NO client-side balance validation?
- [ ] CashBoxEditorView has NO balance fields (OpeningBalance removed)?
- [ ] CashBoxDto has NO balance fields?
- [ ] DbSeeder does NOT create default cash box (accounts not yet seeded)?

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. CashBox with OpeningBalance/CurrentBalance → REMOVE both fields, add AccountId FK
2. CashTransaction with BalanceBefore/BalanceAfter → REPLACE with RunningBalance
3. CashTransaction.Create() internal → CHANGE to public
4. Deposit()/Withdraw() methods on CashBox → REMOVE
5. Client-side balance validation → REMOVE (server validates via Account)
6. Missing `PartyId` FK on Customer/Supplier → Add it and create Party record
7. Missing `AccountId` FK on CashBox → Add it and auto-create account under "1110 — النقدية"
8. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
9. Missing `ProductPrices` table → Add per-unit pricing replacing SalePrice/RetailPrice on Product
10. Missing `InventoryBatches` → Add FIFO batch tracking on purchase
11. Missing `AdditionalCharges` table → Add landed cost allocation on purchase
12. Missing journal entry on cash operations → Call AccountingIntegrationService
13. Missing Excel export on report → Add ClosedXML worksheet generation
14. COGS using PurchaseCost → Change to AverageCost from ProductUnit
15. Payment without allocation → Add PaymentAllocation tracking
16. Missing reversal entries on payment update/delete → Add reversal journal entries
17. Old `StockTransfer`/`StockTransferItem` → Replace with `WarehouseTransfer`/`WarehouseTransferLine`
18. Old `InventoryMovement` → Replace with `InventoryTransaction`/`InventoryTransactionLine`
19. CustomerGroup/SupplierType references → Remove (deferred to V2)
20. OpeningBalance/CurrentBalance on Customer/Supplier/CashBox → Remove (balance on linked Account)
21. PriceLevel/RetailPrice/WholesalePrice on Product → Replace with ProductPrices per-unit pricing
22. NON-smallint PK on lookup tables → Change to smallint with `.HasColumnType("smallint")`
23. NON-bigint PK on AuditLog → Change to bigint with `.HasColumnType("bigint")`
24. Missing filtered unique indexes → Add `.HasFilter("[IsActive] = 1")`
