# Feature Specification: Products Module — Remaining Work

**Feature Branch**: `028-products-module-complete`  
**Created**: 2026-06-10  
**Status**: Draft  
**Input**: Complete remaining Products Module: Opening Balance, Cost removal, TrackExpiry rename, UnitBarcodes cleanup, Product Import

## User Scenarios & Testing

### User Story 1 — Create Product with Opening Stock (Priority: P1)

An inventory manager creates a new product and enters its opening quantity and unit cost. The system automatically creates a batch record ("Opening Batch") and records a journal entry debiting Inventory and crediting Opening Balance Equity.

**Why this priority**: Without opening stock, users cannot begin using the system with existing inventory. This is the first action needed when onboarding a new product or migrating from another system.

**Independent Test**: Can be fully tested by opening the product creation screen, entering "100 pcs at 50 YER each", saving, and verifying:
- The product appears in the list
- An "OPENING" batch exists with `QuantityRemaining = 100`, `UnitCost = 50`
- Warehouse stock reflects 100 units
- A journal entry exists with Dr Inventory 5000 / Cr OpeningBalanceEquity 5000

**Acceptance Scenarios**:

1. **Given** the user is creating a new product, **When** they enter Name "بطاطس عمان", Category "مواد غذائية", Opening Quantity 100, Opening Cost 50, and click Save, **Then** the product is saved with an "OPENING" batch containing 100 units at cost 50, and stock is updated.
2. **Given** the user is creating a new product, **When** they enter Opening Quantity 0 and leave Opening Cost empty, **Then** the product is saved without creating any batch or journal entry.
3. **Given** the user is creating a new product with opening quantity 100 and cost 50, **When** they save, **Then** a balanced journal entry is created: Dr Inventory (5000) / Cr OpeningBalanceEquity (5000).
4. **Given** the user is editing an existing product, **When** they view the opening fields, **Then** they are disabled/hidden (opening stock is creation-only).

---

### User Story 2 — Product Cost from Batches (Priority: P1)

The system calculates product cost exclusively from `InventoryBatches` using FIFO methodology. The `Product.Cost` column is removed entirely — cost is never stored on the product entity itself.

**Why this priority**: This is a fundamental architectural correction. Storing cost on the Product entity contradicts the FIFO batch tracking design and creates data inconsistency risks.

**Independent Test**: Can be tested by:
1. Creating a product with opening stock (Batch A: 100 units at 50)
2. Adding a purchase receipt (Batch B: 50 units at 60)
3. Verifying current FIFO cost = 50 (oldest batch cost)

**Acceptance Scenarios**:

1. **Given** a product has no batches, **When** the system queries its cost, **Then** cost is returned as 0.
2. **Given** a product has one batch (100 units at 50), **When** the system queries cost, **Then** cost = 50.
3. **Given** a product has multiple batches (Batch A: 100@50, Batch B: 50@60), **When** the system queries current FIFO cost, **Then** cost = 50 (oldest remaining batch).
4. **Given** a product has batches and a partial sale occurs (sold 60 from Batch A), **When** the system queries cost for the next sale, **Then** cost = 50 (remaining 40 from Batch A + 50 from Batch B → FIFO cost is still 50 until Batch A is exhausted).

---

### User Story 3 — Rename HasExpiry to TrackExpiry (Priority: P2)

The product entity field `HasExpiry` is renamed to `TrackExpiry` to better communicate its meaning — it controls whether the system tracks expiration dates for this product (enabling FEFO).

**Why this priority**: Naming consistency. All spec documents and analysis use `TrackExpiry`. The current `HasExpiry` name is misleading (it implies the product inherently has an expiry, not that the system tracks it).

**Independent Test**: Can be tested by:
1. Creating a product with `TrackExpiry = true`
2. Verifying the field appears as "له تاريخ انتهاء؟" in the UI
3. Verifying batch creation respects this flag for FEFO behavior

**Acceptance Scenarios**:

1. **Given** a product has `TrackExpiry = true`, **When** a purchase batch is added with an expiry date, **Then** FEFO (First Expiry First Out) is used for cost allocation.
2. **Given** a product has `TrackExpiry = false`, **When** a purchase batch is added with an expiry date, **Then** standard FIFO is used (expiry is ignored).
3. **Given** the database has existing records with `HasExpiry`, **When** the migration runs, **Then** the column is renamed and all existing data is preserved.

---

### User Story 4 — Remove UnitBarcodes Table (Priority: P2)

The deprecated `UnitBarcodes`/`ProductBarcode` table is removed from the schema. All barcode functionality now uses `Product.Barcode` as the single source of truth.

**Why this priority**: Clean up technical debt. The table exists but is marked `[Obsolete]`. Removing it prevents confusion and reduces maintenance burden.

**Independent Test**: Can be tested by:
1. Verifying the `ProductBarcode` entity class no longer exists
2. Verifying the `UnitBarcodes` table is not created by new migrations
3. Verifying that `Product.Barcode` is the only barcode field

**Acceptance Scenarios**:

1. **Given** the database migration runs, **When** it completes, **Then** the `UnitBarcodes` table no longer exists.
2. **Given** a developer searches for `ProductBarcode` references, **When** looking at the codebase, **Then** no functional references remain (only removed/deleted files).
3. **Given** the system is running, **When** a user searches by barcode, **Then** the search uses `Product.Barcode` exclusively.

---

### User Story 5 — Product Import from Excel (Priority: P3)

An inventory manager imports products in bulk from an Excel file. The import supports: product name, barcode, category, description, TrackExpiry, opening quantity, and opening cost.

