# Tasks: Desktop Modules (Phase 5)

**Branch**: `005-desktop-modules`
**Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md) | **Data Model**: [data-model.md](data-model.md)
**Note for implementer**: Read `quickstart.md` and `AGENTS.md` before starting. Every `UserControl`/`Form` MUST set `RightToLeft = RightToLeft.Yes`. Use `decimal` for all money/qty fields. All API calls return `Result<T>` â€” never throw exceptions.

---

## Phase 1: Setup

- [x] T001 Install ClosedXML NuGet in Desktop project: `dotnet add SalesSystem/SalesSystem.Desktop/SalesSystem.Desktop.csproj package ClosedXML --version 0.102.*`
- [x] T002 Create folder structure under `SalesSystem/SalesSystem.Desktop/`: `Controls/Products/`, `Controls/Customers/`, `Controls/Suppliers/`, `Controls/Warehouses/`, `Controls/Sales/`, `Controls/Purchases/`, `Controls/SalesReturns/`, `Controls/PurchaseReturns/`, `Controls/StockTransfers/`, `Controls/Payments/`, `Controls/Reports/`, `Controls/Dashboard/`, `Controls/Categories/`, `Controls/Units/`, `Services/Api/Interfaces/`, `Messaging/Messages/`

---

## Phase 2: Foundational (Blocking â€” complete before any user story)

**âڑ ï¸ڈ All user story phases depend on this phase being complete.**

