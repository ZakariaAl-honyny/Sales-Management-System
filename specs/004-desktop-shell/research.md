# Research: Desktop Shell

**Branch**: `004-desktop-shell` | **Date**: 2026-05-08
**Input**: Spec + Constitution + Existing codebase analysis

---

## R-001: Dependency Injection in WinForms (.NET 10)

**Decision**: Use `Microsoft.Extensions.DependencyInjection` with an `IServiceCollection` composition root in `Program.cs`.

**Rationale**: .NET 10 WinForms supports `Microsoft.Extensions.Hosting` which provides the full DI container, configuration (`IConfiguration`), and `IHttpClientFactory` — all of which are needed. `IHttpClientFactory` specifically requires the generic host infrastructure to function correctly with named clients, polly retry policies, and proper `HttpMessageHandler` lifetime management.

**Pattern**:
```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHttpClient<IAuthApiService, AuthApiService>(client =>
        {
            client.BaseAddress = new Uri(ctx.Configuration["ApiSettings:BaseUrl"]!);
        });
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddTransient<MainForm>();
        services.AddTransient<LoginForm>();
    })
    .Build();
ApplicationConfiguration.Initialize();
Application.Run(host.Services.GetRequiredService<LoginForm>());
```

**Alternatives considered**:
- Manual composition root (new instances) — Rejected: doesn't support `IHttpClientFactory` properly, no lifetime management.
- Static service locator — Rejected: anti-pattern, not testable, violates Clean Architecture.

---

## R-002: API Base URL Configuration via appsettings.json

**Decision**: Use `appsettings.json` with `Microsoft.Extensions.Configuration` binding into a typed `ApiSettings` class.

**Rationale**: This is the standard .NET configuration approach. The Generic Host (`Host.CreateDefaultBuilder()`) automatically loads `appsettings.json` from the application directory, making it immediately available. The file can be pre-configured by the installer (Inno Setup) and edited by the administrator without rebuilding.

**Configuration structure**:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

**Class**:
```csharp
public class ApiSettings { public string BaseUrl { get; set; } = string.Empty; }
```

**Alternatives considered**:
- Environment variable — More suitable for containerized/server apps, not desktop.
- Registry — Windows-only complexity, harder to edit.

---

## R-003: EventBus Implementation Pattern

**Decision**: A thread-safe, in-process pub/sub `EventBus` using `ConcurrentDictionary<Type, List<WeakReference<Delegate>>>` with weak references for automatic GC cleanup.

**Rationale**: Using `WeakReference<Delegate>` prevents memory leaks from forgotten subscriptions (defense-in-depth alongside RULE-012 explicit unsubscribe). The dictionary key is the message type, making dispatch O(1) for type lookup. The `IDisposable` subscription token pattern ensures deterministic cleanup.

**Pattern**:
```csharp
public interface IEventBus
{
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
    void Publish<TMessage>(TMessage message) where TMessage : class;
}
```

**Subscription token**:
```csharp
private class Subscription : IDisposable
{
    private readonly Action _unsubscribe;
    public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
    public void Dispose() => _unsubscribe();
}
```

**UI-thread marshaling** (per RULE-013): Handlers invoke via `form.Invoke()` if `form.InvokeRequired`.

**Alternatives considered**:
- Static event — Not mockable, no lifetime control.
- `System.Reactive` (Rx) — Heavy dependency, outside approved NuGet list.

---

## R-004: NavigationService Pattern

**Decision**: `NavigationService` holds a reference to the `Panel` content area of `MainForm`. It swaps controls by calling `Dispose()` on the current control, clearing the panel, and instantiating the new `UserControl` via the DI container.

**Rationale**: Resolving screens via DI (`serviceProvider.GetRequiredService<T>()`) ensures all screen dependencies (API services, EventBus) are properly injected. Disposing the old control triggers the `Dispose(bool)` → `_subscription?.Dispose()` chain mandated by RULE-012.

**Pattern**:
```csharp
public interface INavigationService
{
    void NavigateTo<TControl>() where TControl : UserControl;
    void SetContentPanel(Panel panel);
}
```

**Alternatives considered**:
- Form-per-module (MDI) — Heavy, complex window management, not mobile-friendly layout.
- Static control factory — No DI injection possible.

