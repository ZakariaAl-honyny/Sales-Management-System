# Phase 28 — Sales Module Implementation Plan
**Version**: 1.0 — Clean Architecture | .NET 10 | SQL Server
**Status**: ✅ REVIEWED & FIXED

---

## 1. Summary

Implement the complete Sales module covering SalesInvoices, SalesInvoiceLines, SalesReturns, and SalesReturnLines. This Phase enables:
- Multi-currency sales invoicing with Cash/Credit/Mixed payment types
- Per-invoice header discount, tax, and other charges
- FIFO-based COGS calculation at posting time (cost sourced from InventoryBatches)
- Automatic InventoryTransaction creation on Post (stock deduction) and Cancel (stock reversal)
- Automatic journal entries via AccountingIntegrationService (Revenue + COGS + VAT)
- Sales returns linked to original invoice lines with auto-refund
- Continuous barcode scanning POS mode (keyboard wedge)

**Scope**: 4 entities (6.1–6.4 from Module 6). CustomerReceipts (6.5) and CustomerReceiptApplications (6.6) are covered by Phase 29.

---

## 2. Key Entities

### 2.1 SalesInvoice (DocumentEntity — inherits Status)
| Field | Type | Notes |
|-------|------|-------|
| InvoiceNo | int | UNIQUE, user-facing number, generated via DocumentSequenceService |
| InvoiceDate | date | Document date (not timestamp) |
| CustomerId | int FK → Customers(Id) | Required |
| WarehouseId | smallint FK → Warehouses(Id) | Required — determines stock location |
| CurrencyId | smallint FK → Currencies(Id) | Required — multi-currency support |
| PaymentType | tinyint | 1=Cash, 2=Credit, 3=Mixed |
| CashBoxId | int FK → CashBoxes(Id) | Nullable — required for Cash/Mixed payment |
| TaxId | smallint FK → Taxes(Id) | Nullable — links to configured tax rate |
| SubTotal | decimal(18,2) | SUM of all line totals |
| DiscountAmount | decimal(18,2) | Header-level only — NO per-line discount |
| TaxAmount | decimal(18,2) | Computed from SubTotal × TaxRate |
| OtherCharges | decimal(18,2) | Additional fees (delivery, etc.) |
| NetTotal | decimal(18,2) | SubTotal - DiscountAmount + TaxAmount + OtherCharges |
| PaidAmount | decimal(18,2) | CHK 0 ≤ PaidAmount ≤ NetTotal |
| RemainingAmount | decimal(18,2) | NetTotal - PaidAmount |
| Notes | nvarchar(500) | Nullable |

### 2.2 SalesInvoiceLine (Entity — no Status)
| Field | Type | Notes |
|-------|------|-------|
| SalesInvoiceId | int FK → SalesInvoices(Id) | Cascade not allowed — Restrict |
| ProductId | int FK → Products(Id) | Required |
| ProductUnitId | int FK → ProductUnits(Id) | Required — which unit was used |
| Quantity | decimal(18,3) | In the selected unit |
| UnitPrice | decimal(18,2) | Sale price per unit in invoice currency |
| LineTotal | decimal(18,2) | Quantity × UnitPrice — simplified, no discount |

**Design decision**: LineTotal = Qty × UnitPrice. NO per-line discount, NO Cost field, NO SaleMode. Cost is sourced from InventoryBatches at posting time via FIFO. SaleMode is implicit from the ProductUnit chosen.

### 2.3 SalesReturn (DocumentEntity — inherits Status)
| Field | Type | Notes |
|-------|------|-------|
| ReturnNo | int | UNIQUE, generated via DocumentSequenceService |
| ReturnDate | date | |
| SalesInvoiceId | int FK → SalesInvoices(Id) | Links to the original invoice |
| CustomerId | int FK → Customers(Id) | |
| WarehouseId | smallint FK → Warehouses(Id) | Restock warehouse |
| CurrencyId | smallint FK → Currencies(Id) | Same currency as original invoice |
| TotalAmount | decimal(18,2) | SUM of return line amounts |
| Notes | nvarchar(500) | Nullable |

