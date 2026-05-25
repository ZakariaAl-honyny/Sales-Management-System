# Quickstart Implementation Order

1. **Domain & EF Core**:
   - Update `Product` entity with `ExpirationDate` and `ImagePath`.
   - Create `StockWriteOff` entity and configuration.
   - Run EF Core Migration.
2. **Application Layer (Services)**:
   - Update Product validation commands.
   - Create `InventoryWriteOffService` handling stock deduction and accounting trigger.
3. **API Controllers**:
   - Update Product controller.
   - Add new Reporting endpoint for expired products.
4. **Desktop UI (WPF)**:
   - Add Expiration Date checkbox and `DatePicker` to `ProductEditor.xaml`.
   - Implement Image Upload logic (copying to `%AppData%`).
   - Create "Expired Products" report screen.
   - Add the automated startup check to `MainViewModel`.
