# Phase 25 — Products Module: Comprehensive Enhancement & Implementation Plan

> **Version**: 1.0 — Full rewrite based on Analysis Parts 1–5, Global Analysis, and codebase audit
> **Scope**: Complete Products module overhaul — Pricing per currency, Multiple price levels, FIFO batch tracking, Assembly/BOM, Product images, Min stock alerts, Enhanced barcode management, Default seeds

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
| 💰 | **Pricing** | Per-unit + per-currency pricing with history | Units, Currencies (Phase 20) |
| 🏷️ | **Barcodes** | Per-unit barcodes, auto-generation, scanning | Units |
| 📦 | **Batches/FIFO** | Purchase lots, FIFO/FEFO allocation, costing | Products, Purchases |
| 🗂️ | **Categories** | Product grouping, default "عام" seed | (standalone) |
| 📜 | **Price History** | Audit trail for all cost/price changes | ProductUnits |

### Data Flow

```text
Desktop → (HttpClient) → Api → Application Services → Infrastructure → SQL Server
                                    ↓
                             Domain (Products, ProductUnits, PurchaseLots, 
                                    BillOfMaterials, ProductImages, Categories)
```

**Stock/Costing flow:**
```text
PurchaseInvoice → Create PurchaseLot (FIFO) → Update WarehouseStock 
               → UpdateProductPricingService → ProductPriceHistory
SaleInvoice    → Deduct from PurchaseLots (FIFO/FEFO) → Update WarehouseStock
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Product Entity ✅ (Exists — needs extension)

**File**: `Domain/Entities/Product.cs` (245 lines)

| Field | Type | Status | Phase 25 Action |
|-------|------|--------|-----------------|
| `Id` | `int PK` | ✅ | Keep |
| `Name` | `string(150)` | ✅ | Keep |
| `Barcode` | `string(50)?` | ✅ | **DEPRECATE** — move to UnitBarcode |
| `CategoryId` | `int?` FK | ✅ | Keep |
| `UnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `WholesaleUnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `RetailUnitId` (Legacy) | `int?` FK | ⚠️ Legacy | **DEPRECATE** — remove |
| `ConversionFactor` (Legacy) | `decimal(18,3)` | ⚠️ Legacy | **DEPRECATE** — use ProductUnit |
| `PurchasePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — cost from PurchaseLots |
| `SalePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductUnit prices |
| `WholesalePrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductUnit prices |
| `RetailPrice` (Legacy) | `decimal(18,2)` | ⚠️ Legacy | **DEPRECATE** — use ProductUnit prices |
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
| `SalesPrice` | `decimal(18,2)` | ✅ | **DEPRECATE** → moved to ProductUnitPrice |
| `WholesalePrice` | `decimal(18,2)` | ✅ | **DEPRECATE** → moved to ProductUnitPrice |
| `PurchaseCost` | `decimal(18,2)` | ✅ | Keep (computed from batches) |
| `SupplierPrice` | `decimal(18,2)` | ✅ | Keep (catalog reference) |
| `LastPurchasePrice` | `decimal(18,2)` | ✅ | Keep (informational) |
| `SortOrder` | `int` | ✅ | Keep |
| Barcodes collection | `List<UnitBarcode>` | ✅ | Keep |

**New fields**:
- `CurrencyId` → ❌ Missing → **ADD** (moved to ProductUnitPrice per analysis)
- Actually pricing goes to **ProductUnitPrice** entity, not here

**Methods**:
- `UpdatePurchaseCost()` — keep
- `UpdateSalesPrice()` — **REMOVE** (pricing is now in ProductUnitPrice)
- `GetPriceByUnit()` — **REMOVE** (use ProductUnitPrice)
- `AddBarcode()` — keep
- `CalculateCostFromBaseUnitCost()` — keep
- `ToBaseUnitQuantity()` — keep

### 2.3 UnitBarcode Entity ✅ (Exists)

**File**: `Domain/Entities/UnitBarcode.cs`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` | ✅ |
| `ProductUnitId` | `int FK` | ✅ |
| `BarcodeValue` | `string(50)` | ✅ |
| `IsDefault` | `bool` | ✅ |
| `SupplierCode` | `string(50)?` | ✅ |

**Note**: This is the correct structure per analysis. The `ProductBarcode` entity (per-product barcode) is legacy and should be deprecated.

### 2.4 ProductBarcode Entity ⚠️ (Legacy — Deprecate)

**File**: `Domain/Entities/ProductBarcode.cs` (36 lines)

Per Analysis Part 3: barcode must follow the **unit**, not the product. This entity is redundant with `UnitBarcode`.

**Decision**: **DEPRECATE** — hide from UI, keep in DB for backwards compat. All new barcode creation goes through `UnitBarcode`.

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

**Enhancement needed**: Add `CurrencyId` + `PriceLevel` fields to support multi-currency price history tracking.

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
| `PurchaseLot` entity + config | ❌ NOT EXIST — must create |
| `PurchaseLotAllocation` entity | ❌ NOT EXIST — must create |
| `BillOfMaterials` entity + config | ❌ NOT EXIST — must create |
| `BillOfMaterialsItem` entity | ❌ NOT EXIST — must create |
| `ProductImage` entity + config | ❌ NOT EXIST — must create |
| `ProductUnitPrice` entity + config | ❌ NOT EXIST — must create (pricing per currency) |
| `PriceLevel` enum (VIP, Distributor) | ❌ NOT EXIST — must create |
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
Purchase → Create PurchaseLot (LotNumber, Qty, UnitCost, ExpiryDate)
         → UpdateProductPricingService (optional — keep WeightedAverage as alternative)
         → InventoryMovement record
Sale → Deduct from PurchaseLots (earliest lot first)
     → If HasExpiry: FEFO (earliest expiry first)
     → InventoryMovement record
```

**Decision Options**:

| Option | Description | Complexity | Migration |
|--------|-------------|------------|-----------|
| **A: FIFO + WeightedAverage** | Add FIFO as CostingMethod = 4, keep WA as default | **HIGH** — ~600 new lines + new tables | Requires new PurchaseLot table, no backfill of existing stock |
| **B: FIFO Only** | Replace WA entirely with FIFO | **HIGH** — requires rewriting UpdateProductPricingService | Breaks existing stock cost data |
| **C: Defer FIFO to V2** | Keep WA only, skip batch tracking | **LOW** — no changes | Analysis explicitly requires FIFO — cannot defer |

**⚠️ RECOMMENDATION**: **Option A** — Add FIFO as costing method option (4). Keep WeightedAverage as default. This satisfies analysis requirements while maintaining backwards compatibility. Users choose their costing method in Settings.

**RULE-068/069 amendment required**:
- Add `FIFO = 4` to `CostingMethod` enum
- Add `EnableFefo` SystemSetting (already planned in Phase 19)
- Update `UpdateProductPricingService` to handle FIFO method

### 3.2 Blocker 2: Pricing Model — Per-Unit + Per-Currency + Price Levels

**Problem**: Current pricing stores `SalesPrice` and `WholesalePrice` directly on `ProductUnit`. Analysis requires:
1. Price = f(ProductUnit, Currency) — per-currency pricing
2. Multiple price levels: Retail, Wholesale, VIP, Distributor
3. Price history per change

**Current**:
```csharp
// ProductUnit.cs
public decimal SalesPrice { get; private set; }      // Single retail price
public decimal WholesalePrice { get; private set; }   // Single wholesale price
```

**Required**:
```csharp
// NEW: ProductUnitPrice entity
public class ProductUnitPrice : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public int CurrencyId { get; private set; }
    public PriceLevel PriceLevel { get; private set; } // Retail=1, Wholesale=2, VIP=3, Distributor=4
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
}
```

**This BLOCKER depends on Phase 20 (Currencies)** — `CurrencyId` FK cannot exist without the Currencies module. Options:

| Option | Description |
|--------|-------------|
| **A: Wait for Phase 20** | Build pricing without CurrencyId, add it in Phase 20 |
| **B: Build now with CurrencyId placeholder** | Add CurrencyId FK now (requires Currency table to exist or be seeded) |

**⚠️ RECOMMENDATION**: **Option A** — Build the `ProductUnitPrice` entity NOW with CurrencyId as `int NOT NULL`, and ensure the Currencies module (Phase 20) seeds at least a "Base Currency" (e.g., YER or SAR) so the FK constraint is satisfied. This avoids a later migration.

### 3.3 Blocker 3: PurchaseLot Entity Doesn't Exist

**Problem**: FIFO tracking requires a `PurchaseLot` entity that tracks:
- ProductId + WarehouseId
- Quantity (remaining)
- UnitCost (in base currency)
- LotNumber (auto-generated or user-provided)
- ExpiryDate (optional — for FEFO)
- ReceivedDate
- SourceInvoiceId

**No PurchaseLot entity, no PurchaseLotConfiguration, no migration, no service layer exists.**

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
| 3 | `Barcode` (Legacy) | `nvarchar(50)` | `null` | ❌ | **UNIQUE** | ⚠️ Deprecate |
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
- `SalePrice` (Legacy — use ProductUnitPrice)
- `WholesalePrice` (Legacy — use ProductUnitPrice)
- `RetailPrice` (Legacy — use ProductUnitPrice)
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

**Deprecated fields** (move to ProductUnitPrice):
- `SalesPrice` → use ProductUnitPrice where PriceLevel = Retail
- `WholesalePrice` → use ProductUnitPrice where PriceLevel = Wholesale

### 4.3 ProductUnitPrice Entity — NEW

Stores price per unit + currency + price level combination.

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `ProductUnitId` | `int FK` | — | ✅ | FK→ProductUnits, Restrict |
| 3 | `CurrencyId` | `int FK` | — | ✅ | FK→Currencies, Restrict |
| 4 | `PriceLevel` | `tinyint` | `1` (Retail) | ✅ | 1=Retail, 2=Wholesale, 3=VIP, 4=Distributor |
| 5 | `Price` | `decimal(18,2)` | `0` | ✅ | `CHECK >= 0` |
| 6 | `EffectiveFrom` | `datetime2` | `GETUTCDATE()` | ✅ | — |
| 7 | `EffectiveTo` | `datetime2?` | `null` | ❌ | Null = currently active |
| 8 | `IsActive` | `bit` | `true` | ❌ | Global QF |

**Unique Index**: `(ProductUnitId, CurrencyId, PriceLevel)` — one active price per combination.

**Seed data**: Each product's base unit gets a default Retail + Wholesale price entry for the base currency (Phase 20 dependency).

### 4.4 PurchaseLot Entity — NEW (FIFO Foundation)

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
- On purchase: create PurchaseLot with `Qty = OriginalQuantity`, `Quantity = OriginalQuantity`
- On sale: deduct from batches (FIFO or FEFO), reduce `Quantity`, create `InventoryMovement` records
- When `Quantity = 0`: lot is fully consumed (keep for history, mark inactive)
- `LotNumber` auto-format: `B-{YYYYMMDD}-{Sequence:000}` (e.g., `B-20260605-001`)

### 4.5 PurchaseLotAllocation Entity — NEW

Tracks which sales invoice items consumed from which purchase lot.

| # | Field | Type | Default | Required | Constraints |
|---|-------|------|---------|----------|-------------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | — |
| 2 | `PurchaseLotId` | `int FK` | — | ✅ | FK→PurchaseLots, Restrict |
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

### 4.8 PriceLevel Enum — NEW

```csharp
public enum PriceLevel : byte
{
    Retail = 1,        // سعر التجزئة
    Wholesale = 2,     // سعر الجملة
    VIP = 3,           // سعر VIP
    Distributor = 4    // سعر الموزع
}
```

**Note**: Analysis Part 5 initially recommended against multiple price levels in V1, but the reference images from محاسب سوفت clearly show this feature. Decision: **Include in V1** with 4 levels. Users can ignore unused levels.

### 4.9 CostingMethod Enum — EXTENDED

```csharp
public enum CostingMethod : byte
{
    WeightedAverage = 1,   // Current — default
    LastPurchasePrice = 2, // Current
    SupplierPrice = 3,     // Current
    FIFO = 4               // NEW — batch-based costing
}
```

### 4.10 ProductPriceHistory Entity — Enhanced

Add these fields to support multi-currency pricing history + price validity periods:

| # | Field | Type | Required | Status |
|---|-------|------|----------|--------|
| `CurrencyId` | `int` FK? | ❌ | **NEW** |
| `PriceLevel` | `tinyint`? | ❌ | **NEW** |
| `OldPrice` | `decimal(18,2)` | ❌ | **NEW** (to complement OldAvgCost) |
| `NewPrice` | `decimal(18,2)` | ❌ | **NEW** |
| `FromDate` | `datetime2` | ✅ | **NEW** — When this price becomes effective |
| `ToDate` | `datetime2?` | ❌ | **NEW** — Optional: when price expires |
| Existing fields | — | — | ✅ Keep |

**Validity logic**:
```csharp
// If ToDate is null, this is the current active price
public bool IsCurrentPrice => ToDate == null || ToDate >= DateTime.UtcNow;
```

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
| `SalesPrice` | ⚠️ Deprecate | Move to ProductUnitPrice.Price with PriceLevel=Retail |
| `WholesalePrice` | ⚠️ Deprecate | Move to ProductUnitPrice.Price with PriceLevel=Wholesale |

### 5.3 Pricing

| Component | Status | Action |
|-----------|--------|--------|
| Per-currency pricing | ❌ Missing | Create ProductUnitPrice entity |
| Multiple price levels | ❌ Missing | Create PriceLevel enum + ProductUnitPrice.PriceLevel |
| ProductUnitPrice entity + config | ❌ Missing | Full build |
| Price history for per-currency changes | ❌ Missing | Extend ProductPriceHistory with CurrencyId + PriceLevel |

### 5.4 Batches / FIFO

| Component | Status | Action |
|-----------|--------|--------|
| PurchaseLot entity | ❌ Missing | Full build — core of FIFO |
| PurchaseLotAllocation entity | ❌ Missing | Track sale→lot consumption |
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

```csharp
// IFifoAllocationService.cs — Application layer
public interface IFifoAllocationService
{
    // Called ON PURCHASE: Create PurchaseLot records
    Task<Result<List<PurchaseLot>>> AddPurchaseBatchesAsync(
        int productUnitId, int warehouseId, decimal quantity,
        decimal unitCostBase, string? lotNumber, DateTime? expiryDate,
        int? purchaseInvoiceId, bool isOpeningBatch, CancellationToken ct);

    // Called ON SALE: Deduct from earliest batches (FIFO) or earliest expiry (FEFO)
    Task<Result<List<PurchaseLotAllocation>>> DeductFromBatchesAsync(
        int productId, int warehouseId, decimal quantity,
        int? salesInvoiceItemId, bool useFefo, CancellationToken ct);

    // Called ON RETURN: Add back to the original batch
    Task<Result> ReturnToBatchAsync(
        int allocationId, decimal quantityReturned,
        int? salesReturnItemId, CancellationToken ct);

    // Get current stock breakdown by batch
    Task<Result<List<BatchStockInfo>>> GetBatchBreakdownAsync(
        int productId, int warehouseId, CancellationToken ct);
}
```

**FIFO Allocation Algorithm**:
```
Input: ProductId, WarehouseId, QuantityNeeded
1. Query PurchaseLots WHERE ProductId AND WarehouseId AND Quantity > 0
2. ORDER BY (useFefo ? ExpiryDate ASC : ReceivedDate ASC, Id ASC)
3. For each lot:
   - TakeQuantity = Min(QuantityNeeded, lot.Quantity)
   - Create PurchaseLotAllocation record
   - lot.Quantity -= TakeQuantity
   - QuantityNeeded -= TakeQuantity
   - If QuantityNeeded == 0: break
4. If QuantityNeeded > 0: return error "Insufficient stock"
```

**Costing integration**: When `CostingMethod = FIFO`, `UpdateProductPricingService` computes cost as the weighted average of all active PurchaseLots (for display). The actual COGS for each sale is the specific lot's UnitCost.

### 6.2 Pricing Model — Per-Unit + Per-Currency + Price Level

**Decision**: Create `ProductUnitPrice` entity as the single source of truth for all pricing.

```csharp
// Price lookup algorithm:
// 1. Find ProductUnitPrice WHERE ProductUnitId = X AND CurrencyId = Y AND PriceLevel = Z
// 2. If not found and Z > 1 (Wholesale/VIP/Distributor): fallback to Retail price for that currency
// 3. If not found for currency: fallback to base currency price with exchange rate conversion
// 4. If still not found: price = 0 (user must enter manually in invoice)

public async Task<Result<decimal>> GetEffectivePriceAsync(
    int productUnitId, int currencyId, PriceLevel priceLevel, CancellationToken ct)
{
    // Exact match first
    var exact = await _repo.FindAsync(pup => 
        pup.ProductUnitId == productUnitId && 
        pup.CurrencyId == currencyId && 
        pup.PriceLevel == priceLevel &&
        pup.IsActive);
    if (exact != null) return Result<decimal>.Success(exact.Price);

    // Fallback to Retail price for this currency
    if (priceLevel != PriceLevel.Retail)
    {
        var retail = await _repo.FindAsync(pup =>
            pup.ProductUnitId == productUnitId &&
            pup.CurrencyId == currencyId &&
            pup.PriceLevel == PriceLevel.Retail &&
            pup.IsActive);
        if (retail != null) return Result<decimal>.Success(retail.Price);
    }

    // Fallback: convert from base currency
    // ... exchange rate conversion logic ...
    return Result<decimal>.Failure("لا يوجد سعر محدد لهذه التركيبة", "PRICE_NOT_FOUND");
}
```

**Price history**: Every create/update/delete of `ProductUnitPrice` records a `ProductPriceHistory` entry with CurrencyId + PriceLevel.

### 6.3 Batch Allocation Algorithm — FIFO vs FEFO

**Decision**: Always use FIFO. Use FEFO only when the product has `HasExpiry = true`.

```csharp
public async Task<Result<List<PurchaseLotAllocation>>> DeductFromBatchesAsync(
    int productId, int warehouseId, decimal quantityNeeded,
    int? salesInvoiceItemId, CancellationToken ct)
{
    var product = await _uow.Products.GetByIdAsync(productId, ct);
    if (product == null) return Result<List<PurchaseLotAllocation>>.Failure("المنتج غير موجود");

    var useFefo = product.HasExpiry;

    var availableLots = await _uow.PurchaseLots.FindAllAsync(l =>
        l.ProductId == productId &&
        l.WarehouseId == warehouseId &&
        l.Quantity > 0 &&
        l.IsActive);

    // Sort: FEFO if has expiry, otherwise FIFO
    var sortedLots = useFefo
        ? availableLots.OrderBy(l => l.ExpiryDate ?? DateTime.MaxValue).ThenBy(l => l.Id)
        : availableLots.OrderBy(l => l.ReceivedDate).ThenBy(l => l.Id);

    var allocations = new List<PurchaseLotAllocation>();
    var remaining = quantityNeeded;

    foreach (var lot in sortedLots)
    {
        if (remaining <= 0) break;

        var takeQuantity = Math.Min(remaining, lot.Quantity);
        var allocation = PurchaseLotAllocation.Create(
            lot.Id, salesInvoiceItemId, takeQuantity, lot.UnitCost);
        lot.DeductQuantity(takeQuantity);

        allocations.Add(allocation);
        remaining -= takeQuantity;
    }

    if (remaining > 0)
        return Result<List<PurchaseLotAllocation>>.Failure("الكمية المتاحة في المخزون غير كافية");

    return Result<List<PurchaseLotAllocation>>.Success(allocations);
}
```

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

### 6.6 Barcode Strategy — Unit-Level Only

**Decision**: Per Analysis Part 3, barcode belongs to the **unit**, not the product. The existing `ProductBarcode` entity is deprecated. All new barcode creation uses `UnitBarcode`.

**Auto-generation**: When creating a unit without a barcode, generate one:
- Format: `29` + `ProductUnitId.ToString("D10")` (e.g., `290000000125`)
- Starting at `290000000001`
- Adding a check digit is deferred to V2 unless specifically requested

### 6.7 Min Stock Alert — Background Service

**Decision**: Implement as a `BackgroundService` that runs periodically (configurable interval, default 6 hours) and checks all products where `WarehouseStock.Quantity <= Product.MinStockLevel`.

**Alert delivery**: Via `EventBus` → `LowStockAlertMessage` → Desktop notification through `IToastNotificationService`.

```csharp
public class MinStockAlertWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            await CheckLowStockAsync(stoppingToken);
        }
    }

    private async Task CheckLowStockAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var lowStockItems = await uow.WarehouseStocks.FindAllAsync(
            ws => ws.Quantity <= ws.Product.MinStockLevel &&
                  ws.Product.IsActive &&
                  ws.Warehouse.IsActive &&
                  ws.Quantity > 0, // Only items that HAVE stock but below threshold
            ct, "Product", "Warehouse");

        foreach (var item in lowStockItems)
        {
            eventBus.Publish(new LowStockAlertMessage(
                item.ProductId, item.Product.Name, item.Warehouse.Name,
                item.Quantity, item.Product.MinStockLevel));
        }
    }
}
```

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
| **Stock Transfer Orders** (between warehouses) | Already planned as separate module |
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
```csharp
// ═══════════════════════════════════════════
// Seed Default Categories
// ═══════════════════════════════════════════
if (!await db.Set<Category>().AnyAsync())
{
    var general = Category.Create("عام", "التصنيف الافتراضي لجميع المنتجات");
    db.Set<Category>().Add(general);
    logger?.LogInformation("Seeded default category 'عام'.");
}
```

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

### Task 2 — Create PurchaseLot Entity + Configuration + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PurchaseLot.cs` | NEW — full entity with guard clauses |
| `Domain/Entities/PurchaseLotAllocation.cs` | NEW — allocation tracking |
| `Infrastructure/Data/Configurations/PurchaseLotConfiguration.cs` | NEW — Fluent API config |
| `Infrastructure/Data/Configurations/PurchaseLotAllocationConfiguration.cs` | NEW — Fluent API config |
| `Infrastructure/Data/Migrations/` | NEW migration: 2 new tables |

**PurchaseLot entity pattern**:
```csharp
public class PurchaseLot : BaseEntity
{
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public int WarehouseId { get; private set; }
    public string LotNumber { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }       // Remaining quantity
    public decimal OriginalQuantity { get; private set; } // Original quantity
    public decimal UnitCost { get; private set; }       // In base currency
    public DateTime? ExpiryDate { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public bool IsOpeningBatch { get; private set; }

    private PurchaseLot() { }

    public static PurchaseLot Create(...)
    {
        // Guard clauses
        if (quantity <= 0) throw new DomainException("الكمية يجب أن تكون أكبر من الصفر");
        if (unitCost <= 0) throw new DomainException("التكلفة يجب أن تكون أكبر من الصفر");
        // ...
    }

    public void DeductQuantity(decimal qty)
    {
        if (qty <= 0) throw new DomainException("الكمية المراد خصمها يجب أن تكون أكبر من الصفر");
        if (qty > Quantity) throw new DomainException("الكمية المطلوبة أكبر من المتاحة في الدفعة");
        Quantity -= qty;
        UpdateTimestamp();
    }

    public void AddReturnQuantity(decimal qty)
    {
        if (qty <= 0) throw new DomainException("الكمية المراد إرجاعها يجب أن تكون أكبر من الصفر");
        if (Quantity + qty > OriginalQuantity)
            throw new DomainException("لا يمكن إرجاع كمية أكبر من الكمية الأصلية للدفعة");
        Quantity += qty;
        UpdateTimestamp();
    }
}
```

**PurchaseLotConfiguration**:
```csharp
builder.ToTable("PurchaseLots");
builder.Property(l => l.LotNumber).IsRequired().HasMaxLength(50);
builder.Property(l => l.Quantity).HasPrecision(18, 3);
builder.Property(l => l.OriginalQuantity).HasPrecision(18, 3);
builder.Property(l => l.UnitCost).HasPrecision(18, 2);
builder.Property(l => l.ReceivedDate).IsRequired();
builder.ToTable(t => t.HasCheckConstraint("CHK_PurchaseLots_Quantity_NonNegative", "[Quantity] >= 0"));
builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
builder.HasOne(l => l.Warehouse).WithMany().HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Restrict);
```

