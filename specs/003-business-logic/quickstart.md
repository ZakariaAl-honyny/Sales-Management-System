# Quickstart: Business Logic Verification

**Feature**: Phase 3 — Business Logic (Critical)
**Date**: 2026-05-07
**Prerequisite**: Phase 2 (Backend Core) complete and running — `http://localhost:5221`

---

## Step 0: Start the API

```powershell
# Kill any existing instances
taskkill /F /IM SalesSystem.Api.exe /T 2>$null

# Start the API
dotnet run --project SalesSystem\SalesSystem.Api\SalesSystem.Api.csproj --no-build
```

Verify: `GET http://localhost:5221/api/v1/health` → `{"status":"OK"}`

---

## Step 1: Authenticate

```http
POST http://localhost:5221/api/v1/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "admin123"
}
```

**Expected**: `200 OK` with `{ "token": "eyJ..." }`

Save the token as `{TOKEN}` for all subsequent requests.

---

## Step 2: Verify Seed Data Exists

```http
GET http://localhost:5221/api/v1/warehouses
Authorization: Bearer {TOKEN}
```
**Expected**: At least one warehouse (Main Warehouse). Note its `warehouseId`.

```http
GET http://localhost:5221/api/v1/suppliers
Authorization: Bearer {TOKEN}
```
**Expected**: At least one supplier or empty array.

```http
GET http://localhost:5221/api/v1/customers
Authorization: Bearer {TOKEN}
```
**Expected**: At least one customer (Cash Customer).

```http
GET http://localhost:5221/api/v1/products
Authorization: Bearer {TOKEN}
```
**Expected**: At least one product. Note its `productId`.

---

## Step 3: Purchase Flow — Create and Post Invoice

### 3a. Create Purchase Invoice (Draft)

```http
POST http://localhost:5221/api/v1/purchase-invoices
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "supplierId": 1,
  "warehouseId": 1,
  "paymentType": 1,
  "paidAmount": 500.00,
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

**Expected**: `201 Created` with `{ "id": N, "invoiceNo": "PUR-2026-000001", "status": "Draft", "totalAmount": 500.00 }`

Note `id` as `{PURCHASE_ID}`.

### 3b. Post the Purchase Invoice

```http
POST http://localhost:5221/api/v1/purchase-invoices/{PURCHASE_ID}/post
Authorization: Bearer {TOKEN}
```

**Expected**: `200 OK` with `status: "Posted"`

### 3c. Verify Stock Increased

```http
GET http://localhost:5221/api/v1/warehouses/1/stock
Authorization: Bearer {TOKEN}
```

**Expected**: Product 1 quantity = 10.000 (or prior + 10)

### 3d. Verify Inventory Movement Created

```http
GET http://localhost:5221/api/v1/inventory-movements?referenceType=PurchaseInvoice&referenceId={PURCHASE_ID}
Authorization: Bearer {TOKEN}
```

**Expected**: 1 movement with `movementType: "PurchaseIn"`, `quantityChange: 10.000`

---

## Step 4: Sales Flow — Create and Post Invoice

### 4a. Create Sales Invoice (Draft)

```http
POST http://localhost:5221/api/v1/sales-invoices
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "customerId": 1,
  "warehouseId": 1,
  "paymentType": 2,
  "paidAmount": 0.00,
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

**Expected**: `201 Created` with `{ "id": N, "invoiceNo": "INV-2026-000001", "status": "Draft", "totalAmount": 225.00, "dueAmount": 225.00 }`

Note `id` as `{SALES_ID}`.

### 4b. Post the Sales Invoice

```http
POST http://localhost:5221/api/v1/sales-invoices/{SALES_ID}/post
Authorization: Bearer {TOKEN}
```

**Expected**: `200 OK` with `status: "Posted"`

### 4c. Verify Stock Decreased

Check warehouse stock for Product 1 — should be 7.000 (10 from purchase - 3 from sale).

### 4d. Verify Customer Balance Increased

```http
GET http://localhost:5221/api/v1/customers/1
Authorization: Bearer {TOKEN}
```

**Expected**: `currentBalance: 225.00` (credit sale)

---

## Step 5: Insufficient Stock Rejection

```http
POST http://localhost:5221/api/v1/sales-invoices
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "warehouseId": 1,
  "paymentType": 1,
  "paidAmount": 0,
  "items": [
    {
      "productId": 1,
      "quantity": 999.000,
      "unitPrice": 75.00,
      "discountAmount": 0.00
    }
  ]
}
```

**Expected**: `400 Bad Request` with error indicating insufficient stock — **no invoice created, stock unchanged**

---

## Step 6: Stock Transfer

```http
POST http://localhost:5221/api/v1/stock-transfers
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "fromWarehouseId": 1,
  "toWarehouseId": 2,
  "items": [
    {
      "productId": 1,
      "quantity": 2.000
    }
  ]
}
```

**Expected if warehouse 2 exists**: `201 Created`, then POST to `/post` → stock moves from W1 to W2, two `InventoryMovement` records created (TransferOut + TransferIn).

---

## Step 7: Customer Payment

```http
POST http://localhost:5221/api/v1/payments/customer
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "customerId": 1,
  "amount": 100.00,
  "paymentMethod": 1,
  "salesInvoiceId": {SALES_ID}
}
```

**Expected**: `201 Created`, customer `currentBalance` decreases from 225.00 to 125.00

---

## Step 8: Cancel a Posted Invoice

```http
POST http://localhost:5221/api/v1/sales-invoices/{SALES_ID}/cancel
Authorization: Bearer {TOKEN}
```

**Expected**: `200 OK`, `status: "Cancelled"`, stock restored to pre-sale level, customer balance reduced back

---

## Step 9: Role-Based Access

```http
# Login as Cashier (if cashier user exists)
POST http://localhost:5221/api/v1/auth/login
{ "userName": "cashier", "password": "..." }

# Attempt purchase invoice creation
POST http://localhost:5221/api/v1/purchase-invoices
Authorization: Bearer {CASHIER_TOKEN}
```

**Expected**: `403 Forbidden`

---

## Checklist: Definition of Done

- [ ] Purchase flow: create → post → stock increased → movement recorded
- [ ] Sales flow: create → post → stock validated → stock decreased → balance updated
- [ ] Insufficient stock rejected with clear error (no partial data)
- [ ] Sales return: stock restored, customer balance reduced
- [ ] Purchase return: stock decreased, supplier balance reduced
- [ ] Stock transfer: dual warehouse movement atomic
- [ ] Payment: balance reduced, payment record created
- [ ] Cancel posted invoice: all effects reversed
- [ ] Cashier cannot access purchase/transfer endpoints (403)
- [ ] All financial totals consistent: `SubTotal + Tax - Discount = TotalAmount`, `TotalAmount - PaidAmount = DueAmount`
- [ ] Every stock change has a corresponding `InventoryMovement` record
- [ ] `InventoryMovements.QuantityAfter` equals subsequent `WarehouseStock.Quantity`
