# API Contracts: Returns (Sales & Purchase)

---

## Sales Returns

**Base URL**: `http://localhost:5221/api/v1/sales-returns`
**Authorization**: Bearer JWT — Policy: `AllStaff`

### POST /api/v1/sales-returns
Create and post a sales return.

**Request Body**
```json
{
  "salesInvoiceId": 1,
  "customerId": 1,
  "warehouseId": 1,
  "returnDate": "2026-05-07T00:00:00Z",
  "notes": "string|null",
  "items": [
    {
      "productId": 1,
      "quantity": 2.000,
      "unitPrice": 75.00,
      "discountAmount": 0.00
    }
  ]
}
```

**Validation Rules**
- `warehouseId` required, > 0
- `salesInvoiceId` optional (null = standalone return)
- `items` required, at least 1 item
- Each item: `productId` > 0, `quantity` > 0, `unitPrice` >= 0
- Return quantity per line must not exceed original invoice line quantity

**Business Rules Enforced on Post**:
1. Validate return quantities against original invoice (if referenced)
2. Stock increased in warehouse → `SaleReturnIn` movement per line
3. Customer balance decreased by return `TotalAmount`
4. All in a single transaction

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `SalesReturnDto` | Return created and posted |
| 400 Bad Request | `{ "error": "Return quantity (5) exceeds original sold quantity (3)..." }` | Quantity validation |
| 400 Bad Request | `{ "error": "..." }` | Other validation |
| 404 Not Found | — | Referenced invoice not found |

### GET /api/v1/sales-returns
List sales returns. Query params: `customerId`, `salesInvoiceId`, `page`, `pageSize`

### GET /api/v1/sales-returns/{id}
Get a single sales return with items.

---

## Purchase Returns

**Base URL**: `http://localhost:5221/api/v1/purchase-returns`
**Authorization**: Bearer JWT — Policy: `ManagerAndAbove`

### POST /api/v1/purchase-returns
Create and post a purchase return.

**Request Body**
```json
{
  "purchaseInvoiceId": 1,
  "supplierId": 1,
  "warehouseId": 1,
  "returnDate": "2026-05-07T00:00:00Z",
  "notes": "string|null",
  "items": [
    {
      "productId": 1,
      "quantity": 3.000,
      "unitCost": 50.00,
      "discountAmount": 0.00
    }
  ]
}
```

**Validation Rules**
- `supplierId` required, > 0
- `warehouseId` required, > 0
- Return quantity per line must not exceed original invoice line quantity
- Sufficient stock must exist in the warehouse (returning from)

**Business Rules Enforced on Post**:
1. Validate return quantities against original invoice (if referenced)
2. Validate sufficient stock in source warehouse
3. Stock decreased in warehouse → `PurchaseReturnOut` movement per line
4. Supplier balance decreased by return `TotalAmount`
5. All in a single transaction

**Responses**: `201 Created` (SalesReturnDto) | `400` (quantity/stock) | `404`

### GET /api/v1/purchase-returns
### GET /api/v1/purchase-returns/{id}

---

## SalesReturnDto / PurchaseReturnDto (Response Shape)
```json
{
  "id": 1,
  "returnNo": "SR-2026-000001",
  "salesInvoiceId": 1,
  "customerId": 1,
  "customerName": "Cash Customer",
  "warehouseId": 1,
  "warehouseName": "Main Warehouse",
  "returnDate": "2026-05-07T00:00:00Z",
  "totalAmount": 150.00,
  "status": 2,
  "statusName": "Posted",
  "notes": null,
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product A",
      "quantity": 2.000,
      "unitPrice": 75.00,
      "discountAmount": 0.00,
      "lineTotal": 150.00
    }
  ]
}
```
