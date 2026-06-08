---
name: "UI Agent"
reasoningEffect: max
role: "WPF UI specialist (MVVM)"
activation: "When working on SalesSystem.DesktopPWF/**"
mode: subagent
---

# UI Agent â€” WPF Desktop Specialist (MVVM)

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط·آ§ط¸â€‍ط·آ³ط¸â€‍ط·آ§ط¸â€¦` instead of `ط§ظ„ط³ظ„ط§ظ…`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## MUST READ FIRST
- `AGENTS.md` â€” Rules 007, 012, 013, 034, 160-170
- `docs/ui-screens.md` â€” Screen structure (Views/ViewModels) and EventBus patterns

## Architecture (WPF + MVVM)
```text
MainWindow.xaml (Shell)
â”œâ”€â”€ Sidebar (navigation)
â”œâ”€â”€ TopBar (user info, logout)
â”œâ”€â”€ Menu (with "ظپطھط­ ظ†ط§ظپط°ط© ط¬ط¯ظٹط¯ط©" items)
â””â”€â”€ ContentArea (hosts Views via Frame/ContentControl)
    â”œâ”€â”€ Views/Products/ProductListView.xaml
    â”œâ”€â”€ Views/Sales/SalesInvoiceEditorView.xaml
    â””â”€â”€ ... (View bound to its ViewModel)

ScreenWindow.xaml (Generic host for independent windows)
â”œâ”€â”€ ContentControl hosts any View
â”œâ”€â”€ Opens non-modally via IScreenWindowService
â”œâ”€â”€ Cascade positioning, Arabic auto-titles
â”œâ”€â”€ WeakReference tracking (no memory leaks)
â””â”€â”€ Cleanup() called on close â†’ EventBus unsubscribed
```

## EventBus Rules (CRITICAL â€” WPF Implementation)

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
1. Desktop NEVER connects to DB â€” only via HttpClient â†’ API
2. EventBus messages carry entity ID only â€” NO data payloads
3. After receiving a message, ALWAYS reload from API
4. Views are for UI only â€” NO business logic in code-behind
5. ViewModels use `INotifyPropertyChanged` and `ICommand` (RelayCommand)
6. All money display uses `decimal` formatting â€” NEVER float
7. Use `IHttpClientFactory` via API services for all backend calls
8. **ALL async commands** MUST use `ExecuteAsync()` wrapper from ViewModelBase â€” NEVER manual try/catch/finally
9. `IsBusy` (protected set) replaces `IsLoading` â€” auto-managed by ExecuteAsync()
10. **DB Health Check on Startup**: App.xaml.cs MUST check `IDatabaseHealthCheckService` before showing login
11. **DatabaseErrorDialog**: Use styled dialog with Retry/Exit buttons on DB connection failure â€” NEVER raw MessageBox
12. **IDialogService**: Use for ALL user-facing messages â€” NEVER `MessageBox.Show`
13. **DialogService methods**: `ShowErrorAsync`, `ShowSuccessAsync`, `ShowWarningAsync`, `ShowInfoAsync`, `ShowConfirmationAsync`, `ShowDeleteConfirmationAsync`
14. **Delete operations**: Use `DeleteStrategy` enum (Cancel/Deactivate/Permanent) via `ShowDeleteConfirmationAsync`
15. **Multi-Window**: Use `IScreenWindowService.OpenScreen(viewModel, options)` for ALL non-modal window opening
16. **ScreenWindow**: Use `Views.ScreenWindow` with `SetContent(page, page.DataContext)` for list views in new windows
17. **OnClosed callback**: ALWAYS use `Application.Current.Dispatcher.InvokeAsync()` for UI operations
18. **Cascade positioning**: Handled by ScreenWindowService automatically â€” no manual positioning
19. **Arabic auto-titles**: ScreenWindowService maps ViewModel type â†’ Arabic name â€” do NOT hardcode Window.Title



#### Async Pattern — ExecuteAsync Wrapper
All async operations MUST use the ExecuteAsync wrapper pattern:
```csharp
// CORRECT:
public async Task LoadCustomersAsync()
{
    await ExecuteAsync(LoadCustomersOperationAsync);
}

private async Task LoadCustomersOperationAsync()
{
    // Business logic — no try/catch, no IsBusy management
    ErrorMessage = null;
    var result = await _customerService.GetAllAsync(...);
    if (result.IsSuccess && result.Value != null)
    {
        Customers = new ObservableCollection<CustomerDto>(result.Value.Items);
    }
    else
    {
        ErrorMessage = HandleFailure(result.Error ?? "فشل التحميل", "LoadCustomers");
    }
}

// WRONG — NEVER do manual try/catch/finally:
public async Task LoadCustomersAsync()
{
    try { IsBusy = true; /* logic */ }
    catch (Exception ex) { /* handle */ }
    finally { IsBusy = false; }
}
```

## ExecuteAsync Pattern (ViewModels)
```csharp
// CORRECT â€” use ExecuteAsync wrapper
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
            ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ ط§ظ„طھط­ظ…ظٹظ„", "LoadProducts");
        }
    }
}

