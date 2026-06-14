# Phase 25 — Products Module: Comprehensive Enhancement & Implementation Plan

> **Version**: 1.0 — Full rewrite based on Analysis Parts 1–5, Global Analysis, and codebase audit
> **Scope**: Complete Products module overhaul — Pricing per unit and currency (no retail/wholesale levels), FIFO batch tracking, Assembly/BOM, Product images, Min stock alerts, Enhanced barcode management, Default seeds

---

## Table of Contents

1. [Products Architecture — 7 Sub-Modules](#1-products-architecture--7-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Products Design Catalog](#4-products-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Products Architecture — 7 Sub-Modules

Based on full codebase audit + user requirements from Analysis Parts 1–5, the Products module for V1 enhancement is divided into **7 sub-modules**:

| # | Sub-Module | Core Purpose | Dependencies |
|---|------------|--------------|--------------|
| 🏗️ | **Products CRUD** | Create, edit, list, search, soft-delete products | Categories, Units |
| 📐 | **Units (UOM)** | Multi-unit with conversion factors, base unit enforcement | Products |
| 💰 | **Pricing** | Per-unit + per-currency pricing with history (no retail/wholesale) | Units, Currencies (Phase 20) |
| 🏷️ | **Barcodes** | One barcode per product (stored on Product entity) | (none) |
| 📦 | **Batches/FIFO** | Purchase lots, FIFO/FEFO allocation, costing | Products, Purchases |
| 🗂️ | **Categories** | Product grouping, default "عام" seed | (standalone) |
| 📜 | **Price History** | Audit trail for all cost/price changes | ProductUnits |

### Data Flow

```text
Desktop → (HttpClient) → Api → Application Services → Infrastructure → SQL Server
                                    ↓
                             Domain (Products, ProductUnits, InventoryBatches, 
                                    BillOfMaterials, ProductImages, Categories)
```

**Stock/Costing flow:**
```text
PurchaseInvoice → Create InventoryBatch (FIFO) → Update WarehouseStock 
               → UpdateProductPricingService → ProductPriceHistory
SaleInvoice    → Deduct from InventoryBatches (FIFO/FEFO) → Update WarehouseStock
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Product Entity ✅ (Exists — needs extension)

**File**: `Domain/Entities/Product.cs` (245 lines)

| Field | Type | Status | Phase 25 Action |
|-------|------|--------|-----------------|
| `Id` | `int PK` | ✅ | Keep |
| `Name` | `string(150)` | ✅ | Keep |
| `Barcode` | `string(50)?` | ❌ Missing | **ADD** — one barcode per product |
| `CategoryId` | `int?` FK | ✅ | Keep |
| `UnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `WholesaleUnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `RetailUnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `ConversionFactor` (Legacy) | `decimal(18,3)` | ⚠️ Legacy | **DEPRECATE** — use ProductUnit |
| `PurchasePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — cost from InventoryBatches |
| `SalePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductPrices per (ProductUnit × CurrencyId) |
| `WholesalePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductPrices per (ProductUnit × CurrencyId) |
| `RetailPrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductPrices per (ProductUnit × CurrencyId) |
| `MinStock` | `decimal(18,3)` | ✅ | **RENAME** → `MinStockLevel` |
| `ReorderLevel` | `decimal(18,3)` | ✅ | Keep |
| `Description` | `string(500)?` | ✅ | Keep |
| `ExpirationDate` | `DateTime?` | ✅ | **REMOVE** — per-batch expiry |
| `ImagePath` | `string(500)?` | ✅ | **REPLACE** → ProductImage entity |
| `AvgCost` | `decimal(18,2)` | ❌ Missing | **ADD** — computed from batches |
| `HasExpiry` | `bool` | ❌ Missing | **ADD** — tracks if product uses expiry |
| `TrackBatches` | `bool` | ❌ Missing | **ADD** — always true per analysis |

**Methods** (all need updating):
- `Create()` — remove legacy params, accept `HasExpiry`, `TrackBatches`
- `Update()` — same
- `RemoveUnit()` — keep
- `AddUnit()` — keep
- `ValidateUnits()` — keep (enhance to require ≥2 units)
- `GetBaseUnit()`, `GetUnitById()` — keep

### 2.2 ProductUnit Entity ✅ (Exists — needs extension)

**File**: `Domain/Entities/ProductUnit.cs` (221 lines)

| Field | Type | Status | Phase 25 Action |
|-------|------|--------|-----------------|
| `Id` | `int PK` | ✅ | Keep |
| `ProductId` | `int FK` | ✅ | Keep |
| `UnitName` | `string(100)` | ✅ | Keep |
| `BaseConversionFactor` | `decimal(18,3)` | ✅ | Keep |
| `IsBaseUnit` | `bool` | ✅ | Keep |
| `SalesPrice` | `decimal(18,2)` | ✅ | **DEPRECATE** → moved to ProductPrice |
| `WholesalePrice` | `decimal(18,2)` | ✅ | **DEPRECATE** → moved to ProductPrice |
| `PurchaseCost` | `decimal(18,2)` | ✅ | Keep (computed from batches) |
| `SupplierPrice` | `decimal(18,2)` | ✅ | Keep (catalog reference) |
| `LastPurchasePrice` | `decimal(18,2)` | ✅ | Keep (informational) |
| `SortOrder` | `int` | ✅ | Keep |
| Barcodes collection | `List<UnitBarcode>` | ✅ | Keep |

**New fields**:
- `CurrencyId` → ❌ Missing → **ADD** (moved to ProductPrice per analysis)
- Actually pricing goes to **ProductPrice** entity, not here

**Methods**:
- `UpdatePurchaseCost()` — keep
- `UpdateSalesPrice()` — **REMOVE** (pricing is now in ProductPrice)
- `GetPriceByUnit()` — **REMOVE** (use ProductPrice)
- `AddBarcode()` — keep
- `CalculateCostFromBaseUnitCost()` — keep
- `ToBaseUnitQuantity()` — keep

### 2.3 UnitBarcode Entity ⚠️ (Deferred)

**File**: `Domain/Entities/UnitBarcode.cs`

Per Analysis Part 3 final revisions: separate unit barcodes are NOT supported in V1. The barcode belongs to the **product** and there is only ONE main barcode per product.

**Decision**: **DEFER** — hide from UI, do not use in V1.

### 2.4 ProductBarcode Entity ⚠️ (Legacy — Deprecate)

**File**: `Domain/Entities/ProductBarcode.cs` (36 lines)

Per Analysis: one barcode per product, so a separate table `ProductBarcode` is redundant.

**Decision**: **DEPRECATE** — hide from UI, keep in DB for backwards compat. Add `Barcode` string directly to `Product` entity.

### 2.5 Category Entity ✅ (Exists — needs seed)

**File**: `Domain/Entities/Category.cs` (37 lines)

| Field | Type | Status | Action |
|-------|------|--------|--------|
| `Id` | `int PK` | ✅ | Keep |
| `Name` | `string(100)` | ✅ | Keep — Unique Index |
| `Description` | `string(250)?` | ✅ | Keep |
| `IsActive` | `bool` | ✅ | Keep (from BaseEntity) |

**Missing**: Default seed "عام" category. No seed data exists for categories.

### 2.6 ProductPriceHistory Entity ✅ (Exists)

**File**: `Domain/Entities/ProductPriceHistory.cs` (91 lines)

| Field | Type | Status |
|-------|------|--------|
| `ProductUnitId` | `int FK` | ✅ |
| `ChangeType` | `string(50)` | ✅ |
| `OldValue` / `NewValue` | `decimal(18,2)` | ✅ |
| `OldRetailPrice` / `NewRetailPrice` | `decimal(18,2)` | ✅ |
| `OldWholesalePrice` / `NewWholesalePrice` | `decimal(18,2)` | ✅ |
| `OldAvgCost` / `NewAvgCost` | `decimal(18,2)` | ✅ |
| `ChangeReason` | `string(500)` | ✅ |
| `ChangedByUserId` | `int` | ✅ |
| `CostingMethod` | `string(50)?` | ✅ |

**Enhancement needed**: Add `CurrencyId` field to support multi-currency price history tracking. (`PriceLevel` is NOT in V1 — pricing is per ProductUnit × CurrencyId only.)

### 2.7 Existing Services

| Service | File | Status |
|---------|------|--------|
| `ProductService` | `Application/Services/ProductService.cs` (375 lines) | ✅ Exists — needs extension |
| `ProductUnitService` | `Application/Services/ProductUnitService.cs` | ✅ Exists |
| `UpdateProductPricingService` | `Application/Services/UpdateProductPricingService.cs` (148 lines) | ✅ Exists — needs FIFO option |
| `ProductPriceService` | `Application/Services/ProductPriceService.cs` | ✅ Exists |
| CategoryService | Not extracted separately | ❌ Missing — needs standalone service |

### 2.8 Existing API

| Controller | Endpoints | Status |
|------------|-----------|--------|
| `ProductsController` | CRUD + search + bulk | ✅ Exists — needs FIFO/assembly endpoints |
| `ProductUnitsController` | CRUD | ✅ Exists |

### 2.9 Existing Desktop Files

| Component | Files | Status |
|-----------|-------|--------|
| List ViewModel | `ProductListViewModel.cs` | ✅ Exists |
| Editor ViewModel | `ProductEditorViewModel.cs` | ✅ Exists — needs massive update |
| List View | `ProductsListView.xaml` + `.cs` | ✅ Exists |
| Editor View | `ProductEditorView.xaml` + `.cs` | ✅ Exists |
| Unit List VM | `ProductUnitsListViewModel.cs` | ✅ Exists |
| Unit Editor VM | `ProductUnitEditorViewModel.cs` | ✅ Exists |
| Unit Builder VM | `ProductUnitBuilderViewModel.cs` | ✅ Exists |
| Unit Row VM | `ProductUnitRowViewModel.cs` | ✅ Exists |
| Selection VM | `ProductSelectionViewModel.cs` | ✅ Exists |
| Selection View | `ProductSelectionView.xaml` + `.cs` | ✅ Exists |
| Product API Service | `ProductApiService.cs` | ✅ Exists |
| ProductUnit API Service | `ProductUnitApiService.cs` | ✅ Exists |

### 2.10 Missing Components

| Component | Status |
|-----------|--------|
| `InventoryBatch` entity + config | ❌ NOT EXIST — must create |
| `InventoryBatchAllocation` entity | ❌ NOT EXIST — must create |
| `BillOfMaterials` entity + config | ❌ NOT EXIST — must create |
| `BillOfMaterialsItem` entity | ❌ NOT EXIST — must create |
| `ProductImage` entity + config | ❌ NOT EXIST — must create |
| `ProductPrice` entity + config | ❌ NOT EXIST — must create (pricing per currency) |
| `IFifoAllocationService` + implementation | ❌ NOT EXIST — must create |
| `IMinStockAlertService` | ❌ NOT EXIST — must create |
| `IProductImageService` | ❌ NOT EXIST — must create |
| `CategoryService` + `CategoriesController` | ❌ NOT EXIST — must create |
| Category list/editor ViewModels + Views | ❌ NOT EXIST — must create |
| Default seed data (category "عام", units) | ❌ NOT EXIST — must seed |

---

## 3. BLOCKER Resolution — Critical Fixes

These 4 issues are **blocking** — they must be resolved before any Phase 25 implementation begins.

### 3.1 Blocker 1: FIFO vs WeightedAverage — Architectural Decision

**Problem**: The current system exclusively uses **WeightedAverage** costing (RULE-068/069). Analysis Part 3 explicitly requires **FIFO** with purchase batches (دفعات), lot numbers, and expiry tracking. The entire stock costing and allocation flow must be redesigned.

**Current flow** (WeightedAverage):
```text
Purchase → UpdateProductPricingService → WeightedAverage formula
         → Update ProductUnit.PurchaseCost
         → No batch tracking
```

**Required flow** (FIFO):
```text
Purchase → Create InventoryBatch (LotNumber, Qty, UnitCost, ExpiryDate)
         → UpdateProductPricingService (optional — keep WeightedAverage as alternative)
         → InventoryMovement record
Sale → Deduct from InventoryBatches (earliest lot first)
     → If HasExpiry: FEFO (earliest expiry first)
     → InventoryMovement record
```

**Decision Options**:

| Option | Description | Complexity | Migration |
|--------|-------------|------------|-----------|
| **A: FIFO + WeightedAverage** | Add FIFO as CostingMethod = 4, keep WA as default | **HIGH** — ~600 new lines + new tables | Requires new InventoryBatch table, no backfill of existing stock |
| **B: FIFO Only** | Replace WA entirely with FIFO | **HIGH** — requires rewriting UpdateProductPricingService | Breaks existing stock cost data |
| **C: Defer FIFO to V2** | Keep WA only, skip batch tracking | **LOW** — no changes | Analysis explicitly requires FIFO — cannot defer |

**⚠️ RECOMMENDATION**: **Option A** — Add FIFO as costing method option (4). Keep WeightedAverage as default. This satisfies analysis requirements while maintaining backwards compatibility. Users choose their costing method in Settings.

**RULE-068/069 amendment required**:
- Add `FIFO = 4` to `CostingMethod` enum
- Add `EnableFefo` SystemSetting (already planned in Phase 19)
- Update `UpdateProductPricingService` to handle FIFO method

### 3.2 Blocker 2: Pricing Model — Per-Unit + Per-Currency Only

**Problem**: Current pricing stores `SalesPrice` and `WholesalePrice` directly on `ProductUnit`. Analysis requires a simplified but more powerful model:
1. Price = f(ProductUnit, Currency) — per-currency pricing per unit.
2. NO MORE Retail/Wholesale/VIP price levels. The unit itself determines the price (e.g., Piece price, Carton price).
3. Price history per change.

**Current**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). Legacy SalesPrice/WholesalePrice on ProductUnit are deprecated — use `ProductPrices` table instead.

