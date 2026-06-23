---
name: "Implement Agent"
model: opencode/deepseek-v4-flash-free
reasoningEffect: max
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

### Domain Entity Pattern — Product (65-table schema — TaxId on invoices only, Barcode on entity)
```csharp
public class Product : BaseEntity
{
    // Private setters — immutable after creation
    public string Name { get; private set; }
    public string? Barcode { get; private set; }       // Products.Barcode varchar(50) null unique filtered
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public string? Description { get; private set; }
    public bool TrackExpiry { get; private set; }
    public int? DefaultPurchaseUnitId { get; private set; }  // Pre-selects unit in purchase screens
    public int? DefaultSalesUnitId { get; private set; }     // Pre-selects unit in sales screens
    public bool IsActive { get; private set; } = true;

    // Units collection — prices live on ProductPrices, not here
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // NO PurchasePrice, SalePrice, WholesalePrice, RetailPrice, TaxId, AvgCost, OpeningQuantity on Product
    // TaxId is on SalesInvoices/PurchaseInvoices only (invoice-level, NOT product-level)

    // Protected constructor for EF Core
    protected Product() { }

    // Static factory method with validation
    public static Product Create(string name, int categoryId, int createdByUserId,
        string? description = null, bool trackExpiry = false,
        int? defaultPurchaseUnitId = null, int? defaultSalesUnitId = null,
        string? barcode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (categoryId <= 0)
            throw new DomainException("مجموعة المنتج مطلوبة");
        return new Product
        {
            Name = name.Trim(),
            Barcode = barcode?.Trim(),
            CategoryId = categoryId,
            Description = description?.Trim(),
            TrackExpiry = trackExpiry,
            DefaultPurchaseUnitId = defaultPurchaseUnitId,
            DefaultSalesUnitId = defaultSalesUnitId,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    // RULE-067: Product MUST have minimum 2 units (base + one additional)
    public void ValidateUnits()
    {
        if (_units.Count < 2)
            throw new DomainException("المنتج يجب أن يحتوي على وحدتين على الأقل: وحدة أساسية + وحدة إضافية");
    }

    // RULE-067: Cannot remove below minimum 2 units
    public void RemoveUnit(ProductUnit unit)
    {
        if (_units.Count <= 2)
            throw new DomainException("لا يمكن حذف الوحدة — يجب أن يحتوي المنتج على وحدتين على الأقل");
        // ... remove logic
    }
}
```

**RULE-543: TaxId is on invoices only (NOT on Product entity)**
- `SalesInvoices.TaxId` and `PurchaseInvoices.TaxId` are the source of truth for tax rate
- `Tax` entity determines the rate applied at sale/purchase time
- Product catalog does NOT store tax — it's determined per transaction

**RULE-544: Product creation = 3 tables atomic via ExecuteTransactionAsync**
```csharp
// Service.CreateAsync MUST wrap in ExecuteTransactionAsync:
return await _uow.ExecuteTransactionAsync<Result<ProductDto>>(async () =>
{
    var product = Product.Create(...);
    await _uow.Products.AddAsync(product, ct);
    // ... add ProductUnits, ProductPrices ...
    await _uow.SaveChangesAsync(ct);
    return Result<ProductDto>.Success(MapToDto(product));
}, ct);
```

**RULE-545: Opening stock is separate inventory transaction**
- NOT stored on Product entity (no OpeningQuantity/OpeningUnitCost fields)
- Use `InventoryAdjustments` with Type=Opening or `InventoryBatches` for initial stock
- Product creation handles catalog data only (name, category, units, prices)

### ProductPrices Entity Pattern (per unit × currency × effective dates)
```csharp
public class ProductPrice : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public ProductUnit? ProductUnit { get; private set; }
    public int CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }
    public decimal Price { get; private set; }           // decimal(18,2)
    public DateTime EffectiveFrom { get; private set; }   // datetime2
    public DateTime? EffectiveTo { get; private set; }    // nullable datetime2

    protected ProductPrice() { }

    public static ProductPrice Create(int productUnitId, int currencyId, decimal price,
        DateTime effectiveFrom, DateTime? effectiveTo = null, int createdByUserId = 0)
    {
        if (price < 0)
            throw new DomainException("السعر لا يمكن أن يكون سالباً");
        if (effectiveFrom == default)
            throw new DomainException("تاريخ بدء السعر مطلوب");
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
            throw new DomainException("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية");
        return new ProductPrice
        {
            ProductUnitId = productUnitId,
            CurrencyId = currencyId,
            Price = price,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
```

### InventoryBatch Entity Pattern (FIFO/FEFO)
```csharp
public class InventoryBatch : BaseEntity
{
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int WarehouseId { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    public string? BatchNo { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public decimal QuantityReceived { get; private set; }  // decimal(18,3)
    public decimal QuantityRemaining { get; private set; } // decimal(18,3) — decreases on sale
    public decimal UnitCost { get; private set; }          // decimal(18,2)
    public int? PurchaseInvoiceId { get; private set; }    // nullable FK

    public static InventoryBatch Create(int productId, int warehouseId, decimal quantity,
        decimal unitCost, string? batchNo = null, DateTime? expiryDate = null,
        int? purchaseInvoiceId = null, int createdByUserId = 0)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من صفر");
        if (unitCost < 0)
            throw new DomainException("التكلفة لا يمكن أن تكون سالبة");
        return new InventoryBatch
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            BatchNo = batchNo,
            ExpiryDate = expiryDate,
            QuantityReceived = quantity,
            QuantityRemaining = quantity,
            UnitCost = unitCost,
            PurchaseInvoiceId = purchaseInvoiceId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deduct(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية المراد خصمها يجب أن تكون أكبر من صفر");
        if (quantity > QuantityRemaining)
            throw new DomainException("الكمية المطلوبة غير متوفرة في هذه الدفعة");
        QuantityRemaining -= quantity;
        UpdateTimestamp();
    }
}
```

### Party Entity Pattern (shared contact data)
```csharp
public class Party : ActivatableEntity
{
    public string Name { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Notes { get; private set; }

    protected Party() { }

    public static Party Create(string name, string? phone = null, string? email = null,
        string? address = null, string? taxNumber = null, string? notes = null,
        int createdByUserId = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("الاسم مطلوب");
        return new Party
        {
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Notes = notes?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Update(string name, string? phone = null, string? email = null,
        string? address = null, string? taxNumber = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("الاسم مطلوب");
        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        Notes = notes?.Trim();
        UpdateTimestamp();
    }
}
```

### Unit Entity Pattern (independent table, seedable)
```csharp
public class Unit : BaseEntity
{
    // smallint PK — lookup table
    public string Name { get; private set; }
    public string Symbol { get; private set; }
    public bool IsSystem { get; private set; }   // Protects seed units
    public bool IsActive { get; private set; } = true;

    protected Unit() { }

    public static Unit Create(string name, string symbol, bool isSystem = false,
        int createdByUserId = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الوحدة مطلوب");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز الوحدة مطلوب");
        return new Unit
        {
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            IsSystem = isSystem,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public override void MarkAsDeleted()
    {
        if (IsSystem)
            throw new DomainException("لا يمكن حذف وحدة النظام — الوحدة محمية");
        IsActive = false;
        UpdateTimestamp();
    }
}
```

### ProductUnit Pattern (with Factor, IsBaseUnit, DefaultPurchase/Sales)
```csharp
public class ProductUnit : BaseEntity
{
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int UnitId { get; private set; }              // FK to Units table
    public Unit? Unit { get; private set; }
    public decimal Factor { get; private set; }           // Conversion to base unit (Base=1, Carton=24)
    public bool IsBaseUnit { get; private set; }          // Exactly one per product
    public bool IsActive { get; private set; } = true;

    private readonly List<UnitBarcode> _barcodes = new();
    public IReadOnlyCollection<UnitBarcode> Barcodes => _barcodes.AsReadOnly();

    protected ProductUnit() { }

    public static ProductUnit Create(int productId, int unitId, decimal factor,
        bool isBaseUnit = false, int createdByUserId = 0)
    {
        if (factor <= 0)
            throw new DomainException("معامل التحويل يجب أن يكون أكبر من صفر");
        if (isBaseUnit && factor != 1)
            throw new DomainException("الوحدة الأساسية يجب أن يكون معامل تحويلها 1");
        return new ProductUnit
        {
            ProductId = productId,
            UnitId = unitId,
            Factor = factor,
            IsBaseUnit = isBaseUnit,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Unit conversion
    public decimal ConvertToBase(decimal quantity) => quantity * Factor;
    public decimal ConvertFromBase(decimal baseQuantity) => baseQuantity / Factor;
}
```

