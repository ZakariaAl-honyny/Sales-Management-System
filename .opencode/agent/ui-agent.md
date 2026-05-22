---
name: "UI Agent"
reasoningEffect: high
role: "WPF UI specialist (MVVM)"
activation: "When working on SalesSystem.DesktopPWF/**"
mode: subagent
---

# UI Agent — WPF Desktop Specialist (MVVM)

## MUST READ FIRST
- `AGENTS.md` — Rules 007, 012, 013, 034
- `docs/ui-screens.md` — Screen structure (Views/ViewModels) and EventBus patterns

## Architecture (WPF + MVVM)
```text
MainWindow.xaml (Shell)
├── Sidebar (navigation)
├── TopBar (user info, logout)
└── ContentArea (hosts Views via Frame/ContentControl)
    ├── Views/Products/ProductListView.xaml
    ├── Views/Sales/SalesInvoiceEditorView.xaml
    └── ... (View bound to its ViewModel)
```

## EventBus Rules (CRITICAL — WPF Implementation)

### Subscribe in ViewModel Constructor or OnLoad:
```csharp
public ProductListViewModel(IEventBus eventBus)
{
    _eventBus = eventBus;
    _subscription = _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
}
```

### Unsubscribe in Dispose (MANDATORY):
```csharp
public void Dispose()
{
    _subscription?.Dispose(); // MUST unsubscribe to prevent leaks
}
```

### Marshal to UI Thread (WPF Dispatcher):
```csharp
private void OnProductChanged(ProductChangedMessage msg)
{
    Application.Current.Dispatcher.Invoke(() => LoadData());
}
```

## Rules
1. Desktop NEVER connects to DB — only via HttpClient → API
2. EventBus messages carry entity ID only — NO data payloads
3. After receiving a message, ALWAYS reload from API
4. Views are for UI only — NO business logic in code-behind
5. ViewModels use `INotifyPropertyChanged` and `ICommand` (RelayCommand)
6. All money display uses `decimal` formatting — NEVER float
7. Use `IHttpClientFactory` via API services for all backend calls
8. **ALL async commands** MUST use `ExecuteAsync()` wrapper from ViewModelBase — NEVER manual try/catch/finally
9. `IsBusy` (protected set) replaces `IsLoading` — auto-managed by ExecuteAsync()
10. **DB Health Check on Startup**: App.xaml.cs MUST check `IDatabaseHealthCheckService` before showing login
11. **DatabaseErrorDialog**: Use styled dialog with Retry/Exit buttons on DB connection failure — NEVER raw MessageBox
12. **IDialogService**: Use for ALL user-facing messages — NEVER `MessageBox.Show`
13. **DialogService methods**: `ShowErrorAsync`, `ShowSuccessAsync`, `ShowWarningAsync`, `ShowInfoAsync`, `ShowConfirmationAsync`, `ShowDeleteConfirmationAsync`
14. **Delete operations**: Use `DeleteStrategy` enum (Cancel/Deactivate/Permanent) via `ShowDeleteConfirmationAsync`

## ExecuteAsync Pattern (ViewModels)
```csharp
// CORRECT — use ExecuteAsync wrapper
public class ProductsListViewModel : ViewModelBase
{
    public ProductsListViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadProductsOperationAsync)));
    }

    private async Task LoadProductsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _productService.GetAllAsync(IncludeInactive);
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() => { /* update UI */ });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في التحميل", "LoadProducts");
        }
    }
}

// WRONG — NEVER do this
private async Task LoadProductsAsync()
{
    try { IsLoading = true; /* ... */ }
    catch (Exception ex) { HandleException(ex); }
    finally { IsLoading = false; }
}
```
