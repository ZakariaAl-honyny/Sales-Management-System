# Feature Specification: Dynamic Unit of Measure & Costing Engine (v4.3)

**Feature Branch**: `008-dynamic-uom-costing`
**Created**: 2026-05-24
**Status**: Draft
**Input**: User description: "Phase 8 — Dynamic UOM & Costing (v4.3)"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Multi-Unit Product Definition (Priority: P1)

A store owner sells the same product in multiple packaging sizes — for example, a bottle of water sold individually, by the box (12 bottles), or by the carton (24 bottles). They want to define these units once per product, assign each unit its own retail and wholesale price, and attach barcodes per unit. Cashiers should be able to sell or purchase in any defined unit without needing to manually convert quantities.

**Why this priority**: This is the foundational capability of the entire phase. All costing, stock, and pricing features depend on a product having one or more defined units. Without this, the system cannot correctly calculate invoice totals or manage stock.

**Independent Test**: Create a product with three units (Piece, Box=12×, Carton=144×), set prices for each, scan each barcode — the correct unit and price appear for each scan.

**Acceptance Scenarios**:

1. **Given** a product exists, **When** an admin adds a unit named "Box" with conversion factor 12, retail price 24.00, and a barcode, **Then** the system saves the unit and makes it selectable on invoices.
2. **Given** a product has two units, **When** an admin attempts to delete the only remaining unit, **Then** the system rejects the action with a clear Arabic message.
3. **Given** exactly one unit is defined as the base unit (conversion factor = 1), **When** a second unit is added with factor 12, **Then** quantities in the base unit correctly convert to the new unit (e.g., 24 base → 2 "Box" units).
4. **Given** a barcode is assigned to a specific product-unit combination, **When** that barcode is scanned at the point of sale, **Then** the system selects that product at the price of the matching unit.

---

### User Story 2 — Automatic Cost Recalculation on Purchase (Priority: P1)

When a buyer records a purchase invoice, the unit cost of each product is often different from the previously recorded cost. The system should automatically recalculate the average cost (using the configured costing strategy) and cascade that new cost to all units of the product, so that profit margins in reports are always accurate.

**Why this priority**: Financial accuracy of cost data directly impacts profit calculations in reports. Incorrect costs produce misleading management decisions.

**Independent Test**: Record a purchase of 100 units of Product A at 5.00 cost when existing stock was 50 units at 4.00 cost — the new weighted-average cost becomes 4.67 and all derived unit costs update automatically.

**Acceptance Scenarios**:

1. **Given** a product with 50 units at cost 4.00, **When** a purchase of 100 units at 5.00 is posted, **Then** the system recalculates weighted-average cost to 4.67 and updates the base unit cost.
2. **Given** a product has a Box unit with factor 12, **When** the base unit cost is updated to 4.67, **Then** the Box unit cost is automatically set to 4.67 × 12 = 56.04.
3. **Given** the system is configured to use "Last Purchase Price" costing, **When** a purchase at 6.00 is posted, **Then** the cost is directly overwritten to 6.00 (no weighted average).
4. **Given** the system is configured to use "Supplier Price" costing, **When** a purchase is posted, **Then** the product's supplier catalog price is used and the purchase unit cost is ignored for costing purposes.

---

### User Story 3 — Price & Cost Change Audit Trail (Priority: P2)

A store manager wants to see the history of all price and cost changes for any product — who changed it, when, what was the old value, and what the new value is. This is required for financial audit and dispute resolution.

**Why this priority**: While not blocking daily operations, the audit trail is critical for financial compliance and accountability. It can be delivered after P1 features are functional.

**Independent Test**: Change a product's retail price, then open the price history screen — a new entry appears showing the old price, new price, date, and the username of who made the change.

**Acceptance Scenarios**:

1. **Given** a product's retail price changes (via manual adjustment or purchase posting), **When** the change is saved, **Then** a new audit record is created with: old price, new price, old cost, new cost, change reason, username, and timestamp.
2. **Given** a product has a history of 10 price changes, **When** a manager views the price history, **Then** all 10 entries are displayed in reverse chronological order.
3. **Given** a cost update is triggered by a purchase invoice posting, **Then** the audit record's change reason indicates it was a purchase-driven update.

---

### User Story 4 — Smart Unit Display in UI (Priority: P3)

Cashiers and managers viewing inventory quantities should see them in the most readable unit. For example, "1440" bottles should display as "10 cartons" not "1440 pieces". The system should automatically choose the best unit for display based on defined thresholds.

**Why this priority**: This is a display-only enhancement with no impact on financial calculations. It improves usability but is not required for operational accuracy.

**Independent Test**: Set stock of Product A to 1440 base units, with Carton defined at 144× — the stock display on the inventory screen shows "10 cartons" rather than "1440 pieces".

**Acceptance Scenarios**:

1. **Given** a product has stock of 144 base units and a Box unit (12×) and Carton unit (144×), **When** the inventory screen displays this product's stock, **Then** it shows "1 carton" rather than "144 pieces" or "12 boxes".
2. **Given** a product has 5 base units and no larger unit applies, **When** stock is displayed, **Then** the base unit name is used (e.g., "5 pieces").
3. **Given** quantities are displayed in a unit other than the base, **When** a cashier hovers over the displayed quantity, **Then** the exact base-unit count is shown as a tooltip.

