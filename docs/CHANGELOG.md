# Changelog

All notable changes to this project will be documented in this file.

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
