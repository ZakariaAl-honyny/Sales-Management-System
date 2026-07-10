# AGENTS.md — Sales Management System (v4.10.7 — Transaction Atomicity & Inventory Audit Trail: All 22 Operations Audited, ExpenseService Journal Entries, SaveChangesAsync Bug Fixes)
# READ THIS FILE FIRST — BEFORE WRITING ANY CODE
# Platform: .NET 10 LTS | Clean Architecture
# WPF Desktop + ASP.NET Core 10 API + SQL Server

---

<!-- SPECKIT START -->
**Active Feature Plan**: [specs/028-products-module-complete/plan.md](specs/028-products-module-complete/plan.md)
<!-- SPECKIT END -->

## 1. Project Overview

A local desktop sales management system for small retail shops.

**Solution Structure (6 Projects):**

```text
SalesSystem/
├── SalesSystem.Contracts/       ← DTOs + Requests + Responses + Result<T>
├── SalesSystem.Domain/          ← Entities + Business Rules + Exceptions
├── SalesSystem.Application/     ← Services + Interfaces + Use Cases
├── SalesSystem.Infrastructure/  ← EF Core + DbContext + Repositories
├── SalesSystem.Api/             ← Controllers + FluentValidation + Middleware
└── SalesSystem.DesktopPWF/     ← WPF UI + MVVM + EventBus
```

**Data Flow (NEVER break this chain):**

```text
Desktop → (HttpClient) → Api → Application → Infrastructure → SQL Server
                                     ↓
                              Domain (ZERO dependencies)
```

---

## 2. CONSTITUTION — Non-Negotiable Rules

**These rules are LAW. They override ALL other instructions.**

### 2.98 Products Module — TaxId, Barcode, Units, ExecuteTransactionAsync (v4.10.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-543 | `Product` entity MUST NOT have `TaxId` — Tax is invoice-level only (`SalesInvoices.TaxId`, `PurchaseInvoices.TaxId`). Tax entity determines tax rate applied at sale/purchase time, NOT on the product catalog. |
| RULE-544 | Product creation MUST be atomic across 3 tables: `Products` + `ProductUnits` + `ProductPrices` — wrapped in `_uow.ExecuteTransactionAsync<Result<ProductDto>>()`. Prevents orphaned ProductUnit/Price records on partial failure. |
| RULE-545 | Opening stock is a SEPARATE inventory transaction (via `InventoryAdjustments` Type=Opening or `InventoryBatches`) — NEVER stored on `Product` entity fields like `OpeningQuantity`/`OpeningUnitCost`. |
| RULE-546 | Services that create BOTH an Account AND an entity (Customer, Supplier, CashBox, Bank) MUST wrap both writes in `_uow.ExecuteTransactionAsync<Result<T>>()` — prevents orphaned accounts when entity creation fails. |
| RULE-547 | `CustomerService.CreateAsync`, `SupplierService.CreateAsync`, `CashBoxService.CreateAsync`, and `BankService.CreateAsync` MUST use `ExecuteTransactionAsync` wrapping account creation + entity creation + SaveChanges in a single transaction. |

### 2.1 Money and Quantity Types

| RULE | WHAT TO DO | WHAT NOT TO DO |
|------|-----------|----------------|
| RULE-001 | Use `decimal(18,2)` for ALL money | NEVER use `float`, `double`, `real`, or SQL `money` |
| RULE-002 | Use `decimal(18,3)` for ALL quantities | NEVER use `int` for quantities |
| RULE-009 | Use `decimal` in SQL Server | NEVER use `float`, `real`, or `money` in SQL |

**Example — CORRECT:**
```csharp
public decimal SalePrice { get; private set; }     // decimal = CORRECT
public decimal Quantity { get; private set; }       // decimal = CORRECT
decimal lineTotal = quantity * unitPrice;           // decimal math = CORRECT
```

**Example — WRONG (NEVER DO THIS):**
```csharp
public float SalePrice { get; private set; }       // float = WRONG
public double Quantity { get; private set; }        // double = WRONG
public int Quantity { get; private set; }           // int = WRONG
```

### 2.2 Financial Formulas (Compute in Domain ONLY)

```csharp
// Sales LineTotal — compute inside SalesInvoiceLine entity
LineTotal = (Quantity * UnitPrice) - DiscountAmount;

// Purchase LineTotal — compute inside PurchaseInvoiceLine entity
LineTotal = (Quantity * UnitCost) - DiscountAmount;

// Invoice totals — compute inside Invoice entity
SubTotal    = Items.Sum(i => i.LineTotal);           // decimal LINQ
TotalAmount = SubTotal - InvoiceDiscount + TaxAmount;
DueAmount   = TotalAmount - PaidAmount;              // ALWAYS computed

// CRITICAL CONSTRAINT:
if (PaidAmount > TotalAmount)
    throw new DomainException("المبلغ المدفوع أكبر من الإجمالي");
```

**NEVER compute these in: UI, Controller, or JavaScript.**

### 2.3 Database Transactions

| RULE | DIRECTIVE |
|------|-----------|
| RULE-003 | ALL financial operations inside `BeginTransactionAsync` |
| RULE-005 | Stock deducted ONLY AFTER invoice saved and has ID |

**Step-by-step transaction pattern:**
```csharp
// Step 1: Validate BEFORE transaction
foreach (var item in request.Items)
{
    var stock = await _uow.WarehouseStocks.GetAsync(...);
    if (stock.Quantity < item.Quantity)
        return Result<T>.Failure("المخزون غير كافٍ");
}

// Step 2: Open transaction
await using var transaction = await _uow.BeginTransactionAsync(ct);
try
{
    // Step 3: Save invoice
    await _uow.SalesInvoices.AddAsync(invoice, ct);
    await _uow.SaveChangesAsync(ct);

    // Step 4: Deduct stock (AFTER save — invoice now has ID)
    foreach (var item in invoice.Items)
    {
        await _inventoryService.DecreaseStockAsync(...);
    }

    // Step 5: Update balance
    if (invoice.DueAmount > 0)
        customer.IncreaseBalance(invoice.DueAmount);

    // Step 6: Commit
    await transaction.CommitAsync(ct);
    return Result<T>.Success(dto);
}
catch (Exception ex)
{
    // Step 7: Rollback on ANY failure
    await transaction.RollbackAsync(ct);
    return Result<T>.Failure("حدث خطأ");
}
```

### 2.4 Invoice Lifecycle

```text
Draft (1) → Posted (2) → Cancelled (3)

ALLOWED:
  Draft    → Posted      ✅ (stock + balance change happens HERE)
  Draft    → Cancelled   ✅ (no stock/balance impact)
  Posted   → Cancelled   ✅ (MUST reverse stock + balance)

FORBIDDEN:
  Posted   → Draft       ❌ NEVER
  Cancelled → anything   ❌ NEVER (terminal state)
  Editing a Posted invoice ❌ NEVER (cancel + create new instead)
```

| RULE | DIRECTIVE |
|------|-----------|
| RULE-004 | NO hard delete for invoices — use `Status = Cancelled` |
| RULE-019 | Invoice states: Draft=1, Posted=2, Cancelled=3 |
| RULE-020 | Stock/balance affected ONLY when Status = Posted |
| RULE-021 | NO editing Posted invoices — cancel + create new |
| RULE-018 | Cancellation MUST reverse ALL stock and balance |

### 2.5 Result Pattern

| RULE | DIRECTIVE |
|------|-----------|
| RULE-006 | ALL service methods return `Result<T>` or `Result` |
| RULE-025 | Controllers translate Result to HTTP status codes |

**CORRECT pattern:**
```csharp
// Service — returns Result<T>
public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
{
    var product = await _uow.Products.GetByIdAsync(id, ct);
    if (product == null)
        return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);
    return Result<ProductDto>.Success(MapToDto(product));
}

// Controller — translates to HTTP
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id, CancellationToken ct)
{
    var result = await _service.GetByIdAsync(id, ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
}
```

**WRONG pattern (NEVER DO THIS):**
```csharp
// WRONG — throwing exceptions from service
public async Task<ProductDto> GetByIdAsync(int id)
{
    var product = await _repo.GetByIdAsync(id);
    if (product == null) throw new Exception("Not found"); // ❌ WRONG
    return MapToDto(product);
}
```

### 2.6 Architecture Boundaries

| RULE | DO THIS | DON'T DO THIS |
|------|---------|---------------|
| RULE-007 | Desktop calls API via `HttpClient` | Desktop NEVER connects to DB |
| RULE-022 | Controllers delegate to Services | Controllers NEVER have business logic |
| RULE-023 | Domain has ZERO NuGet packages | Domain NEVER references EF Core |
| RULE-024 | Use `IUnitOfWork` for multi-table ops | NEVER use raw DbContext in Services |

### 2.7 Stock Integrity

| RULE | DIRECTIVE |
|------|-----------|
| RULE-010 | `WarehouseStocks.Quantity` has `CHECK (Quantity >= 0)` in DB |
| RULE-027 | Validate stock BEFORE opening transaction |
| RULE-028 | Record EVERY stock change in `InventoryMovements` |
| RULE-029 | InventoryMovement stores: ProductId, WarehouseId, MovementType, QuantityChange, QuantityBefore, QuantityAfter, ReferenceType, ReferenceId |

### 2.8 Balance Direction

```text
Customer's Account balance > 0 = Customer owes US money
Customer's Account balance < 0 = We owe the customer
Supplier's Account balance > 0 = We owe the supplier
Supplier's Account balance < 0 = Supplier owes US
```

### 2.9 Audit Trail

| RULE | DIRECTIVE |
|------|-----------|
| RULE-016 | ALL entities have `CreatedByUserId int NULL FK` and `CreatedAt datetime2` |
| RULE-026 | Invoice tables use `CreatedByUserId int FK` referencing Users table |

**CRITICAL:** Users table MUST use soft delete only (`IsActive = false`).
NEVER hard-delete a User — invoices reference them via FK.

### 2.10 Data and Text

| RULE | DIRECTIVE |
|------|-----------|
| RULE-008 | ALL text columns use `nvarchar` (Arabic + English) |

### 2.11 Thread Safety

| RULE | DIRECTIVE |
|------|-----------|
| RULE-011 | `DocumentSequenceService` uses `SemaphoreSlim` lock |

### 2.12 EventBus (Desktop)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-012 | Unsubscribe in `Dispose()` or View Unload |
| RULE-013 | Marshal handlers to UI thread via `Application.Current.Dispatcher` |
| RULE-034 | Messages carry entity ID only — NO data payloads |

**CORRECT EventBus usage in a ViewModel:**
```csharp
public class ProductsListViewModel : ViewModelBase, IDisposable
{
    private IDisposable? _subscription;

    public ProductsListViewModel(IEventBus eventBus)
    {
        _subscription = eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
        _ = LoadDataAsync();
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        // Reload from API — do NOT use data from the message
        _ = LoadDataAsync();
    }

    public void Dispose()
    {
        _subscription?.Dispose(); // MUST unsubscribe
    }
}
```

### 2.13 Validation (4 Layers)

| Layer | What | Example |
|-------|------|---------|
| Domain | Business rules in Entity | `PaidAmount <= TotalAmount` |
| Application | Pre-conditions in Service | Stock availability check |
| API | FluentValidation on Requests | `Quantity > 0`, `Name` not empty |
| Database | CHECK constraints | `Quantity >= 0` |

### 2.14 Security

| RULE | DIRECTIVE |
|------|-----------|
| RULE-038 | ALL endpoints need `[Authorize]` (except `/api/auth/login`) |
| RULE-039 | Passwords: BCrypt hash, work factor = 12 |
| RULE-040 | Connection strings: environment variables only |

### 2.15 Logging

| RULE | DIRECTIVE |
|------|-----------|
| RULE-035 | Use `Serilog` — NEVER `Console.WriteLine` |
| RULE-036 | Log: exceptions, invoice creation/cancellation, stock changes, logins |
| RULE-037 | NEVER log: passwords, connection strings |

### 2.16 EF Core Conventions

| Convention | Rule |
|-----------|------|
| Config style | Fluent API ONLY — no DataAnnotations on Entities |
| Delete behavior | `Restrict` on ALL FKs — NEVER cascade |
| Soft delete | Global query filter `IsActive == true` |
| Strings | `nvarchar` with explicit `MaxLength` |
| Decimals | `.HasPrecision(18, 2)` or `.HasPrecision(18, 3)` |

### 2.17 Clean Code & Centralized Design (New)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-041 | **DRY Principle**: NEVER duplicate business logic (e.g., pricing, unit conversion). All core math must live in `SalesSystem.Domain`. |
| RULE-042 | **Rich Domain Model**: Use `private set` for critical entity properties (like `CurrentQuantity`). State changes MUST happen via centralized domain methods (e.g., `DeductStock()`). |
| RULE-043 | **CQRS & MediatR**: Strictly separate Read operations (Queries) from Write operations (Commands) to prevent UI reads from blocking transactional logic. |
| RULE-044 | **FluentValidation**: EVERY Command (Write operation) MUST have an associated `AbstractValidator` executed before reaching the Database. |

### 2.18 Per-Unit Pricing (v4.1 — Updated)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-045 | Pricing is per (Product + Unit + Currency) — NEVER store RetailPrice/WholesalePrice on Product entity |
| RULE-046 | Use `ProductPrices` table: ProductUnitId FK, CurrencyId FK, Price decimal(18,2), EffectiveFrom datetime2, EffectiveTo datetime2 (nullable) — supports multi-currency pricing with effective date ranges |
| RULE-047 | Unit conversion uses Factor from ProductUnit — NEVER compute conversion in UI or Service |
| RULE-048 | Stock is always stored in base unit — conversion happens inside Domain via ProductUnit.Factor |
| RULE-049 | Invoice items store `ProductUnitId` to determine which unit price was used — NEVER store SaleMode |

### 2.19 Delete Strategy (v4.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-050 | Use `DeleteStrategy` enum for ALL delete operations |
| RULE-051 | Three options: Cancel (0), Deactivate (1), Permanent (2) |

**Enum Definition:**
```csharp
public enum DeleteStrategy
{
    Cancel = 0,      // Abort operation
    Deactivate = 1,  // Soft delete - set IsActive = false
    Permanent = 2   // Hard delete - physical removal from DB
}
```

**UI Pattern (WPF):**
```csharp
var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المنتج: {product.Name}");
if (strategy == DeleteStrategy.Cancel) return;
if (strategy == DeleteStrategy.Deactivate)
{
    // Soft delete - keep entity, mark as inactive
    var result = await _productService.DeleteAsync(product.Id);
}
else if (strategy == DeleteStrategy.Permanent)
{
    // Hard delete - check references first
    var result = await _productService.DeletePermanentlyAsync(product.Id);
}
```

**API Endpoints:**
- `DELETE /api/v1/products/{id}` → Soft delete (IsActive=false)
- `DELETE /api/v1/products/permanent/{id}` → Hard delete (with reference validation)
- Same pattern for: Categories, Units, Warehouses, Customers, Suppliers, Users

### 2.20 Defensive Programming (v4.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-052 | All Domain entities MUST have Guard Clauses to prevent invalid states |
| RULE-053 | Use `DomainException` with Arabic messages for validation failures |

**Pattern:**
```csharp
// In entity constructor or factory method
if (string.IsNullOrWhiteSpace(name))
    throw new DomainException("الاسم مطلوب");
if (price < 0)
    throw new DomainException("السعر لا يمكن أن يكون سالباً");
if (quantity <= 0)
    throw new DomainException("الكمية يجب أن تكون أكبر من الصفر");
```

**Entities with Guard Clauses:** Product, Customer, Supplier, SalesInvoice, PurchaseInvoice, WarehouseStock, StockTransfer, SalesReturn, PurchaseReturn, User, Category, Unit, Warehouse, DocumentSequence, StoreSettings, InventoryMovement, CustomerPayment, SupplierPayment, SalesInvoiceLine, PurchaseInvoiceLine, SalesReturnItem, PurchaseReturnItem, StockTransferItem

### 2.21 WPF Dialog Service (v4.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-054 | Use `IDialogService` for ALL user-facing messages |
| RULE-055 | NEVER use raw `MessageBox.Show` |

**DialogService Methods:**
```csharp
public interface IDialogService
{
    Task ShowErrorAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<DeleteStrategy> ShowDeleteConfirmationAsync(string itemDescription);
}
```

**Styled Dialogs Location:** `Views/Dialogs/`
- `ErrorDialog.xaml` - Red theme, error icon ✕
- `SuccessDialog.xaml` - Green theme, success icon ✓
- `WarningDialog.xaml` - Yellow/orange theme, warning icon ⚠
- `ConfirmationDialog.xaml` - Blue theme, question icon ?
- `DeleteConfirmationDialog.xaml` - Orange/red theme, 3 buttons (Cancel/Deactivate/Delete)

### 2.22 Toast Notifications (v4.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-056 | Use `IToastNotificationService` for minor success messages |
| RULE-057 | Toast auto-dismisses: Success/Info = 3s, Error = 5s |

**Service:**
```csharp
public interface IToastNotificationService
{
    void ShowSuccess(string message);  // Green, 3s
    void ShowError(string message);    // Red, 5s
    void ShowInfo(string message);     // Blue, 3s
}
```

**Usage:** Use for delete/restore confirmations, minor success feedback.
**Location:** `Services/App/Toast/ToastWindow.xaml`

### 2.23 Real-Time UI Validation (v4.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-058 | ViewModelBase implements `INotifyDataErrorInfo` |
| RULE-058 | ViewModelBase implements `INotifyDataErrorInfo` |
| RULE-059 | **InterActive Validation** — Save buttons ALWAYS enabled (no CanExecute blocking). On click, `Validate()` shows styled warning dialog with ALL missing/incorrect fields listed. Required fields marked with `*` and input fields have ToolTips explaining validation rules. Unique fields (barcode, username) have explicit uniqueness explanation. |

**Pattern — Enable buttons, validate on click with clear warning:**
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

**XAML Pattern — Always enabled, ToolTips explain rules:**
```xml
<TextBlock Text="اسم المنتج *" Style="{StaticResource LabelStyle}"/>
<TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
         ToolTip="أدخل اسم المنتج — هذا الحقل إلزامي"
         Style="{StaticResource ModernTextBox}"/>
<TextBlock Text="{Binding NameError}" Foreground="{StaticResource ErrorBrush}" 
           Visibility="{Binding HasNameError, Converter={StaticResource BoolToVisibility}}"/>

<!-- Unique field explanation -->
<TextBox Text="{Binding Barcode}"
         ToolTip="الباركود — يجب أن يكون فريداً لكل منتج"
         Style="{StaticResource ModernTextBox}"/>
<TextBlock Text="الباركود يجب أن يكون فريداً — لا يمكن تكرار نفس الرمز لمنتجين مختلفين" 
           Style="{StaticResource HelperTextStyle}"/>

<!-- Save button — no IsEnabled binding -->
<Button Command="{Binding SaveCommand}" Style="{StaticResource PrimaryButton}"
        ToolTip="حفظ البيانات المدخلة">
```

**WRONG — Never disable buttons:**
```csharp
SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);     // ❌ WRONG
Button IsEnabled="{Binding CanSave}"                                // ❌ WRONG
```

**Validation Rules (Arabic):**
- `"يجب اختيار منتج"` (Product required)
- `"الكمية يجب أن تكون أكبر من صفر"` (Quantity must be > 0)
- `"السعر لا يمكن أن يكون سالباً"` (Price cannot be negative)

### 2.24 Units & Product Units (v4.3 — Updated)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-060 | `Units` is an INDEPENDENT table (seed data + user-addable) — NEVER embed unit names in ProductUnit. `Units` has: Id, Name, Symbol, IsSystem (protects seed units), IsActive. |
| RULE-061 | `ProductUnit` links Product to Unit via `UnitId` FK — stores `Factor` (conversion to base unit) and `IsBaseUnit` flag. Base unit always has `Factor = 1`. |
| RULE-062 | Derived units have `Factor > 1` (e.g., Carton=24 means 1 Carton = 24 Base units). Use `ConvertToUnit(decimal quantity, int fromUnitId, int toUnitId)` for all unit conversions. |
| RULE-063 | `UnitBarcode` stores ALL barcodes per product-unit combination — never embed barcode in Unit entity |
| RULE-064 | `SmartUnitFormatter` selects best display unit based on quantity threshold — use in UI only |
| RULE-065 | Pricing is stored in `ProductPrices` table (ProductUnitId + CurrencyId + Price) — NEVER store prices on ProductUnit entity |
| RULE-066 | Cost cascade: When purchase cost updates via WeightedAverage, ALL product units recalculate from base unit cost × Factor |
| RULE-067 | `ProductMustHaveAtLeastOneUnit` rule enforced in Domain — throw `DomainException` if deleting last unit. Product MUST have at least one base unit + one additional unit. |
| RULE-464 | `Product` entity MUST have optional `DefaultPurchaseUnitId` (int?) and `DefaultSalesUnitId` (int?) — pre-selects units in purchase/sales screens for faster data entry |
| RULE-465 | Units seed data: حبة (PCS), كرتون (CTN), كيلو (KG), جرام (G), لتر (L), متر (M), بالة (BAL) — all with `IsSystem = true` |
| RULE-466 | User can add new units, but cannot delete units used by any ProductUnit — soft-deactivate instead |

**ProductUnit Entity pattern:**
```csharp
public class ProductUnit
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public int UnitId { get; private set; }          // FK to Units table
    public Unit Unit { get; private set; }           // Navigation property
    public decimal Factor { get; private set; }      // Conversion to base unit (Base=1, Carton=24)
    public bool IsBaseUnit { get; private set; }     // Exactly one per product
    public bool IsActive { get; private set; }
    public ICollection<UnitBarcode> Barcodes { get; private set; }

    public decimal ConvertToUnit(decimal quantity, decimal targetFactor)
    {
        var baseQty = quantity * Factor;
        return baseQty / targetFactor;
    }
}
```

### 2.25 Costing Strategy (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-068 | Costing method stored in `SystemSettings` table — seeded as `WeightedAverage` (1) |
| RULE-069 | Three methods: WeightedAverage (1), LastPurchasePrice (2), SupplierPrice (3) |
| RULE-070 | Costing update fires AFTER purchase invoice is saved and has an ID |
| RULE-071 | WeightedAverage = `(OldStock * OldAvgCost + NewQty * NewUnitCost) / (OldStock + NewQty)` |
| RULE-072 | LastPurchasePrice = overwrite `AvgCost` with incoming `UnitCost` directly |
| RULE-073 | SupplierPrice = use `Product.SupplierPrice` (catalog price) — no cost calculation |
| RULE-074 | Cost cascade: ALL product units updated from base unit cost × Factor |
| RULE-075 | `UpdateProductPricingService` handles all three methods — NEVER write costing logic outside this service |
| RULE-076 | Add audit entry in `ProductPriceHistory` on EVERY cost change |

