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

```csharp
// SalesInvoice.cs — add
public int? CurrencyId { get; private set; }
public decimal ExchangeRate { get; private set; } = 1m; // Rate at invoice time
public decimal BaseCurrencyTotal { get; private set; }  // Total in base currency

public void SetCurrency(int? currencyId, decimal exchangeRate)
{
    CurrencyId = currencyId;
    ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
    RecalculateTotals();
}
```

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

```csharp
// SalesInvoiceItem.cs — add
public int? PriceOverrideApprovedBy { get; private set; }
public string? PriceOverrideReason { get; private set; }

public void SetPriceOverride(decimal newPrice, int? approvedByUserId, string? reason)
{
    if (newPrice < 0) throw new DomainException("السعر لا يمكن أن يكون سالباً");
    UnitPrice = newPrice;
    PriceOverrideApprovedBy = approvedByUserId;
    PriceOverrideReason = reason;
    RecalculateLineTotal();
}
```

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
```csharp
public interface IJournalEntryService
{
    Task<Result> CreateSalesInvoiceEntriesAsync(SalesInvoice invoice, 
        List<(int batchId, decimal quantity, decimal unitCost)> costAllocations,
        CancellationToken ct);
}
```
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

```csharp
public record BatchAllocation(int BatchId, decimal Quantity, decimal UnitCost);

public async Task<List<BatchAllocation>> DecreaseStockWithBatchAllocationAsync(
    int productId, int warehouseId, decimal quantity, 
    bool useFefo = false, CancellationToken ct = default)
{
    // Fetch active batches ordered by FIFO/FEFO
    // Deduct from oldest/nearest-expiry batches
    // Return allocation list for COGS
}
```

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

```csharp
public enum QuotationStatus : byte
{
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    Converted = 4,
    Rejected = 5,
    Expired = 6   // Auto-expired (ValidUntil passed) — HIGH: Phase 28 Fix 4
}
```

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

**Cash refund — explicit CashTransaction creation (HIGH):**
```csharp
// When a Sales Return is posted and refund is cash-based:
// Automatically create CashTransaction with RefundOut type
var cashTx = CashTransaction.Create(
    cashBoxId: salesReturn.CashBoxId!.Value,
    amount: salesReturn.RefundAmount,
    type: CashTransactionType.RefundOut,   // RefundOut = 6
    description: $"مرتجع مبيعات #{salesReturn.ReturnNo}",
    referenceId: salesReturn.Id,            // Link to SalesReturn
    referenceType: "SalesReturn"
);
await _uow.CashTransactions.AddAsync(cashTx, ct);
// This updates CashBox.CurrentBalance automatically (via computed sum of transactions)
```

**If refund amount > customer balance**: Create credit balance (negative DueAmount) that customer can use for future purchases.

### 6.8 Customer Credit Limit Enforcement (HIGH — Phase 28 Fix 3)

**Rule**: Before posting a Sales Invoice, check if the customer has a credit limit configured.

**Logic**:
```csharp
// Pre-Post validation — runs BEFORE transaction opens
if (invoice.CustomerId.HasValue)
{
    var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId.Value, ct);
    if (customer == null)
        return Result<SalesInvoiceDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

    // Check if customer has credit limit
    if (customer.CreditLimit.HasValue && customer.CreditLimit > 0)
    {
        var projectedBalance = customer.CurrentBalance + invoice.TotalAmount;
        if (projectedBalance > customer.CreditLimit.Value)
        {
            // Log warning, notify user
            Log.Warning("Customer {CustId} credit limit exceeded: {Balance} > {Limit}",
                customer.Id, projectedBalance, customer.CreditLimit);

            // Allow override with manager permission check
            if (!currentUser.IsManagerOrAbove())
                return Result<SalesInvoiceDto>.Failure(
                    $"رصيد العميل {customer.Name} يتجاوز الحد الائتماني ({customer.CreditLimit:N2})",
                    ErrorCodes.CreditLimitExceeded);
            
            // Manager override — show warning but allow
            Log.Information("Manager override: credit limit exceeded for customer {CustId}", customer.Id);
        }
    }
}
```

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

**Domain method pattern**:
```csharp
public void SetCurrency(int? currencyId, decimal exchangeRate)
{
    CurrencyId = currencyId;
    ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
    BaseCurrencyTotal = TotalAmount * ExchangeRate;
}
```

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
```csharp
private async Task LoadAvailablePrices(int productId, int unitId, int? currencyId)
{
    var result = await _productService.GetPricesAsync(productId, unitId, currencyId);
    if (result.IsSuccess && result.Value != null)
    {
        AvailablePrices = new ObservableCollection<ProductPriceDto>(result.Value);
        var activePrice = result.Value.FirstOrDefault(p => p.IsActive);
        if (activePrice != null)
            CurrentUnitPrice = activePrice.Price;
    }
}
```

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

