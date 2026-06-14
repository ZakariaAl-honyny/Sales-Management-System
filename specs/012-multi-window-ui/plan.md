# Implementation Plan: Multi-Window & UI Polish (v4.5)

**Branch**: `012-multi-window-ui` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This feature transforms the WPF Desktop client from a single-window, modal-driven application into a fully multitasking-capable desktop experience. It introduces a `ScreenWindowService` that manages non-modal editors via a generic `ScreenWindow` host, replaces all raw `MessageBox.Show` calls with the centralized `IDialogService`, enforces newest-first list sorting across all screens, and adds Arabic ToolTips to every interactive control. The architecture ensures zero memory leaks from non-modal windows by using `WeakReference<Window>` for tracking and strict `EventBus` disposal in ViewModel `Cleanup()`.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (WPF Desktop only)
**Architecture Scope**: Entirely within `SalesSystem.DesktopPWF` — no Domain, Application, Infrastructure, or API changes
**Target**: Retail cashiers and managers who need to work on multiple documents simultaneously (e.g., creating a sales invoice while checking a purchase invoice)
**Constraints**:
- Zero memory leaks — non-modal windows and EventBus subscriptions are the highest risk area
- Self-ownership crash in dialog `Owner` resolution must be eliminated
- `System.Windows.MessageBox.Show` must have zero occurrences in the codebase after this phase
- Arabic ToolTips must be embedded inline in XAML, not extracted to resource dictionaries
- List sorting must be applied client-side on `ObservableCollection` population, not assumed from API order

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No financial math changes |
| II | Domain Formulas | ✅ N/A | No formula changes |
| III | Transactional Integrity | ✅ N/A | No DB writes in this phase |
| IV | Invoice Lifecycle | ✅ N/A | No invoice logic changes |
| V | Stock Integrity | ✅ N/A | No stock logic changes |
| VI | Result Pattern | ✅ N/A | Desktop UI only; no Result<T> changes |
| VII | Architecture Boundaries | ✅ PASS | All changes stay within DesktopPWF |
| VIII | Security | ✅ N/A | No security changes |
| IX | Four-Layer Validation | ✅ N/A | No validation changes |
| X | Logging | ✅ N/A | No new logging |
| XI | EF Core Conventions | ✅ N/A | No EF Core changes |
| XII | Audit Trail | ✅ N/A | No audit changes |
| XIII | Delete Strategy | ✅ N/A | No delete logic changes |
| XIV | Defensive Programming | ✅ PASS | Dialog owner resolution defends against InvalidOperationException |
| XV | WPF Dialogs | ✅ PASS | Mandates 100% IDialogService usage |
| XVI | Toast Notifications | ✅ N/A | No toast changes |
| XVII | Real-Time UI Validation | ✅ N/A | Validation pattern unchanged |

**Gate Result**: ✅ ALL CLEAR — No rules violated.

---

## Multi-Window Screen Management

### ScreenWindowService — The Central Window Manager

A dedicated `IScreenWindowService` interface and its `ScreenWindowService` implementation become the single authority for opening all non-modal windows. The service is registered as a singleton in the Desktop DI container so that window count tracking persists across the application lifetime. Every ViewModel that needs to open a new window (editor or list) receives `IScreenWindowService` via constructor injection — never creates windows directly.

#### Window Tracking with WeakReference

The service maintains a `List<WeakReference<Window>>` for all open non-modal windows. Each weak reference allows the garbage collector to reclaim closed windows without the service holding a strong reference that would cause a memory leak. When a new window is requested, the service first purges any `WeakReference` entries whose target has been collected (by checking `Target != null` and `Target.IsVisible`). Dead entries are removed from the list before computing the cascade offset.

#### Naming Convention for View Resolution

The `OpenScreen(viewModel, options)` overload resolves the View type from the ViewModel type using a deterministic naming convention:

1. Take the ViewModel's full type name, e.g., `SalesSystem.DesktopPWF.ViewModels.Products.ProductEditorViewModel`
2. Replace the `ViewModels` segment with `Views`: `SalesSystem.DesktopPWF.Views.Products.ProductEditorViewModel`
3. Replace the `ViewModel` suffix with `View`: `SalesSystem.DesktopPWF.Views.Products.ProductEditorView`
4. Attempt to instantiate the resolved type via the DI container (which handles constructor injection)

