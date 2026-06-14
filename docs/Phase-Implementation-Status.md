# Phase Implementation Status

> **Last Updated**: June 8, 2026
> **Current Version**: v4.10 (Phases 18-25 Complete, Schema Refactored to 65 Tables, Phases 26-32 Updated)
> **Source Analysis**: `docs/all new Anylysis for update system features/`

---

## 📊 Implementation Progress

| Phase | Module | Status | Lines of Code | Key Deliverables |
|-------|--------|--------|--------------|-----------------|
| ⚓ **18** | **Accounting Foundation** | ✅ Completed | ~1,519 | Chart of Accounts entities, Journal Entries, Fiscal Years, SystemAccountMappings |
| ⚙️ **19** | **Settings Module** | ✅ Completed | ~2,200 | SystemSettings (29 seeded), Tax entity, Memory Caching, StoreSettings, CostingMethod UI |
| 💱 **20** | **Currencies Module** | ✅ Completed | ~1,800 | Currency entity, ExchangeRateHistory, Multi-currency invoice support, Desktop CRUD |
| 👤 **21** | **Users & Permissions** | ✅ Completed | ~2,333 | 4 roles, 33 permissions, passwordless creation, AuditLog, lockout, session tracking |
| 📒 **22** | **Chart of Accounts** | ✅ Completed | ~3,500 | 60-account hierarchy, 4 levels, dual-mode Desktop UI, SystemAccountMappings updated |
| 👥 **23** | **Customers Module** | ✅ Completed | ~2,100 | Party entity (shared contact data), AccountId mandatory (auto-created under 1210), CheckCreditLimit returns bool, NO CustomerGroup/SupplierType/CustomerType in V1 |
| 🤖 **24** | **Accounting Integration** | ✅ Completed | ~700 | IAccountingIntegrationService, auto journal entries for all money ops, payment reversals, per-entity account routing (Customer.AccountId/Supplier.AccountId), PurchaseReturnAccountId, single Sales Revenue account |
| 📦 **25** | **Products Module** | ✅ Completed | ~3,100 | Multi-currency pricing via ProductPrices per (ProductUnit × CurrencyId), FIFO batches (InventoryBatches), NO PriceLevel in V1, ProductImages, DefaultPurchaseUnitId/DefaultSalesUnitId |
| 🏭 **26** | **Warehouses Module** | 📝 Planned | — | Warehouse types (Main/Store/Showroom), WarehouseTransfers replace StockTransfers, InventoryTransactions replace InventoryMovements, NO physical count in V1 |
| 🛒 **27** | **Purchases Module** | 📝 Planned | — | Multi-currency via CurrencyId FK, landed cost via OtherCharges on invoice header, NO PurchaseOrders in V1, standalone returns with PurchaseInvoiceLineId link |
| 💳 **28** | **Sales Module** | 📝 Planned | — | Multi-currency via CurrencyId FK, profit display per line, NO SalesQuotations in V1, barcode POS, credit limit enforcement (CheckCreditLimit returns bool) |
| 💰 **29** | **Receipts & Payments** | 🟡 Partial — CashBox ✅ | ~2,500 | CustomerReceipts (سندات قبض) with multi-invoice allocation, SupplierPayments with multi-invoice allocation, ReceiptVouchers/PaymentVouchers replace CashTransactions, NO Cheques/DailyClosure in V1 |
| 📓 **30** | **Journal Entries** | 📝 Planned | — | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing, ReceiptVouchers (سندات قبض) and PaymentVouchers (سندات صرف) for manual entries |
| 📊 **31** | **Reports** | 📝 Planned | — | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |
| 👥 **32** | **Suppliers Module** | 📝 Planned | — | Party entity with shared contact data, AccountId mandatory (auto-created under 2100), NO SupplierType in V1, CreditLimit, NO OpeningBalance on entity (journal entry is source of truth) |

---

## 🔄 v4.10 — Schema Refactoring (82 → 65 Tables)

