# MASTER IMPLEMENTATION PLAN
## Sales Management System — Feature Expansion
### Version: v4.0 | Platform: .NET 10 LTS | Date: 2026
### Desktop: WPF + MVVM (`SalesSystem.DesktopPWF`) | NOT WinForms

> **READ `AGENTS.md` AND `docs/CONSTITUTION.md` BEFORE WRITING ANY CODE.**
> All rules in those files are LAW and override everything in this plan.

---

## Executive Summary

This plan governs the implementation of new enterprise-grade feature sets on top of
the existing MVP v3.0 foundation. The system currently has a fully working WPF Desktop
application (`SalesSystem.DesktopPWF`). This plan is structured so that the Desktop
is hardened first; Mobile and Web will follow without requiring any backend changes.

### Platform Roadmap

```text
Phase A (NOW)    → Desktop WPF (SalesSystem.DesktopPWF) — all new features here
Phase B (FUTURE) → Mobile (.NET MAUI / Flutter) — same API, zero backend changes
Phase C (FUTURE) → Web (Blazor / React) — same API, zero backend changes
```

> **Architecture Guarantee:** Business logic lives ONLY in `SalesSystem.Domain` and
> `SalesSystem.Application`. Desktop, Mobile, and Web are UI-only layers.

---

## Feature Overview

| Spec | Feature | Status | Priority |
|------|---------|--------|----------|
| SPEC-008 | Wholesale & Retail Dual-Unit System | 🆕 New | **P0 — Critical** |
| SPEC-009 | Intelligent Low Stock Management | 🆕 New | **P0 — Critical** |
| SPEC-010 | Barcode Hardware Integration (Desktop) | 🆕 New | P1 — High |
| SPEC-006 | Printing — A4 Invoice + 80mm Thermal | 🔄 In Progress | P1 — High |
| SPEC-007 | System Services — Settings + Backup | 🔄 In Progress | P2 — Medium |
| SPEC-011 | Mobile Camera Barcode Support | 🔮 Future | P3 — Future |

---

## SPEC-008: Wholesale & Retail Dual-Unit System

### Business Requirement
Support simultaneous wholesale (e.g., Box) and retail (e.g., Piece) selling from the
same warehouse. Stock tracking in the DB always uses the **Retail (smallest) unit**.
The UI converts and displays intelligently using Domain methods.

---

### 8.1 — Database Migration

> **File:** `SalesSystem.Infrastructure/Migrations/XXXX_AddWholesaleRetailToProducts.cs`

Add to `Products` table:

| Column | SQL Type | Constraint | Notes |
|--------|----------|-----------|-------|
| `WholesaleUnitId` | `int` | `NULL`, FK → `Units.Id`, `RESTRICT` | e.g., "Box" |
| `RetailUnitId` | `int` | `NULL`, FK → `Units.Id`, `RESTRICT` | e.g., "Piece" |
| `ConversionFactor` | `decimal(18,3)` | `NOT NULL`, default `1`, `CHECK > 0` | 1 Box = N Pieces |
| `WholesalePrice` | `decimal(18,2)` | `NOT NULL`, default `0` | Price per wholesale unit |
| `RetailPrice` | `decimal(18,2)` | `NOT NULL`, default `0` | Price per retail unit |

> **Migration Safety:** Keep legacy `UnitId` and `SalePrice` as `NULL` temporarily.
> Drop them in a **separate follow-up migration** after a data-migration script runs.

---

### 8.2 — Domain Layer: Product Entity

> **File:** `SalesSystem.Domain/Entities/Product.cs`

```csharp
// New properties — private set enforced (RULE-042)
public int?     WholesaleUnitId  { get; private set; }
public int?     RetailUnitId     { get; private set; }
public decimal  ConversionFactor { get; private set; } = 1m; // decimal(18,3)
public decimal  WholesalePrice   { get; private set; }        // decimal(18,2)
public decimal  RetailPrice      { get; private set; }        // decimal(18,2)

// ── Centralized conversion methods (RULE-041 — lives in Domain ONLY) ──────

/// Returns how many full wholesale boxes fit in the given retail quantity.
public decimal ConvertRetailToWholesaleBoxes(decimal retailQty)
    => ConversionFactor > 0 ? Math.Floor(retailQty / ConversionFactor) : 0;

/// Returns the remaining retail units after filling whole boxes.
public decimal GetRemainingRetailAfterWholesale(decimal retailQty)
    => ConversionFactor > 0 ? retailQty % ConversionFactor : retailQty;

/// Converts a wholesale quantity into its retail-unit equivalent.
public decimal ConvertWholesaleToRetail(decimal wholesaleQty)
    => wholesaleQty * ConversionFactor;

/// Returns the correct unit price based on sale mode.
public decimal GetUnitPrice(SaleMode mode)
    => mode == SaleMode.Wholesale ? WholesalePrice : RetailPrice;

/// Returns the retail-unit equivalent. ALWAYS use this when deducting stock.
public decimal GetRetailQuantityEquivalent(decimal inputQty, SaleMode mode)
    => mode == SaleMode.Wholesale ? inputQty * ConversionFactor : inputQty;
```

---

### 8.3 — New Enum

> **File:** `SalesSystem.Domain/Enums/SaleMode.cs`

```csharp
public enum SaleMode : byte { Retail = 1, Wholesale = 2 }
```

---

### 8.4 — Contracts (DTOs & Requests)

> **`AllDtos.cs`** — Update `ProductDto`:
```csharp
public record ProductDto(
    int Id, string Code, string Name,
    string? WholesaleUnitName, string? RetailUnitName,
    decimal ConversionFactor,
    decimal WholesalePrice, decimal RetailPrice,
    decimal PurchasePrice, decimal MinStock, bool IsActive
);
```

> **`AllRequests.cs`** — Update `CreateProductRequest` / `UpdateProductRequest`:
> Add: `WholesaleUnitId`, `RetailUnitId`, `ConversionFactor`, `WholesalePrice`, `RetailPrice`

> **`AllRequests.cs`** — Update `CreateSalesInvoiceItemRequest`:
> Add: `SaleMode Mode`

---

