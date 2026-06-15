# Tasks: Products Module — Remaining Work

**Input**: Design documents from `specs/028-products-module-complete/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Included per user story where applicable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Solution root**: `SalesSystem/`
- **6 projects**: `Domain/`, `Contracts/`, `Application/`, `Infrastructure/`, `Api/`, `DesktopPWF/`
- **Tests**: `Tests/` subfolders per test project

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Domain entity cleanup and contracts DTO changes that ALL user stories depend on.

**⚠️ CRITICAL**: These tasks must be completed before the migration can be created.

- [ ] T001 [P] Rename `HasExpiry` to `TrackExpiry` on `Product` entity in `SalesSystem.Domain/Entities/Product.cs` (property declaration + Create() + Update() parameters)
- [ ] T002 [P] Remove `Cost` property and `UpdateCost()` method from `Product` entity in `SalesSystem.Domain/Entities/Product.cs`
- [ ] T003 [P] Rename `HasExpiry` to `TrackExpiry` in `ProductDto` record at `SalesSystem.Contracts/DTOs/AllDtos.cs`
- [ ] T004 [P] Remove `Cost` field from `ProductDto` record at `SalesSystem.Contracts/DTOs/AllDtos.cs`
- [ ] T005 [P] Delete `ProductBarcode` entity file at `SalesSystem.Domain/Entities/ProductBarcode.cs`
- [ ] T006 [P] Delete `ProductBarcodeConfiguration.cs` at `SalesSystem.Infrastructure/Data/Configurations/ProductBarcodeConfiguration.cs`
- [ ] T007 [P] Remove `DbSet<ProductBarcode>` from `SalesSystem.Infrastructure/Data/SalesDbContext.cs`
- [ ] T008 [P] Remove `ProductBarcodes` property from `IUnitOfWork` interface at `SalesSystem.Application/Interfaces/IUnitOfWork.cs`
- [ ] T009 [P] Remove `_productBarcodes` field and property from `UnitOfWork` at `SalesSystem.Infrastructure/Data/UnitOfWork.cs`
- [ ] T010 Create single migration `Phase28_ProductsModuleRemaining` that: renames `HasExpiry`→`TrackExpiry`, drops `Cost` column from Products, drops `ProductBarcodes` table, updates model snapshot

**Checkpoint**: Schema and foundation ready for all user stories.

---

## Phase 2: User Story 1 — Create Product with Opening Stock (Priority: P1) 🎯 MVP

**Goal**: Inventory managers can create a new product with opening quantity and cost, which automatically creates an InventoryBatch("OPENING"), updates WarehouseStock, records InventoryMovement, and creates a journal entry.

**Independent Test**: Create a new product with Name="بطاطس عمان", Category="مواد غذائية", Opening Quantity=100, Opening Cost=50. Save. Verify:
1. Product appears in product list
2. Batches tab shows one batch with BatchNo="OPENING", Quantity=100, UnitCost=50
3. Warehouse Stock reflects 100 units
4. A journal entry exists: Dr Inventory 5000 / Cr OpeningBalanceEquity 5000

### Implementation for User Story 1

- [ ] T011 [US1] Add `OpeningQuantity` (decimal?), `OpeningUnitCost` (decimal?), `OpeningExpiryDate` (DateTime?) fields to `CreateProductRequest` at `SalesSystem.Contracts/Requests/ProductRequests.cs`
- [ ] T012 [P] [US1] Add `CreateProductOpeningEntryAsync(int productId, string productName, decimal totalOpeningValue, int createdByUserId, DateTime transactionDate, CancellationToken ct)` method to `IAccountingIntegrationService` interface and implement in `AccountingIntegrationService.cs` (Dr Inventory / Cr OpeningBalanceEquity, EntryType = OpeningBalance, ReferenceType = "Product")
- [ ] T013 [US1] Update `ProductService.CreateAsync()` at `SalesSystem.Application/Services/ProductService.cs` to: (a) resolve default warehouse via `warehouses.FirstOrDefault(w => w.IsDefault)`, (b) if `OpeningQuantity > 0`: create `InventoryBatch("OPENING")`, call `_inventoryService.IncreaseStockAsync()`, and call `_accountingService.CreateProductOpeningEntryAsync()` (if cost > 0) — all wrapped in `_uow.ExecuteTransactionAsync()`
- [ ] T014 [US1] Update `CreateProductRequestValidator` at `SalesSystem.Api/Validators/CreateProductRequestValidator.cs`: validate OpeningQuantity > 0 when provided, OpeningUnitCost >= 0, OpeningExpiryDate required when TrackExpiry = true and OpeningQuantity > 0
- [ ] T015 [US1] Add `OpeningQuantity`, `OpeningUnitCost`, `OpeningExpiryDate` properties and `ShowOpeningFields` (computed: `!IsEditMode`) to `ProductEditorViewModel` at `SalesSystem.DesktopPWF/ViewModels/Products/ProductEditorViewModel.cs` — populate `CreateProductRequest` with these values when saving in create mode
- [ ] T016 [US1] Add opening stock section to `ProductEditorView.xaml` (visible only in create mode via `Visibility="{Binding ShowOpeningFields, Converter=...}"`): "الكمية الافتتاحية" TextBox, "تكلفة الوحدة" TextBox, "تاريخ الانتهاء" DatePicker — before the "وحدات القياس" card

**Checkpoint**: US1 complete — user can create product with opening stock.

---

## Phase 3: User Story 2 — Product Cost from Batches (Priority: P1)

**Goal**: The system calculates product cost exclusively from `InventoryBatches` using weighted average FIFO methodology. The `Product.Cost` column is removed and all 30+ read references are replaced with `ProductCostService` calls.

**Independent Test**: 
1. Create a product with opening stock (Batch A: 100 units at 50)
2. Query cost → returns 50
3. Add a purchase receipt (Batch B: 50 units at 60)
4. Query cost → returns 53.33 (weighted avg: (100×50 + 50×60) / 150)
5. Sell 120 units — COGS should use FIFO layers (100@50 + 20@60 = 6200)

### Implementation for User Story 2

- [ ] T017 [P] [US2] Create `IProductCostService` interface at `SalesSystem.Application/Interfaces/Services/IProductCostService.cs` with methods: `GetAverageCostAsync(int productId, CancellationToken ct)` → `Result<decimal>`, `GetFifoLayersAsync(int productId, decimal quantity, CancellationToken ct)` → `Result<List<FifoLayerDto>>`, `GetLatestCostAsync(int productId, CancellationToken ct)` → `Result<decimal>`
- [ ] T018 [P] [US2] Create `ProductCostService` implementation at `SalesSystem.Application/Services/ProductCostService.cs` — weighted average from `InventoryBatches.Where(b => b.ProductId == productId && b.QuantityRemaining > 0)`, FIFO layers ordered by `CreatedAt ASC`, each layer returns (BatchId, BatchNo, QuantityConsumed, UnitCost, TotalCost)
- [ ] T019 [P] [US2] Create `FifoLayerDto` record in `SalesSystem.Contracts/DTOs/AllDtos.cs` — fields: BatchId (int), BatchNo (string), QuantityConsumed (decimal), UnitCost (decimal), TotalCost (decimal, computed)
- [ ] T020 [US2] Register `IProductCostService` / `ProductCostService` in DI at `SalesSystem.Api/Program.cs`
- [ ] T021 [US2] Replace `p.Cost` with `_productCostService.GetAverageCostAsync(p.Id, ct)` in `ProductService.MapToDto()` at `SalesSystem.Application/Services/ProductService.cs` (line 302)
- [ ] T022 [US2] Replace `productUnit.Product.Cost` / `product.Cost` reads with `_productCostService.GetAverageCostAsync()` in `SalesService.PostAsync()` at `SalesSystem.Application/Services/SalesService.cs` (lines 168, 176, 410) — use `GetFifoLayersAsync` for COGS calculation
- [ ] T023 [US2] Replace `item.Product.Cost` with `_productCostService.GetAverageCostAsync()` in `AccountingIntegrationService.ReverseSalesPostEntryAsync()` at `SalesSystem.Application/Accounting/Services/AccountingIntegrationService.cs` (line 416)
- [ ] T024 [US2] Replace `product.Cost` with `_productCostService.GetAverageCostAsync()` in `BarcodeLookupService` at `SalesSystem.Application/Services/BarcodeLookupService.cs` (lines 55-56)
- [ ] T025 [US2] Replace `ws.Product.Cost` with `_productCostService.GetAverageCostAsync()` in `ReportRepository` at `SalesSystem.Infrastructure/Repositories/ReportRepository.cs` (lines 230-231) — may need async query pattern change
- [ ] T026 [US2] Update `UpdateProductPricingService` at `SalesSystem.Application/Services/UpdateProductPricingService.cs` — remove `product.UpdateCost()` calls (line 62, 72), keep ProductPriceHistory audit for cost changes
- [ ] T027 [US2] Replace `product.Cost` reads in `SalesInvoiceEditorViewModel` at `SalesSystem.DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` (lines 189, 191, 676, 678, 1097, 1099, 1581, 1592) — use `ProductDto.Cost` until ProductDto.Cost removal, then fallback to API cost endpoint
- [ ] T028 [US2] Replace `product.Cost` reads in `PurchaseInvoiceEditorViewModel` at `SalesSystem.DesktopPWF/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` (lines 1086, 1130, 1179, 1197, 1421)
- [ ] T029 [US2] Replace `product.Cost` reads in `InventoryOperationEditorViewModel` at `SalesSystem.DesktopPWF/ViewModels/InventoryOperations/InventoryOperationEditorViewModel.cs` (lines 409, 450, 653)
- [ ] T030 [US2] Update `ProductSelectionViewModel` at `SalesSystem.DesktopPWF/ViewModels/Products/ProductSelectionViewModel.cs` (line 28) — remove `Cost` delegation or replace with API cost query

**Checkpoint**: US2 complete — no `Product.Cost` reads remain anywhere. Cost always from batches.

---

## Phase 4: User Story 3 — HasExpiry → TrackExpiry Rename (Priority: P2)

**Goal**: The `HasExpiry` field is renamed to `TrackExpiry` across all remaining layers (Application, Tests) to align with spec documents.

**Independent Test**: Search codebase for `HasExpiry` — zero functional references remain (only in previous migration files). Create a product with `TrackExpiry = true` and verify FEFO behavior.

### Implementation for User Story 3

- [ ] T031 [US3] Rename `HasExpiry` to `TrackExpiry` in `ProductService.cs` at `SalesSystem.Application/Services/ProductService.cs` (lines 109, 301)
- [ ] T032 [US3] Rename `HasExpiry` to `TrackExpiry` in `ProductConfiguration.cs` at `SalesSystem.Infrastructure/Data/Configurations/ProductConfiguration.cs`
- [ ] T033 [P] [US3] Rename `HasExpiry` to `TrackExpiry` in all test `ProductDto` constructions across:
  - `Tests/SalesSystem.DesktopPWF.Tests/ViewModels/Products/ProductListViewModelTests.cs`
  - `Tests/SalesSystem.DesktopPWF.Tests/ViewModels/Dashboard/DashboardViewModelTests.cs`
  - `Tests/SalesSystem.DesktopPWF.Tests/ViewModels/Purchases/PurchaseInvoiceEditorViewModelTests.cs`
  - `Tests/SalesSystem.DesktopPWF.Tests/ViewModels/Sales/SalesInvoiceEditorViewModelTests.cs`
  - `Tests/SalesSystem.DesktopPWF.Tests/ViewModels/Products/ProductEditorViewModelTests.cs`
  - `Tests/SalesSystem.Api.Tests/Controllers/Products/ProductsControllerTests.cs`

**Checkpoint**: US3 complete — `HasExpiry` fully replaced by `TrackExpiry`.

---

## Phase 5: User Story 4 — ProductBarcode Removal (Priority: P2)

**Goal**: The deprecated `ProductBarcode` entity is fully removed from the codebase. Only migration Designer files retain references (auto-generated, acceptable).

**Independent Test**: Search for `class ProductBarcode` — not found. Search `ProductBarcodes` (plural) — only in migration Designer files. Verify product creation and barcode search still work using `Product.Barcode`.

### Implementation for User Story 4

- [ ] T034 [US4] Remove stale `ProductBarcode` comment reference from `ProductUnitService.cs` at `SalesSystem.Application/Services/ProductUnitService.cs` (line 53 — mentions UnitBarcode)
- [ ] T035 [US4] Verify no remaining functional references to `ProductBarcode` entity outside of migration `.Designer.cs` files — run grep for `ProductBarcode(?!Configuration|Designer|ModelSnapshot)` to confirm

**Checkpoint**: US4 complete — `ProductBarcode` entity fully excised.

---

## Phase 6: User Story 5 — Product Import from Excel (Priority: P3)

**Goal**: Inventory managers can bulk-import products from Excel with opening stock. The existing `ProductImportViewModel` is fully functional — the XAML View just needs to be completed to expose all its features.

**Independent Test**: Create an Excel file with 5 products (names, categories, barcodes, base unit names, opening quantities and costs). Import through the UI. Verify all 5 products appear with correct data and opening batches.

### Implementation for User Story 5

- [ ] T036 [US5] Complete `ProductImportView.xaml` at `SalesSystem.DesktopPWF/Views/Products/ProductImportView.xaml` — replace placeholder with full UI including:
  - File selection: Button "اختيار ملف" + path TextBlock bound to `SelectedFilePath`
  - Template download: Button "تنزيل قالب" bound to `DownloadTemplateCommand`
  - Preview DataGrid: bound to `ImportRows`, columns for Product Name, Category, Barcode, Base Unit, Opening Qty, Opening Cost, Status
  - Action buttons: Preview (`PreviewCommand`) + ExecuteImport (`ExecuteImportCommand`)
  - Results summary: Success count, failure count, error list
  - Reset button bound to `ResetCommand`
- [ ] T037 [US5] Update `ProductImportView.xaml.cs` to set DataContext (if not already done)
- [ ] T038 [US5] Verify the import flow end-to-end: File select → Preview → Execute → Results displayed

**Checkpoint**: US5 complete — Excel import fully functional with opening stock support.

---

## Phase 7: Build & Test Verification (Polish)

**Purpose**: Ensure zero build errors and all tests pass after all changes.

- [ ] T039 Run `dotnet build SalesSystem.sln` — verify 0 errors, 0 warnings across all 6+ projects
- [ ] T040 Run `dotnet test Tests/SalesSystem.Domain.Tests` — all tests pass
- [ ] T041 Run `dotnet test Tests/SalesSystem.Application.Tests` — all tests pass
- [ ] T042 Run `dotnet test Tests/SalesSystem.DesktopPWF.Tests` — all tests pass
- [ ] T043 Run `dotnet test Tests/SalesSystem.Api.Tests` — all tests pass (if buildable)
- [ ] T044 Run `dotnet test Tests/SalesSystem.Infrastructure.Tests` — all tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (Setup) ──────────► Migration ──► US1 (Opening Stock)
                                    ├──► US2 (Cost from Batches)
                                    ├──► US3 (TrackExpiry Rename)
                                    ├──► US4 (ProductBarcode Removal)
                                    └──► US5 (Product Import)
                                              │
                                    Phase 7 ◄──┘ (Build & Test)
```