**PriceOverrideDialog XAML pattern**:
```xml
<Window x:Class="SalesSystem.DesktopPWF.Views.Dialogs.PriceOverrideDialog"
        WindowStyle="None" AllowsTransparency="True" Background="Transparent">
    <Grid>
        <Rectangle Fill="#80000000"/> <!-- Overlay -->
        <Border Background="White" CornerRadius="16" Padding="24" Width="400">
            <StackPanel>
                <TextBlock Text="⚠️ السعر أقل من السعر الرسمي" FontSize="16" FontWeight="Bold"/>
                <TextBlock Text="السعر الرسمي: {0:N2}" Margin="0,12,0,4"/>
                <TextBlock Text="السعر المدخل: {1:N2}" Foreground="#E65100"/>
                <TextBlock Text="سبب التعديل *" Margin="0,12,0,4"/>
                <TextBox Text="{Binding Reason}" AcceptsReturn="True" Height="80"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,16,0,0">
                    <Button Content="✅ تأكيد" Command="{Binding ConfirmCommand}" Style="{StaticResource PrimaryButton}"/>
                    <Button Content="❌ إلغاء" Command="{Binding CancelCommand}" Style="{StaticResource SecondaryButton}"/>
                </StackPanel>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

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

**Profit calculation in service (PostAsync)**:
```csharp
// After batch allocation
decimal itemTotalCost = 0;
foreach (var alloc in batchAllocations)
{
    itemTotalCost += alloc.Quantity * alloc.UnitCost;
}
item.SetCost(allocationTotalCost: itemTotalCost, unitCost: itemTotalCost / item.Quantity);
item.SetProfit(item.LineTotal - itemTotalCost);

// Invoice totals
invoice.SetTotalCost(Items.Sum(i => i.CostPrice ?? 0));
invoice.SetTotalProfit(invoice.TotalAmount - invoice.TotalCost);
```

**XAML profit column**:
```xml
<DataGridTextColumn Header="التكلفة" Binding="{Binding UnitCost, StringFormat=N2}" 
    Width="90" ElementStyle="{StaticResource NumericTextStyle}"/>
<DataGridTextColumn Header="الربح" Binding="{Binding ProfitAmount, StringFormat=N2}" 
    Width="90">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock" BasedOn="{StaticResource NumericTextStyle}">
            <Setter Property="Foreground" Value="{Binding ProfitAmount, Converter={StaticResource ProfitToColorConverter}}"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

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

**Journal entry template (Cash Sale)**:
```csharp
// Dr: Cash/Bank                    TotalAmount - TaxAmount (net cash)
// Dr: Customer Receivable          0 (cash sale)
// Cr: Sales Revenue                SubTotal - DiscountAmount
// Cr: Tax Payable                  TaxAmount
// Cr: Delivery Revenue             AdditionalCharges (if any)
// --- COGS Entry ---
// Dr: COGS                         TotalCost
// Cr: Inventory                    TotalCost
```

**Journal entry template (Credit Sale)**:
```csharp
// Dr: Customer Receivable          TotalAmount
// Cr: Sales Revenue                SubTotal - DiscountAmount
// Cr: Tax Payable                  TaxAmount
// Cr: Delivery Revenue             AdditionalCharges (if any)
// --- COGS Entry ---
// Dr: COGS                         TotalCost
// Cr: Inventory                    TotalCost
```

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

**Batch return logic**:
```csharp
private async Task<Result> ReturnToBatchesAsync(SalesReturn salesReturn, CancellationToken ct)
{
    foreach (var item in salesReturn.Items)
    {
        if (item.ReturnedBatchId.HasValue)
        {
            // Return stock to original batch
            await _inventoryService.IncreaseStockInBatchAsync(
                item.ReturnedBatchId.Value,
                item.Quantity,
                MovementType.SaleReturnIn,
                ct);
        }
    }
    return Result.Success();
}
```