### 8.5 — Sales Service: Dual-Mode Stock Logic

> **File:** `SalesSystem.Application/Services/SalesService.cs`

```text
When processing a sales line item:
1. Fetch Product from repository.
2. retailQty = product.GetRetailQuantityEquivalent(request.Quantity, request.Mode)
3. Validate: WarehouseStock.Quantity >= retailQty  ← BEFORE transaction
4. unitPrice = product.GetUnitPrice(request.Mode)
5. Deduct retailQty from WarehouseStock (INSIDE transaction, AFTER invoice saved)
6. Log InventoryMovement with retailQty change (RULE-028/029)
```

> **CRITICAL:** `WarehouseStocks.Quantity` is ALWAYS in retail units.

---

### 8.6 — API Validators

- `CreateProductRequestValidator`: add `RuleFor(x => x.ConversionFactor).GreaterThan(0)`
- `CreateSalesInvoiceItemRequestValidator`: add `RuleFor(x => x.Mode).IsInEnum()`

---

### 8.7 — EF Core Configuration

> **File:** `SalesSystem.Infrastructure/Data/Configurations/ProductConfiguration.cs`

```csharp
builder.Property(p => p.ConversionFactor).HasPrecision(18, 3).HasDefaultValue(1m);
builder.Property(p => p.WholesalePrice).HasPrecision(18, 2).HasDefaultValue(0m);
builder.Property(p => p.RetailPrice).HasPrecision(18, 2).HasDefaultValue(0m);
builder.HasOne(p => p.WholesaleUnit).WithMany()
    .HasForeignKey(p => p.WholesaleUnitId).OnDelete(DeleteBehavior.Restrict);
builder.HasOne(p => p.RetailUnit).WithMany()
    .HasForeignKey(p => p.RetailUnitId).OnDelete(DeleteBehavior.Restrict);
```

---

### 8.8 — Desktop WPF UI Changes

**Product Editor** (`Views/Products/ProductEditorView.xaml`):
- Replace single Unit ComboBox → two ComboBoxes: **Wholesale Unit** + **Retail Unit**
- Add `ConversionFactor` NumericTextBox with live preview label:
  *"1 [WholesaleUnit] = [Factor] [RetailUnit]"*
- Add two price fields: **Wholesale Price** + **Retail Price**

**Sales Invoice Editor** (`Views/Sales/SalesInvoiceEditorView.xaml`):
- Add **Sale Mode** toggle per line item: `Retail | Wholesale`
- Selecting `Wholesale` → auto-populates wholesale unit & price from ViewModel
- Selecting `Retail` → auto-populates retail unit & price from ViewModel
- ViewModel calls `product.GetUnitPrice()` and `product.GetRetailQuantityEquivalent()`
  — NEVER recalculate in XAML or code-behind

---

## SPEC-009: Intelligent Low Stock Management

### Business Requirement
Show all stock items at or below `ReorderLevel` per warehouse, with suggested purchase
quantities converted from retail units into wholesale units.

---

### 9.1 — Verify DB Column

Confirm `WarehouseStocks.ReorderLevel decimal(18,3) NOT NULL default 0` exists.
If not — create EF Core migration to add it (tracks threshold in retail units).

---

### 9.2 — New DTO

> **File:** `SalesSystem.Contracts/DTOs/AllDtos.cs`

```csharp
public record LowStockReportDto(
    int     ProductId,
    string  ProductName,
    string  WarehouseName,
    decimal CurrentRetailQty,
    decimal ReorderLevelRetailQty,
    decimal DeficitRetailQty,
    decimal SuggestedWholesaleBoxes,   // Product.ConvertRetailToWholesaleBoxes()
    decimal SuggestedRetailRemainder,  // Product.GetRemainingRetailAfterWholesale()
    string  WholesaleUnitName,
    string  RetailUnitName
);
```

---

### 9.3 — CQRS Query (READ — RULE-043)

> **File:** `SalesSystem.Application/Queries/GetLowStockReportQuery.cs`

```csharp
public record GetLowStockReportQuery(int? WarehouseId)
    : IRequest<Result<List<LowStockReportDto>>>;
```

> **File:** `SalesSystem.Application/Queries/GetLowStockReportQueryHandler.cs`

```csharp
public async Task<Result<List<LowStockReportDto>>> Handle(
    GetLowStockReportQuery request, CancellationToken ct)
{
    var query = _uow.WarehouseStocks
        .AsNoTracking()
        .Include(ws => ws.Product).ThenInclude(p => p.WholesaleUnit)
        .Include(ws => ws.Product).ThenInclude(p => p.RetailUnit)
        .Include(ws => ws.Warehouse)
        .Where(ws => ws.Quantity <= ws.ReorderLevel && ws.IsActive);

    if (request.WarehouseId.HasValue)
        query = query.Where(ws => ws.WarehouseId == request.WarehouseId.Value);

    var stocks = await query.ToListAsync(ct);

    return Result<List<LowStockReportDto>>.Success(stocks.Select(ws =>
    {
        var deficit = ws.ReorderLevel - ws.Quantity;
        return new LowStockReportDto(
            ws.Product.Id, ws.Product.Name, ws.Warehouse.Name,
            ws.Quantity, ws.ReorderLevel, deficit,
            ws.Product.ConvertRetailToWholesaleBoxes(deficit),
            ws.Product.GetRemainingRetailAfterWholesale(deficit),
            ws.Product.WholesaleUnit?.Name ?? "—",
            ws.Product.RetailUnit?.Name ?? "—"
        );
    }).ToList());
}
```

---

### 9.4 — API Endpoint

> **File:** `SalesSystem.Api/Controllers/ReportsController.cs`

```csharp
[HttpGet("low-stock")]
[Authorize(Policy = "ManagerAndAbove")]
public async Task<IActionResult> GetLowStock(
    [FromQuery] int? warehouseId, CancellationToken ct)
{
    var result = await _mediator.Send(new GetLowStockReportQuery(warehouseId), ct);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
}
```

---

### 9.5 — Desktop WPF: Low Stock View

> **File:** `SalesSystem.DesktopPWF/Views/Reports/LowStockView.xaml`
> **ViewModel:** `SalesSystem.DesktopPWF/ViewModels/Reports/LowStockViewModel.cs`