### Removed Tables (17)
| Old Table | Replacement | Reason |
|-----------|-------------|--------|
| ProductBarcodes | — | Merged into Products.Barcode column |
| ProductImages | — | Deferred to V2 |
| BillOfMaterials | — | Deferred to V2 |
| ProductPriceHistory | ProductPrices | New multi-currency model |
| StoreSettings | CompanySettings | Renamed + simplified |
| CustomerGroup | — | Not in V1 |
| CustomerPayments | CustomerReceipts | Renamed for accounting clarity |
| SupplierPayments | SupplierPayments (new) | Restructured with allocation |
| CashTransactions | ReceiptVouchers/PaymentVouchers | Proper accounting vouchers |
| DailyClosures | — | Deferred to V2 |
| Cheques | — | Deferred to V2 |
| PurchaseLots | InventoryBatches | Unified batch tracking |
| InventoryMovements | InventoryTransactions | Structured with lines |
| StockTransfers | WarehouseTransfers | Renamed for clarity |
| StockTransferItems | WarehouseTransferLines | Renamed for clarity |
| InventoryOperations | — | Merged into InventoryTransactions |
| StockWriteOffs | — | Covered by InventoryAdjustments |

### Added Tables (8)
| New Table | Module | Purpose |
|-----------|--------|---------|
| Parties | Core | Shared contact data for Customers/Suppliers/Employees |
| ProductPrices | Products | Multi-currency pricing per unit |
| InventoryBatches | Inventory | FIFO/FEFO batch tracking |
| InventoryTransactions | Inventory | Structured stock movement tracking |
| InventoryTransactionLines | Inventory | Per-product transaction details |
| WarehouseTransfers | Inventory | Warehouse-to-warehouse transfers |
| WarehouseTransferLines | Inventory | Transfer line items with batch links |
| CustomerReceipts | Sales | Customer payment receipts with invoice allocation |
| CustomerReceiptApplications | Sales | Links receipts to specific invoices |
| ReceiptVouchers | Accounting | Manual receipt vouchers (سندات قبض) |
| PaymentVouchers | Accounting | Manual payment vouchers (سندات صرف) |

---

## ✅ Phase 18 — Accounting Foundation (Complete)

**Key Entities**: `Account`, `JournalEntry`, `JournalEntryLine`, `FiscalYear`

**Features**:
- 4-level Chart of Accounts hierarchy (Group → Main → Sub → Detail)
- 60 seeded accounts across Assets, Liabilities, Equity, Revenue, Expenses
- Journal entries with double-entry validation (CHK_DebitOrCredit, CHK_NoNegativeValues)
- SystemAccountMappings for 13 system operation types
- Fiscal Year with open/close lifecycle
- Annual Closing: revenue/expense → RetainedEarnings transfer, fiscal year lock

**Architecture Decisions**:
- Account level range 1-10 with CHECK constraint
- System accounts (L1-L2) protected — never deleted or edited
- Leaf accounts (L4+) allow transactions; parent accounts block transactions
- Color-coded per account type (blue=Asset, red=Liability, green=Equity/Revenue, orange=Expense)

---

## ✅ Phase 19 — Settings Module (Complete)

**Key Entities**: `SystemSetting`, `Tax`, `StoreSettings`

**Features**:
- 29 system settings across 8 categories (Inventory, Sales, Purchases, Barcode, Print, Notifications, Accounting, General)
- Memory caching: `IMemoryCache` + `ConcurrentDictionary` with 5-minute sliding expiration
- Tax entity with CRUD, percentage/fixed types, `IsDefault` flag
- Tax on invoices: `TaxId` FK with `DeleteBehavior.Restrict`
- StoreSettings (company info) with deprecation strategy
- Costing Method: RadioButton UI with 3 options (WeightedAverage, LastPurchasePrice, SupplierPrice)
- Admin-only settings endpoints

**Key Rules**: RULE-291 through RULE-301

---