**Migration SQL** (generated by EF Core):
```sql
CREATE TABLE PurchaseLots (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId int NOT NULL,
    ProductUnitId int NOT NULL,
    WarehouseId int NOT NULL,
    LotNumber nvarchar(50) NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    OriginalQuantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NOT NULL,
    ExpiryDate datetime2 NULL,
    ReceivedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
    PurchaseInvoiceId int NULL,
    IsOpeningBatch bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    -- Audit fields...
);
ALTER TABLE PurchaseLots ADD CONSTRAINT CHK_PurchaseLots_Quantity_NonNegative CHECK (Quantity >= 0);
```

**Estimate**: ~3 hours

---

### Task 3 — Create FIFO Allocation Service (IFifoAllocationService)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IFifoAllocationService.cs` | NEW — interface with 5 methods |
| `Application/Services/FifoAllocationService.cs` | NEW — implementation |
| `Contracts/Common/AllDtos.cs` | Add `BatchStockInfoDto`, `PurchaseLotDto` |

**IFifoAllocationService Interface**:
```csharp
public interface IFifoAllocationService
{
    Task<Result<List<PurchaseLot>>> AddPurchaseBatchesAsync(
        int productUnitId, int warehouseId, decimal quantity,
        decimal unitCostBase, string? lotNumber, DateTime? expiryDate,
        int? purchaseInvoiceId, bool isOpeningBatch, CancellationToken ct);

    Task<Result<List<PurchaseLotAllocation>>> DeductFromBatchesAsync(
        int productId, int warehouseId, decimal quantityNeeded,
        int? salesInvoiceItemId, CancellationToken ct);

    Task<Result> ReturnToBatchAsync(
        int allocationId, decimal quantityReturned,
        int? salesReturnItemId, CancellationToken ct);

    Task<Result<List<BatchStockInfo>>> GetBatchBreakdownAsync(
        int productId, int warehouseId, CancellationToken ct);

    Task<Result<decimal>> GetCurrentStockCostAsync(
        int productId, int warehouseId, CancellationToken ct);
}
```

**Key implementation details**:
- `AddPurchaseBatchesAsync`: Converts quantity to base unit first → creates PurchaseLot
- `DeductFromBatchesAsync`: FEFO if product.HasExpiry, otherwise FIFO
- Batch operations inside transactions (RULE-003)
- Every allocation = `InventoryMovement` record (RULE-029)

**Logging** (RULE-035):
- `Log.Information("PurchaseLot {LotNumber} created: Product {ProductId}, Qty {Qty}, Cost {Cost}", ...)`
- `Log.Information("FIFO allocation: {Qty} units from Lot {LotId} to InvoiceItem {ItemId}", ...)`

**Estimate**: ~4 hours

---

### Task 4 — Update UpdateProductPricingService for FIFO

**Files**:

| File | Change |
|------|--------|
| `Application/Services/UpdateProductPricingService.cs` | Add FIFO costing method handler |
| `Domain/Enums/CostingMethod.cs` | Add `FIFO = 4` |

**New logic in UpdateFromPurchaseAsync**:
```csharp
// After existing WA/LPP/SP logic:
if (costingMethod == CostingMethod.FIFO)
{
    // Create PurchaseLot via IFifoAllocationService
    var batchResult = await _fifoService.AddPurchaseBatchesAsync(
        request.ProductUnitId,
        request.WarehouseId,
        request.NewQuantityPurchased,
        newBaseUnitCost, // Already converted to base currency
        request.LotNumber,
        request.ExpiryDate,
        request.PurchaseInvoiceId,
        isOpeningBatch: false,
        ct);

    if (!batchResult.IsSuccess)
        return Result.Failure(batchResult.Error!);

    // Update the unit's LastPurchasePrice for display
    purchasedUnit.UpdatePurchaseCost(newBaseUnitCost);
    
    // Record history
    _priceHistoryService.RecordCostChange(/*...*/);
    
    return Result.Success();
}
```

**Note**: When FIFO is selected:
- `AvgCost` on Product = calculated from active PurchaseLots (weighted average of remaining quantities)
- Actual sale COGS = specific lot's UnitCost (not computed average)

**Estimate**: ~2 hours

---

### Task 5 — Create ProductUnitPrice Entity + Pricing Service

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/ProductUnitPrice.cs` | NEW — per-unit, per-currency, per-level pricing |
| `Domain/Enums/PriceLevel.cs` | NEW — enum (Retail=1, Wholesale=2, VIP=3, Distributor=4) |
| `Infrastructure/Data/Configurations/ProductUnitPriceConfiguration.cs` | NEW — Fluent API config |
| `Application/Interfaces/Services/IProductUnitPriceService.cs` | NEW — interface |
| `Application/Services/ProductUnitPriceService.cs` | NEW — implementation with price lookup + history |
| `Infrastructure/Data/Migrations/` | NEW migration |
| `Contracts/DTOs/AllDtos.cs` | Add `ProductUnitPriceDto` |
| `Contracts/Requests/ProductRequests.cs` | Add `CreateProductUnitPriceRequest`, `UpdateProductUnitPriceRequest` |

**ProductUnitPriceService methods**:
```csharp
Task<Result<ProductUnitPriceDto>> SetPriceAsync(
    int productUnitId, int currencyId, PriceLevel priceLevel,
    decimal price, int changedByUserId, CancellationToken ct);

Task<Result<ProductUnitPriceDto>> UpdatePriceAsync(
    int id, decimal newPrice, int changedByUserId, CancellationToken ct);

Task<Result<List<ProductUnitPriceDto>>> GetPricesForUnitAsync(
    int productUnitId, CancellationToken ct);

Task<Result<decimal>> GetEffectivePriceAsync(
    int productUnitId, int currencyId, PriceLevel priceLevel, CancellationToken ct);

Task<Result> DeletePriceAsync(int id, CancellationToken ct);
```

**GetEffectivePriceAsync algorithm** (fallback chain):
1. Exact match: ProductUnitId + CurrencyId + PriceLevel
2. Fallback to Retail price for same unit + currency
3. Fallback to base currency price (converted via exchange rate)
4. Return 0 if nothing found (user enters manually)

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

**ProductImage entity**:
```csharp
public class ProductImage : BaseEntity
{
    public int ProductId { get; private set; }
    public string ImagePath { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    public static ProductImage Create(int productId, string imagePath, bool isPrimary = false, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new DomainException("مسار الصورة مطلوب");
        return new ProductImage { ProductId = productId, ImagePath = imagePath, IsPrimary = isPrimary, SortOrder = sortOrder };
    }

    public void SetPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
}
```

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

**BillOfMaterials entity**:
```csharp
public class BillOfMaterials : BaseEntity
{
    public int AssemblyProductId { get; private set; }  // The finished good
    public int ComponentProductId { get; private set; } // The raw material
    public int ComponentUnitId { get; private set; }    // Unit of component
    public decimal QuantityRequired { get; private set; } // How many units per assembly
    public decimal WastePercentage { get; private set; }  // e.g., 5% waste

    // Unique: one component can only appear once per assembly
}
```

**AssemblyService.ProduceAsync**: Transactional — deduct components, add assembly product, record history.

**Estimate**: ~3 hours

---

### Task 8 — Add CostingMethod.FIFO = 4 + EnableFefo Seed

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/CostingMethod.cs` | Add `FIFO = 4` |

```csharp
public enum CostingMethod : byte
{
    WeightedAverage = 1,
    LastPurchasePrice = 2,
    SupplierPrice = 3,
    FIFO = 4  // NEW
}
```

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

**New Product.Create signature**:
```csharp
public static Product Create(
    string name,
    int? categoryId = null,
    decimal minStockLevel = 0,
    decimal reorderLevel = 0,
    string? description = null,
    bool hasExpiry = false,
    bool trackBatches = true,
    int? createdByUserId = null)
```

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
| `ProductUnit.UpdateSalesPrice()` | **REMOVE** — pricing via ProductUnitPriceService |
| `ProductUnit.GetPriceByUnit()` | **REMOVE** — use ProductUnitPriceService |

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
| 📦 **Batches** | PurchaseLot breakdown (read-only list) |
| 📈 **History** | ProductPriceHistory (read-only list) |

**Properties to add**:
```csharp
public string? Name { get; set; }
public int? CategoryId { get; set; }
public string? Description { get; set; }
public bool HasExpiry { get; set; }
public bool TrackBatches { get; set; } // Always true, disabled
public decimal MinStockLevel { get; set; }
public decimal ReorderLevel { get; set; }
public ObservableCollection<ProductUnitRowViewModel> Units { get; }
public ObservableCollection<ProductUnitPriceDto> Prices { get; }
public ObservableCollection<ProductImageDto> Images { get; }
public string? NewBarcode { get; set; }
```

**INotifyDataErrorInfo validation** (RULE-228):
- Name required (max 150)
- At least 1 base unit + 1 derived unit required
- BaseConversionFactor > 1 for derived units
- MinStockLevel >= 0

**Interactive Validation** (RULE-059): Save always enabled, validate on click:
```csharp
private bool Validate()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Name))
        errors.Add("• اسم المنتج مطلوب");
    if (!Units.Any(u => u.IsBaseUnit))
        errors.Add("• يجب إضافة وحدة أساسية واحدة على الأقل");
    if (!Units.Any(u => !u.IsBaseUnit && u.ConversionFactor > 1))
        errors.Add("• يجب إضافة وحدة إضافية واحدة على الأقل (مثل كرتون)");
    
    if (errors.Any())
    {
        _ = _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
        RequestFocusFirstInvalidField();
        return false;
    }
    return true;
}
```

**Estimate**: ~6 hours (largest single task)

---

### Task 11.1 — Add Opening Stock Section to Product Creation Flow

**Problem**: Analysis Part 3 (lines 1663-1695) requires that when creating a new product, users can enter an opening quantity to create the initial stock balance immediately. This must create a `PurchaseLot` with `IsOpeningBatch = true` and an `InventoryMovement` record.

**Design**:
- Add an "Opening Stock" expandable section to the Product Editor (Tab 1: Basic Info or a dedicated area)
- Fields: Warehouse (dropdown), Quantity (decimal(18,3)), Unit Cost (decimal(18,2)), Expiry Date (if HasExpiry)
- Optional: user can skip and add stock later
- On save: create `PurchaseLot` with `IsOpeningBatch = true` + `InventoryMovement`

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Products/ProductEditorViewModel.cs` | Add opening stock properties + save logic |
| `Views/Products/ProductEditorView.xaml` | Add "Opening Stock" expandable section |
| `Application/Services/ProductService.cs` | Add `CreateOpeningStockBatchAsync()` method |
| `Contracts/DTOs/AllDtos.cs` | Add `OpeningStockDto` for the UI binding |
| `Contracts/Requests/ProductRequests.cs` | Add optional `OpeningStock` to `CreateProductRequest` |

**ViewModel properties**:
```csharp
private bool _hasOpeningStock;
private int? _openingWarehouseId;
private decimal _openingQuantity;
private decimal _openingUnitCost;
private DateTime? _openingExpiryDate;
private ObservableCollection<WarehouseDto> _warehouses = new();

// Properties with INotifyDataErrorInfo validation:
public bool HasOpeningStock { get; set; ... }
public int? OpeningWarehouseId { get; set; ... }  // Required if HasOpeningStock
public decimal OpeningQuantity { get; set; ... }    // > 0 if HasOpeningStock
public decimal OpeningUnitCost { get; set; ... }    // >= 0 if HasOpeningStock
public DateTime? OpeningExpiryDate { get; set; ... } // Only if HasExpiry
public ObservableCollection<WarehouseDto> Warehouses { get; set; }

// Load warehouses in constructor:
_ = LoadWarehousesAsync();
```

**OpeningStockDto**:
```csharp
public record OpeningStockDto(
    int? WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    DateTime? ExpiryDate
);
```

**Update CreateProductRequest**:
```csharp
public record CreateProductRequest(
    string Name,
    int? CategoryId,
    string? Description,
    bool HasExpiry,
    decimal MinStockLevel,
    decimal ReorderLevel,
    OpeningStockDto? OpeningStock = null,  // NEW — optional
    List<CreateProductUnitDto> Units,
    List<CreateProductUnitPriceDto>? Prices = null
);
```

**Service method — CreateOpeningStockBatchAsync()**:
```csharp
public async Task<Result<PurchaseLot>> CreateOpeningStockBatchAsync(
    Product product, int productUnitId, OpeningStockDto dto, int? createdByUserId, CancellationToken ct)
{
    // Validate warehouse exists
    var warehouse = await _uow.Warehouses.GetByIdAsync(dto.WarehouseId!.Value, ct);
    if (warehouse == null)
        return Result<PurchaseLot>.Failure("المستودع غير موجود", ErrorCodes.NotFound);

    // Generate lot number for opening batch
    var lotNumber = $"OPN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    var purchaseLot = PurchaseLot.Create(
        productId: product.Id,
        productUnitId: productUnitId,
        warehouseId: dto.WarehouseId.Value,
        lotNumber: lotNumber,
        quantity: dto.Quantity,
        unitCost: dto.UnitCost,
        expiryDate: dto.ExpiryDate,
        receivedDate: DateTime.UtcNow,
        purchaseInvoiceId: null,
        isOpeningBatch: true,
        createdByUserId: createdByUserId
    );

