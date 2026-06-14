# Phase 28 — Sales Module: Comprehensive Enhancement & New Features Implementation Plan

> **Version**: 1.0 — Full analysis of current Sales module + 16 implementation tasks
> **Scope**: Sales Invoice enhancement, Sales Quotations (NEW), Sales Returns enhancement, Barcode POS (NEW)
> **Based on**: Analysis Part 3 (FIFO batch allocation), Analysis Part 5 (multi-currency, pricing, sales return), Global Analysis

---

## Table of Contents
1. [Architecture — 4 Sub-Modules](#1-architecture--4-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog — Full Specs](#4-design-catalog--full-specs)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 4 Sub-Modules

The Sales Module is divided into **4 sub-modules**:

| # | Sub-Module | Type | Description |
|---|-----------|------|-------------|
| S1 | **Sales Invoices** | Enhancement | Multi-currency, price override with approval, enhanced profit display, auto-journal entries, auto-payment creation, enhanced discount model |
| S2 | **Sales Quotations** | NEW | Pre-sales quote (عرض سعر) — no stock/account impact, can convert to invoice |
| S3 | **Sales Returns** | Enhancement | FIFO batch return, discount handling, auto customer refund, enhanced journal entries |
| S4 | **Barcode POS** | NEW | Continuous scan mode for fast POS-style entry, sound feedback, auto-add with quantity increment |

### Data Flow

```text
┌─────────────────────────────────────────────────────┐
│                  Desktop (WPF)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ SalesInvoice  │  │ SalesQuote  │  │ SalesReturn│ │
│  │ EditorVM      │  │ EditorVM    │  │ EditorVM   │ │
│  └──────┬───────┘  └──────┬───────┘  └─────┬──────┘ │
│         │                 │                 │         │
│  ┌──────▼─────────────────▼─────────────────▼──────┐ │
│  │           ApiService (HttpClient)                │ │
│  └──────┬─────────────────┬─────────────────┬──────┘ │
└─────────┼─────────────────┼─────────────────┼────────┘
          │                 │                 │
┌─────────▼─────────────────▼─────────────────▼────────┐
│                    API (ASP.NET Core)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ SalesInvoice │  │ SalesQuote  │  │ SalesReturn│ │
│  │ Controller   │  │ Controller   │  │ Controller │ │
│  └──────┬───────┘  └──────┬───────┘  └─────┬──────┘ │
│         │                 │                 │         │
│  ┌──────▼─────────────────▼─────────────────▼──────┐ │
│  │              Application Services                │ │
│  │  (SalesInvoiceService, SalesQuoteService,        │ │
│  │   SalesReturnService, InventoryService,          │ │
│  │   JournalEntryService, CustomerPaymentService)   │ │
│  └──────┬─────────────────┬─────────────────┬──────┘ │
└─────────┼─────────────────┼─────────────────┼────────┘
          │                 │                 │
┌─────────▼─────────────────▼─────────────────▼────────┐
│                  Infrastructure                        │
│  ┌────────────┐  ┌──────────┐  ┌────────────────┐   │
│  │ IUnitOfWork│  │ Repository│  │ EF Core Config │   │
│  └────────────┘  └──────────┘  └────────────────┘   │
└─────────┬─────────────────┬─────────────────┬────────┘
          │                 │                 │
┌─────────▼─────────────────▼─────────────────▼────────┐
│                   SQL Server                          │
│  SalesInvoices │ SalesInvoiceItems │ SalesQuotations │
│  SalesQuotationItems │ SalesReturns │ SalesReturnItems│
│  InventoryMovements │ JournalEntries │ CustomerPayments│
└──────────────────────────────────────────────────────┘
```

### Lifecycle States

```text
Sales Invoice:    Draft (1) → Posted (2) → Cancelled (3)
Sales Quotation:  Draft (1) → Sent (2) → Accepted (3) → Converted (4) → Rejected (5)
Sales Return:     Draft (1) → Posted (2) → Cancelled (3)
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Entities ✅

| Entity | File | Lines | Status |
|--------|------|-------|--------|
| `SalesInvoice` | `Domain/Entities/SalesInvoice.cs` | 165 | ✅ Exists |
| `SalesInvoiceItem` | `Domain/Entities/SalesInvoiceItem.cs` | 58 | ✅ Exists |
| `SalesReturn` | `Domain/Entities/SalesReturn.cs` | 126 | ✅ Exists (SalesReturn + SalesReturnItem) |

### 2.2 EF Core Configurations ✅

| Configuration | File | Status |
|--------------|------|--------|
| `SalesInvoiceConfiguration` | `Infrastructure/Data/Configurations/SalesInvoiceConfiguration.cs` | ✅ Exists |
| `SalesInvoiceItemConfiguration` | `Infrastructure/Data/Configurations/SalesInvoiceItemConfiguration.cs` | ✅ Exists |

### 2.3 Contracts ✅

| Type | File | Status |
|------|------|--------|
| `SalesInvoiceDto` | `Contracts/DTOs/AllDtos.cs` | ✅ Exists |
| `SalesInvoiceItemDto` | `Contracts/DTOs/AllDtos.cs` | ✅ Exists |
| `SalesReturnDto` | `Contracts/DTOs/AllDtos.cs` | ✅ Exists |
| `SalesReturnItemDto` | `Contracts/DTOs/AllDtos.cs` | ✅ Exists |
| `SalesInvoiceResponse` | `Contracts/Responses/SalesInvoiceResponses.cs` | ✅ Exists |
| `SalesInvoiceItemResponse` | `Contracts/Responses/SalesInvoiceResponses.cs` | ✅ Exists |
| `CreateSalesInvoiceRequest` | `Contracts/Requests/` | ✅ Likely exists |
| `UpdateSalesInvoiceRequest` | `Contracts/Requests/` | ✅ Likely exists |

### 2.4 Application Services

| Service | File | Status |
|---------|------|--------|
| `SalesInvoiceService` | `Application/Services/` | ✅ Exists |
| `SalesReturnService` | `Application/Services/SalesReturnService.cs` | ✅ Exists |
| `InventoryService` | `Application/Services/` | ✅ Exists |

### 2.5 API Controllers ✅

| Controller | File | Status |
|-----------|------|--------|
| `SalesInvoicesController` | `Api/Controllers/SalesInvoicesController.cs` | ✅ Exists |
| `SalesReturnsController` | `Api/Controllers/` | ✅ Likely exists |

### 2.6 Desktop ViewModels ✅

| ViewModel | File | Lines | Status |
|-----------|------|-------|--------|
| `SalesInvoiceEditorViewModel` | `DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | 1486 | ✅ Exists (complex) |
| `SalesInvoiceListViewModel` | `DesktopPWF/ViewModels/Sales/SalesInvoiceListViewModel.cs` | — | ✅ Exists |
| `SalesReturnEditorViewModel` | `DesktopPWF/ViewModels/Returns/SalesReturnEditorViewModel.cs` | — | ✅ Exists |
| `SalesReturnListViewModel` | `DesktopPWF/ViewModels/Returns/SalesReturnListViewModel.cs` | — | ✅ Exists |
| `SalesInvoiceSelectionViewModel` | `DesktopPWF/ViewModels/Invoices/SalesInvoiceSelectionViewModel.cs` | — | ✅ Exists |

### 2.7 Desktop Views ✅

| View | File | Status |
|------|------|--------|
| `SalesInvoiceEditorView` | `DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` | ✅ Exists |
| `SalesInvoicesListView` | `DesktopPWF/Views/Sales/SalesInvoicesListView.xaml` | ✅ Exists |
| `SalesReturnEditorView` | `DesktopPWF/Views/Returns/SalesReturnEditorView.xaml` | ✅ Exists |
| `SalesReturnsListView` | `DesktopPWF/Views/Returns/SalesReturnsListView.xaml` | ✅ Exists |
| `SalesInvoiceSelectionView` | `DesktopPWF/Views/Invoices/SalesInvoiceSelectionView.xaml` | ✅ Exists |

### 2.8 API Services (Desktop — HTTP Clients) ✅

| Service | File | Status |
|---------|------|--------|
| `SalesInvoiceApiService` | `DesktopPWF/Services/Api/SalesInvoiceApiService.cs` | ✅ Exists |
| `SalesReturnApiService` | `DesktopPWF/Services/Api/SalesReturnApiService.cs` | ✅ Exists |

### 2.9 Enums Used ✅

| Enum | Values | File |
|------|--------|------|
| `InvoiceStatus` | Draft=1, Posted=2, Cancelled=3 | `Domain/Enums/` ✅ |
| `PaymentType` | Cash=1, Credit=2, Mixed=3 | `Domain/Enums/` ✅ |
| `SaleMode` | Retail=1, Wholesale=2 | `Domain/Enums/` ✅ |
| `MovementType` | PurchaseIn=1, SaleOut=2, SaleReturnIn=3, etc. | `Domain/Enums/` ✅ |

### 2.10 Existing Sales Features Summary

| Feature | Status |
|---------|--------|
| Sales invoice CRUD (Draft → Posted → Cancelled) | ✅ |
| Stock deduction on Post | ✅ |
| Stock reversal on Cancel | ✅ |
| Customer balance update on Post | ✅ |
| FIFO batch allocation on sale | ✅ (if Phase 25 implemented) |
| Real-time profit display | ✅ (basic) |
| Print preview (A4 + 80mm thermal) | ✅ |
| Payment type (Cash/Credit/Mixed) | ✅ |
| Basic line-item discount | ✅ |
| TaxId FK on invoice | ✅ (from Phase 19) |
| Search by product name/barcode | ✅ |
| Multi-unit selection | ✅ |
| Draft save/posting | ✅ |
| Sales return CRUD | ✅ |

### 2.11 Files to Create (NEW)

| # | File | Purpose |
|---|------|---------|
| 1 | `Domain/Entities/SalesQuotation.cs` | Sales quotation entity |
| 2 | `Domain/Entities/SalesQuotationItem.cs` | Sales quotation line item |
| 3 | `Infrastructure/Data/Configurations/SalesQuotationConfiguration.cs` | EF Core config |
| 4 | `Infrastructure/Data/Configurations/SalesQuotationItemConfiguration.cs` | EF Core config |
| 5 | `Contracts/DTOs/SalesQuotationDto.cs` | Quotation DTO |
| 6 | `Contracts/Requests/SalesQuotationRequests.cs` | Create/Update requests |
| 7 | `Contracts/DTOs/ProfitDisplayDto.cs` | Profit display DTO |
| 8 | `Application/Interfaces/ISalesQuotationService.cs` | Quotation service interface |
| 9 | `Application/Services/SalesQuotationService.cs` | Quotation service implementation |
| 10 | `Api/Controllers/SalesQuotationsController.cs` | Quotation API endpoints |
| 11 | `DesktopPWF/Services/Api/SalesQuotationApiService.cs` | Quotation HTTP client |
| 12 | `DesktopPWF/ViewModels/Sales/SalesQuotationEditorViewModel.cs` | Quotation editor VM |
| 13 | `DesktopPWF/ViewModels/Sales/SalesQuotationListViewModel.cs` | Quotation list VM |
| 14 | `DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml` | Quotation editor view |
| 15 | `DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml.cs` | Code-behind |
| 16 | `DesktopPWF/Views/Sales/SalesQuotationsListView.xaml` | Quotation list view |
| 17 | `DesktopPWF/Views/Sales/SalesQuotationsListView.xaml.cs` | Code-behind |
| 18 | `DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml` | Price override approval dialog |
| 19 | `DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml.cs` | Code-behind |
| 20 | `DesktopPWF/ViewModels/Sales/DraftInvoicesViewModel.cs` | Draft invoice management VM |
| 21 | `DesktopPWF/Views/Sales/DraftInvoicesView.xaml` | Draft management view |
| 22 | `DesktopPWF/Views/Sales/DraftInvoicesView.xaml.cs` | Code-behind |
| 23 | `DesktopPWF/Controls/BarcodeScannerControl.xaml` | Continuous barcode scan control |
| 24 | `DesktopPWF/Controls/BarcodeScannerControl.xaml.cs` | Code-behind |

### 2.12 Files to Modify (ENHANCE)

| # | File | Change |
|---|------|--------|
| 1 | `Domain/Entities/SalesInvoice.cs` | Add CurrencyId, ExchangeRate, EnhancedDiscountType/DiscountValue, AdditionalCharges, Items list with batch allocation |
| 2 | `Domain/Entities/SalesInvoiceItem.cs` | Add ProductUnitId, CostPrice, ProfitAmount, BatchAllocation tracking |
| 3 | `Domain/Entities/SalesReturn.cs` | Add TaxAmount, DiscountAmount, TotalAmount, AdditionalCharges, CurrencyId, ExchangeRate, CashBoxId, batch return tracking |
| 4 | `Domain/Entities/SalesReturnItem.cs` | Add ProductUnitId, UnitCost (at time of sale), BatchId for return |
| 5 | `Contracts/DTOs/AllDtos.cs` | Extend SalesInvoiceDto, SalesInvoiceItemDto, SalesReturnDto with new fields |
| 6 | `Contracts/Responses/SalesInvoiceResponses.cs` | Add currency, profit, enhanced fields |
| 7 | `Contracts/Requests/SalesInvoiceRequests.cs` | Add CurrencyId, ExchangeRate, price override fields |
| 8 | `SalesInvoiceConfiguration.cs` | Add new column configs + FK |
| 9 | `SalesInvoiceItemConfiguration.cs` | Add new column configs |
| 10 | `SalesInvoiceService.cs` (Application) | Add: currency handling, price override approval, auto-journal, auto-payment, enhanced profit calc |
| 11 | `SalesReturnService.cs` | Add: FIFO batch return, auto refund, discount on return, enhanced journal |
| 12 | `SalesInvoicesController.cs` | Add new endpoints (Quotation convert, batch allocation info) |
| 13 | `SalesInvoiceEditorViewModel.cs` | Add: currency selector, price level picker, profit display, price override dialog, barcode scan mode |
| 14 | `SalesInvoiceListViewModel.cs` | Add: draft management filter, profit column |
| 15 | `SalesInvoiceEditorView.xaml` | Add: currency field, price level selector, profit columns, compact restyle |
| 16 | `SalesInvoicesListView.xaml` | Add: profit column, draft badge |
| 17 | `SalesInvoiceApiService.cs` | Add new endpoints |
| 18 | `ISalesInvoiceApiService.cs` | Add new methods |
| 19 | `App.xaml.cs` (DesktopPWF) | DI registrations for new ViewModels, services + navigation entries |
| 20 | `Program.cs` (API) | DI registrations for new services |
| 21 | `DbSeeder.cs` | Seed SystemSettings for sales (ShowProfitInInvoice, AutoPostInvoices, PreventBelowRetailPrice, etc.) |
| 22 | `Messaging/Messages/AppMessages.cs` | Add SalesQuotationChangedMessage |

---

## 3. BLOCKER Resolution — Critical Fixes

These 4 issues were identified during analysis as **blocking** — they must be resolved before Phase 28 implementation begins.

### 3.1 Blocker 1: CurrencyId + ExchangeRate Missing from SalesInvoice

**Problem**: Sales invoice currently has no concept of multi-currency. All prices are assumed in the base currency (configured in StoreSettings.CurrencyCode). Analysis Part 5 (lines 2156-2229) requires:
- Sales price linked to (Product + Unit + Currency)
- ExchangeRate stored on invoice at time of transaction
- Auto-conversion from base currency if no price exists for selected currency
- Currency gain/loss tracking (need base-currency-equivalent amounts)

**Root cause**: The SalesInvoice entity was designed before multi-currency was specified. Currency infrastructure (Currency entity, CurrencyRates table) was added in Phase 20 but SalesInvoice was never updated.

**Fix**:
1. Add `int? CurrencyId` FK and `decimal ExchangeRate` to `SalesInvoice`
2. Add `decimal BaseCurrencyTotal` — total converted to base currency (for COGS calculation)
3. Store the exchange rate at time of invoice creation (not live rate)
4. When invoice is posted, convert all amounts to base currency for journal entries

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/SalesInvoice.cs` for the canonical `SetCurrency()` method.

**Files changed**: `SalesInvoice.cs`, `SalesInvoiceConfiguration.cs`, `SalesInvoiceDto.cs`, `SalesInvoiceResponse.cs`, `SalesInvoiceService.cs`, contracts, migrations

**Estimate**: ~1 hour

---

### 3.2 Blocker 2: Price Override Approval Workflow

**Problem**: Analysis Part 5 (lines 1574-1833) specifies:
- User CAN modify sale price within the invoice (overrides default)
- Modifying price does NOT change the official product price
- When selling BELOW the minimum allowed price (PreventBelowRetailPrice setting), a warning/approval dialog must appear
- When selling BELOW cost (AllowBelowCostSale setting), a warning must appear
- Manager approval required for below-cost sales

Currently, there is NO approval workflow. The price field on `SalesInvoiceItem` is freely editable with no validation or approval gate.

**Fix**:
1. Add `PriceOverrideApprovedBy` (int? UserId) and `PriceOverrideReason` (string?) to SalesInvoiceItem
2. Create `PriceOverrideDialog` — styled WPF dialog showing:
   - Current official price
   - Proposed override price
   - Reason for override (required text field)
   - Warning level: BelowRetail / BelowCost
3. Add `SystemSettings` entries:
   - `PreventBelowRetailPrice` (bool, default false) — block or warn
   - `AllowBelowCostSale` (bool, default false) — warn or block
4. `SalesInvoiceService` checks override on Post — if price < official, requires approval record

> See `docs/AGENTS.md` for domain entity patterns. See `Domain/Entities/SalesInvoiceItem.cs` for the canonical `SetPriceOverride()` method.

**Files changed**: `SalesInvoiceItem.cs`, `SalesInvoiceItemConfiguration.cs`, `SalesInvoiceService.cs`, `SalesInvoiceEditorViewModel.cs`, `SalesInvoiceEditorView.xaml`, `PriceOverrideDialog.xaml` (NEW)

**Estimate**: ~2 hours

---

### 3.3 Blocker 3: Auto-Journal Entry Creation (Chart of Accounts Dependency)

**Problem**: Sales invoice posting must auto-create journal entries. The journal entries require:
- Chart of Accounts to exist (Phase 19/20 dependency)
- Accounts for: Sales Revenue, COGS, Inventory, Customer Receivables, Cash/Bank, Sales Tax Payable, Delivery/Service Revenue
- Costing method (WeightedAverage/FIFO) to compute COGS

Currently, `SalesInvoiceService.PostAsync()` deducts stock and updates customer balance but does NOT create journal entries.

**Fix**:
1. Create `IJournalEntryService` interface in Application layer:
> See `docs/AGENTS.md` §2.76 (Phase 24 — Accounting Integration Rules, RULE-373) for journal entry mapping for sales. See `Application/Interfaces/Services/IJournalEntryService.cs` and `Application/Services/AccountingIntegrationService.cs` for the canonical interfaces and implementation.
2. Create standard journal entry template for sales:
   - **Cash Sale**: Dr Cash/Bank | Cr Sales Revenue (+ Cr Tax Payable)
   - **Credit Sale**: Dr Customer Receivable | Cr Sales Revenue (+ Cr Tax Payable)
   - **COGS**: Dr COGS | Cr Inventory
   - **Delivery Charges**: Dr Cash/Bank (+Dr Customer) | Cr Delivery Revenue
3. Run INSIDE the Post transaction (not separately)
4. Store JournalEntry reference on SalesInvoice: `int? JournalEntryId`

**Files changed**: `SalesInvoiceService.cs`, `IJournalEntryService.cs` (NEW), `JournalEntryService.cs` (NEW, or use existing), `SalesInvoice.cs` (add JournalEntryId)

**Estimate**: ~3 hours

---

### 3.4 Blocker 4: FIFO Batch Allocation on Sale (Phase 25 Dependency)

**Problem**: Analysis Part 3 (lines 1743-1757) requires that when a sale is posted:
- Stock is deducted from FIFO batches (oldest batch first)
- If FEFO enabled and product has expiry date, deduct nearest-expiry batch first
- COGS is computed from actual batch cost, not average
- The batch allocation is recorded per sales invoice item

Currently, existing `SalesInvoiceService.PostAsync()` calls `_inventoryService.DecreaseStockAsync()` which only decrements total stock quantity — it does NOT allocate from specific batches.

**Fix**:
1. Batch allocation must exist and return cost allocation per item
2. `SalesInvoiceItem` must store batch allocation records: `List<SalesInvoiceBatchAllocation>`
3. `InventoryService.DecreaseStockAsync()` must accept batch selection strategy (FIFO/FEFO)
4. Return list of `(batchId, quantity, unitCost)` for COGS calculation

> See `docs/AGENTS.md` §2.7 (Stock Integrity, RULE-028/029) and §2.25 (Costing Strategy). See `Application/Services/InventoryService.cs` for the canonical batch allocation implementation.

**Files changed**: `InventoryService.cs`, `IInventoryService.cs`, `SalesInvoiceItem.cs` (add batch allocations), `SalesInvoiceService.cs` (use batch allocation)

**Estimate**: ~3 hours

---

## 4. Design Catalog — Full Specs

### 4.1 SalesInvoice Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `InvoiceNo` | `int` | — | ✅ | User-facing number (RULE-254) |
| `CustomerId` | `int? FK` | `null` | ❌ | Default cash customer if null |
| `WarehouseId` | `int FK` | — | ✅ | Stock deduction warehouse |
| `CashBoxId` | `int? FK` | `null` | ❌ | For cash payments |
| `TaxId` | `int? FK` | `null` | ❌ | From Phase 19 |
| `**CurrencyId**` | `**int? FK**` | `**null**` | ❌ | **NEW**: Multi-currency |
| `**JournalEntryId**` | `**int? FK**` | `**null**` | ❌ | **NEW**: Auto-created on Post |
| `InvoiceDate` | `datetime2` | `UTC now` | ✅ | — |
| `DueDate` | `date?` | `null` | ❌ | For credit sales |
| `PaymentType` | `byte` | `1` (Cash) | ✅ | 1=Cash, 2=Credit, 3=Mixed |
| `**ExchangeRate**` | `**decimal(18,6)**` | `**1**` | ✅ | **NEW**: Rate at invoice creation |
| `**BaseCurrencyTotal**` | `**decimal(18,2)**` | `**0**` | ✅ | **NEW**: Total in base currency |
| `SubTotal` | `decimal(18,2)` | `0` | ✅ | Sum of line totals |
| `**DiscountType**` | `**byte?**` | `**null**` | ❌ | **NEW**: 0=Amount, 1=Percent |
| `**DiscountValue**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: Raw discount input |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | Computed from DiscountValue |
| `**AdditionalCharges**` | `**decimal(18,2)**` | `**0**` | ❌ | **NEW**: delivery, service fees |
| `TaxAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `TotalAmount` | `decimal(18,2)` | `0` | ✅ | SubTotal - Discount + Tax + Charges |
| `PaidAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `DueAmount` | `decimal(18,2)` | `0` | ✅ | TotalAmount - PaidAmount |
| `Status` | `byte` | `1` (Draft) | ✅ | 1=Draft, 2=Posted, 3=Cancelled |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | — |
| `**TotalCost**` | `**decimal(18,2)**` | `**0**` | ❌ | **NEW**: Sum of (qty × unit cost) from batch allocation |
| `**TotalProfit**` | `**decimal(18,2)**` | `**0**` | ❌ | **NEW**: TotalAmount - TotalCost |
| `CreatedByUserId` | `int? FK` | — | ❌ | Audit (BaseEntity) |

### 4.2 SalesInvoiceItem Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `SalesInvoiceId` | `int FK` | — | ✅ | Parent invoice |
| `ProductId` | `int FK` | — | ✅ | — |
| `**ProductUnitId**` | `**int? FK**` | `**null**` | ❌ | **NEW**: Unit used at sale time |
| `Quantity` | `decimal(18,3)` | — | ✅ | — |
| `UnitPrice` | `decimal(18,2)` | — | ✅ | Actual sale price (may differ from official) |
| `**OfficialPrice**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: Official price at time of sale |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | Line-level discount |
| `LineTotal` | `decimal(18,2)` | — | ✅ | = (Qty × UnitPrice) - Discount |
| `Mode` | `byte` | `1` (Retail) | ✅ | 1=Retail, 2=Wholesale |
| `**PriceOverrideApprovedBy**` | `**int? FK**` | `**null**` | ❌ | **NEW**: User who approved price override |
| `**PriceOverrideReason**` | `**nvarchar(200)?**` | `**null**` | ❌ | **NEW**: Reason for price override |
| `**CostPrice**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: Total cost from batch allocation |
| `**UnitCost**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: Per-unit cost at time of sale |
| `**ProfitAmount**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: LineTotal - CostPrice |
| **BatchAllocations** | `List<BatchAllocation>` | — | ❌ | **NEW**: Allocation per batch (stored separately or serialized) |
| `Notes` | `nvarchar(200)?` | `null` | ❌ | — |

### 4.3 SalesQuotation Entity — NEW

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `QuotationNo` | `int` | — | ✅ | User-facing number |
| `CustomerId` | `int? FK` | `null` | ❌ | — |
| `WarehouseId` | `int FK` | — | ✅ | Pricing warehouse |
| `CurrencyId` | `int? FK` | `null` | ❌ | Multi-currency support |
| `ExchangeRate` | `decimal(18,6)` | `1` | ✅ | — |
| `QuotationDate` | `datetime2` | `UTC now` | ✅ | — |
| `ValidUntil` | `date?` | `null` | ❌ | Quotation expiry |
| `Status` | `byte` | `1` (Draft) | ✅ | 1=Draft, 2=Sent, 3=Accepted, 4=Converted, 5=Rejected |
| `SubTotal` | `decimal(18,2)` | `0` | ✅ | — |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `TaxAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `TotalAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | — |
| `TermsAndConditions` | `nvarchar(2000)?` | `null` | ❌ | Quotation terms |
| `**ConvertedToInvoiceId**` | `**int? FK**` | `**null**` | ❌ | **NEW**: If converted, link to SalesInvoice |
| `CreatedByUserId` | `int? FK` | — | ❌ | Audit |

### 4.4 SalesQuotationItem Entity — NEW

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `SalesQuotationId` | `int FK` | — | ✅ | — |
| `ProductId` | `int FK` | — | ✅ | — |
| `ProductUnitId` | `int? FK` | `null` | ❌ | — |
| `Quantity` | `decimal(18,3)` | — | ✅ | — |
| `UnitPrice` | `decimal(18,2)` | — | ✅ | Quoted price |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `LineTotal` | `decimal(18,2)` | — | ✅ | = (Qty × UnitPrice) - Discount |
| `Mode` | `byte` | `1` (Retail) | ✅ | — |
| `Notes` | `nvarchar(200)?` | `null` | ❌ | — |

### 4.5 SalesReturn Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `ReturnNo` | `string` | — | ✅ | Will migrate to int per RULE-254 |
| `SalesInvoiceId` | `int? FK` | `null` | ❌ | Link to original invoice |
| `CustomerId` | `int? FK` | `null` | ✅ | — |
| `WarehouseId` | `int FK` | — | ✅ | Return-to warehouse |
| `CashBoxId` | `int? FK` | `null` | ❌ | **NEW**: For cash refund |
| `CurrencyId` | `int? FK` | `null` | ❌ | **NEW**: Multi-currency |
| `ExchangeRate` | `decimal(18,6)` | `1` | ✅ | **NEW** |
| `ReturnDate` | `datetime2` | `UTC now` | ✅ | — |
| `Status` | `byte` | `1` (Draft) | ✅ | 1=Draft, 2=Posted, 3=Cancelled |
| `SubTotal` | `decimal(18,2)` | `0` | ✅ | — |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | **NEW**: Discount on return |
| `TaxAmount` | `decimal(18,2)` | `0` | ✅ | **NEW** |
| `AdditionalCharges` | `decimal(18,2)` | `0` | ❌ | **NEW** |
| `TotalAmount` | `decimal(18,2)` | `0` | ✅ | **NEW**: Was computed as = SubTotal previously |
| `RefundAmount` | `decimal(18,2)` | `0` | ❌ | **NEW**: Amount refunded to customer |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | — |
| `ReturnReason` | `nvarchar(500)?` | `null` | ❌ | **NEW** |

### 4.6 SalesReturnItem Entity — ENHANCED

| Field | Type | Default | Required | Notes |
|-------|------|---------|----------|-------|
| `Id` | `int PK` | Auto-Increment | ✅ | — |
| `SalesReturnId` | `int FK` | — | ✅ | — |
| `ProductId` | `int FK` | — | ✅ | — |
| `ProductUnitId` | `int? FK` | `null` | ❌ | **NEW** |
| `Quantity` | `decimal(18,3)` | — | ✅ | — |
| `UnitPrice` | `decimal(18,2)` | — | ✅ | Price at original sale |
| `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | — |
| `LineTotal` | `decimal(18,2)` | — | ✅ | — |
| `Mode` | `byte` | `1` (Retail) | ✅ | — |
| `**ReturnedBatchId**` | `**int? FK**` | `**null**` | ❌ | **NEW**: Batch returned to |
| `**UnitCost**` | `**decimal(18,2)?**` | `**null**` | ❌ | **NEW**: Cost at time of original sale |
| `Notes` | `nvarchar(200)?` | `null` | ❌ | — |

### 4.7 Quotation Status Enum — NEW

> See `Domain/Enums/QuotationStatus.cs` for the canonical enum definition.

### 4.8 Barcode POS Mode — Specification

**Continuous scan mode features:**
1. **Scanner input field** always focused (auto-refocus after each scan)
2. **Event-driven interception**: Scanner input is intercepted by the ViewModel **BEFORE** focus changes (RULE-233) via `Application.Current.Dispatcher` — the barcode string is captured from the focused input, looked up, and the product is auto-added before any UI focus event fires
3. **ISoundService feedback (RULE-231, Fix 5)**:
   - `ISoundService.PlaySuccess()` on valid barcode scan → product added
   - `ISoundService.PlayError()` on invalid/unknown barcode
   - `ISoundService.PlayWarning()` on quantity/price warnings
   - Audio feedback fires on: successful scan, quantity adjustments, pre-save validation dialogs, and save/post success events
4. **Auto-add**: Scanning barcode that exists → `BarcodeService.LookupAsync()` result auto-adds product line to invoice DataGrid — **no manual "Search" button click needed**
5. **If already in grid**: Increment quantity of existing line item (not duplicate)
6. **Scanner input auto-clear**: After each successful scan, the input field clears automatically for the next product — no user action required
7. **Quick total view**: Show running total + item count on scan bar
8. **Quantity override**: Type number before scan → e.g., type "5" then scan → adds 5 units
9. **Price override**: After scan, can tap item row to modify price/quantity before posting

---

## 5. Gap Analysis

### 5.1 Sales Invoice — Current vs Required

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| CurrencyId + ExchangeRate | ❌ Missing | ✅ Required (Analysis Part 5) | **ADD** to SalesInvoice entity + DTO + service |
| Multiple price levels per unit | ❌ Single default price | ✅ Show all available prices | **ADD** price picker in editor |
| Price override with approval | ❌ No approval flow | ✅ Manager approval below min | **ADD** PriceOverrideDialog |
| Enhanced profit display (per-item) | ❌ Basic total only | ✅ Per-item cost + profit | **ADD** CostPrice, UnitCost, ProfitAmount to items |
| Auto-journal entries on Post | ❌ Missing | ✅ Auto-create (Analysis Part 5) | **ADD** JournalEntryService call in PostAsync |
| Auto-customer receipt on cash | ❌ Missing | ✅ Auto-create receipt | **ADD** CustomerPayment creation in PostAsync |
| DiscountType (Amount/Percent) | ❌ Amount only | ✅ Both types | **ADD** DiscountType + DiscountValue fields |
| Additional charges (delivery, etc.) | ❌ Missing | ✅ Required (Analysis Part 5) | **ADD** AdditionalCharges field |
| Enhanced tax handling | ✅ TaxId FK | ✅ Include TaxRate in DTO | Minor enhancement |
| Draft invoice management screen | ❌ Filter only | ✅ Standalone list | **ADD** DraftInvoicesViewModel + View |
| TotalCost + TotalProfit on invoice | ❌ Missing | ✅ Show total profit | **ADD** compute on Post |
| Item OfficialPrice tracking | ❌ Missing | ✅ Track official vs actual | **ADD** OfficialPrice field |
| BatchAllocation tracking on items | ❌ Missing | ✅ Required for FIFO audit | **ADD** batch allocation records |
| Barcode continuous scan | ❌ Single scan | ✅ POS-style continuous | **ADD** BarcodeScannerControl |

### 5.2 Sales Quotation — Current vs Required

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Quotation entity + CRUD | ❌ Missing | ✅ NEW | **CREATE** from scratch |
| Quotation → Invoice conversion | ❌ Missing | ✅ NEW | **ADD** ConvertToInvoice method |
| Quotation lifecycle (5 states) | ❌ Missing | ✅ Draft→Sent→Accepted→Converted→Rejected | **ADD** enum + state machine |
| Valid until date | ❌ Missing | ✅ NEW | **ADD** date field |
| Terms and conditions | ❌ Missing | ✅ NEW | **ADD** text field |
| No stock/balance impact | ❌ Missing | ✅ By design | Ensured — no stock operations |
| Print quotation | ❌ Missing | ✅ Future | Deferred to print phase |
| Desktop screens | ❌ Missing | ✅ NEW | **CREATE** List + Editor VMs/Views |

### 5.3 Sales Return — Current vs Required

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| FIFO batch return | ❌ No batch tracking | ✅ Return to same batches | **ADD** ReturnedBatchId |
| Discount on return | ❌ Missing | ✅ Support discount | **ADD** DiscountAmount + DiscountType |
| Tax on return | ❌ Missing | ✅ Support tax reversal | **ADD** TaxAmount, TaxId |
| Additional charges | ❌ Missing | ✅ Support | **ADD** AdditionalCharges |
| Auto customer refund | ❌ Missing | ✅ Auto create refund payment | **ADD** CashBoxId, RefundAmount |
| Currency support | ❌ Missing | ✅ Multi-currency return | **ADD** CurrencyId, ExchangeRate |
| Return reason | ❌ Missing | ✅ User-facing reason | **ADD** ReturnReason |
| Unit cost at time of sale tracking | ❌ Missing | ✅ Needed for COGS reversal | **ADD** UnitCost field |
| Enhanced profit impact display | ❌ Missing | ✅ Show return P&L | **ADD** profit calculation |

---

## 6. Architectural Decisions

### 6.1 Price Override Rules

**Decision**: Price override is allowed with warnings/approvals:

| Scenario | Behaviour |
|----------|-----------|
| Price = official price | No warning, no approval |
| Price < official but > cost | Warning dialog: "السعر أقل من السعر الرسمي" (user acknowledges) |
| Price < cost | Warning + Manager approval required (PriceOverrideDialog with reason) |
| Setting: `PreventBelowRetailPrice = true` | Block price decrease entirely unless manager override |
| Setting: `AllowBelowCostSale = false` | Block below-cost sale entirely |

**Implementation**:
- `SalesInvoiceEditorViewModel` checks price when user modifies UnitPrice in grid
- If below threshold → calls `_dialogService.ShowPriceOverrideAsync()` (new dialog)
- On Post, `SalesInvoiceService` double-checks — validates PriceOverrideApprovedBy if price was overridden

### 6.2 Quotation Lifecycle

```text
Draft (1) → Sent (2) → Accepted (3) → Converted (4)
                     → Rejected (5)
                     → Expired (6) [auto when ValidUntil passed]

Auto-Expiry Check:
- Before ANY status transition or ConvertToInvoice, check ValidUntil
- If ValidUntil.HasValue && ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow)
  → Mark quotation as Expired (6)
  → Block conversion with: "انتهت صلاحية عرض السعر"