### WarehouseTransfer / WarehouseTransferLine Pattern (replaces StockTransfer)
```csharp
public class WarehouseTransfer : DocumentEntity  // Draft/Posted/Cancelled
{
    public string TransferNo { get; private set; }  // Generated via DocumentSequence
    public int FromWarehouseId { get; private set; }
    public Warehouse? FromWarehouse { get; private set; }
    public int ToWarehouseId { get; private set; }
    public Warehouse? ToWarehouse { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<WarehouseTransferLine> _lines = new();
    public IReadOnlyCollection<WarehouseTransferLine> Lines => _lines.AsReadOnly();

    public static WarehouseTransfer Create(string transferNo, int fromWarehouseId,
        int toWarehouseId, string? notes = null, int createdByUserId = 0) { ... }
}

public class WarehouseTransferLine : BaseEntity
{
    public int TransferId { get; private set; }
    public WarehouseTransfer? Transfer { get; private set; }
    public int ProductUnitId { get; private set; }
    public ProductUnit? ProductUnit { get; private set; }
    public decimal Quantity { get; private set; }  // In base unit
    public string? BatchNo { get; private set; }
}
```

### InventoryTransaction / InventoryTransactionLine Pattern (replaces InventoryMovement)
```csharp
public class InventoryTransaction : BaseEntity
{
    public int WarehouseId { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    public string ReferenceType { get; private set; }  // "SalesInvoice", "PurchaseInvoice", "Transfer"
    public int ReferenceId { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<InventoryTransactionLine> _lines = new();
    public IReadOnlyCollection<InventoryTransactionLine> Lines => _lines.AsReadOnly();

    public static InventoryTransaction Create(int warehouseId, string referenceType,
        int referenceId, string? notes = null, int createdByUserId = 0) { ... }
}

public class InventoryTransactionLine : BaseEntity
{
    public int TransactionId { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }
    public int ProductUnitId { get; private set; }
    public ProductUnit? ProductUnit { get; private set; }
    public decimal Quantity { get; private set; }     // In base unit (positive = in, negative = out)
    public decimal UnitCost { get; private set; }      // decimal(18,2)
    public string? BatchNo { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
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

### ExecuteTransactionAsync for Account+Entity Services (RULE-546/RULE-547)
Services that create BOTH an Account AND an entity MUST wrap both writes in `ExecuteTransactionAsync`:
```csharp
// CORRECT — CustomerService.CreateAsync wraps account + entity in single transaction:
return await _uow.ExecuteTransactionAsync<Result<CustomerDto>>(async () =>
{
    // Step 1: Auto-create GL Account
    var parentAccount = await _uow.Accounts.GetByCodeAsync("1130", ct);
    var account = Account.Create(...);
    await _uow.Accounts.AddAsync(account, ct);

    // Step 2: Create entity with AccountId
    var customer = Customer.Create(..., account.Id);
    await _uow.Customers.AddAsync(customer, ct);

    // Step 3: Single SaveChanges — atomic
    await _uow.SaveChangesAsync(ct);
    return Result<CustomerDto>.Success(MapToDto(customer, account));
}, ct);

// Services that MUST use this pattern:
// - CustomerService.CreateAsync (Account under "1130 — العملاء")
// - SupplierService.CreateAsync (Account under "1320 — الموردون")
// - CashBoxService.CreateAsync (Account under "1110 — النقدية")
// - BankService.CreateAsync (Account under "1120 — البنوك")
// - SalesService.CreateAsync (multi-table: Invoice + Lines + Stock)
// - ProductService.CreateAsync (3 tables: Products + ProductUnits + ProductPrices)

// WRONG — Creating account and entity separately without transaction:
var account = await CreateAccountAsync(...);  // ❌ Account saved
var customer = await CreateCustomerAsync(account.Id);  // ❌ If this fails, orphaned account
// MUST use ExecuteTransactionAsync wrapping BOTH
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

### Per-Entity Account Routing Pattern (v4.10 — Phase 18 Remediation)

```csharp
// AccountingIntegrationService — Use per-entity accounts instead of fixed system mappings

// Helper methods for per-entity account resolution:
private static int GetCustomerAccountId(SalesInvoice invoice, SystemAccountMappingsDto m)
{
    return invoice.Customer?.AccountId > 0
        ? invoice.Customer.AccountId
        : m.AccountsReceivableAccountId;
}

private static int GetSupplierAccountId(PurchaseInvoice invoice, SystemAccountMappingsDto m)
{
    return invoice.Supplier?.AccountId > 0
        ? invoice.Supplier.AccountId
        : m.AccountsPayableAccountId;
}

// CORRECT — use helpers in all sales/purchase journal entry methods:
var customerAccountId = GetCustomerAccountId(invoice, m);
// then use customerAccountId instead of m.AccountsReceivableAccountId
lines.Add(new JournalEntryLineRequest(customerAccountId, invoice.TotalAmount, 0, "الجزء الآجل"));

// CORRECT — opening entries accept per-entity account parameter:
public async Task<Result<int>> CreateCustomerOpeningEntryAsync(
    int customerId, string customerName, int customerAccountId, decimal openingBalance, ...)
{
    lines.Add(new JournalEntryLineRequest(customerAccountId, openingBalance, 0, "رصيد افتتاحي"));
}

// CORRECT — payment reversals accept per-entity account parameter:
public async Task<Result<int>> ReverseCustomerPaymentEntryAsync(
    int paymentId, decimal amount, string customerName, int customerAccountId, ...)

// WRONG — NEVER use fixed system mapping account IDs:
m.AccountsReceivableAccountId  // ❌ use invoice.Customer?.AccountId instead!
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
- Fixed `AccountsReceivableAccountId`/`AccountsPayableAccountId` in journal entries (use per-entity account routing via `GetCustomerAccountId`/`GetSupplierAccountId` helpers instead)
- Split Cash Sales (1521) and Credit Sales (1522) revenue accounts (use single `1520 — إيرادات المبيعات` instead)
- Crediting `InventoryAssetAccountId` for purchase return reversals (use `PurchaseReturnAccountId` instead)

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

### Features to Fix By Default — Inventory Operations

When you encounter any code related to these areas, apply fixes automatically:

1. `InventoryService.CreateTransactionAsync()` requiring Desktop to provide TransactionNo → ADD auto-generation via `_sequenceService.GetNextIntAsync("InventoryTransaction", ct)` when `<= 0`
2. `InventoryAdjustmentService.PostAsync()` directly setting `WarehouseStock.Quantity` → CHANGE to use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync`
3. `InventoryCountService.PostAsync()` creating one Adjustment per line → CHANGE to create ONE Adjustment per Post with `ReferenceType = "InventoryCount"`
4. `AdjustmentType` validator with range `(1,2)` → CHANGE to `InclusiveBetween(1, 3)`
5. `ReportsController` with `CancellationToken` after optional parameters → MOVE CancellationToken BEFORE optional params
6. Inventory Operations ViewModels NOT implementing `IDisposable` → ADD `IDisposable` with `Cleanup()` in `Dispose()`
7. `InvoiceLineViewModel`/`PurchaseInvoiceLineViewModel` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes → CHANGE to only call calculator when `_lastModifiedField == Total`

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

### IncludeInactive Checkbox (v4.10.5)

For list ViewModels where entities support soft-delete (IsActive property):

**Property in ViewModel:**
```csharp
private bool _includeInactive;
public bool IncludeInactive
{
    get => _includeInactive;
    set
    {
        if (SetProperty(ref _includeInactive, value))
        {
            _ = LoadDataAsync();
        }
    }
}
```

**Add CheckBox in XAML toolbar:**
```xml
<CheckBox Content="عرض غير النشطة" 
          IsChecked="{Binding IncludeInactive}" 
          VerticalAlignment="Center" 
          FontSize="11"
          ToolTip="عرض العناصر غير النشطة"/>
```

**Client-side filtering when API lacks includeInactive parameter:**
After loading data into the ObservableCollection:
```csharp
if (!IncludeInactive)
{
    var toRemove = Items.Where(x => !x.IsActive).ToList();
    foreach (var item in toRemove) Items.Remove(item);
}
```