## ✅ Phase 20 — Currencies Module (Complete)

**Key Entities**: `Currency`, `ExchangeRateHistory`

**Features**:
- Currency entity with ISO code (3 chars), symbol, exchange rate, FractionName
- Exchange Rate History with effective dated changes
- Multi-currency invoice support (CurrencyId + ExchangeRate on sales/purchase)
- IsSystem guard on seeded currencies (YER/USD/SAR)
- AllStaff policy for read endpoints
- Desktop CRUD with INotifyDataErrorInfo validation

**Bug Fixes Applied**:
- BUG-001: `Currency.Create()` accepts `isSystem` parameter
- BUG-003: `includeInactive` parameter passthrough
- BUG-004: OldRate validation `<= 0`
- BUG-005: Controller HTTP 404 vs 400
- BUG-006: Filtered unique index `[IsActive] = 1` guard
- BUG-007: `UpdateExchangeRateRequestValidator` added
- BUG-008: CurrencyCode validation = exactly 3 characters (ISO 4217)
- BUG-009: `IsActive` removed from `UpdateCurrencyRequest`
- BUG-010: `IDisposable` on list VM
- BUG-011: CanExecute removed from Edit/Delete

---

## ✅ Phase 21 — Users & Permissions (Complete)

**Key Entities**: `User` (enhanced), `Permission`, `RolePermission`, `AuditLog`, `UserSession`

**Features**:
- 4 roles: Admin, Accountant, Cashier, Observer
- 33 permission codes across 9 categories
- Passwordless user creation (PasswordHash = null, MustChangePassword = true)
- UserStatus enum (Active=1, Inactive=2, Locked=3) replacing IsActive
- Account lockout after 5 failed login attempts
- AuditLog with long Id (bigint), 3 indexes
- UserSession tracking for active sessions
- 10 new API endpoints across 3 controllers
- Desktop: Password change screen, AuditLog browser, Permission management, StatusBar

**Key Rules**: RULE-305 through RULE-320

---

## ✅ Phase 22 — Chart of Accounts (Complete)

**Key Entities**: `Account` (expanded)

**Features**:
- 60-account hierarchy: 5 Groups (L1), 8 Main (L2), 20 Sub (L3), 27 Detail (L4)
- Numeric codes: L1=3 chars, L2+=4-10 chars
- New properties: Level, Description, ColorCode, AllowTransactions, OpeningBalance
- DB-level CHECK constraint for Level range (1-10)
- System accounts (L1-L2) protected from modification/deletion
- Two-pass seeder (L1-L2 → SaveChanges → L3-L4)
- Dual-mode Desktop UI: TreeView + DataGrid toggle
- Full CRUD API with policy-based authorization

**Bug Fixes Applied**:
- `HasChildren()` domain guard → service-level `AnyAsync()` DB query
- Entity fetched once (not twice) in Delete/PermanentDelete
- `Explanation` field added across all layers
- L1 account codes limited to 3 chars
- Update validators match Create validators
- `:byte` route constraints → `:int:min(1):max(N)`
- Health check uses SecureDbContextFactory, not raw IConfiguration

---

## ✅ Phase 23 — Customers Module (Complete)

**Key Entities**: `Customer` (enhanced), `CustomerGroup`

**Features**:
- CustomerType REMOVED — Cash/Credit is per-invoice on PaymentType, not customer attribute
- CustomerGroup entity (soft-deletable, child reference guard)
- AccountId FK (optional) linking to Chart of Accounts
- CreditLimit enforcement via `CheckCreditLimit()` returning bool
- API: CustomerGroupsController (kebab-case routes `customer-groups`)
- API: Report endpoints (by-group, balance, aging)
- Desktop: Group filter, CustomerGroup editor, Account lookup
- Seeded: "عام" group + "عميل نقدي" default customer

**Key Rules**: RULE-353 through RULE-370

---

## ✅ Phase 24 — Accounting Integration (Complete)

