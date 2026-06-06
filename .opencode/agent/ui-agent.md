---
name: "UI Agent"
reasoningEffect: high
role: "WPF UI specialist (MVVM)"
activation: "When working on SalesSystem.DesktopPWF/**"
mode: subagent
---

# UI Agent — WPF Desktop Specialist (MVVM)

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

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

## Error Message Pattern (v4.5.1 — ALL ViewModels)

```csharp
// CORRECT — log the real error, show user-friendly message
catch (Exception ex)
{
    Serilog.Log.Error(ex, "Failed to save invoice");
    await _dialogService.ShowErrorAsync("خطأ في حفظ الفاتورة",
        "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.");
}

// WRONG — NEVER show ex.Message to users
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("خطأ", $"حدث خطأ: {ex.Message}"); // ❌
}
```

### Dialog Titles Pattern
- Save failures: `"خطأ في حفظ الفاتورة"`, `"خطأ في حفظ التحويل"`, etc.
- Post failures: `"خطأ في الترحيل"`
- Cancel failures: `"خطأ في الإلغاء"`
- Load failures: `"خطأ في تحميل البيانات"`
- Delete failures: `"خطأ في الحذف"`
- Print failures: `"خطأ في الطباعة"`
- Export failures: `"خطأ في تصدير الملف"`

### List ViewModel Add Pattern
- AddCommand MUST be `RelayCommand` (sync, not async) — it just opens a dialog
- AddCommand MUST NOT have a CanExecute predicate — always enabled
- Add method MUST check `_dialogService.ShowDialog()` return and refresh on true
- NEVER create View windows manually — use `_dialogService.ShowDialog(vm)`

### ToolTip Requirements (v4.5.1)
- EVERY Button, MenuItem, and ListBoxItem MUST have an Arabic ToolTip
- ToolTips must describe the user action, not just repeat the label text
- Action buttons (Post, Cancel, Delete) MUST explain consequences
- Empty-state buttons MUST have ToolTips (first-time users)
- ToolTips must be added in XAML via `ToolTip="text"` attribute

### Identifier Strategy — No Code Field (v4.5.3)
- Product, Customer, Supplier, Warehouse editor screens MUST NOT have a Code text field
- List ViewModels MUST NOT filter/search by Code
- Selection ViewModels MUST NOT show a Code column
- Use auto-increment Id (int PK) as sole identifier for display and search
- WarehouseResponse bindings must NOT include a Code field — it was removed from the record

