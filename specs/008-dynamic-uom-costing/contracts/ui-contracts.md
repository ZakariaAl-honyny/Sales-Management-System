# UI Contracts: Dynamic UOM & Costing Engine

## ViewModels

### `ProductUnitEditorViewModel`
Fields:
- `UnitName` (Required)
- `ConversionFactor` (Numeric, > 0)
- `RetailPrice` (Numeric, >= 0)
- `WholesalePrice` (Numeric, >= 0)
- `Barcodes` (ObservableCollection<string>)

Validation: Implements `INotifyDataErrorInfo`. Checks for required fields and numeric ranges.

### `ProductUnitsListViewModel`
Displays the grid of units for a selected product.
Actions: Add Unit, Edit Unit, Delete Unit (shows 3-button delete dialog).

### `ProductPriceHistoryViewModel`
Displays a read-only data grid of price/cost changes.

### `SmartUnitFormatter`
A UI-only service to format quantities for display.
Methods:
- `string FormatQuantity(int productId, decimal baseQuantity)`: Returns a human-readable string (e.g., "2 Cartons").