**Refund logic** — create CashTransaction.RefundOut (HIGH — Phase 28 Fix 2):
```csharp
// When a Sales Return is posted and refund is cash-based, automatically:
// 1. Create CashTransaction with RefundOut type
// 2. Link CashTransaction to SalesReturn reference
// 3. This updates CashBox.CurrentBalance automatically via computed sum

if (salesReturn.CashBoxId.HasValue && salesReturn.RefundAmount > 0)
{
    // Create formal CashTransaction (immutable — RULE-082)
    var cashTx = CashTransaction.Create(
        cashBoxId: salesReturn.CashBoxId.Value,
        amount: salesReturn.RefundAmount,
        type: CashTransactionType.RefundOut,   // RefundOut = 6 (RULE-208)
        description: $"مرتجع مبيعات #{salesReturn.ReturnNo}",
        referenceId: salesReturn.Id,            // Link CashTransaction → SalesReturn
        referenceType: "SalesReturn",
        createdByUserId: currentUserId);
    
    await _uow.CashTransactions.AddAsync(cashTx, ct);
    // CashBox.CurrentBalance recomputed from transaction sum
    Log.Information("Refund transaction created: SalesReturn #{Rn}, CashBox {Cb}, Amount {Amt}",
        salesReturn.ReturnNo, salesReturn.CashBoxId.Value, salesReturn.RefundAmount);
}
```

**Estimate**: ~4 hours

---

### Task 7 — Auto Customer Receipt Creation on Cash Sales

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/ICustomerPaymentService.cs` | Add `CreateFromSalesInvoiceAsync(SalesInvoice)` method if not exists |
| `Application/Services/CustomerPaymentService.cs` | Implement auto-creation of payment receipt |
| `Application/Services/SalesInvoiceService.cs` | Call auto-payment creation inside Post transaction |

**Logic**:
```csharp
// Auto-create customer payment receipt when:
// 1. PaymentType = Cash (1) or Mixed (3)
// 2. PaidAmount > 0
// 3. SystemSetting "AutoCreatePaymentReceipt" = true (default)

if ((invoice.PaymentType == PaymentType.Cash || invoice.PaymentType == PaymentType.Mixed)
    && invoice.PaidAmount > 0
    && await _systemSettingsRepo.GetBoolAsync("AutoCreatePaymentReceipt", true, ct))
{
    var paymentResult = await _customerPaymentService.CreateFromSalesInvoiceAsync(invoice, ct);
    if (!paymentResult.IsSuccess)
        Log.Warning("Auto-payment creation failed for invoice {Id}: {Error}", invoice.Id, paymentResult.Error);
}
```

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

**SalesQuotation entity**:
```csharp
public class SalesQuotation : BaseEntity
{
    public int QuotationNo { get; private set; }
    public int? CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public int? CurrencyId { get; private set; }
    public decimal ExchangeRate { get; private set; } = 1m;
    public DateTime QuotationDate { get; private set; }
    public DateOnly? ValidUntil { get; private set; }
    public QuotationStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public string? TermsAndConditions { get; private set; }
    public int? ConvertedToInvoiceId { get; private set; }

    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual SalesInvoice? ConvertedToInvoice { get; private set; }
    public virtual List<SalesQuotationItem> Items { get; private set; } = new();

    // Factory method with guard clauses (RULE-052)
    public static SalesQuotation Create(int quotationNo, int warehouseId, /*...*/) { /*...*/ }

    // Convert to invoice — returns data, doesn't create
    public SalesInvoiceData ToInvoiceData() { /*...*/ }