**When NOT to add IncludeInactive:**
- Status-based entities (invoices with Draft/Posted/Cancelled) — use IncludeCancelled instead
- Log/appended-only entities (audit logs, transactions, notifications) — no soft-delete
- Sessions — use IncludeRevoked instead

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
public record SalesInvoiceLineDto(int Id, int ProductId, string ProductName, ...);
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

### Passwordless User Creation (No Role — DB-driven via UserRole join table)
```csharp
public static User Create(string userName, int? defaultCashBoxId = null,
    int? createdByUserId = null)
{
    if (string.IsNullOrWhiteSpace(userName))
        throw new DomainException("اسم المستخدم مطلوب.");
    return new User
    {
        UserName = userName.Trim(),
        DefaultCashBoxId = defaultCashBoxId,
        MustChangePassword = true,
        PasswordHash = null,
        LoginAttempts = 0,
        IsActive = true,
        IsLocked = false,
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
        IsLocked = false;
        LastLoginAt = DateTime.UtcNow;
    }
    else
    {
        LoginAttempts++;
        if (LoginAttempts >= 5)
            IsLocked = true;
    }
}
```

### AuthService Login — IsLocked + MustChangePassword
```csharp
if (user.IsLocked || !user.IsActive)
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

### PermissionService Transactional Update (byte roleId — DB-driven Role entity)
```csharp
public async Task<Result> UpdateRolePermissionsAsync(byte roleId, List<int> permissionIds, CancellationToken ct)
{
    await _uow.ExecuteTransactionAsync(async () =>
    {
        var existing = await _uow.RolePermissions.GetQueryable()
            .Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        _uow.RolePermissions.RemoveRange(existing);
        foreach (var permId in permissionIds)
            await _uow.RolePermissions.AddAsync(RolePermission.Create(roleId, permId), ct);
    }, ct);
    return Result.Success();
}
```

## Phase 22 — Chart of Accounts Module Patterns (v4.6.9+)

### Account Entity — 13-param Create() with Level
```csharp
public class Account : BaseEntity
{
    // Core properties
    public string AccountCode { get; private set; }         // Numeric string, 4-10 digits
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public AccountType AccountType { get; private set; }    // Enum(byte)
    public int Level { get; private set; }                  // 1-10, CHECK constraint
    public int? ParentAccountId { get; private set; }       // Self-referencing FK
    public Account? ParentAccount { get; private set; }
    public bool IsSystemAccount { get; private set; }       // L1-L2 only
    public string? Description { get; private set; }        // Max 500
    public string? ColorCode { get; private set; }          // Hex, e.g. #2196F3
    public bool AllowTransactions { get; private set; }     // L4+ = true
    public decimal OpeningBalance { get; private set; }     // 18,2
    public string? Notes { get; private set; }

    // Children for tree building
    private readonly List<Account> _children = new();
    public IReadOnlyCollection<Account> Children => _children.AsReadOnly();

    // Protected constructor for EF Core
    protected Account() { }

    // Static factory — 13 parameters, Level is required (5th param)
    public static Account Create(string accountCode, string nameAr, string? nameEn,
        AccountType accountType, int level, int? parentAccountId, bool isSystemAccount,
        string? description, string? colorCode, bool allowTransactions,
        decimal openingBalance, string? notes, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new DomainException("كود الحساب مطلوب");
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");
        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");
        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        return new Account
        {
            AccountCode = accountCode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim(),
            AccountType = accountType,
            Level = level,
            ParentAccountId = parentAccountId,
            IsSystemAccount = isSystemAccount,
            Description = description?.Trim(),
            ColorCode = colorCode?.Trim(),
            AllowTransactions = allowTransactions,
            OpeningBalance = openingBalance,
            Notes = notes?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    // Update — guards system accounts
    public void Update(AccountType accountType, int level, string? description,
        string? colorCode, bool allowTransactions)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظام — الحساب محمي");
        AccountType = accountType;
        Level = level;
        Description = description?.Trim();
        ColorCode = colorCode?.Trim();
        AllowTransactions = allowTransactions;
        UpdateTimestamp();
    }

    // HasChildren — prevents deletion of parent accounts
    public bool HasChildren() => _children.Count > 0;

    // MarkAsDeleted — guards system accounts AND parent accounts
    public override void MarkAsDeleted()
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن حذف حساب نظام — الحساب محمي");
        if (HasChildren())
            throw new DomainException("لا يمكن حذف حساب رئيسي — لديه حسابات فرعية");
        base.MarkAsDeleted();
    }

    // IsDebitNormal — preserved from Phase 18
    public bool IsDebitNormal() => AccountType switch
    {
        AccountType.Asset => true,
        AccountType.Expense => true,
        AccountType.Liability => false,
        AccountType.Equity => false,
        AccountType.Revenue => false,
        _ => true
    };
}
```

### Account Fluent API Configuration — CHECK Constraint + Self-Referencing FK
```csharp
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CHK_Account_Level_Range",
            "[Level] >= 1 AND [Level] <= 10"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.AccountCode).IsUnique();
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.AccountType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Level).IsRequired().HasDefaultValue(4);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ColorCode).HasMaxLength(7);  // #RRGGBB
        builder.Property(x => x.AllowTransactions).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);

        // Self-referencing FK with Restrict (NO Cascade)
        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Global query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
```

### Two-Pass Seeder — Account Hierarchy
```csharp
// First pass: create Level 1-2 accounts (Group + Main)
var cash = Account.Create("1100", "النقدية", "Cash", AccountType.Asset, 2, null,
    true, "النقدية في الصندوق", "#2196F3", false, 0m, null, adminId);
await context.Accounts.AddAsync(cash);
await context.SaveChangesAsync(ct);  // IDs generated here

// Query back IDs after first save
var cashAccount = await context.Accounts.FirstAsync(a => a.AccountCode == "1100", ct);

// Second pass: create Level 3-4 with ParentAccountId
var pettyCash = Account.Create("1101", "الصندوق", "Petty Cash", AccountType.Asset, 3,
    cashAccount.Id, false, "صندوق النقدية الرئيسي", "#2196F3", false, 0m, null, adminId);
await context.Accounts.AddAsync(pettyCash);
await context.SaveChangesAsync(ct);

// Update SystemAccountMappings after all accounts created
var cashId = (await context.Accounts.FirstAsync(a => a.AccountCode == "1100", ct)).Id;
mappings.UpdateCashAccount(cashId);
```

### AccountService — Tree Builder (No N+1)
```csharp
public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct)
{
    var accounts = await _uow.Accounts.GetQueryable()
        .Where(a => a.IsActive)
        .OrderBy(a => a.AccountCode)
        .ToListAsync(ct);

    var tree = accounts.Where(a => a.ParentAccountId == null)
        .Select(a => BuildTreeNode(a, accounts))
        .ToList();

    return Result<List<AccountTreeNodeDto>>.Success(tree);
}

private AccountTreeNodeDto BuildTreeNode(Account account, List<Account> allAccounts)
{
    return new AccountTreeNodeDto
    {
        Id = account.Id,
        AccountCode = account.AccountCode,
        NameAr = account.NameAr,
        NameEn = account.NameEn,
        AccountType = account.AccountType,
        AccountTypeDisplay = account.AccountType.ToString(),
        Level = account.Level,
        LevelDisplay = $"المستوى {account.Level}",
        AllowTransactions = account.AllowTransactions,
        ColorCode = account.ColorCode,
        OpeningBalance = account.OpeningBalance,
        Children = allAccounts.Where(a => a.ParentAccountId == account.Id)
            .Select(a => BuildTreeNode(a, allAccounts))
            .ToList()
    };
}
```

### CreateAsync — Parent/Level/Code Validation
```csharp
public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, int userId, CancellationToken ct)
{
    // Validate parent exists (if provided)
    if (request.ParentAccountId.HasValue)
    {
        var parent = await _uow.Accounts.GetByIdAsync(request.ParentAccountId.Value, ct);
        if (parent == null)
            return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
        if (request.Level <= parent.Level)
            return Result<AccountDto>.Failure("مستوى الحساب يجب أن يكون أعمق من الحساب الأب");
    }

    // Validate code uniqueness
    var existing = await _uow.Accounts.GetQueryable()
        .AnyAsync(a => a.AccountCode == request.AccountCode && a.IsActive, ct);
    if (existing)
        return Result<AccountDto>.Failure("كود الحساب موجود مسبقاً", ErrorCodes.DuplicateEntry);

    var account = Account.Create(/* 13 params */);
    await _uow.Accounts.AddAsync(account, ct);
    await _uow.SaveChangesAsync(ct);
    return Result<AccountDto>.Success(MapToDto(account));
}
```

### Controller — 404 vs 400 + Policy Enforcement
```csharp
[HttpGet("tree")]
[Authorize(Policy = "AllStaff")]
public async Task<IActionResult> GetTree(CancellationToken ct)
{
    var result = await _accountService.GetTreeAsync(ct);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
}

