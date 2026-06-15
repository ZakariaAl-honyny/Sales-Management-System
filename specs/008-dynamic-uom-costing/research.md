# Research: Dynamic UOM & Costing Engine (v4.3)

**Feature**: `008-dynamic-uom-costing`
**Date**: 2026-05-24
**Status**: Complete — All unknowns resolved

---

## Decision Log

### D-001: Unit Pricing Storage Strategy

**Decision**: Store `RetailPrice` and `WholesalePrice` directly on `ProductUnit`, not derived from base unit.

**Rationale**: Pricing in retail is rarely a simple multiple of the base unit price. A 12-pack of water may retail at 15.00 (not 12 × 1.50 = 18.00). Storing per-unit prices avoids lossy reverse-computation and allows arbitrary price setting at each packaging level.

**Alternatives considered**:
- *Derive from base price × factor*: Rejected — mathematically incorrect for real-world pricing.
- *Percentage markup per unit*: Rejected — harder for cashiers to understand; more configuration surface.

---

### D-002: Costing Strategy Extensibility

**Decision**: Three strategies encoded as `CostingMethod` enum (byte) stored in `SystemSettings` table, seeded with `WeightedAverage = 1`. The `UpdateProductPricingService` uses a strategy-pattern switch internally.

**Rationale**: Enum + service switch is the simplest extension point that avoids over-engineering (no full Strategy pattern injection needed for 3 fixed options). New strategies can be added with a new enum value + switch case.

**Alternatives considered**:
- *IStrategy interface per costing method*: Rejected — over-engineered for 3 fixed options in a single-store system.
- *Per-product costing method*: Rejected — the spec mandates a single system-wide setting.

---

### D-003: Weighted Average Formula Precision

**Decision**: Calculate WAC as `(OldStock × OldAvgCost + NewQty × NewUnitCost) / (OldStock + NewQty)`, rounded to `decimal(18,2)`. On zero denominator (first purchase ever), use `NewUnitCost` directly.

**Rationale**: Standard weighted average cost (WAC) formula used in retail accounting. `decimal(18,2)` precision matches the financial precision rule (Constitution I). Edge case of zero stock is explicitly handled to avoid `DivideByZeroException`.

**Alternatives considered**:
- *FIFO costing*: Out of scope per spec.
- *Standard cost*: Not applicable for dynamic purchase prices.

---

### D-004: Cost Cascade Mechanism

**Decision**: After updating the base unit's `AvgCost`, the `UpdateProductPricingService` iterates all `ProductUnit` records for that product and sets `DerivedUnit.AvgCost = NewBaseCost × DerivedUnit.ConversionFactor`. This runs within the same purchase-posting transaction.

**Rationale**: Performing the cascade in the same transaction ensures atomic consistency — no window where the base unit has a new cost but derived units still carry the old cost.

**Alternatives considered**:
- *Trigger-based cascade in SQL*: Rejected — violates Clean Architecture (business logic in DB layer).
- *Background job cascade*: Rejected — creates a consistency window; violates transactional integrity (Constitution III).

---

### D-005: Barcode Uniqueness Scope

**Decision**: Barcode uniqueness is enforced globally (across all products and all units) via a `UNIQUE` index on `UnitBarcodes.Barcode`. Enforced at DB level AND Application level (pre-insert check returning `Result.Failure` with Arabic message).

**Rationale**: A barcode scanner at POS must resolve to exactly one product-unit. Allowing the same barcode on multiple records would cause ambiguity. The pre-insert check at Application layer gives a user-friendly error before hitting the DB constraint.

**Alternatives considered**:
- *Per-product barcode uniqueness*: Rejected — POS scanners don't know the product context before scanning.

---

### D-006: ProductUnit Soft vs Hard Delete

**Decision**: `ProductUnit` uses `DeleteStrategy`:
- **Deactivate (soft)**: Mark `IsActive = false`. Unit disappears from new invoices but historical invoice items remain intact (FK preserved).
- **Permanent**: Only allowed if the unit has zero references in `SalesInvoiceLines`, `PurchaseInvoiceLines`. Blocked if it is the last unit on the product (FR-005).

**Rationale**: Soft delete preserves FK integrity on historical invoice lines (Constitution XIII). The "last unit" guard enforces FR-005.

---

### D-007: SmartUnitFormatter Thresholds

**Decision**: Display in the largest unit whose quantity is ≥ 1 after division. Algorithm:
1. Sort product units by `ConversionFactor` descending.
2. For each unit: `display = floor(baseQty / factor)`. If `display >= 1`, use this unit and stop.
3. Fallback: base unit with exact quantity.

**Rationale**: Simple, deterministic, and consistent with how retail staff intuitively read quantities ("2 cartons" is clearer than "24 boxes" or "288 pieces"). No configuration required.

---

### D-008: Price History Trigger Points

**Decision**: `ProductPriceHistory` entries are created in these three scenarios:
1. **Purchase posting** — by `UpdateProductPricingService` (reason: `"تحديث التكلفة من فاتورة شراء رقم {invoiceId}"`).
2. **Manual price adjustment** — by a new `AdjustProductUnitPriceAsync` method on `IProductPriceService` (reason: `"تعديل يدوي للسعر"`).
3. **Unit creation** — initial history entry with old values = 0 (reason: `"إنشاء وحدة جديدة"`).

**Rationale**: Ensures SC-003 (100% of changes tracked). Creation entry ensures the audit trail starts from day one.

---

### D-009: API Endpoint Design

**Decision**: Nest unit endpoints under products:
- `GET /api/v1/products/{productId}/units` — list all units for a product
- `POST /api/v1/products/{productId}/units` — add a unit
- `PUT /api/v1/products/{productId}/units/{unitId}` — update a unit
- `DELETE /api/v1/products/{productId}/units/{unitId}` — soft/hard delete (body: `DeleteStrategy`)
- `GET /api/v1/products/{productId}/price-history` — list price/cost history
- `GET /api/v1/barcodes/{barcode}` — resolve barcode → ProductUnit + Product (used by POS scanner)

**Rationale**: RESTful nesting makes the product-unit ownership explicit. The barcode resolver is a top-level endpoint because it's accessed by scanning without knowing the product.

---

## Open Questions (Resolved)

| # | Question | Resolution |
|---|----------|------------|
| Q1 | What happens to existing products with no `ProductUnit` rows? | Migration seeds one `ProductUnit` (base, ConversionFactor=1) per existing product using the product's current `RetailPrice` and `WholesalePrice` |
| Q2 | Should `ConversionFactor` be changeable after unit is created? | Yes, with a warning dialog in UI; allowed at API level if no invoice items reference the unit |
| Q3 | Which user identity is used for `ChangedByUserId` in price history during purchase posting? | The `CreatedByUserId` on the purchase invoice — passed through to `UpdateProductPricingService` |
