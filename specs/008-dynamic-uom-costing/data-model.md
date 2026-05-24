# Data Model: Dynamic UOM & Costing Engine

## Entities

### `ProductUnit`
Represents a defined unit of measure for a specific product.
- `Id` (int, PK)
- `ProductId` (int, FK)
- `UnitName` (nvarchar(50)) - e.g., "Piece", "Box"
- `ConversionFactor` (decimal(18,3)) - relative to base unit
- `RetailPrice` (decimal(18,2)) - specific to this unit
- `WholesalePrice` (decimal(18,2)) - specific to this unit
- `AvgCost` (decimal(18,2)) - specific to this unit
- `IsBaseUnit` (bool) - exactly one per product
- `IsActive` (bool) - soft delete flag
- `CreatedAt` (datetime2)
- `CreatedByUserId` (int, FK)

**Validation Rules**:
- `ConversionFactor > 0`
- `RetailPrice >= 0`, `WholesalePrice >= 0`
- `AvgCost >= 0`
- `UnitName` cannot be empty.
- A product must have at least one active unit.
- A product can have only one active base unit (`IsBaseUnit = true`, `ConversionFactor = 1`).

### `UnitBarcode`
Represents a scannable barcode for a specific `ProductUnit`.
- `Id` (int, PK)
- `ProductUnitId` (int, FK)
- `Barcode` (nvarchar(100)) - MUST be globally unique
- `IsActive` (bool)

**Validation Rules**:
- `Barcode` must be unique across all products and units in the system.

### `ProductPriceHistory`
Immutable audit log of price and cost changes.
- `Id` (int, PK)
- `ProductUnitId` (int, FK)
- `OldRetailPrice` (decimal(18,2))
- `NewRetailPrice` (decimal(18,2))
- `OldWholesalePrice` (decimal(18,2))
- `NewWholesalePrice` (decimal(18,2))
- `OldAvgCost` (decimal(18,2))
- `NewAvgCost` (decimal(18,2))
- `ChangeReason` (nvarchar(255))
- `ChangedByUserId` (int, FK)
- `ChangedAt` (datetime2)

### Modified Entities

#### `Product`
- Remove: `RetailPrice`, `WholesalePrice`, `AvgCost` (now stored on `ProductUnit`).
- Relationships: `ICollection<ProductUnit> Units`.

#### `SystemSettings`
- Add: `CostingMethod` (byte/enum: 1 = WeightedAverage, 2 = LastPurchasePrice, 3 = SupplierPrice). Default = 1.

#### `SalesInvoiceItem` & `PurchaseInvoiceItem`
- Modify: Link to `ProductId` but store `TransactionUnitId` (or unit name/factor) to freeze the unit details at the time of the transaction.
- Add: `SaleMode` (Retail/Wholesale) on `SalesInvoiceItem`.
