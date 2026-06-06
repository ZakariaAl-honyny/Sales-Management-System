---
name: "Implement Agent"
reasoningEffect: high
role: "Production-quality C# code writer"
activation: "When implementing features"
mode: subagent
---

# Implement Agent

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

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

### ⚠️ Transaction Strategy — CRITICAL
NEVER use `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()` in services because `SqlServerRetryingExecutionStrategy` is configured — it DOES NOT support user-initiated transactions.

**CORRECT**: Single `SaveChangesAsync()` (EF Core wraps it in an implicit transaction):
```csharp
// Single SaveChangesAsync — atomic via EF Core implicit transaction
await _uow.Currencies.AddAsync(currency, ct);
await _uow.SaveChangesAsync(ct);
```

**CORRECT (multi-write atomicity)**: Use `CreateExecutionStrategy().ExecuteAsync()`:
```csharp
await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
    // multiple write operations...
    await _dbContext.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
});
```

**WRONG — This throws InvalidOperationException:**
```csharp
await using var transaction = await _uow.BeginTransactionAsync(ct); // ❌ CRASHES
try { ... await transaction.CommitAsync(ct); }
catch { await transaction.RollbackAsync(ct); throw; }
```

### InventoryService Contract
```csharp
// DecreaseStockAsync: called INSIDE external transaction
// IncreaseStockAsync: called INSIDE external transaction
// Creates InventoryMovement record for EVERY stock change
// Stores: QuantityChange, QuantityBefore, QuantityAfter
```

### isSystem Protection Pattern
```csharp
// Domain entity factory — add `isSystem` param for system-protected entities
public static Currency Create(string name, string code, string symbol,
    decimal exchangeRateToBase, bool isBaseCurrency = false,
    string? fractionName = null, bool isSystem = false)
{
    // ... validation ...
    return new Currency
    {
        Name = name.Trim(),
        // ...
        IsSystem = isSystem,  // NOT hardcoded false
    };
}

// MarkAsDeleted — always guard system records
public override void MarkAsDeleted()
{
    if (IsSystem)
        throw new DomainException("لا يمكن حذف عملة النظام — العملة محمية");
    IsActive = false;
    UpdateTimestamp();
}
```

### Controller 404 vs 400 Pattern
```csharp
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id, CancellationToken ct)
{
    var result = await _currencyService.DeleteAsync(id, userId, ct);
    if (result.IsSuccess)
        return Ok(new { message = "تم حذف العملة بنجاح" });
    if (result.ErrorCode == ErrorCodes.NotFound)
        return NotFound(new { error = result.Error });
    return BadRequest(new { error = result.Error });
}
```

### DocumentSequenceService
```csharp
// Thread-safe via: private static readonly SemaphoreSlim _lock = new(1, 1);
// Supports both string prefix sequences and int sequences

// String format: {PREFIX}-{YEAR}-{LastNumber:D6}
// Example: GetNextNumberAsync("INV", ct) → "INV-2026-000001"
// Example: GetNextNumberAsync("PUR", ct) → "PUR-2026-000001"

// Int format: simple incrementing integer
// Example: GetNextIntAsync("SalesInvoice", ct) → 42
// Example: GetNextIntAsync("PurchaseInvoice", ct) → 17

// DocumentSequence entity has both GetNextNumber() and GetNextInt() methods
// ALWAYS use GetNextIntAsync for InvoiceNo generation — NEVER lastId + 1
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
- Skip `SaveChangesAsync` for financial operations
- Use `BeginTransactionAsync` when `SqlServerRetryingExecutionStrategy` is configured (use single `SaveChangesAsync` instead)
- Bare `.WithOne()` on relationships — always specify `.WithOne(x => x.NavigationProperty)`
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
- lastId + 1 for InvoiceNo generation (use DocumentSequenceService.GetNextIntAsync instead — not thread-safe)
- Non-unique InvoiceNo (duplicates cause search/return/report confusion — use UNIQUE index per document type)

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

### Invoice Number Strategy — InvoiceNo as int, UNIQUE, Thread-Safe via DocumentSequenceService (v4.6.7)

SalesInvoice and PurchaseInvoice have `int InvoiceNo` — a user-facing invoice number, separate from the auto-increment `Id` PK. UNIQUE per document type (duplicates NOT allowed). Default generated thread-safely via `IDocumentSequenceService.GetNextIntAsync()`.

```csharp
// CORRECT — int InvoiceNo property (NOT string)
public class SalesInvoice : BaseEntity
{
    public int Id { get; private set; }       // Auto-increment PK
    public int InvoiceNo { get; private set; } // User-facing invoice number (UNIQUE per type)
    public int WarehouseId { get; private set; }
}

