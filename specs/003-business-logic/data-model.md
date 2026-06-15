# Data Model: Business Logic Implementation

**Feature**: Phase 3 — Business Logic (Critical)
**Branch**: `003-business-logic` | **Date**: 2026-05-07
**Schema Status**: All tables already migrated — Phase 3 adds Application layer only

---

## Note on Schema Completeness

All entities below are **already defined in the Domain layer** and **fully migrated to SQL Server** via the `SyncInventoryAndSettings` migration. This document serves as the authoritative reference for how Phase 3 services interact with these entities.

---

## Entity: SalesInvoice

**Purpose**: Records a sale from the store to a customer (or walk-in).

| Field | Type | Rules |
|---|---|---|
| `SalesInvoiceId` | `int` PK | Auto-generated |
| `InvoiceNo` | `nvarchar(20)` | Unique, auto-generated `INV-{YYYY}-{000000}` |
| `CustomerId` | `int?` FK → Customers | Nullable (walk-in = null or cash customer) |
| `WarehouseId` | `int` FK → Warehouses | Required — source warehouse |
| `InvoiceDate` | `datetime2` | Defaults to UTC now |
| `DueDate` | `DateOnly?` | Optional payment due date |
| `PaymentType` | `tinyint` | Cash=1, Credit=2, Mixed=3 |
| `SubTotal` | `decimal(18,2)` | Domain-computed: `Σ Items.LineTotal` |
| `DiscountAmount` | `decimal(18,2)` | Invoice-level discount |
| `TaxAmount` | `decimal(18,2)` | Invoice-level tax |
| `TotalAmount` | `decimal(18,2)` | Domain-computed: `SubTotal − Discount + Tax` |
| `PaidAmount` | `decimal(18,2)` | Must be ≤ TotalAmount (domain + DB enforced) |
| `DueAmount` | `decimal(18,2)` | Domain-computed: `TotalAmount − PaidAmount` |
| `Notes` | `nvarchar(500)?` | Optional |
| `Status` | `tinyint` | Draft=1, Posted=2, Cancelled=3 |
| `CreatedByUserId` | `int?` FK → Users | Audit — set on creation |
| `CreatedAt` | `datetime2` | Auto-set |
| `IsActive` | `bit` | Soft-delete (always true for invoices — use Cancelled) |

**State Machine**: Draft → Posted → Cancelled. Cancelled is terminal. No hard-delete.

**Domain methods**: `Create()`, `AddItem()`, `RemoveItem()`, `RecalculateTotals()`, `SetPaidAmount()`, `Post()`, `Cancel()`

---

## Entity: SalesInvoiceLine

**Purpose**: One line on a sales invoice.

| Field | Type | Rules |
|---|---|---|
| `SalesInvoiceLineId` | `int` PK | Auto-generated |
| `SalesInvoiceId` | `int` FK → SalesInvoices | Required |
| `ProductId` | `int` FK → Products | Required |
| `Quantity` | `decimal(18,3)` | > 0 |
| `UnitPrice` | `decimal(18,2)` | ≥ 0 |
| `DiscountAmount` | `decimal(18,2)` | ≥ 0 |
| `LineTotal` | `decimal(18,2)` | Domain-computed: `(Quantity × UnitPrice) − DiscountAmount` |

---

## Entity: PurchaseInvoice

**Purpose**: Records a purchase from a supplier to restock the warehouse.

| Field | Type | Rules |
|---|---|---|
| `PurchaseInvoiceId` | `int` PK | Auto-generated |
| `InvoiceNo` | `nvarchar(20)` | Unique, auto-generated `PUR-{YYYY}-{000000}` |
| `SupplierId` | `int` FK → Suppliers | Required |
| `WarehouseId` | `int` FK → Warehouses | Required — destination warehouse |
| `InvoiceDate` | `datetime2` | Defaults to UTC now |
| `DueDate` | `DateOnly?` | Optional |
| `PaymentType` | `tinyint` | Cash=1, Credit=2, Mixed=3 |
| `SubTotal` | `decimal(18,2)` | Domain-computed |
| `DiscountAmount` | `decimal(18,2)` | Invoice-level discount |
| `TaxAmount` | `decimal(18,2)` | Invoice-level tax |
| `TotalAmount` | `decimal(18,2)` | Domain-computed |
| `PaidAmount` | `decimal(18,2)` | Must be ≤ TotalAmount |
| `DueAmount` | `decimal(18,2)` | Domain-computed |
| `Notes` | `nvarchar(500)?` | Optional |
| `Status` | `tinyint` | Draft=1, Posted=2, Cancelled=3 |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

**Domain methods**: `Create()`, `AddItem()`, `RecalculateTotals()`, `SetPaidAmount()`, `Post()`, `Cancel()`

