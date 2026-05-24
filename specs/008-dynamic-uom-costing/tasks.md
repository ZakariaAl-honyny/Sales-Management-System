---
description: "Task list for Dynamic UOM & Costing Engine (v4.3)"
---

# Tasks: Dynamic UOM & Costing Engine (v4.3)

**Input**: `specs/008-dynamic-uom-costing/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [x] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS all user stories)

**⚠️ Complete and verify before any Phase 3+ work.**

- [ ] T002 Add `CostingMethod` enum to Domain: `public enum CostingMethod : byte { WeightedAverage = 1, LastPurchasePrice = 2, SupplierPrice = 3 }` — FILE: `SalesSystem/SalesSystem.Domain/Enums/CostingMethod.cs` — RULE-069

- [ ] T003 [P] Add `SaleMode` enum: `public enum SaleMode : byte { Retail = 1, Wholesale = 2 }` — FILE: `SalesSystem/SalesSystem.Domain/Enums/SaleMode.cs` — RULE-049

- [ ] T004 [P] Create `ProductUnit` entity in Domain. Properties: `Id (int)`, `ProductId (int)`, `UnitName (nvarchar 50)`, `ConversionFactor (decimal 18,3)`, `RetailPrice (decimal 18,2)`, `WholesalePrice (decimal 18,2)`, `AvgCost (decimal 18,2)`, `IsBaseUnit (bool)`, `IsActive (bool)`, `CreatedAt (datetime2)`, `CreatedByUserId (int)`. Constructor guard clauses using `DomainException`: name not empty, factor > 0, prices >= 0. Method `GetPriceByUnit(SaleMode mode)` returns RetailPrice or WholesalePrice. Method `UpdateCost(decimal newCost)` sets AvgCost — FILE: `SalesSystem/SalesSystem.Domain/Entities/ProductUnit.cs` — RULE-042, RULE-052, RULE-065

- [ ] T005 [P] Create `UnitBarcode` entity: `Id (int)`, `ProductUnitId (int)`, `Barcode (nvarchar 100)`, `IsActive (bool)`. Constructor guard clause: barcode not empty — FILE: `SalesSystem/SalesSystem.Domain/Entities/UnitBarcode.cs` — RULE-063

- [ ] T006 [P] Create `ProductPriceHistory` entity: `Id (int)`, `ProductUnitId (int)`, `OldRetailPrice`, `NewRetailPrice`, `OldWholesalePrice`, `NewWholesalePrice`, `OldAvgCost`, `NewAvgCost` (all decimal 18,2), `ChangeReason (nvarchar 255)`, `ChangedByUserId (int)`, `ChangedAt (datetime2)`. No setters after construction — immutable — FILE: `SalesSystem/SalesSystem.Domain/Entities/ProductPriceHistory.cs` — RULE-084, RULE-085

- [ ] T007 Add `Units` navigation property to `Product` entity: `public ICollection<ProductUnit> Units { get; private set; } = new List<ProductUnit>();` Also add domain method `GetBaseUnit()` returning the unit where `IsBaseUnit == true`. Add domain rule guard in `AddUnit(ProductUnit unit)` that throws `DomainException("يجب أن يكون للمنتج وحدة قياس واحدة على الأقل")` if trying to remove the last active unit — FILE: `SalesSystem/SalesSystem.Domain/Entities/Product.cs` — RULE-067

- [ ] T008 [P] Add DTOs to Contracts project:
  - `ProductUnitDto { Id, ProductId, UnitName, ConversionFactor, RetailPrice, WholesalePrice, AvgCost, IsBaseUnit, IsActive, Barcodes: List<string> }`
  - `ProductPriceHistoryDto { Id, ProductUnitId, UnitName, OldRetailPrice, NewRetailPrice, OldWholesalePrice, NewWholesalePrice, OldAvgCost, NewAvgCost, ChangeReason, ChangedByUserName, ChangedAt }`
  - `BarcodeResolutionDto { ProductId, ProductName, ProductUnitId, UnitName, ConversionFactor, RetailPrice, WholesalePrice }`
  — FILE: `SalesSystem/SalesSystem.Contracts/Responses/ProductUnitDto.cs`, `ProductPriceHistoryDto.cs`, `BarcodeResolutionDto.cs`

- [ ] T009 [P] Add request DTOs to Contracts:
  - `AddProductUnitRequest { UnitName, ConversionFactor, RetailPrice, WholesalePrice, IsBaseUnit, Barcodes: List<string> }`
  - `UpdateProductUnitRequest { UnitName, RetailPrice, WholesalePrice }`
  — FILE: `SalesSystem/SalesSystem.Contracts/Requests/AddProductUnitRequest.cs`, `UpdateProductUnitRequest.cs`

- [ ] T010 Add EF Core configurations (Fluent API only, no DataAnnotations):
  - `ProductUnitConfiguration`: table `ProductUnits`, all decimal props with `HasPrecision`, `UnitName` nvarchar(50), FK to Products with `DeleteBehavior.Restrict`, FK to Users for `CreatedByUserId`, global query filter `IsActive == true`
  - `UnitBarcodeConfiguration`: table `UnitBarcodes`, `Barcode` nvarchar(100), `HasIndex(x => x.Barcode).IsUnique()`, FK to ProductUnits with `DeleteBehavior.Restrict`
  - `ProductPriceHistoryConfiguration`: table `ProductPriceHistory`, all decimal `HasPrecision(18,2)`, `ChangeReason` nvarchar(255), FK to ProductUnits and Users with `DeleteBehavior.Restrict`
  — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/Configurations/ProductUnitConfiguration.cs`, `UnitBarcodeConfiguration.cs`, `ProductPriceHistoryConfiguration.cs` — RULE-016, RULE-008, RULE-011(EF)

