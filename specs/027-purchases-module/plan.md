# Phase 27 — Purchases Module Implementation Plan

## Summary

Implement the complete Purchases module covering 6 database tables across 4 sub-modules: Purchase Invoices, Purchase Returns, Supplier Payments, and Supplier Payment Applications. The module integrates with Inventory (batch creation at posting), Accounting (auto journal entries via AccountingIntegrationService), and Supplier management (balance updates). Multi-currency support, landed cost through OtherCharges, and a Draft→Posted→Cancelled lifecycle apply to all document entities.

---

## 1. Key Entities

### 1.1 PurchaseInvoices (DocumentEntity)
- `InvoiceNo` (int, UNIQUE per table) — user-facing number, thread-safe generation via DocumentSequenceService.GetNextIntAsync("PurchaseInvoice"). Null in request DTO means auto-generate.
- `InvoiceDate` (date) — document date, not necessarily today
- `SupplierId` (int FK → Suppliers) — required
- `WarehouseId` (smallint FK → Warehouses) — destination warehouse for received goods
- `CurrencyId` (smallint FK → Currencies) — multi-currency support; exchange rate for base currency conversion stored on line items
- `PaymentType` (tinyint: 1=Cash, 2=Credit, 3=Mixed)
- `CashBoxId` (int? FK → CashBoxes) — required when PaymentType is Cash or Mixed (partially paid via cash)
- `TaxId` (smallint? FK → Taxes) — optional tax rate reference
- `SubTotal`, `DiscountAmount`, `TaxAmount`, `OtherCharges`, `NetTotal`, `PaidAmount`, `RemainingAmount` (all decimal(18,2))
- `Notes` (nvarchar(500)?)
- `AttachmentPath` (nvarchar(500)?) — optional single image/attachment
- `Status` (tinyint: 1=Draft, 2=Posted, 3=Cancelled)

### 1.2 PurchaseInvoiceLines (Entity)
- `PurchaseInvoiceId` (int FK → PurchaseInvoices)
- `ProductId` (int FK → Products)
- `ProductUnitId` (int FK → ProductUnits) — which unit of measure was used
- `Quantity` (decimal(18,3))
- `UnitPrice` (decimal(18,2)) — the unit cost in the invoice's currency
- `LineTotal` (decimal(18,2)) = Quantity × UnitPrice — computed in Domain
- `UnitCostBase` (decimal(18,2)) — UnitPrice converted to base currency using the exchange rate at posting time, stored for batch cost calculation
- NO DiscountAmount per line — header-level discount only

### 1.3 PurchaseReturns (DocumentEntity)
- `ReturnNo` (int, UNIQUE per table) — auto-generated via DocumentSequenceService
- `ReturnDate` (date)
- `PurchaseInvoiceId` (int FK → PurchaseInvoices) — the original invoice being returned
- `SupplierId` (int FK → Suppliers) — denormalized for fast query
- `WarehouseId` (smallint FK → Warehouses)
- `CurrencyId` (smallint FK → Currencies)
- `TotalAmount` (decimal(18,2))
- `Notes` (nvarchar(500)?)
- `Status` (tinyint: 1=Draft, 2=Posted, 3=Cancelled)

### 1.4 PurchaseReturnLines (Entity)
- `PurchaseReturnId` (int FK → PurchaseReturns)
- `PurchaseInvoiceLineId` (int FK → PurchaseInvoiceLines) — links to original line to identify source batch for FIFO
- `Quantity` (decimal(18,3))
- `Amount` (decimal(18,2)) — UnitPrice × Quantity from original line

### 1.5 SupplierPayments (DocumentEntity)
- `PaymentNo` (int, UNIQUE per table) — auto-generated via DocumentSequenceService
- `PaymentDate` (date)
- `SupplierId` (int FK → Suppliers)
- `CashBoxId` (int FK → CashBoxes)
- `CurrencyId` (smallint FK → Currencies)
- `Amount` (decimal(18,2))
- `Notes` (nvarchar(500)?)
- `Status` (tinyint: 1=Draft, 2=Posted, 3=Cancelled)

### 1.6 SupplierPaymentApplications (Entity)
- `SupplierPaymentId` (int FK → SupplierPayments)
- `PurchaseInvoiceId` (int FK → PurchaseInvoices)
- `AppliedAmount` (decimal(18,2))
- Optional junction — only created when user explicitly distributes payment across specific invoices. If omitted, the payment reduces the supplier's overall balance without invoice-level allocation.

---

## 2. Business Rules

