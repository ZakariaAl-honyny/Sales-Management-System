# Phase 29 — Receipts & Payments Module (المقبوضات والمدفوعات)

**Version**: 1.0  
**Status**: ✅ Reviewed & Fixed (v2.4 cross-phase gap analysis)  
**Dependencies**: Phase 22 (Chart of Accounts), Phase 23 (Customers), Phase 24 (Accounting Automation), Phase 32 (Suppliers)  
**Target**: .NET 10 Clean Architecture — SalesSystem.Contracts/Domain/Application/Infrastructure/Api/DesktopPWF

---

## 1. Summary

This Phase implements the **Receipts & Payments** module — the operational layer for all cash inflow and outflow. It covers four main areas:

1. **CashBoxes** (الصناديق) — lightweight operational registers linked to GL Accounts
2. **Banks** (البنوك) — bank account records linked to GL Accounts
3. **CustomerReceipts** (سندات قبض) — money received from customers, with optional invoice distribution
4. **SupplierPayments** (سندات صرف) — money paid to suppliers, with optional invoice distribution

**Key philosophy**: Every receipt/payment creates an automatic journal entry via `AccountingIntegrationService`. The receipt/payment itself is the operational record; the journal entry is the accounting record. Source of truth for balances is the **GL Account** (linked via `CashBox.AccountId` / `Bank.AccountId`), not the cash register.

---

## 2. Key Entities

### 2.1 CashBoxes
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `AccountId` | `int` FK → Accounts | **Mandatory** — balance lives on Account |
| `BranchId` | `smallint` FK → Branches | Branch association |
| `Name` | `nvarchar(150)` | Required |
| `Description` | `nvarchar(300)` | Optional |
| `IsActive` | `bit` | Soft delete |
| `CreatedByUserId` | `int?` FK → Users | |
| `UpdatedByUserId` | `int?` FK → Users | |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2?` | |

**Notes**:
- NO `OpeningBalance`, NO `CurrentBalance`, NO `Deposit()`, NO `Withdraw()` methods
- Balance is tracked exclusively on the linked `Account` entity via journal entries
- Auto-account creation: if `AccountId` is not provided, service creates a Level-4 sub-account under parent `"1110 — النقدية"` with auto-incremented code (1111, 1112, ...)
- `CategoryId`, `PhoneNumber`, `TaxNumber`, `Address` metadata fields are **not in V1** — deferred

### 2.2 Banks
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `AccountId` | `int` FK → Accounts | **Mandatory** — balance lives on Account |
| `Name` | `nvarchar(150)` | Required |
| `AccountNumber` | `nvarchar(100)` | Optional |
| `IBAN` | `nvarchar(100)` | Optional |
| `IsActive` | `bit` | Soft delete |
| `CreatedByUserId` | `int?` FK → Users | |
| `UpdatedByUserId` | `int?` FK → Users | |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2?` | |

**Notes**:
- Auto-account creation under parent `"1120 — البنوك"` (Bank Accounts, Level 3 under Current Assets)
- Banks are read-only operational references; all cash movement goes through CashBoxes
- Bank transfers (cheques, wire transfers) are **deferred to V2** — V1: cash transactions only