- [x] T003 Create `SalesSystem/SalesSystem.Desktop/Services/HttpClientService.cs` â€” a base service class with `private readonly HttpClient _client; private readonly IAuthService _auth;` constructor. Implement `Task<List<T>?> GetListAsync<T>(string path)`, `Task<T?> GetAsync<T>(string path)`, `Task<Result<T>> PostAsync<T>(string path, object body)`, `Task<Result<T>> PutAsync<T>(string path, object? body = null)`, `Task<Result> DeleteAsync(string path)`. Each method: add `Authorization: Bearer {_auth.Token}` header, deserialize with `System.Text.Json`, catch `HttpRequestException` and return `Result.Failure(...)` on error.
- [x] T004 [P] Create all EventBus message records in `SalesSystem/SalesSystem.Desktop/Messaging/Messages/`: `ProductChangedMessage.cs` (`record ProductChangedMessage(int ProductId)`), `CategoryChangedMessage.cs`, `UnitChangedMessage.cs`, `CustomerChangedMessage.cs`, `SupplierChangedMessage.cs`, `WarehouseChangedMessage.cs`, `SaleInvoiceChangedMessage.cs`, `PurchaseInvoiceChangedMessage.cs`, `SalesReturnChangedMessage.cs`, `PurchaseReturnChangedMessage.cs`, `StockTransferChangedMessage.cs`, `CustomerPaymentChangedMessage.cs`, `SupplierPaymentChangedMessage.cs`, `StockChangedMessage.cs` (`record StockChangedMessage(int ProductId, int WarehouseId)`)
- [x] T005 [P] Create API service interfaces in `SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/`. Create `IProductApiService.cs` with methods: `Task<List<ProductResponse>> GetAllAsync(string? search, int? categoryId, bool includeInactive)`, `Task<ProductResponse?> GetByBarcodeAsync(string barcode)`, `Task<Result<ProductResponse>> CreateAsync(CreateProductRequest r)`, `Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest r)`, `Task<Result> DeactivateAsync(int id)`, `Task<Result> ReactivateAsync(int id)`.
- [x] T006 [P] Create `ICategoryApiService.cs`: `GetAllAsync(bool includeInactive)`, `CreateAsync(CreateCategoryRequest r)`, `DeleteAsync(int id)`. Create `IUnitApiService.cs`: same pattern for units. Both in `Services/Api/Interfaces/`.
- [x] T007 [P] Create `ICustomerApiService.cs` and `ISupplierApiService.cs` in `Services/Api/Interfaces/`: `GetAllAsync(string? search, bool includeInactive)`, `GetByIdAsync(int id)`, `CreateAsync(...)`, `UpdateAsync(int id, ...)`, `DeactivateAsync(int id)`, `ReactivateAsync(int id)`.
- [x] T008 [P] Create `IWarehouseApiService.cs`: `GetAllAsync(bool includeInactive)`, `GetByIdAsync(int id)`, `GetStockAsync(int warehouseId)â†’List<WarehouseStockSummaryResponse>`, `CreateAsync(...)`, `UpdateAsync(int id, ...)`, `SetDefaultAsync(int id)`.
- [x] T009 [P] Create `ISalesInvoiceApiService.cs`: `GetAllAsync(DateTime? from, DateTime? to, InvoiceStatus? status)`, `GetByIdAsync(int id)`, `CreateAsync(CreateSalesInvoiceRequest r)`, `UpdateAsync(int id, CreateSalesInvoiceRequest r)`, `PostAsync(int id)`, `CancelAsync(int id)`. Create `IPurchaseInvoiceApiService.cs` mirroring this with Purchase types.
- [x] T010 [P] Create `ISalesReturnApiService.cs` and `IPurchaseReturnApiService.cs` in `Services/Api/Interfaces/`: `GetAllAsync(DateTime? from, DateTime? to)`, `GetByIdAsync(int id)`, `CreateAsync(...)`, `PostAsync(int id)`, `CancelAsync(int id)`.
- [x] T011 [P] Create `IStockTransferApiService.cs`: same 5 methods as returns but with `CreateStockTransferRequest`. Create `ICustomerPaymentApiService.cs`: `GetAllAsync(int? customerId, DateTime? from, DateTime? to)`, `CreateAsync(CreateCustomerPaymentRequest r)`. Create `ISupplierPaymentApiService.cs` mirroring it.
- [x] T012 [P] Create `IReportApiService.cs`: methods returning `Task<DataTable>` â€” `GetDailySalesReportAsync(DateTime from, DateTime to)`, `GetDailyPurchasesReportAsync(...)`, `GetStockReportAsync(int? warehouseId)`, `GetCustomerBalanceReportAsync(int? customerId)`, `GetSupplierBalanceReportAsync(int? supplierId)`, `GetProductMovementReportAsync(int? productId, DateTime? from, DateTime? to)`, `GetLowStockReportAsync()`. Create `IDashboardApiService.cs`: `GetSummaryAsync()â†’DashboardSummaryResponse`.
- [x] T013 Implement all concrete API services in `SalesSystem/SalesSystem.Desktop/Services/Api/`: `ProductApiService.cs`, `CategoryApiService.cs`, `UnitApiService.cs`, `CustomerApiService.cs`, `SupplierApiService.cs`, `WarehouseApiService.cs`, `SalesInvoiceApiService.cs`, `PurchaseInvoiceApiService.cs`, `SalesReturnApiService.cs`, `PurchaseReturnApiService.cs`, `StockTransferApiService.cs`, `CustomerPaymentApiService.cs`, `SupplierPaymentApiService.cs`, `ReportApiService.cs`, `DashboardApiService.cs`. Each injects `HttpClientService` and calls the correct API path (see `contracts/api-contracts.md` for all paths).
- [x] T014 Register all new services in `SalesSystem/SalesSystem.Desktop/Program.cs`: `services.AddTransient<IProductApiService, ProductApiService>()` for all 15 services. Also register `services.AddSingleton<HttpClientService>()`.

**Checkpoint**: Build must succeed with 0 errors before proceeding.

---

## Phase 3: US1 â€” Products CRUD Module (P1) ًںژ¯ MVP

**Goal**: Searchable product list with add/edit/deactivate and Category+Unit sub-dialogs.
**Independent Test**: Run Desktop, navigate to Products, add a product, verify it appears. Search by name. Deactivate it, verify it disappears. Toggle "Show Deactivated", verify it reappears and can be re-activated.