If the resolved type does not exist or cannot be instantiated, the service logs a Serilog error and falls back to a `ContentControl` with a "View not found" message. This convention eliminates the need for a ViewModel-to-View registration map — any ViewModel with a corresponding View in the `Views` namespace automatically works.

#### Service Overloads

The service exposes two overloads for opening windows:

1. **`OpenScreen(object viewModel, ScreenWindowOptions options)`** — Primary method for opening editors. Resolves the View by naming convention, creates a `ScreenWindow`, sets the View as its Content, and shows the window non-modally. Returns the created `ScreenWindow` instance so callers can attach to window-level events if needed.

2. **`OpenWindow(Window window, ScreenWindowOptions options)`** — Accepts a pre-created `Window` instance for cases where the caller needs to customize beyond what `ScreenWindow` provides (e.g., list screens opened as standalone windows from the MainWindow menu, or windows with specific toolbar configurations). The service still applies cascade positioning and tracking.

#### ScreenWindowOptions

The `ScreenWindowOptions` class carries all configuration for the new window:
- `Title` (string) — Arabic window title, e.g. "فاتورة بيع جديدة"
- `Width` / `Height` (double?) — optional explicit dimensions; defaults to 800×600
- `OnClosed` (Action<object>?) — callback invoked after the window closes and the ViewModel cleans up; receives the ViewModel as parameter
- `IsModal` (bool) — reserved for future modal use; currently always false

### ScreenWindow — The Generic Host

`ScreenWindow` is a minimal `Window` subclass with no code-behind logic beyond setting its `Content` to the provided View. It contains no knowledge of what ViewModel it hosts — it is purely a shell. The window's `Title` is set from `ScreenWindowOptions.Title` (always an Arabic string like "فاتورة بيع جديدة" — never English or CLR type names). All `ScreenWindow` instances use `MinWidth="500"` and `MinHeight="350"` to ensure editors have adequate space while remaining compact.

The `ScreenWindow` hooks the `Closed` event to trigger the lifecycle: it calls `(DataContext as ViewModelBase)?.Cleanup()` and then invokes the `OnClosed` callback. This ensures cleanup happens regardless of how the window is closed (user clicks ✕, Esc key, or programmatic Close()). The ViewModel's `IsDisposed` flag is set to prevent double-cleanup if `Cleanup()` is called manually.

### ViewModel Lifecycle

When the user triggers a close action (close button, Esc key, or a ViewModel-invoked `CloseRequested` event), the following sequence executes:

1. The ViewModel raises `CloseRequested` (an `event EventHandler` on `ViewModelBase`).
2. `ScreenWindowService` intercepts this by handling the event, closes the owning `ScreenWindow` via `window.Close()`, and calls `ViewModel.Cleanup()`.
3. `Cleanup()` disposes all EventBus subscriptions, cancels any pending `CancellationTokenSource` (via `_cts.Cancel()`), sets `IsDisposed = true`, and releases any other unmanaged resources. The method is designed to be idempotent — calling it twice does not throw.
4. The `OnClosed` callback from `ScreenWindowOptions` fires as the last step, marshaling any UI thread operations through `Application.Current.Dispatcher.InvokeAsync()`. This is critical because the callback might need to refresh a list on the MainWindow — accessing UI elements from a finalizer or background thread would crash with an `InvalidOperationException`.

#### Cleanup and IDisposable Contract

Every ViewModel that subscribes to the `EventBus` must implement `IDisposable` and call `Cleanup()` in its `Dispose()` method. The `ScreenWindowService` does NOT call `Dispose()` directly — it calls `Cleanup()` when the window closes. The `Dispose()` pattern is reserved for cases where the DI container or a `using` block manages the ViewModel lifetime outside of the window lifecycle.

The `IsDisposed` guard property prevents `ExecuteAsync()` from running operations after cleanup. Every async command checks `if (IsDisposed) return;` at the start of its execution method to prevent "ObjectDisposedException" or operations on stale data.