---

## Entity: PurchaseInvoiceLine

| Field | Type | Rules |
|---|---|---|
| `PurchaseInvoiceLineId` | `int` PK | Auto-generated |
| `PurchaseInvoiceId` | `int` FK → PurchaseInvoices | Required |
| `ProductId` | `int` FK → Products | Required |
| `Quantity` | `decimal(18,3)` | > 0 |
| `UnitCost` | `decimal(18,2)` | ≥ 0 |
| `DiscountAmount` | `decimal(18,2)` | ≥ 0 |
| `LineTotal` | `decimal(18,2)` | Domain-computed: `(Quantity × UnitCost) − DiscountAmount` |

---

## Entity: SalesReturn

**Purpose**: Records goods returned by a customer against a prior sale.

| Field | Type | Rules |
|---|---|---|
| `SalesReturnId` | `int` PK | Auto-generated |
| `ReturnNo` | `nvarchar(20)` | Unique, auto-generated `SR-{YYYY}-{000000}` |
| `SalesInvoiceId` | `int?` FK → SalesInvoices | Optional — null = standalone return |
| `CustomerId` | `int?` FK → Customers | Optional |
| `WarehouseId` | `int` FK → Warehouses | Return destination warehouse |
| `ReturnDate` | `datetime2` | Defaults to UTC now |
| `TotalAmount` | `decimal(18,2)` | Domain-computed |
| `Notes` | `nvarchar(500)?` | Optional |
| `Status` | `tinyint` | Draft=1, Posted=2, Cancelled=3 |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

---

## Entity: PurchaseReturn

**Purpose**: Records goods returned to a supplier against a prior purchase.

| Field | Type | Rules |
|---|---|---|
| `PurchaseReturnId` | `int` PK | Auto-generated |
| `ReturnNo` | `nvarchar(20)` | Unique, auto-generated `PR-{YYYY}-{000000}` |
| `PurchaseInvoiceId` | `int?` FK → PurchaseInvoices | Optional |
| `SupplierId` | `int` FK → Suppliers | Required |
| `WarehouseId` | `int` FK → Warehouses | Source warehouse (stock leaves here) |
| `ReturnDate` | `datetime2` | Defaults to UTC now |
| `TotalAmount` | `decimal(18,2)` | Domain-computed |
| `Notes` | `nvarchar(500)?` | Optional |
| `Status` | `tinyint` | Draft=1, Posted=2, Cancelled=3 |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

---

## Entity: StockTransfer

**Purpose**: Records movement of stock between two warehouses.

| Field | Type | Rules |
|---|---|---|
| `StockTransferId` | `int` PK | Auto-generated |
| `TransferNo` | `nvarchar(20)` | Unique, auto-generated `TRF-{YYYY}-{000000}` |
| `FromWarehouseId` | `int` FK → Warehouses | Source — must differ from `ToWarehouseId` |
| `ToWarehouseId` | `int` FK → Warehouses | Destination |
| `TransferDate` | `datetime2` | Defaults to UTC now |
| `Notes` | `nvarchar(500)?` | Optional |
| `Status` | `tinyint` | Draft=1, Posted=2, Cancelled=3 |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

**Constraint**: `FromWarehouseId ≠ ToWarehouseId` — enforced in Domain `Create()`.

---

## Entity: StockTransferItem

| Field | Type | Rules |
|---|---|---|
| `StockTransferItemId` | `int` PK | Auto-generated |
| `StockTransferId` | `int` FK → StockTransfers | Required |
| `ProductId` | `int` FK → Products | Required |
| `Quantity` | `decimal(18,3)` | > 0 |
| `Notes` | `nvarchar(500)?` | Optional |

---

## Entity: WarehouseStock

**Purpose**: Current on-hand quantity of a product in a warehouse.

| Field | Type | Rules |
|---|---|---|
| `WarehouseStockId` | `int` PK | Auto-generated |
| `WarehouseId` | `int` FK → Warehouses | Part of unique key |
| `ProductId` | `int` FK → Products | Part of unique key |
| `Quantity` | `decimal(18,3)` | `CHECK (Quantity >= 0)` — DB enforced |

**Unique constraint**: `(WarehouseId, ProductId)` — one row per product per warehouse.

**Write authority**: `InventoryService` is the **only** code path that modifies `Quantity`.

---

## Entity: InventoryMovement

**Purpose**: Append-only audit log of every stock change.

