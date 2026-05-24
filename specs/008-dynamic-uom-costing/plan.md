# Implementation Plan: Dynamic UOM & Costing Engine (v4.3)

**Branch**: `008-dynamic-uom-costing` | **Date**: 2026-05-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/008-dynamic-uom-costing/spec.md`

---

## Summary

Enable products to carry multiple units of measure — each with its own conversion factor, retail/wholesale price, average cost, and barcodes. When a purchase invoice is posted, the system automatically recalculates the product cost using the configured strategy (Weighted Average, Last Purchase Price, or Supplier Price) and cascades the updated cost to all derived units. Every price/cost change is recorded immutably in `ProductPriceHistory`. A display-only `SmartUnitFormatter` presents quantities in the most legible unit in the UI.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, FluentValidation 11, Serilog 8, BCrypt.Net-Next 4
**Storage**: SQL Server 2019+ — new tables: `ProductUnits`, `UnitBarcodes`, `ProductPriceHistory`; extended: `SystemSettings` (CostingMethod seed row)
**Testing**: xUnit + Moq + FluentAssertions (matches existing test projects)
**Target Platform**: Windows x64 (API as Windows Service + WPF Desktop)
**Project Type**: Desktop App + REST API (Clean Architecture — 6 existing projects)
**Performance Goals**: Cost recalculation for a single product (all units) completes within 3 seconds after invoice post; barcode lookup resolves in < 100 ms
**Constraints**: All money = `decimal(18,2)`; all quantities = `decimal(18,3)`; no float/double anywhere; full transactional integrity on purchase posting
**Scale/Scope**: Single-store retail, ~1,000 active products, each with up to 5 units

---

## Constitution Check

*GATE: Must pass before Phase 0 research.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | `RetailPrice`, `WholesalePrice`, `AvgCost` → `decimal(18,2)`; `ConversionFactor`, `Quantity` → `decimal(18,3)` |
| II | Domain-Computed Financial Formulas | ✅ PASS | Weighted average formula lives in `UpdateProductPricingService` (Application layer); cost cascade in `ProductUnit` entity method |
| III | Transactional Integrity | ✅ PASS | Cost update fires inside the existing purchase-posting transaction (after invoice saved with ID) |
| IV | Invoice Lifecycle State Machine | ✅ PASS | No new invoice state; cost update only on `Posted` transition |
| V | Stock Integrity | ✅ PASS | Stock deduction uses base-unit quantity (ConvertToUnit applied in Domain before deduction); InventoryMovement created per existing pattern |
| VI | Result Pattern | ✅ PASS | `UpdateProductPricingService` returns `Result<T>`; all new API endpoints return `Result` |
| VII | Clean Architecture Boundaries | ✅ PASS | `SmartUnitFormatter` lives in `DesktopPWF` only (UI layer); pricing logic in `Application`; entities in `Domain` |
| VIII | Security | ✅ PASS | New `/api/v1/products/{id}/units` endpoints carry `[Authorize]`; no new public endpoints |
| IX | Four-Layer Validation | ✅ PASS | Domain guard clauses on `ProductUnit`; FluentValidation on `AddProductUnitRequest`; `CHECK (ConversionFactor > 0)` in DB |
| X | Logging | ✅ PASS | Cost recalculations logged via Serilog at `Information` level with product ID and new cost value |
| XI | EF Core Conventions | ✅ PASS | Fluent API config only; all FKs use `DeleteBehavior.Restrict`; `nvarchar` for all name fields |
| XII | Audit Trail | ✅ PASS | `ProductPriceHistory.ChangedByUserId` references `Users` table; Users remain soft-delete only |
| XIII | Delete Strategy | ✅ PASS | Deleting a `ProductUnit`: attempts Deactivate; hard-delete blocked if referenced by invoice items |
| XIV | Defensive Programming | ✅ PASS | `ProductUnit` constructor has guard clauses: name not empty, conversionFactor > 0, prices ≥ 0 |
| XV | WPF Interactive Dialogs | ✅ PASS | All dialogs in new ViewModels via `IDialogService` |
| XVI | Toast Notifications | ✅ PASS | Unit saved/deleted confirmations via `IToastNotificationService` |
| XVII | Real-Time UI Validation | ✅ PASS | Unit editor ViewModel implements `INotifyDataErrorInfo`; save button respects `HasErrors` |

**Gate Result**: ✅ ALL CLEAR — proceed to Phase 0.

---

## Project Structure

### Documentation (this feature)

```text
specs/008-dynamic-uom-costing/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── contracts/           ← Phase 1 output
│   ├── api-contracts.md
│   └── ui-contracts.md
└── tasks.md             ← Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── ProductUnit.cs              ← NEW
│       ├── UnitBarcode.cs              ← NEW
│       └── ProductPriceHistory.cs      ← NEW
│
├── SalesSystem.Contracts/
│   ├── Requests/
│   │   ├── AddProductUnitRequest.cs    ← NEW
│   │   └── UpdateProductUnitRequest.cs ← NEW
│   └── Responses/
│       ├── ProductUnitDto.cs           ← NEW
│       └── ProductPriceHistoryDto.cs   ← NEW
│
├── SalesSystem.Application/
│   ├── Interfaces/Services/
│   │   └── IUpdateProductPricingService.cs  ← NEW
│   └── Services/
│       └── UpdateProductPricingService.cs   ← NEW
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs           ← EXTEND (add DbSets)
│   │   └── Configurations/
│   │       ├── ProductUnitConfiguration.cs  ← NEW
│   │       ├── UnitBarcodeConfiguration.cs  ← NEW
│   │       └── ProductPriceHistoryConfiguration.cs ← NEW
│   └── Migrations/                     ← NEW migration
│
├── SalesSystem.Api/
│   ├── Controllers/
│   │   └── ProductUnitsController.cs   ← NEW
│   └── Validators/
│       ├── AddProductUnitRequestValidator.cs   ← NEW
│       └── UpdateProductUnitRequestValidator.cs ← NEW
│
└── SalesSystem.DesktopPWF/
    ├── Services/Formatting/
    │   └── SmartUnitFormatter.cs       ← NEW
    ├── Services/Api/
    │   └── ProductUnitApiService.cs    ← NEW
    ├── Views/Products/
    │   └── ProductUnitEditorView.xaml  ← NEW
    └── ViewModels/Products/
        └── ProductUnitEditorViewModel.cs ← NEW
```

---

## Complexity Tracking

No constitution violations requiring justification — standard architecture patterns used throughout.
