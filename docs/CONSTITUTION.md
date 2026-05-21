# Project Constitution — Sales Management System
# Version: 2.2 (v4.3) | Platform: .NET 10 LTS | Date: 2026

---

## 1. Project Identity

| Field            | Value                                         |
|------------------|-----------------------------------------------|
| Project Name     | Sales Management System                       |
| Version          | MVP v3.0                                      |
| Target Platform  | .NET 10 LTS (Support until Nov 2028)          |
| Architecture     | Clean Architecture + CQRS (6-project solution)|
| Database         | SQL Server 2019+                              |
| Desktop UI       | WPF (Windows Presentation Foundation)         |
| Backend API      | ASP.NET Core 10 Web API                       |
| ORM              | Entity Framework Core 10                      |
| Future Expansion | Web (Blazor) + Mobile (.NET MAUI 10)          |

---

## 2. Non-Negotiable Rules
### These rules override ANY other instruction from any source.

---

### 2.1 Financial Calculations

ALL monetary values: `decimal(18,2)` — NEVER `float` / `double` / `real` / SQL `money`.
ALL quantities: `decimal(18,3)` — NEVER `int` unless human explicitly approves.

**Canonical Formulas (computed in Domain Layer ONLY):**

```csharp
// Sales LineTotal — inside SalesInvoiceItem entity
// Quantity is always stored in Retail Units. 
// If Mode == Wholesale, Price is WholesalePrice, and QtyInRetail = Qty * ConversionFactor.
LineTotal = (Quantity * UnitPrice) - DiscountAmount;

// Purchase LineTotal — inside PurchaseInvoiceItem entity
LineTotal = (Quantity * UnitCost) - DiscountAmount;

// Invoice totals — inside Invoice entity
SubTotal    = Items.Sum(i => i.LineTotal);
TotalAmount = SubTotal - InvoiceDiscount + TaxAmount;
DueAmount   = TotalAmount - PaidAmount;

// Wholesale/Retail Conversion (Domain Only)
// RetailQty = (Mode == Wholesale) ? InputQty * ConversionFactor : InputQty;
// Stock Deduction ALWAYS uses RetailQty.
```

**Critical Constraint:** `PaidAmount <= TotalAmount` — enforced in Domain entity AND as DB CHECK constraint.

---

### 2.2 Database Transactions

Every operation affecting more than one table: inside `BeginTransactionAsync`.

**Transaction Protocol:**
1. Validate ALL preconditions (stock, balances) BEFORE opening transaction
2. `BeginTransactionAsync()`
3. Save invoice → get ID
4. Modify stock (using invoice ID as reference)
5. Create InventoryMovement records
6. Update customer/supplier balance
7. `CommitAsync()`
8. On ANY exception: `RollbackAsync()` — NO partial commits EVER

---

### 2.3 Result Pattern

- Every Service method returns: `Result<T>` or `Result`
- No raw exceptions exposed to Controllers or UI
- Controllers translate Result to HTTP status codes only

---

### 2.4 Invoice Lifecycle

```text
Draft (1) → Posted (2) → Cancelled (3)

ALLOWED:
  Draft    → Posted      ✅ (triggers stock + balance changes)
  Draft    → Cancelled   ✅ (no stock/balance impact)
  Posted   → Cancelled   ✅ (MUST reverse stock + balance)

FORBIDDEN:
  Posted   → Draft       ❌ NEVER
  Cancelled → anything   ❌ NEVER (terminal state)
```

- NO hard delete for any invoice — EVER
- NO editing a Posted invoice — cancel + create new
- Cancellation MUST reverse ALL stock and balance effects

---

### 2.5 Stock Integrity

- Validate stock availability BEFORE opening transaction (Stock is always in Retail Units)
- Deduct stock AFTER saving invoice (to have reference ID)
- Record EVERY stock change in InventoryMovements table
- NO negative quantities — enforced at DB level: `CHECK (Quantity >= 0)`

**InventoryMovement Required Fields:**
`ProductId`, `WarehouseId`, `MovementType`, `QuantityChange`, `QuantityBefore`, `QuantityAfter`, `ReferenceType`, `ReferenceId`, `MovementDate`, `CreatedByUserId`

