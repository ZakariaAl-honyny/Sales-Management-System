# API Contracts: Product Lifecycle & Media Management

**Feature**: `014-product-lifecycle`

## Updated Requests / DTOs

### `CreateProductRequest` & `UpdateProductRequest`
- **Added Fields**:
  - `DateTime? ExpirationDate`
  - `string? ImagePath`
  
### `ProductDto`
- **Added Fields**:
  - `DateTime? ExpirationDate`
  - `string? ImagePath`

## New Endpoints

### Write-Off Operations
- `POST /api/v1/inventory/writeoff`
  - Request Body:
    ```json
    {
      "productId": 105,
      "warehouseId": 1,
      "unitId": 2,
      "quantity": 10.5,
      "reason": "Expired"
    }
    ```
  - Response: `Result<StockWriteOffDto>`

### Reporting Endpoints
- `GET /api/v1/reports/expired-products`
  - Query Params:
    - `thresholdDays` (optional, default: 0 for already expired)
  - Response: `Result<List<ExpiredProductDto>>`