**Key Service**: `IAccountingIntegrationService` / `AccountingIntegrationService`

**Features**:
- Auto journal entries for ALL money operations
- Customer opening balance: Dr AR / Cr OpeningBalanceEquity
- Supplier opening balance: Dr OpeningBalanceEquity / Cr AP
- Sales post: Revenue entry (Dr Cash/AR, Cr SalesRevenue, Cr VAT) + COGS entry (Dr COGS, Cr Inventory)
- Sales cancel: Full reversal with Dr↔Cr swap
- Purchase post: Dr Inventory, Dr VAT Input, Cr Cash/AP
- Purchase cancel: Full reversal
- Customer Payment: Dr Cash, Cr AR
- Supplier Payment: Dr AP, Cr Cash
- Payment Update/Delete reversals: Reverse old entry before creating new

**Key Rules**: RULE-371 through RULE-388

---

## 📝 Phase 25 — Products Module (Planned)

**Key Entities**: `Product` (restructured), `ProductPrices`, `InventoryBatches`, `ProductImages`, `PriceLevel`

**Features Planned**:
- Product entity simplified: remove PurchasePrice/SalePrice/WholesalePrice/RetailPrice/ExpirationDate/Barcode
- Multi-currency pricing via `ProductPrices` (ProductUnitId, CurrencyId, Price, EffectiveFrom, EffectiveTo)
- FIFO/FEFO batch tracking via `InventoryBatches` (ProductId, PurchaseInvoiceItemId, Quantity, UnitCost, BatchNo, ExpiryDate)
- PriceLevel enum: Retail=1, Wholesale=2, VIP=3, Distributor=4
- PriceLevelService returns `Result<decimal>` for price calculations
- Multiple product images per product
- Opening stock on creation: optional Quantity + UnitCost + Expiry per warehouse
- BOM (Bill of Materials) support for assembled products

**Key Rules**: RULE-389 through RULE-395

---

## 📝 Phase 26 — Warehouses Module (Planned)

**Key Entities**: `Warehouse` (enhanced)

**Features Planned**:
- Warehouse types: Main (1), Store (2), Showroom (3)
- Additional fields: ManagerName, AccountId FK, Address, Phone
- StockAdjustmentType enum: Addition=1, Deduction=2, Correction=3
- StockIssueReason enum: SalesReturn=1, Damage=2, Expiry=3, InternalUse=4, Other=5
- Physical Count deferred to V2
- Every inventory operation creates InventoryMovement record

**Key Rules**: RULE-396 through RULE-400

---

## 📝 Phase 27 — Purchases Module (Planned)

**Key Entities**: `PurchaseInvoice` (enhanced), `AdditionalCharge`, `PurchaseOrder`

**Features Planned**:
- Multi-currency: CurrencyId + ExchangeRate on purchase invoice
- Landed cost: `AdditionalCharge` entity distributes transport/customs/handling across items
- Purchase Order entity: separate table, Draft/Approved/Received/Cancelled lifecycle
- Partial PO receipt via PurchaseInvoice
- Standalone purchase returns (not linked to original invoice)
- Single attachment for invoice image

**Key Rules**: RULE-401 through RULE-405

---

## 📝 Phase 28 — Sales Module (Planned)

**Key Entities**: `SalesInvoice` (enhanced), `SalesQuotation`

**Features Planned**:
- Multi-currency: CurrencyId + ExchangeRate on sales invoice
- Profit display per line: SalePrice - AverageCost
- Price override with permission check
- Sales Quotation entity: expiry date, Draft/Confirmed/Expired/Converted lifecycle
- Continuous barcode scanning POS mode (keyboard wedge scanner)
- Credit limit enforcement: `Customer.CheckCreditLimit()` before posting

**Key Rules**: RULE-406 through RULE-410

---

## 🟡 Phase 29 — Receipts & Payments (Partial — CashBox ✅ Done)

