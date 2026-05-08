# API Contracts: Sales Invoices

**Base URL**: `http://localhost:5221/api/v1/sales-invoices`
**Authorization**: Bearer JWT — Policy: `AllStaff` (Admin, Manager, Cashier)

---

## POST /api/v1/sales-invoices
Create a new sales invoice in Draft status.

**Request Body**
```json
{
  "customerId": 1,
  "warehouseId": 1,
  "invoiceDate": "2026-05-07T00:00:00Z",
  "dueDate": "2026-05-14",
  "paymentType": 1,
  "discountAmount": 0.00,
  "taxAmount": 0.00,
  "paidAmount": 150.00,
  "notes": "string|null",
  "items": [
    {
      "productId": 1,
      "quantity": 3.000,
      "unitPrice": 75.00,
      "discountAmount": 0.00
    }
  ]
}
```

**Validation Rules**
- `warehouseId` required, > 0
- `paymentType` required, one of [1, 2, 3]
- `paidAmount` >= 0
- `discountAmount` >= 0, `taxAmount` >= 0
- `items` required, at least 1 item
- Each item: `productId` > 0, `quantity` > 0, `unitPrice` >= 0

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `SalesInvoiceDto` | Invoice created in Draft |
| 400 Bad Request | `{ "error": "...", "errorCode": "..." }` | Validation failure |
| 401 Unauthorized | — | No/invalid token |
| 403 Forbidden | — | Wrong role |

---

## GET /api/v1/sales-invoices
List sales invoices with optional filtering.

**Query Parameters**
- `status` (int, optional): 1=Draft, 2=Posted, 3=Cancelled
- `customerId` (int, optional)
- `warehouseId` (int, optional)
- `fromDate` / `toDate` (ISO date, optional)
- `page` (int, default 1), `pageSize` (int, default 20)

**Response**: `200 OK` → `PagedResult<SalesInvoiceDto>`

---

## GET /api/v1/sales-invoices/{id}
Get a single sales invoice with all items.

**Response**: `200 OK` → `SalesInvoiceDto` | `404 Not Found`

---

## POST /api/v1/sales-invoices/{id}/post
Post a Draft invoice. Triggers stock validation, stock decrease, movement recording, and balance update.

**Request Body**: (empty)

**Business Rules Enforced**:
1. Stock validated per line BEFORE transaction opens
2. Invoice saved → gets ID
3. Stock decreased per line → `InventoryMovement` created per line
4. Customer balance increased by `DueAmount` if > 0
5. All in a single transaction — rolls back on any failure

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `SalesInvoiceDto` (status=Posted) | Success |
| 400 Bad Request | `{ "error": "Insufficient stock for product X..." }` | Stock shortage |
| 400 Bad Request | `{ "error": "Only draft invoices can be posted." }` | Wrong state |
| 404 Not Found | — | Invoice not found |

---

## POST /api/v1/sales-invoices/{id}/cancel
Cancel a Draft or Posted invoice.

**Business Rules Enforced** (Posted → Cancelled only):
1. Stock restored per line → `SaleReturnIn` movement per line
2. Customer balance decreased by original `DueAmount`
3. All in a single transaction

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `SalesInvoiceDto` (status=Cancelled) | Success |
| 400 Bad Request | `{ "error": "Invoice is already cancelled." }` | Terminal state |
| 404 Not Found | — | Invoice not found |

---

## SalesInvoiceDto (Response Shape)
```json
{
  "id": 1,
  "invoiceNo": "INV-2026-000001",
  "customerId": 1,
  "customerName": "Cash Customer",
  "warehouseId": 1,
  "warehouseName": "Main Warehouse",
  "invoiceDate": "2026-05-07T00:00:00Z",
  "dueDate": "2026-05-14",
  "paymentType": 1,
  "paymentTypeName": "Cash",
  "subTotal": 225.00,
  "discountAmount": 0.00,
  "taxAmount": 0.00,
  "totalAmount": 225.00,
  "paidAmount": 225.00,
  "dueAmount": 0.00,
  "status": 2,
  "statusName": "Posted",
  "notes": null,
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product A",
      "quantity": 3.000,
      "unitPrice": 75.00,
      "discountAmount": 0.00,
      "lineTotal": 225.00
    }
  ]
}
```