- [ ] T011 Add `DbSet<ProductUnit> ProductUnits`, `DbSet<UnitBarcode> UnitBarcodes`, `DbSet<ProductPriceHistory> ProductPriceHistory` to `SalesDbContext` and register the three new configurations in `OnModelCreating` — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/SalesDbContext.cs`

- [ ] T012 Add `IProductUnitRepository` interface with methods: `GetByProductIdAsync`, `GetByIdAsync`, `GetBarcodeAsync(string barcode)`, `AddAsync`, `AddBarcodeAsync`, `AddPriceHistoryAsync` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Repositories/IProductUnitRepository.cs`

- [ ] T013 [P] Implement `ProductUnitRepository` implementing `IProductUnitRepository`, using `SalesDbContext` via constructor injection — FILE: `SalesSystem/SalesSystem.Infrastructure/Repositories/ProductUnitRepository.cs`

- [ ] T014 [P] Register `IProductUnitRepository` → `ProductUnitRepository` as Scoped in Infrastructure DI extension method — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs`

- [ ] T015 Add `IUpdateProductPricingService` interface with method: `Task<Result> UpdateCostAsync(int productId, int purchaseInvoiceId, decimal newUnitCost, decimal newQty, int changedByUserId, CancellationToken ct)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IUpdateProductPricingService.cs` — RULE-006

- [ ] T016 Create EF Core migration for the three new tables. Run: `dotnet ef migrations add AddDynamicUOM --project SalesSystem/SalesSystem.Infrastructure --startup-project SalesSystem/SalesSystem.Api`. Verify generated migration SQL has correct types (decimal, nvarchar) and the unique index on UnitBarcodes.Barcode — FILE: `SalesSystem/SalesSystem.Infrastructure/Migrations/`

- [ ] T017 Write a data migration step in `DbSeeder.cs` (inside `SeedAsync`): for every existing `Product` that has no `ProductUnit` rows, insert one base `ProductUnit` with `UnitName = "قطعة"`, `ConversionFactor = 1`, `IsBaseUnit = true`, `RetailPrice` and `AvgCost` from the product's existing price columns. This prevents breaking existing data — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/DbSeeder.cs`

**Checkpoint**: `dotnet build` passes. Migration runs. Existing products each have one `ProductUnit` row.

---

## Phase 3: User Story 1 — Multi-Unit Product Definition (Priority: P1) 🎯

**Goal**: Admins can define multiple units per product with per-unit pricing and barcodes. Cashiers can scan any unit barcode to auto-select the product and price.

**Independent Test**: Create product → add Box unit (factor=12, retail=24.00) with barcode "BOX123" → `GET /api/v1/barcodes/BOX123` returns productId, unitId, retail=24.00.