### Invoice Number Strategy — InvoiceNo as int, UNIQUE (v4.6.7)
- SalesInvoice and PurchaseInvoice editor screens MUST have an InvoiceNo (int) text field — user-facing invoice number
- Editor ViewModel: `int InvoiceNo` property, `InvoiceNo = 0` for new invoices (service computes via DocumentSequenceService — NOT lastId+1)
- Editor shows suggested next InvoiceNo loaded from API; user can override (validated for uniqueness)
- DocumentSequenceService used on Desktop via IApiService call to API endpoint (`GET /api/v1/sequences/next/{key}`)
- Invoice list ViewModels display `InvoiceNo` column and filter by `InvoiceNo == parsedInt || Id == parsedInt`
- `SupplierInvoiceNo` display is kept for purchase invoices (supplier's reference) and labeled as "رقم فاتورة المورد" (supplier invoice number) — distinct from system "رقم الفاتورة"
- Report ViewModels MUST include `int InvoiceNo` in DTOs
- Print preview ViewModels: `InvoiceNumber` (string) set from `InvoiceNo.ToString()` via builder

### Interactive Validation (v4.6) — Buttons Always Enabled, Validate on Click

**CRITICAL CHANGE:** Save/Post buttons are NEVER disabled via CanExecute. Validation happens when the user clicks the button.

#### CORRECT Pattern:
```csharp
// ViewModel — no CanExecute predicate
SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO predicate

private bool Validate()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Name))
        errors.Add("• اسم المنتج مطلوب");
    if (errors.Any())
    {
        _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة",
            "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors));
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
SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);  // ❌ blocks button
Button IsEnabled="{Binding CanSave}"  // ❌ disables button in XAML
```

#### XAML Requirements:
1. NO `IsEnabled` binding on Save/Post buttons
2. Required fields marked with `*` in label Text (e.g., `Text="اسم المنتج *"`)
3. Every TextBox/ComboBox has `ToolTip="..."` explaining validation rule
4. Unique fields (barcode, username) have helper TextBlock:
   ```xml
   <TextBlock Text="الباركود يجب أن يكون فريداً — لا يمكن تكرار نفس الرمز لمنتجين مختلفين" 
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
<RadioButton Content="متوسط التكلفة المرجح  (Weighted Average)"
             IsChecked="{Binding IsWeightedAverageSelected}"
             GroupName="CostingMethod"
             FontSize="14" FontWeight="Bold"/>
<RadioButton Content="آخر سعر توريد  (Last Purchase Price)"
             IsChecked="{Binding IsLastPriceSelected}"
             GroupName="CostingMethod"/>
<RadioButton Content="سعر المورد  (Supplier Catalog Price)"
             IsChecked="{Binding IsSupplierPriceSelected}"
             GroupName="CostingMethod"/>
```

Each RadioButton must have Arabic explanation text below it.

### Price Sync Indicators (v4.6) — Purchase Invoice

In PurchaseInvoiceEditorView.xaml DataGrid, the "التكلفة" column MUST include a sync warning:

```xml
<!-- Unit Cost with sync indicator -->
<DataGridTemplateColumn Header="التكلفة" Width="130">
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
- ALL ViewModels MUST use `LogSystemError(message, context, exception)` — NEVER `Serilog.Log.Error(ex, ...)` directly
- `LogSystemError` is defined in ViewModelBase — call it directly (inherited)

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
// CORRECT — add OrderByDescending when populating ObservableCollection
foreach (var item in result.Value.OrderByDescending(x => x.Id))
{
    Items.Add(item);
}

// For invoices — sort by InvoiceDate descending
foreach (var item in result.Value.OrderByDescending(x => x.InvoiceDate))
{
    Invoices.Add(item);
}
```

### Dialog Owner Safety Pattern (v4.6.1)

```csharp
// CORRECT — guard against self-ownership
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
                AddError(nameof(Name), "اسم العميل مطلوب");
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
        AddError(nameof(Name), "اسم العميل مطلوب");
    if (Price <= 0)
        AddError(nameof(Price), "السعر يجب أن يكون أكبر من صفر");
    return await ValidateAllAsync();  // Handles dialog + focus
}
```

### UI Compacting — Mobile-Ready Density (v4.6.6)

ALL XAML views MUST use compact sizes. The Styles.xaml now provides compact default tokens — do NOT override them with hardcoded sizes.

#### Global Style Tokens (Styles.xaml — DO NOT OVERRIDE)
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
| Dialog icon border | 44×44 | Never use 50×50+ |
| Dialog icon font | 20px | Never use 24+ |

#### CORRECT Header Pattern
```xml
<Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="تحرير فاتورة" FontSize="14" FontWeight="Bold" Foreground="White"/>
    </StackPanel>
</Border>
```

#### CORRECT Footer Pattern
```xml
<Border Background="White" Padding="12,8" BorderThickness="0,1,0,0" BorderBrush="{StaticResource BorderBrush}">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="✅ ترحيل" Style="{StaticResource PrimaryButton}" ToolTip="..."/>
        <Button Content="🚫 إلغاء" Style="{StaticResource SecondaryButton}" ToolTip="..." Margin="8,0,0,0"/>
    </StackPanel>
</Border>
```

#### CORRECT Form Field Spacing
```xml
<StackPanel>
    <TextBlock Text="الاسم *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Name}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>
    <TextBlock Text="السعر *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Price}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,8"/>
</StackPanel>
```

#### CORRECT Empty-State Pattern
```xml
<Button Content="➕ إضافة أول منتج" Command="{Binding AddCommand}"
        Style="{StaticResource PrimaryButton}" Margin="0,12,0,0" Width="140"
        ToolTip="فتح شاشة إضافة منتج جديد"/>
```

#### CORRECT Dialog Title Pattern
```xml
<TextBlock Text="تأكيد الحذف" FontSize="16" FontWeight="Bold" Foreground="{StaticResource DialogTitleColor}"/>
```

#### CORRECT Dialog Icon Pattern
```xml
<Border Width="44" Height="44" CornerRadius="22" Background="{StaticResource ErrorBrush}">
    <TextBlock Text="✕" FontSize="20" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Border>
```

#### CORRECT Dialog Button Pattern
```xml
<Button Content="نعم" Command="{Binding ConfirmCommand}"
        Style="{StaticResource PrimaryButton}" MinWidth="80" ToolTip="تأكيد الإجراء"/>
<Button Content="إلغاء" Command="{Binding CancelCommand}"
        Style="{StaticResource SecondaryButton}" MinWidth="80" ToolTip="إلغاء الإجراء" Margin="8,0,0,0"/>
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
```
