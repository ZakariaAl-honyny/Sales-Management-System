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

## v4.6.8 — Phase 18 & Phase 20 Remediations

### CurrencyEditorViewModel — Dual Constructor Pattern
- MUST have parameterless constructor delegating to parameterized constructor:
  ```csharp
  public CurrencyEditorViewModel() : this(App.GetService<ICurrencyApiService>(),
      App.GetService<IDialogService>(), App.GetService<IToastNotificationService>()) { }
  public CurrencyEditorViewModel(ICurrencyApiService currencyApiService, IDialogService dialogService,
      IToastNotificationService toastService) { ... }
  ```

### CurrenciesListViewModel — IDisposable
- MUST implement `IDisposable` (not just override `Cleanup()`):
  ```csharp
  public class CurrenciesListViewModel : ViewModelBase, IDisposable
  {
      public void Dispose() => Cleanup();  // Ensures EventBus unsubscription
  }
  ```

### ExchangeRate Display Format — N6 Not N2
- Exchange rate bindings in `CurrencyEditorView.xaml` DataGrid MUST use `N6` format:
  ```xml
  <DataGridTextColumn Binding="{Binding OldRate, StringFormat='{}{0:N6}'}" Header="السعر القديم"/>
  <DataGridTextColumn Binding="{Binding NewRate, StringFormat='{}{0:N6}'}" Header="السعر الجديد"/>
  ```

### Edit/Delete Commands — No CanExecute
- `EditCommand` and `DeleteCommand` MUST NOT have CanExecute predicates (per RULE-059):
  ```csharp
  // CORRECT — always enabled
  EditCommand = new RelayCommand(EditCurrency);      // NO predicate
  DeleteCommand = new AsyncRelayCommand(DeleteCurrencyAsync);  // NO predicate
  // WRONG — blocks buttons when nothing selected
  EditCommand = new RelayCommand(EditCurrency, () => SelectedCurrency != null); // ❌
  ```

### Restore Success — Toast Not Dialog
- Restore/deactivate success MUST use `IToastNotificationService` (not modal dialog):
  ```csharp
  // CORRECT — toast auto-dismisses
  _toastService.ShowSuccess("تم استعادة العملة بنجاح");
    // WRONG — modal dialog blocks user
    await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة العملة بنجاح"); // ❌
    ```

## v4.6.9 — Phase 19 Settings Module Remediations

### StoreSettings Service Locator Pattern
- `SettingsViewModel` uses `App.GetService<>()` for DI access (parameterless constructor). This is an anti-pattern but follows existing codebase convention. When refactoring, prefer constructor injection.

### SystemSettingsViewModel Pattern
- Follows dictionary-based approach: `MapFromDictionary()` reads from API response, `BuildDictionary()` writes back.
- Uses `ParseBool()`/`ParseInt()` safe-parse helpers with default values.
- Uses `SetDialogService()` and `ExecuteAsync()` wrappers.
- ENH-001/002 [FIXED]: SystemSettingsViewModel now has all 32 seeded settings as properties — including Print section (ThermalPrinterName, A4PrinterName, LogoPath, StoreTaxNumber, ShowLogo, FooterNote) and Notifications section (LowStockAlert, ExpiryAlert, ExpiryAlertDays, CreditLimitAlert).
- ENH-003 [FIXED]: StoreSettingsService validates known system setting keys by type (boolean → TryParse, integer → TryParse with ranges) before saving batch updates.
- ENH-004 [FIXED]: Tax entity has SetDefault() and ClearDefault() domain methods for cleaner service code.
- ENH-005 [FIXED]: CashBox.Create() now sets `OpeningBalance = initialBalance` — was always 0.
- ENH-007 [FIXED]: Currency entity has `SetAsBaseCurrency()` / `UnsetBaseCurrency()` domain methods.
- ENH-012 [FIXED]: Removed unnecessary `async` from lambda in CurrencyEditorViewModel.