**Key Entities**: `CashBox` (refactored), `CashTransaction` (refactored), `CustomerPayment` (enhanced pending), `SupplierPayment` (enhanced pending), `Cheque`, `PaymentAllocation`

### ✅ Completed — CashBox Accounting Integration
- **CashBox refactored**: Removed `OpeningBalance`/`CurrentBalance`/`Deposit()`/`Withdraw()` — balance tracked on linked Account via `AccountId` FK
- **CashTransaction refactored**: `BalanceBefore`/`BalanceAfter` → `RunningBalance` (computed cumulative sum); `Create()` made public
- **Auto-account creation**: Service auto-creates Level-4 detail account under parent `"1110 — النقدية"` when `AccountId` is null
- **Metadata fields**: `CategoryId` (FK to Categories), `PhoneNumber`, `TaxNumber`, `Address` for classifying cash registers
- **EF Configuration**: FK mappings for Account (Restrict), Category (Restrict); phone/tax/address field configs
- **DTOs/Validators updated**: No balance fields; Phone/Tax/Address length rules
- **Desktop UI**: CashBoxEditor with Category dropdown, Phone/TaxNumber/Address fields; no balance fields
- **CashTransferViewModel**: Removed client-side balance validation (server validates via Account)
- **FinancialReportService**: RunningBalance usage; no CashBox balance fallback
- **DbSeeder**: No default cash box seed (accounts not yet seeded at that point)
- **Tests rewritten**: CashBoxServiceTests, CashBoxTests (Domain), CashTransactionTests — all passing
- **Build**: 0 errors, all unit tests passing

### 📝 Planned — Remaining
- Multi-invoice payment distribution via `PaymentAllocation`
- Cheque management: ChequeNumber, BankName, IssueDate, MaturityDate, Status (Pending/Cleared/Bounced/Cancelled)
- Cash/ Bank/ Cheque payment methods
- DailyClosure: ActualCashCount, ExpectedBalance, Difference, DifferenceReconciled flag
- Cash transactions create automatic journal entries

**Key Rules**: RULE-077 through RULE-083, RULE-411 through RULE-415

---

## 📝 Phase 30 — Journal Entries (Planned)

**Key Entities**: `JournalEntry` (enhanced)

**Features Planned**:
- 3-state lifecycle: Draft (1) → Posted (2) → Cancelled (3)
- Multi-currency: CurrencyId FK, ExchangeRate, display original + base amounts
- Attachments: single AttachmentPath or JournalEntryAttachments table
- FiscalYear entity: Year, StartDate, EndDate, IsOpen, OpenedAt, ClosedAt, ClosedByUserId
- Annual Closing: revenue/expense → RetainedEarnings, fiscal year lock, opening entry for new year

**Key Rules**: RULE-416 through RULE-420

---

## 📝 Phase 31 — Reports Module (Planned)

**Key Deliverables**: 35+ Report DTOs across 7 categories

**Categories**:
1. **Financial** (6): Trial Balance, General Ledger, Income Statement, Balance Sheet, Cash Flow, Account Statement
2. **Inventory** (5): Stock Balance, Stock Movement, Inventory Valuation, Low Stock Alert, Expired Products
3. **Sales** (6): Sales Invoice, Customer Statement, Sales Profitability, Sales By Product, Unpaid Invoices, Daily Sales
4. **Purchases** (4): Purchase Invoice, Supplier Statement, Purchases By Product, Unpaid Purchases
5. **Cash/Box** (4): Cashbox Statement, Daily Closure, Transaction Log, Cash Transfer Report
6. **Transactions** (5): Journal Entries, Customer Payments, Supplier Payments, Receipt Vouchers, Payment Vouchers
7. **Users** (5): User Activity, Login History, Permission Changes, Audit Trail, System Log

**Key Rules**: RULE-421 through RULE-425

---

## 📝 Phase 32 — Suppliers Module (Planned)

**Key Entities**: `Supplier` (enhanced)

