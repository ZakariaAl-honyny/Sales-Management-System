# Research Findings — Products Module Remaining Work

## Design Decisions

### Decision 1: Opening Batch Creation Flow

**Decision**: Create the opening batch inside `ProductService.CreateAsync()` AFTER saving the Product (to get Product.Id), within a single `ExecuteTransactionAsync` call. The flow is:

1. Create Product entity via `Product.Create()` (Domain factory)
2. `_uow.Products.AddAsync(product)`
3. `_uow.SaveChangesAsync(ct)` — get Product.Id
4. If opening quantity > 0:
   a. Resolve default warehouse (`_uow.Warehouses.GetDefaultAsync()` or `warehouses.FirstOrDefault(w => w.IsDefault)`)
   b. Create `InventoryBatch` via `InventoryBatch.Create(product.Id, warehouseId, openingQty, openingCost, "OPENING", expiryDate: openingExpiryDate)`
   c. Create `WarehouseStock` (via `_inventoryService.IncreaseStockAsync(...)`)
   d. Create `InventoryMovement` (done by IncreaseStockAsync internally)
   e. Create journal entry (Dr Inventory / Cr OpeningBalanceEquity via `AccountingIntegrationService`)
5. Return `Result<ProductDto>.Success(productDto)`

**Rationale**: The existing `InventoryService.IncreaseStockAsync()` already auto-creates WarehouseStock and records InventoryMovement. We need to add InventoryBatch creation before/after the stock increase. All operations wrapped in `_uow.ExecuteTransactionAsync()`.

**Alternatives considered**:
- Creating a separate `OpeningStockService` — rejected because opening stock is intrinsic to product creation.
- Using `InventoryBatchService.CreateAsync()` — rejected because it doesn't update WarehouseStock or InventoryMovement.

---

### Decision 2: Journal Entry for Opening Balance

**Decision**: Add a new method `CreateProductOpeningEntryAsync(...)` to `AccountingIntegrationService` following the existing pattern of `CreateCustomerOpeningEntryAsync`.

**Journal entry structure**:
```
Dr Inventory (System Account: المخزون)          = totalOpeningValue
Cr OpeningBalanceEquity (System Account: رصيد افتتاحي)  = totalOpeningValue
```

**Rationale**: The AccountingIntegrationService already has 10 methods following a consistent pattern. Adding one more is simpler and more maintainable than creating a new service.

**Details**:
- Total opening value = sum over all opening batches: `(opening quantity in base units × unit cost)` for each batch.
- Uses `_systemAccountService.GetMappingsAsync()` to resolve Inventory and OpeningBalanceEquity account IDs.
- ReferenceType = "Product", ReferenceId = product.Id.
- EntryType = JournalEntryType.OpeningBalance.
- Points to `_accountingIntegrationService` to ensure the existing pattern (create Draft → Post) is followed.

---

### Decision 3: Default Warehouse Resolution

**Decision**: Use `_uow.Warehouses.GetDefaultAsync()` — or the existing Desktop pattern `warehouses.FirstOrDefault(w => w.IsDefault) ?? warehouses.First()` — to resolve the default warehouse for opening stock.

In the service layer, add a helper method or use `WarehouseService` to get the default warehouse. The DB ensures at most one active default via filtered unique index.

**Rationale**: DbSeeder already seeds "المخزن الرئيسي" with `IsDefault = true`. The filtered unique index ensures only one default. Service already enforces this.

---

### Decision 4: Product.Cost Removal Strategy

**Decision**: Remove `Product.Cost` in a SINGLE migration that also renames `HasExpiry` → `TrackExpiry` and removes `ProductBarcode` table. This avoids multiple schema migrations.

**Cost migration plan**:
- Remove `Cost` column from Products table
- Remove `UpdateCost()` domain method from Product entity
- Add a `ProductCostService` (or `FifoCostService`) that computes cost from InventoryBatches
- Replace ALL `Product.Cost` reads with calls to the cost service

**Cost reading replacement pattern**:
```csharp
// BEFORE: Product.Cost
var cost = product.Cost;

// AFTER: IProductCostService.GetAverageCostAsync(productId)
var costResult = await _productCostService.GetAverageCostAsync(productId);
var cost = costResult.IsSuccess ? costResult.Value : 0m;
```

**Impact assessment**: 30+ read references across all layers need updating. The Desktop layer (Sales/Purchase/Pricing VMs) will need the most changes since they read Cost for display and calculation.

**FIFO cost calculation**:
```csharp
// Average cost = sum(QuantityRemaining × UnitCost) / sum(QuantityRemaining)
var batches = await _uow.InventoryBatches
    .FindAsync(b => b.ProductId == productId && b.QuantityRemaining > 0)
    .OrderBy(b => b.CreatedAt)
    .ToListAsync(ct);
if (!batches.Any()) return 0m;
var totalValue = batches.Sum(b => b.QuantityRemaining * b.UnitCost);
var totalQty = batches.Sum(b => b.QuantityRemaining);
return totalValue / totalQty;
```

---

### Decision 5: FIFO Cost Query Location