- [x] T015 [P] [US1] Create `SalesSystem/SalesSystem.Desktop/Controls/Categories/CategoryManagerDialog.cs` â€” a `Form` (modal) with `DataGridView` (single Name column, ReadOnly), `Button btnAdd`, `Button btnDelete`, `Button btnClose`. On load, call `ICategoryApiService.GetAllAsync()` and bind to grid. Add creates via `CreateAsync()`, Delete calls `DeleteAsync()`. Publishes `CategoryChangedMessage` after each change. `RightToLeft = Yes`, `RightToLeftLayout = true`.
- [x] T016 [P] [US1] Create `SalesSystem/SalesSystem.Desktop/Controls/Units/UnitManagerDialog.cs` â€” identical structure to CategoryManagerDialog but for Units via `IUnitApiService`. Has Name + Symbol columns.
- [x] T017 [US1] Create `SalesSystem/SalesSystem.Desktop/Controls/Products/ProductEditorForm.cs` â€” a modal `Form` with fields: `txtCode`, `txtBarcode`, `txtName`, `cmbCategory` (ComboBox), `cmbUnit` (ComboBox), `numPurchasePrice` (MoneyTextBox/decimal), `numSalePrice`, `numMinStock` (3dp), `txtDescription`, `btnSaveCategory` (small "+" button next to cmbCategory that opens CategoryManagerDialog), `btnSaveUnit` (same for UnitManagerDialog), `btnSave`, `btnCancel`. On load: populate cmbCategory from `ICategoryApiService.GetAllAsync()`, populate cmbUnit from `IUnitApiService.GetAllAsync()`. If editId provided, populate fields from `IProductApiService.GetByIdAsync(editId)`. On Save: validate Code and Name non-empty, call `CreateAsync` or `UpdateAsync`, on success publish `ProductChangedMessage`, close with `DialogResult.OK`.
- [x] T018 [US1] Create `SalesSystem/SalesSystem.Desktop/Controls/Products/ProductsListControl.cs` â€” a `UserControl`. Controls: `SearchBarControl` (300ms debounce), `CheckBox chkShowInactive` (Arabic label: "ط¹ط±ط¶ ط؛ظٹط± ط§ظ„ظ†ط´ط·"), `DataGridView dgvProducts` (columns: Code, Barcode, Name, Category, Unit, SalePrice, Stock Status, IsActive), `ToolStrip` with buttons: `btnAdd` ("ط¥ط¶ط§ظپط©"), `btnEdit` ("طھط¹ط¯ظٹظ„"), `btnDeactivate` ("طھط¹ط·ظٹظ„"), `btnReactivate` ("طھظپط¹ظٹظ„"), `btnRefresh` ("طھط­ط¯ظٹط«"). Subscribe to `ProductChangedMessage` in `OnLoad`, unsubscribe in `Dispose(bool)`. LoadData calls `IProductApiService.GetAllAsync(search, categoryId:null, includeInactive: chkShowInactive.Checked)`. Double-click on row opens `ProductEditorForm` in edit mode. `btnDeactivate` shows confirmation MessageBox in Arabic then calls `DeactivateAsync`. `btnReactivate` calls `ReactivateAsync`. `RightToLeft = Yes`. Apply role check: hide `btnAdd`, `btnEdit`, `btnDeactivate` if user role < Manager.
- [x] T019 [US1] Replace placeholder in `Program.cs`: change `services.AddTransient<ProductsPlaceholderControl>()` to `services.AddTransient<ProductsListControl>()`. Update `NavigationService` or `MainForm` to resolve `ProductsListControl` instead of the placeholder.

**Checkpoint**: Products module fully functional. EventBus fires on save. Category and Unit sub-dialogs open from ProductEditorForm.

---

## Phase 4: US2 â€” Customers & Suppliers CRUD (P1)

**Goal**: Customer and Supplier list screens with balance display and full CRUD.
**Independent Test**: Add customer, verify zero balance. Search by name/phone. Deactivate and re-activate. Repeat for Supplier.

- [x] T020 [P] [US2] Create `SalesSystem/SalesSystem.Desktop/Controls/Customers/CustomerEditorForm.cs` â€” modal Form with fields: `txtName` (required), `txtPhone`, `txtAddress`, `btnSave`, `btnCancel`. Create and Update via `ICustomerApiService`. Publishes `CustomerChangedMessage` on success.
- [x] T021 [P] [US2] Create `SalesSystem/SalesSystem.Desktop/Controls/Customers/CustomersListControl.cs` â€” UserControl with SearchBarControl, `chkShowInactive`, DataGridView columns: Name, Phone, Address, CurrentBalance (formatted 2dp, red if >0, green if <0, gray if 0), IsActive. ToolStrip: Add, Edit, Deactivate, Reactivate, Refresh. Cashier role: hide Add/Edit/Deactivate/Reactivate buttons. Subscribe/unsubscribe `CustomerChangedMessage`.
- [x] T022 [P] [US2] Create `SalesSystem/SalesSystem.Desktop/Controls/Suppliers/SupplierEditorForm.cs` and `SalesSystem/SalesSystem.Desktop/Controls/Suppliers/SuppliersListControl.cs` â€” mirror Customer files exactly but use `ISupplierApiService`, `SupplierChangedMessage`, and "Supplier owed" balance semantics (positive = we owe them).
- [x] T023 [US2] Register new controls in `Program.cs` and replace Customer/Supplier placeholders in navigation.