---

### 2.6 Balance Direction Convention

| Entity | Positive Balance | Negative Balance |
|--------|-----------------|------------------|
| Customer > 0 | Customer owes US money | — |
| Customer < 0 | — | We owe the customer |
| Supplier > 0 | We owe the supplier | — |
| Supplier < 0 | — | Supplier owes US |

---

### 2.7 Architecture Boundaries

- Desktop NEVER connects to database directly — only via `HttpClient` → API
- API Controllers NEVER contain business logic — delegate to Application layer
- Domain layer has ZERO dependencies on Infrastructure (no EF Core, no NuGet)
- All multi-table operations use `IUnitOfWork` pattern
- **CQRS Principle**: Separate Read operations (Queries) from Write operations (Commands).

---

### 2.8 Security Rules

- ALL API endpoints require `[Authorize]` with JWT Bearer (except `/api/auth/login`)
- Passwords: BCrypt hash, work factor = 12 — NEVER plain text
- Connection strings: environment variables or encrypted config
- JWT token expiry: 8 hours
- JWT claims: `UserId`, `UserName`, `FullName`, `role` (numeric: 1/2/3)
- Token storage (Desktop): in-memory ONLY — never persisted to disk
- NO sensitive data in logs

---

### 2.9 Validation Strategy (4 Layers)

| Layer | What It Validates | Example |
|-------|------------------|---------|
| Domain | Business rules in Entity methods | `PaidAmount <= TotalAmount` |
| Application | Pre-conditions in Service methods | Stock availability |
| API | FluentValidation on all Request models | `Quantity > 0` |
| Database | CHECK constraints as final defense | `Quantity >= 0` |

---

### 2.10 Logging Standard

- Framework: Serilog — NEVER `Console.WriteLine`
- Log file: `logs/sales-system-{Date:yyyyMMdd}.log` (rolling daily, 30 days retention)
- ALWAYS log: exceptions, invoice creation/cancellation, stock changes, login attempts
- NEVER log: passwords, full connection strings, personal data

---

### 2.11 EF Core Conventions

| Convention | Mandate |
|-----------|---------|
| Config style | Fluent API ONLY — no DataAnnotations on Entities |
| Delete behavior | Restrict on ALL FKs — NEVER cascade |
| Soft delete | Global query filter: `IsActive == true` |
| Strings | `nvarchar` with explicit MaxLength |
| Decimals | `.HasPrecision(18, 2)` or `.HasPrecision(18, 3)` |
| Audit fields | `CreatedByUserId int FK`, `CreatedAt datetime2` |

---

### 2.12 Audit Trail

- ALL invoice/financial tables use `CreatedByUserId int NULL FK` referencing Users table
- Users table MUST use soft delete only (`IsActive = false`) — NEVER hard delete
- This ensures FK integrity is maintained for all historical records

---

### 2.13 Delete Strategy (v4.2)

Three-option delete confirmation for ALL entities:

| Strategy | Enum Value | Behavior |
|----------|-----------|---------|
| Cancel | 0 | Abort operation, no changes |
| Deactivate | 1 | Soft delete — `IsActive = false` |
| Permanent | 2 | Hard delete — physical DB removal (if not referenced) |

**Enum:**
```csharp
public enum DeleteStrategy { Cancel = 0, Deactivate = 1, Permanent = 2 }
```

**Implementation Pattern:**
```csharp
var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المنتج: {name}");
if (strategy == DeleteStrategy.Cancel) return;

if (strategy == DeleteStrategy.Deactivate)
    await _service.DeleteAsync(id); // Soft delete
else if (strategy == DeleteStrategy.Permanent)
    await _service.DeletePermanentlyAsync(id); // Hard delete with reference check
```

**Reference Validation Required:**
- Product → Check SalesInvoiceItems, PurchaseInvoiceItems
- Category → Check Products
- Unit → Check Products (UnitId, RetailUnitId, WholesaleUnitId)
- Warehouse → Check WarehouseStocks, StockTransfers
- Customer → Check SalesInvoices, CustomerPayments
- Supplier → Check PurchaseInvoices, SupplierPayments
- User → Check not last Admin