### Cascade Positioning Logic

The offset anchors on the MainWindow's current position to handle multi-monitor setups correctly. The formula is:

```text
mainWindow = Application.Current.MainWindow
baseLeft = mainWindow.Left
baseTop  = mainWindow.Top
offset   = (purgedWindowCount % 10) * 30
newLeft  = baseLeft + offset
newTop   = baseTop + offset
```

This means:
- Window 0: offset from MainWindow by (30, 30)
- Window 1: offset by (60, 60)
- ...
- Window 9: offset by (300, 300)
- Window 10: resets to (30, 30) — wraps around to keep windows on-screen

After computing the position, the service validates against `SystemParameters.WorkArea` (the visible desktop area excluding the taskbar). If the computed `newLeft + window.Width` exceeds `WorkArea.Right`, the window is shifted left. If `newTop + window.Height` exceeds `WorkArea.Bottom`, the window is shifted up. This prevents windows from opening partially off-screen, which would confuse users and make title bars inaccessible.

The cascade logic also respects per-monitor DPI: if the MainWindow is on a high-DPI monitor, the offset is scaled by the monitor's DPI ratio to ensure consistent visual spacing.

### Auto-Titles

Every editor ViewModel declares a static `EntityDisplayName` string in its metadata (e.g., `"فاتورة بيع"`, `"منتج"`, `"عميل"`). `ScreenWindowService` appends "جديد" for create mode or the entity name for edit mode. Titles are never English class names — the user always sees Arabic window captions.

### MainWindow Integration

The MainWindow sidebar gains a "فتح نافذة جديدة" submenu under each module heading. For example:
- المبيعات → فتح نافذة مبيعات جديدة → opens a `SalesInvoicesListView` in a new `ScreenWindow`
- المشتريات → فتح نافذة مشتريات جديدة → opens `PurchaseInvoicesListView`
- المنتجات → فتح نافذة منتجات جديدة → opens `ProductsListView`
- العملاء → فتح نافذة عملاء جديدة → opens `CustomersListView`

This allows users to monitor a list while editing a record in a separate window. Each "فتح نافذة جديدة" menu item uses `ScreenWindowService.OpenWindow()` with a pre-created list View and its ViewModel, configured with the list's title and default dimensions (1000×700 for list screens).

## MessageBox.Show Elimination Strategy

After this phase, `System.Windows.MessageBox.Show` must have zero occurrences in the DesktopPWF project. The replacement strategy:

1. **Validation errors** → `IDialogService.ShowWarningAsync()` with aggregated error list
2. **Save success messages** → `IToastNotificationService.ShowSuccess()` (auto-dismisses after 3 seconds)
3. **Confirmation prompts** → `IDialogService.ShowConfirmationAsync()` returning `bool`
4. **Delete confirmation** → `IDialogService.ShowDeleteConfirmationAsync()` returning `DeleteStrategy`
5. **System errors** → `IDialogService.ShowErrorAsync()` with Arabic-friendly message (raw exception logged via Serilog)
6. **Information messages** → `IDialogService.ShowInfoAsync()` with blue theme

A search for `MessageBox.` in all `.cs` files under `SalesSystem.DesktopPWF` must return zero results. Any third-party or library code that uses MessageBox internally is acceptable only if it is not directly called by application code.

---

## Dialog Service Ownership Resolution

The `IDialogService` implementation (`DialogService`) must safely resolve the owner window for every dialog. The previous pattern of blindly setting `Owner = Application.Current.MainWindow` caused `InvalidOperationException` ("Cannot set Owner property to itself") when the dialog was triggered from a non-modal `ScreenWindow` whose DataContext happened to be the same as MainWindow's.

The fix uses a multi-step owner resolution strategy:

1. Get the active window via `Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)`
2. If the active window is not null and is not `this` (the dialog itself), set it as Owner
3. If the active window equals `this` (self-ownership guard), fall back to `WindowStartupLocation.CenterScreen`
4. If no active window exists, use `Application.Current.MainWindow` with a null check
5. If MainWindow is also null (during shutdown), center on screen