- **Phase 1 (T001-T009)**: Parallel — all entity/DTO/ORM cleanup tasks are independent
- **Migration (T010)**: Blocks ALL user stories — must complete first
- **US1-T011 through US1-T016**: Sequential within US1 (request → service → validator → VM → View)
- **US2-T017 through US2-T030**: Mostly sequential (interface → service → replace reads layer by layer)
- **US3-T031 through US3-T033**: Simple rename — can all run in parallel
- **US4-T034 through US4-T035**: Quick cleanup — single pass
- **US5-T036 through US5-T038**: XAML-only changes, independent of other stories

### User Story Independence

| Story | Depends On | Independent? |
|-------|-----------|-------------|
| US1 (Opening Stock) | Phase 1 + Migration | ✅ Yes — uses existing `InventoryBatch`, no Cost dependency |
| US2 (Cost from Batches) | Phase 1 + Migration | ✅ Yes — completely independent from US1 |
| US3 (TrackExpiry Rename) | Phase 1 + Migration | ✅ Yes — pure rename, no behavioral change |
| US4 (ProductBarcode Removal) | Phase 1 + Migration | ✅ Yes — cleanup only |
| US5 (Product Import) | Phase 1 | ✅ Yes — XAML-only, independent of migration |

### Parallel Opportunities

