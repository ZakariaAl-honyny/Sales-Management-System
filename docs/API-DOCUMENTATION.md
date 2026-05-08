# Sales Management System - API Documentation

## Base URL
```
http://localhost:5221/api/v1
```

## Authentication
All endpoints (except `/auth/login`) require JWT Bearer authentication.

**Login:**
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-05-08T16:27:14Z"
}
```

---

## Endpoints Summary

| Controller | Methods | Policy |
|-------------|---------|--------|
| Auth | POST /login | Anonymous |
| Products | GET, POST, PUT, DELETE | ManagerAndAbove |
| Categories | GET, POST, PUT, DELETE | ManagerAndAbove |
| Units | GET, POST, PUT, DELETE | ManagerAndAbove |
| Customers | GET (AllStaff), CRUD (ManagerAndAbove) | Mixed |
| Suppliers | GET, POST, PUT, DELETE | ManagerAndAbove |
| Warehouses | GET, POST, PUT, DELETE | AdminOnly |
| SalesInvoices | GET, POST, POST/{id}/post, POST/{id}/cancel | AllStaff |
| PurchaseInvoices | GET, POST, POST/{id}/post, POST/{id}/cancel | ManagerAndAbove |
| SalesReturns | GET, POST | AllStaff |
| PurchaseReturns | GET, POST | ManagerAndAbove |
| StockTransfers | GET, POST | ManagerAndAbove |
| Payments (customer) | GET, POST | AllStaff |
| Payments (supplier) | GET, POST | ManagerAndAbove |
| Inventory | GET /stock, /movements, /warehouse-stocks | ManagerAndAbove |

---

## Response Types

### Success Responses
- `200 OK` - Successful GET, PUT, POST actions
- `201 Created` - Successful POST (create) actions

### Error Responses
- `400 Bad Request` - Validation errors or business logic errors
- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - User lacks required role
- `404 Not Found` - Resource not found

---

## Common DTOs

### PagedResult
```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

### ProductDto
```json
{
  "id": 1,
  "code": "P001",
  "name": "Product Name",
  "barcode": "123456789",
  "categoryId": 1,
  "categoryName": "Category",
  "unitId": 1,
  "unitName": "Piece",
  "salePrice": 100.00,
  "purchasePrice": 80.00,
  "minStock": 10,
  "isActive": true
}
```

### SalesInvoiceDto
```json
{
  "id": 1,
  "invoiceNo": "INV-2026-000001",
  "customerId": 1,
  "customerName": "Customer Name",
  "warehouseId": 1,
  "warehouseName": "Main Warehouse",
  "invoiceDate": "2026-05-08T00:00:00",
  "status": 2,
  "subTotal": 1000.00,
  "discountAmount": 0.00,
  "taxAmount": 100.00,
  "totalAmount": 1100.00,
  "paidAmount": 500.00,
  "dueAmount": 600.00,
  "items": []
}
```

---

## Role Policies

| Policy | Roles | Description |
|--------|-------|-------------|
| `AdminOnly` | Admin (1) | Full system access |
| `ManagerAndAbove` | Admin, Manager | Create, edit, delete |
| `AllStaff` | Admin, Manager, Cashier | View and create |

---

## Invoice Statuses

| Status | Value | Description |
|--------|-------|-------------|
| Draft | 1 | Created, not posted |
| Posted | 2 | Finalized, affects stock/balances |
| Cancelled | 3 | Cancelled, reversed stock/balances |

---

## Movement Types (Inventory)

| Type | Value | Description |
|------|-------|-------------|
| PurchaseIn | 1 | Stock received from purchase |
| SaleOut | 2 | Stock sold |
| SaleReturnIn | 3 | Customer returned items |
| PurchaseReturnOut | 4 | Returned to supplier |
| TransferOut | 5 | Stock transferred out |
| TransferIn | 6 | Stock transferred in |
| Adjustment | 7 | Manual adjustment |

---

## Payment Types

| Type | Value | Description |
|------|-------|-------------|
| Cash | 1 | Cash payment |
| Credit | 2 | Credit payment |
| Mixed | 3 | Combination |

---

## Error Response Format

```json
{
  "error": "Error message in Arabic or English"
}
```

---

## Scalar UI

Access the interactive API documentation at:
```
http://localhost:5221/scalar/v1
```