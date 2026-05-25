# Quickstart Implementation Order

1. **Domain Layer**:
   - Audit `SalesInvoice`, `PurchaseInvoice`, and `WarehouseStock` entities.
   - Verify that guard clauses exist for checking stock quantity and payment limits (`PaidAmount <= TotalAmount`).
   - Throw `DomainException` with clear Arabic messages for violations.
2. **Infrastructure Layer**:
   - Verify `UnitOfWork` implements `BeginTransactionAsync`, `CommitAsync`, and `RollbackAsync` using EF Core transactions.
   - Implement `DocumentSequenceService` using `SemaphoreSlim` to guarantee thread-safe document number generation.
3. **Application Layer (Services)**:
   - Refactor `SalesInvoiceService.CreateInvoiceAsync` (and similar methods for Purchase, Return, Transfer).
   - Implement the standard pattern:
     1. Pre-transaction validation (check stock).
     2. `await _uow.BeginTransactionAsync(ct);`
     3. Save draft invoice to DB (generates ID).
     4. Calculate totals / validate payments.
     5. Perform stock adjustments (decrease source / increase destination).
     6. Post invoice.
     7. `await transaction.CommitAsync(ct);`
     8. `catch (Exception) { await transaction.RollbackAsync(ct); return Result.Failure; }`
4. **API Layer**:
   - Ensure the global exception handler middleware catches any leaked `DomainException` and converts it to a clean `400 Bad Request`.