// WRONG â€” NEVER do this
private async Task LoadProductsAsync()
{
    try { IsLoading = true; /* ... */ }
    catch (Exception ex) { HandleException(ex); }
    finally { IsLoading = false; }
}
```

## Multi-Window Pattern (v4.5)
```csharp
// CORRECT â€” opening an editor non-modally
private void AddNewInvoice()
{
    var editorVm = App.GetService<SalesInvoiceEditorViewModel>();
    _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
    {
        Title = "ظپط§طھظˆط±ط© ط¨ظٹط¹ ط¬ط¯ظٹط¯ط©",
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

// CORRECT â€” opening a list screen in a new window
private void OpenNewSalesWindow_Click(object sender, RoutedEventArgs e)
{
    var page = new Views.Sales.SalesInvoicesListView();
    var window = new Views.ScreenWindow();
    window.SetContent(page, page.DataContext);
    App.GetService<IScreenWindowService>().OpenWindow(window, new ScreenWindowOptions
    {
        Title = "ط§ظ„ظ…ط¨ظٹط¹ط§طھ", Width = 1000, Height = 700
    });
}

// WRONG â€” NEVER block MainWindow with ShowDialog()
private void AddNewInvoice()
{
    var editorVm = new SalesInvoiceEditorViewModel();
    var editorWindow = new SalesInvoiceEditorView { DataContext = editorVm };
    editorVm.CloseRequested += () => editorWindow.DialogResult = true;
    if (editorWindow.ShowDialog() == true) { /* ... */ }  // â‌Œ BLOCKS
}
```

## Error Message Pattern (v4.5.1 â€” ALL ViewModels)

```csharp
// CORRECT â€” log the real error, show user-friendly message
catch (Exception ex)
{
    Serilog.Log.Error(ex, "Failed to save invoice");
    await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط©",
        "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹. ظٹط±ط¬ظ‰ ط§ظ„ظ…ط­ط§ظˆظ„ط© ظ…ط±ط© ط£ط®ط±ظ‰.");
}

// WRONG â€” NEVER show ex.Message to users
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("ط®ط·ط£", $"ط­ط¯ط« ط®ط·ط£: {ex.Message}"); // â‌Œ
}
```

### Dialog Titles Pattern
- Save failures: `"ط®ط·ط£ ظپظٹ ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط©"`, `"ط®ط·ط£ ظپظٹ ط­ظپط¸ ط§ظ„طھط­ظˆظٹظ„"`, etc.
- Post failures: `"ط®ط·ط£ ظپظٹ ط§ظ„طھط±ط­ظٹظ„"`
- Cancel failures: `"ط®ط·ط£ ظپظٹ ط§ظ„ط¥ظ„ط؛ط§ط،"`
- Load failures: `"ط®ط·ط£ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ"`
- Delete failures: `"ط®ط·ط£ ظپظٹ ط§ظ„ط­ط°ظپ"`
- Print failures: `"ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©"`
- Export failures: `"ط®ط·ط£ ظپظٹ طھطµط¯ظٹط± ط§ظ„ظ…ظ„ظپ"`

### List ViewModel Add Pattern
- AddCommand MUST be `RelayCommand` (sync, not async) â€” it just opens a dialog
- AddCommand MUST NOT have a CanExecute predicate â€” always enabled
- Add method MUST check `_dialogService.ShowDialog()` return and refresh on true
- NEVER create View windows manually â€” use `_dialogService.ShowDialog(vm)`

### ToolTip Requirements (v4.5.1)
- EVERY Button, MenuItem, and ListBoxItem MUST have an Arabic ToolTip
- ToolTips must describe the user action, not just repeat the label text
- Action buttons (Post, Cancel, Delete) MUST explain consequences
- Empty-state buttons MUST have ToolTips (first-time users)
- ToolTips must be added in XAML via `ToolTip="text"` attribute

### Identifier Strategy â€” No Code Field (v4.5.3)
- Product, Customer, Supplier, Warehouse editor screens MUST NOT have a Code text field
- List ViewModels MUST NOT filter/search by Code
- Selection ViewModels MUST NOT show a Code column
- Use auto-increment Id (int PK) as sole identifier for display and search
- WarehouseResponse bindings must NOT include a Code field â€” it was removed from the record

### Invoice Number Strategy â€” InvoiceNo as int, UNIQUE (v4.6.7)
- SalesInvoice and PurchaseInvoice editor screens MUST have an InvoiceNo (int) text field â€” user-facing invoice number
- Editor ViewModel: `int InvoiceNo` property, `InvoiceNo = 0` for new invoices (service computes via DocumentSequenceService â€” NOT lastId+1)
- Editor shows suggested next InvoiceNo loaded from API; user can override (validated for uniqueness)
- DocumentSequenceService used on Desktop via IApiService call to API endpoint (`GET /api/v1/sequences/next/{key}`)
- Invoice list ViewModels display `InvoiceNo` column and filter by `InvoiceNo == parsedInt || Id == parsedInt`
- `SupplierInvoiceNo` display is kept for purchase invoices (supplier's reference) and labeled as "ط±ظ‚ظ… ظپط§طھظˆط±ط© ط§ظ„ظ…ظˆط±ط¯" (supplier invoice number) â€” distinct from system "ط±ظ‚ظ… ط§ظ„ظپط§طھظˆط±ط©"
- Report ViewModels MUST include `int InvoiceNo` in DTOs
- Print preview ViewModels: `InvoiceNumber` (string) set from `InvoiceNo.ToString()` via builder

### Interactive Validation (v4.6) â€” Buttons Always Enabled, Validate on Click

**CRITICAL CHANGE:** Save/Post buttons are NEVER disabled via CanExecute. Validation happens when the user clicks the button.

#### CORRECT Pattern:
```csharp
// ViewModel â€” no CanExecute predicate
SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO predicate

private bool Validate()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Name))
        errors.Add("â€¢ ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬ ظ…ط·ظ„ظˆط¨");
    if (errors.Any())
    {
        _ = _dialogService.ShowWarningAsync("ط¨ظٹط§ظ†ط§طھ ط؛ظٹط± ظ…ظƒطھظ…ظ„ط©",
            "ظٹط±ط¬ظ‰ ط¥ظƒظ…ط§ظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¥ظ„ط²ط§ظ…ظٹط© ط§ظ„طھط§ظ„ظٹط©:\n\n" + string.Join("\n", errors));
        return false;
    }
    return true;
}

private async Task SaveAsync()
{
    if (!Validate()) return;
    // ... save logic
}
```

#### WRONG Pattern (NEVER):
```csharp
SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);  // â‌Œ blocks button
Button IsEnabled="{Binding CanSave}"  // â‌Œ disables button in XAML
```

#### XAML Requirements:
1. NO `IsEnabled` binding on Save/Post buttons
2. Required fields marked with `*` in label Text (e.g., `Text="ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬ *"`)
3. Every TextBox/ComboBox has `ToolTip="..."` explaining validation rule
4. Unique fields (barcode, username) have helper TextBlock:
   ```xml
   <TextBlock Text="ط§ظ„ط¨ط§ط±ظƒظˆط¯ ظٹط¬ط¨ ط£ظ† ظٹظƒظˆظ† ظپط±ظٹط¯ط§ظ‹ â€” ظ„ط§ ظٹظ…ظƒظ† طھظƒط±ط§ط± ظ†ظپط³ ط§ظ„ط±ظ…ط² ظ„ظ…ظ†طھط¬ظٹظ† ظ…ط®طھظ„ظپظٹظ†" 
              Style="{StaticResource HelperTextStyle}"/>
   ```

#### What to Remove from ViewModel:
- Remove CanExecute predicate from command constructor
- Remove `CanSave` property
- Remove `CanSave()` / `CanPost()` / `CanPrint()` methods
- Remove `OnPropertyChanged(nameof(CanSave))` from property setters
- Remove `RaiseCanExecuteChanged()` from property setters

#### What to Remove from XAML:
- Remove `IsEnabled="{Binding CanSave}"` from Button elements

#### What to Add to Validate():
- Collect ALL errors into a `List<string>`
- If errors exist, call `_dialogService.ShowWarningAsync("screen title", errorMsg)`
- Return false if invalid

### Costing Method Settings UI (v4.6)

Add 3 RadioButtons in SettingsView for costing method selection:

```xml
<RadioButton Content="ظ…طھظˆط³ط· ط§ظ„طھظƒظ„ظپط© ط§ظ„ظ…ط±ط¬ط­  (Weighted Average)"
             IsChecked="{Binding IsWeightedAverageSelected}"
             GroupName="CostingMethod"
             FontSize="14" FontWeight="Bold"/>
