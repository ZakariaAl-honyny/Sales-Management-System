# Data Model: Foundation Setup

**Date**: 2026-05-06
**Source**: `docs/database-schema.md` + Constitution Principles I, II, XI, XII

---

## BaseEntity (Abstract)

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment |
| CreatedAt | DateTime | Set on creation, never modified |
| UpdatedAt | DateTime? | Set on update |
| IsActive | bool | Soft delete flag, default true |

---

## 1. User

| Field | Type | Constraints |
|-------|------|-------------|
| UserId | int | PK |
| UserName | string | Required, MaxLength(50), Unique |
| PasswordHash | string | Required, MaxLength(256) |
| FullName | string | Required, MaxLength(150) |
| Role | UserRole (byte) | Required, CHECK IN (1,2,3) |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

**Validation**: UserName unique. Password MUST be BCrypt hashed (never plain).
**Soft Delete**: MUST use IsActive=false. Hard delete FORBIDDEN (FK integrity).

---

## 2. Unit

| Field | Type | Constraints |
|-------|------|-------------|
| UnitId | int | PK |
| Name | string | Required, MaxLength(50) |
| Symbol | string? | MaxLength(20) |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

---

## 3. Category

| Field | Type | Constraints |
|-------|------|-------------|
| CategoryId | int | PK |
| Name | string | Required, MaxLength(100) |
| Description | string? | MaxLength(250) |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

---

## 4. Product

| Field | Type | Constraints |
|-------|------|-------------|
| ProductId | int | PK |
| Code | string? | MaxLength(30), Unique |
| Barcode | string? | MaxLength(50), Unique |
| Name | string | Required, MaxLength(150) |
| CategoryId | int? | FK → Categories (Restrict) |
| UnitId | int? | FK → Units (Restrict) |
| PurchasePrice | decimal(18,2) | Required, default 0 |
| SalePrice | decimal(18,2) | Required, default 0 |
| MinStock | decimal(18,3) | Required, default 0 |
| Description | string? | MaxLength(500) |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

**Navigation**: Category, Unit, WarehouseStocks (collection)

---

## 5. Warehouse

| Field | Type | Constraints |
|-------|------|-------------|
| WarehouseId | int | PK |
| Code | string? | MaxLength(30), Unique |
| Name | string | Required, MaxLength(100) |
| Location | string? | MaxLength(250) |
| IsDefault | bool | default false |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

---

## 6. WarehouseStock

| Field | Type | Constraints |
|-------|------|-------------|
| WarehouseStockId | int | PK |
| WarehouseId | int | FK → Warehouses (Restrict), Required |
| ProductId | int | FK → Products (Restrict), Required |
| Quantity | decimal(18,3) | Required, default 0, CHECK >= 0 |
| UpdatedAt | DateTime | Required |

**Unique**: (WarehouseId, ProductId)
**Note**: Does NOT inherit BaseEntity (no IsActive, no CreatedAt).

---

## 7. Supplier

| Field | Type | Constraints |
|-------|------|-------------|
| SupplierId | int | PK |
| Code | string? | MaxLength(30), Unique |
| Name | string | Required, MaxLength(150) |
| Phone | string? | MaxLength(20) |
| Email | string? | MaxLength(100) |
| Address | string? | MaxLength(250) |
| OpeningBalance | decimal(18,2) | default 0 |
| CurrentBalance | decimal(18,2) | default 0 |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

**Balance Convention**: Positive = We owe supplier. Negative = Supplier owes us.

---

## 8. Customer

| Field | Type | Constraints |
|-------|------|-------------|
| CustomerId | int | PK |
| Code | string? | MaxLength(30), Unique |
| Name | string | Required, MaxLength(150) |
| Phone | string? | MaxLength(20) |
| Email | string? | MaxLength(100) |
| Address | string? | MaxLength(250) |
| OpeningBalance | decimal(18,2) | default 0 |
| CurrentBalance | decimal(18,2) | default 0 |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

**Balance Convention**: Positive = Customer owes us. Negative = We owe customer.

---

## 9. PurchaseInvoice

