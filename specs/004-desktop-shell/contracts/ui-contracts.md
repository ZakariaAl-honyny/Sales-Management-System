# UI Contracts: Desktop Shell

**Branch**: `004-desktop-shell` | **Date**: 2026-05-08

---

## Overview

This document defines the contracts for all UI components (Forms, UserControls, Services) in the Desktop Shell. These contracts govern how Phase 5 module screens interact with shell services.

---

## Service Contracts

### ISessionService

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// Manages the current user's authenticated session (in-memory only).
/// </summary>
public interface ISessionService
{
    /// <summary>The current session, or null if not authenticated.</summary>
    UserSession? Current { get; }

    /// <summary>True if a valid session exists.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Stores the authenticated session after successful login.
    /// NEVER persists to disk (Constitution Rule VIII).
    /// </summary>
    void SignIn(UserSession session);

    /// <summary>
    /// Clears the session. Called on logout or 401 token expiry.
    /// </summary>
    void SignOut();
}
```

---

### IAuthApiService

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// Communicates with the backend authentication endpoint.
/// Endpoint: POST /api/v1/auth/login
/// </summary>
public interface IAuthApiService
{
    /// <summary>
    /// Authenticates with the API. Returns UserSession on success,
    /// or Failure with the raw API error message on failure (per clarification Q3).
    /// </summary>
    Task<Result<UserSession>> LoginAsync(
        string userName,
        string password,
        CancellationToken ct = default);
}
```

**Backend endpoint contract**:
- `POST /api/v1/auth/login`
- Request body: `{ "userName": string, "password": string }`
- 200 OK response: `{ "token": string, "userId": int, "userName": string, "fullName": string, "role": byte }`
- 401 response: `{ "error": string }`

---

### INavigationService

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// Manages screen navigation in the content panel area of MainForm.
/// Disposes the current control before loading the next.
/// </summary>
public interface INavigationService
{
    /// <summary>Registers the content Panel managed by this service.</summary>
    void SetContentPanel(Panel panel);

    /// <summary>
    /// Navigates to the given UserControl type.
    /// Resolves the control via DI, disposes the previous control.
    /// </summary>
    void NavigateTo<TControl>() where TControl : UserControl;

    /// <summary>The Type of the currently displayed control, or null.</summary>
    Type? CurrentScreen { get; }
}
```

---

### IEventBus

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// In-process pub/sub event bus for cross-screen notifications.
/// Messages carry entity IDs only — NO data payloads (Constitution Rule EventBus).
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to messages of type TMessage.
    /// Returns a disposable subscription token.
    /// MUST be disposed in Dispose(bool disposing) of the subscriber.
    /// </summary>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        where TMessage : class;

    /// <summary>
    /// Publishes a message to all current subscribers of type TMessage.
    /// Handlers are invoked on the UI thread via Control.Invoke if needed.
    /// </summary>
    void Publish<TMessage>(TMessage message)
        where TMessage : class;
}
```

---

### INotificationService

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// Displays transient toast notifications in the bottom-right corner.
/// Auto-dismisses after 3 seconds. Does NOT block UI interaction.
/// </summary>
public interface INotificationService
{
    /// <summary>Shows a green success toast with the given message.</summary>
    void ShowSuccess(string message);

    /// <summary>Shows a red error toast with the raw API error message.</summary>
    void ShowError(string message);

    /// <summary>Shows an orange warning toast with the given message.</summary>
    void ShowWarning(string message);
}
```

---

### IDialogService

```csharp
namespace SalesSystem.Desktop.Services.Interfaces;

/// <summary>
/// Provides confirmation dialogs for destructive actions.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog in Arabic.
    /// Returns true if user confirmed, false if cancelled.
    /// </summary>
    bool Confirm(string message, string title = "تأكيد");
}
```

---

## UserControl Contracts

### BaseModuleControl (Abstract Base)

All Phase 5 module screens MUST inherit from this to ensure consistent EventBus lifecycle:

```csharp
namespace SalesSystem.Desktop.Controls;

public abstract class BaseModuleControl : UserControl
{
    private readonly List<IDisposable> _subscriptions = new();

    /// <summary>Register subscriptions here. Called by OnLoad automatically.</summary>
    protected abstract void RegisterSubscriptions();

    /// <summary>Add a subscription token to be auto-disposed on Dispose.</summary>
    protected void AddSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterSubscriptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var s in _subscriptions) s.Dispose();
            _subscriptions.Clear();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

---

### Common Control APIs

#### SearchBarControl

```csharp
// Bindable event — fires 300ms after last keystroke
public event EventHandler<string>? SearchChanged;

// Properties
public string Placeholder { get; set; }  // Arabic placeholder text
public string SearchText  { get; }       // Current search value
```

#### LoadingOverlayControl

```csharp
// Methods
public void Show();   // Overlay appears over parent control
public void Hide();   // Overlay disappears

// Properties
public string LoadingText { get; set; }  // Default: "جاري التحميل..."
```

#### SummaryCardControl

```csharp
// Properties
public string  Title { get; set; }   // Arabic card title
public string  Value { get; set; }   // Formatted display value
public Image?  Icon  { get; set; }   // Optional icon (16x16 or 32x32)
public Color   AccentColor { get; set; }  // Left border accent color
```

#### MoneyTextBox

```csharp
// Inherits TextBox
// Allows: digits, single '.', backspace only
// OnLeave: formats to 2 decimal places (e.g. "1500" → "1500.00")
public decimal DecimalValue { get; }  // Parsed value, 0 if invalid
```

---

## Message Contracts

```csharp
namespace SalesSystem.Desktop.Messages;

// All messages: ID only — NO data (Constitution Rule EventBus)
public abstract record EntityChangedMessage(int EntityId);

public record ProductChangedMessage(int EntityId)          : EntityChangedMessage(EntityId);
public record CustomerChangedMessage(int EntityId)         : EntityChangedMessage(EntityId);
public record SupplierChangedMessage(int EntityId)         : EntityChangedMessage(EntityId);
public record SalesInvoiceChangedMessage(int EntityId)     : EntityChangedMessage(EntityId);
public record PurchaseInvoiceChangedMessage(int EntityId)  : EntityChangedMessage(EntityId);
public record StockChangedMessage(int EntityId)            : EntityChangedMessage(EntityId);
public record SessionExpiredMessage(int EntityId = 0)      : EntityChangedMessage(EntityId);
```

---

## NuGet Dependencies (Desktop-only addition)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Hosting` | 10.x | Generic Host, DI, Configuration |

> All other packages already in the approved list (`Microsoft.Extensions.Http`, `System.Text.Json`) are pulled in transitively by the Generic Host.