[HttpPost]
[Authorize(Policy = "ManagerAndAbove")]
public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
{
    var userId = GetCurrentUserId();
    var result = await _accountService.CreateAsync(request, userId, ct);
    if (result.IsSuccess)
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    if (result.ErrorCode == ErrorCodes.NotFound)
        return NotFound(new { error = result.Error });
    return BadRequest(new { error = result.Error });
}

[HttpDelete("permanent/{id:int}")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
{
    var result = await _accountService.PermanentDeleteAsync(id, ct);
    if (result.IsSuccess)
        return Ok(new { message = "تم حذف الحساب نهائياً" });
    if (result.ErrorCode == ErrorCodes.NotFound)
        return NotFound(new { error = result.Error });
    return BadRequest(new { error = result.Error });
}
```

### Desktop AccountApiService — Typed HTTP Client
```csharp
public class AccountApiService : IAccountApiService
{
    private readonly HttpClient _http;

    public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/v1/accounts/tree", ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<ApiResponse<List<AccountTreeNodeDto>>>(ct);
                return json?.Success == true
                    ? Result<List<AccountTreeNodeDto>>.Success(json.Data!)
                    : Result<List<AccountTreeNodeDto>>.Failure(json?.Message ?? "فشل تحميل شجرة الحسابات");
            }
            return await HandleResponseAsync<List<AccountTreeNodeDto>>(response);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "فشل الاتصال بالخادم لتحميل شجرة الحسابات");
            return Result<List<AccountTreeNodeDto>>.Failure("تعذر الاتصال بالخادم");
        }
    }
}
```

### Desktop AccountsListViewModel — Dual-Mode Tree/Grid + IDisposable
```csharp
public class AccountsListViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountApiService _accountApiService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private IDisposable? _subscription;

    // Dual-mode toggle
    private bool _isTreeView = true;
    public bool IsTreeView { get => _isTreeView; set => SetProperty(ref _isTreeView, value); }

    public ICommand ToggleViewCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public AccountsListViewModel(IAccountApiService accountApiService,
        IScreenWindowService screenWindowService, IEventBus eventBus,
        IDialogService dialogService)
    {
        _accountApiService = accountApiService;
        _screenWindowService = screenWindowService;
        _eventBus = eventBus;
        _dialogService = dialogService;

        ToggleViewCommand = new RelayCommand(() => IsTreeView = !IsTreeView);
        AddCommand = new RelayCommand(AddAccount);          // NO CanExecute per RULE-059
        EditCommand = new AsyncRelayCommand(EditAccountAsync);  // NO CanExecute
        DeleteCommand = new AsyncRelayCommand(DeleteAccountAsync);
        RefreshCommand = new AsyncRelayCommand(async () => await LoadAccountsAsync());

        _subscription = eventBus.Subscribe<AccountChangedMessage>(OnAccountChanged);
        _ = LoadAccountsAsync();
    }

    public void Dispose() => Cleanup();  // EventBus unsubscription via base class
}
```

### Desktop AccountEditorView — Dual Constructor + INotifyDataErrorInfo
```csharp
public class AccountEditorViewModel : ViewModelBase
{
    // Dual constructor: parameterless (for DI) + parameterized (for testing)
    public AccountEditorViewModel() : this(App.GetService<IAccountApiService>(),
        App.GetService<IDialogService>(), App.GetService<IToastNotificationService>()) { }

    public AccountEditorViewModel(IAccountApiService accountApiService,
        IDialogService dialogService, IToastNotificationService toastService)
    {
        _accountApiService = accountApiService;
        _toastService = toastService;
        SetDialogService(dialogService);  // RULE-227

        SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO CanExecute
    }

    // ValidateAsync follows RULE-229/338
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();  // Clear INotifyDataErrorInfo errors
        if (string.IsNullOrWhiteSpace(AccountCode))
            AddError(nameof(AccountCode), "كود الحساب مطلوب");
        if (string.IsNullOrWhiteSpace(NameAr))
            AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");
        // ... more field validations ...
        return await ValidateAllAsync();  // Shows styled warning dialog automatically
    }
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

### Phase 22 Code Review Fix Patterns (v4.6.9+)

When implementing Account Service methods:

**DeleteAsync Pattern (no double entity fetch):**
```csharp
// CORRECT - fetch once
var account = await _uow.Accounts.GetByIdAsync(id, ct);
if (account == null) return Result.Failure("الحساب غير موجود", ErrorCodes.NotFound);

// Use DB query for children check, NOT account.HasChildren()
var hasChildren = await _uow.Accounts.Query()
    .AnyAsync(a => a.ParentAccountId == id, ct);
if (hasChildren) return Result.Failure("لا يمكن حذف حساب رئيسي", ErrorCodes.HasChildren);

account.MarkAsDeleted();
await _uow.SaveChangesAsync(ct);
return Result.Success();
```

**PermanentDeleteAsync Pattern (no double entity fetch):**
```csharp
var account = await _uow.Accounts.GetByIdAsync(id, ct);
if (account == null) return Result.Failure("الحساب غير موجود", ErrorCodes.NotFound);

try
{
    _uow.Accounts.DeleteRange(new[] { account }); // already-loaded entity
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}
catch (DbUpdateException ex)
{
    Log.Error(ex, "Failed to permanently delete account {Id}", id);
    return Result.Failure("لا يمكن حذف الحساب لأنه مرتبط بعمليات أخرى");
}
```

**Route Constraints - Never use :byte:**
```csharp
// WRONG - causes HTTP 500:
// [HttpGet("by-type/{type:byte}")]

// CORRECT:
[HttpGet("by-type/{type:int:min(1):max(5)}")]
public async Task<IActionResult> GetByType(AccountType type, CancellationToken ct)
```

### Smallint FK Pattern (Lookup Tables)

Lookup tables (Units, Roles, Departments, Currencies, Branches, Taxes, AccountCategories) use **smallint PK**:
```csharp
// EF Core Configuration for lookup tables:
builder.Property(x => x.Id).HasColumnType("smallint").ValueGeneratedOnAdd();
builder.HasKey(x => x.Id);

// Entity — Id stays as int but DB stores smallint:
public class Unit : BaseEntity
{
    public int Id { get; private set; }  // int in C#, smallint in SQL
    public string Name { get; private set; }
}
```

### Bigint PK Pattern (High-Volume Tables)

AuditLog and ImportLog use **bigint PK**:
```csharp
// EF Core Configuration:
builder.Property(x => x.Id).HasColumnType("bigint").ValueGeneratedOnAdd();
builder.HasKey(x => x.Id);

// Entity — Id stays as long in C#:
public class AuditLog
{
    public long Id { get; private set; }  // bigint in SQL
    public int? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public int EntityId { get; private set; }
    public string? OldValues { get; private set; }  // JSON
    public string? NewValues { get; private set; }   // JSON
    public DateTime Timestamp { get; private set; }

    // Indexes: (UserId, Timestamp DESC), (EntityType, EntityId), (Timestamp DESC)
}

public class ImportLog
{
    public long Id { get; private set; }  // bigint
    public string EntityType { get; private set; } = string.Empty;
    public int ImportedCount { get; private set; }
    public int FailedCount { get; private set; }
    public DateTime ImportedAt { get; private set; }
}
```

### Perpetual Inventory Transaction Pattern (65-table schema)

NO `Purchases` account — ALL inventory costs go DIRECTLY to Inventory Asset account:

```csharp
// Purchase Invoice Post → Dr Inventory (not Purchases), Cr Cash/AP
// Journal entry lines:
//   Dr InventoryAssetAccountId (for item costs)
//   Dr VAT Input Account (if taxable)
//   Cr AccountsPayableAccountId or CashAccountId

// Sales Invoice Post → COGS side:
// Dr COGS AccountId (via AverageCost)
// Cr InventoryAssetAccountId

// Purchase Return → Dr Cash/AP, Cr PurchaseReturnAccountId (not Inventory directly)
// Journal entry lines:
//   Dr AccountsPayableAccountId or CashAccountId
//   Cr PurchaseReturnAccountId (contra expense, not Inventory)
```