### 2.3 CustomerReceipts
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `ReceiptNo` | `int` | **UNIQUE** — user-facing number |
| `ReceiptDate` | `date` | Document date |
| `CustomerId` | `int` FK → Customers | Receiving customer |
| `CashBoxId` | `int` FK → CashBoxes | Which cash register |
| `CurrencyId` | `smallint` FK → Currencies | Transaction currency |
| `Amount` | `decimal(18,2)` | Total receipt amount |
| `Notes` | `nvarchar(500)` | Optional |
| `Status` | `tinyint` | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | `int?` FK → Users | Extracted from JWT, never client-supplied |
| `UpdatedByUserId` | `int?` FK → Users | |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2?` | |

**Notes**:
- `ReceiptNo` auto-generated via `IDocumentSequenceService.GetNextIntAsync("CustomerReceipt", ct)` — thread-safe with `SemaphoreSlim`
- UNIQUE index on `ReceiptNo`
- Status lifecycle: Draft → Posted → Cancelled (terminal)
- Posted receipts are **immutable** — no edits. Changes require cancellation + new receipt.

### 2.4 CustomerReceiptApplications
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `CustomerReceiptId` | `int` FK → CustomerReceipts | |
| `SalesInvoiceId` | `int` FK → SalesInvoices | Target invoice |
| `AppliedAmount` | `decimal(18,2)` | Amount applied to this invoice |

**Notes**:
- **Optional** — only created when user explicitly distributes the receipt to specific invoices
- If no applications exist → receipt is recorded as a lump-sum payment (applied to oldest outstanding invoices by default)
- Multiple applications per receipt allowed (splitting across invoices)
- `SUM(AppliedAmount)` MUST NOT exceed `CustomerReceipts.Amount`
- No cascade delete — applications remain even if source receipt is cancelled (visibility for audit)

### 2.5 SupplierPayments
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `PaymentNo` | `int` | **UNIQUE** — user-facing number |
| `PaymentDate` | `date` | Document date |
| `SupplierId` | `int` FK → Suppliers | Paying supplier |
| `CashBoxId` | `int` FK → CashBoxes | Which cash register |
| `CurrencyId` | `smallint` FK → Currencies | Transaction currency |
| `Amount` | `decimal(18,2)` | Total payment amount |
| `Notes` | `nvarchar(500)` | Optional |
| `Status` | `tinyint` | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | `int?` FK → Users | Extracted from JWT |
| `UpdatedByUserId` | `int?` FK → Users | |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2?` | |

**Notes**:
- `PaymentNo` auto-generated via `IDocumentSequenceService.GetNextIntAsync("SupplierPayment", ct)`
- Same Status lifecycle as CustomerReceipts

