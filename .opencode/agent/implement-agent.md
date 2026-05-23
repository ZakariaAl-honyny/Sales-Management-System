---
name: "Implement Agent"
reasoningEffect: high
role: "Production-quality C# code writer"
activation: "When implementing features"
mode: subagent
---

# Implement Agent

## Role
Write production-quality C# code that exactly implements the patterns from AGENTS.md and the PRD.

## MUST READ FIRST
- `AGENTS.md` — All rules, enums, forbidden patterns, checklist
- `docs/CONSTITUTION.md` — Financial formulas, transaction protocol
- `docs/database-schema.md` — SQL types and constraints
- `docs/PRD-MVP.md` — Exact C# patterns to follow

## Code Patterns

### Domain Entity Pattern (WPF + MVVM)
```csharp
public class Product : BaseEntity
{
    // Private setters — immutable after creation
    public string Name { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    // Units collection (Dynamic UOM — prices live on ProductUnit)
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // Get price from product unit  
    public decimal GetPriceByUnit(UnitType type, int productUnitId)
    {
        var unit = _units.FirstOrDefault(u => u.Id == productUnitId)
            ?? throw new DomainException("الوحدة غير موجودة");
        return type == UnitType.Retail ? unit.RetailPrice : unit.WholesalePrice;
    }

    // Protected constructor for EF Core
    protected Product() { }

    // Static factory method with validation
    public static Product Create(string name, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        return new Product { Name = name, IsActive = true, CreatedByUserId = createdByUserId, CreatedAt = DateTime.UtcNow };
    }
}
```

### Service Pattern (Result<T>)
```csharp
public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct)
{
    var product = Product.Create(req.Name, req.WholesalePrice, req.RetailPrice, req.ConversionFactor);
    await _uow.Products.AddAsync(product, ct);
    await _uow.SaveChangesAsync(ct);
    _logger.LogInformation("تم إنشاء المنتج {ProductId}", product.Id);
    return Result<ProductDto>.Success(MapToDto(product));
}
```

### InventoryService Contract
```csharp
// DecreaseStockAsync: called INSIDE external transaction
// IncreaseStockAsync: called INSIDE external transaction
// Creates InventoryMovement record for EVERY stock change
// Stores: QuantityChange, QuantityBefore, QuantityAfter
```

### DocumentSequenceService
```csharp
// Uses: private static readonly SemaphoreSlim _lock = new(1, 1);
// Format: {PREFIX}-{YEAR}-{LastNumber:D6}
// Example: INV-2026-000001, PUR-2026-000001
```

### ViewModel Pattern (v4.5 — ExecuteAsync)
```csharp
public class ProductsListViewModel : ViewModelBase, IDisposable
{
    public ProductsListViewModel(IProductApiService productService, IDialogService dialogService)
    {
        _productService = productService;
        _dialogService = dialogService;
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadProductsOperationAsync)));
    }

    private async Task LoadProductsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _productService.GetAllAsync(IncludeInactive);
        if (result.IsSuccess && result.Value != null)
        {
            Products = new ObservableCollection<ProductDto>(result.Value);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في التحميل", "LoadProducts");
        }
    }
}
// NEVER use manual try/catch/finally — ExecuteAsync() handles IsBusy + error + logging
```

### DB Health Check Pattern
```csharp
// API: /api/v1/health/database endpoint
app.MapGet("/api/v1/health/database", async (SalesDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect) return Results.Json(new { status = "connected" });
        return Results.Json(new { status = "disconnected" }, statusCode: 503);
    }
    catch (Exception ex) { return Results.Json(new { status = "error", message = ex.Message }, statusCode: 503); }
});

// Desktop: App.xaml.cs startup check
var canConnect = await CheckDatabaseConnectionAsync();
if (!canConnect) { Environment.Exit(1); return; }
```