**Required**:
> See `docs/AGENTS.md` for domain entity patterns and `docs/database-schema.md` for table definitions. Note: `PriceLevel` is NOT in V1 — pricing is per (ProductUnit × CurrencyId) only.

**This BLOCKER depends on Phase 20 (Currencies)** — `CurrencyId` FK cannot exist without the Currencies module. Options:

| Option | Description |
|--------|-------------|
| **A: Wait for Phase 20** | Build pricing without CurrencyId, add it in Phase 20 |
| **B: Build now with CurrencyId placeholder** | Add CurrencyId FK now (requires Currency table to exist or be seeded) |

**⚠️ RECOMMENDATION**: **Option A** — Build the `ProductPrice` entity NOW with CurrencyId as `int NOT NULL`, and ensure the Currencies module (Phase 20) seeds at least a "Base Currency" (e.g., YER or SAR) so the FK constraint is satisfied. This avoids a later migration.

### 3.3 Blocker 3: InventoryBatch Entity Doesn't Exist

**Problem**: FIFO tracking requires a `InventoryBatch` entity that tracks:
- ProductId + WarehouseId
- Quantity (remaining)
- UnitCost (in base currency)
- LotNumber (auto-generated or user-provided)
- ExpiryDate (optional — for FEFO)
- ReceivedDate
- SourceInvoiceId

**No InventoryBatch entity, no InventoryBatchConfiguration, no migration, no service layer exists.**

