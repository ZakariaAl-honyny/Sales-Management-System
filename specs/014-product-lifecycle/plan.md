# Implementation Plan: Product Lifecycle & Media Management

**Branch**: `014-product-lifecycle` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This feature implements the complete product lifecycle from creation through sale, return, cost tracking, and write-off. Products are created with a name, category, optional barcode, optional image, and optional opening stock. Opening stock creates an `InventoryBatch` (with `BatchNo = "OPENING"`), a `WarehouseStock` row, an `InventoryTransaction` of type `OpeningBalance`, and an automatic journal entry debiting `InventoryAsset` and crediting `OpeningBalanceEquity`. Purchases create new `InventoryBatches`; sales consume from the oldest batch first (FIFO); returns reverse FIFO consumption on sale returns or debit from the original batch on purchase returns. Cost is recalculated as weighted average across all batches after each purchase. Price changes are tracked historically via `ProductPrices` with `EffectiveFrom` dates (never in-place edits). `ProductCategories` get full CRUD with soft delete. Expired goods are written off via `InventoryAdjustment` with type `Damage`, which deducts stock and creates a journal entry. All operations are wrapped in `ExecuteTransactionAsync` to guarantee atomicity.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, ASP.NET Core, WPF)
**Architecture Scope**: Full Stack — Domain, Infrastructure, Application, API, Desktop
**Storage**: SQL Server 2019+; product images stored on local filesystem (%AppData%), not in the database
**Primary Dependencies**: ClosedXML (existing for Excel export), QuestPDF (existing for printing)
**Constraints**:
- Product `Cost` is NOT stored on the Product entity — cost is derived from `InventoryBatches` via FIFO query at transaction time
- `HasExpiry` is renamed to `TrackExpiry` across all layers
- `ProductBarcode`/`UnitBarcode` table is removed — barcode lives on `Products.Barcode` (varchar(50), unique filtered)
- Opening stock is optional — products can be created without it
- `InventoryBatch.BatchNo` uses the string "OPENING" for opening stock entries, and auto-increment int for purchase-based batches
- No Bill of Materials (BOM), no assembly/disassembly, no multi-level BOM in V1
- No `ProductImages` table — a single `ImagePath` (nvarchar(500)) on the `Products` entity is sufficient for V1
- Weighted average cost is the default costing method; FIFO is the default consumption strategy

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ PASS | Money = decimal(18,2), Quantity = decimal(18,3), Cost = decimal(18,2) |
| II | Domain Formulas | ✅ PASS | LineTotal = Qty × UnitPrice (computed in entity), SubTotal = SUM(LineTotal) |
| III | Transactional Integrity | ✅ PASS | ALL lifecycle operations wrapped in `ExecuteTransactionAsync` |
| IV | Invoice Lifecycle | ✅ N/A | Document state unchanged by this phase |
| V | Stock Integrity | ✅ PASS | Every stock change creates InventoryTransaction + InventoryTransactionLine; FIFO consumption from oldest batch; WarehouseStock quantity ≥ 0 CHECK constraint |
| VI | Result Pattern | ✅ PASS | All service methods return `Result<T>` |
| VII | Architecture Boundaries | ✅ PASS | Image I/O in Infrastructure; Domain has zero dependencies; Desktop calls API via HttpClient |
| VIII | Security | ✅ PASS | All product endpoints have `[Authorize]`; image upload validates file type and size server-side |
| IX | Four-Layer Validation | ✅ PASS | Domain guards (name required, quantity > 0), Service checks (duplicate barcode), FluentValidation (max lengths), DB CHECK constraints (quantity ≥ 0) |
| X | Logging | ✅ PASS | Stock transactions, cost changes, write-offs, and expired stock notifications logged via Serilog |
| XI | EF Core Conventions | ✅ PASS | Fluent API only, DeleteBehavior.Restrict on all FKs, nvarchar for text, decimal precision explicit |
| XII | Audit Trail | ✅ PASS | All InventoryTransactions track CreatedByUserId and CreatedAt; ProductPriceHistory records every cost/price change |
| XIII | Delete Strategy | ✅ PASS | Products soft-deleted (IsActive = false); ProductCategories soft-deleted |
| XIV | Defensive Programming | ✅ PASS | Guard clauses on all entity factories: Product.Create(), InventoryBatch.Create(), WarehouseStock.Create() |