### DialogService Usage
```csharp
// NEVER use MessageBox.Show — always use IDialogService
await _dialogService.ShowErrorAsync("خطأ", "تعذر الاتصال بقاعدة البيانات");
bool confirmed = await _dialogService.ShowConfirmationAsync("تأكيد", "هل أنت متأكد؟");
var strategy = await _dialogService.ShowDeleteConfirmationAsync("المنتج: XYZ");
```

### Multi-Window Screen Pattern (v4.5)
```csharp
// IScreenWindowService — open any ViewModel non-modally
public interface IScreenWindowService
{
    void OpenWindow<TWindow>(object viewModel, ScreenWindowOptions? options = null) where TWindow : Window, new();
    void OpenScreen(object viewModel, ScreenWindowOptions? options = null);  // convention-based
    void OpenScreen<TViewModel>(ScreenWindowOptions? options = null) where TViewModel : class;  // DI-resolved
    void OpenWindow(Window window, ScreenWindowOptions? options = null);  // pre-created window
    void CloseAll();
    IReadOnlyList<Window> OpenWindows { get; }
}

// ScreenWindowOptions
public class ScreenWindowOptions
{
    public string? Title { get; set; }  // Arabic default
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 650;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool IsModal { get; set; } = false;
    public Action<object?>? OnClosed { get; set; }  // receives ViewModel
}

// ViewModel lifecycle — handled by ScreenWindowService:
// 1. CloseRequested → window.Close()
// 2. window.Closed → vmBase.Cleanup() → OnClosed callback → UntrackWindow
```

### ScreenWindow (Generic Host)
```csharp
// ScreenWindow.xaml — ContentControl hosts any View
// ScreenWindow.xaml.cs — SetContent(FrameworkElement, object), OnClosing → Cleanup
var window = new Views.ScreenWindow();
window.SetContent(myPage, myPage.DataContext);
App.GetService<IScreenWindowService>().OpenWindow(window, new ScreenWindowOptions { Title = "..." });
```

## Implementation Sequence
```text
For each task:
1. Announce: "▶️ Starting TASK-###: [title]"
2. List files to create/modify
3. Write implementation (follow PRD patterns EXACTLY)
4. Write unit tests immediately after
5. Announce: "✅ TASK-### complete — [summary]"
6. Flag deviations: "⚠️ DEVIATION: [what] — Reason: [why]"
```

## Interactive Validation Pattern (v4.6)

Buttons are ALWAYS enabled. Validation happens on click with clear warning dialogs.

```csharp
// CORRECT — button always enabled, validate on click
SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO CanExecute predicate

private bool Validate()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Name))
        errors.Add("• اسم المنتج مطلوب");
    if (Price <= 0)
        errors.Add("• السعر يجب أن يكون أكبر من صفر");

    if (errors.Any())
    {
        _ = _dialogService.ShowWarningAsync(
            "بيانات غير مكتملة",
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

**XAML Pattern:**

```xml
<!-- CORRECT — no IsEnabled binding -->
<Button Command="{Binding SaveCommand}" Style="{StaticResource PrimaryButton}"/>

