# Quickstart Implementation Order

1. **ViewModel Preparation**:
   - Update `SalesInvoiceEditorViewModel` to manage a `ViewMode` toggle (Standard vs. Touch).
   - Ensure the active `CartItems` list is observable and synchronized.
2. **UI Controls**:
   - Create `NumericKeypadControl.xaml` for touch number entry.
   - Refactor the existing `SalesInvoiceEditor.xaml` to use a `ContentControl` that swaps views.
3. **Touch POS Layout (Right Panel)**:
   - Create `TouchPosView.xaml`.
   - Implement the `WrapPanel`/`UniformGrid` for Categories.
   - Implement the `WrapPanel` for Products (with `IsAsync=True` Image binding).
4. **Fast Cart Management (Left Panel)**:
   - Create `TouchPosCartView.xaml`.
   - Implement the in-line fast cart DataGrid/ListView with `+`, `-`, and `Delete` buttons.
   - Integrate the `NumericKeypadControl` and action buttons (`Cash`, `Card`, `Draft`).
5. **Testing**:
   - Verify layout responsiveness on 1024x768 sizing.
   - Perform test sales using standard view and touch view, verifying both generate identical API calls.