---

### 2.14 Defensive Programming (v4.2)

ALL Domain entities MUST have Guard Clauses in constructors/factories:

```csharp
public static Product Create(...)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new DomainException("الاسم مطلوب");
    if (purchasePrice < 0)
        throw new DomainException("سعر الشراء لا يمكن أن يكون سالباً");
    if (retailPrice < 0)
        throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");
    if (conversionFactor <= 0)
        throw new DomainException("نسبة التحويل يجب أن تكون أكبر من صفر");
    // ...
}
```

**Entities with Guard Clauses:** Product, Customer, Supplier, SalesInvoice, PurchaseInvoice, WarehouseStock, StockTransfer, SalesReturn, PurchaseReturn, User, Category, Unit, Warehouse, DocumentSequence, StoreSettings, InventoryMovement, CustomerPayment, SupplierPayment, SalesInvoiceItem, PurchaseInvoiceItem, SalesReturnItem, PurchaseReturnItem, StockTransferItem, ProductUnit, UnitBarcode, CashBox, CashTransaction, ProductPriceHistory

**Exception Type:** `DomainException` — NEVER `ArgumentException` in Domain layer.

---

### 2.15 WPF Interactive Dialogs (v4.2)

**DialogService Interface:**
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

**Styled Dialogs** (RTL Arabic):
- ErrorDialog — Red theme, ✕ icon
- SuccessDialog — Green theme, ✓ icon
- WarningDialog — Yellow theme, ⚠ icon
- ConfirmationDialog — Blue theme, ? icon (Yes/No)
- DeleteConfirmationDialog — 3 buttons: Cancel/Deactivate/Delete

**NEVER use raw `MessageBox.Show` in ViewModels.**

---

### 2.16 Toast Notifications (v4.2)

**For minor success messages — auto-dismiss:**
```csharp
_toastService.ShowSuccess("تم حذف المنتج بنجاح");  // Green, 3s
_toastService.ShowError("فشل الحذف");              // Red, 5s
_toastService.ShowInfo("تم التحديث");               // Blue, 3s
```

**Location:** `Services/App/Toast/ToastWindow.xaml`

---

### 2.17 Real-Time UI Validation (v4.2)

**ViewModelBase implements INotifyDataErrorInfo:**
```csharp
public void AddError(string propertyName, string errorMessage);
public void ClearErrors(string propertyName);
public bool HasErrors { get; }
```

**Save Button CanExecute:**
```csharp
public bool CanSave => !HasErrors && !string.IsNullOrWhiteSpace(Name);
// Button.IsEnabled="{Binding CanSave}"
```

**Validation Error Messages (Arabic):**
- "الاسم مطلوب"
- "الكمية يجب أن تكون أكبر من الصفر"
- "السعر لا يمكن أن يكون سالباً"
- "يجب اختيار منتج"

---

### 2.18 API Permanent Delete Endpoints (v4.2)

| Endpoint | Description |
|----------|-------------|
| `DELETE /api/v1/products/{id}` | Soft delete (IsActive=false) |
| `DELETE /api/v1/products/permanent/{id}` | Hard delete with reference check |
| Same pattern for all entities |

Reference validation returns 400 Bad Request if entity is used in transactions.

---

### 2.19 Dynamic Unit of Measure (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-060 | `ProductUnit` stores conversion factor from base unit |
| RULE-061 | Base unit always has `ConversionFactor = 1` |
| RULE-062 | Derived units `ConversionFactor > 1` |
| RULE-063 | `UnitBarcode` stores ALL barcodes per product-unit combination |
| RULE-064 | `SmartUnitFormatter` selects best display unit based on quantity — UI only |
| RULE-065 | `RetailPrice` and `WholesalePrice` stored per `ProductUnit` (not per Product) |
| RULE-066 | Cost cascade: ALL product units recalculate from base unit cost × conversion factor |
| RULE-067 | `ProductMustHaveAtLeastOneUnit` — throw `DomainException` if deleting last unit |

**Conversion Pattern:**
```csharp
var baseQty = quantity * sourceUnit.ConversionFactor;
var targetQty = baseQty / targetUnit.ConversionFactor;
// Example: 2 boxes (factor 24) → pieces = 2 * 24 / 1 = 48 pieces
```

