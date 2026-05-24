# UI Contracts: Cash Boxes (v4.3)

## ViewModels

### `CashBoxesListViewModel`
Displays a list of all active cash boxes with their real-time current balance.
- Properties: `ObservableCollection<CashBoxDto> CashBoxes`, `CashBoxDto SelectedCashBox`
- Commands: `AddCommand`, `ViewTransactionsCommand`, `TransferCommand`, `CloseDayCommand`, `DeactivateCommand`
- Subscribe to `CashBoxChangedMessage` on EventBus; unsubscribe in `Dispose()`

### `CashBoxEditorViewModel`
Create a new cash box.
- Fields: `Name*` (TextBox, ToolTip="اسم الصندوق مطلوب"), `OpeningBalance*` (NumericTextBox ≥ 0)
- `SaveCommand` always enabled. `Validate()` shows `ShowWarningAsync` on missing fields.
- On success: `ShowSuccess("تم إنشاء الصندوق بنجاح")` toast + publish `CashBoxChangedMessage`.

### `CashBoxTransactionsViewModel`
Displays the transaction history for a selected cash box.
- Properties: `ObservableCollection<CashTransactionDto> Transactions`, `decimal CurrentBalance`, `DateRange FilterRange`
- Commands: `RecordExpenseCommand`, `RefreshCommand`
- RecordExpense shows an inline dialog for Amount + Notes, then calls the API.

### `CashTransferViewModel`
Handles cash transfer between two boxes.
- Fields: `SourceCashBox` (ComboBox), `DestinationCashBox` (ComboBox), `Amount*` (NumericTextBox > 0), `Notes` (TextBox)
- `TransferCommand` always enabled. Validate shows warning if amount > source balance.
- On success: `ShowSuccess("تم التحويل بنجاح")` toast + refresh both box balances.

### `DailyClosureViewModel`
Performs and views daily closures.
- Displays: Today's income total, expense total, projected closing balance.
- `CloseCommand`: confirms via `ShowConfirmationAsync("هل تريد إغلاق الصندوق لهذا اليوم؟")` before posting.

## Navigation
- Cash Boxes section is accessible from the main menu (side nav).
- Transactions view opens in a separate `ScreenWindowService` panel.