// CORRECT — SalesInvoice.Create with int invoiceNo parameter
public static SalesInvoice Create(
    int invoiceNo,                       // Required int invoice number
    int warehouseId,
    int? customerId = null,
    DateTime? invoiceDate = null,
    int? createdByUserId = null)
{
    if (invoiceNo <= 0)
        throw new DomainException("رقم الفاتورة يجب أن يكون أكبر من الصفر");
    // ...
}

// CORRECT — service generates default via DocumentSequenceService (NEVER lastId + 1)
var invoiceNo = request.InvoiceNo ?? 0;
if (invoiceNo <= 0)
{
    invoiceNo = await _documentSequenceService.GetNextIntAsync("SalesInvoice", ct);
}

// CORRECT — DocumentSequenceService with SemaphoreSlim (thread-safe)
public class DocumentSequenceService : IDocumentSequenceService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<int> GetNextIntAsync(string sequenceKey, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var sequence = await _uow.DocumentSequences
                .FindByKeyAsync(sequenceKey, ct);
            if (sequence == null)
            {
                sequence = DocumentSequence.Create(sequenceKey, 1);
                await _uow.DocumentSequences.AddAsync(sequence, ct);
            }
            else
            {
                sequence.IncrementNextInt();
            }
            await _uow.SaveChangesAsync(ct);
            return sequence.CurrentInt;
        }
        finally
        {
            _lock.Release();
        }
    }
}

// CORRECT — searching by InvoiceNo (int comparison)
var searchText = SearchText?.Trim();
if (!string.IsNullOrWhiteSpace(searchText) && int.TryParse(searchText, out var num))
{
    Invoices = Invoices.Where(i => i.InvoiceNo == num).ToList();
}

// CORRECT — report DTO with int InvoiceNo
public record SalesReportDto(
    DateTime InvoiceDate,
    int Id,
    int InvoiceNo,                       // int, not string
    string CustomerName,
    decimal SubTotal, ...);

// CORRECT — print DTO formats int as string
public record InvoicePrintDto(
    string InvoiceNumber,                // Formatted from InvoiceNo.ToString()
    string CustomerName, ...);

// Builder: .InvoiceNumber = invoice.InvoiceNo.ToString()
```

**When adding/supporting InvoiceNo as int, UNIQUE, with DocumentSequenceService:**
1. Add `int InvoiceNo` property to Domain entity with guard `invoiceNo <= 0`
2. Add `int invoiceNo` parameter to factory methods (Create)
3. Remove old string `InvoiceNo` config, keep int with `.HasIndex(i => i.InvoiceNo).IsUnique()`
4. Add `int InvoiceNo` to DTOs and Responses
5. Service calls `IDocumentSequenceService.GetNextIntAsync("SalesInvoice"/"PurchaseInvoice", ct)` — NEVER `lastId + 1`
6. Validators: `GreaterThan(0)` rule only when InvoiceNo is provided; validate uniqueness on user override
7. Desktop VM: int property, `InvoiceNo = 0` for new (service computes via DocumentSequenceService)
8. Search/filter by `InvoiceNo == parsedInt || Id == parsedInt`
9. UNIQUE index on InvoiceNo per document type — catch DbUpdateException on user override duplicate

**Note:** `SupplierInvoiceNo` (string?) on `PurchaseInvoice` is the SUPPLIER's invoice reference number — this is NOT the system InvoiceNo and is kept for supplier reference only. Do not confuse it.

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

### Rate Limiting Pattern (v4.6.4)

**Service Registration (Program.cs):**
```csharp
builder.Services.AddRateLimiter(options =>
{
    // Global: 100 req/min per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Login: 5 attempts per 15 min
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    // Arabic 429 response
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0");
        }
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Error = "تم تجاوز الحد المسموح من الطلبات. حاول مجدداً بعد قليل",
            Code = "RATE_LIMIT_EXCEEDED"
        }, ct);
    };
});
```

**Middleware Pipeline Order (CRITICAL):**
```csharp
app.UseHttpsRedirection();
app.UseCors("DesktopOnly");
app.UseRateLimiter();       // ← BEFORE auth
app.UseAuthentication();
app.UseAuthorization();
```

**Controller Usage:**
```csharp
[HttpPost("login")]
[EnableRateLimiting("LoginPolicy")]
[AllowAnonymous]
public async Task<IActionResult> Login(LoginRequest request)
```

### User Hard-Delete Guard Pattern (v4.6.4)

```csharp
// PermanentDeleteAsync MUST return Result.Failure — never hard-delete users
public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default)
{
    _logger.LogWarning("Attempt to hard-delete user {UserId} blocked — soft delete only", id);
    return Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي — استخدم خاصية تعطيل الحساب بدلاً من ذلك",
        ErrorCodes.InvalidOperation);
}
```

### Connection String Security Pattern (v4.6.4)

```json
// appsettings.Development.json — connection string from env var only
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "_comment": "Connection string is loaded from SALESSYSTEM_DB_CONNECTION environment variable per RULE-040"
  }
}
```

### UI Compacting Pattern (v4.6.6) — Mobile-Ready Density

When creating or modifying XAML views, use compact sizes. DO NOT hardcode sizes that duplicate style defaults.

#### CORRECT XAML Patterns:
```xml
<!-- CORRECT — no hardcoded Height/Padding on buttons → style provides 28px height + 10,4 padding -->
<Button Content="حفظ" Command="{Binding SaveCommand}" Style="{StaticResource PrimaryButton}"/>
<Button Content="إلغاء" Command="{Binding CancelCommand}" Style="{StaticResource SecondaryButton}"/>

