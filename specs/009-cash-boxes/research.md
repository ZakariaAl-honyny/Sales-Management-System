# Research: Cash Boxes (v4.3)

**Feature**: `009-cash-boxes`
**Date**: 2026-05-24
**Status**: Complete — All unknowns resolved

---

## Decision Log

### D-001: CurrentBalance Storage Strategy

**Decision**: `CurrentBalance` is NOT stored as a column. It is always computed on demand as the sum of `CashTransaction.Amount` values, with income types adding and expense/outgoing types subtracting.

**Rationale**: Storing a mutable balance creates a drift risk — if any transaction write fails silently, the stored balance diverges from reality. Computing from the immutable transaction log is always correct. For performance (SC-006: < 1s for 10,000 transactions), an index on `CashTransactions(CashBoxId, CreatedAt)` is sufficient.

**Alternatives considered**:
- *Stored balance with event sourcing reconciliation*: Rejected — adds complexity without benefit at this scale.
- *Denormalized cached balance updated on every write*: Rejected — violates FR-001 and creates eventual consistency risk.

---

### D-002: Amount Sign Convention

**Decision**: All `CashTransaction.Amount` values are **positive decimals**. The **direction** (credit vs debit) is encoded by `TransactionType`:
- Credit types (increase balance): `OpeningBalance(1)`, `SalesIncome(2)`, `TransferIn(5)`, `CustomerPayment(8)`
- Debit types (decrease balance): `Expense(3)`, `TransferOut(4)`, `RefundOut(6)`, `SupplierPayment(7)`

**Rationale**: Storing negative amounts adds confusion (double-negatives in queries). Using type-as-direction matches standard double-entry bookkeeping conventions and makes `CHECK (Amount > 0)` enforceable at DB level (FR-011).

---

### D-003: Atomic Transfer Pattern

**Decision**: `CashBoxService.TransferAsync` follows the 7-step transaction protocol:
1. Validate source balance ≥ transfer amount (BEFORE transaction)
2. Validate source ≠ destination (BEFORE transaction)
3. `BeginTransactionAsync`
4. Read source `CurrentBalance` (inside transaction for concurrency safety)
5. Re-validate balance (double-check inside transaction)
6. Create `TransferOut` on source
7. Create `TransferIn` on destination
8. `CommitAsync` — on ANY exception: `RollbackAsync`

**Rationale**: The pre-transaction check (step 1) gives a fast user-friendly error. The in-transaction re-check (step 5) handles race conditions where two concurrent transfers could both pass the pre-check but only one can succeed (RULE-003).

---

### D-004: Invoice Integration Point

**Decision**: `CashTransaction` creation for invoice payments is triggered **inside the existing invoice posting transaction**, after the invoice is saved and has an ID. It is injected via `ICashBoxService.RecordInvoicePaymentAsync(cashBoxId, amount, type, invoiceId, invoiceType, userId, ct)`.

**Rationale**: Keeps the cash transaction and invoice post atomic — no orphaned `CashTransaction` without a valid invoice reference (SC-003). Follows the existing pattern established by stock deduction in `PurchaseInvoiceService`.

**Alternatives considered**:
- *Domain event pattern*: Rejected — EventBus is desktop-only; the API layer needs synchronous cash tracking.
- *Separate API call from desktop after invoice post*: Rejected — creates a window where invoice is posted but cash box is not updated.

---

### D-005: DailyClosure Uniqueness Enforcement

**Decision**: Enforced at two layers:
1. **DB**: `UNIQUE INDEX` on `DailyClosures(CashBoxId, ClosureDate)`.
2. **Application**: Pre-insert check returns `Result.Failure("تم إغلاق الصندوق بالفعل لهذا اليوم")` before hitting the DB constraint.

**Rationale**: Defense-in-depth (Constitution IX). The application-level check gives a user-friendly Arabic error; the DB constraint is the final safety net.

---

### D-006: BalanceBefore / BalanceAfter Snapshot

**Decision**: Each `CashTransaction` stores `BalanceBefore` and `BalanceAfter` as a snapshot at the time of creation. These are computed inside the service before writing.

**Rationale**: Enables instant single-row audit queries without having to sum all prior transactions. Also provides data integrity verification — if `BalanceAfter - BalanceBefore` doesn't match `±Amount`, data corruption is detectable (FR-012).

---

### D-007: SalesInvoice / PurchaseInvoice Schema Extension

**Decision**: Add `CashBoxId int NULL FK` to `SalesInvoices` and `PurchaseInvoices`. Nullable because credit invoices (`PaidAmount = 0`) have no cash box association.

**Rationale**: FR-003 requires the cash transaction to reference the invoice. The FK on the invoice table enables the reverse query too (given an invoice, find its cash box). `NULL` for credit invoices avoids phantom FK rows.

---

### D-008: CashBox Soft Delete

**Decision**: `CashBox` uses soft delete (`IsActive = false`). Hard delete is forbidden because `CashTransaction` records reference the cash box via FK with `DeleteBehavior.Restrict`.

**Rationale**: Constitution XIII — hard delete blocked by referencing transactions. Soft delete preserves history.
