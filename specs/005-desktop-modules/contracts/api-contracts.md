# API Contracts: Desktop ↔ API (005-desktop-modules)

**Branch**: `005-desktop-modules` | **Date**: 2026-05-08  
**Style**: REST/JSON over HTTP | **Auth**: JWT Bearer

---

## Base URL & Auth

```
Base URL:  http://localhost:5000/api   (development)
Auth:      Authorization: Bearer {jwt_token}
Encoding:  application/json  (UTF-8, supports Arabic nvarchar fields)
```

All endpoints require `Authorization: Bearer {token}` except `/api/auth/login`.

---

## 1. Products

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/products` | ManagerAndAbove | List products (search, categoryId, includeInactive) |
| GET | `/api/products/{id}` | ManagerAndAbove | Get product by ID |
| GET | `/api/products/barcode/{barcode}` | AllStaff | Lookup by barcode (invoice entry) |
| POST | `/api/products` | ManagerAndAbove | Create product |
| PUT | `/api/products/{id}` | ManagerAndAbove | Update product |
| PUT | `/api/products/{id}/deactivate` | ManagerAndAbove | Soft-delete |
| PUT | `/api/products/{id}/reactivate` | ManagerAndAbove | Re-activate |

**Query params (GET /api/products)**:
- `search` (string) — filters Name, Code, Barcode
- `categoryId` (int?) — filter by category
- `includeInactive` (bool, default false)

---

## 2. Categories & Units

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/categories` | ManagerAndAbove | List categories |
| POST | `/api/categories` | ManagerAndAbove | Create category |
| DELETE | `/api/categories/{id}` | ManagerAndAbove | Delete category (if no products) |
| GET | `/api/units` | ManagerAndAbove | List units |
| POST | `/api/units` | ManagerAndAbove | Create unit |
| DELETE | `/api/units/{id}` | ManagerAndAbove | Delete unit (if no products) |

---

## 3. Customers

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/customers` | AllStaff | List customers (search, includeInactive) |
| GET | `/api/customers/{id}` | AllStaff | Get by ID |
| POST | `/api/customers` | ManagerAndAbove | Create |
| PUT | `/api/customers/{id}` | ManagerAndAbove | Update |
| PUT | `/api/customers/{id}/deactivate` | ManagerAndAbove | Soft-delete |
| PUT | `/api/customers/{id}/reactivate` | ManagerAndAbove | Re-activate |

---

## 4. Suppliers

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/suppliers` | ManagerAndAbove | List suppliers |
| GET | `/api/suppliers/{id}` | ManagerAndAbove | Get by ID |
| POST | `/api/suppliers` | ManagerAndAbove | Create |
| PUT | `/api/suppliers/{id}` | ManagerAndAbove | Update |
| PUT | `/api/suppliers/{id}/deactivate` | ManagerAndAbove | Soft-delete |
| PUT | `/api/suppliers/{id}/reactivate` | ManagerAndAbove | Re-activate |

---

## 5. Warehouses

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/warehouses` | AllStaff | List warehouses |
| GET | `/api/warehouses/{id}` | AllStaff | Get by ID |
| GET | `/api/warehouses/{id}/stock` | ManagerAndAbove | Get per-product stock |
| POST | `/api/warehouses` | AdminOnly | Create |
| PUT | `/api/warehouses/{id}` | AdminOnly | Update |
| PUT | `/api/warehouses/{id}/set-default` | AdminOnly | Set as default |

---

## 6. Sales Invoices

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/sales-invoices` | AllStaff | List (dateFrom, dateTo, status) |
| GET | `/api/sales-invoices/{id}` | AllStaff | Get with line items |
| POST | `/api/sales-invoices` | AllStaff | Create draft |
| PUT | `/api/sales-invoices/{id}` | AllStaff | Update draft |
| PUT | `/api/sales-invoices/{id}/post` | AllStaff | Post (triggers stock+balance) |
| PUT | `/api/sales-invoices/{id}/cancel` | ManagerAndAbove | Cancel (reverses stock+balance) |