<RadioButton Content="ط¢ط®ط± ط³ط¹ط± طھظˆط±ظٹط¯  (Last Purchase Price)"
             IsChecked="{Binding IsLastPriceSelected}"
             GroupName="CostingMethod"/>
<RadioButton Content="ط³ط¹ط± ط§ظ„ظ…ظˆط±ط¯  (Supplier Catalog Price)"
             IsChecked="{Binding IsSupplierPriceSelected}"
             GroupName="CostingMethod"/>
```

Each RadioButton must have Arabic explanation text below it.

### Price Sync Indicators (v4.6) â€” Purchase Invoice

In PurchaseInvoiceEditorView.xaml DataGrid, the "ط§ظ„طھظƒظ„ظپط©" column MUST include a sync warning:

```xml
<!-- Unit Cost with sync indicator -->
<DataGridTemplateColumn Header="ط§ظ„طھظƒظ„ظپط©" Width="130">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBox Text="{Binding UnitCost, ...}" />
                <TextBlock Text="{Binding PriceDifferenceIndicator}"
                           FontSize="10" Foreground="#E65100"
                           Visibility="{Binding CostChangedFromDatabase,
                                        Converter={StaticResource BoolToVisibility}}"/>
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### LogSystemError Pattern (v4.6)
- ALL ViewModels MUST use `LogSystemError(message, context, exception)` â€” NEVER `Serilog.Log.Error(ex, ...)` directly
- `LogSystemError` is defined in ViewModelBase â€” call it directly (inherited)

### ValidationErrorsDialog Pattern (v4.6)
- Use `_dialogService.ShowValidationErrorsAsync(title, List<string> errors)` for validation
- After showing dialog, call `RequestFocusFirstInvalidField()` to auto-focus first error field
- In View code-behind, subscribe to `FocusFirstInvalidFieldRequested`:
  ```csharp
  vm.FocusFirstInvalidFieldRequested += (_, _) =>
      ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
  ```

### Dialog Overlay Pattern (v4.6)
- All dialogs MUST have: `WindowStyle="None"`, `AllowsTransparency="True"`, `Background="Transparent"`
- Full-screen `#80000000` dimming rectangle behind centered card
- Button hover effects via ControlTemplate.Triggers (IsMouseOver, IsPressed)
- `PositionOverOwner()` in code-behind

### Newest-First Sorting Pattern (v4.6.1)

ALL list ViewModels MUST sort items by newest first:

```csharp
// CORRECT â€” add OrderByDescending when populating ObservableCollection
foreach (var item in result.Value.OrderByDescending(x => x.Id))
{
    Items.Add(item);
}

// For invoices â€” sort by InvoiceDate descending
foreach (var item in result.Value.OrderByDescending(x => x.InvoiceDate))
{
    Invoices.Add(item);
}
```

### Dialog Owner Safety Pattern (v4.6.1)

```csharp
// CORRECT â€” guard against self-ownership
private void PositionOverOwner()
{
    var mainWindow = System.Windows.Application.Current.MainWindow;
    if (mainWindow != null && mainWindow != this)
    {
        Owner = mainWindow;
        Width = Owner.ActualWidth;
        Height = Owner.ActualHeight;
        Left = Owner.Left;
        Top = Owner.Top;
    }
    else
    {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
```

### WPF Validation ErrorTemplate Pattern (v4.6.2)

The ErrorTemplate in Styles.xaml provides a consistent validation visual:

```xml
<ControlTemplate x:Key="ErrorTemplate">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        <Border Grid.Column="0" Grid.ColumnSpan="2"
                BorderBrush="#EF4444" BorderThickness="1.5"
                CornerRadius="4">
            <AdornedElementPlaceholder x:Name="Placeholder"/>
        </Border>
        <Border Grid.Column="1" Background="#EF4444"
                CornerRadius="10" Width="20" Height="20"
                Margin="4,0" VerticalAlignment="Center"
                ToolTip="{Binding [0].ErrorContent}">
            <TextBlock Text="!" Foreground="White" 
                       FontWeight="Bold" FontSize="14"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"/>
        </Border>
    </Grid>
</ControlTemplate>
```

Controls get red border on Validation.HasError:
```xml
<Style TargetType="TextBox" BasedOn="{StaticResource ModernTextBox}">
    <Style.Triggers>
        <Trigger Property="Validation.HasError" Value="True">
            <Setter Property="BorderBrush" Value="#EF4444"/>
            <Setter Property="BorderThickness" Value="1.5"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**ViewModel Code Pattern:**
```csharp
// In constructor:
_dialogService = dialogService;
SetDialogService(_dialogService);

// Property setter with real-time validation:
public string Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value))
        {
            if (string.IsNullOrWhiteSpace(value))
                AddError(nameof(Name), "ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ…ط·ظ„ظˆط¨");
            else
                ClearErrors(nameof(Name));
        }
    }
}

// Pre-save validation:
private async Task<bool> ValidateAsync()
{
    ClearAllErrors();
    if (string.IsNullOrWhiteSpace(Name))
        AddError(nameof(Name), "ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ…ط·ظ„ظˆط¨");
    if (Price <= 0)
        AddError(nameof(Price), "ط§ظ„ط³ط¹ط± ظٹط¬ط¨ ط£ظ† ظٹظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† طµظپط±");
    return await ValidateAllAsync();  // Handles dialog + focus
}
```

### UI Compacting â€” Mobile-Ready Density (v4.6.6)

ALL XAML views MUST use compact sizes. The Styles.xaml now provides compact default tokens â€” do NOT override them with hardcoded sizes.

#### Global Style Tokens (Styles.xaml â€” DO NOT OVERRIDE)
| Token | Value | Notes |
|-------|-------|-------|
| Button height | 28px | Never set Height=36 or Height=40 |
| Button padding | 10,4 | Never set Padding=16,0 or Padding=24,0 |
| TextBox/ComboBox height | 28px | Never set Height=36 |
| Font size (general) | 11px | Section headers at 14px max |
| DataGrid row height | 24px | Compact rows |
| Dialog title font | 16px | Never use 18/20/22 |
| Section header font | 14px | Never use 16/18/20 |
| Sidebar width | 200px | Never use 220/240 |
| ScreenWindow MinWidth | 500px | Never use 600+ |
| ScreenWindow MinHeight | 350px | Never use 400+ |
| Dialog icon border | 44أ—44 | Never use 50أ—50+ |
| Dialog icon font | 20px | Never use 24+ |

#### CORRECT Header Pattern
```xml
<Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="طھط­ط±ظٹط± ظپط§طھظˆط±ط©" FontSize="14" FontWeight="Bold" Foreground="White"/>
    </StackPanel>