```

### 6.3 Batch Allocation Strategy

**Decision**: Batch allocation occurs at **Post time** (not at item-add time):

```text
1. Draft/Edit phase: User adds items, sets quantities → NO batch allocation yet
2. User clicks "Post" (ترحيل):
   a. Open transaction
   b. Save invoice (gets ID)
   c. For each item:
      - Call InventoryService.DecreaseStockWithBatchAllocationAsync()
      - Gets: List of (batchId, qty, unitCost)
      - Store allocations (serialized JSON or join table)
      - Compute item.UnitCost = totalCost / quantity
      - Compute item.ProfitAmount = LineTotal - totalCost
   d. Compute invoice.TotalCost = sum of item totalCost
   e. Compute invoice.TotalProfit = TotalAmount - TotalCost
   f. Create journal entries
   g. Update customer balance
   h. Create payment receipt (if cash)
   i. Commit
```

### 6.4 Real-Time Profit Calculation

**Decision**: Two modes controlled by SystemSetting `ShowProfitInInvoice`:
- **Enabled**: Show green/red profit per item row + invoice total profit
  - `Item.UnitCost` computed from batch allocation at Post time
  - `Item.ProfitAmount = LineTotal - (UnitCost × Quantity)`
  - Invoice.TotalProfit = TotalAmount - TotalCost
- **Disabled**: Profit hidden from sales screen, available in reports only

**Profit display in XAML**: 
- Green text for positive profit, Red for negative
- ToolTip: "الربح = {LineTotal:N2} - التكلفة {CostPrice:N2}"
- Icon: 📈 (profit) / 📉 (loss)

### 6.5 Draft Invoice Management

**Decision**: Dedicated "Draft Invoices" screen showing all invoices with Status = Draft:
- Shows: InvoiceNo, Customer, Date, Total, Age (days since creation)
- Actions: Edit (continue editing), Post (directly post), Delete (remove draft permanently)
- Auto-delete drafts older than configurable days (default: never auto-delete)
- List sorted by InvoiceDate descending (newest drafts first)

### 6.6 Why NOT a Separate Posting Screen

Following the analysis decision in Part 5 (lines 1065-1147): V1 uses two buttons ("Save as Draft" / "Save & Post") rather than a separate posting approval screen. This keeps the UI simple for the target market (small retail shops).

### 6.7 Sales Return → Customer Refund Strategy

| Payment Type of Original Invoice | Refund Behaviour |
|----------------------------------|------------------|
| Cash (PaymentType = 1) | Auto-create `CashTransaction` with `CashTransactionType.RefundOut` + reduce CashBox balance |
| Credit (PaymentType = 2) | Reduce Customer balance (DueAmount decreases) |
| Mixed (PaymentType = 3) | Cash refund for paid portion + reduce balance for unpaid |

> See `docs/AGENTS.md` §2.26 (CashBox rules, RULE-079: RunningBalance, RULE-082: immutable transactions). See `Application/Services/SalesReturnService.cs` for the canonical refund CashTransaction creation.

**If refund amount > customer balance**: Create credit balance (negative DueAmount) that customer can use for future purchases.

### 6.8 Customer Credit Limit Enforcement (HIGH — Phase 28 Fix 3)

**Rule**: Before posting a Sales Invoice, check if the customer has a credit limit configured.

> See `docs/AGENTS.md` for service layer patterns (RULE-202: return Result<T>) and transaction patterns (RULE-003: validate before transaction). See `Application/Services/SalesInvoiceService.cs` for the canonical credit limit check in PostAsync.

**Flow**:
1. Pre-validate credit limit BEFORE opening the Post transaction
2. If `projectedBalance > creditLimit` and user is Cashier → BLOCK with error message
3. If `projectedBalance > creditLimit` and user is Manager/Admin → show warning but ALLOW (override with permission)

**New error codes**:
- `ErrorCodes.CreditLimitExceeded = "CREDIT_LIMIT_EXCEEDED"`
- `ErrorCodes.QuotationExpired = "QUOTATION_EXPIRED"`

**Fields needed on Customer entity**:
- `decimal? CreditLimit` — maximum allowed balance (null = no limit)
- `CustomerType` — `Normal` / `Credit` (to distinguish cash vs credit customers)

### 6.9 Why INotifyDataErrorInfo for Validation

Following RULE-228/229: SalesInvoiceEditorViewModel already uses validation patterns. Enhanced validation will:
- Use `ClearAllErrors()` + `AddError()` + `await ValidateAllAsync()` on Pre-Post
- Save always enabled (no CanExecute) — RULE-059
- Show validation dialog listing ALL errors before Post

---

## 7. Non-V1 Items (Deferred)

These features were identified in analysis but are **deferred** to future versions:

| Feature | Reason |
|---------|--------|
| **Customer loyalty/rewards system** | Requires customer analytics module — future phase |
| **Gift cards** | Separate payment instrument — needs gift card entity + clearing |
| **Mobile POS (Android/iOS)** | Platform-specific — separate product, not V1 |
| **E-invoice integration (ZATCA/FATOORA)** | Saudi compliance — requires government API integration, future phase |
| **Online store / e-commerce sync** | External integration — requires web API, separate phase |
| **Customer behavior analytics from drafts** | Analysis Part 5 (lines 1248-1278) — deferred |
| **Digital signature on invoice** | Security feature — future phase |
| **Recurring invoices / subscriptions** | Requires scheduling engine — not V1 |
| **Multi-branch / multi-store** | Requires branch entity + separate inventory — future phase |
| **Offline mode / sync** | Complex — requires local queue + conflict resolution |
| **Touch POS / touchscreen mode** | Analysis shows `TouchPosViewModel` already exists — but full touch mode will be enhanced later |
| **Quotation print** | Print module can be enhanced later |
| **Price list by customer group** | Requires customer group entity — future phase |

---

## 8. Implementation Tasks

All tasks include:
- **Logging**: `Log.Information()` / `LogSystemError()` via RULE-199
- **Error handling**: Result<T> pattern (RULE-006), catch DbUpdateException (RULE-200)
- **ToolTips**: Arabic ToolTips on ALL interactive controls (RULE-185-190)
- **UI Compact**: No hardcoded Height=36/40, style defaults at 28px (RULE-262-274)
- **Validation**: INotifyDataErrorInfo (RULE-228-229), Save always enabled (RULE-059)
- **Arabic strings**: Valid UTF-8 encoding (RULE-249-253)

**Total estimate**: ~40 hours (broken down per task below)

---

### Task 1 — Add CurrencyId + ExchangeRate to SalesInvoice & Item

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoice.cs` | Add `int? CurrencyId`, `decimal ExchangeRate`, `decimal BaseCurrencyTotal`, nav property `Currency` |
| `Domain/Entities/SalesInvoice.cs` | Add `SetCurrency(int? currencyId, decimal exchangeRate)` method |
| `Domain/Entities/SalesInvoiceItem.cs` | Add `int? ProductUnitId`, `decimal UnitCost`, `decimal ProfitAmount` |
| `Infrastructure/Data/Configurations/SalesInvoiceConfiguration.cs` | Add FK config for CurrencyId: `DeleteBehavior.Restrict` (RULE-214) |
| `Infrastructure/Data/Configurations/SalesInvoiceItemConfiguration.cs` | Add ProductUnitId FK config, `HasPrecision(18,2)` for UnitCost/ProfitAmount |
| `Infrastructure/Data/Migrations/` | NEW migration: ALTER TABLE SalesInvoices ADD + FKs |
| `Contracts/DTOs/AllDtos.cs` | Add `CurrencyId`, `CurrencyName`, `ExchangeRate`, `BaseCurrencyTotal`, `TotalCost`, `TotalProfit` to SalesInvoiceDto |
| `Contracts/DTOs/AllDtos.cs` | Add `ProductUnitId`, `ProductUnitName`, `UnitCost`, `ProfitAmount` to SalesInvoiceItemDto |
| `Contracts/Responses/SalesInvoiceResponses.cs` | Add currency fields |
| `Contracts/Requests/SalesInvoiceRequests.cs` | Add `int? CurrencyId`, `decimal ExchangeRate` to requests |
| `Application/Services/SalesInvoiceService.cs` | Map currency fields in DTO conversion |
| `DesktopPWF/Services/Api/SalesInvoiceApiService.cs` | Update API client methods |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/SalesInvoice.cs` for the canonical `SetCurrency()` method.

**Logging**: `Log.Information("Invoice {InvoiceNo}: Currency set to {CurrencyId} @ rate {ExchangeRate}", invoiceNo, currencyId, exchangeRate)`

**Estimate**: ~2 hours

---

### Task 2 — Enhanced Price Selection (Multiple Price Levels per Unit)

**Objective**: When user selects a product + unit, show all available prices for that combination and auto-select the active one.

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `AvailablePrices` list to product response |
| `Contracts/DTOs/AllDtos.cs` | Add `ProductPriceDto(int Id, int ProductUnitId, int CurrencyId, decimal Price, string PriceLevelName, bool IsActive)` |
| `DesktopPWF/Services/Api/ProductApiService.cs` | Add endpoint `GET /api/v1/products/{id}/prices?unitId={unitId}&currencyId={currencyId}` |
| `DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | Add `_availablePrices` ObservableCollection, `OnProductSelected` auto-select best price |
| `DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` | Add price dropdown showing available prices + active price indicator |
| `Api/Controllers/ProductsController.cs` | Add prices endpoint |

