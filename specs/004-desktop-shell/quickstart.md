# Quick Start: Desktop Shell Development

**Branch**: `004-desktop-shell` | **Date**: 2026-05-08

---

## Prerequisites

- .NET 10 SDK installed
- SQL Server running (for backend API)
- Phase 1–3 API running on `https://localhost:7001`

---

## Project Structure (this feature)

```text
SalesSystem/SalesSystem.Desktop/
├── Program.cs                        ← Generic Host + DI composition root
├── appsettings.json                  ← API base URL configuration
│
├── Configuration/
│   └── ApiSettings.cs                ← Typed config binding
│
├── Models/
│   ├── UserSession.cs                ← In-memory session entity
│   ├── NavigationItem.cs             ← Sidebar menu entry
│   └── Notification.cs              ← Toast notification model
│
├── Messages/
│   └── Messages.cs                   ← EventBus message types (ID-only)
│
├── Services/
│   ├── Interfaces/
│   │   ├── ISessionService.cs
│   │   ├── IAuthApiService.cs
│   │   ├── INavigationService.cs
│   │   ├── IEventBus.cs
│   │   ├── INotificationService.cs
│   │   └── IDialogService.cs
│   │
│   ├── SessionService.cs
│   ├── AuthApiService.cs
│   ├── NavigationService.cs
│   ├── EventBus.cs
│   ├── NotificationService.cs
│   ├── DialogService.cs
│   └── Http/
│       └── AuthTokenHandler.cs       ← DelegatingHandler for 401 detection
│
├── Forms/
│   ├── LoginForm.cs / .Designer.cs   ← Initial login screen
│   └── MainForm.cs / .Designer.cs    ← Shell with sidebar + content panel
│
└── Controls/
    ├── Common/
    │   ├── SearchBarControl.cs
    │   ├── LoadingOverlayControl.cs
    │   ├── SummaryCardControl.cs
    │   └── MoneyTextBox.cs
    └── Placeholders/
        ├── DashboardControl.cs        ← Placeholder for Phase 5
        ├── ProductsControl.cs
        ├── CustomersControl.cs
        ├── SuppliersControl.cs
        ├── WarehousesControl.cs
        ├── PurchasesControl.cs
        ├── SalesControl.cs
        ├── ReturnsControl.cs
        ├── TransfersControl.cs
        ├── PaymentsControl.cs
        ├── ReportsControl.cs
        ├── SettingsControl.cs
        └── UsersControl.cs
```

---

## Setting Up the Project

### Step 1 — Add NuGet Packages

```bash
# From SalesSystem.Desktop directory
dotnet add package Microsoft.Extensions.Hosting --version 10.*
```

> All other packages (`System.Text.Json`, `Microsoft.Extensions.Http`) are included in the Generic Host.

### Step 2 — Add appsettings.json

Create `SalesSystem/SalesSystem.Desktop/appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

Set **Copy to Output Directory** = `Copy if newer` in the `.csproj`:
```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Step 3 — Validate Build

```bash
dotnet build SalesSystem/SalesSystem.Desktop/SalesSystem.Desktop.csproj
```

---

## Key Development Patterns

### Implementing a New Screen UserControl

```csharp
public partial class ProductsControl : UserControl
{
    private readonly IProductApiService _api;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notifications;
    private IDisposable? _subscription;

    public ProductsControl(IProductApiService api, IEventBus eventBus,
                           INotificationService notifications)
    {
        _api = api;
        _eventBus = eventBus;
        _notifications = notifications;
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Subscribe — ALWAYS in OnLoad
        _subscription = _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
        _ = LoadDataAsync();
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        // Re-query API — do NOT use msg data (RULE-034)
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync() { /* call API, populate grid */ }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription?.Dispose(); // MUST unsubscribe (RULE-012)
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### Publishing an EventBus Message

```csharp
// After saving a product:
_eventBus.Publish(new ProductChangedMessage(savedProduct.Id));
// Only pass the ID — no data payload (RULE-034)
```

### Navigating to a Screen

```csharp
// From sidebar button click:
_navigationService.NavigateTo<ProductsControl>();
```

### Showing Notifications

```csharp
_notificationService.ShowSuccess("تم الحفظ بنجاح");
_notificationService.ShowError(result.Error!);
```

### Confirming Destructive Actions

```csharp
if (_dialogService.Confirm("هل أنت متأكد من الحذف؟"))
{
    // proceed with delete
}
```

---

## Verification Checklist

After completing implementation, verify:

- [ ] Login succeeds with valid API credentials
- [ ] Login shows Arabic error for invalid credentials
- [ ] Sidebar shows 13 items for Admin role
- [ ] Sidebar shows 10 items for Manager role (no Warehouses, Settings, Users)
- [ ] Sidebar shows 3 items for Cashier role (Sales, Returns, Customers)
- [ ] Clicking each sidebar item loads the placeholder control
- [ ] Previous control is disposed on navigation (confirmed via breakpoint in Dispose)
- [ ] Toast notification appears bottom-right, auto-dismisses in 3 seconds
- [ ] Confirmation dialog appears before destructive action
- [ ] App launches windowed at 1280×800
- [ ] Logout clears session and returns to LoginForm
- [ ] API unavailable → Arabic error message, no crash