**Gate Result**: ✅ ALL CLEAR — No violations.

---

## Product Creation Flow

### Domain Entity: Product

The `Product` entity is stripped of all direct price and cost fields. It carries only intrinsic product properties:
- `Name` (nvarchar(200), required)
- `Barcode` (varchar(50), nullable, unique filtered)
- `CategoryId` (int FK to ProductCategories, required)
- `TaxId` (smallint FK to Taxes, nullable)
- `Description` (nvarchar(500), nullable)
- `TrackExpiry` (bit, default false — renamed from `HasExpiry`)
- `ImagePath` (nvarchar(500), nullable — single image, not a table)
- `ReorderLevel` (decimal(18,3), default 0)

No `Cost`, no `SalePrice`, no `PurchasePrice`, no `StockQuantity` — these are either in `InventoryBatches`, `ProductPrices`, or `WarehouseStocks` respectively.

### Service Layer: CreateAsync

When `ProductService.CreateAsync()` is called, it:
1. Validates barcode uniqueness (if barcode provided)
2. Creates the `Product` entity via `Product.Create(name, categoryId, ...)` with Domain guard clauses
3. Creates the first `ProductUnit` (base unit, Factor=1, IsBaseUnit=true) — every product needs at least one unit
4. If `OpeningQuantity > 0` in the request: creates an `InventoryBatch` with `BatchNo = 0` (representing "OPENING"), `QuantityReceived = OpeningQuantity`, `QuantityRemaining = OpeningQuantity`, `UnitCost = OpeningUnitCost`
5. Creates or updates `WarehouseStock` for the default warehouse with the opening quantity
6. Creates an `InventoryTransaction` of type `OpeningBalance` (10) with one `InventoryTransactionLine`
7. Calls `AccountingIntegrationService` to create an automatic journal entry: Dr InventoryAsset (Account from SystemAccountMappings) / Cr OpeningBalanceEquity (Account 1422)

Steps 4–7 are optional (only if opening stock is provided). Steps 2–7 are wrapped in `ExecuteTransactionAsync` for atomicity. The default warehouse is resolved from system settings (`DefaultWarehouseId`) or the first active warehouse if not configured.

---

## Product Purchase Flow (Posting a Purchase Invoice)

When a `PurchaseInvoice` is posted:
1. For each line item, an `InventoryBatch` is created with:
   - `BatchNo` = next auto-increment batch number for this product
   - `QuantityReceived` = line quantity
   - `QuantityRemaining` = line quantity (fully available)
   - `UnitCost` = line unit price (the purchase cost)
   - `PurchaseInvoiceId` = FK to the source invoice
   - `ExpiryDate` = from the invoice line (if TrackExpiry is true)
2. `WarehouseStock.Quantity` is increased by the line quantity
3. An `InventoryTransaction` of type `Purchase` (1) is created with lines for each product
4. **Cost recalculation**: The service computes the new weighted average cost across all batches for this product+warehouse:
   - `newAvgCost = (SUM(QuantityRemaining × UnitCost across all batches) + newQty × newUnitCost) / (SUM(QuantityRemaining) + newQty)`
   - This cost is NOT stored on the Product — it is computed at query time by `IInventoryBatchRepository.GetWeightedAverageCostAsync(productId, warehouseId)`
5. A journal entry is created (by `AccountingIntegrationService`): Dr Inventory / Dr VAT Input / Cr AccountsPayable (or Cash)

---

## Product Sale Flow (Posting a Sales Invoice)

When a `SalesInvoice` is posted:
1. For each line item, the service queries `InventoryBatches` for the product in the specified warehouse, filtered to those with `QuantityRemaining > 0`, sorted by FIFO (oldest `CreatedAt` first, or by `ExpiryDate` ascending when `TrackExpiry = true`)
2. The line quantity is consumed from batches sequentially:
   - From the oldest batch: deduct `min(QuantityRemaining, remainingQuantity)` from `QuantityRemaining`
   - Move to the next batch if the first batch is fully consumed
   - Repeat until the full sale quantity is allocated