**Price selection logic**:
> See `docs/AGENTS.md` for ViewModel patterns (ExecuteAsync wrapper, RULE-141). See `Desktop/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` for the canonical price loading implementation.

**Estimate**: ~2 hours

---

### Task 3 — Price Override with Manager Approval

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoiceItem.cs` | Add `int? PriceOverrideApprovedBy`, `string? PriceOverrideReason`, `decimal? OfficialPrice` |
| `Domain/Entities/SalesInvoiceItem.cs` | Add `SetPriceOverride(decimal newPrice, int? approvedBy, string? reason)` |
| `Infrastructure/Data/Configurations/SalesInvoiceItemConfiguration.cs` | Add HasMaxLength for reason |
| `Contracts/DTOs/AllDtos.cs` | Add override fields to SalesInvoiceItemDto |
| `DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml` | **NEW**: Styled WPF dialog with warning level, price comparison, reason text |
| `DesktopPWF/Views/Dialogs/PriceOverrideDialog.xaml.cs` | Dialog logic |
| `DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | Add price validation on item add/edit |
| `Application/Services/SalesInvoiceService.cs` | Validate override on Post |
| `DesktopPWF/Services/App/IDialogService.cs` | Add `ShowPriceOverrideAsync(PriceOverrideRequest)` method |
| `DesktopPWF/Services/App/DialogService.cs` | Implement price override dialog |