**Checkpoint**: Both Customers and Suppliers screens functional with balance color-coding.

---

## Phase 5: US3 â€” Warehouses Module (P1)

**Goal**: Admin-only warehouse management with per-product stock view.
**Independent Test**: Add warehouse, set as default, view stock breakdown.

- [x] T024 [P] [US3] Create `SalesSystem/SalesSystem.Desktop/Controls/Warehouses/WarehouseEditorForm.cs` â€” modal Form: `txtName` (required), `txtAddress`, `chkIsDefault`, `btnSave`, `btnCancel`. Calls `IWarehouseApiService.CreateAsync` or `UpdateAsync`. If `chkIsDefault` checked on save, also calls `SetDefaultAsync`. Publishes `WarehouseChangedMessage`.
- [x] T025 [US3] Create `SalesSystem/SalesSystem.Desktop/Controls/Warehouses/WarehousesListControl.cs` â€” UserControl, AdminOnly (hide all buttons if role < Admin). DataGridView columns: Name, Address, IsDefault (âœ“ marker), IsActive. Second tab or expandable panel shows stock per product when a warehouse row is selected: calls `GetStockAsync(warehouseId)` and binds to a second DataGridView (ProductName, ProductCode, Quantity). Subscribe/unsubscribe `WarehouseChangedMessage`.
- [x] T026 [US3] Register `WarehousesListControl` in `Program.cs` and replace warehouse placeholder.

**Checkpoint**: Warehouses screen Admin-only. Stock view works per warehouse.

---

## Phase 6: US4 â€” Sales Invoice Module (P1) ًںژ¯

**Goal**: Full sales invoice creation, draft save, posting with stock deduction.
**Independent Test**: Create invoice with 3 items, apply discount, toggle tax, post. Verify stock decreased and customer balance updated. Verify invoice number format INV-YYYY-NNNNNN.

- [x] T027 [US4] Create `SalesSystem/SalesSystem.Desktop/Controls/Sales/SalesInvoiceForm.cs` â€” a full-size modal Form. Header section: `cmbCustomer` (ComboBox, loads all active customers), `cmbWarehouse` (ComboBox, loads all active warehouses, pre-selects default), `dtpInvoiceDate` (DateTimePicker), `cmbPaymentType` (Cash/Credit/Mixed). Tax section: `chkTaxInclusive` (CheckBox, Arabic: "ط´ط§ظ…ظ„ ط§ظ„ط¶ط±ظٹط¨ط©"), `numTaxRate` (NumericUpDown, 0-100, 2dp). Line items section: `DataGridView dgvItems` with columns: ProductCode (TextBox for barcode scan), ProductName, Quantity (decimal, editable), UnitPrice (decimal, editable), DiscountAmount (decimal, editable), LineTotal (computed, read-only). Buttons under grid: `btnAddItem` ("ط¥ط¶ط§ظپط© طµظ†ظپ"), `btnRemoveItem` ("ط­ط°ظپ طµظ†ظپ"). Totals panel (read-only labels): SubTotal, TaxAmount, InvoiceDiscount (NumericUpDown), TotalAmount, PaidAmount (MoneyTextBox, enabled for Mixed/Cash), DueAmount. Action buttons: `btnSaveDraft`, `btnPost`, `btnCancel` (cancel invoice), `btnClose`. Status badge label. On every `CellEndEdit` in dgvItems and on tax/discount change: recompute display preview using formula from `plan.md` آ§Key Design Decisions. On `btnPost` click: confirm with Arabic dialog, call `ISalesInvoiceApiService.PostAsync(id)`, on success publish `SaleInvoiceChangedMessage` and `StockChangedMessage` for each item.
- [x] T028 [US4] Create `SalesSystem/SalesSystem.Desktop/Controls/Sales/SalesListControl.cs` â€” UserControl with date range pickers (`dtpFrom`, `dtpTo`), status filter ComboBox, DataGridView (InvoiceNumber, Customer, Date, TotalAmount, Status), ToolStrip: New, View, Post, Cancel Invoice, Refresh. New opens blank `SalesInvoiceForm`. View/double-click opens in read-only mode for Posted/Cancelled. Subscribe/unsubscribe `SaleInvoiceChangedMessage`.
- [x] T029 [US4] Register `SalesListControl` in `Program.cs`, replace sales placeholder. Apply role gating: all roles see list; Cashier cannot access Cancel Invoice button.

