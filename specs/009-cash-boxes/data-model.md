# Data Model: Cash Boxes (v4.3)

## New Entities

### `CashBox`
Represents a physical cash drawer or safe.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int, PK | |
| `Name` | nvarchar(100) | Required, unique per store |
| `OpeningBalance` | decimal(18,2) | Set once at creation, ≥ 0 |
| `IsActive` | bool | Soft delete flag |
| `CreatedAt` | datetime2 | |
| `CreatedByUserId` | int, FK → Users | |

**Computed (not stored)**: `CurrentBalance = Sum(credit transactions) - Sum(debit transactions)`

**Credit types**: OpeningBalance(1), SalesIncome(2), TransferIn(5), CustomerPayment(8)
**Debit types**: Expense(3), TransferOut(4), RefundOut(6), SupplierPayment(7)

**Validation Rules**:
- `Name` not empty
- `OpeningBalance >= 0`
- Soft delete only — hard delete forbidden (referenced by CashTransactions)

---

### `CashTransaction`
Immutable record of a single cash event. No UPDATE or DELETE ever permitted.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int, PK | |
| `CashBoxId` | int, FK → CashBoxes | Restrict |
| `TransactionType` | byte | Enum: 1–8 |
| `Amount` | decimal(18,2) | Always > 0 |
| `BalanceBefore` | decimal(18,2) | Snapshot at write time |
| `BalanceAfter` | decimal(18,2) | Snapshot at write time |
| `ReferenceType` | nvarchar(50) | e.g., "SalesInvoice", "PurchaseInvoice", "Transfer" — nullable |
| `ReferenceId` | int, nullable | FK value without FK constraint (to allow cross-entity refs) |
| `Notes` | nvarchar(255) | Optional free text |
| `CreatedAt` | datetime2 | |
| `CreatedByUserId` | int, FK → Users | Restrict |

**Validation Rules**:
- `Amount > 0` (enforced at Domain, API FluentValidation, and DB CHECK constraint)
- `TransactionType` must be a valid enum value (1–8)
- Immutable — no edit or delete endpoint exists

**DB Index**: `IX_CashTransactions_CashBoxId_CreatedAt` (for fast balance computation)

---

### `DailyClosure`
Immutable end-of-day snapshot. One per cash box per calendar date.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int, PK | |
| `CashBoxId` | int, FK → CashBoxes | Restrict |
| `ClosureDate` | date | |
| `OpeningBalance` | decimal(18,2) | |
| `TotalIncome` | decimal(18,2) | Sum of credit transactions for the day |
| `TotalExpense` | decimal(18,2) | Sum of debit transactions for the day |
| `ClosingBalance` | decimal(18,2) | OpeningBalance + TotalIncome - TotalExpense |
| `ClosedByUserId` | int, FK → Users | Restrict |
| `CreatedAt` | datetime2 | |

**DB Constraint**: `UNIQUE INDEX IX_DailyClosures_CashBoxId_ClosureDate`

**Validation Rules**:
- One per CashBoxId per ClosureDate — duplicates rejected
- Immutable — no edit or delete endpoint

---

## Modified Entities

### `SalesInvoice`
- **Add**: `CashBoxId int NULL FK → CashBoxes` (`DeleteBehavior.Restrict`)
- Nullable: credit invoices (`PaidAmount = 0`) have no cash box

### `PurchaseInvoice`
- **Add**: `CashBoxId int NULL FK → CashBoxes` (`DeleteBehavior.Restrict`)

### `CustomerPayment` (existing)
- **Add**: `CashBoxId int FK → CashBoxes` (required — all customer payments are cash)

### `SupplierPayment` (existing)
- **Add**: `CashBoxId int FK → CashBoxes` (required — all supplier payments are cash)

---

## Enum: `CashTransactionType`
```
OpeningBalance = 1   // Credit — initial box opening
SalesIncome    = 2   // Credit — from sales invoice payment
Expense        = 3   // Debit  — petty cash disbursement
TransferOut    = 4   // Debit  — transfer to another box
TransferIn     = 5   // Credit — transfer from another box
RefundOut      = 6   // Debit  — sales return refund
SupplierPayment= 7   // Debit  — payment to supplier
CustomerPayment= 8   // Credit — payment from customer
```