| Field | Type | Constraints |
|-------|------|-------------|
| PurchaseInvoiceId | int | PK |
| InvoiceNo | string | Required, MaxLength(30), Unique |
| SupplierId | int | FK → Suppliers (Restrict) |
| WarehouseId | int | FK → Warehouses (Restrict) |
| InvoiceDate | DateTime | Required |
| DueDate | DateOnly? | |
| PaymentType | PaymentType (byte) | Required |
| SubTotal | decimal(18,2) | default 0 |
| DiscountAmount | decimal(18,2) | default 0 |
| TaxAmount | decimal(18,2) | default 0 |
| TotalAmount | decimal(18,2) | default 0 |
| PaidAmount | decimal(18,2) | default 0 |
| DueAmount | decimal(18,2) | default 0 (computed) |
| Notes | string? | MaxLength(500) |
| Status | InvoiceStatus (byte) | Required, default Draft |
| CreatedBy | string? | MaxLength(150) |
| UpdatedBy | string? | MaxLength(150) |

**Domain Logic**:
- `LineTotal = (Quantity * UnitCost) - DiscountAmount` (in PurchaseInvoiceItem)
- `SubTotal = Items.Sum(i => i.LineTotal)`
- `TotalAmount = SubTotal - DiscountAmount + TaxAmount`
- `DueAmount = TotalAmount - PaidAmount`
- Guard: `PaidAmount <= TotalAmount`

**Navigation**: Supplier, Warehouse, Items (collection)

---

## 10. PurchaseInvoiceItem

| Field | Type | Constraints |
|-------|------|-------------|
| PurchaseInvoiceItemId | int | PK |
| PurchaseInvoiceId | int | FK → PurchaseInvoices (Restrict) |
| ProductId | int | FK → Products (Restrict) |
| Quantity | decimal(18,3) | Required |
| UnitCost | decimal(18,2) | Required |
| DiscountAmount | decimal(18,2) | default 0 |
| LineTotal | decimal(18,2) | Computed in domain |
| Notes | string? | MaxLength(250) |

**Note**: Does NOT inherit BaseEntity.

---

## 11. SalesInvoice

Same structure as PurchaseInvoice with:
- `CustomerId int? FK → Customers` (nullable for cash sales)
- `UnitPrice` instead of `UnitCost` in line items

**Domain Logic**: Same formulas, `UnitPrice` replaces `UnitCost`.

---

## 12. SalesInvoiceItem

| Field | Type | Constraints |
|-------|------|-------------|
| SalesInvoiceItemId | int | PK |
| SalesInvoiceId | int | FK → SalesInvoices (Restrict) |
| ProductId | int | FK → Products (Restrict) |
| Quantity | decimal(18,3) | Required |
| UnitPrice | decimal(18,2) | Required |
| DiscountAmount | decimal(18,2) | default 0 |
| LineTotal | decimal(18,2) | Computed in domain |
| Notes | string? | MaxLength(250) |

---

## 13-14. PurchaseReturn / PurchaseReturnItem

| Field | Type | Constraints |
|-------|------|-------------|
| PurchaseReturnId | int | PK |
| ReturnNo | string | Required, MaxLength(30), Unique |
| PurchaseInvoiceId | int? | FK → PurchaseInvoices (optional) |
| SupplierId | int | FK → Suppliers (Restrict) |
| WarehouseId | int | FK → Warehouses (Restrict) |
| ReturnDate | DateTime | Required |
| Reason | string? | MaxLength(250) |
| SubTotal | decimal(18,2) | default 0 |
| TotalAmount | decimal(18,2) | default 0 |
| Status | InvoiceStatus (byte) | Required |

**Items**: PurchaseReturnItemId, PurchaseReturnId FK, ProductId FK,
Quantity decimal(18,3), UnitCost decimal(18,2), LineTotal decimal(18,2)

---

## 15-16. SalesReturn / SalesReturnItem

Same pattern as PurchaseReturn with `CustomerId` and `SalesInvoiceId`
references, `UnitPrice` instead of `UnitCost`.

---

## 17-18. StockTransfer / StockTransferItem

| Field | Type | Constraints |
|-------|------|-------------|
| StockTransferId | int | PK |
| TransferNo | string | Required, MaxLength(30), Unique |
| FromWarehouseId | int | FK → Warehouses (Restrict) |
| ToWarehouseId | int | FK → Warehouses (Restrict) |
| TransferDate | DateTime | Required |
| Notes | string? | MaxLength(500) |
| Status | InvoiceStatus (byte) | Required |

**Items**: StockTransferItemId, StockTransferId FK, ProductId FK,
Quantity decimal(18,3), Notes nvarchar(250)