### 2.4 SalesReturnLine (Entity — no Status)
| Field | Type | Notes |
|-------|------|-------|
| SalesReturnId | int FK → SalesReturns(Id) | |
| SalesInvoiceLineId | int FK → SalesInvoiceLines(Id) | Links to the ORIGINAL invoice line |
| Quantity | decimal(18,3) | Returned quantity in original unit |
| Amount | decimal(18,2) | Refund amount = Qty × OriginalUnitPrice (or agreed return price) |

**Design decision**: SalesReturnLines link to SalesInvoiceLineId (not ProductId directly). This enables precise tracking of which original line items are being returned, simplifies reverse FIFO batch lookup, and ensures the return amount can reference the original sale price.

---

## 3. Business Rules

### 3.1 Invoice Lifecycle
```
Draft (1) → Posted (2) → Cancelled (3)
```
- **Draft → Posted**: Validates stock availability, creates InventoryTransaction + lines (stock deduction via FIFO), creates journal entries (Revenue + COGS), updates Customer.CurrentBalance for credit/mixed invoices
- **Draft → Cancelled**: Allowed only if invoice has no stock or accounting impact (Draft has none)
- **Posted → Cancelled**: Creates REVERSAL InventoryTransaction (stock add-back via FIFO), creates REVERSAL journal entries, reverses Customer.CurrentBalance update
- **Never**: Posted → Draft, Cancelled → anything

### 3.2 Stock Validation & Deduction
- Stock is validated BEFORE opening transaction: check WarehouseStocks.Quantity ≥ requested quantity for each product
- Stock deduction happens AFTER invoice is saved (invoice has ID for InventoryTransaction reference)
- Cost is sourced from InventoryBatches oldest-first (FIFO): debit QuantityRemaining from earliest batch
- Each deduction creates an InventoryTransactionLine with BatchId, Quantity, UnitCost, TotalCost

### 3.3 Payment Type Rules
- **Cash (1)**: CashBoxId required, PaidAmount = NetTotal, RemainingAmount = 0 — auto-generates full payment
- **Credit (2)**: CashBoxId nullable, PaidAmount can be 0 or partial, RemainingAmount reflects debt — updates Customer.CurrentBalance by RemainingAmount
- **Mixed (3)**: CashBoxId required, PaidAmount is partial, RemainingAmount = NetTotal - PaidAmount — updates Customer.CurrentBalance by RemainingAmount

### 3.4 Multi-Currency
- Invoice CurrencyId FK determines invoice currency
- UnitPrice is entered in invoice currency
- Journal entries convert to base currency using ExchangeRate from CurrencyRates table
- CashBox must match invoice currency or conversion is applied

### 3.5 Invoice Number Generation
- InvoiceNo is int, UNIQUE per SalesInvoices table
- Generated via IDocumentSequenceService.GetNextIntAsync("SalesInvoice", ct) — thread-safe via SemaphoreSlim
- Request DTO uses int? InvoiceNo (null = auto-generate)

### 3.6 Sales Return Rules
- Return must reference an existing Posted invoice
- Returned quantity cannot exceed original line quantity minus previously returned quantity
- On posting: creates InventoryTransaction (SaleReturn type — stock add-back), creates reversal journal entry (refund)
- Linking to SalesInvoiceLineId enables FIFO batch return: the same batch consumed at sale is credited back
- Auto-refund: if the original invoice was Cash/Mixed, a CustomerReceipt reversal is created

### 3.7 Credit Limit
- Before posting a Credit or Mixed invoice, check Customer.CheckCreditLimit(additionalAmount)
- This is a NON-THROWING domain method returning bool — SOFT WARNING only
- If warning shown, user can proceed or cancel

### 3.8 Barcode Scanning POS Mode
- Continuous input mode: no modal dialog or manual focus required
- Scan product barcode → BarcodeLookupService identifies Product + ProductUnit
- Unit is auto-selected from matching UnitBarcode (each unit can have its own barcode)
- If product found: add line with Quantity=1, auto-focus back to scan field
- If barcode matches multiple units: show quick-pick overlay for unit selection

---

## 4. Financial Formulas

```text
LineTotal        = Quantity × UnitPrice                                    (computed in Domain, stored on line)
SubTotal         = SUM(LineTotal)                                          (computed in Domain)
TaxAmount        = SubTotal × Tax.Rate / 100                               (if TaxId is set)
NetTotal         = SubTotal - DiscountAmount + TaxAmount + OtherCharges    (computed in Domain)
RemainingAmount  = NetTotal - PaidAmount                                   (computed in Domain)

Constraint: PaidAmount >= 0 AND PaidAmount <= NetTotal
```

