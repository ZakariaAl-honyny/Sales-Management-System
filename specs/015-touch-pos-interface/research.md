# Research: Touch-Optimized Quick POS Interface

**Feature**: `015-touch-pos-interface`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: View Switching Mechanism (Retail vs. Touch POS)

**Decision**: The main `SalesInvoiceEditor.xaml` will host a `ContentControl`. The `SalesInvoiceEditorViewModel` will expose a `CurrentViewMode` enum (Standard vs. Touch). Based on this state, the UI will swap out the inner UserControl between `RetailSalesView.xaml` and `TouchPosView.xaml`.

**Rationale**: This provides a seamless transition without opening new windows or losing the active `SalesInvoice` state. Both views can bind to the exact same shared data context (or synchronized sub-contexts) so cart data is natively preserved during toggling.

---

### D-002: WPF High-Performance Image Rendering

**Decision**: Product image tiles in the Touch POS layout will use WPF `BitmapImage` with `CacheOption = BitmapCacheOption.OnLoad` and XAML bindings using `IsAsync=True`. 

**Rationale**: Satisfies FR-005 and SC-002. Since the POS view may display dozens of product images simultaneously, decoding images asynchronously is strictly required to prevent the main UI thread from freezing and missing touch events.

---

### D-003: On-Screen Numeric Keypad Implementation

**Decision**: Create a generic `NumericKeypadControl.xaml` UserControl that exposes `ICommand` bindings for number presses (0-9, backspace, clear). It will be embedded inside the `TouchPosCartView.xaml`.

**Rationale**: Promotes reusability. The keypad simply modifies a string/decimal property (e.g., `PaidAmount`) on the ViewModel in real-time, matching standard POS hardware behavior.