**Resolution**: This is a full new entity build within Phase 25 — see [Section 4 — Products Design Catalog](#4-products-design-catalog).

### 3.4 Blocker 4: Default Category "عام" & Default Units Not Seeded

**Problem**: Analysis Part 2 explicitly requires:
- Default category "عام" (General) — for products without specific category
- Default units: حبة (Piece), كرتون (Carton), علبة (Box), كيلو (KG), جرام (Gram), لتر (Liter), متر (Meter)

These must be system-wide seeds, not per-user data. No seed data exists for either.

**Resolution**: Add seed data in `DbSeeder.cs` with independent `AnyAsync()` guards.

---

## 4. Products Design Catalog

### 4.1 Product Entity — Extended

| # | Field | Type | Default | Required | Constraints | Status |
|---|-------|------|---------|----------|-------------|--------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — | ✅ Exists |
| 2 | `Name` | `nvarchar(150)` | — | ✅ | — | ✅ Exists |
| 3 | `Barcode` | `nvarchar(50)` | `null` | ❌ | **UNIQUE** | **NEW** (Moved from external tables) |
| 4 | `CategoryId` | `int FK` | `1` (عام) | ❌ | FK→Categories, Restrict | ✅ Exists |
| 5 | `MinStockLevel` | `decimal(18,3)` | `0` | ❌ | `CHECK >= 0` | ✏️ Rename from MinStock |
| 6 | `ReorderLevel` | `decimal(18,3)` | `0` | ❌ | `CHECK >= 0` | ✅ Exists |
| 7 | `Description` | `nvarchar(500)` | `null` | ❌ | — | ✅ Exists |
| 8 | `AvgCost` | `decimal(18,2)` | `0` | ❌ | Computed from batches | **NEW** |
| 9 | `HasExpiry` | `bit` | `false` | ✅ | If true → FEFO | **NEW** |
| 10 | `TrackBatches` | `bit` | `true` | ✅ | Always true | **NEW** |
| 11 | `IsActive` | `bit` | `true` | ❌ | Global QF | ✅ In BaseEntity |

**Deprecated fields** (keep in DB, hide from UI, remove in future migration):
- `UnitId` (Legacy FK to Units table)
- `WholesaleUnitId` (Legacy FK)
- `RetailUnitId` (Legacy FK)
- `ConversionFactor` (Legacy decimal)
- `PurchasePrice` (Legacy — cost from batches now)
- `SalePrice` (Legacy — use ProductPrice)
- `WholesalePrice` (Legacy — use ProductPrice)
- `RetailPrice` (Legacy — use ProductPrice)
- `ExpirationDate` (Legacy — per-batch expiry)
- `ImagePath` (Legacy — use ProductImage entity)

### 4.2 ProductUnit Entity — Enhanced

| # | Field | Type | Default | Required | Status |
|---|-------|------|---------|----------|--------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | ✅ Exists |
| 2 | `ProductId` | `int FK` | — | ✅ | ✅ Exists |
| 3 | `UnitName` | `nvarchar(100)` | — | ✅ | ✅ Exists |
| 4 | `BaseConversionFactor` | `decimal(18,3)` | `1` | ✅ | ✅ Exists |
| 5 | `IsBaseUnit` | `bit` | `false` | ✅ | ✅ Exists |
| 6 | `PurchaseCost` | `decimal(18,2)` | `0` | ❌ | ✅ Exists (computed) |
| 7 | `SupplierPrice` | `decimal(18,2)` | `0` | ❌ | ✅ Exists |
| 8 | `LastPurchasePrice` | `decimal(18,2)` | `0` | ❌ | ✅ Exists |
| 9 | `SortOrder` | `int` | `1` | ❌ | ✅ Exists |

**Dropdown unit options** (not stored, but suggested in UI):
- حبة (Piece) — base unit
- كرتون (Carton) — factor > 1
- علبة (Box) — factor > 1
- كيلو (KG) — base unit for weight products
- جرام (Gram) — factor < 1 (1 KG = 1000 grams)
- لتر (Liter) — base unit for liquids
- متر (Meter) — base unit for length

**Deprecated fields** (move to ProductPrice):
- `SalesPrice` → use ProductPrice (per ProductUnit × CurrencyId — no PriceLevel in V1)
- `WholesalePrice` → use ProductPrice (per ProductUnit × CurrencyId — no PriceLevel in V1)

### 4.3 ProductPrice Entity — NEW

Stores price per unit + currency + price level combination.

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `ProductUnitId` | `int FK` | — | ✅ | FK→ProductUnits, Restrict |
| 3 | `CurrencyId` | `int FK` | — | ✅ | FK→Currencies, Restrict |
| 4 | `Price` | `decimal(18,2)` | `0` | ✅ | `CHECK >= 0` |
| 5 | `EffectiveFrom` | `datetime2` | `GETUTCDATE()` | ✅ | — |
| 6 | `EffectiveTo` | `datetime2?` | `null` | ❌ | Null = currently active |
| 7 | `IsActive` | `bit` | `true` | ❌ | Global QF |

**Unique Index**: `(ProductUnitId, CurrencyId)` — one active price per combination.

**Seed data**: Each product's base unit gets a default price entry for the base currency (Phase 20 dependency).

### 4.4 InventoryBatch Entity — NEW (FIFO Foundation)

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `ProductId` | `int FK` | — | ✅ | FK→Products, Restrict |
| 3 | `ProductUnitId` | `int FK` | — | ✅ | FK→ProductUnits, Restrict |
| 4 | `WarehouseId` | `int FK` | — | ✅ | FK→Warehouses, Restrict |
| 5 | `LotNumber` | `nvarchar(50)` | Auto-generated | ✅ | — |
| 6 | `Quantity` | `decimal(18,3)` | — | ✅ | `CHECK >= 0` (remaining) |
| 7 | `OriginalQuantity` | `decimal(18,3)` | — | ✅ | `CHECK > 0` |
| 8 | `UnitCost` | `decimal(18,2)` | — | ✅ | In base currency |
| 9 | `ExpiryDate` | `datetime2?` | `null` | ❌ | For FEFO |
| 10 | `ReceivedDate` | `datetime2` | `GETUTCDATE()` | ✅ | — |
| 11 | `PurchaseInvoiceId` | `int FK?` | `null` | ❌ | FK→PurchaseInvoices |
| 12 | `IsOpeningBatch` | `bit` | `false` | ❌ | True for opening stock |
| 13 | `IsActive` | `bit` | `true` | ❌ | Global QF |

**Key behaviors**:
- On purchase: create InventoryBatch with `Qty = OriginalQuantity`, `Quantity = OriginalQuantity`
- On sale: deduct from batches (FIFO or FEFO), reduce `Quantity`, create `InventoryMovement` records
- When `Quantity = 0`: lot is fully consumed (keep for history, mark inactive)
- `LotNumber` auto-format: `B-{YYYYMMDD}-{Sequence:000}` (e.g., `B-20260605-001`)

### 4.5 InventoryBatchAllocation Entity — NEW

Tracks which sales invoice items consumed from which purchase lot.

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `InventoryBatchId` | `int FK` | — | ✅ | FK→InventoryBatches, Restrict |
| 3 | `SalesInvoiceItemId` | `int FK?` | `null` | ❌ | FK→SalesInvoiceItems |
| 4 | `SalesReturnItemId` | `int FK?` | `null` | ❌ | FK→SalesReturnItems |
| 5 | `Quantity` | `decimal(18,3)` | — | ✅ | `CHECK > 0` |
| 6 | `UnitCost` | `decimal(18,2)` | — | ✅ | Cost at time of allocation |
| 7 | `CreatedAt` | `datetime2` | `GETUTCDATE()` | ✅ | — |

### 4.6 BillOfMaterials Entity — NEW (Assembly Products)

For products made from other products (بضائع تامة — assemblies).

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `AssemblyProductId` | `int FK` | — | ✅ | FK→Products (the finished product) |
| 3 | `ComponentProductId` | `int FK` | — | ✅ | FK→Products (the raw material) |
| 4 | `ComponentUnitId` | `int FK` | — | ✅ | FK→ProductUnits |
| 5 | `QuantityRequired` | `decimal(18,3)` | — | ✅ | `CHECK > 0` |
| 6 | `WastePercentage` | `decimal(18,2)` | `0` | ❌ | `CHECK >= 0` |
| 7 | `IsActive` | `bit` | `true` | ❌ | Global QF |

**Unique Index**: `(AssemblyProductId, ComponentProductId)` — one entry per component per assembly.

**Behavior**: When creating/editing an assembly product, the BOM defines components. On "produce" operation, components are deducted from stock and the assembly product is added.

### 4.7 ProductImage Entity — NEW

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `ProductId` | `int FK` | — | ✅ | FK→Products, Restrict |
| 3 | `ImagePath` | `nvarchar(500)` | — | ✅ | Relative path or URL |
| 4 | `IsPrimary` | `bit` | `false` | ❌ | One primary image per product |
| 5 | `SortOrder` | `int` | `0` | ❌ | Display order |
| 6 | `IsActive` | `bit` | `true` | ❌ | Global QF |

**Storage**: Images stored on disk (App_Data/ProductImages/), path saved in DB.

> See `Domain/Enums/` for enum definitions and `docs/AGENTS.md` §3 for canonical enum values.

### 4.9 ProductPriceHistory Entity — Enhanced

Add these fields to support multi-currency pricing history + price validity periods:

| # | Field | Type | Required | Status |
|---|-------|------|----------|--------|
| `CurrencyId` | `int` FK? | ❌ | **NEW** |
| `OldPrice` | `decimal(18,2)` | ❌ | **NEW** (to complement OldAvgCost) |
| `NewPrice` | `decimal(18,2)` | ❌ | **NEW** |
| `FromDate` | `datetime2` | ✅ | **NEW** — When this price becomes effective |
| `ToDate` | `datetime2?` | ❌ | **NEW** — Optional: when price expires |
| Existing fields | — | — | ✅ Keep |

> See `docs/AGENTS.md` for domain entity patterns.

**UI display rules**:
- Current price clearly marked (green highlight)
- Historical prices shown in a list with FromDate/ToDate
- Expired prices shown in red

---

## 5. Gap Analysis

### 5.1 Product Entity

| Field | Status | Action |
|-------|--------|--------|
| `AvgCost` | ❌ Missing | Add computed field, update from batches |
| `HasExpiry` | ❌ Missing | Add per-product expiry flag |
| `TrackBatches` | ❌ Missing | Add (always true per analysis) |
| 6 legacy fields (`UnitId`, `WholesaleUnitId`, `RetailUnitId`, `ConversionFactor`, `PurchasePrice`, `SalePrice`, `WholesalePrice`, `RetailPrice`, `ExpirationDate`, `ImagePath`) | ⚠️ Legacy | Deprecate — keep in DB, hide from UI |
| `MinStock` → `MinStockLevel` | ✏️ Rename | Rename property + config |

### 5.2 ProductUnit Entity

| Field | Status | Action |
|-------|--------|--------|
| `SalesPrice` | ⚠️ Deprecate | Move to ProductPrice.Price |
| `WholesalePrice` | ⚠️ Deprecate | Move to ProductPrice.Price (if appropriate unit exists, else drop) |

### 5.3 Pricing

| Component | Status | Action |
|-----------|--------|--------|
| Per-currency pricing | ❌ Missing | Create ProductPrice entity |
| ProductPrice entity + config | ❌ Missing | Full build |
| Price history for per-currency changes | ❌ Missing | Extend ProductPriceHistory with CurrencyId |

### 5.4 Batches / FIFO

| Component | Status | Action |
|-----------|--------|--------|
| InventoryBatch entity | ❌ Missing | Full build — core of FIFO |
| InventoryBatchAllocation entity | ❌ Missing | Track sale→lot consumption |
| FIFO allocation service | ❌ Missing | Create IFifoAllocationService |
| FEFO logic | ❌ Missing | Extend allocation for expiry-based sorting |
| CostingMethod.FIFO = 4 | ❌ Missing | Add to enum + service |

### 5.5 Assembly / BOM

| Component | Status | Action |
|-----------|--------|--------|
| BillOfMaterials entity | ❌ Missing | Full build |
| BillOfMaterialsItem integrated | ❌ Missing | Same entity handles components |
| Assembly production logic | ❌ Missing | Service method to produce assemblies |

### 5.6 Product Images

| Component | Status | Action |
|-----------|--------|--------|
| ProductImage entity | ❌ Missing | Full build |
| Image upload service | ❌ Missing | File storage + path management |
| Multiple images per product | ❌ Missing | Gallery support |

### 5.7 Categories

| Component | Status | Action |
|-----------|--------|--------|
| Seed "عام" category | ❌ Missing | Add to DbSeeder |
| CategoryService | ❌ Missing | Create if not existing |
| Category list/editor screens | ❌ Missing | Create Desktop screens |
| Default category assignment | ❌ Missing | Set CategoryId default = 1 |

### 5.8 Min Stock Alerts

| Component | Status | Action |
|-----------|--------|--------|
| Background service | ❌ Missing | Create as BackgroundService |
| Alert notification | ❌ Missing | Desktop notification via EventBus |
| Dashboard integration | ❌ Missing | Show count on Dashboard |

### 5.9 Seed Data

| Seed | Status | Action |
|------|--------|--------|
| Category "عام" | ❌ Missing | Add to DbSeeder with AnyAsync guard |
| 7 default units | ❌ Missing | Add unit templates (not stored per-product) |
| Default costing method | ✅ Exists | Already seeded as WeightedAverage=1 |

---

## 6. Architectural Decisions

### 6.1 FIFO Implementation Strategy

**Decision**: Add FIFO as `CostingMethod.FIFO = 4` alongside existing WeightedAverage, LastPurchasePrice, and SupplierPrice. The user selects the costing method in Settings.

**Implementation approach**:

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**FIFO Allocation Algorithm**:
```
Input: ProductId, WarehouseId, QuantityNeeded
1. Query InventoryBatches WHERE ProductId AND WarehouseId AND Quantity > 0
2. ORDER BY (useFefo ? ExpiryDate ASC : ReceivedDate ASC, Id ASC)
3. For each lot:
   - TakeQuantity = Min(QuantityNeeded, lot.Quantity)
   - Create InventoryBatchAllocation record
   - lot.Quantity -= TakeQuantity
   - QuantityNeeded -= TakeQuantity
   - If QuantityNeeded == 0: break
4. If QuantityNeeded > 0: return error "Insufficient stock"
```

**Costing integration**: When `CostingMethod = FIFO`, `UpdateProductPricingService` computes cost as the weighted average of all active InventoryBatches (for display). The actual COGS for each sale is the specific lot's UnitCost.

### 6.2 Pricing Model — Per-Unit + Per-Currency ONLY

**Decision**: Create `ProductPrice` entity as the single source of truth for all pricing.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Price history**: Every create/update/delete of `ProductPrice` records a `ProductPriceHistory` entry with CurrencyId.

### 6.3 Batch Allocation Algorithm — FIFO vs FEFO

**Decision**: Always use FIFO. Use FEFO only when the product has `HasExpiry = true`.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns. Note: `InventoryBatches` is named `InventoryBatches` in the final schema — see `docs/database-schema.md`.

### 6.4 Assembly/BOM Structure

**Decision**: Simple flat BOM (single-level, no nested assemblies in V1). One `BillOfMaterials` table with `AssemblyProductId` + `ComponentProductId` pairs.

**Production flow**:
```
1. User selects assembly product + quantity
2. System checks: do we have all components in stock?
3. System deducts components from stock (FIFO)
4. System adds assembly product to stock (with calculated cost = sum of component costs)
5. ProductPriceHistory records for all affected products
```

### 6.5 Product Validation: Minimum 2 Units

**Decision**: Enforce that every product must have at least:
- 1 base unit (e.g., حبة) — IsBaseUnit = true, ConversionFactor = 1
- 1 derived unit (e.g., كرتون) — ConversionFactor > 1

This is enforced in `Product.ValidateUnits()` and checked at save time.

### 6.6 Barcode Strategy — One Barcode Per Product

**Decision**: Per final revisions in Analysis Part 3, the barcode belongs to the **product**, not the unit. There is exactly ONE main barcode per product in V1. When scanning the barcode, the POS selects the product, and the user selects the specific unit from a dropdown.

**Implementation**: 
- Add `public string? Barcode { get; private set; }` to the `Product` entity.
- Deprecate `ProductBarcode` and `UnitBarcode` entities.
- Ensure the `Barcode` column has a unique index in the database.

### 6.7 Min Stock Alert — Background Service

**Decision**: Implement as a `BackgroundService` that runs periodically (configurable interval, default 6 hours) and checks all products where `WarehouseStock.Quantity <= Product.MinStockLevel`.

**Alert delivery**: Via `EventBus` → `LowStockAlertMessage` → Desktop notification through `IToastNotificationService`.

> See `docs/AGENTS.md` for BackgroundService patterns and `docs/AGENTS.md` §2.15 for Serilog logging conventions.

### 6.8 Product Code — Design Decision (RULE-191 vs Analysis)

> **⚠️ Design Decision — Product Code (RULE-191 vs Analysis)**
>
> Analysis Part 2:148-161 explicitly shows Product table with a `Code` column (e.g., PRD-1001).
> However, AGENTS.md RULE-191 states: "Product, Customer, Supplier, and Warehouse MUST NOT have a Code column — use auto-increment Id (int PK) as the sole identifier."
>
> **Decision**: Follow RULE-191 — no Code column. Users identify products by:
> - Auto-increment Id (shown as #1001)
> - Product Name
> - Barcode (unique per product-unit)
>
> **Rationale**:
> - Code adds complexity with no business value
> - Barcode already serves as the unique product identifier for scanning
> - Id provides a simple numeric reference
> - Simplifies search (search by Name or Barcode only)

---

## 7. Non-V1 Items (Deferred)

These features appeared in Analysis Parts 1–5 but are deferred to future versions:

| Feature | Reason |
|---------|--------|
| **Product Variants** (Size/Color/SKU combinations) | Requires variant engine — deferred to V2 |
| **Supplier Catalog Sync** (auto-update prices from supplier) | Integration feature — V2 |
| **Bulk Price Update from Excel** | Excel import feature — separate phase |
| **Barcode Printing** (label generation + print) | Requires label designer — V2 |
| **Serial Number Tracking** (per-unit serial tracking) | Requires SerialNumber entity + tracking engine — V2 |
| **Composite Products** (multi-level BOM) | Nested assemblies add complexity — V2 |
| **Warehouse Transfer Orders** (between warehouses) | Already planned as separate module |
| **Physical Inventory (Stocktake)** | Separate module — V2 |
| **Purchase Request** (internal request before purchase) | Analysis says defer to V2 |
| **Sales Quotation** (offer before invoice) | Analysis says defer to V2 |
| **EAN-13 Check Digit** for barcodes | Can be added later without schema change |
| **Product Groups** (customer-tier groups for pricing) | Deferred — per-customer pricing is V2 |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 0 — Blocker Resolution: FIFO vs WeightedAverage Confirmation

**Before any code is written**, the user must confirm:

- **Option A**: Add FIFO (CostingMethod=4) alongside WeightedAverage ✅ (RECOMMENDED)
- **Option B**: FIFO only, replace WeightedAverage
- **Option C**: Defer FIFO to V2, keep WA

**If Option A** (default recommendation):
1. Add `FIFO = 4` to `CostingMethod` enum in `Domain/Enums/`
2. Update `SystemSettingsRepository` to accept value `4`
3. Update `UpdateProductPricingService` to handle FIFO method
4. Add `EnableFefo` to seeded SystemSettings

**Estimate**: ~30 minutes (configuration only, actual FIFO logic in Task 3-4)

---

### Task 1 — Seed Default Category "عام" + Default Units Templates

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Add independent `AnyAsync()` guard for Categories → seed "عام" |
| `Infrastructure/Data/DbSeeder.cs` | Add unit templates (not stored as rows, used for UI suggestions) |

**Seed block**:
> See `Infrastructure/Data/Seeders/DbSeeder.cs` for existing seed patterns.

**Default unit templates** (stored as `SystemSetting` or constant list):
| Unit Name (Arabic) | Type | Conversion Factor |
|--------------------|------|-----------------|
| حبة | Base | 1 |
| كرتون | Derived | Configurable |
| علبة | Derived | Configurable |
| كيلو | Base (weight) | 1 |
| جرام | Derived (weight) | 0.001 |
| لتر | Base (liquid) | 1 |
| متر | Base (length) | 1 |

These are NOT stored as database rows — they are displayed as suggestions in the `ProductUnitBuilderViewModel` dropdown. The user selects from these when creating units.

**Estimate**: ~30 minutes

---

### Task 2 — Create InventoryBatch Entity + Configuration + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/InventoryBatch.cs` | NEW — full entity with guard clauses |
| `Domain/Entities/InventoryBatchAllocation.cs` | NEW — allocation tracking |
| `Infrastructure/Data/Configurations/InventoryBatchConfiguration.cs` | NEW — Fluent API config |
| `Infrastructure/Data/Configurations/InventoryBatchAllocationConfiguration.cs` | NEW — Fluent API config |
| `Infrastructure/Data/Migrations/` | NEW migration: 2 new tables |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the canonical `InventoryBatches` table definition. Note: Named `InventoryBatches` in the final schema (not `InventoryBatches`).

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions (all FKs use `DeleteBehavior.Restrict`, explicit precision on decimals, CHECK constraints).

> See `docs/database-schema.md` for the canonical DDL definitions. Note: Named `InventoryBatches` in the final schema.

**Estimate**: ~3 hours

---

### Task 3 — Create FIFO Allocation Service (IFifoAllocationService)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IFifoAllocationService.cs` | NEW — interface with 5 methods |
| `Application/Services/FifoAllocationService.cs` | NEW — implementation |
| `Contracts/Common/AllDtos.cs` | Add `BatchStockInfoDto`, `InventoryBatchDto` |

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Key implementation details**:
- `AddPurchaseBatchesAsync`: Converts quantity to base unit first → creates InventoryBatch
- `DeductFromBatchesAsync`: FEFO if product.HasExpiry, otherwise FIFO
- Batch operations inside transactions (RULE-003)
- Every allocation = `InventoryMovement` record (RULE-029)

**Logging** (RULE-035):
- `Log.Information("InventoryBatch {LotNumber} created: Product {ProductId}, Qty {Qty}, Cost {Cost}", ...)`
- `Log.Information("FIFO allocation: {Qty} units from Lot {LotId} to InvoiceItem {ItemId}", ...)`

**Estimate**: ~4 hours

---

### Task 4 — Update UpdateProductPricingService for FIFO

**Files**:

| File | Change |
|------|--------|
| `Application/Services/UpdateProductPricingService.cs` | Add FIFO costing method handler |
| `Domain/Enums/CostingMethod.cs` | Add `FIFO = 4` |

> See `UpdateProductPricingService` in `Application/Services/` for the existing WA/LPP/SP logic patterns. FIFO handler calls `IFifoAllocationService.AddPurchaseBatchesAsync()` then records cost change via `ProductPriceHistory`.

**Note**: When FIFO is selected:
- `AvgCost` on Product = calculated from active InventoryBatches (weighted average of remaining quantities)
- Actual sale COGS = specific lot's UnitCost (not computed average)

**Estimate**: ~2 hours

---

### Task 5 — Create ProductPrice Entity + Pricing Service

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/ProductPrice.cs` | NEW — per-unit, per-currency, per-level pricing |
| `Domain/Enums/PriceLevel.cs` | 🔴 NOT in V1 — pricing is per (ProductUnit × CurrencyId) only; no Retail/Wholesale levels |
| `Infrastructure/Data/Configurations/ProductPriceConfiguration.cs` | NEW — Fluent API config |
| `Application/Interfaces/Services/IProductPriceService.cs` | NEW — interface |
| `Application/Services/ProductPriceService.cs` | NEW — implementation with price lookup + history |
| `Infrastructure/Data/Migrations/` | NEW migration |
| `Contracts/DTOs/AllDtos.cs` | Add `ProductPriceDto` |
| `Contracts/Requests/ProductRequests.cs` | Add `CreateProductPriceRequest`, `UpdateProductPriceRequest` |

> See `IProductPriceService` in `Application/Interfaces/Services/` for the full interface definition.

Task<Result<ProductPriceDto>> UpdatePriceAsync(
    int id, decimal newPrice, int changedByUserId, CancellationToken ct);

Task<Result<List<ProductPriceDto>>> GetPricesForUnitAsync(
    int productUnitId, CancellationToken ct);

Task<Result<decimal>> GetEffectivePriceAsync(
    int productUnitId, int currencyId, CancellationToken ct);

Task<Result> DeletePriceAsync(int id, CancellationToken ct);
```

**GetEffectivePriceAsync algorithm** (fallback chain):
1. Exact match: ProductUnitId + CurrencyId
2. Fallback to base currency price (converted via exchange rate)
3. Return 0 if nothing found (user enters manually)

**Estimate**: ~3 hours

---

### Task 6 — Create ProductImage Entity + Upload Service

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/ProductImage.cs` | NEW — entity with ImagePath, IsPrimary, SortOrder |
| `Infrastructure/Data/Configurations/ProductImageConfiguration.cs` | NEW — Fluent API config |
| `Application/Interfaces/Services/IProductImageService.cs` | NEW — interface |
| `Application/Services/ProductImageService.cs` | NEW — file upload + CRUD |
| `Infrastructure/Data/Migrations/` | NEW migration |
| `Contracts/DTOs/AllDtos.cs` | Add `ProductImageDto` |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

**Image storage**: Files saved to `%AppData%/SalesSystem/ProductImages/{ProductId}/` with GUID-based filenames.

**Estimate**: ~2 hours

---

### Task 7 — Create BillOfMaterials Entity + Config

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/BillOfMaterials.cs` | NEW — AssemblyProductId + ComponentProductId pairs |
| `Infrastructure/Data/Configurations/BillOfMaterialsConfiguration.cs` | NEW — Fluent API config |
| `Application/Interfaces/Services/IAssemblyService.cs` | NEW — interface |
| `Application/Services/AssemblyService.cs` | NEW — BOM CRUD + production logic |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

**AssemblyService.ProduceAsync**: Transactional — deduct components, add assembly product, record history.

**Estimate**: ~3 hours

---

### Task 8 — Add CostingMethod.FIFO = 4 + EnableFefo Seed

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/CostingMethod.cs` | Add `FIFO = 4` |

> See `Domain/Enums/` for enum definitions and `docs/AGENTS.md` §3 for canonical enum values.

**Also ensure**: `SystemSettingsRepository.GetCostingMethodAsync()` handles value 4 safely. `SettingsViewModel` RadioButton for FIFO option.

**EnableFefo SystemSetting**: Already planned in Phase 19 seed data (Task 5): `EnableFefo = "false"`.

**Estimate**: ~15 minutes

---

### Task 9 — Update Product Entity: Add AvgCost, HasExpiry, TrackBatches + Deprecate Legacy Fields

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Product.cs` | Add `AvgCost`, `HasExpiry`, `TrackBatches`; mark legacy fields as `[Obsolete]` |
| `Domain/Entities/Product.cs` | Update `Create()` + `Update()` signatures — remove legacy params |
| `Infrastructure/Data/Configurations/ProductConfiguration.cs` | Add new field configs; keep legacy columns in table |
| `Infrastructure/Data/Migrations/` | NEW migration: ADD columns |
| All services/DTOs that reference legacy fields | Update mappings |

> See `Domain/Entities/Product.cs` for the canonical Product.Create() signature and `docs/AGENTS.md` for domain factory method patterns.

**Estimate**: ~2 hours

---

### Task 10 — Update ProductUnit Entity + Deprecate Price Fields

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/ProductUnit.cs` | Mark `SalesPrice`, `WholesalePrice` as `[Obsolete]` |
| `ProductUnit.CreateBaseUnit()` | Remove `salesPrice` param |
| `ProductUnit.CreateDerivedUnit()` | Remove `salesPrice`, `wholesalePrice` params |
| `ProductUnit.UpdatePurchaseCost()` | Keep — cost still relevant |
| `ProductUnit.UpdateSalesPrice()` | **REMOVE** — pricing via ProductPriceService |
| `ProductUnit.GetPriceByUnit()` | **REMOVE** — use ProductPriceService |

**Estimate**: ~1 hour

---

### Task 11 — Update ProductEditorViewModel (Massive Update)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Products/ProductEditorViewModel.cs` | Complete rewrite — new fields, tabs, validation |

**New tab structure**:

| Tab | Content |
|-----|---------|
| 📋 **Basic Info** | Name, Category (default "عام"), Description, HasExpiry, TrackBatches |
| 📐 **Units** | ProductUnitBuilder (base unit + derived units with conversion factors) |
| 💰 **Pricing** | Per-unit, per-currency, per-price-level grid |
| 🖼️ **Images** | Product image gallery (upload, set primary, reorder) |
| 📦 **Batches** | InventoryBatch breakdown (read-only list) |
| 📈 **History** | ProductPriceHistory (read-only list) |

> See `docs/AGENTS.md` for ViewModelBase patterns (INotifyDataErrorInfo, ExecuteAsync, properties with SetProperty).

**INotifyDataErrorInfo validation** (RULE-228):
- Name required (max 150)
- At least 1 base unit + 1 derived unit required
- BaseConversionFactor > 1 for derived units
- MinStockLevel >= 0

> See `docs/AGENTS.md` §2.23 for Interactive Validation pattern (RULE-059) — buttons always enabled, Validate() on click with styled warning dialog.

**Estimate**: ~6 hours (largest single task)

---

### Task 11.1 — Add Opening Stock Section to Product Creation Flow

**Problem**: Analysis Part 3 (lines 1663-1695) requires that when creating a new product, users can enter an opening quantity to create the initial stock balance immediately. This must create a `InventoryBatch` with `IsOpeningBatch = true` and an `InventoryMovement` record.

**Design**:
- Add an "Opening Stock" expandable section to the Product Editor (Tab 1: Basic Info or a dedicated area)
- Fields: Warehouse (dropdown), Quantity (decimal(18,3)), Unit Cost (decimal(18,2)), Expiry Date (if HasExpiry)
- Optional: user can skip and add stock later
- On save: create `InventoryBatch` with `IsOpeningBatch = true` + `InventoryMovement`

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Products/ProductEditorViewModel.cs` | Add opening stock properties + save logic |
| `Views/Products/ProductEditorView.xaml` | Add "Opening Stock" expandable section |
| `Application/Services/ProductService.cs` | Add `CreateOpeningStockBatchAsync()` method |
| `Contracts/DTOs/AllDtos.cs` | Add `OpeningStockDto` for the UI binding |
| `Contracts/Requests/ProductRequests.cs` | Add optional `OpeningStock` to `CreateProductRequest` |

> See `docs/AGENTS.md` for ViewModelBase patterns and `docs/AGENTS.md` §2.23 for Interactive Validation.

> See `Contracts/DTOs/` for DTO/record patterns.

> See `Contracts/Requests/` for Request/record patterns.

> See `docs/CONSTITUTION.md` for the Result<T> pattern, `docs/AGENTS.md` for service layer patterns, and `docs/AGENTS.md` §2.15 for Serilog logging conventions.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

> See XAML patterns in existing views at `DesktopPWF/Views/` for control styling and layout conventions.

> See `docs/AGENTS.md` §2.23 for Interactive Validation pattern (RULE-059).

**Logging**:
- `Log.Information("Opening stock batch created for Product {ProductId}: Qty={Qty}, Cost={Cost}, Warehouse={WarehouseId}", ...)`
- `Log.Information("Opening stock skipped for Product {ProductId} — user opted to add stock later", ...)`

**Arabic ToolTips** (RULE-185-190):
- Expander header: `"توسيع قسم الرصيد الافتتاحي — إدخال الكمية الابتدائية للمنتج في المخزون"`
- Warehouse combo: `"اختر المستودع الذي سيتم إضافة الرصيد الافتتاحي إليه"`
- Quantity field: `"إدخال الكمية الافتتاحية — سيتم إنشاء دفعة مخزون أولية"`
- Unit Cost field: `"تكلفة شراء الوحدة — ستُستخدم لحساب متوسط التكلفة والقيمة الدفترية"`

**Estimate**: ~3 hours

---

### Task 12 — Update ProductEditorView.xaml (Compact Styles + ToolTips)

**Files**:

| File | Change |
|------|--------|
| `Views/Products/ProductEditorView.xaml` | Rewrite with tab control, compact styles |
| `Views/Products/ProductEditorView.xaml.cs` | Minimal changes |

> See XAML patterns in existing views at `DesktopPWF/Views/` for tab control, form styling, and compact layout conventions (RULE-262-274).

**Arabic ToolTips** (RULE-185-190):
- Save button: `" حفظ المنتج — سيتم إنشاء الوحدات والأسعار والباركود"`
- Cancel: `"إلغاء التعديل والعودة"`
- Add unit button: `"إضافة وحدة قياس جديدة — يجب إضافة وحدة أساسية ووحدة إضافية على الأقل"`
- Generate barcode: `"توليد باركود تلقائي — سيتم إنشاء باركود داخلي للوحدة"`
- Upload image: `"رفع صورة للمنتج — الصيغ المدعومة: JPG, PNG"`

**UI Compact** (RULE-262-274):
- Button heights: via style (28px) — no hardcoded `Height="36"`
- Padding: `10,4` via style — no hardcoded `Padding="16,0"`
- Header: `Padding="12,6"`, Footer: `Padding="12,8"`
- Field margins: `Margin="0,0,0,6"` between fields
- Tab headers: compact font

**Estimate**: ~5 hours

---

### Task 13 — Update ProductsListView: Balance/Cost/Profit Columns

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Products/ProductsListViewModel.cs` | Add cost/profit columns, enhanced search |
| `Views/Products/ProductsListView.xaml` | Add columns for AvgCost, Stock Qty, Profit Margin |

> See XAML patterns in `DesktopPWF/Views/` for DataGrid column styling conventions.

> See existing list ViewModel patterns in `DesktopPWF/ViewModels/` for search/filter implementation.

**Estimate**: ~2 hours

---

### Task 14 — Create MinStockAlert Background Service

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IMinStockAlertService.cs` | NEW — interface |
| `Application/Services/MinStockAlertService.cs` | NEW — query + alert |
| `DesktopPWF/Messaging/LowStockAlertMessage.cs` | NEW — EventBus message |

**Implementation**: See Section 6.7 for full pattern.

**Estimate**: ~2 hours

---

### Task 15 — FluentValidation Updates for All New Requests

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/ProductRequests/` | NEW validators for CreateProductRequest (updated), CreateProductPriceRequest |
| `Api/Validators/InventoryBatchValidators/` | NEW validators for batch operations |
| `Api/Validators/AssemblyValidators/` | NEW validators for BillOfMaterials requests |

> See existing validators in `SalesSystem.Api/Validators/` for FluentValidation patterns (RULE-044).

**Estimate**: ~1.5 hours

---

### Task 16 — API Endpoint Enhancements

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/ProductsController.cs` | Add batch breakdown, price history, image upload endpoints |
| `Api/Controllers/ProductUnitsController.cs` | Add pricing endpoints |
| NEW: `Api/Controllers/BatchesController.cs` | InventoryBatch CRUD + FIFO allocation |
| NEW: `Api/Controllers/AssembliesController.cs` | BOM CRUD + production |

**New endpoints**:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/v1/products/{id}/batches` | Get batch breakdown for a product |
| GET | `/api/v1/products/{id}/price-history` | Get price history for a product |
| POST | `/api/v1/products/{id}/images` | Upload product image |
| DELETE | `/api/v1/products/images/{imageId}` | Delete product image |
| GET | `/api/v1/product-units/{id}/prices` | Get all prices for a unit |
| POST | `/api/v1/product-unit-prices` | Set a price |
| PUT | `/api/v1/product-unit-prices/{id}` | Update a price |
| DELETE | `/api/v1/product-unit-prices/{id}` | Delete a price |
| GET | `/api/v1/batches` | List InventoryBatches with filters |
| GET | `/api/v1/batches/{id}` | Get batch detail |
| GET | `/api/v1/assemblies` | List BOMs |
| POST | `/api/v1/assemblies` | Create BOM entry |
| POST | `/api/v1/assemblies/produce` | Produce assembly (deduct components + add product) |

**Controller purity** (RULE-203): All controllers inject service interfaces only — NO `DbContext` or `IUnitOfWork` injection.

**Estimate**: ~3 hours

---

### Task 17 — Seed Data Updates

**Files**:

| File | Change |
|------|--------|
| `Infrastructure/Data/DbSeeder.cs` | Add category seed, unit templates |

Already covered in Task 1. This task ensures all seed blocks are properly guarded and logged.

**Estimate**: ~15 minutes (included in Task 1)

---

### Task 18 — Migration Plan (Sequential Migrations)

**Migration order** (to avoid FK dependency issues):

| Migration # | Description | Dependency |
|-------------|-------------|------------|
| 1 | `AddCategorySeed` | None — additive data seed only |
| 2 | `AddAvgCost_HasExpiry_TrackBatches_ToProducts` | None — additive columns |
| 3 | `CreateProductPrices` | Requires ProductUnits table + Currencies table (Phase 20) |
| 4 | `CreateProductImages` | Requires Products table |
| 5 | `CreateInventoryBatches` | Requires Products + Warehouses + ProductUnits tables |
| 6 | `CreateInventoryBatchAllocations` | Requires InventoryBatches table |
| 7 | `CreateBillOfMaterials` | Requires Products table |

**Data preservation**: All migrations are additive — no data loss. Legacy columns are kept in tables, not dropped.

**⚠️ Migration 3 (ProductPrices) depends on Phase 20 (Currencies)**:
- If Phase 20 is not yet complete: create a placeholder Currency with Id=1 (Base Currency) in a data seed
- Mark `CurrencyId` FK as optional initially (`int?`), change to required in Phase 20

> See EF Core migration rollback: `dotnet ef migrations remove` or manual SQL `ALTER TABLE DROP COLUMN` for the listed columns.

**Estimate**: Migration creation + testing: ~2 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | ProductPrice.Price, InventoryBatch.UnitCost, Product.AvgCost | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | InventoryBatch.Quantity, BillOfMaterials.QuantityRequired, Product.MinStockLevel | ✅ |
| **RULE-003** | Multi-table ops in transaction | FIFO allocation + stock deduction + InventoryMovement in single transaction | ✅ |
| **RULE-006** | ALL services return `Result<T>` | IFifoAllocationService, IProductPriceService, IAssemblyService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All new entities | ✅ |
| **RULE-016** | BaseEntity audit fields | All new entities inherit BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | All new services | ✅ |
| **RULE-029** | EVERY stock change = InventoryMovement | FIFO allocation + product image + assembly production | ✅ |
| **RULE-035** | Serilog for logging | All services | ✅ |
| **RULE-036** | Log critical operations | Batch creation, price changes, assembly production | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All new controllers | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | InventoryBatch.DeductQuantity(), InventoryBatch.AddReturnQuantity(), ProductPrice.SetPrice() | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | All new request validators | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Product, Category, ProductUnit | ✅ |
| **RULE-052** | Guard Clauses on all entities | InventoryBatch.Create, BillOfMaterials.Create, ProductImage.Create | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | ProductEditorViewModel — AddError/ClearErrors | ✅ |
| **RULE-059** | Save always enabled, validate on click | ProductEditorViewModel — no CanExecute blocking | ✅ |
| **RULE-060** | ProductUnit conversion factor | ProductUnit.BaseConversionFactor | ✅ |
| **RULE-061** | Base unit factor = 1 | ProductUnit.IsBaseUnit enforced (CHK constraint) | ✅ |
| **RULE-062** | Derived units factor > 1 | ProductUnit.CreateDerivedUnit guard | ✅ |
| **RULE-063** | UnitBarcode stores ALL barcodes | UnitBarcode entity — barcode per unit | ✅ |
| **RULE-064** | SmartUnitFormatter selects display unit | UI only — deferred | ⏳ |
| **RULE-065** | Pricing per ProductUnit (not Product) | ProductPrice entity with ProductUnitId | ✅ |
| **RULE-066** | Cost cascade: purchase cost → unit cost | UpdateProductPricingService with FIFO option | ✅ |
| **RULE-067** | ProductMustHaveAtLeastOneUnit | Product.RemoveUnit() guard | ✅ |
| **RULE-068** | CostingMethod default = WeightedAverage (1) | Seed unchanged | ✅ |
| **RULE-069** | 3 methods: WA=1, LPP=2, SP=3 | Extended: FIFO=4 | ✅ |
| **RULE-070** | Costing update fires AFTER purchase saved | Same pattern — after ID obtained | ✅ |
| **RULE-071** | WeightedAverage formula | Already implemented — unchanged | ✅ |
| **RULE-072** | LastPurchasePrice overwrite logic | Already implemented — unchanged | ✅ |
| **RULE-073** | SupplierPrice = catalog price | Already implemented — unchanged | ✅ |
| **RULE-074** | Cost cascade: ALL units updated | UpdateProductPricingService cascades | ✅ |
| **RULE-075** | UpdateProductPricingService handles all methods | Extended to handle FIFO | ✅ |
| **RULE-076** | ProductPriceHistory on EVERY cost change | All price/cost changes recorded | ✅ |
| **RULE-084** | ProductPriceHistory records EVERY price change | Extended with CurrencyId (PriceLevel NOT in V1) | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | ProductEditorViewModel, ProductsListViewModel | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Product editor opens via OpenScreen() | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ المنتج"`, `"بيانات غير مكتملة"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowValidationErrorsAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, file upload failures, FIFO errors | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, price not found, stock insufficient | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | All API services | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | ProductEditorView, ProductsListView | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إضافة وحدة قياس جديدة — يجب إضافة وحدة أساسية..." | ✅ |
| **RULE-187** | Action buttons explain consequences | "حفظ المنتج — سيتم إنشاء الوحدات والأسعار والباركود" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "إدارة المنتجات — إضافة وتعديل الأصناف" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول صنف — أضف منتجاً جديداً للنظام" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException | ProductService.PermanentDeleteAsync | ✅ |
| **RULE-202** | ALL Service methods return Result\<T\> | All new services | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | All new controllers — service only | ✅ |
| **RULE-210** | CHECK constraints at DB level | `CHK_InventoryBatches_Quantity_NonNegative`, `CHK_ProductPrices_Price_NonNegative` | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | All new FK configurations | ✅ |
| **RULE-220** | Newest-first sorting on lists | ProductsListViewModel: newest by Id desc | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | ProductEditorViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | ProductEditorViewModel | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | ProductEditorViewModel.Validate() | ✅ |
| **RULE-249** | Arabic string literals valid UTF-8 | All Arabic strings verified | ✅ |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | TabControl headers, dialog titles | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | Empty product list | ✅ |
| **RULE-270** | Dialog icons: 44×44 max | All error/success dialogs | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Batch data loss during FIFO migration** | **HIGH** — existing products have no InventoryBatches | Medium | Create opening batches for existing stock at time of migration with `IsOpeningBatch = true` and cost = current AvgCost |
| **Pricing inconsistency after deprecation** | **HIGH** — SalesPrice/WholesalePrice still read by old code | High | Keep legacy columns populated via sync method during transition period; remove only after all consumers updated |
| **FIFO migration complexity** | **HIGH** — requires new tables + algorithm + validation | Medium | Build FIFO as Option A (alongside WA); extensive unit tests before integration |
| **CurrencyId FK dependency on Phase 20** | **HIGH** — ProductPrice cannot be built without Currencies | High | Seed a base currency (Id=1) as part of Phase 25 if Phase 20 isn't ready; mark FK as int? until Phase 20 |
| **ProductPrice falling back to wrong currency** | **MEDIUM** — pricing errors in multi-currency invoices | Low | Implement strict fallback chain: exact match → retail price for currency → base currency conversion → manual entry |
| **Assembly production consuming wrong batches** | **MEDIUM** — incorrect COGS | Low | Assembly always calls `IFifoAllocationService.DeductFromBatchesAsync()` — same FIFO algorithm as sales |
| **Image upload failing silently** | **MEDIUM** — user thinks image saved | Medium | Validate file size (max 5MB) + format (JPG/PNG) + return clear error message |
| **MinStockAlert flooding EventBus** | **LOW** — UI performance degradation | Low | Batch alerts into single message per warehouse; throttle to max 1 alert per product per day |
| **Legacy field deprecation breaking existing code** | **MEDIUM** — compilation errors | Medium | Use `[Obsolete]` attribute with `error: false` first; change to `error: true` in next phase |
| **Barcode uniqueness violation after migration** | **MEDIUM** — duplicate barcodes across units | Low | Query all UnitBarcodes before auto-generating to ensure uniqueness; add DB unique index on BarcodeValue |
| **Product editor becoming too complex (6 tabs)** | **LOW** — user confusion | Low | Tab labels use clear Arabic names with ⓘ help icons; default to Basic Info tab on open |
| **PriceLevel excluded from V1** | **LOW** — pricing per (ProductUnit × CurrencyId) only, no level concept | Low | Removed entirely from V1 — UI falls back to flat per-currency price per unit |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **InventoryBatch migration causes issues** | `DROP TABLE InventoryBatchAllocations; DROP TABLE InventoryBatches;` |
| **ProductPrice migration causes issues** | `DROP TABLE ProductPrices;` |
| **ProductImage migration causes issues** | `DROP TABLE ProductImages;` |
| **BillOfMaterials migration causes issues** | `DROP TABLE BillOfMaterials;` |
| **Product column changes cause issues** | `ALTER TABLE Products DROP COLUMN AvgCost, HasExpiry, TrackBatches;` |
| **FIFO allocation service not working** | Switch back to WeightedAverage in Settings; FIFO remains optional opt-in |
| **Seed data seeding too aggressively** | `DELETE FROM Categories WHERE Name = N'عام';` + reset identity |
| **All new features need full rollback** | 1. Revert all migration files via `dotnet ef migrations remove` (sequentially) 2. Revert code changes via git: `git revert <phase25-commit-hash>` |
| **Pricing fallback too aggressive** | Remove fallback chain logic, require explicit price per currency+level |
| **Legacy field removal too early** | Revert `[Obsolete]` attributes; keep fields populated via sync |

---

### Task 19 — Comprehensive Unit Tests: Domain + Service + Validation + Config (7 Sub-Modules)

**Files**:

| File | Change |
|------|--------|
| `Tests/SalesSystem.Domain.Tests/Entities/ProductTests.cs` | **NEW** — Product factory tests |
| `Tests/SalesSystem.Domain.Tests/Entities/ProductUnitTests.cs` | **NEW** — conversion + base/derived unit tests |
| `Tests/SalesSystem.Domain.Tests/Entities/ProductPriceTests.cs` | **NEW** — pricing entity guards |
| `Tests/SalesSystem.Domain.Tests/Entities/InventoryBatchTests.cs` | **NEW** — batch creation + deduction tests |
| `Tests/SalesSystem.Domain.Tests/Entities/BillOfMaterialsTests.cs` | **NEW** — BOM guards |
| `Tests/SalesSystem.Application.Tests/Services/ProductServiceTests.cs` | **NEW** — service layer + FIFO + opening stock |
| `Tests/SalesSystem.Application.Tests/Services/FifoAllocationServiceTests.cs` | **NEW** — FIFO/FEFO allocation tests |
| `Tests/SalesSystem.Application.Tests/Services/ProductPriceServiceTests.cs` | **NEW** — pricing fallback chain |
| `Tests/SalesSystem.Application.Tests/Services/AssemblyServiceTests.cs` | **NEW** — production flow |
| `Tests/SalesSystem.Api.Tests/Validators/ProductRequestValidatorTests.cs` | **NEW** — validation rules |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/ProductConfigurationTests.cs` | **NEW** — EF config tests |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/InventoryBatchConfigurationTests.cs` | **NEW** — batch config tests |

---

#### 19.1 — Domain Entity Tests: Product.Create()

Test `Create()` factory and all 7 sub-module validations:

> See existing xUnit test patterns in `Tests/SalesSystem.Domain.Tests/` for Product entity factory tests. Test cases: valid creation, empty/null/long name, negative MinStockLevel, zero MinStockLevel, null CategoryId, HasExpiry false, TrackBatches default, Update changes, Update empty name, AddUnit, RemoveUnit last unit, RemoveUnit multiple, GetBaseUnit, GetBaseUnit missing.

**Estimate**: ~2 hours

---

#### 19.2 — Domain Entity Tests: ProductUnit + ProductPrice + InventoryBatch

> See existing xUnit test patterns in `Tests/SalesSystem.Domain.Tests/` for ProductUnit entity factory tests. Test cases: CreateBaseUnit, CreateDerivedUnit with valid factor, CreateDerivedUnit with factor ≤1, conversion methods, SalesPrice/WholesalePrice deprecation, etc.
        Assert.Equal(1, unit.ProductId);
        Assert.Equal("كرتون", unit.UnitName);
        Assert.Equal(24m, unit.BaseConversionFactor);
        Assert.False(unit.IsBaseUnit);
        Assert.Equal(2, unit.SortOrder);
    }

    [Fact]
    public void CreateDerivedUnit_WithFactorEqualToOrLessThanOne_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, "كرتون", 1m, createdByUserId: 1));
        Assert.Contains("معامل التحويل يجب أن يكون أكبر من 1", ex.Message);
    }

    [Fact]
    public void CreateBaseUnit_WithFactorNotEqualToOne_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUnit.CreateBaseUnit(1, "حبة", 2m, createdByUserId: 1));
        Assert.Contains("معامل التحويل للوحدة الأساسية يجب أن يكون 1", ex.Message);
    }

    [Fact]
    public void ConvertToUnit_ConvertsCorrectly()
    {
        var baseUnit = ProductUnit.CreateBaseUnit(1, "حبة", 1m, createdByUserId: 1);
        // 48 pieces ÷ 24 = 2 cartons
        var result = baseUnit.ConvertToUnit(48m, 24m);
        Assert.Equal(2m, result);
    }

    [Fact]
    public void ToBaseUnitQuantity_ConvertsCorrectly()
    {
        var carton = ProductUnit.CreateDerivedUnit(1, "كرتون", 24m, createdByUserId: 1);
        // 3 × 24 = 72 base units
        var result = carton.ToBaseUnitQuantity(3m);
        Assert.Equal(72m, result);
    }

    [Fact]
    public void CalculateCostFromBaseUnitCost_CalculatesCorrectly()
    {
        // Base unit cost = 5.00, factor = 24 → 5 * 24 = 120
        var carton = ProductUnit.CreateDerivedUnit(1, "كرتون", 24m, createdByUserId: 1);
        var cost = carton.CalculateCostFromBaseUnitCost(5m);
        Assert.Equal(120m, cost);
    }

    [Fact]
    public void UpdatePurchaseCost_UpdatesCorrectly()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة", 1m, createdByUserId: 1);
        unit.UpdatePurchaseCost(15m);
        Assert.Equal(15m, unit.PurchaseCost);
    }
}