---

## 5. Posting Flow (Status → Posted)

### Step 1: Pre-validation (BEFORE transaction)
- Check all line items reference valid products/units
- Validate stock availability per warehouse
- Check credit limit for Credit/Mixed invoices
- Validate Customer exists and is active

### Step 2: Begin transaction via IUnitOfWork.ExecuteTransactionAsync()
```
Inside ExecuteTransactionAsync:
├── Step 2a: Set invoice Status = Posted
├── Step 2b: Save invoice + lines (SaveChangesAsync)
│   └── Invoice now has an Id
├── Step 2c: Create InventoryTransaction (TransactionType=Sale)
│   └── For each line:
│       ├── Debit from oldest InventoryBatch (FIFO)
│       ├── Reduce WarehouseStocks.Quantity
│       └── Create InventoryTransactionLine (BatchId, Qty, UnitCost, TotalCost)
├── Step 2d: Create journal entries via AccountingIntegrationService
│   ├── Revenue entry:
│   │   Dr CashBox.AccountId (Cash) OR Customer.AccountId (Credit)
│   │   Cr SalesRevenue (SystemAccountMappings)
│   │   Cr VATOutput (SystemAccountMappings) — if taxable
│   └── COGS entry:
│       Dr COGS (SystemAccountMappings) — SUM of TotalCost from batches
│       Cr InventoryAsset (SystemAccountMappings)
├── Step 2e: Update Customer.CurrentBalance (if credit/mixed)
│   └── IncreaseBalance(RemainingAmount)
├── Step 2f: Create CustomerReceipt (if Cash/Mixed)
│   └── Only if PaidAmount > 0 — auto-receipt
└── Commit transaction
```

### Step 3: Return Result<SalesInvoiceDto> with full data

---

## 6. Cancellation Flow (Status → Cancelled)

```
Inside ExecuteTransactionAsync:
├── Verify current Status == Posted
├── Set Status = Cancelled
├── Create reversal InventoryTransaction (TransactionType=SaleReturn)
│   └── Credit back to original InventoryBatches (same FIFO batches)
├── Increase WarehouseStocks.Quantity for each line
├── Create reversal journal entries via AccountingIntegrationService
│   └── Dr/Cr swapped from original
├── Decrease Customer.CurrentBalance (reverse credit effect)
└── Commit
```

---

## 7. UI Screens

### 7.1 SalesInvoicesListView
- Grid: InvoiceNo, InvoiceDate, CustomerName, NetTotal, PaidAmount, RemainingAmount, Status badge
- Toolbar: Add New, Edit, Post, Cancel, Print, Search
- Filters: Date range, Customer, Status, PaymentType
- Sorting: Newest-first by InvoiceDate DESC
- Row actions: Edit (Draft only), Post (Draft only), Cancel (Posted only), View (all)

### 7.2 SalesInvoiceEditorView (non-modal via ScreenWindowService)
- Tabs or sections:
  - **Header**: InvoiceDate, Customer (searchable dropdown), Warehouse, Currency, PaymentType (Cash/Credit/Mixed), CashBox (shown for Cash/Mixed), Tax
  - **Lines DataGrid**: Product (barcode scan + search), Unit, Qty, UnitPrice, LineTotal (read-only computed)
  - **Summary**: SubTotal, DiscountAmount, TaxAmount, OtherCharges, NetTotal, PaidAmount, RemainingAmount
  - **Notes**: Multi-line text
- Barcode scan field at top: continuous input, auto-adds line
- Toolbar: Add Line, Remove Line, Save (Draft), Save & Post
- Validation on Save click (buttons always enabled — no CanExecute):
  - Customer required, at least one line, quantities > 0, amounts valid
  - Warning dialog listing ALL validation errors

### 7.3 SalesReturnsListView
- Grid: ReturnNo, ReturnDate, SalesInvoiceNo, CustomerName, TotalAmount, Status
- Filter: Date range, Customer, Status

### 7.4 SalesReturnEditorView (non-modal)
- Header: ReturnDate, SalesInvoice (searchable), Customer (read-only from invoice), Warehouse, Currency
- Lines: loaded from original invoice lines, user enters return quantity and optionally adjusts refund amount
- Validation: return quantity ≤ (original qty - previously returned qty)