**Decision**: Create `IProductCostService` / `ProductCostService` in the Application layer. This service:
- `GetAverageCostAsync(int productId, CancellationToken ct)` → `Result<decimal>`: Weighted average from all active batches.
- `GetFifoLayersAsync(int productId, int quantity, CancellationToken ct)` → `Result<List<FifaLayerDto>>`: Returns which batches and quantities to consume for a given quantity (for COGS calculation).
- `GetLatestCostAsync(int productId, CancellationToken ct)` → `Result<decimal>`: Returns the most recent purchase cost.

**Rationale**: Separates cost calculation from product/batch/warehouse services. The SalesService and AccountingIntegrationService can inject this service instead of reading `Product.Cost`.

---

### Decision 6: HasExpiry → TrackExpiry Rename

**Decision**: Rename across ALL layers in one pass:

| Layer | Change |
|-------|--------|
| Domain | `Product.HasExpiry` → `Product.TrackExpiry` |
| Contracts | `ProductDto.HasExpiry` → `ProductDto.TrackExpiry` |
| Application | `product.HasExpiry` → `product.TrackExpiry` in ProductService |
| Infrastructure | Column rename in migration |
| Desktop | ViewModel property + XAML binding |
| Tests | All `HasExpiry:` → `TrackExpiry:` in ProductDto constructions |

**Scope**: 12+ reference locations (from research data). The migration must:
1. Rename column `HasExpiry` to `TrackExpiry` in Products table
2. Update all `ProductDto` record constructions in tests
3. Update all `Product.cs` property/method references

---

### Decision 7: UnitBarcodes/ProductBarcode Removal

**Decision**: Remove `ProductBarcode` entity, `ProductBarcodes` DbSet, `ProductBarcodes` repository, and `ProductBarcodeConfiguration` in the same migration as Cost removal and HasExpiry rename.

**Files to delete or modify**:
- DELETE: `SalesSystem.Domain/Entities/ProductBarcode.cs`
- DELETE: `SalesSystem.Infrastructure/Data/Configurations/ProductBarcodeConfiguration.cs`
- MODIFY: `SalesSystem.Infrastructure/Data/SalesDbContext.cs` — remove `DbSet<ProductBarcode>`
- MODIFY: `SalesSystem.Application/Interfaces/IUnitOfWork.cs` — remove `ProductBarcodes` property
- MODIFY: `SalesSystem.Infrastructure/Data/UnitOfWork.cs` — remove `_productBarcodes` field and property

**Note**: The `UnitBarcodes` table was already removed by migration `20260609205359_Phase25_RemoveUnitBarcode`. Only the `ProductBarcode` entity remains as `[Obsolete]` with zero business logic usage.

---

### Decision 8: Product Import Excel Format

**Decision**: The existing `ProductImportViewModel` already handles Excel import via ClosedXML with the following columns:
- Product Name (required)
- Category Name (required — mapped to CategoryId)
- Barcode (optional — validated unique)
- Base Unit Name (required — mapped to UnitId)
- Purchase Cost (optional — used for opening cost)
- Min Stock Level (optional)
- Description (optional)
- Opening Quantity (NEW — to be added)
- Opening Expiry Date (NEW — to be added)

**Format**: `.xlsx` (OpenXML) via ClosedXML. Two-step flow:
1. Preview (parse locally, validate via API)
2. Execute (API creates all products with opening batches)

**Current state**: ViewModel is fully implemented, but XAML View is a placeholder. Need to complete the View.

---

### Decision 9: Migration Order

**Decision**: Create ONE migration (`Phase28_ProductsModuleRemaining`) that:
1. Renames `HasExpiry` → `TrackExpiry` on Products table
2. Removes `Cost` column from Products table
3. Drops `ProductBarcodes` table
4. Removes `ProductBarcodes` DbSet from model snapshot
5. (No changes to ProductPrices, ProductUnits, or InventoryBatches)

This is a clean, single migration that can be applied after rolling back or removing the two existing Phase 25 migrations (`Phase25_RemoveUnitBarcode` and `Phase25_ProductEntityCleanup`).

---

### Decision 10: ProductImportView XAML Completion

**Decision**: Complete the `ProductImportView.xaml` to match the existing `ProductImportViewModel`. The view needs:
- File selector (button + path display)
- Download template button
- Preview DataGrid with all import columns
- Import results summary (success/failure counts)
- Error list for failed rows
- Execute Import button (enabled after preview)
- Progress indicator

The ViewModel already has all the commands and properties — the View just needs to bind to them.

---

## Summary of All Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Opening batch creation | Inside ProductService.CreateAsync() + ExecuteTransactionAsync | Simple, atomic, follows existing patterns |
| Journal entry for opening | New method in AccountingIntegrationService | Consistent pattern, reuses existing account mapping |
| Default warehouse | _uow.Warehouses.GetDefaultAsync() or IsDefault flag | Already seeded and enforced |
| Cost removal | ProductCostService with weighted avg from batches | Separates concern, enables FIFO |
| FIFO cost query | IProductCostService (new Application service) | Clean separation of concerns |
| HasExpiry rename | Single pass across all 7 layers | Minimizes migration complexity |
| ProductBarcode removal | Delete entity + config + DbSet | Zero business logic usage |
| Excel import columns | Existing format + new opening fields | Extends what already works |
| Migration | Single Phase28 migration | Clean schema evolution |
| ImportView XAML | Complete bindings to existing ViewModel | ViewModel already ready |