<!-- CORRECT — header border with compact padding -->
<Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
    <TextBlock Text="العنوان" FontSize="14" FontWeight="Bold" Foreground="White"/>
</Border>

<!-- CORRECT — footer border with compact padding -->
<Border Background="White" Padding="12,8" BorderThickness="0,1,0,0" BorderBrush="{StaticResource BorderBrush}">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="✅ ترحيل نهائي" Command="{Binding PostCommand}" Style="{StaticResource PrimaryButton}" ToolTip="..."/>
        <Button Content="💾 حفظ" Command="{Binding SaveCommand}" Style="{StaticResource SecondaryButton}" ToolTip="..."/>
    </StackPanel>
</Border>

<!-- CORRECT — compact form field spacing -->
<StackPanel>
    <TextBlock Text="الاسم *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Name}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>
    <TextBlock Text="السعر *" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Price}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,8"/>
</StackPanel>

<!-- CORRECT — compact empty-state -->
<Button Content="➕ إضافة أول منتج" Command="{Binding AddCommand}"
        Style="{StaticResource PrimaryButton}" Margin="0,12,0,0" Width="140"
        ToolTip="فتح شاشة إضافة منتج جديد"/>

<!-- CORRECT — section header with compact font -->
<TextBlock Text="ℹ️ تفاصيل الفاتورة" FontWeight="Bold" FontSize="14" Margin="0,0,0,8"/>
```

#### WRONG Patterns (NEVER Do These):
```xml
<!-- WRONG — hardcoded height that duplicates style -->
<Button Height="36" Padding="16,0" ... />  <!-- ❌ style already provides 28px and 10,4 -->

<!-- WRONG — oversized header/footer padding -->
<Border Padding="16,12" ... />  <!-- ❌ should be 12,6 or 12,8 -->

<!-- WRONG — oversized field spacing -->
<TextBox Margin="0,0,0,12" ... />  <!-- ❌ should be 6 or 8 -->

<!-- WRONG — oversized dialog font -->
<TextBlock FontSize="20" ... />  <!-- ❌ dialog titles max 16, headers max 14 -->

