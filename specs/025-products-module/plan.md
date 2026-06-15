# Implementation Plan: Phase 25 — Products Module

**Phase**: 25 | **Version**: v4.6.9+ | **Dependencies**: Phases 18-24 complete (Accounting, Settings, Currencies, Users, COA, Customers, Accounting Automation)

---

## Summary

Implement the complete Products Module: ProductCategories, Products, Units, ProductUnits, ProductPrices, and opening stock via InventoryBatches. This module replaces the legacy v4.x product system that stored prices, costs, and barcodes directly on the Product entity. The new design is built on a per-unit × per-currency pricing model, cost sourced exclusively from InventoryBatches (FIFO), and a single barcode per product.

**5 screens**: ProductCategories → Products → ProductUnits → ProductPrices → Excel Import

**6 tables**: ProductCategories, Products, Units, ProductUnits, ProductPrices, InventoryBatches

---

## Key Entities

### ProductCategories
Simple classification table independent from AccountCategories. Each product belongs to exactly one category. Category has Name (unique, required) and Description (optional). Used for product grouping in reports and filters. Seeded with a default "عام" category.

### Products
Lean entity — stores only product identity data. Name (required), Barcode (varchar(50), unique filtered when non-null and active), CategoryId (FK, required), TaxId (FK, nullable), Description (optional), TrackExpiry (bit), ReorderLevel (decimal 18,3), ImagePath (optional). **NO cost, NO price, NO stock quantity, NO barcode per unit** — all live in related tables.

### Units
Global lookup table (smallint PK) shared across all products. Seeded with 7 default units: حبة (PCS), كرتون (CTN), علبة (BOX), كيلو (KG), جرام (G), لتر (L), متر (M). IsSystem flag protects seeded units from deletion. Users can add custom units and can disable (soft-delete) units not in use.

### ProductUnits
Junction table connecting a product to a unit with a conversion factor. Exactly one base unit per product (Factor = 1, IsBaseUnit = true). Every product MUST have at least 2 units: one base + one additional. Factor is decimal(18,3). Inventory is stored in base unit quantities only — all conversions happen through this table.

### ProductPrices
The only pricing mechanism — **no RetailPrice/WholesalePrice/PriceLevel columns on Product or ProductUnit**. Price is defined per (ProductUnit × CurrencyId) with EffectiveFrom date and optional EffectiveTo date. This replaces the old dual-price (retail/wholesale) model with a flat per-unit price list that feels natural to small shop users: "حبة = 150 YER" and "كرتون = 3000 YER" are independent prices.

### InventoryBatches
Cost container. Every stock-receiving operation (purchase, opening stock, adjustment) creates a batch record carrying QuantityReceived, QuantityRemaining (decimal 18,3), and UnitCost (decimal 18,2). Cost is NEVER stored on Product. FIFO allocation consumes from the oldest non-zero QuantityRemaining batch at sale time. FEFO (expiry-priority) kicks in when TrackExpiry = true.

---

## Business Rules

1. **Product must be inventory-only in V1** — no service items, no non-stock items. Every product goes in/out of stock and has batches.

2. **Each product needs ≥ 2 units** (one base + one additional). This is enforced at the Domain level by a guard clause on Product creation.

3. **Base unit is mandatory, exactly one per product.** All stock quantities are stored in base unit values internally. Conversion is done by Factor division/multiplication.

4. **Barcode follows the product, not the unit.** In V1, a product has one primary barcode. When scanning at POS, the system identifies the product, then the user selects the unit. Per-unit barcodes (UnitBarcodes) are deferred to a later version.

5. **Prices are set per (ProductUnit × CurrencyId) with effective dates.** No inherited or computed prices. The user explicitly enters the price of each unit in each currency. Price history is retained via versioned rows (new row = new EffectiveFrom).

6. **Cost comes from batches, never from Product.** Weighted average cost is computed from InventoryBatches for reporting. FIFO cost is computed by consuming oldest remaining batches at sale time.

7. **Stock quantity = sum of QuantityRemaining across all batches for that product in that warehouse.** WarehouseStocks is the live balance view; InventoryBatches is the detail.

8. **Opening stock on product creation** creates: an InventoryBatch with BatchNo = "OPENING" (or BatchNo = 0), a WarehouseStock row (warehouse resolved from settings or default), an InventoryTransaction (type OpeningBalance), and a journal entry (Dr Inventory, Cr OpeningBalanceEquity). All wrapped in a single transaction.

9. **Soft delete for products** — set IsActive = false. Cannot soft-delete a product with positive stock or active references in open invoices.

10. **System units (IsSystem = true) cannot be deleted** — only disabled.

---

## Pricing Philosophy

The new pricing model eliminates the concept of "Retail vs Wholesale vs VIP" price levels. Instead, the user defines the price of each unit the way they naturally think:

| Product | Unit | Currency | Price |
|---------|------|----------|-------|
| مياه أروى | حبة | YER | 150 |
| مياه أروى | كرتون | YER | 3,000 |
| مياه أروى | بالة | YER | 95,000 |
| مياه أروى | حبة | USD | 1.00 |
| مياه أروى | كرتون | USD | 20.00 |

This is simpler for small shop users because:
- They don't think in "discount levels" — they think in "what's the price of a carton?"
- A customer may buy 1 piece (حبة) at one price, or a full carton (كرتون) at a different per-unit rate — both are explicit prices, not computed discounts.
- Multi-currency pricing comes naturally: each (unit, currency) pair gets its own price.
- Price changes are tracked via effective-dated rows, not overwrites.

---

## InventoryBatch & Costing

### How batches work
Every stock-receiving event creates one or more InventoryBatch rows:
- **Opening stock** → Batch with PurchaseInvoiceId = null, BatchNo = auto-increment starting from 1 for OPENING batches
- **Purchase invoice** → Batch with PurchaseInvoiceId = the invoice's FK, carrying the purchase unit cost
- **Transfer in** → Batch transferred from source warehouse

### FIFO consumption at sale
When a sales invoice is posted:
1. The sale quantity in base units is allocated from the oldest non-empty batch (by CreatedAt ASC).
2. Each batch's QuantityRemaining is reduced proportionally.
3. COGS is calculated as sum of (qty_from_batch × batch_unit_cost).
4. If TrackExpiry = true, FEFO applies: nearest expiry batch is consumed first.

### Weighted Average (for reporting)
System computes WAC as: `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)` across all active batches per product.

### Journal entries
- **Opening stock**: Dr `المخزون (Inventory Asset)` / Cr `رصيد افتتاحي (Opening Balance Equity)`
- **Purchase**: Dr `المخزون` / Cr `المورد (AP)` or `الصندوق (Cash)`
- **Sale (COGS side)**: Dr `تكلفة البضاعة المباعة (COGS)` / Cr `المخزون`
- **Sale (Revenue side)**: Dr `الصندوق (Cash)` or `العميل (AR)` / Cr `المبيعات (Sales Revenue)`

---

## UI Screens

### 1. ProductCategories List + Editor
Simple CRUD list showing Name and product count per category. Editor has Name (required) and Description (optional). Opens non-modally via ScreenWindowService. Delete guarded against categories that have products.

### 2. Products List + Editor
List shows: Name, Barcode, Category, Stock quantity (from WarehouseStocks), Last cost (from latest batch), Status (Active/Inactive). Sorted newest-first.

Editor is a tabbed or sectioned form:
- **Basic Info tab**: Name (required), Barcode, Category (dropdown, required), Tax (dropdown, optional), Description, TrackExpiry checkbox, ReorderLevel, ImagePath (file picker).
- **Units tab**: Add/remove units with Factor. First unit added becomes the base. Minimum 2 units enforced. Shows base unit indicator.
- **Opening Stock section**: Visible only during Create (hidden during Edit). Fields: Opening Quantity, Base Unit Cost, Expiry Date (if TrackExpiry), Warehouse (resolved from default or user-selectable).

### 3. Units List + Editor
Global units management. List shows Name, Symbol, Status. Seeded units have IsSystem badge. Editor has Name (required), Symbol (optional). Cannot delete in-use or system units — only disable.

### 4. ProductUnits Screen
Opened from within a product editor (sub-screen). Shows grid of: Unit, Factor, IsBaseUnit. Allows adding units from the global Units list and setting the Factor. Warnings if user tries to remove the last unit or change the base unit with existing stock.

### 5. ProductPrices Screen
Opened as a standalone screen under the Products module tab. Shows a filterable list: Product, Unit, Currency, Price, EffectiveFrom, EffectiveTo. User can filter by product or category. Add/edit price rows directly in a DataGrid or via an editor panel. Price history is visible but not editable (versioned).

### 6. Excel Import
Bulk product creation from Excel files using ClosedXML. Two-file approach:
- **File 1 — Products**: columns: Name, Category, BaseUnit, OpeningQty, OpeningCost, ExpiryDate, Barcode, TrackExpiry, ReorderLevel.
- **File 2 — Units & Prices**: columns: ProductName, Unit, Factor, Currency, Price.

Import service validates each row, reports errors per row (Arabic messages), and creates products + units + prices + opening batches in a single transaction per product. Shows progress bar and summary dialog (X created, Y errors).

---

## Tasks (ordered by dependency)