### Phase 21 — Users & Permissions Module (v4.6.9)
- User entity rebuilt: passwordless creation, UserStatus enum, lockout 5 attempts, Phone/Email/Avatar/DefaultCashBoxId
- Permission + RolePermission entities: 33 seed permissions, 4-role model, Permission Management UI with grouped checkboxes
- AuditLog: long Id, 3 indexes, paginated browser with filters (action/entity/date)
- Auth: MustChangePassword flow, SetPassword/ChangePassword screens, account lockout
- Desktop: Enhanced UserEditor (avatar 80×80, Phone/Email), PasswordChangeScreen (3 fields, INotifyDataErrorInfo), AuditLog browser (DataGrid + filters), Permission Management (role tabs + category expanders + checkboxes)
- MainWindow StatusBar: avatar, role badge, change password link, logout button
- Permission-based nav visibility: ApplyPermissions() from API

## Phase 22 — Chart of Accounts Desktop UI (v4.6.9+)

### Architecture
```text
Views/Accounts/
├── AccountsListView.xaml + .xaml.cs        ← Dual-mode TreeView/DataGrid
├── AccountEditorView.xaml + .xaml.cs       ← Account form

ViewModels/Accounts/
├── AccountsListViewModel.cs              ← Tree/grid toggle, search, filter, EventBus
└── AccountEditorViewModel.cs             ← INotifyDataErrorInfo, level auto-set

Services/Api/
├── IAccountApiService.cs                 ← Typed HTTP client interface
└── AccountApiService.cs                  ← HTTP implementation with try-catch

Messaging/Messages/AppMessages.cs         ← AccountChangedMessage appended
```

### AccountsListView — Dual-Mode (TreeView + DataGrid)
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
        <DataGridTextColumn Header="الكود" Binding="{Binding AccountCode}" Width="80"/>
        <DataGridTextColumn Header="الاسم (عربي)" Binding="{Binding NameAr}" Width="*"/>
        <DataGridTextColumn Header="النوع" Binding="{Binding AccountTypeDisplay}" Width="100"/>
        <DataGridTextColumn Header="المستوى" Binding="{Binding Level}" Width="60"/>
        <DataGridTextColumn Header="الرصيد الافتتاحي" Binding="{Binding OpeningBalance, StringFormat=N2}" Width="120"/>
        <DataGridCheckBoxColumn Header="يسمح بالحركات" Binding="{Binding AllowTransactions}" Width="80"/>
    </DataGrid.Columns>
</DataGrid>
```

### AccountsListViewModel — IDisposable + Dual-Mode Pattern
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

### AccountEditorView — Form Layout
- Full form: Code, NameAr, NameEn, AccountType (ComboBox), Level (read-only TextBlock),
  ParentAccountId (hidden/parent selector), ColorCode (text input), OpeningBalance (NumericTextBox),
  AllowTransactions (CheckBox), Description (TextArea), Notes (TextArea)
- Required fields marked with `*` and have ToolTips
- ColorCode shows a preview swatch next to the text input
- Save command always enabled (NO CanExecute) — validates on click

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
            <TextBlock Text="الكود *" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding AccountCode}" ToolTip="كود الحساب — 4 إلى 10 أرقام" Grid.Column="1"/>
            <TextBlock Text="الاسم (عربي) *" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding NameAr}" ToolTip="اسم الحساب باللغة العربية — هذا الحقل إلزامي" Grid.Column="4"/>
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
            <TextBlock Text="الاسم (إنجليزي)" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding NameEn}" Grid.Column="1"/>
            <TextBlock Text="النوع *" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <ComboBox ItemsSource="{Binding AccountTypes}" SelectedItem="{Binding SelectedAccountType}"
                      DisplayMemberPath="DisplayName" Grid.Column="4" ToolTip="اختر نوع الحساب"/>
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
            <TextBlock Text="المستوى" Style="{StaticResource LabelStyle}"/>
            <TextBlock Text="{Binding Level}" FontWeight="Bold" VerticalAlignment="Center" Grid.Column="1"/>
            <TextBlock Text="اللون" Grid.Column="3" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding ColorCode}" ToolTip="كود اللون السداسي (مثل #2196F3)" Grid.Column="4"/>
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
            <TextBlock Text="الرصيد الافتتاحي" Style="{StaticResource LabelStyle}"/>
            <TextBox Text="{Binding OpeningBalance, StringFormat=N2}" Grid.Column="1"/>
            <CheckBox Content="يسمح بالحركات" IsChecked="{Binding AllowTransactions}"
                      ToolTip="تفعيل الحركات على هذا الحساب" Grid.Column="3" Grid.ColumnSpan="2" VerticalAlignment="Center"/>
        </Grid>

        <!-- Buttons -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,12,0,0">
            <Button Command="{Binding SaveCommand}" Content="حفظ"
                    ToolTip="حفظ بيانات الحساب — سيتم التحقق من صحة البيانات قبل الحفظ"
                    Style="{StaticResource PrimaryButton}"/>
            <Button Command="{Binding CancelCommand}" Content="إلغاء" Margin="8,0,0,0"
                    ToolTip="إلغاء وإغلاق نافذة التحرير"/>
        </StackPanel>
    </StackPanel>
</ScrollViewer>
```

