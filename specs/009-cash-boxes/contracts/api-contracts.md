# API Contracts: Cash Boxes (v4.3)

## `CashBoxesController` — `/api/v1/cash-boxes`

### `GET /api/v1/cash-boxes`
List all active cash boxes with computed current balance.
**Response**: `Result<List<CashBoxDto>>`
`CashBoxDto`: `{ Id, Name, OpeningBalance, CurrentBalance, IsActive }`

### `POST /api/v1/cash-boxes`
Create a new cash box.
**Request**: `CreateCashBoxRequest { Name, OpeningBalance }`
**Validation**: Name NotEmpty MaxLength(100); OpeningBalance ≥ 0
**Response**: `Result<CashBoxDto>`

### `DELETE /api/v1/cash-boxes/{id}`
Soft delete (deactivate) a cash box.
**Response**: `Result`
**Business Rule**: Reject if box has transactions recorded today.

### `GET /api/v1/cash-boxes/{id}/transactions`
List all transactions for a cash box, ordered by `CreatedAt` descending. Supports optional date range query: `?from=YYYY-MM-DD&to=YYYY-MM-DD`
**Response**: `Result<List<CashTransactionDto>>`
`CashTransactionDto`: `{ Id, TransactionType, TransactionTypeName, Amount, BalanceBefore, BalanceAfter, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedByUserName }`

### `POST /api/v1/cash-boxes/{id}/transactions`
Record a manual transaction (Expense only — SalesIncome and SupplierPayment are created automatically by invoice posting).
**Request**: `AddCashTransactionRequest { TransactionType, Amount, Notes }`
**Validation**: Amount > 0; TransactionType must be `Expense(3)` only via this endpoint
**Response**: `Result<CashTransactionDto>`

### `POST /api/v1/cash-boxes/transfer`
Transfer cash between two boxes atomically.
**Request**: `CashTransferRequest { SourceCashBoxId, DestinationCashBoxId, Amount, Notes }`
**Validation**: Amount > 0; Source ≠ Destination
**Response**: `Result`
**Business Rule**: Source balance must cover the amount; atomic dual-entry.

### `GET /api/v1/cash-boxes/{id}/daily-closures`
List all daily closures for a cash box.
**Response**: `Result<List<DailyClosureDto>>`
`DailyClosureDto`: `{ Id, ClosureDate, OpeningBalance, TotalIncome, TotalExpense, ClosingBalance, ClosedByUserName, CreatedAt }`

### `POST /api/v1/cash-boxes/{id}/daily-closures`
Perform a daily closure for today.
**Response**: `Result<DailyClosureDto>`
**Business Rule**: Rejects if closure already exists for `CashBoxId + today's date`.