### 2.1 Purchase Invoice Lifecycle
- **Draft (1)**: Can be edited, deleted, or posted. No effect on inventory, accounting, or supplier balance. Not visible in financial reports.
- **Posted (2)**: Terminal for editing. Triggers: InventoryBatch creation for each line, InventoryTransaction creation (Type=Purchase), WarehouseStock quantity increase, accounting entry (Dr Inventory + VAT Input, Cr Cash/AP), supplier CurrentBalance update. Cannot be deleted — only cancelled.
- **Cancelled (3)**: Terminal state. Triggers: reversal of all posting effects — decrease WarehouseStock, reverse the journal entry (Dr Cash/AP, Cr Inventory + VAT Input), decrease supplier CurrentBalance. Reversal entries reference the original via ReferenceType/ReferenceId.

### 2.2 PaidAmount Constraint
- `PaidAmount <= NetTotal` enforced at domain level with DomainException
- `RemainingAmount = NetTotal - PaidAmount` always computed, never stored as independent input
- Cash payment (PaymentType=Cash): PaidAmount automatically equals NetTotal
- Credit payment (PaymentType=Credit): PaidAmount defaults to 0
- Partial payment (PaymentType=Mixed): PaidAmount between 0 and NetTotal

### 2.3 Smart Line Input
- User can enter any two of (Quantity, UnitPrice, LineTotal) and the system computes the third
- Example: Enter Quantity=10 and UnitPrice=5 → LineTotal=50
- Example: Enter Quantity=10 and LineTotal=50 → UnitPrice=5
- This is a UI-layer convenience; the Domain always receives all three values

### 2.4 Multi-Currency
- Invoice currency is selected at header level via CurrencyId
- Each line receives UnitPrice in the invoice currency
- At posting time, `UnitCostBase` is computed: UnitPrice × ExchangeRate (rate from CurrencyRates table effective on InvoiceDate)
- Base currency UnitCostBase is stored on the line and used for InventoryBatch.UnitCost
- Exchange rate snapshot stored to handle future rate changes

### 2.5 OtherCharges as Landed Cost
- OtherCharges covers transport, customs, clearance, etc.
- At posting time, OtherCharges is distributed across all lines proportionally by LineTotal weight
- Each line's batch UnitCost = (LineTotal / SUM(LineTotal) × OtherCharges / Quantity) + UnitCostBase
- This ensures true landed cost in inventory valuation

### 2.6 InventoryBatch Creation on Posting
- Each PurchaseInvoiceLine creates ONE InventoryBatch
- BatchNo = auto-increment per product (can be PurchaseInvoiceLineId)
- QuantityReceived = line.Quantity
- QuantityRemaining = line.Quantity (full batch available until sales/returns consume it)
- UnitCost = computed landed cost from step 2.5
- WarehouseId from invoice header, ProductId/ProductUnitId from line
- PurchaseInvoiceId stored on batch for traceability

### 2.7 Purchase Return Constraints
- Return must reference a posted PurchaseInvoice
- Return lines link to PurchaseInvoiceLineId (not ProductId directly) — this identifies the exact batch
- Quantity returned cannot exceed (original line Quantity − previously returned Quantity) — enforced at service level by querying sum of existing returns
- When return is posted: InventoryBatch.QuantityRemaining decreases, WarehouseStock decreases, reversal journal entry credits Inventory and debits Cash/AP (mirroring original)
- If the original invoice was cash: return creates a refund CashTransaction (Type=RefundOut) via CashBox

### 2.8 Supplier Payment Constraints
- Payment Amount must be > 0
- When posted: creates accounting entry (Dr AccountsPayable/SupplierAccount, Cr CashBox), decreases supplier CurrentBalance
- If distributed via SupplierPaymentApplications: updates RemainingAmount on each linked PurchaseInvoice proportionally
- Cancellation reverses the payment entry and restores supplier balance

---

## 3. Financial Formulas

```
PurchaseInvoice:
  LineTotal       = Quantity × UnitPrice                       // Domain entity compute
  SubTotal        = SUM(LineTotal)                              // Domain entity compute
  NetTotal        = SubTotal − DiscountAmount + TaxAmount + OtherCharges
  RemainingAmount = NetTotal − PaidAmount
  Constraint:     PaidAmount <= NetTotal

Landed Cost Distribution (service-level):
  TotalLineWeight = SUM(LineTotal for all lines)
  EachLineShare   = (LineTotal / TotalLineWeight) × OtherCharges
  EachLineCost    = (LineTotal + EachLineShare) / Quantity      // actual unit cost including landed cost

Purchase Return:
  ReturnAmount    = SUM(return line Amount)

Journal Entry (Posted Purchase, Credit):
  Dr Inventory           (SUM of landed costs)
  Dr InputVAT            (TaxAmount, if applicable)
  Cr AccountsPayable     (NetTotal)

Journal Entry (Posted Purchase, Cash):
  Dr Inventory           (SUM of landed costs)
  Dr InputVAT            (TaxAmount, if applicable)
  Cr CashBox             (NetTotal)

Journal Entry (Cancellation — reverses original):
  Dr AccountsPayable / CashBox
  Cr Inventory
  Cr InputVAT
```

