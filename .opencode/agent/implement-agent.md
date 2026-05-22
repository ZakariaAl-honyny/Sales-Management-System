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
- `docs/PRD-MVP-v3.0.md` — Exact C# patterns to follow

## Code Patterns

### Domain Entity Pattern (WPF + MVVM)
```csharp
public class Product : BaseEntity
{
    // Private setters — immutable after creation
    public string Name { get; private set; }
    public decimal WholesalePrice { get; private set; } // decimal(18,2)
    public decimal RetailPrice { get; private set; }    // decimal(18,2)
    public decimal ConversionFactor { get; private set; } // decimal(18,3)
    public bool IsActive { get; private set; } = true;

    // Wholesale/Retail Logic (Domain Only)
    public decimal GetUnitPrice(SaleMode mode) => mode == SaleMode.Wholesale ? WholesalePrice : RetailPrice;

    // Protected constructor for EF Core
    protected Product() { }

    // Static factory method with validation
    public static Product Create(string name, decimal wholesalePrice, decimal retailPrice, decimal conversionFactor)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        return new Product { Name = name, WholesalePrice = wholesalePrice, RetailPrice = retailPrice, ConversionFactor = conversionFactor };
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