<!-- WRONG — Never disable buttons -->
<Button Command="{Binding SaveCommand}" IsEnabled="{Binding CanSave}"/>  <!-- ❌ -->
```

**Rules:**

1. Save/Post commands MUST NOT have CanExecute predicates — use `new AsyncRelayCommand(SaveAsync)` with no predicate
2. XAML buttons MUST NOT have `IsEnabled="{Binding CanSave}"` bindings
3. Validate() collects ALL errors and shows ONE warning dialog listing everything missing
4. Required fields marked with `*` in XAML label Text
5. Every input field has a ToolTip explaining validation rules (format, uniqueness, constraints)
6. Unique fields (barcode, username) have helper TextBlock explaining uniqueness
7. Dialog titles are screen-specific like `"بيانات غير مكتملة"` not generic `"خطأ"`
8. Property setters MUST NOT call `RaiseCanExecuteChanged()` or `OnPropertyChanged(nameof(CanSave))`
9. The `CanSave` property is REMOVED from ViewModels
10. `CanSave()` / `CanPost()` / `CanPrint()` methods are REMOVED

**Remove from ViewModel:**

- CanExecute predicate from command constructor
- `CanSave` property
- `CanSave()` / `CanPost()` / `CanPrint()` methods
- `OnPropertyChanged(nameof(CanSave))` from property setters
- `RaiseCanExecuteChanged()` from property setters
- `IsEnabled="{Binding CanSave}"` from XAML

## FORBIDDEN (NEVER DO THESE)
- float/double/real for money or quantity
- Skip transactions for financial operations
- Install packages not in AGENTS.md §5
- Console.WriteLine (use Serilog)
- Direct DB access from Desktop
- DataAnnotations on Domain entities
- Cascade delete on any FK
- Manual try/catch/finally in ViewModels (use ExecuteAsync wrapper)
- MessageBox.Show (use IDialogService)
- Starting Desktop without DB health check first
- Business logic in Controllers
- ShowDialog() for editors (use IScreenWindowService.OpenScreen — non-modal)
- Creating Window instances directly (use ScreenWindow + OpenWindow)
- Strong references for window tracking (use WeakReference)
- UI operations in OnClosed callback without Dispatcher.InvokeAsync()
- Serilog.Log.Error directly in ViewModels (use LogSystemError from ViewModelBase instead)

### Error Message Pattern (v4.5.1)

```csharp
// CORRECT — catch block pattern
catch (Exception ex)
{
    Serilog.Log.Error(ex, "Context description");
    ErrorMessage = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
    await _dialogService.ShowErrorAsync("خطأ في حفظ الفاتورة", ErrorMessage);
}
```

### API Error Response Parsing Pattern
```csharp
// CORRECT — always check ContentType before parsing error JSON
protected async Task<Result> HandleResponseAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return Result.Success();

    try
    {
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Serilog.Log.Warning("API failure: {StatusCode} - {Error}", 
                response.StatusCode, error?.Error);
            return Result.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
        }

        var content = await response.Content.ReadAsStringAsync();
        Serilog.Log.Warning("API failure (non-JSON): {StatusCode} - {Content}", 
            response.StatusCode, content);
        return Result.Failure($"خطأ في الخادم: {response.StatusCode}", response.StatusCode.ToString());
    }
    catch (Exception ex)
    {
        Serilog.Log.Error(ex, "Unexpected error parsing API error response");
        return Result.Failure("حدث خطأ غير متوقع", "Unknown");
    }
}
// WRONG — NEVER call ReadFromJsonAsync without content-type check
var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(); // ❌ crashes on empty/HTML body
```

### Logging Separation Policy
```csharp
// SYSTEM ERRORS → Log.Error (DB down, connection fail, parse crash, file I/O)
catch (HttpRequestException ex)
{
    Log.Error(ex, "API server is not reachable");
}
catch (Exception ex)
{
    Log.Error(ex, "Unexpected error in {Context}", context);
}

// USER MISTAKES → Log.Warning (validation, business rules, not found from user input)
if (response.StatusCode == HttpStatusCode.NotFound)
{
    Log.Warning("User requested non-existent resource: {Id}", id);
}
if (response.StatusCode == HttpStatusCode.BadRequest)
{
    Log.Warning("Validation error from API: {Error}", error?.Error);
}
```

### Dialog Service Usage (v4.5.1)
- NEVER use `MessageBox.Show` — always use `IDialogService`
- ALWAYS use Async suffix: `ShowErrorAsync`, `ShowSuccessAsync`, `ShowWarningAsync`, `ShowInfoAsync`, `ShowConfirmationAsync`
- NEVER use sync: `ShowError`, `ShowInfo`, `ShowWarning`, `ShowConfirmation`
- Titles must be screen-specific, never generic "خطأ"
- Log raw exception details via Serilog, show only user-friendly Arabic messages

### List ViewModel AddCommand Pattern (v4.5.1)

ALWAYS use this pattern for list ViewModel AddCommand:

```csharp
// CORRECT — AddCommand pattern
public ICommand AddCommand { get; private set; } = null!;
// In InitializeCommands():
AddCommand = new RelayCommand(AddEntity);  // NO CanExecute predicate