> See `docs/AGENTS.md` §2.43 (UI ToolTips RULE-185-190) and §2.64 (UI Compacting RULE-262-274). See `Desktop/Views/Dialogs/PriceOverrideDialog.xaml` for the canonical dialog pattern.

**Estimate**: ~3 hours

---

### Task 4 — Enhanced Profit Display (Per-Item + Total)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoice.cs` | Add `TotalCost` and `TotalProfit` computed properties |
| `Domain/Entities/SalesInvoiceItem.cs` | Add `UnitCost` and `ProfitAmount` |
| `Application/Services/SalesInvoiceService.cs` | Compute costs from batch allocation during Post |
| `Contracts/DTOs/AllDtos.cs` | Add `UnitCost`, `ProfitAmount` to SalesInvoiceItemDto; `TotalCost`, `TotalProfit` to SalesInvoiceDto |
| `DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | Add profit display properties + visibility toggle (based on ShowProfitInInvoice setting) |
| `DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` | Add profit columns in DataGrid (green/red) |

> See `docs/AGENTS.md` §2.2 for financial formulas. See `Application/Services/SalesInvoiceService.cs` for the canonical profit calculation in PostAsync.

**XAML profit column**:
> See `docs/AGENTS.md` §2.64 (UI Compacting Rules). See `Desktop/Views/Sales/SalesInvoiceEditorView.xaml` for the canonical profit column pattern.

**Estimate**: ~2 hours

---

### Task 5 — Auto-Journal Entry Creation on Sales Post

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesInvoice.cs` | Add `int? JournalEntryId`, `JournalEntry? JournalEntry` nav property |
| `Application/Interfaces/IJournalEntryService.cs` | Add `CreateSalesInvoiceEntriesAsync(SalesInvoice, costAllocations)` if not exists |
| `Application/Services/JournalEntryService.cs` | Implement journal entry creation for sales |
| `Application/Services/SalesInvoiceService.cs` | Call `_journalEntryService.CreateSalesInvoiceEntriesAsync()` inside Post transaction |
| `Infrastructure/Data/Configurations/SalesInvoiceConfiguration.cs` | Add FK for JournalEntryId (Restrict) |