**Checkpoint**: Full sales flow works end-to-end. Real-time totals update. Tax toggle affects TotalAmount correctly.

---

## Phase 7: US5 â€” Purchase Invoice Module (P1)

**Goal**: Purchase invoice with supplier, warehouse destination, and stock increase on post.
**Independent Test**: Create purchase with 2 items, post. Verify stock increased and supplier balance updated.

- [x] T030 [US5] Create `SalesSystem/SalesSystem.Desktop/Controls/Purchases/PurchaseInvoiceForm.cs` â€” mirror `SalesInvoiceForm` but: `cmbSupplier` instead of `cmbCustomer`, `UnitCost` column instead of `UnitPrice`, warehouse is "destination". API calls go to `IPurchaseInvoiceApiService`. Publishes `PurchaseInvoiceChangedMessage`.
- [x] T031 [US5] Create `SalesSystem/SalesSystem.Desktop/Controls/Purchases/PurchasesListControl.cs` â€” mirror `SalesListControl` using `IPurchaseInvoiceApiService` and `PurchaseInvoiceChangedMessage`. Role: Cashier cannot access this screen at all (hide in navigation).
- [x] T032 [US5] Register `PurchasesListControl` in `Program.cs`, replace purchases placeholder.

**Checkpoint**: Purchases module functional. Stock increases on post. Supplier balance updated.

---

## Phase 8: US6 â€” Returns Modules (P2)

**Goal**: Sales returns (stock in, customer balance down) and purchase returns (stock out, supplier balance down).
**Independent Test**: Create sales return referencing original invoice, verify qty validation and stock reversal. Repeat for purchase return.

