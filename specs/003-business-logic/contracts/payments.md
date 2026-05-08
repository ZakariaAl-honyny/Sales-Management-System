# API Contracts: Payments

**Base URL**: `http://localhost:5221/api/v1/payments`
**Authorization**: Bearer JWT — Policy: `AllStaff` (Admin, Manager, Cashier)

---

## Customer Payments

### POST /api/v1/payments/customer
Record a payment received from a customer, reducing their outstanding balance.

**Request Body**
```json
{
  "customerId": 1,
  "salesInvoiceId": 1,
  "amount": 200.00,
  "paymentMethod": 1,
  "paymentDate": "2026-05-07T00:00:00Z",
  "notes": "string|null"
}
```

**Validation Rules**
- `customerId` required, > 0
- `amount` required, > 0
- `paymentMethod` required, one of [1, 2, 3]
- `salesInvoiceId` optional (link to specific invoice)

**Business Rules**:
1. Create `CustomerPayment` record
2. Decrease `Customer.CurrentBalance` by `Amount`
3. Log the payment
4. All in a single transaction

**Responses**

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `CustomerPaymentDto` | Payment recorded |
| 400 Bad Request | `{ "error": "...", "errorCode": "VALIDATION_ERROR" }` | Amount ≤ 0 or missing fields |
| 404 Not Found | — | Customer not found |

### GET /api/v1/payments/customer
List customer payments.

**Query Parameters**: `customerId`, `salesInvoiceId`, `fromDate`, `toDate`, `page`, `pageSize`

### GET /api/v1/payments/customer/{id}

---

## Supplier Payments

### POST /api/v1/payments/supplier
Record a payment made to a supplier, reducing their outstanding balance.

**Request Body**
```json
{
  "supplierId": 1,
  "purchaseInvoiceId": 1,
  "amount": 500.00,
  "paymentMethod": 1,
  "paymentDate": "2026-05-07T00:00:00Z",
  "notes": "string|null"
}
```

**Authorization**: `ManagerAndAbove` for supplier payments

**Validation Rules**
- `supplierId` required, > 0
- `amount` required, > 0
- `paymentMethod` required, one of [1, 2, 3]

**Business Rules**:
1. Create `SupplierPayment` record
2. Decrease `Supplier.CurrentBalance` by `Amount`
3. Log the payment
4. All in a single transaction

**Responses**: `201 Created` (SupplierPaymentDto) | `400` | `404`

### GET /api/v1/payments/supplier
### GET /api/v1/payments/supplier/{id}

---

## CustomerPaymentDto (Response Shape)
```json
{
  "id": 1,
  "paymentNo": "CP-2026-000001",
  "customerId": 1,
  "customerName": "Cash Customer",
  "salesInvoiceId": 1,
  "salesInvoiceNo": "INV-2026-000001",
  "amount": 200.00,
  "paymentMethod": 1,
  "paymentMethodName": "Cash",
  "paymentDate": "2026-05-07T00:00:00Z",
  "notes": null
}
```

## SupplierPaymentDto (Response Shape)
```json
{
  "id": 1,
  "paymentNo": "SP-2026-000001",
  "supplierId": 1,
  "supplierName": "Main Supplier",
  "purchaseInvoiceId": 1,
  "purchaseInvoiceNo": "PUR-2026-000001",
  "amount": 500.00,
  "paymentMethod": 1,
  "paymentMethodName": "Cash",
  "paymentDate": "2026-05-07T00:00:00Z",
  "notes": null
}
```