3. `WarehouseStock.Quantity` is decreased by the total allocated quantity
4. An `InventoryTransaction` of type `Sale` (3) is created with lines linked to the specific batch IDs consumed, recording quantity, unit cost, and total cost per batch
5. **Cost of Goods Sold (COGS)** is computed as the sum of `consumedQuantity × batchUnitCost` across all consumed batches — this is the actual cost, not an average
6. A journal entry is created: Dr COGS / Cr Inventory (at the actual FIFO cost), plus Dr Cash (or AR) / Cr SalesRevenue / Cr VAT Output (at the sale price)

### FIFO Consumption Algorithm

```text
remaining = saleQuantity
for each batch in batches.OrderBy(CreatedAt) [or OrderBy(ExpiryDate) when TrackExpiry]:
    if remaining <= 0: break
    deductAmount = min(batch.QuantityRemaining, remaining)
    batch.QuantityRemaining -= deductAmount
    remaining -= deductAmount
    record consumption: (batch.Id, deductAmount, batch.UnitCost)
```

---

## Product Return Flow

### Purchase Return

When a `PurchaseReturn` is posted:
1. For each return line, the corresponding `PurchaseInvoiceLine`'s original `InventoryBatch` is located
2. `batch.QuantityRemaining` is decreased by the return quantity (stock goes back to the supplier)
3. `WarehouseStock.Quantity` is decreased
4. An `InventoryTransaction` of type `PurchaseReturn` (2) is created
5. Cost is reversed at the original purchase cost from that batch
6. A reversal journal entry is created: Dr AccountsPayable / Cr Inventory

### Sales Return

When a `SalesReturn` is posted:
1. The original `SalesInvoiceLine` is located, along with the `InventoryTransactionLines` that recorded the FIFO consumption
2. A **new** `InventoryBatch` is created for the returned goods with:
   - `BatchNo` = next batch number
   - `QuantityReceived` = return quantity
   - `QuantityRemaining` = return quantity
   - `UnitCost` = original cost from the consumed batch (ensures cost fidelity)
3. `WarehouseStock.Quantity` is increased
4. An `InventoryTransaction` of type `SaleReturn` (4) is created
5. A reversal journal entry is created: Dr Inventory / Cr COGS (at the original cost)

---

## Cost Tracking

### Weighted Average Cost (Default)

The `IInventoryBatchRepository.GetWeightedAverageCostAsync(productId, warehouseId)` method computes:

```text
totalCost = SUM(QuantityRemaining × UnitCost) for all batches with QuantityRemaining > 0
totalQty  = SUM(QuantityRemaining) for those batches
averageCost = totalQty > 0 ? totalCost / totalQty : 0
```

This is called whenever the system needs to display "current cost" — on the product editor, on sales invoice lines (for profit computation), and on reports. The average is NOT cached; it is computed fresh each time to reflect the latest batch quantities.

### Price History (ProductPrices)

Prices are NEVER edited in place. Every price change creates a new `ProductPrices` row with:
- `ProductUnitId` — which unit this price applies to
- `CurrencyId` — multi-currency support
- `Price` — the new price value
- `EffectiveFrom` — the date this price becomes active
- `EffectiveTo` — null (set when superseded by a later price)

The active price for a given date is the one with `EffectiveFrom <= targetDate AND (EffectiveTo IS NULL OR EffectiveTo > targetDate)`. This provides a complete audit trail of pricing history without overwriting.

---

## Product Image Management

