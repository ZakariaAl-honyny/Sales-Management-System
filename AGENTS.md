# AGENTS.md — Sales Management System (v4.5 Refactored)
# READ THIS FILE FIRST — BEFORE WRITING ANY CODE
# Platform: .NET 10 LTS | Clean Architecture
# WPF Desktop + ASP.NET Core 10 API + SQL Server

---

<!-- SPECKIT START -->
**Active Feature Plan**: [specs/006-printing/plan.md](specs/006-printing/plan.md)
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
// Sales LineTotal — compute inside SalesInvoiceItem entity
LineTotal = (Quantity * UnitPrice) - DiscountAmount;

// Purchase LineTotal — compute inside PurchaseInvoiceItem entity
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
Customer.CurrentBalance > 0 = Customer owes US money
Customer.CurrentBalance < 0 = We owe the customer
Supplier.CurrentBalance > 0 = We owe the supplier
Supplier.CurrentBalance < 0 = Supplier owes US
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

### 2.18 Wholesale/Retail Pricing (v4.1)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-045 | Product has TWO prices: `RetailPrice` and `WholesalePrice` - NEVER compute prices dynamically |
| RULE-046 | Use `Product.GetPriceByUnit(UnitType)` to get correct price - NEVER use conditional logic outside Domain |
| RULE-047 | Use `Product.ConvertToSmallestUnit(quantity, unitType)` for unit conversion - NEVER compute conversion in UI or Service |
| RULE-048 | Use `Stock.DeductStock(quantity, unitType, conversionFactor)` - conversion happens inside Domain |
| RULE-049 | Invoice items store `SaleMode` (Retail/Wholesale) to determine which price was used |

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

**Entities with Guard Clauses:** Product, Customer, Supplier, SalesInvoice, PurchaseInvoice, WarehouseStock, StockTransfer, SalesReturn, PurchaseReturn, User, Category, Unit, Warehouse, DocumentSequence, StoreSettings, InventoryMovement, CustomerPayment, SupplierPayment, SalesInvoiceItem, PurchaseInvoiceItem, SalesReturnItem, PurchaseReturnItem, StockTransferItem

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
| RULE-059 | Save buttons disabled via `CanExecute` when HasErrors |

**Implementation:**
```csharp
// ViewModelBase methods
public void AddError(string propertyName, string errorMessage);
public void ClearErrors(string propertyName);
public void ClearAllErrors();
public bool HasErrors { get; }
```

