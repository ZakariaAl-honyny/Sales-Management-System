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

- [x] T002 Add `CostingMethod` enum to Domain — FILE: `SalesSystem/SalesSystem.Domain/Enums/CostingMethod.cs` — RULE-069

- [x] T003 [P] Add `SaleMode` enum — FILE: `SalesSystem/SalesSystem.Domain/Enums/SaleMode.cs` — RULE-049

- [x] T004 [P] Create `ProductUnit` entity in Domain — FILE: `SalesSystem/SalesSystem.Domain/Entities/ProductUnit.cs` — RULE-042, RULE-052, RULE-065

- [x] T005 [P] Create `UnitBarcode` entity — FILE: `SalesSystem/SalesSystem.Domain/Entities/UnitBarcode.cs` — RULE-063

- [x] T006 [P] Create `ProductPriceHistory` entity — FILE: `SalesSystem/SalesSystem.Domain/Entities/ProductPriceHistory.cs` — RULE-084, RULE-085

- [x] T007 Add `Units` navigation property to `Product` entity + `GetBaseUnit()` + `AddUnit()` guard — FILE: `SalesSystem/SalesSystem.Domain/Entities/Product.cs` — RULE-067

- [x] T008 [P] Add DTOs to Contracts project — FILE: `SalesSystem/SalesSystem.Contracts/Responses/ProductUnitDto.cs`, `ProductPriceHistoryDto.cs`, `BarcodeResolutionDto.cs`

- [x] T009 [P] Add request DTOs to Contracts — FILE: `SalesSystem/SalesSystem.Contracts/Requests/AddProductUnitRequest.cs`, `UpdateProductUnitRequest.cs`

- [x] T010 Add EF Core configurations (Fluent API only, no DataAnnotations) — FILE: `ProductUnitConfiguration.cs`, `UnitBarcodeConfiguration.cs`, `ProductPriceHistoryConfiguration.cs`

- [x] T011 Add `DbSet` entries to `SalesDbContext` and register configs in `OnModelCreating` — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/SalesDbContext.cs`

- [x] T012 Add `IProductUnitRepository` interface *(used IUnitOfWork pattern instead)*

- [x] T013 [P] Implement `ProductUnitRepository` *(used IUnitOfWork pattern instead)*

- [x] T014 [P] Register repository in DI *(handled via IUnitOfWork registration)*

- [x] T015 Add `IUpdateProductPricingService` interface — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IUpdateProductPricingService.cs` — RULE-006

- [x] T016 Create EF Core migration for the three new tables — FILE: `SalesSystem/SalesSystem.Infrastructure/Migrations/`

- [x] T017 Write data migration step in `DbSeeder.cs` to seed base ProductUnit for existing products — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/DbSeeder.cs`

**Checkpoint**: ✅ `dotnet build` passes. ✅ Existing products each have one `ProductUnit` row.

---

## Phase 3: User Story 1 — Multi-Unit Product Definition (Priority: P1) 🎯

- [x] T018 [US1] Implement `UpdateProductPricingService` class (stub) — FILE: `SalesSystem/SalesSystem.Application/Services/UpdateProductPricingService.cs` — RULE-075

- [x] T019 [P] [US1] Add `IProductUnitService` interface — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IProductUnitService.cs` — RULE-006

- [x] T020 [US1] Implement `ProductUnitService` — FILE: `SalesSystem/SalesSystem.Application/Services/ProductUnitService.cs` — RULE-003, RULE-067

- [x] T021 [P] [US1] Add `AddProductUnitRequestValidator` — FILE: `SalesSystem/SalesSystem.Api/Validators/AddProductUnitRequestValidator.cs` — RULE-044

- [x] T022 [P] [US1] Add `UpdateProductUnitRequestValidator` — FILE: `SalesSystem/SalesSystem.Api/Validators/UpdateProductUnitRequestValidator.cs` — RULE-044

- [x] T023 [US1] Create `ProductUnitsController` — FILE: `SalesSystem/SalesSystem.Api/Controllers/ProductUnitsController.cs` — RULE-025, RULE-038

- [x] T024 [P] [US1] Create `BarcodesController` — FILE: `SalesSystem/SalesSystem.Api/Controllers/BarcodesController.cs` — RULE-025, RULE-038