    public void MarkAsSent() { /* guard Status == Draft */ }
    public void MarkAsAccepted() { /* guard Status == Sent */ }
    public void MarkAsConverted(int invoiceId) { /* guard Status == Accepted */ }
    public void MarkAsRejected() { /* guard Status == Sent or Accepted */ }
}
```

**ConvertToInvoice logic** (with auto-expiry check — Phase 28 Fix 4):
```csharp
public async Task<Result<SalesInvoiceDto>> ConvertToInvoiceAsync(int quotationId, CancellationToken ct)
{
    var quotation = await _uow.SalesQuotations.GetByIdAsync(quotationId, ct);
    if (quotation == null)
        return Result<SalesInvoiceDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);
    if (quotation.Status != QuotationStatus.Accepted)
        return Result<SalesInvoiceDto>.Failure("يجب أن يكون عرض السعر مقبولاً للتحويل", ErrorCodes.InvalidOperation);

    // Auto-expiry check: if ValidUntil has passed, mark expired and block conversion
    if (quotation.ValidUntil.HasValue && quotation.ValidUntil.Value < DateOnly.FromDateTime(DateTime.UtcNow))
    {
        quotation.MarkAsExpired();  // Sets Status = Expired
        await _uow.SaveChangesAsync(ct);
        return Result<SalesInvoiceDto>.Failure("انتهت صلاحية عرض السعر", ErrorCodes.QuotationExpired);
    }

    await using var tx = await _uow.BeginTransactionAsync(ct);
    try
    {
        // Create SalesInvoice from quotation data
        var invoiceNoResult = await _documentSequenceService.GetNextIntAsync("SalesInvoice", ct);
        if (!invoiceNoResult.IsSuccess)
            return Result<SalesInvoiceDto>.Failure(invoiceNoResult.Error!);
        var invoiceNo = invoiceNoResult.Value;
        var invoice = SalesInvoice.Create(
            quotation.WarehouseId, invoiceNo,
            customerId: quotation.CustomerId,
            /* map from quotation */
        );
        
        foreach (var qItem in quotation.Items)
        {
            var item = SalesInvoiceItem.Create(qItem.ProductId, qItem.Quantity, 
                qItem.UnitPrice, qItem.DiscountAmount, qItem.Mode);
            invoice.AddItem(item);
        }

        await _uow.SalesInvoices.AddAsync(invoice, ct);
        
        // Mark quotation as converted
        quotation.MarkAsConverted(invoice.Id);
        
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        
        Log.Information("Quotation {QId} converted to Invoice {InvId}", quotationId, invoice.Id);
        _eventBus.Publish(new SaleInvoiceChangedMessage(invoice.Id));
        
        return Result<SalesInvoiceDto>.Success(MapToDto(invoice));
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync(ct);
        Log.Error(ex, "Failed to convert quotation {QId} to invoice", quotationId);
        return Result<SalesInvoiceDto>.Failure("فشل تحويل عرض السعر إلى فاتورة", ErrorCodes.InternalError);
    }
}
```

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

**BarcodeScannerControl logic** (event-driven, RULE-233):
```csharp
// Continuous scan mode — event-driven via Application.Current.Dispatcher
// Scanner input is intercepted BEFORE focus change
private async void OnBarcodeScanned(string barcode)
{
    _soundService.PlaySuccess(); // Audible feedback (RULE-231)
    
    // If quantity prefix typed (e.g., "5" then scan)
    int quantityOverride = _pendingQuantity > 0 ? _pendingQuantity : 1;
    _pendingQuantity = 0;
    
    // Lookup product via BarcodeService — no manual Search button click
    var product = await _barcodeService.LookupAsync(barcode);
    if (product == null)
    {
        _soundService.PlayError();
        await _dialogService.ShowWarningAsync("باركود غير معروف", $"الباركود {barcode} غير مسجل");
        return;
    }
    
    // Marshal to UI thread via Application.Current.Dispatcher
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        // Check if already in grid → increment quantity
        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += quantityOverride;
            existingItem.RecalculateLineTotal();
        }
        else
        {
            // Add new line
            var newItem = new InvoiceLineViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantityOverride,
                UnitPrice = product.RetailPrice,
                // ...
            };
            Items.Add(newItem);
        }
    });
    
    // Auto-clear scanner input for next scan
    ScannerInput = string.Empty;
    // Auto-refocus scanner input
    _ = FocusScannerInput();
}
```

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

**Structure**:
```csharp
public class SalesInvoiceEditorViewModel : ViewModelBase
{
    // ── New Properties ──
    public ObservableCollection<CurrencyDto> Currencies { get; }
    public CurrencyDto? SelectedCurrency { get; set; }  // Triggers price refresh
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal AdditionalCharges { get; set; }
    public byte DiscountType { get; set; }  // 0=Amount, 1=Percent
    public decimal DiscountValue { get; set; }
    public decimal TotalCost { get; private set; }
    public decimal TotalProfit { get; private set; }
    public bool ShowProfit { get; set; }  // From SystemSettings
    public string DraftAge { get; private set; }  // "منذ 3 أيام"

    // ── New Commands ──
    public ICommand ToggleScanModeCommand { get; }
    public ICommand ConvertFromQuotationCommand { get; }
    
    // ── Existing (enhanced) ──
    // SaveAsDraftCommand, PostCommand remain — no CanExecute (RULE-059)
}
```

**Estimate**: ~4 hours

---

### Task 11 — Updated SalesInvoiceEditorView.xaml (Compact + ToolTips)

**Objective**: Restyle the editor view with compact dimensions (RULE-262-274) and comprehensive Arabic ToolTips (RULE-185-190).

**Layout changes**:

```xml
<!-- Header section — compact padding -->
<Border Background="{StaticResource PrimaryBrush}" Padding="12,6">
    <TextBlock Text="فاتورة بيع" FontSize="14" FontWeight="Bold" Foreground="White"/>
