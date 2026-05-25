# Data Model: Critical Business Rules Reference (Phase 16)

**Feature**: `016-critical-business-rules`

*(Note: This phase does not introduce new entities, but enforces strict validation and state mutation rules on existing entities).*

## Entities Modified/Audited

### `SalesInvoice` & `PurchaseInvoice`
**Location**: `SalesSystem.Domain/Entities/...`
- **Method Audit**: Ensure methods like `AddPayment(decimal amount)` include Guard Clauses:
  ```csharp
  if (PaidAmount + amount > TotalAmount)
      throw new DomainException("المبلغ المدفوع أكبر من الإجمالي");
  ```
- **State Audit**: Ensure status only transitions to `Posted` (2) via explicit methods.

### `WarehouseStock`
**Location**: `SalesSystem.Domain/Entities/Inventory/WarehouseStock.cs`
- **Method Audit**: Ensure methods like `DecreaseStock(decimal quantity)` include Guard Clauses:
  ```csharp
  if (CurrentQuantity < quantity)
      throw new DomainException("المخزون غير كافٍ");
  ```

### `DocumentSequence`
**Location**: `SalesSystem.Domain/Entities/Settings/DocumentSequence.cs`
- Ensure the table supports the `DocumentSequenceService` atomic updates.

## EF Core Configuration

No schema changes required. The focus is strictly on the implementation of `IUnitOfWork` inside `SalesSystem.Infrastructure/Persistence/UnitOfWork.cs` to ensure `BeginTransactionAsync`, `CommitAsync`, and `RollbackAsync` are flawlessly implemented using `IDbContextTransaction`.
