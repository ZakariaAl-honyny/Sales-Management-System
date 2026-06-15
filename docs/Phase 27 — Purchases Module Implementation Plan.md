# Phase 27 — Purchases Module: Comprehensive Implementation Plan

> **Version**: 1.1 — Built from analysis of Analysis Part 1–5, Global Analysis, full codebase audit, and user requirements
> **Fixes applied**: FIFO batch costing, partial PO→Invoice receive workflow, AdditionalCharge.AccountId FK, PurchaseReturn LinkToInvoice mode, decimal precision audit, Arabic DomainException guards
> **Scope**: Enhance existing Purchase Invoice module with multi-currency, additional fees, attachments, auto accounting entries, purchase orders (NEW), and purchase return enhancements for V1

---

## Table of Contents

1. [Architecture — 3 Sub-Modules](#1-architecture--3-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog](#4-design-catalog)
5.  [Gap Analysis](#5-gap-analysis)
    - 5.6 [FIFO Batch Costing on Purchase Receipt](#56-fifo-batch-costing-on-purchase-receipt)
6.  [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 3 Sub-Modules

Based on full codebase audit + user requirements from analysis documents, the Purchases module for V1 enhancement is divided into **3 main sub-modules**:

| # | Sub-Module | Status | Impact |
|---|------------|--------|--------|
| 📋 | **Purchase Invoices (Enhanced)** | ✅ Exists — needs enhancement | Multi-currency, additional fees, attachments, enhanced discounts, auto-journal entries, auto-payment creation |
| 📑 | **Purchase Orders (NEW)** | ❌ Does NOT exist | Pre-purchase request/approval workflow — independent from invoices |
| 🔄 | **Purchase Returns (Enhanced)** | ✅ Exists — needs enhancement | FIFO batch return, supplier discount, currency support |

### Data Flow

```text
Purchase Order (Draft/Approved/Received) → Purchase Invoice (Draft) → Purchase Invoice (Posted)
                                                                         ↓
                                                            Stock Increase (InventoryMovement)
                                                            Cost Update (UpdateProductPricingService)
                                                            Supplier Balance Update
                                                            Auto Journal Entry (GL)
                                                            Auto Supplier Payment (Cash purchases)
                                                            CashBox Transaction
```

### Invoice Lifecycle (Existing — Unchanged)

```text
Draft (1) → Posted (2) → Cancelled (3)

ALLOWED:
  Draft    → Posted      ✅ (stock + balance + accounting happen HERE)
  Draft    → Cancelled   ✅ (no stock/balance impact)
  Posted   → Cancelled   ✅ (MUST reverse stock + balance + accounting)

FORBIDDEN:
  Posted   → Draft       ❌ NEVER
  Cancelled → anything   ❌ NEVER (terminal state)
  Editing a Posted invoice ❌ NEVER (cancel + create new instead)
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Purchase Invoice — Existing ✅

**Entity**: `SalesSystem.Domain.Entities.PurchaseInvoice`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` | ✅ Exists |
| `SupplierId` | `int FK` | ✅ Exists |
| `WarehouseId` | `int FK` | ✅ Exists |
| `CashBoxId` | `int? FK` | ✅ Exists |
| `TaxId` | `int? FK` | ✅ Exists (from Phase 19) |
| `InvoiceDate` | `DateTime` | ✅ Exists |
| `DueDate` | `DateOnly?` | ✅ Exists |
| `PaymentType` | `PaymentType` (byte) | ✅ Exists |
| `SubTotal` | `decimal(18,2)` | ✅ Exists |
| `DiscountAmount` | `decimal(18,2)` | ✅ Exists |
| `TaxAmount` | `decimal(18,2)` | ✅ Exists |
| `TotalAmount` | `decimal(18,2)` | ✅ Exists |
| `PaidAmount` | `decimal(18,2)` | ✅ Exists |
| `DueAmount` | `decimal(18,2)` | ✅ Exists |
| `InvoiceNo` | `int` | ✅ Exists (RULE-254) |
| `SupplierInvoiceNo` | `string(50)?` | ✅ Exists |
| `Notes` | `string(500)?` | ✅ Exists |
| `Status` | `InvoiceStatus` (byte) | ✅ Exists |
| `CurrencyId` | `int? FK` | **❌ NEW — add** |
| `ExchangeRate` | `decimal(18,6)?` | **❌ NEW — add** |
| `CostInBaseCurrency` | `decimal(18,2)?` | **❌ NEW — add** |
| `AttachmentPath` | `string(255)?` | **❌ NEW — add** |
| `DiscountType` | `DiscountType?` (Percentage/Amount) | **❌ NEW — add** |
| `DiscountRate` | `decimal(18,2)?` | **❌ NEW — add** |
| `AdditionalFeesTotal` | `decimal(18,2)` | **❌ NEW — add** |

**Entity**: `SalesSystem.Domain.Entities.PurchaseInvoiceLine`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` | ✅ Exists |
| `PurchaseInvoiceId` | `int FK` | ✅ Exists |
| `ProductId` | `int FK` | ✅ Exists |
| `ProductUnitId` | `int FK` | **❌ MISSING — add** |
| `Quantity` | `decimal(18,3)` | ✅ Exists |
| `UnitCost` | `decimal(18,2)` | ✅ Exists |
| `DiscountAmount` | `decimal(18,2)` | ✅ Exists |
| `LineTotal` | `decimal(18,2)` | ✅ Exists |
| `Mode` | `SaleMode` (byte) | ✅ Exists |
| `Notes` | `string(250)?` | ✅ Exists |
| `DiscountType` | `DiscountType?` | **❌ NEW — add** |
| `DiscountRate` | `decimal(18,2)?` | **❌ NEW — add** |
| `CostInBaseCurrency` | `decimal(18,2)?` | **❌ NEW — add** |
| `AdditionalFeesAmount` | `decimal(18,2)` | **❌ NEW — add** |

**Configuration**: `PurchaseInvoiceConfiguration.cs` + `PurchaseInvoiceLineConfiguration.cs` — ✅ Exists

**Service**: `PurchaseService` / `IPurchaseService` — ✅ Exists (504 lines)
- `GetByIdAsync` ✅
- `GetAllAsync` (filtered/paginated) ✅
- `CreateAsync` (transaction-based, item creation) ✅
- `UpdateAsync` (draft only) ✅
- `PostAsync` (stock increase, cost update, supplier balance, cash transaction) ✅
- `CancelAsync` (reverse stock, balance, cash transaction) ✅
- `MapToDto` ✅

**Controller**: `PurchaseInvoicesController` — ✅ Exists
- `GET /api/v1/purchase-invoices` ✅
- `GET /api/v1/purchase-invoices/{id}` ✅
- `POST /api/v1/purchase-invoices` ✅
- `PUT /api/v1/purchase-invoices/{id}` ✅
- `POST /api/v1/purchase-invoices/{id}/post` ✅
- `POST /api/v1/purchase-invoices/{id}/cancel` ✅

**Desktop ViewModels**:
- `PurchaseInvoiceEditorViewModel` (1155 lines) — ✅ Exists
- `PurchaseInvoiceListViewModel` — ✅ Exists

**Desktop Views**:
- `PurchaseInvoiceEditorView.xaml` / `.cs` — ✅ Exists
- `PurchaseInvoicesListView.xaml` / `.cs` — ✅ Exists

**API Service**: `PurchaseInvoiceApiService` — ✅ Exists

### 2.2 Purchase Return — Existing ✅

**Entity**: `SalesSystem.Domain.Entities.PurchaseReturn`

| Field | Type | Status |
|-------|------|--------|
| `ReturnNo` | `string` | ✅ Exists |
| `PurchaseInvoiceId` | `int? FK` | ✅ Exists |
| `SupplierId` | `int FK` | ✅ Exists |
| `WarehouseId` | `int FK` | ✅ Exists |
| `ReturnDate` | `DateTime` | ✅ Exists |
| `Notes` | `string?` | ✅ Exists |
| `SubTotal` | `decimal(18,2)` | ✅ Exists |
| `TotalAmount` | `decimal(18,2)` | ✅ Exists |
| `Status` | `InvoiceStatus` | ✅ Exists |
| `CurrencyId` | `int? FK` | **❌ NEW — add** |
| `ExchangeRate` | `decimal(18,6)?` | **❌ NEW — add** |
| `DiscountAmount` | `decimal(18,2)` | **❌ NEW — add** |
| `DiscountType` | `DiscountType?` | **❌ NEW — add** |
| `DiscountRate` | `decimal(18,2)?` | **❌ NEW — add** |

**Entity**: `PurchaseReturnItem` — ✅ Exists (basic)
- Missing: `ProductUnitId`, `CostInBaseCurrency`

**Service**: `PurchaseReturnService` / `IPurchaseReturnService` — ✅ Exists
- Missing: enhanced discount, FIFO batch return, currency support

**Controller**: `PurchaseReturnsController` — ✅ Exists
**Desktop ViewModels**: `PurchaseReturnEditorViewModel`, `PurchaseReturnListViewModel` — ✅ Exists

### 2.3 Purchase Order ❌ (Does NOT exist)

| Component | Status |
|-----------|--------|
| `PurchaseOrder` entity | **❌ MISSING** |
| `PurchaseOrderItem` entity | **❌ MISSING** |
| `PurchaseOrderConfiguration` | **❌ MISSING** |
| `PurchaseOrderService` / `IPurchaseOrderService` | **❌ MISSING** |
| `PurchaseOrdersController` | **❌ MISSING** |
| `PurchaseOrderDto` / Requests / Responses | **❌ MISSING** |
| Purchase Order desktop screens (List + Editor) | **❌ MISSING** |

### 2.4 Additional Fee ❌ (Does NOT exist)

| Component | Status |
|-----------|--------|
| `AdditionalFee` entity (transport, customs, etc.) | **❌ MISSING** |
| `AdditionalFeeAllocation` entity (fee distribution to items) | **❌ MISSING** |
| Fee distribution service | **❌ MISSING** |
| Desktop UI for fee entry | **❌ MISSING** |

### 2.5 Auto Journal Entry ❌ (Does NOT exist for purchases)

| Component | Status |
|-----------|--------|
| Purchase → Journal entry mapping | **❌ MISSING** |
| Purchase post hook to create GL entries | **❌ MISSING** |
| Debit/Credit accounts configuration for purchases | **❌ MISSING** |

### 2.6 Invoice Attachments ❌ (Does NOT exist)

| Component | Status |
|-----------|--------|
| `AttachmentPath` on PurchaseInvoice | **❌ MISSING** |
| File upload endpoint | **❌ MISSING** |
| File picker in Desktop editor | **❌ MISSING** |

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: CurrencyId/ExchangeRate Fields Missing from PurchaseInvoice and PurchaseReturn

**Problem**: The Currencies module (Phase 20) creates `Currency` entity and `ExchangeRateHistory` table. However, `PurchaseInvoice` and `PurchaseReturn` entities have no `CurrencyId` FK or `ExchangeRate` field. Without these:
- Multi-currency purchase invoices cannot be created
- Purchase costs in foreign currencies cannot be converted to base currency
- Cost distribution and batch creation work only in base currency

**Dependency**: Phase 20 (Currencies Module) must be complete BEFORE Phase 27 implementation.

**Fix**: Add `int? CurrencyId` FK, `decimal(18,6)? ExchangeRate`, and `decimal(18,2)? CostInBaseCurrency` to both `PurchaseInvoice` and `PurchaseReturn` entities.

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/PurchaseInvoice.cs` for the canonical `Create()` factory method.

**Files changed**: `PurchaseInvoice.cs`, `PurchaseInvoiceConfiguration.cs`, `PurchaseReturn.cs`, `PurchaseReturnConfiguration.cs`, migration, DTOs, services, ViewModels, Views

---

### 3.2 Blocker 2: Additional Fees Distribution Algorithm

**Problem**: The analysis (Analysis Part 4, Part 5) requires support for additional fees (transport, customs, loading, unloading) that must be distributed proportionally across purchase invoice items. There is no existing algorithm or entity for this.

**Fix**: Create `AdditionalFee` entity (fee definitions per invoice) + `AdditionalFeeAllocation` entity (automatically computed distribution).

**Distribution Algorithm** — Two strategies (user-selectable):

> See `docs/AGENTS.md` for domain entity patterns and `docs/CONSTITUTION.md` for financial formulas. See `Application/Services/FeeDistributionService.cs` for the canonical fee distribution algorithm.

**Files changed**: New entities (`AdditionalFee`, `AdditionalFeeAllocation`), new configurations, new migration, fee distribution service

---

### 3.3 Blocker 3: Auto Journal Entry Creation on Purchase Post

**Problem**: The analysis requires auto-creation of journal entries when a purchase invoice is posted. This depends on the Chart of Accounts module (Phase 22 — Accounting Foundation) which defines the COA structure and journal entry service.

**Dependency**: Phase 22 (Accounting Foundation) must provide:
- `Account` entity with account codes
- `JournalEntry` / `JournalEntryLine` entities
- `IJournalEntryService` interface with `CreateAsync()`

**Fix**: Add a hook in `PurchaseService.PostAsync()` that calls `_journalEntryService.CreateFromPurchaseAsync()` after stock update, cost update, and supplier balance update.

**Auto-accounting entries for purchases**:

| Entry | Debit | Credit | When |
|-------|-------|--------|------|
| Inventory (بضاعة) | LineTotal sum + allocated fees | — | Always, per item |
| Purchase VAT (ضريبة مشتريات) | TaxAmount | — | If TaxId set |
| Supplier (المورد) | — | TotalAmount (or DueAmount) | Always |
| Cash/Bank (الصندوق/البنك) | — | PaidAmount | If cash/partial payment |

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` §2.3 for the step-by-step transaction pattern. See `Application/Services/PurchaseService.cs` for the canonical post implementation.

**Files changed**: `PurchaseService.cs`, new `IPurchaseJournalService` (or extend existing), config flag check

---

### 3.4 Blocker 4: ProductUnitId Missing from PurchaseInvoiceLine

**Problem**: `PurchaseInvoiceLine` has `ProductId` but no `ProductUnitId`. With Dynamic UOM (RULE-060), the user enters quantity in a purchase unit (e.g., "Carton" with factor 24), but the system stores cost per base unit. Without `ProductUnitId`, there is no way to know which unit was used for the purchase.

**Fix**: Add `int ProductUnitId` FK to `PurchaseInvoiceLine`.

> See `docs/AGENTS.md` for domain entity patterns. See `Domain/Entities/PurchaseInvoiceLine.cs` for the canonical entity definition.

**Files changed**: `PurchaseInvoiceLine.cs`, `PurchaseInvoiceLineConfiguration.cs`, DTOs, services, migration

---

## 4. Design Catalog

### 4.1 PurchaseInvoice — Enhanced Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | Existing |
| 2 | `SupplierId` | `int FK` | — | ✅ | Existing |
| 3 | `WarehouseId` | `int FK` | — | ✅ | Existing |
| 4 | `CashBoxId` | `int? FK` | `null` | ❌ | Existing |
| 5 | `TaxId` | `int? FK` | `null` | ❌ | Existing (Phase 19) |
| 6 | `CurrencyId` | `int? FK` | `null` | ❌ | **NEW** — Phase 20 dependency |
| 7 | `InvoiceDate` | `DateTime` | `UTC Now` | ✅ | Existing |
| 8 | `DueDate` | `DateOnly?` | `null` | ❌ | Existing |
| 9 | `PaymentType` | `PaymentType` (byte) | `Cash=1` | ✅ | Existing |
| 10 | `InvoiceNo` | `int` | — | ✅ | Existing (RULE-254) |
| 11 | `SupplierInvoiceNo` | `nvarchar(50)?` | `null` | ❌ | Existing |
| 12 | `Notes` | `nvarchar(500)?` | `null` | ❌ | Existing |
| 13 | `Status` | `InvoiceStatus` (byte) | `Draft=1` | ✅ | Existing |
| 14 | `SubTotal` | `decimal(18,2)` | `0` | ✅ | Existing — computed |
| 15 | `DiscountAmount` | `decimal(18,2)` | `0` | ✅ | Existing |
| 16 | `DiscountType` | `DiscountType?` | `null` | ❌ | **NEW** — `Amount=0`, `Percentage=1` |
| 17 | `DiscountRate` | `decimal(18,2)?` | `null` | ❌ | **NEW** — used if DiscountType = Percentage |
| 18 | `TaxAmount` | `decimal(18,2)` | `0` | ✅ | Existing |
| 19 | `TotalAmount` | `decimal(18,2)` | `0` | ✅ | Existing — computed |
| 20 | `PaidAmount` | `decimal(18,2)` | `0` | ✅ | Existing |
| 21 | `DueAmount` | `decimal(18,2)` | `0` | ✅ | Existing — computed |
| 22 | `ExchangeRate` | `decimal(18,6)?` | `null` | ❌ | **NEW** — rate for multi-currency |
| 23 | `CostInBaseCurrency` | `decimal(18,2)?` | `null` | ❌ | **NEW** — total cost in base currency |
| 24 | `AdditionalFeesTotal` | `decimal(18,2)` | `0` | ❌ | **NEW** — sum of all additional fees |
| 25 | `AttachmentPath` | `nvarchar(255)?` | `null` | ❌ | **NEW** — optional invoice image |
| 26 | `CreatedByUserId` | `int? FK` | — | ❌ | Existing (BaseEntity) |
| 27 | `CreatedAt` | `datetime2` | — | ✅ | Existing (BaseEntity) |

**Domain methods to add/update**:

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/AGENTS.md` §2.2 for financial formulas. See `Domain/Entities/PurchaseInvoice.cs` for the canonical domain methods.

**DiscountType enum** (add to Domain/Enums):

> See `Domain/Enums/DiscountType.cs` for the canonical enum definition.

### 4.2 PurchaseInvoiceLine — Enhanced Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | Existing |
| 2 | `PurchaseInvoiceId` | `int FK` | — | ✅ | Existing |
| 3 | `ProductId` | `int FK` | — | ✅ | Existing |
| 4 | `ProductUnitId` | `int FK` | — | ✅ | **NEW** — unit used for purchase |
| 5 | `Quantity` | `decimal(18,3)` | — | ✅ | Existing |
| 6 | `UnitCost` | `decimal(18,2)` | — | ✅ | Existing — in invoice currency |
| 7 | `DiscountAmount` | `decimal(18,2)` | `0` | ❌ | Existing |
| 8 | `DiscountType` | `DiscountType?` | `null` | ❌ | **NEW** |
| 9 | `DiscountRate` | `decimal(18,2)?` | `null` | ❌ | **NEW** |
| 10 | `LineTotal` | `decimal(18,2)` | — | ✅ | Existing — computed |
| 11 | `Mode` | `SaleMode` (byte) | `Retail=1` | ✅ | Existing |
| 12 | `CostInBaseCurrency` | `decimal(18,2)?` | `null` | ❌ | **NEW** |
| 13 | `AdditionalFeesAmount` | `decimal(18,2)` | `0` | ❌ | **NEW** — allocated fees |
| 14 | `Notes` | `nvarchar(250)?` | `null` | ❌ | Existing |

**Updated domain method**:

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/AGENTS.md` §2.2 for financial formulas. See `Domain/Entities/PurchaseInvoiceLine.cs` for the canonical definition.

### 4.3 AdditionalFee — NEW Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | |
| 2 | `PurchaseInvoiceId` | `int FK` | — | ✅ | Links to invoice |
| 3 | `FeeName` | `nvarchar(100)` | — | ✅ | e.g., "نقل", "جمارك", "تخليص" |
| 4 | `FeeAmount` | `decimal(18,2)` | — | ✅ | Total fee amount |
| 5 | `DistributionMethod` | `DistributionMethod` (byte) | `ByCost=0` | ✅ | `ByCost=0`, `ByQuantity=1` |
| 6 | `AccountId` | `int? FK` | `null` | ❌ | **NEW** — Chart of Accounts entry for auto-journal credit side |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions. See `Domain/Entities/AdditionalFee.cs`, `Domain/Enums/DistributionMethod.cs`, and `Infrastructure/Data/Configurations/AdditionalFeeConfiguration.cs` for canonical definitions.

### 4.4 AdditionalFeeAllocation — NEW Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | |
| 2 | `AdditionalFeeId` | `int FK` | — | ✅ | Links to fee |
| 3 | `PurchaseInvoiceLineId` | `int FK` | — | ✅ | Links to invoice item |
| 4 | `AllocatedAmount` | `decimal(18,2)` | — | ✅ | Computed allocation |

> See `docs/AGENTS.md` for domain entity patterns. See `Domain/Entities/AdditionalFeeAllocation.cs` for the canonical definition.

### 4.5 PurchaseOrder — NEW Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | |
| 2 | `OrderNo` | `int` | — | ✅ | User-facing order number |
| 3 | `SupplierId` | `int FK` | — | ✅ | |
| 4 | `WarehouseId` | `int FK` | — | ✅ | |
| 5 | `CurrencyId` | `int? FK` | `null` | ❌ | Optional multi-currency |
| 6 | `ExchangeRate` | `decimal(18,6)?` | `null` | ❌ | |
| 7 | `OrderDate` | `DateTime` | `UTC Now` | ✅ | |
| 8 | `ExpectedDate` | `DateOnly?` | `null` | ❌ | Expected delivery date |
| 9 | `Status` | `PurchaseOrderStatus` (byte) | `Draft=1` | ✅ | Draft=1, Approved=2, PartiallyReceived=3, Received=4, Cancelled=5 |
| 10 | `SubTotal` | `decimal(18,2)` | `0` | ✅ | Computed |
| 11 | `DiscountAmount` | `decimal(18,2)` | `0` | ❌ | |
| 12 | `TaxAmount` | `decimal(18,2)` | `0` | ❌ | |
| 13 | `TotalAmount` | `decimal(18,2)` | `0` | ✅ | Computed |
| 14 | `Notes` | `nvarchar(500)?` | `null` | ❌ | |
| 15 | `CreatedByUserId` | `int? FK` | — | ❌ | BaseEntity |

**PurchaseOrderStatus enum** (add to Domain/Enums):

> See `Domain/Enums/PurchaseOrderStatus.cs` for the canonical enum definition.

### 4.6 PurchaseOrderItem — NEW Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | |
| 2 | `PurchaseOrderId` | `int FK` | — | ✅ | |
| 3 | `ProductId` | `int FK` | — | ✅ | |
| 4 | `ProductUnitId` | `int FK` | — | ✅ | |
| 5 | `Quantity` | `decimal(18,3)` | — | ✅ | Ordered quantity |
| 6 | `ReceivedQuantity` | `decimal(18,3)` | `0` | ❌ | Quantity already received via invoices |
| 7 | `PendingReceiveQuantity` | `decimal(18,3)` | (computed) | ❌ | **NEW** — `Quantity - ReceivedQuantity` (read-only computed property) |
| 8 | `UnitCost` | `decimal(18,2)` | — | ✅ | Expected/negotiated cost |
| 9 | `LineTotal` | `decimal(18,2)` | — | ✅ | Computed |
| 10 | `Notes` | `nvarchar(250)?` | `null` | ❌ | |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/PurchaseOrderItem.cs` for the canonical definition.

### 4.7 PurchaseReturn — Enhanced Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `ReturnNo` | `nvarchar(50)` | — | ✅ | Existing — generated |
| 2 | `PurchaseInvoiceId` | `int? FK` | `null` | ❌ | Existing — optional |
| 3 | `LinkToInvoice` | `bit` | `true` | ✅ | **NEW** — `true` = standard return linked to invoice; `false` = standalone return (no invoice reference) |
| 4 | `SupplierId` | `int FK` | — | ✅ | Existing |
| 5 | `WarehouseId` | `int FK` | — | ✅ | Existing |
| 6 | `CurrencyId` | `int? FK` | `null` | ❌ | **NEW** |
| 7 | `ExchangeRate` | `decimal(18,6)?` | `null` | ❌ | **NEW** |
| 8 | `ReturnDate` | `DateTime` | `UTC Now` | ✅ | Existing |
| 9 | `Notes` | `nvarchar(500)?` | `null` | ❌ | Existing |
| 10 | `SubTotal` | `decimal(18,2)` | `0` | ✅ | Existing |
| 11 | `DiscountAmount` | `decimal(18,2)` | `0` | ❌ | **NEW** — supplier discount on return |
| 12 | `DiscountType` | `DiscountType?` | `null` | ❌ | **NEW** |
| 13 | `DiscountRate` | `decimal(18,2)?` | `null` | ❌ | **NEW** |
| 14 | `TotalAmount` | `decimal(18,2)` | `0` | ✅ | Existing |
| 15 | `Status` | `InvoiceStatus` | `Draft=1` | ✅ | Existing |

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/PurchaseReturn.cs` for the canonical factory methods (`Create()` and `CreateStandalone()`).

### 4.8 PurchaseReturnItem — Enhanced Entity

| # | Field | Type | Default | Required | Notes |
|---|-------|------|---------|----------|-------|
| 1 | `Id` | `int PK` | Auto-Increment | ✅ | Existing |
| 2 | `PurchaseReturnId` | `int FK` | — | ✅ | Existing |
| 3 | `ProductId` | `int FK` | — | ✅ | Existing |
| 4 | `ProductUnitId` | `int FK` | — | ✅ | **NEW** |
| 5 | `Quantity` | `decimal(18,3)` | — | ✅ | Existing |
| 6 | `UnitCost` | `decimal(18,2)` | — | ✅ | Existing |
| 7 | `DiscountAmount` | `decimal(18,2)` | `0` | ❌ | Existing |
| 8 | `LineTotal` | `decimal(18,2)` | — | ✅ | Existing |
| 9 | `CostInBaseCurrency` | `decimal(18,2)?` | `null` | ❌ | **NEW** |
| 10 | `Mode` | `SaleMode` | `Retail=1` | ✅ | Existing |
| 11 | `Notes` | `nvarchar(250)?` | `null` | ❌ | Existing |

### 4.9 DTOs — New and Updated

**New DTOs**:

> See `SalesSystem.Contracts/` for canonical DTO definitions, including `PurchaseOrderDto`, `PurchaseOrderItemDto`, `AdditionalFeeDto`, and `AdditionalFeeAllocationDto`.

**Updated DTOs**:

> See `SalesSystem.Contracts/` for canonical DTO definitions, including `PurchaseInvoiceDto`, `PurchaseInvoiceLineDto`, and `PurchaseReturnDto`.

### 4.10 Requests — New and Updated

> See `SalesSystem.Contracts/` and `Contracts/Requests/` for canonical request definitions.

### 4.11 FluentValidation Rules

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns. See `Api/Validators/` for canonical FluentValidation definitions.

---

## 5. Gap Analysis

### 5.1 Purchase Invoice — Enhanced

| Component | Status | Action |
|-----------|--------|--------|
| `CurrencyId` + `ExchangeRate` + `CostInBaseCurrency` | ❌ MISSING | Add to entity + config + migration + DTO + service |
| `AdditionalFeesTotal` | ❌ MISSING | Add to entity + computed on Post |
| `AttachmentPath` | ❌ MISSING | Add to entity + file upload endpoint |
| `DiscountType` + `DiscountRate` | ❌ MISSING | Add to entity + domain methods + recalc |
| `ProductUnitId` on Item | ❌ MISSING | Add to PurchaseInvoiceLine |
| `CostInBaseCurrency` on Item | ❌ MISSING | Add for multi-currency cost tracking |
| `AdditionalFeesAmount` on Item | ❌ MISSING | Add for fee distribution per item |
| `DiscountType` + `DiscountRate` on Item | ❌ MISSING | Add for line-level percentage discount |
| `IAdditionalFeeService` + service | ❌ MISSING | Create new fee service |
| Fee distribution algorithm | ❌ MISSING | Create `FeeDistributionService` |
| Auto journal entry | ❌ MISSING | Add hook in PostAsync |
| Auto supplier payment | ❌ MISSING | Create payment on cash purchases |
| File upload endpoint | ❌ MISSING | Add to controller |
| `AdditionalFeeConfiguration` | ❌ MISSING | Create — FK to PurchaseInvoice (`Restrict`), FK to Account (`Restrict`), precision `decimal(18,2)`, `FeeName` max 100 |
| `AdditionalFeeAllocationConfiguration` | ❌ MISSING | Create — FKs with `Restrict`, precision `decimal(18,2)` |
| Migration | ❌ MISSING | Create migration script |

### 5.2 Purchase Order — New

| Component | Status | Action |
|-----------|--------|--------|
| `PurchaseOrder` entity | ❌ MISSING | Create from scratch |
| `PurchaseOrderItem` entity | ❌ MISSING | Create from scratch |
| `PurchaseOrderConfiguration` | ❌ MISSING | Create |
| `PurchaseOrderItemConfiguration` | ❌ MISSING | Create |
| `IPurchaseOrderService` | ❌ MISSING | Create with Result<T> + UoW |
| `PurchaseOrderService` | ❌ MISSING | Create CRUD + status transitions |
| `PurchaseOrdersController` | ❌ MISSING | Create with endpoints |
| `PurchaseOrderDto` | ❌ MISSING | Create |
| `CreatePurchaseOrderRequest` + Validator | ❌ MISSING | Create |
| `UpdatePurchaseOrderRequest` + Validator | ❌ MISSING | Create |
| Desktop `PurchaseOrderListViewModel` | ❌ MISSING | Create |
| Desktop `PurchaseOrderEditorViewModel` | ❌ MISSING | Create |
| Desktop `PurchaseOrdersListView.xaml` | ❌ MISSING | Create |
| Desktop `PurchaseOrderEditorView.xaml` | ❌ MISSING | Create |
| API Service `IPurchaseOrderApiService` | ❌ MISSING | Create |
| Migration | ❌ MISSING | Create |
| Navigation + DI registration | ❌ MISSING | Wire up |

### 5.3 Purchase Return — Enhanced

| Component | Status | Action |
|-----------|--------|--------|
| `CurrencyId` + `ExchangeRate` | ❌ MISSING | Add to entity + config |
| `DiscountAmount` + `DiscountType` + `DiscountRate` | ❌ MISSING | Add for supplier discount on return |
| `LinkToInvoice` flag + `CreateStandalone()` | ❌ MISSING | Add for "without reference" return mode |
| `ProductUnitId` on Item | ❌ MISSING | Add |
| `CostInBaseCurrency` on Item | ❌ MISSING | Add |
| FIFO batch return logic | ❌ MISSING | Enhance return service to reference original batches |
| Migration | ❌ MISSING | Create |

### 5.4 Auto Accounting Entry

| Component | Status | Action |
|-----------|--------|--------|
| `CreateFromPurchaseAsync()` on journal service | ❌ MISSING | Create interface method + implementation |
| Purchase post hook | ❌ MISSING | Add to PurchaseService.PostAsync |
| Config flag `AutoCreateJournalEntry` | ✅ EXISTS (Phase 19) | Check and use |

### 5.5 Auto Supplier Payment

| Component | Status | Action |
|-----------|--------|--------|
| Auto-create `SupplierPayment` on cash purchase | ❌ MISSING | Add hook in PostAsync |
| Link payment to invoice | ❌ MISSING | Create payment record |
| Different from CashBox transaction | ❌ NOTE | CashBox transaction already exists — need SupplierPayment record too |

---

### 5.6 FIFO Batch Costing on Purchase Receipt

**Analysis reference**: Analysis Part 5 §BatchFIFO requires "كل فاتورة شراء...ترتبط بالباتش...حسب FIFO".

**Problem**: When a purchase invoice is posted, the system updates average cost but does not track individual purchase lots/batches, which is required for FIFO-based cost calculation and sales return cost allocation.

**Fix**: Integrate with the `PurchaseLot` entity from Phase 25 (ProductLot system). On purchase invoice post, each line item creates or updates a `PurchaseLot` record.

> See `docs/AGENTS.md` §2.7 for stock integrity patterns (InventoryBatches/FIFO). See `Application/Services/PurchaseService.cs` for the canonical batch creation in PostAsync.

**Cost allocation on lot creation**:
```
Lot.UnitCost = PurchaseInvoiceLine.UnitCost + (allocated fees / quantity)
Lot.CostInBaseCurrency = converted using invoice.ExchangeRate
```

**Integration with UpdateProductPricingService**:
> See `docs/AGENTS.md` §2.25 for the costing strategy (WeightedAverage formula) and `Application/Services/UpdateProductPricingService.cs` for the canonical implementation.

**Changes required**:

| Component | Action |
|-----------|--------|
| `Domain/Entities/PurchaseLot.cs` | ✅ Already exists (Phase 25) — verify `purchaseInvoiceId`, `PurchaseInvoiceLineId` fields exist |
| `PurchaseLotConfiguration.cs` | Add FK to PurchaseInvoiceLine if missing |
| `PurchaseService.PostAsync()` | Add lot creation loop after stock update |
| Migration | Add PurchaseInvoiceLineId FK if missing |

**Note**: This integrates with Phase 25 `PurchaseLot` entity. Cost is allocated per lot on purchase receipt. The lot record enables FIFO cost allocation on sales returns and provides complete purchase traceability.

| Component | Status | Action |
|-----------|--------|--------|
| `PurchaseLot` entity | ✅ Exists (Phase 25) | Verify field alignment |
| Lot creation in PostAsync | ❌ MISSING | Add loop after stock increase |
| Cost cascade via UpdateProductPricingService | ✅ Exists | No change — pricing service already recalculates from lot costs |

---

## 6. Architectural Decisions

### 6.1 Fee Distribution: Proportional by Cost (Default) vs. Quantity

**Options**:
1. **ByCost** (default): `allocated = (item.LineTotal / invoice.SubTotal) * fee.Amount`
2. **ByQuantity**: `allocated = (item.Quantity / totalQuantity) * fee.Amount`

**Decision**: Support **both** methods, selectable per fee line via `AdditionalFee.DistributionMethod`. Default to `ByCost` as it aligns with accounting standards where fees increase the cost basis proportionally to item value.

### 6.2 Purchase Order Lifecycle

| From | To | Conditions |
|------|----|------------|
| Draft | Approved | User approves (no separate approval step in V1 — auto-approve on save) |
| Draft/Approved | Cancelled | User cancels — no stock/balance impact |
| Approved | PartiallyReceived | When partial invoice created — `PendingReceiveQuantity > 0` |
| PartiallyReceived | Received | When remaining quantity fully invoiced |
| Approved | Received | When fully received in one invoice |

**Decision for V1**: Keep simple 5-state lifecycle: Draft → Approved (auto on save) → PartiallyReceived (partial receipt) → Received (fully received) → Cancelled. No multi-level approval workflow.

> See `docs/AGENTS.md` for domain entity patterns (private set, domain methods). See `Domain/Entities/PurchaseOrder.cs` for the canonical `ReceiveItems()` method.

### 6.3 Auto-Journal Entry Trigger Point

**Decision**: Create journal entry AFTER stock update and BEFORE cash transaction recording, inside the same transaction in `PurchaseService.PostAsync()`. This ensures:
- If journal creation fails, the entire post rolls back
- Journal entry references the now-posted invoice ID
- GL accounts reflect actual cost including fee allocations

### 6.4 Purchase Order → Purchase Invoice Conversion

**Decision**: When creating a Purchase Invoice, the user can optionally link it to a Purchase Order. Items from the order are pre-populated, and the order's `ReceivedQuantity` is updated. The order status changes to `Received` only when ALL items are fully received. Partial receipts keep the order at `Approved`.

### 6.5 Attachment Storage Strategy

**Decision**: Store attachment as file path (`AttachmentPath` nvarchar(255)) on `PurchaseInvoice`. Files stored in `%ProgramData%\SalesSystem\PurchaseAttachments\{InvoiceId}\`. Only one attachment per invoice in V1 (per analysis recommendation). Future: `InvoiceAttachments` table for multiple files.

### 6.6 Discount Model: Line-Level + Invoice-Level

**Decision**: Support both line-level and invoice-level discounts. Each can be either amount or percentage:
- **Line-level**: `PurchaseInvoiceLine.DiscountType` + `DiscountAmount` or `DiscountRate`
- **Invoice-level**: `PurchaseInvoice.DiscountType` + `DiscountAmount` or `DiscountRate`
- Invoice-level discount applies AFTER line-level discounts are computed
- Computed as: `TotalAfterLineDiscounts = Sum(LineTotals) -> InvoiceDiscount -> + Tax -> + Fees`

### 6.7 Auto Supplier Payment Creation

**Decision**: When a purchase invoice is posted with `PaymentType = Cash` and `PaidAmount > 0`, auto-create a `SupplierPayment` record in addition to the existing CashBox transaction. This provides a proper payment trail linked to the supplier account.

### 6.8 Why Purchase Order is V1 (Not Deferred)

Although a purchase order adds scope, it is:
1. **Core to purchase workflow**: Users need to request purchases before executing them
2. **Independent from invoice**: Can be implemented without breaking existing invoice flow
3. **Simple lifecycle**: No approval workflow in V1 — just Draft + Approved + Received + Cancelled
4. **Integration point**: Purchase invoice can optionally reference a PO

### 6.9 Why NOT Composite Invoice Number with PO reference

Invoice numbers are sequential ints per RULE-254. Purchase Orders have their own `OrderNo` sequence. There is no requirement to embed PO reference in invoice number — the link is through the optional `PurchaseOrderId` FK on the invoice service (not stored on the invoice entity itself, but tracked during creation).

---

## 7. Non-V1 Items (Deferred)

These features appeared in analysis but are **deferred** to future versions:

| Feature | Reason |
|---------|--------|
| **Purchase Approval Workflow** | Multi-level approval (Manager → Admin) adds UI complexity. V1: single-step create → post |
| **Automatic Reorder (Auto-PO)** | Requires min-stock alerts integration with auto-PO generation — future phase |
| **Supplier Catalog Sync** | API-based automatic price/catalog sync from supplier systems — enterprise feature |
| **Recurring Purchase Orders** | E.g., monthly standing orders — low priority for V1 |
| **OCR Invoice Scanning** | Auto-read supplier invoice PDF/image — requires external OCR service |
| **Multiple Attachments** | V1 supports single attachment. Future: `InvoiceAttachments` table for multiple files |
| **Purchase Budget Control** | Check PO against department budget before approval — Phase 30+ |
| **Drop-ship Purchase Orders** | PO ships directly to customer — complex workflow |
| **Supplier Portal** | Web portal for suppliers to submit invoices — enterprise feature |
| **Batch FEFO Override** | Allow user to manually select which batch(es) to consume from — advanced feature |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), Arabic ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 1 — Add CurrencyId + ExchangeRate + CostInBaseCurrency to PurchaseInvoice

**Dependency**: Phase 20 (Currencies Module) must be complete — `Currency` entity and service must exist.

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PurchaseInvoice.cs` | Add `int? CurrencyId`, `decimal? ExchangeRate`, `decimal? CostInBaseCurrency`, `Currency? Currency` nav property |
| `Domain/Entities/PurchaseInvoice.cs` | Add `SetCurrency()` domain method |
| `Domain/Entities/PurchaseInvoice.cs` | Update `Create()` params to accept optional currencyId + exchangeRate |
| `Infrastructure/Data/Configurations/PurchaseInvoiceConfiguration.cs` | Add FK config: `HasForeignKey(pi => pi.CurrencyId).OnDelete(DeleteBehavior.Restrict)` + precision for ExchangeRate `HasPrecision(18,6)` |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE PurchaseInvoices ADD CurrencyId int NULL, ExchangeRate decimal(18,6) NULL, CostInBaseCurrency decimal(18,2) NULL` + FK |
| `Contracts/DTOs/AllDtos.cs` — `PurchaseInvoiceDto` | Add `CurrencyId`, `CurrencyCode`, `ExchangeRate`, `CostInBaseCurrency` |
| `Contracts/Requests/PurchaseRequests.cs` — `CreatePurchaseInvoiceRequest` | Add `int? CurrencyId`, `decimal? ExchangeRate` |
| `Contracts/Requests/PurchaseRequests.cs` — `UpdatePurchaseInvoiceRequest` | Same |
| `Application/Services/PurchaseService.cs` | Map currency fields in `CreateAsync()`, `UpdateAsync()`, `MapToDto()` |
| `Application/Services/PurchaseService.cs` | During Post, convert costs to base currency using ExchangeRate if set |
| `Application/Interfaces/Services/IPurchaseService.cs` | Update interface method signatures if needed |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add currency-related properties + load currencies from API |
| `Desktop/Views/Purchases/PurchaseInvoiceEditorView.xaml` | Add Currency combo box + ExchangeRate field (compact style) |

**Domain method**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/PurchaseInvoice.cs` for the canonical `SetCurrency()` method.

**Logging**: `Log.Information("Purchase Invoice {Id} set to currency {CurrencyId} @ rate {ExchangeRate}", id, currencyId, exchangeRate)`

**Validation** (RULE-044): If `CurrencyId` is set, `ExchangeRate > 0` is required.

**ToolTips** (RULE-185-190):
- Currency combo: `"اختيار عملة الفاتورة — العملة الأساسية هي الافتراضية"`
- ExchangeRate field: `"سعر صرف العملة مقابل العملة الأساسية — مطلوب عند اختيار عملة أجنبية"`

**Estimate**: ~2 hours

---

### Task 2 — Add ProductUnitId to PurchaseInvoiceLine + PurchaseReturnItem

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PurchaseInvoiceLine.cs` | Add `int ProductUnitId` + `ProductUnit? ProductUnit` nav + update `Create()` |
| `Domain/Entities/PurchaseInvoiceLine.cs` | Update guard clauses — require `productUnitId > 0` |
| `Domain/Entities/PurchaseReturnItem.cs` | Add `int ProductUnitId` + nav |
| `Infrastructure/Data/Configurations/PurchaseInvoiceLineConfiguration.cs` | Add FK + index config |
| `Infrastructure/Data/Configurations/PurchaseReturnItemConfiguration.cs` | Add FK + index config |
| `Infrastructure/Data/Migrations/` | Migration to add columns + FK |
| `Contracts/DTOs/AllDtos.cs` | Update `PurchaseInvoiceLineDto`, `PurchaseReturnItemDto` |
| `Contracts/Requests/PurchaseRequests.cs` | Update create/update item requests |
| `Application/Services/PurchaseService.cs` | Map `ProductUnitId` in item creation + DTO mapping |
| `Application/Services/PurchaseReturnService.cs` | Same |
| All ViewModels and Views | Update item binding to include unit selection |

**Estimate**: ~1.5 hours

---

### Task 3 — Additional Fee Entity + Fee Distribution Service (NEW)

**Files**:

| File | Content |
|------|---------|
| `Domain/Entities/AdditionalFee.cs` | NEW entity (see Section 4.3) |
| `Domain/Entities/AdditionalFeeAllocation.cs` | NEW entity (see Section 4.4) |
| `Domain/Enums/DistributionMethod.cs` | NEW enum: `ByCost=0`, `ByQuantity=1` |
| `Infrastructure/Data/Configurations/AdditionalFeeConfiguration.cs` | NEW — FK, precision, delete restrict |
| `Infrastructure/Data/Configurations/AdditionalFeeAllocationConfiguration.cs` | NEW — FK, precision |
| `Infrastructure/Data/Migrations/` | NEW migration for 2 tables |
| `Contracts/DTOs/AllDtos.cs` | Add `AdditionalFeeDto` |
| `Contracts/Requests/PurchaseRequests.cs` | Add `CreateAdditionalFeeRequest` |
| `Application/Interfaces/Services/IAdditionalFeeService.cs` | NEW — Interface for fee management |
| `Application/Services/AdditionalFeeService.cs` | NEW — CRUD + distribution logic |
| `Application/Services/FeeDistributionService.cs` | NEW — Distribution algorithm service |

> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern. See `Application/Services/FeeDistributionService.cs` for the canonical implementation.

**Validation** (RULE-044):
- `CreateAdditionalFeeRequestValidator`: FeeName required (max 100), FeeAmount > 0, DistributionMethod 0-1

**Logging**: `Log.Information("Additional fee {FeeName} ({Amount}) distributed across {Count} items on invoice {InvoiceId}", feeName, amount, items.Count, invoiceId)`

**Estimate**: ~3 hours

---

### Task 4 — Enhanced Discount Model (Line-Level + Invoice-Level, Amount or Percentage)

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/DiscountType.cs` | NEW enum: `Amount=0`, `Percentage=1` |
| `Domain/Entities/PurchaseInvoice.cs` | Add `DiscountType`, `DiscountRate` properties + `SetDiscount()` method |
| `Domain/Entities/PurchaseInvoice.cs` | Update `RecalculateTotals()` to support percentage discount on invoice |
| `Domain/Entities/PurchaseInvoiceLine.cs` | Add `DiscountType`, `DiscountRate` + update `RecalculateLineTotal()` |
| `Domain/Entities/PurchaseReturn.cs` | Add `DiscountType`, `DiscountRate` properties + update totals |
| `Domain/Entities/PurchaseReturnItem.cs` | Add discount fields + update recalc |
| `Infrastructure/Data/Configurations/` | Update all configurations with new columns |
| `Infrastructure/Data/Migrations/` | Migration for new columns |
| `Contracts/DTOs/AllDtos.cs` | Add discount fields to all DTOs |
| `Contracts/Requests/PurchaseRequests.cs` | Add discount fields to requests |
| `Application/Services/PurchaseService.cs` | Map new fields + update validation |
| `Application/Services/PurchaseReturnService.cs` | Map new fields |
| All ViewModels and Views | Add UI for discount type selection (radio: مبلغ/نسبة) + rate field |

**Validation** (RULE-044):
> See `Api/Validators/CreatePurchaseInvoiceRequestValidator.cs` for the canonical FluentValidation rules including discount percentage validation.

**ToolTips** (RULE-185-190):
- Discount type radio: `"نوع الخصم: مبلغ ثابت أو نسبة مئوية"`
- Discount rate field: `"نسبة الخصم — مثال: 10 تعني 10%"`
- Discount amount field: `"مبلغ الخصم — أدخل القيمة مباشرة"`

**Estimate**: ~2 hours

---

### Task 5 — Add Attachment Support (Invoice Image)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PurchaseInvoice.cs` | Add `string? AttachmentPath` + `SetAttachment()` |
| `Infrastructure/Data/Configurations/PurchaseInvoiceConfiguration.cs` | Add `.Property(pi => pi.AttachmentPath).HasMaxLength(255)` |
| `Infrastructure/Data/Migrations/` | Migration: `ALTER TABLE PurchaseInvoices ADD AttachmentPath nvarchar(255) NULL` |
| `Contracts/DTOs/AllDtos.cs` | Add `string? AttachmentPath` to `PurchaseInvoiceDto` |
| `Contracts/Requests/PurchaseRequests.cs` | Add `string? AttachmentBase64`, `string? AttachmentFileName` |
| `Api/Controllers/PurchaseInvoicesController.cs` | Add `POST /api/v1/purchase-invoices/{id}/upload-attachment` endpoint |
| `Api/Controllers/PurchaseInvoicesController.cs` | Update `Create` to accept base64 attachment |
| `Application/Services/PurchaseService.cs` | Handle file save in `CreateAsync()`: decode base64 → save to disk → store path |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add attachment-related properties + file picker command |
| `Desktop/Views/Purchases/PurchaseInvoiceEditorView.xaml` | Add photo icon button `📷 إرفاق صورة` + thumbnail preview |

**File storage path**: `%ProgramData%\SalesSystem\PurchaseAttachments\{InvoiceId}\`

**Logging**: `Log.Information("Attachment uploaded for purchase invoice {Id}: {FileName}", invoiceId, fileName)`

**ToolTips** (RULE-185-190):
- Upload button: `"📷 إرفاق صورة فاتورة المورد — اختياري"`
- Remove button: `"إزالة الصورة المرفقة"`

**Estimate**: ~1.5 hours

---

### Task 6 — Purchase Order Entity + CRUD (NEW)

#### 6.1 Domain Layer

**Files**: `Domain/Entities/PurchaseOrder.cs`, `Domain/Entities/PurchaseOrderItem.cs`, `Domain/Enums/PurchaseOrderStatus.cs`

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods). See `Domain/Entities/PurchaseOrder.cs` for the canonical definition.

#### 6.2 Infrastructure Layer

**Files**: `PurchaseOrderConfiguration.cs`, `PurchaseOrderItemConfiguration.cs`, migration

#### 6.3 Contracts Layer

**Files**: `Contracts/DTOs/AllDtos.cs` — Add `PurchaseOrderDto`, `PurchaseOrderItemDto`
**Files**: `Contracts/Requests/PurchaseOrderRequests.cs` — NEW: `CreatePurchaseOrderRequest`, `UpdatePurchaseOrderRequest`

#### 6.4 Application Layer

**Files**:
- `Application/Interfaces/Services/IPurchaseOrderService.cs` — NEW
- `Application/Services/PurchaseOrderService.cs` — NEW: CRUD + status transitions

**Service methods**:
> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern. See `Application/Interfaces/Services/IPurchaseOrderService.cs` for the canonical interface.

#### 6.5 API Layer

**File**: `Api/Controllers/PurchaseOrdersController.cs`

| Method | Endpoint | Policy |
|--------|----------|--------|
| GET | `/api/v1/purchase-orders` | `ManagerAndAbove` |
| GET | `/api/v1/purchase-orders/{id}` | `ManagerAndAbove` |
| GET | `/api/v1/purchase-orders/pending` | `ManagerAndAbove` |
| POST | `/api/v1/purchase-orders` | `ManagerAndAbove` |
| PUT | `/api/v1/purchase-orders/{id}` | `ManagerAndAbove` |
| POST | `/api/v1/purchase-orders/{id}/cancel` | `ManagerAndAbove` |

**Controller purity** (RULE-203): Inject `IPurchaseOrderService` only — NO `DbContext` or `IUnitOfWork`

#### 6.6 Desktop Layer

**Files** (10 files):

| File | Content |
|------|---------|
| `Services/Api/PurchaseOrderApiService.cs` | NEW — HTTP client (content-type guard RULE-184) |
| `ViewModels/Purchases/PurchaseOrderListViewModel.cs` | NEW — List with newest-first sort (RULE-220) |
| `Views/Purchases/PurchaseOrdersListView.xaml` + `.cs` | NEW — DataGrid + ToolTips + compact styles |
| `ViewModels/Purchases/PurchaseOrderEditorViewModel.cs` | NEW — INotifyDataErrorInfo (RULE-228), SetDialogService(), ValidateAllAsync() |
| `Views/Purchases/PurchaseOrderEditorView.xaml` + `.cs` | NEW — Form with items grid |
| `Messaging/Messages/AppMessages.cs` | Add `PurchaseOrderChangedMessage` |
| `App.xaml.cs` | DI registrations + navigation |

**ViewModel patterns** (RULE-141):
- All async commands wrapped in `ExecuteAsync()`
- LogSystemError() for system errors (RULE-199)
- Screen-specific dialog titles: `"خطأ في حفظ أمر الشراء"` (RULE-173)
- No MessageBox.Show (RULE-174)

**UI Compact** (RULE-262-274):
- All heights via styles (28px default)
- Header: `Padding="12,6"`, Footer: `Padding="12,8"`
- Section margins: `Margin="0,0,0,6"`
- Dialog font: `FontSize="16"`, section headers: `FontSize="14"`
- Empty-state buttons: `Margin="0,12,0,0"` Width="140"

**ToolTips** (RULE-185-190):
- Add button: `"إنشاء أمر شراء جديد"`
- Edit button: `"تعديل أمر الشراء"`
- Cancel button: `"إلغاء أمر الشراء"`
- Save button: `"حفظ أمر الشراء — سيتم اعتماده تلقائياً"`
- Product search: `"البحث عن صنف لإضافته لأمر الشراء"`
- Empty-state: `"➕ إنشاء أول أمر شراء — طلب شراء بضاعة من المورد"`

**Estimate**: ~6 hours

---

### Task 7 — Purchase Return Enhancement (Discount, Currency, ProductUnitId, LinkToInvoice)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PurchaseReturn.cs` | Add `CurrencyId`, `ExchangeRate`, `DiscountType`, `DiscountRate`, `DiscountAmount`, `LinkToInvoice` |
| `Domain/Entities/PurchaseReturn.cs` | Update `Create()` + add `CreateStandalone()` factory + `RecalculateTotals()` for discounts |
| `Domain/Entities/PurchaseReturnItem.cs` | Add `ProductUnitId` + `CostInBaseCurrency` |
| `Domain/Entities/PurchaseReturnItem.cs` | Update `Create()` with new params |
| `Infrastructure/Data/Configurations/` | Update configurations |
| `Infrastructure/Data/Migrations/` | Migration |
| `Contracts/DTOs/AllDtos.cs` | Update DTOs |
| `Contracts/Requests/` | Update return requests |
| `Application/Services/PurchaseReturnService.cs` | Map new fields + enhance Post for discount accounting |
| `Application/Interfaces/Services/IPurchaseReturnService.cs` | Update interface |
| `Desktop/ViewModels/Returns/PurchaseReturnEditorViewModel.cs` | Add discount + currency fields |
| `Desktop/Views/Returns/PurchaseReturnEditorView.xaml` | Add discount section + currency combo |

**Estimate**: ~2 hours

---

### Task 8 — Auto Journal Entry Creation on Purchase Post

**Dependency**: Phase 22 (Accounting Foundation) must provide `IJournalEntryService`.

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IJournalEntryService.cs` | Add `CreateFromPurchaseAsync(PurchaseInvoice invoice, int userId, CancellationToken ct)` |
| `Application/Services/JournalEntryService.cs` | Implement purchase → journal entry mapping |
| `Application/Services/PurchaseService.cs` | Add hook after stock update, before commit |
| `Domain/Enums/JournalEntryReferenceType.cs` | Add `PurchaseInvoice = 1` (if not existing) |

**Journal entry mapping for purchases**:

> See `docs/AGENTS.md` §2.76 (Phase 24 — Accounting Integration Rules, RULE-373/374) for journal entry mapping patterns. See `Application/Services/AccountingIntegrationService.cs` for the canonical `CreateFromPurchaseAsync()` implementation.

**Note**: The exact account mapping depends on the Chart of Accounts structure (Phase 22). Default accounts must be configured in `SystemSettings` with keys like `Account.PurchaseInventory`, `Account.PurchaseVat`, `Account.SupplierPayable`.

**Logging**: `Log.Information("Journal entry created for purchase invoice {InvoiceId} ({InvoiceNo})", invoice.Id, invoice.InvoiceNo)`

**Validation**: If auto-journal entry fails AND setting `AutoCreateJournalEntry` is `true`, rollback the entire transaction.

**Estimate**: ~2 hours (depends on Phase 22 completion)

---

### Task 9 — Auto Supplier Payment Creation on Cash Purchases

**Files**:

| File | Change |
|------|--------|
| `Application/Services/PurchaseService.cs` | Add supplier payment creation hook after Post |
| `Application/Interfaces/Services/ISupplierPaymentService.cs` | Ensure `CreateAsync` exists |
| `Domain/Entities/SupplierPayment.cs` | Ensure entity exists (check current state) |
| `Application/Services/SupplierPaymentService.cs` | Add `CreateFromPurchaseAsync()` method |

**Logic**:
> See `docs/AGENTS.md` for transaction patterns (RULE-003/RULE-005) and service layer patterns. See `Application/Services/PurchaseService.cs` for the canonical PostAsync() implementation.

**Logging**: `Log.Information("Auto supplier payment of {Amount} created for purchase invoice {Id}", paidAmount, invoice.Id)`

**Estimate**: ~1 hour

---

### Task 10 — Enhanced PurchaseInvoiceEditorViewModel (New Fields)

**Files**:

| File | Change |
|------|--------|
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add: `Currencies` collection, `SelectedCurrencyId`, `ExchangeRate`, `AttachmentPath`, `DiscountType`, `DiscountRate`, `AdditionalFees` collection |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add: `CurrencyChanged` → enable/disable ExchangeRate field |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add: `BrowseAttachmentCommand`, `RemoveAttachmentCommand` |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Add: `AddAdditionalFeeCommand` — opens inline fee entry |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Update: `SaveOperationAsync()` to map new fields |
| `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Update: `LoadInvoiceAsync()` to restore new fields |

**ViewModel properties to add**:
> See `docs/AGENTS.md` for ViewModel patterns (RULE-141: ExecuteAsync wrapper, INotifyDataErrorInfo). See `Desktop/ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` for the canonical implementation.

**Estimate**: ~3 hours

---

### Task 11 — Updated PurchaseInvoiceEditorView.xaml (Compact + ToolTips)

**Files**: `Desktop/Views/Purchases/PurchaseInvoiceEditorView.xaml`

**Changes**:
1. Add Currency combo box row (with `ExchangeRate` field) — appears when currency != base
2. Add Discount type radio buttons (مبلغ / نسبة) with rate input
3. Add Additional Fees section with inline DataGrid
4. Add Attachment upload button with thumbnail preview
5. Apply UI Compact rules (RULE-262-274) across entire view

**XAML structure for new fields**:
> See `docs/AGENTS.md` §2.64 (UI Compacting Rules RULE-262-274) and §2.43 (UI ToolTips RULE-185-190). See `Desktop/Views/Purchases/PurchaseInvoiceEditorView.xaml` for the canonical XAML.

**Estimate**: ~3 hours

---

### Task 12 — API Endpoint Updates

**Files**: `Api/Controllers/PurchaseInvoicesController.cs`, `Api/Controllers/PurchaseOrdersController.cs` (NEW)

**Updated PurchaseInvoicesController endpoints**:

| Method | Endpoint | Change |
|--------|----------|--------|
| GET | `/api/v1/purchase-invoices` | Add `currencyId` query param filter |
| POST | `/api/v1/purchase-invoices` | Accept new request fields (currency, fees, discount type, attachment) |
| PUT | `/api/v1/purchase-invoices/{id}` | Accept new request fields |
| POST | `/api/v1/purchase-invoices/{id}/post` | No change (logic changes in service) |
| POST | `/api/v1/purchase-invoices/{id}/cancel` | No change |
| POST | `/api/v1/purchase-invoices/{id}/upload-attachment` | **NEW** — file upload endpoint |
| GET | `/api/v1/purchase-invoices/{id}/attachment` | **NEW** — download attachment |

**File upload endpoint**:
> See `docs/AGENTS.md` §2.47 for Controller purity rules (RULE-202/203). See `Api/Controllers/PurchaseInvoicesController.cs` for the canonical attachment endpoint.

**New PurchaseOrdersController endpoints**:

| Method | Endpoint | Policy |
|--------|----------|--------|
| GET | `/api/v1/purchase-orders` | `ManagerAndAbove` |
| GET | `/api/v1/purchase-orders/{id}` | `ManagerAndAbove` |
| GET | `/api/v1/purchase-orders/pending` | `ManagerAndAbove` |
| POST | `/api/v1/purchase-orders` | `ManagerAndAbove` |
| PUT | `/api/v1/purchase-orders/{id}` | `ManagerAndAbove` |
| POST | `/api/v1/purchase-orders/{id}/cancel` | `ManagerAndAbove` |

**Estimate**: ~2 hours

---

### Task 13 — Purchase Invoice List ViewModel Enhancement

**Files**:

| File | Change |
|------|--------|
| `Desktop/ViewModels/Purchases/PurchaseInvoiceListViewModel.cs` | Add currency display column + filter by currency |
| `Desktop/Views/Purchases/PurchaseInvoicesListView.xaml` | Add Currency column to DataGrid + currency filter |

**Estimate**: ~1 hour

---

### Task 14 — Purchase Reports Endpoints

**Files**: `Api/Controllers/ReportsController.cs` or new `PurchaseReportsController.cs`

**New report endpoints**:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reports/purchases/summary` | Summary by supplier, period |
| GET | `/api/v1/reports/purchases/supplier/{supplierId}` | Supplier purchase history |
| GET | `/api/v1/reports/purchases/pending` | Pending purchase orders |
| GET | `/api/v1/reports/purchases/by-product/{productId}` | Purchase history by product (for cost analysis) |

**Estimate**: ~2 hours



### Task 15 — Comprehensive Unit Tests (Purchases Module)

**Test projects**:
- `SalesSystem.Domain.Tests/Entities/Purchases/`
- `SalesSystem.Application.Tests/Services/`
- `SalesSystem.Api.Tests/Validators/`
- `SalesSystem.Infrastructure.Tests/Configurations/`

---

#### 15.1 Domain Entity Tests

**PurchaseInvoiceTests.cs** — Test `PurchaseInvoice.Create()`:
| Test | Expected |
|------|----------|
| Valid input creates with SupplierId InvoiceDate InvoiceNo (int) Status=Draft | Passes |
| InvoiceNo = 0 throws DomainException | Throws |
| InvoiceNo = -5 throws DomainException | Throws |
| InvoiceNo is int (not string) type verified | Verified |
| SupplierId = 0 throws DomainException | Throws |
| InvoiceDate default throws DomainException | Throws |
| Notes > 500 chars throws DomainException | Throws |
| No items throws DomainException | Throws |
| SupplierInvoiceNo > 50 chars throws DomainException | Throws |
| Status lifecycle Draft(1) Posted(2) Cancelled(3) | Verified |
| Posted to Draft transition throws DomainException | Throws |
| Cancelled to anything throws DomainException | Throws |
| Post() with no items throws DomainException | Throws |
| Post() on Draft Status=Posted stock increased | Passes |
| Cancel() on Posted Status=Cancelled stock reversed | Passes |
| PaidAmount > TotalAmount throws DomainException | Throws |
| DueAmount = TotalAmount - PaidAmount computed correctly | Passes |

**PurchaseInvoiceLineTests.cs**:
| Test | Expected |
|------|----------|
| Valid item ProductId Quantity UnitCost LineTotal Qty x UnitCost - DiscountAmount | Passes |
| Quantity = 0 throws DomainException | Throws |
| Quantity negative throws DomainException | Throws |
| UnitCost negative throws DomainException | Throws |
| ProductId = 0 throws DomainException | Throws |
| DiscountAmount negative throws DomainException | Throws |
| DiscountAmount > LineTotal throws DomainException | Throws |
| ProductUnitId = 0 throws DomainException | Throws |
| CostInBaseCurrency = UnitCost x ExchangeRate | Passes |
| AdditionalFeesAmount allocated correctly | Passes |

**PurchaseOrderTests.cs** — Test `PurchaseOrder.Create()`:
| Test | Expected |
|------|----------|
| Valid input SupplierId OrderDate Status=Pending(1) | Passes |
| Status lifecycle Pending(1) Approved(2) PartiallyReceived(3) Received(4) Cancelled(5) | Verified |
| Approve() on Pending Status=Approved | Passes |
| Approve() on Cancelled throws DomainException | Throws |
| Receive() on Approved Status=Received or PartiallyReceived | Passes |
| Receive() on Received throws DomainException | Throws |
| Cancel() on Pending/Approved to Cancelled | Passes |
| Cancel() on Received throws DomainException | Throws |
| PendingReceiveQuantity = ordered - received | Passes |
| PartiallyReceived Status=3 when partial items received | Verified |
| SupplierId = 0 throws DomainException | Throws |
| OrderDate default throws DomainException | Throws |
| No items throws DomainException | Throws |
| Notes > 500 chars throws DomainException | Throws |

**PurchaseOrderItemTests.cs**:
| Test | Expected |
|------|----------|
| Valid item ProductId Quantity UnitCost | Passes |
| Quantity = 0 throws DomainException | Throws |
| UnitCost < 0 throws DomainException | Throws |
| ReceivedQuantity init = 0 updates on partial receive | Passes |
| RemainingQuantity = Quantity - ReceivedQuantity | Passes |
| MarkAsReceived(qty) where qty > RemainingQuantity throws DomainException | Throws |
| MarkAsReceived(qty) valid ReceivedQuantity incremented | Passes |

**AdditionalFeeTests.cs**:
| Test | Expected |
|------|----------|
| Valid fee FeeName Amount AllocationMethod ByCost/ByQuantity | Passes |
| FeeName empty throws DomainException | Throws |
| Amount <= 0 throws DomainException | Throws |
| AccountId = 0 throws DomainException | Throws |
| AllocationMethod out of range throws DomainException | Throws |
| FeeAllocation.Create() valid ItemId AllocatedAmount | Passes |
| FeeAllocation.Create() AllocatedAmount <= 0 throws DomainException | Throws |
| AccountId FK navigation property exists | Verified |

**PurchaseReturnTests.cs**:
| Test | Expected |
|------|----------|
| Create() with reference (LinkToInvoice=true) linked to PurchaseInvoice | Passes |
| CreateStandalone() (LinkToInvoice=false) no reference | Passes |
| Create() with null PurchaseInvoiceId when LinkToInvoice=true throws DomainException | Throws |
| CreateStandalone() valid data Status=Draft no reference | Passes |
| InvoiceNo = 0 throws DomainException | Throws |
| SupplierId = 0 throws DomainException | Throws |
| ReturnDate default throws DomainException | Throws |
| No items throws DomainException | Throws |
| Post() on Draft Status=Posted stock returned | Passes |
| Cancel() on Posted Status=Cancelled stock reversed | Passes |

**PurchaseReturnItemTests.cs**:
| Test | Expected |
|------|----------|
| Valid item ProductId Quantity UnitCost | Passes |
| Quantity positive stored correctly | Passes |
| CostInBaseCurrency = UnitCost x ExchangeRate | Passes |
| ProductUnitId required (non-zero) | Verified |

**47 Arabic DomainException guards verified**:
| Entity | Guard Count | Sample Messages |
|--------|-------------|-----------------|
| PurchaseInvoice | 12 | "رقم الفاتورة يجب أن يكون أكبر من صفر", "المورد مطلوب", "الملاحظات تتجاوز 500 حرف" |
| PurchaseInvoiceLine | 8 | "الكمية يجب أن تكون أكبر من الصفر", "التكلفة لا يمكن أن تكون سالبة", "وحدة القياس مطلوبة" |
| PurchaseOrder | 8 | "المورد مطلوب", "تاريخ الأمر مطلوب", "يجب إضافة صنف واحد على الأقل" |
| PurchaseOrderItem | 4 | "الكمية يجب أن تكون أكبر من الصفر", "التكلفة لا يمكن أن تكون سالبة" |
| AdditionalFee | 6 | "اسم الرسم مطلوب", "المبلغ يجب أن يكون أكبر من صفر", "حساب الرسم الإضافي مطلوب" |
| PurchaseReturn | 6 | "رقم المرتجع يجب أن يكون أكبر من صفر", "المورد مطلوب", "تاريخ المرتجع مطلوب" |
| PurchaseReturnItem | 3 | domain guards for quantity and cost |

---

#### 15.2 Service Tests (using Mock)

**PurchaseServiceTests.cs**:
| Test | Expected |
|------|----------|
| CreateAsync(validRequest) returns Result.Success | Passes |
| CreateAsync(null) returns Result.Failure | Passes |
| CreateAsync with duplicate InvoiceNo returns Result.Failure (unique constraint) | Passes |
| PostAsync(invoiceId) Status=Posted stock increased cost updated | Passes |
| PostAsync with insufficient supplier balance returns Result.Failure | Passes |
| PostAsync on already Posted invoice returns Result.Failure | Passes |
| CancelAsync(invoiceId) Status=Cancelled stock reversed | Passes |
| CancelAsync non-existent returns Result.Failure | Passes |
| Transaction rollback: stock update fails invoice stays Draft | Verified |
| Transaction rollback: journal entry fails ALL changes undone | Verified |
| GetByIdAsync(id) returns correct dto | Passes |
| GetPagedAsync(page size) paginated results | Passes |
| Auto journal entry on Post Debit Inventory Credit AP | Verified |
| WeightedAverage cost cascade on Post ALL product units updated | Verified |
| Supplier balance update on Post IncreaseBalance(DueAmount) | Verified |
| Supplier balance reversal on Cancel DecreaseBalance(TotalAmount) | Verified |
| FIFO batch costing PurchaseLot.Create() per-line cost allocation | Verified |

**PurchaseOrderServiceTests.cs**:
| Test | Expected |
|------|----------|
| CreateAsync(validRequest) returns Result.Success | Passes |
| ApproveAsync(orderId) Status=Approved | Passes |
| ReceiveAsync(orderId items) updates ReceivedQuantity + status | Passes |
| ReceiveAsync partial Status=PartiallyReceived(3) | Passes |
| ReceiveAsync full Status=Received(4) | Passes |
| CancelAsync(orderId) Pending to Cancelled | Passes |
| CancelAsync(orderId) on Received returns Result.Failure | Passes |
| Transaction rollback on Receive failure no partial stock | Verified |

**AdditionalFeeServiceTests.cs**:
| Test | Expected |
|------|----------|
| CreateFeeAsync(validFee) returns Result.Success | Passes |
| AllocateFeesAsync(invoiceId feeIds) allocations computed and applied | Passes |
| Allocation ByCost: fee distributed by LineTotal | Verified |
| Allocation ByQuantity: fee distributed by Quantity | Verified |
| RemoveFeeAsync(feeId) fee removed allocations reversed | Passes |
| AccountId FK validated before allocation | Verified |

**PurchaseReturnServiceTests.cs**:
| Test | Expected |
|------|----------|
| CreateReturnAsync(validRequest) with reference linked correctly | Passes |
| CreateReturnAsync(validRequest) standalone no reference | Passes |
| PostReturnAsync(returnId) stock returned supplier balance adjusted | Passes |
| CancelReturnAsync(returnId) stock reversed | Passes |
| Transaction rollback on Post failure | Verified |

---

#### 15.3 FluentValidation Tests

**CreatePurchaseInvoiceRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request IsValid=true | Passes |
| SupplierId = 0 validation error | Passes |
| InvoiceDate empty validation error | Passes |
| Items empty validation error | Passes |
| Item Quantity <= 0 validation error | Passes |
| Item UnitCost < 0 validation error | Passes |
| Item ProductId = 0 validation error | Passes |
| Item ProductUnitId = 0 validation error | Passes |
| InvoiceNo <= 0 with AutoGenerate=false validation error | Passes |
| InvoiceNo null auto-generate mode (valid) | Passes |
| SupplierInvoiceNo > 50 chars validation error | Passes |
| DiscountRate out of range (0-100) validation error | Passes |
| Notes > 500 chars validation error | Passes |

**CreatePurchaseOrderRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request IsValid=true | Passes |
| SupplierId = 0 validation error | Passes |
| Items empty validation error | Passes |
| Item Quantity <= 0 validation error | Passes |
| Notes > 500 chars validation error | Passes |

**CreateAdditionalFeeRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request IsValid=true | Passes |
| FeeName empty validation error | Passes |
| Amount <= 0 validation error | Passes |
| AccountId = 0 validation error | Passes |
| AllocationMethod out of range validation error | Passes |
| FeeName > 200 chars validation error | Passes |

**CreatePurchaseReturnRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request with reference IsValid=true | Passes |
| Valid standalone request IsValid=true | Passes |
| SupplierId = 0 validation error | Passes |
| Items empty validation error | Passes |
| Item Quantity <= 0 validation error | Passes |
| LinkToInvoice=true with null PurchaseInvoiceId validation error | Passes |

---

#### 15.4 Database Configuration Tests

**PurchaseInvoiceConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| InvoiceNo is int not string Property(x => x.InvoiceNo).HasColumnType(int) | Verified |
| UNIQUE index on InvoiceNo | Verified |
| SupplierInvoiceNo HasMaxLength(50) + nvarchar | Verified |
| SubTotal HasPrecision(18 2) | Verified |
| DiscountAmount HasPrecision(18 2) | Verified |
| TaxAmount HasPrecision(18 2) | Verified |
| TotalAmount HasPrecision(18 2) | Verified |
| PaidAmount HasPrecision(18 2) | Verified |
| DueAmount computed not persisted | Verified |
| CurrencyId FK DeleteBehavior.Restrict | Verified |
| Status stored as int Draft=1 Posted=2 Cancelled=3 | Verified |
| HasQueryFilter(x => x.IsActive) | Verified |
| ExchangeRate HasPrecision(18 6) | Verified |
| CostInBaseCurrency HasPrecision(18 2) | Verified |
| DiscountType stored as int | Verified |
| DiscountRate HasPrecision(18 2) | Verified |
| AttachmentPath HasMaxLength(500) | Verified |
| AdditionalFeesTotal HasPrecision(18 2) | Verified |
| FK on all references uses DeleteBehavior.Restrict | Verified |

**PurchaseInvoiceLineConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Quantity HasPrecision(18 3) | Verified |
| UnitCost HasPrecision(18 2) | Verified |
| LineTotal HasPrecision(18 2) computed | Verified |
| DiscountAmount HasPrecision(18 2) | Verified |
| DiscountRate HasPrecision(18 2) | Verified |
| AdditionalFeesAmount HasPrecision(18 2) | Verified |
| CostInBaseCurrency HasPrecision(18 2) | Verified |
| ProductId FK DeleteBehavior.Restrict | Verified |
| ProductUnitId FK DeleteBehavior.Restrict | Verified |
| InvoiceId FK DeleteBehavior.Restrict | Verified |

**PurchaseOrderConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| OrderNo is int | Verified |
| Status stored as int Pending=1 Approved=2 PartiallyReceived=3 Received=4 Cancelled=5 | Verified |
| HasQueryFilter(x => x.IsActive) | Verified |
| SupplierId FK DeleteBehavior.Restrict | Verified |
| Notes HasMaxLength(500) | Verified |

**PurchaseOrderItemConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Quantity HasPrecision(18 3) | Verified |
| ReceivedQuantity HasPrecision(18 3) | Verified |
| UnitCost HasPrecision(18 2) | Verified |
| TotalCost HasPrecision(18 2) | Verified |
| ProductId FK DeleteBehavior.Restrict | Verified |
| OrderId FK DeleteBehavior.Restrict | Verified |

**AdditionalFeeConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| FeeName HasMaxLength(200) + IsRequired | Verified |
| Amount HasPrecision(18 2) | Verified |
| AccountId FK DeleteBehavior.Restrict | Verified |
| AllocationMethod stored as int | Verified |
| PurchaseInvoiceId FK DeleteBehavior.Restrict | Verified |

**PurchaseReturnConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| InvoiceNo is int | Verified |
| LinkToInvoice bool stored as bit | Verified |
| InvoiceId FK nullable DeleteBehavior.Restrict | Verified |
| HasQueryFilter(x => x.IsActive) | Verified |
| ReturnDate IsRequired | Verified |

**PurchaseReturnItemConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Quantity HasPrecision(18 3) | Verified |
| UnitCost HasPrecision(18 2) | Verified |
| LineTotal HasPrecision(18 2) | Verified |
| CostInBaseCurrency HasPrecision(18 2) | Verified |
| ProductUnitId FK DeleteBehavior.Restrict | Verified |

---

#### 15.5 Phase-Specific Tests

| # | Test Area | Test Case | Expected |
|---|-----------|-----------|----------|
| 1 | **InvoiceNo int** | Create PurchaseInvoice with InvoiceNo=1001 | Stored as int verified type |
| 2 | **InvoiceNo duplicate** | Create two invoices with InvoiceNo=1001 | Second save throws unique constraint violation |
| 3 | **InvoiceNo auto-gen** | Create with null InvoiceNo service calls IDocumentSequenceService.GetNextIntAsync("PurchaseInvoice", ct) | Correct auto-increment |
| 4 | **Status lifecycle** | Draft Posted Cancelled transitions | All valid/invalid transitions verified |
| 5 | **PO Status lifecycle** | Pending Approved PartiallyReceived Received Cancelled | All 5 states verified |
| 6 | **PartiallyReceived PO** | Receive 5 of 10 items Status=PartiallyReceived(3) | Correct status |
| 7 | **PurchaseLot FIFO** | Create PurchaseLot with cost allocation per-line | Lot cost correctly recorded |
| 8 | **Partial receive FIFO** | Receive partial PO lot cost allocated to received qty only | Correct cost basis |
| 9 | **AdditionalCharge.AccountId** | Fee linked to valid CoA account | FK constraint satisfied |
| 10 | **Auto journal entry** | Post invoice Debit Inventory Credit AccountsPayable | Correct double entry |
| 11 | **Cost cascade WA** | Receive 100 units at 15 SAR old stock 50 at 10 SAR WA = 13.33 | Correct weighted average |
| 12 | **decimal(18 3) quantity** | PurchaseInvoiceLine.Quantity = 100.500 | Stored with 3 decimal places |
| 13 | **decimal(18 2) money** | All monetary fields SubTotal=1250.75 TotalAmount=1500.00 | Stored with 2 decimal places |
| 14 | **decimal(18 6) exchange** | ExchangeRate=3.754321 | Stored with 6 decimal places |
| 15 | **PurchaseReturn reference** | CreateReturn linked to PurchaseInvoice | Stock returns to original batches |
| 16 | **PurchaseReturn standalone** | CreateStandalone no original invoice | Stock returns using AvgCost |
| 17 | **Fee allocation ByCost** | 100 SAR fee on items with LineTotals 200 300 alloc 40 60 | Correct proportion |
| 18 | **Fee allocation ByQuantity** | 100 SAR fee on items with Qty 10 15 alloc 40 60 | Correct proportion |
| 19 | **SupplierInvoiceNo ref** | Supplier reference number stored separately from system InvoiceNo | Both preserved distinct |
| 20 | **Discount percentage** | 10% discount on 1000 SAR invoice DiscountAmount=100 | Correct calculation |

**Estimate**: ~12 hours (3h Domain 3h Service 2h Validation 2h Configuration 2h Phase-specific)


### Task 16 — Self-Explanation ◉ Tooltips (Purchases Module)

**Objective**: Add ⓘ (InfoTooltip) controls next to key terms in the Purchases module UI. Each tooltip provides an Arabic explanation of the term using the same `InfoTooltip` UserControl pattern from Phase 18.

**Pattern**: `◉` icon with styled ToolTip bound to `HelpText` property.

**Where to add**: Purchase Invoice editor, Purchase Order editor, Purchase Return editor — next to section headers, labels, and DataGrid column headers.

**ⓘ Terms table (Purchases)**:

| Term | Explanation (Arabic) |
|------|---------------------|
| فاتورة شراء | "فاتورة الشراء هي مستند يثبت قيام الشركة بشراء بضاعة أو خدمات من المورد." |
| أمر شراء | "أمر الشراء هو مستند ترسله الشركة للمورد لطلب شراء بضاعة، ويتم تحويله لفاتورة عند الاستلام." |
| تكلفة البضاعة | "تكلفة البضاعة هي المبلغ الذي دفعته الشركة لشراء المنتج، وتشمل سعر الشراء + المصاريف الإضافية (نقل، جمارك)." |
| متوسط التكلفة | "متوسط التكلفة = (قيمة المخزون القديم + قيمة المشتريات الجديدة) ÷ (الكمية القديمة + الكمية الجديدة). يستخدم لحساب تكلفة البضاعة." |
| FIFO | "FIFO تعني أول وارد يصرف أولاً. النظام سيصرف البضاعة الأقدم في المخزون أولاً عند البيع." |
| Batch / باتش | "الباتش هو دفعة إنتاج أو شحنة محددة. يتيح تتبع البضاعة حسب تاريخ الاستلام وتاريخ الانتهاء." |
| رسوم إضافية | "الرسوم الإضافية هي تكاليف إضافية تضاف إلى تكلفة البضاعة مثل: النقل، التحميل، التخليص، الجمارك. توزع على الأصناف." |
| مرتجع شراء | "مرتجع الشراء هو إعادة بضاعة تم شراؤها إلى المورد. يقلل من رصيد المخزون والمبلغ المستحق للمورد." |
| مورد نقدي | "المورد النقدي هو مورد افتراضي يستخدم عند الشراء نقداً بدون تحديد مورد حقيقي." |

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Controls/InfoTooltip.xaml` + `.cs` | ✅ Already exists (from Phase 18) — no changes needed |
| `DesktopPWF/Views/Purchases/PurchaseInvoiceEditorView.xaml` | Add `ⓘ` tooltips next to section headers: فاتورة شراء, تكلفة البضاعة, رسوم إضافية, مرتجع شراء |
| `DesktopPWF/Views/Purchases/PurchaseOrderEditorView.xaml` | Add `ⓘ` tooltips next to: أمر شراء, Batch/باتش, FIFO |
| `DesktopPWF/Views/Purchases/PurchaseReturnEditorView.xaml` | Add `ⓘ` tooltip next to: مرتجع شراء |
| `DesktopPWF/Views/Purchases/PurchaseInvoicesListView.xaml` | Add `ⓘ` tooltips next to column headers and filter labels |

**XAML usage pattern**:
```xml
<!-- In header or next to label -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="فاتورة شراء" Style="{StaticResource LabelStyle}"/>
    <controls:InfoTooltip HelpText="فاتورة الشراء هي مستند يثبت قيام الشركة بشراء بضاعة أو خدمات من المورد."/>
</StackPanel>
```

**Estimate**: ~1 hour

---

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | All purchase monetary fields (SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, etc.) | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | PurchaseInvoiceLine.Quantity, PurchaseOrderItem.Quantity, PurchaseReturnItem.Quantity | ✅ |
| **RULE-003** | Multi-table ops in transaction | PurchaseService.CreateAsync, PostAsync, CancelAsync — already wrapped in BeginTransactionAsync | ✅ |
| **RULE-004** | NO hard delete for invoices | Invoice lifecycle: Draft → Posted → Cancelled only | ✅ |
| **RULE-005** | Stock updated AFTER invoice saved | PurchaseService.PostAsync: SaveChanges → IncreaseStockAsync | ✅ |
| **RULE-006** | ALL services return `Result<T>` | PurchaseService, PurchaseOrderService, PurchaseReturnService, AdditionalFeeService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | FeeName, Notes, AttachmentPath, SupplierInvoiceNo | ✅ |
| **RULE-016** | BaseEntity audit fields | All entities inherit BaseEntity (CreatedAt, CreatedByUserId, IsActive) | ✅ |
| **RULE-019** | InvoiceStatus: Draft=1, Posted=2, Cancelled=3 | PurchaseInvoice, PurchaseReturn status fields | ✅ |
| **RULE-020** | Stock/balance affected ONLY when Posted | Stock increase + supplier balance update only in PostAsync | ✅ |
| **RULE-021** | NO editing Posted invoices | PurchaseService.UpdateAsync checks Status == Draft | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | PurchaseService, PurchaseOrderService, PurchaseReturnService, AdditionalFeeService | ✅ |
| **RULE-035** | Serilog for logging | All services Log.Information on CRUD + post + cancel | ✅ |
| **RULE-036** | Log critical operations | Invoice creation, post, cancel, stock changes, journal entries, payments | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | PurchaseInvoicesController, PurchaseOrdersController, PurchaseReturnsController | ✅ |
| **RULE-039** | BCrypt passwords | N/A (no auth changes in this phase) | ✅ N/A |
| **RULE-042** | Rich Domain — `private set` + domain methods | All entities use private set + factory methods + behavior methods | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreatePurchaseInvoiceRequest, CreatePurchaseOrderRequest, CreateAdditionalFeeRequest validators | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Purchase entities use Status=Cancelled (not DELETE) | ✅ |
| **RULE-052** | Guard Clauses on all entities | All entity Create() methods guard against invalid state | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "المنتج مطلوب", "الكمية يجب أن تكون أكبر من الصفر" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | PurchaseInvoiceEditorViewModel, PurchaseOrderEditorViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | All editor ViewModels — no CanExecute blocking | ✅ |
| **RULE-060** | Dynamic UOM — ProductUnit stores conversion | PurchaseInvoiceLine.ProductUnitId links to unit | ✅ |
| **RULE-065** | Pricing stored per ProductUnit | Per-unit cost recorded via ProductUnitId | ✅ |
| **RULE-071** | WeightedAverage formula | PurchaseService.PostAsync → UpdateProductPricingService | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All desktop ViewModels use ExecuteAsync | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | PurchaseOrderEditor opens via OpenScreen() | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ فاتورة المشتريات"`, `"خطأ في حفظ أمر الشراء"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, file I/O | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation, business rules, not found | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | All API services check content-type before JSON parse | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إضافة رسم إضافي (نقل، جمارك، تخليص)" ✅, not "إضافة رسم" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل فاتورة المشتريات — سيتم تحديث المخزون والتكلفة ورصيد المورد" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "أوامر الشراء — إنشاء وإدارة طلبات الشراء" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إنشاء أول أمر شراء — طلب شراء بضاعة من المورد" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException | No hard deletes in this phase (only status changes) | ✅ N/A |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | PurchaseService, PurchaseOrderService, PurchaseReturnService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | All controllers inject services only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | All FK configurations use Restrict (currency, fee, product) | ✅ |
| **RULE-220** | Newest-first sorting on lists | PurchaseInvoiceList: OrderByDescending(InvoiceDate), PurchaseOrderList: OrderByDescending(Id) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | PurchaseInvoiceEditorVM, PurchaseOrderEditorVM | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor ViewModels use AddError/ClearErrors | ✅ |
| **RULE-229** | ValidateAsync() uses ClearAllErrors + AddError + ValidateAllAsync | All editor ViewModels | ✅ |
| **RULE-244** | User hard-delete guarded | N/A (no user changes in this phase) | ✅ N/A |
| **RULE-254** | InvoiceNo is int (not string) | PurchaseInvoice.InvoiceNo, PurchaseOrder.OrderNo = int | ✅ |
| **RULE-258** | SupplierInvoiceNo (string?) is supplier reference only | `SupplierInvoiceNo` on PurchaseInvoice — max 50 chars validated | ✅ |
| **RULE-261** | UNIQUE index on InvoiceNo | Duplicate InvoiceNos NOT allowed | ✅ |
| **RULE-254** | InvoiceNo is int (not string) | PurchaseInvoice.InvoiceNo = int | ✅ |
| **RULE-262** | No hardcoded Height=36/40 on buttons | All new XAML uses style defaults (28px) | ✅ |
| **RULE-263** | No hardcoded Padding=16+ on buttons | All new XAML uses style defaults (10,4) | ✅ |
| **RULE-264** | Header/Footer compact padding | Header: 12,6 / Footer: 12,8 | ✅ |
| **RULE-265** | Compact section margins | Margin="0,0,0,6" between fields | ✅ |
| **RULE-266** | Dialog title FontSize=16 | All dialog titles | ✅ |
| **RULE-267** | Section header FontSize=14 | All section headers | ✅ |
| **RULE-268** | Empty-state compact margins | Margin="0,12,0,0" Width="140" | ✅ |

---

## 10. Risks & Mitigations

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|------------|--------|------------|
| 1 | **Phase 20 (Currencies) not complete** → cannot add CurrencyId FK | High | Blocking | Implement Phase 20 before Phase 27. If not possible, add FK as nullable with default base currency logic |
| 2 | **Phase 22 (Accounting Foundation) not complete** → auto journal entry blocked | High | Medium | Defer auto-journal entry to Phase 30+. Post still works without it — just log a warning |
| 3 | **Fee distribution algorithm wrong** → incorrect product costing | Medium | High | Unit test ALL distribution scenarios with known inputs/outputs. Support both ByCost and ByQuantity |
| 4 | **Performance: large invoice (500+ items)** with fee distribution and journal entry creation | Low | Medium | All operations inside single transaction. Test with 1000 items. Batch allocations in memory |
| 5 | **Migration conflicts** — multiple developers adding FK columns to PurchaseInvoice table | Medium | Medium | Coordinate migration naming: `20260627_AddCurrencyToPurchaseInvoices`, `20260627_AddProductUnitIdToPurchaseItems` |
| 6 | **Purchase Order scope creep** — users want approval workflow in V1 | Medium | Low | Clearly document "Approval Workflow deferred to V2" (Section 7). V1 PO = simple Draft/Approved/Cancelled |
| 7 | **Attachment storage security** — users upload sensitive invoices | Low | Medium | Store in `%ProgramData%\SalesSystem\PurchaseAttachments\` with ACL restricted to app user. No direct URL access |
| 8 | **ExchangeRate precision** — 6 decimal places may not be enough for some currencies | Low | Low | Exchange rate uses `decimal(18,6)` — not money, not quantity, so RULE-001/002 do not apply. All cost conversions from foreign to base currency round to `decimal(18,2)` |
| 9 | **SupplierPayment auto-creation duplicates CashBox transaction** | Medium | Medium | Ensure `SupplierPayment` record is separate from `CashTransaction`. The CashBox transaction records cash movement; SupplierPayment records accounts payable settlement |
| 10 | **Line-level percentage discount with fee allocation** — complex math | Medium | Medium | Compute: LineTotal = (Qty × UnitCost) - lineDiscount. Then allocate fees on computed LineTotal. Document calculation order clearly |
| 11 | **Purchase Return without original invoice** — no FIFO batch to return to | Medium | Medium | Use `LinkToInvoice = false` (standalone mode). Items selected manually. Stock return uses current AvgCost (not batch-specific). Log warning: "Standalone return — no original batch found, using AvgCost" |
| 12 | **Migration rollback complexity** — 6+ new tables/columns | Low | High | Script all rollback commands in Section 11. Test rollback before deployment |

---

## 11. Rollback Plan

All SQL commands are ORDERED by dependency (child tables first).

### 11.1 Rollback All Phase 27 Changes

```sql
-- ═══════════════════════════════════════════
-- Phase 27 Rollback Script
-- Run in a SINGLE transaction
-- ═══════════════════════════════════════════
BEGIN TRANSACTION;

-- 1. Drop NEW tables (child first)
DROP TABLE IF EXISTS [dbo].[AdditionalFeeAllocations];
DROP TABLE IF EXISTS [dbo].[AdditionalFees];
DROP TABLE IF EXISTS [dbo].[PurchaseOrderItems];
DROP TABLE IF EXISTS [dbo].[PurchaseOrders];

-- 2. Drop NEW columns from PurchaseReturn tables
DROP TABLE IF EXISTS [dbo].[PurchaseReturnItems_backup];
-- (If rollback needed: restore from backup)

-- 3. Remove NEW columns from PurchaseInvoice
ALTER TABLE [dbo].[PurchaseInvoices] DROP CONSTRAINT IF EXISTS [FK_PurchaseInvoices_Currencies_CurrencyId];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [CurrencyId];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [ExchangeRate];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [CostInBaseCurrency];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [AdditionalFeesTotal];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [AttachmentPath];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [DiscountType];
ALTER TABLE [dbo].[PurchaseInvoices] DROP COLUMN IF EXISTS [DiscountRate];

-- 4. Remove NEW columns from PurchaseInvoiceLines
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP CONSTRAINT IF EXISTS [FK_PurchaseInvoiceLines_ProductUnits_ProductUnitId];
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP COLUMN IF EXISTS [ProductUnitId];
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP COLUMN IF EXISTS [CostInBaseCurrency];
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP COLUMN IF EXISTS [DiscountType];
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP COLUMN IF EXISTS [DiscountRate];
ALTER TABLE [dbo].[PurchaseInvoiceLines] DROP COLUMN IF EXISTS [AdditionalFeesAmount];

-- 5. Remove NEW columns from PurchaseReturns
ALTER TABLE [dbo].[PurchaseReturns] DROP COLUMN IF EXISTS [CurrencyId];
ALTER TABLE [dbo].[PurchaseReturns] DROP COLUMN IF EXISTS [ExchangeRate];
ALTER TABLE [dbo].[PurchaseReturns] DROP COLUMN IF EXISTS [DiscountAmount];
ALTER TABLE [dbo].[PurchaseReturns] DROP COLUMN IF EXISTS [DiscountType];
ALTER TABLE [dbo].[PurchaseReturns] DROP COLUMN IF EXISTS [DiscountRate];

-- 6. Remove NEW columns from PurchaseReturnItems
ALTER TABLE [dbo].[PurchaseReturnItems] DROP COLUMN IF EXISTS [ProductUnitId];
ALTER TABLE [dbo].[PurchaseReturnItems] DROP COLUMN IF EXISTS [CostInBaseCurrency];

-- 7. Drop NEW enums tables if they were created separately
-- (Enums are typically code-only, no table drop needed)

COMMIT TRANSACTION;
GO
```

### 11.2 Post-Rollback Verification

Verify with these queries:

```sql
-- Verify columns removed
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PurchaseInvoices' 
  AND COLUMN_NAME IN ('CurrencyId', 'ExchangeRate', 'CostInBaseCurrency', 
                      'AttachmentPath', 'DiscountType', 'DiscountRate');
-- Expected: empty result set

SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PurchaseOrders';
-- Expected: empty result set (table dropped)

-- Verify FK count back to original
SELECT COUNT(*) FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('PurchaseInvoices');
-- Expected: original count (before Phase 27 additions)
```

### 11.3 Code Rollback

1. Revert all entity changes in `SalesSystem.Domain/Entities/`
2. Revert all configuration changes in `Infrastructure/Data/Configurations/`
3. Delete new files: `PurchaseOrder.cs`, `PurchaseOrderService.cs`, `AdditionalFee.cs`, etc.
4. Revert `PurchaseService.cs` to previous version
5. Revert `PurchaseInvoiceEditorViewModel.cs` to previous version
6. Delete `PurchaseOrdersController.cs`
7. Remove navigation entries from `App.xaml.cs`
8. Remove DI registrations for new services
9. Update all DTOs and Requests to remove new fields
10. Revert migration to previous version: `dotnet ef migrations remove`

### 11.4 Data Preservation (Before Migration)

Always take a backup before applying Phase 27 migration:

```sql
BACKUP DATABASE [SalesSystemDb] 
TO DISK = N'C:\Backup\SalesSystemDb_Phase27_PreMigration.bak'
WITH INIT, NAME = N'Phase27-PreMigration-FullBackup';
```