- [ ] T018 [US1] Implement `UpdateProductPricingService` class (implements `IUpdateProductPricingService`). In the constructor inject `IUnitOfWork`, `IProductUnitRepository`, `ILogger<UpdateProductPricingService>`. Stub the `UpdateCostAsync` method returning `Result.Success()` for now (will be completed in US2) — FILE: `SalesSystem/SalesSystem.Application/Services/UpdateProductPricingService.cs` — RULE-075

- [ ] T019 [P] [US1] Add `IProductUnitService` interface with methods:
  - `Task<Result<List<ProductUnitDto>>> GetByProductIdAsync(int productId, CancellationToken ct)`
  - `Task<Result<ProductUnitDto>> AddUnitAsync(int productId, AddProductUnitRequest req, int createdByUserId, CancellationToken ct)`
  - `Task<Result<ProductUnitDto>> UpdateUnitAsync(int productId, int unitId, UpdateProductUnitRequest req, CancellationToken ct)`
  - `Task<Result> DeleteUnitAsync(int productId, int unitId, DeleteStrategy strategy, CancellationToken ct)`
  - `Task<Result<BarcodeResolutionDto>> ResolveBarCodeAsync(string barcode, CancellationToken ct)`
  — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IProductUnitService.cs` — RULE-006

- [ ] T020 [US1] Implement `ProductUnitService` (implements `IProductUnitService`). `AddUnitAsync`: validate barcode uniqueness (pre-insert check → `Result.Failure("الباركود مستخدم مسبقاً")` if duplicate), enforce last-unit rule, save via `IProductUnitRepository`, create initial `ProductPriceHistory` entry with reason `"إنشاء وحدة جديدة"`. `DeleteUnitAsync`: check last-unit guard → if strategy = Deactivate set `IsActive = false`; if Permanent check invoice references. All writes inside `IUnitOfWork.SaveChangesAsync` — FILE: `SalesSystem/SalesSystem.Application/Services/ProductUnitService.cs` — RULE-003, RULE-067

- [ ] T021 [P] [US1] Add `AddProductUnitRequestValidator` (FluentValidation): `UnitName` NotEmpty MaxLength(50); `ConversionFactor` GreaterThan(0); `RetailPrice` GreaterThanOrEqualTo(0); `WholesalePrice` GreaterThanOrEqualTo(0) — FILE: `SalesSystem/SalesSystem.Api/Validators/AddProductUnitRequestValidator.cs` — RULE-044

- [ ] T022 [P] [US1] Add `UpdateProductUnitRequestValidator`: `UnitName` NotEmpty MaxLength(50); `RetailPrice` and `WholesalePrice` GreaterThanOrEqualTo(0) — FILE: `SalesSystem/SalesSystem.Api/Validators/UpdateProductUnitRequestValidator.cs` — RULE-044

- [ ] T023 [US1] Create `ProductUnitsController` with `[Authorize]` and `[Route("api/v1/products/{productId:int}/units")]`. Inject `IProductUnitService`. Implement:
  - `GET /` → `GetByProductIdAsync` → 200/404
  - `POST /` → validate with `AddProductUnitRequestValidator`, `AddUnitAsync` → 201/400
  - `PUT /{unitId}` → `UpdateUnitAsync` → 200/400/404
  - `DELETE /{unitId}` → accept `{ "strategy": int }` body → `DeleteUnitAsync` → 200/409
  — FILE: `SalesSystem/SalesSystem.Api/Controllers/ProductUnitsController.cs` — RULE-025, RULE-038

- [ ] T024 [P] [US1] Create `BarcodesController` with `[Authorize]` and `[Route("api/v1/barcodes")]`. Single endpoint: `GET /{barcode}` → `IProductUnitService.ResolveBarCodeAsync(barcode)` → 200 with `BarcodeResolutionDto` or 404 — FILE: `SalesSystem/SalesSystem.Api/Controllers/BarcodesController.cs` — RULE-025, RULE-038

- [ ] T025 [P] [US1] Add `IProductUnitApiService` interface and `ProductUnitApiService` implementation using `IHttpClientFactory`. Methods: `GetByProductIdAsync(int)`, `AddUnitAsync(int, AddProductUnitRequest)`, `UpdateUnitAsync(int, int, UpdateProductUnitRequest)`, `DeleteUnitAsync(int, int, DeleteStrategy)`, `ResolveBarCodeAsync(string)` — all return `Result<T>` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/ProductUnitApiService.cs` — RULE-007

