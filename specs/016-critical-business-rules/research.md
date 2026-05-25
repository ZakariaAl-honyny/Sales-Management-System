# Research: Critical Business Rules Reference (Phase 16)

**Feature**: `016-critical-business-rules`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: Thread-Safe Invoice Number Generation

**Decision**: Use a centralized `SemaphoreSlim` (or similar async lock mechanism) within a dedicated singleton `DocumentSequenceService`.

**Rationale**: When multiple cashiers attempt to checkout concurrently, standard database queries like `MAX(InvoiceNumber) + 1` will result in duplicate keys. An application-level lock ensures numbers are dispensed sequentially and safely without causing heavy database table locks.

---

### D-002: Validation vs. Transaction Ordering

**Decision**: The service layer must perform "Read-Only" validations (e.g., checking if `RequestedQuantity <= WarehouseStock`) *before* calling `BeginTransactionAsync`. 

**Rationale**: Opening a transaction locks resources. By validating prerequisites before opening the transaction, we minimize the duration of the lock, heavily improving concurrency and system performance. Only after basic validations pass do we open the transaction, create the draft, save, calculate totals, validate `PaidAmount`, deduct stock, post, and commit.

---

### D-003: Domain Logic Isolation

**Decision**: The calculation of `LineTotal`, `SubTotal`, `TotalAmount`, and the validation of `PaidAmount <= TotalAmount` will remain strictly encapsulated inside the Domain entities (e.g., `SalesInvoice`).

**Rationale**: This enforces Clean Architecture and the Anemic Domain Model anti-pattern avoidance (Rule-042). The application layer simply calls `invoice.SetPaidAmount(amount)` which internally throws a `DomainException` if the amount exceeds the total. The Application layer catches this (or lets it bubble up) and triggers a transaction rollback.
