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
- [ ] Multi-table operations in `BeginTransactionAsync`?
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

## Output Format
For each file, report: `✅ PASS` or `❌ FAIL: [specific violation]`