    await _uow.PurchaseLots.AddAsync(purchaseLot, ct);

    // Record inventory movement
    var movement = InventoryMovement.Create(
        productId: product.Id,
        warehouseId: dto.WarehouseId.Value,
        movementType: MovementType.PurchaseIn,  // Opening = initial stock-in
        quantityChange: dto.Quantity,
        quantityBefore: 0m,
        quantityAfter: dto.Quantity,
        referenceType: "Product",
        referenceId: product.Id,
        description: $"رصيد افتتاحي للمنتج: {product.Name}",
        createdByUserId: createdByUserId
    );

    await _uow.InventoryMovements.AddAsync(movement, ct);

    // Update warehouse stock
    var stock = await _uow.WarehouseStocks.GetAsync(product.Id, dto.WarehouseId.Value, ct)
                ?? WarehouseStock.Create(product.Id, dto.WarehouseId.Value, 0m);
    stock.AddStock(dto.Quantity);
    if (stock.Id == 0)
        await _uow.WarehouseStocks.AddAsync(stock, ct);

    _logger.LogInformation(
        "Opening stock batch created for Product {ProductId}: Lot={LotNumber}, Qty={Qty}, Cost={Cost}, Warehouse={WarehouseId}",
        product.Id, lotNumber, dto.Quantity, dto.UnitCost, dto.WarehouseId);

    return Result<PurchaseLot>.Success(purchaseLot);
}
```

**Integration into ProductService.CreateAsync()**:
```csharp
// After product + units are saved and have IDs:
if (request.OpeningStock?.WarehouseId.HasValue == true && request.OpeningStock.Quantity > 0)
{
    var baseUnit = product.GetBaseUnit();
    var batchResult = await CreateOpeningStockBatchAsync(
        product,
        baseUnit.Id,
        request.OpeningStock,
        request.CreatedByUserId,
        ct);
    if (!batchResult.IsSuccess)
        return Result<ProductDto>.Failure(batchResult.Error!);
}
```

**XAML section — Opening Stock (expandable, placed after basic info)**:
```xml
<!-- Opening Stock Section -->
<Expander Header="📦 رصيد افتتاحي (اختياري)" 
          IsExpanded="{Binding HasOpeningStock}"
          Margin="0,8,0,0"
          ToolTip="إدخال الكمية الافتتاحية للمنتج — يمكن تخطي هذه الخطوة وإضافة المخزون لاحقاً">
    <StackPanel Margin="0,6,0,0">
        <TextBlock Text="أدخل الرصيد الافتتاحي للمنتج — سيتم إنشاء دفعة مخزون أولية"
                   Style="{StaticResource HelperTextStyle}" Margin="0,0,0,6"/>

        <TextBlock Text="المستودع *" Style="{StaticResource LabelStyle}"/>
        <ComboBox ItemsSource="{Binding Warehouses}" 
                  SelectedValue="{Binding OpeningWarehouseId}"
                  DisplayMemberPath="Name" SelectedValuePath="Id"
                  Style="{StaticResource ModernComboBox}" Margin="0,0,0,6"
                  ToolTip="اختر المستودع الذي سيتم إضافة الرصيد الافتتاحي إليه"/>

        <Grid Margin="0,0,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,4,0">
                <TextBlock Text="الكمية *" Style="{StaticResource LabelStyle}"/>
                <TextBox Text="{Binding OpeningQuantity, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource ModernTextBox}"
                         ToolTip="الكمية الافتتاحية للمنتج في المخزون"/>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="4,0,0,0">
                <TextBlock Text="تكلفة الوحدة *" Style="{StaticResource LabelStyle}"/>
                <TextBox Text="{Binding OpeningUnitCost, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource ModernTextBox}"
                         ToolTip="تكلفة شراء الوحدة — ستُستخدم لحساب متوسط التكلفة"/>
            </StackPanel>
        </Grid>

        <!-- Expiry Date (only if HasExpiry is true) -->
        <StackPanel Visibility="{Binding HasExpiry, Converter={StaticResource BoolToVisibility}}"
                    Margin="0,6,0,0">
            <TextBlock Text="تاريخ انتهاء الصلاحية" Style="{StaticResource LabelStyle}"/>
            <DatePicker SelectedDate="{Binding OpeningExpiryDate}"
                        Style="{StaticResource ModernDatePicker}"
                        ToolTip="اختر تاريخ انتهاء الصلاحية للدفعة الافتتاحية"/>
        </StackPanel>
    </StackPanel>
</Expander>
```

**Validation in ViewModel.Validate()**:
```csharp
if (HasOpeningStock)
{
    if (!OpeningWarehouseId.HasValue)
        errors.Add("• المستودع مطلوب للرصيد الافتتاحي");
    if (OpeningQuantity <= 0)
        errors.Add("• الكمية الافتتاحية يجب أن تكون أكبر من صفر");
    if (OpeningUnitCost < 0)
        errors.Add("• تكلفة الوحدة لا يمكن أن تكون سالبة");
    if (HasExpiry && !OpeningExpiryDate.HasValue && OpeningQuantity > 0)
        errors.Add("• تاريخ انتهاء الصلاحية مطلوب للمنتجات ذات تاريخ انتهاء");
}
```

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

**XAML structure**:
```xml
<!-- ProductEditorView.xaml — Tab-based layout -->
<Grid Margin="10">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- Header -->
    <Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
        <TextBlock Text="{Binding HeaderText}" FontSize="14" FontWeight="Bold" Foreground="White"/>
    </Border>

    <!-- Error message bar -->
    <Border Grid.Row="1" Background="#FFE0E0" Padding="8,4" 
            Visibility="{Binding ErrorMessage, Converter={StaticResource StringNotEmptyToVisibility}}">
        <TextBlock Text="{Binding ErrorMessage}" Foreground="#C62828" FontSize="12"/>
    </Border>

    <!-- Tab Control -->
    <TabControl Grid.Row="2" Margin="0,8,0,0">
        <!-- Tab 1: Basic Info -->
        <TabItem Header="📋 معلومات أساسية">
            <ScrollViewer>
                <StackPanel Margin="0,6,0,0">
                    <TextBlock Text="اسم المنتج *" Style="{StaticResource LabelStyle}"/>
                    <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
                             ToolTip="أدخل اسم المنتج — هذا الحقل إلزامي"
                             Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"/>
                    
                    <TextBlock Text="التصنيف" Style="{StaticResource LabelStyle}"/>
                    <ComboBox ItemsSource="{Binding Categories}" 
                              SelectedValue="{Binding CategoryId}"
                              DisplayMemberPath="Name" SelectedValuePath="Id"
                              ToolTip="اختر تصنيف المنتج — التصنيف الافتراضي 'عام'"
                              Style="{StaticResource ModernComboBox}" Margin="0,0,0,6"/>

                    <TextBlock Text="الوصف" Style="{StaticResource LabelStyle}"/>
                    <TextBox Text="{Binding Description}" AcceptsReturn="True"
                             ToolTip="وصف اختياري للمنتج"
                             Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"
                             MinHeight="60"/>

                    <CheckBox Content="له تاريخ انتهاء" IsChecked="{Binding HasExpiry}"
                              ToolTip="تفعيل تتبع تواريخ انتهاء الصلاحية — سيعمل النظام بـ FEFO لهذا المنتج"
                              Margin="0,0,0,4"/>

                    <Grid Margin="0,6,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" Margin="0,0,4,0">
                            <TextBlock Text="أقل كمية" Style="{StaticResource LabelStyle}"/>
                            <TextBox Text="{Binding MinStockLevel}"
                                     ToolTip="الحد الأدنى للمخزون — عند الوصول لهذه الكمية سيظهر تنبيه"
                                     Style="{StaticResource ModernTextBox}"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="4,0,0,0">
                            <TextBlock Text="حد إعادة الطلب" Style="{StaticResource LabelStyle}"/>
                            <TextBox Text="{Binding ReorderLevel}"
                                     ToolTip="كمية إعادة الطلب الموصى بها"
                                     Style="{StaticResource ModernTextBox}"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </ScrollViewer>
        </TabItem>

        <!-- Tab 2: Units -->
        <TabItem Header="📐 الوحدات">
            <!-- ProductUnitBuilder content -->
        </TabItem>

        <!-- Tab 3: Pricing -->
        <TabItem Header="💰 الأسعار">
            <!-- Per-unit, per-currency, per-level grid -->
        </TabItem>

        <!-- Tab 4: Images -->
        <TabItem Header="🖼️ الصور">
            <!-- Image upload + gallery -->
        </TabItem>

        <!-- Tab 5: Batches (read-only) -->
        <TabItem Header="📦 الدفعات" IsEnabled="{Binding IsExistingProduct}">
            <!-- PurchaseLot breakdown -->
        </TabItem>
    </TabControl>

    <!-- Footer buttons -->
    <Border Grid.Row="3" Background="White" Padding="12,8" 
            BorderThickness="0,1,0,0" BorderBrush="{StaticResource BorderBrush}">
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
            <Button Content="💾 حفظ" Command="{Binding SaveCommand}"
                    Style="{StaticResource PrimaryButton}"
                    ToolTip="حفظ المنتج مع جميع البيانات المدخلة"/>
            <Button Content="❌ إلغاء" Command="{Binding CancelCommand}"
                    Style="{StaticResource SecondaryButton}"
                    ToolTip="إلغاء التعديل والعودة"/>
        </StackPanel>
    </Border>
</Grid>
```

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

**New DataGrid columns**:
```xml
<DataGridTextColumn Header="التكلفة" Binding="{Binding AvgCost, StringFormat={}{0:N2}}"/>
<DataGridTextColumn Header="الكمية" Binding="{Binding StockQuantity, StringFormat={}{0:N3}}"/>
<DataGridTextColumn Header="آخر بيع" Binding="{Binding LastSalePrice, StringFormat={}{0:N2}}"/>
<DataGridTextColumn Header="الهامش" Binding="{Binding ProfitMargin, StringFormat={}{0:P1}}"/>
```

**Search enhancement**: Search by name, barcode, category name, unit name:
```csharp
var searchVal = SearchText?.Trim();
if (!string.IsNullOrWhiteSpace(searchVal))
{
    Products = new ObservableCollection<ProductDto>(
        allProducts.Where(p =>
            p.Name.Contains(searchVal, StringComparison.OrdinalIgnoreCase) ||
            (p.Barcode != null && p.Barcode.Contains(searchVal)) ||
            (p.CategoryName != null && p.CategoryName.Contains(searchVal, StringComparison.OrdinalIgnoreCase))));
}
```

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
| `Api/Validators/ProductRequests/` | NEW validators for CreateProductRequest (updated), CreateProductUnitPriceRequest |
| `Api/Validators/PurchaseLotValidators/` | NEW validators for batch operations |
| `Api/Validators/AssemblyValidators/` | NEW validators for BillOfMaterials requests |

**Validator examples**:
```csharp
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MaximumLength(150).WithMessage("الاسم لا يتجاوز 150 حرفاً");
        RuleFor(x => x.MinStockLevel).GreaterThanOrEqualTo(0)
            .WithMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");
    }
}

