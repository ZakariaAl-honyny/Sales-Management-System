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
# Sales Management System API Documentation

**Base URL:** `http://localhost:5221`  
**Version:** v1  
**Authentication:** Bearer JWT Token (except login endpoint)

---

## Table of Contents

1. [Authentication](#authentication)
2. [Categories](#categories)
3. [Products](#products)
4. [Customers](#customers)
5. [Suppliers](#suppliers)
6. [Warehouses](#warehouses)
7. [Units](#units)
8. [Settings](#settings)
9. [Backup](#backup)
10. [Reports](#reports)
11. [Health Check](#health-check)
12. [Parties](#parties-v410)
13. [ProductPrices](#productprices-v410)
14. [InventoryBatches](#inventorybatches-v410)
15. [InventoryTransactions](#inventorytransactions-v410)
16. [WarehouseTransfers](#warehousetransfers-v410)
17. [CustomerReceipts](#customerreceipts-v410)
18. [ReceiptVouchers](#receiptvouchers-سندات-قبض)
19. [PaymentVouchers](#paymentvouchers-سندات-صرف)
20. [Expenses](#expenses)

---

## Authentication

### POST /api/v1/auth/login

Login and get JWT token.

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "admin",
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
| Parties | GET, POST, PUT, DELETE | AllStaff (GET), Manager+ (write) |
| ProductPrices | GET, POST, PUT, DELETE | ManagerAndAbove |
| InventoryBatches | GET | ManagerAndAbove |
| InventoryTransactions | GET | ManagerAndAbove |
| WarehouseTransfers | GET, POST, POST/{id}/post, POST/{id}/cancel | ManagerAndAbove |
| CustomerReceipts | GET, POST, POST/{id}/post, POST/{id}/cancel | AllStaff |
| ReceiptVouchers | GET, POST, POST/{id}/post, POST/{id}/cancel | AllStaff |
| PaymentVouchers | GET, POST, POST/{id}/post, POST/{id}/cancel | ManagerAndAbove |
| Expenses | GET, POST, POST/{id}/post, POST/{id}/cancel | ManagerAndAbove |

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
  "wholesaleUnitId": 2,
  "wholesaleUnitName": "كرتون",
  "retailUnitId": 1,
  "retailUnitName": "قطعة",
  "conversionFactor": 12.000,
  "purchasePrice": 80.00,
  "wholesalePrice": 95.00,
  "retailPrice": 10.00,
  "minStock": 10,
**Response (200):**
```json
{
  "userId": 1,
  "userName": "admin",
  "fullName": "Administrator",
  "role": 1,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-05-07T15:30:19.9160469Z"
}
```

**Response (401):**
```json
{
  "error": "Invalid credentials"
}
```

---

## Categories

**Base URL:** `/api/v1/categories`  
**Authorization:** Manager+ (Role 1 or 2)

### GET /api/v1/categories

Get all categories with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "name": "Electronics",
      "description": "Electronic products",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/categories/{id}

Get category by ID.

**Response (200):**
```json
{
  "id": 1,
  "name": "Electronics",
  "description": "Electronic products",
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "الفئة غير موجودة"
}
```

### POST /api/v1/categories

Create new category.

**Request Body:**
```json
{
  "name": "New Category",
  "description": "Category description"
}
```

**Response (201):**
```json
{
  "id": 2,
  "name": "New Category",
  "description": "Category description",
  "isActive": true
}
```

**Validation Errors (400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["اسم الفئة مطلوب"]
  }
}
```

### PUT /api/v1/categories/{id}

Update category.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Category",
  "description": "Updated description",
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "name": "Updated Category",
  "description": "Updated description",
  "isActive": true
}
```

### DELETE /api/v1/categories/{id}

Delete category.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "الفئة غير موجودة"
}
```

---

## Products

**Base URL:** `/api/v1/products`  
**Authorization:** All Staff (GET), Manager+ (POST/PUT/DELETE)

### GET /api/v1/products

Get all products with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name or code |
| categoryId | int | No | Filter by category |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "code": "PROD001",
      "barcode": "123456789",
      "name": "Product Name",
      "categoryId": 1,
      "categoryName": "Electronics",
      "wholesaleUnitId": 2,
      "wholesaleUnitName": "كرتون",
      "retailUnitId": 1,
      "retailUnitName": "قطعة",
      "conversionFactor": 12.000,
      "purchasePrice": 100.00,
      "wholesalePrice": 1100.00,
      "retailPrice": 100.00,
      "minStock": 10,
      "description": "Product description",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/products/{id}

Get product by ID.

**Response (200):**
```json
{
  "id": 1,
  "code": "PROD001",
  "barcode": "123456789",
  "name": "Product Name",
  "categoryId": 1,
  "categoryName": "Electronics",
  "unitId": 1,
  "unitName": "قطعة",
  "purchasePrice": 100.00,
  "salePrice": 150.00,
  "minStock": 10,
  "description": "Product description",
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
**Response (404):**
```json
{
  "error": "المنتج غير موجود"
}
```

### POST /api/v1/products

Create new product.

**Request Body:**
```json
{
  "name": "New Product",
  "code": "PROD002",
  "barcode": "987654321",
  "categoryId": 1,
  "wholesaleUnitId": 2,
  "retailUnitId": 1,
  "conversionFactor": 12,
  "purchasePrice": 100.00,
  "wholesalePrice": 1100.00,
  "retailPrice": 100.00,
  "minStock": 10,
  "description": "Product description"
}
```

**Response (201):**
```json
{
  "id": 2,
  "code": "PROD002",
  "barcode": "987654321",
  "name": "New Product",
  "categoryId": 1,
  "categoryName": "Electronics",
  "unitId": 1,
  "unitName": "قطعة",
  "purchasePrice": 100.00,
  "salePrice": 150.00,
  "minStock": 10,
  "description": "Product description",
  "isActive": true
}
```

**Validation Errors (400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["اسم المنتج مطلوب"],
    "Code": ["كود المنتج مطلوب"]
  }
}
```

### PUT /api/v1/products/{id}

Update product.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Product",
  "code": "PROD001",
  "barcode": "123456789",
  "categoryId": 1,
  "unitId": 1,
  "costPrice": 120.00,
  "salePrice": 180.00,
  "minStock": 15,
  "description": "Updated description",
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "code": "PROD001",
  "barcode": "123456789",
  "name": "Updated Product",
  "categoryId": 1,
  "categoryName": "Electronics",
  "unitId": 1,
  "unitName": "قطعة",
  "purchasePrice": 120.00,
  "salePrice": 180.00,
  "minStock": 15,
  "description": "Updated description",
  "isActive": true
}
```

### DELETE /api/v1/products/{id}

Delete product.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "المنتج غير موجود"
}
```

---

## Customers

**Base URL:** `/api/v1/customers`  
**Authorization:** All Staff (GET), Manager+ (POST/PUT/DELETE)

### GET /api/v1/customers

Get all customers with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name or code |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "code": "CASH",
      "name": "عميل نقدي",
      "phone": "0123456789",
      "email": "customer@example.com",
      "address": "Cairo, Egypt",
      "openingBalance": 0.00,
      "currentBalance": 0.00,
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/customers/{id}

Get customer by ID.

**Response (200):**
```json
{
  "id": 1,
  "code": "CASH",
  "name": "عميل نقدي",
  "phone": "0123456789",
  "email": "customer@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "العميل غير موجود"
}
```

### POST /api/v1/customers

Create new customer.

**Request Body:**
```json
{
  "name": "New Customer",
  "code": "CUST001",
  "phone": "0123456789",
  "email": "new@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00
}
```

**Response (201):**
```json
{
  "id": 2,
  "code": "CUST001",
  "name": "New Customer",
  "phone": "0123456789",
  "email": "new@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

**Validation Errors (400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["اسم العميل مطلوب"],
    "Code": ["كود العميل مطلوب"]
  }
}
```

### PUT /api/v1/customers/{id}

Update customer.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Customer",
  "code": "CUST001",
  "phone": "0123456789",
  "email": "updated@example.com",
  "address": "Updated Address",
  "openingBalance": 0.00,
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "code": "CUST001",
  "name": "Updated Customer",
  "phone": "0123456789",
  "email": "updated@example.com",
  "address": "Updated Address",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

### DELETE /api/v1/customers/{id}

Delete customer.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "العميل غير موجود"
}
```

---

## Suppliers

**Base URL:** `/api/v1/suppliers`  
**Authorization:** Manager+ (Role 1 or 2)

### GET /api/v1/suppliers

Get all suppliers with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name or code |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "code": "SUP001",
      "name": "Supplier Name",
      "phone": "0123456789",
      "email": "supplier@example.com",
      "address": "Cairo, Egypt",
      "openingBalance": 0.00,
      "currentBalance": 0.00,
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/suppliers/{id}

Get supplier by ID.

**Response (200):**
```json
{
  "id": 1,
  "code": "SUP001",
  "name": "Supplier Name",
  "phone": "0123456789",
  "email": "supplier@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "المورد غير موجود"
}
```

### POST /api/v1/suppliers

Create new supplier.

**Request Body:**
```json
{
  "name": "New Supplier",
  "code": "SUP002",
  "phone": "0123456789",
  "email": "newsupplier@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00
}
```

**Response (201):**
```json
{
  "id": 2,
  "code": "SUP002",
  "name": "New Supplier",
  "phone": "0123456789",
  "email": "newsupplier@example.com",
  "address": "Cairo, Egypt",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

### PUT /api/v1/suppliers/{id}

Update supplier.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Supplier",
  "code": "SUP001",
  "phone": "0123456789",
  "email": "updated@example.com",
  "address": "Updated Address",
  "openingBalance": 0.00,
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "code": "SUP001",
  "name": "Updated Supplier",
  "phone": "0123456789",
  "email": "updated@example.com",
  "address": "Updated Address",
  "openingBalance": 0.00,
  "currentBalance": 0.00,
  "isActive": true
}
```

### DELETE /api/v1/suppliers/{id}

Delete supplier.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "المورد غير موجود"
}
```

---

## Warehouses

**Base URL:** `/api/v1/warehouses`  
**Authorization:** Admin Only (Role 1)

### GET /api/v1/warehouses

Get all warehouses with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name or code |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "code": "WH-001",
      "name": "المخزن الرئيسي",
      "location": "Cairo",
      "isDefault": true,
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/warehouses/{id}

Get warehouse by ID.

**Response (200):**
```json
{
  "id": 1,
  "code": "WH-001",
  "name": "المخزن الرئيسي",
  "location": "Cairo",
  "isDefault": true,
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "المخزن غير موجود"
}
```

### POST /api/v1/warehouses

Create new warehouse.

**Request Body:**
```json
{
  "name": "New Warehouse",
  "code": "WH-002",
  "location": "Giza",
  "isDefault": false
}
```

**Response (201):**
```json
{
  "id": 2,
  "code": "WH-002",
  "name": "New Warehouse",
  "location": "Giza",
  "isDefault": false,
  "isActive": true
}
```

### PUT /api/v1/warehouses/{id}

Update warehouse.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Warehouse",
  "code": "WH-001",
  "location": "New Location",
  "isDefault": true,
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "code": "WH-001",
  "name": "Updated Warehouse",
  "location": "New Location",
  "isDefault": true,
  "isActive": true
}
```

### DELETE /api/v1/warehouses/{id}

Delete warehouse.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "المخزن غير موجود"
}
```

---

## Units

**Base URL:** `/api/v1/units`  
**Authorization:** Manager+ (Role 1 or 2)

### GET /api/v1/units

Get all units with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "name": "قطعة",
      "symbol": "pcs",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### GET /api/v1/units/{id}

Get unit by ID.

**Response (200):**
```json
{
  "id": 1,
  "name": "قطعة",
  "symbol": "pcs",
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "الوحدة غير موجودة"
}
```

### POST /api/v1/units

Create new unit.

**Request Body:**
```json
{
  "Name": "كرتون",
  "Symbol": "ctn"
}
```

**Response (201):**
```json
{
  "id": 6,
  "name": "كرتون",
  "symbol": "ctn",
  "isActive": true
}
```

### PUT /api/v1/units/{id}

Update unit.

**Request Body:**
```json
{
  "Id": 1,
  "Name": "قطعة",
  "Symbol": "pcs",
  "IsActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "name": "قطعة",
  "symbol": "pcs",
  "isActive": true
}
```

### DELETE /api/v1/units/{id}

Delete unit.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "الوحدة غير موجودة"
}
```

---

## Settings

**Base URL:** `/api/v1/settings`  
**Authorization:** All Staff (GET), Admin Only (PUT)

### GET /api/v1/settings
Retrieve store settings (Name, Address, Tax Number, etc.).

### PUT /api/v1/settings
Update store settings. Requires `Admin` role.

---

## Backup

**Base URL:** `/api/v1/backup`  
**Authorization:** Admin Only

### GET /api/v1/backup/list
List all database backup files in the backup directory.

### POST /api/v1/backup/create
Create a new SQL Server database backup (`.bak` file).

### POST /api/v1/backup/restore
Restore database from a selected backup file.  
**Warning:** This operation terminates all connections and forces an application restart.

---

## Reports

**Base URL:** `/api/v1/reports`  
**Authorization:** Manager+

### GET /api/v1/reports/low-stock
Get report of products below reorder level with suggested order quantities in both wholesale and retail units.

---

## Health Check

### GET /api/v1/health

Check API health status.

**Response (200):**
```json
{
  "Status": "OK",
  "Version": "1.0",
  "Timestamp": "2026-05-07T12:00:00.0000000Z"
}
```

---

## Parties (v4.10)

**Base URL:** `/api/v1/parties`  
**Authorization:** All Staff (GET), Manager+ (POST/PUT/DELETE)

### GET /api/v1/parties

List all parties with pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search by name or phone |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "name": "عميل نقدي",
      "phone": "0123456789",
      "email": "cash@example.com",
      "address": "Cairo",
      "taxNumber": "123-456-789",
      "notes": "",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/parties/{id}

Get party by ID.

**Response (200):**
```json
{
  "id": 1,
  "name": "عميل نقدي",
  "phone": "0123456789",
  "email": "cash@example.com",
  "address": "Cairo",
  "taxNumber": "123-456-789",
  "notes": "",
  "isActive": true
}
```

**Response (404):**
```json
{
  "error": "الطرف غير موجود"
}
```

### POST /api/v1/parties

Create new party.

**Request Body:**
```json
{
  "name": "New Party",
  "phone": "0123456789",
  "email": "party@example.com",
  "address": "Cairo",
  "taxNumber": "123-456-789",
  "notes": ""
}
```

**Response (201):**
```json
{
  "id": 2,
  "name": "New Party",
  "phone": "0123456789",
  "email": "party@example.com",
  "address": "Cairo",
  "taxNumber": "123-456-789",
  "notes": "",
  "isActive": true
}
```

**Validation Errors (400):**
```json
{
  "errors": {
    "Name": ["اسم الطرف مطلوب"]
  }
}
```

### PUT /api/v1/parties/{id}

Update party.

**Request Body:**
```json
{
  "id": 1,
  "name": "Updated Party",
  "phone": "0987654321",
  "email": "updated@example.com",
  "address": "Giza",
  "taxNumber": "987-654-321",
  "notes": "Updated notes",
  "isActive": true
}
```

**Response (200):**
```json
{
  "id": 1,
  "name": "Updated Party",
  "phone": "0987654321",
  "email": "updated@example.com",
  "address": "Giza",
  "taxNumber": "987-654-321",
  "notes": "Updated notes",
  "isActive": true
}
```

### DELETE /api/v1/parties/{id}

Soft delete party (sets `IsActive = false`).

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

**Response (400):**
```json
{
  "error": "الطرف غير موجود"
}
```

---

## ProductPrices (v4.10)

**Base URL:** `/api/v1/products/{productId}/prices`  
**Authorization:** Manager+ (Role 1 or 2)

### GET /api/v1/products/{productId}/prices

List all prices for a product unit.

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "productUnitId": 1,
      "currencyId": 1,
      "currencyCode": "YER",
      "price": 15000.00,
      "effectiveFrom": "2026-01-01T00:00:00",
      "effectiveTo": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/products/{productId}/prices/{id}

Get specific price.

**Response (200):**
```json
{
  "id": 1,
  "productUnitId": 1,
  "currencyId": 1,
  "currencyCode": "YER",
  "price": 15000.00,
  "effectiveFrom": "2026-01-01T00:00:00",
  "effectiveTo": null
}
```

**Response (404):**
```json
{
  "error": "السعر غير موجود"
}
```

### POST /api/v1/products/{productId}/prices

Create new price.

**Request Body:**
```json
{
  "productUnitId": 1,
  "currencyId": 1,
  "price": 16000.00,
  "effectiveFrom": "2026-06-01T00:00:00",
  "effectiveTo": null
}
```

**Response (201):**
```json
{
  "id": 2,
  "productUnitId": 1,
  "currencyId": 1,
  "currencyCode": "YER",
  "price": 16000.00,
  "effectiveFrom": "2026-06-01T00:00:00",
  "effectiveTo": null
}
```

### PUT /api/v1/products/{productId}/prices/{id}

Update price.

**Request Body:**
```json
{
  "price": 17000.00,
  "effectiveFrom": "2026-06-01T00:00:00",
  "effectiveTo": "2026-12-31T00:00:00"
}
```

**Response (200):**
```json
{
  "id": 2,
  "productUnitId": 1,
  "currencyId": 1,
  "currencyCode": "YER",
  "price": 17000.00,
  "effectiveFrom": "2026-06-01T00:00:00",
  "effectiveTo": "2026-12-31T00:00:00"
}
```

### DELETE /api/v1/products/{productId}/prices/{id}

Delete price.

**Response (200):**
```json
{
  "message": "تم الحذف بنجاح",
  "id": 1
}
```

---

## InventoryBatches (v4.10)

**Base URL:** `/api/v1/inventory/batches`  
**Authorization:** Manager+

### GET /api/v1/inventory/batches

List all batches with optional filters.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| productId | int | No | Filter by product |
| warehouseId | int | No | Filter by warehouse |
| expiryBefore | date | No | Filter batches expiring before date |
| expiryAfter | date | No | Filter batches expiring after date |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product Name",
      "warehouseId": 1,
      "warehouseName": "المخزن الرئيسي",
      "batchNo": "BATCH-2026-001",
      "expiryDate": "2027-06-01",
      "quantityReceived": 100.000,
      "quantityRemaining": 85.000,
      "unitCost": 12000.00,
      "purchaseInvoiceId": 1
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/inventory/batches/{id}

Get batch by ID.

**Response (200):**
```json
{
  "id": 1,
  "productId": 1,
  "productName": "Product Name",
  "warehouseId": 1,
  "warehouseName": "المخزن الرئيسي",
  "batchNo": "BATCH-2026-001",
  "expiryDate": "2027-06-01",
  "quantityReceived": 100.000,
  "quantityRemaining": 85.000,
  "unitCost": 12000.00,
  "purchaseInvoiceId": 1
}
```

### GET /api/v1/inventory/batches/{id}/movements

Get all inventory movements for a specific batch.

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "transactionId": 10,
      "movementType": "SaleOut",
      "quantity": -15.000,
      "unitCost": 12000.00,
      "referenceType": "SalesInvoice",
      "referenceId": 5,
      "createdAt": "2026-06-10T14:30:00"
    }
  ]
}
```

---

## InventoryTransactions (v4.10)

**Base URL:** `/api/v1/inventory/transactions`  
**Authorization:** Manager+

### GET /api/v1/inventory/transactions

List inventory transactions with filters.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| warehouseId | int | No | Filter by warehouse |
| type | int | No | Filter by movement type (1-7) |
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "referenceType": "PurchaseInvoice",
      "referenceId": 1,
      "warehouseId": 1,
      "warehouseName": "المخزن الرئيسي",
      "notes": "استلام مشتريات",
      "createdAt": "2026-06-01T10:00:00",
      "lines": [
        {
          "productId": 1,
          "productName": "Product Name",
          "quantity": 100.000,
          "unitCost": 12000.00
        }
      ]
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/inventory/transactions/{id}

Get transaction with its lines.

**Response (200):**
```json
{
  "id": 1,
  "referenceType": "PurchaseInvoice",
  "referenceId": 1,
  "warehouseId": 1,
  "warehouseName": "المخزن الرئيسي",
  "notes": "استلام مشتريات",
  "createdAt": "2026-06-01T10:00:00",
  "lines": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Product Name",
      "productUnitId": 1,
      "quantity": 100.000,
      "unitCost": 12000.00,
      "batchNo": "BATCH-2026-001",
      "expiryDate": "2027-06-01"
    }
  ]
}
```

---

## WarehouseTransfers (v4.10)

**Base URL:** `/api/v1/warehouse-transfers`  
**Authorization:** Manager+

### GET /api/v1/warehouse-transfers

List warehouse transfers.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | int | No | Filter by status (1=Draft, 2=Posted, 3=Cancelled) |
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "transferNo": "TRF-2026-000001",
      "fromWarehouseId": 1,
      "fromWarehouseName": "المخزن الرئيسي",
      "toWarehouseId": 2,
      "toWarehouseName": "مخزن العرض",
      "status": 1,
      "notes": "تحويل مخزون",
      "createdAt": "2026-06-10T09:00:00",
      "totalItems": 2
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/warehouse-transfers/{id}

Get transfer with lines.

**Response (200):**
```json
{
  "id": 1,
  "transferNo": "TRF-2026-000001",
  "fromWarehouseId": 1,
  "fromWarehouseName": "المخزن الرئيسي",
  "toWarehouseId": 2,
  "toWarehouseName": "مخزن العرض",
  "status": 1,
  "notes": "تحويل مخزون",
  "createdAt": "2026-06-10T09:00:00",
  "lines": [
    {
      "id": 1,
      "productUnitId": 1,
      "productName": "Product Name",
      "quantity": 50.000,
      "batchNo": "BATCH-2026-001"
    }
  ]
}
```

### POST /api/v1/warehouse-transfers

Create new transfer (draft).

**Request Body:**
```json
{
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "notes": "تحويل مخزون",
  "items": [
    {
      "productUnitId": 1,
      "quantity": 50.000,
      "batchNo": "BATCH-2026-001"
    }
  ]
}
```

**Response (201):**
```json
{
  "id": 1,
  "transferNo": "TRF-2026-000001",
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "status": 1,
  "notes": "تحويل مخزون",
  "lines": [
    {
      "productUnitId": 1,
      "quantity": 50.000
    }
  ]
}
```

### PUT /api/v1/warehouse-transfers/{id}

Update draft transfer.

**Request Body:**
```json
{
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "notes": "Updated notes",
  "items": [
    {
      "productUnitId": 1,
      "quantity": 75.000,
      "batchNo": "BATCH-2026-001"
    }
  ]
}
```

**Response (200):**
```json
{
  "id": 1,
  "notes": "Updated notes",
  "status": 1
}
```

### POST /api/v1/warehouse-transfers/{id}/post

Post draft transfer — deducts stock from source warehouse, adds to destination.

**Response (200):**
```json
{
  "id": 1,
  "status": 2,
  "message": "تم ترحيل التحويل بنجاح"
}
```

**Response (400):**
```json
{
  "error": "المخزون غير كافٍ في المخزن المصدر"
}
```

### POST /api/v1/warehouse-transfers/{id}/cancel

Cancel posted transfer — reverses stock movement.

**Response (200):**
```json
{
  "id": 1,
  "status": 3,
  "message": "تم إلغاء التحويل بنجاح"
}
```

---

## CustomerReceipts (v4.10)

**Base URL:** `/api/v1/customer-receipts`  
**Authorization:** All Staff

### GET /api/v1/customer-receipts

List customer receipts.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| customerId | int | No | Filter by customer |
| status | int | No | Filter by status (1=Draft, 2=Posted, 3=Cancelled) |
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "receiptNo": "CP-2026-000001",
      "customerId": 1,
      "customerName": "عميل نقدي",
      "amount": 5000.00,
      "paymentMethod": "Cash",
      "cashBoxId": 1,
      "cashBoxName": "الصندوق الرئيسي",
      "status": 2,
      "receiptDate": "2026-06-10T00:00:00",
      "notes": "دفعة على فاتورة",
      "createdAt": "2026-06-10T10:00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/customer-receipts/{id}

Get receipt with payment allocations.

**Response (200):**
```json
{
  "id": 1,
  "receiptNo": "CP-2026-000001",
  "customerId": 1,
  "customerName": "عميل نقدي",
  "amount": 5000.00,
  "paymentMethod": "Cash",
  "cashBoxId": 1,
  "cashBoxName": "الصندوق الرئيسي",
  "currencyId": 1,
  "currencyCode": "YER",
  "exchangeRate": 1.00,
  "status": 2,
  "receiptDate": "2026-06-10T00:00:00",
  "notes": "دفعة على فاتورة",
  "allocations": [
    {
      "invoiceId": 1,
      "invoiceType": "SalesInvoice",
      "allocatedAmount": 5000.00
    }
  ]
}
```

### POST /api/v1/customer-receipts

Create new receipt (draft).

**Request Body:**
```json
{
  "customerId": 1,
  "amount": 5000.00,
  "paymentMethod": "Cash",
  "cashBoxId": 1,
  "currencyId": 1,
  "exchangeRate": 1.00,
  "receiptDate": "2026-06-10",
  "notes": "دفعة على فاتورة",
  "allocations": [
    {
      "invoiceId": 1,
      "invoiceType": "SalesInvoice",
      "allocatedAmount": 5000.00
    }
  ]
}
```

**Response (201):**
```json
{
  "id": 2,
  "receiptNo": "CP-2026-000002",
  "status": 1,
  "amount": 5000.00
}
```

### POST /api/v1/customer-receipts/{id}/post

Post receipt — adds cash transaction and updates customer balance.

**Response (200):**
```json
{
  "id": 2,
  "status": 2,
  "message": "تم ترحيل السند بنجاح"
}
```

### POST /api/v1/customer-receipts/{id}/cancel

Cancel receipt — reverses cash transaction and customer balance.

**Response (200):**
```json
{
  "id": 2,
  "status": 3,
  "message": "تم إلغاء السند بنجاح"
}
```

---

## ReceiptVouchers (سندات قبض)

**Base URL:** `/api/v1/vouchers/receipt`  
**Authorization:** All Staff

### GET /api/v1/vouchers/receipt

List receipt vouchers.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| status | int | No | Filter by status |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "voucherNo": "RV-2026-000001",
      "amount": 10000.00,
      "fromPartyName": "مورد نقدي",
      "cashBoxId": 1,
      "cashBoxName": "الصندوق الرئيسي",
      "status": 2,
      "voucherDate": "2026-06-10T00:00:00",
      "notes": "سند قبض",
      "createdAt": "2026-06-10T10:00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/vouchers/receipt/{id}

Get receipt voucher by ID.

**Response (200):**
```json
{
  "id": 1,
  "voucherNo": "RV-2026-000001",
  "partyId": 1,
  "fromPartyName": "مورد نقدي",
  "amount": 10000.00,
  "cashBoxId": 1,
  "cashBoxName": "الصندوق الرئيسي",
  "currencyId": 1,
  "exchangeRate": 1.00,
  "status": 2,
  "voucherDate": "2026-06-10T00:00:00",
  "notes": "سند قبض",
  "referenceType": null,
  "referenceId": null
}
```

### POST /api/v1/vouchers/receipt

Create receipt voucher.

**Request Body:**
```json
{
  "partyId": 1,
  "amount": 10000.00,
  "cashBoxId": 1,
  "currencyId": 1,
  "exchangeRate": 1.00,
  "voucherDate": "2026-06-10",
  "notes": "سند قبض"
}
```

**Response (201):**
```json
{
  "id": 2,
  "voucherNo": "RV-2026-000002",
  "status": 1,
  "amount": 10000.00
}
```

### POST /api/v1/vouchers/receipt/{id}/post

Post receipt voucher.

**Response (200):**
```json
{
  "id": 2,
  "status": 2,
  "message": "تم ترحيل سند القبض بنجاح"
}
```

### POST /api/v1/vouchers/receipt/{id}/cancel

Cancel receipt voucher.

**Response (200):**
```json
{
  "id": 2,
  "status": 3,
  "message": "تم إلغاء سند القبض بنجاح"
}
```

---

## PaymentVouchers (سندات صرف)

**Base URL:** `/api/v1/vouchers/payment`  
**Authorization:** Manager+

### GET /api/v1/vouchers/payment

List payment vouchers.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| status | int | No | Filter by status |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "voucherNo": "PV-2026-000001",
      "amount": 5000.00,
      "toPartyName": "مورد نقدي",
      "cashBoxId": 1,
      "cashBoxName": "الصندوق الرئيسي",
      "status": 2,
      "voucherDate": "2026-06-10T00:00:00",
      "notes": "سند صرف",
      "createdAt": "2026-06-10T10:00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/vouchers/payment/{id}

Get payment voucher by ID.

**Response (200):**
```json
{
  "id": 1,
  "voucherNo": "PV-2026-000001",
  "partyId": 1,
  "toPartyName": "مورد نقدي",
  "amount": 5000.00,
  "cashBoxId": 1,
  "cashBoxName": "الصندوق الرئيسي",
  "currencyId": 1,
  "exchangeRate": 1.00,
  "status": 2,
  "voucherDate": "2026-06-10T00:00:00",
  "notes": "سند صرف",
  "referenceType": null,
  "referenceId": null
}
```

### POST /api/v1/vouchers/payment

Create payment voucher.

**Request Body:**
```json
{
  "partyId": 1,
  "amount": 5000.00,
  "cashBoxId": 1,
  "currencyId": 1,
  "exchangeRate": 1.00,
  "voucherDate": "2026-06-10",
  "notes": "سند صرف مورد"
}
```

**Response (201):**
```json
{
  "id": 2,
  "voucherNo": "PV-2026-000002",
  "status": 1,
  "amount": 5000.00
}
```

### POST /api/v1/vouchers/payment/{id}/post

Post payment voucher.

**Response (200):**
```json
{
  "id": 2,
  "status": 2,
  "message": "تم ترحيل سند الصرف بنجاح"
}
```

### POST /api/v1/vouchers/payment/{id}/cancel

Cancel payment voucher.

**Response (200):**
```json
{
  "id": 2,
  "status": 3,
  "message": "تم إلغاء سند الصرف بنجاح"
}
```

---

## Expenses

**Base URL:** `/api/v1/expenses`  
**Authorization:** Manager+

### GET /api/v1/expenses

List expenses.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | date | No | Start date |
| toDate | date | No | End date |
| categoryId | int | No | Filter by expense category |
| status | int | No | Filter by status |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "expenseNo": "EXP-2026-000001",
      "categoryId": 1,
      "categoryName": "إيجار",
      "amount": 50000.00,
      "cashBoxId": 1,
      "cashBoxName": "الصندوق الرئيسي",
      "status": 2,
      "expenseDate": "2026-06-01T00:00:00",
      "notes": "إيجار المحل",
      "createdAt": "2026-06-01T09:00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### GET /api/v1/expenses/{id}

Get expense by ID.

**Response (200):**
```json
{
  "id": 1,
  "expenseNo": "EXP-2026-000001",
  "categoryId": 1,
  "categoryName": "إيجار",
  "accountId": 1,
  "accountName": "مصروفات إيجار",
  "amount": 50000.00,
  "cashBoxId": 1,
  "cashBoxName": "الصندوق الرئيسي",
  "currencyId": 1,
  "exchangeRate": 1.00,
  "status": 2,
  "expenseDate": "2026-06-01T00:00:00",
  "notes": "إيجار المحل"
}
```

### POST /api/v1/expenses

Create new expense (draft).

**Request Body:**
```json
{
  "categoryId": 1,
  "amount": 50000.00,
  "cashBoxId": 1,
  "currencyId": 1,
  "exchangeRate": 1.00,
  "expenseDate": "2026-06-01",
  "notes": "إيجار المحل"
}
```

**Response (201):**
```json
{
  "id": 2,
  "expenseNo": "EXP-2026-000002",
  "status": 1,
  "amount": 50000.00
}
```

### POST /api/v1/expenses/{id}/post

Post expense — deducts from cash box and creates journal entry.

**Response (200):**
```json
{
  "id": 2,
  "status": 2,
  "message": "تم ترحيل المصروف بنجاح"
}
```

### POST /api/v1/expenses/{id}/cancel

Cancel expense — reverses cash transaction and journal entry.

**Response (200):**
```json
{
  "id": 2,
  "status": 3,
  "message": "تم إلغاء المصروف بنجاح"
}
```

---

## Error Responses

All endpoints may return the following error responses:

### 401 Unauthorized
```json
{
  "error": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "error": "Access denied"
}
```

### 404 Not Found
```json
{
  "error": "Entity not found"
}
```

### 400 Bad Request
```json
{
  "error": "Error message"
}
```

---

## Roles and Permissions

| Role | Value | Permissions |
|------|-------|-------------|
| Admin | 1 | All operations |
| Manager | 2 | Products, Customers, Suppliers, Units |
| Cashier | 3 | Read only (Products, Customers) |

---

## Using the API

### Example: Get Products

```bash
# 1. Login to get token
curl -X POST http://localhost:5221/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# 2. Use token to call protected endpoint
curl http://localhost:5221/api/v1/products \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## Swagger / Scalar UI

When running in development mode, you can access the interactive API documentation at:

**Scalar UI:** http://localhost:5221/scalar/v1  
**OpenAPI JSON:** http://localhost:5221/openapi/v1.json

---