```
┌──────────────────────────────────────────────────────────────┐
│  Warehouse: [▼ All Warehouses]   [↻ Refresh]                  │
│  [📊 Export to Excel]  [🖨️ Print]                              │
├──────────────┬────────────┬──────────┬──────────┬────────────┤
│ Product Name │ Warehouse  │ Current  │ Reorder  │ Suggested  │
├──────────────┼────────────┼──────────┼──────────┼────────────┤
│ Orange Juice │ Main WH    │  5 Pcs   │ 24 Pcs   │ 2 Boxes   │
│              │            │          │          │ + 7 Pcs    │
│ Full Milk 1L │ Branch     │  0 Pcs 🔴│ 10 Pcs   │ 1 Box      │
└──────────────┴────────────┴──────────┴──────────┴────────────┘
```

**Color coding (WPF DataGrid row style via converter):**
- 🔴 Red = `CurrentRetailQty == 0`
- 🟠 Orange = `0 < CurrentRetailQty <= ReorderLevel`

**Export:** `ClosedXML` (approved in `AGENTS.md §5`) → `.xlsx`, grouped by `WarehouseName`

**NEVER** recalculate Domain math in the ViewModel — use pre-calculated DTO values.

---

### 9.6 — Dashboard Integration

`DashboardViewModel.cs` already shows `LowStockCount` card.
Verify clicking the card navigates to `LowStockView` with no warehouse pre-filter.

---

## SPEC-010: Barcode Hardware Integration (Desktop WPF)

### Business Requirement
USB barcode scanners (wired/wireless) act as Keyboard Emulators — they type a barcode
string and fire `Enter`. Detect this and auto-add the product to the active invoice.

---

### 10.1 — Barcode Input Service

> **File:** `SalesSystem.DesktopPWF/Services/App/IBarcodeInputService.cs`

```csharp
public interface IBarcodeInputService
{
    /// Returns completed barcode string when Enter is received; null otherwise.
    string? ProcessKey(Key key, char? keyChar);
    void Reset();
}
```

> **File:** `SalesSystem.DesktopPWF/Services/App/BarcodeInputService.cs`

```csharp
public class BarcodeInputService : IBarcodeInputService
{
    private readonly StringBuilder _buffer = new();
    private DateTime _lastKeyTime = DateTime.MinValue;
    private const int ScannerTimeoutMs = 100; // Scanners type far faster than humans

    public string? ProcessKey(Key key, char? keyChar)
    {
        var now = DateTime.Now;
        if ((now - _lastKeyTime).TotalMilliseconds > ScannerTimeoutMs)
            _buffer.Clear();
        _lastKeyTime = now;

        if (key == Key.Enter && _buffer.Length > 0)
        {
            var barcode = _buffer.ToString();
            _buffer.Clear();
            return barcode;
        }
        if (keyChar.HasValue && !char.IsControl(keyChar.Value))
            _buffer.Append(keyChar.Value);
        return null;
    }

    public void Reset() => _buffer.Clear();
}
```

Register as **Singleton** in DI (shared scan buffer across views).

---

### 10.2 — Sales Invoice ViewModel Integration

> **File:** `SalesSystem.DesktopPWF/ViewModels/Invoices/SalesInvoiceEditorViewModel.cs`

```text
On BarcodeBox_KeyDown (event routed from View to ViewModel command):
  1. barcode = _barcodeService.ProcessKey(key, keyChar)
  2. If null → return (still scanning)
  3. result = await _productApiService.GetByBarcodeAsync(barcode, ct)
  4a. Success → AddProductToInvoiceGrid(result.Value, qty=1, mode=Retail)
               _soundService.PlaySuccess()
  4b. Not found → ShowWarning("Barcode not found: " + barcode)
                  _soundService.PlayError()
  5. Mark event as handled
```

Apply identical pattern to **Purchase Invoice** ViewModel for receiving goods.

---

### 10.3 — API: Lookup by Barcode

> **File:** `SalesSystem.Api/Controllers/ProductsController.cs`

```csharp
[HttpGet("barcode/{barcode}")]
[Authorize(Policy = "AllStaff")]
public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken ct)
{
    var result = await _productService.GetByBarcodeAsync(barcode, ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
}
```

> **File:** `SalesSystem.Application/Services/ProductService.cs`

```csharp
public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct)
{
    var product = await _uow.Products
        .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive, ct);
    return product is null
        ? Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound)
        : Result<ProductDto>.Success(MapToDto(product));
}
```

---

## SPEC-006: Phase 6 — Printing (In Progress)

### Current Status: 16 tasks in `specs/006-printing/tasks.md` — all pending.

### Implementation Order

| Phase | Tasks | Goal |
|-------|-------|------|
| 6-A | T001–T006 | Print DTOs + `IPrinterService` interface |
| 6-B | T007–T011 | `PrintHelper` (GDI+ RTL) + `InvoicePrinter` (A4) |
| 6-C | T012–T014 | `ReceiptPrinter` (80mm thermal) |
| 6-D | T015–T016 | Hardening: logo fallback, decimal precision audit |

### A4 Layout

```
[Logo 60×60]   [Store Name — Bold]       [INVOICE / فاتورة مبيعات]
[Address]      [Phone]                   [INV-2026-000001]
                                         [Date: 2026-05-16]
──────────────────────────────────────────────────────────────
 #  │ Product Name    │  Qty   │  Price  │  Disc  │  Total
────┼─────────────────┼────────┼─────────┼────────┼─────────
 1  │ Orange Juice    │  2.000 │  15.00  │  0.00  │  30.00
──────────────────────────────────────────────────────────────
                               SubTotal:              30.00
                               Discount:               0.00
                               Tax (0%):               0.00
                               TOTAL:                 30.00
                               Paid:                  30.00
                               Due:                    0.00
```

**Print rules:** Currency = `{0:N2}` (2dp) | Quantity = `{0:N3}` (3dp)

---

## SPEC-007: Phase 7 — System Services (In Progress)

### 7.1 — Store Settings