</Border>
```

#### CORRECT Footer Pattern
```xml
<Border Background="White" Padding="12,8" BorderThickness="0,1,0,0" BorderBrush="{StaticResource BorderBrush}">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="âœ… طھط±ط­ظٹظ„" Style="{StaticResource PrimaryButton}" ToolTip="..."/>
        <Button Content="ًںڑ« ط¥ظ„ط؛ط§ط،" Style="{StaticResource SecondaryButton}" ToolTip="..." Margin="8,0,0,0"/>
    </StackPanel>
</Border>
```

#### CORRECT Form Field Spacing
```xml
<StackPanel>
    <TextBlock Text="ط§ظ„ط§ط³ظ… *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Name}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>
    <TextBlock Text="ط§ظ„ط³ط¹ط± *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Price}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,8"/>
</StackPanel>
```

#### CORRECT Empty-State Pattern
```xml
<Button Content="â‍• ط¥ط¶ط§ظپط© ط£ظˆظ„ ظ…ظ†طھط¬" Command="{Binding AddCommand}"
        Style="{StaticResource PrimaryButton}" Margin="0,12,0,0" Width="140"
        ToolTip="ظپطھط­ ط´ط§ط´ط© ط¥ط¶ط§ظپط© ظ…ظ†طھط¬ ط¬ط¯ظٹط¯"/>
```

#### CORRECT Dialog Title Pattern
```xml
<TextBlock Text="طھط£ظƒظٹط¯ ط§ظ„ط­ط°ظپ" FontSize="16" FontWeight="Bold" Foreground="{StaticResource DialogTitleColor}"/>
```

#### CORRECT Dialog Icon Pattern
```xml
<Border Width="44" Height="44" CornerRadius="22" Background="{StaticResource ErrorBrush}">
    <TextBlock Text="âœ•" FontSize="20" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Border>
```

#### CORRECT Dialog Button Pattern
```xml
<Button Content="ظ†ط¹ظ…" Command="{Binding ConfirmCommand}"
        Style="{StaticResource PrimaryButton}" MinWidth="80" ToolTip="طھط£ظƒظٹط¯ ط§ظ„ط¥ط¬ط±ط§ط،"/>
<Button Content="ط¥ظ„ط؛ط§ط،" Command="{Binding CancelCommand}"
        Style="{StaticResource SecondaryButton}" MinWidth="80" ToolTip="ط¥ظ„ط؛ط§ط، ط§ظ„ط¥ط¬ط±ط§ط،" Margin="8,0,0,0"/>
```

#### VERIFICATION (checklist for each view):
- [ ] No `Height="36"` or `Height="40"` on buttons/textboxes/combos
- [ ] No `Padding="16,0"` or larger on buttons
- [ ] Header padding = `12,6` max
- [ ] Footer padding = `12,8` max
- [ ] Form field margins = `0,0,0,6` or `0,0,0,8`
- [ ] Dialog title FontSize = 16
- [ ] Section header FontSize = 14
- [ ] Empty-state buttons: Margin="0,12,0,0", Width="140"
- [ ] No `Width="120"` on buttons (use `MinWidth`)
- [ ] Sidebar width = 200
- [ ] ScreenWindow MinWidth 500, MinHeight 350

### Settings Relocation & IsEnabled Bindings (v4.6.3)

1. **Namespace & File Structure**:
   - Store settings views and view models MUST live under `Views/Settings` and `ViewModels/Settings` respectively.
   - Any settings view model MUST be registered in dependency injection under `App.xaml.cs`.

2. **Buttons Always Enabled**:
   - Action buttons (Save, Post, Print) in View XAML MUST NOT bind their `IsEnabled` property to VM properties like `HasChanges` or `CanSave` (e.g., `IsEnabled="{Binding HasChanges}"` is FORBIDDEN).
   - Leave buttons enabled at all times. Clicking the button must trigger `Validate()` and display clear warning dialogs for missing or incorrect fields.

## v4.6.8 â€” Phase 18 & Phase 20 Remediations

### CurrencyEditorViewModel â€” Dual Constructor Pattern
- MUST have parameterless constructor delegating to parameterized constructor:
  ```csharp
  public CurrencyEditorViewModel() : this(App.GetService<ICurrencyApiService>(),
      App.GetService<IDialogService>(), App.GetService<IToastNotificationService>()) { }
  public CurrencyEditorViewModel(ICurrencyApiService currencyApiService, IDialogService dialogService,
      IToastNotificationService toastService) { ... }
  ```

### CurrenciesListViewModel â€” IDisposable
- MUST implement `IDisposable` (not just override `Cleanup()`):
  ```csharp
  public class CurrenciesListViewModel : ViewModelBase, IDisposable
  {
      public void Dispose() => Cleanup();  // Ensures EventBus unsubscription
  }
  ```

### ExchangeRate Display Format â€” N6 Not N2
- Exchange rate bindings in `CurrencyEditorView.xaml` DataGrid MUST use `N6` format:
  ```xml
  <DataGridTextColumn Binding="{Binding OldRate, StringFormat='{}{0:N6}'}" Header="ط§ظ„ط³ط¹ط± ط§ظ„ظ‚ط¯ظٹظ…"/>
  <DataGridTextColumn Binding="{Binding NewRate, StringFormat='{}{0:N6}'}" Header="ط§ظ„ط³ط¹ط± ط§ظ„ط¬ط¯ظٹط¯"/>
  ```

### Edit/Delete Commands â€” No CanExecute
- `EditCommand` and `DeleteCommand` MUST NOT have CanExecute predicates (per RULE-059):
  ```csharp
  // CORRECT â€” always enabled
  EditCommand = new RelayCommand(EditCurrency);      // NO predicate
  DeleteCommand = new AsyncRelayCommand(DeleteCurrencyAsync);  // NO predicate
  // WRONG â€” blocks buttons when nothing selected
  EditCommand = new RelayCommand(EditCurrency, () => SelectedCurrency != null); // â‌Œ
  ```

### Restore Success â€” Toast Not Dialog
- Restore/deactivate success MUST use `IToastNotificationService` (not modal dialog):
  ```csharp
  // CORRECT â€” toast auto-dismisses
  _toastService.ShowSuccess("طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„ط¹ظ…ظ„ط© ط¨ظ†ط¬ط§ط­");
    // WRONG â€” modal dialog blocks user
    await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„ط¹ظ…ظ„ط© ط¨ظ†ط¬ط§ط­"); // â‌Œ
    ```

## v4.6.9 â€” Phase 19 Settings Module Remediations

### StoreSettings Service Locator Pattern
- `SettingsViewModel` uses `App.GetService<>()` for DI access (parameterless constructor). This is an anti-pattern but follows existing codebase convention. When refactoring, prefer constructor injection.

### SystemSettingsViewModel Pattern
- Follows dictionary-based approach: `MapFromDictionary()` reads from API response, `BuildDictionary()` writes back.
- Uses `ParseBool()`/`ParseInt()` safe-parse helpers with default values.
- Uses `SetDialogService()` and `ExecuteAsync()` wrappers.
- ENH-001/002 [FIXED]: SystemSettingsViewModel now has all 32 seeded settings as properties â€” including Print section (ThermalPrinterName, A4PrinterName, LogoPath, StoreTaxNumber, ShowLogo, FooterNote) and Notifications section (LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert).
- ENH-003 [FIXED]: StoreSettingsService validates known system setting keys by type (boolean â†’ TryParse, integer â†’ TryParse with ranges) before saving batch updates.
- ENH-004 [FIXED]: Tax entity has SetDefault() and ClearDefault() domain methods for cleaner service code.
- ENH-005 [FIXED]: CashBox.Create() now sets `OpeningBalance = initialBalance` â€” was always 0.
- ENH-007 [FIXED]: Currency entity has `SetAsBaseCurrency()` / `UnsetBaseCurrency()` domain methods.
- ENH-012 [FIXED]: Removed unnecessary `async` from lambda in CurrencyEditorViewModel.

### Phase 21 â€” Users & Permissions Module (v4.6.9)
- User entity rebuilt: passwordless creation, UserStatus enum, lockout 5 attempts, Phone/Email/Avatar/DefaultCashBoxId
- Permission + RolePermission entities: 33 seed permissions, 4-role model, Permission Management UI with grouped checkboxes
- AuditLog: long Id, 3 indexes, paginated browser with filters (action/entity/date)
- Auth: MustChangePassword flow, SetPassword/ChangePassword screens, account lockout
- Desktop: Enhanced UserEditor (avatar 80أ—80, Phone/Email), PasswordChangeScreen (3 fields, INotifyDataErrorInfo), AuditLog browser (DataGrid + filters), Permission Management (role tabs + category expanders + checkboxes)
- MainWindow StatusBar: avatar, role badge, change password link, logout button
- Permission-based nav visibility: ApplyPermissions() from API

## Phase 22 â€” Chart of Accounts Desktop UI (v4.6.9+)

### Architecture
```text
Views/Accounts/
â”œâ”€â”€ AccountsListView.xaml + .xaml.cs        â†گ Dual-mode TreeView/DataGrid
â”œâ”€â”€ AccountEditorView.xaml + .xaml.cs       â†گ Account form