---

### Edge Cases

- What happens when a product is purchased in a unit that has no defined conversion factor to the base? → System must reject the purchase with a validation error; conversion factor is mandatory on unit creation.
- What happens when stock goes to zero during a weighted average cost recalculation? → System uses the new purchase price directly as the cost (division by zero prevention).
- What happens when two units for the same product have identical barcodes? → The system must reject the duplicate with an Arabic error message at the time of entry.
- What happens when an admin changes the costing strategy mid-operation (while a purchase is in draft)? → The strategy used is the one active at the time of invoice posting; draft state uses latest setting.
- What happens when a derived unit's conversion factor is changed after stock exists? → System must warn the user that changing the factor affects all historical cost interpretations and require explicit confirmation.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each product MUST support one or more units of measure, with exactly one designated as the base unit (the smallest/foundational unit with conversion factor = 1).
- **FR-002**: Each unit MUST have a conversion factor representing how many base units it contains (e.g., Box = 12 means 1 Box = 12 base units).
- **FR-003**: Each unit MUST have its own independent retail price and wholesale price; prices are NOT computed from the base unit price.
- **FR-004**: Each product-unit combination MUST support one or more barcodes; a barcode scan MUST resolve to exactly one product-unit combination.
- **FR-005**: The system MUST prevent deletion of the last remaining unit on any product, displaying a clear Arabic refusal message.
- **FR-006**: When a purchase invoice is posted, the system MUST automatically recalculate the cost of affected products using the active costing strategy configured in system settings.
- **FR-007**: The system MUST support exactly three costing strategies: Weighted Average, Last Purchase Price, and Supplier Price. Only one strategy is active system-wide at a time.
- **FR-008**: Weighted Average cost MUST be calculated as: (OldStock × OldCost + NewQty × NewUnitCost) ÷ (OldStock + NewQty), with the result precision maintained to two decimal places.
- **FR-009**: After a base unit cost is recalculated, ALL derived units of the same product MUST have their costs automatically updated as: DerivedCost = NewBaseCost × ConversionFactor.
- **FR-010**: The system MUST record a price/cost change audit entry for EVERY change to retail price, wholesale price, or cost — regardless of trigger (purchase, manual edit, supplier sync).
- **FR-011**: Each audit entry MUST capture: which product-unit changed, the old and new values for retail price, wholesale price, and cost, the username of who triggered the change, a change reason, and the timestamp.
- **FR-012**: The system MUST provide a smart display mode (view-only) that presents quantities in the most appropriate unit based on the magnitude of the quantity, without altering the stored base-unit quantity.
- **FR-013**: Invoice items MUST record which unit was used at time of sale/purchase (sale mode: Retail or Wholesale), so historical invoices remain accurate even if unit definitions change later.

### Key Entities

- **ProductUnit**: Represents one unit of measure for a product. Carries the unit name, conversion factor (relative to base), retail price, wholesale price, average cost, and whether it is the base unit. Belongs to exactly one product.
- **UnitBarcode**: Represents a single scannable barcode tied to one ProductUnit. One ProductUnit may have many barcodes.
- **ProductPriceHistory**: An immutable audit record of a single price or cost change event on a ProductUnit. Once written, it is never modified or deleted.
- **SystemSettings (CostingMethod)**: A single system-wide setting controlling which costing strategy is applied during purchase posting.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cashiers can scan any product barcode (across all defined units) and the correct unit with its price is selected instantly — with zero manual lookups required.
- **SC-002**: After posting a purchase invoice, the cost of all affected product units is updated within 3 seconds and is reflected immediately on the inventory screen.
- **SC-003**: 100% of price and cost changes — regardless of trigger — appear in the product price history log; no change is ever untracked.
- **SC-004**: When using Weighted Average costing, the calculated cost is always arithmetically correct (verified against a manual calculation) with no rounding errors beyond 2 decimal places.
- **SC-005**: The system correctly prevents creating a product state with zero units — any such attempt results in a user-visible refusal, with no data corruption.
- **SC-006**: A store manager can view the full price history of any product and identify the exact user, date, and reason for every price or cost change without ambiguity.

---

## Assumptions

- The costing strategy is configured once at the system level and applies uniformly to all products; per-product strategies are out of scope for this phase.
- "Supplier Price" costing uses the catalog price stored on the product record, not the price on a specific purchase order.
- Changing the costing strategy in System Settings takes effect for the next posted invoice, not retroactively for historical data.
- The smart unit display (SmartUnitFormatter) is used only for on-screen display; all stored values, API responses, and printed documents use base-unit quantities unless the invoice explicitly stores the transaction unit.
- A product may have at most one base unit (conversion factor = 1). Attempting to define a second base unit is a validation error.
- Price history is kept indefinitely and is not subject to retention or deletion policies.
- The barcode uniqueness constraint is enforced globally across all products and all units — two different product-unit combinations cannot share the same barcode.
- Wholesale price is used when the invoice sale mode is "Wholesale"; retail price is used otherwise. The choice is made per invoice, not per line item.
