# Feature Specification: Touch-Optimized Quick POS Interface (Phase 15)

**Feature Branch**: `015-touch-pos-interface`  
**Created**: 2026-05-25  
**Status**: Draft  
**Input**: Phase 15 — Touch-Optimized Quick POS Interface (Restaurant-Style Layout)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dual-Mode Sales Interface Toggle (Priority: P1)

A cashier needs the flexibility to switch between the standard detailed retail interface and a fast, touch-friendly restaurant-style interface depending on the current customer traffic or their personal preference.

**Why this priority**: Essential to provide the alternative interface without removing the legacy functionality.

**Independent Test**: The user clicks a toggle button on the Sales view. The UI switches completely to the touch-optimized layout. The user switches back. The active shopping cart items remain intact and are not lost during the transition.

**Acceptance Scenarios**:

1. **Given** the user is on the Sales screen, **When** they click the layout toggle button, **Then** the interface switches to the Touch POS layout.
2. **Given** the user has items in their cart, **When** they toggle the layout back and forth, **Then** the cart items and totals are preserved without loss of data.

---

### User Story 2 - Touch-Optimized Category and Product Selection (Priority: P1)

A cashier using a touch screen monitor needs to quickly locate products visually instead of typing barcodes. They want to tap a category (e.g., "Drinks") and instantly see large, easy-to-tap tiles of products with their images and prices.

**Why this priority**: The core value proposition of a Touch POS system is rapid visual item selection.

**Independent Test**: Tap the "Drinks" category tile. The product grid updates smoothly to show drinks. Tap a product tile (e.g., "Cola"). The item is immediately added to the cart without the screen freezing or menus closing.

**Acceptance Scenarios**:

1. **Given** the Touch POS layout is active, **When** the user taps a category tile, **Then** the product grid instantly populates with products belonging to that category.
2. **Given** the product grid is populated, **When** the user taps a product tile, **Then** the product is added to the active cart in under 50 milliseconds, and the UI remains completely responsive.
3. **Given** products have optional images, **When** the product grid renders, **Then** the images are lazy-loaded asynchronously so scrolling and tapping are never blocked.

---

### User Story 3 - In-Line Fast Cart Management (Priority: P1)

A cashier makes a mistake or a customer changes their mind. The cashier needs to instantly increase the quantity, decrease it, or remove an item entirely directly from the active cart list without opening sub-menus or dialogues.

**Why this priority**: Speed at checkout is critical. Any delay in modifying the cart reduces the benefit of the POS layout.

**Independent Test**: Add an item to the cart. Tap the `+` button on the item's row to increment quantity. The subtotal and grand total update instantly. Tap the delete icon to remove it. The cart is instantly cleared.

**Acceptance Scenarios**:

1. **Given** an item is in the cart, **When** the user taps the `+` or `-` button on the item row, **Then** the quantity adjusts immediately and all invoice totals recalculate instantly.
2. **Given** an item is in the cart, **When** the user taps the remove/delete icon on the item row, **Then** the item is removed from the cart without requiring additional confirmation dialogues.

---

### User Story 4 - Quick Checkout & Numeric Keypad (Priority: P2)

A cashier needs to finalize the sale rapidly using touch. They need a large on-screen numeric keypad to enter the amount paid by the customer, followed by single-tap action buttons to complete the sale (Cash, Card, or Draft).

**Why this priority**: Completing the workflow purely via touch screen (without reaching for a physical keyboard) is necessary for true POS environments.

**Independent Test**: Use the on-screen keypad to enter "50". Tap the "Cash" button. The system processes the invoice and resets the screen for the next customer.

**Acceptance Scenarios**:

1. **Given** the user needs to input the paid amount, **When** they tap the on-screen numeric keypad, **Then** the numbers are entered into the paid amount field.
2. **Given** the cart has items and the paid amount is sufficient, **When** the user taps the "Cash" or "Card" button, **Then** the invoice is posted directly and the screen resets for the next sale.
3. **Given** a customer steps away to get an item, **When** the user taps "Draft", **Then** the current cart is saved as a draft and the screen is cleared for the next customer.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a UI toggle to switch between standard Retail Grid and Touch POS layouts.
- **FR-002**: The Touch POS Layout MUST strictly follow a Right-To-Left (RTL) structure: Categories/Products on the right panel, Active Cart on the left panel.
- **FR-003**: The system MUST render categories as a touch-friendly grid of tiles (`UniformGrid` or `WrapPanel`).
- **FR-004**: The system MUST render products as large, touch-friendly buttons displaying Name, Price, and Image.
- **FR-005**: Product images MUST be loaded asynchronously (Lazy Loaded) to prevent UI thread blocking.
- **FR-006**: Tapping a product MUST execute the `AddToCartCommand` instantly without closing or refreshing the overall page layout.
- **FR-007**: Cart item rows MUST include inline `+`, `-`, and `Remove` action buttons.
- **FR-008**: Invoice totals (Subtotal, Tax, Grand Total) MUST recalculate and display in real-time instantly upon any cart modification.
- **FR-009**: The system MUST provide an on-screen numeric keypad for data entry in touch environments.
- **FR-010**: The interface MUST provide dedicated one-click action buttons for checkout: [Cash], [Card], and [Draft].
- **FR-011**: The Touch POS interface MUST route all final checkout actions through the exact same backend Application logic (`CreateInvoiceCommand`) as the standard interface to guarantee consistency.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cart data is 100% preserved when toggling between standard and POS views.
- **SC-002**: Tapping a product adds it to the cart and updates the total in under 50 milliseconds (No UI lag).
- **SC-003**: The layout scales to standard monitor resolutions without element overlapping, overflowing, or requiring horizontal scrolling.
- **SC-004**: Invoices created via the Touch POS layout are structurally and financially identical in the database to those created via the standard layout.

## Assumptions

- The target deployment devices have a screen resolution sufficient for POS layouts (e.g., 1024x768 minimum, typically 1920x1080).
- Users have standard touch monitors; multi-touch gestures (like pinch-to-zoom) are not required.
- The optional images for products (Phase 14) are already supported by the backend and infrastructure.
