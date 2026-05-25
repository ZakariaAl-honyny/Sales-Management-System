# UI Contracts: Multi-Window & UI Polish (v4.5)

**Feature**: `012-multi-window-ui`
**Date**: 2026-05-25

---

## New Services

### `IScreenWindowService`
- **Method**: `void OpenNonModal(ViewModelBase viewModel, string title, double width = 800, double height = 600)`
- **Behavior**: Instantiates `ScreenWindow.xaml`, sets `DataContext = viewModel`, calculates cascade offset, and calls `window.Show()`.

---

## New Views

### `ScreenWindow.xaml`
- **Type**: `System.Windows.Window`
- **Properties**: `WindowStartupLocation = Manual`, RTL layout (`FlowDirection="RightToLeft"`).
- **Content**: A single `<ContentControl Content="{Binding}" />`.
- **Code-behind**: Hooks the `Closed` event to cast `DataContext` to `IDisposable` and explicitly call `Dispose()`.

---

## Service Modifications

### `DialogService`
- **Update**: Modify `PositionOverOwner` (or the `ShowDialog` wrapper).
- **Logic**:
  ```csharp
  var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
  if (activeWindow != null && activeWindow != dialogWindow)
  {
      dialogWindow.Owner = activeWindow;
  }
  else
  {
      dialogWindow.Owner = Application.Current.MainWindow;
  }
  dialogWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
  ```

---

## Global UI Polish

### List Sorting
- Update all generic/list API endpoints OR ViewModel loads to apply `.OrderByDescending(x => x.Id)` or `.OrderByDescending(x => x.CreatedAt)`.
- **Affected Views**: Products, Customers, Suppliers, Invoices, Receipts.

### Arabic ToolTips
- All primary `<Button>` elements get `ToolTip="[Arabic description]"`.
- All required `<TextBox>` elements get `ToolTip="[Arabic hint]"`.