ViewModels/Accounts/
â”œâ”€â”€ AccountsListViewModel.cs              â†گ Tree/grid toggle, search, filter, EventBus
â””â”€â”€ AccountEditorViewModel.cs             â†گ INotifyDataErrorInfo, level auto-set

Services/Api/
â”œâ”€â”€ IAccountApiService.cs                 â†گ Typed HTTP client interface
â””â”€â”€ AccountApiService.cs                  â†گ HTTP implementation with try-catch

Messaging/Messages/AppMessages.cs         â†گ AccountChangedMessage appended
```

### AccountsListView â€” Dual-Mode (TreeView + DataGrid)
- TreeView uses `HierarchicalDataTemplate` with recursive `Children` binding
- Each TreeView node shows color indicator via `Background="{Binding ColorCode}"`
- DataGrid shows flat view with columns: Code, NameAr, AccountType, Level, OpeningBalance
- ToggleViewCommand switches between modes
- Search by name or code filters both views
- AccountType ComboBox filter for the DataGrid

```xml
<!-- TreeView Mode -->
<TreeView ItemsSource="{Binding TreeData}" Grid.Column="1" 
          Visibility="{Binding IsTreeView, Converter={StaticResource BoolToVisibility}}">
    <TreeView.ItemTemplate>
        <HierarchicalDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal">
                <Border Background="{Binding ColorCode}" Width="12" Height="12"
                        CornerRadius="2" Margin="0,0,4,0" VerticalAlignment="Center"/>
                <TextBlock Text="{Binding AccountCode}" FontWeight="SemiBold" Margin="0,0,4,0"/>
                <TextBlock Text="{Binding NameAr}"/>
                <TextBlock Text="{Binding LevelDisplay}" Foreground="Gray" Margin="8,0,0,0"/>
            </StackPanel>
        </HierarchicalDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>

<!-- DataGrid Mode -->
<DataGrid ItemsSource="{Binding FlatData}" AutoGenerateColumns="False"
          Visibility="{Binding IsTreeView, Converter={StaticResource InvertedBoolToVisibility}}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="ط§ظ„ظƒظˆط¯" Binding="{Binding AccountCode}" Width="80"/>
        <DataGridTextColumn Header="ط§ظ„ط§ط³ظ… (ط¹ط±ط¨ظٹ)" Binding="{Binding NameAr}" Width="*"/>
        <DataGridTextColumn Header="ط§ظ„ظ†ظˆط¹" Binding="{Binding AccountTypeDisplay}" Width="100"/>
        <DataGridTextColumn Header="ط§ظ„ظ…ط³طھظˆظ‰" Binding="{Binding Level}" Width="60"/>
        <DataGridTextColumn Header="ط§ظ„ط±طµظٹط¯ ط§ظ„ط§ظپطھطھط§ط­ظٹ" Binding="{Binding OpeningBalance, StringFormat=N2}" Width="120"/>
        <DataGridCheckBoxColumn Header="ظٹط³ظ…ط­ ط¨ط§ظ„ط­ط±ظƒط§طھ" Binding="{Binding AllowTransactions}" Width="80"/>
    </DataGrid.Columns>