### AccountEditorViewModel — INotifyDataErrorInfo + ValidateAllAsync
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
                AddError(nameof(AccountCode), "كود الحساب مطلوب");
            else if (value.Trim().Length < 4 || value.Trim().Length > 10)
                AddError(nameof(AccountCode), "كود الحساب يجب أن يكون بين 4 و 10 أرقام");
        }
    }

    // Pre-save validation — RULE-229/338
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
            {                _toastService.ShowSuccess("تم حفظ الحساب بنجاح");
                _eventBus.Publish(new AccountChangedMessage(result.Value.Id));
                CloseRequested?.Invoke();
            }
            else
            {
                await _dialogService.ShowWarningAsync("خطأ في حفظ الحساب", result.Error!);
            }
        });
    }
}
```

### AccountApiService — Typed HTTP Client
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
            Log.Error(ex, "فشل الاتصال بالخادم عند إنشاء الحساب");
            return Result<AccountDto>.Failure("تعذر الاتصال بالخادم");
        }
    }

    private async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return Result<T>.Failure("استجابة فارغة من الخادم");

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (apiResponse?.Success == true && apiResponse.Data != null)
                return Result<T>.Success(apiResponse.Data);
            return Result<T>.Failure(apiResponse?.Message ?? "فشل العملية");
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "فشل تحليل استجابة API");
            return Result<T>.Failure("خطأ في استجابة الخادم");
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
<Button Content="دليل الحسابات" Command="{Binding NavigateToChartOfAccountsCommand}"
        ToolTip="عرض وإدارة دليل الحسابات — هيكل حسابات المنشأة"
        Style="{StaticResource SidebarButtonStyle}"/>

<!-- Menu item -->
<MenuItem Header="الحسابات" Command="{Binding NavigateToChartOfAccountsCommand}"
          ToolTip="فتح شاشة دليل الحسابات"/>
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
        Title = "دليل الحسابات",
        Width = 1000,
        Height = 700
    });
});
```

### Key UI Rules for Phase 22
- **RULE-332**: AccountsListViewModel MUST implement `IDisposable` and call `Cleanup()` in `Dispose()` to unsubscribe from EventBus
- **RULE-338**: AccountEditorViewModel.ValidateAsync() MUST follow `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` pattern
- **RULE-059**: Save/Edit/Delete commands MUST have NO CanExecute predicates (always enabled)
- **RULE-227**: SetDialogService() MUST be called in every editor constructor
- **RULE-185-190**: ALL interactive controls MUST have Arabic ToolTips
- **RULE-262-274**: Compact styles — no hardcoded Height=36/40 or Padding=16+
- **RULE-340**: Desktop Accounts View MUST provide dual-mode display (TreeView + DataGrid toggle)
```

### Phase 22 Code Review Fixes (v4.6.9+)

#### Account Editor Edit Mode
- **RULE-348**: Account code MUST be read-only during edit (`IsAccountCodeReadOnly = true`) — NEVER allow changing code on existing accounts
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
```

```