<!-- WRONG — fixed button width (use MinWidth) -->
<Button Width="120" ... />  <!-- ❌ use MinWidth="80" or MinWidth="100" -->
```

#### Style Token Reference (Styles.xaml):
- Button default: Height=28, Padding=10,4
- ModernTextBox/ModernComboBox: Height=28
- PageMargin: 10 (for outer grid margin)
- CardStyle padding: handled internally
- Sidebar: Width=200
- Dialog title: FontSize=16
- Section header: FontSize=14
- DataGrid row: Height=24 (CompactDataGrid)
- Toolbar spacing: Margin between buttons 4-6px

### FallbackErrorDialog Pattern (v4.6.4)

```csharp
// App.xaml.cs — thread-safe fallback for unhandled exceptions
private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
{
    var exception = e.Exception;
    Log.Error(exception, "Unhandled exception");
    
    // Show fallback dialog on UI thread
    Application.Current.Dispatcher.Invoke(() =>
    {
        var dialog = new FallbackErrorDialog(exception.Message);
        dialog.ShowDialog();
    });
    
    e.Handled = true;
}
```

## v4.6.8 — Phase 18 & Phase 20 Remediations

### Transaction Strategy Enforcement (RULE-275/276)
NEVER use `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()` when `SqlServerRetryingExecutionStrategy` is configured — it does NOT support user-initiated transactions.

**For single-write atomicity**: Use a single `SaveChangesAsync()` (EF Core wraps in implicit transaction).

**For multi-write atomicity** (e.g., Annual Closing Service that saves JournalEntry + FiscalYearClosure):
```csharp
await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
    await _uow.JournalEntries.AddAsync(closingEntry, ct);
    await _uow.SaveChangesAsync(ct);
    var closure = FiscalYearClosure.Create(..., closingEntryId: closingEntry.Id);
    await _uow.FiscalYearClosures.AddAsync(closure, ct);
    await _uow.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
});
```

### SystemAccountMappings Navigation Mappings
ALL 13 navigation properties on `SystemAccountMappings` MUST be mapped with proper lambdas, NOT bare `HasOne<Account>()`:
```csharp
// CORRECT — specifies navigation property
builder.HasOne(x => x.DefaultCashAccount).WithMany().HasForeignKey(x => x.DefaultCashAccountId).OnDelete(DeleteBehavior.Restrict);
// REPEAT for all: DefaultBankAccount, AccountsReceivableAccount, InventoryAccount, etc.

// WRONG — bare generic, navigation property NOT mapped
builder.HasOne<Account>().WithMany().HasForeignKey(x => x.DefaultCashAccountId); // ❌
```

### JournalEntryLineConfiguration DB CHECK Constraints
Two CHECK constraints are REQUIRED on `JournalEntryLineConfiguration`:
```csharp
builder.ToTable(t =>
{
    t.HasCheckConstraint("CHK_DebitOrCredit",
        "(Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0) OR (Debit = 0 AND Credit = 0)");
    t.HasCheckConstraint("CHK_NoNegativeValues", "Debit >= 0 AND Credit >= 0");
});
```

### Currency Filtered Indexes — IsActive Guard
All unique indexes on Currency MUST exclude soft-deleted records:
```csharp
// CORRECT — includes IsActive filter
builder.HasIndex(c => c.Name).IsUnique().HasFilter("[IsActive] = 1");
builder.HasIndex(c => c.Code).IsUnique().HasFilter("[IsActive] = 1");
builder.HasIndex(c => c.IsBaseCurrency).IsUnique().HasFilter("[IsBaseCurrency] = 1 AND [IsActive] = 1"); // Two conditions!
```

### JournalEntryLine.Account Navigation
Use `HasOne(x => x.Account)` — NOT `HasOne<Account>()`:
```csharp
// CORRECT
builder.HasOne(x => x.Account).WithMany(x => x.JournalLines).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
// WRONG
builder.HasOne<Account>().WithMany(x => x.JournalLines).HasForeignKey(x => x.AccountId); // ❌
```

### ReversedByEntryId FK Configuration
The self-referencing FK on `JournalEntry` MUST be explicitly configured:
```csharp
builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.ReversedByEntryId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
```

### JournalEntryNumberGenerator Daily Reset
Generator MUST query by today's prefix, not global last entry:
```csharp
var prefix = $"JE-{DateTime.Today:yyyyMMdd}";
var todayEntries = await _uow.JournalEntries.ToListAsync(je => je.EntryNumber.StartsWith(prefix), ct: ct);
var nextNumber = todayEntries.Count + 1;
return $"JE-{DateTime.Today:yyyyMMdd}-{nextNumber:D4}";
```

### Controller 404 vs 400 Pattern
Delete/update endpoints MUST differentiate between NotFound and business errors:
```csharp
if (result.IsSuccess) return Ok(...);
if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
return BadRequest(new { error = result.Error });
```

### Currency.Read Endpoints — AllStaff Policy
GET endpoints for Currency MUST use `AllStaff` policy, not `AdminOnly`:
```csharp
[Authorize(Policy = "AllStaff")]
[HttpGet]
public async Task<IActionResult> GetAll(...)
```

## v4.6.9 — Phase 19 Settings Module Remediations

### SystemSetting.Create() Pattern
```csharp
public static SystemSetting Create(string settingKey, string settingValue,
    string dataType = "string", string category = "General", ...)
{
    if (string.IsNullOrWhiteSpace(settingKey))
        throw new DomainException("مفتاح الإعداد مطلوب.");
    if (string.IsNullOrWhiteSpace(category))
        throw new DomainException("تصنيف الإعداد مطلوب.");
    var validDataTypes = new[] { "string", "int", "bool", "decimal" };
    if (!validDataTypes.Contains(dataType))
        throw new DomainException("نوع البيانات غير صالح.");
    // ...
}
```

### Repository → Service Commit Pattern
```csharp
// Repository — only prepares entities
public async Task SetBatchSystemSettingsAsync(...)
{
    // ... entity operations ...
    // NO SaveChangesAsync here
    InvalidateCache();
}

