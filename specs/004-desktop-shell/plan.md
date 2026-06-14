# Implementation Plan: Desktop Shell

**Branch**: `004-desktop-shell` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/004-desktop-shell/spec.md`

---

## Summary

Build the WPF desktop shell for the Sales Management System, hosted in the `SalesSystem.DesktopPWF` project. This phase establishes the application foundation: MVVM framework, styled dialogs, toast notifications, non-modal window management, startup orchestration (DB health check → Login → MainWindow), typed HTTP clients for API communication, and native Windows sound feedback. The shell uses the .NET Generic Host for DI composition, reads the API base URL from `appsettings.json`, and adheres to the strict Clean Architecture rule that the Desktop NEVER connects to the database directly.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10-windows
**Primary Dependencies**: `Microsoft.Extensions.Hosting` 10.x, `Microsoft.Extensions.Http` 10.x, `System.Text.Json` 10.x, `Serilog.Sinks.File`
**Storage**: In-memory only (JWT token, UserSession). Config from `appsettings.json`.
**Testing**: Manual integration tests against running API. Unit tests for ViewModels and services.
**Target Platform**: Windows 10/11 (WPF, .NET 10-windows)
**Project Type**: WPF desktop application (MVVM pattern)
**Performance Goals**: Login < 3s | List load < 2s | Navigation < 500ms | EventBus dispatch < 100ms
**Constraints**: Token NEVER persisted (Constitution Rule VIII) | Desktop NEVER connects to DB (Rule VII) | EventBus messages carry ID only (RULE-034) | INotifyDataErrorInfo required (RULE-228) | All FKs Restrict | No Cascade delete
**Scale/Scope**: Shell framework + 1 main window + 1 login window + 5 dialog types + toast overlay + 6 core services + typed HttpClient factory

---

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Decimal-Only Financial Precision | ✅ N/A | Shell has no financial calculations |
| II. Domain-Computed Financial Formulas | ✅ N/A | No formulas in UI shell |
| III. Transactional Integrity | ✅ N/A | No DB writes from Desktop |
| IV. Invoice Lifecycle | ✅ N/A | Shell does not manage invoice state |
| V. Stock Integrity | ✅ N/A | Shell does not handle stock |
| VI. Result Pattern | ✅ REQUIRED | All `IXxxApiService` methods return `Result<T>` |
| VII. Clean Architecture | ✅ REQUIRED | Desktop → HttpClient → API only. No direct DB access. |
| VIII. Security | ✅ REQUIRED | Token in-memory only. 401 handler triggers sign-out. |
| IX. Four-Layer Validation | ✅ Partial | INotifyDataErrorInfo in ViewModels. API-side FluentValidation. |
| X. Serilog Logging | ✅ REQUIRED | Serilog in App.xaml.cs startup. Log.Error for system errors, Log.Warning for user errors (RULE-182/183). |
| XI. EF Core Conventions | ✅ N/A | No EF Core in Desktop |
| XII. Audit Trail | ✅ N/A | Audit is an API-layer concern |
| DeleteBehavior.Restrict | ✅ N/A | No FK definitions in Desktop |
| Interactive Validation | ✅ REQUIRED | Buttons always enabled, validate on click with warning dialog (RULE-059) |
| INotifyDataErrorInfo | ✅ REQUIRED | ViewModelBase implements INotifyDataErrorInfo (RULE-228/229) |

**Gate Result: ✅ PASSED** — No violations.

---

## Project Structure

### Documentation (this feature)

```text
specs/004-desktop-shell/
├── plan.md              ← This file
├── spec.md              ← Feature specification
├── research.md          ← Phase 0: Design decisions
├── data-model.md        ← Phase 1: Entities + service interfaces
├── quickstart.md        ← Phase 1: Developer guide
├── contracts/
│   └── ui-contracts.md  ← Phase 1: Service + control contracts
└── tasks.md             ← Phase 2 output (speckit-tasks command)
```

### Source Code Layout

```text
SalesSystem/SalesSystem.DesktopPWF/
├── App.xaml / App.xaml.cs                   ← Startup orchestration + DI root
├── appsettings.json                         ← API base URL config
├── MainWindow.xaml / .cs                    ← Shell window with sidebar navigation
├── LoginWindow.xaml / .cs                   ← Authentication entry point
│
├── ViewModels/
│   ├── ViewModelBase.cs                     ← Base MVVM (INotifyPropertyChanged, INotifyDataErrorInfo,
│   │                                          ExecuteAsync, IsBusy, HandleFailure, LogSystemError,
│   │                                          ValidateAllAsync, SetDialogService, Cleanup)
│   ├── LoginWindowViewModel.cs              ← Login logic + token storage
│   └── MainViewModel.cs                     ← Navigation + role gating
│
├── Views/
│   ├── Login/
│   │   └── LoginView.xaml / .cs             ← Login form
│   ├── Dialogs/
│   │   ├── ErrorDialog.xaml / .cs           ← Red theme, error icon ✕
│   │   ├── SuccessDialog.xaml / .cs         ← Green theme, success icon ✓
│   │   ├── WarningDialog.xaml / .cs         ← Yellow/orange theme, warning icon ⚠
│   │   ├── InfoDialog.xaml / .cs            ← Blue theme, info icon ℹ
│   │   ├── ConfirmationDialog.xaml / .cs    ← Blue theme, question icon ?
│   │   ├── DeleteConfirmationDialog.xaml/.cs← Orange/red, 3 buttons (Cancel/Deactivate/Delete)
│   │   ├── ValidationErrorsDialog.xaml/.cs  ← Lists all validation errors in a scrollable dialog
│   │   ├── DatabaseErrorDialog.xaml / .cs   ← Retry/Exit on DB connection failure (RULE-154)
│   │   └── FallbackErrorDialog.xaml / .cs   ← Thread-safe overlay for unhandled exceptions
│   ├── ScreenWindow.xaml / .cs              ← Generic non-modal host for any View/ViewModel pair
│   └── Dashboard/
│       └── DashboardView.xaml / .cs         ← Summary cards, auto-refresh via EventBus
│
├── Services/
│   ├── App/
│   │   ├── IDialogService.cs / DialogService.cs       ← 10 dialog methods (async + sync, styled)
│   │   ├── IEventBus.cs / EventBus.cs                 ← In-memory pub/sub, ID-only messages
│   │   ├── ISessionService.cs / SessionService.cs     ← JWT token + user session in memory
│   │   ├── IScreenWindowService.cs / ScreenWindowService.cs  ← Non-modal window manager
│   │   ├── ScreenWindowOptions.cs                     ← Title, size, OnClosed options
│   │   ├── ISoundService.cs / SoundService.cs         ← Native Windows sounds (SystemSounds)
│   │   ├── IUpdaterService.cs / UpdaterService.cs     ← Background auto-update check
│   │   ├── INavigationService.cs / NavigationService.cs  ← Sidebar navigation + role gating
│   │   └── IPrinterService.cs                          ← Print API integration
│   ├── Api/
│   │   ├── IApiService.cs                             ← Base interface with HttpClient
│   │   ├── AuthApiService.cs                           ← Login/logout/token refresh
│   │   ├── DatabaseHealthCheckService.cs               ← Startup DB connectivity check
│   │   └── (typed ApiServices per module — Phase 5)
│   └── Toast/
│       ├── ToastNotificationService.cs                ← Auto-dismissing overlay (3s/5s)
│       ├── ToastWindow.xaml / .cs                      ← Toast overlay window
│       └── IToastNotificationService.cs               ← ShowSuccess/ShowError/ShowInfo
│
├── Messaging/
│   └── Messages/
│       └── AppMessages.cs                    ← EntityChangedMessage base + concrete types
│
├── Converters/                               ← XAML value converters (BoolToVisibility, etc.)
├── Enums/                                    ← Desktop-specific enums
├── Helpers/                                  ← Utility helpers
├── Models/                                   ← Desktop-side models (UserSession, NavigationItem)
└── Resources/
    └── Styles.xaml                           ← Global styles, ErrorTemplate, compact sizing