public class CreateProductUnitPriceRequestValidator : AbstractValidator<CreateProductUnitPriceRequest>
{
    public CreateProductUnitPriceRequestValidator()
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0)
            .WithMessage("السعر لا يمكن أن يكون سالباً");
        RuleFor(x => x.PriceLevel).IsInEnum()
            .WithMessage("مستوى السعر غير صالح");
    }
}
```

**Estimate**: ~1.5 hours

---

### Task 16 — API Endpoint Enhancements

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/ProductsController.cs` | Add batch breakdown, price history, image upload endpoints |
| `Api/Controllers/ProductUnitsController.cs` | Add pricing endpoints |
| NEW: `Api/Controllers/BatchesController.cs` | PurchaseLot CRUD + FIFO allocation |
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
| GET | `/api/v1/batches` | List PurchaseLots with filters |
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
| 3 | `CreateProductUnitPrices` | Requires ProductUnits table + Currencies table (Phase 20) |
| 4 | `CreateProductImages` | Requires Products table |
| 5 | `CreatePurchaseLots` | Requires Products + Warehouses + ProductUnits tables |
| 6 | `CreatePurchaseLotAllocations` | Requires PurchaseLots table |
| 7 | `CreateBillOfMaterials` | Requires Products table |

**Data preservation**: All migrations are additive — no data loss. Legacy columns are kept in tables, not dropped.

**⚠️ Migration 3 (ProductUnitPrices) depends on Phase 20 (Currencies)**:
- If Phase 20 is not yet complete: create a placeholder Currency with Id=1 (Base Currency) in a data seed
- Mark `CurrencyId` FK as optional initially (`int?`), change to required in Phase 20

**Rollback**: Each migration can be reversed individually:
```sql
-- Reverse migration 2:
ALTER TABLE Products DROP COLUMN AvgCost, HasExpiry, TrackBatches;
-- Reverse migration 3:
DROP TABLE ProductUnitPrices;
-- Reverse migration 5:
DROP TABLE PurchaseLotAllocations;
DROP TABLE PurchaseLots;
```

**Estimate**: Migration creation + testing: ~2 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | ProductUnitPrice.Price, PurchaseLot.UnitCost, Product.AvgCost | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | PurchaseLot.Quantity, BillOfMaterials.QuantityRequired, Product.MinStockLevel | ✅ |
| **RULE-003** | Multi-table ops in transaction | FIFO allocation + stock deduction + InventoryMovement in single transaction | ✅ |
| **RULE-006** | ALL services return `Result<T>` | IFifoAllocationService, IProductUnitPriceService, IAssemblyService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All new entities | ✅ |
| **RULE-016** | BaseEntity audit fields | All new entities inherit BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | All new services | ✅ |
| **RULE-029** | EVERY stock change = InventoryMovement | FIFO allocation + product image + assembly production | ✅ |
| **RULE-035** | Serilog for logging | All services | ✅ |
| **RULE-036** | Log critical operations | Batch creation, price changes, assembly production | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All new controllers | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | PurchaseLot.DeductQuantity(), PurchaseLot.AddReturnQuantity(), ProductUnitPrice.SetPrice() | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | All new request validators | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Product, Category, ProductUnit | ✅ |
| **RULE-052** | Guard Clauses on all entities | PurchaseLot.Create, BillOfMaterials.Create, ProductImage.Create | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | ProductEditorViewModel — AddError/ClearErrors | ✅ |
| **RULE-059** | Save always enabled, validate on click | ProductEditorViewModel — no CanExecute blocking | ✅ |
| **RULE-060** | ProductUnit conversion factor | ProductUnit.BaseConversionFactor | ✅ |
| **RULE-061** | Base unit factor = 1 | ProductUnit.IsBaseUnit enforced (CHK constraint) | ✅ |
| **RULE-062** | Derived units factor > 1 | ProductUnit.CreateDerivedUnit guard | ✅ |
| **RULE-063** | UnitBarcode stores ALL barcodes | UnitBarcode entity — barcode per unit | ✅ |
| **RULE-064** | SmartUnitFormatter selects display unit | UI only — deferred | ⏳ |
| **RULE-065** | Pricing per ProductUnit (not Product) | ProductUnitPrice entity with ProductUnitId | ✅ |
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
| **RULE-084** | ProductPriceHistory records EVERY price change | Extended with CurrencyId + PriceLevel | ✅ |
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
| **RULE-210** | CHECK constraints at DB level | `CHK_PurchaseLots_Quantity_NonNegative`, `CHK_ProductUnitPrices_Price_NonNegative` | ✅ |
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
| **Batch data loss during FIFO migration** | **HIGH** — existing products have no PurchaseLots | Medium | Create opening batches for existing stock at time of migration with `IsOpeningBatch = true` and cost = current AvgCost |
| **Pricing inconsistency after deprecation** | **HIGH** — SalesPrice/WholesalePrice still read by old code | High | Keep legacy columns populated via sync method during transition period; remove only after all consumers updated |
| **FIFO migration complexity** | **HIGH** — requires new tables + algorithm + validation | Medium | Build FIFO as Option A (alongside WA); extensive unit tests before integration |
| **CurrencyId FK dependency on Phase 20** | **HIGH** — ProductUnitPrice cannot be built without Currencies | High | Seed a base currency (Id=1) as part of Phase 25 if Phase 20 isn't ready; mark FK as int? until Phase 20 |
| **ProductUnitPrice falling back to wrong currency** | **MEDIUM** — pricing errors in multi-currency invoices | Low | Implement strict fallback chain: exact match → retail price for currency → base currency conversion → manual entry |
| **Assembly production consuming wrong batches** | **MEDIUM** — incorrect COGS | Low | Assembly always calls `IFifoAllocationService.DeductFromBatchesAsync()` — same FIFO algorithm as sales |
| **Image upload failing silently** | **MEDIUM** — user thinks image saved | Medium | Validate file size (max 5MB) + format (JPG/PNG) + return clear error message |
| **MinStockAlert flooding EventBus** | **LOW** — UI performance degradation | Low | Batch alerts into single message per warehouse; throttle to max 1 alert per product per day |
| **Legacy field deprecation breaking existing code** | **MEDIUM** — compilation errors | Medium | Use `[Obsolete]` attribute with `error: false` first; change to `error: true` in next phase |
| **Barcode uniqueness violation after migration** | **MEDIUM** — duplicate barcodes across units | Low | Query all UnitBarcodes before auto-generating to ensure uniqueness; add DB unique index on BarcodeValue |
| **Product editor becoming too complex (6 tabs)** | **LOW** — user confusion | Low | Tab labels use clear Arabic names with ⓘ help icons; default to Basic Info tab on open |
| **PriceLevel not understood by users** | **LOW** — users ignore VIP/Distributor levels | Low | Default to Retail + Wholesale only in UI; VIP/Distributor are expandable sections |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **PurchaseLot migration causes issues** | `DROP TABLE PurchaseLotAllocations; DROP TABLE PurchaseLots;` |
| **ProductUnitPrice migration causes issues** | `DROP TABLE ProductUnitPrices;` |
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
| `Tests/SalesSystem.Domain.Tests/Entities/ProductUnitPriceTests.cs` | **NEW** — pricing entity guards |
| `Tests/SalesSystem.Domain.Tests/Entities/PurchaseLotTests.cs` | **NEW** — batch creation + deduction tests |
| `Tests/SalesSystem.Domain.Tests/Entities/BillOfMaterialsTests.cs` | **NEW** — BOM guards |
| `Tests/SalesSystem.Application.Tests/Services/ProductServiceTests.cs` | **NEW** — service layer + FIFO + opening stock |
| `Tests/SalesSystem.Application.Tests/Services/FifoAllocationServiceTests.cs` | **NEW** — FIFO/FEFO allocation tests |
| `Tests/SalesSystem.Application.Tests/Services/ProductUnitPriceServiceTests.cs` | **NEW** — pricing fallback chain |
| `Tests/SalesSystem.Application.Tests/Services/AssemblyServiceTests.cs` | **NEW** — production flow |
| `Tests/SalesSystem.Api.Tests/Validators/ProductRequestValidatorTests.cs` | **NEW** — validation rules |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/ProductConfigurationTests.cs` | **NEW** — EF config tests |
| `Tests/SalesSystem.Infrastructure.Tests/Configurations/PurchaseLotConfigurationTests.cs` | **NEW** — batch config tests |

---

#### 19.1 — Domain Entity Tests: Product.Create()

Test `Create()` factory and all 7 sub-module validations:

```csharp
public class ProductTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesProductCorrectly()
    {
        var product = Product.Create(
            name: "منتج تجربة",
            categoryId: 1,
            minStockLevel: 5m,
            reorderLevel: 10m,
            description: "منتج للاختبار",
            hasExpiry: true,
            trackBatches: true,
            createdByUserId: 1);

        Assert.Equal("منتج تجربة", product.Name);
        Assert.Equal(1, product.CategoryId);
        Assert.Equal(5m, product.MinStockLevel);
        Assert.Equal(10m, product.ReorderLevel);
        Assert.Equal("منتج للاختبار", product.Description);
        Assert.True(product.HasExpiry);
        Assert.True(product.TrackBatches);
        Assert.Equal(1, product.CreatedByUserId);
        Assert.True(product.IsActive);
        Assert.Equal(0m, product.AvgCost);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create(""));
        Assert.Contains("اسم المنتج مطلوب", ex.Message);
    }

    [Fact]
    public void Create_WithNullName_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create(null!));
        Assert.Contains("اسم المنتج مطلوب", ex.Message);
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ThrowsDomainException()
    {
        var longName = new string('ا', 151);
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create(longName));
        Assert.Contains("الاسم لا يمكن أن يتجاوز 150 حرف", ex.Message);
    }

    [Fact]
    public void Create_WithNegativeMinStockLevel_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create("منتج", minStockLevel: -1m));
        Assert.Contains("الحد الأدنى للمخزون لا يمكن أن يكون سالباً", ex.Message);
    }

    [Fact]
    public void Create_WithZeroMinStockLevel_IsAllowed()
    {
        var product = Product.Create("منتج", minStockLevel: 0m);
        Assert.Equal(0m, product.MinStockLevel);
    }

    [Fact]
    public void Create_WithoutCategoryId_SetsNull()
    {
        var product = Product.Create("منتج");
        Assert.Null(product.CategoryId);
    }

    [Fact]
    public void Create_WithHasExpiryFalse_SetsCorrectly()
    {
        var product = Product.Create("منتج", hasExpiry: false);
        Assert.False(product.HasExpiry);
    }

    [Fact]
    public void Create_DefaultsToTrackBatchesTrue()
    {
        var product = Product.Create("منتج");
        Assert.True(product.TrackBatches);
    }

    [Fact]
    public void Update_ChangesAllowedFields()
    {
        var product = Product.Create("منتج قديم", categoryId: 1);
        product.Update("منتج محدث", categoryId: 2, "وصف جديد",
            10m, 20m, true, true, updatedByUserId: 2);

        Assert.Equal("منتج محدث", product.Name);
        Assert.Equal(2, product.CategoryId);
        Assert.Equal("وصف جديد", product.Description);
        Assert.Equal(10m, product.MinStockLevel);
        Assert.Equal(20m, product.ReorderLevel);
        Assert.True(product.HasExpiry);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsDomainException()
    {
        var product = Product.Create("منتج");
        var ex = Assert.Throws<DomainException>(() =>
            product.Update("", null, null, 0m, 0m, false, true, null));
        Assert.Contains("اسم المنتج مطلوب", ex.Message);
    }

    // ─── Units Management ─────────────────────────────────────────

    [Fact]
    public void AddUnit_WithValidUnit_AddsToCollection()
    {
        var product = Product.Create("منتج");
        var unit = ProductUnit.CreateBaseUnit(product.Id, "حبة", 1m, createdByUserId: 1);
        product.AddUnit(unit);

        Assert.Single(product.Units);
        Assert.Contains(unit, product.Units);
    }

    [Fact]
    public void RemoveUnit_WithLastUnit_ThrowsDomainException()
    {
        var product = Product.Create("منتج");
        var unit = ProductUnit.CreateBaseUnit(product.Id, "حبة", 1m, createdByUserId: 1);
        product.AddUnit(unit);

        var ex = Assert.Throws<DomainException>(() =>
            product.RemoveUnit(unit));
        Assert.Contains("يجب أن يحتوي المنتج على وحدة واحدة على الأقل", ex.Message);
    }

    [Fact]
    public void RemoveUnit_WhenMultipleUnits_RemovesSuccessfully()
    {
        var product = Product.Create("منتج");
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "حبة", 1m, createdByUserId: 1);
        var derivedUnit = ProductUnit.CreateDerivedUnit(product.Id, "كرتون", 24m, createdByUserId: 1);
        product.AddUnit(baseUnit);
        product.AddUnit(derivedUnit);

        product.RemoveUnit(derivedUnit);

        Assert.Single(product.Units);
        Assert.DoesNotContain(derivedUnit, product.Units);
    }

    [Fact]
    public void GetBaseUnit_ReturnsCorrectUnit()
    {
        var product = Product.Create("منتج");
        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, "حبة", 1m, createdByUserId: 1);
        var derivedUnit = ProductUnit.CreateDerivedUnit(product.Id, "كرتون", 24m, createdByUserId: 1);
        product.AddUnit(baseUnit);
        product.AddUnit(derivedUnit);

        var result = product.GetBaseUnit();
        Assert.Equal(baseUnit, result);
        Assert.True(result.IsBaseUnit);
    }

    [Fact]
    public void GetBaseUnit_WhenNoBaseUnit_ThrowsDomainException()
    {
        var product = Product.Create("منتج");
        var unit = ProductUnit.CreateDerivedUnit(product.Id, "كرتون", 24m, createdByUserId: 1);
        product.AddUnit(unit);

        Assert.Throws<DomainException>(() => product.GetBaseUnit());
    }
}
```

**Estimate**: ~2 hours

---

#### 19.2 — Domain Entity Tests: ProductUnit + ProductUnitPrice + PurchaseLot

```csharp
public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_SetsIsBaseUnitTrue()
    {
        var unit = ProductUnit.CreateBaseUnit(1, "حبة", 1m, createdByUserId: 1);
        Assert.Equal(1, unit.ProductId);
        Assert.Equal("حبة", unit.UnitName);
        Assert.Equal(1m, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
        Assert.Equal(1, unit.SortOrder);
    }

    [Fact]
    public void CreateDerivedUnit_WithFactorGreaterThanOne_SetsCorrectly()
    {
        var unit = ProductUnit.CreateDerivedUnit(1, "كرتون", 24m, sortOrder: 2, createdByUserId: 1);
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

public class ProductUnitPriceTests
{
    [Fact]
    public void Create_SetsAllFieldsCorrectly()
    {
        var price = ProductUnitPrice.Create(
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
            ProductUnitPrice.Create(1, 1, PriceLevel.Retail, -10m, createdByUserId: 1));
        Assert.Contains("السعر لا يمكن أن يكون سالباً", ex.Message);
    }

    [Fact]
    public void Create_WithZeroPrice_IsAllowed()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 0m, createdByUserId: 1);
        Assert.Equal(0m, price.Price);
    }

    [Fact]
    public void SetPrice_UpdatesPriceAndEffectiveFrom()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        price.SetPrice(75m, 2);

        Assert.Equal(75m, price.Price);
        Assert.Equal(2, price.UpdatedByUserId);
    }

    [Fact]
    public void SetPrice_WithNegativePrice_ThrowsDomainException()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        var ex = Assert.Throws<DomainException>(() =>
            price.SetPrice(-5m, 2));
        Assert.Contains("السعر لا يمكن أن يكون سالباً", ex.Message);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndEffectiveTo()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        price.Deactivate(2);

        Assert.False(price.IsActive);
        Assert.NotNull(price.EffectiveTo);
    }
}