// Service — owns the commit
public async Task<Result> UpdateSystemSettingsAsync(...)
{
    await _systemSettingsRepo.SetBatchSystemSettingsAsync(settings, ct);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}
```

### Tax.Update() Pattern
```csharp
public void Update(string name, decimal rate, bool isDefault)
{
    // ... guard clauses ...
    Name = name.Trim();
    Rate = rate;
    IsDefault = isDefault;
    UpdateTimestamp();  // REQUIRED
}
```

### Controller Error Handling Pattern
```csharp
return result.ErrorCode == ErrorCodes.NotFound
    ? NotFound(new { error = result.Error })
    : BadRequest(new { error = result.Error });
```

## v4.6.9 — Phase 20 BUG-008 Fix: CurrencyCode Domain Validation

When implementing Currency entity validation, use this pattern:

```csharp
if (string.IsNullOrWhiteSpace(code))
    throw new DomainException("رمز العملة مطلوب.");
if (code.Trim().Length != 3)
    throw new DomainException("رمز العملة يجب أن يكون 3 أحرف.");
// Then apply uppercase:
code = code.Trim().ToUpperInvariant();
```

ISO 4217 currency codes are ALWAYS exactly 3 uppercase characters. Never use a generic max-length validation.

## v4.6.9 — Phase 19 Settings Module Enhancement Patterns

### Tax.ClearDefault() and Tax.SetDefault() Pattern
```csharp
public void SetDefault()
{
    IsDefault = true;
    UpdateTimestamp();  // REQUIRED — audit trail
}

public void ClearDefault()
{
    IsDefault = false;
    UpdateTimestamp();
}
```

### SystemSettingsViewModel Property Pattern
Every system setting seeded in DbSeeder MUST have a corresponding strongly-typed property in `SystemSettingsViewModel`, with mapping in both `MapFromDictionary()` and `BuildDictionary()`. Boolean settings use `ParseBool()`, integer settings use `ParseInt()`:

```csharp
private bool _hideTaxInSales;
public bool HideTaxInSales
{
    get => _hideTaxInSales;
    set => SetProperty(ref _hideTaxInSales, value);
}

// MapFromDictionary:
HideTaxInSales = ParseBool(settings, "HideTaxInSales");

// BuildDictionary:
["HideTaxInSales"] = HideTaxInSales.ToString().ToLower(),
```

### Service-Level System Settings Validation Pattern
When accepting `Dictionary<string, string>` for batch settings update, validate known keys before saving:

```csharp
private static string? ValidateSystemSettings(Dictionary<string, string> settings)
{
    foreach (var kvp in settings)
    {
        if (string.IsNullOrWhiteSpace(kvp.Key))
            return "مفتاح الإعداد لا يمكن أن يكون فارغاً";
        var value = kvp.Value;
        switch (kvp.Key)
        {
            case "CostingMethod":
            case "StockAlertDays":
            case "DecimalPlaces":
            case "ExpiryAlertDays":
                if (!int.TryParse(value, out var intVal) || intVal < 0)
                    return $"قيمة '{kvp.Key}' يجب أن تكون رقماً صحيحاً موجباً";
                if (kvp.Key == "CostingMethod" && (intVal < 1 || intVal > 3))
                    return "طريقة التكلفة يجب أن تكون 1-3";
                if (kvp.Key == "DecimalPlaces" && (intVal < 0 || intVal > 6))
                    return "عدد المنازل العشرية يجب أن يكون بين 0 و 6";
                break;
            case "AllowNegativeStock":
            case "AutoPostInvoices":
            // All boolean keys...
                if (!bool.TryParse(value, out _))
                    return $"قيمة '{kvp.Key}' يجب أن تكون true أو false";
                break;
        }
    }
    return null;
}
```

## v4.6.9 — Phase 20 Currency Module Enhancement Patterns

### CashBox.Create() — Fix OpeningBalance
```csharp
public static CashBox Create(... decimal initialBalance = 0)
{
    // ... guard clauses ...
    return new CashBox
    {
        OpeningBalance = initialBalance,  // REQUIRED — was always 0!
        CurrentBalance = initialBalance,
        // ...
    };
}
```

### Currency SetAsBaseCurrency / UnsetBaseCurrency Pattern
```csharp
public void SetAsBaseCurrency()
{
    IsBaseCurrency = true;
    UpdateTimestamp();  // REQUIRED — audit trail
}