---

## 4. Integration Points

| Integration | What Happens | When |
|-------------|--------------|------|
| **Inventory** | Creates InventoryBatch per line, updates WarehouseStock.Quantity, creates InventoryTransaction + InventoryTransactionLines | On POST (not Draft) |
| **Accounting** | Creates balanced JournalEntry via AccountingIntegrationService with ReferenceType="PurchaseInvoice", ReferenceId=InvoiceId | On POST; reversal on CANCEL |
| **Supplier Balance** | Increases CurrentBalance by RemainingAmount (credit purchase); decreases on payment or cancel | On POST payment; on cancel |
| **CashBox** | Records CashTransaction with Type=SupplierPayment (payment) or RefundOut (return refund) | On POST payment/return |
| **DocumentSequence** | GetNextIntAsync for InvoiceNo, ReturnNo, PaymentNo | On first save (Draft creation) |
| **CurrencyRates** | Reads exchange rate effective on InvoiceDate for base currency conversion | On POST |
| **SupplierPaymentApplications** | Updates PurchaseInvoice.RemainingAmount when payment is invoice-allocated | On POST payment |

---

## 5. Implementation Tasks

### Task 1 — Domain Entities
- Create PurchaseInvoice entity (DocumentEntity base): factory Create() with guard clauses, Post() method, Cancel() method. Compute SubTotal/NetTotal/RemainingAmount in domain. Status transition validators.
- Create PurchaseInvoiceLine entity: Create(), UpdateLineTotal() compute on set.
- Create PurchaseReturn entity: Create(), Post(), Cancel().
- Create PurchaseReturnLine entity: Create(), validate quantity against original line's remaining.
- Create SupplierPayment entity: Create(), Post(), Cancel().
- Create SupplierPaymentApplication entity: Create().
- Add enums if new ones needed (all reuse existing InvoiceStatus, PaymentType).

### Task 2 — EF Core Configuration
- Fluent API config for all 6 tables: primary keys, FK relationships with DeleteBehavior.Restrict, decimal precision (18,2) for money and (18,3) for quantities, nvarchar max lengths, UNIQUE indexes on InvoiceNo/ReturnNo/PaymentNo, global query filters for soft delete on ActivatableEntity inheritors (none here — DocumentEntity uses Status, not IsActive).
- PurchaseInvoiceConfiguration: composite index on (SupplierId, InvoiceDate DESC) for supplier invoice history queries.

### Task 3 — DTOs & Requests
- PurchaseInvoiceDto, PurchaseInvoiceLineDto, PurchaseReturnDto, PurchaseReturnLineDto, SupplierPaymentDto, SupplierPaymentApplicationDto
- CreatePurchaseInvoiceRequest (with int? InvoiceNo for auto-gen), UpdatePurchaseInvoiceRequest (Draft only), CreatePurchaseReturnRequest, CreateSupplierPaymentRequest
- Validators for each request: FluentValidation with Arabic messages, Quantity > 0, UnitPrice >= 0, PaidAmount <= NetTotal, etc.

### Task 4 — Application Services
- IPurchaseInvoiceService + PurchaseInvoiceService: CreateAsync, UpdateAsync (Draft only), PostAsync, CancelAsync, GetByIdAsync, GetAllAsync (filtered by status/date/supplier)
- IPurchaseReturnService + PurchaseReturnService: CreateAsync, PostAsync, CancelAsync, GetByInvoiceIdAsync
- ISupplierPaymentService + SupplierPaymentService: CreateAsync, PostAsync, CancelAsync, GetBySupplierIdAsync
- All services return Result<T>, handle transactions via IUnitOfWork.ExecuteTransactionAsync

### Task 5 — Posting Pipeline (PostAsync)
- Validate stock availability (if negative stock not allowed in settings)
- Compute UnitCostBase using CurrencyRates exchange rate
- Distribute OtherCharges across lines for landed cost
- Create InventoryBatch per line
- Update WarehouseStock.Quantity (+line.Quantity)
- Create InventoryTransaction (Type=Purchase) with lines
- Call AccountingIntegrationService to create JournalEntry
- Update Supplier.CurrentBalance (+RemainingAmount)
- If CashBoxId specified: create CashTransaction (Type=SupplierPayment) for PaidAmount
- All within a single ExecuteTransactionAsync

### Task 6 — Cancellation Pipeline (CancelAsync)
- Verify current Status == Posted
- Reverse InventoryBatch.QuantityRemaining (reduce by line quantity unless already consumed)
- Reverse WarehouseStock.Quantity (−line.Quantity per line)
- Create InventoryTransaction (Type=Purchase, but reversal — or a separate cancellation transaction type)
- Call AccountingIntegrationService to create reversal JournalEntry
- Reverse Supplier.CurrentBalance (−original RemainingAmount)
- Reverse CashTransaction if applicable
- All in one ExecuteTransactionAsync