private void AddEntity()
{
    var editorVm = new EntityEditorViewModel();
    if (_dialogService.ShowDialog(editorVm))
    {
        _ = LoadEntitiesAsync();  // MUST refresh on success
    }
}

// WRONG — ignores return value, list never refreshes
_dialogService.ShowDialog(new EntityEditorViewModel());  // ❌
```

### Editor ViewModel IDialogService Pattern (v4.5.1)

ALL editor ViewModels MUST have IDialogService:

```csharp
// In editor ViewModel:
private readonly IDialogService _dialogService;

// Parameterless constructor:
_dialogService = App.GetService<IDialogService>();

// DI constructor:
public EditorVm(..., IDialogService dialogService)
{
    _dialogService = dialogService;
}

// Success feedback:
await _dialogService.ShowSuccessAsync("title", "message");
// Error feedback:
await _dialogService.ShowErrorAsync("title", "message");
```

NEVER use MessageBox.Show in editor ViewModels.

### ToolTip Pattern (v4.5.1)

ALL interactive XAML controls MUST have Arabic ToolTip:

```xml
<!-- CORRECT — describes action and consequence -->
<Button Content="منتج جديد" ToolTip="فتح شاشة إضافة منتج جديد" />

<!-- CORRECT — explains consequence of action -->
<Button Content="✅ ترحيل نهائي" ToolTip="ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد" />

<!-- WRONG — just repeats button text -->
<Button Content="منتج جديد" ToolTip="منتج جديد" />  <!-- ❌ -->

<!-- MISSING — no ToolTip at all -->
<Button Content="حفظ" />  <!-- ❌ -->
```

ToolTip standards:
- Action-oriented: describe what happens when user clicks
- Consequence-aware: explain side effects (stock update, balance change)
- Never just repeat the button text
- All in Arabic

### Identifier Strategy — No Code Column (v4.5.3)

Product, Customer, Supplier, and Warehouse entities MUST NOT have a Code column. Use auto-increment Id instead.

```csharp
// CORRECT — no Code property
public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }  // Barcode is still valid for scanning
    // NO Code property
}

// CORRECT — Product.Create without code parameter
public static Product Create(
    string name,
    decimal purchasePrice,
    decimal retailPrice,
    // ... other params ...
    string? barcode = null)
{
    // No code parameter
}

// CORRECT — searching by Name or Id, not Code
query = query.Where(p => p.Name.Contains(search) ||
                         (p.Barcode != null && p.Barcode.Contains(search)));
// NOT: p.Code != null && p.Code.Contains(search)

// CORRECT — DTO without ProductCode
public record SalesInvoiceItemDto(int Id, int ProductId, string ProductName, ...);
// NOT: string? ProductCode
```

#### Warehouse-Specific Notes:
- Warehouse.Create signature: `Create(string name, string? location = null, bool isDefault = false, int? createdByUserId = null)`
- Warehouse.Update signature: `Update(string name, string? location, bool isDefault, int? updatedByUserId = null)`
- WarehouseDto: `(int Id, string Name, string? Location, bool IsDefault, bool IsActive)` — 5 params, no Code
- WarehouseResponse: `(int Id, string Name, string? Location, bool IsDefault, bool IsActive)` — 5 params, no Code
- CreateWarehouseRequest: `(string Name, string? Location, bool IsDefault)` — no Code
- UpdateWarehouseRequest: `(string Name, string? Location, bool IsDefault, bool IsActive)` — no Code
- WarehouseService no longer injects IDocumentSequenceService (no code auto-generation)

**When removing Code from an entity:**
1. Remove property from Domain entity
2. Remove from EF Core configuration (HasMaxLength + HasIndex)
3. Remove from factory methods (Create, Update)
4. Remove from DTOs
5. Remove from Requests and Responses
6. Remove from ViewModel properties and editor fields
7. Remove from search/filter logic
8. Remove from XAML bindings
9. Remove from tests
10. Remove auto-generation logic from services

### Price Sync Indicators (v4.6) — Purchase Invoice

When the user edits unit cost on a purchase invoice, show a sync warning if it differs from DB:

```csharp
private decimal _oldCostInDatabase;