This prevents the crash in all scenarios:
- Dialog triggered from MainWindow → owner is MainWindow
- Dialog triggered from a ScreenWindow editor → owner is the ScreenWindow instance
- Dialog triggered during application shutdown → centers on screen (no crash)
- Dialog triggered from a background thread with no active window → centers on screen

The `FallbackErrorDialog` (a thread-safe, overlay-based error dialog) is used for unhandled exceptions that occur outside the normal dialog flow, such as in `AppDomain.CurrentDomain.UnhandledException` or `TaskScheduler.UnobservedTaskException`. This dialog does not set an Owner — it uses `WindowStartupLocation.CenterScreen` and `Topmost = true` to ensure visibility.

## Application Shutdown Mode Change

With non-modal windows, the standard WPF `ShutdownMode="OnLastWindowClose"` would keep the application alive as long as any ScreenWindow is open, which is incorrect — closing all non-modal editors should not keep the app running. Conversely, `ShutdownMode="OnMainWindowClose"` shuts down the entire application when the MainWindow closes, which is the desired behavior.

The `App.xaml` changes to `ShutdownMode="OnExplicitShutdown"` to give full control over the shutdown lifecycle:

- **MainWindow close** → If not logging out, call `Application.Current.Shutdown()`
- **LoginWindow close button** → Calls `Application.Current.Shutdown()` (not just `Close()`)
- **Logout flow** → Sets a `_isLoggingOut` flag, opens LoginWindow, then calls `this.Close()` on MainWindow — the flag prevents the MainWindow.Closed handler from calling Shutdown() during logout

This ensures that closing the main application window (or the login window) terminates the process, while closing individual ScreenWindow editors does not.

---

## Newest-First List Sorting

Every list ViewModel applies `.OrderByDescending(x => x.Id)` when populating its `ObservableCollection` from the API response. Invoice-type lists use `.OrderByDescending(x => x.InvoiceDate)` for proper chronological newest-first ordering. This override is applied client-side because the API may return data in a different order depending on the query. Specifically:
- Product, Customer, Supplier, Warehouse lists: sort by `Id` descending
- SalesInvoice, PurchaseInvoice lists: sort by `InvoiceDate` descending (then by `Id` as tiebreaker)
- Payment and receipt lists: sort by `Id` descending
- Return lists: sort by `ReturnDate` descending

---

## Arabic ToolTips

Every interactive control (Button, MenuItem, TextBox, ComboBox, ListBoxItem, Hyperlink) across all 63+ views receives an Arabic `ToolTip` describing the action or expected input. This is a massive horizontal change — every XAML file in the Desktop project is touched — but each change is mechanically simple (adding a `ToolTip` attribute).

### ToolTip Content Guidelines

1. **Action buttons** explain the consequence, not just the name. For example:
   - Save button: `"حفظ البيانات المدخلة — سيتم التحقق من صحة الحقول قبل الحفظ"`
   - Post button: `"ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد"`
   - Cancel button: `"إلغاء العملية وتجاهل جميع التغييرات"`
   - Delete button: `"حذف هذا العنصر — يمكن استعادته لاحقاً من قائمة العناصر المحذوفة"`

2. **Navigation menu items** describe the destination screen:
   - "المبيعات" menu: `"عرض وإدارة فواتير البيع"`
   - "المنتجات" menu: `"إدارة قائمة المنتجات وإضافة منتجات جديدة"`
   - "التقارير" menu: `"عرض التقارير المالية والإدارية"`

3. **Input fields** explain the data format and requirement:
   - Phone textbox: `"أدخل رقم الجوال — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام"`
   - Barcode textbox: `"أدخل الباركود — يجب أن يكون فريداً لكل منتج"`
   - Date picker: `"اختر التاريخ من التقويم"`

4. **Error dismiss button**: `"إخفاء رسالة الخطأ"`

5. **Empty-state buttons** (shown when a list is empty):
   - "➕ إضافة أول عميل": `"إضافة أول عميل للبدء في إدارة العملاء"`

### Implementation Strategy