| # | Task | Layer | Depends On |
|---|------|-------|------------|
| 1 | Update Units entity + config (smallint PK, IsSystem) | Domain + Infrastructure | — |
| 2 | Update Product entity (remove Cost/Price, add Barcode/TrackExpiry/ReorderLevel/ImagePath, add TaxId) | Domain | — |
| 3 | Create ProductCategory entity + config | Domain + Infrastructure | — |
| 4 | Create ProductUnit entity + config (Factor decimal 18,3, IsBaseUnit, composite unique per product) | Domain + Infrastructure | 1, 2 |
| 5 | Create ProductPrice entity + config (ProductUnitId + CurrencyId + Price + EffectiveFrom/To, CHK Price>=0) | Domain + Infrastructure | 3, 4 |
| 6 | Update InventoryBatch entity + config (add SupplierBatchNo varchar(100), update BatchNo as int) | Infrastructure | — |
| 7 | Update DbSeeder: seed 7 default Units, 1 default ProductCategory | Infrastructure | 1, 3 |
| 8 | Update SalesDbContext: all new DbSets + new migration | Infrastructure | 1-7 |
| 9 | Create ProductCategoryService + IProductCategoryService (CRUD, Result<T>) | Application | 8 |
| 10 | Create UnitService + IUnitService (CRUD with IsSystem guard, Result<T>) | Application | 8 |
| 11 | Update ProductService (remove cost/price logic, add CreateAsync with optional opening stock, add batch + journal entry creation) | Application | 8 |
| 12 | Create ProductUnitService + IProductUnitService (add/remove units, set base unit, validate min 2) | Application | 8 |
| 13 | Create ProductPriceService + IProductPriceService (CRUD with effective dates, price history) | Application | 8 |
| 14 | Create ProductImportService + IProductImportService (Excel parsing, bulk creation, error reporting) | Application | ClosedXML |
| 15 | Create FluentValidators for all Create/Update Requests (each Request DTO) | Api | Contracts |
| 16 | Create API Controllers: ProductCategoriesController, UnitsController, ProductsController (updated), ProductUnitsController (nested), ProductPricesController, ProductImportController | Api | 9-14 |
| 17 | Update Desktop IProductApiService and create new API service interfaces | Desktop | 16 |
| 18 | Create ProductCategoriesListViewModel + EditorViewModel + Views | Desktop | 17 |
| 19 | Create UnitsListViewModel + EditorViewModel + Views | Desktop | 17 |
| 20 | Update ProductsListViewModel + ProductEditorViewModel (add opening stock section, hide on edit, units tab) | Desktop | 17 |
| 21 | Create ProductUnitsListViewModel (per product) + Editor sub-view | Desktop | 17 |
| 22 | Create ProductPricesListViewModel + EditorViewModel + Views (filter by product/unit/currency) | Desktop | 17 |
| 23 | Create ProductImportViewModel + View (file picker, column mapping, progress, results) | Desktop | 17 |
| 24 | Register all new VMs/Views in Desktop DI container | Desktop | 18-23 |
| 25 | Add MainWindow sidebar navigation for all Products module screens | Desktop | 24 |
| 26 | Write/update tests: Domain entities, Application services, API validators, Desktop ViewModels | Tests | 1-25 |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Breaking change**: existing Products have cost/price columns that need migration | High | Migration script moves current product costs to an OPENING batch per product; prices to ProductPrices rows. Drop old columns after data migration. |
| **Cost display performance**: querying WAC across batches for list view may be slow at 10k+ products | Medium | Use a denormalized WarehouseStocks.LastCost column updated on every purchase/post. Recompute via background job if needed. |
| **User confusion**: removing retail/wholesale and replacing with per-unit pricing | Low | The per-unit model is actually MORE intuitive for small shops. Training material shows "قائمة أسعار الوحدات" as a single price list, not two columns. |
| **Opening stock warehouse**: default warehouse may not exist for first-time users | Medium | DbSeeder creates "المخزن الرئيسي" as default. Opening stock screen pre-selects it but allows override. |
| **Excel import encoding**: Arabic column headers in Excel may cause parsing issues | Low | Use ClosedXML which handles UTF-8 natively. Validate column names against Arabic constants. |
| **FIFO complexity at scale**: many small batches slow down COGS calculation | Low | V1 targets small retail (< 10k products, < 100k invoices/year). FIFO over batches is performant at this scale. Optimize with DB-level batch sorting if needed. |

---

## Cross-References

- **database-schema.md**: Module 3 (tables 3.1-3.5) for Products, ProductCategories, Units, ProductUnits, ProductPrices; Module 5 (table 5.2) for InventoryBatches.
- **Products Module Analysis**: Full Arabic design rationale in `docs/all new Anylysis for update system features/Products Module.md`.
- **Units Philosophy**: Seed data + IsSystem design in `docs/all new Anylysis for update system features/Lastes details abount new features.md` lines 1-100.
- **Phase Dependency Map**: Implementation sequence in `docs/all new Anylysis for update system features/Plan for Phases.md`.