**Entities:** `ProductUnit`, `UnitBarcode`

---

### 2.20 Costing Strategy (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-068 | Costing method stored in `SystemSettings` table — seeded as `WeightedAverage` (1) |
| RULE-069 | Three methods: WeightedAverage (1), LastPurchasePrice (2), SupplierPrice (3) |
| RULE-070 | Costing update fires AFTER purchase invoice is saved and has an ID |
| RULE-071 | WeightedAverage = `(OldStock * OldAvgCost + NewQty * NewUnitCost) / (OldStock + NewQty)` |
| RULE-072 | LastPurchasePrice = overwrite `AvgCost` with incoming `UnitCost` directly |
| RULE-073 | SupplierPrice = use `Product.SupplierPrice` (catalog price) — no cost calculation |
| RULE-074 | Cost cascade: ALL product units updated |
| RULE-075 | `UpdateProductPricingService` handles all three methods |
| RULE-076 | Add audit entry in `ProductPriceHistory` on EVERY cost change |

**Service:** `UpdateProductPricingService` in `SalesSystem.Application` layer.

---

### 2.21 Cash Boxes (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-077 | CashBox has `OpeningBalance`, `CurrentBalance` — computed from `CashTransaction` sum |
| RULE-078 | `CashTransaction` records: OpeningBalance, Income, Expense, Transfer, Refund, Payment |
| RULE-079 | Every invoice payment references `CashBoxId` |
| RULE-080 | `CashBox.CurrentBalance` NEVER goes negative |
| RULE-081 | Cash transfer between boxes requires TWO transactions (Out + In) |
| RULE-082 | Cash transactions immutable — no editing, cancellation via offsetting entry |
| RULE-083 | `DailyClosure` computes: OpeningBalance + TotalIncome - TotalExpense = ClosingBalance |

**CashTransactionType:**
```csharp
public enum CashTransactionType : byte
{
    OpeningBalance = 1, SalesIncome = 2, Expense = 3,
    TransferOut = 4, TransferIn = 5, RefundOut = 6,
    SupplierPayment = 7, CustomerPayment = 8
}
```

**Entities:** `CashBox`, `CashTransaction`

---

### 2.22 Product Price History (v4.3)

| RULE | DIRECTIVE |
|------|-----------|
| RULE-084 | `ProductPriceHistory` records EVERY price/cost change |
| RULE-085 | Fields: `ProductUnitId`, `OldRetailPrice`, `NewRetailPrice`, `OldWholesalePrice`, `NewWholesalePrice`, `OldCost`, `NewCost`, `ChangedByUserId`, `ChangeReason` |
| RULE-086 | Triggers: Purchase invoice, manual price adjustment, supplier sync |
| RULE-087 | History query available in Reports — kept indefinitely |

**Entity:** `ProductPriceHistory`

---

## 3. Technology Stack

### Backend

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 LTS |
| Framework | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2019+ |
| Validation | FluentValidation 11.x |
| Logging | Serilog + Serilog.Sinks.File |
| Auth | JWT Bearer |
| Password Hash | BCrypt.Net-Next (work factor: 12) |
| API Docs | Swashbuckle (Swagger) |

### Frontend (Desktop)

| Component | Technology |
|-----------|-----------|
| UI | WPF (Windows Presentation Foundation) |
| HTTP | HttpClient + IHttpClientFactory |
| JSON | System.Text.Json |
| Architecture | Shell + UserControls + EventBus (Pub/Sub) |

### Deployment

| Component | Technology |
|-----------|-----------|
| API Hosting | Windows Service |
| Desktop | MSI Installer (Inno Setup) |
| Config Security | Windows DPAPI or Environment Variables |

---

## 4. User Roles & Permissions

**Enum:** `Admin = 1, Manager = 2, Cashier = 3`

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
| Backup/Restore | ✅ | ❌ | ❌ | AdminOnly |

---

## 5. Document Sequence Format