public class PurchaseLotTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesLotCorrectly()
    {
        var lot = PurchaseLot.Create(
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
            PurchaseLot.Create(1, 1, 1, "LOT001", 0m, 25m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("الكمية يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            PurchaseLot.Create(1, 1, 1, "LOT001", -10m, 25m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("الكمية يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithZeroUnitCost_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 0m, null, DateTime.UtcNow, null, false, null));
        Assert.Contains("التكلفة يجب أن تكون أكبر من الصفر", ex.Message);
    }

    [Fact]
    public void Create_WithOpeningBatch_SetsFlag()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "OPN-20260605-001",
            100m, 25m, null, DateTime.UtcNow, null, isOpeningBatch: true, null);
        Assert.True(lot.IsOpeningBatch);
    }

    [Fact]
    public void DeductQuantity_ReducesRemainingQuantity()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(30m);
        Assert.Equal(70m, lot.Quantity);
    }

    [Fact]
    public void DeductQuantity_WithExcessiveAmount_ThrowsDomainException()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        var ex = Assert.Throws<DomainException>(() =>
            lot.DeductQuantity(150m));
        Assert.Contains("الكمية المطلوبة أكبر من المتاحة في الدفعة", ex.Message);
    }

    [Fact]
    public void AddReturnQuantity_IncreasesRemainingQuantity()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(50m);
        lot.AddReturnQuantity(20m);
        Assert.Equal(70m, lot.Quantity);
    }

    [Fact]
    public void AddReturnQuantity_ExceedingOriginal_ThrowsDomainException()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
        lot.DeductQuantity(50m);
        var ex = Assert.Throws<DomainException>(() =>
            lot.AddReturnQuantity(60m));
        Assert.Contains("لا يمكن إرجاع كمية أكبر من الكمية الأصلية للدفعة", ex.Message);
    }

    [Fact]
    public void IsFullyConsumed_WhenQuantityZero_ReturnsTrue()
    {
        var lot = PurchaseLot.Create(1, 1, 1, "LOT001", 100m, 25m, null, DateTime.UtcNow, null, false, null);
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

```csharp
public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IWarehouseStockRepository> _stockRepoMock;
    private readonly Mock<IPurchaseLotRepository> _lotRepoMock;
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductRepository>();
        _stockRepoMock = new Mock<IWarehouseStockRepository>();
        _lotRepoMock = new Mock<IPurchaseLotRepository>();
        _movementRepoMock = new Mock<IInventoryMovementRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();

        _uowMock.Setup(x => x.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(x => x.WarehouseStocks).Returns(_stockRepoMock.Object);
        _uowMock.Setup(x => x.PurchaseLots).Returns(_lotRepoMock.Object);
        _uowMock.Setup(x => x.InventoryMovements).Returns(_movementRepoMock.Object);

        _service = new ProductService(_uowMock.Object, _loggerMock.Object);
    }

    // ─── CreateAsync Success ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateProductRequest(
            "منتج جديد", categoryId: 1, description: null,
            hasExpiry: false, minStockLevel: 5m, reorderLevel: 10m,
            openingStock: null,
            units: new List<CreateProductUnitDto>
            {
                new("حبة", 1m, true, 1),
                new("كرتون", 24m, false, 2)
            },
            prices: null);

        _productRepoMock.Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("منتج جديد", result.Value.Name);
    }

    // ─── CreateAsync Failure ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithEmptyName_ReturnsFailure()
    {
        var request = new CreateProductRequest("", null, null,
            false, 0m, 0m, null, new List<CreateProductUnitDto>(), null);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("اسم المنتج مطلوب", result.Error);
    }

    // ─── GetByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsDto()
    {
        var product = Product.Create("منتج موجود");
        typeof(Product).GetProperty("Id")!.SetValue(product, 5);

        _productRepoMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _service.GetByIdAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Id);
        Assert.Equal("منتج موجود", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsFailure()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }

    // ─── Transaction Rollback ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_OnException_RollsBackTransaction()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
        _productRepoMock.Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var request = new CreateProductRequest("منتج", null, null,
            false, 0m, 0m, null, new List<CreateProductUnitDto>(), null);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

**Estimate**: ~2 hours

---

#### 19.4 — Opening Stock Batch Service Tests

```csharp
public class ProductServiceOpeningStockTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IWarehouseRepository> _warehouseRepoMock;
    private readonly Mock<IPurchaseLotRepository> _lotRepoMock;
    private readonly Mock<IWarehouseStockRepository> _stockRepoMock;
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _service;

    public ProductServiceOpeningStockTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductRepository>();
        _warehouseRepoMock = new Mock<IWarehouseRepository>();
        _lotRepoMock = new Mock<IPurchaseLotRepository>();
        _stockRepoMock = new Mock<IWarehouseStockRepository>();
        _movementRepoMock = new Mock<IInventoryMovementRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();

        _uowMock.Setup(x => x.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(x => x.Warehouses).Returns(_warehouseRepoMock.Object);
        _uowMock.Setup(x => x.PurchaseLots).Returns(_lotRepoMock.Object);
        _uowMock.Setup(x => x.WarehouseStocks).Returns(_stockRepoMock.Object);
        _uowMock.Setup(x => x.InventoryMovements).Returns(_movementRepoMock.Object);

        _service = new ProductService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOpeningStockBatchAsync_CreatesLotWithIsOpeningBatchTrue()
    {
        var product = Product.Create("منتج", categoryId: 1);
        typeof(Product).GetProperty("Id")!.SetValue(product, 10);
        var baseUnit = ProductUnit.CreateBaseUnit(10, "حبة", 1m, createdByUserId: 1);
        product.AddUnit(baseUnit);

        var warehouse = new Warehouse("المستودع الرئيسي", 1);
        typeof(Warehouse).GetProperty("Id")!.SetValue(warehouse, 5);

        var dto = new OpeningStockDto(WarehouseId: 5, Quantity: 200m, UnitCost: 15m, ExpiryDate: null);

        _warehouseRepoMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        var result = await _service.CreateOpeningStockBatchAsync(
            product, baseUnit.Id, dto, createdByUserId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsOpeningBatch);
        Assert.Equal(200m, result.Value.Quantity);
        Assert.Equal(200m, result.Value.OriginalQuantity);
        Assert.Equal(15m, result.Value.UnitCost);
        Assert.Equal(5, result.Value.WarehouseId);
        Assert.Equal(10, result.Value.ProductId);
        Assert.Null(result.Value.PurchaseInvoiceId);
    }

    [Fact]
    public async Task CreateOpeningStockBatchAsync_CreatesInventoryMovement()
    {
        var product = Product.Create("منتج");
        typeof(Product).GetProperty("Id")!.SetValue(product, 10);
        var baseUnit = ProductUnit.CreateBaseUnit(10, "حبة", 1m, createdByUserId: 1);
        product.AddUnit(baseUnit);

        var warehouse = new Warehouse("مستودع", 1);
        typeof(Warehouse).GetProperty("Id")!.SetValue(warehouse, 5);

        var dto = new OpeningStockDto(WarehouseId: 5, Quantity: 100m, UnitCost: 10m, ExpiryDate: null);
        _warehouseRepoMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        var result = await _service.CreateOpeningStockBatchAsync(
            product, baseUnit.Id, dto, createdByUserId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _movementRepoMock.Verify(x => x.AddAsync(
            It.Is<InventoryMovement>(m =>
                m.ProductId == 10 &&
                m.WarehouseId == 5 &&
                m.MovementType == MovementType.PurchaseIn &&
                m.QuantityChange == 100m &&
                m.QuantityBefore == 0m &&
                m.QuantityAfter == 100m &&
                m.ReferenceType == "Product"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOpeningStockBatchAsync_UpdatesOrCreatesWarehouseStock()
    {
        var product = Product.Create("منتج");
        typeof(Product).GetProperty("Id")!.SetValue(product, 10);
        var baseUnit = ProductUnit.CreateBaseUnit(10, "حبة", 1m, createdByUserId: 1);
        product.AddUnit(baseUnit);

        var warehouse = new Warehouse("مستودع", 1);
        typeof(Warehouse).GetProperty("Id")!.SetValue(warehouse, 5);

        var existingStock = WarehouseStock.Create(10, 5, 50m);
        typeof(WarehouseStock).GetProperty("Id")!.SetValue(existingStock, 1);

        var dto = new OpeningStockDto(WarehouseId: 5, Quantity: 100m, UnitCost: 10m, ExpiryDate: null);
        _warehouseRepoMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _stockRepoMock.Setup(x => x.GetAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        var result = await _service.CreateOpeningStockBatchAsync(
            product, baseUnit.Id, dto, createdByUserId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(150m, existingStock.Quantity); // 50 + 100
    }

    [Fact]
    public async Task CreateOpeningStockBatchAsync_WhenWarehouseNotFound_ReturnsFailure()
    {
        var product = Product.Create("منتج");
        typeof(Product).GetProperty("Id")!.SetValue(product, 10);
        var baseUnit = ProductUnit.CreateBaseUnit(10, "حبة", 1m, createdByUserId: 1);

        _warehouseRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var dto = new OpeningStockDto(WarehouseId: 999, Quantity: 100m, UnitCost: 10m, null);

        var result = await _service.CreateOpeningStockBatchAsync(
            product, baseUnit.Id, dto, createdByUserId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }
}
```

**Estimate**: ~2 hours

---

#### 19.5 — FIFO Allocation Service Tests

```csharp
public class FifoAllocationServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPurchaseLotRepository> _lotRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IWarehouseStockRepository> _stockRepoMock;
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock;
    private readonly Mock<ILogger<FifoAllocationService>> _loggerMock;
    private readonly FifoAllocationService _service;

    public FifoAllocationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _lotRepoMock = new Mock<IPurchaseLotRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _stockRepoMock = new Mock<IWarehouseStockRepository>();
        _movementRepoMock = new Mock<IInventoryMovementRepository>();
        _loggerMock = new Mock<ILogger<FifoAllocationService>>();

        _uowMock.Setup(x => x.PurchaseLots).Returns(_lotRepoMock.Object);
        _uowMock.Setup(x => x.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(x => x.WarehouseStocks).Returns(_stockRepoMock.Object);
        _uowMock.Setup(x => x.InventoryMovements).Returns(_movementRepoMock.Object);

        _service = new FifoAllocationService(_uowMock.Object, _loggerMock.Object);
    }

    // ─── AddPurchaseBatchesAsync ──────────────────────────────────

    [Fact]
    public async Task AddPurchaseBatchesAsync_CreatesLotCorrectly()
    {
        var result = await _service.AddPurchaseBatchesAsync(
            productUnitId: 1, warehouseId: 2, quantity: 100m,
            unitCostBase: 25m, lotNumber: "B-20260605-001",
            expiryDate: null, purchaseInvoiceId: 5,
            isOpeningBatch: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var lot = result.Value[0];
        Assert.Equal(100m, lot.Quantity);
        Assert.Equal(100m, lot.OriginalQuantity);
        Assert.Equal(25m, lot.UnitCost);
        Assert.Equal("B-20260605-001", lot.LotNumber);
        Assert.Equal(5, lot.PurchaseInvoiceId);
        Assert.False(lot.IsOpeningBatch);
    }

    // ─── DeductFromBatchesAsync — FIFO ────────────────────────────

    [Fact]
    public async Task DeductFromBatchesAsync_Fifo_DeductsFromOldestLotFirst()
    {
        var product = Product.Create("منتج", hasExpiry: false);
        typeof(Product).GetProperty("Id")!.SetValue(product, 1);
        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var lot1 = CreateLot(1, 1, 1, "LOT001", 100m, 20m, DateTime.UtcNow.AddDays(-10));
        var lot2 = CreateLot(1, 1, 1, "LOT002", 50m, 25m, DateTime.UtcNow.AddDays(-5));

        _lotRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<PurchaseLot, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<PurchaseLot> { lot1, lot2 });

        var result = await _service.DeductFromBatchesAsync(
            productId: 1, warehouseId: 1, quantityNeeded: 80m,
            salesInvoiceItemId: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        // 80 units taken: 80 from LOT001 (oldest), 0 from LOT002
        Assert.Equal(20m, lot1.Quantity);  // 100 - 80
        Assert.Equal(50m, lot2.Quantity);  // untouched
    }

    [Fact]
    public async Task DeductFromBatchesAsync_Fifo_SpansMultipleLots()
    {
        var product = Product.Create("منتج", hasExpiry: false);
        typeof(Product).GetProperty("Id")!.SetValue(product, 1);
        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var lot1 = CreateLot(1, 1, 1, "LOT001", 50m, 20m, DateTime.UtcNow.AddDays(-10));
        var lot2 = CreateLot(1, 1, 1, "LOT002", 100m, 25m, DateTime.UtcNow.AddDays(-5));

        _lotRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<PurchaseLot, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<PurchaseLot> { lot1, lot2 });

        var result = await _service.DeductFromBatchesAsync(
            productId: 1, warehouseId: 1, quantityNeeded: 120m,
            salesInvoiceItemId: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(0m, lot1.Quantity);   // LOT001 fully consumed (50)
        Assert.Equal(30m, lot2.Quantity);  // LOT002 has 100 - 70 = 30 remaining
    }

    // ─── DeductFromBatchesAsync — FEFO ────────────────────────────

    [Fact]
    public async Task DeductFromBatchesAsync_Fefo_DeductsEarliestExpiryFirst()
    {
        var product = Product.Create("منتج", hasExpiry: true);
        typeof(Product).GetProperty("Id")!.SetValue(product, 1);
        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var lot1 = CreateLot(1, 1, 1, "LOT001", 100m, 20m,
            receivedDate: DateTime.UtcNow.AddDays(-10), expiryDate: DateTime.UtcNow.AddMonths(6));
        var lot2 = CreateLot(1, 1, 1, "LOT002", 100m, 25m,
            receivedDate: DateTime.UtcNow.AddDays(-5), expiryDate: DateTime.UtcNow.AddMonths(3));

        _lotRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<PurchaseLot, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<PurchaseLot> { lot1, lot2 });

        var result = await _service.DeductFromBatchesAsync(
            productId: 1, warehouseId: 1, quantityNeeded: 100m,
            salesInvoiceItemId: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // LOT002 expires first (3 months) → fully consumed
        Assert.Equal(100m, lot1.Quantity); // LOT001 untouched
        Assert.Equal(0m, lot2.Quantity);   // LOT002 fully consumed (100 - 100)
    }

    // ─── Insufficient Stock ────────────────────────────────────────

    [Fact]
    public async Task DeductFromBatchesAsync_WhenInsufficientStock_ReturnsFailure()
    {
        var product = Product.Create("منتج");
        typeof(Product).GetProperty("Id")!.SetValue(product, 1);
        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var lot1 = CreateLot(1, 1, 1, "LOT001", 30m, 20m, DateTime.UtcNow);

        _lotRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<PurchaseLot, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<PurchaseLot> { lot1 });

        var result = await _service.DeductFromBatchesAsync(
            productId: 1, warehouseId: 1, quantityNeeded: 100m,
            salesInvoiceItemId: 10, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("الكمية المتاحة في المخزون غير كافية", result.Error);
    }

    // ─── ReturnToBatchAsync ────────────────────────────────────────

    [Fact]
    public async Task ReturnToBatchAsync_WithValidAllocation_ReturnsToLot()
    {
        var lot = CreateLot(1, 1, 1, "LOT001", 70m, 20m, DateTime.UtcNow);
        lot.DeductQuantity(30m); // consumed 30, remaining 70

        var allocation = PurchaseLotAllocation.Create(1, 10, 30m, 20m);
        typeof(PurchaseLotAllocation).GetProperty("Id")!.SetValue(allocation, 1);

        _lotRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lot);

        var result = await _service.ReturnToBatchAsync(
            allocationId: 1, quantityReturned: 10m,
            salesReturnItemId: 20, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(80m, lot.Quantity); // 70 + 10
    }

    // ─── GetBatchBreakdownAsync ────────────────────────────────────

    [Fact]
    public async Task GetBatchBreakdownAsync_ReturnsAllActiveLots()
    {
        var lot1 = CreateLot(1, 1, 1, "LOT001", 50m, 20m, DateTime.UtcNow);
        var lot2 = CreateLot(1, 1, 1, "LOT002", 30m, 25m, DateTime.UtcNow);

        _lotRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<PurchaseLot, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<PurchaseLot> { lot1, lot2 });

        var result = await _service.GetBatchBreakdownAsync(
            productId: 1, warehouseId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(80m, result.Value.Sum(b => b.RemainingQuantity));
    }

    private static PurchaseLot CreateLot(int productId, int unitId, int warehouseId,
        string lotNo, decimal qty, decimal cost, DateTime receivedDate,
        DateTime? expiryDate = null)
    {
        // Helper to create lots via reflection for test setup
        // (Production code uses PurchaseLot.Create)
        var lot = PurchaseLot.Create(productId, unitId, warehouseId, lotNo,
            qty, cost, expiryDate, receivedDate, null, false, null);
        return lot;
    }
}
```

**Estimate**: ~4 hours

---

#### 19.6 — Pricing Service Tests (Fallback Chain)

```csharp
public class ProductUnitPriceServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IProductUnitPriceRepository> _priceRepoMock;
    private readonly Mock<ICurrencyRepository> _currencyRepoMock;
    private readonly Mock<IProductPriceHistoryRepository> _historyRepoMock;
    private readonly Mock<ILogger<ProductUnitPriceService>> _loggerMock;
    private readonly ProductUnitPriceService _service;

    public ProductUnitPriceServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _priceRepoMock = new Mock<IProductUnitPriceRepository>();
        _currencyRepoMock = new Mock<ICurrencyRepository>();
        _historyRepoMock = new Mock<IProductPriceHistoryRepository>();
        _loggerMock = new Mock<ILogger<ProductUnitPriceService>>();

        _uowMock.Setup(x => x.ProductUnitPrices).Returns(_priceRepoMock.Object);
        _uowMock.Setup(x => x.Currencies).Returns(_currencyRepoMock.Object);
        _uowMock.Setup(x => x.ProductPriceHistories).Returns(_historyRepoMock.Object);

        _service = new ProductUnitPriceService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_ExactMatch_ReturnsPrice()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        _priceRepoMock.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<ProductUnitPrice, bool>>>()))
            .ReturnsAsync(price);

        var result = await _service.GetEffectivePriceAsync(
            productUnitId: 1, currencyId: 1, priceLevel: PriceLevel.Retail,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_WholesaleFallbackToRetail_UsesRetailPrice()
    {
        var retailPrice = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 60m, createdByUserId: 1);
        _priceRepoMock.SetupSequence(x => x.FindAsync(
                It.IsAny<Expression<Func<ProductUnitPrice, bool>>>()))
            .ReturnsAsync((ProductUnitPrice?)null)  // No exact wholesale price
            .ReturnsAsync(retailPrice);              // Found retail price

        var result = await _service.GetEffectivePriceAsync(
            productUnitId: 1, currencyId: 1, priceLevel: PriceLevel.Wholesale,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(60m, result.Value);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_NoPriceFound_ReturnsFailure()
    {
        _priceRepoMock.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<ProductUnitPrice, bool>>>()))
            .ReturnsAsync((ProductUnitPrice?)null);

        var result = await _service.GetEffectivePriceAsync(
            productUnitId: 1, currencyId: 1, priceLevel: PriceLevel.Retail,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("لا يوجد سعر محدد لهذه التركيبة", result.Error);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_WithDifferentCurrency_FallsBack()
    {
        var retailInBaseCurrency = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);

        _priceRepoMock.SetupSequence(x => x.FindAsync(
                It.IsAny<Expression<Func<ProductUnitPrice, bool>>>()))
            .ReturnsAsync((ProductUnitPrice?)null)  // No price in target currency (Id=2)
            .ReturnsAsync(retailInBaseCurrency);     // Found in base currency (Id=1)

        var result = await _service.GetEffectivePriceAsync(
            productUnitId: 1, currencyId: 2, priceLevel: PriceLevel.Retail,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value);
    }

    [Fact]
    public async Task SetPriceAsync_CreatesPriceAndRecordsHistory()
    {
        var result = await _service.SetPriceAsync(
            productUnitId: 1, currencyId: 1, priceLevel: PriceLevel.Retail,
            price: 75m, changedByUserId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _priceRepoMock.Verify(x => x.AddAsync(
            It.Is<ProductUnitPrice>(p =>
                p.ProductUnitId == 1 &&
                p.CurrencyId == 1 &&
                p.PriceLevel == PriceLevel.Retail &&
                p.Price == 75m),
            It.IsAny<CancellationToken>()), Times.Once);
        _historyRepoMock.Verify(x => x.AddAsync(
            It.Is<ProductPriceHistory>(h =>
                h.ChangeType == "PriceUpdate" &&
                h.NewValue == 75m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePriceAsync_UpdatesPriceAndRecordsHistory()
    {
        var price = ProductUnitPrice.Create(1, 1, PriceLevel.Retail, 50m, createdByUserId: 1);
        typeof(ProductUnitPrice).GetProperty("Id")!.SetValue(price, 10);

        _priceRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);

        var result = await _service.UpdatePriceAsync(10, 80m, changedByUserId: 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(80m, price.Price);
        _historyRepoMock.Verify(x => x.AddAsync(
            It.Is<ProductPriceHistory>(h =>
                h.OldValue == 50m && h.NewValue == 80m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePriceAsync_WhenPriceNotFound_ReturnsFailure()
    {
        _priceRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductUnitPrice?)null);

        var result = await _service.UpdatePriceAsync(999, 80m, changedByUserId: 2, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }
}
```

**Estimate**: ~3 hours

---

#### 19.7 — FluentValidation Tests

```csharp
public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator;

    public CreateProductRequestValidatorTests()
    {
        _validator = new CreateProductRequestValidator();
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateProductRequest(
            "منتج صالح", categoryId: 1, description: "اختبار",
            hasExpiry: false, minStockLevel: 5m, reorderLevel: 10m,
            openingStock: null,
            units: new List<CreateProductUnitDto>
            {
                new("حبة", 1m, true, 1),
                new("كرتون", 24m, false, 2)
            });

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var request = new CreateProductRequest("", null, null,
            false, 0m, 0m, null, new List<CreateProductUnitDto>());

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void NegativeMinStockLevel_FailsValidation()
    {
        var request = new CreateProductRequest(
            "منتج", null, null, false, -1m, 0m, null,
            new List<CreateProductUnitDto> { new("حبة", 1m, true, 1) });

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MinStockLevel");
    }

    [Fact]
    public void NoUnits_FailsValidation()
    {
        var request = new CreateProductRequest(
            "منتج", null, null, false, 0m, 0m, null,
            new List<CreateProductUnitDto>());

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}

public class CreateProductUnitPriceRequestValidatorTests
{
    private readonly CreateProductUnitPriceRequestValidator _validator;

    public CreateProductUnitPriceRequestValidatorTests()
    {
        _validator = new CreateProductUnitPriceRequestValidator();
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateProductUnitPriceRequest(
            productUnitId: 1, currencyId: 1,
            priceLevel: PriceLevel.Retail, price: 50m);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NegativePrice_FailsValidation()
    {
        var request = new CreateProductUnitPriceRequest(
            1, 1, PriceLevel.Retail, -10m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void InvalidPriceLevel_FailsValidation()
    {
        var request = new CreateProductUnitPriceRequest(
            1, 1, (PriceLevel)99, 50m);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PriceLevel");
    }
}
```

**Estimate**: ~2 hours

---

#### 19.8 — Database Configuration Tests

```csharp
public class ProductConfigurationTests
{
    [Fact]
    public void ProductConfiguration_HasCorrectTableName()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Product));
        Assert.Equal("Products", entity!.GetTableName());
    }

    [Fact]
    public void Name_HasRequiredMaxLength150()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.Name));
        Assert.False(prop!.IsNullable);
        Assert.Equal(150, prop.GetMaxLength());
    }

    [Fact]
    public void MinStockLevel_HasPrecision18_3()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.MinStockLevel));
        Assert.Equal(18, prop!.GetPrecision());
        Assert.Equal(3, prop.GetScale());
    }

    [Fact]
    public void AvgCost_HasPrecision18_2()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var prop = modelBuilder.Model.FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.AvgCost));
        Assert.Equal(18, prop!.GetPrecision());
        Assert.Equal(2, prop.GetScale());
    }

    [Fact]
    public void CategoryIdFK_UsesDeleteBehaviorRestrict()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Product))!;
        var fk = entity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Category));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk!.DeleteBehavior);
    }

    [Fact]
    public void Product_HasQueryFilterForIsActive()
    {
        var modelBuilder = new ModelBuilder();
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Product))!;
        var queryFilter = entity.GetQueryFilter();
        Assert.NotNull(queryFilter);
    }
}