---

## R-005: JWT Token Management (In-Memory Session)

**Decision**: `SessionService` (Singleton) holds the current `UserSession` in a private field. `AuthApiService` calls the login endpoint, maps the response to a `UserSession`, stores it in `SessionService`, and configures the `HttpClient` `DefaultRequestHeaders.Authorization` bearer token.

**Rationale**: An in-memory singleton respects RULE-VIII (token never persisted to disk). The singleton lifetime matches the application lifetime. Token expiry detection uses HTTP 401 response interception via a custom `DelegatingHandler` that calls `SessionService.SignOut()` and raises a session-expired event on the EventBus.

**UserSession model**:
```csharp
public class UserSession
{
    public int UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string Token { get; init; } = string.Empty;
}
```

**Alternatives considered**:
- Windows Credential Manager — Persists token, violates RULE-VIII.
- Static field — Not mockable, not testable.

---

## R-006: Toast Notification Implementation (Bottom-Right, Auto-Dismiss 3s)

**Decision**: A frameless `Form` subclass (`ToastForm`) displayed as a non-focus-stealing overlay using `ShowWithoutActivation = true` and `WS_EX_NOACTIVATE` extended style. Position calculated from `Screen.PrimaryScreen.WorkingArea` (bottom-right minus margin). Auto-dismiss via `System.Windows.Forms.Timer` (3 seconds).

**Rationale**: WinForms has no built-in toast control. A frameless helper form is the standard approach that renders above all content without disrupting keyboard focus. Three distinct background colors distinguish success (green), error (red), and warning (orange).

**Alternatives considered**:
- `ToolTip` control — Doesn't support custom styling or auto-dismiss timing.
- `StatusStrip` — Permanent bar, not transient.
- Third-party library — Not in approved NuGet list.

---

## R-007: Reusable Common Controls

**Decision**: Build the following WinForms `UserControl` components:

| Control | Type | Key Behavior |
|---------|------|-------------|
| `SearchBarControl` | `UserControl` | `TextBox` + debounce `System.Windows.Forms.Timer` (300ms delay). Fires `SearchChanged` event. |
| `LoadingOverlayControl` | `UserControl` | Semi-transparent `Panel` + animated `PictureBox` (animated GIF). Docked `Fill`. Show/Hide methods. |
| `SummaryCardControl` | `UserControl` | Title `Label` + Value `Label` + optional icon `PictureBox`. Properties: `Title`, `Value`, `Icon`. |
| `MoneyTextBox` | `TextBox` subclass | Overrides `OnKeyPress` to allow digits, `.`, and backspace only. Formats to 2dp on `Leave`. |

**Alternatives considered**:
- Third-party component suite (DevExpress, Telerik) — Not in approved NuGet list, requires license.

---

## R-008: Sidebar Design (Always Expanded, RTL-Aware)

**Decision**: A vertical `Panel` (fixed width 220px, docked `Left`) containing a `FlowLayoutPanel` for menu items. Each item is a `Button` styled to appear as a navigation entry. Active item highlighted via `BackColor` change. RTL layout supported via `RightToLeft = Yes` on the sidebar panel.

**Rationale**: Simpler than a `TreeView` or third-party menu component. Pure WinForms with no dependencies. Fixed width avoids layout complexity. RTL property handles Arabic text direction natively.

**Alternatives considered**:
- `ListView`— More complex state management.
- `TreeView` — Unnecessary hierarchy for single-level sidebar.

---

## Summary: All NEEDS CLARIFICATION Resolved

| Item | Resolution |
|------|-----------|
| DI Container | Microsoft.Extensions.Hosting (Generic Host) |
| API URL Config | `appsettings.json` → typed `ApiSettings` |
| EventBus pattern | `ConcurrentDictionary` + `WeakReference<Delegate>` + `IDisposable` token |
| NavigationService | DI-resolved `UserControl` in a content `Panel` |
| Token storage | In-memory `SessionService` singleton |
| Token expiry | 401 `DelegatingHandler` + EventBus session-expired message |
| Toast position | Bottom-right `Form` overlay, 3s auto-dismiss |
| Common controls | 4 WinForms `UserControl` components |
| Sidebar | `Panel` + `FlowLayoutPanel` with buttons, 220px fixed width |