- [x] T033 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/SalesReturns/SalesReturnForm.cs` â€” Form with: optional `cmbOriginalInvoice` (ComboBox to select original sales invoice by number), `cmbCustomer`, `cmbWarehouse`, tax toggle, line items grid. Validation: if original invoice selected, quantity entered must not exceed (original qty âˆ’ already returned qty). Calls `ISalesReturnApiService`. Publishes `SalesReturnChangedMessage`.
- [x] T034 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/SalesReturns/SalesReturnsListControl.cs` â€” standard list pattern using `ISalesReturnApiService`. Subscribe `SalesReturnChangedMessage`.
- [x] T035 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/PurchaseReturns/PurchaseReturnForm.cs` and `PurchaseReturnsListControl.cs` â€” mirror Sales Returns but for suppliers via `IPurchaseReturnApiService`. Publishes `PurchaseReturnChangedMessage`.
- [x] T036 [US6] Register both return list controls in `Program.cs`, replace placeholders.

**Checkpoint**: Both return flows functional. Qty validation enforced. Stock and balance reversed on post.

---

## Phase 9: US7 â€” Stock Transfer Module (P2)

**Goal**: Transfer stock between two warehouses with movement audit trail.
**Independent Test**: Transfer 10 units from Warehouse A to B. Verify A stock âˆ’10, B stock +10, two InventoryMovement records.

- [x] T037 [US7] Create `SalesSystem/SalesSystem.Desktop/Controls/StockTransfers/StockTransferForm.cs` â€” Form with `cmbSourceWarehouse`, `cmbDestinationWarehouse` (validate source â‰  destination on Post, show Arabic error if equal), `dtpTransferDate`, `txtNotes`, line items grid (ProductCode/Name/Quantity columns). Calls `IStockTransferApiService`. Publishes `StockTransferChangedMessage` and `StockChangedMessage` per item.
- [x] T038 [US7] Create `SalesSystem/SalesSystem.Desktop/Controls/StockTransfers/StockTransfersListControl.cs` â€” standard list pattern. Role: ManagerAndAbove only. Subscribe `StockTransferChangedMessage`.
- [x] T039 [US7] Register `StockTransfersListControl` in `Program.cs`, replace placeholder.

**Checkpoint**: Transfer posts correctly. Source stock decreases, destination increases. Same-warehouse transfer blocked.

---

## Phase 10: US8 â€” Payments Module (P2)

**Goal**: Record customer and supplier payments; allow overpayments (negative balance).
**Independent Test**: Record payment of 1500 on customer with balance 1000. Verify balance = âˆ’500. Filter payments by date range.

- [x] T040 [P] [US8] Create `SalesSystem/SalesSystem.Desktop/Controls/Payments/CustomerPaymentForm.cs` â€” modal Form: `cmbCustomer` (shows name + current balance), `numAmount` (MoneyTextBox), optional `cmbInvoice` (ComboBox of customer's posted unpaid invoices), `txtNotes`, `btnSave`. If amount > current balance, show warning MessageBox (Arabic: "ط³ظٹط¤ط¯ظٹ ظ‡ط°ط§ ط§ظ„ط¯ظپط¹ ط¥ظ„ظ‰ ط±طµظٹط¯ ط¯ط§ط¦ظ†") but still allow saving. Calls `ICustomerPaymentApiService.CreateAsync`. Publishes `CustomerPaymentChangedMessage`.
- [x] T041 [P] [US8] Create `SalesSystem/SalesSystem.Desktop/Controls/Payments/CustomerPaymentsListControl.cs` â€” UserControl: customer filter ComboBox, date range pickers, DataGridView (PaymentNumber, Customer, Amount, InvoiceNumber, Date), New button opens `CustomerPaymentForm`. Subscribe `CustomerPaymentChangedMessage`.
- [x] T042 [P] [US8] Create `SupplierPaymentForm.cs` and `SupplierPaymentsListControl.cs` in `Controls/Payments/` â€” mirror Customer Payment files using `ISupplierPaymentApiService` and `SupplierPaymentChangedMessage`.
- [x] T043 [US8] Register all four payment controls in `Program.cs`, replace placeholders.

**Checkpoint**: Both payment flows work. Overpayment warning shown. Balance goes negative.

---

## Phase 11: US9 â€” Reports Module (P3)

**Goal**: 7 report types with date/entity filters, Excel and CSV export.
**Independent Test**: Generate Daily Sales Report, verify totals match invoices. Export to Excel and CSV, verify files open correctly.

- [x] T044 [US9] Create `SalesSystem/SalesSystem.Desktop/Controls/Reports/ReportsControl.cs` â€” UserControl. Left panel: `ListBox` or `TreeView` with 7 report names (Arabic): Daily Sales, Daily Purchases, Stock, Customer Balance, Supplier Balance, Product Movement, Low Stock Alert. Right panel: dynamic filter area (shows/hides controls based on selected report: date pickers, customer/supplier/warehouse/product dropdowns). `btnGenerate` button calls the appropriate `IReportApiService` method, binds result `DataTable` to `DataGridView dgvReport`. Export panel: `btnExportExcel` and `btnExportCsv` buttons (disabled until report generated). Implement `ExportToExcel(DataTable, string)` using `ClosedXML`: `new XLWorkbook()`, add worksheet, `ws.Cell(1,1).InsertTable(data)`, save via `SaveFileDialog`. Implement `ExportToCsv(DataTable, string)` using `StreamWriter` with UTF-8 encoding. Role: ManagerAndAbove only.
- [x] T045 [US9] Register `ReportsControl` in `Program.cs`, replace reports placeholder.

**Checkpoint**: All 7 reports generate. Excel/CSV export produces valid files. Date filters work.

---

## Phase 12: US10 â€” Dashboard Module (P3)

**Goal**: Summary cards with today's totals, auto-refresh via EventBus.
**Independent Test**: Log in, verify cards show correct values. Post a sales invoice, verify dashboard totals update automatically.

- [x] T046 [US10] Create `SalesSystem/SalesSystem.Desktop/Controls/Dashboard/DashboardControl.cs` â€” UserControl. Layout: grid of `SummaryCardControl` instances: Today's Sales (decimal, 2dp), Today's Purchases, Total Customers, Total Products, Low Stock Alerts (count), Recent Invoices (DataGridView, last 10). On `OnLoad`: call `IDashboardApiService.GetSummaryAsync()` and bind cards. Subscribe to `SaleInvoiceChangedMessage`, `PurchaseInvoiceChangedMessage`, `StockChangedMessage` â€” each triggers a full reload via `LoadDataAsync()`. Marshal via `InvokeRequired` check. Cashier sees only sales-related cards (hide purchases and supplier cards). Unsubscribe all 3 subscriptions in `Dispose(bool)`.
- [x] T047 [US10] Register `DashboardControl` in `Program.cs`, replace dashboard placeholder. Set Dashboard as the default view on login.

**Checkpoint**: Dashboard loads on login. EventBus auto-refresh works after invoice post.

---

## Phase 13: Polish & Cross-Cutting

- [x] T048 [P] Audit all new `UserControl` and `Form` files: verify every one has `RightToLeft = RightToLeft.Yes` and forms have `RightToLeftLayout = true`.
- [x] T049 [P] Audit all `MoneyTextBox` usages: verify `decimal` parsing with 2dp for money fields and 3dp for quantity fields.
- [x] T050 [P] Audit all `Dispose(bool)` implementations in `UserControl` files: verify every EventBus subscription has a matching `_subscription?.Dispose()`.
- [x] T051 [P] Audit role-based UI gating in all list controls: verify ManagerAndAbove restrictions and AdminOnly restrictions match the Permissions Matrix in `AGENTS.md آ§6`.
- [x] T052 Remove all files from `SalesSystem/SalesSystem.Desktop/Controls/Placeholders/` that have been replaced. Remove their `services.AddTransient<XxxPlaceholderControl>()` registrations from `Program.cs`.
- [x] T053 Run `dotnet build SalesSystem/SalesSystem.sln` and fix any compiler errors or warnings related to new code.
- [ ] T054 Push branch to remote: `git add -A && git commit -m "feat: implement all Phase 5 desktop modules" && git push origin 005-desktop-modules`

---

## Dependencies & Execution Order

```
Phase 1 (Setup)
  â””â”€â”€ Phase 2 (Foundational) â†گ BLOCKS EVERYTHING
        â”œâ”€â”€ Phase 3 (US1 Products)   â†گ Start here for MVP
        â”œâ”€â”€ Phase 4 (US2 Customers/Suppliers)
        â”œâ”€â”€ Phase 5 (US3 Warehouses)
        â”‚     â””â”€â”€ Phase 6 (US4 Sales)      â†گ Needs Products + Customers + Warehouses
        â”‚           â””â”€â”€ Phase 7 (US5 Purchases)  â†گ Needs Products + Suppliers + Warehouses
        â”‚                 â”œâ”€â”€ Phase 8 (US6 Returns)
        â”‚                 â”œâ”€â”€ Phase 9 (US7 Transfers)
        â”‚                 â””â”€â”€ Phase 10 (US8 Payments)
        â””â”€â”€ Phase 11 (US9 Reports)    â†گ No blocking deps, run after Phase 7
        â””â”€â”€ Phase 12 (US10 Dashboard) â†گ No blocking deps, run last
              â””â”€â”€ Phase 13 (Polish)