public class PurchaseLotConfigurationTests
{
    [Fact]
    public void PurchaseLot_TableNameIsCorrect()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PurchaseLot));
        Assert.Equal("PurchaseLots", entity!.GetTableName());
    }

    [Fact]
    public void QuantityField_HasPrecision18_3()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var prop = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!
            .FindProperty(nameof(PurchaseLot.Quantity));
        Assert.Equal(18, prop!.GetPrecision());
        Assert.Equal(3, prop.GetScale());
    }

    [Fact]
    public void UnitCost_HasPrecision18_2()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var prop = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!
            .FindProperty(nameof(PurchaseLot.UnitCost));
        Assert.Equal(18, prop!.GetPrecision());
        Assert.Equal(2, prop.GetScale());
    }

    [Fact]
    public void LotNumber_HasMaxLength50()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var prop = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!
            .FindProperty(nameof(PurchaseLot.LotNumber));
        Assert.Equal(50, prop!.GetMaxLength());
    }

    [Fact]
    public void FK_Warehouse_UsesDeleteBehaviorRestrict()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!;
        var fk = entity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Warehouse));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk!.DeleteBehavior);
    }

    [Fact]
    public void FK_Product_UsesDeleteBehaviorRestrict()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!;
        var fk = entity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Product));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk!.DeleteBehavior);
    }

    [Fact]
    public void HasCheckConstraint_QuantityNonNegative()
    {
        var modelBuilder = new ModelBuilder();
        new PurchaseLotConfiguration().Configure(modelBuilder.Entity<PurchaseLot>());

        var entity = modelBuilder.Model.FindEntityType(typeof(PurchaseLot))!;
        var checkConstraints = entity.GetCheckConstraints();
        Assert.Contains(checkConstraints, c => c.Name == "CHK_PurchaseLots_Quantity_NonNegative");
    }
}