- [ ] T026 [US1] Build `ProductUnitEditorView.xaml` + `ProductUnitEditorViewModel.cs`. Fields: UnitName* (TextBox, ToolTip="اسم الوحدة مطلوب"), ConversionFactor* (TextBox numeric, ToolTip="عامل التحويل — عدد الوحدات الأساسية في هذه الوحدة"), RetailPrice* (TextBox), WholesalePrice (TextBox), Barcodes (ListBox with Add/Remove buttons). `SaveCommand` always enabled. `Validate()` shows `_dialogService.ShowWarningAsync` listing missing fields. On success: `_toastService.ShowSuccess("تم حفظ الوحدة")` and publish `ProductChangedMessage` on EventBus — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Products/ProductUnitEditorView.xaml` + `ViewModels/Products/ProductUnitEditorViewModel.cs` — RULE-054, RULE-059

- [ ] T027 [US1] Build `ProductUnitsListView.xaml` + `ProductUnitsListViewModel.cs`. DataGrid columns: UnitName, ConversionFactor, RetailPrice, WholesalePrice, AvgCost, IsBaseUnit (badge), Barcodes (comma-separated). Toolbar: "إضافة وحدة" button opens `ProductUnitEditorView`. Delete button shows `_dialogService.ShowDeleteConfirmationAsync`. Subscribe to `ProductChangedMessage` on EventBus, unsubscribe in `Dispose()` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Products/ProductUnitsListView.xaml` + `ViewModels/Products/ProductUnitsListViewModel.cs` — RULE-012, RULE-013, RULE-054

- [ ] T028 [US1] Register `IProductUnitService` → `ProductUnitService` (Scoped) in `InfrastructureDI`. Register `IProductUnitApiService` → `ProductUnitApiService` (Scoped) in `App.xaml.cs`. Register `ProductUnitsListViewModel` and `ProductUnitEditorViewModel` in `App.xaml.cs` — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs` + `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: Admins can add/edit/delete product units via UI. Barcode scan via `GET /api/v1/barcodes/{barcode}` resolves correctly.

---

## Phase 4: User Story 2 — Automatic Cost Recalculation on Purchase (Priority: P1)

**Goal**: Posting a purchase invoice triggers cost recalculation per the configured `SystemSettings.CostingMethod` and cascades the updated cost to all derived units.

**Independent Test**: POST a purchase invoice for productId=1, qty=100, unitCost=5.00. Check `ProductUnits` — base unit AvgCost updated, derived units' AvgCost = newBaseCost × factor. Check `ProductPriceHistory` for new entry.

- [ ] T029 [US2] Implement the full body of `UpdateProductPricingService.UpdateCostAsync`. Logic:
  1. Read `SystemSettings.CostingMethod` from DB (or inject via options).
  2. Read current base `ProductUnit` (where `IsBaseUnit == true` for the product).
  3. Read current `WarehouseStock.Quantity` for the product as `oldStock`.
  4. Compute `newAvgCost` based on `CostingMethod`:
     - `WeightedAverage`: `(oldStock * oldAvgCost + newQty * newUnitCost) / (oldStock + newQty)`. Guard: if denominator = 0 use `newUnitCost`.
     - `LastPurchasePrice`: `newAvgCost = newUnitCost`.
     - `SupplierPrice`: `newAvgCost = product.SupplierPrice` (read from Product entity).
  5. Update base unit: `baseUnit.UpdateCost(newAvgCost)`.
  6. For each non-base `ProductUnit`: `unit.UpdateCost(newAvgCost * unit.ConversionFactor)`.
  7. Create one `ProductPriceHistory` entry per updated unit.
  8. Log with Serilog: `"تحديث تكلفة المنتج {ProductId} — التكلفة الجديدة: {NewCost}"`
  — FILE: `SalesSystem/SalesSystem.Application/Services/UpdateProductPricingService.cs` — RULE-068–RULE-076

