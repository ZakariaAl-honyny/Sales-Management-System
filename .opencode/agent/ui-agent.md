---
name: "UI Agent"
reasoningEffect: high
role: "WPF UI specialist (MVVM)"
activation: "When working on SalesSystem.DesktopPWF/**"
mode: subagent
---

# UI Agent — WPF Desktop Specialist (MVVM)

## MUST READ FIRST
- `AGENTS.md` — Rules 007, 012, 013, 034, 160-170
- `docs/ui-screens.md` — Screen structure (Views/ViewModels) and EventBus patterns

## Architecture (WPF + MVVM)
```text
MainWindow.xaml (Shell)
├── Sidebar (navigation)
├── TopBar (user info, logout)
├── Menu (with "فتح نافذة جديدة" items)
└── ContentArea (hosts Views via Frame/ContentControl)
    ├── Views/Products/ProductListView.xaml
    ├── Views/Sales/SalesInvoiceEditorView.xaml
    └── ... (View bound to its ViewModel)

ScreenWindow.xaml (Generic host for independent windows)
├── ContentControl hosts any View
├── Opens non-modally via IScreenWindowService
├── Cascade positioning, Arabic auto-titles
├── WeakReference tracking (no memory leaks)
└── Cleanup() called on close → EventBus unsubscribed
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
15. **Multi-Window**: Use `IScreenWindowService.OpenScreen(viewModel, options)` for ALL non-modal window opening
16. **ScreenWindow**: Use `Views.ScreenWindow` with `SetContent(page, page.DataContext)` for list views in new windows
17. **OnClosed callback**: ALWAYS use `Application.Current.Dispatcher.InvokeAsync()` for UI operations
18. **Cascade positioning**: Handled by ScreenWindowService automatically — no manual positioning
19. **Arabic auto-titles**: ScreenWindowService maps ViewModel type → Arabic name — do NOT hardcode Window.Title

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

## Multi-Window Pattern (v4.5)
```csharp
// CORRECT — opening an editor non-modally
private void AddNewInvoice()
{
    var editorVm = App.GetService<SalesInvoiceEditorViewModel>();
    _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
    {
        Title = "فاتورة بيع جديدة",
        OnClosed = (vm) =>
        {
            if (vm is SalesInvoiceEditorViewModel editor && editor.InvoiceId.HasValue)
            {
                _eventBus.Publish(new SaleInvoiceChangedMessage(editor.InvoiceId.Value));
                Application.Current.Dispatcher.InvokeAsync(() => _ = LoadInvoicesAsync());
            }
        }
    });
}

// CORRECT — opening a list screen in a new window
private void OpenNewSalesWindow_Click(object sender, RoutedEventArgs e)
{
    var page = new Views.Sales.SalesInvoicesListView();
    var window = new Views.ScreenWindow();
    window.SetContent(page, page.DataContext);
    App.GetService<IScreenWindowService>().OpenWindow(window, new ScreenWindowOptions
    {
        Title = "المبيعات", Width = 1000, Height = 700
    });
}

// WRONG — NEVER block MainWindow with ShowDialog()
private void AddNewInvoice()
{
    var editorVm = new SalesInvoiceEditorViewModel();
    var editorWindow = new SalesInvoiceEditorView { DataContext = editorVm };
    editorVm.CloseRequested += () => editorWindow.DialogResult = true;
    if (editorWindow.ShowDialog() == true) { /* ... */ }  // ❌ BLOCKS
}
```