**Features Planned**:
- AccountId FK linking to Chart of Accounts (auto-create sub-account)
- SupplierType enum (Cash=1, Credit=2) — informational only, not business logic
- CreditLimit validation: `CheckCreditLimit()` returns bool
- OpeningBalance → auto journal entry (Dr OpeningBalanceEquity / Cr AP)
- UI: Balance display in list view, credit limit indicator, account lookup
- Reports: Supplier balance aging, unpaid purchases by supplier

**Key Implementation Tasks**:
1. Add `AccountId` (int? FK), `SupplierType` (byte), `SupplierGroupId` (int? FK) to Supplier entity
2. Add `SupplierGroup` entity (soft-deletable, child reference guard)
3. Create/Update Supplier service methods accept `accountId`, `supplierType`, `supplierGroupId` params
4. API: SupplierGroupsController with kebab-case routes
5. API: Report endpoints (by-group, balance, aging)
6. Desktop: Supplier Editor with group/account dropdowns, type selection
7. Desktop: Supplier List with group filter
8. Seeder: "مورد نقدي" default supplier with Cash type

---

## 🧩 Cross-Phase Design Decisions

### 1. Multi-Currency Everywhere
ALL financial entities MUST support multi-currency:
- Prices: `ProductPrices` table per currency
- Invoices: `CurrencyId` + `ExchangeRate` on sales/purchase invoices
- Payments: `CurrencyId` + `ExchangeRate` on payments
- Journal entries: `CurrencyId` + `ExchangeRate` with both original and base amounts
- CashBoxes: Per-box `CurrencyId` (YER box, USD box, SAR box)

### 2. Chart of Accounts Integration
ALL operational entities MUST link to COA:
- CashBox → AccountId FK (cash/bank account)
- Warehouse → AccountId FK (inventory account)
- Customer → AccountId FK (receivables account)
- Supplier → AccountId FK (payables account)
- Auto-creation of sub-accounts on entity creation

### 3. FIFO/FEFO Batch Costing
Cost allocation MUST use batch tracking:
- `InventoryBatches` records purchase lots with cost and expiry
- COGS calculated from actual batch costs (FIFO) or nearest-expiry (FEFO)
- SystemSetting configures costing method: FIFO, FEFO, or WeightedAverage

### 4. Landed Cost Distribution
Purchase costs MUST include additional charges:
- `AdditionalCharge` entity (transport, customs, handling)
- Distributed across invoice items by quantity or value weighting
- True landed cost reflected in inventory valuation

### 5. Automatic Journal Entries
ALL money operations MUST create journal entries:
- `IAccountingIntegrationService` with 10 methods covering all operations
- Post/create → forward entries (Dr/Cr)
- Cancel/delete → reversal entries (Dr↔Cr swapped)
- Payment update → reverse old + create new

### 6. Payment Allocation
Payments MUST track multi-invoice settlement:
- `PaymentAllocation` entity links payment to invoices
- Supports partial payments across multiple invoices
- Full audit trail for each allocation

### 7. Self-Explaining System (ⓘ)
Educational tooltip system for non-accountant users:
- `AccountTypeHelp` table stores explanations per account type
- Clicking ⓘ shows explanation + examples
- Applied to: COA, Journal Entries, Receipt/Payment Vouchers
- Goal: No external training required

### 8. CashBox as Operational Layer (Refactored v4.9)
CashBox is a lightweight register facade over Chart of Accounts:
- CashBox has NO balance fields — balance lives on linked Account entity
- TRUE balance source = Account (via Chart of Accounts) or RunningBalance from CashTransaction sum
- CashTransaction.RunningBalance is computed as cumulative sum of all transaction amounts
- CashBox stores metadata only: Name, AccountId, CategoryId, PhoneNumber, TaxNumber, Address
- DailyClosure reconciles actual cash against Account balance

