# Data Model: Desktop Shell

**Branch**: `004-desktop-shell` | **Date**: 2026-05-08
**Source**: spec.md + research.md

---

## 1. Desktop-Side Entities

These entities live exclusively in `SalesSystem.Desktop` memory — they are NOT persisted to disk.

---

### 1.1 UserSession

**Purpose**: Represents the authenticated user's identity and token for the current session.

```csharp
namespace SalesSystem.Desktop.Models;

public sealed class UserSession
{
    public int    UserId   { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role   { get; init; }
    public string Token    { get; init; } = string.Empty;

    // Derived helpers
    public bool IsAdmin   => Role == UserRole.Admin;
    public bool IsManager => Role is UserRole.Admin or UserRole.Manager;
}
```

**Validation rules**:
- `Token` MUST be non-null, non-empty after successful login.
- `UserId` MUST be > 0.
- `Role` MUST map to a known `UserRole` enum value.

**Lifecycle**:
- Created: when `AuthApiService.LoginAsync()` returns 200 OK.
- Destroyed: on logout, token expiry (401 response), or application exit.
- Stored: in `SessionService` singleton — NEVER written to disk.

---

### 1.2 NavigationItem

**Purpose**: Describes a sidebar menu entry.

```csharp
namespace SalesSystem.Desktop.Models;

public sealed class NavigationItem
{
    public string    Label       { get; init; } = string.Empty; // Arabic label
    public string    IconKey     { get; init; } = string.Empty; // Image resource key
    public Type      ScreenType  { get; init; } = null!;        // UserControl type to load
    public UserRole  MinRole     { get; init; }                 // Minimum required role
    public bool      IsVisible(UserRole userRole) => userRole <= MinRole;
}
```

**Role visibility logic**:
```
Admin (1)   → sees ALL items (MinRole 1, 2, or 3)
Manager (2) → sees items where MinRole >= 2
Cashier (3) → sees items where MinRole = 3
```

**Navigation registry** (defined in `NavigationService`):
| Label (Arabic) | MinRole | Screen |
|----------------|---------|--------|
| لوحة التحكم | Admin | DashboardControl |
| المنتجات | Manager | ProductsControl |
| العملاء | Cashier | CustomersControl |
| الموردون | Manager | SuppliersControl |
| المستودعات | Admin | WarehousesControl |
| فواتير الشراء | Manager | PurchasesControl |
| فواتير البيع | Cashier | SalesControl |
| مرتجعات | Cashier | ReturnsControl |
| تحويل المخزون | Manager | TransfersControl |
| المدفوعات | Manager | PaymentsControl |
| التقارير | Manager | ReportsControl |
| الإعدادات | Admin | SettingsControl |
| المستخدمون | Admin | UsersControl |

---

### 1.3 EventMessage (Base)

**Purpose**: Lightweight pub/sub message. Carries only the entity identifier (per RULE-034).

```csharp
namespace SalesSystem.Desktop.Messages;

// Base — all messages derive from this
public abstract record EntityChangedMessage(int EntityId);

// Concrete message types
public record ProductChangedMessage(int EntityId)    : EntityChangedMessage(EntityId);
public record CustomerChangedMessage(int EntityId)   : EntityChangedMessage(EntityId);
public record SupplierChangedMessage(int EntityId)   : EntityChangedMessage(EntityId);
public record SalesInvoiceChangedMessage(int EntityId) : EntityChangedMessage(EntityId);
public record PurchaseInvoiceChangedMessage(int EntityId) : EntityChangedMessage(EntityId);
public record StockChangedMessage(int EntityId)      : EntityChangedMessage(EntityId);
public record SessionExpiredMessage(int EntityId = 0) : EntityChangedMessage(EntityId);
```

**Constraints**:
- Messages MUST NOT carry data payloads (per RULE-034).
- Subscribers MUST re-query the API to get fresh data.

---

### 1.4 Notification

**Purpose**: A transient user-visible message rendered as a toast.

```csharp
namespace SalesSystem.Desktop.Models;

public enum NotificationType { Success, Error, Warning }

public sealed class Notification
{
    public string           Message  { get; init; } = string.Empty;
    public NotificationType Type     { get; init; }
    public int              Duration { get; init; } = 3000; // ms, default 3s
}
```

---

## 2. Configuration Model

```csharp
namespace SalesSystem.Desktop.Configuration;

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7001";
}
```

**Binding** (`appsettings.json`):
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

---

## 3. Service Interfaces

### 3.1 ISessionService

```csharp
public interface ISessionService
{
    UserSession? Current { get; }
    bool IsAuthenticated { get; }
    void SignIn(UserSession session);
    void SignOut();
}
```

### 3.2 IAuthApiService

```csharp
public interface IAuthApiService
{
    Task<Result<UserSession>> LoginAsync(string userName, string password, CancellationToken ct = default);
}
```

### 3.3 INavigationService

```csharp
public interface INavigationService
{
    void SetContentPanel(Panel panel);
    void NavigateTo<TControl>() where TControl : UserControl;
    Type? CurrentScreen { get; }
}
```

### 3.4 IEventBus

```csharp
public interface IEventBus
{
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
    void Publish<TMessage>(TMessage message) where TMessage : class;
}
```

### 3.5 INotificationService

```csharp
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
}
```

### 3.6 IDialogService

```csharp
public interface IDialogService
{
    bool Confirm(string message, string title = "تأكيد");
}
```

---

## 4. State Transitions

### Session Lifecycle

```text
App Launch
    │
    ▼
[LoginForm shown]
    │ valid credentials
    ▼
[UserSession created → SessionService.SignIn()]
    │
    ▼
[MainForm shown] ←─────────────────────────────────────────┐
    │                                                        │
    │  API returns 401        User clicks Logout             │
    ▼                         ▼                             │
[SessionService.SignOut()]   [SessionService.SignOut()]      │
    │                                                        │
    ▼                                                        │
[SessionExpiredMessage published]                           │
    │                                                        │
    ▼                                                        │
[LoginForm shown] ──────────────────── re-login ───────────┘
```

### Navigation Lifecycle

```text
User clicks sidebar item
    │
    ▼
NavigationService.NavigateTo<TControl>()
    │
    ├─ Dispose previous UserControl (→ unsubscribes EventBus)
    ├─ Clear Panel.Controls
    ├─ Resolve new TControl via IServiceProvider
    ├─ Dock = Fill
    └─ Panel.Controls.Add(newControl) → triggers OnLoad → Subscribe EventBus
```

---

## 5. Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Decimal-Only | ✅ N/A | No financial calculations in shell |
| II. Domain Formulas | ✅ N/A | No formulas in shell layer |
| III. Transactional Integrity | ✅ N/A | No DB writes in Desktop |
| VI. Result Pattern | ✅ Applied | `IAuthApiService.LoginAsync` returns `Result<UserSession>` |
| VII. Clean Architecture | ✅ Applied | Desktop → HttpClient → API only (no DB) |
| VIII. Security | ✅ Applied | Token in-memory only, never persisted |
| XII. EventBus Rules | ✅ Applied | Subscribe OnLoad, unsubscribe Dispose, ID-only messages |
| X. Logging | ⚠️ Deferred | Desktop logging added in Phase 7 (production hardening) |

**No violations. Gates PASSED.**
