# Implementation Tasks: Touch-Optimized Quick POS Interface

**Feature**: `015-touch-pos-interface`
**Spec**: [spec.md](./spec.md)
**Plan**: [plan.md](./plan.md)

## Implementation Strategy
To support execution by a smaller/cheaper LLM model, these tasks are written with extreme specificity. Each task includes exact file paths, class names, and detailed logic expectations. We will follow a strict horizontal slice per User Story after setting up the foundational schema.

## Dependencies & Execution Order
1. **Phase 1 (Setup)**: MUST be completed first.
2. **Phase 2 (US1)**: Depends on Phase 1 (View switching logic).
3. **Phase 3 (US2)**: Depends on Phase 1. Can be done in parallel with Phase 4.
4. **Phase 4 (US3)**: Depends on Phase 1.
5. **Phase 5 (US4)**: Depends on Phase 4 (Cart view needs to exist).
6. **Phase 6 (Polish)**: Final integration.

---

## Phase 1: Setup & Foundations
*Goal: Prepare the common UI controls and ViewModel state for mode switching.*

- [ ] T001 Create `NumericKeypadControl.xaml` and `NumericKeypadControl.xaml.cs` in `SalesSystem.DesktopPWF/Views/Controls/`. Implement a generic on-screen numpad grid (buttons 1-9, 0, Backspace, Clear). Expose a `DependencyProperty` named `InputValue` (string) that updates when buttons are pressed.
- [ ] T002 Update `SalesSystem.DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs`. Add `public enum SalesViewMode { Standard, Touch }`. Add a property `public SalesViewMode CurrentViewMode { get; set; }` (with RaisePropertyChanged). Add `public IRelayCommand ToggleViewModeCommand` that switches the mode between Standard and Touch. 

---

## Phase 2: User Story 1 — Dual-Mode Sales Interface Toggle (Priority P1)
*Goal: Allow users to toggle between the standard grid and the new touch layout.*

- [ ] T003 [US1] Create a `SalesViewModeToVisibilityConverter.cs` in `SalesSystem.DesktopPWF/Converters/` that returns `Visibility.Visible` if the passed mode matches the bound mode, and `Collapsed` otherwise. Register it in `App.xaml`.
- [ ] T004 [US1] Update `SalesSystem.DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` (or `.xaml`, ensure exact name matches your project). Add a Toggle Button in the top header/toolbar `Command="{Binding ToggleViewModeCommand}"` with text "تبديل المظهر".
- [ ] T005 [US1] In `SalesInvoiceEditorView.xaml`, wrap the existing main grid (Retail Layout) in a `Grid` or `Border` with `Visibility="{Binding CurrentViewMode, Converter={StaticResource SalesViewModeToVisibilityConverter}, ConverterParameter=Standard}"`. Create an empty sibling `Grid` with `Visibility="{Binding CurrentViewMode, Converter={StaticResource SalesViewModeToVisibilityConverter}, ConverterParameter=Touch}"`. This sibling grid will host the Touch POS UI.

---

## Phase 3: User Story 2 — Category and Product Selection (Priority P1)
*Goal: Build the right-side panel with large, touch-friendly category and product tiles.*

- [ ] T006 [P] [US2] Create `TouchPosViewModel.cs` in `SalesSystem.DesktopPWF/ViewModels/Sales/`. It must take dependencies `ICategoryApiService` and `IProductApiService`. Include `ObservableCollection<CategoryDto> Categories`, `ObservableCollection<ProductDto> Products`, and `CategoryDto SelectedCategory`. Implement `LoadCategoriesAsync()`, `SelectCategoryCommand` (loads products for category), and an `AddToCartCommand` (which sends an EventBus message or invokes a delegate to add the item to the main invoice).
- [ ] T007 [US2] Create `TouchPosView.xaml` (UserControl) and its code-behind in `SalesSystem.DesktopPWF/Views/Sales/`. Set `d:DataContext="{d:DesignInstance Type=viewmodels:TouchPosViewModel}"`. 
- [ ] T008 [US2] In `TouchPosView.xaml`, create a Top/Bottom split layout. Top: an `ItemsControl` using a horizontal `WrapPanel` bound to `Categories`. Bottom: a `ScrollViewer` containing an `ItemsControl` using a `UniformGrid` (e.g., 4 columns) bound to `Products`.
- [ ] T009 [US2] In `TouchPosView.xaml`, design the Product `DataTemplate`. It must be a large `Button` binding to `AddToCartCommand`. Inside the button, show the Product Name, Price, and an `Image` control bound to `ImagePath` with `IsAsync=True` for lazy loading (FR-005).

---

## Phase 4: User Story 3 — In-Line Fast Cart Management (Priority P1)
*Goal: Build the left-side panel for active cart management with inline + / - controls.*

- [ ] T010 [P] [US3] Create `TouchPosCartViewModel.cs` in `SalesSystem.DesktopPWF/ViewModels/Sales/`. It should maintain a reference to the active `SalesInvoiceDto` or `ObservableCollection<SalesInvoiceItemDto>`. Implement commands: `IncreaseQtyCommand`, `DecreaseQtyCommand`, `RemoveItemCommand` (each taking the `SalesInvoiceItemDto` as parameter). These commands MUST trigger the calculation logic in the Domain/MainViewModel so totals update.
- [ ] T011 [US3] Create `TouchPosCartView.xaml` (UserControl) and its code-behind in `SalesSystem.DesktopPWF/Views/Sales/`. Set `d:DataContext="{d:DesignInstance Type=viewmodels:TouchPosCartViewModel}"`.
- [ ] T012 [US3] In `TouchPosCartView.xaml`, implement a top area using a `ListView` or `DataGrid` bound to the cart items. In the row template, include TextBlocks for Name and Total Price, and three Buttons: `+`, `-`, and `🗑` bound to the commands from T010.

---

## Phase 5: User Story 4 — Quick Checkout & Numeric Keypad (Priority P2)
*Goal: Integrate the numpad and checkout action buttons into the cart view.*

- [ ] T013 [US4] Update `TouchPosCartViewModel.cs` to add a `public string PaidAmountString` property. Add checkout commands: `CashCheckoutCommand`, `CardCheckoutCommand`, `DraftCommand` that delegate back to the main `SalesInvoiceEditorViewModel` save logic.
- [ ] T014 [US4] Update `TouchPosCartView.xaml` bottom section. Add the `controls:NumericKeypadControl` and bind its `InputValue` to `PaidAmountString` (`Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`).
- [ ] T015 [US4] Below the numpad in `TouchPosCartView.xaml`, add a horizontal `UniformGrid` with three large action buttons: `[ كاش ]`, `[ شبكة ]`, and `[ مسودة ]`. Bind them to their respective commands. Add a section displaying Subtotal, Tax, and Grand Total.

---

## Phase 6: Polish & Integration

- [ ] T016 Update `SalesInvoiceEditorViewModel.cs` constructor to instantiate `TouchPosViewModel` and `TouchPosCartViewModel` as public properties (`TouchPosVM` and `TouchPosCartVM`). Pass the necessary dependencies and delegates/actions so they can manipulate the main cart items.
- [ ] T017 Update `SalesInvoiceEditorView.xaml` sibling grid (created in T005) to include both the new UserControls side-by-side using a grid layout (e.g., Left Column 35% for Cart, Right Column 65% for Products).
- [ ] T018 Run the application and verify SC-001 (cart state preserved on toggle) and SC-002 (product click latency < 50ms). Verify the UI renders correctly in RTL mode without overlaps (SC-003).