```

**Structure Decision**: Single WPF project `SalesSystem.DesktopPWF` (not WinForms). All ViewModels and Views organized by feature folder. Services split between `App/` (application infrastructure) and `Api/` (HTTP clients). The legacy `SalesSystem.Desktop` WinForms project does not exist — the system uses WPF exclusively.

---

## Key Design Decisions

### Decision 1: MVVM Framework — ViewModelBase

The `ViewModelBase` class provides the shared foundation for all ViewModels in the system. It implements `INotifyPropertyChanged` (via `SetProperty<T>`) and `INotifyDataErrorInfo` (via `AddError`/`ClearErrors`/`ClearAllErrors`/`ValidateAllAsync`). Core members:

| Member | Purpose |
|--------|---------|
| `SetProperty<T>(ref T, T, string)` | Property change notification with equality check |
| `ExecuteAsync(Func<Task>)` / `ExecuteResultAsync<T>` | Wraps async operations with `IsBusy` + `StatusMessage` + error handling (RULE-141–146) |
| `IsBusy` (bool) | Loading state indicator, replaces legacy `IsLoading` (RULE-142) |
| `StatusMessage` (string) | User-facing status during async operations |
| `HandleFailure(string, string)` | Logs at Warning level, transforms errors to Arabic, returns user-friendly string (RULE-172) |
| `LogSystemError(string, string, Exception?)` | Logs at Error level, reserved for system errors only (RULE-199) |
| `ValidateAllAsync()` | Checks `HasErrors`, shows validation warning dialog if errors present (RULE-229) |
| `ValidateField(Func<bool>, string, string)` | Single-field INotifyDataErrorInfo validation for property setters |
| `SetDialogService(IDialogService)` | Must be called in every Editor VM constructor (RULE-227) |
| `Cleanup()` | Virtual method for unsubscribing EventBus, disposed in `Dispose()` (RULE-012) |
| `CloseRequested` event | Signals the View (Window) to close |
| `FocusFirstInvalidFieldRequested` event | Signals the View to auto-focus the first error field |

### Decision 2: DialogService — 9 Styled Dialogs

The `IDialogService` interface provides a unified API for all user-facing messages. Every dialog is a dedicated WPF `Window` subclass with themed styling — NEVER raw `MessageBox.Show` (RULE-055).

| Dialog | Theme | Icon | Purpose |
|--------|-------|------|---------|
| `ErrorDialog` | Red | ✕ | System errors, API failures, unexpected exceptions |
| `SuccessDialog` | Green | ✓ | Successful saves, posts, exports |
| `WarningDialog` | Yellow/Orange | ⚠ | Business rule violations, missing data on validate |
| `InfoDialog` | Blue | ℹ | Informational messages |
| `ConfirmationDialog` | Blue | ? | Yes/No user confirmation |
| `DeleteConfirmationDialog` | Orange/Red | ✕ | 3-button (Cancel/Deactivate/Permanent) delete strategy |
| `ValidationErrorsDialog` | Yellow | ⚠ | Scrollable list of all validation errors (RULE-059) |
| `DatabaseErrorDialog` | Red | DB icon | Retry/Exit on startup DB connection failure (RULE-154) |
| `FallbackErrorDialog` | Red | ✕ | Thread-safe overlay for unhandled exceptions |

All dialogs use `GetActiveWindow()` for owner resolution with self-ownership guard (RULE-224/225). Async methods (`ShowErrorAsync`, `ShowSuccessAsync`, etc.) marshal to UI thread via `Dispatcher.InvokeAsync`. The `ShowDialog(object viewModel)` method resolves Views by naming convention (`ViewModel` → `View`) and supports `CloseRequested` event binding.

### Decision 3: ToastNotificationService — Auto-Dismissing Overlays

The toast notification system provides non-intrusive, auto-dismissing feedback for minor operations. Toasts are lightweight overlay windows that appear in the top-right corner of the active screen and auto-close after a configurable duration:

| Toast Type | Color | Duration | Use Case |
|------------|-------|----------|----------|
| `ShowSuccess` | Green | 3 seconds | Delete/restore confirmations, minor save success (RULE-289) |
| `ShowError` | Red | 5 seconds | API call failures with non-blocking severity |
| `ShowInfo` | Blue | 3 seconds | Informational feedback |

Implementation: `ToastWindow.xaml` is a borderless, topmost `Window` positioned via `SystemParameters.WorkArea`. Each toast auto-closes via `DispatcherTimer`. A static lock protects the `_activeToasts` list; new toasts dismiss existing ones before stacking.

### Decision 4: ScreenWindowService — Non-Modal Window Management

The `ScreenWindowService` manages all non-modal editor windows. It enforces the architecture rule that editors open non-modally (RULE-160/161) — NEVER via `ShowDialog()`.

| Feature | Implementation |
|---------|---------------|
| View resolution | `ViewModel` → `View` by naming convention (replace "ViewModel" with "View" in FullName) |
| Window hosting | `ScreenWindow.xaml` wraps any `FrameworkElement` (UserControl) with a host title bar |
| Direct window | If resolved type is a `Window`, opens it directly (backward compatibility) |
| WeakReference tracking | `List<WeakReference<Window>>` prevents memory leaks (RULE-163) |
| Cascade positioning | New windows offset `+30px` × `(count % 10)` from MainWindow (RULE-164) |
| OnClosed callback | Invokes `OnClosed` action via `Dispatcher.InvokeAsync()` for UI thread safety (RULE-165) |
| Close lifecycle | `CloseRequested` → close window → `Cleanup()` → fire `OnClosed` (RULE-166) |
| Auto-titles | Arabic names from `ScreenWindowOptions.Title` (e.g., "فاتورة بيع جديدة") (RULE-167) |

The `OpenScreen(object viewModel, ScreenWindowOptions?)` method is the primary entry point for editors. It resolves the View, creates or wraps it in a `ScreenWindow`, configures size/title/modal behavior from options, and tracks the window in the WeakReference list.

### Decision 5: Startup Flow — DB Health Check → Login → MainWindow

The application startup sequence follows a strict order in `App.xaml.cs`:

```
Application_Startup
  ├── Setup Serilog logging
  ├── Configure DI container (ServiceCollection)
  ├── CheckDatabaseConnectionAsync()        ← Blocks until success or user exits
  │     └── DatabaseErrorDialog (Retry/Exit) on failure
  ├── If authenticated → show MainWindow
  └── If not authenticated → show LoginWindow