public class ProductPriceTests
{
    [Fact]
    public void Create_SetsAllFieldsCorrectly()
    {
        var price = ProductPrice.Create(
            productUnitId: 1, currencyId: 1,
            priceLevel: PriceLevel.Retail, price: 50m,
            createdByUserId: 1);

        Assert.Equal(1, price.ProductUnitId);
        Assert.Equal(1, price.CurrencyId);
        Assert.Equal(PriceLevel.Retail, price.PriceLevel);
        Assert.Equal(50m, price.Price);
        Assert.True(price.IsActive);
        Assert.NotNull(price.EffectiveFrom);
        Assert.Null(price.EffectiveTo);
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductPrice.Create(1, 1, PriceLevel.Retail, -10m, createdByUserId: 1));
        Assert.Contains("السعر لا يمكن أن يكون سالباً", ex.Message);
    }

    [Fact]
    public void Create_WithZeroPrice_IsAllowed()
    {
        var price = ProductPrice.Create(1, 1, PriceLevel.Retail, 0m, createdByUserId: 1);
        Assert.Equal(0m, price.Price);
    }

    [Fact]
    public void SetPrice_UpdatesPriceAndEffectiveFrom()
    {
        var price = ProductPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        price.SetPrice(75m, 2);

        Assert.Equal(75m, price.Price);
        Assert.Equal(2, price.UpdatedByUserId);
    }

    [Fact]
    public void SetPrice_WithNegativePrice_ThrowsDomainException()
    {
        var price = ProductPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        var ex = Assert.Throws<DomainException>(() =>
            price.SetPrice(-5m, 2));
        Assert.Contains("السعر لا يمكن أن يكون سالباً", ex.Message);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndEffectiveTo()
    {
        var price = ProductPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        price.Deactivate(2);

        Assert.False(price.IsActive);
        Assert.NotNull(price.EffectiveTo);
    }
}