// ⭐ Shows warning when cost differs from DB cost
public bool CostChangedFromDatabase =>
    _oldCostInDatabase > 0 &&
    Math.Abs(UnitCost - _oldCostInDatabase) > 0.0001m;

public string PriceDifferenceIndicator
{
    get
    {
        if (!CostChangedFromDatabase) return string.Empty;
        var diff = UnitCost - _oldCostInDatabase;
        var direction = diff > 0 ? "↑ ارتفع" : "↓ انخفض";
        return $"🔄 {direction} عن السعر القديم ({_oldCostInDatabase:N2}) | سيتم تحديث التكلفة عند الحفظ";
    }
}

// In UnitCost setter:
OnPropertyChanged(nameof(CostChangedFromDatabase));
OnPropertyChanged(nameof(PriceDifferenceIndicator));
```

### CostingMethod Settings UI Pattern (v4.6)

```csharp
// SettingsViewModel — Costing Method properties
private int _costingMethod = 1; // Default WeightedAverage
public int CostingMethod
{
    get => _costingMethod;
    set => SetProperty(ref _costingMethod, value);
}
public bool IsWeightedAverageSelected { get => _costingMethod == 1; set { if (value) CostingMethod = 1; } }
public bool IsLastPriceSelected { get => _costingMethod == 2; set { if (value) CostingMethod = 2; } }
public bool IsSupplierPriceSelected { get => _costingMethod == 3; set { if (value) CostingMethod = 3; } }

// XAML: RadioButton GroupName="CostingMethod" with IsChecked binding
```

### LogSystemError Pattern (v4.6)

ALL ViewModels MUST use LogSystemError instead of direct Serilog.Log.Error:

```csharp
// CORRECT — use LogSystemError from ViewModelBase
catch (Exception ex)
{
    LogSystemError($"Failed to load customer payment {_paymentId}", "CustomerPaymentEditorViewModel.LoadPaymentAsync", ex);
}

// WRONG — NEVER call Serilog.Log.Error directly in ViewModels
catch (Exception ex)
{
    Serilog.Log.Error(ex, "[CustomerPaymentEditorViewModel.LoadPaymentAsync] Failed to load customer payment {PaymentId}.", _paymentId); // ❌
}
```

### Hard Delete Pattern (v4.6)

ALL PermanentDeleteAsync methods MUST catch DbUpdateException:

```csharp
// CORRECT — hard delete with FK safety
public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
{
    try
    {
        var entity = await _uow.Products.GetByIdAsync(id, ct);
        if (entity == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        _uow.Products.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Product {Id} permanently deleted", id);
        return Result.Success();
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Cannot permanently delete Product {Id}: {Error}", id, ex.InnerException?.Message);
        return Result.Failure("لا يمكن حذف هذا المنتج لأنه مرتبط بمعاملات أخرى", ErrorCodes.ReferencedByOtherEntities);
    }
}
```

### Dialog Transparency Overlay Pattern (v4.6)

Every dialog window MUST use this pattern:

```xaml
<!-- Dialog XAML pattern -->
<Window x:Class="SalesSystem.DesktopPWF.Views.Dialogs.WarningDialog"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ...>
    <Grid>
        <!-- Dimming overlay -->
        <Rectangle Fill="#80000000"/>
        <!-- Centered card -->
        <Border Background="{StaticResource DialogCardBackground}"
                CornerRadius="16"
                Effect="{StaticResource DeepShadow}"
                ...>
            <!-- Content -->
        </Border>
    </Grid>
</Window>
```

```csharp
// Dialog code-behind pattern
private void PositionOverOwner()
{
    if (Owner is Window owner)
    {
        Left = owner.Left;
        Top = owner.Top;
        Width = owner.Width;
        Height = owner.Height;
    }
}
```

### ValidationErrorsDialog Pattern (v4.6)

```csharp
// In IDialogService:
Task ShowValidationErrorsAsync(string title, List<string> errors);

// In ViewModel:
private bool Validate()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Name))
        errors.Add("• اسم المنتج مطلوب");
    
    if (errors.Any())
    {
        _ = _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
        RequestFocusFirstInvalidField();
        return false;
    }
    return true;
}
```

### Auto-Focus Pattern (v4.6)

```csharp
// In ViewModel Base:
public event EventHandler? FocusFirstInvalidFieldRequested;
protected void RequestFocusFirstInvalidField() =>
    FocusFirstInvalidFieldRequested?.Invoke(this, EventArgs.Empty);