### 7.5 MainWindow Integration
- Sidebar: المبيعات → فواتير البيع, مرتجع المبيعات
- Navigation: opens list views in content area or new windows
- Permissions: AllStaff for read, ManagerAndAbove for write (per AGENTS.md matrix)

---

## 8. Service Layer

### 8.1 ISalesInvoiceService / SalesInvoiceService
| Method | Returns | Notes |
|--------|---------|-------|
| GetByIdAsync(id) | Result<SalesInvoiceDto> | With lines and customer info |
| GetAllAsync(filter) | Result<List<SalesInvoiceDto>> | Paginated, filtered |
| CreateAsync(request) | Result<SalesInvoiceDto> | Creates as Draft |
| UpdateAsync(id, request) | Result<SalesInvoiceDto> | Only if Draft |
| PostAsync(id) | Result<SalesInvoiceDto> | The main posting flow |
| CancelAsync(id) | Result<SalesInvoiceDto> | Reversal flow |
| GetByInvoiceNoAsync(invoiceNo) | Result<SalesInvoiceDto> | For quick lookup |
| GetForReturnAsync(invoiceId) | Result<SalesInvoiceForReturnDto> | Loads invoice lines for return creation |

### 8.2 ISalesReturnService / SalesReturnService
| Method | Returns | Notes |
|--------|---------|-------|
| GetByIdAsync(id) | Result<SalesReturnDto> | |
| GetAllAsync(filter) | Result<List<SalesReturnDto>> | |
| CreateAsync(request) | Result<SalesReturnDto> | Creates as Draft |
| PostAsync(id) | Result<SalesReturnDto> | Creates inventory add-back + reversal journal |
| CancelAsync(id) | Result<SalesReturnDto> | Reverses the return |

### 8.3 Inventory Integration
- SalesInvoiceService calls IInventoryService.DeductStockAsync() during Post
- SalesInvoiceService calls IInventoryService.AddStockAsync() during Cancel
- SalesReturnService calls IInventoryService.AddStockAsync() during Post
- All stock operations use FIFO batch allocation via InventoryBatchService

### 8.4 Accounting Integration
- SalesInvoiceService calls IAccountingIntegrationService via AccountingIntegrationService:
  - CreateSalesPostEntryAsync(invoice, systemAccounts) — Revenue + COGS
  - ReverseSalesPostEntryAsync(invoice, systemAccounts) — Cancel
- SalesReturnService calls:
  - CreateSalesReturnEntryAsync(salesReturn, systemAccounts) — Refund reversal
  - ReverseSalesReturnEntryAsync(salesReturn, systemAccounts) — Cancel

### 8.5 DocumentSequenceService
- `GetNextIntAsync("SalesInvoice")` generates InvoiceNo
- `GetNextIntAsync("SalesReturn")` generates ReturnNo
- Thread-safe via SemaphoreSlim, unique per table

---

## 9. API Endpoints

| Method | Route | Auth | Notes |
|--------|-------|------|-------|
| GET | /api/v1/sales-invoices | AllStaff | List with filters |
| GET | /api/v1/sales-invoices/{id} | AllStaff | Full detail |
| POST | /api/v1/sales-invoices | ManagerAndAbove | Create Draft |
| PUT | /api/v1/sales-invoices/{id} | ManagerAndAbove | Update Draft |
| POST | /api/v1/sales-invoices/{id}/post | ManagerAndAbove | Post → stock + accounting |
| POST | /api/v1/sales-invoices/{id}/cancel | ManagerAndAbove | Cancel → reversal |
| GET | /api/v1/sales-invoices/by-number/{invoiceNo} | AllStaff | InvoiceNo lookup |
| GET | /api/v1/sales-invoices/{id}/for-return | AllStaff | Load invoice lines for return |
| GET | /api/v1/sales-returns | AllStaff | List |
| GET | /api/v1/sales-returns/{id} | AllStaff | Detail |
| POST | /api/v1/sales-returns | ManagerAndAbove | Create Draft |
| POST | /api/v1/sales-returns/{id}/post | ManagerAndAbove | Post return |
| POST | /api/v1/sales-returns/{id}/cancel | ManagerAndAbove | Cancel return |

### Request/Response DTOs