</Border>

<!-- Scanner bar — compact -->
<BarcodeScannerControl DataContext="{Binding}" 
    Margin="0,6,0,6" Height="32"/>

<!-- Invoice data grid — compact rows -->
<DataGrid ItemsSource="{Binding Items}" 
          Style="{StaticResource CompactDataGrid}"
          RowHeight="24" ...>

<!-- Profit column (conditional) -->
<DataGridTextColumn Header="الربح" 
    Visibility="{Binding ShowProfit, Converter={StaticResource BoolToVisibility}}"
    .../>

<!-- Footer — compact -->
<Border Background="White" Padding="12,8" BorderThickness="0,1,0,0">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="💾 حفظ كمسودة" Command="{Binding SaveAsDraftCommand}" 
                ToolTip="حفظ الفاتورة كمسودة — لا تؤثر على المخزون"/>
        <Button Content="✅ ترحيل" Command="{Binding PostCommand}" 
                ToolTip="ترحيل الفاتورة — سيتم خصم المخزون وتحديث رصيد العميل"/>
    </StackPanel>
</Border>
```

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

**List ViewModel**:
```csharp
public class SalesQuotationListViewModel : ViewModelBase, IDisposable
{
    public ObservableCollection<SalesQuotationDto> Quotations { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ConvertToInvoiceCommand { get; }
    public ICommand RefreshCommand { get; }

    // Newest-first sort (RULE-220)
    private async Task LoadQuotationsAsync()
    {
        var result = await _quotationService.GetAllAsync();
        if (result.IsSuccess)
        {
            InvokeOnUIThread(() =>
            {
                Quotations.Clear();
                foreach (var q in result.Value.OrderByDescending(x => x.Id))
                    Quotations.Add(q);
            });
        }
    }
}
```

**Editor ViewModel**:
```csharp
public class SalesQuotationEditorViewModel : ViewModelBase
{
    // Properties with INotifyDataErrorInfo
    public int QuotationNo { get; set; }
    public int? CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public int? CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime QuotationDate { get; set; } = DateTime.Today;
    public DateOnly? ValidUntil { get; set; }
    // ...

    public SalesQuotationEditorViewModel(/* */)
    {
        SetDialogService(_dialogService);  // RULE-227
        SaveCommand = new AsyncRelayCommand(SaveAsync);  // No CanExecute (RULE-059)
    }

    private bool Validate()
    {
        ClearAllErrors();  // RULE-229
        if (WarehouseId <= 0) AddError(nameof(WarehouseId), "المستودع مطلوب");
        if (!Items.Any()) AddError(nameof(Items), "يجب إضافة صنف واحد على الأقل");
        // ...
        return !HasErrors;
    }
}
```

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

**Example — Quotation validator** (RULE-044):
```csharp
public class CreateSalesQuotationRequestValidator : AbstractValidator<CreateSalesQuotationRequest>
{
    public CreateSalesQuotationRequestValidator()
    {
        RuleFor(x => x.QuotationNo).GreaterThan(0).WithMessage("رقم عرض السعر مطلوب");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.ExchangeRate).GreaterThan(0).When(x => x.CurrencyId.HasValue)
            .WithMessage("سعر الصرف يجب أن يكون أكبر من صفر");
        RuleFor(x => x.ValidUntil).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.ValidUntil.HasValue)
            .WithMessage("تاريخ الصلاحية لا يمكن أن يكون في الماضي");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
```

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

**Report DTOs**:
```csharp
public record SalesProfitReportDto(
    int TotalInvoices,
    decimal TotalSales,
    decimal TotalCost,
    decimal TotalProfit,
    decimal ProfitMargin,  // TotalProfit / TotalSales * 100
    List<SalesInvoiceProfitDto> Invoices
);

public record SalesInvoiceProfitDto(
    int InvoiceNo,
    string CustomerName,
    DateTime InvoiceDate,
    decimal TotalAmount,
    decimal TotalCost,
    decimal Profit
);
```

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

**XAML usage pattern**:
```xml
<StackPanel Orientation="Horizontal">
    <TextBlock Text="فاتورة بيع" Style="{StaticResource LabelStyle}"/>
    <controls:InfoTooltip HelpText="فاتورة البيع هي مستند يثبت قيام الشركة ببيع بضاعة أو تقديم خدمة للعميل."/>
</StackPanel>
```

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