> See `docs/AGENTS.md` §2.76 (Phase 24 — Accounting Integration Rules, RULE-373) for canonical journal entry templates for sales.

**Condition**: Only create if SystemSetting `AutoCreateJournalEntry = true`

**Estimate**: ~3 hours

---

### Task 6 — Sales Return Enhancement (FIFO Batch Return, Discount, Auto Refund)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesReturn.cs` | Add: `decimal DiscountAmount`, `decimal TaxAmount`, `decimal AdditionalCharges`, `decimal TotalAmount`, `decimal RefundAmount`, `int? CashBoxId`, `int? CurrencyId`, `decimal ExchangeRate`, `string? ReturnReason` |
| `Domain/Entities/SalesReturn.cs` | Add `SetRefund(decimal refundAmount, int? cashBoxId)`, `SetCurrency(currencyId, rate)` |
| `Domain/Entities/SalesReturnItem.cs` | Add: `int? ProductUnitId`, `int? ReturnedBatchId`, `decimal? UnitCost` |
| `Infrastructure/Data/Configurations/` | Updated configs for new fields + FKs |
| `Contracts/DTOs/AllDtos.cs` | Enhanced SalesReturnDto + SalesReturnItemDto |
| `Contracts/Requests/SalesReturnRequests.cs` | Enhanced requests |
| `Application/Services/SalesReturnService.cs` | Enhanced PostAsync: return to FIFO batches, create refund payment, create journal entries |
| `Application/Services/SalesReturnService.cs` | Add batch return logic: `ReturnToBatchAsync(batchId, qty)` |
| `DesktopPWF/ViewModels/Returns/SalesReturnEditorViewModel.cs` | Add: CashBox selector, discount fields, batch return display |
| `DesktopPWF/Views/Returns/SalesReturnEditorView.xaml` | Enhanced: refund section, batch info, discount fields |

> See `docs/AGENTS.md` §2.7 (Stock Integrity) and `Application/Services/SalesReturnService.cs` for the canonical batch return logic.

> See `docs/AGENTS.md` §2.26 (CashBox rules, RULE-079: RunningBalance, RULE-082: immutable transactions). See `Application/Services/SalesReturnService.cs` for the canonical refund logic.

**Estimate**: ~4 hours

---