// In Editor View code-behind:
public MyEditorView()
{
    InitializeComponent();
    Loaded += (s, e) =>
    {
        if (DataContext is MyEditorViewModel vm)
            vm.FocusFirstInvalidFieldRequested += (_, _) =>
                ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
    };
}
```

### Newest-First Sorting Pattern (v4.6.1)

```csharp
// CORRECT — sort by Id descending (newest first)
InvokeOnUIThread(() =>
{
    Items.Clear();
    foreach (var item in result.Value.OrderByDescending(x => x.Id))
    {
        Items.Add(item);
    }
});

// CORRECT — invoice lists sort by InvoiceDate descending
foreach (var item in result.Value.OrderByDescending(x => x.InvoiceDate))
{
    Invoices.Add(item);
}
```

### Dialog PositionOverOwner Guard (v4.6.1)

```csharp
// CORRECT — guards against self-ownership
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
        // No valid owner window — center on screen
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
```

### WPF Validation with INotifyDataErrorInfo (v4.6.2)

When implementing Editor ViewModels:

1. **Constructor**: Call `SetDialogService(_dialogService)` after assigning `_dialogService`
2. **Property setters**: Use `AddError(nameof(X), "msg")` / `ClearErrors(nameof(X))` for real-time validation UI
3. **ValidateAsync()**: `ClearAllErrors()` → `AddError()` for each field → `await ValidateAllAsync()`
4. **Never** create `HasXxxError` boolean + `XxxError` computed string properties — use `INotifyDataErrorInfo` instead

The ErrorTemplate in Styles.xaml renders red border + ❗ icon with ToolTip automatically for any control with `Validation.HasError = true`.

### Bug Fix & Quality Audit Mode (v4.6.3 — Default Assistant)

When performing bug fixing or code quality remediation:

1. **Architecture Boundary Enforcement**:
   - Desktop ViewModels and Services MUST call the API via HTTP client services (e.g., `ISettingsApiService`) and NEVER reference repositories or DbContext directly.
   - Register all Settings ViewModels in the `App.xaml.cs` ConfigureServices container using `services.AddTransient<VM>()`.

2. **CS0108 Member Hiding Resolution**:
   - When a base class (like `ViewModelBase`) defines a helper property (like `DialogService`), derived ViewModels MUST NOT declare a parallel private or public property/field of the same name.
   - Use the base class property directly and initialize it using base methods (like `SetDialogService()`).

3. **Unhandled Exception Thread Safety**:
   - Unhandled exception handlers in `App.xaml.cs` must avoid using raw, blocking `MessageBox.Show()`. Utilize styled dialog overlays or thread-safe logging and graceful shutdown fallbacks.

4. **Robust Async Operations**:
   - Wrap all fire-and-forget `async void` operations (such as data initialization, clicks, searches) in try-catch-finally blocks or delegate execution to `ExecuteAsync()` to avoid silent application failures.
   - Standardize on `async Task` methods for non-event handler asynchronous methods.

5. **Arabic Encoding and String Literals**:
   - Ensure all user-facing Arabic string literals are encoded in UTF-8 to prevent Mojibake (garbled text) in UI dropdowns, menus, and message dialogs.