public class ProductUnitPriceConfigurationTests
{
    [Fact]
    public void Price_HasPrecision18_2()
    {
        var modelBuilder = new ModelBuilder();
        new ProductUnitPriceConfiguration().Configure(modelBuilder.Entity<ProductUnitPrice>());

        var prop = modelBuilder.Model.FindEntityType(typeof(ProductUnitPrice))!
            .FindProperty(nameof(ProductUnitPrice.Price));
        Assert.Equal(18, prop!.GetPrecision());
        Assert.Equal(2, prop.GetScale());
    }

    [Fact]
    public void FK_ProductUnit_UsesDeleteBehaviorRestrict()
    {
        var modelBuilder = new ModelBuilder();
        new ProductUnitPriceConfiguration().Configure(modelBuilder.Entity<ProductUnitPrice>());

        var entity = modelBuilder.Model.FindEntityType(typeof(ProductUnitPrice))!;
        var fk = entity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ProductUnit));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Restrict, fk!.DeleteBehavior);
    }
}
```

**Estimate**: ~2.5 hours

---

#### 19.9 — Assembly Service Tests

```csharp
public class AssemblyServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IBillOfMaterialsRepository> _bomRepoMock;
    private readonly Mock<IFifoAllocationService> _fifoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IProductPriceHistoryRepository> _historyRepoMock;
    private readonly Mock<ILogger<AssemblyService>> _loggerMock;
    private readonly AssemblyService _service;

    public AssemblyServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _bomRepoMock = new Mock<IBillOfMaterialsRepository>();
        _fifoMock = new Mock<IFifoAllocationService>();
        _productRepoMock = new Mock<IProductRepository>();
        _historyRepoMock = new Mock<IProductPriceHistoryRepository>();
        _loggerMock = new Mock<ILogger<AssemblyService>>();

        _uowMock.Setup(x => x.BillOfMaterials).Returns(_bomRepoMock.Object);
        _uowMock.Setup(x => x.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(x => x.ProductPriceHistories).Returns(_historyRepoMock.Object);

        _service = new AssemblyService(_uowMock.Object, _fifoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProduceAsync_WithValidComponents_DeductsAndCreatesProduct()
    {
        var assemblyProduct = Product.Create("منتج مُجمَّع");
        typeof(Product).GetProperty("Id")!.SetValue(assemblyProduct, 1);

        var component1 = Product.Create("مكون أ");
        typeof(Product).GetProperty("Id")!.SetValue(component1, 2);
        var component2 = Product.Create("مكون ب");
        typeof(Product).GetProperty("Id")!.SetValue(component2, 3);

        var bom = BillOfMaterials.Create(1, 2, 1, 2m, 0m, null);
        var bom2 = BillOfMaterials.Create(1, 3, 1, 3m, 5m, null);
        var boms = new List<BillOfMaterials> { bom, bom2 };

        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assemblyProduct);
        _bomRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<BillOfMaterials, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(boms);

        _fifoMock.Setup(x => x.DeductFromBatchesAsync(
                2, It.IsAny<int>(), 2m, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PurchaseLotAllocation>>.Success(new List<PurchaseLotAllocation>()));
        _fifoMock.Setup(x => x.DeductFromBatchesAsync(
                3, It.IsAny<int>(), 3.15m, null, It.IsAny<CancellationToken>())) // 3 + 5% waste
            .ReturnsAsync(Result<List<PurchaseLotAllocation>>.Success(new List<PurchaseLotAllocation>()));

        var mockTransaction = new Mock<IDbContextTransaction>();
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.ProduceAsync(
            assemblyProductId: 1, warehouseId: 1, quantity: 10m,
            produceUserId: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _fifoMock.Verify(x => x.DeductFromBatchesAsync(
            2, 1, 20m, null, It.IsAny<CancellationToken>()), Times.Once); // 2 × 10 = 20
        _fifoMock.Verify(x => x.DeductFromBatchesAsync(
            3, 1, 31.5m, null, It.IsAny<CancellationToken>()), Times.Once); // 3.15 × 10 = 31.5
        mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_WhenComponentInsufficient_ReturnsFailure()
    {
        var assemblyProduct = Product.Create("منتج مُجمَّع");
        typeof(Product).GetProperty("Id")!.SetValue(assemblyProduct, 1);
        var component = Product.Create("مكون");
        typeof(Product).GetProperty("Id")!.SetValue(component, 2);

        var bom = BillOfMaterials.Create(1, 2, 1, 5m, 0m, null);
        var boms = new List<BillOfMaterials> { bom };

        _productRepoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assemblyProduct);
        _bomRepoMock.Setup(x => x.FindAllAsync(
                It.IsAny<Expression<Func<BillOfMaterials, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(boms);

        _fifoMock.Setup(x => x.DeductFromBatchesAsync(
                2, 1, 5m, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PurchaseLotAllocation>>.Failure("الكمية المتاحة غير كافية"));

        var mockTransaction = new Mock<IDbContextTransaction>();
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        var result = await _service.ProduceAsync(
            assemblyProductId: 1, warehouseId: 1, quantity: 1m,
            produceUserId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_WhenAssemblyProductNotFound_ReturnsFailure()
    {
        _productRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _service.ProduceAsync(
            999, 1, 1m, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.ErrorCode);
    }
}
```

**Estimate**: ~3 hours

---

#### 19.10 — Execution Summary

| Sub-Task | Focus | Key Test Files | Estimate |
|----------|-------|----------------|----------|
| 19.1 | Product.Create() factory + guards + units | `ProductTests.cs` | 2h |
| 19.2 | ProductUnit, ProductUnitPrice, PurchaseLot, BOM entities | `ProductUnitTests.cs`, `PurchaseLotTests.cs`, `BillOfMaterialsTests.cs` | 3h |
| 19.3 | ProductService CRUD + opening stock batch | `ProductServiceTests.cs` | 2h |
| 19.4 | OpeningStock: PurchaseLot + InventoryMovement + WarehouseStock | `ProductServiceOpeningStockTests.cs` | 2h |
| 19.5 | FIFO/FEFO allocation algorithm + multi-lot spanning | `FifoAllocationServiceTests.cs` | 4h |
| 19.6 | Pricing fallback chain + price history recording | `ProductUnitPriceServiceTests.cs` | 3h |
| 19.7 | FluentValidation: Product, ProductUnitPrice requests | `ProductRequestValidatorTests.cs` | 2h |
| 19.8 | EF Config: precision, maxlength, Restrict, check constraints | `ProductConfigurationTests.cs`, `PurchaseLotConfigurationTests.cs` | 2.5h |
| 19.9 | Assembly production + component deduction | `AssemblyServiceTests.cs` | 3h |
| **Total** | **9 sub-tasks** | **~12 test files** | **~23.5h** |