**WeightedAverage formula (C#):**
```csharp
var totalOldValue = oldStock * oldAvgCost;
var totalNewValue = newQty * newUnitCost;
var newAvgCost = (totalOldValue + totalNewValue) / (oldStock + newQty);
// Result: decimal with precision (18,2)
```

### 2.26 Cash Boxes (v4.9 — Refactored for Chart of Accounts Integration)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-077 | CashBox is a **lightweight register entity** with `AccountId` (required FK to Account) — balance is tracked on the linked Chart of Accounts Account, NOT on CashBox. CashBox has NO `OpeningBalance` or `CurrentBalance` fields. |
| RULE-078 | `CashBox` has metadata fields: `CategoryId` (FK to Categories), `PhoneNumber`, `TaxNumber`, `Address` — these are optional for classifying and identifying cash registers by type (cashier desk, bank account, fund, representative custody). |
| RULE-079 | `CashTransaction` records: OpeningBalance, Income, Expense, Transfer, Refund, Payment — uses `RunningBalance` (computed cumulative sum) instead of `BalanceBefore`/`BalanceAfter`. `CashTransaction.Create()` is public (not restricted to CashBox domain methods). |
| RULE-080 | Account balance (NOT CashBox) NEVER goes negative — the `Account` entity on the Chart of Accounts validates balance before dispensing. CashBox service computes running balance as sum of all CashTransaction amounts. |
| RULE-081 | Cash transfer between boxes requires TWO transactions (Out from source, In to destination) — no balance validation on client side. Server validates via running balance computation from CashTransaction records. |
| RULE-082 | Cash transactions are immutable once created — no editing, cancellation via offsetting entry |
| RULE-083 | `DailyClosure` computes: OpeningBalance + TotalIncome - TotalExpense = ClosingBalance (OpeningBalance sourced from Account's opening balance via Chart of Accounts) |

**Auto-Account Creation Pattern:**
When creating a CashBox without an `AccountId`, the service auto-creates a Level-4 detail account under parent `"1101 — النقدية صناديق"` (Cash & Cash Equivalents — a Level 3 account under Current Assets 1100). Account codes auto-increment (1111, 1112, 1113...).

```csharp
// CashBoxService auto-account creation (when AccountId is null):
var parentAccount = await _uow.Accounts.GetByCodeAsync("1101", ct);
var maxCode = await _uow.Accounts.GetMaxChildCodeAsync(parentAccount.Id, ct);
var newCode = (int.Parse(maxCode ?? "1101") + 1).ToString();
var account = Account.Create(newCode, $"صندوق {box.Name}", $"Cash Box {box.Name}",
    AccountType.Asset, 4, parentAccount!.Id, false);
await _uow.Accounts.AddAsync(account, ct);
box.SetAccountId(account.Id);
```

**CashBox Entity Pattern:**
```csharp
public class CashBox : BaseEntity
{
    public string Name { get; private set; }
    public int AccountId { get; private set; }          // FK to Account — balance lives here
    public Account? Account { get; private set; }        // Navigation property
    public int? CategoryId { get; private set; }         // FK to Categories (optional)
    public Category? Category { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public int CurrencyId { get; private set; }          // Per-box currency
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

// CashTransaction — RunningBalance replaces BalanceBefore/BalanceAfter
public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RunningBalance { get; private set; }   // Computed cumulative sum
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

**CashTransactionType enum:**
```csharp
public enum CashTransactionType : byte
{
    OpeningBalance = 1,  // Initial opening
    SalesIncome = 2,     // From sales invoices
    Expense = 3,         // Cash outflow
    TransferOut = 4,     // Transfer to another box
    TransferIn = 5,      // Transfer from another box
    RefundOut = 6,       // Sales return refund
    SupplierPayment = 7, // Payment to supplier
    CustomerPayment = 8  // Payment from customer
}
```

### 2.27 Product Price History (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-084 | `ProductPriceHistory` records EVERY price/cost change — NEVER update price without audit |
| RULE-085 | Record fields: `ProductUnitId`, `OldRetailPrice`, `NewRetailPrice`, `OldWholesalePrice`, `NewWholesalePrice`, `OldCost`, `NewCost`, `ChangedByUserId`, `ChangeReason` |
| RULE-086 | Price change triggers: Purchase invoice (cost update), manual price adjustment, supplier sync |
| RULE-087 | History query available in Reports for audit trail — kept indefinitely |

### 2.28 Print Engine (v4.3 — Implemented)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-088 | A4 PDF generation uses **QuestPDF** (`A4InvoiceDocument`) — NOT WPF FixedDocument |
| RULE-089 | Thermal receipts use **Win32 raw printing** (`OpenPrinter`/`WritePrinter`) with custom `EscPos` builder — NOT WPF print APIs |
| RULE-090 | Print templates stored in `Application/Printing/Contracts/` (DTOs) and `Infrastructure/Printing/` (documents) |
| RULE-091 | All printing goes through `PrintController` API — Desktop NEVER prints directly |
| RULE-092 | Desktop calls `IPrintApiService` (HTTP client) → API `PrintController` → `IPrintService` |
| RULE-093 | Preview shows in WPF `PdfPreviewWindow` (WebBrowser control) before sending to printer |
| RULE-094 | PrintService returns `PrintResult` — NEVER throw from printing code |
| RULE-095 | Supports: 80mm thermal receipt, A4 invoice — configurable per printer via SystemSettings |
| RULE-096 | Thermal output: 42-char monospaced columns, Windows-1256 encoding for Arabic |
| RULE-097 | ESC/POS commands built using `EscPos` static class — NOT external NuGet packages |
| RULE-098 | A4 documents use QuestPDF `Community` license (free for < $1M revenue) |
| RULE-099 | Prerequisite: All 3 target frameworks must be `net10.0-windows` (Infrastructure + Api + Infra.Tests) for Win32 `DllImport` |
| RULE-100 | Print settings (`ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber`, `TaxRate`) stored in `SystemSetting` table with `Category = "Print"` |
| RULE-101 | Missing logo = graceful omission (null) — NEVER crash on missing logo |
| RULE-102 | `IPrintService` methods: `PreviewA4Async`, `PrintA4Async`, `PrintThermalAsync`, `SavePdfAsync` — each returns `PrintResult` |

**Project structure:**
```text
SalesSystem.Application/Printing/
├── Contracts/
│   ├── InvoicePrintDto.cs            # Full invoice data for printing
│   ├── InvoiceItemPrintDto.cs        # Line item
│   ├── InvoiceTypePrint.cs           # Enum: Sales, Purchase, SalesReturn, PurchaseReturn, Test
│   └── PrintResult.cs                # Success/error with messages
├── InvoicePrintDtoBuilder.cs         # 4 builder overloads (sales/purchase/return)
└── IPrintService.cs                  # Interface (4 methods)

SalesSystem.Infrastructure/Printing/
├── A4InvoiceDocument.cs              # QuestPDF A4 document (RTL, logo, tax breakdown)
├── ThermalReceiptGenerator.cs        # ESC/POS receipt builder
├── EscPos.cs                         # Static ESC/POS command builder
├── PrintService.cs                   # Win32 raw printing implementation
├── PrinterException.cs               # Printing-specific exception
└── PrintingBootstrapper.cs           # QuestPDF license init
```

**DI Registration (Program.cs):**
```csharp
// API
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<InvoicePrintDtoBuilder>();
PrintingBootstrapper.Initialize(); // QuestPDF license

// Desktop
builder.Services.AddHttpClient<IPrintApiService, PrintApiService>(client => {
    client.BaseAddress = new Uri("http://localhost:5221");
});
```

**API Endpoints (PrintController):**
```text
GET    /api/v1/print/sales/{id}/preview        → A4 preview HTML
POST   /api/v1/print/sales/{id}/a4             → Print A4 invoice
POST   /api/v1/print/sales/{id}/thermal        → Print thermal receipt
POST   /api/v1/print/sales/{id}/save           → Save PDF to file
GET    /api/v1/print/purchases/{id}/preview    → Purchase A4 preview
POST   /api/v1/print/purchases/{id}/a4         → Print purchase A4
POST   /api/v1/print/purchases/{id}/thermal    → Print purchase thermal
POST   /api/v1/print/purchases/{id}/save       → Save purchase PDF
GET    /api/v1/print/sales/{id}/preview-data   → Preview JSON data
GET    /api/v1/print/purchases/{id}/preview-data
POST   /api/v1/print/test                      → Test page print
```

### 2.29 Auto-Update System (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-103 | Auto-Update NEVER blocks startup — fire-and-forget with silent failure |
| RULE-104 | ALL service methods return `Result<T>` or `Result` — NEVER throw |
| RULE-105 | SHA256 checksum verification required before launching installer |
| RULE-106 | Skipped version persisted to `%AppData%\SalesSystem\settings.json` — NEVER `appsettings.json` |
| RULE-107 | Desktop calls API for updates — NEVER implements its own HTTP download logic |
| RULE-108 | `Environment.Exit(0)` is FORBIDDEN — return `Result<bool>` and let caller handle shutdown |
| RULE-109 | Update check timeout = 8 seconds — NEVER make user wait |
| RULE-110 | Version comparison uses `System.Version` — NEVER string comparison |

**IUpdaterService Interface:**
```csharp
public interface IUpdaterService
{
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default);
    Task<Result<string>> DownloadUpdateAsync(string downloadUrl, string expectedChecksum,
        IProgress<DownloadProgress> progress, CancellationToken ct = default);
    Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath);
    Result<string> GetCurrentVersion();
    Result SkipVersion(string version);
    Result<string> GetSkippedVersion();
}
```

**DI Registration (Program.cs):**
```csharp
builder.Services.AddUpdateServices(builder.Configuration);
// To use GitHub API instead: replace UpdaterService with GitHubUpdaterService in DependencyInjection.cs
```

### 2.30 Security & DPAPI (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-111 | Connection strings encrypted via DPAPI (`IDataProtector`) with `"DPAPI:"` prefix |
| RULE-112 | `FirstRunSetupService` encrypts plaintext connection string on first run — idempotent |
| RULE-113 | `SecureDbContextFactory` decrypts connection string before creating DbContext |
| RULE-114 | DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys` |
| RULE-115 | `SecurityAudit.cs` runs in DEBUG only — checks for unencrypted strings, hardcoded passwords |
| RULE-116 | JWT secret from environment variable — throws `InvalidOperationException` in production if missing |
| RULE-117 | `appsettings.json` writes use atomic pattern: write to `.tmp` → `File.Replace()` → creates `.bak` |

**ConnectionStringProtector Pattern:**
```csharp
public class ConnectionStringProtector : IConnectionStringProtector
{
    private const string Prefix = "DPAPI:";
    private const string Purpose = "SalesSystem.ConnectionString.v1";
    // Encrypt: checks IsEncrypted() first → prevents double-encryption
    // Decrypt: validates prefix → unwraps via IDataProtector
}
```

### 2.31 Backup System (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-118 | Backup uses raw SQL `BACKUP DATABASE` — NEVER SMO dependency |
| RULE-119 | Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` — gives active transactions 30s |
| RULE-120 | `ScheduledBackupWorker` runs daily at 2:00 AM as `BackgroundService` |
| RULE-121 | Backup retention = configurable days (default 30) — old backups auto-deleted |
| RULE-122 | Restore failure triggers `TrySetMultiUserAsync` recovery — NEVER leave DB in SINGLE_USER |
| RULE-123 | Config parsing uses `int.TryParse` — NEVER `int.Parse` on config values |

**ScheduledBackupWorker Pattern:**
```csharp
public class ScheduledBackupWorker : BackgroundService
{
    // Uses IServiceScopeFactory for scoped service resolution
    // Respects CancellationToken for graceful shutdown
    // Runs backup at 2:00 AM daily → then cleanup old backups
}
```

### 2.32 Windows Service (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-124 | API runs as Windows Service via `UseWindowsService()` |
| RULE-125 | Service name: `SalesSystemService` — Arabic display name |
| RULE-126 | Auto-recovery: 3 restarts on failure (1min, 5min, 15min delays) |
| RULE-127 | Serilog EventLog sink for Windows Service logging |
| RULE-128 | SQL retry on startup: 3 attempts × 5 second delay |
| RULE-129 | Database migration runs on service startup (auto-migrate) |

**Program.cs Integration:**
```csharp
builder.Host.UseWindowsService(options => options.ServiceName = "SalesSystemService");
// + Serilog EventLog sink + SQL retry + FirstRunSetupService
```

### 2.33 Admin Screens (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-130 | `AdminOnlyViewModel` base class enforces Admin role via `ISessionService` |
| RULE-131 | Non-admin users get `UnauthorizedAccessException` — NEVER show admin UI |
| RULE-132 | Constructor injection required — NEVER `App.GetService<T>()` in base class |
| RULE-133 | User management: Toggle Status (soft delete), Reset Password — all via API |

**AdminOnlyViewModel Pattern:**
```csharp
public abstract class AdminOnlyViewModel : ViewModelBase
{
    protected AdminOnlyViewModel(ISessionService sessionService)
    {
        var role = sessionService.GetUserRole();
        if (role != UserRole.Admin)
            throw new UnauthorizedAccessException("صلاحية Admin مطلوبة");
    }
}
```

### 2.34 Installer (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-134 | Inno Setup script (`SalesSystem.iss`) — admin install required |
| RULE-135 | .NET 10 runtime check before installation |
| RULE-136 | Windows Service auto-start configured during install |
| RULE-137 | Installer creates backup directory, sets permissions |

### 2.36 ViewModel ExecuteAsync Pattern (v4.5)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-141 | ALL ViewModel async commands MUST use `ExecuteAsync()` wrapper — NEVER manual try/catch/finally |
| RULE-142 | `IsBusy` property (protected set) replaces `IsLoading` — managed by `ExecuteAsync()` |
| RULE-143 | `StatusMessage` property (protected set) for user feedback during operations |
| RULE-144 | `ExecuteAsync(Func<Task>)` — wraps operations with IsBusy + error handling + logging |
| RULE-145 | `ExecuteAsync(Func<Task>, Action<Exception>)` — same with custom error callback for UI display |
| RULE-146 | `ExecuteResultAsync<T>(Func<Task<Result<T>>>)` — wraps Result<T> operations, returns null on failure |

**ExecuteAsync Pattern:**
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

// WRONG — NEVER do this (manual try/catch/finally)
private async Task LoadProductsAsync()
{
    try { IsLoading = true; /* ... */ }
    catch (Exception ex) { HandleException(ex); }
    finally { IsLoading = false; }
}
```

**Key rules:**
- Validation/early-return logic stays OUTSIDE `ExecuteAsync()` (before the wrapper call)
- Business logic goes INSIDE the operation method (no try/catch needed)
- `IsBusy` is automatically set to true/false by `ExecuteAsync()`
- Exceptions are automatically caught, logged via Serilog, and handled

### 2.37 Architecture Decisions (v4.5)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-147 | Service Layer pattern is the standard — NOT CQRS/MediatR |
| RULE-148 | MediatR package is REMOVED — use direct service interfaces |
| RULE-149 | Legacy WinForms project is DELETED — all code rebuilt in DesktopPWF (WPF) |
| RULE-150 | MASTER-PLAN.md reflects actual Clean Architecture (Layered) — NOT aspirational |

### 2.38 Database Health Check & Graceful Error Handling (v4.5)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-151 | API MUST expose `GET /api/v1/health/database` endpoint that checks DB connectivity via `DbContext.Database.CanConnectAsync()` |
| RULE-152 | `GET /api/v1/health` MUST include `Database` field (`Connected`/`Disconnected`) — NEVER return static `OK` |
| RULE-153 | Desktop MUST check database connectivity on startup BEFORE showing login window — use `IDatabaseHealthCheckService` |
| RULE-154 | On DB connection failure, Desktop MUST show `DatabaseErrorDialog` with Retry/Exit buttons — NEVER crash silently |
| RULE-155 | `ExceptionMiddleware` MUST detect DB connection exceptions (`InvalidOperationException` with connection string message, `SqlException`) and return `503 Service Unavailable` with `DATABASE_CONNECTION_ERROR` code |
| RULE-156 | `ExceptionMiddleware` MUST use `IsDatabaseConnectionException()` helper — checks exception type name (avoids hard dependency on SQL Server packages) |
| RULE-157 | `SecureDbContextFactory.GetDecryptedConnectionString()` MUST fall back to `SALESSYSTEM_DB_CONNECTION` environment variable — NEVER throw on missing config alone |
| RULE-158 | Desktop `DatabaseErrorDialog` MUST provide retry loop — user can retry or exit, application NEVER blocks indefinitely |
| RULE-159 | `DatabaseHealthCheckService` MUST catch `HttpRequestException` and `TaskCanceledException` — return `HealthCheckResult` with Arabic error messages |

**Desktop Startup Flow:**
```csharp
// App.xaml.cs
private async Task<bool> CheckDatabaseConnectionAsync()
{
    var healthService = _serviceProvider!.GetRequiredService<IDatabaseHealthCheckService>();

    while (true)
    {
        var result = await healthService.CheckAsync();
        if (result.IsDatabaseConnected) return true;

        var retry = await Dispatcher.InvokeAsync(() =>
        {
            var dialog = new DatabaseErrorDialog(result.ErrorMessage);
            dialog.Owner = MainWindow;
            dialog.ShowDialog();
            return dialog.RetryClicked;
        });

        if (!retry) return false;
        await Task.Delay(1000);
    }
}
```

**API Health Check Pattern:**
```csharp
app.MapGet("/api/v1/health", async (SalesDbContext db) =>
{
    var dbConnected = false;
    try { dbConnected = await db.Database.CanConnectAsync(); } catch { }
    return new { Status = dbConnected ? "OK" : "Degraded", Database = dbConnected ? "Connected" : "Disconnected", Version = "1.0", Timestamp = DateTime.UtcNow };
});

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
```

**ExceptionMiddleware DB Detection Pattern:**
```csharp
private static bool IsDatabaseConnectionException(Exception ex)
{
    if (ex is InvalidOperationException && 
        (ex.Message.Contains("Connection string", StringComparison.OrdinalIgnoreCase) ||
         ex.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)))
        return true;
    if (ex.InnerException != null) return IsDatabaseConnectionException(ex.InnerException);
    var typeName = ex.GetType().FullName ?? "";
    return typeName.Contains("SqlException", StringComparison.Ordinal) ||
           typeName.Contains("SqlClient", StringComparison.Ordinal);
}
```

### 2.39 Dialog Service Enhancement (v4.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-138 | `IDialogService` has `ShowInfoAsync` method — blue theme, info icon |
| RULE-139 | ALL sync dialog methods use styled dialogs — NEVER raw `MessageBox.Show` |
| RULE-140 | `UpdateDialogViewModel` implements `IDisposable` — disposes `_downloadCts` |

### 2.40 Multi-Window Screen Management (v4.5)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-160 | Use `IScreenWindowService` / `ScreenWindowService` for ALL non-modal window opening — NEVER create + ShowDialog directly |
| RULE-161 | Editors MUST open non-modally via `ScreenWindowService.OpenScreen(viewModel, options)` — NEVER `ShowDialog()` |
| RULE-162 | ScreenWindow hosts any View/ViewModel pair in a generic `ScreenWindow.xaml` — NEVER create per-screen Window classes |
| RULE-163 | Window tracking uses `WeakReference<Window>` — NEVER strong references (prevent memory leaks) |
| RULE-164 | Cascade positioning: new windows offset 30px × (count % 10) from MainWindow |
| RULE-165 | `OnClosed` callback MUST marshal UI operations via `Application.Current.Dispatcher.InvokeAsync()` |
| RULE-166 | ViewModel lifecycle: `CloseRequested` → close window → `Cleanup()` → fire `OnClosed` — ALL handled by ScreenWindowService |
| RULE-167 | Auto-titles use Arabic names (e.g., "فاتورة بيع") — NEVER English or type names |
| RULE-168 | MainWindow MUST provide "فتح نافذة جديدة" menu items for opening list screens in new windows |
| RULE-169 | ScreenWindowService resolves View by naming convention: `ViewModel` → `View` in FullName (same as DialogService) |
| RULE-170 | `OpenWindow(Window)` overload for pre-created windows — `OpenScreen(object viewModel)` for convention-based resolution |

**Correct pattern — opening an editor non-modally:**
```csharp
// CORRECT — use ScreenWindowService
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

// WRONG — NEVER do this (modal, blocks MainWindow)
private void AddNewInvoice()
{
    var editorVm = new SalesInvoiceEditorViewModel();
    var editorWindow = new SalesInvoiceEditorView { DataContext = editorVm };
    editorVm.CloseRequested += () => editorWindow.DialogResult = true;
    if (editorWindow.ShowDialog() == true) { /* ... */ }  // ❌ BLOCKS
}
```

**Opening a list screen in a new window:**
```csharp
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
```

### 2.41 Error Message Best Practices (v4.5.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-171 | ALL catch blocks in ViewModels MUST use `Serilog.Log.Error(ex, context)` — NEVER show `ex.Message` in user-facing dialogs |
| RULE-172 | `HandleFailure()` in ViewModelBase MUST transform common errors (timeout, network, not found) into user-friendly Arabic — NEVER pass raw English error text to users |
| RULE-173 | Dialog titles MUST be screen-specific (e.g., `"خطأ في حفظ الفاتورة"`) — NEVER use generic `"خطأ"` alone |
| RULE-174 | `MessageBox.Show` is FORBIDDEN in ViewModels — ALL user-facing messages go through `IDialogService` |
| RULE-175 | ALL `IDialogService` calls must use the `Async` suffix methods (`ShowErrorAsync`, `ShowSuccessAsync`) — NEVER the sync overloads |
| RULE-176 | Success messages MUST name the action (e.g., `"تم تصدير التقرير إلى Excel بنجاح"`) — NEVER `"تم التصدير بنجاح"` |
| RULE-177 | Raw HTTP response bodies MUST NEVER be shown in user-facing dialogs — always log them via Serilog and show a friendly message instead |

The following bugs were fixed in this session:
- **CustomerListViewModel** — replaced manual `new CustomerEditorView { DataContext = vm }` + `window.ShowDialog()` with `_dialogService.ShowDialog(vm)` (violated RULE-160/161)
- **CustomerListViewModel** — replaced `System.Windows.MessageBox.Show()` in `RestoreCustomerAsync()` with `_dialogService.ShowSuccessAsync()` / `HandleFailure()` (violated RULE-174)
- **SupplierListViewModel** — `AddSupplier()` and `EditSupplier()` ignored `_dialogService.ShowDialog()` return value, causing list to never refresh (violated RULE-006/RULE-141)
- **SupplierListViewModel** — replaced `System.Windows.MessageBox.Show()` in `RestoreSupplierAsync()` with `_dialogService.ShowSuccessAsync()` / `HandleFailure()` (violated RULE-174)
- **ProductEditorViewModel** — added `IDialogService` dependency and replaced 4× `System.Windows.MessageBox.Show()` in `SaveAsync()` with proper async dialog calls (violated RULE-174)
- **ProductEditorViewModelTests** — updated 19 constructor calls with `Mock<IDialogService>` parameter

### 2.42 Application Shutdown (v4.5.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-178 | `App.xaml` MUST use `ShutdownMode="OnExplicitShutdown"` — NEVER `OnLastWindowClose` (hidden ScreenWindow instances keep the app alive) |
| RULE-179 | `LoginWindow.CloseButton_Click` MUST call `Application.Current.Shutdown()` — NEVER `Close()` (which only closes the window) |
| RULE-180 | `MainWindow.Closed` event MUST call `System.Windows.Application.Current.Shutdown()` — except during logout (guarded by `_isLoggingOut` flag) |
| RULE-181 | Logout flow: set `_isLoggingOut = true`, clear session, open new LoginWindow, then `this.Close()` — prevents shutdown during logout |

**Related fix:** `MainWindow.xaml.cs` — `_isLoggingOut` flag prevents `Application.Current.Shutdown()` during logout flow. Navigation permissions verified via `CanNavigateTo()` in `NavigationList_SelectionChanged`.

### 2.43 UI ToolTips (v4.5.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-185 | ALL interactive controls (Button, MenuItem, ListBoxItem) MUST have an Arabic `ToolTip` that explains what the control does — NEVER leave a tooltip-less button |
| RULE-186 | ToolTips MUST be user-action-oriented (e.g., `"فتح شاشة إضافة منتج جديد"`) — NEVER just repeat the button text (e.g., not `"منتج جديد"`) |
| RULE-187 | Action buttons (Save, Post, Cancel, Delete, Print) MUST explain consequences — e.g., `"ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد"` |
| RULE-188 | Navigation MenuItems in MainWindow MUST describe the destination screen — e.g., `"عرض وإدارة فواتير البيع"` |
| RULE-189 | Empty-state buttons (e.g., "➕ إضافة أول منتج") MUST have ToolTip — these are often the user's first interaction |
| RULE-190 | Error dismiss ("✕") buttons MUST have ToolTip `"إخفاء رسالة الخطأ"` |

### 2.44 Logging Separation Policy (v4.5.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-182 | `Log.Error` reserved for **system errors only**: DB connection failures, API unreachable, JSON parse crashes, unhandled exceptions, file I/O failures — NEVER for user input validation failures |
| RULE-183 | `Log.Warning` for **user mistakes**: validation errors (e.g., "الاسم مطلوب"), business rule violations (e.g., "المخزون غير كافٍ"), "not found" from user input — NEVER log these at Error level |
| RULE-184 | `HandleResponseAsync` must check `ContentType == "application/json"` before parsing error responses — prevent `JsonException` crash on empty/HTML 404 bodies |

### 2.45 Identifier Strategy — Complete (v4.5.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-191 | Product, Customer, Supplier, and Warehouse MUST NOT have a `Code` column — use auto-increment `Id` (int PK) as the sole identifier |
| RULE-192 | Search/filter by `Id` (int) or `Name` (string) only — NEVER filter by a Code string column |
| RULE-193 | Invoice line items carry `ProductId` (int FK) — `ProductCode` is removed as it was a denormalized duplicate |
| RULE-194 | Report DTOs (StockReport, CustomerBalance, SupplierBalance, LowStock) MUST NOT include Code fields — use Id + Name instead |
| RULE-195 | Code auto-generation services (`DocumentSequenceService` for PRD/CUST/SUP/WH) are REMOVED — no manual or auto-generated code needed |
| RULE-196 | Editor ViewModels (Product, Customer, Supplier, Warehouse) MUST NOT have a `Code` property — remove all UI fields and validation for Code |
| RULE-197 | `DuplicateCode` error constant is REMOVED from ErrorCodes — only `DuplicateBarcode` remains for barcode uniqueness validation |
| RULE-198 | `WarehouseResponse` DTO MUST NOT have a `Code` field — it was removed in v4.5.3 |

### 2.46 Centralized LogSystemError Pattern (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-199 | `LogSystemError()` is the ONLY method for system error logging in ALL ViewModels — NEVER call `Serilog.Log.Error` directly in any ViewModel |
| RULE-200 | ALL hard-delete operations in Application Services MUST catch `DbUpdateException` and return `Result.Failure` with Arabic message — NEVER let FK exception crash the API |
| RULE-201 | All catch blocks in ViewModels MUST use `LogSystemError(message, context, exception)` from ViewModelBase — NEVER `Serilog.Log.Error(ex, ...)` directly |

### 2.47 Service Layer & Controller Purity (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-202 | ALL Application Service methods MUST return `Result<T>` or `Result` — NEVER throw exceptions like `KeyNotFoundException` |
| RULE-203 | Controllers MUST NOT inject `DbContext` or `IUnitOfWork` directly — delegate ALL data access to Application Services |
| RULE-204 | PrintController MUST move `SalesDbContext` queries to a dedicated `IPrintDataService` in Application layer |
| RULE-205 | LogsController MUST move logging logic to an `ILogService` in Application layer |

### 2.48 Enum Integrity (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-206 | ALL enum VALUES MUST match AGENTS.md Section 3 exactly — NEVER deviate from the canonical values |
| RULE-207 | `CostingMethod` values: WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3 |
| RULE-208 | `CashTransactionType` values: OpeningBalance=1, SalesIncome=2, Expense=3, TransferOut=4, TransferIn=5, RefundOut=6, SupplierPayment=7, CustomerPayment=8 |
| RULE-209 | `InvoiceTypePrint` values: Sales=1, Purchase=2, SalesReturn=3, PurchaseReturn=4, Test=5 |

### 2.49 Database CHECK Constraints (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-210 | `WarehouseStocks.Quantity` MUST have a DB-level `CHECK (Quantity >= 0)` constraint in Fluent API configuration |
| RULE-211 | ALL `decimal(18,4)` precision MUST be changed to `decimal(18,2)` for money fields or `decimal(18,3)` for quantity fields — NO other precision allowed |
| RULE-212 | `Product.ReorderLevel` MUST use `HasPrecision(18, 3)` — it is a quantity field, not a money field |
| RULE-213 | `ProductPriceHistory` MUST have a dedicated `IEntityTypeConfiguration<ProductPriceHistory>` with explicit `HasMaxLength` on string fields |

### 2.50 FK Delete Behavior Enforcement (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-214 | ALL foreign keys MUST use `DeleteBehavior.Restrict` — absolute ZERO exceptions for Cascade delete |
| RULE-215 | `ProductUnitConfiguration`, `UnitBarcodeConfiguration`, `ProductBarcodeConfiguration` MUST change `OnDelete(DeleteBehavior.Cascade)` to `OnDelete(DeleteBehavior.Restrict)` |
| RULE-216 | `UnitBarcodeConfiguration` MUST add `.HasQueryFilter(x => x.IsActive)` to match `ProductBarcodeConfiguration` pattern |

### 2.51 Response DTO Integrity (v4.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-217 | `ProductResponse`, `CustomerResponse`, `SupplierResponse` DTOs MUST NOT have a `Code` field — use auto-increment `Id` as sole identifier |
| RULE-218 | `DuplicateCode` error constant is REMOVED from `ErrorCodes` — all references replaced with `DuplicateBarcode` or context-specific error codes |
| RULE-219 | WarehouseService, UnitService, CategoryService MUST NOT return `DuplicateCode` — use appropriate error constants |

### 2.52 Newest-First Sorting (v4.6.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-220 | ALL list ViewModels MUST display newest records first — sort by `Id` descending for entities with auto-increment PK, or by date descending for entities with date fields |
| RULE-221 | Use `.OrderByDescending(x => x.Id)` when populating `ObservableCollection` from API results — NEVER rely on API return order alone |
| RULE-222 | Invoice lists MUST sort by `InvoiceDate` descending (newest invoice first) |
| RULE-223 | Payment lists MUST sort by `Id` descending (newest payment first) |

### 2.53 Dialog Window Owner Safety (v4.6.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-224 | ALL dialog `PositionOverOwner()` methods MUST check `mainWindow != this` before setting `Owner` property — prevents "Cannot set Owner property to itself" error |
| RULE-225 | When `MainWindow` is null or equals `this`, fall back to `WindowStartupLocation.CenterScreen` — NEVER crash |
| RULE-226 | NEVER call `System.Windows.Application.Current.MainWindow` before it has been explicitly set in `Application_Startup` — the first Window created by WPF auto-becomes MainWindow |