```bash
# Phase 1 — all independent Domain/Contracts/Infrastructure cleanup tasks:
Task: T001 Rename HasExpiry in Product.cs
Task: T002 Remove Cost from Product.cs
Task: T003 Rename HasExpiry in ProductDto
Task: T004 Remove Cost from ProductDto
Task: T005 Delete ProductBarcode.cs
Task: T006 Delete ProductBarcodeConfiguration.cs
Task: T007 Remove DbSet<ProductBarcode> from DbContext
Task: T008 Remove ProductBarcodes from IUnitOfWork
Task: T009 Remove ProductBarcodes from UnitOfWork

# US3 (TrackExpiry) — all renames in parallel:
Task: T031 Rename in ProductService.cs
Task: T032 Rename in Configuration
Task: T033 Rename in all test files

# US4 — both cleanup tasks in parallel:
Task: T034 Remove stale comment
Task: T035 Verify no remaining references
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1 (Setup + Migration)
2. Complete Phase 2: US1 — Opening Stock 🎯 **MVP**
3. **STOP and VALIDATE**: Test US1 independently (create product with opening stock)
4. Complete Phase 3: US2 — Cost from Batches
5. **STOP and VALIDATE**: Test US2 independently (cost from batches, FIFO)
6. Deploy/Demo

### Incremental Delivery

1. Phase 1 + Migration → Foundation ready
2. US1 (Opening Stock) → Test independently → **Demo-ready!** 🎯
3. US2 (Cost from Batches) → Test independently → Costing complete
4. US3 + US4 (TrackExpiry + Cleanup) → Cleanup complete
5. US5 (Import) → Bulk import ready

### Parallel Team Strategy

With multiple developers:

1. Developer A: Phase 1 entity cleanup + Migration (T001-T010)
2. Once migration is applied:
   - Developer A: US1 — Opening Stock (T011-T016)
   - Developer B: US2 — Cost from Batches (T017-T030)
   - Developer C: US3 + US4 — Rename + Cleanup (T031-T035)
   - Developer D: US5 — Import View (T036-T038)
3. Developer A (or QA): Phase 7 — Build & Test Verification (T039-T044)

---

## Notes

- [P] tasks = different files, no dependencies — can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- US3 and US4 are low-risk — they are simple renames and deletions with no behavioral change
- US5 is fully independent (XAML-only) — can be done at any time
- Migration (T010) should be verified by generating a SQL script and reviewing it before applying to a real database
- Commit after each phase or logical task group
- Stop at any checkpoint to validate independently