**CreateSalesInvoiceRequest:**
- CustomerId (int, required), WarehouseId (smallint, required), CurrencyId (smallint, required)
- PaymentType (byte, 1-3), CashBoxId (int?, required if Cash/Mixed)
- TaxId (smallint?), InvoiceDate (DateTime), InvoiceNo (int? — null = auto-generate)
- DiscountAmount (decimal), OtherCharges (decimal), Notes (string?)
- Lines: List<SalesInvoiceLineRequest> — ProductId, ProductUnitId, Quantity, UnitPrice

**SalesInvoiceDto:**
- All header fields + CustomerName, WarehouseName, CurrencyCode, CashBoxName, TaxName, StatusDisplay
- Lines with ProductName, UnitName, LineTotal
- Computed: SubTotal, TaxAmount, NetTotal, PaidAmount, RemainingAmount

**CreateSalesReturnRequest:**
- SalesInvoiceId (int, required), WarehouseId (smallint, required), CurrencyId (smallint, required)
- ReturnDate (DateTime), Notes (string?)
- Lines: List<SalesReturnLineRequest> — SalesInvoiceLineId, Quantity, Amount

---

## 10. Tasks

### Task 1 — Domain Entities
Create 4 entities: SalesInvoice, SalesInvoiceLine, SalesReturn, SalesReturnLine.
- SalesInvoice: DocumentEntity base (int Id + Status + audit fields), InvoiceNo unique
- SalesInvoiceLine: Entity base (no Status, no IsActive)
- SalesReturn: DocumentEntity base
- SalesReturnLine: Entity base
- Domain methods: Create(), Post(), Cancel(), UpdateHeader(), AddLine(), RemoveLine()
- Guard clauses: InvoiceNo > 0, quantities > 0, UnitPrice ≥ 0, PaidAmount ≤ NetTotal

### Task 2 — EF Core Configurations
- SalesInvoiceConfiguration: InvoiceNo unique index, all FK Restrict, precision for decimals
- SalesInvoiceLineConfiguration: FK to SalesInvoice + Product + ProductUnit, LineTotal computed
- SalesReturnConfiguration: ReturnNo unique index
- SalesReturnLineConfiguration: FK to SalesReturn + SalesInvoiceLine
- All FK DeleteBehavior.Restrict

### Task 3 — Migration
- Add-Migration + Update-Database for 4 new tables

### Task 4 — Database CHECK Constraints
- SalesInvoices: CHK_PaidAmount_NotNegative (PaidAmount >= 0), CHK_PaidAmount_NotExceedNet (PaidAmount <= NetTotal)
- SalesInvoices: CHK_PaymentType_Range (PaymentType 1-3)
- SalesInvoiceLines: CHK_Quantity_Positive (Quantity > 0), CHK_UnitPrice_NotNegative (UnitPrice >= 0)

### Task 5 — DTOs and Mappings
- SalesInvoiceDto, SalesInvoiceLineDto, SalesInvoiceForReturnDto
- SalesReturnDto, SalesReturnLineDto
- CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest, CreateSalesReturnRequest
- AutoMapper or manual mapping profiles

### Task 6 — FluentValidators
- CreateSalesInvoiceRequestValidator
- UpdateSalesInvoiceRequestValidator
- PostInvoiceRequestValidator
- CreateSalesReturnRequestValidator
- All validators: Arabic error messages, max lengths, value ranges

### Task 7 — ISalesInvoiceService + Implementation
- Full CRUD + Post + Cancel + GetForReturn
- Post flow: validate → ExecuteTransactionAsync → stock deduction → journal entries
- Cancel flow: validate Posted → ExecuteTransactionAsync → stock reversal → journal reversal

### Task 8 — ISalesReturnService + Implementation
- Create (as Draft, validate qty ≤ original - previously returned)
- Post flow: inventory add-back + reversal journal entry + auto-refund
- Cancel flow: reverse the return

### Task 9 — SalesInvoicesController
- 7 endpoints with proper 404 vs 400 differentiation
- [Authorize] with appropriate policies (AllStaff/ManagerAndAbove)

### Task 10 — SalesReturnsController
- 5 endpoints

### Task 11 — Desktop SalesInvoicesListViewModel + View
- DataGrid with filters, actions, newest-first sorting
- IDisposable with EventBus subscription
- Search by InvoiceNo or CustomerName