| Field | Type | Rules |
|---|---|---|
| `InventoryMovementId` | `int` PK | Auto-generated |
| `ProductId` | `int` FK → Products | Required |
| `WarehouseId` | `int` FK → Warehouses | Required |
| `MovementType` | `tinyint` | PurchaseIn=1, SaleOut=2, SaleReturnIn=3, PurchaseReturnOut=4, TransferOut=5, TransferIn=6, Adjustment=7 |
| `QuantityChange` | `decimal(18,3)` | Positive = in, Negative = out |
| `QuantityBefore` | `decimal(18,3)` | Stock level before change |
| `QuantityAfter` | `decimal(18,3)` | Stock level after change |
| `ReferenceType` | `nvarchar(50)` | e.g., "SalesInvoice", "PurchaseInvoice", "StockTransfer" |
| `ReferenceId` | `int` | ID of the originating document |
| `UnitCost` | `decimal(18,2)?` | Optional — captures cost at time of movement |
| `MovementDate` | `datetime2` | Defaults to UTC now |
| `Notes` | `nvarchar(500)?` | Optional |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

**Rules**: Never updated or deleted. One record per product per line item per stock operation.

---

## Entity: CustomerPayment

**Purpose**: Records a cash payment from a customer, reducing their outstanding balance.

| Field | Type | Rules |
|---|---|---|
| `CustomerPaymentId` | `int` PK | Auto-generated |
| `PaymentNo` | `nvarchar(20)` | Unique, auto-generated `CP-{YYYY}-{000000}` |
| `CustomerId` | `int` FK → Customers | Required |
| `SalesInvoiceId` | `int?` FK → SalesInvoices | Optional — link to specific invoice |
| `Amount` | `decimal(18,2)` | > 0 |
| `PaymentMethod` | `tinyint` | Cash=1, Credit=2, Mixed=3 |
| `PaymentDate` | `datetime2` | Defaults to UTC now |
| `Notes` | `nvarchar(500)?` | Optional |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

---

## Entity: SupplierPayment

**Purpose**: Records a payment made to a supplier, reducing their outstanding balance.

| Field | Type | Rules |
|---|---|---|
| `SupplierPaymentId` | `int` PK | Auto-generated |
| `PaymentNo` | `nvarchar(20)` | Unique, auto-generated `SP-{YYYY}-{000000}` |
| `SupplierId` | `int` FK → Suppliers | Required |
| `PurchaseInvoiceId` | `int?` FK → PurchaseInvoices | Optional |
| `Amount` | `decimal(18,2)` | > 0 |
| `PaymentMethod` | `tinyint` | Cash=1, Credit=2, Mixed=3 |
| `PaymentDate` | `datetime2` | Defaults to UTC now |
| `Notes` | `nvarchar(500)?` | Optional |
| `CreatedByUserId` | `int?` FK → Users | Audit |
| `CreatedAt` | `datetime2` | Auto-set |

---

## Balance Direction Rules

```text
Customer.CurrentBalance > 0  → Customer owes the store
Customer.CurrentBalance < 0  → Store owes the customer

Supplier.CurrentBalance > 0  → Store owes the supplier
Supplier.CurrentBalance < 0  → Supplier owes the store
```

**Balance changes**:
- Sales Invoice posted (DueAmount > 0) → Customer balance **increases**
- Sales Invoice cancelled → Customer balance **decreases** (reversed)
- Sales Return posted → Customer balance **decreases**
- Customer Payment recorded → Customer balance **decreases**
- Purchase Invoice posted (DueAmount > 0) → Supplier balance **increases**
- Purchase Invoice cancelled → Supplier balance **decreases** (reversed)
- Purchase Return posted → Supplier balance **decreases**
- Supplier Payment recorded → Supplier balance **decreases**

---

## State Transition Diagram

```
Invoice / Transfer / Return lifecycle:

  [Draft] ──post()──► [Posted] ──cancel()──► [Cancelled]
     │                                              ▲
     └──────────────cancel()───────────────────────┘

  FORBIDDEN: Posted → Draft
  FORBIDDEN: Cancelled → any state
  REQUIRED on Posted → Cancelled: reverse ALL stock + balance effects
```

---

## IUnitOfWork Extension (Phase 3 additions)

```
Existing repositories (Phase 2):
  Users, Products, Categories, Units, Warehouses, Suppliers, Customers

New repositories to add (Phase 3):
  SalesInvoices       → IGenericRepository<SalesInvoice>
  PurchaseInvoices    → IGenericRepository<PurchaseInvoice>
  SalesReturns        → IGenericRepository<SalesReturn>
  PurchaseReturns     → IGenericRepository<PurchaseReturn>
  StockTransfers      → IGenericRepository<StockTransfer>
  CustomerPayments    → IGenericRepository<CustomerPayment>
  SupplierPayments    → IGenericRepository<SupplierPayment>
  WarehouseStocks     → IGenericRepository<WarehouseStock>
  InventoryMovements  → IGenericRepository<InventoryMovement>
```