public class InventoryBatchTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesLotCorrectly()
    {
        var lot = InventoryBatch.Create(
            productId: 1, productUnitId: 1, warehouseId: 2,
            lotNumber: "B-20260605-001",
            quantity: 100m, unitCost: 25m,
            expiryDate: new DateTime(2027, 6, 5),
            receivedDate: DateTime.UtcNow,
            purchaseInvoiceId: null, isOpeningBatch: false,
            createdByUserId: 1);

        Assert.Equal(1, lot.ProductId);
        Assert.Equal(1, lot.ProductUnitId);
        Assert.Equal(2, lot.WarehouseId);
        Assert.Equal("B-20260605-001", lot.LotNumber);
        Assert.Equal(100m, lot.Quantity);
        Assert.Equal(100m, lot.OriginalQuantity);
        Assert.Equal(25m, lot.UnitCost);
        Assert.False(lot.IsOpeningBatch);
        Assert.True(lot.IsActive);
    }

    [Fact]
    public void Create_WithZeroQuantity_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            InventoryBatch.Create(1, 1, 1, "LOT001", 0m, 25m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("الكمية يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            InventoryBatch.Create(1, 1, 1, "LOT001", -10m, 25m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("الكمية يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithZeroUnitCost_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 0m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("التكلفة يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithOpeningBatch_SetsFlag()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "OPN-20260605-001",
            100m, 25m, null, DateTime.UtcNow, null, isOpeningBatch: true, null);
        Assert.True(lot.IsOpeningBatch);
    }

    [Fact]
    public void DeductQuantity_ReducesRemainingQuantity()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(30m);
        Assert.Equal(70m, lot.Quantity);
    }

    [Fact]
    public void DeductQuantity_WithExcessiveAmount_ThrowsDomainException()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        var ex = Assert.Throws<DomainException>(() =>
            lot.DeductQuantity(150m));
        Assert.Contains("الكمية المطلوبة أكبر من المتاحة في الدفعة", ex.Message);
    }

    [Fact]
    public void AddReturnQuantity_IncreasesRemainingQuantity()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(50m);
        lot.AddReturnQuantity(20m);
        Assert.Equal(70m, lot.Quantity);
    }

    [Fact]
    public void AddReturnQuantity_ExceedingOriginal_ThrowsDomainException()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(50m);
        var ex = Assert.Throws<DomainException>(() =>
            lot.AddReturnQuantity(60m));
        Assert.Contains("لا يمكن إرجاع كمية أكبر من الكمية الأصلية للدفعة", ex.Message);
    }

    [Fact]
    public void IsFullyConsumed_WhenQuantityZero_ReturnsTrue()
    {
        var lot = InventoryBatch.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(100m);
        Assert.Equal(0m, lot.Quantity);
    }
}