The `Products.ImagePath` field stores a relative path under `%AppData%\SalesSystem\ProductImages\`. When a user uploads an image:
1. The Desktop client sends the image file to the API via `multipart/form-data`
2. The API validates: file extension (`.jpg`, `.jpeg`, `.png`), file size (max 2MB), and image dimensions (via `SixLabors.ImageSharp`)
3. The image is saved to `%AppData%\SalesSystem\ProductImages\{productId}\{guid}.jpg`
4. The relative path is stored in `Products.ImagePath`
5. The Desktop client loads the image asynchronously via the API endpoint `GET /api/v1/products/{id}/image`, which streams the file bytes

Images are loaded lazily in list views — only the product thumbnail is fetched when the item becomes visible in the scroll viewport. This prevents UI lag when scrolling through hundreds of products.

---

## Expired Stock Write-Off

### Write-Off Flow (via InventoryAdjustment)

When expired stock is identified (via the Expired Products Report) and the user executes a write-off:
1. An `InventoryAdjustment` is created with `AdjustmentType = Damage` (4)
2. `InventoryAdjustmentLines` record each product, batch, quantity, unit cost, and total cost
3. The specified batches have `QuantityRemaining` decreased
4. `WarehouseStock.Quantity` is decreased
5. An `InventoryTransaction` of type `Damage` (9) is created
6. A journal entry is created: Dr InventoryWriteOffExpense / Cr Inventory (at the batch cost)

### Expiry Tracking

Products with `TrackExpiry = true` require an expiration date on their `InventoryBatches`. The Expired Products Report queries:
- **Expired**: `InventoryBatches WHERE ExpiryDate < TODAY AND QuantityRemaining > 0`
- **Near Expiry**: `InventoryBatches WHERE ExpiryDate BETWEEN TODAY AND TODAY + ThresholdDays AND QuantityRemaining > 0`

The threshold (default 30 days) is configurable via `SystemSettings`. The MainDashboard ViewModel checks for expired/near-expiry stock on load and shows a non-blocking notification badge.

---

## ProductCategories CRUD

`ProductCategory` is a simple entity with:
- `Name` (nvarchar(100), required, unique filtered)
- `Description` (nvarchar(500), nullable)

Soft-deletable via `IsActive`. When a category is deleted, its products are NOT deleted — the `CategoryId` FK uses `DeleteBehavior.Restrict`, so the service checks `Products.AnyAsync(p => p.CategoryId == id)` before allowing soft-delete. If products reference the category, deletion is blocked with an Arabic error message: "لا يمكن حذف التصنيف — يوجد منتجات مرتبطة به".

---

## Project Structure

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── Product.cs                          ← UPDATE: remove Cost, remove SalePrice/PurchasePrice, rename HasExpiry→TrackExpiry
│       ├── InventoryBatch.cs                   ← ALREADY EXISTS: BatchNo, QuantityReceived, QuantityRemaining, UnitCost
│       ├── InventoryTransaction.cs             ← ALREADY EXISTS
│       ├── InventoryTransactionLine.cs         ← ALREADY EXISTS
│       ├── ProductPrice.cs                     ← ALREADY EXISTS (ProductPrices table)
│       ├── ProductUnit.cs                      ← ALREADY EXISTS
│       └── ProductCategory.cs                  ← ALREADY EXISTS
├── SalesSystem.Infrastructure/
│   ├── Configurations/
│   │   ├── ProductConfiguration.cs             ← UPDATE: remove Cost, add ImagePath config
│   │   └── InventoryBatchConfiguration.cs      ← UPDATE: add index on (ProductId, WarehouseId, QuantityRemaining)
│   ├── Repositories/
│   │   └── InventoryBatchRepository.cs         ← ADD: GetWeightedAverageCostAsync, GetFifoBatchesAsync
│   └── Data/
│       └── SalesDbContext.cs                   ← Ensure all new entities have DbSets and query filters
├── SalesSystem.Application/
│   ├── Services/
│   │   ├── ProductService.cs                   ← UPDATE: opening stock creation, cost removed from entity
│   │   ├── InventoryService.cs                 ← UPDATE: FIFO consumption, weighted average queries
│   │   ├── PurchaseInvoiceService.cs           ← UPDATE: batch creation on post
│   │   ├── SalesInvoiceService.cs              ← UPDATE: FIFO consumption on post
│   │   ├── PurchaseReturnService.cs            ← UPDATE: batch debit on post
│   │   ├── SalesReturnService.cs               ← UPDATE: new batch creation on post
│   │   └── ProductCategoryService.cs           ← CRUD with soft-delete guard
│   ├── Interfaces/
│   │   └── Repositories/
│   │       └── IInventoryBatchRepository.cs    ← ADD: FIFO and cost query methods
│   └── Accounting/
│       └── AccountingIntegrationService.cs     ← UPDATE: opening stock journal entry, write-off journal entry
├── SalesSystem.Contracts/
│   ├── Requests/
│   │   ├── CreateProductRequest.cs             ← ADD OpeningQuantity, OpeningUnitCost, OpeningExpiryDate
│   │   ├── UpdateProductRequest.cs             ← No cost or price fields
│   │   └── CreateProductCategoryRequest.cs     ← Simple Name + Description
│   └── Responses/
│       ├── ProductResponse.cs                  ← NO Cost, Add ImageUrl, Add CurrentStockQuantity (computed)
│       └── ProductCategoryResponse.cs          ← Simple DTO
├── SalesSystem.Api/
│   ├── Controllers/
│   │   ├── ProductsController.cs               ← ADD image upload/download endpoints, opening stock params
│   │   └── ProductCategoriesController.cs      ← CRUD endpoints
│   └── Validators/
│       └── CreateProductRequestValidator.cs    ← ADD opening stock validation (qty > 0, cost >= 0)
├── SalesSystem.DesktopPWF/
│   ├── ViewModels/
│   │   ├── Products/
│   │   │   ├── ProductEditorViewModel.cs       ← ADD opening stock fields (visible only on create)
│   │   │   ├── ProductCategoryListViewModel.cs ← NEW
│   │   │   └── ProductCategoryEditorViewModel.cs ← NEW
│   │   └── Inventory/
│   │       └── ExpiredStockViewModel.cs        ← NEW: report + write-off action
│   ├── Views/
│   │   ├── Products/
│   │   │   ├── ProductEditorView.xaml          ← ADD: opening stock section, image upload, expiry toggle
│   │   │   ├── ProductCategoryListView.xaml    ← NEW
│   │   │   └── ProductCategoryEditorView.xaml  ← NEW
│   │   └── Inventory/
│   │       └── ExpiredStockView.xaml           ← NEW
│   └── Services/Api/
│       ├── IProductApiService.cs               ← ADD: upload image, download image
│       └── IProductCategoryApiService.cs       ← NEW
└── Tests/
    ├── SalesSystem.Domain.Tests/
    │   └── Entities/ProductTests.cs            ← UPDATE: test without Cost
    ├── SalesSystem.Application.Tests/
    │   ├── Services/ProductServiceTests.cs     ← ADD: opening stock creation tests
    │   └── Services/InventoryServiceTests.cs   ← ADD: FIFO consumption tests
    └── SalesSystem.DesktopPWF.Tests/
        └── ViewModels/...                     ← UPDATE: editor VM tests with opening stock fields
```