public void UnsetBaseCurrency()
{
    IsBaseCurrency = false;
    UpdateTimestamp();
}
```

### InvokeOnUIThreadAsync — No Unnecessary async
```csharp
// CORRECT — no async keyword when no await inside
await InvokeOnUIThreadAsync(() =>
{
    RateHistory.Clear();
    foreach (var item in result.Value) { RateHistory.Add(item); }
});

// WRONG — async but no await
await InvokeOnUIThreadAsync(async () => { /* no await */ });  // ❌
```

## v4.6.9 — Phase 21 Users & Permissions Module Patterns

### Passwordless User Creation
```csharp
public static User Create(string userName, string fullName, UserRole role,
    string? phone = null, string? email = null, int? defaultCashBoxId = null,
    int? createdByUserId = null)
{
    if (string.IsNullOrWhiteSpace(userName))
        throw new DomainException("اسم المستخدم مطلوب.");
    if (string.IsNullOrWhiteSpace(fullName))
        throw new DomainException("الاسم الكامل مطلوب.");
    return new User
    {
        UserName = userName.Trim(),
        FullName = fullName.Trim(),
        Role = role,
        Status = UserStatus.Active,
        Phone = phone?.Trim(),
        Email = email?.Trim(),
        DefaultCashBoxId = defaultCashBoxId,
        MustChangePassword = true,
        PasswordHash = null,
        LoginAttempts = 0,
        CreatedByUserId = createdByUserId,
        CreatedAt = DateTime.UtcNow
    };
}
```

### Login Attempt Tracking Pattern
```csharp
public void RecordLoginAttempt(bool success)
{
    if (success)
    {
        LoginAttempts = 0;
        Status = UserStatus.Active;
        LastLoginAt = DateTime.UtcNow;
    }
    else
    {
        LoginAttempts++;
        if (LoginAttempts >= 5)
            Status = UserStatus.Locked;
    }
}
```

### AuthService Login — MustChangePassword + Lockout
```csharp
if (user.Status == UserStatus.Locked)
    return Result<LoginResponse>.Failure("الحساب مغلق مؤقتاً", ErrorCodes.AccountLocked);

if (user.MustChangePassword || string.IsNullOrWhiteSpace(user.PasswordHash))
    return Result<LoginResponse>.Failure("يجب تعيين كلمة المرور", ErrorCodes.RequiresPasswordSetup);
```

### Audit Entity Pattern (long Id)
```csharp
public class AuditLog
{
    public long Id { get; private set; }  // bigint for high volume
    public int? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    // ...
}
```

### Permission Entity with IsSystem Guard
```csharp
public class Permission : BaseEntity
{
    public string Name { get; private set; }
    public string DisplayNameAr { get; private set; }
    public bool IsSystem { get; private set; }  // System permissions cannot be deleted
}
```

### PermissionService Transactional Update
```csharp
public async Task<Result> UpdateRolePermissionsAsync(UserRole role, List<int> permissionIds, CancellationToken ct)
{
    await _uow.ExecuteTransactionAsync(async () =>
    {
        var existing = await _uow.RolePermissions.GetQueryable()
            .Where(rp => rp.Role == role).ToListAsync(ct);
        _uow.RolePermissions.RemoveRange(existing);
        foreach (var permId in permissionIds)
            await _uow.RolePermissions.AddAsync(RolePermission.Create(role, permId), ct);
    }, ct);
    return Result.Success();
}
```

## Default Bug Fixing

When implementing new features or modifying existing code, you MUST:
1. Fix any garbled Arabic strings you encounter (rewrite with correct UTF-8 Arabic)
2. Fix any `MessageBox.Show` calls by replacing with `IDialogService` calls
3. Fix any direct `HttpClient` usage in ViewModels (should use typed service classes)
4. Fix any shadowed base class properties (e.g., `_dialogService` fields that shadow `DialogService` property)
5. Fix any service locator patterns (`App.GetService<T>()` in ViewModels that should use constructor injection)
6. Apply all relevant rules from AGENTS.md CONSTITUTION automatically without being asked