### 2.6 SupplierPaymentApplications
| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` PK | |
| `SupplierPaymentId` | `int` FK → SupplierPayments | |
| `PurchaseInvoiceId` | `int` FK → PurchaseInvoices | Target invoice |
| `AppliedAmount` | `decimal(18,2)` | Amount applied to this invoice |

**Notes**:
- Same optional-applications pattern as CustomerReceiptApplications
- `SUM(AppliedAmount)` MUST NOT exceed `SupplierPayments.Amount`

---

## 3. Business Rules

### RULE-001: CashBox Balance Is Always on GL Account
The `CashBox` entity stores NO balance fields. The current balance is computed as the running sum of all journal entry lines debiting/crediting the linked `Account`. For fast UI display, a `GetCurrentBalanceAsync(cashBoxId)` service method computes it on demand. Future optimization: cache balance on `Account.OpeningBalance` computed field.

### RULE-002: Receipt/Payment Numbering
- `ReceiptNo` and `PaymentNo` are `int`, **UNIQUE** per table
- Generated via `IDocumentSequenceService` with `SemaphoreSlim` lock (thread-safe)
- Request DTOs use `int? ReceiptNo` — null means auto-generate
- User override is allowed but validated for uniqueness

### RULE-003: Three-State Lifecycle
```
Draft (1) → Posted (2) → Cancelled (3)
```
- **Draft → Posted**: Creates journal entry + updates Customer/Supplier balance
- **Draft → Cancelled**: No financial impact (no entry was created)
- **Posted → Cancelled**: Creates **reversal journal entry** (Debit↔Credit swapped)

### RULE-004: Immutability After Posting
Once a receipt or payment is posted:
- NO edits allowed
- NO partial reversals
- Only full cancellation via `Status = Cancelled` with reversal journal entry
- This aligns with the invoice lifecycle (RULE-019 in AGENTS.md)

### RULE-005: Journal Entry on Posting
On **post** (not save), `AccountingIntegrationService` creates:

**Customer Receipt:**
```
Dr  CashBox.AccountId         (Amount)
Cr  Customer.AccountId        (Amount)
```

**Supplier Payment:**
```
Dr  Supplier.AccountId        (Amount)
Cr  CashBox.AccountId         (Amount)
```

### RULE-006: Journal Entry on Cancellation
If the receipt/payment was **Posted** and is being **Cancelled**:
1. Create a reversal journal entry mirroring the original with Dr↔Cr swapped
2. Set `IsReversed = true` on the original journal entry
3. Link via `ReversedByEntryId` FK

### RULE-007: Customer/Supplier Balance Update
- Posting a receipt: `Customer.DecreaseBalance(Amount)` (customer owes us less)
- Cancelling a posted receipt: `Customer.IncreaseBalance(Amount)` (reversal)
- Posting a payment: `Supplier.DecreaseBalance(Amount)` (we owe supplier less)
- Cancelling a posted payment: `Supplier.IncreaseBalance(Amount)` (reversal)

### RULE-008: Optional Invoice Applications
- Receipt/Payment can be "unapplied" (lump-sum) — applications table stays empty
- When applications exist, `SUM(AppliedAmount)` must not exceed receipt/payment `Amount`
- Applications are recorded at posting time, not at draft time
- When cancelling a posted receipt/payment, applications are **retained** (for audit visibility) but their financial impact is reversed via the journal entry

### RULE-009: Currency Handling
- Each receipt/payment stores `CurrencyId` (the transaction currency)
- Journal entries are created in **base currency** (using exchange rate from `CurrencyRates`)
- `Amount` on receipt/payment is in transaction currency
- No multi-currency receipt/payment splitting in V1 (single currency per transaction)

### RULE-010: Amount Validation
- `Amount > 0` — no zero or negative receipts/payments
- `Amount <= Customer.CurrentBalance` for receipts (cannot receive more than outstanding) — **soft warning**, not blocking
- `Amount <= Supplier.CurrentBalance` for payments — **soft warning**, not blocking
- `Amount` uses `decimal(18,2)` precision

### RULE-011: No Negative CashBox Balance
The `CashBox.Account` balance (sum of all journal entries) MUST NOT go negative. The `AccountService` validates this before allowing any posting that would create a debit entry on a cash account.

### RULE-012: All FK Restrict
Every foreign key in this module uses `DeleteBehavior.Restrict` — no cascade delete on any relationship.

---

## 4. Design Decisions

### 4.1 NO CashTransactions Table
The schema intentionally removes the `CashTransactions` table. Instead, **CustomerReceipts** and **SupplierPayments** serve as the operational record of all cash movement. Why:
- Every cash movement is either money in (receipt) or money out (payment)
- No need for a generic "CashTransaction" that duplicates what receipts/payments already capture
- The linked journal entries handle the accounting side
- Simplifies the data model — fewer tables, clearer semantics

### 4.2 NO DailyClosures Table (Deferred to V2)
Daily closure (جرد الصندوق) is deferred to V2. In V1:
- Operators can view cash box balance via journal entry queries
- Actual cash counting and variance reporting are manual
- V2 will add: `DailyClosure` entity with `ActualCashCount`, `ExpectedBalance`, `Difference`, `DifferenceReconciled` flag

### 4.3 NO Cheques Table (Deferred to V2)
Cheque management is deferred to V2. In V1:
- All payments are **cash-only** (CashBox-based)
- V2 will add: `Cheque` entity with `ChequeNumber`, `BankName`, `MaturityDate`, `Status` (Pending/Cleared/Bounced/Cancelled)

### 4.4 CashBox Is Lightweight Operational Entity
Per the analysis (Analysis Part 3, lines 39-493):
- CashBox is **not** a replacement for the GL Account
- It is an **operational wrapper** for the user (cashier, branch, currency context)
- Benefits: easy UI selection ("الصندوق الرئيسي"), cashier assignment, multi-currency support, branch isolation
- The GL Account is the source of truth for balance
- This design allows future expansion (permissions on CashBox, DailyClosure, cashier tracking)

### 4.5 Optional Applications Pattern
Many small retail shops record receipts/payments as lump sums (especially cash sales). Forcing invoice-level distribution on every transaction would be burdensome. The design:
- **Default**: No applications — receipt is recorded against the customer's overall balance
- **Optional**: User can distribute to specific invoices when they need to track which invoices are settled
- Applications are created at **post time**, not draft time, to avoid stale application data if the user changes the amount before posting

### 4.6 Receipt/Payment vs Invoice Payment
This phase does NOT implement automatic payment on cash invoice creation. That remains a separate flow:
- Cash invoice: creates the invoice with `PaidAmount = NetTotal` (no separate receipt needed)
- Credit invoice: creates the invoice with `PaidAmount = 0` (receipt follows later)
- This phase handles the **separate receipt/payment** scenario only

### 4.7 Integration with AccountingIntegrationService
Every posting/cancellation calls `AccountingIntegrationService` which:
1. Creates `JournalEntry` with `EntryType = Receipt (4)` or `Payment (5)`
2. Creates balanced `JournalEntryLines` (Dr + Cr)
3. Returns `Result<int>` (the journal entry ID) — never throws
4. The caller is responsible for wrapping in `ExecuteTransactionAsync`

### 4.8 User ID Extraction
- `CreatedBy` is extracted from JWT claims in the Controller, **never** accepted from client requests
- This prevents user ID spoofing (follows RULE-382 from Phase 24)

---

## 5. Integration Points

| Integration | Direction | Details |
|-------------|-----------|---------|
| **Chart of Accounts** (Phase 22) | CashBox → Account | `CashBox.AccountId` links to GL Account |
| **Chart of Accounts** (Phase 22) | Bank → Account | `Bank.AccountId` links to GL Account |
| **Customers** (Phase 23) | Receipt → Customer | `CustomerReceipts.CustomerId` FK |
| **Suppliers** (Phase 32) | Payment → Supplier | `SupplierPayments.SupplierId` FK |
| **SalesInvoices** (Phase 28) | Application → Invoice | `CustomerReceiptApplications.SalesInvoiceId` FK |
| **PurchaseInvoices** (Phase 27) | Application → Invoice | `SupplierPaymentApplications.PurchaseInvoiceId` FK |
| **Accounting Automation** (Phase 24) | Post/Cancel → Journal Entry | `AccountingIntegrationService` for Dr/Cr entries |
| **Branches** | CashBox → Branch | `CashBox.BranchId` FK |
| **Currencies** (Phase 20) | Receipt/Payment → Currency | `CurrencyId` FK with exchange rate lookup |
| **DocumentSequences** | Numbering | `IDocumentSequenceService` for `ReceiptNo`/`PaymentNo` |
| **Desktop** | UI | CRUD lists, editors, posting dialog, cancellation dialog |

---

## 6. Implementation Tasks

### Task 1: Domain Entities
- Create `CashBox` entity (ActivatableEntity) — no balance fields, `AccountId` FK
- Create `Bank` entity (ActivatableEntity) — `AccountId` FK
- Create `CustomerReceipt` entity (DocumentEntity) — `ReceiptNo` int unique
- Create `CustomerReceiptApplication` entity (Entity) — no audit fields
- Create `SupplierPayment` entity (DocumentEntity) — `PaymentNo` int unique
- Create `SupplierPaymentApplication` entity (Entity) — no audit fields
- Add Guard Clauses for all entity creation (`DomainException` with Arabic messages)
- Domain methods: `Post()`, `Cancel()`, `AddApplication()`, `RemoveApplication()`

### Task 2: EF Core Configurations
- `CashBoxConfiguration`: `AccountId` FK Restrict, `HasQueryFilter(x => x.IsActive)`
- `BankConfiguration`: `AccountId` FK Restrict, `HasQueryFilter(x => x.IsActive)`
- `CustomerReceiptConfiguration`: `ReceiptNo` unique index, `Status` conversion, all FK Restrict
- `CustomerReceiptApplicationConfiguration`: Composite unique index `(CustomerReceiptId, SalesInvoiceId)`, FK Restrict
- `SupplierPaymentConfiguration`: `PaymentNo` unique index, `Status` conversion, all FK Restrict
- `SupplierPaymentApplicationConfiguration`: Composite unique index `(SupplierPaymentId, PurchaseInvoiceId)`, FK Restrict
- Add FK mappings on `SalesInvoice` and `PurchaseInvoice` for the application navigation properties

### Task 3: Database Migration
- Generate EF Core migration for all 6 new tables
- Verify: all FK `DeleteBehavior.Restrict`, all decimal `HasPrecision(18,2)`, all unique indexes, CHECK constraints

### Task 4: DTOs & Requests
- `CashBoxDto`, `CreateCashBoxRequest`, `UpdateCashBoxRequest`
- `BankDto`, `CreateBankRequest`, `UpdateBankRequest`
- `CustomerReceiptDto`, `CreateCustomerReceiptRequest`, `UpdateCustomerReceiptRequest`
- `CustomerReceiptApplicationDto`, `CreateCustomerReceiptApplicationRequest`
- `SupplierPaymentDto`, `CreateSupplierPaymentRequest`, `UpdateSupplierPaymentRequest`
- `SupplierPaymentApplicationDto`, `CreateSupplierPaymentApplicationRequest`
- `ReceiptLookupDto`, `PaymentLookupDto` (for dropdown/combo selection)
- All DTOs include `AccountId` + `AccountName` for display

### Task 5: FluentValidators
- `CreateCustomerReceiptRequestValidator`: `Amount > 0`, `ReceiptDate` not future, `CustomerId` required, `CashBoxId` required, `CurrencyId` required
- `CreateCustomerReceiptApplicationRequestValidator`: `AppliedAmount > 0`, `AppliedAmount <= RemainingAmount`
- `CreateSupplierPaymentRequestValidator`: same pattern as receipt
- `CreateSupplierPaymentApplicationRequestValidator`: same pattern
- `CreateCashBoxRequestValidator`: `Name` not empty, `BranchId` required
- `CreateBankRequestValidator`: `Name` not empty, `AccountNumber` maxlength

### Task 6: Application Services
- `ICashBoxService` / `CashBoxService`: CRUD + `GetCurrentBalanceAsync(cashBoxId)`
- `IBankService` / `BankService`: CRUD
- `ICustomerReceiptService` / `CustomerReceiptService`: CRUD + `PostAsync` + `CancelAsync`
- `ISupplierPaymentService` / `SupplierPaymentService`: CRUD + `PostAsync` + `CancelAsync`
- All service methods return `Result<T>` — never throw
- `PostAsync`: validates → calls `AccountingIntegrationService.CreateReceiptEntryAsync()` or `CreatePaymentEntryAsync()` → updates customer/supplier balance → commits via `ExecuteTransactionAsync`
- `CancelAsync`: validates → calls `AccountingIntegrationService.ReverseReceiptEntryAsync()` or `ReversePaymentEntryAsync()` → reverses customer/supplier balance → commits via `ExecuteTransactionAsync`

### Task 7: API Controllers
- `CashBoxesController`: CRUD with `ManagerAndAbove` policy
- `BanksController`: CRUD with `ManagerAndAbove` policy
- `CustomerReceiptsController`: CRUD + Post + Cancel with `AllStaff` policy (View on reads, Finance on writes)
- `SupplierPaymentsController`: CRUD + Post + Cancel with `ManagerAndAbove` policy
- All controllers: `[Authorize]`, 404 vs 400 differentiation, JWT userId extraction
- `[EnableRateLimiting("LoginPolicy")]` on all write endpoints

### Task 8: Desktop API Services
- `ICashBoxApiService` / `CashBoxApiService` — typed HttpClient
- `IBankApiService` / `BankApiService` — typed HttpClient
- `ICustomerReceiptApiService` / `CustomerReceiptApiService` — typed HttpClient
- `ISupplierPaymentApiService` / `SupplierPaymentApiService` — typed HttpClient
- All using `HandleResponseAsync<T>` with content-type guard

### Task 9: Desktop ViewModels
- `CashBoxesListViewModel`: list, search, toggle active, delete (soft), IDisposable, newest-first
- `CashBoxEditorViewModel`: INotifyDataErrorInfo, SetDialogService(), ValidateAllAsync(), dual constructor
- `CustomerReceiptsListViewModel`: list by Customer, newest-first, Status badges, Post + Cancel buttons
- `CustomerReceiptEditorViewModel`: Customer picker, CashBox picker, optional invoice distribution grid
- `SupplierPaymentsListViewModel`: list by Supplier, newest-first, Status badges, Post + Cancel buttons
- `SupplierPaymentEditorViewModel`: Supplier picker, CashBox picker, optional invoice distribution grid
- All ViewModels: `ExecuteAsync()` wrapper, no CanExecute predicates, `IToastNotificationService` for success

### Task 10: Desktop Views (XAML)
- `CashBoxesListView` + `CashBoxEditorView` — compact styles
- `CustomerReceiptsListView` + `CustomerReceiptEditorView` — compact styles, Status badges (Draft/Posted/Cancelled)
- `SupplierPaymentsListView` + `SupplierPaymentEditorView` — compact styles
- All views: Arabic ToolTips, ErrorTemplate red border, RTL layout
- Invoice distribution sub-grid for applications (optional, collapsible section)
- Post confirmation dialog: "سيتم ترحيل السند — هل أنت متأكد؟"
- Cancel confirmation dialog: "سيتم إنشاء قيد عكسي — لا يمكن التراجع"

### Task 11: DI Registration
- Register all new services in API `Program.cs`
- Register all new API services + ViewModels in Desktop `Program.cs`
- Add migration step for `DbSeeder` (seed default cash boxes if none exist)

### Task 12: EventBus Integration
- `CashBoxChangedMessage` — publish on create/update/toggle
- `CustomerReceiptChangedMessage` — publish on create/post/cancel
- `SupplierPaymentChangedMessage` — publish on create/post/cancel
- Lists subscribe via EventBus, unsubscribe in `Cleanup()` / `Dispose()`

### Task 13: Seeder Updates
- Default cash boxes: "الصندوق الرئيسي" (linked to cash account `1111`), "صندوق المبيعات" (linked to `1112`)
- Default entry in `DocumentSequences` for `"CustomerReceipt"` and `"SupplierPayment"` starting at 1
- SystemAccountMapping for receipt/payment accounts if not already exists

---

## 7. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **No automatic invoice payment on cash sales** | User must manually create receipt for cash sales; extra step | Acceptable in V1 — cash sales invoices set `PaidAmount = NetTotal` directly, no separate receipt needed |
| **CashBox balance query performance** | Running balance from journal entries could be slow with many transactions | Optimize with indexed query on `(AccountId, CreatedAt DESC)`; cache `CurrentBalance` on `Account` as a computed field in V2 |
| **No Cheque support** | Cannot track cheque payments | Acceptable for V1 — all payments are cash; cheque module planned for V2 |
| **No DailyClosure** | Cashiers cannot formally close the day | Acceptable for V1 — balance checking is manual via UI queries |
| **Race condition on ReceiptNo/PaymentNo generation** | Duplicate numbers under high concurrency | Mitigated: `SemaphoreSlim` lock + `UNIQUE` index on `ReceiptNo`/`PaymentNo` |
| **Invoice application mismatch** | User applies more than invoice remaining amount | Mitigated: validate `AppliedAmount <= Invoice.RemainingAmount` at posting time |
| **Currency confusion** | User posts receipt in wrong currency | Mitigated: enforce `CashBox.CurrencyId == Receipt.CurrencyId` (optional in V1, recommended) |

---

## 8. What's NOT in V1 (Deferred)

| Feature | Target Phase | Reason |
|---------|-------------|--------|
| `DailyClosure` entity | Phase 30+ (V2) | Too complex for V1; manual balance checks suffice |
| `Cheque` entity (lifecycle) | Phase 30+ (V2) | Cheque processing (pending/cleared/bounced) adds significant complexity |
| `CashTransaction` generic table | Not planned | Replaced by specific Receipt/Payment entities |
| CashBox assignments (user → cash box) | Phase 30+ (V2) | Simple at entity level but complex at permissions level |
| Multi-currency receipt splitting | Phase 30+ (V2) | Single currency per transaction for V1 simplicity |
| Automated bank reconciliation | Phase 31+ (V2) | Requires bank statement import feature |
| CashBox opening/closing balance tracking | Phase 30+ (V2) | Covered by `DailyClosure` in V2 |