### Task 12 — Desktop SalesInvoiceEditorViewModel + View
- Non-modal via ScreenWindowService
- Barcode scan field at top for continuous scanning
- Lines DataGrid: Product/Unit selection, Qty, UnitPrice, LineTotal
- Summary section with computed totals
- Validation via INotifyDataErrorInfo + ValidateAllAsync() on save
- Save & Post action

### Task 13 — Desktop SalesReturnsListViewModel + View
- DataGrid with ReturnNo, InvoiceNo, Customer, TotalAmount, Status

### Task 14 — Desktop SalesReturnEditorViewModel + View
- Non-modal via ScreenWindowService
- Load original invoice lines, enter return qty/amount
- Validation: qty ≤ (original - previously returned)

### Task 15 — Integration Tests
- Test posting flow: creates InventoryTransaction + lines
- Test cancellation: creates reversal + stock add-back
- Test credit limit warning
- Test sales return qty validation
- Test multi-currency invoice calculations

### Task 16 — MainWindow Integration
- Sidebar: فواتير البيع, مرتجع المبيعات
- Navigation commands
- Permissions check via AllStaff/ManagerAndAbove

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Missing ProductUnit mapping at invoice line | Data integrity | FK constraint + FluentValidation ensures unit belongs to product |
| Stock deduction without InventoryTransaction | Audit gap | Mandatory: InventoryTransaction created in same transaction as Post |
| FIFO batch allocation complex for returns | Wrong cost | Link SalesReturnLine to SalesInvoiceLineId — debit same batch |
| Barcode scanning performance with many products | Slow lookup | Index on UnitBarcode.Barcode, cache lookup in memory |
| PaidAmount > NetTotal | Invalid state | DB CHECK constraint + Domain guard |
| Concurrent InvoiceNo generation | Duplicates | DocumentSequenceService with SemaphoreSlim |
| CashBox/Currency mismatch | Accounting error | Validate CashBox.CurrencyId matches invoice CurrencyId before Post |
| Customer not found or inactive | Runtime error | Pre-validate Customer exists and is active before transaction |

---

## 12. Dependencies

| Dependency | Phase | Notes |
|------------|-------|-------|
| Products + ProductUnits | Phase 25 | Required for invoice lines |
| Warehouses + WarehouseStocks | Phase 26 | Required for stock location |
| InventoryBatches + FIFO service | Phase 25 | Required for cost allocation |
| Customers | Phase 23 | Required as invoice party |
| Currencies + ExchangeRates | Phase 20 | Required for multi-currency |
| CashBoxes | Phase 29 (partially seeded in 19) | Required for Cash/Mixed payments |
| Taxes | Phase 19 | Required for tax calculation |
| DocumentSequences | Phase 19 | Required for InvoiceNo generation |
| InventoryTransactions | Phase 26 | Required for stock recording |
| AccountingIntegrationService | Phase 24 | Required for auto journal entries |
| SystemAccountMappings | Phase 22 | Required for mapped account IDs |
| CustomerReceipts | Phase 29 | Required for auto-payment on Cash invoices |

---

## 13. Key Design Decisions

1. **NO SalesQuotations in V1**: Quotations are deferred to future version. Phase 28 focuses only on posted invoices and returns.

2. **NO per-line discount**: Discount is applied at invoice header level only. Simplifies line structure and maintains consistency with Purchases module.

3. **LineTotal = Qty × UnitPrice**: Simplified formula. No discount subtraction at line level. The unit price is the effective price after any per-unit discount.

4. **Cost NOT stored on invoice lines**: Cost is sourced from InventoryBatches at posting time via FIFO. This keeps the invoice clean and the cost accurate (reflecting actual batch cost at time of sale).

5. **SalesReturnLine links to SalesInvoiceLineId**: Enables precise FIFO batch return. The return debits the same batch that was consumed at sale, maintaining inventory cost integrity.

6. **Auto-receipt for Cash/Mixed invoices**: When PaidAmount > 0, a CustomerReceipt is auto-created during Post, eliminating manual receipt entry.

7. **Continuous barcode scanning**: Uses keyboard wedge scanner input. No modal dialog or manual focus required between scans. Auto-adds lines as products are scanned.

8. **PaymentType affects accounting**: Cash invoices post to CashBox.AccountId (Dr), Credit invoices post to Customer.AccountId (Dr). This distinction is captured in the debit side — the revenue account is always the same (1520 — إيرادات المبيعات).