**Request body (POST/PUT)**:
```json
{
  "customerId": 1,
  "warehouseId": 1,
  "paymentType": 1,
  "paidAmount": 500.00,
  "invoiceDiscount": 0.00,
  "taxRate": 15.00,
  "isTaxInclusive": false,
  "notes": null,
  "items": [
    { "productId": 1, "quantity": 2.000, "unitPrice": 100.00, "discountAmount": 0.00 }
  ]
}
```

---

## 7. Purchase Invoices

Mirror of Sales Invoices with `/api/purchase-invoices`, `supplierId` instead of `customerId`, `unitCost` instead of `unitPrice`.

---

## 8. Sales Returns

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/sales-returns` | AllStaff | List |
| GET | `/api/sales-returns/{id}` | AllStaff | Get |
| POST | `/api/sales-returns` | AllStaff | Create draft |
| PUT | `/api/sales-returns/{id}/post` | AllStaff | Post |
| PUT | `/api/sales-returns/{id}/cancel` | ManagerAndAbove | Cancel |

**Request includes optional `originalSalesInvoiceId` for quantity validation.**

---

## 9. Purchase Returns

Mirror of Sales Returns with `/api/purchase-returns`.

---

## 10. Stock Transfers

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/stock-transfers` | ManagerAndAbove | List |
| GET | `/api/stock-transfers/{id}` | ManagerAndAbove | Get |
| POST | `/api/stock-transfers` | ManagerAndAbove | Create draft |
| PUT | `/api/stock-transfers/{id}/post` | ManagerAndAbove | Post (deduct source, add dest) |
| PUT | `/api/stock-transfers/{id}/cancel` | ManagerAndAbove | Cancel |

**Validation**: `sourceWarehouseId != destinationWarehouseId` enforced at API layer.

---

## 11. Customer Payments

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/customer-payments` | ManagerAndAbove | List (customerId?, dateFrom, dateTo) |
| POST | `/api/customer-payments` | ManagerAndAbove | Record payment |

**Overpayment allowed**: A payment exceeding `CurrentBalance` results in negative balance (credit).

---

## 12. Supplier Payments

Mirror of Customer Payments with `/api/supplier-payments`.

---

## 13. Reports

| Method | Path | Role | Query Params |
|--------|------|------|-------------|
| GET | `/api/reports/daily-sales` | ManagerAndAbove | dateFrom, dateTo |
| GET | `/api/reports/daily-purchases` | ManagerAndAbove | dateFrom, dateTo |
| GET | `/api/reports/stock` | ManagerAndAbove | warehouseId? |
| GET | `/api/reports/customer-balance` | ManagerAndAbove | customerId? |
| GET | `/api/reports/supplier-balance` | ManagerAndAbove | supplierId? |
| GET | `/api/reports/product-movement` | ManagerAndAbove | productId?, dateFrom, dateTo |
| GET | `/api/reports/low-stock` | ManagerAndAbove | — |

All report responses are `application/json` arrays of flat objects that the Desktop converts to `DataTable` for display and export.

---

## 14. Dashboard

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/api/dashboard/summary` | AllStaff | Today's totals, counts |

**Response**:
```json
{
  "todaySalesTotal": 12500.00,
  "todayPurchasesTotal": 4200.00,
  "totalCustomers": 248,
  "totalProducts": 412,
  "lowStockCount": 7,
  "recentSalesInvoices": [...]
}
```

---

## 15. Error Response Format

All error responses follow the standard format:
```json
{
  "error": "رسالة الخطأ باللغة العربية",
  "code": "INSUFFICIENT_STOCK",
  "details": { "productId": 5, "available": 2.000, "requested": 10.000 }
}
```

HTTP Status codes:
- `200 OK` — success
- `201 Created` — resource created
- `400 Bad Request` — validation failure
- `401 Unauthorized` — no/invalid token
- `403 Forbidden` — insufficient role
- `404 Not Found` — resource not found
- `409 Conflict` — business rule violation (e.g., duplicate code)
- `500 Internal Server Error` — unexpected error (never exposed in production)