| File | Layer | Action |
|------|-------|--------|
| `IStoreSettingsService.cs` | Application/Interfaces/Services | Create |
| `StoreSettingsService.cs` | Application/Services | Create |
| `SettingsController.cs` | Api/Controllers | Create |

```
GET  /api/v1/settings  [AllStaff]    — Read store configuration
PUT  /api/v1/settings  [AdminOnly]   — Update store configuration
```

### 7.2 — Database Backup & Restore

| File | Layer | Action |
|------|-------|--------|
| `IBackupService.cs` | Application/Interfaces/Services | Create |
| `BackupService.cs` | Application/Services | Create (`ExecuteSqlRawAsync`) |
| `BackupController.cs` | Api/Controllers | Create |

```
POST /api/v1/backup/create   [AdminOnly]
POST /api/v1/backup/restore  [AdminOnly]
```

> **WPF UI:** Show explicit confirmation dialog before Restore:
> *"This will terminate all active connections and replace ALL data. Continue?"*
> Log both operations via Serilog (RULE-036).

---

## SPEC-011: Mobile Barcode — Zero Backend Cost (Future)

When the Mobile app is built:
1. Use `ZXing.Net.Mobile` (MAUI) or Google ML Kit (Flutter) for camera scanning.
2. Call `GET /api/v1/products/barcode/{barcode}` — already defined in SPEC-010 §10.3.
3. **No new API endpoints needed.** API-first architecture delivers this for free.

---

## Dependency Order

```
SPEC-008 Migration
    └── Domain (Product methods)
            └── Contracts (DTOs, Requests, SaleMode enum)
                    └── Application Service + Validators
                            ├── Desktop: Product Editor View/VM
                            │       └── Desktop: Sales Invoice Mode Toggle
                            └── SPEC-009 Low Stock Query Handler
                                    └── API Endpoint
                                            └── Desktop: LowStockView + VM
                                                    └── Dashboard nav

SPEC-010 BarcodeInputService (Singleton in DI)
    ├── Sales Invoice VM integration
    ├── Purchase Invoice VM integration
    └── API: GET /products/barcode/{barcode}

SPEC-006 Print DTOs → PrintHelper → InvoicePrinter → ReceiptPrinter
SPEC-007 Settings + Backup (independent — no dependencies)
```

---

## File Map

| Spec | New Files | Modified Files |
|------|-----------|----------------|
| SPEC-008 | `XXXX_Migration.cs`, `SaleMode.cs` | `Product.cs`, `ProductConfiguration.cs`, `AllDtos.cs`, `AllRequests.cs`, `SalesService.cs`, `ProductEditorView.xaml`, `SalesInvoiceEditorView.xaml`, `SalesInvoiceEditorViewModel.cs` |
| SPEC-009 | `GetLowStockReportQuery.cs`, `GetLowStockReportQueryHandler.cs`, `LowStockView.xaml`, `LowStockViewModel.cs` | `AllDtos.cs`, `ReportsController.cs`, `DashboardViewModel.cs` |
| SPEC-010 | `IBarcodeInputService.cs`, `BarcodeInputService.cs` | `SalesInvoiceEditorViewModel.cs`, `PurchaseInvoiceEditorViewModel.cs`, `ProductsController.cs`, `ProductService.cs` |
| SPEC-006 | `IPrinterService.cs`, `PrintHelper.cs`, `InvoicePrinter.cs`, `ReceiptPrinter.cs`, 4× Print DTOs, `PrintDtoExtensions.cs` | Sales/Purchase invoice Views |
| SPEC-007 | `IStoreSettingsService.cs`, `StoreSettingsService.cs`, `IBackupService.cs`, `BackupService.cs`, `SettingsController.cs`, `BackupController.cs` | `UnitOfWork.cs` |

---

## Pre-Implementation Checklist (Run Before Each Spec)

- [ ] `AGENTS.md` read in full
- [ ] `docs/CONSTITUTION.md` read in full
- [ ] Feature branch created: `git checkout -b feature/spec-00X-name`
- [ ] No business logic in Controllers, ViewModels, or Views
- [ ] All money = `decimal(18,2)` | All quantities = `decimal(18,3)`
- [ ] Every new FK → `DeleteBehavior.Restrict`
- [ ] Every Command has a `FluentValidation` validator
- [ ] Every stock change creates an `InventoryMovement` record
- [ ] All service methods return `Result<T>` — never throw to caller
- [ ] `[Authorize]` present on every new API endpoint
- [ ] Serilog logs all critical operations (no passwords, no connection strings)
- [ ] WPF only: EventBus subscribe on load, unsubscribe in `Dispose`
- [ ] WPF only: EventBus handlers marshal to UI thread

---

*Last Updated: 2026-05-16 | Owner: Project Lead*

Implementation Plan: Sales System Enhancement
📋 Overview & Guiding Principles for Smaller Models
Important Note for AI Agents: Each phase is self-contained. Implement one phase at a time. Do not proceed to the next phase until all tests in the current phase pass.

🗂️ Phase 0: Database Schema Changes (Do This First)
Task 0.1 — Add Missing Columns to Existing Tables
SQL

-- Run this migration FIRST before any code changes

-- Add to Products table
ALTER TABLE Products ADD WholesalePrice DECIMAL(18,4) NOT NULL DEFAULT 0;
ALTER TABLE Products ADD RetailPrice DECIMAL(18,4) NOT NULL DEFAULT 0;
ALTER TABLE Products ADD ConversionFactor INT NOT NULL DEFAULT 1;
ALTER TABLE Products ADD ReorderLevel DECIMAL(18,4) NOT NULL DEFAULT 0;

-- Add to InvoiceItems table  
ALTER TABLE InvoiceItems ADD UnitType INT NOT NULL DEFAULT 0; -- 0=Retail, 1=Wholesale
ALTER TABLE InvoiceItems ADD ConversionFactor INT NOT NULL DEFAULT 1;
ALTER TABLE InvoiceItems ADD QuantityInSmallestUnit DECIMAL(18,4) NOT NULL DEFAULT 0;
Task 0.2 — Create New Tables
SQL

