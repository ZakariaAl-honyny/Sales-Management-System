# AGENTS.md — Sales Management System (v4.0 Expansion)
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
| Print specs | `.opencode/agent/printing.md` |
| Code patterns | `.opencode/agent/implement-agent.md` |

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