- [ ] T030 [US2] In the existing `PurchaseInvoiceService.PostAsync` method: after the invoice is saved and has an ID (inside the transaction, Step 4), call `await _pricingService.UpdateCostAsync(item.ProductId, invoice.Id, item.UnitCost, item.Quantity, invoice.CreatedByUserId, ct)` for each invoice item. Wrap any pricing failure in a Serilog warning but do NOT rollback the transaction for pricing failures (pricing is a best-effort update, the invoice must be posted) — FILE: `SalesSystem/SalesSystem.Application/Services/PurchaseInvoiceService.cs` — RULE-003, RULE-070

- [ ] T031 [P] [US2] Register `IUpdateProductPricingService` → `UpdateProductPricingService` as Scoped in `InfrastructureDI` — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs`

- [ ] T032 [P] [US2] Add unit test for `UpdateProductPricingService` covering: (a) WeightedAverage with existing stock, (b) WeightedAverage with zero existing stock, (c) LastPurchasePrice overwrites cost, (d) cost cascades to all derived units (verify AvgCost = newBase × factor) — FILE: `SalesSystem/Tests/SalesSystem.Application.Tests/Services/UpdateProductPricingServiceTests.cs`

**Checkpoint**: Post a purchase invoice → `ProductUnits` table shows updated AvgCost on all units → `ProductPriceHistory` has new rows.

---

## Phase 5: User Story 3 — Price & Cost Change Audit Trail (Priority: P2)

**Goal**: Every price/cost change is recorded immutably in `ProductPriceHistory`. Managers can view the full history per product.

**Independent Test**: Change a product's retail price manually via PUT endpoint → `GET /api/v1/products/{id}/price-history` returns a new entry with old/new values, reason, username, and timestamp.

- [ ] T033 [US3] Add `GET /api/v1/products/{productId}/price-history` endpoint to `ProductUnitsController`. Returns `Result<List<ProductPriceHistoryDto>>` ordered by `ChangedAt` descending. Include `ChangedByUserName` (JOIN to Users table in repository query) — FILE: `SalesSystem/SalesSystem.Api/Controllers/ProductUnitsController.cs` — RULE-025

- [ ] T034 [P] [US3] Add `GetPriceHistoryByProductIdAsync(int productId)` method to `IProductUnitRepository` and implement it in `ProductUnitRepository` with a JOIN to `Users` to populate `ChangedByUserName` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Repositories/IProductUnitRepository.cs` + `SalesSystem/SalesSystem.Infrastructure/Repositories/ProductUnitRepository.cs`

- [ ] T035 [P] [US3] Add `AdjustPriceAsync(int productId, int unitId, decimal newRetailPrice, decimal newWholesalePrice, int changedByUserId, CancellationToken ct)` method to `IProductUnitService` and implement in `ProductUnitService`. This must create a `ProductPriceHistory` entry with reason `"تعديل يدوي للسعر"` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IProductUnitService.cs` + `SalesSystem/SalesSystem.Application/Services/ProductUnitService.cs` — RULE-086

- [ ] T036 [P] [US3] Add `GetPriceHistoryAsync(int productId)` to `IProductUnitApiService` and `ProductUnitApiService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/ProductUnitApiService.cs`

- [ ] T037 [US3] Build `ProductPriceHistoryView.xaml` + `ProductPriceHistoryViewModel.cs`. Read-only DataGrid with columns: UnitName, OldRetailPrice, NewRetailPrice, OldAvgCost, NewAvgCost, ChangeReason, ChangedByUserName, ChangedAt. Display in RTL Arabic layout. Load history on `OnNavigatedTo` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Products/ProductPriceHistoryView.xaml` + `ViewModels/Products/ProductPriceHistoryViewModel.cs`

**Checkpoint**: `GET /api/v1/products/{id}/price-history` returns full ordered history. UI grid shows all entries.

---

## Phase 6: User Story 4 — Smart Unit Display in UI (Priority: P3)

**Goal**: UI displays stock quantities in the most readable unit (e.g., "2 cartons" instead of "288 pieces"). Display-only — no stored value changes.