### Phase 23 — Customers Module Implementation Patterns (65-table schema: Parties + Accounts, No CustomerGroup)

#### Architecture
- **Parties** table stores shared contact data (Name, Phone, Email, Address, TaxNumber)
- **Customers** table: PartyId FK, AccountId FK (mandatory), CategoryId FK, CreditLimit
- **Suppliers** table: PartyId FK, AccountId FK (mandatory), CategoryId FK
- **No CustomerGroup/SupplierType** — payment type is per-invoice (SalesInvoice.PaymentType)
- **Account auto-created** under `"1210 — العملاء"` for customers, `"2100 — حسابات الموردين"` for suppliers

#### Customer Entity Pattern (no CustomerGroupId, no OpeningBalance, no CustomerType)
```csharp
public class Customer : BaseEntity
{
    public int PartyId { get; private set; }          // FK to Parties (shared contact data)
    public Party? Party { get; private set; }
    public int AccountId { get; private set; }        // FK to Account — auto-created by service
    public Account? Account { get; private set; }
    public int? CategoryId { get; private set; }      // FK to Categories
    public Category? Category { get; private set; }
    public decimal CreditLimit { get; private set; }  // decimal(18,2)
    public string? Notes { get; private set; }

    // NO CustomerGroupId, NO CustomerType, NO OpeningBalance, NO CurrentBalance, NO CurrencyId

    public static Customer Create(int partyId, int accountId, int? categoryId = null,
        decimal creditLimit = 0, string? notes = null, int createdByUserId = 0)
    {
        if (partyId <= 0) throw new DomainException("الطرف مطلوب");
        if (accountId <= 0) throw new DomainException("الحساب مطلوب");
        return new Customer
        {
            PartyId = partyId, AccountId = accountId, CategoryId = categoryId,
            CreditLimit = Math.Max(0, creditLimit), Notes = notes?.Trim(),
            IsActive = true, CreatedByUserId = createdByUserId, CreatedAt = DateTime.UtcNow
        };
    }

    // CheckCreditLimit returns bool — never throws
    public bool CheckCreditLimit(decimal additionalAmount)
    {
        if (CreditLimit <= 0) return true;  // No limit = no restriction
        // Balance is on linked Account, not here — caller must check Account balance
        return true;  // Soft warning only, caller decides enforcement
    }
}
```

#### Supplier Entity Pattern (no SupplierType, no OpeningBalance)
```csharp
public class Supplier : BaseEntity
{
    public int PartyId { get; private set; }          // FK to Parties
    public Party? Party { get; private set; }
    public int AccountId { get; private set; }        // FK to Account — auto-created by service
    public Account? Account { get; private set; }
    public int? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public string? Notes { get; private set; }

    // NO SupplierType, NO OpeningBalance, NO CurrentBalance, NO CurrencyId

    public static Supplier Create(int partyId, int accountId, int? categoryId = null,
        string? notes = null, int createdByUserId = 0) { /* similar to Customer.Create */ }

    // CheckCreditLimit returns bool — never throws
    public bool CheckCreditLimit(decimal additionalAmount) => true;
}
```

#### Account Auto-Creation Pattern (Customer/Supplier Service)
```csharp
// CustomerService.CreateAsync:
private async Task<Account> CreateCustomerAccountAsync(CancellationToken ct)
{
    var parentAccount = await _uow.Accounts.GetByCodeAsync("1210", ct);  // العملاء
    var maxCode = await _uow.Accounts.GetMaxChildCodeAsync(parentAccount!.Id, ct);
    var newCode = (int.Parse(maxCode ?? "1210") + 1).ToString();
    var account = Account.Create(newCode, $"عميل {request.Name}", $"Customer {request.Name}",
        AccountType.Asset, 4, parentAccount.Id, false, null, "#2196F3", true, 0m, null, userId);
    await _uow.Accounts.AddAsync(account, ct);
    return account;
}

// SupplierService.CreateAsync:
private async Task<Account> CreateSupplierAccountAsync(CancellationToken ct)
{
    var parentAccount = await _uow.Accounts.GetByCodeAsync("2100", ct);  // حسابات الموردين
    // ... same pattern with AccountType.Liability and "#F44336" color
}
```

#### CustomerService GetAllAsync with Include Pattern
```csharp
var (items, total) = await _uow.Customers.GetPagedAsync(
    predicate, q => q.OrderByDescending(c => c.Id), page, pageSize, ct, includeInactive,
    "Party", "Account", "Category");  // NO "CustomerGroup" — removed in 65-table schema
```

#### CustomerEditorViewModel Pattern (no CustomerGroup dropdown)
```csharp
private async Task LoadLookupDataAsync()
{
    // NO CustomerGroup loading — removed
    // Load categories (optional)
    var categoriesResult = await _categoryService.GetAllAsync();
    // Account is display-only after save — NOT user-selectable
}

// DTO without CustomerGroupId/CustomerType
public record CustomerDto(int Id, int PartyId, string Name, string? Phone, string? Email,
    int AccountId, string? AccountName, decimal CreditLimit, bool IsActive);
```

#### XAML Pitfalls — Critical WPF Patterns
```xml
<!-- CORRECT: ComboBox uses ModernComboBox style -->
<ComboBox ItemsSource="{Binding CategoryOptions}"
          SelectedValue="{Binding CategoryId}"
          Style="{StaticResource ModernComboBox}"
          ToolTip="اختر تصنيف العميل"/>

<!-- WRONG: ModernTextBox has TargetType=TextBox, throws XamlParseException on ComboBox -->
<ComboBox Style="{StaticResource ModernTextBox}"/>  <!-- ❌ CRASHES -->

<!-- CORRECT: Use ItemTemplate (no DisplayMemberPath) -->
<ComboBox ItemsSource="{Binding CategoryOptions}"
          SelectedItem="{Binding SelectedCategory}"
          Style="{StaticResource ModernComboBox}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>

<!-- WRONG: DisplayMemberPath + ItemTemplate together throws InvalidOperationException -->
<ComboBox DisplayMemberPath="Name" ...>     <!-- ❌ Remove this -->
    <ComboBox.ItemTemplate>...              <!-- ❌ CRASHES if both present -->
</ComboBox>
```

#### Phone & Email Validation Pattern
```csharp
// CreateCustomerRequestValidator + UpdateCustomerRequestValidator
RuleFor(x => x.Phone)
    .Matches(@"^05\d{8}$").WithMessage("رقم الهاتف يجب أن يبدأ بـ 05 ويتكون من 10 أرقام")
    .When(x => !string.IsNullOrEmpty(x.Phone));

RuleFor(x => x.Email)
    .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
    .When(x => !string.IsNullOrEmpty(x.Email));
```

#### Report Endpoint Pattern
```csharp
Task<Result<PagedResult<CustomerBalanceReportDto>>> GetCustomerBalanceReportAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
Task<Result<PagedResult<CustomerAgingReportDto>>> GetCustomerAgingReportAsync(int page, int pageSize, CancellationToken ct = default);

// Controller
[HttpGet("by-group/{groupId:int:min(1)}")]
[Authorize(Policy = "AllStaff")]
public async Task<IActionResult> GetByGroup(int groupId, CancellationToken ct) { ... }
```

#### DTO Pattern for Reports
```csharp
public record CustomerBalanceReportDto(int Id, string Name, string? Phone, string? GroupName,
    decimal CurrentBalance, decimal CreditLimit, string BalanceStatus);

public record CustomerAgingReportDto(int Id, string Name, string? Phone,
    decimal CurrentBalance, string AgingBucket, DateTime CalculationDate);
```

## Phase 24 — Accounting Integration Implementation Patterns

Read `AGENTS.md` §2.76 (RULE-371 to RULE-388) before implementing accounting automation.

### AccountingIntegrationService Pattern:
```csharp
public async Task<Result<int>> CreateXxxEntryAsync(...)
{
    var mappings = await _systemAccountService.GetMappingsAsync(ct);
    if (mappings == null) return Result<int>.Failure("لم يتم تهيئة الحسابات النظامية");
    if (mappings.SomeAccountId <= 0)
        return Result<int>.Failure("الحساب النظامي غير مهيأ: [name]");
    
    var request = new CreateJournalEntryRequest(
        TransactionDate: DateTime.UtcNow,
        Description: "...",
        EntryType: JournalEntryType.Xxx,
        ReferenceType: "EntityType",
        ReferenceNumber: refNumber,
        ReferenceId: entityId,
        Lines: new List<JournalEntryLineRequest>
        {
            new(AccountCode: code1, AccountNameAr: name1, Debit: amount, Credit: 0, Description: "..."),
            new(AccountCode: code2, AccountNameAr: name2, Debit: 0, Credit: amount, Description: "..."),
        });
    return await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
}
```

