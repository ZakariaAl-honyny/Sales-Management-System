# API Contracts: Purchase Invoices

**Base URL**: `http://localhost:5221/api/v1/purchase-invoices`
**Authorization**: Bearer JWT — Policy: `ManagerAndAbove` (Admin, Manager)

---

## POST /api/v1/purchase-invoices
Create a new purchase invoice in Draft status.

**Request Body**
```json
{
  "supplierId": 1,
  "warehouseId": 1,
  "invoiceDate": "2026-05-07T00:00:00Z",
  "dueDate": "2026-06-07",
  "paymentType": 2,
  "discountAmount": 0.00,
  "taxAmount": 0.00,
  "paidAmount": 0.00,
  "notes": "string|null",
  "items": [
    {
      "productId": 1,
      "quantity": 10.000,
      "unitCost": 50.00,
      "discountAmount": 0.00
    }
  ]
}
```

**Validation Rules**
- `supplierId` required, > 0
- `warehouseId` required, > 0
- `paymentType` one of [1, 2, 3]
- `paidAmount` >= 0
- `items` required, at least 1 item
- Each item: `productId` > 0, `quantity` > 0, `unitCost` >= 0

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `PurchaseInvoiceDto` | Invoice created in Draft |
| 400 Bad Request | `{ "error": "...", "errorCode": "VALIDATION_ERROR" }` | Validation failure |
| 401 Unauthorized | — | No/invalid token |
| 403 Forbidden | — | Cashier role |

---

## GET /api/v1/purchase-invoices
List purchase invoices.

**Query Parameters**
- `status`, `supplierId`, `warehouseId`, `fromDate`, `toDate`, `page`, `pageSize`

**Response**: `200 OK` → `PagedResult<PurchaseInvoiceDto>`

---

## GET /api/v1/purchase-invoices/{id}

**Response**: `200 OK` → `PurchaseInvoiceDto` | `404 Not Found`

---

## POST /api/v1/purchase-invoices/{id}/post
Post a Draft purchase invoice.

**Business Rules Enforced**:
1. Invoice saved → gets ID
2. Stock increased per line in destination warehouse
3. `PurchaseIn` `InventoryMovement` created per line
4. Supplier balance increased by `DueAmount` if > 0
5. All in a single transaction

**Responses**: `200 OK` (Posted) | `400` (wrong state) | `404`

---

## POST /api/v1/purchase-invoices/{id}/cancel
Cancel a Draft or Posted purchase invoice.

**Business Rules Enforced** (Posted → Cancelled):
1. Stock decreased per line (reversed) → `PurchaseReturnOut` movement per line
2. Supplier balance decreased by original `DueAmount`
3. All in a single transaction

**Responses**: `200 OK` (Cancelled) | `400` (terminal state) | `404`

---

## PurchaseInvoiceDto (Response Shape)
```json
{
  "id": 1,
  "invoiceNo": "PUR-2026-000001",
  "supplierId": 1,
  "supplierName": "Main Supplier",
  "warehouseId": 1,
  "warehouseName": "Main Warehouse",
  "invoiceDate": "2026-05-07T00:00:00Z",
  "dueDate": "2026-06-07",
  "paymentType": 2,
  "paymentTypeName": "Credit",
  "subTotal": 500.00,
  "discountAmount": 0.00,
  "taxAmount": 0.00,
  "totalAmount": 500.00,
  "paidAmount": 0.00,
  "dueAmount": 500.00,
  "status": 2,
  "statusName": "Posted",
  "notes": null,
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product A",
      "quantity": 10.000,
      "unitCost": 50.00,
      "discountAmount": 0.00,
      "lineTotal": 500.00
    }
  ]
}
```
