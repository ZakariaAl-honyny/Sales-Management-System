# Changelog

All notable changes to this project will be documented in this file.

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