---

## Verification Checklist

- [ ] Product entity has NO Cost, SalePrice, PurchasePrice properties
- [ ] `HasExpiry` renamed to `TrackExpiry` across all 7 layers
- [ ] Cost is derived from `InventoryBatches` via `GetWeightedAverageCostAsync` — never stored on Product
- [ ] Opening stock on product creation: creates `InventoryBatch` (BatchNo="OPENING"), `WarehouseStock`, `InventoryTransaction`, and journal entry (Dr Inventory / Cr OpeningBalanceEquity)
- [ ] Purchase posting: creates `InventoryBatch` with BatchNo, QuantityReceived, QuantityRemaining, UnitCost
- [ ] Sale posting: FIFO consumption from oldest batches, COGS = sum(consumedQty × batchUnitCost)
- [ ] Purchase return: debits from original batch's QuantityRemaining
- [ ] Sales return: creates new InventoryBatch with original cost, increments WarehouseStock
- [ ] Weighted average cost is computed from `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)`
- [ ] Price changes create new ProductPrices row with EffectiveFrom (never in-place edit)
- [ ] Image upload: validates extension (jpg/png), max size (2MB), stores on filesystem, sets ImagePath
- [ ] Image loaded asynchronously in list views (lazy loading via scroll visibility)
- [ ] Write-off creates InventoryAdjustment (Damage type), deducts batch QuantityRemaining, creates journal entry
- [ ] Expired Products Report: filters "Expired" and "Near Expiry" with configurable threshold
- [ ] MainDashboard shows non-blocking notification for expired/near-expiry stock
- [ ] ProductCategories CRUD: soft-delete with guard against category-in-use
- [ ] All operations wrapped in `ExecuteTransactionAsync`
- [ ] All service methods return `Result<T>`
- [ ] Build: 0 errors, 0 warnings
- [ ] All existing tests pass (2,083+)