-- Multi-barcode support table
CREATE TABLE ProductBarcodes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    BarcodeValue NVARCHAR(100) NOT NULL,
    UnitType INT NOT NULL DEFAULT 0, -- 0=Retail, 1=Wholesale
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_ProductBarcodes_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT UQ_BarcodeValue UNIQUE (BarcodeValue)
);

CREATE INDEX IX_ProductBarcodes_BarcodeValue ON ProductBarcodes(BarcodeValue);
CREATE INDEX IX_ProductBarcodes_ProductId ON ProductBarcodes(ProductId);
✅ Phase 0 Checklist
 Migration runs without errors
 All existing data is intact
 Indexes are created
🏗️ Phase 1: Domain Layer Changes
Instructions for AI Agent: Only touch files in the Domain project. No UI, no database queries here.

Task 1.1 — Update Product Entity
csharp

// File: Domain/Entities/Product.cs
// ADD these properties to the existing Product class

public class Product : BaseEntity
{
    // === EXISTING PROPERTIES (do not remove) ===
    public string Name { get; private set; }
    public string Barcode { get; private set; } // Keep for backward compatibility
    
    // === NEW PROPERTIES TO ADD ===
    public decimal WholesalePrice { get; private set; }
    public decimal RetailPrice { get; private set; }
    public int ConversionFactor { get; private set; } // e.g., 1 Box = 12 Pieces
    public decimal ReorderLevel { get; private set; }
    
    // Navigation property
    public ICollection<ProductBarcode> Barcodes { get; private set; } 
        = new List<ProductBarcode>();

    // === NEW DOMAIN METHOD: Get price by unit type ===
    public decimal GetPriceByUnit(UnitType unitType)
    {
        return unitType == UnitType.Wholesale ? WholesalePrice : RetailPrice;
    }

    // === NEW DOMAIN METHOD: Convert to smallest unit ===
    public decimal ConvertToSmallestUnit(decimal quantity, UnitType unitType)
    {
        if (unitType == UnitType.Wholesale)
            return quantity * ConversionFactor;
        
        return quantity; // Retail is already smallest unit
    }
}
Task 1.2 — Create ProductBarcode Entity
csharp

// File: Domain/Entities/ProductBarcode.cs
// CREATE this new file

public class ProductBarcode : BaseEntity
{
    public int ProductId { get; private set; }
    public string BarcodeValue { get; private set; }
    public UnitType UnitType { get; private set; }
    public bool IsDefault { get; private set; }
    
    // Navigation
    public Product Product { get; private set; }

    // Private constructor for EF Core
    private ProductBarcode() { }

    public static ProductBarcode Create(
        int productId, 
        string barcodeValue, 
        UnitType unitType, 
        bool isDefault = false)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("Barcode value cannot be empty");
        
        return new ProductBarcode
        {
            ProductId = productId,
            BarcodeValue = barcodeValue.Trim(),
            UnitType = unitType,
            IsDefault = isDefault
        };
    }
}
Task 1.3 — Create UnitType Enum
csharp

// File: Domain/Enums/UnitType.cs
// CREATE this new file

public enum UnitType
{
    Retail = 0,     // تجزئة
    Wholesale = 1   // جملة
}
Task 1.4 — Update Stock Entity (Rich Domain Model)
csharp

// File: Domain/Entities/Stock.cs
// ADD these methods to the existing Stock class

public class Stock : BaseEntity
{
    // EXISTING: current quantity in SMALLEST unit (pieces)
    public decimal CurrentQuantityInPieces { get; private set; }
    public int ProductId { get; private set; }

    // ============================================
    // ADD THESE TWO METHODS — Core Business Logic
    // ============================================
    
    /// <summary>
    /// Deducts stock. Always converts to smallest unit internally.
    /// Call this from Application layer — NEVER calculate conversion outside.
    /// </summary>
    public void DeductStock(decimal quantity, UnitType unitType, int conversionFactor)
    {
        var quantityInPieces = unitType == UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        if (CurrentQuantityInPieces < quantityInPieces)
            throw new DomainException(
                $"Insufficient stock. Available: {CurrentQuantityInPieces}, " +
                $"Requested: {quantityInPieces} pieces");

        CurrentQuantityInPieces -= quantityInPieces;
    }

    /// <summary>
    /// Adds stock. Always converts to smallest unit internally.
    /// </summary>
    public void AddStock(decimal quantity, UnitType unitType, int conversionFactor)
    {
        var quantityInPieces = unitType == UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        CurrentQuantityInPieces += quantityInPieces;
    }
}
✅ Phase 1 Checklist
 Product entity compiles with new properties
 ProductBarcode entity created
 UnitType enum created
 Stock.DeductStock() and Stock.AddStock() methods added
 No references to UI or database in Domain layer
🔧 Phase 2: Infrastructure Layer (EF Core Configuration)
Instructions for AI Agent: Only touch DbContext and entity configuration files.

Task 2.1 — Configure ProductBarcode in DbContext
csharp

// File: Infrastructure/Persistence/Configurations/ProductBarcodeConfiguration.cs
// CREATE this new file

public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("ProductBarcodes");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.BarcodeValue)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.UnitType)
            .IsRequired()
            .HasConversion<int>();
            
        builder.HasIndex(x => x.BarcodeValue)
            .IsUnique()
            .HasDatabaseName("IX_ProductBarcodes_BarcodeValue");
            
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
Task 2.2 — Add Repository Method for Barcode Lookup
csharp

// File: Infrastructure/Repositories/ProductBarcodeRepository.cs
// CREATE this new file

public interface IProductBarcodeRepository
{
    Task<BarcodeSearchResult?> FindByBarcodeAsync(
        string barcodeValue, 
        CancellationToken cancellationToken = default);
}

// DTO returned by the search — keeps Domain clean
public record BarcodeSearchResult(
    int ProductId,
    string ProductName,
    UnitType UnitType,
    decimal Price,          // Price for this specific unit type
    int ConversionFactor,
    decimal CurrentStock    // In smallest unit
);

// Implementation
public class ProductBarcodeRepository : IProductBarcodeRepository
{
    private readonly AppDbContext _context;