### 2.54 WPF Validation ErrorTemplate & ValidateAllAsync (v4.6.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-227 | ALL editor ViewModels MUST call `SetDialogService()` in every constructor to enable `ValidateAllAsync()` from ViewModelBase |
| RULE-228 | Use `INotifyDataErrorInfo` (`AddError`/`ClearErrors`) in property setters for real-time validation — NEVER use parallel `HasXxxError` boolean + computed string properties |
| RULE-229 | Pre-save validation MUST call `ClearAllErrors()` then `AddError()` for each field, then `await ValidateAllAsync()` from ViewModelBase — this shows the styled validation warning dialog automatically |
| RULE-230 | The `Validation.ErrorTemplate` in `Styles.xaml` MUST render a red border + ❗ icon badge with `ToolTip` bound to `[0].ErrorContent` — applies to TextBox, PasswordBox, and ComboBox |

### 2.55 Sound Service (ISoundService) & Transaction Feedback (v4.6.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-231 | `ISoundService` handles system-wide audio feedback via native Windows sounds (`PlaySuccessSound()`, `PlayErrorSound()`, `PlayWarningSound()`) |
| RULE-232 | Audio feedback MUST fire on: successful barcode scans, quantity adjustments, pre-save validation dialogs, and save/post success events |

**ISoundService Interface:**
```csharp
public interface ISoundService
{
    void PlaySuccess();
    void PlayError();
    void PlayWarning();
}
```

### 2.56 Barcode Scanning (Continuous Input) & Event-Driven Selection (v4.6.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-233 | Continuous scanning MUST allow scanning product barcodes consecutively without closing the editor window or manually focusing the search field |
| RULE-234 | `BarcodeLookupService` identifies products by barcode and automatically raises selection events marshaled to the UI thread via `Application.Current.Dispatcher` |

### 2.57 Auto-Update System & DPAPI Security Integration (v4.6.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-235 | `IUpdaterService` handles background update checking, utilizing `%AppData%\SalesSystem\settings.json` to store skipped versions |
| RULE-236 | DPAPI encryption via `IConnectionStringProtector` must be used for database connection strings to secure them at rest, applying `"DPAPI:"` prefix to raw values |
| RULE-237 | `FirstRunSetupService` performs atomic write of encrypted settings on application startup via `.tmp` -> `File.Replace()` pattern |

### 2.58 WPF ViewModels Code Quality Standard (v4.6.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-238 | ViewModels MUST NOT declare `async void` for operations that can throw exceptions, unless they are standard event handlers or ICommand execute wraps. All other async operations must return `Task` and be executed within `ExecuteAsync` wrappers |
| RULE-239 | Derived list ViewModels MUST NOT shadow base class helper properties (such as `DialogService`) to avoid compile warnings and null reference issues. Use `SetDialogService()` instead |

### 2.59 Rate Limiting & Brute-Force Protection (v4.6.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-240 | ALL login endpoints MUST use `[EnableRateLimiting("LoginPolicy")]` — 5 attempts per 15 minutes per IP |
| RULE-241 | Global rate limit of 100 requests per minute per IP for all unauthenticated requests |
| RULE-242 | Rate limit exceeded responses MUST return HTTP 429 with Arabic message and `RATE_LIMIT_EXCEEDED` code |
| RULE-243 | Rate limiter middleware MUST be placed BEFORE `UseAuthentication()` in the middleware pipeline |

### 2.60 User Hard-Delete Protection (v4.6.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-244 | `UserService.PermanentDeleteAsync()` MUST return `Result.Failure` — never hard-delete users |
| RULE-245 | Any attempt to hard-delete a user MUST be logged as a Serilog warning |
| RULE-246 | Users can only be soft-deleted (deactivated) via `DeleteAsync()` which sets `IsActive = false` |

### 2.61 Connection String Security (v4.6.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-247 | `appsettings.Development.json` MUST NOT contain plaintext connection strings — use `SALESSYSTEM_DB_CONNECTION` env var |
| RULE-248 | All connection string values in config files MUST be empty strings with a `_comment` property explaining env var usage |

### 2.62 Garbled Arabic Text Prevention (v4.6.4)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-249 | ALL Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text — NEVER paste Arabic text through non-UTF-8 terminals or editors that re-encode characters |
| RULE-250 | Before committing any C# file containing Arabic strings, verify the file is saved with UTF-8 encoding (BOM recommended) — use `file --mime-encoding` or editor's "Save with Encoding" feature |
| RULE-251 | If viewing a file shows garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file was saved with the wrong encoding — rewrite ALL string literals from scratch with correct Arabic characters |
| RULE-252 | Editor ViewModels and any file with user-facing messages MUST be checked for Arabic encoding integrity at code review time |
| RULE-253 | When reviewing PRs, spot-check 3-5 Arabic string literals by reading them aloud in the diff — if any look like `ط§ط®طھط¨ط§ط±` instead of `اختبار`, flag the entire file for encoding review |

### 2.63 Invoice Number Strategy — InvoiceNo as int, UNIQUE, Thread-Safe via DocumentSequenceService (v4.6.7)

SalesInvoice and PurchaseInvoice have an `InvoiceNo` (int) property — a user-facing invoice number. This is separate from the auto-increment `Id` PK. The InvoiceNo MUST be UNIQUE per document type (SalesInvoice, PurchaseInvoice) and is generated thread-safely via `DocumentSequenceService` using `SemaphoreSlim` lock.

| RULE | DIRECTIVE |
|------|-----------|
| RULE-254 | `SalesInvoice` and `PurchaseInvoice` MUST have `int InvoiceNo` (NOT string) — required property with guard `invoiceNo <= 0` |
| RULE-255 | Default InvoiceNo: service calls `IDocumentSequenceService.GetNextIntAsync("SalesInvoice"\|"PurchaseInvoice", ct)` — thread-safe via `SemaphoreSlim` + DB sequence. NEVER use `lastId + 1` (not thread-safe). |
| RULE-256 | Request DTOs use `int? InvoiceNo` — null or ≤ 0 means "auto-generate" via DocumentSequenceService. User override validated for uniqueness. |
| RULE-257 | Search by invoice number: Desktop shows `InvoiceNo` column, search by int comparison |
| RULE-258 | `SupplierInvoiceNo` (string?) is a SUPPLIER's invoice reference number — NOT the system InvoiceNo. Kept on `PurchaseInvoice` for supplier reference only |
| RULE-259 | `SalesReportDto` and `PurchaseReportDto` use `int InvoiceNo` |
| RULE-260 | `InvoicePrintDto` (print/PDF generation) uses `string InvoiceNumber` formatted from `InvoiceNo.ToString()` |
| RULE-261 | InvoiceNo MUST have a UNIQUE index per document type (e.g., unique per SalesInvoice table, unique per PurchaseInvoice table). No duplicates allowed — analysis confirms duplicate invoice numbers cause confusion in search, returns, reports, and customer service. |

### 2.64 UI Compacting — Mobile-Ready Density (v4.6.6)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-262 | XAML MUST NOT have hardcoded `Height="36"` or `Height="40"` on Button/TextBox/ComboBox elements — style defaults now provide compact 28px height |
| RULE-263 | XAML MUST NOT have hardcoded `Padding="16,0"` or `Padding="24,0"` or `Padding="20,0"` on buttons — style defaults now handle padding at 10,4 |
| RULE-264 | XAML header/footer borders MUST use `Padding="12,6"` for headers and `Padding="12,8"` for footers — NEVER `Padding="16,12"` or `Padding="20,12"` or larger |
| RULE-265 | XAML form section margins between fields MUST be `Margin="0,0,0,6"` or `Margin="0,0,0,8"` — NEVER `Margin="0,0,0,12"` or `Margin="0,0,0,16"` |
| RULE-266 | XAML dialog title font sizes MUST be `FontSize="16"` — NEVER `FontSize="20"` or `FontSize="22"` |
| RULE-267 | XAML section header font sizes MUST be `FontSize="14"` — NEVER `FontSize="18"` or `FontSize="20"` |
| RULE-268 | XAML empty-state button margins MUST be `Margin="0,12,0,0"` with `Width="140"` — NEVER `Margin="0,20,0,0"` with `Width="160"` or larger |
| RULE-269 | XAML MainWindow sidebar width MUST be `Width="200"` — NEVER `Width="220"` or `Width="240"` |
| RULE-270 | XAML dialog icon border sizes MUST be `Width="44" Height="44"` with `FontSize="20"` — NEVER `50×50` with `FontSize="24"` |
| RULE-271 | XAML ScreenWindow MUST use `MinWidth="500" MinHeight="350"` — NEVER `MinWidth="600" MinHeight="400"` |
| RULE-272 | XAML dialog button widths MUST use `MinWidth` (80-100) — NEVER fixed `Width="120"` or `Width="130"` |
| RULE-273 | ALL XAML views MUST remove hardcoded `Height` and `Padding` that duplicate style defaults — let Styles.xaml be the single source for button/input sizes |
| RULE-274 | When adding new views, use the compact global styles from `Styles.xaml` — NEVER add custom oversized heights or padding |

### 2.65 Transaction Strategy & EF Core Configuration (v4.6.8)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-275 | NEVER use `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()` when `SqlServerRetryingExecutionStrategy` is configured — the execution strategy does not support user-initiated transactions. Use `IUnitOfWork.ExecuteTransactionAsync()` (which wraps `CreateExecutionStrategy().ExecuteAsync()` with an explicit transaction) or a single `SaveChangesAsync()` call (EF Core wraps it in an implicit transaction) instead. |
| RULE-276 | Use `IUnitOfWork.ExecuteTransactionAsync(Func<Task> operation, CancellationToken ct)` for atomic multi-save operations — this wraps `DbContext.Database.CreateExecutionStrategy().ExecuteAsync()` with an explicit transaction inside the delegate. NEVER use `BeginTransactionAsync()` directly when retrying execution strategy is configured. |
| RULE-277 | `ExchangeRate` on `CustomerPayment` and `SupplierPayment` MUST use `.HasPrecision(18, 2)` — NEVER leave exchange rate precision unspecified (defaults to truncation risk). |
| RULE-278 | `JournalEntry` to `JournalEntryLine` relationship MUST use `.WithOne(x => x.JournalEntry)` specifying the navigation property — NEVER bare `.WithOne()` (creates shadow FK `JournalEntryId1`). |
| RULE-279 | Editor ViewModels MUST follow the `CashBoxEditorViewModel` pattern: parameterless constructor delegating to parameterized constructor, `ValidateAsync()` calling `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()`, and `IToastNotificationService` for success feedback. |
| RULE-280 | `LogSystemError()` is reserved for SYSTEM errors only (DB failures, API unreachable, JSON parse crashes) — NEVER call it for API business validation errors (e.g., duplicate name/code). Use `HandleFailure()` alone, which logs at Warning level per RULE-183. |

### 2.66 Phase 18 & Phase 20 Code Review Remediations (v4.6.8)

All bugs from the Phase 18 accounting review (`docs/phase18_accounting_review.md`) and Phase 20 currencies review (`docs/currencies_module_review.md`) have been fixed in this session. The following rules codify the remediations enforced by those fixes:

| RULE | DIRECTIVE |
|------|-----------|
| RULE-281 | `IUnitOfWork` MUST have `ExecuteTransactionAsync(Func<Task> operation, CancellationToken ct)` method for atomic multi-save operations using `CreateExecutionStrategy().ExecuteAsync()` with an explicit `BeginTransactionAsync()` / `CommitAsync()` inside the delegate — NEVER perform two consecutive `SaveChangesAsync()` calls without a wrapping transaction. |
| RULE-282 | `JournalEntryLineConfiguration` MUST have `CHK_DebitOrCredit` (exactly one of Debit/Credit is non-zero, or both zero) and `CHK_NoNegativeValues` (Debit >= 0 AND Credit >= 0) CHECK constraints via `builder.ToTable(t => t.HasCheckConstraint(...))`. |
| RULE-283 | All enum properties in EF Core entity configurations MUST use `.HasConversion<int>()` explicitly — NEVER rely on EF Core convention alone (applies to `Account.AccountType`, `JournalEntry.EntryType`, and all other enum properties). |
| RULE-284 | `SystemAccountMappings` navigation properties MUST be mapped with navigation lambda (e.g., `HasOne(x => x.DefaultCashAccount)`) — NEVER use bare `HasOne<Account>()` which leaves the nav property unmapped. |
| RULE-285 | `JournalEntryLine.Account` navigation property MUST use `HasOne(x => x.Account)` with lambda — NEVER `HasOne<Account>()` without lambda (creates shadow FK or unmapped navigation). |
| RULE-286 | `Currency.Create()` factory method MUST accept an `isSystem` parameter (default `false`) and assign it to `IsSystem` — NEVER hardcode `IsSystem = false` (breaks seed data protection for system currencies). |
| RULE-287 | Filtered unique index on `Currency.IsBaseCurrency` MUST include `AND [IsActive] = 1` in the filter — a soft-deleted base currency must not prevent setting a new base currency. |
| RULE-288 | Controller endpoints MUST return `404 NotFound` when the service returns `ErrorCodes.NotFound` (entity not found) and `400 BadRequest` for business validation errors — NEVER always return `BadRequest` for all failure types. |
| RULE-289 | `CurrenciesListViewModel` (and all list ViewModels with EventBus subscriptions) MUST implement `IDisposable` and call `Cleanup()` (which disposes EventBus subscriptions) in `Dispose()` — use `IToastNotificationService.ShowSuccess()` for minor success messages, not modal dialogs. |
| RULE-290 | `JournalEntryNumberGenerator` MUST query by today's date prefix (e.g., `EntryNumber.StartsWith("JE-20260606")`) for correct daily sequence reset — NEVER query the last entry globally by `Id` (breaks daily reset on quiet days). |

### 2.67 Phase 19 — Settings Module: Code Review Remediations (v4.6.9)

All bugs from the Phase 19 settings review (`docs/phase19_settings_review.md`) have been fixed in this session. The following rules codify the remediations:

| RULE | DIRECTIVE |
|------|-----------|
| RULE-291 | `SetBatchSystemSettingsAsync()` MUST NOT call `SaveChangesAsync()` directly — the repository only prepares entities, the caller (service layer) commits via `IUnitOfWork.SaveChangesAsync()`. This follows RULE-024 (repository NEVER owns transaction commit). |
| RULE-292 | ALL Domain entity `Update()` methods MUST call `UpdateTimestamp()` at the end — NEVER leave `UpdatedAt` null after modifications (applies to `Tax.Update()`, `StoreSettings.Update()`, and all future entities). |
| RULE-293 | `SystemSetting.Create()` MUST validate `Category` (not empty) and `DataType` (must be one of: string, int, bool, decimal) via guard clauses — NEVER allow invalid data types or empty categories to pass through. |
| RULE-294 | Filtered unique indexes on soft-deletable entities MUST include `AND [IsActive] = 1` in the filter — a soft-deleted record must not prevent creating a new record with the same unique value (applies to `Tax.IsDefault`, `Currency.IsBaseCurrency`, and all future filtered indexes). |
| RULE-295 | `SystemSettingsRepository.SetStringAsync()` MUST accept a `category` parameter (default `null` → `"General"`) for auto-created settings — NEVER hardcode `category: "Print"` which miscategorizes non-Print settings. |
| RULE-296 | `DbSeeder` MUST seed ALL 25+ system settings from the plan specification — missing settings must be flagged and added during code review. Current seed count: 29 settings across Inventory (4), Sales (8), Purchases (3), Barcode (3), Accounting (1), Print (5), Notifications (4), General (3). |
| RULE-297 | `StoreSettings` seed data MUST pass `defaultTaxRate: 0m` (deprecated — Tax entity is source of truth) — NEVER seed with `15m` which contradicts the deprecation strategy. |

### 2.68 Phase 20 — Currencies Module: BUG-008 Fix & Base Currency Immutability (v4.6.9)

This rule codifies the last remaining remediation from the Phase 20 currencies review (`docs/phase20_currencies_review.md`). Earlier Phase 20 remediations (RULE-281 through RULE-290) were applied in v4.6.8 and are documented in §2.66. This section documents the BUG-008 fix and base currency immutability rules applied in v4.6.9.

| RULE | DIRECTIVE |
|------|-----------|
| RULE-298 | `Currency.Create()` MUST validate that `code.Trim().Length == 3` — ISO 4217 currency codes are always exactly 3 characters. NEVER use a generic max-length validation like `Length > 10`. |
| RULE-471 | `Currency.IsBaseCurrency` MUST be IMMUTABLE after creation — NEVER allow user to change the base currency once the system is initialized. Base currency is determined at setup and locked for the system lifetime. |
| RULE-472 | `FractionName` MUST be stored on Currency entity (e.g., "فلس" for YER, "Cent" for USD) — enables proper display of sub-currency units in invoices and reports. |
| RULE-473 | `IsSystem` flag MUST protect system-seeded currencies (YER, USD) from deletion or deactivation — user-created currencies can be soft-deleted. `Currency.Create()` accepts `bool isSystem = false` parameter. |
| RULE-474 | Filtered unique index on `Currency.IsBaseCurrency` MUST include `AND [IsActive] = 1` in the filter — a soft-deleted base currency must not prevent setting a new base currency. |

### 2.69 Phase 19 — Settings Module: Enhancement Remediations (v4.6.9)

Following the Phase 19 code review, 8 enhancements were identified. The following rules codify the remediations (NOT bug fixes, which were already in §2.67):

| RULE | DIRECTIVE |
|------|-----------|
| RULE-299 | `Tax` entity MUST have `SetDefault()` and `ClearDefault()` domain methods that set `IsDefault` and call `UpdateTimestamp()` — NEVER set `IsDefault` directly from service code. |
| RULE-300 | `SystemSettingsViewModel` MUST expose ALL seeded system settings as strongly-typed properties — fields added in v4.6.9: `HideTaxInSales`, `ShowExpiryInInvoices`, `HideTaxInPurchases`, `ShowLogo`, `FooterNote` (Print), `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` (Notifications), `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber` (Print settings). |
| RULE-301 | `StoreSettingsService.UpdateSystemSettingsAsync()` MUST validate known system setting keys by type before saving — boolean keys checked via `bool.TryParse`, integer keys via `int.TryParse` with range validation (CostingMethod 1-3, DecimalPlaces 0-6, StockAlertDays 1-365). |

### 2.70 Phase 20 — Currencies Module: Enhancement Remediations (v4.6.9)

Following the Phase 20 currencies review, 3 additional enhancements were applied in v4.6.9:

| RULE | DIRECTIVE |
|------|-----------|
| RULE-302 | Opening balance is a Journal Entry (Dr Cash Account / Cr OpeningBalanceEquity) — NEVER store OpeningBalance or CurrentBalance on CashBox entity. Balance is tracked on the linked GL Account. |
| RULE-303 | `Currency` entity MUST have `SetAsBaseCurrency()` and `UnsetBaseCurrency()` domain methods that set `IsBaseCurrency` and call `UpdateTimestamp()` — NEVER toggle `IsBaseCurrency` directly from service code. These methods are SYSTEM-ONLY (used during seeding/setup) — NEVER user-callable after initialization. |
| RULE-304 | `InvokeOnUIThreadAsync` callbacks MUST NOT use `async` keyword when the lambda contains no `await` — unnecessary `async` creates a fire-and-forget `Task` inside the dispatcher. |

### 2.71 Phase 21 — Users & Permissions Module Rules (v4.10.4 — Schema-Aligned, DB-Driven Roles)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-305 | `User.Create()` MUST use passwordless creation — `PasswordHash = null`, `MustChangePassword = true`. NEVER accept or hash a password in the factory method. |
| RULE-306 | User entity uses `IsActive` (bool from ActivatableEntity) + `IsLocked` (bool) — NOT `UserStatus` enum. `IsActive` controls soft-delete, `IsLocked` controls lockout. |
| RULE-307 | `RecordLoginAttempt()` MUST be used for ALL login attempts — success resets counter to 0, failure increments; at 5 failures sets `IsLocked = true`. |
| RULE-308 | `UserService.CreateAsync()` MUST accept `request.Password` if provided (hash with BCrypt work factor 12), or fall back to default "12345678". NEVER hardcode password hashing. |
| RULE-309 | `Permission` entity MUST use `IsSystem` flag to protect system permissions — `IsSystem = true` permissions MUST NOT be deletable or modifiable. |
| RULE-310 | `AuditLog` entity MUST use `long Id` (bigint) for high-volume audit tables — NEVER `int`. |
| RULE-311 | `AuditLog` MUST have indexes on `(UserId, CreatedAt DESC)`, `(EntityType, EntityId)`, and `(CreatedAt DESC)` for query performance. |
| RULE-312 | ALL entities (Permission, RolePermission, AuditLog, UserSession) MUST use `DeleteBehavior.Restrict` on ALL foreign keys. |
| RULE-313 | Login flow MUST check `MustChangePassword` before password verification — if true, return `RequiresPasswordSetup` error and redirect to password change screen. |
| RULE-314 | `AuthService.ChangePasswordAsync()` MUST verify current password via `BCrypt.Verify` before allowing change — NEVER skip current password validation. |
| RULE-315 | Every login success/failure MUST create an `AuditLog` entry — `LoginSuccess`, `LoginFailed` (with attempt count), `LoginBlocked_Locked`. |
| RULE-316 | `PermissionService.UpdateRolePermissionsAsync()` MUST use `_uow.ExecuteTransactionAsync()` for atomic role-permission updates — NEVER direct `SaveChangesAsync()` for remove+add operations. |
| RULE-317 | Desktop UI permission filtering MUST use API-based permission checks (`CurrentUserDto.Permissions`) — NEVER hardcoded role-to-permission mappings in XAML. |
| RULE-318 | Roles are DB-driven via `Role` entity + `UserRole` join table — the `UserRole` enum (`Domain/Enums/UserRole.cs` and `Contracts/Enums/CoreEnums.cs`) MUST NOT exist. Use `byte` role IDs throughout. |
| RULE-319 | `DbSeeder` MUST seed 45 permissions across 12 categories with 9-role assignments: Admin (ALL 45), Manager (43, excluding System.Settings/Users), Accountant (accounting+reports), Treasurer (cashbox+banking), Cashier (sales+customers), Warehouse Supervisor (inventory+products), Sales Employee (sales+customers), Observer (view-only), Branch Manager (branch-scoped). |
| RULE-320 | Default admin user MUST be seeded passwordless (`PasswordHash = null`, `MustChangePassword = true`) — NEVER seed with a hardcoded password hash. Password set on first login via `SetInitialPassword()`. |
| RULE-321 | `UserService.PermanentDeleteAsync()` MUST return `Result.Failure` — NEVER hard-delete users (soft-delete only via `IsActive = false`). |
| RULE-322 | Desktop screens already exist for: PasswordChangeView (first-login password change), AuditLogListView (audit log browser), PermissionManagementView (admin role-permission grid). These are NOT missing — the plan was already implemented. |

### 2.72 Phase 22 — Chart of Accounts Module Rules (v4.6.9+)

### 2.73 Phase 22 — Code Review Bug Fix Rules (v4.6.9+)

The Chart of Accounts module introduces a 4-level hierarchy (Group→Main→Sub→Detail) for the `Account` entity, with 74 seeded accounts, CRUD API, and Desktop tree/grid dual-mode UI. The following rules codify Phase 22 design decisions:

| RULE | DIRECTIVE |
|------|-----------|
| RULE-321 | `Account` entity MUST have `Level` (int, 1-10) property with DB CHECK constraint `CHK_Account_Level_Range [Level] >= 1 AND [Level] <= 10` — NEVER allow Level outside valid range. |
| RULE-322 | `Account.Create()` MUST accept exactly 13 parameters (accountCode, nameAr, nameEn, nature, isLeaf, parentId, isSystem, categoryId, level, description, colorCode, notes, createdByUserId) — NEVER omit the `level` parameter (required from Phase 22 onward). AccountCode is system-generated via hierarchical numbering, ColorCode is auto-generated from Nature. |
| RULE-323 | `Account.MarkAsDeleted()` MUST guard against system accounts (`IsSystemAccount` → `DomainException`) AND parent accounts with children (`HasChildren()` → `DomainException`) — NEVER allow deletion of system or parent accounts. |
| RULE-324 | `Account.Update()` MUST guard against modifying system accounts (`IsSystemAccount` → `DomainException`) — system accounts are read-only after seeding. |
| RULE-325 | `AccountingSeeder` MUST use a two-pass approach: create all Level 1 accounts → `SaveChangesAsync` → query IDs → create Level 2 with `ParentAccountId` → `SaveChangesAsync` → Level 3 → Level 4 — NEVER create accounts with unresolved parent references. |
| RULE-326 | Seeder MUST set `AllowTransactions = false` for levels < 4 (Group/Main/Sub) and `AllowTransactions = true` for level >= 4 (Detail/leaf accounts) — leaf accounts must allow transactions, parent accounts must not. |
| RULE-327 | Seeder MUST set `IsSystemAccount = true` for Level 1 (Group) and Level 2 (Main) accounts only — Level 3 (Sub) and Level 4 (Detail) accounts are NOT system accounts (user-modifiable). |
| RULE-328 | Seeder color codes MUST follow: Asset=`#2196F3` (blue), Liability=`#F44336` (red), Equity/Revenue=`#4CAF50` (green), Expense=`#FF9800` (orange) — color codes enable visual identification in TreeView. |
| RULE-329 | `AccountDto` MUST have computed properties `AccountTypeDisplay` and `LevelDisplay` for UI binding — NEVER require ViewModel to translate byte/int to display string. |
| RULE-330 | `AccountTreeNodeDto` MUST have a recursive `Children` list (`List<AccountTreeNodeDto>`) for TreeView rendering — the service `GetTreeAsync()` builds this from a flat list. |
| RULE-331 | `AccountsController` GET endpoints MUST use `AllStaff` policy, POST/PUT use `ManagerAndAbove`, DELETE soft use `ManagerAndAbove`, DELETE permanent use `AdminOnly` — protect write operations appropriately. |
| RULE-332 | `AccountsListViewModel` (and all list ViewModels with EventBus subscriptions) MUST implement `IDisposable` and call `Cleanup()` (which unsubscribes from EventBus) in `Dispose()` — prevent EventBus memory leaks. |
| RULE-333 | `AccountService.GetTreeAsync()` MUST build a recursive tree from a flat `List<Account>` using `BuildTreeNode()` — NEVER query the database recursively (N+1 problem). |
| RULE-334 | `AccountService.CreateAsync()` MUST validate: parent exists, `request.Level > parent.Level` (child deeper than parent), `request.Level <= 10`, and unique `AccountCode` — return `Result.Failure` with Arabic message on any violation. |
| RULE-335 | `AccountService.PermanentDeleteAsync()` MUST catch `DbUpdateException` (reference integrity) and return `Result.Failure` with Arabic message — NEVER let FK exception crash the API. |
| RULE-336 | Seeder MUST seed exactly 74 accounts: 5 Level 1 (Groups), 8 Level 2 (Main), 24 Level 3 (Sub), 37 Level 4 (Detail) — the hierarchical structure must match the plan in `docs/Phase 22 — Chart of Accounts Module Implementation Plan.md`. |
| RULE-337 | `SystemAccountMappings` in `AccountingSeeder` MUST be updated with the new account IDs after the two-pass seed — maps cash, bank, AR, AP, VAT, capital, sales, COGS, etc. for journal entry services. |
| RULE-338 | `AccountEditorViewModel.ValidateAsync()` MUST follow RULE-229: `ClearAllErrors()` → `AddError()` for each field → `await ValidateAllAsync()` (which shows the styled validation warning dialog automatically) — NEVER call `ShowValidationErrorsAsync` manually. |
| RULE-339 | `AccountDto.AccountTypeDisplay` MUST use the `AccountType` enum (not hardcoded byte values) for compile-time safety — cast byte to `AccountType` and switch on enum members. |
| RULE-340 | Desktop Accounts View MUST provide dual-mode display (TreeView with `HierarchicalDataTemplate` + DataGrid flat view) toggleable by the user — accountants prefer tree, casual users prefer grid. |