ToolTips are added inline in XAML as attribute strings — no resource dictionaries, no localization files. This matches the existing Arabic-first approach where all user-facing strings are embedded directly in the markup. Each view file is updated individually, with the `ToolTip` property bound directly (for dynamic ToolTips) or as a static attribute (for static ToolTips). Views with DataTemplate-based controls require special attention to ensure ToolTips are set on the correct visual element within the template.

---

## EventBus Memory Management

The `EventBus` is a in-memory message bus that allows decoupled communication between ViewModels. When a list ViewModel needs to refresh after a record is created or edited, it subscribes to a typed message (e.g., `ProductChangedMessage`) and reloads its data when the message is published.

### Subscription Pattern

Every subscription returns an `IDisposable` token. The token MUST be stored as a class field and disposed in `Cleanup()`:

```text
ViewModel Constructor:
    _subscription = _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged)

Cleanup():
    _subscription?.Dispose()
    _subscription = null
```

### Why WeakReference Is Not Enough

The EventBus holds a delegate reference to the subscriber's handler method. Even if the ViewModel's Window is garbage-collected, the EventBus subscription keeps a strong reference to the ViewModel via the delegate target. This is a classic memory leak pattern in event-driven architectures. Explicit disposal of the subscription token is the ONLY reliable way to break this reference chain.

### What Subscribes

Only list ViewModels that auto-refresh subscribe to the EventBus. Editor ViewModels do NOT subscribe — they are opened and closed, and on close they may publish a change message. The publisher side (editor ViewModels) publishes once before closing:

```text
Editor Save → Publish EntityChangedMessage(entityId) → EventBus delivers to all subscribers
List ViewModel receives → ReloadDataAsync() → ObservableCollection repopulated
```

### Dispatcher Safety

The EventBus dispatches messages on the publisher's thread. If the publisher is on a background thread (e.g., during an API call), the subscriber's handler MUST marshal UI updates to the dispatcher. The `ViewModelBase.InvokeOnUIThreadAsync()` method handles this with `Application.Current.Dispatcher.InvokeAsync()`. List ViewModels subscribe with an automatic dispatcher marshal wrapper to ensure thread safety without cluttering every handler.

---

## Project Structure

```text
SalesSystem.DesktopPWF/
├── Services/
│   ├── App/
│   │   └── ScreenWindowService.cs        ← NEW: IScreenWindowService + implementation
│   └── Dialog/
│       └── DialogService.cs              ← UPDATE: Fix owner resolution, add safe fallback
├── Views/
│   ├── ScreenWindow.xaml/.cs             ← NEW: Generic non-modal window host
│   └── Resources/Styles.xaml             ← UPDATE: Minor polish (no structural changes)
└── ViewModels/
    └── Base/
        └── ViewModelBase.cs              ← UPDATE: Add CloseRequested event, Cleanup() virtual, IsDisposed guard
```

All 14 editor ViewModels receive `IToastNotificationService` for success feedback. All 14 list ViewModels implement `IDisposable` with EventBus subscription disposal. All 63+ views receive Arabic ToolTips. All 15+ list screens apply `OrderByDescending`.

---

## Verification Checklist

- [ ] `ScreenWindowService` opens non-modal editors via `OpenScreen(viewModel, options)`
- [ ] Cascade offset = `(count % 10) * 30` from MainWindow
- [ ] `WeakReference<Window>` prevents memory leaks — profiler confirms GC after window close
- [ ] `OnClosed` callback marshals UI updates via `Dispatcher.InvokeAsync()`
- [ ] All dialog `Owner` resolution guards against self-ownership (`mainWindow != this`)
- [ ] `FallbackErrorDialog` shown for unhandled exceptions (thread-safe)
- [ ] 0 instances of `MessageBox.Show` in the codebase
- [ ] All lists display newest-first (OrderByDescending on Id or date)
- [ ] All interactive controls have Arabic ToolTips
- [ ] `ViewModelBase.Cleanup()` disposes EventBus subscriptions
- [ ] MainWindow has "فتح نافذة جديدة" menu items for all list screens
- [ ] Build: 0 errors, 0 warnings