public class BillOfMaterialsTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesBomCorrectly()
    {
        var bom = BillOfMaterials.Create(
            assemblyProductId: 1, componentProductId: 2,
            componentUnitId: 3, quantityRequired: 4m,
            wastePercentage: 5m, createdByUserId: 1);

        Assert.Equal(1, bom.AssemblyProductId);
        Assert.Equal(2, bom.ComponentProductId);
        Assert.Equal(3, bom.ComponentUnitId);
        Assert.Equal(4m, bom.QuantityRequired);
        Assert.Equal(5m, bom.WastePercentage);
        Assert.True(bom.IsActive);
    }

    [Fact]
    public void Create_WithZeroQuantityRequired_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BillOfMaterials.Create(1, 2, 3, 0m, 0m, null));
        Assert.Contains("الكمية المطلوبة يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithNegativeWastePercentage_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BillOfMaterials.Create(1, 2, 3, 4m, -1m, null));
        Assert.Contains("نسبة الهالك لا يمكن أن تكون سالبة", ex.Message);
    }

    [Fact]
    public void Create_WithZeroWastePercentage_IsAllowed()
    {
        var bom = BillOfMaterials.Create(1, 2, 3, 4m, 0m, null);
        Assert.Equal(0m, bom.WastePercentage);
    }
}
```

**Estimate**: ~3 hours

---

#### 19.3 — Service Tests: ProductService (Opening Stock + CRUD)

> See existing xUnit service test patterns in `Tests/SalesSystem.Application.Tests/Services/` for setup with Mock<IUnitOfWork>, repository mocks, and Result<T> assertions.

**Estimate**: ~2 hours

---

#### 19.4 — Opening Stock Batch Service Tests

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

**Estimate**: ~2 hours

---

#### 19.5 — FIFO Allocation Service Tests

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

**Estimate**: ~4 hours

---

#### 19.6 — Pricing Service Tests (Fallback Chain)

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

**Estimate**: ~3 hours

---

#### 19.7 — FluentValidation Tests

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

**Estimate**: ~2 hours

---

#### 19.8 — Database Configuration Tests

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for canonical table definitions.

> **Note**: `InventoryBatches` / `InventoryBatchAllocation` entities are named `InventoryBatches` / `InventoryBatchAllocation` in the final schema. `PriceLevel` is NOT in V1 — pricing is per (ProductUnit × CurrencyId) only.

**Estimate**: ~2.5 hours

---

#### 19.9 — Assembly Service Tests

See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions.

**Estimate**: ~3 hours

---

#### 19.10 — Execution Summary

| Sub-Task | Focus | Key Test Files | Estimate |
|----------|-------|----------------|----------|
| 19.1 | Product.Create() factory + guards + units | `ProductTests.cs` | 2h |
| 19.2 | ProductUnit, ProductPrice, InventoryBatch, BOM entities | `ProductUnitTests.cs`, `InventoryBatchTests.cs`, `BillOfMaterialsTests.cs` | 3h |
| 19.3 | ProductService CRUD + opening stock batch | `ProductServiceTests.cs` | 2h |
| 19.4 | OpeningStock: InventoryBatch + InventoryMovement + WarehouseStock | `ProductServiceOpeningStockTests.cs` | 2h |
| 19.5 | FIFO/FEFO allocation algorithm + multi-lot spanning | `FifoAllocationServiceTests.cs` | 4h |
| 19.6 | Pricing fallback chain + price history recording | `ProductPriceServiceTests.cs` | 3h |
| 19.7 | FluentValidation: Product, ProductPrice requests | `ProductRequestValidatorTests.cs` | 2h |
| 19.8 | EF Config: precision, maxlength, Restrict, check constraints | `ProductConfigurationTests.cs`, `InventoryBatchConfigurationTests.cs` | 2.5h |
| 19.9 | Assembly production + component deduction | `AssemblyServiceTests.cs` | 3h |
| **Total** | **9 sub-tasks** | **~12 test files** | **~23.5h** |