### Task 7 — Auto Customer Receipt Creation on Cash Sales

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/ICustomerPaymentService.cs` | Add `CreateFromSalesInvoiceAsync(SalesInvoice)` method if not exists |
| `Application/Services/CustomerPaymentService.cs` | Implement auto-creation of payment receipt |
| `Application/Services/SalesInvoiceService.cs` | Call auto-payment creation inside Post transaction |

> See `docs/AGENTS.md` for transaction patterns (RULE-003) and `docs/CONSTITUTION.md` for Result<T>. See `Application/Services/SalesInvoiceService.cs` for the canonical auto-payment logic in PostAsync.

**Estimate**: ~2 hours

---

### Task 8 — Sales Quotation Entity + CRUD (NEW Module)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/SalesQuotation.cs` | **NEW**: Full entity with QuotationStatus enum support |
| `Domain/Entities/SalesQuotationItem.cs` | **NEW**: Line item entity |
| `Domain/Enums/QuotationStatus.cs` | **NEW**: Draft=1, Sent=2, Accepted=3, Converted=4, Rejected=5 |
| `Infrastructure/Data/Configurations/SalesQuotationConfiguration.cs` | **NEW**: FK → Customer (optional), Warehouse, Currency; HasMaxLength |
| `Infrastructure/Data/Configurations/SalesQuotationItemConfiguration.cs` | **NEW**: FK → Quotation, Product, ProductUnit |
| `Infrastructure/Data/Migrations/` | NEW migration: CREATE TABLE |
| `Contracts/DTOs/SalesQuotationDto.cs` | **NEW**: DTO with items |
| `Contracts/Requests/SalesQuotationRequests.cs` | **NEW**: CreateSalesQuotationRequest, UpdateSalesQuotationRequest |
| `Application/Interfaces/ISalesQuotationService.cs` | **NEW**: CRUD interface with Result<T> |
| `Application/Services/SalesQuotationService.cs` | **NEW**: CRUD implementation (NO stock/balance impact) |
| `Application/Services/SalesQuotationService.cs` | Add `ConvertToInvoiceAsync(int quotationId)` — creates SalesInvoice from quote |
| `Api/Controllers/SalesQuotationsController.cs` | **NEW**: REST API with `[Authorize]` (AllStaff) |
| `DesktopPWF/Services/Api/SalesQuotationApiService.cs` | **NEW**: HTTP client |
| `DesktopPWF/ViewModels/Sales/SalesQuotationListViewModel.cs` | **NEW**: List VM with newest-first sort (RULE-220) |
| `DesktopPWF/Views/Sales/SalesQuotationsListView.xaml` | **NEW**: DataGrid + ToolTips |
| `DesktopPWF/Views/Sales/SalesQuotationsListView.xaml.cs` | **NEW**: Code-behind |
| `DesktopPWF/ViewModels/Sales/SalesQuotationEditorViewModel.cs` | **NEW**: Editor VM with INotifyDataErrorInfo (RULE-228) |
| `DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml` | **NEW**: Compact form (RULE-262-274) |
| `DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml.cs` | **NEW**: Code-behind |
| `DesktopPWF/Messaging/Messages/AppMessages.cs` | **NEW**: `SalesQuotationChangedMessage` record |
| `DesktopPWF/App.xaml.cs` | DI registrations + navigation |
| `Api/Program.cs` | DI registrations |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/SalesQuotation.cs` for the canonical entity definition.

> See `docs/AGENTS.md` for transaction patterns (RULE-003/RULE-005) and `docs/CONSTITUTION.md` for the Result<T> pattern. See `Application/Services/SalesQuotationService.cs` for the canonical `ConvertToInvoiceAsync()` implementation.

**Estimate**: ~6 hours

---

### Task 9 — Barcode Continuous Scan Mode for Fast Entry

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Controls/BarcodeScannerControl.xaml` | **NEW**: Scanner input with auto-focus |
| `DesktopPWF/Controls/BarcodeScannerControl.xaml.cs` | **NEW**: Continuous scan logic, quantity prefix |
| `DesktopPWF/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | Integrate BarcodeScannerControl, add scan handler |
| `DesktopPWF/Services/App/BarcodeInputService.cs` | Enhance with continuous scan mode setting |
| `DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` | Add BarcodeScannerControl at top of editor |
| `DesktopPWF/Services/App/ISoundService.cs` | **Fix 5**: Ensure `PlaySuccess()`, `PlayError()`, `PlayWarning()` are implemented and wired to all scan/validation/save events (RULE-231). Inject into BarcodeScannerControl and SalesInvoiceEditorViewModel |

> See `docs/AGENTS.md` §2.56 (Barcode Scanning, RULE-233/234) and §2.55 (Sound Service, RULE-231/232). See `Desktop/Controls/BarcodeScannerControl.xaml.cs` and `Desktop/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` for the canonical scan logic.

**Estimate**: ~3 hours

---

### Task 10 — Enhanced SalesInvoiceEditorViewModel

**Objective**: Integrate ALL new features into the existing 1486-line editor ViewModel.

**Changes**:

| Component | Change |
|-----------|--------|
| **Currency selector** | Add `ObservableCollection<CurrencyDto> Currencies`, `SelectedCurrency` property, auto-convert prices |
| **Price level picker** | Show dropdown of available prices when product selected |
| **Profit display** | Add `TotalCost`, `TotalProfit` computed properties, visibility based on settings |
| **Price override** | Hook line item price change → show PriceOverrideDialog if below threshold |
| **Barcode scanner** | Integrate BarcodeScannerControl, handle continuous scan — wire `ISoundService.PlaySuccess()` on scan, `PlayError()` on invalid barcode, `PlayWarning()` on validation warnings (Fix 5, RULE-231) |
| **Additional charges** | Add `AdditionalCharges` field, include in TotalAmount calculation |
| **DiscountType** | Support Amount/Percent toggle + DiscountValue field |
| **Enhanced validation** | Call `ClearAllErrors()` + `ValidateAllAsync()` on Pre-Post (RULE-229) |
| **Draft age display** | Show days since creation for draft invoices |
| **Post validation** | Validate: stock availability, price override approvals, **customer credit limit enforcement (Fix 3)** — if customer has `CreditLimit`, check `currentBalance + invoiceTotal <= creditLimit` before allowing Post; manager override allowed with warning |
| **Auto-focus** | `FocusFirstInvalidFieldRequested` event for validation (RULE-059) |

> See `docs/AGENTS.md` for ViewModel patterns (ExecuteAsync, RULE-141; INotifyDataErrorInfo, RULE-228). See `Desktop/ViewModels/Sales/SalesInvoiceEditorViewModel.cs` for the canonical implementation.

**Estimate**: ~4 hours

---

### Task 11 — Updated SalesInvoiceEditorView.xaml (Compact + ToolTips)

**Objective**: Restyle the editor view with compact dimensions (RULE-262-274) and comprehensive Arabic ToolTips (RULE-185-190).

> See `docs/AGENTS.md` §2.64 (UI Compacting Rules RULE-262-274) and §2.43 (UI ToolTips RULE-185-190). See `Desktop/Views/Sales/SalesInvoiceEditorView.xaml` for the canonical compact layout.

**ToolTips required (all buttons)** (RULE-185-190):

| Control | ToolTip |
|---------|---------|
| Save as Draft | "حفظ الفاتورة كمسودة — لا تؤثر على المخزون أو الحسابات" |
| Post | "ترحيل الفاتورة — سيتم خصم المخزون وتحديث رصيد العميل وإنشاء القيود المحاسبية" |
| Cancel Invoice | "إلغاء الفاتورة — لا يمكن التراجع عن الإلغاء" |
| Add Item | "إضافة صنف جديد للفاتورة" |
| Remove Item | "حذف الصنف من الفاتورة" |
| Barcode scan | "مسح باركود الصنف — يمكن إدخال الكمية قبل المسح" |
| Currency selector | "اختيار عملة الفاتورة — الأسعار حسب العملة المحددة" |
| Price override | "تعديل سعر البيع — يتطلب موافقة المدير إذا كان أقل من السعر الرسمي" |
| Print | "طباعة الفاتورة — A4 أو 80mm حسب الإعدادات" |
| Customer selector | "اختيار العميل — إذا كان البيع نقدياً سيتم اختيار العميل النقدي الافتراضي" |
| Discount type | "نوع الخصم — مبلغ ثابت أو نسبة مئوية" |

**Estimate**: ~3 hours

---

### Task 12 — Sales Quotation Desktop Screens (List + Editor)

**Files**: See Task 8 file list (all new files).

**ViewModel patterns** (RULE-141):
- All async commands use `ExecuteAsync()` wrapper
- `LogSystemError()` for logging (RULE-199)
- `IDialogService` for all user messages (RULE-174)
- Arabic ToolTips on all buttons (RULE-185-190)
- Compact UI styles (RULE-262-274)
- `INotifyDataErrorInfo` in editor (RULE-228)

> See `docs/AGENTS.md` for ViewModel patterns (RULE-141: ExecuteAsync, RULE-220: newest-first sort, RULE-228: INotifyDataErrorInfo, RULE-227: SetDialogService(), RULE-059: no CanExecute). See `Desktop/ViewModels/Sales/SalesQuotationListViewModel.cs` and `SalesQuotationEditorViewModel.cs` for canonical implementations.

**Estimate**: ~4 hours

---

### Task 13 — Draft Invoice Management Screen

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/ViewModels/Sales/DraftInvoicesViewModel.cs` | **NEW**: List drafts with age, Edit/Post/Delete actions |
| `DesktopPWF/Views/Sales/DraftInvoicesView.xaml` | **NEW**: DataGrid with action buttons |
| `DesktopPWF/Views/Sales/DraftInvoicesView.xaml.cs` | **NEW**: Code-behind |
| `DesktopPWF/App.xaml.cs` | DI registration + navigation entry |

**Features**:
- Shows ONLY invoices with Status = Draft
- Columns: InvoiceNo, Customer, Date, Total, Age (days), Actions
- Actions: Edit (open editor), Post (direct post), Delete (permanent with warning)
- Newest-first sort (InvoiceDate descending)
- Search by InvoiceNo or CustomerName

**Estimate**: ~2 hours

---

### Task 14 — API Endpoint Updates

**Files**: `SalesInvoicesController.cs`, `SalesReturnsController.cs`, `SalesQuotationsController.cs` (NEW)

**New/Updated Endpoints**:

| Method | Endpoint | Change | Policy |
|--------|----------|--------|--------|
| `POST` | `/api/v1/sales/quotations` | **NEW** | `AllStaff` |
| `GET` | `/api/v1/sales/quotations` | **NEW** | `AllStaff` |
| `GET` | `/api/v1/sales/quotations/{id}` | **NEW** | `AllStaff` |
| `PUT` | `/api/v1/sales/quotations/{id}` | **NEW** | `AllStaff` |
| `DELETE` | `/api/v1/sales/quotations/{id}` | **NEW** (soft) | `ManagerAndAbove` |
| `POST` | `/api/v1/sales/quotations/{id}/send` | **NEW** (mark Sent) | `AllStaff` |
| `POST` | `/api/v1/sales/quotations/{id}/accept` | **NEW** (mark Accepted) | `AllStaff` |
| `POST` | `/api/v1/sales/quotations/{id}/convert` | **NEW** (→ Invoice) | `AllStaff` |
| `POST` | `/api/v1/sales/quotations/{id}/reject` | **NEW** | `AllStaff` |
| `GET` | `/api/v1/products/{id}/prices?unitId=&currencyId=` | **NEW** | `AllStaff` |
| `GET` | `/api/v1/sales/invoices/drafts` | **NEW** (filter drafts) | `AllStaff` |
| `POST` | `/api/v1/sales/invoices/{id}/post` | **ENHANCED** (journal entry + payment) | `AllStaff` |
| `GET` | `/api/v1/sales/reports/profit` | **NEW** | `ManagerAndAbove` |
| `GET` | `/api/v1/sales/reports/by-customer` | **NEW** | `ManagerAndAbove` |
| `GET` | `/api/v1/sales/reports/by-product` | **NEW** | `ManagerAndAbove` |

**Controller purity** (RULE-203): All controllers inject Application services only — NO `DbContext` or `IUnitOfWork`.

**Estimate**: ~2 hours

---

### Task 15 — FluentValidation Updates