```

| Step | Component | Notes |
|------|-----------|-------|
| Serilog | `SetupLogging()` | File sink in `%AppData%\SalesSystem\Logs` |
| DI | `ConfigureServices()` | Registers all services, ViewModels, Views, and typed HttpClients |
| DB health | `DatabaseHealthCheckService.CheckAsync()` | Calls `GET /api/v1/health/database`. Returns `HealthCheckResult` with Arabic error. Retry loop via `DatabaseErrorDialog` (RULE-153–154). |
| Auth | `ISessionService.IsAuthenticated` | Checks if valid JWT token exists in memory from previous session |
| Login | `LoginWindow` | Shown when no valid session. Credentials → `AuthApiService.LoginAsync()` → store token in `SessionService` |
| MainWindow | `MainWindow` | Shell with sidebar navigation, role-based visibility. `ShutdownMode="OnExplicitShutdown"` (RULE-178) |

**Shutdown rules** (RULE-178–181):
- `App.xaml` uses `ShutdownMode="OnExplicitShutdown"` — hidden ScreenWindow instances must not keep the app alive.
- `LoginWindow` close button calls `Application.Current.Shutdown()`.
- `MainWindow.Closed` calls `Application.Current.Shutdown()` except during logout (guarded by `_isLoggingOut` flag).
- Logout flow: set `_isLoggingOut = true`, clear session, show LoginWindow, then `this.Close()`.

### Decision 6: HttpClient Setup — Typed Clients With Auth Token

Desktop communicates with the API exclusively via typed `HttpClient` instances registered through `Microsoft.Extensions.Http`. Every call goes through the API layer — no direct database access (RULE-007).

| Pattern | Description |
|---------|-------------|
| Registration | `services.AddHttpClient<IXxxApiService, XxxApiService>(c => c.BaseAddress = new Uri(config.ApiBaseUrl))` |
| Auth token | `AuthTokenHandler` (DelegatingHandler) injects `Authorization: Bearer {token}` from `ISessionService` into every request |
| 401 handling | `AuthTokenHandler` intercepts 401 responses → clears session → navigates to LoginWindow |
| Base URL | Read from `appsettings.json` `ApiSettings:BaseUrl` — configurable per environment |
| Content-Type guard | `HandleResponseAsync()` checks `ContentType == "application/json"` before parsing error responses — prevents `JsonException` crash on empty/HTML 404 bodies (RULE-184) |

Each typed API service wraps `HttpClient` calls with `Result<T>` return types (RULE-006). The pattern:

```csharp
public async Task<Result<List<ProductDto>>> GetAllAsync(CancellationToken ct = default)
{
    var response = await _httpClient.GetAsync("api/v1/products", ct);
    return await HandleResponseAsync<List<ProductDto>>(response, ct);
}
```

### Decision 7: SoundService — Native Windows Audio Feedback

The `ISoundService` interface provides three audio cues using `System.Media.SystemSounds` (native Windows sounds, no external dependencies):

| Method | Sound | When Fired |
|--------|-------|------------|
| `PlaySuccess()` | `SystemSounds.Asterisk` | Successful save/post, barcode scan detected |
| `PlayError()` | `SystemSounds.Hand` | Pre-save validation failure, API error response |
| `PlayWarning()` | `SystemSounds.Exclamation` | Warning dialog display, business rule violation |
| `PlayNotification()` | `SystemSounds.Beep` | General notification (optional, unbound by default) |

All methods are fire-and-forget — they do not block the UI thread. Sound cues are integrated into ViewModels via `ISoundService` injection and called alongside `IDialogService`/`IToastNotificationService` for multi-modal feedback.

### Decision 8: EventBus — In-Memory Pub/Sub

The `IEventBus` provides cross-module communication within the desktop process. Messages carry entity ID only — no data payloads (RULE-034). All handlers must marshal UI updates to the dispatcher thread (RULE-013).

| Rule | Directive |
|------|-----------|
| Subscribe | In ViewModel constructor (RULE-012) |
| Unsubscribe | In `Dispose()` / `Cleanup()` (RULE-012) |
| Message payload | Entity ID only — `EntityChangedMessage<T>` with `int EntityId` (RULE-034) |
| UI thread | Handlers use `Application.Current.Dispatcher.InvokeAsync()` (RULE-013) |
| Concurrency | `ConcurrentDictionary<Type, List<Delegate>>` with per-type lock on mutation |

Concrete message types include `ProductChangedMessage`, `CustomerChangedMessage`, `SaleInvoiceChangedMessage`, etc. — one per module. All inherit from `EntityChangedMessage<T>`.

---

## Implementation Order

### Phase 4A — Core Infrastructure (Blocking)

1. **4A-01**: Create `ViewModelBase` with INotifyPropertyChanged, INotifyDataErrorInfo, `SetProperty`, `ExecuteAsync`, `IsBusy`, `StatusMessage`, `HandleFailure`, `LogSystemError`, `ValidateAllAsync`, `ValidateField`, `SetDialogService`, `Cleanup`, `RelayCommand`, `AsyncRelayCommand`
2. **4A-02**: Create `EventBus.cs` — `IEventBus` + `EventBus` (ConcurrentDictionary pub/sub)
3. **4A-03**: Create `IDialogService.cs` + `DialogService.cs` — 10 methods, styled dialogs, naming convention view resolution, `GetActiveWindow()` with self-ownership guard
4. **4A-04**: Create all 9 dialog Windows (Error, Success, Warning, Info, Confirmation, DeleteConfirmation, ValidationErrors, DatabaseError, FallbackError) + `ScreenWindow.xaml`

### Phase 4B — Application Services

5. **4B-01**: Create `ISessionService.cs` + `SessionService.cs` (in-memory token + user)
6. **4B-02**: Create `ISoundService.cs` + `SoundService.cs` (SystemSounds wrapper)
7. **4B-03**: Create `IToastNotificationService.cs` + `ToastNotificationService.cs` + `ToastWindow.xaml`
8. **4B-04**: Create `IScreenWindowService.cs` + `ScreenWindowService.cs` + `ScreenWindowOptions.cs`
9. **4B-05**: Create `IAuthApiService.cs` + `AuthApiService.cs` (login/logout/token)
10. **4B-06**: Create `IDatabaseHealthCheckService` + `DatabaseHealthCheckService.cs`
11. **4B-07**: Create `IUpdaterService.cs` + `UpdaterService.cs` (background update check)

### Phase 4C — Shell UI

12. **4C-01**: Create `App.xaml` + `App.xaml.cs` (startup orchestration, DI, Serilog, DB health check)
13. **4C-02**: Create `LoginWindow.xaml` + `LoginWindowViewModel.cs` + `LoginView`
14. **4C-03**: Create `MainWindow.xaml` + `MainViewModel.cs` (sidebar navigation, role gating, logout)
15. **4C-04**: Create `INavigationService` + `NavigationService.cs`
16. **4C-05**: Create `DashboardView.xaml` + `DashboardViewModel.cs`
17. **4C-06**: Create `Resources/Styles.xaml` (global styles, ErrorTemplate for INotifyDataErrorInfo)
18. **4C-07**: Create converters (`BoolToVisibility`, `InverseBool`, `StatusToColor`, etc.)
19. **4C-08**: Create messaging types in `Messaging/Messages/AppMessages.cs`

### Phase 4D — DI Registration

20. **4D-01**: Register all services, ViewModels, and typed HttpClients in `ConfigureServices()`
21. **4D-02**: Configure `appsettings.json` with `ApiSettings:BaseUrl`
22. **4D-03**: Wire up Serilog with file sink + desktop-friendly configuration

---

## Complexity Tracking

| Concern | Status |
|---------|--------|
| Constitution violations | ✅ None — shell is a pure UI infrastructure layer |
| Clean Architecture violation | ✅ None — Desktop never connects to DB |
| Dependency explosion | ✅ 6 core services + 1 typed client (auth) — no bloat |
| Cross-cutting concerns | ✅ Logging, DI, EventBus, error handling all centralized |
| Thread safety | ✅ EventBus uses ConcurrentDictionary; Toast uses lock; ScreenWindowService uses lock for WeakReference list |
| Self-ownership guard | ✅ `GetActiveWindow()` checks `owner != window` before setting Owner |
| Shutdown mode | ✅ `OnExplicitShutdown` prevents hidden ScreenWindow instances from keeping app alive |
