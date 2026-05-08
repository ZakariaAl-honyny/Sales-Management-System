# API Contracts: Stock Transfers

**Base URL**: `http://localhost:5221/api/v1/stock-transfers`
**Authorization**: Bearer JWT — Policy: `ManagerAndAbove` (Admin, Manager)

---

## POST /api/v1/stock-transfers
Create and post a stock transfer between two warehouses atomically.

**Request Body**
```json
{
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "transferDate": "2026-05-07T00:00:00Z",
  "notes": "string|null",
  "items": [
    {
      "productId": 1,
      "quantity": 5.000,
      "notes": "string|null"
    }
  ]
}
```

**Validation Rules**
- `fromWarehouseId` required, > 0
- `toWarehouseId` required, > 0
- `fromWarehouseId` ≠ `toWarehouseId` (self-transfer is forbidden)
- `items` required, at least 1 item
- Each item: `productId` > 0, `quantity` > 0
- Sufficient stock must exist in `fromWarehouseId` for every line

**Business Rules Enforced on Post**:
1. Validate `fromWarehouseId ≠ toWarehouseId`
2. Validate stock in source warehouse per line (BEFORE transaction)
3. BEGIN TRANSACTION
4. Save StockTransfer (Draft) → get ID
5. Post transfer (Status = Posted)
6. For each item:
   - Decrease `WarehouseStock` in `fromWarehouse` → create `TransferOut` movement
   - Increase `WarehouseStock` in `toWarehouse` → create `TransferIn` movement
7. COMMIT (ROLLBACK on any failure)

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `StockTransferDto` (status=Posted) | Transfer complete |
| 400 Bad Request | `{ "error": "Source and destination warehouse cannot be the same." }` | Self-transfer |
| 400 Bad Request | `{ "error": "Insufficient stock for product X in warehouse Y..." }` | Stock shortage |
| 401 Unauthorized | — | No/invalid token |
| 403 Forbidden | — | Cashier role |

---

## GET /api/v1/stock-transfers
List stock transfers.

**Query Parameters**: `fromWarehouseId`, `toWarehouseId`, `status`, `fromDate`, `toDate`, `page`, `pageSize`

**Response**: `200 OK` → `PagedResult<StockTransferDto>`

---

## GET /api/v1/stock-transfers/{id}
Get a single stock transfer with all items and movement references.

**Response**: `200 OK` → `StockTransferDto` | `404 Not Found`

---

## POST /api/v1/stock-transfers/{id}/cancel
Cancel a Draft or Posted stock transfer.

**Business Rules** (Posted → Cancelled):
1. Reverse all stock movements atomically:
   - Increase `fromWarehouse` stock back → `TransferIn` reversal movement
   - Decrease `toWarehouse` stock back → `TransferOut` reversal movement
2. All in a single transaction

**Responses**: `200 OK` (Cancelled) | `400` (terminal) | `404`

---

## StockTransferDto (Response Shape)
```json
{
  "id": 1,
  "transferNo": "TRF-2026-000001",
  "fromWarehouseId": 1,
  "fromWarehouseName": "Main Warehouse",
  "toWarehouseId": 2,
  "toWarehouseName": "Secondary Warehouse",
  "transferDate": "2026-05-07T00:00:00Z",
  "status": 2,
  "statusName": "Posted",
  "notes": null,
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product A",
      "quantity": 5.000,
      "notes": null
    }
  ]
}
```