### Task 7 — API Controllers
- PurchaseInvoicesController: CRUD + Post + Cancel endpoints, [Authorize] with policy checks (ManagerAndAbove for write, AllStaff for read)
- PurchaseReturnsController: Create, Post, Cancel, GetByInvoice
- SupplierPaymentsController: Create, Post, Cancel, GetBySupplier
- All controllers delegate to services, translate Result<T> to HTTP responses (200/201/400/404)

### Task 8 — Desktop UI (ViewModels + Views)
- PurchaseInvoicesListViewModel: sorted by InvoiceDate DESC, filter by supplier/status/date range, EventBus subscription, IDisposable
- PurchaseInvoiceEditorViewModel: INotifyDataErrorInfo, ValidateAllAsync on save, smart line input (compute 3rd field), non-modal via ScreenWindowService
- PurchaseReturnsListViewModel + PurchaseReturnEditorViewModel
- SupplierPaymentsListViewModel + SupplierPaymentEditorViewModel
- SupplierPaymentApplicationsDialog (optional, shown when user clicks "توزيع الدفعة")

### Task 9 — Desktop API Services
- IPurchaseInvoiceApiService, IPurchaseReturnApiService, ISupplierPaymentApiService
- Typed HttpClient implementations mapping to API endpoints
- Error handling via HandleResponseAsync with content-type guard

---

## 6. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **FIFO batch consumption conflict** — return trying to reduce a batch already fully consumed by sales | Data inconsistency | Track batch's TotalConsumedQuantity; prevent return greater than (QuantityReceived − TotalConsumedQuantity − already_returned) |
| **OtherCharges distribution precision** — rounding errors across lines | Pennies of variance | Apply rounding to the largest line to absorb remainder; document policy explicitly |
| **Exchange rate change between draft and posting** — user creates draft with one rate, posts days later with different rate | Cost variance | Snapshot exchange rate on first save (Draft) and use that rate at posting; allow manual override |
| **Cancellation fails due to consumed batches** — stock already sold can't be physically returned from supplier | Partial cancellation complexity | For V1: block cancellation if any batch has QuantityRemaining < original Quantity. Require a purchase return (which can handle partial quantities) instead. Full reversal cancellation deferred to V2. |
| **SupplierPaymentApplications complexity** — users may find invoice allocation confusing | Low adoption | Keep optional; if omitted, payment reduces overall supplier balance. The application feature is additive, not blocking. |
| **Attachment storage** — large images bloating database or file system | Storage overhead | Store file path (not binary) in AttachmentPath; configure a dedicated uploads directory outside the app. Enforce max file size at API level (e.g., 5MB). |

---

## 7. Key Design Decisions (Rationale)

1. **NO PurchaseOrders table**: PO workflow (approval → partial receipt) is a V2 feature. V1 focuses on direct purchase invoice entry. The system allows saving as Draft if the user needs to prepare an order-like document, but there is no dedicated PO entity, status workflow, or partial receipt tracking.

2. **NO AdditionalFees entity**: Instead, OtherCharges on the invoice header captures transport, customs, clearance, etc. This avoids a separate allocation engine for V1. The landed cost distribution logic (proportional by line weight) runs at posting time and adjusts each batch's UnitCost. In V2, a separate AdditionalCharge entity with AccountId FK could offer per-charge type tracking.

3. **NO per-line discount**: Discount is applied at header level only (DiscountAmount). This simplifies the data model and avoids complex discount allocation across lines. Per-line discount can be added in V2 if needed.

4. **PurchaseReturnLines link to PurchaseInvoiceLineId**: This preserves the link to the original purchase line and its associated InventoryBatch. When a return is posted, the system can directly reduce the correct batch's QuantityRemaining without searching for the right batch. This is essential for FIFO integrity.

5. **SupplierPaymentApplications optional**: Many small shops pay suppliers without allocating to specific invoices (they just track total balance). Invoice-level allocation is an optional convenience for shops that need detailed aging or per-invoice settlement tracking.

6. **Draft→Posted→Cancelled lifecycle**: Consistent with all document entities in the system (Sales, Inventory, Accounting). Drafts are editable and deletable with no side effects. Posting triggers ALL side effects atomically. Cancellation reverses ALL side effects but is blocked if inventory batches have been partially consumed (user must use purchase return instead).

7. **Exchange rate snapshot on posting**: The conversion to base currency (UnitCostBase) happens at posting time, not at draft creation. This prevents exchange rate fluctuations between draft and posting from affecting inventory cost. However, the rate effective on InvoiceDate is used, so if the draft is posted the same day, the rate is stable.
