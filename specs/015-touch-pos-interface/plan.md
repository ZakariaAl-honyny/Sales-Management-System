# Implementation Plan: Touch-Optimized Quick POS Interface (Phase 15)

**Branch**: `015-touch-pos-interface` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase introduces a dual-mode sales interface, adding a fast, touch-friendly "Restaurant-Style" POS layout alongside the standard retail grid. The Touch POS layout is a pure UI overlay — it routes every business operation through the exact same `SalesInvoiceEditorViewModel`, `SalesInvoiceService`, and Domain entities as the standard grid. No API, service, or database changes are required. The toggle between Standard and Touch modes lives in the `SalesInvoiceEditorViewModel` via a `CurrentViewMode` enum property (Standard/Touch), and the `SalesInvoiceEditorView.xaml` uses a `ContentControl` bound to this property to swap `RetailSalesView` and `TouchPosView` UserControls. Cart data is preserved across toggles because both views bind to the same editor ViewModel. The Touch POS layout uses a right-panel `UniformGrid` for category and product tile selection (with lazy-loaded images for performance), a left-panel active cart with inline `+`/`-`/Remove buttons, a bottom `NumericKeypadControl` for touch-driven paid-amount entry, and single-tap action buttons for Cash/Card/Draft checkout. All totals (Subtotal, Tax, GrandTotal) update in real-time via standard property-changed bindings to the shared ViewModel. The existing `MainViewModel.NavigateToPosCommand` opens the editor in Touch mode by setting `CurrentViewMode = SalesViewMode.Touch` before showing the `ScreenWindow`.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (WPF Desktop only)
**Architecture Scope**: Entirely within `SalesSystem.DesktopPWF` — no Domain, Application, Infrastructure, or API changes
**Target**: Cashiers in high-traffic retail environments who need fast, visual, touch-based product selection
**Performance Goals**: < 50ms cart addition latency, 60fps smooth scrolling, lazy-loaded images never block UI thread
**Constraints**:
- Zero backend changes — all operations reuse existing API endpoints (`POST /api/v1/sales-invoices`, `GET /api/v1/products`, `GET /api/v1/categories`)
- Cart data MUST persist across mode toggles (Standard ↔ Touch)
- The on-screen `NumericKeypadControl` must be a reusable generic UserControl, not tied to the POS view
- Products without images show a placeholder icon (product name initial) — NEVER a broken image
- Touch targets must be minimum 44×44 effective pixels (touch usability standard)
- All checkout actions (Cash/Card/Draft) map one-to-one to existing `SalesInvoiceService` operations: Post (Cash/Card) or SaveDraft (Draft)

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No financial math changes — reused from shared ViewModel |
| II | Domain Formulas | ✅ N/A | No formula changes — all totals computed in shared ViewModel |
| III | Transactional Integrity | ✅ N/A | No transaction changes — reused from shared service layer |
| IV | Invoice Lifecycle | ✅ PASS | Cash/Card buttons call existing Post flow; Draft button calls existing SaveDraft flow |
| V | Stock Integrity | ✅ N/A | No stock logic changes — shared service layer handles this |
| VI | Result Pattern | ✅ N/A | No Result<T> changes — reused from shared service layer |
| VII | Architecture Boundaries | ✅ PASS | All changes stay within DesktopPWF; API calls go through existing `ISalesInvoiceApiService` |
| VIII | Security | ✅ N/A | No security changes — authorization flows through existing API |
| IX | Four-Layer Validation | ✅ N/A | Shared service layer validators apply unchanged |
| X | Logging | ✅ N/A | No new logging |
| XI | EF Core Conventions | ✅ N/A | No EF Core changes |
| XII | Audit Trail | ✅ N/A | No audit changes |
| XIII | Delete Strategy | ✅ N/A | No delete logic changes |
| XIV | Defensive Programming | ✅ PASS | ViewModel guard against null `SalesInvoiceEditorViewModel.CurrentInvoice` during toggle |
| XV | WPF Dialogs | ✅ PASS | Confirmation dialogs for Cash/Card checkout via existing IDialogService |
| XVI | Toast Notifications | ✅ N/A | Unchanged |
| XVII | Real-Time UI Validation | ✅ N/A | Validation pattern unchanged |

**Gate Result**: ✅ ALL CLEAR — No rules violated.

---

## Dual-Mode View Architecture

### ViewMode Toggle

The `SalesInvoiceEditorViewModel` exposes a `CurrentViewMode` property of type `SalesViewMode` (enum: `Standard = 0, Touch = 1`). The `SalesInvoiceEditorView.xaml` wraps its content in a `ContentControl` whose `Content` is bound to `CurrentViewMode` via a `ViewModeTemplateSelector` or two explicit `DataTemplate` definitions. When the user clicks the toggle button (a `ToggleButton` in the toolbar region), the ViewModel switches the mode:

```text
Toggle → CurrentViewMode = (CurrentViewMode == Standard ? Touch : Standard)
       → ContentControl re-evaluates template → new UserControl renders
       → cart data is preserved because ViewModel state is unchanged
```

The toggle button has an Arabic ToolTip explaining the switch: `"التبديل بين وضع البيع السريع والوضع التفصيلي"`. The toggle button is always visible in both modes so the user can switch freely.

### Shared ViewModel State

Both layouts bind to the same `SalesInvoiceEditorViewModel` instance. This means:
- `CartItems` (ObservableCollection) — shared
- `SubTotal`, `TaxAmount`, `GrandTotal` — shared computed properties
- `PaidAmount` — shared
- `SelectedCustomer` — shared
- `CurrentInvoice` (the draft DTO) — shared

When the user toggles modes, no data is serialized, copied, or moved — the ViewModel reference is the same object. This guarantees zero data loss and zero latency on toggle. The only difference in the layout is which UserControl renders the data visually.

---

## Touch POS Layout Structure

### Right Panel: Category and Product Tile Grid

The right panel occupies ~60% of the window width and contains:

1. **Category Row** (top): A horizontal `WrapPanel` of category tiles. Each tile is a `Border` with rounded corners, `MinWidth="80"`, `Height="44"`, background color from the category's color code (if any), white text showing the category name, and a `ToolTip` showing the category description. Tapping a category tile filters the product grid below. A "الكل" (All) tile is always the first tile, showing all products.

2. **Product Grid** (below categories): A `UniformGrid` with `Columns="4"` (or `5` depending on window width, adjusted via a `SizeChanged` handler). Each product tile is a `Border` with:
   - Optional image (loaded via `BitmapImage` with `IsAsync=True` and `CacheOption = BitmapCacheOption.OnLoad`) — if no image, a placeholder circle with the first letter of the product name
   - Product name (2-line max, `TextTrimming="CharacterEllipsis"`)
   - Price formatted in base currency (e.g., `"15.00 ر.س"`)
   - Touch-friendly `MinHeight="80"` and `MinWidth="90"`
   - `Command="{Binding DataContext.AddProductCommand, RelativeSource={...}}"` with `CommandParameter` set to the product ID

3. **Empty State**: If no products match the selected category, the grid shows a centered `TextBlock` with message `"لا توجد منتجات في هذه المجموعة"`.

### Left Panel: Active Cart and Totals

The left panel occupies ~40% of the window width and contains:

1. **Cart List** (`ListBox` with `ScrollViewer`): Each item row shows:
   - Product name (right-aligned)
   - Quantity with inline `[−]` `[数量]` `[+]` buttons — tapping adjusts quantity directly on the ViewModel
   - Line total (computed as `Quantity × UnitPrice`)
   - Remove `✕` button (red, top-right corner of the item card)
   - Each cart item card has `MinHeight="48"` for comfortable touch targeting

2. **Totals Section** (below the cart, always visible):
   - Subtotal: `"المجموع الفرعي: {SubTotal:N2} ر.س"`
   - Tax: `"الضريبة: {TaxAmount:N2} ر.س"` (if applicable)
   - Grand Total: `"الإجمالي: {GrandTotal:N2} ر.س"` (large font, bold)
   - Remaining: `"المتبقي: {RemainingAmount:N2} ر.س"` (if partially paid)

3. **Numeric Keypad** (below totals): A reusable `NumericKeypadControl` with buttons for digits 0–9, decimal point (`.`), backspace, and clear. Each button is `Width="60"` `Height="50"` with `FontSize="18"` for easy touch. Tapping a digit appends it to the `PaidAmount` string property on the ViewModel, which is parsed to `decimal` for the invoice. The keypad is its own UserControl (`Views/Controls/NumericKeypadControl.xaml`) with dependency properties for `Value` (string) and `ValueChanged` event — reusable across any screen that needs touch numeric input.

4. **Action Buttons** (below keypad):
   - **📄 كاش** (Cash) — Posts the invoice as Cash payment type. Shows confirmation dialog first.
   - **💳 بطاقة** (Card) — Posts the invoice as Card payment type. Shows confirmation dialog first.
   - **💾 مسودة** (Draft) — Saves the invoice as Draft without payment processing.
   - **❌ إلغاء** (Cancel) — Clears the current cart (with confirmation if items exist).

Each action button is `Height="48"` with `FontSize="16"` and has a distinct background color (green for Cash, blue for Card, gray for Draft, red for Cancel). All buttons trigger the existing ViewModel commands (`PostInvoiceCommand`, `SaveDraftCommand`, `CancelCommand`) — the Touch POS view simply invokes the same command infrastructure.

### On-Screen Numeric Keypad Control

The `NumericKeypadControl` is a self-contained UserControl with:

```text
Properties:
  - Value (string DP) — Two-way bindable, the current entered value
  - MaxValue (decimal DP) — Optional cap (default 999999.99)
  - HasDecimal (bool DP) — Whether to show decimal point button (default true)

Layout:
  4×4 grid: [7] [8] [9] [←]  (backspace)
             [4] [5] [6] [⌫]  (clear)
             [1] [2] [3] [.]
             [±] [0] [00] [↵] (not used in POS, reserved for future)

All buttons raise routed events that the parent ViewModel handles via commands
bound through the keypad's Value DP.
```

The keypad does NOT contain business logic — it only manipulates the `Value` string. Parsing to `decimal` and validation happens in the ViewModel's `PaidAmount` setter, which applies `AddError` if the amount exceeds the grand total.

---

## Performance Considerations

### Image Lazy Loading

The product grid uses `VirtualizingStackPanel` internally (via `ListBox` with `VirtualizingPanel.ScrollUnit="Item"`) so that only visible product tiles have their images decoded. Each product tile binds its `Image.Source` to a `BitmapImage` with:

```text
BitmapImage construction:
  - Create with `CacheOption = BitmapCacheOption.OnLoad` (decode once, cache in memory)
  - Set `UriSource` to the API image endpoint `GET /api/v1/products/{id}/image`
  - Set `IsAsync = true` to decode on a background thread
  - Bind in XAML with `FallbackValue` set to a placeholder drawing brush
```

This ensures that scrolling through hundreds of products never freezes the UI. The first time an image is loaded, it is cached by WPF's `BitmapCache` so subsequent scrolls are instant.

### Cart Update Responsiveness

Cart item `+`/`-` buttons bind directly to `ICommand` properties on the ViewModel (`IncreaseQuantityCommand`, `DecreaseQuantityCommand`, `RemoveItemCommand`). These commands execute synchronously on the UI thread (no API call — they only modify in-memory `ObservableCollection` items) and the property-changed notifications for `SubTotal`, `TaxAmount`, `GrandTotal` cascade instantly. Measured performance target: < 50ms from tap to visual update.

---

## Integration with Existing Navigation

The `MainViewModel.NavigateToPosCommand` (already registered in the sidebar Expander under "المبيعات" → "نقطة البيع") creates a `SalesInvoiceEditorViewModel` via DI, sets `CurrentViewMode = SalesViewMode.Touch`, and opens it in a `ScreenWindow`:

```text
1. Resolve SalesInvoiceEditorViewModel from DI container
2. Set editorVm.CurrentViewMode = SalesViewMode.Touch
3. Open via screenService.OpenScreen(editorVm, options)
   - Title = "نقطة البيع (الكاشير)"
   - OnClosed = refresh SalesInvoiceListViewModel via EventBus if invoice was posted
```

The `SalesInvoiceEditorView.xaml` detects the initial `CurrentViewMode` value in its `Loaded` event and sets the corresponding VisualState or ContentTemplate. No special constructor or parameter is needed — the toggle happens purely through the ViewModel property.

---

## Project Structure

```
SalesSystem.DesktopPWF/
├── ViewModels/
│   ├── Sales/
│   │   └── SalesInvoiceEditorViewModel.cs     ← UPDATE: add CurrentViewMode property, SalesViewMode enum
│   └── Controls/
│       └── NumericKeypadViewModel.cs          ← NEW: lightweight VM for the keypad (optional, can use code-behind)
├── Views/
│   ├── Sales/
│   │   ├── SalesInvoiceEditorView.xaml        ← UPDATE: add ContentControl for mode switching, toggle button
│   │   ├── TouchPosView.xaml                  ← NEW: right panel (category/products) + left panel (cart + totals)
│   │   └── TouchPosView.xaml.cs               ← NEW: code-behind for size-dependent column count
│   └── Controls/
│       └── NumericKeypadControl.xaml/.cs      ← NEW: reusable on-screen numeric keypad
└── Resources/
    └── Styles.xaml                            ← UPDATE: add TouchPos tile styles (category tile, product tile, keypad button)
```

---

## Verification Checklist

- [ ] Toggle between Standard and Touch modes preserves cart items and totals without data loss
- [ ] Category tiles filter product grid; "الكل" tile shows all products
- [ ] Product tiles show name, price, and image (or placeholder); images lazy-loaded
- [ ] Tapping a product tile adds it to cart in < 50ms
- [ ] Cart item `+`/`-`/Remove buttons work instantly without API calls
- [ ] Totals (SubTotal, TaxAmount, GrandTotal) update in real-time on every cart change
- [ ] On-screen numeric keypad enters digits into PaidAmount field
- [ ] Cash button posts invoice with PaymentType=Cash; Card button posts with PaymentType=Card
- [ ] Draft button saves invoice as Draft without payment
- [ ] Cancel button clears cart (with confirmation if items exist)
- [ ] Touch POS uses same API endpoints as Standard mode — no backend changes
- [ ] No database schema or Domain entity changes
- [ ] All interactive controls have Arabic ToolTips
- [ ] Build: 0 errors, 0 warnings
