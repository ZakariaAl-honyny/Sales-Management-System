# Implementation Plan: Dynamic UOM & Costing Engine (v4.3)

**Branch**: `008-dynamic-uom-costing` | **Date**: 2026-05-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/008-dynamic-uom-costing/spec.md`

---

## Summary

Enable products to carry multiple units of measure, each with its own conversion factor and price list (per currency × effective date range). When a purchase invoice is posted, the system automatically recalculates the product cost using the configured costing strategy (Weighted Average, Last Purchase Price, or Supplier Price) and cascades the updated cost to all derived units via `InventoryBatches`. Every price and cost change is recorded immutably in `ProductPriceHistory`. A display-only `SmartUnitFormatter` presents quantities in the most legible unit in the UI. Costing is sourced from `InventoryBatches.UnitCost` — not stored on `ProductUnit` directly. Pricing is separated from costing and lives in the `ProductPrices` table (per `ProductUnit` × `CurrencyId` with effective date ranges).

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, FluentValidation 11, Serilog 8, BCrypt.Net-Next 4
**Storage**: SQL Server 2019+ — new tables: `ProductUnits`, `ProductPrices`, `ProductPriceHistory`; existing extensions: `Units` (seeded system units), `InventoryBatches` (cost source), `SystemSettings` (CostingMethod seed row)
**Testing**: xUnit + Moq + FluentAssertions (matches existing test projects)
**Target Platform**: Windows x64 (API as Windows Service + WPF Desktop)
**Project Type**: Desktop App + REST API (Clean Architecture — 6 existing projects)
**Performance Goals**: Cost recalculation for a single product completes within 3 seconds after invoice post; barcode lookup resolves in < 100 ms
**Constraints**: All money = `decimal(18,2)`; all quantities = `decimal(18,3)`; no float/double anywhere; full transactional integrity on purchase posting; all FKs use `DeleteBehavior.Restrict`
**Scale/Scope**: Single-store retail, ~1,000 active products, each with up to 5 units

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | `Price` on `ProductPrices` → `decimal(18,2)`; `Factor` on `ProductUnits` → `decimal(18,3)`; `UnitCost` on `InventoryBatches` → `decimal(18,2)` |
| II | Domain-Computed Financial Formulas | ✅ PASS | Weighted average formula lives in `UpdateProductPricingService` (Application layer); cost cascade derived from base unit cost × conversion factor |
| III | Transactional Integrity | ✅ PASS | Cost update fires inside the existing purchase-posting transaction — after invoice saved with an ID, before commit |
| IV | Invoice Lifecycle State Machine | ✅ PASS | No new invoice state; cost update only on `Posted` transition; `Cancelled` invoices reverse the batch and cost impact |
| V | Stock Integrity | ✅ PASS | Stock deduction uses base-unit quantity through `UnitId` + `Factor` resolution; `InventoryTransactionLines` store `ProductUnitId` to preserve the transaction unit |
| VI | Result Pattern | ✅ PASS | `UpdateProductPricingService` returns `Result<int>`; all new API endpoints return `Result<T>` |
| VII | Clean Architecture Boundaries | ✅ PASS | `SmartUnitFormatter` lives in `DesktopPWF` only (UI layer); costing logic in `Application`; entities in `Domain`; `ProductPrices` domain entity has zero EF Core dependencies |
| VIII | Security | ✅ PASS | New `/api/v1/products/{productId}/units` and `/api/v1/products/{productId}/prices` endpoints carry `[Authorize]` with `ManagerAndAbove` policy |
| IX | Four-Layer Validation | ✅ PASS | Domain guard clauses on `ProductUnit`, `ProductPrices`; FluentValidation on all request DTOs; `CHECK (Factor > 0)` and `CHECK (Price >= 0)` in DB |
| X | Logging | ✅ PASS | Cost recalculations logged via Serilog at `Information` level with product ID, new cost value, and costing method used |
| XI | EF Core Conventions | ✅ PASS | Fluent API config only; all FKs use `DeleteBehavior.Restrict`; `nvarchar` for all names; `smallint` PK for `Units`; filtered unique indexes include `AND [IsActive] = 1` |
| XII | Audit Trail | ✅ PASS | `ProductPriceHistory.ChangedByUserId` references `Users`; price history entries are immutable and never deleted |
| XIII | Delete Strategy | ✅ PASS | `ProductUnit` uses `DeleteStrategy` — soft delete (`IsActive=false`) preferred; hard delete blocked if referenced by invoice lines or if it's the last unit on the product |
| XIV | Defensive Programming | ✅ PASS | `ProductUnit` constructor guards: name not empty, factor > 0. `ProductPrices` constructor guards: price >= 0, effectiveFrom not default |
| XV | WPF Interactive Dialogs | ✅ PASS | All dialogs in new ViewModels via `IDialogService` — warning dialog on conversion factor change, delete confirmation with strategy selection |
| XVI | Toast Notifications | ✅ PASS | Unit saved/deleted confirmations, price update confirmations via `IToastNotificationService` |
| XVII | Real-Time UI Validation | ✅ PASS | `ProductUnitEditorViewModel` implements `INotifyDataErrorInfo`; `ProductPricesEditorViewModel` validates price ≥ 0, dates, currency selection |

**Gate Result**: ✅ ALL CLEAR — proceed to phased implementation.

---

## Entity Architecture — Key Design Decisions

### Units vs ProductUnits Separation

The database schema introduces a distinct `Units` table (smallint PK, `Name`, `Symbol`, `IsSystem`, `IsActive`) which defines the vocabulary of units globally (e.g., "حبة", "كرتون", "صندوق"). The `ProductUnits` junction table then links a specific product to a specific unit with a `Factor` (conversion factor) and `IsBaseUnit` flag. This separates the unit *definition* from the product-*specific* conversion factor, allowing "Box" to mean 12 for one product and 24 for another.

- **Seeded system units**: `حبة` (Piece), `كرتون` (Carton), `صندوق` (Box), `كيلو` (Kilogram), `لتر` (Liter), `متر` (Meter) — these are system-protected (`IsSystem=true`) and cannot be deleted.
- **User-defined units**: Store owners can create additional units (e.g., "زجاجة", "طبق") — these are not system-protected and can be soft-deleted if unused.
- **One base unit per product**: Exactly one `ProductUnit` per product has `IsBaseUnit=true` and `Factor=1`. All other units of that product must have `Factor > 1`.

### Pricing Decoupled from Costing

The old plan stored `RetailPrice`, `WholesalePrice`, and `AvgCost` directly on `ProductUnit`. The new schema cleanly separates three concerns:

1. **Pricing** → `ProductPrices` table: `ProductUnitId` FK × `CurrencyId` FK × `Price` × `EffectiveFrom`/`EffectiveTo` date range. A product can have different prices in different currencies with effective date ranges. There is **no `PriceLevel` enum** in V1 — the distinction between retail and wholesale is made by `SaleMode` on the invoice line, not by a separate price column.

2. **Costing** → `InventoryBatches.UnitCost`: The cost of a product is never stored on `ProductUnit`. Instead, it is derived from `InventoryBatches` using the configured costing method (`WeightedAverage`, `LastPurchasePrice`, or `SupplierPrice`). For display purposes, the current average cost is computed on-the-fly from batches.

3. **Display cost on ProductUnit**: While `AvgCost` is not stored as a column, the service layer computes the current effective cost for each product unit as `BaseUnitCost × Factor` and surfaces it via ProductUnit DTOs for UI display.

### Barcode Strategy — V1 Simplification

**KEY CHANGE from the old plan**: There is **no `UnitBarcodes` table** in V1. Barcode is stored directly on the `Products` table as `varchar(50)` with a filtered unique index (`WHERE Barcode IS NOT NULL AND IsActive = 1`). This means barcode is per-product, not per-product-unit. The rationale:

- V1 scope is limited; per-unit barcodes add significant complexity (barcode resolution must identify both the product AND the unit).
- Products with multiple barcodes per unit (e.g., two different box barcodes for the same product) are rare in single-store retail.
- The `UnitBarcode` entity is deferred to V2 where full multi-barcode POS scanning with unit auto-detection will be implemented.

### Invoice Line Unit Tracking

When an invoice line is created (Sales or Purchase), it records `ProductUnitId` FK — this freezes which unit was used at the time of transaction. This ensures historical invoice accuracy even if the product's unit definitions or conversion factors change later.

- **SalesInvoiceLines**: `ProductId`, `ProductUnitId`, `Quantity` (in the transaction unit), `UnitPrice`, `LineTotal`, `SaleMode` (Retail/Wholesale)
- **PurchaseInvoiceLines**: `ProductId`, `ProductUnitId`, `Quantity` (in the transaction unit), `UnitPrice`, `LineTotal`
- On posting, the service converts the transaction quantity to base units using `Factor` for stock deduction and batch cost allocation.

---

## Costing Methods — Detailed Design

### 1. Weighted Average (Default)

Formula: `NewAvgCost = (OldStockQuantity × OldAvgCost + NewQuantity × NewUnitCost) ÷ (OldStockQuantity + NewQuantity)`

- `OldStockQuantity` = SUM of `QuantityRemaining` across all open `InventoryBatches` for that product in that warehouse
- `OldAvgCost` = Weighted average of `UnitCost` across all open batches (SUM(QuantityRemaining × UnitCost) ÷ SUM(QuantityRemaining))
- On zero stock (first purchase ever or stock fully depleted), the new purchase cost is used directly — avoiding division by zero.
- After updating the base cost, derived unit costs are computed as `DerivedCost = BaseCost × Factor`.

### 2. Last Purchase Price

- On every purchase posting, the product's cost is overwritten to the `UnitPrice` of the most recent purchase invoice line.
- All derived units are recalculated from the new base cost × conversion factor.
- This is the simplest costing model — used when inventory is fast-moving and prior purchase prices are irrelevant.

### 3. Supplier Price

- The costing engine uses the `SupplierPrice` stored on the product record (via a hypothetical supplier price list, deferred to V2).
- In V1, Supplier Price costing means the purchase `UnitPrice` is ignored for costing — the system uses the last known catalog price from the default supplier.
- This costing method is least used in retail but important for consignment or fixed-price contract scenarios.

### Costing Method Selection

- The active costing method is stored as a `SystemSetting` with `SettingKey = "CostingMethod"`, `SettingType = 2` (Integer), and `Category = "Inventory"`.
- Seeded default: `WeightedAverage` (value `1`).
- The costing method is read once at the start of the purchase posting transaction and applied uniformly to all lines in that invoice.
- Changing the costing method mid-operation takes effect on the next posted invoice — never retroactively.

### Cost Cascade Mechanism

After the base unit cost is recalculated (by any of the three methods), the `UpdateProductPricingService` performs the following cascade:

1. Compute the new base unit cost (`ProductUnit` where `IsBaseUnit=true`).
2. Iterate all non-base `ProductUnit` records for that product.
3. Set each derived unit's display cost to `BaseCost × Factor`.
4. Create a `ProductPriceHistory` entry recording the old and new costs for EACH unit, with `ChangeReason` indicating the trigger invoice ID.
5. The cascade runs inside the same transaction as the purchase posting — atomic commit or full rollback.

---

## ProductPriceHistory — Audit Trail

Every price or cost change creates an immutable `ProductPriceHistory` record:

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | Auto-increment |
| `ProductUnitId` | int FK | Which unit changed |
| `OldPrice` | decimal(18,2) | Previous price (0 for initial creation) |
| `NewPrice` | decimal(18,2) | New price |
| `OldCost` | decimal(18,2) | Previous cost (0 for initial) |
| `NewCost` | decimal(18,2) | New cost |
| `PriceType` | tinyint | 1=Retail, 2=Wholesale, 3=Cost (which field changed) |
| `CurrencyId` | smallint | Which currency's price changed |
| `ChangeReason` | nvarchar(255) | Arabic description of trigger |
| `ChangedByUserId` | int FK | The user who triggered the change |
| `CreatedAt` | datetime2 | Timestamp |

**Trigger points**:
- Purchase invoice posting (reason: `"تحديث التكلفة من فاتورة شراء رقم {invoiceId}"`)
- Manual price adjustment via `ProductPrices` editor (reason: `"تعديل يدوي للسعر"`)
- Product unit creation (initial history entry with old values = 0, reason: `"إنشاء وحدة جديدة"`)
- Product unit conversion factor change (reason: `"تغيير عامل التحويل"`)

---

## InventoryBatches — Cost Source

Costing in V1 is batch-based via `InventoryBatches`:

- On purchase posting: A new `InventoryBatch` is created per purchase line with `QuantityReceived`, `QuantityRemaining`, `UnitCost`, `BatchNo`, `ExpiryDate` (if product tracks expiry), and `PurchaseInvoiceId` reference.
- On sale posting: Stock is deducted from the oldest batch first (FIFO) — `QuantityRemaining` is decremented. If `QuantityRemaining` reaches zero, the batch is considered consumed but the record remains for audit.
- On FEFO (for products with `TrackExpiry=true`): The batch with the nearest `ExpiryDate` is consumed first, regardless of receipt order.
- On purchase cancellation: The corresponding batch's `QuantityRemaining` is reduced (or the batch is soft-cancelled).
- The current average cost for display is computed as: `SUM(QuantityRemaining × UnitCost) ÷ SUM(QuantityRemaining)` across all non-zero batches.

---

## SmartUnitFormatter — UI Only

A presentation-layer helper in `DesktopPWF/Services/Formatting/` that selects the best display unit for a given base-unit quantity:

**Algorithm**:
1. Fetch the product's units sorted by `Factor` descending.
2. For each unit (largest first): compute `DisplayQuantity = floor(BaseQuantity / Factor)`.
3. If `DisplayQuantity >= 1`, use this unit and show `"{DisplayQuantity} {UnitName}"`.
4. If no unit qualifies (quantity < 1 in the smallest non-base unit), fall back to the base unit with exact decimal quantity.
5. On hover, show a ToolTip with the exact base-unit count: `"{BaseQuantity} {BaseUnitName}"`.

This formatter is purely presentational — it never modifies stored data or API responses. All business logic (costing, stock, price calculations) works in base units.

---

## Project Structure — Source Code

```
SalesSystem/
├── SalesSystem.Domain/
│   ├── Entities/
│   │   ├── ProductUnit.cs                   ← NEW (ProductId, UnitId, Factor, IsBaseUnit)
│   │   ├── ProductPrice.cs                  ← NEW (ProductUnitId, CurrencyId, Price, EffectiveFrom/To)
│   │   └── ProductPriceHistory.cs           ← NEW (audit trail record)
│   └── Enums/
│       └── PriceType.cs                     ← NEW (1=Retail, 2=Wholesale, 3=Cost)
│
├── SalesSystem.Contracts/
│   ├── Requests/
│   │   ├── AddProductUnitRequest.cs         ← NEW
│   │   ├── UpdateProductUnitRequest.cs      ← NEW
│   │   ├── AddProductPriceRequest.cs        ← NEW
│   │   └── UpdateProductPriceRequest.cs     ← NEW
│   └── Responses/
│       ├── ProductUnitDto.cs                ← NEW (includes effective cost)
│       ├── ProductPriceDto.cs               ← NEW
│       └── ProductPriceHistoryDto.cs        ← NEW
│
├── SalesSystem.Application/
│   ├── Interfaces/Services/
│   │   ├── IProductUnitService.cs           ← NEW
│   │   ├── IProductPriceService.cs          ← NEW
│   │   └── IUpdateProductPricingService.cs  ← NEW
│   └── Services/
│       ├── ProductUnitService.cs            ← NEW
│       ├── ProductPriceService.cs           ← NEW
│       └── UpdateProductPricingService.cs   ← NEW (costing engine — strategy pattern via switch)
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs                ← EXTEND (add DbSets for ProductUnit, ProductPrice, ProductPriceHistory)
│   │   └── Configurations/
│   │       ├── ProductUnitConfiguration.cs  ← NEW (FK ProductId, FK UnitId, UK ProductId+UnitId, Factor CHK>0)
│   │       ├── ProductPriceConfiguration.cs ← NEW (FK ProductUnitId, FK CurrencyId, Price CHK>=0)
│   │       └── ProductPriceHistoryConfiguration.cs ← NEW (FK ProductUnitId, FK ChangedByUserId)
│   └── Migrations/                          ← NEW migration
│
├── SalesSystem.Api/
│   ├── Controllers/
│   │   ├── ProductUnitsController.cs        ← NEW (nested under /api/v1/products/{productId}/units)
│   │   └── ProductPricesController.cs       ← NEW (nested under /api/v1/product-units/{unitId}/prices)
│   └── Validators/
│       ├── AddProductUnitRequestValidator.cs     ← NEW
│       ├── UpdateProductUnitRequestValidator.cs  ← NEW
│       ├── AddProductPriceRequestValidator.cs    ← NEW
│       └── UpdateProductPriceRequestValidator.cs ← NEW
│
└── SalesSystem.DesktopPWF/
    ├── Services/Formatting/
    │   └── SmartUnitFormatter.cs            ← NEW (display-only, base-64 quantity → best unit)
    ├── Services/Api/
    │   ├── ProductUnitApiService.cs         ← NEW
    │   └── ProductPriceApiService.cs        ← NEW
    ├── Views/Products/
    │   ├── ProductUnitEditorView.xaml       ← NEW
    │   └── ProductPriceEditorView.xaml      ← NEW
    └── ViewModels/Products/
        ├── ProductUnitEditorViewModel.cs    ← NEW
        └── ProductPriceEditorViewModel.cs   ← NEW