### Service Integration Pattern (inside existing transaction, before CommitAsync):
```csharp
var entryResult = await _accountingService.CreateXxxEntryAsync(..., userId, ct);
if (!entryResult.IsSuccess)
{
    await transaction.RollbackAsync(ct);
    return Result<...>.Failure(entryResult.Error!);
}
```

### Key DO NOTs:
- NEVER hardcode `createdByUserId: 1` — accept userId from controller
- NEVER use `lastId + 1` for InvoiceNo — use `IDocumentSequenceService.GetNextIntAsync()`
- NEVER use `PurchaseCost` for COGS — use `AverageCost ?? PurchaseCost`
- NEVER clamp negative netRevenue to zero — return Result.Failure
- NEVER use string-based ReferenceNumber alone for reverse lookups — use ReferenceId (int FK) first
- NEVER let AccountingIntegrationService own transactions — caller is responsible
```

---

## 📋 Phase Awareness (Phases 23-31 + Purchases/Sales Analysis Gaps)

The system is currently at **v4.10.4 with Phases 18-25 completed. A comprehensive deep review of all 8 modules (Core, Organization, Products, Accounting, Inventory, Sales, Purchases, Infrastructure) was completed ??? 42+ structural mismatches fixed against `docs/database-schema.md`, old database dropped, fresh `InitialCreate_v2` migration generated and applied, build: 0 errors/0 warnings, 1,574 tests passing.**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | ✅ Completed | Multi-currency pricing (ProductPrices — per unit × currency × effective dates), FIFO batches (InventoryBatches), product images, opening stock, Units independent table, ProductUnit with Factor/IsBaseUnit, Perpetual Inventory (no Purchases account) |
| 26 — Warehouses Module | 📝 Planned | Warehouse types, manager, AccountId FK, stock adjustments, issue reasons, physical count V2 |
| 27 — Purchases Module | ✅ Completed | Multi-currency, landed cost via OtherCharges (AllocateAdditionalCharges), Purchase Orders, standalone returns, attachments |
| 28 — Sales Module | ✅ Completed | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement, price enforcement (PreventBelowRetailPrice, AllowBelowCostSale), DeliveryChargesRevenue account, flexible input |
| 29 — Receipts & Payments | 📝 Planned | Multi-invoice distribution, Cheques, PaymentAllocation, CashBox.AccountId, DailyClosure |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |

### Key Architecture Rules for Subagents

When implementing or reviewing code, ALWAYS enforce these rules:

1. **Multi-Currency First**: All pricing MUST support multi-currency via ProductPrices table — NEVER store single-currency prices on Product entity
2. **FIFO/FEFO Batches**: Inventory MUST use InventoryBatches for cost allocation — NEVER use weighted-average only
3. **Landed Cost**: Purchase costs MUST include AdditionalCharge distribution — NEVER record purchase cost without transport/customs allocation
4. **Auto Journal Entries**: Every money-affecting operation MUST create journal entries via AccountingIntegrationService — NEVER leave the general ledger out of sync
5. **Chart of Accounts Links**: CashBox, Warehouse, Customer, Supplier MUST link to Account via AccountId FK — NEVER operate without COA integration
6. **Payment Allocation**: Payments MUST use PaymentAllocation for multi-invoice settlement — NEVER leave partial payments untracked
7. **Report Excellence**: ALL reports MUST support Excel export via ClosedXML — NEVER limit to on-screen display only
8. **Passwordless Users**: User.Create() NEVER accepts a password — MustChangePassword=true is the default
9. **ReferenceId over ReferenceNumber**: Journal entry lookups use int FK (ReferenceId), not string matching
10. **AvgCost for COGS**: COGS uses ProductUnit.AverageCost (weighted average), never PurchaseCost

### 💡 Bug Prevention Checklist

When writing or reviewing code in ANY layer, check these:
- [ ] Does the code handle multi-currency correctly? (CurrencyId + ExchangeRate on all financial entities)
- [ ] Are all prices stored per ProductUnit (not per Product)?
- [ ] Does costing use the configured CostingMethod from SystemSettings?
- [ ] Are all FK relationships `DeleteBehavior.Restrict`?
- [ ] Does the service return `Result<T>` (not throw exceptions)?
- [ ] Is the controller free of business logic (delegates to service)?
- [ ] Do all ViewModels use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] Are all buttons ALWAYS enabled (no CanExecute predicates)?
- [ ] Does the validation use `INotifyDataErrorInfo` (not `HasXxxError` booleans)?
- [ ] Does every editor call `ValidateAllAsync()` on save?
- [ ] Is the connection string DPAPI-encrypted or from env var?
- [ ] Are Arabic messages properly UTF-8 encoded?
- [ ] Does the list display newest-first (OrderByDescending)?
- [ ] Are EventBus subscriptions disposed in `Cleanup()`?

### CashBox Entity Pattern (v4.9 — No Balance Fields)

```csharp
// CashBox — lightweight register entity, NO balance fields
public class CashBox : BaseEntity
{
    public string Name { get; private set; }
    public int AccountId { get; private set; }          // FK to Account — balance lives here
    public Account? Account { get; private set; }
    public int? CategoryId { get; private set; }         // FK to Categories
    public Category? Category { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public int CurrencyId { get; private set; }
    public ICollection<CashTransaction> Transactions { get; private set; }

    // NO OpeningBalance, NO CurrentBalance, NO Deposit(), NO Withdraw()

    public static CashBox Create(string name, int accountId, int currencyId,
        int? categoryId = null, string? phoneNumber = null,
        string? taxNumber = null, string? address = null, int createdByUserId = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الصندوق مطلوب");
        return new CashBox
        {
            Name = name.Trim(), AccountId = accountId, CurrencyId = currencyId,
            CategoryId = categoryId, PhoneNumber = phoneNumber, TaxNumber = taxNumber,
            Address = address, IsActive = true,
            CreatedByUserId = createdByUserId, CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAccountId(int accountId) { AccountId = accountId; UpdateTimestamp(); }
}

// CashTransaction — uses RunningBalance instead of BalanceBefore/BalanceAfter
public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RunningBalance { get; private set; }   // Cumulative sum
    public string? Description { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? ReferenceType { get; private set; }

    public static CashTransaction Create(int cashBoxId, CashTransactionType type,
        decimal amount, decimal runningBalance, string? description = null,
        int? referenceId = null, string? referenceType = null, int createdByUserId = 0)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من صفر");
        return new CashTransaction
        {
            CashBoxId = cashBoxId, Type = type, Amount = amount,
            RunningBalance = runningBalance, Description = description,
            ReferenceId = referenceId, ReferenceType = referenceType,
            CreatedByUserId = createdByUserId, CreatedAt = DateTime.UtcNow
        };
    }
}
```

### CashBox Service Pattern — Auto-Account Creation
When `AccountId` is null in `CashBoxService.CreateAsync()`, auto-create a Level-4 detail account under parent `"1110 — النقدية"`:
```csharp
var parentAccount = await _uow.Accounts.GetByCodeAsync("1110", ct);
var maxCode = await _uow.Accounts.GetMaxChildCodeAsync(parentAccount.Id, ct);
var newCode = (int.Parse(maxCode ?? "1110") + 1).ToString();
var account = Account.Create(newCode, $"صندوق {box.Name}", $"Cash Box {box.Name}",
    AccountType.Asset, 4, parentAccount!.Id, false);
await _uow.Accounts.AddAsync(account, ct);
box.SetAccountId(account.Id);
```

CashTransfer MUST NOT validate balance on client side — server validates via Account:
```csharp
// CORRECT — NO client-side balance check
private async Task TransferAsync() { /* validate fields only, not balance */ }
// WRONG — if (sourceBox.CurrentBalance < Amount) return; // ❌ REMOVED
```

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. CashBox with OpeningBalance/CurrentBalance → REMOVE both fields, add AccountId FK
2. CashTransaction with BalanceBefore/BalanceAfter → REPLACE with RunningBalance
3. CashTransaction.Create() internal → CHANGE to public
4. Deposit()/Withdraw() methods on CashBox → REMOVE (service creates CashTransaction directly)
5. Client-side balance validation in CashTransfer → REMOVE (server validates via Account)
6. Missing `AccountId` FK on CashBox → Add it and link to default cash account under "1110 — النقدية"
7. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
8. Missing `PartyId` FK on Customer/Supplier → Add it and create Party record (shared contact data)
9. Missing `CurrencyId` on financial entities → Add multi-currency support (NOT on Customer/Supplier)
10. Missing `ProductPrices` → Replace SalePrice/RetailPrice on Product with per-unit pricing table
11. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking via InventoryBatches
12. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
13. Missing journal entry on cash operations → Call AccountingIntegrationService
14. Missing Excel export on report → Add ClosedXML worksheet generation
15. COGS using PurchaseCost → Change to AverageCost from ProductUnit
16. Payment without allocation → Add PaymentAllocation tracking
17. Missing reversal entries on payment update/delete → Add reversal journal entries
18. Using fixed AR/AP account IDs instead of per-entity customer/supplier accounts → Use `GetCustomerAccountId`/`GetSupplierAccountId` helpers
19. Missing `PurchaseReturnAccountId` from SystemAccountMappings → Add it with FK to Accounts
20. Using split 1521/1522 revenue accounts → Merge into single `1520 — إيرادات المبيعات`
21. Crediting Inventory for purchase returns → Credit `PurchaseReturnAccountId` instead
22. Old `StockTransfer`/`StockTransferItem` → Replace with `WarehouseTransfer`/`WarehouseTransferLine`
23. Old `InventoryMovement` → Replace with `InventoryTransaction`/`InventoryTransactionLine`
24. CustomerGroup/SupplierType references in V1 → Remove (deferred to V2)
25. OpeningBalance/CurrentBalance on Customer/Supplier/CashBox → Remove (balance on linked Account)
26. PurchaseInvoice without `OtherCharges` → ADD `OtherCharges` property (decimal) with guard `otherCharges < 0`, update `RecalculateTotals()` to include `+ OtherCharges`, update EF config with `.HasPrecision(18, 2)`, update DTOs/Requests/Desktop VM/XAML, and update `AccountingIntegrationService.CreatePurchasePostEntryAsync()` to use `SubTotal - Discount + OtherCharges`.
27. Sales without price enforcement → ADD `IProductPriceService` to `SalesService`, enforce `PreventBelowRetailPrice` and `AllowBelowCostSale` settings in `PostAsync()`, fix Desktop VM `GetDefaultPrice()` to use actual price lookup, fix `CostInBaseCurrency` to load from invoice DTO.
28. DeliveryCharges NOT in separate account → ADD `SystemAccountKey.DeliveryChargesRevenue = 21`, seed account `1533 — إيرادات التوصيل` under parent `1530 — إيرادات أخرى`, update `CreateSalesPostEntryAsync()` to credit DeliveryChargesRevenue separately from SalesRevenue, mirror in `ReverseSalesPostEntryAsync()`.
29. Purchase Return blocking standalone mode → FIX `Validate()` to allow `PurchaseInvoiceId = null` when supplier selected and items entered, add `SelectedSupplierId`/`IsLinkedToInvoice` properties, fix hardcoded `ProductUnitId: 1` to use actual ProductUnitId, add `CreatePurchaseReturnEntryAsync()`/`ReversePurchaseReturnEntryAsync()` in AccountingIntegrationService, fix `PostedAt`/`CancelledAt` in `Post()`/`Cancel()`.
30. Fixed-price input (no flexible input) → CREATE `FlexibleInputCalculator` helper class with `CalculationField` enum (Quantity/Price/Total) and `Calculate()`, add `LineTotalInput`/`_lastModifiedField`/`_isRecalculating` to line ViewModels, make LineTotal column editable (not `IsReadOnly`), implement `RecalculateFromFlexibleInput()`.
31. `AllowBelowCostSale` blocking instead of warning → CHANGE seed default from `"false"` to `"true"` in `DbSeeder.cs`. CHANGE `SalesService.PostAsync()` from `return Result.Failure(...)` to `_logger.LogWarning(...)` + continue (don't block). Per analysis: "ولا نمنع البيع" — below-cost sales must warn but never block. This applies to BOTH `CreateAsync()` and `PostAsync()` methods.
32. Missing deep review of analysis documents → Before implementing new Sales/Purchases features, always check ALL 13 analysis documents in `docs/all new Anylysis for update system features/` to ensure no duplicated work. The file `Sales and Purchases new details.md` may be 0 bytes (empty) — check all other `.md` files in the directory for actual content.
33. `Account.Create()` missing `allowTransactions: true` for Level 4+ accounts → ADD `allowTransactions: true` to ALL `Account.Create()` calls where `level >= 4`. The default is `false` and `Account.Create()` throws `DomainException("الحساب التفصيلي يجب أن يسمح بالحركات")` when Level >= 4 and `allowTransactions` is false. Check BankService, CashBoxService, CustomerService, SupplierService, EmployeeService, PartyService — ALL must pass `allowTransactions: true`.
34. Service interface without implementation → CHECK that every registered service interface has a concrete implementation class. Missing implementations cause `InvalidOperationException` at DI resolution. Currently affected: `CashBoxReportService` was missing (now fixed). Search for all `services.AddScoped<I, T>` or `services.AddTransient<I, T>` and verify `T` exists.
35. Report API endpoints missing from controller → CHECK that Desktop ViewModel API calls match actual controller endpoints. Search `ReportApiService.cs` for all `Get*Async()` calls with URLs, then verify each URL has a matching route in the API controller. Common mismatches: `detailed-stock-ledger`, `reports/returns`, `reports/aging` (use `customers/reports/aging` for customer-specific vs unified for both).
36. Payment update without journal entry reversal → ADD reversal logic to `SupplierPaymentService.UpdateAsync()` and `CustomerReceiptService.UpdateAsync()` for posted payments. When amount changes: (1) reverse original journal entry, (2) create new journal entry for updated amount. Wrap in `ExecuteTransactionAsync()`. Use per-entity `Supplier.Party.AccountId`/`Customer.Party.AccountId` routing.
37. Standalone sales return without journal entries → ADD `CreateSalesReturnEntryAsync()` in `AccountingIntegrationService` that Dr `SalesReturnsAccount` / Cr `CustomerAccount` (for return amount) and Dr `InventoryAccount` / Cr `COGSAccount` (for returned cost). Use per-entity `Customer.Party.AccountId` with fallback to `AccountsReceivable`.
38. Report service returning hardcoded failure stub → NEVER return stub failures from report services. If the query is complex, implement actual logic using available data entities (ReceiptVoucher, PaymentVoucher, etc.). Current example: `CashFlowReportService` was returning `"تقرير التدفق النقدي قيد إعادة البناء"` instead of computing from ReceiptVoucher/PaymentVoucher data.
39. `SalesReturn.Post()` without `PostedAt` / `SalesReturn.Cancel()` without `CancelledAt` → ADD `PostedAt = DateTime.UtcNow` after status change in `Post()`, ADD `CancelledAt = DateTime.UtcNow` after status change in `Cancel()`. Same pattern as `PurchaseReturn.Post()`/`Cancel()`.
40. `SalesReturnService` not creating journal entries → ADD `IAccountingIntegrationService` to DI constructor, call `CreateSalesReturnEntryAsync()` on Post (after stock increase), call `ReverseSalesReturnEntryAsync()` on Cancel (before stock reversal). Use per-entity `Customer.AccountId` for Dr/Cr routing. Add eager loading of `Customer` navigation property in both queries.
41. `ProductUnitId` hardcoded to `1` in ViewModel line creation → REPLACE all `ProductUnitId = 1` occurrences with `product.DefaultPurchaseUnitId` (purchase/inventory VMs) or `product.DefaultSalesUnitId` (sales VMs). Fallback `0` when no default is configured (service auto-determines rather than breaking with wrong unit).
42. `AllocateAdditionalCharges()` inline in `PurchaseService.PostAsync()` → EXTRACT to a standalone `AdditionalChargeAllocator.Allocate()` static method in `Helpers/AdditionalChargeAllocator.cs`. Takes `AllocationLine[]` (Index, LineTotal, Quantity, UnitCost), returns `Dictionary<int, decimal>` keyed by line index (not ProductId — prevents duplicate-product collision).
43. Wrong base class (SalesReturn/PurchaseReturn extending `AuditableEntity` instead of `DocumentEntity`) ??? CHANGE to `DocumentEntity`. DocumentEntity = has Status (Draft/Posted/Cancelled) lifecycle + PostedAt/CancelledAt timestamps. SalesReturn.Post()/.Cancel() was missing PostedAt/CancelledAt.
44. Wrong base class (UserSession extending `AuditableEntity` instead of `ActivatableEntity`) ??? CHANGE to `ActivatableEntity` (session revocation via IsActive).
45. Missing `[IsActive]` filter removal on DocumentEntity indexes ??? REMOVE `.HasFilter("[IsActive] = 1")` from ALL DocumentEntity index configs (ReceiptVoucher, PaymentVoucher, SalesInvoice, PurchaseInvoice). DocumentEntity has NO IsActive column ??? filter causes SQL runtime error. CRITICAL: this is a runtime crash, not a warning.
46. Missing BranchId FK on Warehouse/CashBox ??? ADD `.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict)` to configuration.
47. `HasConversion<int>()` on enum properties ??? CHANGE to `HasConversion<byte>()` + `.HasColumnType("tinyint")` for ALL enum columns (InvoiceStatus, PaymentType, AccountType, JournalEntryType, etc.). EF Core defaults to `HasConversion<int>()` which wastes 3 bytes per column and uses `int` SQL type instead of `tinyint`.
48. Missing `.HasColumnType("date")` on date columns ??? ADD `.HasColumnType("date")` to ALL date properties: InvoiceDate, EntryDate, BirthDate, HireDate, EffectiveFrom, EffectiveTo, ExpiryDate, ReturnDate, VoucherDate, PostedAt, CancelledAt. EF Core defaults to `datetime2(7)` which wastes 7 bytes per row.
49. `SortOrder` as `int` ??? CHANGE to `short` with `.HasColumnType("smallint")` (e.g., JournalEntryLine.SortOrder and other sort-order columns).
50. Bare `.WithMany()` without navigation property lambda ??? CHANGE to `.WithMany(p => p.Applications)` or appropriate nav property. Bare `.WithMany()` creates EF shadow FK column named `EntityNameId` (e.g., `SupplierPaymentId1`). CRITICAL for SupplierPaymentApplication, Product collections (`_prices`/`_units`).
51. Description/Notes `MaxLength` mismatch ??? Align with schema: JournalEntryLine.Description=300, Account.Description=500, Employee.Notes=1000, Permission.DisplayName=150.
52. AuditLog index direction incorrect ??? FIX from `(UserId, CreatedAt)` (ASC/ASC) to `(UserId ASC, CreatedAt DESC)`. Add missing `SystemLog(Level, CreatedAt DESC)` composite index.
53. `User.PasswordHash` as nullable (string?) ??? Schema says `nvarchar(256) NOT NULL`. CHANGE to `string.Empty` default. Update all tests that expect null.
54. `BranchId` with `.HasDefaultValue((short)0)` ??? CHANGE to `.IsRequired(false)` when FK is optional (SystemAccountMapping). Default 0 breaks FK constraint when no Branch record exists with Id=0.
55. `SalesReturnLines`/`PurchaseReturnLines` referencing `ProductId` ??? MUST reference `SalesInvoiceLineId`/`PurchaseInvoiceLineId` per schema. Return lines link to original invoice line, not product directly.
56. `SystemSetting.DataType` as string ??? CHANGE to `SettingType` (byte enum). Remove `Note` and `UpdatedBy` fields. Update DbSeeder.
57. `Notification.Category` as string ??? CHANGE to `NotificationCategory` (byte enum). Remove duplicate Category column.
58. `Customer.CategoryId`/`Supplier.CategoryId` (`int?`) without FK ??? Document as loose property (int does not match AccountCategory's smallint PK). Consider `HasColumnType("int")` override.
59. Entity configurations lacking explicit column type for tinyint enums ??? ADD `.HasColumnType("tinyint")` to ALL enum properties. EF Core defaults to `int` SQL type.
60. Missing HasQueryFilter(IsActive) on ActivatableEntity configurations ??? VERIFY every ActivatableEntity has `.HasQueryFilter(x => x.IsActive)` in its EF configuration. Missing this filter means soft-deleted records appear in queries.

## Auto-Fix Rules (Phase 21 — Users & Permissions)

When implementing code in the Users & Permissions domain, ALWAYS apply these fixes automatically:

1. **UserService.CreateAsync**: MUST check `request.Password` if provided (hash with BCrypt work factor 12), fall back to "12345678" if null. NEVER hardcode only "12345678".
2. **No UserRole enum**: Use DB-driven `Role` entity. `PermissionService.GetRolePermissionsAsync()` must query `_uow.Roles.ToListAsync()` — NEVER use `Enum.GetValues<UserRole>()`.
3. **No UserStatus enum**: User uses `IsActive` (bool, for soft-delete) + `IsLocked` (bool, for lockout). NEVER add `Status` enum.
4. **No FullName/Phone/Email/AvatarPath on User**: Profile fields live on linked Employee/Party entity.
5. **LoginAttempts**: Use `short` type (smallint in DB). Track with `RecordLoginAttempt()`.
6. **AuditLog**: Use `long Id` (bigint). Split Details into `OldValues`/`NewValues`/`ChangedColumns` (not single Details field).
7. **UserSession**: Use `AuditableEntity` (not `ActivatableEntity`). Use `SessionToken` + `IsRevoked`.
8. **RolePermission**: Use `ExecuteTransactionAsync()` for atomic update operations.
9. **Desktop screens exist**: PasswordChangeView, AuditLogListView, PermissionManagementView — all already implemented. Don't create duplicates.
10. **45 permissions minimum**: DbSeeder must seed at least 45 permissions across 12+ categories with 9-role assignments.

## Auto-Fix Rules (Phase 27 — Transaction Atomicity & Audit Trail)

When implementing or reviewing code, ALWAYS apply these fixes automatically:

1. **ExpenseService**: MUST inject `IAccountingIntegrationService` + `IDocumentSequenceService`. PostAsync creates `Dr ExpenseAccount / Cr CashBox.Account`. CancelAsync reverses. Sequence uses `GetNextIntAsync("Expense", ct)` — NEVER `ToListIgnoreFiltersAsync().Max() + 1`.
2. **All 22 transaction operations MUST use `ExecuteTransactionAsync`**: SalesService.Post/Cancel, PurchaseService.Post/Cancel, SalesReturnService.Post/Cancel, PurchaseReturnService.Post/Cancel, CustomerReceiptService.Create/Post/Cancel/Delete, SupplierPaymentService.Create/Post/Cancel/Delete, ExpenseService.Create/Post/Cancel, InventoryAdjustmentService.Post/Cancel, InventoryCountService.Post/Cancel, WarehouseTransferService.Create/Post/Cancel, ReceiptVoucherService.Post/Cancel, PaymentVoucherService.Post/Cancel, InventoryService.CreateTransfer/PostTransfer/CancelTransfer.
3. **InventoryTransaction audit trail**: EVERY stock-affecting operation MUST create `InventoryTransaction.Create()` + `AddLine()` per product. Types: Sale=3, Purchase=1, SaleReturn=4, PurchaseReturn=2, Adjustment=8, Count=7, TransferOut=5, TransferIn=6. Created INSIDE service wrappers (NOT inside IncreaseStockAsync/DecreaseStockAsync).
4. **DeleteAsync/CancelAsync MUST call `SaveChangesAsync`**: Missing SaveChangesAsync = silent rollback. Check CustomerReceiptService, SupplierPaymentService, and any soft-delete operation.
5. **No BeginTransactionAsync directly**: ALL multi-save operations MUST use `ExecuteTransactionAsync` (RULE-275). The execution strategy does not support user-initiated transactions.
6. **FIFO batch restoration NOT implemented yet**: Stock restored via `IncreaseStockAsync()` but batch `QuantityRemaining` is not decremented back. This is a known gap — do NOT attempt to implement without storing original batch allocations.
7. **AccountingIntegrationService methods MUST return Result<int>**: NEVER throw. NEVER own transactions — caller wraps in ExecuteTransactionAsync.
8. **CreateExpenseEntryAsync**: Dr ExpenseAccount / Cr CashBox.Account. Load CashBox.AccountId via `_uow.CashBoxes.GetByIdAsync()`. Return `Result<int>` with journal entry ID.