| Document Type | Prefix | Format |
|--------------|--------|--------|
| Sales Invoice | INV | `INV-{YYYY}-{000001}` |
| Purchase Invoice | PUR | `PUR-{YYYY}-{000001}` |
| Sales Return | SR | `SR-{YYYY}-{000001}` |
| Purchase Return | PR | `PR-{YYYY}-{000001}` |
| Stock Transfer | TRF | `TRF-{YYYY}-{000001}` |
| Customer Payment | CP | `CP-{YYYY}-{000001}` |
| Supplier Payment | SP | `SP-{YYYY}-{000001}` |

Generated by `DocumentSequenceService` using `SemaphoreSlim` lock.

---

## 6. Seed Data Requirements

| Table | Seed Record | Purpose |
|-------|-------------|---------|
| Users | admin / BCrypt(admin123) / Role=1 | Default admin login |
| Warehouses | WH-001 / "المخزن الرئيسي" / IsDefault=true | Default warehouse |
| Customers | CASH / "عميل نقدي" / Balance=0 | Cash customer for walk-in sales |
| Units | قطعة, كيلو, لتر, متر, صندوق | Basic measurement units |
| DocumentSequences | INV,PUR,SR,PR,TRF,CP,SP / LastNumber=0 | Invoice number sequences |
| SystemSettings | CostingMethod=1 (WeightedAverage) | Default costing method |
| CashBoxes | "الصندوق الرئيسي" / "Main Cash Box" — IsDefault=true | Default cash box |

---

## 7. Development Workflow

```text
Step 1: Define requirements
Step 2: Resolve ambiguities
Step 3: Technical design
Step 4: Break into tasks (max 4h each)
Step 5: Verify coverage (must be >= 80%)
Step 6: Write code (follow AGENTS.md patterns)
Step 7: Quality validation (all checklist items must PASS)
```

---

## 8. Pre-Submission Checklist

**Every item must be YES before submitting code:**

### Financial
- [ ] All money fields = `decimal` (not float/double)
- [ ] All quantities = `decimal` (not int)
- [ ] Financial calculations in Domain only
- [ ] `PaidAmount <= TotalAmount` validated in Domain AND DB

### Transactions & Stock
- [ ] Multi-table operations in a transaction
- [ ] Stock checked BEFORE transaction opens
- [ ] Stock deducted AFTER invoice saved
- [ ] InventoryMovement created for every stock change
- [ ] Rollback on ANY failure

### Architecture
- [ ] Service returns `Result<T>` (no raw exceptions)
- [ ] No business logic in Controller
- [ ] No direct DB access from Desktop
- [ ] Fluent API config (no DataAnnotations on entities)
- [ ] All FKs use `DeleteBehavior.Restrict`

### Security & Audit
- [ ] Controller has `[Authorize]`
- [ ] FluentValidation validator exists for Request model
- [ ] Serilog logs critical operations
- [ ] No passwords or connection strings in logs
- [ ] `CreatedByUserId` populated from JWT claims
- [ ] Users soft-deleted only (never hard delete)

### Desktop
- [ ] EventBus: subscribe in OnLoad, unsubscribe in Dispose
- [ ] EventBus handlers marshal to UI thread

### Delete & Defensive Programming
- [ ] Delete uses DeleteStrategy enum (not MessageBox)
- [ ] Guard Clauses exist for all entity constructors
- [ ] DomainException used (not ArgumentException)
- [ ] Permanent delete checks references before removal

### Dynamic UOM & Costing (v4.3)
- [ ] Pricing stored per ProductUnit (not on Product)
- [ ] Unit conversions computed in Domain (not UI or Service)
- [ ] Barcodes stored in UnitBarcode table (not embedded in Unit)
- [ ] Costing update uses UpdateProductPricingService
- [ ] CashBox.CurrentBalance validated before dispensing
- [ ] CashTransaction entries immutable — no direct editing
- [ ] ProductPriceHistory recorded on EVERY price/cost change
- [ ] At least one ProductUnit per product enforced in Domain

### WPF UI
- [ ] DialogService used (not raw MessageBox)
- [ ] Toast notifications for minor success messages
- [ ] INotifyDataErrorInfo with HasErrors property
- [ ] Save buttons disabled when form has errors
- [ ] Red border styles on invalid fields