```

---

## Complexity Tracking

| Concern | Complexity | Mitigation |
|---------|------------|------------|
| Unit conversion across 4+ layers | Medium | Factor-based conversion isolated in `ProductUnit.ConvertToUnit(quantity, targetFactor)` — single Domain method used by all callers |
| Three costing methods with atomic cascade | Medium | Strategy pattern inside `UpdateProductPricingService` with enum switch; shared cascade logic extracted to private method |
| FIFO/FEFO batch consumption | Medium | `InventoryBatch` repository method `GetConsumptionOrder(productId, warehouseId, quantity)` returns ordered batch list |
| Price history from multiple triggers | Low | Single `RecordPriceChangeAsync` method on `ProductPriceService` called by all trigger points |
| Multi-currency pricing | Low | `ProductPrices` filtered by `CurrencyId` + `EffectiveFrom/To`; current price query uses a simple date-range filter |
| Smart unit display formatting | Low | Pure function with no side effects — unit-testable in isolation |

---

## Implementation Phases

**Phase 0 — Research**: Complete (see `research.md`). Architecture decisions documented for unit/price separation, barcode strategy, batch-based costing.

**Phase 1 — Data Model & Contracts**: Define `ProductUnit`, `ProductPrice`, `ProductPriceHistory` entities with guard clauses. Create all request/response DTOs. Update `SalesInvoiceLine` and `PurchaseInvoiceLine` to reference `ProductUnitId`.

**Phase 2 — Infrastructure**: EF Core entity configurations for all new tables. Migration to create tables and seed system units (6 seeded units). Add `CostingMethod` system setting seed.

**Phase 3 — Application Services**: `ProductUnitService` (CRUD with last-unit guard), `ProductPriceService` (CRUD with effective date overlap validation), `UpdateProductPricingService` (costing engine with strategy switch, batch update, cascade logic, price history recording).

**Phase 4 — API Layer**: Controllers with FluentValidation. All endpoints under `[Authorize]` with `ManagerAndAbove` policy for write operations. Barcode lookup endpoint at `/api/v1/barcodes/{barcode}` for POS scanner integration.

**Phase 5 — Desktop UI**: `ProductUnitEditorView` (list + inline editor for product units), `ProductPriceEditorView` (currency-specific pricing grid), `SmartUnitFormatter` integrated into inventory and invoice list displays. EventBus messages for unit/price changes to auto-refresh product-aware screens.
