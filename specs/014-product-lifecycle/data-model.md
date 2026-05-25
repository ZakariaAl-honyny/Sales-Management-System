# Data Model: Product Lifecycle & Media Management

**Feature**: `014-product-lifecycle`

## Entities

### `Product` (Modified)
**Location**: `SalesSystem.Domain/Entities/Products/Product.cs`
- Added Properties:
  - `ExpirationDate` (`DateTime?`): Optional expiration date.
  - `ImagePath` (`string?`): The relative path to the image stored in the local file system.

### `StockWriteOff` (New Entity)
**Location**: `SalesSystem.Domain/Entities/Inventory/StockWriteOff.cs`
- **Properties**:
  - `Id` (`int`): Primary Key.
  - `ProductId` (`int`): Foreign Key to Product.
  - `WarehouseId` (`int`): Foreign Key to Warehouse.
  - `Quantity` (`decimal(18,3)`): The amount written off.
  - `WriteOffDate` (`DateTime`): Defaults to current system date.
  - `Reason` (`string`): e.g., "Expired", "Damaged".
  - `CreatedByUserId` (`int`): Audit field.
  - `CreatedAt` (`DateTime`): Audit field.

## EF Core Configuration

### `ProductConfiguration`
- `.Property(p => p.ImagePath).HasMaxLength(500)`

### `StockWriteOffConfiguration`
- Table Name: `StockWriteOffs`
- Primary Key: `Id`
- Properties:
  - `Quantity`: `.HasPrecision(18, 3)`
  - `Reason`: `.HasMaxLength(250)`
- Relationships:
  - `HasOne(w => w.Product).WithMany().HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Restrict)`
  - `HasOne(w => w.Warehouse).WithMany().HasForeignKey(w => w.WarehouseId).OnDelete(DeleteBehavior.Restrict)`