**Files**: Create/update validators in `Api/Validators/`

| Validator | Change |
|-----------|--------|
| `CreateSalesInvoiceRequestValidator` | Add CurrencyId (nullable), ExchangeRate (> 0 if CurrencyId set), AdditionalCharges (>= 0) |
| `UpdateSalesInvoiceRequestValidator` | Same |
| `CreateSalesReturnRequestValidator` | **NEW**: ReturnNo required, CustomerId or SalesInvoiceId required, RefundAmount check |
| `CreateSalesQuotationRequestValidator` | **NEW**: QuotationNo required (> 0), WarehouseId required, ValidUntil cannot be in past |
| `UpdateSalesQuotationRequestValidator` | **NEW**: Same |
| `PostInvoiceRequestValidator` | **ENHANCED**: Validate stock, validate price overrides |

> See `docs/AGENTS.md` for validation patterns (RULE-044: FluentValidation for EVERY Command). See `Api/Validators/CreateSalesQuotationRequestValidator.cs` for the canonical validator.

**Estimate**: ~1 hour

---

### Task 16 — Sales Reports Endpoints

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/SalesReportsController.cs` | **NEW**: Sales report endpoints |
| `Application/Interfaces/ISalesReportService.cs` | **NEW**: Report service interface |
| `Application/Services/SalesReportService.cs` | **NEW**: Report queries |
| `Contracts/DTOs/SalesReportDtos.cs` | **NEW**: Report DTOs |

**Endpoints**:

| Method | Endpoint | Description | Policy |
|--------|----------|-------------|--------|
| `GET` | `/api/v1/sales/reports/profit?from=&to=&warehouseId=` | Profit report (total + per invoice) | `ManagerAndAbove` |
| `GET` | `/api/v1/sales/reports/by-customer?from=&to=` | Sales grouped by customer | `ManagerAndAbove` |
| `GET` | `/api/v1/sales/reports/by-product?from=&to=` | Sales grouped by product | `ManagerAndAbove` |
| `GET` | `/api/v1/sales/reports/daily?from=&to=` | Daily sales summary | `ManagerAndAbove` |

> See `docs/AGENTS.md` §2.83 (Phase 31 — Reports Module Rules, RULE-421). See `SalesSystem.Contracts/` for canonical report DTOs.

**Estimate**: ~3 hours

---

### Task 17 — Self-Explanation ◉ Tooltips (Sales Module)

**Objective**: Add ⓘ (InfoTooltip) controls next to key terms in the Sales module UI. Each tooltip provides an Arabic explanation of the term using the same `InfoTooltip` UserControl pattern from Phase 18.

**Pattern**: `◉` icon with styled ToolTip bound to `HelpText` property.

**Where to add**: Sales Invoice editor, Sales Quotation editor, Sales Return editor, Barcode POS view — next to section headers, labels, and DataGrid column headers.

**ⓘ Terms table (Sales)**:

| Term | Explanation (Arabic) |
|------|---------------------|
| فاتورة بيع | "فاتورة البيع هي مستند يثبت قيام الشركة ببيع بضاعة أو تقديم خدمة للعميل." |
| عرض سعر | "عرض السعر هو مستند أولي يرسل للعميل قبل إتمام عملية البيع. يمكن تحويله لفاتورة عند الموافقة." |
| سعر البيع | "سعر البيع هو المبلغ الذي يدفعه العميل مقابل المنتج. يمكن تعديله داخل الفاتورة لكنه لا يغير السعر الرسمي." |
| الربح | "الربح = سعر البيع - تكلفة البضاعة. يظهر لحظياً في فاتورة البيع." |
| عميل نقدي | "العميل النقدي هو عميل افتراضي يستخدم عند البيع نقداً بدون تحديد عميل حقيقي." |
| مرتجع بيع | "مرتجع البيع هو استرجاع بضاعة من العميل. يزيد المخزون ويقلل المبلغ المستحق من العميل." |
| خصم | "الخصم هو تخفيض في السعر. يمكن أن يكون على مستوى السطر (خصم على صنف معين) أو على مستوى الفاتورة (خصم إجمالي)." |
| السقف الائتماني | "السقف الائتماني هو أقصى مبلغ يمكن أن يدين به العميل للشركة. عند تجاوزه يظهر تحذير." |
| باركود | "الباركود هو رمز شريطي يقرأ بواسطة الماسح الضوئي لتحديد المنتج بسرعة. يمكن مسحه ضوئياً لإضافة المنتج للفاتورة." |
| تصفية مخزون | "عند تفعيل التصفية، يمكن بيع المنتج بسعر أقل من التكلفة بدون تحذير. تستخدم لتصريف البضاعة القديمة." |

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Controls/InfoTooltip.xaml` + `.cs` | ✅ Already exists (from Phase 18) — no changes needed |
| `DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` | Add `ⓘ` tooltips next to section headers: فاتورة بيع, سعر البيع, الربح, خصم, باركود, السقف الائتماني |
| `DesktopPWF/Views/Sales/SalesQuotationEditorView.xaml` | Add `ⓘ` tooltip next to: عرض سعر |
| `DesktopPWF/Views/Sales/SalesReturnEditorView.xaml` | Add `ⓘ` tooltip next to: مرتجع بيع |
| `DesktopPWF/Views/Sales/SalesInvoicesListView.xaml` | Add `ⓘ` tooltips next to column headers |

> See `docs/AGENTS.md` §2.43 (UI ToolTips). See `Desktop/Controls/InfoTooltip.xaml` for the canonical tooltip control pattern.

**Estimate**: ~1 hour

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | All SalesInvoice/SalesReturn money fields | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | Item Quantity fields | ✅ |
| **RULE-003** | Multi-table ops in transaction | PostAsync, CancelAsync, ConvertToInvoiceAsync | ✅ |
| **RULE-004** | NO hard delete for invoices | SalesInvoice: Cancel sets Status=Cancelled | ✅ |
| **RULE-005** | Stock deducted AFTER invoice saved | PostAsync: Save → Get ID → Deduct stock | ✅ |
| **RULE-006** | ALL services return `Result<T>` | SalesInvoiceService, SalesQuotationService, etc. | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | Notes, Reasons, Terms fields | ✅ |
| **RULE-016** | BaseEntity audit fields | All entities inherit BaseEntity | ✅ |
| **RULE-019** | InvoiceStatus: Draft=1, Posted=2, Cancelled=3 | SalesInvoice, SalesReturn | ✅ |
| **RULE-020** | Stock/balance affected only when Posted | PostAsync = stock + balance; Draft = no impact | ✅ |
| **RULE-021** | NO editing Posted invoices | Guard in AddItem/RemoveItem | ✅ |
| **RULE-024** | IUnitOfWork for multi-table ops | All services inject IUnitOfWork | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information/Error | ✅ |
| **RULE-036** | Log critical operations | Invoice creation, posting, cancel, price override | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All controllers | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | SalesInvoice.Create, SetCurrency, Post, Cancel; SalesQuotation.Create, MarkAsSent | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | All request validators | ✅ |
| **RULE-050** | DeleteStrategy enum | Draft deletion, quotation deletion | ✅ |
| **RULE-052** | Guard Clauses on all entities | All factory/domain methods throw DomainException with Arabic messages | ✅ |
| **RULE-053** | DomainException in Arabic | All Arabic messages | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified | ✅ |
| **RULE-058** | INotifyDataErrorInfo | SalesQuotationEditorViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | No CanExecute predicates (RULE-059) | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModels | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | All editor windows open via OpenScreen() | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ الفاتورة"`, `"تنبيه عرض السعر"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | Verified | ✅ |
| **RULE-175** | ALL dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, price warnings | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | All ApiServices | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All new/modified XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إضافة صنف جديد للفاتورة" ✅ not "إضافة" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل الفاتورة — سيتم خصم المخزون وتحديث رصيد العميل وإنشاء القيود المحاسبية" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "المبيعات — إدارة فواتير البيع وعروض الأسعار والمرتجعات" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة أول فاتورة بيع جديدة" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() in ViewModels | All ViewModels use LogSystemError() | ✅ |
| **RULE-200** | Hard-delete catch DbUpdateException → Result.Failure | Draft deletion, quotation deletion | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | Verified | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | All Application services | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | All controllers inject services only | ✅ |
| **RULE-210** | CHECK constraints at DB level | Quantity >= 0 on WarehouseStocks | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | All SalesInvoice, SalesItem, Quotation configs | ✅ |
| **RULE-220** | Newest-first sorting on lists | All list ViewModels: OrderByDescending(Id) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | All editor ViewModels | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor ViewModels | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation | ✅ |
| **RULE-230** | Validation ErrorTemplate (red border + ❗) | All TextBox/ComboBox in editor views | ✅ |
| **RULE-231** | ISoundService for audio feedback | Barcode scan success/error | ✅ |
| **RULE-232** | Audio on scan, validation, save/post | BarcodeScannerControl, validation dialogs | ✅ |
| **RULE-233** | Continuous scanning without closing editor | BarcodeScannerControl auto-refocus | ✅ |
| **RULE-249** | UTF-8 Arabic string literals | All Arabic strings verified | ✅ |
| **RULE-250** | Files saved with UTF-8 encoding | All .cs/.xaml files | ✅ |
| **RULE-251** | Fix garbled Arabic if found | Flag any mojibake at review | ✅ |
| **RULE-254** | InvoiceNo is int, NOT string | SalesInvoice.InvoiceNo, SalesQuotation.QuotationNo | ✅ |
| **RULE-255** | Auto-generate InvoiceNo via IDocumentSequenceService.GetNextIntAsync("SalesInvoice", ct) | Service logic | ✅ |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new/modified XAML: compact 28px via style | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All XAML: 10,4 via style | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new/modified views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Form fields in editor views | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | PriceOverrideDialog, validation dialogs | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All section headers | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state views | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | All screen windows | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| # | Risk | Impact | Mitigation |
|---|------|--------|------------|
| 1 | **Multi-currency without Currency entity** (Phase 20 not complete) | **HIGH** — blocks Task 1 | Must verify Currency entity + CurrencyRates exist before starting |
| 2 | **FIFO batch allocation not yet implemented** (Phase 25 incomplete) | **HIGH** — blocks Tasks 4, 6 | If Phase 25 not done, Tasks 4/6 must use WeightedAverage fallback |
| 3 | **Chart of Accounts not complete** (blocks journal entries) | **HIGH** — blocks Task 5 | Verify JournalEntryService + Account entity exist; fallback: skip journal creation |
| 4 | **PriceOverrideDialog WPF threading** | Medium | Use `Application.Current.Dispatcher.InvokeAsync()` (RULE-165) |
| 5 | **Large ViewModel (1486 lines) modification** | Medium | Refactor into partial class or dedicated services; unit test after each change |
| 6 | **Continuous barcode scan conflicts with keyboard input** | Medium | Use configurable prefix/suffix from scanner; debounce rapid scans |
| 7 | **Quotation → Invoice conversion mismatch** | Medium | Full data validation before conversion; all fields must map |
| 8 | **Sales return refund to CashBox with insufficient balance** | Medium | Validate CashBox.CurrentBalance >= RefundAmount before creating refund transaction |
| 9 | **Migration conflicts with existing DB state** | Low | All new columns nullable/additive; no breaking schema changes |
| 10 | **Performance: batch allocation on large invoices** | Low | Batch allocation queries use indexed columns (ProductId, WarehouseId, ExpiryDate) |
| 11 | **Price override approval stored but not checked on Post** | Medium | Service-layer validation double-checks: `if price < official && !Approved → fail` |
| 12 | **Duplicate quotation numbers** | Low | QuotationNo uses same pattern as InvoiceNo (auto-generated via IDocumentSequenceService.GetNextIntAsync, unique) |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| **Multi-currency breaks invoice posting** | `ALTER TABLE SalesInvoices DROP COLUMN CurrencyId, ExchangeRate, BaseCurrencyTotal;` + revert migration |
| **Price override workflow too complex for V1** | Keep PriceOverrideDialog but remove approval requirement — just show warning dialog, no approval needed |
| **Auto-journal entries cause incorrect postings** | Set `AutoCreateJournalEntry = false` in SystemSettings; remove JournalEntryId from invoice |
| **Sales quotation not needed** | Drop table: `DROP TABLE SalesQuotationItems; DROP TABLE SalesQuotations;` remove all files |
| **Barcode scan mode not stable** | Remove BarcodeScannerControl from editor XAML; keep regular search as fallback |
| **Sales return batch return fails** | Disable batch return logic; use simple stock increase without batch tracking |
| **Draft management screen not needed** | Remove DraftInvoicesView/ViewModel; remove navigation entry |
| **All enhancements cause issues** | Revert all Phase 28 migrations: `Remove-Migration` (EF Core) |
| **Critical bug in production** | Restore from backup: `RESTORE DATABASE SalesSystem FROM DISK = 'backup.bak' WITH REPLACE, RECOVERY;` |