### 2.74 Phase 22 — Code Review Bug Fix Rules (v4.6.9+)

The following rules codify the bugs found and fixed during Phase 22 code review. These rules prevent regression and ensure all Phase 22 modules (Chart of Accounts) maintain consistency across Domain, Service, API, and Desktop layers.

| RULE | DIRECTIVE |
|------|-----------|
| RULE-341 | `HasChildren()` domain guard on `Account.MarkAsDeleted()` is DEFENSE-IN-DEPTH only — the `_subAccounts` list is NEVER loaded by EF Core. Service-level `DeleteAsync()`/`PermanentDeleteAsync()` MUST use `AnyAsync(a => a.ParentAccountId == id)` BEFORE calling `MarkAsDeleted()`. NEVER rely on the domain guard alone to protect parent accounts. |
| RULE-342 | `AccountService.DeleteAsync()` MUST NOT fetch the entity twice — call `await _uow.Accounts.GetByIdAsync(id, ct)` once, then call `account.MarkAsDeleted()` on the loaded entity. `PermanentDeleteAsync()` MUST use `DeleteRange(new[] { account })` on the already-loaded entity. NEVER call `GetByIdAsync` a second time within the same operation. |
| RULE-343 | All new entities with an `Explanation` field MUST add it to: Domain entity (nullable `string?`), EF config (`nvarchar(500)`), DTO (`AccountDto`, `AccountTreeNodeDto`), Create/Update Request DTOs, Service mapping (`MapToDto()`), Validator (`MaxLength(500)`), and Seeder (Arabic text for seed records). The `Explanation` field MUST be a `string?` nullable — NEVER `string` (non-nullable) as it's optional user input. |
| RULE-344 | AccountCode MUST follow a hierarchical expanding numbering scheme: Level 1 (Groups) = 1 digit, Level 2 (Main) = 2 digits, Level 3 (Sub) = 4 digits, Level 4 (Detail) = 8 digits — enforced in `CreateAccountRequestValidator` with `MustMatchHierarchicalCodeLength(level)`. NEVER allow codes that violate the hierarchical scheme (breaks financial reporting standards). |
| RULE-345 | ALL controller route parameters MUST use built-in ASP.NET Core route constraints only — `:byte` is NOT a supported route constraint and will cause `InvalidOperationException` (HTTP 500). Use `:int`, `:int:min(1):max(N)`, `:guid`, or `:string` instead. NEVER use `:byte`, `:sbyte`, `:short`, `:ushort`, `:uint`, or `:ulong` as route constraints. |
| RULE-346 | All Update Validators MUST have the same field validations as their corresponding Create Validators — `NameAr` required + Arabic error message, `NameEn` with `MaxLength`, `ColorCode` with hex format validation (`Regex("^#[0-9A-Fa-f]{6}$")`). NEVER create an Update validator that is less strict than the Create validator. |
| RULE-347 | All seeded accounts in `AccountingSeeder` MUST have an Arabic `explanation` text describing the account's purpose — NEVER leave `explanation` as `null` for seeded accounts. The explanation helps users understand the chart of accounts hierarchy and account purpose without external documentation. |
| RULE-348 | Account editor ViewModels in Desktop MUST support edit mode — when opening an existing account, the VM must load the account data via API, populate all fields, and set `AccountCode` as read-only (`IsAccountCodeReadOnly = true`). NEVER allow changing the account code on an existing account (breaks integrity of chart of accounts references). |
| RULE-349 | List ViewModels for Chart of Accounts MUST implement `EditSelectedAccountCommand` and `DeleteSelectedAccountCommand` with ToolBar/ToolBarTray buttons — open editor non-modally for edit, use `IDialogService.ShowDeleteConfirmationAsync` for delete. Search/filter MUST work in BOTH TreeView mode and DataGrid mode. |
| RULE-350 | The `AccountsController` route `{type:int:min(1):max(5)}` MUST use the correct `AccountType` enum range — AccountType enum values are 1-5 (Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5). NEVER hardcode a different range that doesn't match the enum. |
| RULE-351 | `nameof` operator MUST be used for validator property names instead of string literals — e.g., `RuleFor(x => x.NameAr)` NOT `RuleFor("NameAr")`. String literals for property names cause silent validator failures when properties are renamed. |
| RULE-352 | Desktop health check MUST use `SecureDbContextFactory.GetDecryptedConnectionString()` as the single source of truth for connection strings — NEVER inject raw `IConfiguration` into health check services. The health check bypass must still go through DPAPI decryption to avoid false "connection refused" errors. |
| RULE-353 | AccountCode MUST be system-generated via `AccountCodeGeneratorService` using hierarchical expanding numbering (1-2-4-8 digits per level) — NEVER accept user-supplied AccountCode. The generator MUST use `SemaphoreSlim` for thread safety and auto-increment within the parent account's code range. |

| RULE | DIRECTIVE |
|------|-----------|
| RULE-475 | `SalesService.PostAsync()` MUST enforce `PreventBelowRetailPrice` setting — before posting, look up `ProductPrices` for each item and reject if `item.UnitPrice < effectivePrice`. Return `Result.Failure("سعر البيع أقل من السعر الرسمي للمنتج: {name}")`. |
| RULE-476 | `SalesService.PostAsync()` MUST enforce `AllowBelowCostSale` setting — if disabled, compare each item's `UnitPrice` against `CostInBaseCurrency` and reject if below cost. Return `Result.Failure("سعر البيع أقل من تكلفة المنتج: {name}")`. |
| RULE-477 | `IProductPriceService` MUST be injected into `SalesService` for price lookups — use `GetEffectivePriceForInvoiceAsync()` to find the registered price for a ProductUnitId + CurrencyId combination. |
| RULE-478 | Price override within invoice (`IsPriceOverridden`) MUST NOT change the base price in `ProductPrices` table — override is recorded only on the invoice line, not propagated to catalog. Discount is the CORRECT way to give price reductions, not price modification below default. |
| RULE-479 | `Desktop InvoiceLineViewModel.GetDefaultPrice()` MUST NOT return `0m` stub — use actual price lookup from API (ProductPrices or ProductDto price field) to enable correct `IsPriceOverridden` detection. |

### 2.87 Phase 27 — Purchase Invoice OtherCharges (Landed Cost) Rules (v4.10.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-480 | `PurchaseInvoice` entity MUST have `OtherCharges` property (decimal) with guard clause `otherCharges < 0` — included in `RecalculateTotals()` as `NetTotal = SubTotal - DiscountAmount + TaxAmount + OtherCharges`. |
| RULE-481 | `AllocateAdditionalCharges()` MUST be called in `PurchaseService.PostAsync()` — distribute `OtherCharges` proportionally by line total: `lineShare = (LineTotal / SubTotal) × OtherCharges`, then `landedUnitCost = UnitCost + (lineShare / Quantity)`. |
| RULE-482 | Inventory stock increase and batch creation MUST use `landedUnitCost` (unit cost plus distributed other charges share) — NEVER use raw `UnitCost` alone when `OtherCharges > 0`. |
| RULE-483 | `CreatePurchaseInvoiceRequest`, `UpdatePurchaseInvoiceRequest`, and `PurchaseInvoiceDto` MUST include `OtherCharges` field (decimal) — mirroring `SalesInvoice` which already has it. |
| RULE-484 | `PurchaseInvoiceConfiguration` MUST map `.Property(pi => pi.OtherCharges).HasPrecision(18, 2)` — the DB column already exists from `InitialCreate` migration. |
| RULE-485 | Desktop `PurchaseInvoiceEditorViewModel` MUST have `OtherCharges` property included in `RecalculateTotals()`, `BuildRequest()`, `BuildUpdateRequest()`, and `LoadInvoiceAsync()` — XAML must show "مصاريف إضافية" input field. |
| RULE-486 | `AccountingIntegrationService.CreatePurchasePostEntryAsync()` MUST use `SubTotal - DiscountAmount + OtherCharges` as `netInventoryCost` for the Dr Inventory line — NOT just `SubTotal - DiscountAmount`. |

### 2.88 Phase 27 — Purchase Return Standalone Mode Rules (v4.10.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-487 | Purchase Return MUST support BOTH linked-to-invoice (`PurchaseInvoiceId` set) AND standalone mode (`PurchaseInvoiceId = null`) — Desktop editor MUST NOT block standalone returns with "يرجى اختيار فاتورة" validation when supplier is selected and items are entered. |
| RULE-488 | `PurchaseReturnService.PostAsync()` MUST create journal entries via `CreatePurchaseReturnEntryAsync()` — Dr Supplier Account / Cr PurchaseReturnAccount — with per-entity account routing (`Supplier.Party.AccountId` fallback to `AccountsPayable`). |
| RULE-489 | `PurchaseReturn.Post()` and `Cancel()` MUST set `PostedAt = DateTime.UtcNow` and `CancelledAt = DateTime.UtcNow` respectively — these were missing from `DocumentEntity` lifecycle. |
| RULE-490 | `GET /api/v1/purchase-returns/returned-quantities/{invoiceId:int}` endpoint MUST exist for querying previously returned quantities — returns `Dictionary<ProductId, TotalReturnedQuantity>` from posted returns linked to the invoice. |
| RULE-491 | `PurchaseReturnEditorViewModel` MUST NOT hardcode `ProductUnitId: 1` — use the actual `ProductUnitId` from the return line item's invoice item or input. |
| RULE-492 | `AccountingIntegrationService` MUST have `ReversePurchaseReturnEntryAsync()` for cancelling posted purchase returns — Dr PurchaseReturnAccount / Cr Supplier Account (reversal of Dr↔Cr). |

### 2.89 Phase 28 — Sales DeliveryChargesRevenue Account Rules (v4.10.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-493 | Sales OtherCharges (delivery, service fees) MUST be credited to a separate revenue account `DeliveryChargesRevenue` — NOT lumped into SalesRevenue. Journal entry must be: Dr Cash/AR = TotalAmount, Cr SalesRevenue = SubTotal - Discount, Cr DeliveryChargesRevenue = OtherCharges, Cr VatOutput = TaxAmount. |
| RULE-494 | `SystemAccountKey` MUST have `DeliveryChargesRevenue = 21` enum value — mapped to account `1533 — إيرادات التوصيل` in `AccountingSeeder` under parent `1530 — إيرادات أخرى`. |
| RULE-495 | `ReverseSalesPostEntryAsync()` MUST mirror the split — Dr SalesRevenue + Dr DeliveryChargesRevenue + Dr VatOutput / Cr Cash/AR — with `DeliveryChargesRevenue` included when `invoice.OtherCharges > 0`. |

### 2.90 Phase 28 — Flexible Input Rules (v4.10.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-496 | Sales and Purchase line items MUST support Flexible Input — user enters ANY TWO of (Quantity, UnitPrice/UnitCost, LineTotal) and the system calculates the third automatically. Use `FlexibleInputCalculator` helper with `CalculationField` enum (Quantity/Price/Total). |
| RULE-497 | LineTotal column in Sales and Purchase DataGrids MUST be editable (NOT `IsReadOnly`) — bind to `LineTotalInput` property. When user edits LineTotal, the system recalculates Quantity or UnitPrice based on which field was `_lastModifiedField`. Use `_isRecalculating` guard flag to prevent infinite recursion. |
| RULE-498 | `InvoiceLineViewModel` and `PurchaseInvoiceLineViewModel` MUST have `LineTotalInput`, `_lastModifiedField`, and `_isRecalculating` fields. `RecalculateFromFlexibleInput()` MUST only call `FlexibleInputCalculator.Calculate()` when `_lastModifiedField == CalculationField.Total` (user explicitly edited LineTotal). When user edits Quantity or UnitPrice/UnitCost, `_lineTotalInput` MUST be recomputed directly as `_quantity * _unitPrice` WITHOUT using the calculator — otherwise the calculator incorrectly treats the auto-computed total as a user-entered anchor and recalculates Price/Quantity instead. |

### 2.91 Phase 23 — Customers Module Rules (v4.6.9+ — Updated: كل عميل = حساب, No Balance on Entity)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-426 | `Customer` entity MUST have MANDATORY `AccountId` (int, non-nullable FK to `Account`) — auto-created by service, NEVER user-supplied in requests. |
| RULE-427 | `CustomerGroup` NOT in V1 — deferred to V2. No CustomerGroup entity, DTO, service, API, or Desktop UI exists in V1 codebase. |
| RULE-428 | `CustomerType` REMOVED from system entirely — Cash/Credit decision is per-invoice via `SalesInvoice.PaymentType`. |
| RULE-429 | `Customer.Create()` MUST accept `accountId` as required parameter (`int`, non-nullable) — `customerGroupId` is NOT accepted. |
| RULE-430 | `Customer.Update()` MUST accept `accountId` as required parameter (`int`, non-nullable) — `customerGroupId` is NOT accepted. |
| RULE-431 | `Customer` entity MUST NOT have `OpeningBalance` or `CurrentBalance` fields — source of truth is the linked GL Account balance. Opening balance is a Journal Entry (Dr AR / Cr OpeningBalanceEquity). |
| RULE-432 | `Customer` entity MUST NOT have `CurrencyId` — currency is per-transaction (invoice/payment), not per-customer. |
| RULE-433 | `Customer.CheckCreditLimit(decimal additionalAmount)` is a NON-THROWING domain method returning `bool` — SOFT WARNING only, caller decides whether to block. |
| RULE-434 | `CustomerDto` MUST include `AccountId` (int, non-nullable) and `AccountName` (string?) — NO `CustomerGroupId`, `CustomerGroupName`, or `CustomerType`. |
| RULE-435 | `CreateCustomerRequest`/`UpdateCustomerRequest` MUST NOT have `AccountId`, `CustomerGroupId`, or `CustomerType` — these are auto-created or removed. |
| RULE-436 | Desktop Customer Editor MUST NOT have CustomerGroup dropdown, AccountId selection combo, or CustomerType radio buttons — AccountName is display-only after save. |
| RULE-437 | Account auto-creation: Service auto-creates Level-4 detail account under parent `"1210 — العملاء"` (Accounts Receivable) when creating a customer. |
| RULE-438 | Seeder MUST NOT seed CustomerGroup — the default customer "عميل نقدي" is created with auto-created account linked to COA. |
| RULE-439 | Phone number validation in customer FluentValidators MUST use regex `^05\d{8}$` with Arabic error message — NEVER only `MaxLength` check. Email MUST use `.EmailAddress()` validation. |
| RULE-440 | `ModernTextBox` style MUST NOT be used on `ComboBox` elements — `ModernTextBox` has `TargetType="TextBox"` and causes `XamlParseException`. Use `ModernComboBox` style instead. |
| RULE-441 | `ComboBox` elements MUST NOT have both `DisplayMemberPath` and `ItemTemplate` set simultaneously — WPF throws `InvalidOperationException`. Use one or the other exclusively. |

