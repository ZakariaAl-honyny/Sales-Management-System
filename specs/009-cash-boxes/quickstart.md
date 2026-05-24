# Quickstart: Cash Boxes (v4.3)

## Implementation Order

1. **Domain entities**: Create `CashBox`, `CashTransaction`, `DailyClosure` with guard clauses and no EF Core dependencies.
2. **Enum**: Add `CashTransactionType` to `SalesSystem.Domain/Enums/`.
3. **EF Core**: Add configurations for three new tables. Extend `SalesInvoice` and `PurchaseInvoice` with nullable `CashBoxId`. Add `CashBoxId` (required) to `CustomerPayment` and `SupplierPayment`.
4. **Migration**: Run `dotnet ef migrations add AddCashBoxes`.
5. **Service**: Implement `CashBoxService` with all methods. Key: `CurrentBalance` computation via LINQ sum, never stored.
6. **Invoice integration**: In `SalesInvoiceService.PostAsync` and `PurchaseInvoiceService.PostAsync`, call `ICashBoxService.RecordInvoicePaymentAsync` inside the existing transaction, after the invoice ID is obtained.
7. **API**: Create `CashBoxesController` with all endpoints under `[Authorize]`.
8. **Desktop**: Create API service, ViewModels, Views in the WPF project.

## Key Invariants to Verify

- `CashBox.CurrentBalance` is never written to the database — any query that reads it must compute from transactions.
- A `TransferOut` always has exactly one matching `TransferIn` in the same transaction.
- `DailyClosure` records have a UNIQUE constraint — verify migration includes the index.
- `Amount > 0` DB CHECK constraint is in the migration.