    public ProductBarcodeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BarcodeSearchResult?> FindByBarcodeAsync(
        string barcodeValue,
        CancellationToken cancellationToken = default)
    {
        // Search in ProductBarcodes table first
        var result = await _context.ProductBarcodes
            .Where(b => b.BarcodeValue == barcodeValue)
            .Select(b => new BarcodeSearchResult(
                b.ProductId,
                b.Product.Name,
                b.UnitType,
                b.UnitType == UnitType.Wholesale 
                    ? b.Product.WholesalePrice 
                    : b.Product.RetailPrice,
                b.Product.ConversionFactor,
                b.Product.Stock.CurrentQuantityInPieces
            ))
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback: search in legacy Products.Barcode column
        if (result == null)
        {
            result = await _context.Products
                .Where(p => p.Barcode == barcodeValue)
                .Select(p => new BarcodeSearchResult(
                    p.Id,
                    p.Name,
                    UnitType.Retail,    // Default to retail for legacy barcodes
                    p.RetailPrice,
                    p.ConversionFactor,
                    p.Stock.CurrentQuantityInPieces
                ))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return result;
    }
}
✅ Phase 2 Checklist
 EF Core configuration added
 DbContext includes DbSet<ProductBarcode>
 Repository interface and implementation created
 Registered in DI container (Startup/Program.cs)
⚙️ Phase 3: Application Layer (Services & Handlers)
Instructions for AI Agent: This is the most critical phase. Follow the exact method signatures.

Task 3.1 — Create BarcodeLookupService
csharp

// File: Application/Services/BarcodeLookupService.cs

public interface IBarcodeLookupService
{
    Task<BarcodeSearchResult?> LookupAsync(string barcode, CancellationToken ct = default);
}

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly IProductBarcodeRepository _barcodeRepo;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(
        IProductBarcodeRepository barcodeRepo,
        ILogger<BarcodeLookupService> logger)
    {
        _barcodeRepo = barcodeRepo;
        _logger = logger;
    }

    public async Task<BarcodeSearchResult?> LookupAsync(
        string barcode, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        _logger.LogInformation("Barcode lookup: {Barcode}", barcode);
        
        return await _barcodeRepo.FindByBarcodeAsync(barcode.Trim(), ct);
    }
}
Task 3.2 — Create GetPriceByUnit Query
csharp

// File: Application/Queries/GetProductPriceByUnit/GetProductPriceByUnitQuery.cs

public record GetProductPriceByUnitQuery(int ProductId, UnitType UnitType) 
    : IRequest<decimal>;

public class GetProductPriceByUnitHandler 
    : IRequestHandler<GetProductPriceByUnitQuery, decimal>
{
    private readonly IAppDbContext _context;

    public GetProductPriceByUnitHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> Handle(
        GetProductPriceByUnitQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        return product.GetPriceByUnit(request.UnitType);
    }
}
Task 3.3 — Update Invoice Command Handler
csharp

// File: Application/Commands/ProcessInvoice/ProcessInvoiceCommandHandler.cs
// KEY CHANGE: Stock deduction now uses Domain method with unit conversion

public class ProcessInvoiceCommandHandler 
    : IRequestHandler<ProcessInvoiceCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProcessInvoiceCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        ProcessInvoiceCommand command, 
        CancellationToken cancellationToken)
    {
        // 1. Create Invoice header
        var invoice = Invoice.Create(command.SupplierId, command.Notes);

        // 2. Process each line item
        foreach (var itemDto in command.Items)
        {
            var product = await _unitOfWork.Products
                .GetByIdWithStockAsync(itemDto.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), itemDto.ProductId);

            // 2a. Get correct price based on unit type
            var unitPrice = product.GetPriceByUnit(itemDto.UnitType);

            // 2b. Add item to invoice
            var invoiceItem = invoice.AddItem(
                productId: itemDto.ProductId,
                quantity: itemDto.Quantity,
                unitPrice: unitPrice,
                unitType: itemDto.UnitType,
                discount: itemDto.Discount,
                conversionFactor: product.ConversionFactor
            );

            // 2c. Update stock — Domain handles conversion internally!
            // For PURCHASE invoice: add stock
            // For SALES invoice: deduct stock
            if (command.InvoiceType == InvoiceType.Purchase)
            {
                product.Stock.AddStock(
                    itemDto.Quantity, 
                    itemDto.UnitType, 
                    product.ConversionFactor);
            }
            else
            {
                product.Stock.DeductStock(
                    itemDto.Quantity, 
                    itemDto.UnitType, 
                    product.ConversionFactor);
            }
        }

        // 3. Save everything in one transaction
        _unitOfWork.Invoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
Task 3.4 — Create Validation
csharp

// File: Application/Commands/ProcessInvoice/ProcessInvoiceCommandValidator.cs

public class ProcessInvoiceCommandValidator 
    : AbstractValidator<ProcessInvoiceCommand>
{
    public ProcessInvoiceCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Invoice must have at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("Invalid product");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero");

            item.RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero");

            item.RuleFor(x => x.UnitType)
                .IsInEnum()
                .WithMessage("Invalid unit type");
        });
    }
}
✅ Phase 3 Checklist
 BarcodeLookupService created and registered
 GetProductPriceByUnitQuery works correctly
 Invoice command handler uses Domain methods for stock
 Fluent Validation added
 No unit conversion logic exists OUTSIDE the Domain layer
🖥️ Phase 4: WPF ViewModel Changes
Instructions for AI Agent: Only touch ViewModel files. Do not touch backend logic here.

Task 4.1 — InvoiceItemViewModel with Auto-Price Update
csharp

// File: WPF/ViewModels/InvoiceItemViewModel.cs
// This is the ViewModel for each ROW in the DataGrid