**XAML Red Border Style:**
```xml
<Style x:Key="ValidationTextBoxStyle" TargetType="TextBox">
    <Setter Property="Validation.ErrorTemplate">
        <Setter.Value>
            <ControlTemplate>
                <DockPanel>
                    <Border BorderBrush="Red" BorderThickness="1">
                        <AdornedElementPlaceholder/>
                    </Border>
                </DockPanel>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

**Validation Rules (Arabic):**
- `"يجب اختيار منتج"` (Product required)
- `"الكمية يجب أن تكون أكبر من صفر"` (Quantity must be > 0)
- `"السعر لا يمكن أن يكون سالباً"` (Price cannot be negative)

### 2.24 Dynamic Unit of Measure (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-060 | `ProductUnit` stores conversion factor from base unit — use `ConvertToUnit(decimal quantity, int fromUnitId, int toUnitId)` for all unit conversions |
| RULE-061 | Base unit always has `ConversionFactor = 1` (represents the smallest/foundational unit) |
| RULE-062 | Derived units `ConversionFactor > 1` (e.g., Box=24 means 1 Box = 24 Base units) |
| RULE-063 | `UnitBarcode` stores ALL barcodes per product-unit combination — never embed barcode in Unit entity |
| RULE-064 | `SmartUnitFormatter` selects best display unit based on quantity threshold — use in UI only |
| RULE-065 | Pricing: `RetailPrice` and `WholesalePrice` stored per `ProductUnit` (not per Product) — use `ProductUnit.GetPriceByUnit(UnitType)` |
| RULE-066 | Cost cascade: When purchase cost updates via WeightedAverage, ALL product units recalculate from base unit cost × conversion factor |
| RULE-067 | `ProductMustHaveAtLeastOneUnit` rule enforced in Domain — throw `DomainException` if deleting last unit |

**ProductUnit Entity pattern:**
```csharp
public class ProductUnit
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string UnitName { get; private set; }      // e.g., "Piece", "Box", "Carton"
    public decimal ConversionFactor { get; private set; } // Base=1, Box=24, Carton=144
    public decimal RetailPrice { get; private set; }   // Price for retail sales
    public decimal WholesalePrice { get; private set; } // Price for wholesale sales
    public bool IsBaseUnit { get; private set; }      // Exactly one per product
    public ICollection<UnitBarcode> Barcodes { get; private set; }

    public decimal ConvertToUnit(decimal quantity, decimal targetFactor)
    {
        var baseQty = quantity * ConversionFactor;
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
| RULE-074 | Cost cascade: ALL product units updated from base unit cost × conversion factor |
| RULE-075 | `UpdateProductPricingService` handles all three methods — NEVER write costing logic outside this service |
| RULE-076 | Add audit entry in `ProductPriceHistory` on EVERY cost change |

**WeightedAverage formula (C#):**
```csharp
var totalOldValue = oldStock * oldAvgCost;
var totalNewValue = newQty * newUnitCost;
var newAvgCost = (totalOldValue + totalNewValue) / (oldStock + newQty);
// Result: decimal with precision (18,2)
```

### 2.26 Cash Boxes (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-077 | CashBox has `OpeningBalance`, `CurrentBalance` — computed from `CashTransaction` sum |
| RULE-078 | `CashTransaction` records: OpeningBalance, Income, Expense, Transfer, Refund, Payment |
| RULE-079 | Every invoice payment references `CashBoxId` — link between invoice and cash box |
| RULE-080 | `CashBox.CurrentBalance` NEVER goes negative — validate before dispensing |
| RULE-081 | Cash transfer between boxes requires TWO transactions (Out from source, In to destination) |
| RULE-082 | Cash transactions are immutable once created — no editing, cancellation via offsetting entry |
| RULE-083 | `DailyClosure` computes: OpeningBalance + TotalIncome - TotalExpense = ClosingBalance |

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

---

## 3. Enums (Use These EXACT Values)

```csharp
public enum UserRole : byte { Admin = 1, Manager = 2, Cashier = 3 }
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

| Feature | Admin (1) | Manager (2) | Cashier (3) | API Policy |
|---------|-----------|-------------|-------------|------------|
| Sales Invoice | ✅ | ✅ | ✅ | AllStaff |
| Sales Return | ✅ | ✅ | ✅ | AllStaff |
| Purchase Invoice | ✅ | ✅ | ❌ | ManagerAndAbove |
| Purchase Return | ✅ | ✅ | ❌ | ManagerAndAbove |
| Products CRUD | ✅ | ✅ | ❌ | ManagerAndAbove |
| Customers CRUD | ✅ | ✅ | View Only | AllStaff |
| Suppliers CRUD | ✅ | ✅ | ❌ | ManagerAndAbove |
| Warehouses CRUD | ✅ | ❌ | ❌ | AdminOnly |
| Stock Transfer | ✅ | ✅ | ❌ | ManagerAndAbove |
| Reports | ✅ | ✅ | ❌ | ManagerAndAbove |
| Settings | ✅ | ❌ | ❌ | AdminOnly |
| User Management | ✅ | ❌ | ❌ | AdminOnly |

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
| Full requirements | `docs/PRD-MVP-v3.0.md` |
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
- [ ] Read and Write operations are separated (CQRS)?
- [ ] Delete operations use DeleteStrategy enum (not direct MessageBox)?
- [ ] Guard Clauses exist for all entity creation?
- [ ] DialogService used instead of MessageBox.Show?
- [ ] Toast notifications for minor success messages?
- [ ] INotifyDataErrorInfo implemented with red border styles?
- [ ] Save buttons disabled when form has errors (CanExecute)?
- [ ] Pricing stored per ProductUnit (not on Product)?
- [ ] Unit conversions computed in Domain (not UI or Service)?
- [ ] Barcodes stored in UnitBarcode table (not embedded in Unit)?
- [ ] Costing update uses UpdateProductPricingService (not custom logic)?
- [ ] CashBox.CurrentBalance validated before dispensing?
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