</DataGrid>
```

### AccountsListViewModel â€” IDisposable + Dual-Mode Pattern
```csharp
public class AccountsListViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountApiService _accountApiService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private IDisposable? _subscription;

    public ObservableCollection<AccountTreeNodeDto> TreeData { get; }      // TreeView source
    public ObservableCollection<AccountDto> FlatData { get; }              // DataGrid source

    private bool _isTreeView = true;
    public bool IsTreeView
    {
        get => _isTreeView;
        set
        {
            if (SetProperty(ref _isTreeView, value))
                OnPropertyChanged(nameof(IsGridView));
        }
    }
    public bool IsGridView => !IsTreeView;

    public ICommand ToggleViewCommand { get; }
    public ICommand AddCommand { get; }          // RelayCommand (NOT AsyncRelayCommand)
    public ICommand EditCommand { get; }         // AsyncRelayCommand
    public ICommand DeleteCommand { get; }       // AsyncRelayCommand

    public AccountsListViewModel(...)
    {
        SetDialogService(dialogService);  // RULE-227
        ToggleViewCommand = new RelayCommand(() => IsTreeView = !IsTreeView);
        AddCommand = new RelayCommand(AddAccount);          // NO CanExecute per RULE-059
        EditCommand = new AsyncRelayCommand(EditAccountAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAccountAsync);

        _subscription = eventBus.Subscribe<AccountChangedMessage>(_ => _ = LoadAccountsAsync());
        _ = LoadAccountsAsync();
    }

    public void Dispose() => Cleanup();  // EventBus unsubscription
}
```

### AccountEditorView â€” Form Layout
- Full form: Code, NameAr, NameEn, AccountType (ComboBox), Level (read-only TextBlock),
  ParentAccountId (hidden/parent selector), ColorCode (text input), OpeningBalance (NumericTextBox),
  AllowTransactions (CheckBox), Description (TextArea), Notes (TextArea)
- Required fields marked with `*` and have ToolTips
- ColorCode shows a preview swatch next to the text input
- Save command always enabled (NO CanExecute) â€” validates on click

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel Margin="12">
        <!-- Code + NameAr -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="20"/>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="ط§ظ„ظƒظˆط¯ *" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding AccountCode}" ToolTip="ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ â€” 4 ط¥ظ„ظ‰ 10 ط£ط±ظ‚ط§ظ…" Grid.Column="1"/>
            <TextBlock Text="ط§ظ„ط§ط³ظ… (ط¹ط±ط¨ظٹ) *" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding NameAr}" ToolTip="ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨ ط¨ط§ظ„ظ„ط؛ط© ط§ظ„ط¹ط±ط¨ظٹط© â€” ظ‡ط°ط§ ط§ظ„ط­ظ‚ظ„ ط¥ظ„ط²ط§ظ…ظٹ" Grid.Column="4"/>
        </Grid>

        <!-- NameEn + AccountType -->
        <Grid Margin="0,6,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="20"/>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="ط§ظ„ط§ط³ظ… (ط¥ظ†ط¬ظ„ظٹط²ظٹ)" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding NameEn}" Grid.Column="1"/>
            <TextBlock Text="ط§ظ„ظ†ظˆط¹ *" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <ComboBox ItemsSource="{Binding AccountTypes}" SelectedItem="{Binding SelectedAccountType}"
                      DisplayMemberPath="DisplayName" Grid.Column="4" ToolTip="ط§ط®طھط± ظ†ظˆط¹ ط§ظ„ط­ط³ط§ط¨"/>
        </Grid>

        <!-- Level (read-only) + ColorCode -->
        <Grid Margin="0,6,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="20"/>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="ط§ظ„ظ…ط³طھظˆظ‰" Style="{StaticResource LabelStyle}"/>
            <TextBlock Text="{Binding Level}" FontWeight="Bold" VerticalAlignment="Center" Grid.Column="1"/>
            <TextBlock Text="ط§ظ„ظ„ظˆظ†" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding ColorCode}" ToolTip="ظƒظˆط¯ ط§ظ„ظ„ظˆظ† ط§ظ„ط³ط¯ط§ط³ظٹ (ظ…ط«ظ„ #2196F3)" Grid.Column="4"/>
        </Grid>

        <!-- OpeningBalance + AllowTransactions -->
        <Grid Margin="0,6,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="20"/>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="ط§ظ„ط±طµظٹط¯ ط§ظ„ط§ظپطھطھط§ط­ظٹ" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding OpeningBalance, StringFormat=N2}" Grid.Column="1"/>
            <CheckBox Content="ظٹط³ظ…ط­ ط¨ط§ظ„ط­ط±ظƒط§طھ" IsChecked="{Binding AllowTransactions}"
                      ToolTip="طھظپط¹ظٹظ„ ط§ظ„ط­ط±ظƒط§طھ ط¹ظ„ظ‰ ظ‡ط°ط§ ط§ظ„ط­ط³ط§ط¨" Grid.Column="3" Grid.ColumnSpan="2" VerticalAlignment="Center"/>
        </Grid>

        <!-- Buttons -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,12,0,0">
            <Button Command="{Binding SaveCommand}" Content="ط­ظپط¸"
                    ToolTip="ط­ظپط¸ ط¨ظٹط§ظ†ط§طھ ط§ظ„ط­ط³ط§ط¨ â€” ط³ظٹطھظ… ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† طµط­ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ ظ‚ط¨ظ„ ط§ظ„ط­ظپط¸"
                    Style="{StaticResource PrimaryButton}"/>
            <Button Command="{Binding CancelCommand}" Content="ط¥ظ„ط؛ط§ط،" Margin="8,0,0,0"
                    ToolTip="ط¥ظ„ط؛ط§ط، ظˆط¥ط؛ظ„ط§ظ‚ ظ†ط§ظپط°ط© ط§ظ„طھط­ط±ظٹط±"/>
        </StackPanel>
    </StackPanel>
</ScrollViewer>
```

### AccountEditorViewModel â€” INotifyDataErrorInfo + ValidateAllAsync
```csharp
public class AccountEditorViewModel : ViewModelBase
{
    // Dual constructor
    public AccountEditorViewModel() : this(App.GetService<IAccountApiService>(),
        App.GetService<IDialogService>(), App.GetService<IToastNotificationService>()) { }

    public AccountEditorViewModel(IAccountApiService accountApiService,
        IDialogService dialogService, IToastNotificationService toastService)
    {
        _accountApiService = accountApiService;
        _toastService = toastService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO CanExecute
    }

    // INotifyDataErrorInfo validation
    private string _accountCode = string.Empty;
    public string AccountCode
    {
        get => _accountCode;
        set
        {
            _accountCode = value;
            OnPropertyChanged();
            ClearErrors(nameof(AccountCode));
            if (string.IsNullOrWhiteSpace(value))
                AddError(nameof(AccountCode), "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ ظ…ط·ظ„ظˆط¨");
            else if (value.Trim().Length < 4 || value.Trim().Length > 10)
                AddError(nameof(AccountCode), "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ ظٹط¬ط¨ ط£ظ† ظٹظƒظˆظ† ط¨ظٹظ† 4 ظˆ 10 ط£ط±ظ‚ط§ظ…");
        }
    }

    // Pre-save validation â€” RULE-229/338
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();
        // Manually trigger each property's setter validation
        AccountCode = _accountCode;
        NameAr = _nameAr;
        // ...
        return await ValidateAllAsync();  // Shows styled warning dialog
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;
        await ExecuteAsync(async () =>
        {
            var result = await _accountApiService.CreateAsync(_dto);
            if (result.IsSuccess)
            {                _toastService.ShowSuccess("طھظ… ط­ظپط¸ ط§ظ„ط­ط³ط§ط¨ ط¨ظ†ط¬ط§ط­");
                _eventBus.Publish(new AccountChangedMessage(result.Value.Id));
                CloseRequested?.Invoke();
            }
            else
            {
                await _dialogService.ShowWarningAsync("ط®ط·ط£ ظپظٹ ط­ظپط¸ ط§ظ„ط­ط³ط§ط¨", result.Error!);
            }
        });
    }
}
```

