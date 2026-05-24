# Feature Specification: Cash Boxes (v4.3)

**Feature Branch**: `009-cash-boxes`
**Created**: 2026-05-24
**Status**: Draft
**Input**: User description: "Phase 9 — Cash Boxes (v4.3)"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Cash Box Setup & Balance Tracking (Priority: P1)

A store manager opens a new shift by creating or opening a cash box and recording an opening balance. As sales and purchases occur throughout the day, every payment transaction is recorded against the cash box — the current balance is always the real-time sum of all transactions, not a stored field that can drift. At any moment, the manager can view the current balance and the full transaction history for the box.

**Why this priority**: This is the foundational capability of the entire module. All invoice payment linking, transfers, and daily closures depend on a correctly functioning cash box with an accurate, always-computed balance.

**Independent Test**: Create a cash box with opening balance 500.00 → record a SalesIncome of 200.00 → record an Expense of 50.00 → query balance: must equal 650.00. The 650.00 must be derived by summing transactions, not from a stored field.

**Acceptance Scenarios**:

1. **Given** a new cash box with `OpeningBalance = 500.00`, **When** a `SalesIncome` transaction of `200.00` is recorded, **Then** `CurrentBalance = 700.00`.
2. **Given** a cash box with `CurrentBalance = 700.00`, **When** an `Expense` transaction of `50.00` is recorded, **Then** `CurrentBalance = 650.00`.
3. **Given** a cash box with `CurrentBalance = 100.00`, **When** an `Expense` of `150.00` is attempted, **Then** the system rejects it with an error: "الرصيد غير كافٍ في الصندوق".
4. **Given** a cash box, **When** the transaction history is requested, **Then** all entries appear in chronological order with type, amount, and resulting balance.

---

### User Story 2 — Invoice Payment Linked to Cash Box (Priority: P1)

Every invoice payment (sale, purchase, customer payment, or supplier payment) must be explicitly linked to a cash box at the time of posting. This ensures the cash box balance always reflects the real money collected or disbursed from that physical box.

**Why this priority**: Without this link, cash tracking is meaningless — a posted invoice has financial impact, and that impact must flow directly into the correct cash box.

**Independent Test**: Post a sales invoice with `PaidAmount = 300.00` linked to CashBox #1 → CashBox #1 `CurrentBalance` increases by 300.00 → `CashTransaction` record of type `SalesIncome` for 300.00 referencing the invoice ID exists.

**Acceptance Scenarios**:

1. **Given** a sales invoice with `PaidAmount = 300.00` and `CashBoxId = 1`, **When** the invoice is posted, **Then** a `CashTransaction(SalesIncome, 300.00, ReferenceType=SalesInvoice, ReferenceId=invoiceId)` is created.
2. **Given** a purchase invoice with `PaidAmount = 200.00` and `CashBoxId = 1`, **When** the invoice is posted, **Then** a `CashTransaction(SupplierPayment, 200.00)` is created and CashBox balance decreases by 200.00.
3. **Given** a sales invoice with `PaidAmount = 0` (full credit), **When** the invoice is posted, **Then** NO `CashTransaction` is created for that invoice.
4. **Given** a posted invoice is cancelled, **When** cancellation occurs, **Then** an offsetting `CashTransaction` is created to reverse the original cash impact.

---

### User Story 3 — Cash Transfer Between Boxes (Priority: P2)

A manager needs to move cash from one cash box to another — for example, moving end-of-day takings from the POS box to the safe box. The transfer must create exactly two immutable transaction entries: one debit (TransferOut) from the source box and one credit (TransferIn) to the destination box. Both entries must be created atomically.

**Why this priority**: This is an operational necessity for multi-box stores, but the store can operate without it by managing boxes independently. It builds on the P1 balance tracking foundation.

**Independent Test**: Transfer 200.00 from CashBox #1 to CashBox #2 → CashBox #1 has a `TransferOut` of 200.00 → CashBox #2 has a `TransferIn` of 200.00 → both balances updated correctly → if either box would go negative, the entire transfer is rejected.

**Acceptance Scenarios**:

1. **Given** CashBox #1 has balance 500.00 and CashBox #2 has balance 100.00, **When** a transfer of 200.00 is made from #1 to #2, **Then** two `CashTransaction` records are created: `TransferOut` on #1 and `TransferIn` on #2, and balances become 300.00 and 300.00 respectively.
2. **Given** CashBox #1 has balance 100.00, **When** a transfer of 200.00 is attempted from #1, **Then** the transfer is rejected: "الرصيد غير كافٍ لإتمام التحويل" — NO transactions are created.
3. **Given** a transfer of 200.00 is being processed, **When** the system fails after creating the `TransferOut` but before `TransferIn`, **Then** neither transaction is persisted (full rollback).

---

### User Story 4 — Daily Closure Computation (Priority: P2)

At the end of each business day, the manager performs a daily closure for each cash box. The closure captures a snapshot: `ClosingBalance = OpeningBalance + TotalIncome - TotalExpense`. This record is immutable once created. The next day's opening balance starts from the previous day's closing balance.

**Why this priority**: The daily closure is an important financial control mechanism, but the store can continue operating without formal closures. It depends on correct balance tracking (US1) being in place.

**Independent Test**: Cash box with `OpeningBalance = 500.00`, `SalesIncome = 800.00`, `Expense = 200.00` → perform daily closure → `DailyClosure.ClosingBalance = 1100.00`. Attempting a second closure for the same day and box must be rejected.