public class InvoiceItemViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    
    // ===========================
    // BACKING FIELDS
    // ===========================
    private int _productId;
    private string _productName = string.Empty;
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discount;
    private UnitType _selectedUnit = UnitType.Retail;
    private int _conversionFactor = 1;
    private bool _isUpdatingPrice = false; // Prevent recursive updates

    // ===========================
    // PROPERTIES
    // ===========================
    
    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                OnPropertyChanged(nameof(Total)); // Recalculate total
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
                OnPropertyChanged(nameof(Total)); // Recalculate total
        }
    }

    public decimal Discount
    {
        get => _discount;
        set
        {
            if (SetProperty(ref _discount, value))
                OnPropertyChanged(nameof(Total));
        }
    }

    // ⭐ KEY PROPERTY: When unit changes, price auto-updates
    public UnitType SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
            {
                // Fire and forget — update price from database
                _ = UpdatePriceForSelectedUnitAsync();
            }
        }
    }

    // Calculated — no setter needed
    public decimal Total => (Quantity * UnitPrice) - Discount;

    // Available units for ComboBox binding
    public List<UnitTypeOption> AvailableUnits { get; } = new()
    {
        new UnitTypeOption(UnitType.Retail, "تجزئة"),
        new UnitTypeOption(UnitType.Wholesale, "جملة")
    };

    // ===========================
    // METHOD: Auto-update price
    // ===========================
    
    private async Task UpdatePriceForSelectedUnitAsync()
    {
        // Guard: don't run if no product selected yet
        if (ProductId <= 0 || _isUpdatingPrice) return;

        try
        {
            _isUpdatingPrice = true;
            
            var newPrice = await _mediator.Send(
                new GetProductPriceByUnitQuery(ProductId, SelectedUnit));
            
            UnitPrice = newPrice; // This triggers Total recalculation via getter
        }
        catch (Exception ex)
        {
            // Log error but don't crash UI
            Debug.WriteLine($"Price update failed: {ex.Message}");
        }
        finally
        {
            _isUpdatingPrice = false;
        }
    }

    // Called when a product is selected (from search or barcode scan)
    public void SetProduct(BarcodeSearchResult result)
    {
        _isUpdatingPrice = true; // Suppress automatic price fetch
        
        ProductId = result.ProductId;
        ProductName = result.ProductName;
        _conversionFactor = result.ConversionFactor;
        UnitPrice = result.Price;
        
        // Set unit AFTER price to avoid double fetch
        _selectedUnit = result.UnitType;
        OnPropertyChanged(nameof(SelectedUnit));
        OnPropertyChanged(nameof(Total));
        
        _isUpdatingPrice = false;
    }
}

// Helper class for ComboBox display
public record UnitTypeOption(UnitType Value, string DisplayName);
Task 4.2 — Main Invoice ViewModel (Barcode Scan Handler)
csharp

// File: WPF/ViewModels/InvoiceViewModel.cs
// ADD this method to existing InvoiceViewModel

public class InvoiceViewModel : BaseViewModel
{
    private readonly IBarcodeLookupService _barcodeService;
    private string _scannedBarcode = string.Empty;

    public string ScannedBarcode
    {
        get => _scannedBarcode;
        set
        {
            if (SetProperty(ref _scannedBarcode, value))
            {
                // Auto-search when barcode is long enough (typical barcodes are 8-13 chars)
                if (value?.Length >= 8)
                    _ = HandleBarcodeScanAsync(value);
            }
        }
    }

    private async Task HandleBarcodeScanAsync(string barcode)
    {
        try
        {
            var result = await _barcodeService.LookupAsync(barcode);
            
            if (result == null)
            {
                // Show "not found" message to user
                StatusMessage = $"⚠️ الصنف غير موجود: {barcode}";
                return;
            }

            // Create new row in DataGrid
            var newItem = new InvoiceItemViewModel(_mediator);
            newItem.SetProduct(result); // Sets product + unit + price atomically
            
            Items.Add(newItem);
            
            // Clear barcode input for next scan
            ScannedBarcode = string.Empty;
            
            // Update totals
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ: {ex.Message}";
        }
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(x => x.Total);
        TaxAmount = IncludeTax ? SubTotal * (TaxPercentage / 100) : 0;
        GrandTotal = SubTotal + TaxAmount - AdditionalDiscount;
        
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(GrandTotal));
    }
}
Task 4.3 — XAML DataGrid Binding
XML

<!-- File: Views/InvoiceView.xaml -->
<!-- KEY BINDING: UnitType ComboBox that triggers price update -->

<DataGrid ItemsSource="{Binding Items}" AutoGenerateColumns="False">
    
    <!-- Product Name Column -->
    <DataGrid.Columns>
        <DataGridTextColumn 
            Header="المنتج" 
            Binding="{Binding ProductName}" 
            Width="*"/>

        <!-- ⭐ Unit ComboBox — triggers price auto-update via ViewModel -->
        <DataGridTemplateColumn Header="الوحدة" Width="100">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <ComboBox 
                        ItemsSource="{Binding AvailableUnits}"
                        DisplayMemberPath="DisplayName"
                        SelectedValuePath="Value"
                        SelectedValue="{Binding SelectedUnit, 
                                       Mode=TwoWay, 
                                       UpdateSourceTrigger=PropertyChanged}"/>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>

        <!-- Quantity -->
        <DataGridTextColumn 
            Header="الكمية" 
            Binding="{Binding Quantity, UpdateSourceTrigger=PropertyChanged}" 
            Width="80"/>

        <!-- Price — auto-updated, but editable -->
        <DataGridTextColumn 
            Header="التكلفة" 
            Binding="{Binding UnitPrice, UpdateSourceTrigger=PropertyChanged}" 
            Width="80"/>

        <!-- Discount -->
        <DataGridTextColumn 
            Header="الخصم" 
            Binding="{Binding Discount, UpdateSourceTrigger=PropertyChanged}" 
            Width="80"/>

        <!-- Total — read-only calculated field -->
        <DataGridTextColumn 
            Header="الإجمالي" 
            Binding="{Binding Total, StringFormat=N2}" 
            Width="100" 
            IsReadOnly="True"/>
    </DataGrid.Columns>
</DataGrid>

<!-- Barcode Input at top -->
<TextBox 
    Text="{Binding ScannedBarcode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    PlaceholderText="مسح الباركود يضيف الصنف تلقائياً"/>
✅ Phase 4 Checklist
 SelectedUnit property setter calls price update
 Total property auto-recalculates (no manual trigger needed)
 SetProduct() sets all fields atomically (no double API calls)
 Barcode scan adds new row automatically
 _isUpdatingPrice flag prevents infinite loops
