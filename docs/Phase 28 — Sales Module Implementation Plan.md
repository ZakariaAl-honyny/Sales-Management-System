# Phase 28 — Sales Module: Comprehensive Enhancement & New Features Implementation Plan

> **Version**: 2.0 — Updated with 9 Sales Scenarios (exact journal entries), Draft/Post/Cancel architecture, Cancel vs Return distinction, Business Rules, Basic vs Advanced UI
> **Scope**: Sales Invoice enhancement, Sales Quotations (NEW), Sales Returns enhancement, Barcode POS (NEW)
> **Based on**: Analysis Part 3 (FIFO batch allocation), Analysis Part 5 (multi-currency, pricing, sales return), Global Analysis, Sales and Purchases new details.md, Invoices Details.md, Accounts.md

---

## Table of Contents
1. [Architecture — 4 Sub-Modules](#1-architecture--4-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog — Full Specs](#4-design-catalog--full-specs)
5. [9 Sales Scenarios with Exact Journal Entries](#5-9-sales-scenarios-with-exact-journal-entries)
6. [Business Rules & Error Prevention](#6-business-rules--error-prevention)
7. [Draft/Post/Cancel Architecture](#7-draftpostcancel-architecture)
8. [Cancel vs Return Distinction](#8-cancel-vs-return-distinction)
9. [Basic vs Advanced UI Mode](#9-basic-vs-advanced-ui-mode)
10. [Gap Analysis](#10-gap-analysis)
11. [Architectural Decisions](#11-architectural-decisions)
12. [Non-V1 Items (Deferred)](#12-non-v1-items-deferred)
13. [Implementation Tasks](#13-implementation-tasks)
14. [API Endpoints](#14-api-endpoints)
15. [FluentValidation Updates](#15-fluentvalidation-updates)
16. [Sales Reports Endpoints](#16-sales-reports-endpoints)
17. [Self-Explanation Tooltips (Sales Module)](#17-self-explanation-tooltips-sales-module)
18. [Compliance Matrix (55+ Rules)](#18-compliance-matrix-55-rules)
19. [Risks & Mitigations](#19-risks--mitigations)
20. [Rollback Plan](#20-rollback-plan)
21. [Unit Test Tasks](#21-unit-test-tasks)
22. [Cross-Reference: Phase 25 & Phase 24](#22-cross-reference-phase-25-inventorybatches--phase-24-accounting-integration)

---

## 1. Architecture — 4 Sub-Modules

The Sales Module is divided into **4 sub-modules**:

| # | Sub-Module | Type | Description |
|---|-----------|------|-------------|
| S1 | **Sales Invoices** | Enhancement | Multi-currency, price override with approval, enhanced profit display, auto-journal entries, auto-payment creation, enhanced discount model, 9 sales scenarios |
| S2 | **Sales Quotations** | NEW | Pre-sales quote (عرض سعر) — no stock/account impact, can convert to invoice |
| S3 | **Sales Returns** | Enhancement | FIFO batch return, discount handling, auto customer refund, enhanced journal entries, full/partial return scenarios |
| S4 | **Barcode POS** | NEW | Continuous scan mode for fast POS-style entry, sound feedback, auto-add with quantity increment |

### Data Flow — Post Operation (All in ONE Transaction)

```
Desktop (WPF)
  SalesInvoice EditorVM | SalesQuote EditorVM | SalesReturn EditorVM
       |                          |                       |
  ApiService (HttpClient)
       |                          |                       |
  API (ASP.NET Core)
  SalesInvoice Controller | SalesQuote Controller | SalesReturn Controller
       |                          |                       |
  Application Services
  (SalesInvoiceService, SalesQuoteService, SalesReturnService, 
   InventoryService, AccountingIntegrationService, CustomerPaymentService)
       |                          |                       |
  Infrastructure
  (IUnitOfWork.ExecuteTx | Repositories | EF Core Config + Migrations)
       |                          |                       |
  SQL Server
  SalesInvoices | SalesInvoiceLines | SalesQuotations | SalesQuotationItems
  SalesReturns | SalesReturnItems | InventoryTransactions | InventoryTransactionLines
  InventoryBatches | WarehouseStocks | JournalEntries | JournalEntryLines
  CustomerReceipts | CustomerReceiptApplications | CashTransactions
```

### What Happens Inside a Single Post Transaction

When "Save & Post" is clicked, the following ALL happen inside ONE database transaction:

```
1. Save SalesInvoice (generates Id + InvoiceNo)
2. Save SalesInvoiceLines
3. Set Status = Posted
4. Create InventoryTransaction (Type = Sale)
5. Create InventoryTransactionLines
6. Allocate stock from FIFO batches (oldest batch first):
   - Deduct InventoryBatches.QuantityRemaining
   - Record batch allocation per line
7. Update WarehouseStocks (decrease quantity)
8. Compute COGS from actual batch costs (unit cost x qty)
9. Journal Entry #1 (Revenue side):
   - Dr CashBox / Dr Customer / Dr DiscountAllowed (as applicable)
   - Cr SalesRevenue
   - Cr VatOutput (if tax applies)
   - Cr DeliveryRevenue (if additional charges)
10. Journal Entry #2 (COGS side):
    - Dr COGS
    - Cr Inventory (based on FIFO batch costs)
11. Update customer balance (DueAmount added)
12. Create CustomerReceipt (if cash/mixed payment PaidAmount > 0)
13. Create CustomerReceiptApplications (allocate payment to invoice)
14. Create CashTransaction (if cash payment CashTransactionType.SalesIncome)
15. Commit transaction
```

**If any step fails the entire transaction is rolled back. Nothing is partially applied.**

### Lifecycle States

```
Sales Invoice:    Draft (1) -> Posted (2) -> Cancelled (3)
Sales Quotation:  Draft (1) -> Sent (2) -> Accepted (3) -> Converted (4) -> Rejected (5)
Sales Return:     Draft (1) -> Posted (2) -> Cancelled (3)
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Entities

| Entity | Status |
|--------|--------|
| SalesInvoice | Exists (165 lines) |
| SalesInvoiceLine | Exists (58 lines) |
| SalesReturn | Exists (SalesReturn + SalesReturnItem) |

### 2.2 EF Core Configurations

| Configuration | Status |
|--------------|--------|
| SalesInvoiceConfiguration | Exists |
| SalesInvoiceLineConfiguration | Exists |

### 2.3 Contracts

| Type | Status |
|------|--------|
| SalesInvoiceDto | Exists |
| SalesInvoiceLineDto | Exists |
| SalesReturnDto | Exists |
| SalesReturnItemDto | Exists |
| SalesInvoiceResponse | Exists |
| SalesInvoiceLineResponse | Exists |
| CreateSalesInvoiceRequest | Likely exists |
| UpdateSalesInvoiceRequest | Likely exists |

### 2.4 Application Services

| Service | Status |
|---------|--------|
| SalesInvoiceService | Exists |
| SalesReturnService | Exists |
| InventoryService | Exists |
| AccountingIntegrationService | Exists (Phase 24) |

### 2.5 API Controllers

| Controller | Status |
|-----------|--------|
| SalesInvoicesController | Exists |
| SalesReturnsController | Likely exists |

### 2.6 Desktop ViewModels

| ViewModel | Status |
|-----------|--------|
| SalesInvoiceEditorViewModel | Exists (1486 lines) |
| SalesInvoiceListViewModel | Exists |
| SalesReturnEditorViewModel | Exists |
| SalesReturnListViewModel | Exists |
| SalesInvoiceSelectionViewModel | Exists |

### 2.7 Desktop Views

| View | Status |
|------|--------|
| SalesInvoiceEditorView | Exists |
| SalesInvoicesListView | Exists |
| SalesReturnEditorView | Exists |
| SalesReturnsListView | Exists |
| SalesInvoiceSelectionView | Exists |

### 2.8 API Services (Desktop HTTP Clients)

| Service | Status |
|---------|--------|
| SalesInvoiceApiService | Exists |
| SalesReturnApiService | Exists |

### 2.9 Enums Used

| Enum | Values | Status |
|------|--------|--------|
| InvoiceStatus | Draft=1, Posted=2, Cancelled=3 | Exists |
| PaymentType | Cash=1, Credit=2, Mixed=3 | Exists |
| SaleMode | Retail=1, Wholesale=2 | Exists |
| MovementType | PurchaseIn=1, SaleOut=2, SaleReturnIn=3, etc. | Exists |
| CostingMethod | WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3 | Exists |

### 2.10 Existing Sales Features Summary

| Feature | Status |
|---------|--------|
| Sales invoice CRUD (Draft -> Posted -> Cancelled) | Exists |
| Stock deduction on Post | Exists |
| Stock reversal on Cancel | Exists |
| Customer balance update on Post | Exists |
| FIFO batch allocation on sale | Exists (if Phase 25 implemented) |
| Real-time profit display | Exists (basic) |
| Print preview (A4 + 80mm thermal) | Exists |
| Payment type (Cash/Credit/Mixed) | Exists |
| Basic line-item discount | Exists |
| TaxId FK on invoice | Exists (Phase 19) |
| Search by product name/barcode | Exists |
| Multi-unit selection | Exists |
| Draft save/posting | Exists |
| Sales return CRUD | Exists |
| AccountingIntegrationService | Exists (Phase 24) |
| Journal entry creation for sales | Exists (Phase 24) |
| Auto-customer receipt on cash sales | Exists (Phase 24) |

### 2.11 Files to Create (NEW)

| # | File | Purpose |
|---|------|---------|
| 1 | Domain/Entities/SalesQuotation.cs | Sales quotation entity |
| 2 | Domain/Entities/SalesQuotationItem.cs | Sales quotation line item |
| 3 | Infrastructure/Data/Configurations/SalesQuotationConfiguration.cs | EF Core config |
| 4 | Infrastructure/Data/Configurations/SalesQuotationItemConfiguration.cs | EF Core config |
| 5 | Contracts/DTOs/SalesQuotationDto.cs | Quotation DTO |
| 6 | Contracts/Requests/SalesQuotationRequests.cs | Create/Update requests |
| 7 | Contracts/DTOs/ProfitDisplayDto.cs | Profit display DTO |
| 8 | Application/Interfaces/ISalesQuotationService.cs | Quotation service interface |
| 9 | Application/Services/SalesQuotationService.cs | Quotation service implementation |
| 10 | Api/Controllers/SalesQuotationsController.cs | Quotation API endpoints |
| 11 | DesktopPWF/Services/Api/SalesQuotationApiService.cs | Quotation HTTP client |
| 12 | DesktopPWF/ViewModels/Sales/SalesQuotationEditorViewModel.cs | Quotation editor VM |
| 13 | DesktopPWF/ViewModels/Sales/SalesQuotationListViewModel.cs | Quotation list VM |
| 14 | DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml | Quotation editor view |
| 15 | DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml.cs | Code-behind |
| 16 | DesktopPWF/Views/Sales/SalesQuotationsListView.xaml | Quotation list view |
| 17 | DesktopPWF/Views/Sales/SalesQuotationsListView.xaml.cs | Code-behind |
| 18 | DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml | Price override approval dialog |
| 19 | DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml.cs | Code-behind |
| 20 | DesktopPWF/ViewModels/Sales/DraftInvoicesViewModel.cs | Draft invoice management VM |
| 21 | DesktopPWF/Views/Sales/DraftInvoicesView.xaml | Draft management view |
| 22 | DesktopPWF/Views/Sales/DraftInvoicesView.xaml.cs | Code-behind |
| 23 | DesktopPWF/Controls/BarcodeScannerControl.xaml | Continuous barcode scan control |
| 24 | DesktopPWF/Controls/BarcodeScannerControl.xaml.cs | Code-behind |

### 2.12 Files to Modify (ENHANCE)

| # | File | Change |
|---|------|--------|
| 1 | Domain/Entities/SalesInvoice.cs | Add CurrencyId, ExchangeRate, BaseCurrencyTotal, EnhancedDiscountType/DiscountValue, AdditionalCharges, Items list with batch allocation |
| 2 | Domain/Entities/SalesInvoiceLine.cs | Add ProductUnitId, CostPrice, ProfitAmount, BatchAllocation tracking, OfficialPrice, PriceOverrideApprovedBy, PriceOverrideReason |
| 3 | Domain/Entities/SalesReturn.cs | Add TaxAmount, DiscountAmount, TotalAmount, AdditionalCharges, CurrencyId, ExchangeRate, CashBoxId, RefundAmount, batch return tracking |
| 4 | Domain/Entities/SalesReturnItem.cs | Add ProductUnitId, UnitCost (at time of sale), BatchId for return |
| 5 | Contracts/DTOs/AllDtos.cs | Extend SalesInvoiceDto, SalesInvoiceLineDto, SalesReturnDto with new fields |
| 6 | Contracts/Responses/SalesInvoiceResponses.cs | Add currency, profit, enhanced fields |
| 7 | Contracts/Requests/SalesInvoiceRequests.cs | Add CurrencyId, ExchangeRate, price override fields, AdditionalCharges |
| 8 | SalesInvoiceConfiguration.cs | Add new column configs + FK |
| 9 | SalesInvoiceLineConfiguration.cs | Add new column configs |
| 10 | SalesInvoiceService.cs (Application) | Add: currency handling, 9-sale-scenario support, price override approval, auto-journal, auto-payment, enhanced profit calc, business rule enforcement |
| 11 | SalesReturnService.cs | Add: FIFO batch return, auto refund, discount on return, enhanced journal, full/partial return |
| 12 | SalesInvoicesController.cs | Add new endpoints (Quotation convert, batch allocation info, post with all-ops) |
| 13 | SalesInvoiceEditorViewModel.cs | Add: currency selector, price level picker, profit display, price override dialog, barcode scan mode, two-button (Save Draft + Save & Post) |
| 14 | SalesInvoiceListViewModel.cs | Add: draft management filter, profit column |
| 15 | SalesInvoiceEditorView.xaml | Add: currency field, price level selector, profit columns, two-button toolbar, compact restyle |
| 16 | SalesInvoicesListView.xaml | Add: profit column, draft badge |
| 17 | SalesInvoiceApiService.cs | Add new endpoints |
| 18 | ISalesInvoiceApiService.cs | Add new methods |
| 19 | App.xaml.cs (DesktopPWF) | DI registrations for new ViewModels, services + navigation entries |
| 20 | Program.cs (API) | DI registrations for new services |
| 21 | DbSeeder.cs | Seed SystemSettings for sales (ShowProfitInInvoice, AutoPostInvoices, PreventBelowRetailPrice, AllowBelowCostSale, etc.) |
| 22 | Messaging/Messages/AppMessages.cs | Add SalesQuotationChangedMessage |
| 23 | AccountingIntegrationService.cs | Add CreateSalesReturnEntryAsync, ReverseSalesReturnEntryAsync, DeliveryChargesRevenue account support |

---

## 3. BLOCKER Resolution — Critical Fixes

These 4 issues are blocking and must be resolved before Phase 28 implementation begins.

### 3.1 Blocker 1: CurrencyId + ExchangeRate Missing from SalesInvoice

**Problem**: Sales invoice currently has no concept of multi-currency. All prices are assumed in the base currency (configured in StoreSettings.CurrencyCode). Analysis requires:
- Sales price linked to (Product + Unit + Currency)
- ExchangeRate stored on invoice at time of transaction
- Auto-conversion from base currency if no price exists for selected currency
- Currency gain/loss tracking (need base-currency-equivalent amounts)

**Fix**:
1. Add int? CurrencyId FK and decimal ExchangeRate to SalesInvoice
2. Add decimal BaseCurrencyTotal total converted to base currency for COGS calculation
3. Store the exchange rate at time of invoice creation (not live rate)
4. When invoice is posted, convert all amounts to base currency for journal entries

**Files changed**: SalesInvoice.cs, SalesInvoiceConfiguration.cs, SalesInvoiceDto.cs, SalesInvoiceResponse.cs, SalesInvoiceService.cs, contracts, migrations

**Estimate**: ~1 hour

### 3.2 Blocker 2: Price Override Approval Workflow

**Problem**: Analysis specifies:
- User CAN modify sale price within the invoice (overrides default)
- Modifying price does NOT change the official product price
- When selling BELOW the minimum allowed price (PreventBelowRetailPrice setting), a warning/approval dialog must appear
- When selling BELOW cost (AllowBelowCostSale setting), a warning must appear
- Manager approval required for below-cost sales

**Fix**:
1. Add PriceOverrideApprovedBy and PriceOverrideReason to SalesInvoiceLine
2. Create PriceOverrideDialog styled WPF dialog
3. Add SystemSettings entries: PreventBelowRetailPrice (bool, default false), AllowBelowCostSale (bool, default true - warn but NEVER block)

**Files changed**: SalesInvoiceLine.cs, SalesInvoiceLineConfiguration.cs, SalesInvoiceService.cs, SalesInvoiceEditorViewModel.cs, SalesInvoiceEditorView.xaml, PriceOverrideDialog.xaml (NEW)

**Estimate**: ~2 hours

### 3.3 Blocker 3: Auto-Journal Entry Creation (Chart of Accounts Dependency)

**Problem**: Sales invoice posting must auto-create journal entries. The 9 sales scenarios require specific journal entry patterns. Accounts needed: Sales Revenue (1520), COGS, Inventory, Customer Receivables, Cash/Bank, VatOutput, Delivery Revenue (1533), Discount Allowed.

**Fix**:
1. Ensure AccountingIntegrationService has methods for ALL 9 sales scenarios
2. Create standard journal entry template for each scenario (see Section 5)
3. Run INSIDE the Post transaction (not separately)
4. Store JournalEntry reference on SalesInvoice: int? JournalEntryId

**Files changed**: SalesInvoiceService.cs, AccountingIntegrationService.cs, SalesInvoice.cs (add JournalEntryId)

**Estimate**: ~3 hours

### 3.4 Blocker 4: FIFO Batch Allocation on Sale (Phase 25 Dependency)

**Problem**: Analysis requires that when a sale is posted stock is deducted from FIFO batches (oldest batch first). If FEFO enabled and product has expiry date, deduct nearest-expiry batch first. COGS is computed from actual batch cost.

**Fix**:
1. Batch allocation must exist and return cost allocation per item
2. SalesInvoiceLine must store batch allocation records
3. InventoryService.DecreaseStockAsync() must accept batch selection strategy (FIFO/FEFO)
4. Return list of (batchId, quantity, unitCost) for COGS calculation

**Files changed**: InventoryService.cs, IInventoryService.cs, SalesInvoiceLine.cs (add batch allocations), SalesInvoiceService.cs (use batch allocation)

**Estimate**: ~3 hours

---

## 4. Design Catalog — Full Specs

### 4.1 SalesInvoice Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| Id | int PK | Auto-Increment | Yes | |
| InvoiceNo | int | | Yes | User-facing number (RULE-254), UNIQUE |
| CustomerId | int? FK | null | No | Default cash customer if null |
| WarehouseId | int FK | | Yes | Stock deduction warehouse |
| CashBoxId | int? FK | null | No | Required for cash/mixed payments |
| TaxId | int? FK | null | No | From Phase 19 |
| CurrencyId | int? FK | null | No | NEW: Multi-currency |
| JournalEntryId | int? FK | null | No | NEW: Auto-created on Post |
| InvoiceDate | datetime2 | UTC now | Yes | |
| DueDate | date? | null | No | For credit sales |
| PaymentType | byte | 1 (Cash) | Yes | 1=Cash, 2=Credit, 3=Mixed |
| ExchangeRate | decimal(18,6) | 1 | Yes | NEW: Rate at invoice creation |
| BaseCurrencyTotal | decimal(18,2) | 0 | Yes | NEW: Total in base currency |
| SubTotal | decimal(18,2) | 0 | Yes | Sum of line totals |
| DiscountType | byte? | null | No | NEW: 0=Amount, 1=Percent |
| DiscountValue | decimal(18,2)? | null | No | NEW: Raw discount input |
| DiscountAmount | decimal(18,2) | 0 | Yes | Computed from DiscountValue |
| AdditionalCharges | decimal(18,2) | 0 | No | NEW: delivery, service fees |
| TaxAmount | decimal(18,2) | 0 | Yes | |
| TotalAmount | decimal(18,2) | 0 | Yes | SubTotal - Discount + Tax + Charges |
| PaidAmount | decimal(18,2) | 0 | Yes | |
| DueAmount | decimal(18,2) | 0 | Yes | TotalAmount - PaidAmount |
| Status | byte | 1 (Draft) | Yes | 1=Draft, 2=Posted, 3=Cancelled |
| Notes | nvarchar(500)? | null | No | |
| TotalCost | decimal(18,2) | 0 | No | NEW: Sum of (qty x unit cost) from batch allocation |
| TotalProfit | decimal(18,2) | 0 | No | NEW: TotalAmount - TotalCost |
| CreatedByUserId | int? FK | | No | Audit (BaseEntity) |

### 4.2 SalesInvoiceLine Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| Id | int PK | Auto-Increment | Yes | |
| SalesInvoiceId | int FK | | Yes | Parent invoice |
| ProductId | int FK | | Yes | |
| ProductUnitId | int? FK | null | No | NEW: Unit used at sale time |
| Quantity | decimal(18,3) | | Yes | |
| UnitPrice | decimal(18,2) | | Yes | Actual sale price (may differ from official) |
| OfficialPrice | decimal(18,2)? | null | No | NEW: Official price at time of sale |
| DiscountAmount | decimal(18,2) | 0 | Yes | Line-level discount |
| LineTotal | decimal(18,2) | | Yes | = (Qty x UnitPrice) - Discount |
| Mode | byte | 1 (Retail) | Yes | 1=Retail, 2=Wholesale |
| PriceOverrideApprovedBy | int? FK | null | No | NEW: User who approved price override |
| PriceOverrideReason | nvarchar(200)? | null | No | NEW: Reason for price override |
| CostPrice | decimal(18,2)? | null | No | NEW: Total cost from batch allocation |
| UnitCost | decimal(18,2)? | null | No | NEW: Per-unit cost at time of sale |
| ProfitAmount | decimal(18,2)? | null | No | NEW: LineTotal - CostPrice |
| BatchAllocations | List<BatchAllocation> | | No | NEW: Allocation per batch |
| Notes | nvarchar(200)? | null | No | |

### 4.3 SalesQuotation Entity — NEW

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| Id | int PK | Auto-Increment | Yes | |
| QuotationNo | int | | Yes | User-facing number |
| CustomerId | int? FK | null | No | |
| WarehouseId | int FK | | Yes | Pricing warehouse |
| CurrencyId | int? FK | null | No | Multi-currency support |
| ExchangeRate | decimal(18,6) | 1 | Yes | |
| QuotationDate | datetime2 | UTC now | Yes | |
| ValidUntil | date? | null | No | Quotation expiry |
| Status | byte | 1 (Draft) | Yes | 1=Draft, 2=Sent, 3=Accepted, 4=Converted, 5=Rejected |
| SubTotal | decimal(18,2) | 0 | Yes | |
| DiscountAmount | decimal(18,2) | 0 | Yes | |
| TaxAmount | decimal(18,2) | 0 | Yes | |
| TotalAmount | decimal(18,2) | 0 | Yes | |
| Notes | nvarchar(500)? | null | No | |
| TermsAndConditions | nvarchar(2000)? | null | No | Quotation terms |
| ConvertedToInvoiceId | int? FK | null | No | NEW: If converted, link to SalesInvoice |
| CreatedByUserId | int? FK | | No | Audit |

### 4.4 SalesReturn Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| Id | int PK | Auto-Increment | Yes | |
| ReturnNo | string | | Yes | |
| SalesInvoiceId | int? FK | null | No | REQUIRED in V1 - no standalone returns |
| CustomerId | int? FK | null | Yes | |
| WarehouseId | int FK | | Yes | Return-to warehouse |
| CashBoxId | int? FK | null | No | For cash refund |
| CurrencyId | int? FK | null | No | Multi-currency |
| ExchangeRate | decimal(18,6) | 1 | Yes | |
| ReturnDate | datetime2 | UTC now | Yes | |
| Status | byte | 1 (Draft) | Yes | 1=Draft, 2=Posted, 3=Cancelled |
| SubTotal | decimal(18,2) | 0 | Yes | |
| DiscountAmount | decimal(18,2) | 0 | Yes | Proportional discount |
| TaxAmount | decimal(18,2) | 0 | Yes | Proportional tax reversal |
| AdditionalCharges | decimal(18,2) | 0 | No | Proportional charges reversal |
| TotalAmount | decimal(18,2) | 0 | Yes | |
| RefundAmount | decimal(18,2) | 0 | No | Amount refunded to customer |
| Notes | nvarchar(500)? | null | No | |
| ReturnReason | nvarchar(500)? | null | No | |

---

## 5. 9 Sales Scenarios with Exact Journal Entries

This section documents the complete accounting treatment for all 9 sales scenarios. Every scenario includes:
- What happens in the database tables
- The exact journal entries (Debit/Credit)
- Stock impact (FIFO batch allocation)
- Payment/Receipt impact

> IMPORTANT: Sales revenue uses a SINGLE account 1520 (Revenue). DO NOT split into Cash Sales and Credit Sales accounts. The distinction between cash/credit is captured via the debit side (CashBox vs Customer account) and via SalesInvoice.PaymentType field for reporting.

### Scenario 1 — Cash Sale

Example: Customer = , Product, Qty = 10, UnitPrice = 100, Total = 1000, Paid = 1000, CashBox = Main

**Draft phase**: Creates SalesInvoices (Status=Draft) + SalesInvoiceLines only. NO stock, NO journal, NO payments.

**Post phase** (one transaction):

| Step | Table | Action |
|------|-------|--------|
| 1 | SalesInvoices | Status -> Posted |
| 2 | InventoryTransactions | Create (Type = Sale) |
| 3 | InventoryTransactionLines | Record per line |
| 4 | InventoryBatches | FIFO allocation: oldest batch first. Deduct QuantityRemaining |
| 5 | WarehouseStocks | Decrease by quantity |
| 6 | JournalEntries | Create revenue entry |
| 7 | JournalEntries | Create COGS entry |
| 8 | CustomerReceipts | Create receipt (if needed) |
| 9 | CashTransactions | Create CashTransactionType.SalesIncome |

**Revenue JE**:
```
CashBox Account                    Dr 1000
    SalesRevenue (1520)                       Cr 1000
```

**COGS JE** (assuming cost = 700):
```
COGS                               Dr 700
    Inventory Asset                           Cr 700
```

### Scenario 2 — Credit Sale

Example: Total = 1000, Paid = 0, Due = 1000

**Stock impact**: Same as Scenario 1 (FIFO batch allocation, WarehouseStock decrease)

**Revenue JE**:
```
Customer Account (per-entity)      Dr 1000
    SalesRevenue (1520)                       Cr 1000
```

**COGS JE**: Same as Scenario 1.
**Customer balance**: Increased by 1000 (DueAmount)

### Scenario 3 — Mixed Sale

Example: Total = 1000, Paid = 300, Due = 700. Validation: PaidAmount <= NetTotal.

**Revenue JE**:
```
CashBox Account                    Dr 300
Customer Account                   Dr 700
    SalesRevenue (1520)                       Cr 1000
```

**COGS JE**: Same as Scenario 1.
**Cash Transaction**: CashTransactionType.SalesIncome for 300.
**Customer balance**: Increased by 700.

### Scenario 4 — Discount on Invoice

Example: Before discount = 1000, Discount = 100, Net = 900.

Key: Discount is recorded in a SEPARATE expense account (Discount Allowed), NOT netted against Sales Revenue.

**Revenue JE**:
```
CashBox Account                    Dr 900
Discount Allowed                   Dr 100
    SalesRevenue (1520)                       Cr 1000
```

**COGS JE**: Same as Scenario 1 (cost unaffected by discount).

### Scenario 5 — Tax (VAT)

Example: Sales = 1000, Tax = 150 (15%), Total = 1150.

**Revenue JE**:
```
CashBox Account                    Dr 1150
    SalesRevenue (1520)                       Cr 1000
    VatOutput                                  Cr 150
```

**COGS JE**: Same as Scenario 1 (cost unaffected by tax).

### Scenario 6 — Additional Charges (Delivery)

Example: Goods = 1000, Delivery = 50, Total = 1050.

Key: Additional charges are credited to a separate revenue account (DeliveryRevenue / 1533), NOT to SalesRevenue.

**Revenue JE**:
```
CashBox Account                    Dr 1050
    SalesRevenue (1520)                       Cr 1000
    DeliveryRevenue (1533)                     Cr 50
```

**COGS JE**: Same as Scenario 1 (cost unaffected by delivery charges).

### Scenario 7 — Customer Receipt

Example: Customer owes 5000, pays 2000. This is a post-invoice operation.

**JE**:
```
CashBox Account                    Dr 2000
    Customer Account                           Cr 2000
```

Tables: CustomerReceipts, CustomerReceiptApplications (optional: multi-invoice allocation), JournalEntries, CashTransactions (CashTransactionType.CustomerPayment).

### Scenario 8 — Full Sales Return

Example: Original invoice = 1000, Full return. Creates a SalesReturn document.

Tables affected: SalesReturns, SalesReturnItems, InventoryBatches (return to original batch), WarehouseStocks (increase), InventoryTransactions (SaleReturnIn), JournalEntries (revenue reversal + COGS reversal), CashTransactions (RefundOut if cash).

**Revenue Reversal JE**:
```
SalesReturns                       Dr 1000
    Customer or CashBox                        Cr 1000
```

**COGS Reversal JE** (cost = 700):
```
Inventory Asset                    Dr 700
    COGS                                       Cr 700
```

Refund: Cash -> RefundOut + reduce CashBox. Credit -> reduce DueAmount. Mixed -> proportional.

### Scenario 9 — Partial Sales Return

Example: Original = 10 units, Return = 2 units. Proportional calculation: returnRatio = 2/10 = 20%.

Revenue reversal = LineTotal x returnRatio. Cost reversal = UnitCost x returnQty. Discount/Tax/Charges reversed proportionally.

**Revenue Reversal JE** (proportional):
```
SalesReturns                       Dr (LineTotal x ratio)
    Customer or CashBox                        Cr (same)
```

**COGS Reversal JE** (proportional):
```
Inventory Asset                    Dr (UnitCost x returnQty)
    COGS                                       Cr (same)
```

### Summary: Tables Affected Per Scenario

| Table | S1 Cash | S2 Credit | S3 Mixed | S4 Discount | S5 Tax | S6 Charges | S7 Receipt | S8 Full Return | S9 Partial Return |
|-------|:-------:|:---------:|:--------:|:-----------:|:------:|:----------:|:----------:|:--------------:|:-----------------:|
| SalesInvoices | Yes | Yes | Yes | Yes | Yes | Yes | - | - | - |
| SalesInvoiceLines | Yes | Yes | Yes | Yes | Yes | Yes | - | - | - |
| SalesReturns | - | - | - | - | - | - | - | Yes | Yes |
| SalesReturnItems | - | - | - | - | - | - | - | Yes | Yes |
| InventoryTransactions | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| InventoryTransactionLines | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| InventoryBatches (FIFO) | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| WarehouseStocks | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| JournalEntries (Revenue) | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| JournalEntries (COGS) | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| CustomerBalance | - | Yes | Yes | - | - | - | Yes | Yes | Yes |
| CustomerReceipts | Yes | - | Yes | Yes | Yes | Yes | Yes | - | - |
| CustomerReceiptApplications | opt | - | opt | - | - | - | Yes | - | - |
| CashTransactions | Yes | - | Yes | Yes | Yes | Yes | Yes | Yes | Yes |

---

## 6. Business Rules & Error Prevention

The following business rules MUST be enforced at BOTH the Desktop UI level AND the API/Service level.

### 6.1 Sales Invoice Business Rules

| # | Rule | UI Enforcement | Backend Enforcement |
|---|------|---------------|-------------------|
| BR-01 | Prevent qty > available stock | Show warning: quantity insufficient | On Post, re-validate stock. Return Result.Failure |
| BR-02 | Customer required for credit sales | If PaymentType=Credit, CustomerId must be set | Return Result.Failure if Credit + no CustomerId |
| BR-03 | CashBox required for cash sales | If Cash/Mixed, CashBoxId must be set | Return Result.Failure if Cash + no CashBoxId |
| BR-04 | PaidAmount <= NetTotal for mixed | Block if PaidAmount > TotalAmount | Validate PaidAmount <= TotalAmount |
| BR-05 | No negative values | Block negative Qty/Price/Discount | Validate all values > 0 |
| BR-06 | No empty invoice | Disable Post if no items | Return Result.Failure if no items |
| BR-07 | No posting non-Draft | N/A | Guard: Status must be Draft |
| BR-08 | No editing Posted/Cancelled | Read-only mode if not Draft | Guard in domain methods |
| BR-09 | No hard-deleting Posted | Show Cancel not Delete | CancelAsync sets Status=Cancelled |
| BR-10 | Price override requires approval | Show PriceOverrideDialog | Check PriceOverrideApprovedBy on Post |

### 6.2 Stock Integrity Rules

| # | Rule | Enforcement |
|---|------|------------|
| BR-S1 | No negative stock (Quantity >= 0) | DB CHECK constraint on WarehouseStocks |
| BR-S2 | Batch allocation uses FIFO/FEFO | InventoryService.DecreaseStockWithBatchAllocationAsync |
| BR-S3 | Batch quantity never negative | Guard in InventoryBatch.DeductQuantity |
| BR-S4 | No transfer to same warehouse | Service validation: sourceId != destinationId |

### 6.3 Sales Return Rules

| # | Rule | Enforcement |
|---|------|------------|
| BR-R1 | Return MUST be linked to original invoice | SalesInvoiceId is REQUIRED in V1 |
| BR-R2 | Return window shows only original invoice products | Load original invoice lines, show them |
| BR-R3 | Cannot return more than remaining qty | Show Sold - PreviouslyReturned = Available. Block excess |
| BR-R4 | Cannot return items not in original invoice | Only original invoice products selectable |
| BR-R5 | Return reversal is proportional | Discount/Tax/Charges reversed by return ratio |
| BR-R6 | Goods return to original batch | ReturnedBatchId = batch goods were sold from |

### 6.4 Programming Rules

| # | Rule | Rationale |
|---|------|-----------|
| BR-P1 | Every Post/Cancel in ONE transaction | All or nothing |
| BR-P2 | Backend re-validates EVERYTHING | Never trust UI alone |
| BR-P3 | Optimistic concurrency on documents | Use RowVersion on SalesInvoice |
| BR-P4 | Historical data NEVER modified | Posted amounts immutable |
| BR-P5 | NO Hard Delete except Drafts | Posted = Cancelled only |
| BR-P6 | Document number is reserved | InvoiceNo NEVER reused |
| BR-P7 | Status is single source of truth | Check Status, not JournalEntry existence |
| BR-P8 | Real-time stock on unit change | Update available stock display immediately |

---

## 7. Draft/Post/Cancel Architecture

This section documents the two-button strategy (Save Draft / Save & Post) and complete lifecycle management.

### 7.1 Two-Button Strategy

| Button | Keyboard | When to Use | What Happens |
|--------|----------|-------------|-------------|
| Save Draft (save draft) | Ctrl+S | Negotiating with customer, items not final | SalesInvoices + SalesInvoiceLines only. Status=Draft. NO stock/accounting/payment |
| Save & Post (save and post) | Ctrl+P | Customer confirmed, payment settled | ALL steps in one transaction |

### 7.2 Draft Phase

```
User clicks Save Draft
    |
    v
1. Validate: Items.Count > 0, no negative values, customer for credit, cashbox for cash
2. Save SalesInvoice (Status=Draft, InvoiceNo=auto-gen)
3. Save SalesInvoiceLines (all items + amounts)
4. Commit

NO InventoryTransaction, NO Batch changes, NO Stock changes
NO JournalEntries, NO Balance changes, NO CashTransactions
```

### 7.3 Post Phase — Complete Flow

```
User clicks Save & Post
    |
    v
Pre-Validation (BEFORE transaction):
- Items.Count > 0, Customer for Credit, CashBox for Cash/Mixed
- PaidAmount <= TotalAmount, Stock available for ALL items
- Price override approvals, Credit limit check (if Credit)
    |
    v (if any fail -> return error, NO transaction opened)
BEGIN TRANSACTION
1. Save SalesInvoice (gets Id, InvoiceNo)
2. Save SalesInvoiceLines
3. Set Status = Posted
4. Create InventoryTransaction (Type = Sale)
5. Create InventoryTransactionLines (per invoice line)
6. FIFO Batch Allocation:
   - For each line: get available batches (oldest first), deduct QuantityRemaining
   - Record batch allocations, compute UnitCost from batch
7. Update WarehouseStocks (decrease by total qty)
8. Journal Entry #1 (Revenue):
   - Dr CashBox/Customer, Dr DiscountAllowed (if any)
   - Cr SalesRevenue, Cr VatOutput (if tax), Cr DeliveryRevenue (if charges)
9. Journal Entry #2 (COGS):
   - Dr COGS, Cr Inventory
10. Update Customer Balance (DueAmount added if Credit)
11. Create CustomerReceipt (if PaidAmount > 0)
12. Create CustomerReceiptApplication (optional, link to invoice)
13. Create CashTransaction (Type=SalesIncome, if Cash/Mixed)
    |
    v
COMMIT TRANSACTION
```

Any step fails -> ROLLBACK entire transaction.

### 7.4 Cancel Flow

```
User clicks Cancel on Posted invoice
    |
    v
PRE-CANCEL CHECKS:
- Invoice has linked returns? -> BLOCK: cannot cancel invoice with returns
- Invoice has partial payments? -> BLOCK: cannot cancel invoice with payments
    |
    v
BEGIN TRANSACTION
1. Create REVERSAL Journal Entry: Dr SalesRevenue, Cr CashBox/Customer
2. Create REVERSAL COGS Entry: Dr Inventory, Cr COGS
3. Restore WarehouseStocks + InventoryBatches
4. Reverse CustomerBalance (decrease DueAmount)
5. Create CashTransaction (Type=RefundOut, if CashBox was credited)
6. Set Status = Cancelled (3)
7. InvoiceNo is RESERVED (never reused)
    |
    v
COMMIT TRANSACTION
```

**CANCELLED STATE IS TERMINAL**: No editing, no re-posting, no further cancellations. Used ONLY when invoice was created by mistake.

---

## 8. Cancel vs Return Distinction

This is a CRITICAL architectural decision. Cancel and Return are NOT the same thing.

### 8.1 When to Use Cancel

| Criteria | Detail |
|----------|--------|
| When | Invoice was created by mistake (wrong customer, wrong items, duplicate) |
| Timeframe | Typically immediate (within minutes/hours) |
| Goods status | Goods were NEVER actually delivered or are still in warehouse |
| Accounting | Full reversal of all journal entries (Dr/Cr swapped) |
| Document | No new document - original invoice marked Cancelled |
| InvoiceNo | Reserved - never reused |

**Pre-conditions for Cancel**:
- BLOCK if invoice has linked SalesReturns
- BLOCK if invoice has partial payments via CustomerReceipts
- BLOCK if invoice is already Cancelled (terminal state)
- Draft can be hard-deleted instead

### 8.2 When to Use Return

| Criteria | Detail |
|----------|--------|
| When | Goods were actually delivered and customer returns them |
| Timeframe | Can be days/weeks after original sale |
| Goods status | Customer physically returns goods |
| Accounting | Creates new SalesReturn document with proportional reversals |
| Document | New SalesReturn document (separate numbering sequence) |
| Stock | Returned to the original batch (maintains FIFO integrity) |

Return always creates a new SalesReturn document - it does NOT modify the original invoice's Cancelled status.

### 8.3 UI Implications

| Action | Button Text | Available When |
|--------|-------------|----------------|
| Delete Draft | Delete Draft | Status=Draft (permanent delete) |
| Cancel Posted | Cancel Invoice | Status=Posted, NO returns, NO partial payments |
| Create Return | Sales Return | Status=Posted, ANYTIME (even with partial payments) |

### 8.4 Decision Matrix

```
Is the invoice newly created by mistake?
    YES -> Can the invoice be cancelled?
        YES (no returns, no payments) -> Use CANCEL
        NO (has returns or payments) -> Use RETURN for remaining items
    NO -> Goods actually delivered and customer returning them?
        YES -> Use RETURN
        NO -> No action needed
```

---

## 9. Basic vs Advanced UI Mode

Following the analysis from Accounts.md, the system supports two UI modes per user.

### 9.1 Basic Mode

Basic users see operational screens only - no accounting terminology:

**Visible**: Sales, Purchases, Products, Inventory, Customers, Suppliers, CashBoxes

**Hidden**: Journal Entries, Chart of Accounts, Trial Balance, Income Statement, Receipt Vouchers (accounting), Payment Vouchers (accounting)

**Sales screen for Basic user**: Customer, Product, Quantity, Price, Payment (Cash/Credit/Mixed only - NO accounting terms)

### 9.2 Advanced Mode

Advanced users see BOTH operational AND accounting screens.

**Additional screens**: Chart of Accounts, Journal Entries, Receipt Vouchers, Payment Vouchers, Account Statement, Trial Balance, Income Statement, Balance Sheet

### 9.3 Implementation

| Mechanism | Detail |
|-----------|--------|
| Per-user setting | User.DisplayMode enum: Basic=0, Advanced=1 |
| XAML visibility | Visibility="{Binding IsAdvancedMode, Converter=...}" on accounting screens |
| Navigation guard | CanNavigate() checks CurrentUser.DisplayMode |
| Default | Cashier/Sales/Warehouse -> Basic. Admin/Manager/Accountant -> Advanced |

### 9.4 Default Cash Customer

| Feature | Detail |
|---------|--------|
| Seeded on setup | Default customer created with auto-generated account under 1130 |
| Auto-selection | When PaymentType=Cash and no customer selected, default cash customer auto-assigned |
| Reports | Cash sales filterable by CustomerId OR PaymentType=Cash |
| Account | Has its own GL account for balance tracking |

---

## 10. Gap Analysis

### 10.1 Sales Invoice

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| CurrencyId + ExchangeRate | Missing | Required | ADD to entity + DTO + service |
| Multiple price levels per unit | Single default price | Show all available prices | ADD price picker |
| Price override with approval | No approval flow | Manager approval below min | ADD PriceOverrideDialog |
| Enhanced profit display | Basic total only | Per-item cost + profit | ADD cost/profit fields |
| Auto-journal entries on Post | Phase 24 | 9 scenarios | ENHANCE AccountingIntegrationService |
| Auto-customer receipt on cash | Phase 24 | Create on Post | VERIFY existing implementation |
| DiscountType (Amount/Percent) | Amount only | Both types | ADD fields |
| Additional charges | Missing | Separate revenue account | ADD field + DeliveryRevenue account |
| Draft management screen | Filter only | Standalone list | ADD ViewModel + View |
| TotalCost + TotalProfit | Missing | Show total profit | ADD compute on Post |
| Batch allocation tracking | Missing | FIFO audit trail | ADD batch records |
| Barcode continuous scan | Single scan | POS-style continuous | ADD BarcodeScannerControl |
| 9 sales scenarios | Partial | All 9 with exact JE | ENHANCE PostAsync |
| Cancel vs Return | Unclear | Clear separation | ADD pre-cancel checks |
| Basic vs Advanced UI | Single UI | Dual-mode | ADD DisplayMode filtering |
| Business rules (BR-01 to BR-10) | Partial | Complete set | ADD enforcement |
| Two-button strategy | Single action | Draft + Post | ADD workflow |

### 10.2 Sales Quotation

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Quotation entity + CRUD | Missing | NEW | CREATE from scratch |
| Quotation to Invoice conversion | Missing | NEW | ADD ConvertToInvoice method |
| Quotation lifecycle (5 states) | Missing | NEW | ADD enum + state machine |
| Valid until date | Missing | NEW | ADD date field |
| Desktop screens | Missing | NEW | CREATE List + Editor |

### 10.3 Sales Return

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| FIFO batch return | No batch tracking | Return to same batches | ADD ReturnedBatchId |
| Discount/Tax/Charges on return | Missing | Proportional reversal | ADD proportional calculation |
| Auto customer refund | Missing | Auto create refund payment | ADD CashBoxId, RefundAmount |
| Return reason | Missing | User-facing | ADD ReturnReason |
| Linked to invoice | Optional | REQUIRED in V1 | ENHANCE validation |
| Load original invoice items | Missing | Show original items | ADD UI + service |

---

## 11. Architectural Decisions

### 11.1 Price Override Rules

| Scenario | Behaviour |
|----------|-----------|
| Price = official price | No warning, no approval |
| Price < official but > cost | Warning: Price is below official price (user acknowledges) |
| Price < cost | Warning + Manager approval required (PriceOverrideDialog with reason) |
| PreventBelowRetailPrice = true | Block price decrease entirely unless manager override |
| AllowBelowCostSale = false | WARNING only - NEVER block. Log warning, show dialog, allow to proceed |

### 11.2 Quotation Lifecycle

```
Draft (1) -> Sent (2) -> Accepted (3) -> Converted (4)
                     -> Rejected (5)
                     -> Expired (6) [auto when ValidUntil passed]
```

Auto-Expiry: Before ANY transition, check ValidUntil. If expired, mark as Expired and block conversion.

### 11.3 Batch Allocation Strategy

Batch allocation occurs at Post time (not at item-add time):
1. Draft: NO batch allocation
2. Post: For each item -> call InventoryService.DecreaseStockWithBatchAllocationAsync() -> get (batchId, qty, unitCost) -> store allocations -> compute UnitCost and ProfitAmount

### 11.4 Real-Time Profit Calculation

Controlled by SystemSetting ShowProfitInInvoice:
- Enabled: Show green/red profit per item + invoice total profit
- Disabled: Profit hidden from sales screen, visible in reports only

### 11.5 Two Buttons (Not Separate Posting Screen)

Following Invoices Details.md analysis: V1 uses Save Draft and Save & Post buttons. This keeps UI simple for small retail shops. Not practical to force Save then Post for every invoice.

### 11.6 Sales Return Customer Refund

| Original Payment | Refund Behaviour |
|-----------------|------------------|
| Cash | Auto-create CashTransaction.RefundOut + reduce CashBox |
| Credit | Reduce Customer balance (DueAmount decreases) |
| Mixed | Cash refund for paid portion + reduce balance for unpaid |

### 11.7 Customer Credit Limit Enforcement

1. Pre-validate BEFORE opening Post transaction
2. If projectedBalance > creditLimit and user is Cashier -> BLOCK
3. If projectedBalance > creditLimit and user is Manager/Admin -> warning but ALLOW

### 11.8 Single Sales Revenue Account

Following Accounts.md analysis: Sales revenue uses SINGLE account 1520. NOT split into Cash Sales (1521) and Credit Sales (1522). PaymentType field on SalesInvoice captures cash/credit/mixed. Reports filter by PaymentType.

### 11.9 Why INotifyDataErrorInfo

Per RULE-228/229: ClearAllErrors() + AddError() + ValidateAllAsync() on Pre-Post. Save always enabled (no CanExecute). Show validation dialog listing ALL errors before Post.

---

## 12. Non-V1 Items (Deferred)

| Feature | Reason |
|---------|--------|
| Customer loyalty/rewards system | Requires customer analytics module |
| Gift cards | Separate payment instrument |
| Mobile POS (Android/iOS) | Platform-specific |
| E-invoice integration (ZATCA/FATOORA) | Government API integration |
| Online store / e-commerce sync | External integration |
| Digital signature on invoice | Security feature |
| Recurring invoices / subscriptions | Scheduling engine |
| Multi-branch / multi-store | Branch entity + separate inventory |
| Offline mode / sync | Local queue + conflict resolution |
| Quotation print | Print module enhancement later |
| Price list by customer group | Customer group entity |
| Standalone Sales Returns (not linked to invoice) | V1 requires SalesInvoiceId |

---
## 13. Implementation Tasks

All tasks include:
- Logging: Log.Information / LogSystemError (RULE-199)
- Error handling: Result pattern (RULE-006), catch DbUpdateException (RULE-200)
- ToolTips: Arabic on ALL interactive controls (RULE-185-190)
- UI Compact: No hardcoded Height=36/40, style defaults at 28px (RULE-262-274)
- Validation: INotifyDataErrorInfo (RULE-228-229), Save always enabled (RULE-059)
- Arabic strings: Valid UTF-8 encoding (RULE-249-253)
- Business Rules: BR-01 through BR-10 at BOTH UI and Backend

**Total estimate**: ~50 hours

### Task 1 — Add CurrencyId + ExchangeRate to SalesInvoice & Item

Files: SalesInvoice.cs, SalesInvoiceLine.cs, SalesInvoiceConfiguration.cs, SalesInvoiceLineConfiguration.cs, Migrations, DTOs, Responses, Requests, SalesInvoiceService.cs, SalesInvoiceApiService.cs

**Estimate**: ~2 hours

### Task 2 — Enhanced Price Selection (Multiple Price Levels per Unit)

When user selects product + unit, show all available prices for that combination and auto-select the active one.

Files: AllDtos.cs, ProductApiService.cs, SalesInvoiceEditorViewModel.cs, SalesInvoiceEditorView.xaml, ProductsController.cs

**Estimate**: ~2 hours

### Task 3 — Price Override with Manager Approval

Files: SalesInvoiceLine.cs (+ SetPriceOverride), SalesInvoiceLineConfiguration.cs, AllDtos.cs, PriceOverrideDialog.xaml (NEW) + .cs, SalesInvoiceEditorViewModel.cs, SalesInvoiceService.cs, IDialogService.cs, DialogService.cs

**Estimate**: ~3 hours

### Task 4 — Enhanced Profit Display (Per-Item + Total)

Files: SalesInvoice.cs (+ TotalCost/TotalProfit), SalesInvoiceLine.cs (+ UnitCost/ProfitAmount), SalesInvoiceService.cs, AllDtos.cs, SalesInvoiceEditorViewModel.cs, SalesInvoiceEditorView.xaml

**Estimate**: ~2 hours

### Task 5 — Auto-Journal Entry Creation on Sales Post (9 Scenarios)

Implement ALL 9 sales scenario journal entries in the Post transaction.

Files: SalesInvoice.cs (+ JournalEntryId), AccountingIntegrationService.cs (ENHANCE with all 9 scenarios), SalesInvoiceService.cs (call accounting service), SalesInvoiceConfiguration.cs (FK)

**The 9 scenarios handled**:

| Scenario | Entry #1 (Revenue) | Entry #2 (COGS) |
|----------|-------------------|------------------|
| 1. Cash | Dr CashBox / Cr SalesRevenue | Dr COGS / Cr Inventory |
| 2. Credit | Dr Customer / Cr SalesRevenue | Dr COGS / Cr Inventory |
| 3. Mixed | Dr CashBox+Dr Customer / Cr SalesRevenue | Dr COGS / Cr Inventory |
| 4. Discount | Dr CashBox+Dr DiscountAllowed / Cr SalesRevenue | Dr COGS / Cr Inventory |
| 5. Tax | Dr CashBox / Cr SalesRevenue+Cr VatOutput | Dr COGS / Cr Inventory |
| 6. Charges | Dr CashBox / Cr SalesRevenue+Cr DeliveryRevenue | Dr COGS / Cr Inventory |
| 7. Receipt | Dr CashBox / Cr Customer (separate op) | - |
| 8. Full Return | Dr SalesReturns / Cr Customer(or CashBox) | Dr Inventory / Cr COGS |
| 9. Partial Return | Dr SalesReturns (prop) / Cr Customer | Dr Inventory / Cr COGS |

Ensure SystemAccountKey.DeliveryChargesRevenue = 21 mapped to account 1533 in AccountingSeeder.

**Estimate**: ~4 hours

---

### Task 6 — Sales Return Enhancement (FIFO Batch Return, Discount, Auto Refund, Proportional Reversal)

Sales return now ALWAYS linked to original invoice (SalesInvoiceId is REQUIRED):
1. User selects original invoice
2. System loads original items showing: Product, Qty Sold, Qty Previously Returned, Qty Available for Return
3. User enters return qty for each item (cannot exceed available)
4. On Post: Proportional calculation - returnRatio = returnQty / originalQty
   - Revenue reversal = originalLineTotal x returnRatio
   - Cost reversal = originalUnitCost x returnQty
   - Discount reversal = originalDiscount x returnRatio
   - Tax reversal = originalTaxAmount x returnRatio
   - Charges reversal = originalAdditionalCharges x returnRatio

Files: SalesReturn.cs (+ fields), SalesReturnItem.cs (+ fields), Configurations, DTOs, Requests, SalesReturnService.cs (+ proportional logic, batch return, refund), SalesReturnEditorViewModel.cs (+ load original invoice), SalesReturnEditorView.xaml

**Estimate**: ~5 hours

### Task 7 — Auto Customer Receipt Creation on Cash/Mixed Sales

When PaidAmount > 0: Create CustomerReceipt + optional CustomerReceiptApplication + CashTransaction.

Files: ICustomerPaymentService.cs, CustomerPaymentService.cs, SalesInvoiceService.cs (call inside Post transaction)

**Estimate**: ~2 hours

### Task 8 — Sales Quotation Entity + CRUD (NEW Module)

Full CRUD for quotations. ConvertToInvoice creates SalesInvoice from quote.

Files: SalesQuotation.cs, SalesQuotationItem.cs, QuotationStatus.cs, Configurations, Migrations, DTOs, Requests, ISalesQuotationService.cs, SalesQuotationService.cs, SalesQuotationsController.cs, SalesQuotationApiService.cs, List VMs/Views, Editor VMs/Views, AppMessages.cs, DI registrations

**Estimate**: ~6 hours

### Task 9 — Barcode Continuous Scan Mode for Fast Entry

Continuous scan with auto-focus, auto-add, quantity prefix, sound feedback.

Files: BarcodeScannerControl.xaml + .cs (NEW), SalesInvoiceEditorViewModel.cs (integrate), BarcodeInputService.cs, SalesInvoiceEditorView.xaml, ISoundService.cs

**Estimate**: ~3 hours

### Task 10 — Business Rules Enforcement (UI + Backend)

Implement ALL business rules from Section 6.

Files: SalesInvoiceEditorViewModel.cs (BR-01 to BR-10), SalesInvoiceEditorView.xaml (real-time stock on unit change), SalesInvoiceService.cs (backend re-validation), SalesReturnService.cs (BR-R1 to BR-R6), Validators

Real-time stock display on unit change:
- When user selects Product -> show qty in base unit
- When user changes Unit -> convert and show qty in selected unit
- Example: 100 pieces available, Carton = 24 pieces. Piece -> "Available: 100". Carton -> "Available: 4"

**Estimate**: ~4 hours

### Task 11 — Two-Button Strategy (Save Draft + Save & Post)

Implement the two-button workflow per Invoices Details.md analysis.

Files: SalesInvoiceEditorViewModel.cs (+ SaveDraftCommand + SavePostCommand), SalesInvoiceEditorView.xaml (two buttons with distinct styling + ToolTips), SalesInvoiceService.cs (Ensure PostAsync does ALL ops, CreateDraftAsync only saves invoice+lines)

ToolTips:
- Save Draft: Save as Draft - does not affect stock or accounting
- Save & Post: Save and Post - stock will be deducted, customer balance updated, journal entries and payments created

**Estimate**: ~3 hours

### Task 12 — Cancel vs Return Distinction

Implement pre-cancel checks. Block cancel when returns/payments exist.

Files: SalesInvoiceService.cs (+ pre-cancel validation with checks for linked returns and payments), SalesInvoiceListViewModel.cs (different action buttons), SalesInvoiceEditorViewModel.cs (hide edit, show cancel for Posted), SalesInvoiceApiService.cs

Pre-cancel flow: If linked returns exist -> failure. If payments exist -> failure.

**Estimate**: ~2 hours

### Task 13 — Basic vs Advanced UI Mode

Implement dual-mode UI per Section 9.

Files: User.cs (+ DisplayMode), MainViewModel.cs (+ IsAdvancedMode), MainWindow.xaml (visibility on accounting screens), LoginViewModel.cs (load DisplayMode), SessionService.cs (expose IsAdvancedMode), UsersController.cs

**Estimate**: ~3 hours

### Task 14 — Enhanced SalesInvoiceEditorViewModel

Integrate ALL new features into the existing 1486-line editor ViewModel.

Changes: Currency selector, price level picker, profit display, price override, barcode scanner, additional charges, DiscountType, two buttons (Save Draft + Save & Post), business rules (BR-01 to BR-10), enhanced validation, draft age display, credit limit enforcement, real-time stock on unit change, auto-focus

**Estimate**: ~5 hours

---

### Task 15 — Sales Quotation Conversion to Invoice (ConvertToInvoice)

When user converts quotation to invoice:
1. Load quotation + items
2. Create SalesInvoice pre-populated from quotation data
3. If quotation is linked to a customer, pre-select that customer
4. Keep original quotation as Converted status
5. Allow price editing before Post (standard SalesInvoiceEditor opens)

Files: ISalesQuotationService.cs (+ ConvertToInvoiceAsync), SalesQuotationService.cs, SalesQuotationEditorViewModel.cs (+ ConvertCommand), SalesQuotationEditorView.xaml

**Estimate**: ~3 hours

### Task 16 — DiscountType Support (Amount / Percentage)

Add DiscountType field (Amount=0, Percent=1) to SalesInvoiceLine.
- DiscountType=Percent: DiscountAmount = (Qty * UnitPrice) * DiscountPercent / 100
- DiscountType=Amount: DiscountAmount = DiscountPercent (stored as fixed value in DiscountAmount, DiscountPercent stores the raw value)

Files: SalesInvoiceLine.cs (+ DiscountType), AllDtos.cs, AllValidators.cs, Configurations, SalesInvoiceEditorViewModel.cs (+ toggle logic), SalesInvoiceEditorView.xaml (percentage/amount toggle UI)

**Estimate**: ~1 hour

### Task 17 — Draft Management Screen

New screen for draft-only invoices with: Cancel All (mass cancel), Resume Editing (open editor in Draft mode), Delete All Old (purge drafts older than N days).

Files: SalesDraftListViewModel.cs (NEW), SalesDraftListView.xaml (NEW), MainViewModel.cs (navigation entry)

**Estimate**: ~2 hours

### Task 18 — Additional Charges Field (Delivery/Service Charges)

Add OtherCharges field to SalesInvoice for delivery/service fees. Seeded to separate revenue account DeliveryChargesRevenue (1533).

Files: SalesInvoice.cs (+ OtherCharges) — already exists, verify. SalesInvoiceService.cs (include in journal entry), SalesInvoiceEditorViewModel.cs (+ field), SalesInvoiceEditorView.xaml (+ UI)

**Estimate**: ~1 hour

### Task 19 — Enhanced Fluent Validation for ALL Request DTOs

Replace/add FluentValidators for ALL sales-related DTOs.

Files: AllDtos.cs validators, CreateSalesInvoiceRequestValidator, UpdateSalesInvoiceRequestValidator, CreateSalesReturnRequestValidator, etc.

**Estimate**: ~2 hours

### Task 20 — Sales Quotation Print + Reports

Add SalesQuotation to PrintController and reports (quotation list with expiry status, quotation aging).

Files: IPrintService.cs, InvoicePrintDtoBuilder.cs (+ BuildFromQuotationAsync), ReportsService.cs (quotation-specific), PrintController.cs

**Estimate**: ~2 hours

### Task 21 — Unit Tests for ALL Sales Operations

| Test | Scope | Assertions |
|------|-------|-----------|
| SalesInvoice.Post.Cash | Service | Stock deducted, JE created, CashTx created, Status=Posted |
| SalesInvoice.Post.Credit | Service | Stock deducted, JE created, Customer balance increased, No CashTx |
| SalesInvoice.Post.Mixed | Service | Stock deducted, JE created, CashTx for paid portion, Customer balance for unpaid |
| SalesInvoice.Post.WithDiscount | Service | JE has Dr DiscountAllowed |
| SalesInvoice.Post.WithTax | Service | JE has Cr VatOutput |
| SalesInvoice.Post.WithCharges | Service | JE has Cr DeliveryChargesRevenue |
| SalesInvoice.Post.InsufficientStock | Service | Result.Failure, no changes |
| SalesInvoice.Post.InvoiceItemTotal | Service | Status=InvoiceItemTotal=Qty*Price-Discount |
| SalesInvoice.Cancel.Posted | Service | No returns, no payments: reversal JE, stock restored, Status=Cancelled |
| SalesInvoice.Cancel.BlockedReturn | Service | Has returns -> Result.Failure, no changes |
| SalesInvoice.Cancel.BlockedPayment | Service | Has payments -> Result.Failure, no changes |
| SalesInvoice.Draft.Save | Service | Status=Draft, no JE, no stock changes |
| SalesQuotation.Create | Service | Status=Draft |
| SalesQuotation.ConvertToInvoice | Service | Invoice created, Quotation=Converted |
| SalesReturn.Post.LinkedToInvoice | Service | Proportional reversal, batch return, CashTx refund |
| PriceOverride.Required | UI | Dialog shown when price < cost + manager approval needed |
| BruteForce | Infrastructure | LoginPolicy rate limit (5/15min per IP) |

**Estimate**: ~4 hours

---

## 14. API Endpoints

### 14.1 Sales Invoice Endpoints

```text
GET    /api/v1/sales?search=&page=&pageSize=&from=&to=&status=&customerId=
        → PaginatedResult<SalesInvoiceDto>

GET    /api/v1/sales/drafts
        → List<SalesInvoiceDto> (only Status=Draft)

GET    /api/v1/sales/{id:int}
        → SalesInvoiceDto

GET    /api/v1/sales/{id:int}/items
        → List<SalesInvoiceLineDto>

GET    /api/v1/sales/by-invoice-no/{invoiceNo:int}
        → SalesInvoiceDto (unique per document type)

POST   /api/v1/sales/draft
        Body: CreateSalesInvoiceRequest (Status forced to Draft)
        → Result<SalesInvoiceDto>

POST   /api/v1/sales/post
        Body: CreateSalesInvoiceRequest (full details)
        → Result<SalesInvoiceDto> — creates invoice + stock + accounting in ONE transaction

PUT    /api/v1/sales/{id:int}
        Body: UpdateSalesInvoiceRequest
        → Result<SalesInvoiceDto> — ONLY allowed for Draft

POST   /api/v1/sales/{id:int}/post
        → Result — posts existing draft

PUT    /api/v1/sales/{id:int}/items
        Body: List<UpdateSalesInvoiceLineRequest>
        → Result — batch update of lines (Draft only)

POST   /api/v1/sales/{id:int}/cancel
        Query: reason (string?)
        → Result — reverses stock + accounting + payments (only if no returns/payments)

DELETE /api/v1/sales/{id:int}
        → Result — hard delete (Draft only)

DELETE /api/v1/sales/permanent/{id:int}
        → Result — force hard delete with FK validation (Draft only)
```

### 14.2 Sales Quotation Endpoints

```text
GET    /api/v1/sales-quotations?search=&status=&customerId=
        → PaginatedResult<SalesQuotationDto>

GET    /api/v1/sales-quotations/{id:int}
        → SalesQuotationDto

POST   /api/v1/sales-quotations
        Body: CreateSalesQuotationRequest
        → Result<SalesQuotationDto>

PUT    /api/v1/sales-quotations/{id:int}
        Body: UpdateSalesQuotationRequest
        → Result<SalesQuotationDto>

PUT    /api/v1/sales-quotations/{id:int}/sent
        → Result (Status=Sent)

PUT    /api/v1/sales-quotations/{id:int}/accept
        → Result (Status=Accepted)

PUT    /api/v1/sales-quotations/{id:int}/reject
        Body: { rejectionReason: string? }
        → Result (Status=Rejected)

POST   /api/v1/sales-quotations/{id:int}/convert-to-invoice
        → Result<SalesInvoiceDto> (creates SalesInvoice from quotation)

DELETE /api/v1/sales-quotations/{id:int}
        → Result (soft delete: IsActive=false, only Draft/Sent)
```

### 14.3 Sales Return Endpoints

```text
GET    /api/v1/sales-returns?search=&from=&to=&invoiceId=&customerId=
        → PaginatedResult<SalesReturnDto>

GET    /api/v1/sales-returns/{id:int}
        → SalesReturnDto

GET    /api/v1/sales-returns/by-invoice/{invoiceId:int}
        → List<SalesReturnDto>

GET    /api/v1/sales-returns/returned-quantities/{invoiceId:int}
        → Dictionary<int, decimal> (ProductId → total returned qty)

POST   /api/v1/sales-returns
        Body: CreateSalesReturnRequest (SalesInvoiceId REQUIRED)
        → Result<SalesReturnDto> — creates return + batch restoration + refund

POST   /api/v1/sales-returns/{id:int}/post
        → Result — posts existing draft return

POST   /api/v1/sales-returns/{id:int}/cancel
        → Result — reverts the return (reverses batch + refund)

DELETE /api/v1/sales-returns/{id:int}
        → Result (hard delete, Draft only)
```

### 14.4 Price Check Endpoint

```text
GET    /api/v1/sales/price-check
        Query: productUnitId, currencyId
        → Result<PriceCheckDto> { officialPrice, costPrice, isBelowCost, hasOverride }
```

### 14.5 Print Endpoints (confirmed working)

```text
GET    /api/v1/print/sales/{id}/preview        → A4 preview HTML
POST   /api/v1/print/sales/{id}/a4             → Print A4 invoice
POST   /api/v1/print/sales/{id}/thermal        → Print thermal receipt
POST   /api/v1/print/sales/{id}/save           → Save PDF
GET    /api/v1/print/sales/{id}/preview-data   → Preview JSON data

GET    /api/v1/print/sales-returns/{id}/preview     → NEW: Sales Return preview
POST   /api/v1/print/sales-returns/{id}/a4          → NEW: Print A4 sales return
POST   /api/v1/print/sales-returns/{id}/thermal     → NEW: Print thermal sales return
POST   /api/v1/print/sales-returns/{id}/save        → NEW: Save sales return PDF
```

---

## 15. FluentValidation Validators

### 15.1 CreateSalesInvoiceRequestValidator

```csharp
public class CreateSalesInvoiceRequestValidator : AbstractValidator<CreateSalesInvoiceRequest>
{
    public CreateSalesInvoiceRequestValidator()
    {
        // Header
        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("تاريخ الفاتورة مطلوب");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("نوع الدفع غير صالح");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.SalesInvoiceLines)
            .NotEmpty().WithMessage("يجب إضافة عنصر واحد على الأقل");

        // Customer for Credit/Mixed
        When(x => x.PaymentType == PaymentType.Credit || x.PaymentType == PaymentType.Mixed, () =>
        {
            RuleFor(x => x.CustomerId)
                .NotNull().WithMessage("يجب اختيار عميل للفاتورة الآجلة")
                .GreaterThan(0).WithMessage("يرجى اختيار عميل صالح");
        });

        // CashBox for Cash/Mixed
        When(x => x.PaymentType == PaymentType.Cash || x.PaymentType == PaymentType.Mixed, () =>
        {
            RuleFor(x => x.CashBoxId)
                .NotNull().WithMessage("يجب اختيار صندوق للفاتورة النقدية")
                .GreaterThan(0).WithMessage("يرجى اختيار صندوق صالح");
        });

        // Currency
        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("العملة مطلوبة");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).WithMessage("سعر الصرف يجب أن يكون أكبر من صفر");

        // OtherCharges
        RuleFor(x => x.OtherCharges)
            .GreaterThanOrEqualTo(0).WithMessage("المصاريف الإضافية لا يمكن أن تكون سالبة");

        // Discount
        RuleFor(x => x.InvoiceDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        // Lines
        RuleForEach(x => x.SalesInvoiceLines)
            .SetValidator(new SalesInvoiceLineRequestValidator());
    }
}
```

### 15.2 SalesInvoiceLineRequestValidator

```csharp
public class SalesInvoiceLineRequestValidator : AbstractValidator<SalesInvoiceLineRequest>
{
    public SalesInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("يجب اختيار منتج");

        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("الوحدة مطلوبة");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر")
            .PrecisionScale(18, 3, false).WithMessage("الكمية غير صالحة");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر")
            .PrecisionScale(18, 2, false).WithMessage("السعر غير صالح");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        When(x => x.DiscountType == DiscountType.Percent, () =>
        {
            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(0, 100).WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
        });
    }
}
```

### 15.3 CreateSalesReturnRequestValidator

```csharp
public class CreateSalesReturnRequestValidator : AbstractValidator<CreateSalesReturnRequest>
{
    public CreateSalesReturnRequestValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .GreaterThan(0).WithMessage("يجب اختيار الفاتورة الأصلية — المردودات مرتبطة بالفاتورة في V1");

        RuleFor(x => x.ReturnDate)
            .NotEmpty().WithMessage("تاريخ المردود مطلوب");

        RuleFor(x => x.ReturnReason)
            .MaximumLength(500).WithMessage("سبب المردود يجب أن لا يتجاوز 500 حرف");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("العميل مطلوب");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("صندوق الاسترداد مطلوب");

        RuleFor(x => x.RefundAmount)
            .GreaterThanOrEqualTo(0).WithMessage("مبلغ الاسترداد لا يمكن أن يكون سالباً");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة عنصر واحد على الأقل للمردود");

        RuleForEach(x => x.Items)
            .SetValidator(new SalesReturnItemRequestValidator());
    }
}
```

### 15.4 CreateSalesQuotationRequestValidator

```csharp
public class CreateSalesQuotationRequestValidator : AbstractValidator<CreateSalesQuotationRequest>
{
    public CreateSalesQuotationRequestValidator()
    {
        RuleFor(x => x.QuotationDate)
            .NotEmpty().WithMessage("تاريخ عرض السعر مطلوب");

        RuleFor(x => x.ValidUntil)
            .NotEmpty().WithMessage("تاريخ صلاحية عرض السعر مطلوب");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("العميل مطلوب");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("العملة مطلوبة");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة عنصر واحد على الأقل");

        RuleFor(x => x.ValidUntil)
            .GreaterThan(x => x.QuotationDate)
            .WithMessage("تاريخ الصلاحية يجب أن يكون بعد تاريخ عرض السعر");
    }
}
```

### 15.5 UpdateSalesInvoiceRequestValidator

```csharp
public class UpdateSalesInvoiceRequestValidator : AbstractValidator<UpdateSalesInvoiceRequest>
{
    // Same as Create but with optional fields — only changes allowed for Draft
    // NO changes allowed for Posted/Cancelled (guard in service)
    public UpdateSalesInvoiceRequestValidator()
    {
        // Same rules as Create for editable fields
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("تاريخ الفاتورة مطلوب");
        // ... same field validations
    }
}
```

---

## 16. Reports

### 16.1 Sales Invoice Report (Existing — Enhanced)

| Column | Type | Source |
|--------|------|--------|
| Invoice No | int | SalesInvoice.InvoiceNo |
| Invoice Date | DateTime | SalesInvoice.InvoiceDate |
| Customer | string | Customer.Name |
| Payment Type | string | PaymentType display (نقدي/آجل/مختلط) |
| Line Items | int | Count of items |
| SubTotal | decimal | Sum of LineTotals |
| Invoice Discount | decimal | SalesInvoice.DiscountAmount |
| Tax Amount | decimal | SalesInvoice.TaxAmount |
| Additional Charges | decimal | SalesInvoice.OtherCharges |
| Net Total | decimal | SalesInvoice.TotalAmount |
| Paid Amount | decimal | SalesInvoice.PaidAmount |
| Due Amount | decimal | SalesInvoice.DueAmount |
| Cost Amount | decimal | Sum of (Line Qty x UnitCost) |
| Profit Amount | decimal | NetTotal - CostAmount |
| Profit % | decimal | Profit / NetTotal × 100 |
| Status | string | Draft/Posted/Cancelled |
| Created By | string | User.Name |
| Exchange Rate | decimal | SalesInvoice.ExchangeRate |
| Base Currency Amount | decimal | NetTotal × ExchangeRate |

Excel export via ClosedXML with "تقرير المبيعات" header, Kutools-style alternating row colors, and RTL layout compatible with Arabic Excel.

### 16.2 Sales Return Report (Enhanced)

| Column | Type | Source |
|--------|------|--------|
| Return No | int | SalesReturn.InvoiceNo |
| Original Invoice No | int | SalesInvoice.InvoiceNo |
| Customer | string | Customer.Name |
| Return Date | DateTime | SalesReturn.InvoiceDate |
| Return Reason | string | SalesReturn.ReturnReason |
| Item Count | int | Count of items |
| Returned Amount | decimal | Sum of returned amounts |
| Refund Amount | decimal | SalesReturn.RefundAmount |
| Proportional Discount | decimal | Original discount × returnRatio |
| Proportional Tax | decimal | Original tax × returnRatio |
| Status | string | SalesReturn.Status |
| Refund Type | string | Cash/ReduceBalance |

### 16.3 Sales Quotation Report (NEW)

| Column | Type | Source |
|--------|------|--------|
| Quotation No | int | SalesQuotation.Id |
| Customer | string | Customer.Name |
| Date | DateTime | SalesQuotation.QuotationDate |
| Valid Until | DateTime | SalesQuotation.ValidUntil |
| Days Remaining | int | (ValidUntil - Today).Days |
| Total | decimal | Sum of item totals |
| Status | string | Draft/Sent/Accepted/Rejected/Expired/Converted |
| Age Category | string | Current/ExpiringSoon(7d)/Expired |

### 16.4 Excel Export

ALL reports must implement Excel export via ClosedXML:

```csharp
public DataTable ToDataTable()
{
    var dt = new DataTable();
    dt.Columns.Add("رقم الفاتورة", typeof(int));
    dt.Columns.Add("التاريخ", typeof(DateTime));
    dt.Columns.Add("العميل", typeof(string));
    // ... other columns
    foreach (var item in Items)
        dt.Rows.Add(item.InvoiceNo, item.InvoiceDate, item.CustomerName, ...);
    return dt;
}
```

### 16.5 CollectionView / Search

```csharp
// Client-side filter for soft-delete and cancelled
private ICollectionView? _filteredView;
private bool _includeCancelled;

public bool IncludeCancelled
{
    get => _includeCancelled;
    set
    {
        if (SetProperty(ref _includeCancelled, value))
            _filteredView?.Refresh();
    }
}

private void CreateFilteredView()
{
    _filteredView = CollectionViewSource.GetDefaultView(_items);
    _filteredView.Filter = item => IncludeCancelled ||
        (item is SalesInvoiceDto inv && inv.Status != InvoiceStatus.Cancelled);
    OnPropertyChanged(nameof(Items));
}
```

---
## 17. Tooltips (Arabic)

### 17.1 Sales Invoice List View

| Control | Tooltip |
|---------|---------|
| Search TextBox | أدخل رقم الفاتورة أو اسم العميل للبحث |
| عرض الملغاة CheckBox | إظهار الفواتير الملغاة مع الفواتير النشطة |
| Refresh Button | تحديث قائمة الفواتير من الخادم |
| New Invoice Button | فتح شاشة إضافة فاتورة بيع جديدة |
| Drafts Tab | عرض الفواتير المسودة فقط — لم يتم ترحيلها بعد |
| View Button | عرض تفاصيل الفاتورة |
| Edit Button | تعديل الفاتورة — متاح فقط للمسودات |
| Cancel Button | إلغاء الفاتورة — سيتم عكس المخزون والقيود المحاسبية |
| Print Button | طباعة الفاتورة (حراري أو A4) |

### 17.2 Sales Invoice Editor

| Control | Tooltip |
|---------|---------|
| Save Draft Button | حفظ كمسودة — لا يؤثر على المخزون أو الحسابات |
| Save & Post Button | حفظ وترحيل — سيتم خصم المخزون وإنشاء القيود المحاسبية والمستندات |
| Customer ComboBox | اختر العميل — مطلوب للفواتير الآجلة |
| CashBox ComboBox | اختر الصندوق النقدي — مطلوب للفواتير النقدية |
| Currency ComboBox | اختر عملة الفاتورة — سعر الصرف يحسب تلقائياً |
| Product Search | ابحث عن المنتج بالاسم أو الباركود — يمكن المسح الضوئي |
| Quantity TextBox | أدخل الكمية — يمكن استخدام المسح الضوئي (رقم + علامة ضرب) |
| UnitPrice TextBox | أدخل سعر الوحدة — قابل للتعديل مع موافقة المدير عند البيع بأقل من التكلفة |
| Discount TextBox | أدخل قيمة أو نسبة الخصم |
| LineTotal TextBox | إجمالي السطر — قابل للتعديل (حساب مرن) |
| Additional Charges | أدخل قيمة المصاريف الإضافية مثل التوصيل |
| Tax CheckBox | تطبيق الضريبة على الفاتورة |
| PaidAmount TextBox | أدخل المبلغ المدفوع من العميل |
| Post Button | ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد وإنشاء القيود المحاسبية |

---

## 18. Compliance Matrix

| Requirement | Status | Verified By |
|-------------|--------|-------------|
| Multi-currency (CurrencyId + ExchangeRate on invoice) | ✅ Task 1 | Code review + test |
| Price override with manager approval | ✅ Task 3 | PriceOverrideDialog test |
| Per-item profit display | ✅ Task 4 | InvoiceDto.LineItems[].ProfitAmount |
| Auto-journal entries (9 scenarios) | ✅ Task 5 | SE-01 to SE-09 assertions |
| SalesReturn linked to original invoice + FIFO batch return | ✅ Task 6 | ReturnResult assertions |
| Return reversal proportional (discount/tax/charges) | ✅ Task 6 | ProportionalCalculation test |
| Auto customer receipt on cash/mixed | ✅ Task 7 | CustomerReceipt existence test |
| Barcode continuous scan | ✅ Task 9 | ScannerControl integration test |
| Two-button strategy (Draft + Post) | ✅ Task 11 | Draft vs Post code paths |
| Cancel blocked when returns/payments exist | ✅ Task 12 | CancelPreCheck test |
| Basic vs Advanced UI mode | ✅ Task 13 | DisplayMode guard test |
| DiscountType (Amount/Percent) | ✅ Task 16 | DiscountAmount formula test |
| All BR-01 to BR-10 enforced | ✅ Task 10 | UI + backend validation tests |
| All BR-S1 to BR-S4 enforced | ✅ Task 10 | DB constraint + domain guard tests |
| All BR-R1 to BR-R6 enforced | ✅ Task 6 | Service validation tests |
| All BR-P1 to BR-P8 enforced | ✅ Task 11 | Transaction scope tests |
| Excel export for all reports | ✅ Section 16 | ReportExportController test |
| FluentValidation for ALL request DTOs | ✅ Section 15 | Validator test per request |
| Arabic ToolTips on ALL interactive controls | ✅ Section 17 | XAML review per view |
| INotifyDataErrorInfo + ValidateAllAsync | ✅ RULE-228/229 | Code pattern review |
| Save buttons ALWAYS enabled (no CanExecute) | ✅ RULE-059 | Code review |
| RULE-250: UTF-8 encoding verified | ✅ Review step | Encoding check on commit |

---
## 19. Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| R1: Concurrent invoice Post creates duplicate InvoiceNo | Low | High | DocumentSequenceService uses SemaphoreSlim lock (RULE-254) |
| R2: Two users Post same invoice draft simultaneously | Low | Medium | Optimistic concurrency via RowVersion on SalesInvoice. Second committer gets DbUpdateConcurrencyException |
| R3: CashBox refund (SalesReturn) causes negative balance | Low | Medium | Account balance validation before CashTransaction creation. CHECK constraint on GL Account |
| R4: FIFO batch allocation not matching actual goods movement | Medium | Medium | Batches matched by ExpiryDate (FEFO) + oldest first (FIFO). User can override batch for specific items |
| R5: Return reversal not exact (rounding differences) | Medium | Low | Proportional calculation with 2 decimal precision. Difference → retained earnings (small) |
| R6: Large quotation dataset performance | Low | Medium | Paginated API + CollectionView filtering. No N+1 queries |
| R7: XAML complexity in SalesInvoiceEditorView (2000+ lines) | High | Medium | Split into UserControls per section (HeaderSection, ItemsSection, PaymentSection) |
| R8: Price override approved by manager but price was already below cost on Post | Low | High | Backend re-validates ALL prices on Post. PriceOverrideApprovedBy checked again. Price may have changed between approval and Post |
| R9: Migration conflicts when adding new fields to existing tables | Medium | Medium | All migrations additive (no destructive changes). Backup before migration |
| R10: Arabic encoding corruption in XAML ToolTips | Low | Medium | All XAML files saved with UTF-8 BOM. Commit hook validates encoding |

---

## 20. Rollback Plan

If the Phase 28 deployment causes issues, the following rollback steps apply:

| Phase | Rollback Action | Data Impact | Time Required |
|-------|----------------|-------------|---------------|
| Task 1 (CurrencyId) | Revert SalesInvoice migration. New invoices will not have CurrencyId. All existing Posted invoices are unaffected | No data loss (column removal requires data migration) | ~1 hour |
| Task 5 (Auto-JE) | Revert AccountingIntegrationService changes. Existing Posted invoices have entries; reversing requires manual cleanup | Existing entries remain (balanced) | ~2 hours |
| Task 3 (Price Override) | Revert setPriceOverride + dialog. Price override approvals stored in audit log only | No financial impact | ~30 min |
| Task 6 (Return enhancement) | Revert proportional logic. Returns revert to full-amount reversal | Existing returns with proportional amounts need recomputation | ~1 hour |
| Complete | Git revert commit + re-deploy API + Desktop | Coordinated rollback across all tiers | ~4 hours |

**Emergency rollback**: `git revert <commit> && dotnet build && dotnet publish`

### Migration Data Safety

| Safety Measure | Detail |
|---------------|--------|
| All migrations additive | No DROP TABLE, no DELETE COLUMN. Only ADD COLUMN (nullable) + CREATE TABLE |
| Old API version compatibility | Desktop can call old API during staggered deploy |
| Posted invoices NOT affected | New fields only used for NEW invoices |
| Financial integrity | All journal entries still balance after rollback (reversal entries remain) |

---
## 21. Unit Tests

### 21.1 Service Layer Tests (SalesInvoiceServiceTests.cs)

```csharp
// Post Tests
[Fact] public async Task PostAsync_CashSale_CreatesJournalEntry()
[Fact] public async Task PostAsync_CreditSale_UpdatesCustomerBalance()
[Fact] public async Task PostAsync_MixedSale_CreatesCashTxAndBalance()
[Fact] public async Task PostAsync_WithDiscount_CreatesDrDiscountAllowed()
[Fact] public async Task PostAsync_WithTax_CreatesCrVatOutput()
[Fact] public async Task PostAsync_WithCharges_CreatesCrDeliveryRevenue()
[Fact] public async Task PostAsync_InsufficientStock_ReturnsFailure()
[Fact] public async Task PostAsync_NoItems_ReturnsFailure()
[Fact] public async Task PostAsync_CustomerNotSetForCredit_ReturnsFailure()
[Fact] public async Task PostAsync_PaidExceedsTotal_ReturnsFailure()
[Fact] public async Task PostAsync_EmptyCashBoxForCash_ReturnsFailure()

// Cancel Tests
[Fact] public async Task CancelAsync_PostedNoReturnsNoPayments_ReversesAll()
[Fact] public async Task CancelAsync_HasReturns_ReturnsFailureBlocked()
[Fact] public async Task CancelAsync_HasPayments_ReturnsFailureBlocked()

// Draft Tests
[Fact] public async Task CreateDraftAsync_SavesWithStatusDraft()
[Fact] public async Task EditDraftAsync_EditableFieldsOnly()
[Fact] public async Task PostDraftAsync_PostsExistingDraft()

// Quotation Tests
[Fact] public async Task ConvertToInvoiceAsync_CreatesSalesInvoice()
[Fact] public async Task ConvertToInvoiceAsync_QuotationStatusConverted()
[Fact] public async Task ConvertToInvoiceAsync_ExpiredQuotation_ReturnsFailure()
```

### 21.2 UI Layer Tests

```csharp
// ViewModel Tests
[Fact] public async Task SavePostCommand_CallsServicePostAsync()
[Fact] public async Task SavePostCommand_ShowsValidationOnEmpty()
[Fact] public async Task SaveDraftCommand_CallsServiceCreateDraftAsync()
[Fact] public async Task ProductSelection_AutoFillsPriceFromPrices()
[Fact] public async Task PriceOverride_ShowsDialogBelowCost()
[Fact] public async Task UnitChange_ConvertsAvailableStock()
[Fact] public async Task LineTotalEdit_RecalculatesQuantity()
[Fact] public async Task BarcodeScan_AddsProductToList()
[Fact] public async Task IncludeInactive_FiltersInvoices()
[Fact] public async Task IncludeCancelled_FiltersCancelled()
```

### 21.3 Controller Tests

```csharp
[Fact] public async Task Post_ValidRequest_ReturnsOk()
[Fact] public async Task Post_InvalidRequest_ReturnsBadRequest()
[Fact] public async Task Cancel_HasReturns_ReturnsConflict()
[Fact] public async Task GetDrafts_ReturnsOnlyDraftStatus()
```

### 21.4 Validator Tests

```csharp
[Fact] public void CreateRequest_EmptyCustomerForCredit_ValidationFails()
[Fact] public void CreateRequest_NegativeQuantity_ValidationFails()
[Fact] public void CreateRequest_NoLines_ValidationFails()
[Fact] public void LineRequest_PriceBelowZero_ValidationFails()
```

### 21.5 Integration Tests

```csharp
[Fact] public async Task PostToCancel_FullLifecycle_AllEntriesBalanced()
[Fact] public async Task PostToReturn_FullReturnLifecycle_StockRestored()
```

---
## 22. Cross-References

### 22.1 Related Phases

| Phase | Dependency | Dependency Type |
|-------|-----------|----------------|
| Phase 24 (Accounting Integration) | AccountingIntegrationService + 9 JE scenarios | ⚠️ Prerequisite |
| Phase 25 (Products) | InventoryBatches, ProductPrices | ⚠️ Prerequisite |
| Phase 26 (Warehouses) | WarehouseStocks, WarehouseTransfer | ⚠️ Prerequisite |
| Phase 23 (Customers) | Customer entity, Customer.AccountId | ⚠️ Prerequisite |
| Phase 29 (Receipts & Payments) | CustomerReceipt, PaymentAllocation | 📌 Cross-feature |
| Phase 30 (Journal Entries) | JournalEntry creation on Post | 📌 Cross-feature |
| Phase 31 (Reports) | Sales report DTOs | 📌 Cross-feature |
| Phase 32 (Suppliers) | Supplier.AccountId pattern | 📌 Reusable pattern |
| Phase 22 (Chart of Accounts) | Account entity, DeliveryChargesRevenue account | ⚠️ Prerequisite |

### 22.2 Related Documents

| Document | Relevant Sections |
|----------|-------------------|
| docs/PRD-MVP.md | FR-006 Sales, FR-009 Payments, FR-010 Reports |
| docs/database-schema.md | SalesInvoice, SalesReturn, CustomerReceipt tables |
| docs/CONSTITUTION.md | Sections 2.4 (Invoice lifecycle), 2.5 (Result pattern), 2.7 (Stock) |
| AGENTS.md | Sections 2.76-2.83 (Phase 24-31 rules), 2.92 (Sales rules) |
| docs/Phase 24 — Accounting Integration Plan.md | Journal entry patterns |
| docs/Phase 22 — Chart of Accounts Module Implementation Plan.md | Account 1520, 1533 seeding |
| docs/all new Anylysis for update system features/Sales and Purchases new details.md | 9 sales scenarios |
| docs/all new Anylysis for update system features/Invoices Details.md | Draft/Post/Cancel, Cancel vs Return, Two buttons |
| docs/all new Anylysis for update system features/Accounts.md | Single SalesRevenue, Basic vs Advanced |

### 22.3 Related RULEs

| RULE | Topic |
|------|-------|
| RULE-475 | SalesService.PostAsync() MUST enforce PreventBelowRetailPrice |
| RULE-476 | SalesService.PostAsync() MUST enforce AllowBelowCostSale |
| RULE-477 | IProductPriceService MUST be injected into SalesService |
| RULE-478 | Price override MUST NOT change ProductPrices base price |
| RULE-479 | Desktop InvoiceLineViewModel.GetDefaultPrice() MUST NOT return 0m |
| RULE-493 | DeliveryChargesRevenue account (1533) — separate from SalesRevenue |
| RULE-494 | SystemAccountKey.DeliveryChargesRevenue = 21 |
| RULE-495 | ReverseSalesPostEntryAsync MUST include DeliveryChargesRevenue |
| RULE-496 | Flexible input: user enters ANY TWO of (Qty, Price, Total) |
| RULE-497 | LineTotal column editable (NOT IsReadOnly) |
| RULE-498 | LineTotalInput + _lastModifiedField + _isRecalculating in line VMs |
| RULE-505 | RecalculateFromFlexibleInput ONLY uses calculator when _lastModifiedField == Total |
| RULE-510 | CreateSalesReturnEntryAsync for standalone sales returns |
| RULE-517 | SalesReturn.Post() MUST set PostedAt |
| RULE-518 | SalesReturnService MUST inject IAccountingIntegrationService |
| RULE-526 | InvoicePrintDto MUST have OtherCharges and FooterNote |
| RULE-527 | PrintController MUST have sales-return print endpoints |

---