**Why this priority**: Bulk import is essential during system initialization and periodic large-scale product additions, but is less critical than core product creation and costing.

**Independent Test**: Can be tested by preparing an Excel file with 10 products, importing it, and verifying all products appear correctly with their opening stock.

**Acceptance Scenarios**:

1. **Given** the user has an Excel file with valid product data, **When** they import it, **Then** all products are created with correct names, categories, and barcodes.
2. **Given** the Excel file includes opening quantity and cost columns, **When** imported, **Then** each product gets an "OPENING" batch and journal entry.
3. **Given** the Excel file has a row with a duplicate barcode, **When** imported, **Then** the duplicate row is reported as an error and skipped.
4. **Given** the user imports a file with missing required fields (name, category), **When** processed, **Then** an import log shows which rows failed and why.

---

### Edge Cases

- **Opening cost is 0**: When opening quantity > 0 but cost = 0, the batch is created with zero cost. No journal entry is created (zero value).
- **Product with opening stock deleted (soft)**: Opening batches remain for audit trail; stock is not affected.
- **Negative opening quantity**: Rejected at validation — opening quantity must be ≥ 0.
- **Multiple products with same name**: Allowed if barcodes differ (barcode is the unique identifier).
- **Excel import with 10,000+ rows**: Handles large files without timeout; shows progress per 100 rows.
- **Opening batch on a product with `TrackExpiry = true`**: Expiry date is required when opening quantity > 0 and TrackExpiry = true.

## Requirements

### Functional Requirements

- **FR-001**: Products module MUST allow entering opening quantity and unit cost when creating a new product.
- **FR-002**: Product creation with opening quantity > 0 MUST automatically create an `InventoryBatch` with `BatchNo = "OPENING"`.
- **FR-003**: Product creation with opening quantity > 0 MUST automatically create a journal entry: Dr Inventory / Cr OpeningBalanceEquity.
- **FR-004**: The `Product` entity MUST NOT have a `Cost` column — product cost MUST be derived from `InventoryBatches` using FIFO.
- **FR-005**: The system MUST expose a cost query method that returns the FIFO cost from the oldest batch with remaining quantity.
- **FR-006**: The `HasExpiry` field on the `Product` entity MUST be renamed to `TrackExpiry` across all layers.
- **FR-007**: The deprecated `UnitBarcodes`/`ProductBarcode` table and entity MUST be removed from the schema and codebase.
- **FR-008**: All barcode lookups MUST use `Product.Barcode` as the single source of truth.
- **FR-009**: The product import feature MUST support Excel files with columns: Name, Barcode, Category, Description, TrackExpiry, OpeningQuantity, OpeningCost.
- **FR-010**: The import MUST validate required fields, detect duplicate barcodes, and generate an import log with per-row success/failure.
- **FR-011**: The import MUST create opening batches and journal entries for rows with opening quantity > 0.
- **FR-012**: Opening stock fields MUST be hidden/disabled when editing an existing product (creation-only).
- **FR-013**: When opening quantity > 0 and cost = 0, the batch is created but no journal entry is generated.
- **FR-014**: When `TrackExpiry = true` and opening quantity > 0, an expiry date is required.

### Key Entities

- **Product**: Core entity. Properties: Id, Name, Barcode, CategoryId, Description, TrackExpiry (renamed from HasExpiry), IsActive, MinStockLevel, ReorderLevel. NO Cost column. NO HasExpiry column.
- **InventoryBatch**: Tracks stock per batch. Properties: Id, ProductId, WarehouseId, BatchNo, ExpiryDate, QuantityReceived, QuantityRemaining, UnitCost, PurchaseInvoiceId, CreatedAt. Opening stock uses `BatchNo = "OPENING"`.
- **ProductUnit**: Unit of measure for a product. Properties: Id, ProductId, UnitId, Factor, IsBaseUnit. Each product has exactly one base unit and at least one additional unit.
- **ProductPrice**: Price per (ProductUnit + Currency) combination. Properties: Id, ProductUnitId, CurrencyId, Price, EffectiveFrom, EffectiveTo, CreatedAt.
- **ProductCategory**: Product grouping. Properties: Id, Name, Description, IsActive.
- **JournalEntry** (existing): Used for recording the opening balance transaction (Dr Inventory / Cr OpeningBalanceEquity).

## Success Criteria

### Measurable Outcomes

- **SC-001**: Inventory managers can create a product with opening stock in under 2 minutes (including data entry).
- **SC-002**: Product cost is always derived from InventoryBatches — zero instances of `Product.Cost` being read anywhere in the system.
- **SC-003**: Zero functional references to `HasExpiry` or `ProductBarcode` remain in the codebase after cleanup.
- **SC-004**: Import of 100 products from Excel completes successfully within 30 seconds.
- **SC-005**: Import error rate for valid data is 0% — all correctly formatted rows are imported without data loss.
- **SC-006**: All existing tests pass after changes (Domain 615+, Application 233+, Infrastructure 170+, DesktopPWF 433+).

## Assumptions

- Opening balance is only available during product creation, not as a post-creation adjustment.
- The default warehouse ("المخزن الرئيسي") is used for opening stock batches — users don't choose a warehouse for opening stock in V1.
- Opening stock always uses the product's base unit for quantity.
- The `OpeningBalanceEquity` account (ID 1422) is already seeded as a system account.
- Barcode is unique across all products — duplicate barcodes are rejected.
- Excel import supports `.xlsx` format (OpenXML) — not legacy `.xls`.
- Product image management is out of scope for this phase (existing images tab remains but no changes).
- `UpdateProductPricingService` costing rewrite (weighted average from batches) is tracked separately in a follow-up phase.