📊 Phase 5: Low Stock Report
Task 5.1 — Low Stock Query
csharp

// File: Application/Queries/LowStock/GetLowStockQuery.cs

public record GetLowStockQuery(int? BranchId = null) 
    : IRequest<List<LowStockItemDto>>;

public record LowStockItemDto(
    int ProductId,
    string ProductName,
    decimal CurrentPieces,
    decimal ReorderLevelPieces,
    decimal ShortfallPieces,
    int ConversionFactor,
    // Smart conversion fields:
    int ShortfallBoxes,      // e.g., 1 (from 15 pieces ÷ 12)
    int ShortfallPiecesRemainder // e.g., 3 (15 - 12)
);

public class GetLowStockHandler 
    : IRequestHandler<GetLowStockQuery, List<LowStockItemDto>>
{
    private readonly IAppDbContext _context;

    public async Task<List<LowStockItemDto>> Handle(
        GetLowStockQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.Stock.CurrentQuantityInPieces <= p.ReorderLevel);

        if (request.BranchId.HasValue)
            query = query.Where(p => p.BranchId == request.BranchId);

        var lowStockItems = await query
            .Select(p => new 
            {
                p.Id,
                p.Name,
                Current = p.Stock.CurrentQuantityInPieces,
                Reorder = p.ReorderLevel,
                p.ConversionFactor
            })
            .ToListAsync(cancellationToken);

        // Apply smart conversion in memory (avoid complex SQL)
        return lowStockItems.Select(item =>
        {
            var shortfall = item.Reorder - item.Current;
            var boxes = (int)(shortfall / item.ConversionFactor);
            var remainder = (int)(shortfall % item.ConversionFactor);
            
            return new LowStockItemDto(
                item.Id,
                item.Name,
                item.Current,
                item.Reorder,
                shortfall,
                item.ConversionFactor,
                boxes,
                remainder
            );
        }).ToList();
    }
}
Task 5.2 — PDF Report Display Logic
csharp

// File: Application/Services/ReportFormatterService.cs
// Formats the smart conversion text for PDF

public static class SmartUnitFormatter
{
    /// <summary>
    /// Returns: "1 كرتون و 3 حبة" instead of "15 حبة"
    /// </summary>
    public static string FormatShortfall(LowStockItemDto item)
    {
        if (item.ConversionFactor <= 1)
            return $"{item.ShortfallPieces} حبة";

        if (item.ShortfallBoxes == 0)
            return $"{item.ShortfallPiecesRemainder} حبة";

        if (item.ShortfallPiecesRemainder == 0)
            return $"{item.ShortfallBoxes} كرتون";

        return $"{item.ShortfallBoxes} كرتون و {item.ShortfallPiecesRemainder} حبة";
    }
}
✅ Phase 5 Checklist
 Query filters by ReorderLevel correctly
 Smart conversion math is correct (test: 15 pieces ÷ 12 = 1 box + 3 pieces)
 Optional branch filtering works
🧪 Phase 6: Unit Tests (Critical — Do Not Skip)
csharp

// File: Tests/Domain/StockEntityTests.cs

public class StockEntityTests
{
    [Fact]
    public void DeductStock_Wholesale_DeductsCorrectPieces()
    {
        // Arrange: 24 pieces in stock
        var stock = Stock.CreateWithQuantity(24);
        
        // Act: Deduct 2 boxes (each box = 12 pieces)
        stock.DeductStock(quantity: 2, unitType: UnitType.Wholesale, conversionFactor: 12);
        
        // Assert: 24 - (2×12) = 0 pieces remaining
        Assert.Equal(0, stock.CurrentQuantityInPieces);
    }

    [Fact]
    public void DeductStock_Retail_DeductsOnePiece()
    {
        var stock = Stock.CreateWithQuantity(24);
        
        stock.DeductStock(quantity: 1, unitType: UnitType.Retail, conversionFactor: 12);
        
        // Assert: 24 - 1 = 23 pieces remaining
        Assert.Equal(23, stock.CurrentQuantityInPieces);
    }

    [Fact]
    public void DeductStock_InsufficientStock_ThrowsDomainException()
    {
        var stock = Stock.CreateWithQuantity(5);
        
        // Trying to deduct 1 box (12 pieces) from only 5 pieces
        Assert.Throws<DomainException>(() =>
            stock.DeductStock(1, UnitType.Wholesale, 12));
    }

    [Fact]
    public void SmartFormatter_15Pieces_With12PerBox_ReturnsCorrectText()
    {
        var item = new LowStockItemDto(1, "Test", 0, 15, 15, 12, 1, 3);
        
        var result = SmartUnitFormatter.FormatShortfall(item);
        
        Assert.Equal("1 كرتون و 3 حبة", result);
    }
}
📦 Summary: Implementation Order
text

┌─────────────────────────────────────────────────────────┐
│              IMPLEMENTATION SEQUENCE                     │
├──────┬──────────────────────────────────┬───────────────┤
│ Step │ Task                             │ Estimated     │
├──────┼──────────────────────────────────┼───────────────┤
│  0   │ Run DB migrations                │ 30 min        │
│  1   │ Domain entities & enums          │ 1 hour        │
│  2   │ EF Core config + Repository      │ 1 hour        │
│  3   │ Application services & handlers  │ 2 hours       │
│  4   │ WPF ViewModels + XAML bindings   │ 2 hours       │
│  5   │ Low stock report & formatter     │ 1 hour        │
│  6   │ Unit tests                       │ 1 hour        │
├──────┼──────────────────────────────────┼───────────────┤
│      │ TOTAL                            │ ~8.5 hours    │
└──────┴──────────────────────────────────┴───────────────┘

RULES FOR AI AGENT:
✅ Complete each phase fully before starting the next
✅ Run tests after Phase 1, 3, and 6
✅ Never put conversion logic in ViewModel or UI code
✅ Never put conversion logic in the Command Handler
✅ Conversion ONLY lives in Stock.DeductStock() / Stock.AddStock()
✅ Price selection ONLY lives in Product.GetPriceByUnit()
6:36 PM
Make Plan frist 