### AccountApiService â€” Typed HTTP Client
```csharp
public interface IAccountApiService
{
    Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetByTypeAsync(AccountType type, CancellationToken ct = default);
    Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default);
}

public class AccountApiService : IAccountApiService
{
    private readonly HttpClient _http;

    public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/accounts", request, ct);
            return await HandleResponseAsync<AccountDto>(response);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "ظپط´ظ„ ط§ظ„ط§طھطµط§ظ„ ط¨ط§ظ„ط®ط§ط¯ظ… ط¹ظ†ط¯ ط¥ظ†ط´ط§ط، ط§ظ„ط­ط³ط§ط¨");
            return Result<AccountDto>.Failure("طھط¹ط°ط± ط§ظ„ط§طھطµط§ظ„ ط¨ط§ظ„ط®ط§ط¯ظ…");
        }
    }

    private async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return Result<T>.Failure("ط§ط³طھط¬ط§ط¨ط© ظپط§ط±ط؛ط© ظ…ظ† ط§ظ„ط®ط§ط¯ظ…");

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (apiResponse?.Success == true && apiResponse.Data != null)
                return Result<T>.Success(apiResponse.Data);
            return Result<T>.Failure(apiResponse?.Message ?? "ظپط´ظ„ ط§ظ„ط¹ظ…ظ„ظٹط©");
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "ظپط´ظ„ طھط­ظ„ظٹظ„ ط§ط³طھط¬ط§ط¨ط© API");
            return Result<T>.Failure("ط®ط·ط£ ظپظٹ ط§ط³طھط¬ط§ط¨ط© ط§ظ„ط®ط§ط¯ظ…");
        }
    }
}
```

### DI Registration (App.xaml.cs)
```csharp
// Services
services.AddSingleton<IAccountApiService, AccountApiService>();

// ViewModels
services.AddTransient<AccountsListViewModel>();
services.AddTransient<AccountEditorViewModel>();

// Views
services.AddTransient<AccountsListView>();
services.AddTransient<AccountEditorView>();
```

### Navigation (MainWindow.xaml + MainViewModel.cs)
```xml
<!-- Sidebar button -->
<Button Content="ط¯ظ„ظٹظ„ ط§ظ„ط­ط³ط§ط¨ط§طھ" Command="{Binding NavigateToChartOfAccountsCommand}"
        ToolTip="ط¹ط±ط¶ ظˆط¥ط¯ط§ط±ط© ط¯ظ„ظٹظ„ ط§ظ„ط­ط³ط§ط¨ط§طھ â€” ظ‡ظٹظƒظ„ ط­ط³ط§ط¨ط§طھ ط§ظ„ظ…ظ†ط´ط£ط©"
        Style="{StaticResource SidebarButtonStyle}"/>

<!-- Menu item -->
<MenuItem Header="ط§ظ„ط­ط³ط§ط¨ط§طھ" Command="{Binding NavigateToChartOfAccountsCommand}"
          ToolTip="ظپطھط­ ط´ط§ط´ط© ط¯ظ„ظٹظ„ ط§ظ„ط­ط³ط§ط¨ط§طھ"/>
```

```csharp
public ICommand NavigateToChartOfAccountsCommand { get; }

// In constructor:
NavigateToChartOfAccountsCommand = new AsyncRelayCommand(async () =>
{
    var vm = App.GetService<AccountsListViewModel>();
    var view = new AccountsListView { DataContext = vm };
    _screenWindowService.OpenScreen(view, new ScreenWindowOptions
    {
        Title = "ط¯ظ„ظٹظ„ ط§ظ„ط­ط³ط§ط¨ط§طھ",
        Width = 1000,
        Height = 700
    });
});
```

### Key UI Rules for Phase 22
- **RULE-332**: AccountsListViewModel MUST implement `IDisposable` and call `Cleanup()` in `Dispose()` to unsubscribe from EventBus
- **RULE-338**: AccountEditorViewModel.ValidateAsync() MUST follow `ClearAllErrors()` â†’ `AddError()` â†’ `await ValidateAllAsync()` pattern
- **RULE-059**: Save/Edit/Delete commands MUST have NO CanExecute predicates (always enabled)
- **RULE-227**: SetDialogService() MUST be called in every editor constructor
- **RULE-185-190**: ALL interactive controls MUST have Arabic ToolTips
- **RULE-262-274**: Compact styles â€” no hardcoded Height=36/40 or Padding=16+
- **RULE-340**: Desktop Accounts View MUST provide dual-mode display (TreeView + DataGrid toggle)
```

### Phase 22 Code Review Fixes (v4.6.9+)

#### Account Editor Edit Mode
- **RULE-348**: Account code MUST be read-only during edit (`IsAccountCodeReadOnly = true`) â€” NEVER allow changing code on existing accounts
- Load existing account data via `GetByIdAsync` before populating fields
- Switch from `CreateAsync` to `UpdateAsync` when editing existing account

#### Account List ViewModel
- **RULE-349**: MUST implement `EditSelectedAccountCommand` + `DeleteSelectedAccountCommand` with ToolBar buttons
- Edit: open AccountEditorView non-modally via `_screenWindowService.OpenScreen(vm)`
- Delete: use `_dialogService.ShowDeleteConfirmationAsync()` for soft delete
- Search/filter MUST work in BOTH TreeView mode (filter nodes) and DataGrid mode (filter rows)

#### Dual-Mode Display
- **RULE-340**: TreeView with `HierarchicalDataTemplate` (recursive children) + DataGrid flat view
- Toggle via RadioButton/CheckBox in toolbar
- TreeView selected item MUST sync with DataGrid selection when switching modes

### Phase 23 â€” Customers Module

#### Desktop UI Changes

**CustomerEditorViewModel:**
- New dependency: IAccountApiService
- New properties: CustomerType (byte?), AvailableGroups, SelectedGroup, AvailableAccounts, SelectedAccount
- Loads AvailableGroups from API on init via GetAllGroupsAsync()
- Loads AvailableAccounts from AccountApiService
- CustomerType dropdown with Cash/Credit options
- CustomerGroup dropdown bound to AvailableGroups
- Account lookup bound to AvailableAccounts

**CustomerListViewModel:**
- Added AvailableGroups ObservableCollection
- Added SelectedGroupFilter for group filtering
- Loads groups on init, applies group filter in FilterCustomers

**CustomerGroup Management:**
- CustomerGroupDto: Id, Name, Description, IsActive
- Future: CustomerGroupManagementView (dedicated CRUD screen)

#### Rules
- Editor opens non-modally via IScreenWindowService (RULE-161)
- Editor VM calls SetDialogService() in constructor (RULE-227)
- Save buttons always enabled â€” validate on click (RULE-229)
- Lists sorted newest-first (RULE-220)
- Compact XAML styles (RULE-262-274)

#### XAML Pitfalls — NEVER Do These
- ❌ NEVER use `Style="{StaticResource ModernTextBox}"` on a `ComboBox` — `ModernTextBox` has `TargetType="TextBox"`, which throws `XamlParseException`. Use `ModernComboBox` instead.
- ❌ NEVER set both `DisplayMemberPath="Xxx"` AND `<ComboBox.ItemTemplate>` on the same `ComboBox` — WPF throws `InvalidOperationException`. Use one or the other, not both.
- ❌ NEVER hardcode `Height="36"` or `Height="40"` on Button/TextBox/ComboBox — let Styles.xaml handle sizing via compact global styles (RULE-262).
- ❌ NEVER use `Width="120"` on dialog buttons — use `MinWidth="80"` or `MinWidth="100"` (RULE-272).
- ❌ NEVER use `FontSize="20"` or larger for dialog titles — use `FontSize="16"` (RULE-266).
- ❌ NEVER make CustomerType a required field on customer creation — it's informational only. The Cash/Credit decision belongs on the Sales Invoice (`PaymentType`), not on the Customer.

```