### 9. Seed Data Completeness
System MUST seed on first run:
| Item | Value |
|------|-------|
| Company | Default entry |
| Currencies | YER (base) + USD |
| Warehouse | المخزن الرئيسي |
| CashBox | الصندوق الرئيسي |
| Units | حبة, كرتون, علبة, كيلو, جرام, لتر, متر |
| Chart of Accounts | 60 accounts |
| Default Customer | عميل نقدي |
| Default Supplier | مورد نقدي |
| Default Category | عام |

---

## 🐛 Known Bugs & Edge Cases

| ID | Severity | Description | Status |
|----|----------|-------------|--------|
| C-1 | CRITICAL | COGS uses PurchaseCost instead of AverageCost in accounting integration | ✅ Fixed (Phase 24) |
| C-2 | CRITICAL | Payment Update/Delete missing reversal journal entries | ✅ Fixed (Phase 24) |
| C-3 | CRITICAL | Payment Update C-3: Reverse old entry before creating new | ✅ Fixed (Phase 24) |
| C-4 | HIGH | CashTransactionType on purchase cancel uses SupplierPayment instead of RefundOut | ✅ Fixed (Phase 24) |
| C-5 | HIGH | ReferenceId lookup relies on string matching instead of int FK | ✅ Fixed (Phase 24) |
| C-6 | MEDIUM | InvoiceNo generation uses lastId+1 (not thread-safe) | ✅ Fixed (Phase 24) |
| C-7 | MEDIUM | NetRevenue clamped to 0 when discount > subtotal (unbalanced entries) | ✅ Fixed (Phase 24) |
| C-8 | MEDIUM | JournalEntriesController accepts client-supplied CreatedBy | ✅ Fixed (Phase 24) |

---

## 📅 Recommended Implementation Order (Phases 25-32)

```text
Phase 25 — Products Module       (Multi-currency pricing, FIFO batches)
     ↓
Phase 26 — Warehouses Module     (Types, accounts, adjustments)
     ↓
Phase 27 — Purchases Module      (Multi-currency, landed cost, PO)
     ↓
Phase 28 — Sales Module          (Multi-currency, quotations, POS)
     ↓
Phase 29 — Receipts & Payments   (Cheques, allocations)
     ↓
Phase 30 — Journal Entries       (3-state lifecycle, FiscalYear)
     ↓
Phase 31 — Reports Module        (35+ DTOs, Excel export)
     ↓
Phase 32 — Suppliers Module      (AccountId FK, SupplierType, CreditLimit, OpeningBalance journals)
```

**Dependencies**:
- Phase 25 depends on: Phases 18-24
- Phase 26 depends on: Phase 25 (inventory batches)
- Phase 27 depends on: Phases 25 + 26 (products + warehouses)
- Phase 28 depends on: Phases 25 + 26 + 29 (products, warehouses, cash)
- Phase 29 depends on: Phases 18-24 (chart of accounts, accounting)
- Phase 30 depends on: All phases (final accounting integration)
- Phase 31 depends on: All phases (reports on everything)
- Phase 32 depends on: Phases 18-24 (chart of accounts, accounting integration)

---

## 🔗 Cross-References

| Document | Link |
|----------|------|
| AGENTS.md (Master Rules) | `../AGENTS.md` |
| Plan for Phases (Matrix) | `all new Anylysis for update system features/Plan for Phases.md` |
| Global Analysis | `all new Anylysis for update system features/Global Analysis.md` |
| Analysis Part 1 | `all new Anylysis for update system features/Analysis Part 1.md` |
| Analysis Part 2 | `all new Anylysis for update system features/Analysis Part 2.md` |
| Analysis Part 3 | `all new Anylysis for update system features/Analysis Part 3.md` |
| Analysis Part 4 | `all new Anylysis for update system features/Analysis Part 4.md` |
| Analysis Part 5 | `all new Anylysis for update system features/Analysis Part 5.md` |
| Phase 18 Implementation Plan | `Phase 18 — Accounting Foundation Implementation Plan.md` |
| Phase 22 Implementation Plan | `Phase 22 — Chart of Accounts Module Implementation Plan.md` |