**Acceptance Scenarios**:

1. **Given** a cash box with `OpeningBalance = 500.00`, `SalesIncome = 800.00`, `Expense = 200.00`, **When** daily closure is performed, **Then** `DailyClosure.ClosingBalance = 1100.00`.
2. **Given** a daily closure already exists for today's date and a given cash box, **When** another closure is attempted for the same box and date, **Then** the system rejects it: "تم إغلاق الصندوق بالفعل لهذا اليوم".
3. **Given** a daily closure is created, **When** any field is modified, **Then** the system prevents the modification — closure records are read-only.

---

### Edge Cases

- What happens when a cash box has zero balance and an expense is attempted? → System rejects with "الرصيد غير كافٍ في الصندوق".
- What happens if the same cash box is used for a transfer to itself? → System rejects: "لا يمكن التحويل إلى نفس الصندوق".
- What happens if a sales return is processed for an invoice with cash payment? → A `RefundOut` transaction reduces the cash box balance by the refund amount.
- What happens if a `CashTransaction` is attempted to be edited? → System does not expose any edit endpoint; corrections must be made via an offsetting new transaction.
- What if `OpeningBalance` is negative? → System rejects with a domain guard clause: "الرصيد الافتتاحي لا يمكن أن يكون سالباً".

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST track a `CashBox` entity with an `OpeningBalance` and a `CurrentBalance` that is always computed as the sum of all its `CashTransaction` records — never stored as a mutable field.
- **FR-002**: The system MUST prevent any transaction that would cause `CashBox.CurrentBalance` to go below zero — validation occurs BEFORE the transaction is created.
- **FR-003**: Every invoice payment (sales, purchase, customer payment, supplier payment) with `PaidAmount > 0` MUST create a corresponding `CashTransaction` record referencing the invoice and cash box.
- **FR-004**: A cash transfer between two boxes MUST create exactly two atomic `CashTransaction` records (TransferOut + TransferIn) within a single database transaction. Partial transfer is forbidden.
- **FR-005**: `CashTransaction` records are immutable once created — no update or hard-delete is permitted. Corrections must be made via a new offsetting transaction.
- **FR-006**: The system MUST support a `DailyClosure` operation that computes: `ClosingBalance = OpeningBalance + TotalIncome - TotalExpense` for a specific cash box and date.
- **FR-007**: Only one `DailyClosure` may exist per cash box per calendar day — attempting a second closure for the same box and date must be rejected.
- **FR-008**: `CashTransaction` type MUST be one of: `OpeningBalance(1)`, `SalesIncome(2)`, `Expense(3)`, `TransferOut(4)`, `TransferIn(5)`, `RefundOut(6)`, `SupplierPayment(7)`, `CustomerPayment(8)`.
- **FR-009**: The system MUST support multiple active cash boxes simultaneously. Each box tracks its own balance independently.
- **FR-010**: A sales return refund (cash-paid original invoice) MUST create a `RefundOut` transaction that reduces the cash box balance.
- **FR-011**: All `CashTransaction` amounts must use `decimal(18,2)` precision. Negative amounts are forbidden — transaction type encodes direction.
- **FR-012**: Every `CashTransaction` MUST record: `CashBoxId`, `TransactionType`, `Amount`, `BalanceBefore`, `BalanceAfter`, `ReferenceType`, `ReferenceId`, `CreatedByUserId`, `CreatedAt`.

### Key Entities

- **CashBox**: Represents a physical cash drawer or safe. Attributes: `Name`, `OpeningBalance`, `IsActive`. `CurrentBalance` is computed (not stored).
- **CashTransaction**: Immutable record of a single financial event in a cash box. References the cash box, the transaction type, amount, balance snapshot before/after, and optional invoice reference.
- **DailyClosure**: Immutable end-of-day snapshot per cash box per date. Records: `CashBoxId`, `ClosureDate`, `OpeningBalance`, `TotalIncome`, `TotalExpense`, `ClosingBalance`, `ClosedByUserId`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cash box balance is always arithmetically correct — the sum of all `CashTransaction.Amount` values (with directional sign by type) equals `CurrentBalance` with 100% accuracy.
- **SC-002**: Transactions that would cause a negative balance are rejected 100% of the time, with no partial writes occurring.
- **SC-003**: Every posted invoice with `PaidAmount > 0` has a corresponding `CashTransaction` — 0% orphaned payments.
- **SC-004**: Cash transfers between boxes complete atomically — 0 cases of a `TransferOut` existing without its matching `TransferIn`.
- **SC-005**: Daily closure computation matches manual calculation (OpeningBalance + TotalIncome − TotalExpense) with zero discrepancy.
- **SC-006**: Balance inquiry and transaction history load within 1 second for boxes with up to 10,000 transactions.

---

## Assumptions

- A store may have between 1 and 10 active cash boxes (typical: 1–3).
- The system operates in a single time zone — daily closure is based on the local server date.
- There is no integration with external payment terminals (credit card machines) in this phase — cash only.
- An "expense" in a cash box represents petty cash disbursements (e.g., supplies, utilities) recorded manually by the manager.
- Customer/Supplier payments recorded outside of invoices (e.g., standalone partial payment) are also linked to a cash box and create `CustomerPayment(8)` or `SupplierPayment(7)` transactions.
- The `OpeningBalance` of a cash box is set once when the box is first created — subsequent days use the previous day's closing balance as the new opening balance.
- Cancellation of a posted invoice with cash payment must reverse the cash transaction via an offsetting entry (not deletion).