---

## 12. Unit Test Tasks

### T17 — Domain Entity Tests (Sales Module)

| ID | Test | Expected |
|----|------|----------|
| T17.01 | `SalesInvoice.Create()` with valid args → entity created with correct InvoiceNo (int), status Draft | No exception |
| T17.02 | `SalesInvoice.Create()` with invoiceNo ≤ 0 → `DomainException("رقم الفاتورة يجب أن يكون أكبر من صفر")` | Arabic message |
| T17.03 | `SalesInvoice.Create()` with null customerId → `DomainException` | Arabic message |
| T17.04 | `SalesInvoice.Post()` transitions from Draft→Posted | Property check |
| T17.05 | `SalesInvoice.Cancel()` transitions from Draft→Cancelled, Posted→Cancelled | Property check |
| T17.06 | `SalesInvoice.Post()` when already Cancelled → `DomainException` | Arabic message |
| T17.07 | `SalesInvoice.Cancel()` when already Cancelled → `DomainException` | Arabic message |
| T17.08 | `SalesQuotation.Create()` with valid args → status Draft, ValidUntil set | No exception |
| T17.09 | `SalesQuotation.Send()` → status Sent | Property check |
| T17.10 | `SalesQuotation.Accept()` → status Accepted | Property check |
| T17.11 | `SalesQuotation.Expire()` → status Expired | Property check |
| T17.12 | `SalesQuotation.Convert()` → status Converted | Property check |
| T17.13 | `SalesQuotation.Convert()` when already Expired → `DomainException` | Arabic message |
| T17.14 | `SalesQuotation.Accept()` when already Expired → `DomainException` | Arabic message |
| T17.15 | `SalesQuotation.IsExpired()` when ValidUntil < UtcNow → true | Boolean check |
| T17.16 | `SalesReturn.Create()` with valid args → status Draft, references original invoice | No exception |
| T17.17 | `SalesReturnItem.ValidateReturnQuantity()` > original sold → `DomainException` | Arabic message |
| T17.18 | `SalesInvoiceItem.CalculateLineTotal()` = `(Qty * UnitPrice) - DiscountAmount` | Correct decimal math |
| T17.19 | `SalesInvoiceItem.CalculateLineTotal()` with zero discount = `Qty * UnitPrice` | Correct decimal math |
| T17.20 | `SalesInvoice.TotalAmount` = `SubTotal - InvoiceDiscount + TaxAmount` | Correct decimal math |
| T17.21 | `SalesInvoice.DueAmount` = `TotalAmount - PaidAmount` | Correct decimal math |
| T17.22 | `SalesInvoice.PaidAmount > TotalAmount` → `DomainException("المبلغ المدفوع أكبر من الإجمالي")` | Arabic message |

### T18 — Service Tests (Sales Module, Mock IUnitOfWork)

| ID | Test | Expected |
|----|------|----------|
| T18.01 | `SalesInvoiceService.CreateAsync()` with valid request → `Result<SalesInvoiceDto>.Success` | IsSuccess = true |
| T18.02 | `SalesInvoiceService.CreateAsync()` with insufficient stock → `Result.Failure` with Arabic message | IsSuccess = false |
| T18.03 | `SalesInvoiceService.PostAsync()` deducts stock → `InventoryMovement` created for each item | QuantityBefore/After correct |
| T18.04 | `SalesInvoiceService.PostAsync()` with Credit payment → `CashTransaction` NOT created | No CashTransaction |
| T18.05 | `SalesInvoiceService.PostAsync()` with Cash payment → `CashTransaction.SalesIncome` created | TransactionType = SalesIncome |
| T18.06 | `SalesInvoiceService.PostAsync()` updates `Customer.CurrentBalance` (+ DueAmount) | Balance increased |
| T18.07 | `SalesInvoiceService.CancelAsync()` reverses stock → `InventoryMovement` with SaleReturnIn | Quantity restored |
| T18.08 | `SalesInvoiceService.CancelAsync()` reverses `Customer.CurrentBalance` (− DueAmount) | Balance decreased |
| T18.09 | `SalesInvoiceService.CancelAsync()` creates `CashTransaction.RefundOut` if payment existed | TransactionType = RefundOut |
| T18.10 | `SalesInvoiceService.PostAsync()` fails → transaction rollback, no stock/balance change | Verify via mocks |
| T18.11 | `SalesQuotationService.CreateAsync()` → `Result<SalesQuotationDto>.Success` | IsSuccess = true |
| T18.12 | `SalesQuotationService.ConvertToInvoiceAsync()` → new `SalesInvoice` created with items copied | Invoice links to QuotationId |
| T18.13 | `SalesReturnService.CreateAsync()` → stock increased, `MovementType.SaleReturnIn` recorded | Quantity after = before + returned |
| T18.14 | `SalesReturnService.PostAsync()` → `CashTransaction.RefundOut` for cash refunds | TransactionType = RefundOut |
| T18.15 | `SalesReturnService.PostAsync()` → balance decreased (return reduces customer debt) | Balance calculation correct |
| T18.16 | `BarcodeLookupService` finds product by barcode → raises event on UI thread | Verify Dispatcher.InvokeAsync called |
| T18.17 | `ISoundService.PlaySuccess()` called on valid barcode scan | Mock.Verify |
| T18.18 | `ISoundService.PlayWarning()` called on validation dialog | Mock.Verify |
| T18.19 | `ISoundService.PlayError()` called on operation failure | Mock.Verify |
| T18.20 | Price override without approval → `Result.Failure("السعر المصرح به يتجاوز الصلاحية")` | IsSuccess = false |
| T18.21 | Price override with Manager/Admin role AND approval → `Result.Success` | IsSuccess = true |
| T18.22 | `FIFO batch allocation` — sale deducts from earliest batch first | Correct batch quantities |
| T18.23 | `Customer credit limit` — Cashier invoice exceeds limit → `Result.Failure` | IsSuccess = false |
| T18.24 | `Customer credit limit` — Manager/Admin exceeds limit → `Result.Success` (override allowed) | IsSuccess = true |

### T19 — FluentValidation Tests (Sales Module)

| ID | Test | Expected |
|----|------|----------|
| T19.01 | `CreateSalesInvoiceRequest` all valid → passes validation | IsValid = true |
| T19.02 | `CreateSalesInvoiceRequest` empty CustomerId → `"يجب اختيار العميل"` | Specific error |
| T19.03 | `CreateSalesInvoiceRequest` empty Items → `"يجب إضافة صنف واحد على الأقل"` | Specific error |
| T19.04 | `CreateSalesInvoiceRequest` item with Qty ≤ 0 → `"الكمية يجب أن تكون أكبر من صفر"` | Specific error |
| T19.05 | `CreateSalesInvoiceRequest` item with UnitPrice < 0 → validation error | Specific error |
| T19.06 | `CreateSalesQuotationRequest` ValidUntil in the past → `"تاريخ الصلاحية يجب أن يكون في المستقبل"` | Specific error |
| T19.07 | `CreateSalesReturnRequest` missing InvoiceId → `"يجب اختيار فاتورة المبيعات"` | Specific error |

### T20 — Database Configuration Tests (Sales Module)

| ID | Test | Expected |
|----|------|----------|
| T20.01 | `SalesInvoiceConfiguration` → `InvoiceNo` has `.HasColumnType("int")` NOT string | Correct type |
| T20.02 | `SalesInvoiceItemConfiguration` → `UnitPrice` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.03 | `SalesInvoiceItemConfiguration` → `Quantity` has `.HasPrecision(18, 3)` | Precision = (18,3) |
| T20.04 | `SalesInvoiceConfiguration` → FK `CreatedByUserId` is `DeleteBehavior.Restrict` | Restrict |
| T20.05 | `SalesInvoiceConfiguration` → FK `CustomerId` is `DeleteBehavior.Restrict` | Restrict |
| T20.06 | `SalesInvoiceConfiguration` → FK `CashBoxId` is `DeleteBehavior.Restrict` | Restrict |
| T20.07 | `SalesReturnConfiguration` → FK `OriginalInvoiceId` is `DeleteBehavior.Restrict` | Restrict |
| T20.08 | `SalesQuotationConfiguration` → `ValidUntil` is `.IsRequired()` | Required |
| T20.09 | All Sales entities have `CreatedAt` with `.HasDefaultValueSql("GETUTCDATE()")` | Default value |