- [x] T025 [P] [US1] Add `IProductUnitApiService` and `ProductUnitApiService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/ProductUnitApiService.cs` — RULE-007

- [x] T026 [US1] Build `ProductUnitEditorView.xaml` + `ProductUnitEditorViewModel.cs` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Products/ProductUnitEditorView.xaml` + `ViewModels/Products/ProductUnitEditorViewModel.cs` — RULE-054, RULE-059

- [x] T027 [US1] Build `ProductUnitsListView.xaml` + `ProductUnitsListViewModel.cs` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Products/ProductUnitsListView.xaml` + `ViewModels/Products/ProductUnitsListViewModel.cs` — RULE-012, RULE-013, RULE-054

- [x] T028 [US1] Register services in DI — FILE: `SalesSystem/SalesSystem.Api/Program.cs` + `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: ✅ Admins can add/edit/delete product units via UI. ✅ Barcode scan resolves correctly.

---

## Phase 4: User Story 2 — Automatic Cost Recalculation on Purchase (Priority: P1)

- [x] T029 [US2] Implement the full body of `UpdateProductPricingService.UpdateCostAsync` — FILE: `SalesSystem/SalesSystem.Application/Services/UpdateProductPricingService.cs` — RULE-068–RULE-076

- [x] T030 [US2] Hook cost recalculation into `PurchaseService.PostAsync` — FILE: `SalesSystem/SalesSystem.Application/Services/PurchaseService.cs` — RULE-003, RULE-070

- [x] T031 [P] [US2] Register `IUpdateProductPricingService` → `UpdateProductPricingService` — FILE: `SalesSystem/SalesSystem.Api/Program.cs`

- [x] T032 [P] [US2] Add unit tests for `UpdateProductPricingService` (8 tests, all passing) — FILE: `SalesSystem/Tests/SalesSystem.Application.Tests/Services/UpdateProductPricingServiceTests.cs`

**Checkpoint**: ✅ Post a purchase invoice → cost cascade updates all units → ProductPriceHistory has new rows.

---

## Phase 5: User Story 3 — Price & Cost Change Audit Trail (Priority: P2)

*Not included in current MVP scope.*

- [ ] T033–T037 (Deferred)

---

## Phase 6: User Story 4 — Smart Unit Display in UI (Priority: P3)

*Not included in current MVP scope.*

- [ ] T038–T039 (Deferred)

---

## Phase 7: Polish & Cross-Cutting

- [x] T040 [P] Verify all new endpoints have `[Authorize]` — ✅ All controllers have `[Authorize]` — RULE-038

- [x] T041 [P] Verify no `MessageBox.Show` in new ViewModels — ✅ None found — RULE-055

- [x] T042 [P] Add Arabic ToolTips to all interactive controls in XAML files — ✅ Applied — RULE-031

- [ ] T043 Update `docs/CHANGELOG.md` with v4.3 entry *(pending)*

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS Phase 3–6**
- **Phase 3 (US1)**: Depends on Phase 2 only
- **Phase 4 (US2)**: Depends on Phase 2 + T018 (stub `UpdateProductPricingService`)
- **Phase 5 (US3)**: Depends on Phase 2 + T019/T020
- **Phase 6 (US4)**: Depends on Phase 2 + T008
- **Phase 7 (Polish)**: Depends on all story phases

---

## Implementation Strategy

### MVP First (US1 + US2 only) ✅ COMPLETE

1. ✅ Phase 2: Foundational
2. ✅ Phase 3: US1 — Units, pricing, barcodes
3. ✅ Phase 4: US2 — Cost recalculation on purchase
4. ✅ STOP AND VALIDATE — Build: 0 errors, Tests: 8/8 pricing tests passing

---

## Notes

- All money fields: `decimal(18,2)` — NEVER float/double (RULE-001)
- All quantities: `decimal(18,3)` — NEVER int (RULE-002)
- `SmartUnitFormatter` is UI-only — no backend equivalent (RULE-064)
- Cost cascade runs inside the purchase-posting transaction (RULE-070)
- `ProductPriceHistory` is immutable — no UPDATE or DELETE ever (RULE-084)
- `[P]` = parallelizable (different files, no incomplete dependencies)