## Phase Awareness — Phases 23–31 Feature Scope

### Phase Table
| Phase | Focus | Key Files |
|-------|-------|-----------|
| 23 | Customers Module (CRUD, groups, credit limits) | Desktop/CustomersListView.xaml, Desktop/CustomerEditorView.xaml |
| 24 | Accounting Integration (auto journal entries) | — (backend-only, no new UI screens) |
| 25 | Products Module v2 (ProductPrices, multi-currency pricing) | Desktop/ProductsListView.xaml, Desktop/ProductEditorView.xaml |
| 26 | Warehouses Module (type, manager, stock adjustments) | Desktop/WarehousesListView.xaml, Desktop/WarehouseEditorView.xaml |
| 27 | Purchases Module v2 (PO, landed cost) | Desktop/PurchaseOrdersListView.xaml, Desktop/PurchaseOrderEditorView.xaml |
| 28 | Sales Module v2 (quotes, POS, credit check) | Desktop/SalesQuotationsListView.xaml, Desktop/SalesPOSView.xaml |
| 29 | Receipts & Payments (cheques, allocation) | Desktop/ChequesListView.xaml, Desktop/PaymentAllocationView.xaml |
| 30 | Journal Entries (manual entries, fiscal year) | Desktop/JournalEntriesListView.xaml, Desktop/FiscalYearView.xaml |
| 31 | Reports (financial, inventory, sales, Excel export) | Desktop/ReportsView.xaml, Desktop/ReportViewer.xaml |

### UI Rules for Phases 23-31
1. All new editor Views MUST follow compact density (RULE-262 to 274) — no hardcoded heights/paddings
2. All new Views MUST have Arabic ToolTips on ALL interactive controls
3. All new editor ViewModels MUST use `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` pattern
4. All new list Views MUST sort by newest-first (RULE-220)
5. Dual-mode (TreeView + DataGrid) for hierarchical data like chart of accounts
6. `ModernComboBox` style on ComboBox — NEVER `ModernTextBox`
7. NEVER set both `DisplayMemberPath` AND `ItemTemplate` on same ComboBox
8. Dialog buttons use `MinWidth="80"` or `MinWidth="100"` — NEVER fixed `Width="120"`
9. Dialog titles `FontSize="16"` max — NEVER `FontSize="20"` or larger
10. Empty-state buttons `Margin="0,12,0,0"` Width="140" — NEVER larger margins/widths

### CashBox Editor Pattern (v4.9 — No Balance Fields)

The CashBox editor has NO balance fields (OpeningBalance/CurrentBalance removed). It has AccountId (label-only), CategoryId dropdown, and metadata fields.

**CashBoxEditorViewModel properties:**
```csharp
// NO OpeningBalance, NO CurrentBalance
public string Name { get; set; }
public int AccountId { get; set; }               // Auto-set or from AccountId param
public string? AccountName { get; set; }          // Display-only label
public int? CategoryId { get; set; }              // Dropdown
public string? PhoneNumber { get; set; }
public string? TaxNumber { get; set; }
public string? Address { get; set; }
public int CurrencyId { get; set; }              // Required

// Lookup data
public ObservableCollection<CategoryDto> AvailableCategories { get; }
```

**XAML Layout (no balance fields):**
```xml
<!-- No OpeningBalance field -->
<!-- Account info label (read-only) -->
<TextBlock Text="{Binding AccountName}" Style="{StaticResource InfoLabelStyle}"/>

<!-- Category dropdown -->
<ComboBox ItemsSource="{Binding AvailableCategories}"
          DisplayMemberPath="Name"
          SelectedValue="{Binding CategoryId}"
          SelectedValuePath="Id"
          Style="{StaticResource ModernComboBox}"
          ToolTip="تصنيف الصندوق (خزينة، بنك، صندوق مندوب)"/>

<!-- Phone/Tax/Address -->
<TextBox Text="{Binding PhoneNumber}" ToolTip="رقم الهاتف — اختياري" 
         Style="{StaticResource ModernTextBox}"/>
<TextBox Text="{Binding TaxNumber}" ToolTip="الرقم الضريبي — اختياري"
         Style="{StaticResource ModernTextBox}"/>
<TextBox Text="{Binding Address}" ToolTip="العنوان — اختياري"
         Style="{StaticResource ModernTextBox}" TextWrapping="Wrap"/>
```

**CashTransferViewModel — NO client-side balance validation:**
```csharp
// CORRECT — NO CurrentBalance check
private async Task TransferAsync()
{
    if (!Validate()) return;
    var result = await _service.TransferAsync(request, userId);
    // ... handle result
}

// WRONG — NEVER check client-side balance
if (SelectedSourceCashBox.CurrentBalance < Amount) // ❌ removed
{
    await _dialogService.ShowWarningAsync("خطأ", "الرصيد غير كافٍ"); // ❌
    return;
}
```

### Default Fix Items
1. Remove hardcoded `Height="36/40"` on buttons/inputs → let style handle at 28px
2. Remove hardcoded `Padding="16,0"` or larger on buttons → let style handle at 10,4
3. Reduce header padding to `12,6` and footer padding to `12,8`
4. Reduce form margins to `8px` or less between fields
5. Reduce dialog title fonts to `16`, section headers to `14`
6. Reduce dialog icon borders to `44×44` with font `20`
7. Replace `MessageBox.Show` with `IDialogService` async methods
8. Remove `CanExecute` predicate from SaveCommand constructors
9. Add `if (!Validate()) return;` at start of SaveAsync
10. Add `SetDialogService()` in Editor ViewModel constructors
11. Add Arabic ToolTips to ALL interactive controls missing them
12. Add `*` to required field labels + helper text for unique fields
