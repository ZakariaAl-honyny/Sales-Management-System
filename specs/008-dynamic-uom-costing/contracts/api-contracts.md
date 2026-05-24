# API Contracts: Dynamic UOM & Costing Engine

## `ProductUnitsController`

### `GET /api/v1/products/{productId}/units`
Returns all active units for a product.
**Response**: `Result<List<ProductUnitDto>>`

### `POST /api/v1/products/{productId}/units`
Adds a new unit to a product.
**Request**: `AddProductUnitRequest` (UnitName, ConversionFactor, RetailPrice, WholesalePrice, Barcodes)
**Response**: `Result<ProductUnitDto>`

### `PUT /api/v1/products/{productId}/units/{unitId}`
Updates an existing unit.
**Request**: `UpdateProductUnitRequest`
**Response**: `Result<ProductUnitDto>`

### `DELETE /api/v1/products/{productId}/units/{unitId}`
Soft or hard deletes a unit based on `DeleteStrategy`.
**Request Body**: `{ "strategy": 1 }` // 1 = Deactivate, 2 = Permanent
**Response**: `Result`

### `GET /api/v1/products/{productId}/price-history`
Returns the price/cost history for a product.
**Response**: `Result<List<ProductPriceHistoryDto>>`

## `BarcodesController`

### `GET /api/v1/barcodes/{barcode}`
Resolves a scanned barcode to a product and unit.
**Response**: `Result<BarcodeResolutionDto>` (ProductId, ProductUnitId, UnitName, RetailPrice, WholesalePrice)