### 2.92 Accounts.md Analysis — Bank Auto-Account, Employee Endpoint, & Parent Code Fixes (v4.10.2)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-502 | `Bank` entity MUST follow the same pattern as `CashBox`: `AccountId` is `int?` (nullable), with a `SetAccountId(int)` domain method. When `AccountId` is null at creation, `BankService` MUST auto-create a Level-4 detail account under parent `"1102 — البنوك"` (Bank Accounts) — mirroring `CashBoxService` auto-creation under `"1101 — النقدية صناديق"`. Account code auto-increments from existing child codes. |
| RULE-503 | `EmployeesController` MUST expose `POST /api/v1/employees/{id}/auto-create-account` endpoint that calls `EmployeeService.AutoCreateEmployeeAccountAsync()` — creates a Level-4 detail account under parent `"1170 — عهد الموظفين"` (Employee Custody). This endpoint is needed because custody/advance/lone workflows need accounts created before transaction processing. |
| RULE-504 | `CustomerService.AutoCreateCustomerAccountAsync()` MUST look up parent account by code `"1103"` (Accounts Receivable/العملاء) — NOT `"1210"` (Fixed Assets). `SupplierService.AutoCreateSupplierAccountAsync()` MUST look up parent account by code `"2101"` (Accounts Payable/الموردون) — NOT `"1320"` (doesn't exist). These parent codes were discovered during Accounts.md analysis and are CRITICAL for correct COA linking. |
| RULE-505 | `RecalculateFromFlexibleInput()` in `InvoiceLineViewModel` and `PurchaseInvoiceLineViewModel` MUST ONLY call `FlexibleInputCalculator.Calculate()` when `_lastModifiedField == CalculationField.Total` — when user edited Quantity or UnitPrice/UnitCost, `_lineTotalInput` MUST be recomputed directly as `_quantity * _unitPrice` (or `_quantity * _unitCost`). NEVER pass Quantity or Price as `lastModifiedField` to the calculator because the auto-computed total is treated as a user-entered anchor, causing incorrect recalculation. |
| RULE-506 | `Account.Create()` MUST receive `allowTransactions: true` when `level >= 4` — the domain guard `if (level >= 4 && !allowTransactions)` throws `DomainException("الحساب التفصيلي يجب أن يسمح بالحركات")`. This applies to ALL auto-creation callers: BankService, CashBoxService, CustomerService, SupplierService, EmployeeService, PartyService. NEVER omit `allowTransactions: true` for detail accounts. |
| RULE-507 | EVERY service interface registered in DI MUST have a concrete implementation class. Missing implementations cause `InvalidOperationException` at DI resolution. Search for ALL `services.AddScoped<I, T>` / `services.AddTransient<I, T>` and verify `T` exists as a concrete class. |
| RULE-508 | Report API endpoints MUST match the URLs called by Desktop ViewModels. When adding a report ViewModel, verify the API controller has a matching route. Common mismatches: `detailed-stock-ledger`, `reports/returns`, `reports/aging`. For each `Get*Async()` call in `ReportApiService.cs`, verify the corresponding route exists in the controller. |
| RULE-509 | `SupplierPaymentService.UpdateAsync()` and `CustomerReceiptService.UpdateAsync()` MUST create reversal journal entries when a posted payment's amount changes — (1) reverse the original via `ReverseSupplierPaymentEntryAsync()`/`ReverseCustomerPaymentEntryAsync()`, (2) create new entry for updated amount. Wrap in `ExecuteTransactionAsync()`. Use per-entity account routing with fallback to SystemAccountMappings. |
| RULE-510 | `AccountingIntegrationService` MUST have `CreateSalesReturnEntryAsync()` for standalone sales returns — Dr `SalesReturnsAccount` / Cr `CustomerAccount` (per-entity routing) for return amount, Dr `InventoryAccount` / Cr `COGSAccount` for returned cost. This is separate from `ReverseSalesPostEntryAsync()` which handles full invoice cancellations. |
| RULE-511 | Report services MUST NOT return hardcoded failure stubs. If a report query is complex, implement actual logic using available data entities (ReceiptVoucher, PaymentVoucher, InventoryTransaction, etc.) rather than returning `Result.Failure("تحت التطوير")` or similar stubs. |
| RULE-512 | ALL financial report ViewModels MUST support Excel export via ClosedXML — if a report has PDF export but no Excel export, it is incomplete. Check `AccountStatementViewModel` and all 27 report ViewModels for `ExportExcelCommand`. |
| RULE-506 | `AllowBelowCostSale` MUST default to `true` (allowed) with WARNING-only behavior — per analysis: "أنا أنصح: ✅ السماح. لكن مع تنبيه." When the setting is disabled (`false`), `SalesService.PostAsync()` MUST log a warning via `_logger.LogWarning` but MUST NOT block the sale (return `Result.Failure`). This differs from `PreventBelowRetailPrice` which DOES block. The analysis clearly states: "ولا نمنع البيع" (do not block the sale). |

### 2.93 Inventory Operations Rules (v4.10.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-507 | `InventoryService.CreateTransactionAsync()` MUST auto-generate `TransactionNo` via `_sequenceService.GetNextIntAsync("InventoryTransaction", ct)` when the provided `TransactionNo <= 0`. NEVER require the Desktop to provide a TransactionNo — the service layer handles auto-generation (similar to InvoiceNo pattern in RULE-255). |
| RULE-508 | `InventoryAdjustmentService.PostAsync()` MUST update `WarehouseStocks` per line — Addition=Increase, Deduction=Decrease, Correction=delta(target-current). MUST use `_inventoryService.IncreaseStockAsync()`/`DecreaseStockAsync()` for atomic stock changes with audit trail. MUST inject `IInventoryService` + `IDocumentSequenceService`. |
| RULE-509 | `InventoryCountService.PostAsync()` MUST create a single `InventoryAdjustment` (Correction type per RULE-397) + update stock per line. MUST inject `IInventoryService` + `IDocumentSequenceService` + `IInventoryAdjustmentService`. Count creates one Adjustment per Post (not per line) for cleaner audit trail with `ReferenceType = "InventoryCount"`. |
| RULE-510 | `InventoryAdjustmentEditorViewModel` MUST follow the `InventoryTransactionEditorViewModel` pattern — same DI, command, validation, and messaging patterns. 5 commands: `LoadWarehouses`, `LoadProducts`, `AddLine`, `RemoveLine`, `SaveDraft`/`Post`. Uses INotifyDataErrorInfo. AdjustmentType must support 1-3 (Addition, Deduction, Correction). |
| RULE-511 | `InventoryCountEditorViewModel` MUST use `InventoryCountLineItem` mutable class for two-way binding. Supports `AddLine`/`RemoveLine` commands, Post flow with ValidateAllAsync, and edit mode for existing counts. Count lines store `ExpectedQuantity`, `ActualQuantity`, and compute `Difference`. |
| RULE-512 | `WarehouseTransferEditorViewModel` MUST NOT hardcode IDs as `short` — use `short` for warehouse IDs matching `Warehouse.Id` type. Validate `SourceWarehouseId != DestinationWarehouseId` before posting. 5 commands: `LoadWarehouses`, `LoadSourceProducts`, `LoadDestinationProducts`, `AddLine`, `RemoveLine`, `SaveDraft`/`Post`. |
| RULE-513 | `InventoryAdjustmentRequestValidator` MUST validate `AdjustmentType` range as `InclusiveBetween(1, 3)` — NOT `(1, 2)` which incorrectly excludes Correction=3. |
| RULE-514 | `ReportsController` MUST place `CancellationToken` parameter BEFORE any optional parameters — ASP.NET Core model binding requires cancellation tokens to precede optional params to avoid binding errors. |
| RULE-515 | `InventoryCountService.PostAsync()` MUST compute `Difference` for each line as `(ActualQuantity - ExpectedQuantity)` — positive difference = surplus (stock increase), negative difference = shortage (stock decrease). Apply increase or decrease via `IInventoryService` based on sign. |
| RULE-516 | All Inventory Operations ViewModels (InventoryAdjustmentEditorViewModel, InventoryCountEditorViewModel, WarehouseTransferEditorViewModel) MUST implement `IDisposable` and call `Cleanup()` in `Dispose()` to unsubscribe EventBus subscriptions — same pattern as RULE-289. MUST use `IToastNotificationService` for minor success messages, not modal dialogs. |
| RULE-517 | `SalesReturn.Post()` MUST set `PostedAt = DateTime.UtcNow` and `SalesReturn.Cancel()` MUST set `CancelledAt = DateTime.UtcNow` — these timestamps are required for audit trail compliance per DocumentEntity lifecycle (RULE-489). |
| RULE-518 | `SalesReturnService` MUST inject `IAccountingIntegrationService` and call `CreateSalesReturnEntryAsync()` on Post AND `ReverseSalesReturnEntryAsync()` (or equivalent reversal) on Cancel — sales returns WITHOUT journal entries leave the general ledger out of sync. `AccountingIntegrationService` MUST have BOTH `CreateSalesReturnEntryAsync()` and `ReverseSalesReturnEntryAsync()` methods. |
| RULE-519 | `ProductUnitId` MUST NEVER be hardcoded to `1` in any ViewModel — use the product's `DefaultPurchaseUnitId` (for purchase/inventory screens) or `DefaultSalesUnitId` (for sales screens) when creating line items. Fallback to `0` (let service determine) rather than hardcoding `1`. |
| RULE-520 | `AllocateAdditionalCharges()` MUST be extracted to a standalone static helper class (`AdditionalChargeAllocator`) rather than being inline in `PurchaseService.PostAsync()` — enables DRY compliance (RULE-041) and unit testability. |

### 2.94 Accounts.md Deep Review Rules (v4.10.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-521 | `ReportExportController.Export()` MUST delegate to `IReportExportService.ExportAsync()` for ALL 20+ report types — NEVER return a `BadRequest` stub. Each report type builds an Arabic-column `DataTable` and exports via ClosedXML (Excel) or QuestPDF (PDF). |
| RULE-522 | `Permission.cs` MUST have ALL permission flags matching the AGENTS.md Section 6 matrix: SalesInvoice, SalesReturn, CustomerView, CustomerManagement, PurchaseInvoice, PurchaseReturn, ProductManagement, SupplierManagement, WarehouseTransfer, Reports, WarehouseManagement, Settings, UserManagement, Backup, ChartOfAccounts, JournalEntries, CashBoxes, Currencies, FiscalYear. Missing flags cause permission bypass or blocked access. |
| RULE-523 | `CanNavigate()` in `MainViewModel` MUST use `_ => false` as the default (deny-by-default) — EVERY screen tag must be explicitly listed. The old `_ => true` pattern allowed unauthorized users to access Manager/Admin screens through navigation. |
| RULE-524 | Organization Management screens (Branches, Departments, Employees, Banks, Parties, Expenses) and accounting screens (ReceiptVouchers, PaymentVouchers, سندات القبض المحاسبية, سندات الصرف) MUST have `Visibility="{Binding IsAdvancedMode, Converter={StaticResource BoolToVisibility}}"` to hide them from Basic users per the Accounts.md UI separation pattern. |
| RULE-525 | ALL XAML windows SHOULD have keyboard shortcuts defined via `<Window.InputBindings>` — at minimum F3 (Products), F4 (Customers), F5 (Purchases), F8 (Reports). Keyboard shortcuts improve operational efficiency per Accounts.md Phase 5 UI analysis. |
| RULE-526 | `InvoicePrintDto` MUST have `OtherCharges` and `FooterNote` properties — OtherCharges from the invoice (delivery/shipping fees) must appear in both A4 and thermal print. FooterNote from PrintSettings replaces the hardcoded "شكراً لتعاملكم معنا". |
| RULE-527 | `PrintController` MUST have return print endpoints (`GET/POST /api/v1/print/sales-returns/{id}/...` and `GET/POST /api/v1/print/purchase-returns/{id}/...`) — the builder already has `BuildFromSalesReturnAsync()`/`BuildFromPurchaseReturnAsync()` methods. |
| RULE-528 | `ThermalReceiptGenerator` MUST accept `EscPosCodePage` as a parameter instead of hardcoding `Encoding.GetEncoding(1256)` — the code page should come from `PrintSettingsDto`. |
| RULE-529 | JWT tokens MUST include a `jti` (JWT ID) claim with a new `Guid` — this enables individual token identification and future revocation support. |
| RULE-530 | JWT secret from environment variable MUST be validated for minimum length (`< 32` characters throws `InvalidOperationException`) — a short secret creates an HS256 security vulnerability. |
| RULE-531 | `SecurityAudit` class MUST expose a non-conditional `RunSecurityAudit()` method callable from production — the `[Conditional("DEBUG")]` automatic check is kept for dev builds but a production endpoint can call the new method. |
| RULE-532 | `JournalEntryLine` entity MUST have a composite index on `(JournalEntryId, AccountId)` in addition to the individual indexes — the accounting integration service frequently queries by both foreign keys simultaneously. |
| RULE-533 | `DailyClosureReportViewModel` MUST be removed from DI and MainViewModel when its DataTemplate is removed — orphaned registrations waste resources and orphaned commands cause navigation to blank screens. |
| RULE-534 | Permission system SHOULD support operation-level granularity (`PermissionOperation` enum: View, Create, Edit, Post, Cancel, Delete, PriceOverride) for future separation of duties — feature-level flags alone are insufficient for enterprise deployments. |

### 2.95 Section 4 FORBIDDEN Updates (add these to the forbidden list)

Add these NEW forbidden patterns:

```text
❌ ReportExportController returning BadRequest stub instead of routing to export service
❌ CanNavigate() with _ => true fallback (must use _ => false deny-by-default)
❌ Missing permission flags in Permission.cs (must match AGENTS.md Section 6)
❌ Advanced-only screens visible to Basic users without IsAdvancedMode guard
❌ Print endpoints missing for returns (sales-returns, purchase-returns)
❌ OtherCharges missing from InvoicePrintDto and print templates
❌ FooterNote missing from InvoicePrintDto (must display configurable footer from settings)
❌ ThermalReceiptGenerator hardcoding code page 1256 (accept from settings)
❌ JWT tokens without jti claim (breaks token identification)
❌ JWT secret without minimum length validation (< 32 chars)
❌ [Conditional("DEBUG")] as sole SecurityAudit access (add production endpoint)
❌ Missing composite index on JournalEntryLine(JournalEntryId, AccountId)
❌ Orphaned ViewModel registrations after DataTemplate removal (DailyClosureReportViewModel)
❌ Feature-level-only permissions without operation-level granularity
```

### 2.96 Section 9 Pre-Submission Checklist Updates

Add these NEW checklist items at the end of the existing checklist:

- [ ] ReportExportController properly delegates to export service (no stub)?
- [ ] All Permission.cs flags match AGENTS.md Section 6 matrix?
- [ ] CanNavigate() uses _ => false deny-by-default with all screens explicit?
- [ ] Organization Management and accounting screens have IsAdvancedMode guards?
- [ ] Keyboard shortcuts (F3/F4/F5/F8) defined in MainWindow.InputBindings?
- [ ] InvoicePrintDto has OtherCharges + FooterNote properties?
- [ ] PrintController has returns endpoints (sales-returns, purchase-returns)?
- [ ] ThermalReceiptGenerator accepts code page parameter?
- [ ] JWT tokens include jti claim?
- [ ] JWT secret validated for minimum 32 characters?
- [ ] SecurityAudit has production-callable method?
- [ ] Composite index on JournalEntryLine(JournalEntryId, AccountId) exists?
- [ ] No orphaned ViewModel DI registrations after UI removal?
- [ ] UserService.CreateAsync uses request.Password if provided (never hardcodes password hash)?
- [ ] UserRole enum NOT referenced anywhere (Domain/Enums/UserRole.cs deleted, Contracts/Enums/CoreEnums.cs cleaned)?
- [ ] PermissionService queries DB Role entity (not Enum.GetValues)?
- [ ] At least 45 permissions seeded in DbSeeder?
- [ ] All 9 roles seeded with appropriate permissions?
- [ ] Desktop PermissionManagementView registered in DI?
- [ ] Desktop AuditLogListView registered in DI?
- [ ] Desktop PasswordChangeView registered in DI?
- [ ] Password change screen shown on first login (MustChangePassword=true)?
- [ ] Account lockout after 5 failed attempts?
- [ ] AccountCode uses hierarchical expanding numbering (1-2-4-8 digits)?
- [ ] AccountCode is system-generated (never user-supplied)?
- [ ] ColorCode is auto-generated from Nature (never user-supplied)?
- [ ] OpeningBalance removed from Account entity (handled via Journal Entry)?
- [ ] AccountCodeGeneratorService uses SemaphoreSlim for thread safety?
- [ ] Opening balance Journal Entry wrapped in transaction with account creation?
- [ ] Seeder uses 74 accounts (5+8+24+37) with hierarchical codes?
- [ ] AccountCode uses hierarchical expanding numbering (1-2-4-8 digits)?
- [ ] AccountCode is system-generated (never user-supplied)?
- [ ] ColorCode is auto-generated from Nature (never user-supplied)?
- [ ] OpeningBalance removed from Account entity (handled via Journal Entry)?
- [ ] AccountCodeGeneratorService uses SemaphoreSlim for thread safety?
- [ ] Opening balance Journal Entry wrapped in transaction with account creation?
- [ ] Seeder uses 74 accounts (5+8+24+37) with hierarchical codes?

Phase 24: Accounting Engine Automation → Automatic journal entries for all money operations
Phase 25: Payment Update/Delete Reversal Entries → Fix C-2/C-3: Reversal entries for payment update/delete
Phase 26: Code Review Remediations (C-1 through C-8) → Fix COGS, netRevenue, CashTransactionType, ReferenceId lookup, InvoiceNo generation

### 2.76 Phase 24 — Accounting Integration Rules (v4.6.9+)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-371 | AccountingIntegrationService MUST create balanced double-entry journal entries for EVERY money-affecting operation — NEVER skip journal entry creation. |
| RULE-372 | Customer/Supplier opening balance MUST create a journal entry: Dr AccountsReceivable / Cr OpeningBalanceEquity (customer) OR Dr OpeningBalanceEquity / Cr AccountsPayable (supplier). |
| RULE-373 | Sales invoice posting MUST create TWO sets of journal entries: Revenue side (Dr Cash/AR, Cr Sales Revenue, Cr VAT Output) AND COGS side (Dr COGS, Cr Inventory). |
| RULE-374 | Purchase invoice posting MUST create a journal entry: Dr Inventory, Dr VAT Input, Cr Cash/AP. |
| RULE-375 | All cancellation/reversal operations MUST create reversal journal entries mirroring the original with Dr↔Cr swapped. |
| RULE-376 | Payment creation MUST create a journal entry: Dr Cash, Cr AR (customer payment) OR Dr AP, Cr Cash (supplier payment). |
| RULE-377 | Payment UPDATE/DELETE MUST create reversal journal entries for the original amount before applying changes — NEVER leave orphan journal entries in the general ledger. |
| RULE-378 | COGS in sales journal entries MUST use AverageCost (weighted average cost from ProductUnit), NOT PurchaseCost — the costing method set in SystemSettings determines the actual cost value. |
| RULE-379 | NetRevenue (SubTotal - Discount) MUST be validated: if Discount > SubTotal, return Result.Failure — NEVER clamp to zero (causes unbalanced entries). |
| RULE-380 | Journal entry reference lookup MUST use ReferenceId (int FK) as primary key, with ReferenceNumber (string) as fallback — NEVER rely on string matching alone. |
| RULE-381 | Reverse journal entry queries MUST have a computation fallback: if original entry not found, compute estimated amounts from source document items and log a warning. |
| RULE-382 | JournalEntriesController.Create() MUST extract CreatedBy userId from JWT claims — NEVER accept client-supplied CreatedBy field (prevents user ID spoofing). |
| RULE-383 | CustomerService/SupplierService Create/Update/Delete MUST accept int userId parameter from controller — NEVER hardcode userId (e.g., `createdByUserId: 1`). |
| RULE-384 | InvoiceNo MUST be generated via IDocumentSequenceService.GetNextIntAsync() (thread-safe with SemaphoreSlim) — NEVER use `lastId + 1` (causes duplicates under concurrency). |
| RULE-385 | OpeningBalanceEquityAccount (1422) MUST be seeded with isSystemAccount=true (protected from deletion) even though it's Level 3 — this is a deliberate exception to RULE-327. |
| RULE-386 | JournalEntryConfiguration MUST have a composite index on (ReferenceType, ReferenceId) with filter `[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL` for reference-based lookups. |
| RULE-387 | CashTransactionType on purchase cancel MUST use RefundOut (reversal), NOT SupplierPayment (forward payment type). |
| RULE-388 | All AccountingIntegrationService methods MUST return Result<int> (never throw) and MUST NOT own transactions — the caller is responsible for wrapping in ExecuteTransactionAsync. |

### 2.77 Phase 25 — Products Module Rules (v4.6.9+ — Updated for Per-Unit Pricing)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-389 | Product entity MUST be SIMPLIFIED: only Name, CategoryId, Description, TrackExpiry (bool), IsActive, CreatedAt, UpdatedAt. NO PurchasePrice, SalePrice, WholesalePrice, RetailPrice, ExpirationDate, Barcode, OpeningQuantity, OpeningUnitCost, or CurrentQuantity on Product. |
| RULE-390 | `ProductPrices` entity: ProductUnitId FK, CurrencyId FK, Price decimal(18,2), EffectiveFrom datetime2, EffectiveTo datetime2 (nullable) — supports multi-currency pricing with effective date ranges. Price is per (Product + Unit + Currency), NOT per Product. |
| RULE-391 | `InventoryBatches` entity: ProductId FK, WarehouseId FK, BatchNo, ExpiryDate (nullable), QuantityReceived decimal(18,3), QuantityRemaining decimal(18,3), UnitCost decimal(18,2), PurchaseInvoiceId FK (nullable) — enables FIFO/FEFO cost allocation. |
| RULE-392 | NO `PriceLevel` enum in V1 — pricing is simply per (ProductUnit + Currency). No Retail/Wholesale/VIP/Distributor concept. User defines price per unit directly. |
| RULE-393 | Prices are retrieved from `ProductPrices` table filtered by ProductUnitId + CurrencyId + EffectiveFrom/EffectiveTo — NEVER compute prices dynamically outside this lookup. |
| RULE-394 | Product images stored in `ProductImages` table (multiple images per product, ProductId FK, ImagePath, IsPrimary) — ImagePath on Product entity is NOT needed; all images in ProductImages table. |
| RULE-395 | Opening stock on product creation: optional `OpeningQuantity`, `OpeningUnitCost`, `ExpiryDate` per warehouse — creates initial InventoryBatch + WarehouseStock entry. Opening stock generates a Journal Entry: Dr Inventory / Cr OpeningBalanceEquity. |

### 2.78 Phase 26 — Warehouses Module Rules (v4.6.9+)
| RULE | DIRECTIVE |
|------|-----------|
| RULE-396 | Warehouse entity MUST have: `Type` (Main=1, Store=2, Showroom=3), `ManagerName` (nullable string), `AccountId` (int? FK to Account linking to Chart of Accounts), `Address` (nullable string), `Phone` (nullable string). |
| RULE-397 | `StockAdjustmentType` enum: Addition=1, Deduction=2, Correction=3 — for inventory adjustment operations. |
| RULE-398 | `StockIssueReason` enum: SalesReturn=1, Damage=2, Expiry=3, InternalUse=4, Other=5. |
| RULE-399 | Physical Count deferred to V2 — initial stock on product creation covers opening stock needs. |
| RULE-467 | `WarehouseTransfer` / `WarehouseTransferLine` entities replace `StockTransfer` / `StockTransferItem` — supports multi-item transfers with full audit trail. TransferLine stores ProductUnitId, Quantity (in base unit), BatchNo. |
| RULE-468 | `InventoryTransaction` / `InventoryTransactionLine` entities replace `InventoryMovement` — Transaction stores ReferenceType, ReferenceId, WarehouseId, Notes. TransactionLine stores ProductUnitId, Quantity (in base unit), UnitCost, BatchNo, ExpiryDate. |
| RULE-469 | Perpetual Inventory system: NO `Purchases` account used — all inventory costs go DIRECTLY to Inventory Asset account. Purchase invoice posts Dr Inventory / Cr Cash/AP. |
| RULE-470 | `InventoryOperation` and `StockWriteOff` are NOT in V1 — deferred to V2. All stock changes happen through inventory transactions (Receipt, Issue, Transfer, Adjustment). |

### 2.79 Phase 27 — Purchases Module Rules (v4.6.9+)
| RULE | DIRECTIVE |
|------|-----------|
| RULE-401 | Purchase invoice MUST support: multi-currency (CurrencyId + ExchangeRate), additional charges (AdditionalCharge entity with AccountId FK), partial PO receipt, attachments (single AttachmentPath). |
| RULE-402 | `AdditionalCharge` entity: PurchaseInvoiceId FK, Description, Amount decimal(18,2), AccountId FK (int) — distributes extra costs (transport, customs, etc.) across invoice items for true landed cost. |
| RULE-403 | Purchase Order (PO) entity: separate table with its own sequence, can be partially received via PurchaseInvoice. PO has Draft/Approved/Received/Cancelled statuses. |
| RULE-404 | Purchase return MUST be standalone (not linked to original invoice) — supports returning items purchased outside the system. |
| RULE-405 | PurchaseCost distribution: `AllocateAdditionalCharges()` service distributes AdditionalCharge amounts across invoice items by quantity or value weighting. |

### 2.80 Phase 28 — Sales Module Rules (v4.6.9+)
| RULE | DIRECTIVE |
|------|-----------|
| RULE-406 | Sales invoice MUST support: multi-currency (CurrencyId + ExchangeRate), profit display per line (SalePrice - AverageCost), price override with permission check, barcode POS mode. |
| RULE-407 | `SalesQuotation` entity: separate table with expiry date, convertible to SalesInvoice. Quotation has Draft/Confirmed/Expired/Converted statuses. |
| RULE-408 | Continuous barcode scanning POS mode: keyboard wedge scanner support — auto-adds product by barcode without manual focus/button click. |
| RULE-409 | Credit limit check: `Customer.CheckCreditLimit(additionalAmount)` returns bool — must be checked BEFORE posting a credit sale. |
| RULE-410 | Sales return MUST generate automatic refund: CashTransaction with RefundOut type, reverse journal entry for the return amount. |

### 2.81 Phase 29 — Receipts & Payments Module Rules (v4.6.9+)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-411 | CustomerPayment/SupplierPayment MUST support: multi-invoice distribution, cheque management (Cheque entity), cash/ bank/ cheque payment methods. |
| RULE-412 | `Cheque` entity: ChequeNumber, BankName, IssueDate, MaturityDate, Status (Pending=1, Cleared=2, Bounced=3, Cancelled=4), Amount, PaymentId FK. |
| RULE-413 | Payment distribution: one payment can settle multiple invoices — `PaymentAllocation` entity tracks (PaymentId, InvoiceId, InvoiceType, AllocatedAmount). |
| RULE-414 | CashBox MUST have AccountId FK linking to Chart of Accounts — cash transactions MUST create automatic journal entries. |
| RULE-415 | DailyClosure MUST compute: ActualCashCount, ExpectedBalance, Difference (actual - expected), DifferenceReconciled flag. |

### 2.82 Phase 30 — Journal Entries Module Rules (v4.6.9+)
| RULE | DIRECTIVE |
|------|-----------|
| RULE-416 | JournalEntry MUST support 3-state lifecycle: Draft (1) → Posted (2) → Cancelled (3) — identical to invoice lifecycle. |
| RULE-417 | JournalEntry MUST support multi-currency: CurrencyId FK, ExchangeRate decimal(18,2), display both original and base currency amounts per line. |
| RULE-418 | JournalEntry MUST support attachments: `AttachmentPath` (single) or `JournalEntryAttachments` table (multiple for V2). |
| RULE-419 | `FiscalYear` entity: Year int, StartDate, EndDate, IsOpen bool, OpenedAt, ClosedAt, ClosedByUserId FK. |
| RULE-420 | Annual Closing: Transfer revenue/expense balances to RetainedEarnings, close all income statement accounts, lock fiscal year. Opening entry for new fiscal year. |

### 2.83 Phase 31 — Reports Module Rules (v4.6.9+)
| RULE | DIRECTIVE |
|------|-----------|
| RULE-421 | Reports MUST use dedicated DTOs with computed display properties (e.g., `BalanceStatus`, `AgingBucket`) — NEVER require ViewModel to translate raw data. |
| RULE-422 | Hierarchical Income Statement DTO: Revenue - COGS = GrossProfit - OperatingExpenses = NetIncome with subtotals at each level. |
| RULE-423 | Hierarchical Balance Sheet DTO: Assets = Liabilities + Equity with section subtotals, computed from Account balances. |
| RULE-424 | Excel export via ClosedXML — ALL report DTOs must support `ToDataTable()` or equivalent for worksheet generation. |
| RULE-425 | 35+ report DTOs across 7 categories: Financial (6), Inventory (5), Sales (6), Purchases (4), Cash/Box (4), Transactions (5), Users (5). |

### 2.84 Phase 32 — Suppliers Module Rules (v4.6.9+ — Updated: كل مورد = حساب, No Balance on Entity)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-442 | `Supplier` entity MUST have MANDATORY `AccountId` (int, non-nullable FK to `Account`) — auto-created by service, NEVER user-supplied in requests. |
| RULE-443 | `SupplierType` NOT in V1 — deferred to V2. No SupplierType entity or enum in V1 codebase. Payment terms stored on PurchaseInvoice (per-invoice), not on Supplier entity. |
| RULE-444 | `Supplier.Create()` MUST accept `accountId` as required parameter (`int`, non-nullable). |
| RULE-445 | `Supplier.Update()` MUST accept `accountId` as required parameter (`int`, non-nullable). |
| RULE-446 | `Supplier` entity MUST NOT have `OpeningBalance` or `CurrentBalance` fields — source of truth is the linked GL Account balance. Opening balance is a Journal Entry (Dr OpeningBalanceEquity / Cr AP). |
| RULE-447 | `Supplier` entity MUST NOT have `CurrencyId` — currency is per-transaction (invoice/payment), not per-supplier. |
| RULE-448 | `Supplier.CheckCreditLimit(decimal additionalAmount)` is a NON-THROWING domain method returning `bool` — SOFT WARNING only. |
| RULE-449 | `SupplierDto` MUST include `AccountId` (int, non-nullable) and `AccountName` (string?) — NO `SupplierType`, `SupplierTypeName`. |
| RULE-450 | `CreateSupplierRequest`/`UpdateSupplierRequest` MUST NOT have `AccountId` — auto-created by service. |
| RULE-451 | Desktop Supplier Editor MUST NOT have OpeningBalance input field, AccountId selection combo, or SupplierType radio — AccountName is display-only after save. |
| RULE-452 | Account auto-creation: Service auto-creates Level-4 detail account under parent `"2100 — حسابات الموردين"` (Accounts Payable) when creating a supplier. |
| RULE-453 | Seeder MUST seed a default supplier "مورد نقدي" with auto-created account linked to COA under 2100 parent. |
| RULE-454 | Phone number validation in supplier FluentValidators MUST use regex `^05\d{8}$` with Arabic error message — NEVER only `MaxLength` check. Email MUST use `.EmailAddress()` validation. |

### 2.85 Phase 18 Post-Analysis Remediations (v4.10)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-455 | `SystemAccountMappings` MUST have `PurchaseReturnAccountId` (int, non-nullable FK to Account) — NEVER omit the Purchase Returns account mapping. |
| RULE-456 | `AccountingIntegrationService` MUST use per-entity accounts for ALL customer/supplier journal entries — use `Customer.AccountId` and `Supplier.AccountId` instead of fixed `SystemAccountMappings.AccountsReceivableAccountId`/`AccountsPayableAccountId`. |
| RULE-457 | Sales revenue MUST use a SINGLE account (`1520 — إيرادات المبيعات`) — NEVER split into separate Cash Sales (`1521`) and Credit Sales (`1522`) accounts. The `PaymentType` distinction is captured in the debit side (Cash vs AR), not in the revenue account. |
| RULE-458 | Purchase return reversal journal entries MUST credit `PurchaseReturnAccountId` (contra expense account) — NEVER credit `InventoryAssetAccountId` directly. This enables proper financial reporting of purchase returns separate from inventory adjustments. |
| RULE-459 | `GetCustomerAccountId(SalesInvoice, SystemAccountMappingsDto)` helper MUST be used in ALL sales-related journal entry methods — falls back to `AccountsReceivableAccountId` when `invoice.Customer` nav property is null. |
| RULE-460 | `GetSupplierAccountId(PurchaseInvoice, SystemAccountMappingsDto)` helper MUST be used in ALL purchase-related journal entry methods — falls back to `AccountsPayableAccountId` when `invoice.Supplier` nav property is null. |
| RULE-461 | `AccountingIntegrationService.CreateCustomerOpeningEntryAsync()` and `CreateSupplierOpeningEntryAsync()` MUST accept `int customerAccountId`/`int supplierAccountId` parameters — NEVER hardcode `AccountsReceivableAccountId`/`AccountsPayableAccountId` from system mappings. |
| RULE-462 | `AccountingIntegrationService.ReverseCustomerPaymentEntryAsync()` and `ReverseSupplierPaymentEntryAsync()` MUST accept `int customerAccountId`/`int supplierAccountId` parameters — NEVER use system mapping defaults. |
| RULE-463 | COA account `1630` (Returns) MUST cover BOTH Sales Returns and Purchase Returns — rename to `المردودات` and add child account `1632 — مردودات مشتريات` under it. |

### 2.97 Success/Failure User Feedback (v4.10.4 — Comprehensive Audit)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-536 | EVERY ViewModel operation (Save, Post, Cancel, Delete, Restore) MUST show BOTH a success message AND a failure message — NEVER leave the user without feedback. |
| RULE-537 | Minor operations (delete, restore, post, cancel) MUST use `_toastService.ShowSuccess("تم [العملية] بنجاح")` for success — NEVER use modal dialog for minor successes. |
| RULE-538 | Major operations (save, create, update) MUST use `_dialogService.ShowSuccessAsync("عنوان النجاح", "تم [العملية] بنجاح")` for success — use modal dialog for important operations. |
| RULE-539 | ALL failure paths MUST show `await _dialogService.ShowErrorAsync("عنوان الخطأ", ErrorMessage!)` after `HandleFailure()` — NEVER silently swallow errors. |
| RULE-540 | ALL list ViewModels MUST have an ErrorMessage bar in XAML (Border with Visibility="{Binding ErrorMessage, Converter=...}") to show error text — NEVER rely only on dialog boxes for error display. |
| RULE-541 | The ErrorMessage bar MUST be placed between the header and content area — use `Grid.Row=2` in standard 3-row layouts (header=0, search=1, content=2). |
| RULE-542 | ALL editor forms MUST have a loading overlay (Border spanning all Grid.Rows with IsBusy visibility binding) — NEVER leave the user without visual feedback during async operations. |

**Success Message Arabic Templates:**
- Save: "تم حفظ [العنصر] بنجاح"
- Delete: "تم حذف [العنصر] بنجاح"
- Restore: "تم استعادة [العنصر] بنجاح"
- Post: "تم ترحيل [العنصر] بنجاح — سيتم تحديث المخزون والرصيد"
- Cancel: "تم إلغاء [العنصر] بنجاح"
- Export: "تم تصدير التقرير بنجاح"

**Error Message Dialog Titles:**
- Save failures: "خطأ في حفظ [العنصر]"
- Delete failures: "خطأ في حذف [العنصر]"
- Post failures: "خطأ في الترحيل"
- Cancel failures: "خطأ في الإلغاء"
- Load failures: "خطأ في تحميل البيانات"

### 2.98 XAML UI Quality (v4.10.4 — Comprehensive View Audit)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-543 | ALL ContextMenu MenuItems MUST have Arabic ToolTips explaining the action (e.g., `ToolTip="تعديل العنصر المحدد"` for "تعديل") — NEVER leave ContextMenu items without ToolTips. |
| RULE-544 | ALL CheckBox controls in list views (especially "عرض غير النشطة" and "عرض الملغاة") MUST have Arabic ToolTips explaining what they filter. |
| RULE-545 | ALL form controls (TextBox, ComboBox, DatePicker) in editor views MUST have Arabic ToolTips explaining the input rules — NEVER leave any input without a ToolTip. |
| RULE-546 | ComboBox elements MUST NOT use `Style="{StaticResource ComboBoxStyle}"` — use `Style="{StaticResource ModernComboBox}"` instead. `ComboBoxStyle` has `TargetType="ComboBox"` but the `ModernComboBox` name is the correct style key. |
| RULE-547 | ALL views MUST have an ErrorMessage display Border at the top (between header and content) bound to `ErrorMessage` — NEVER let errors go invisible. |
| RULE-548 | ALL editor views MUST have a loading overlay Border spanning all Grid.Rows with `Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibility}}"` and `Panel.ZIndex="1000"`. |
| RULE-549 | The loading overlay Border MUST contain a ProgressBar (indeterminate) with Arabic "جاري المعالجة..." text — never a blank overlay. |
| RULE-550 | ComboBox elements MUST have `ItemsSource`, `SelectedValuePath="Key"`, and `DisplayMemberPath="Value"` bound to their option collections — NEVER leave a ComboBox without ItemsSource (functional bug). |
| RULE-551 | ComboBox elements MUST NOT have BOTH `DisplayMemberPath` AND `ItemTemplate` set simultaneously — WPF throws InvalidOperationException. Use one or the other. |

### 2.99 IncludeInactive Checkbox Pattern (v4.10.5)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-552 | ALL list ViewModels for entities supporting soft-delete (IsActive) MUST have an `IncludeInactive` property that triggers data reload when changed. |
| RULE-553 | `IncludeInactive` MUST be a bool property with a setter that calls `SetProperty` then reloads data via `_ = LoadItemsAsync()`. |
| RULE-554 | ALL list XAML views for soft-deletable entities MUST have a CheckBox bound to `IncludeInactive` with Arabic content "عرض غير النشطة" and an Arabic ToolTip explaining the filter. |
| RULE-555 | The IncludeInactive CheckBox MUST be placed in the toolbar/search area, typically next to the search box or filter controls. |
| RULE-556 | When the DTO has an `IsActive` field, client-side filtering MUST be implemented using a CollectionView filter: if not IncludeInactive, filter out inactive items. When the API supports `includeInactive` parameter, pass it directly. |
| RULE-557 | The `IncludeInactive` property pattern: `private bool _includeInactive; public bool IncludeInactive { get => _includeInactive; set { if (SetProperty(ref _includeInactive, value)) { _ = LoadItemsAsync(); } } }` |
| RULE-558 | Entities with status-based lifecycles (Draft/Posted/Cancelled like invoices, journal entries) should use `IncludeCancelled` instead of `IncludeInactive` where appropriate. |
| RULE-559 | Views for log/transaction/appended-only entities (audit logs, inventory transactions, notifications) do NOT need IncludeInactive since they don't support soft-delete. |

### 2.100 Transaction Atomicity & Inventory Audit Trail (v4.10.7)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-560 | ALL 22 transaction operations that affect stock, cash, or accounting MUST be wrapped in `ExecuteTransactionAsync` — NO exceptions. This includes: SalesService.PostAsync/CancelAsync, PurchaseService.PostAsync/CancelAsync, SalesReturnService.PostAsync/CancelAsync, PurchaseReturnService.PostAsync/CancelAsync, CustomerReceiptService.CreateAsync/PostAsync/CancelAsync/DeleteAsync, SupplierPaymentService.CreateAsync/PostAsync/CancelAsync/DeleteAsync, ExpenseService.CreateAsync/PostAsync/CancelAsync, InventoryAdjustmentService.PostAsync/CancelAsync, InventoryCountService.PostAsync/CancelAsync, WarehouseTransferService.CreateAsync/PostAsync/CancelAsync, ReceiptVoucherService.PostAsync/CancelAsync, PaymentVoucherService.PostAsync/CancelAsync, InventoryService.CreateTransferAsync/PostTransferAsync/CancelTransferAsync. |
| RULE-561 | `InventoryTransaction` records MUST be created for EVERY stock-affecting operation — SalesService.PostAsync (type=Sale), SalesService.CancelAsync (type=SaleReturn), PurchaseService.PostAsync (type=Purchase), PurchaseService.CancelAsync (type=PurchaseReturn), SalesReturnService.PostAsync (type=SaleReturn), SalesReturnService.CancelAsync (type=SaleReturn), PurchaseReturnService.PostAsync (type=PurchaseReturn), PurchaseReturnService.CancelAsync (type=PurchaseReturn), InventoryAdjustmentService.PostAsync (type=Adjustment), InventoryCountService.PostAsync (type=Count), WarehouseTransferService.PostTransferAsync (type=TransferOut/TransferIn). Each InventoryTransaction MUST have at least one `InventoryTransactionLine` per product. |
| RULE-562 | `ExpenseService` MUST inject `IAccountingIntegrationService` and create journal entries: PostAsync creates `Dr ExpenseAccount / Cr CashBox.Account`; CancelAsync creates reversal `Dr CashBox.Account / Cr ExpenseAccount`. Expense sequence MUST use `IDocumentSequenceService.GetNextIntAsync("Expense", ct)` — NEVER `ToListIgnoreFiltersAsync().Max() + 1` (not thread-safe). |
| RULE-563 | `InventoryService` methods that modify stock in bulk (IncreaseStockAsync, DecreaseStockAsync, CreateTransferAsync, PostTransferAsync, CancelTransferAsync) MUST be wrapped in `ExecuteTransactionAsync` — stock updates are financial operations requiring atomic commits. |
| RULE-564 | `DeleteAsync` and `CancelAsync` methods MUST call `SaveChangesAsync` before returning — missing `SaveChangesAsync` means deletes are silently rolled back. This applies to CustomerReceiptService.DeleteAsync, SupplierPaymentService.DeleteAsync, and any other soft-delete operation. |
| RULE-565 | When creating `InventoryTransaction` records inside a service's `ExecuteTransactionAsync` wrapper, use `InventoryTransaction.Create()` + `AddLine()` — NEVER create `InventoryTransaction` inside `IncreaseStockAsync`/`DecreaseStockAsync` (would cause duplicate records when those methods are called from multiple callers). |
| RULE-566 | FIFO batch restoration in SalesService.CancelAsync/PurchaseService.CancelAsync is NOT yet implemented — stock is restored via `IncreaseStockAsync()` but batch `QuantityRemaining` is not decremented back. This is a known gap to be addressed when batch allocation tracking is added. |

## 3. Enums (Use These EXACT Values)

```csharp
public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }
public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }
public enum MovementType : byte
{
    PurchaseIn = 1, SaleOut = 2, SaleReturnIn = 3,
    PurchaseReturnOut = 4, TransferOut = 5, TransferIn = 6, Adjustment = 7
}
public enum UnitType : byte { Retail = 0, Wholesale = 1 }  // For wholesale/retail pricing
public enum SaleMode : byte { Retail = 1, Wholesale = 2 }  // Invoice line item mode
public enum DeleteStrategy { Cancel = 0, Deactivate = 1, Permanent = 2 }
public enum CostingMethod : byte { WeightedAverage = 1, LastPurchasePrice = 2, SupplierPrice = 3 }
public enum CashTransactionType : byte
{
    OpeningBalance = 1, SalesIncome = 2, Expense = 3,
    TransferOut = 4, TransferIn = 5, RefundOut = 6,
    SupplierPayment = 7, CustomerPayment = 8
}
public enum InvoiceTypePrint : byte
{
    Sales = 1, Purchase = 2, SalesReturn = 3, PurchaseReturn = 4, Test = 5
}
public enum PriceLevel : byte { Retail = 1, Wholesale = 2, VIP = 3, Distributor = 4 }
public enum WarehouseType : byte { Main = 1, Store = 2, Showroom = 3 }
public enum StockAdjustmentType : byte { Addition = 1, Deduction = 2, Correction = 3 }
public enum StockIssueReason : byte { SalesReturn = 1, Damage = 2, Expiry = 3, InternalUse = 4, Other = 5 }
public enum QuotationStatus : byte { Draft = 1, Confirmed = 2, Expired = 3, Converted = 4 }
public enum POStatus : byte { Draft = 1, Approved = 2, Received = 3, Cancelled = 4 }
public enum ChequeStatus : byte { Pending = 1, Cleared = 2, Bounced = 3, Cancelled = 4 }
```

---

## 4. FORBIDDEN — Never Do These

```text
❌ float / double / real for money or quantity
❌ varchar (always use nvarchar)
❌ Console.WriteLine (use Serilog)
❌ Direct DB from Desktop
❌ Business logic in Controllers
❌ Throwing exceptions from Services (use Result<T>)
❌ Hard-deleting invoices or users
❌ Editing a Posted invoice
❌ Data payloads in EventBus messages
❌ DataAnnotations on Domain Entities (use Fluent API)
❌ Cascade delete on any FK
❌ InvoiceNo as string (use int) — NOT nullable, NOT unique
❌ Hard-deleting Users (soft delete only — invoices reference them)
❌ Duplicating business logic outside of the Domain layer
❌ Direct property modification on Entities from outside the class (use Domain methods instead)
❌ Storing price/cost on `Product` instead of `ProductUnit` (use per-unit pricing)
❌ Embedding barcodes in Unit entity (use UnitBarcode table)
❌ Computing unit conversion outside Domain (use `ProductUnit.ConvertToUnit`)
❌ Hard-coding costing logic outside `UpdateProductPricingService`
❌ Allowing `CashBox.CurrentBalance` to go negative
❌ Editing CashTransaction entries (immutable — use offsetting entry)
❌ Updating price/cost without recording in `ProductPriceHistory`
❌ Using WPF `FixedDocument`/`PrintDialog` for printing (use QuestPDF + Win32 raw printing instead)
❌ Throwing exceptions from `IPrintService` (return `PrintResult` instead)
❌ Desktop calling printer directly (always go through `PrintController` API)
❌ Using external ESC/POS NuGet packages (use custom `EscPos` builder)
❌ Crashes on missing logo (omit gracefully — null checks)
❌ Storing print settings outside `SystemSetting` table (use `Category = "Print"`)
❌ Using external ESC/POS NuGet packages (use custom `EscPos` builder)
❌ Crashes on missing logo (omit gracefully — null checks)
❌ Blocking startup for update checks (fire-and-forget only)
❌ Using `Environment.Exit(0)` in services (return `Result<bool>` instead)
❌ Storing skipped version in `appsettings.json` (use `%AppData%`)
❌ Using `int.Parse` on config values (use `int.TryParse`)
❌ Leaving database in SINGLE_USER mode after restore failure
❌ Using SMO for backup (use raw SQL `BACKUP DATABASE`)
❌ Hardcoding JWT secret (use environment variable — throw in production)
❌ Writing `appsettings.json` without atomic pattern (use `.tmp` → `File.Replace()`)
❌ Allowing `CashBox.CurrentBalance` to go negative
❌ Manual try/catch/finally in ViewModel commands (use ExecuteAsync wrapper)
❌ IsLoading property (use IsBusy from ViewModelBase instead)
❌ MediatR/CQRS pattern (use Service Layer — MediatR package removed)
❌ Legacy WinForms code (deleted — use DesktopPWF WPF only)
❌ Starting Desktop without checking DB connection (use DatabaseHealthCheckService first)
❌ Letting DB connection exceptions crash the API without returning DATABASE_CONNECTION_ERROR
❌ Showing raw exception messages to end users (use DatabaseErrorDialog with Arabic messages)
❌ Using `SecureDbContextFactory` without fallback to `SALESSYSTEM_DB_CONNECTION` env var
❌ Opening editors with `ShowDialog()` (always use `ScreenWindowService.OpenScreen` — non-modal)
❌ Creating Window instances directly for screens (use `ScreenWindow` + `OpenWindow` instead)
❌ Using strong references for window tracking (use `WeakReference<Window>`)
❌ Calling UI operations from `OnClosed` callback without `Dispatcher.InvokeAsync()`
❌ Using `MessageBox.Show` in ViewModels (use `IDialogService`)
❌ Showing `ex.Message` in user-facing dialogs (log via Serilog, show Arabic-friendly message)
❌ Generic `"خطأ"` dialog titles (use screen-specific titles)
❌ Sync `_dialogService.ShowError()` without Async suffix
❌ Raw HTTP response bodies in error dialogs (log via Serilog instead)
❌ `ShutdownMode="OnLastWindowClose"` (use `OnExplicitShutdown`)
❌ `LoginWindow.CloseButton_Click` calling `Close()` instead of `Shutdown()`
❌ Logging user validation errors at Error level (use Warning for user mistakes)
❌ Calling ReadFromJsonAsync on non-JSON response bodies without content-type guard
❌ Manual `new ...View { DataContext = vm }` + `window.ShowDialog()` in ViewModels (use _dialogService.ShowDialog)
❌ Ignoring `_dialogService.ShowDialog()` return value in list ViewModels (must refresh on true)
❌ Button or MenuItem without Arabic ToolTip
❌ ToolTip that just repeats the button text (e.g., Button="منتج جديد" ToolTip="منتج جديد")
❌ Code column on Product, Customer, Supplier, or Warehouse entities (use Id instead)
❌ InvoiceNo as string on SalesInvoice or PurchaseInvoice entities (use int instead)
❌ `lastId + 1` for InvoiceNo generation (not thread-safe — use DocumentSequenceService)
❌ InvoiceNo as string (use int, nullable in requests, UNIQUE per document type)
❌ DocumentSequenceService for old INV/PUR prefix strings (use GetNextIntAsync instead for invoices)
❌ SupplierInvoiceNo used as system InvoiceNo (it's supplier's reference only)
❌ Non-unique InvoiceNo (duplicates are NOT allowed — causes search/return/report confusion)
❌ WarehouseResponse still containing Code field
❌ ProductCode on invoice item DTOs (use ProductId only)
❌ Code auto-generation for products, customers, or suppliers
❌ Filtering/searching by Code string column
❌ Serilog.Log.Error directly in ViewModels (use LogSystemError from ViewModelBase instead)
❌ T-SQL without IsDatabaseConnectionException guard in ExceptionMiddleware
❌ Cascade delete on ANY FK (ProductUnit, UnitBarcode, ProductBarcode)
❌ Application service throwing exceptions instead of returning Result.Failure (ProductPriceService.GetPriceByUnitAsync)
❌ Controller injecting DbContext or IUnitOfWork directly (PrintController, LogsController)
❌ Enum values that don't match AGENTS.md Section 3 exactly
❌ decimal(18,4) for money fields (use decimal(18,2))
❌ Missing CHECK constraint on WarehouseStocks.Quantity
❌ Missing FluentAPI configuration for ProductPriceHistory
❌ Missing HasQueryFilter(IsActive) on UnitBarcodeConfiguration
❌ Code field on Product/Customer/Supplier Response DTOs
❌ DuplicateCode in ErrorCodes
❌ Setting Window.Owner = this (self-ownership crash)
❌ Relying on API return order for list display (always sort client-side)
❌ HasXxxError / XxxError boolean + computed string pattern for validation (use INotifyDataErrorInfo AddError/ClearErrors instead)
❌ Plaintext connection strings in any appsettings file (use env var `SALESSYSTEM_DB_CONNECTION`)
❌ Hard-deleting Users (soft delete only — `PermanentDeleteAsync` MUST return `Result.Failure`)
❌ Login endpoint without rate limiting (use `[EnableRateLimiting("LoginPolicy")]`)
❌ Unhandled exception dialog without FallbackErrorDialog (use thread-safe dialog overlay)
❌ Duplicating validation dialog logic in each Editor ViewModel (use ValidateAllAsync from ViewModelBase)
❌ Committing C# files with garbled Arabic (mojibake) — always verify UTF-8 encoding
❌ Hardcoded `Height="36"` or `Height="40"` on Button/TextBox/ComboBox (let style handle at 28px)
❌ Hardcoded `Padding="16+"` on buttons (let style handle at 10,4)
❌ Header/footer padding larger than `12,6` / `12,8` (use compact values)
❌ Empty-state button margins `20px` or widths `160px` (use `12px` / `140px`)
❌ Dialog title `FontSize` larger than 16 or section header larger than 14
❌ MainWindow sidebar wider than `200` (use compact navigation width)
❌ Dialog icon borders `50×50` or larger (use `44×44` max)
❌ `BeginTransactionAsync` when `SqlServerRetryingExecutionStrategy` is configured
❌ `ExchangeRate` on payments without `.HasPrecision(18, 2)` (causes silent truncation)
❌ Bare `.WithOne()` on relationships — always specify the navigation property
❌ `LogSystemError` for business validation errors from API (use `HandleFailure` only — Warning level)
❌ `ValidateAsync()` with custom dialog instead of `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()`
❌ `SetBatchSystemSettingsAsync` calling `SaveChangesAsync` directly (must use `_uow.SaveChangesAsync()`)
❌ Tax.Update() without `UpdateTimestamp()` (audit trail broken)
❌ SystemSetting.Create() without Category or DataType validation
❌ Filtered unique index on soft-deletable entities missing `AND [IsActive]` guard
❌ Hardcoded `category: "Print"` in `SetStringAsync()` auto-create
❌ Seed data with `defaultTaxRate: 15m` (must be `0m`)
❌ CurrencyCode validation using `Length > N` instead of `Length != 3` (ISO 4217 requires exactly 3 characters)
❌ `HasChildren()` domain guard used as sole protection for parent accounts (service MUST use `AnyAsync()` DB query)
❌ Fetching entity twice in `DeleteAsync()`/`PermanentDeleteAsync()` (use already-loaded entity)
❌ `Explanation` field missing from any layer (Domain, EF, DTO, Request, Service, Validator, Seeder)
❌ `:byte` route constraint on controller endpoints (use `:int:min(1):max(N)` instead)
❌ Controller route range not matching the enum value range (e.g., hardcoded `1-3` instead of `1-5`)
❌ Update Validators with fewer rules than Create Validators
❌ System account with empty `explanation` in seed data (all seeded accounts need Arabic explanations)
❌ Account code not read-only during edit mode (breaks integration with existing references)
❌ `string` (non-nullable) for `Explanation` field (must be `string?` nullable)
❌ `nameof` operator NOT used in validator `RuleFor` calls (string literals break on rename)
❌ Health check injecting raw `IConfiguration` instead of `SecureDbContextFactory`
❌ `CheckCreditLimit()` throwing exceptions (return bool instead)
❌ CustomerGroup in V1 (deferred to V2 — no CustomerGroup entity, service, controller, or Desktop UI in V1)
❌ CustomerType/SupplierType in V1 (deferred to V2 — payment type is per-invoice)
❌ User-supplied AccountId in Customer/Supplier requests (auto-created by service)
❌ OpeningBalance input field in Customer/Supplier Desktop forms (journal entry is source of truth)
❌ `ModernTextBox` style on `ComboBox` elements — `ModernTextBox` has `TargetType="TextBox"`, causes `XamlParseException`
❌ `DisplayMemberPath` and `ItemTemplate` on same `ComboBox` — WPF throws `InvalidOperationException`
❌ client-supplied CreatedBy in journal entry requests (always extract from JWT)
❌ Hardcoded userId (createdByUserId: 1) in Customer/Supplier services
❌ lastId + 1 for InvoiceNo generation (use DocumentSequenceService)
❌ PurchaseCost for COGS calculation (use AverageCost)
❌ Clamping negative netRevenue to 0 (return validation error instead)
❌ SupplierPayment CashTransactionType on purchase cancel (use RefundOut)
❌ String-based ReferenceNumber lookup alone (use ReferenceId with fallback)
❌ Missing composite index on JournalEntry(ReferenceType, ReferenceId)
❌ Payment update/delete without reversal journal entries
❌ PurchasePrice/SalePrice on Product entity (use ProductPrices table instead)
❌ Single-currency pricing (must support multi-currency via ProductPrices)
❌ No batch tracking for inventory (must use InventoryBatches for FIFO/FEFO)
❌ Purchase invoice without landed cost distribution (must use AdditionalCharge entity)
❌ Sales quotation without expiry date (quotations must expire)
❌ Payment without allocation tracking (must use PaymentAllocation entity)
❌ CashBox without AccountId FK (must link to Chart of Accounts)
❌ OpeningBalance/CurrentBalance on CashBox entity (balance lives on linked Account only)
❌ BalanceBefore/BalanceAfter on CashTransaction (use RunningBalance instead)
❌ Deposit()/Withdraw() methods on CashBox (removed — service creates CashTransaction records directly)
❌ Client-side balance validation for CashBox transfers (server validates via Account)
❌ CashTransaction.Create() being internal (must be public — callable from service layer)
❌ Fixed AccountsReceivableAccountId/AccountsPayableAccountId in AccountingIntegrationService (use per-entity Customer.AccountId/Supplier.AccountId instead)
❌ Split Cash Sales (1521) and Credit Sales (1522) revenue accounts (use single 1520 instead)
❌ Crediting InventoryAssetAccountId for purchase return reversals (use PurchaseReturnAccountId instead)
❌ Missing PurchaseReturnAccountId in SystemAccountMappings (Purchase Returns account required for financial reporting)
❌ Hardcoding system mapping account IDs in customer/supplier opening entries or payment reversals (accept per-entity accountId parameters)
❌ Journal entry without multi-currency support (must store CurrencyId + ExchangeRate)
❌ Report without Excel export (ALL report DTOs must support ClosedXML)
❌ Hard-coded price levels (use PriceLevel enum + ProductPrices table)
❌ Optional/nullable AccountId on Customer/Supplier (must be mandatory non-nullable int, auto-created by service)
❌ Deleting fiscal year (must use open/close lifecycle, not delete)
❌ Physical count in V1 (deferred to V2 — use opening stock instead)
❌ OpeningBalance/CurrentBalance on Customer/Supplier entity (balance lives on linked Account only)
❌ CurrencyId on Customer/Supplier entity (currency is per-transaction, not per-entity)
❌ Changing IsBaseCurrency after system creation (immutable — locked at setup)
❌ Products without a second unit (must have at least base unit + one additional unit)
❌ ProductPrices without EffectiveFrom/EffectiveTo date ranges (pricing must have date tracking)
❌ InventoryMovement without ProductUnitId (use ProductUnitId instead of raw ProductId)
❌ PurchaseInvoice without `OtherCharges` property (must match SalesInvoice for landed cost consistency)
❌ `AllocateAdditionalCharges()` missing from `PurchaseService.PostAsync()` — landed cost MUST be distributed proportionally
❌ `OtherCharges` missing from `CreatePurchaseInvoiceRequest`/`UpdatePurchaseInvoiceRequest`/`PurchaseInvoiceDto`
❌ `OtherCharges` missing from Desktop VM `RecalculateTotals()`/`BuildRequest()`/`LoadInvoiceAsync()`
❌ Sales Price Enforcement NOT wired — `SalesService.PostAsync()` MUST check `PreventBelowRetailPrice` and `AllowBelowCostSale`
❌ `IProductPriceService` NOT injected into `SalesService` (price enforcement requires it)
❌ `GetDefaultPrice()` returning `0m` stub in Editor VM (breaks `IsPriceOverridden` detection)
❌ `CostInBaseCurrency` hardcoded to `0m` on load (must use actual cost from invoice DTO)
❌ DeliveryCharges NOT in separate revenue account (OtherCharges credited to SalesRevenue instead of `DeliveryChargesRevenue`)
❌ `DeliveryChargesRevenueAccountId` missing from `SystemAccountKey` enum and `AccountingSeeder`
❌ Purchase Return VALIDATION requiring invoice (standalone mode must be allowed)
❌ Purchase Return without journal entries (`AccountingIntegrationService` MUST have `CreatePurchaseReturnEntryAsync`/`ReversePurchaseReturnEntryAsync`)
❌ `ProductUnitId` hardcoded to `1` in Purchase Return editor
❌ `PostedAt`/`CancelledAt` NOT set in `PurchaseReturn.Post()`/`Cancel()` methods
❌ Flexible Input missing — LineTotal column MUST be editable (not `IsReadOnly`)
❌ `LineTotalInput`/`_lastModifiedField`/`_isRecalculating` NOT implemented in line ViewModels
❌ `FlexibleInputCalculator` helper class missing
❌ `RecalculateFromFlexibleInput()` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator must ONLY be called when `_lastModifiedField == Total` — Quantity/Price changes should directly compute `_lineTotalInput = _quantity * _unitPrice`)
❌ `Bank.AccountId` as non-nullable `int` (use `int?` with `SetAccountId()` for auto-creation support)
❌ `CustomerService` looking up AR parent by code `"1210"` (Fixed Assets) instead of `"1103"` (العملاء)
❌ `SupplierService` looking up AP parent by code `"2100"` (doesn't exist) instead of `"1320"` (الموردون)
❌ Missing `POST /api/v1/employees/{id}/auto-create-account` endpoint (Employee custody workflow needs it)
❌ `Account.Create()` for Level 4+ without `allowTransactions: true` (DomainException thrown)
❌ Service interface without concrete implementation class (DI resolution crash)
❌ Report API URL mismatch between Desktop ViewModel and Controller (404 errors)
❌ Payment UpdateAsync without journal entry reversal for posted amounts
❌ Standalone sales return without `CreateSalesReturnEntryAsync()` (missing journal entries)
❌ Report service returning hardcoded stub `"تحت التطوير"` instead of real data
❌ Report ViewModel with PDF export but NO Excel export (incomplete)
❌ `CashBoxReportService` missing implementation (3 API endpoints crash at runtime)
❌ `FinancialReportService.GetCashFlowReportAsync()` returning stub instead of computing from ReceiptVoucher/PaymentVoucher data
❌ `AllowBelowCostSale` BLOCKING instead of WARNING-only (per analysis: "ولا نمنع البيع" — must warn but never block below-cost sales)
❌ `AllowBelowCostSale` defaulting to `"false"` (must default to `true` per analysis spec: "السماح مع تنبيه")
❌ `InventoryService.CreateTransactionAsync()` requiring Desktop to provide TransactionNo (service MUST auto-generate via DocumentSequenceService when `<= 0`)
❌ `InventoryAdjustmentService.PostAsync()` directly setting `WarehouseStock.Quantity` (MUST use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync` for atomic + audit trail)
❌ `InventoryCountService.PostAsync()` creating one Adjustment per line (MUST create ONE adjustment per Post with `ReferenceType = "InventoryCount"`)
❌ `AdjustmentType` validator with range `(1,2)` excluding Correction=3 (MUST use `InclusiveBetween(1, 3)`)
❌ `ReportsController` with `CancellationToken` AFTER optional parameters (MUST precede optional params)
❌ Inventory Operations ViewModels NOT implementing `IDisposable` (EventBus subscriptions MUST be disposed)
❌ `InvoiceLineViewModel`/`PurchaseInvoiceLineViewModel` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator MUST only be called when `_lastModifiedField == Total`)
❌ `SalesReturn.Post()` without `PostedAt = DateTime.UtcNow` (missing audit trail timestamp — same issue as RULE-489 for PurchaseReturn)
❌ `SalesReturn.Cancel()` without `CancelledAt = DateTime.UtcNow` (missing audit trail timestamp)
❌ `SalesReturnService` NOT creating journal entries on Post/Cancel (general ledger out of sync — MUST inject IAccountingIntegrationService)
❌ `ProductUnitId` hardcoded to `1` in ANY ViewModel (breaks inventory tracking — use DefaultPurchaseUnitId/DefaultSalesUnitId instead)
❌ `AllocateAdditionalCharges()` logic inline in `PurchaseService.PostAsync()` (MUST extract to standalone `AdditionalChargeAllocator` helper)
❌ CustomerType/SupplierType in V1 (deferred to V2 — payment type is per-invoice)
❌ User-supplied AccountId in Customer/Supplier requests (auto-created by service)
❌ OpeningBalance input field in Customer/Supplier Desktop forms (journal entry is source of truth)
❌ `ModernTextBox` style on `ComboBox` elements — `ModernTextBox` has `TargetType="TextBox"`, causes `XamlParseException`
❌ `DisplayMemberPath` and `ItemTemplate` on same `ComboBox` — WPF throws `InvalidOperationException`
❌ client-supplied CreatedBy in journal entry requests (always extract from JWT)
❌ Hardcoded userId (createdByUserId: 1) in Customer/Supplier services
❌ lastId + 1 for InvoiceNo generation (use DocumentSequenceService)
❌ PurchaseCost for COGS calculation (use AverageCost)
❌ Clamping negative netRevenue to 0 (return validation error instead)
❌ SupplierPayment CashTransactionType on purchase cancel (use RefundOut)
❌ String-based ReferenceNumber lookup alone (use ReferenceId with fallback)
❌ Missing composite index on JournalEntry(ReferenceType, ReferenceId)
❌ Payment update/delete without reversal journal entries
❌ PurchasePrice/SalePrice on Product entity (use ProductPrices table instead)
❌ Single-currency pricing (must support multi-currency via ProductPrices)
❌ No batch tracking for inventory (must use InventoryBatches for FIFO/FEFO)
❌ Purchase invoice without landed cost distribution (must use AdditionalCharge entity)
❌ Sales quotation without expiry date (quotations must expire)
❌ Payment without allocation tracking (must use PaymentAllocation entity)
❌ CashBox without AccountId FK (must link to Chart of Accounts)
❌ OpeningBalance/CurrentBalance on CashBox entity (balance lives on linked Account only)
❌ BalanceBefore/BalanceAfter on CashTransaction (use RunningBalance instead)
❌ Deposit()/Withdraw() methods on CashBox (removed — service creates CashTransaction records directly)
❌ Client-side balance validation for CashBox transfers (server validates via Account)
❌ CashTransaction.Create() being internal (must be public — callable from service layer)
❌ Fixed AccountsReceivableAccountId/AccountsPayableAccountId in AccountingIntegrationService (use per-entity Customer.AccountId/Supplier.AccountId instead)
❌ Split Cash Sales (1521) and Credit Sales (1522) revenue accounts (use single 1520 instead)
❌ Crediting InventoryAssetAccountId for purchase return reversals (use PurchaseReturnAccountId instead)
❌ Missing PurchaseReturnAccountId in SystemAccountMappings (Purchase Returns account required for financial reporting)
❌ Hardcoding system mapping account IDs in customer/supplier opening entries or payment reversals (accept per-entity accountId parameters)
❌ Journal entry without multi-currency support (must store CurrencyId + ExchangeRate)
❌ Report without Excel export (ALL report DTOs must support ClosedXML)
❌ Hard-coded price levels (use PriceLevel enum + ProductPrices table)
❌ Optional/nullable AccountId on Customer/Supplier (must be mandatory non-nullable int, auto-created by service)
❌ Deleting fiscal year (must use open/close lifecycle, not delete)
❌ Physical count in V1 (deferred to V2 — use opening stock instead)
❌ OpeningBalance/CurrentBalance on Customer/Supplier entity (balance lives on linked Account only)
❌ CurrencyId on Customer/Supplier entity (currency is per-transaction, not per-entity)
❌ Changing IsBaseCurrency after system creation (immutable — locked at setup)
❌ Products without a second unit (must have at least base unit + one additional unit)
❌ ProductPrices without EffectiveFrom/EffectiveTo date ranges (pricing must have date tracking)
❌ InventoryMovement without ProductUnitId (use ProductUnitId instead of raw ProductId)
❌ PurchaseInvoice without `OtherCharges` property (must match SalesInvoice for landed cost consistency)
❌ `AllocateAdditionalCharges()` missing from `PurchaseService.PostAsync()` — landed cost MUST be distributed proportionally
❌ `OtherCharges` missing from `CreatePurchaseInvoiceRequest`/`UpdatePurchaseInvoiceRequest`/`PurchaseInvoiceDto`
❌ `OtherCharges` missing from Desktop VM `RecalculateTotals()`/`BuildRequest()`/`LoadInvoiceAsync()`
❌ Sales Price Enforcement NOT wired — `SalesService.PostAsync()` MUST check `PreventBelowRetailPrice` and `AllowBelowCostSale`
❌ `IProductPriceService` NOT injected into `SalesService` (price enforcement requires it)
❌ `GetDefaultPrice()` returning `0m` stub in Editor VM (breaks `IsPriceOverridden` detection)
❌ `CostInBaseCurrency` hardcoded to `0m` on load (must use actual cost from invoice DTO)
❌ DeliveryCharges NOT in separate revenue account (OtherCharges credited to SalesRevenue instead of `DeliveryChargesRevenue`)
❌ `DeliveryChargesRevenueAccountId` missing from `SystemAccountKey` enum and `AccountingSeeder`
❌ Purchase Return VALIDATION requiring invoice (standalone mode must be allowed)
❌ Purchase Return without journal entries (`AccountingIntegrationService` MUST have `CreatePurchaseReturnEntryAsync`/`ReversePurchaseReturnEntryAsync`)
❌ `ProductUnitId` hardcoded to `1` in Purchase Return editor
❌ `PostedAt`/`CancelledAt` NOT set in `PurchaseReturn.Post()`/`Cancel()` methods
❌ Flexible Input missing — LineTotal column MUST be editable (not `IsReadOnly`)
❌ `LineTotalInput`/`_lastModifiedField`/`_isRecalculating` NOT implemented in line ViewModels
❌ `FlexibleInputCalculator` helper class missing
❌ `RecalculateFromFlexibleInput()` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator must ONLY be called when `_lastModifiedField == Total` — Quantity/Price changes should directly compute `_lineTotalInput = _quantity * _unitPrice`)
❌ `Bank.AccountId` as non-nullable `int` (use `int?` with `SetAccountId()` for auto-creation support)
❌ `CustomerService` looking up AR parent by code `"1210"` (Fixed Assets) instead of `"1103"` (العملاء)
❌ `SupplierService` looking up AP parent by code `"2100"` (doesn't exist) instead of `"1320"` (الموردون)
❌ Missing `POST /api/v1/employees/{id}/auto-create-account` endpoint (Employee custody workflow needs it)
❌ `Account.Create()` for Level 4+ without `allowTransactions: true` (DomainException thrown)
❌ Service interface without concrete implementation class (DI resolution crash)
❌ Report API URL mismatch between Desktop ViewModel and Controller (404 errors)
❌ Payment UpdateAsync without journal entry reversal for posted amounts
❌ Standalone sales return without `CreateSalesReturnEntryAsync()` (missing journal entries)
❌ Report service returning hardcoded stub `"تحت التطوير"` instead of real data
❌ Report ViewModel with PDF export but NO Excel export (incomplete)
❌ `CashBoxReportService` missing implementation (3 API endpoints crash at runtime)
❌ `FinancialReportService.GetCashFlowReportAsync()` returning stub instead of computing from ReceiptVoucher/PaymentVoucher data
❌ `AllowBelowCostSale` BLOCKING instead of WARNING-only (per analysis: "ولا نمنع البيع" — must warn but never block below-cost sales)
❌ `AllowBelowCostSale` defaulting to `"false"` (must default to `true` per analysis spec: "السماح مع تنبيه")
❌ `InventoryService.CreateTransactionAsync()` requiring Desktop to provide TransactionNo (service MUST auto-generate via DocumentSequenceService when `<= 0`)
❌ `InventoryAdjustmentService.PostAsync()` directly setting `WarehouseStock.Quantity` (MUST use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync` for atomic + audit trail)
❌ `InventoryCountService.PostAsync()` creating one Adjustment per line (MUST create ONE adjustment per Post with `ReferenceType = "InventoryCount"`)
❌ `AdjustmentType` validator with range `(1,2)` excluding Correction=3 (MUST use `InclusiveBetween(1, 3)`)
❌ `ReportsController` with `CancellationToken` AFTER optional parameters (MUST precede optional params)
❌ Inventory Operations ViewModels NOT implementing `IDisposable` (EventBus subscriptions MUST be disposed)
❌ `InvoiceLineViewModel`/`PurchaseInvoiceLineViewModel` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator MUST only be called when `_lastModifiedField == Total`)
❌ `SalesReturn.Post()` without `PostedAt = DateTime.UtcNow` (missing audit trail timestamp — same issue as RULE-489 for PurchaseReturn)
❌ `SalesReturn.Cancel()` without `CancelledAt = DateTime.UtcNow` (missing audit trail timestamp)
❌ `SalesReturnService` NOT creating journal entries on Post/Cancel (general ledger out of sync — MUST inject IAccountingIntegrationService)
❌ `ProductUnitId` hardcoded to `1` in ANY ViewModel (breaks inventory tracking — use DefaultPurchaseUnitId/DefaultSalesUnitId instead)
❌ `AllocateAdditionalCharges()` logic inline in `PurchaseService.PostAsync()` (MUST extract to standalone `AdditionalChargeAllocator` helper)
❌ CustomerType/SupplierType in V1 (deferred to V2 — payment type is per-invoice)
❌ User-supplied AccountId in Customer/Supplier requests (auto-created by service)
❌ OpeningBalance input field in Customer/Supplier Desktop forms (journal entry is source of truth)
❌ `ModernTextBox` style on `ComboBox` elements — `ModernTextBox` has `TargetType="TextBox"`, causes `XamlParseException`
❌ `DisplayMemberPath` and `ItemTemplate` on same `ComboBox` — WPF throws `InvalidOperationException`
❌ client-supplied CreatedBy in journal entry requests (always extract from JWT)
❌ Hardcoded userId (createdByUserId: 1) in Customer/Supplier services
❌ lastId + 1 for InvoiceNo generation (use DocumentSequenceService)
❌ PurchaseCost for COGS calculation (use AverageCost)
❌ Clamping negative netRevenue to 0 (return validation error instead)
❌ SupplierPayment CashTransactionType on purchase cancel (use RefundOut)
❌ String-based ReferenceNumber lookup alone (use ReferenceId with fallback)
❌ Missing composite index on JournalEntry(ReferenceType, ReferenceId)
❌ Payment update/delete without reversal journal entries
❌ PurchasePrice/SalePrice on Product entity (use ProductPrices table instead)
❌ Single-currency pricing (must support multi-currency via ProductPrices)
❌ No batch tracking for inventory (must use InventoryBatches for FIFO/FEFO)
❌ Purchase invoice without landed cost distribution (must use AdditionalCharge entity)
❌ Sales quotation without expiry date (quotations must expire)
❌ Payment without allocation tracking (must use PaymentAllocation entity)
❌ CashBox without AccountId FK (must link to Chart of Accounts)
❌ OpeningBalance/CurrentBalance on CashBox entity (balance lives on linked Account only)
❌ BalanceBefore/BalanceAfter on CashTransaction (use RunningBalance instead)
❌ Deposit()/Withdraw() methods on CashBox (removed — service creates CashTransaction records directly)
❌ Client-side balance validation for CashBox transfers (server validates via Account)
❌ CashTransaction.Create() being internal (must be public — callable from service layer)
❌ Fixed AccountsReceivableAccountId/AccountsPayableAccountId in AccountingIntegrationService (use per-entity Customer.AccountId/Supplier.AccountId instead)
❌ Split Cash Sales (1521) and Credit Sales (1522) revenue accounts (use single 1520 instead)
❌ Crediting InventoryAssetAccountId for purchase return reversals (use PurchaseReturnAccountId instead)
❌ Missing PurchaseReturnAccountId in SystemAccountMappings (Purchase Returns account required for financial reporting)
❌ Hardcoding system mapping account IDs in customer/supplier opening entries or payment reversals (accept per-entity accountId parameters)
❌ Journal entry without multi-currency support (must store CurrencyId + ExchangeRate)
❌ Report without Excel export (ALL report DTOs must support ClosedXML export)
❌ Hard-coded price levels (use PriceLevel enum + ProductPrices table)
❌ Optional/nullable AccountId on Customer/Supplier (must be mandatory non-nullable int, auto-created by service)
❌ Deleting fiscal year (must use open/close lifecycle, not delete)
❌ Physical count in V1 (deferred to V2 — use opening stock instead)
❌ OpeningBalance/CurrentBalance on Customer/Supplier entity (balance lives on linked Account only)
❌ CurrencyId on Customer/Supplier entity (currency is per-transaction, not per-entity)
❌ Changing IsBaseCurrency after system creation (immutable — locked at setup)
❌ Products without a second unit (must have at least base unit + one additional unit)
❌ ProductPrices without EffectiveFrom/EffectiveTo date ranges (pricing must have date tracking)
❌ InventoryMovement without ProductUnitId (use ProductUnitId instead of raw ProductId)
❌ PurchaseInvoice without `OtherCharges` property (must match SalesInvoice for landed cost consistency)
❌ `AllocateAdditionalCharges()` missing from `PurchaseService.PostAsync()` — landed cost MUST be distributed proportionally
❌ `OtherCharges` missing from `CreatePurchaseInvoiceRequest`/`UpdatePurchaseInvoiceRequest`/`PurchaseInvoiceDto`
❌ `OtherCharges` missing from Desktop VM `RecalculateTotals()`/`BuildRequest()`/`LoadInvoiceAsync()`
❌ Sales Price Enforcement NOT wired — `SalesService.PostAsync()` MUST check `PreventBelowRetailPrice` and `AllowBelowCostSale`
❌ `IProductPriceService` NOT injected into `SalesService` (price enforcement requires it)
❌ `GetDefaultPrice()` returning `0m` stub in Editor VM (breaks `IsPriceOverridden` detection)
❌ `CostInBaseCurrency` hardcoded to `0m` on load (must use actual cost from invoice DTO)
❌ DeliveryCharges NOT in separate revenue account (OtherCharges credited to SalesRevenue instead of `DeliveryChargesRevenue`)
❌ `DeliveryChargesRevenueAccountId` missing from `SystemAccountKey` enum and `AccountingSeeder`
❌ Purchase Return VALIDATION requiring invoice (standalone mode must be allowed)
❌ Purchase Return without journal entries (`AccountingIntegrationService` MUST have `CreatePurchaseReturnEntryAsync`/`ReversePurchaseReturnEntryAsync`)
❌ `ProductUnitId` hardcoded to `1` in Purchase Return editor
❌ `PostedAt`/`CancelledAt` NOT set in `PurchaseReturn.Post()`/`Cancel()` methods
❌ Flexible Input missing — LineTotal column MUST be editable (not `IsReadOnly`)
❌ `LineTotalInput`/`_lastModifiedField`/`_isRecalculating` NOT implemented in line ViewModels
❌ `FlexibleInputCalculator` helper class missing
❌ `RecalculateFromFlexibleInput()` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator must ONLY be called when `_lastModifiedField == Total` — Quantity/Price changes should directly compute `_lineTotalInput = _quantity * _unitPrice`)
❌ `Bank.AccountId` as non-nullable `int` (use `int?` with `SetAccountId()` for auto-creation support)
❌ `CustomerService` looking up AR parent by code `"1210"` (Fixed Assets) instead of `"1103"` (العملاء)
❌ `SupplierService` looking up AP parent by code `"2100"` (doesn't exist) instead of `"1320"` (الموردون)
❌ Missing `POST /api/v1/employees/{id}/auto-create-account` endpoint (Employee custody workflow needs it)
❌ `Account.Create()` for Level 4+ without `allowTransactions: true` (DomainException thrown)
❌ Service interface without concrete implementation class (DI resolution crash)
❌ Report API URL mismatch between Desktop ViewModel and Controller (404 errors)
❌ Payment UpdateAsync without journal entry reversal for posted amounts
❌ Standalone sales return without `CreateSalesReturnEntryAsync()` (missing journal entries)
❌ Report service returning hardcoded stub `"تحت التطوير"` instead of real data
❌ Report ViewModel with PDF export but NO Excel export (incomplete)
❌ `CashBoxReportService` missing implementation (3 API endpoints crash at runtime)
❌ `FinancialReportService.GetCashFlowReportAsync()` returning stub instead of computing from ReceiptVoucher/PaymentVoucher data
❌ `AllowBelowCostSale` BLOCKING instead of WARNING-only (per analysis: "ولا نمنع البيع" — must warn but never block below-cost sales)
❌ `AllowBelowCostSale` defaulting to `"false"` (must default to `true` per analysis spec: "السماح مع تنبيه")
❌ `InventoryService.CreateTransactionAsync()` requiring Desktop to provide TransactionNo (service MUST auto-generate via DocumentSequenceService when `<= 0`)
❌ `InventoryAdjustmentService.PostAsync()` directly setting `WarehouseStock.Quantity` (MUST use `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync` for atomic + audit trail)
❌ `InventoryCountService.PostAsync()` creating one Adjustment per line (MUST create ONE adjustment per Post with `ReferenceType = "InventoryCount"`)
❌ `AdjustmentType` validator with range `(1,2)` excluding Correction=3 (MUST use `InclusiveBetween(1, 3)`)
❌ `ReportsController` with `CancellationToken` AFTER optional parameters (MUST precede optional params)
❌ Inventory Operations ViewModels NOT implementing `IDisposable` (EventBus subscriptions MUST be disposed)
❌ `InvoiceLineViewModel`/`PurchaseInvoiceLineViewModel` calling `FlexibleInputCalculator.Calculate()` for Quantity/Price changes (calculator MUST only be called when `_lastModifiedField == Total`)
❌ `SalesReturn.Post()` without `PostedAt = DateTime.UtcNow` (missing audit trail timestamp — same issue as RULE-489 for PurchaseReturn)
❌ `SalesReturn.Cancel()` without `CancelledAt = DateTime.UtcNow` (missing audit trail timestamp)
❌ `SalesReturnService` NOT creating journal entries on Post/Cancel (general ledger out of sync — MUST inject IAccountingIntegrationService)
❌ `ProductUnitId` hardcoded to `1` in ANY ViewModel (breaks inventory tracking — use DefaultPurchaseUnitId/DefaultSalesUnitId instead)
❌ `AllocateAdditionalCharges()` logic inline in `PurchaseService.PostAsync()` (MUST extract to standalone `AdditionalChargeAllocator` helper)
❌ ReportExportController returning BadRequest stub instead of routing to export service
❌ CanNavigate() with _ => true fallback (must use _ => false deny-by-default)
❌ Missing permission flags in Permission.cs (must match AGENTS.md Section 6)
❌ Advanced-only screens visible to Basic users without IsAdvancedMode guard
❌ Print endpoints missing for returns (sales-returns, purchase-returns)
❌ OtherCharges missing from InvoicePrintDto and print templates
❌ FooterNote missing from InvoicePrintDto (must display configurable footer from settings)
❌ ThermalReceiptGenerator hardcoding code page 1256 (accept from settings)
❌ JWT tokens without jti claim (breaks token identification)
❌ JWT secret without minimum length validation (< 32 chars)
❌ [Conditional("DEBUG")] as sole SecurityAudit access (add production endpoint)
❌ Missing composite index on JournalEntryLine(JournalEntryId, AccountId)
❌ Orphaned ViewModel registrations after DataTemplate removal (DailyClosureReportViewModel)
❌ Feature-level-only permissions without operation-level granularity
❌ UserRole enum in Domain or Contracts (replaced by DB-driven Role entity)
❌ Hardcoded 3-role model (Admin/Manager/Cashier) — must use 9 DB-driven roles
❌ UserService.CreateAsync always hashing "12345678" without checking request.Password
❌ PermissionService.GetRolePermissionsAsync using Enum.GetValues (must query DB Role entity)
❌ User entity with FullName/Phone/Email/Status/PasswordChangedAt fields (not in schema)
❌ User entity without IsLocked/LoginAttempts (lockout tracking mandatory)
❌ DbSeeder with hardcoded permission count 33 (must be 45 now)
❌ OpeningBalance column on Accounts table (use Journal Entry instead)
❌ User-supplied AccountCode (system-generated via hierarchical numbering)
❌ User-supplied ColorCode (auto-generated from Nature)
❌ Non-thread-safe AccountCode generation (use SemaphoreSlim)
❌ Creating an account with OpeningBalance outside a transaction (must use BeginTransactionAsync)
❌ Level 4 account code shorter than 8 digits (must follow 1-2-4-8 hierarchical scheme)
❌ ContextMenu MenuItem without Arabic ToolTip (add ToolTip="تعديل العنصر المحدد" etc.)
❌ CheckBox without Arabic ToolTip (especially "عرض غير النشطة" / "عرض الملغاة")
❌ Editor form control without Arabic ToolTip (every TextBox/ComboBox needs one)
❌ ComboBoxStyle instead of ModernComboBox style key
❌ Missing ItemsSource on ComboBox (functional bug — ComboBox never populates)
❌ DisplayMemberPath AND ItemTemplate on same ComboBox (InvalidOperationException)
❌ Missing ErrorMessage bar in list view (Border bound to ErrorMessage between header and content)
❌ Missing loading overlay in editor view (IsBusy Border spanning all Grid rows)
❌ Silent success — ViewModel closes/saves without success toast or dialog
❌ Silent failure — HandleFailure() called without ShowErrorAsync dialog
❌ List ViewModel for soft-deletable entity missing `IncludeInactive` property
❌ List XAML view for soft-deletable entity missing `عرض غير النشطة` CheckBox
❌ IncludeInactive toggle with no data reload mechanism (must trigger refresh)
❌ CollectionView not filtering inactive records when IncludeInactive is false
```


---

## 5. Approved NuGet Packages

| Package | Layer |
|---------|-------|
| `Microsoft.EntityFrameworkCore.SqlServer` 10.x | Infrastructure |
| `Microsoft.EntityFrameworkCore.Tools` 10.x | Infrastructure |
| `BCrypt.Net-Next` 4.x | Infrastructure |
| `FluentValidation.AspNetCore` 11.x | Api |
| `Serilog.AspNetCore` 8.x | Api |
| `Serilog.Sinks.File` 5.x | Api |
| `Microsoft.AspNetCore.Authentication.JwtBearer` 10.x | Api |
| `Swashbuckle.AspNetCore` 6.x | Api |
| `Microsoft.Extensions.Http` 10.x | Desktop |
| `System.Text.Json` 10.x | Desktop |
| `ClosedXML` 0.102.x | Desktop (Excel report export — FR-021) |
| `QuestPDF` 2024.3.x | Infrastructure (A4 PDF generation) |
| `SixLabors.ImageSharp` 3.1.x | Infrastructure (logo image resize for print) |
| `System.Drawing.Common` 10.x | Infrastructure (Win32 printer interop) |
| `Microsoft.Extensions.Hosting.WindowsServices` 10.x | Api (Windows Service hosting) |
| `Microsoft.AspNetCore.DataProtection` 10.x | Infrastructure (DPAPI encryption) |
| `Serilog.Sinks.EventLog` 8.x | Api (Windows Service logging) |

**Any package not listed here requires human approval.**

---

## 6. Permissions Matrix

| Feature | Admin (1) | Manager (2) | Accountant (3) | Treasurer (4) | Cashier (5) | Whse Supervisor (6) | Sales Employee (7) | Observer (8) | Branch Manager (9) | API Policy |
|---------|:---------:|:-----------:|:--------------:|:-------------:|:-----------:|:-------------------:|:------------------:|:------------:|:------------------:|------------|
| Sales View | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | AllStaff |
| Sales Create | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ✅ | AllStaff |
| Sales Edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ManagerAndAbove |
| Sales Delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | AdminOnly |
| Sales Cancel | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Sales Return | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ | ManagerAndAbove |
| Sales Print | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ✅ | AllStaff |
| Purchases View | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ | ManagerAndAbove |
| Purchases Create | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Purchases Edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Purchases Cancel | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Purchases Return | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Purchases Print | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Inventory View | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | AllStaff |
| Inventory Transfer | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Inventory Adjust | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Inventory Count | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Warehouse Manage | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Customers View | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | AllStaff |
| Customers Create | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ManagerAndAbove |
| Customers Edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ManagerAndAbove |
| Customers Delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Suppliers View | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ManagerAndAbove |
| Suppliers Create | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Suppliers Edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Suppliers Delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Products View | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ | AllStaff |
| Products Create | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Products Edit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Products Delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Reports View | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ManagerAndAbove |
| Accounting View | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ManagerAndAbove |
| Accounting Manage | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Currencies View | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Currencies Manage | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| FiscalYear Manage | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Employees View | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Employees Manage | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| System Settings | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| System Users | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Backup Manage | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |
| Operations Cashbox | ✅ | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | ✅ | AllStaff |
| Operations Banking | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Operations Expenses | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ManagerAndAbove |
| Audit Log | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | AdminOnly |

---

## 7. Invoice Number Formats

```text
Sales:            INV-{YYYY}-{000001}
Purchases:        PUR-{YYYY}-{000001}
Sales Returns:    SR-{YYYY}-{000001}
Purchase Returns: PR-{YYYY}-{000001}
Stock Transfers:  TRF-{YYYY}-{000001}
Customer Payments:CP-{YYYY}-{000001}
Supplier Payments:SP-{YYYY}-{000001}
```

---

## 8. Cross-Reference Guide

| Topic | Read This File |
|-------|---------------|
| Financial formulas | `docs/CONSTITUTION.md` |
| Full requirements | `docs/PRD-MVP.md` |
| Database schema | `docs/database-schema.md` |
| UI/UX flows | `docs/ui-screens.md` |
| Security details | `.opencode/agent/security-auditor.md` |
| Print specs | `specs/006-printing/plan.md` |
| Code patterns | `.opencode/agent/implement-agent.md` |
| Implementation plan | `docs/MASTER-PLAN.md` |
| Costing & UOM specs | `docs/CONSTITUTION.md` sections 2.24–2.27 |

---

## 9. Before Submitting Code — Checklist

**Every item must be YES:**

- [ ] All money fields = `decimal` (not float/double)?
- [ ] All quantities = `decimal` (not int)?
- [ ] Financial calculations in Domain only?
- [ ] Multi-table operations in a transaction?
- [ ] Stock checked BEFORE transaction?
- [ ] InventoryMovement created for every stock change?
- [ ] Service returns `Result<T>` (no raw exceptions)?
- [ ] Controller has `[Authorize]`?
- [ ] FluentValidation exists for Request model?
- [ ] Serilog logs critical operations?
- [ ] EventBus: subscribe in OnLoad, unsubscribe in Dispose?
- [ ] No business logic in Controller?
- [ ] Fluent API config (no DataAnnotations on entities)?
- [ ] All FKs use `DeleteBehavior.Restrict`?
- [ ] Users soft-deleted only (never hard delete)?
- [ ] Domain Entities use `private set` for critical properties and methods for state changes?
- [ ] Service Layer pattern used (NOT CQRS/MediatR)?
- [ ] Delete operations use DeleteStrategy enum (not direct MessageBox)?
- [ ] Guard Clauses exist for all entity creation?
- [ ] DialogService used instead of MessageBox.Show?
- [ ] Toast notifications for minor success messages?
- [ ] INotifyDataErrorInfo implemented with red border styles?
- [ ] Save buttons ALWAYS enabled (no CanExecute) — validate on click with warning dialog?
- [ ] Pricing stored per ProductUnit (not on Product)?
- [ ] Unit conversions computed in Domain (not UI or Service)?
- [ ] Barcodes stored in UnitBarcode table (not embedded in Unit)?
- [ ] Costing update uses UpdateProductPricingService (not custom logic)?
- [ ] CashBox has AccountId FK linking to Chart of Accounts (balance lives on Account)?
- [ ] CashBox has NO OpeningBalance/CurrentBalance fields (removed)?
- [ ] CashTransaction uses RunningBalance (not BalanceBefore/BalanceAfter)?
- [ ] CashTransaction.Create() is public (not internal)?
- [ ] CashBox auto-creates sub-account under "1101 — النقدية صناديق" when AccountId is null?
- [ ] Deposit()/Withdraw() methods removed from CashBox domain entity?
- [ ] Cash transfer has NO client-side balance validation (server validates via Account)?
- [ ] CashTransaction entries immutable (no direct editing)?
- [ ] ProductPriceHistory recorded on EVERY price/cost change?
- [ ] At least one ProductUnit per product enforced in Domain?
- [ ] Printing uses QuestPDF for A4 (not WPF FixedDocument)?
- [ ] Thermal receipts use Win32 raw printing (not WPF PrintDialog)?
- [ ] PrintService returns `PrintResult` (never throws exceptions)?
- [ ] Desktop calls PrintController API (never prints directly)?
- [ ] Logo is optional — missing logo handled gracefully (null check)?
- [ ] Print settings stored in `SystemSetting` table with `Category = "Print"`?
- [ ] ESC/POS commands built with custom `EscPos` class (not external package)?
- [ ] Infrastructure + Api + Infra.Tests target `net10.0-windows`? (required for Win32 DllImport)
- [ ] Auto-Update NEVER blocks startup (fire-and-forget with 3s delay)?
- [ ] All update service methods return `Result<T>` (not raw exceptions)?
- [ ] SHA256 checksum verified before launching installer?
- [ ] Skipped version stored in `%AppData%` (not `appsettings.json`)?
- [ ] Connection strings encrypted via DPAPI with `"DPAPI:"` prefix?
- [ ] `FirstRunSetupService` uses atomic file write (`.tmp` → `File.Replace()`)?
- [ ] Backup uses raw SQL `BACKUP DATABASE` (not SMO)?
- [ ] Restore uses `ROLLBACK AFTER 30` (not `ROLLBACK IMMEDIATE`)?
- [ ] `ScheduledBackupWorker` uses `int.TryParse` for config values?
- [ ] Windows Service uses `UseWindowsService()` with recovery policy?
- [ ] JWT secret from env var — throws in production if missing?
- [ ] Admin screens use `AdminOnlyViewModel` with constructor injection?
- [ ] `UpdateDialogViewModel` implements `IDisposable` (dispose CTS)?
- [ ] NO `MessageBox.Show` anywhere — use `IDialogService` with styled dialogs?
- [ ] `IDialogService` has `ShowInfoAsync` (blue theme, info icon)?
- [ ] Inno Setup script includes .NET 10 runtime check?
- [ ] ALL ViewModel commands use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] `IsBusy` used instead of `IsLoading` in all ViewModels?
- [ ] MediatR NOT used (Service Layer pattern only)?
- [ ] Legacy WinForms code deleted (not referenced)?
- [ ] MASTER-PLAN.md reflects actual architecture (not aspirational)?
- [ ] API exposes `GET /api/v1/health/database` endpoint?
- [ ] Desktop checks DB connectivity on startup before showing login?
- [ ] `DatabaseErrorDialog` shown on connection failure with Retry/Exit?
- [ ] `ExceptionMiddleware` detects DB exceptions and returns `DATABASE_CONNECTION_ERROR`?
- [ ] `SecureDbContextFactory` falls back to `SALESSYSTEM_DB_CONNECTION` env var?
- [ ] Editors open non-modally via `ScreenWindowService.OpenScreen()` — NOT `ShowDialog()`?
- [ ] `WeakReference<Window>` used for window tracking (not strong references)?
- [ ] `OnClosed` callbacks use `Dispatcher.InvokeAsync()` for UI thread safety?
- [ ] Auto-titles set to Arabic names (e.g., "فاتورة بيع") — not English type names?
- [ ] MainWindow has "فتح نافذة جديدة" menu items for list screens?
- [ ] ViewModel lifecycle: `Cleanup()` called on window close, EventBus unsubscribed?
- [ ] `Cascade positioning` applied: 30px offset × (count % 10) from MainWindow?
- [ ] All catch blocks log via Serilog — NEVER show ex.Message to user?
- [ ] Dialog titles are screen-specific (never generic "خطأ")?
- [ ] `MessageBox.Show` is NOT used anywhere?
- [ ] ALL dialog calls use Async suffix (ShowErrorAsync)?
- [ ] `HandleFailure()` transforms errors to user-friendly Arabic?
- [ ] `App.xaml` uses `ShutdownMode="OnExplicitShutdown"`?
- [ ] LoginWindow close button calls `Application.Current.Shutdown()`?
- [ ] MainWindow handles shutdown on close (except during logout)?
- [ ] No raw HTTP response bodies in user-facing dialogs?
- [ ] Log.Error used for system errors only (not user validation mistakes)?
- [ ] HandleResponseAsync checks content type before parsing error JSON?
- [ ] Log.Warning used for validation/user errors (not Log.Error)?
- [ ] No manual window creation in ViewModels (use _dialogService.ShowDialog)?
- [ ] AddCommand uses RelayCommand (not AsyncRelayCommand) and has NO CanExecute predicate?
- [ ] List ViewModels check ShowDialog return value before refreshing?
- [ ] Editor ViewModels have IDialogService dependency?
- [ ] No MessageBox.Show anywhere in ViewModels?
- [ ] ALL buttons have Arabic ToolTip explaining the action?
- [ ] ToolTips describe user action/consequence (not just repeat button text)?
- [ ] Navigation MenuItems have destination description ToolTips?
- [ ] Empty-state buttons have ToolTips?
- [ ] Error dismiss buttons have ToolTip?
- [ ] Product/Customer/Supplier/Warehouse have NO Code column/property?
- [ ] WarehouseResponse DTO excludes Code field?
- [ ] SalesInvoice and PurchaseInvoice have `int InvoiceNo` (NOT string)?
- [ ] `SalesInvoice.Create()` requires `int invoiceNo` (second param)?
- [ ] `PurchaseInvoice.Create()` requires `int invoiceNo` (third param)?
- [ ] Request DTOs use `int? InvoiceNo` (null = auto-generate via DocumentSequenceService)?
- [ ] Service calls `IDocumentSequenceService.GetNextIntAsync(key)` — NEVER `lastId + 1`?
- [ ] Report DTOs (`SalesReportDto`, `PurchaseReportDto`, `InvoicePrintDto`) use `int InvoiceNo`?
- [ ] `InvoicePrintDto.InvoiceNumber` (string) formatted via `.ToString()` on int?
- [ ] `SupplierInvoiceNo` kept only as supplier's reference (not system InvoiceNo)?
- [ ] UNIQUE index on InvoiceNo per document type?
- [ ] All search/filter uses Id or Name, not Code?
- [ ] Invoice item DTOs carry ProductId only (no ProductCode)?
- [ ] Report DTOs exclude Code fields?
- [ ] Editor ViewModels exclude Code property?
- [ ] LogSystemError used instead of direct Serilog.Log.Error in ViewModels?
- [ ] All hard delete services catch DbUpdateException?
- [ ] Controllers have NO direct DbContext or IUnitOfWork injection?
- [ ] All services return Result<T> (never throw exceptions)?
- [ ] Enum values match AGENTS.md Section 3 exactly?
- [ ] No Cascade delete on any FK?
- [ ] WarehouseStocks has CHECK (Quantity >= 0)?
- [ ] ALL money fields use decimal(18,2) (not 18,4)?
- [ ] ProductPriceHistory has Fluent API config?
- [ ] UnitBarcode has HasQueryFilter(IsActive)?
- [ ] Product/Customer/Supplier Response DTOs have NO Code field?
- [ ] ErrorCodes has NO DuplicateCode constant?
- [ ] PrintDataService returns `Result<InvoicePrintDto>` (not nullable `InvoicePrintDto?`)?
- [ ] Purchase Invoice DataGrid has PriceDifferenceIndicator with orange `#E65100` TextBlock?
- [ ] SettingsView has CostingMethod RadioButton group (WeightedAverage/LastPurchasePrice/SupplierPrice)?
- [ ] SettingsViewModel has CostingMethod, IsWeightedAverageSelected, IsLastPriceSelected, IsSupplierPriceSelected properties?
- [ ] StoreSettingsDto and UpdateSettingsRequest include CostingMethod field?
- [ ] SettingsController Get/Update support CostingMethod via ISystemSettingsRepository?
- [ ] Lists sorted newest-first using OrderByDescending?
- [ ] Dialog PositionOverOwner() guards against self-ownership?
- [ ] WindowStartupLocation.CenterScreen fallback when no valid owner?
- [ ] SetDialogService() called in every Editor ViewModel constructor?
- [ ] All validation uses INotifyDataErrorInfo (no HasXxxError booleans)?
- [ ] ErrorTemplate renders red border + icon for invalid fields?
- [ ] ValidateAsync() calls ClearAllErrors() + AddError() + await ValidateAllAsync()?
- [ ] Sound Service (`ISoundService`) integrated and audible cues triggered?
- [ ] Continuous scanning active with event-driven Dispatcher marshaling?
- [ ] Auto-update checking done asynchronously in background?
- [ ] DPAPI encryption applied to connection string with "DPAPI:" prefix?
- [ ] No `async void` used for operation methods (use `Task` + `ExecuteAsync`)?
- [ ] No shadowing of base class properties (like `DialogService`)?
- [ ] Rate limiting configured (Login: 5/15min, Global: 100/min)?
- [ ] User hard-delete guarded (PermanentDeleteAsync returns Result.Failure)?
- [ ] No plaintext connection strings in any config file?
- [ ] FluentValidators enhanced for all invoice/payment/transfer requests?
- [ ] FallbackErrorDialog exists for unhandled exceptions?
- [ ] Login endpoint has `[EnableRateLimiting("LoginPolicy")]`?
- [ ] Rate limiter middleware placed BEFORE `UseAuthentication()`?
- [ ] All views use compact sizes — no hardcoded `Height=36/40` on buttons/inputs?
- [ ] Header/footer padding uses `12,6` / `12,8` or smaller?
- [ ] Section margins between form fields use `8px` or less?
- [ ] Dialog title fonts = 16 or less, section headers = 14 or less?
- [ ] MainWindow sidebar width = 200 (not 220 / 240)?
- [ ] Empty-state buttons use `Margin=0,12,0,0` Width=140 (not 20px/160)?
- [ ] No `BeginTransactionAsync()` used when `SqlServerRetryingExecutionStrategy` is configured?
- [ ] `ExchangeRate` on payment entities has `.HasPrecision(18, 2)` in Fluent API?
- [ ] `JournalEntry` → `JournalEntryLine` uses `.WithOne(x => x.JournalEntry)` not bare `.WithOne()`?
- [ ] Editor ViewModels call `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` in `ValidateAsync()`?
- [ ] `LogSystemError` NOT called for business validation errors — only for system errors?
- [ ] Editor ViewModels have `IToastNotificationService` for success feedback?
- [ ] `result.IsSuccess` used without `&& result.Value != null` guard (Result<T> invariant guarantees non-null)?
- [ ] `HasChildren()` NOT used as sole parent-account guard — service uses `AnyAsync` DB query?
- [ ] Entity NOT fetched twice in `DeleteAsync()`/`PermanentDeleteAsync()`?
- [ ] Every new entity with an `Explanation` field has it in ALL layers (Domain, EF, DTO, Request, Service, Validator, Seeder)?
- [ ] No `:byte` or unsupported route constraints on controller endpoints?
- [ ] Controller route ranges match the actual enum value range (e.g., AccountType 1-5)?
- [ ] Update Validators have same rules as Create Validators (NameAr, NameEn, ColorCode)?
- [ ] All seeded accounts have Arabic `explanation` text?
- [ ] Account code is read-only during edit mode in Desktop?
- [ ] List ViewModels for Accounts have Edit/Delete commands with toolbar buttons?
- [ ] Search/filter works in BOTH TreeView and DataGrid modes?
- [ ] `nameof` operator used in validator `RuleFor` calls (no string literals)?
- [ ] Health check uses `SecureDbContextFactory.GetDecryptedConnectionString()` (not raw `IConfiguration`)?
- [ ] Customer.CheckCreditLimit() returns bool (never throws)?
- [ ] Supplier.CheckCreditLimit() returns bool (never throws)?
- [ ] AccountId MANDATORY (non-nullable int) on Customer/Supplier entities?
- [ ] CustomerGroup NOT in V1 — no entity, service, controller, or Desktop UI?
- [ ] CustomerType/SupplierType NOT in V1 — payment type per-invoice?
- [ ] Desktop Customer/Supplier Editor has NO CustomerGroup dropdown, AccountId selection, or CustomerType/SupplierType radio?
- [ ] Desktop Supplier Editor has NO OpeningBalance input field?
- [ ] AccountName is display-only in Customer/Supplier editors?
- [ ] Account auto-created under 1210 parent for customers, 2100 parent for suppliers?
- [ ] `ModernTextBox` style NOT used on `ComboBox` elements (use `ModernComboBox`)?
- [ ] `DisplayMemberPath` and `ItemTemplate` NOT both set on same `ComboBox`?
- [ ] Customer/Supplier opening balance creates journal entry (Dr/Cr)?
- [ ] Sales/Purchase post creates journal entries?
- [ ] Cancellation creates reversal entries?
- [ ] Payment Update/Delete creates reversal entries?
- [ ] COGS uses AverageCost (not PurchaseCost)?
- [ ] netRevenue validated (not clamped to zero)?
- [ ] JournalEntriesController extracts userId from JWT?
- [ ] Customer/Supplier services use userId from controller (not hardcoded)?
- [ ] InvoiceNo uses DocumentSequenceService (not lastId+1)?
- [ ] Composite index on JournalEntry(ReferenceType, ReferenceId) exists?
- [ ] CashTransactionType on purchase cancel = RefundOut?
- [ ] All AccountingIntegrationService methods return Result<int>?
- [ ] CashBox.AccountId FK exists with DeleteBehavior.Restrict?
- [ ] CashBox auto-creates Level-4 sub-account under parent "1101 — النقدية صناديق"?
- [ ] CashBoxService computes running balance from CashTransaction sum (no Deposit/Withdraw)?
- [ ] CashBoxDto/CashTransactionDto reflect new architecture (no balance fields)?
- [ ] CashBoxEditorView has Category dropdown, Phone/TaxNumber/Address fields?
- [ ] CashTransferViewModel has NO client-side balance validation?
- [ ] ReportsViewModel uses Account balance (not CashBox OpeningBalance/CurrentBalance)?
- [ ] FinancialReportService uses RunningBalance (not BalanceAfter)?
- [ ] DbSeeder does NOT create default cash box (accounts not yet seeded)?
- [ ] `SystemAccountMappings` has `PurchaseReturnAccountId` (FK to Account)?
- [ ] `AccountingIntegrationService` uses per-entity `Customer.AccountId` / `Supplier.AccountId` — not fixed AR/AP?
- [ ] Sales revenue uses single account `1520 — إيرادات المبيعات` (not split 1521/1522)?
- [ ] Purchase return reversal credits `PurchaseReturnAccountId` (not `InventoryAssetAccountId`)?
- [ ] `CreateCustomerOpeningEntryAsync` / `CreateSupplierOpeningEntryAsync` accept `customerAccountId` / `supplierAccountId` parameters?
- [ ] `ReverseCustomerPaymentEntryAsync` / `ReverseSupplierPaymentEntryAsync` accept `customerAccountId` / `supplierAccountId` parameters?
- [ ] COA account 1630 renamed to `المردودات` with child 1632 `مردودات مشتريات`?
- [ ] All callers updated (CustomerService, SupplierService, PaymentService, ChequeService) to pass per-entity account IDs?
- [ ] Products use ProductPrices table (ProductUnitId + CurrencyId + Price) — NOT RetailPrice/WholesalePrice on Product?
- [ ] Inventory batches tracked via InventoryBatches table (FIFO/FEFO)?
- [ ] Units is independent table (seed data + user-addable) — NOT embedded in ProductUnit?
- [ ] Product entity has DefaultPurchaseUnitId/DefaultSalesUnitId optional fields?
- [ ] Product has at least 2 units (base unit + one additional unit)?
- [ ] Currency.IsBaseCurrency IMMUTABLE after creation (locked at setup)?
- [ ] FractionName stored on Currency entity?
- [ ] IsSystem flag on Currency protects system-seeded currencies from deletion?
- [ ] Customer/Supplier entity has NO OpeningBalance/CurrentBalance/CurrencyId fields?
- [ ] Per-entity account routing: Customer.AccountId / Supplier.AccountId used in journal entries?
- [ ] Perpetual inventory system — no Purchases account, Dr Inventory directly?
- [ ] WarehouseTransfer/WarehouseTransferLine replaced StockTransfer/StockTransferItem?
- [ ] InventoryTransaction/InventoryTransactionLine replaced InventoryMovement?
- [ ] Build: 0 errors, 0 warnings across ALL 6 production projects?
- [ ] CashBoxService registered in API DI and all 13 methods return Result<T>?
- [ ] CashBoxService auto-creates sub-account under "1101 — النقدية صناديق" when AccountId is null?
- [ ] CashBoxService uses IReceiptVoucherService/IPaymentVoucherService (no duplication)?
- [ ] PDF export (QuestPDF) is first-choice export for ALL 27+ report ViewModels?
- [ ] Multi-currency (SelectedCurrencyId, ExchangeRate) wired in SalesInvoiceEditorViewModel?
- [ ] CompanySettings.DefaultCurrencyId typed as `short` (not int) across all layers?
- [ ] PurchaseInvoice has `OtherCharges` (decimal) property with guard clause `otherCharges < 0`?
- [ ] `AllocateAdditionalCharges()` called in `PurchaseService.PostAsync()` — landed cost distributed proportionally?
- [ ] `OtherCharges` included in Create/Update/Desktop/Accounting for purchase invoices?
- [ ] `SalesService.PostAsync()` checks `PreventBelowRetailPrice` and `AllowBelowCostSale` settings?
- [ ] `IProductPriceService` injected into `SalesService` for price enforcement lookups?
- [ ] `DeliveryChargesRevenueAccountId` in `SystemAccountKey` (21) and seeded in COA?
- [ ] DeliveryCharges credited to separate `DeliveryChargesRevenue` account (not SalesRevenue)?
- [ ] Purchase Return supports standalone mode (`PurchaseInvoiceId = null` allowed)?
- [ ] `CreatePurchaseReturnEntryAsync()` / `ReversePurchaseReturnEntryAsync()` exist in `AccountingIntegrationService`?
- [ ] `PostedAt`/`CancelledAt` set in `PurchaseReturn.Post()`/`Cancel()`?
- [ ] `FlexibleInputCalculator` helper class exists with `CalculationField` enum?
- [ ] LineTotal column editable in Sales/Purchase DataGrids (not `IsReadOnly`)?
- [ ] `LineTotalInput`/`_lastModifiedField`/`_isRecalculating` in line ViewModels?
- [ ] `RecalculateFromFlexibleInput()` ONLY calls `FlexibleInputCalculator` when Total is modified (NOT for Quantity/Price)?
- [ ] `Bank.AccountId` is `int?` (nullable) with `SetAccountId()` domain method?
- [ ] `BankService` auto-creates sub-account under parent "1102 — البنوك" when AccountId is null?
- [ ] `BankConfiguration` makes AccountId FK optional (`.IsRequired(false)`)?
- [ ] `EmployeesController` has `POST /api/v1/employees/{id}/auto-create-account` endpoint?
- [ ] `CustomerService.AutoCreateCustomerAccountAsync()` uses parent code `"1103"` (NOT `"1210"`)?
- [ ] `SupplierService.AutoCreateSupplierAccountAsync()` uses parent code `"1320"` (NOT `"2100"`)?
- [ ] All service interfaces have concrete implementations (no DI resolution crash)?
- [ ] Report API endpoints match Desktop ViewModel calls (detailed-stock-ledger, returns, aging)?
- [ ] Payment update/delete creates reversal journal entries?
- [ ] `CreateSalesReturnEntryAsync()` exists in `AccountingIntegrationService`?
- [ ] Report services don't return hardcoded stubs (CashFlowReport)?
- [ ] ALL report ViewModels have Excel export (ClosedXML)?
- [ ] AccountStatementViewModel has Excel export?
- [ ] All stale navigation menu items guarded with "تحت التطوير" dialog (no NullReferenceException)?
- [ ] All 2,083+ tests pass?
- [ ] `SalesReturn.Post()` sets `PostedAt = DateTime.UtcNow` and `SalesReturn.Cancel()` sets `CancelledAt = DateTime.UtcNow`?
- [ ] `SalesReturnService` injects `IAccountingIntegrationService` and creates journal entries on Post (CreateSalesReturnEntryAsync) + Cancel (ReverseSalesReturnEntryAsync)?
- [ ] `ProductUnitId` NOT hardcoded to `1` anywhere in Desktop ViewModels (use product.DefaultPurchaseUnitId / DefaultSalesUnitId)?
- [ ] `AllocateAdditionalCharges()` extracted to standalone `AdditionalChargeAllocator` helper class (not inline in PurchaseService.PostAsync)?
- [ ] `InventoryService.CreateTransactionAsync()` uses `_sequenceService.GetNextIntAsync()` when `TransactionNo <= 0` — NEVER require Desktop to provide TransactionNo?
- [ ] `InventoryAdjustmentService.PostAsync()` updates stock via `IInventoryService.IncreaseStockAsync`/`DecreaseStockAsync` — NEVER direct `WarehouseStock.Quantity` assignment?
- [ ] `InventoryCountService.PostAsync()` creates ONE `InventoryAdjustment` per Post with `ReferenceType = "InventoryCount"` — NOT one per line?
- [ ] InventoryAdjustmentRequestValidator validates `AdjustmentType` with `InclusiveBetween(1, 3)` — NOT `(1, 2)`?
- [ ] `ReportsController` places `CancellationToken` BEFORE optional parameters?
- [ ] All Inventory Operations ViewModels implement `IDisposable` and dispose EventBus subscriptions in `Cleanup()`?
- [ ] ReportExportController properly delegates to export service (no stub)?
- [ ] All Permission.cs flags match AGENTS.md Section 6 matrix?
- [ ] CanNavigate() uses _ => false deny-by-default with all screens explicit?
- [ ] Organization Management and accounting screens have IsAdvancedMode guards?
- [ ] Keyboard shortcuts (F3/F4/F5/F8) defined in MainWindow.InputBindings?
- [ ] InvoicePrintDto has OtherCharges + FooterNote properties?
- [ ] PrintController has returns endpoints (sales-returns, purchase-returns)?
- [ ] ThermalReceiptGenerator accepts code page parameter?
- [ ] JWT tokens include jti claim?
- [ ] JWT secret validated for minimum 32 characters?
- [ ] SecurityAudit has production-callable method?
- [ ] Composite index on JournalEntryLine(JournalEntryId, AccountId) exists?
- [ ] No orphaned ViewModel DI registrations after UI removal?
- [ ] UserService.CreateAsync uses request.Password if provided (never hardcodes password hash)?
- [ ] UserRole enum NOT referenced anywhere (Domain/Enums/UserRole.cs deleted, Contracts/Enums/CoreEnums.cs cleaned)?
- [ ] PermissionService queries DB Role entity (not Enum.GetValues)?
- [ ] At least 45 permissions seeded in DbSeeder?
- [ ] All 9 roles seeded with appropriate permissions?
- [ ] Desktop PermissionManagementView registered in DI?
- [ ] Desktop AuditLogListView registered in DI?
- [ ] Desktop PasswordChangeView registered in DI?
- [ ] Password change screen shown on first login (MustChangePassword=true)?
- [ ] Account lockout after 5 failed attempts?
- [ ] AccountCode uses hierarchical expanding numbering (1-2-4-8 digits)?
- [ ] AccountCode is system-generated (never user-supplied)?
- [ ] ColorCode is auto-generated from Nature (never user-supplied)?
- [ ] OpeningBalance removed from Account entity (handled via Journal Entry)?
- [ ] AccountCodeGeneratorService uses SemaphoreSlim for thread safety?
- [ ] Opening balance Journal Entry wrapped in transaction with account creation?
- [ ] Seeder uses 74 accounts (5+8+24+37) with hierarchical codes?
- [ ] EVERY ViewModel operation has BOTH success toast AND error dialog?
- [ ] ALL list views have ErrorMessage bar (Border with StringNotEmptyToVisibility)?
- [ ] ALL editor views have loading overlay (IsBusy ProgressBar)?
- [ ] All list ViewModels have IncludeInactive property? (for soft-deletable entities)
- [ ] All list XAML views have "عرض غير النشطة" CheckBox?
- [ ] ALL ContextMenu MenuItems have Arabic ToolTips?
- [ ] ALL CheckBox controls have Arabic ToolTips?
- [ ] ALL form controls have Arabic ToolTips?
- [ ] ComboBox elements missing ItemsSource have been fixed?
- [ ] ComboBoxStyle reference changed to ModernComboBox?
- [ ] No silent Save/Post/Cancel/Delete without user feedback?
- [ ] All list ViewModels for soft-deletable entities have IncludeInactive property?
- [ ] All list XAML views for soft-deletable entities have "عرض غير النشطة" CheckBox with ToolTip?
- [ ] Client-side filtering implemented for IncludeInactive toggle (CollectionView filter or API param)?
- [ ] Entities with status-based lifecycles use IncludeCancelled instead of IncludeInactive where appropriate?
- [ ] Product entity has NO TaxId — TaxId is on SalesInvoice/PurchaseInvoice only?
- [ ] Product entity has Barcode property (Products.Barcode varchar(50) null unique filtered)?
- [ ] Product.ValidateUnits() enforces minimum 2 units (base + one additional)?
- [ ] Product.RemoveUnit() guards at 2 units (cannot remove below minimum)?
- [ ] Product creation = 3 tables atomic via ExecuteTransactionAsync (Products + ProductUnits + ProductPrices)?
- [ ] CustomerService.CreateAsync wrapped in ExecuteTransactionAsync<Result<CustomerDto>>?
- [ ] SupplierService.CreateAsync wrapped in ExecuteTransactionAsync<Result<SupplierDto>>?
- [ ] CashBoxService.CreateAsync wrapped in ExecuteTransactionAsync<Result<CashBoxDto>>?
- [ ] BankService.CreateAsync wrapped in ExecuteTransactionAsync<Result<BankDto>>?
- [ ] SalesService.CreateAsync uses ExecuteTransactionAsync (not raw BeginTransactionAsync)?
- [ ] Opening stock on Product creation is a SEPARATE inventory transaction (not on Product entity)?
- [ ] No TaxId in CreateProductRequest, ProductDto, or Product editor XAML?
- [ ] Product editor XAML has no TaxId binding (Tax is invoice-level only)?
- [ ] Service creating Account + Entity wraps BOTH in single ExecuteTransactionAsync (prevents orphaned accounts)?
- [ ] Product unit validation shows Arabic message explaining minimum 2 units requirement?
- [ ] ALL 22 transaction operations wrapped in `ExecuteTransactionAsync`? (SalesService.Post/Cancel, PurchaseService.Post/Cancel, SalesReturnService.Post/Cancel, PurchaseReturnService.Post/Cancel, CustomerReceiptService.Create/Post/Cancel/Delete, SupplierPaymentService.Create/Post/Cancel/Delete, ExpenseService.Create/Post/Cancel, InventoryAdjustmentService.Post/Cancel, InventoryCountService.Post/Cancel, WarehouseTransferService.Create/Post/Cancel, ReceiptVoucherService.Post/Cancel, PaymentVoucherService.Post/Cancel, InventoryService.CreateTransfer/PostTransfer/CancelTransfer)
- [ ] `InventoryTransaction` + `InventoryTransactionLine` created for EVERY stock-affecting operation? (Sale, Purchase, SaleReturn, PurchaseReturn, Adjustment, Count, Transfer)
- [ ] `ExpenseService` creates journal entries on Post (`Dr ExpenseAccount / Cr CashBox.Account`) and reversal on Cancel?
- [ ] `ExpenseService` uses `IDocumentSequenceService.GetNextIntAsync("Expense", ct)` — NOT `ToListIgnoreFiltersAsync().Max() + 1`?
- [ ] `DeleteAsync`/`CancelAsync` methods call `SaveChangesAsync` before returning? (CustomerReceiptService, SupplierPaymentService)
- [ ] `InventoryTransaction` created via `InventoryTransaction.Create()` + `AddLine()` inside service wrappers — NOT inside `IncreaseStockAsync`/`DecreaseStockAsync`?
- [ ] No `BeginTransactionAsync` used directly — all wrapped via `ExecuteTransactionAsync` (RULE-275)?