**Constraint**: FromWarehouseId != ToWarehouseId (domain validation)

---

## 19. CustomerPayment

| Field | Type | Constraints |
|-------|------|-------------|
| CustomerPaymentId | int | PK |
| PaymentNo | string | Required, MaxLength(30), Unique |
| CustomerId | int | FK → Customers (Restrict) |
| SalesInvoiceId | int? | FK → SalesInvoices (optional) |
| PaymentDate | DateTime | Required |
| Amount | decimal(18,2) | Required |
| PaymentMethod | byte | Required |
| ReferenceNo | string? | MaxLength(50) |
| Notes | string? | MaxLength(500) |
| CreatedBy | string? | MaxLength(150) |

**Note**: Does NOT have UpdatedAt (payments are immutable).

---

## 20. SupplierPayment

Same structure as CustomerPayment with `SupplierId` and
`PurchaseInvoiceId` references.

---

## 21. InventoryMovement

| Field | Type | Constraints |
|-------|------|-------------|
| InventoryMovementId | long (bigint) | PK |
| ProductId | int | FK → Products (Restrict) |
| WarehouseId | int | FK → Warehouses (Restrict) |
| MovementType | MovementType (byte) | Required |
| QuantityChange | decimal(18,3) | Required (positive=IN, negative=OUT) |
| QuantityBefore | decimal(18,3) | Required |
| QuantityAfter | decimal(18,3) | Required |
| ReferenceType | string | Required, MaxLength(30) |
| ReferenceId | int | Required |
| UnitCost | decimal(18,2)? | |
| MovementDate | DateTime | Required |
| Notes | string? | MaxLength(500) |
| CreatedByUserId | int? | FK → Users (Restrict) |

**Indexes**: (ProductId, MovementDate DESC), (ReferenceType, ReferenceId)
**Note**: Immutable audit record. No update, no delete.

---

## 22. StoreSettings

| Field | Type | Constraints |
|-------|------|-------------|
| StoreSettingsId | int | PK |
| StoreName | string | Required, MaxLength(150) |
| Phone | string? | MaxLength(20) |
| Address | string? | MaxLength(250) |
| LogoPath | string? | MaxLength(255) |
| CurrencyCode | string | Required, MaxLength(10), default "SAR" |
| DefaultTaxRate | decimal(5,2) | default 0 |
| IsTaxEnabled | bool | default false |
| UpdatedAt | DateTime? | |

**Note**: Single-row table. Does NOT inherit BaseEntity.

---

## 23. DocumentSequence

| Field | Type | Constraints |
|-------|------|-------------|
| DocumentSequenceId | int | PK |
| DocumentType | string | Required, MaxLength(10), Unique |
| Prefix | string | Required, MaxLength(10) |
| Year | int | Required |
| LastNumber | int | Required, default 0 |

**Note**: Used by DocumentSequenceService with SemaphoreSlim lock.

---

## Entity Relationship Summary

```text
User ──< InventoryMovement (CreatedByUserId)

Category ──< Product
Unit ──< Product
Product ──< WarehouseStock >── Warehouse
Product ──< SalesInvoiceItem
Product ──< PurchaseInvoiceItem
Product ──< SalesReturnItem
Product ──< PurchaseReturnItem
Product ──< StockTransferItem
Product ──< InventoryMovement

Customer ──< SalesInvoice ──< SalesInvoiceItem
Customer ──< SalesReturn ──< SalesReturnItem
Customer ──< CustomerPayment

Supplier ──< PurchaseInvoice ──< PurchaseInvoiceItem
Supplier ──< PurchaseReturn ──< PurchaseReturnItem
Supplier ──< SupplierPayment

Warehouse ──< WarehouseStock
Warehouse ──< SalesInvoice
Warehouse ──< PurchaseInvoice
Warehouse ──< SalesReturn
Warehouse ──< PurchaseReturn
Warehouse ──< StockTransfer (From)
Warehouse ──< StockTransfer (To)
Warehouse ──< InventoryMovement
```

## Enums

```csharp
public enum UserRole : byte { Admin = 1, Manager = 2, Cashier = 3 }
public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }
public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }
public enum MovementType : byte
{
    PurchaseIn = 1, SaleOut = 2, SaleReturnIn = 3,
    PurchaseReturnOut = 4, TransferOut = 5, TransferIn = 6,
    Adjustment = 7
}
```