```

### Parallel Opportunities

- T004â€“T012 (all message + interface creation tasks) can all run in parallel
- T015+T016 (Category + Unit dialogs) can run in parallel
- T020+T022 (Customer + Supplier forms) can run in parallel
- T033+T035 (Sales + Purchase return forms) can run in parallel
- T040+T042 (Customer + Supplier payment forms) can run in parallel

---

## Implementation Strategy

### MVP (deliver Products module first)
1. Complete Phase 1 + Phase 2 â†’ build succeeds
2. Complete Phase 3 (US1 Products) â†’ validate Products CRUD
3. **STOP and TEST**: Products screen fully working

### Full Delivery Order
1. Phases 1â€“2 (infrastructure)
2. Phase 3 (Products) â†’ Phase 4 (Customers/Suppliers) â†’ Phase 5 (Warehouses)
3. Phase 6 (Sales) â†’ Phase 7 (Purchases)
4. Phases 8â€“10 (Returns, Transfers, Payments)
5. Phases 11â€“12 (Reports, Dashboard)
6. Phase 13 (Polish)

---

## Task Count Summary

| Phase | Tasks | User Story |
|-------|-------|-----------|
| Phase 1 Setup | 2 | â€” |
| Phase 2 Foundational | 12 | â€” |
| Phase 3 Products | 5 | US1 |
| Phase 4 Customers/Suppliers | 4 | US2 |
| Phase 5 Warehouses | 3 | US3 |
| Phase 6 Sales Invoice | 3 | US4 |
| Phase 7 Purchase Invoice | 3 | US5 |
| Phase 8 Returns | 4 | US6 |
| Phase 9 Transfers | 3 | US7 |
| Phase 10 Payments | 4 | US8 |
| Phase 11 Reports | 2 | US9 |
| Phase 12 Dashboard | 2 | US10 |
| Phase 13 Polish | 7 | â€” |
| **Total** | **54** | |