**Independent Test**: Stock = 288 base units. Product has Box (12×) and Carton (144×). `SmartUnitFormatter.Format(288, units)` returns `"2 كرتون"`.

- [ ] T038 [US4] Implement `SmartUnitFormatter` as a static service class (no DI needed — pure function). Method: `public static string Format(decimal baseQuantity, IEnumerable<ProductUnitDto> units)`. Algorithm: sort units by `ConversionFactor` descending; for each unit compute `display = Math.Floor(baseQuantity / unit.ConversionFactor)`; return `"{display} {unit.UnitName}"` for the first unit where `display >= 1`. Fallback: return `"{baseQuantity} {baseUnit.UnitName}"` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Formatting/SmartUnitFormatter.cs` — RULE-064

- [ ] T039 [P] [US4] Integrate `SmartUnitFormatter` in inventory/stock list ViewModels: wherever `WarehouseStock.Quantity` is displayed (e.g., `WarehouseStocksListViewModel`), call `SmartUnitFormatter.Format(stock.Quantity, productUnits)` for the display binding — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Inventory/WarehouseStocksListViewModel.cs`

**Checkpoint**: Inventory screen displays "2 كرتون" for a product with 288 base units when Carton (144×) is defined.

---

## Phase 7: Polish & Cross-Cutting

- [ ] T040 [P] Verify all new endpoints have `[Authorize]`. Search: `grep -r "public.*IActionResult\|public.*Task<IActionResult" SalesSystem/SalesSystem.Api/Controllers/ProductUnitsController.cs` — all must have `[Authorize]` — RULE-038

- [ ] T041 [P] Verify no `MessageBox.Show` in new ViewModels: `grep -r "MessageBox.Show" SalesSystem/SalesSystem.DesktopPWF/ViewModels/Products/` — RULE-055

- [ ] T042 [P] Add Arabic ToolTips to all interactive controls in `ProductUnitEditorView.xaml` and `ProductUnitsListView.xaml` per RULE-031

- [ ] T043 Update `docs/CHANGELOG.md` with v4.3 entry: Dynamic UOM (ProductUnit, UnitBarcode), Smart Unit Formatter, Weighted Average costing, Price History audit trail — FILE: `docs/CHANGELOG.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS Phase 3–6**
- **Phase 3 (US1)**: Depends on Phase 2 only
- **Phase 4 (US2)**: Depends on Phase 2 + T018 (stub `UpdateProductPricingService`)
- **Phase 5 (US3)**: Depends on Phase 2 + T019/T020 (price history creation in service)
- **Phase 6 (US4)**: Depends on Phase 2 + T008 (ProductUnitDto needed for formatter)
- **Phase 7 (Polish)**: Depends on all story phases

### Parallel Opportunities

```
Phase 2: T003, T004, T005, T006 run in parallel → T007 → T008, T009 in parallel → T010 → T011 → T012 → T013, T014 in parallel → T016 → T017

Phase 3: T019, T021, T022, T024, T025 run in parallel with T018 → T020 (needs T019) → T023 (needs T020) → T026 → T027 → T028

Phase 4: T031, T032 run in parallel with T029 → T030 (needs T029)

Phase 5: T034, T035, T036 run in parallel → T033 (needs T034) → T037 (needs T035, T036)
```

---

## Implementation Strategy

### MVP First (US1 + US2 only)

1. Complete Phase 2: Foundational
2. Complete Phase 3: US1 — Units, pricing, barcodes
3. Complete Phase 4: US2 — Cost recalculation on purchase
4. **STOP AND VALIDATE** — test purchase posting triggers cost cascade
5. Deploy (audit trail + smart display can follow)

### Incremental Delivery

- Phase 2 → Phases 3+4 (core purchasing accuracy) → Phase 5 (audit) → Phase 6 (display polish)

---

## Notes

- All money fields: `decimal(18,2)` — NEVER float/double (RULE-001)
- All quantities: `decimal(18,3)` — NEVER int (RULE-002)
- `SmartUnitFormatter` is UI-only — no backend equivalent (RULE-064)
- Cost cascade runs inside the purchase-posting transaction (RULE-070)
- `ProductPriceHistory` is immutable — no UPDATE or DELETE ever (RULE-084)
- `[P]` = parallelizable (different files, no incomplete dependencies)
