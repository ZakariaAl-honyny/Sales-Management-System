# Phase 26 — Warehouses & Inventory Module Implementation Plan

> **Version**: 2.0 — Rewritten for new schema (Module 5: Inventory + Module 2.2: Warehouses)
> **Scope**: 11 tables — Warehouses, WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, InventoryCounts, InventoryCountLines, InventoryAdjustments, InventoryAdjustmentLines, WarehouseTransfers, WarehouseTransferLines
> **Enums**: InventoryTransactionType (12 values), InventoryReferenceType (7 values), AdjustmentType (4 values)

---

## 1. Summary

Implement the complete Warehouses & Inventory Module covering 11 database tables: Warehouse CRUD (per branch), live stock balances (WarehouseStocks), batch tracking with FIFO/FEFO (InventoryBatches), comprehensive inventory transactions (InventoryTransactions/InventoryTransactionLines), physical counts (InventoryCounts/InventoryCountLines), stock adjustments for damage/shortage/opening (InventoryAdjustments/InventoryAdjustmentLines), and inter-warehouse transfers (WarehouseTransfers/WarehouseTransferLines).

Key structural changes from v1: old `InventoryMovements` replaced by `InventoryTransactions` with 12 transaction types; old `StockTransfers` renamed to `WarehouseTransfers`; old `StockWriteOffs` merged into `InventoryAdjustments` with `Damage` type. Physical Count deferred to V2 (entity structure exists, no UI or posting logic).

---

## 2. Key Entities

### 2.1 Warehouses (`smallint` PK — ActivatableEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | smallint PK | Max 32,767 warehouses |
| BranchId | smallint FK → Branches(Id) | Every warehouse belongs to a branch |
| Name | nvarchar(150) NOT NULL | Unique filtered `[IsActive]=1` |
| Phone | nvarchar(30) NULL | |
| Address | nvarchar(300) NULL | |
| Notes | nvarchar(500) NULL | |
| IsActive | bit NOT NULL default 1 | Soft delete via global filter |

### 2.2 WarehouseStocks (`int` PK — AuditableEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| WarehouseId | smallint FK → Warehouses(Id) | |
| ProductId | int FK → Products(Id) | |
| Quantity | decimal(18,3) NOT NULL default 0 | CHECK `>= 0` |
| **UK** | UNIQUE(WarehouseId, ProductId) | One row per product per warehouse |

**Notes**: NO IsActive — live balance tracking only. Inherits `AuditableEntity` (CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId). Stock mutations happen via service methods only: `IncreaseStock()`, `DecreaseStock()`, `SetQuantity()`.

### 2.3 InventoryBatches (`int` PK — AuditableEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| BatchNo | int NOT NULL | Internal sequential batch number |
| ProductId | int FK → Products(Id) | |
| WarehouseId | smallint FK → Warehouses(Id) | |
| PurchaseInvoiceId | int NULL FK → PurchaseInvoices(Id) | Source purchase invoice |
| SupplierBatchNo | varchar(100) NULL | Supplier's batch reference |
| ExpiryDate | date NULL | For FEFO tracking |
| QuantityReceived | decimal(18,3) NOT NULL | CHECK `>= 0` |
| QuantityRemaining | decimal(18,3) NOT NULL | CHECK `>= 0` |
| UnitCost | decimal(18,2) NOT NULL | CHECK `>= 0` |
| **Indexes** | (ProductId, WarehouseId), (ExpiryDate) filtered, (PurchaseInvoiceId) | |

**Notes**: NO IsActive. Opening-balance batches use `BatchNo = 0` with `PurchaseInvoiceId = NULL`. For non-expiry products, `ExpiryDate` is NULL. For FEFO products (`Product.TrackExpiry = true`), consumption order is nearest expiry first.

### 2.4 InventoryTransactions (`int` PK — DocumentEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| TransactionNo | int NOT NULL | Unique, auto-generated |
| TransactionType | tinyint NOT NULL | 1–12 (see Enums) |
| WarehouseId | smallint FK → Warehouses(Id) | |
| ReferenceType | tinyint NULL | 1–7 (see Enums) |
| ReferenceId | int NULL | FK to source document |
| TransactionDate | date NOT NULL | |
| Notes | nvarchar(500) NULL | |
| Status | tinyint NOT NULL | 1=Draft, 2=Posted, 3=Cancelled |
| **Indexes** | (WarehouseId, TransactionDate DESC), (ReferenceType, ReferenceId) | |

**Notes**: Acts as the unified audit log for ALL inventory movements. Posted transactions are immutable — cancellation creates reversal transactions. TransactionType distinguishes direction: Purchase/SaleReturn/TransferIn/Adjustment/OpeningBalance/InternalReceipt = INCREASE stock; PurchaseReturn/Sale/TransferOut/Damage/CountShortage/InternalIssue = DECREASE stock.

### 2.5 InventoryTransactionLines (`int` PK — Entity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| InventoryTransactionId | int FK → InventoryTransactions(Id) | |
| ProductId | int FK → Products(Id) | |
| ProductUnitId | int FK → ProductUnits(Id) | Unit used in the transaction |
| BatchId | int NULL FK → InventoryBatches(Id) | NULL for non-batched/non-expiry products |
| Quantity | decimal(18,3) NOT NULL | Positive only (direction determined by transaction type) |
| UnitCost | decimal(18,2) NOT NULL | Cost at time of transaction |
| TotalCost | decimal(18,2) NOT NULL | Quantity × UnitCost |

### 2.6 InventoryCounts (`int` PK — DocumentEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| CountNo | int NOT NULL | Unique, auto-generated |
| WarehouseId | smallint FK → Warehouses(Id) | |
| CountDate | date NOT NULL | |
| Notes | nvarchar(500) NULL | |
| Status | tinyint NOT NULL | 1=Draft, 2=Posted, 3=Cancelled |

### 2.7 InventoryCountLines (`int` PK — Entity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| InventoryCountId | int FK → InventoryCounts(Id) | |
| ProductId | int FK → Products(Id) | |
| BatchId | int FK → InventoryBatches(Id) | Links count to specific batch |
| SystemQuantity | decimal(18,3) NOT NULL | Expected quantity from system |
| ActualQuantity | decimal(18,3) NOT NULL | Counted quantity (manual entry) |
| DifferenceQuantity | decimal(18,3) NOT NULL | Actual - System |

### 2.8 InventoryAdjustments (`int` PK — DocumentEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| AdjustmentNo | int NOT NULL | Unique, auto-generated |
| WarehouseId | smallint FK → Warehouses(Id) | |
| AdjustmentType | tinyint NOT NULL | 1=Opening, 2=Increase, 3=Shortage, 4=Damage |
| AdjustmentDate | date NOT NULL | |
| Notes | nvarchar(500) NULL | |
| Status | tinyint NOT NULL | 1=Draft, 2=Posted, 3=Cancelled |

### 2.9 InventoryAdjustmentLines (`int` PK — Entity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| InventoryAdjustmentId | int FK → InventoryAdjustments(Id) | |
| ProductId | int FK → Products(Id) | |
| BatchId | int NULL FK → InventoryBatches(Id) | NULL for Opening adjustments |
| Quantity | decimal(18,3) NOT NULL | Positive only |
| UnitCost | decimal(18,2) NOT NULL | |
| TotalCost | decimal(18,2) NOT NULL | |

### 2.10 WarehouseTransfers (`int` PK — DocumentEntity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| TransferNo | int NOT NULL | Unique, auto-generated |
| FromWarehouseId | smallint FK → Warehouses(Id) | Source warehouse |
| ToWarehouseId | smallint FK → Warehouses(Id) | Destination warehouse |
| TransferDate | date NOT NULL | |
| Notes | nvarchar(500) NULL | |
| Status | tinyint NOT NULL | 1=Draft, 2=Posted, 3=Cancelled |

**Notes**: MUST guard against `FromWarehouseId == ToWarehouseId` — throw `DomainException`. On posting, creates TWO InventoryTransaction entries: TransferOut (decrease FromWarehouse stock) and TransferIn (increase ToWarehouse stock), both linked via `ReferenceId` to the transfer document.

### 2.11 WarehouseTransferLines (`int` PK — Entity)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| WarehouseTransferId | int FK → WarehouseTransfers(Id) | |
| ProductId | int FK → Products(Id) | |
| BatchId | int FK → InventoryBatches(Id) | Specific batch transferred |
| Quantity | decimal(18,3) NOT NULL | |
| UnitCost | decimal(18,2) NOT NULL | Cost at time of transfer |
| TotalCost | decimal(18,2) NOT NULL | |

---

## 3. Enums

### InventoryTransactionType (tinyint)
| Value | Name | Direction | Description |
|-------|------|-----------|-------------|
| 1 | Purchase | IN | Stock received from supplier |
| 2 | PurchaseReturn | OUT | Stock returned to supplier |
| 3 | Sale | OUT | Stock sold to customer |
| 4 | SaleReturn | IN | Stock returned by customer |
| 5 | TransferOut | OUT | Stock leaving source warehouse |
| 6 | TransferIn | IN | Stock arriving at destination warehouse |
| 7 | Count | NEUTRAL | Physical count variance (adjustment created separately) |
| 8 | Adjustment | IN/OUT | Manual stock adjustment |
| 9 | Damage | OUT | Damaged/written-off stock |
| 10 | OpeningBalance | IN | Initial stock on product creation |
| 11 | InternalIssue | OUT | Internal use/consumption |
| 12 | InternalReceipt | IN | Internal production/receipt |

### InventoryReferenceType (tinyint)
| Value | Name | Source Table |
|-------|------|-------------|
| 1 | PurchaseInvoice | PurchaseInvoices |
| 2 | SalesInvoice | SalesInvoices |
| 3 | PurchaseReturn | PurchaseReturns |
| 4 | SalesReturn | SalesReturns |
| 5 | Transfer | WarehouseTransfers |
| 6 | Count | InventoryCounts |
| 7 | Adjustment | InventoryAdjustments |

### AdjustmentType (tinyint)
| Value | Name | Description |
|-------|------|-------------|
| 1 | Opening | Opening balance on product creation |
| 2 | Increase | Manual stock increase (correction) |
| 3 | Shortage | Manual stock decrease (loss/correction) |
| 4 | Damage | Damaged/expired/written-off stock |

---

## 4. Business Rules

### 4.1 Warehouse CRUD
- Warehouse names must be unique within the same branch (filtered unique index `[IsActive]=1`).
- A warehouse cannot be hard-deleted if it has any WarehouseStock records with quantity > 0, or any InventoryTransactions referencing it.
- Soft-delete protects active stock: setting `IsActive = false` on a warehouse with positive stock must be blocked by the service.
- At least one warehouse must exist per branch — guard against deleting the last warehouse.

### 4.2 Stock Integrity
- `WarehouseStocks.Quantity` has DB CHECK `>= 0` — EF migration enforces this as a table check constraint.
- ALL stock mutations MUST go through the `IInventoryService` (never direct `DbContext` manipulation of WarehouseStocks).
- Every stock mutation MUST create an `InventoryTransaction` + `InventoryTransactionLine` record for audit trail.
- Stock is deducted AFTER the source document (invoice/transfer/adjustment) is saved and has an ID — wrap in transaction.

### 4.3 Batch Costing (FIFO Default)
- On sale: consume from earliest batch first (oldest `QuantityRemaining > 0`), or nearest expiry first when `Product.TrackExpiry = true` (FEFO).
- On purchase return: debit from the original batch's `QuantityRemaining`.
- On transfer: preserve batch identity — transfer moves the exact batch, not averaged cost.
- On adjustment/damage: consume from oldest batch first (FIFO), or nearest expiry first (FEFO).
- `BatchNo = 0` is reserved for opening balance batches (no PurchaseInvoiceId).

### 4.4 Transaction Lifecycle
- ALL inventory documents follow 3-state lifecycle: Draft (1) → Posted (2) → Cancelled (3).
- Draft status: editable, no stock impact.
- Posted status: stock affected, immutable (no edits allowed).
- Cancelled status: MUST reverse all stock changes via offsetting InventoryTransaction entries (NOT by modifying the original). Cancellation creates reversal entries with opposite direction and `ReferenceId` pointing to the original transaction.
- A Cancelled document can never be re-Posted — terminal state.

### 4.5 Transfer Rules
- `FromWarehouseId` and `ToWarehouseId` must be different — validated in domain entity.
- Both source and destination warehouses must be active (IsActive = true).
- On posting: creates TWO InventoryTransactions — TransferOut (source, decrease) and TransferIn (destination, increase). Both reference the same WarehouseTransfer document via `ReferenceId`.
- On cancellation: reverses BOTH transactions (TransferIn reversed as TransferOut and vice versa).
- NO accounting entries are created for transfers — inventory moves between warehouses within the same entity; the balance sheet is unaffected.

### 4.6 Adjustment Rules
- Opening adjustments (`AdjustmentType = 1`) create new `InventoryBatch` with `BatchNo = 0` and link to the product's opening entry.
- Damage adjustments (`AdjustmentType = 4`) decrement stock from the oldest batch first.
- Shortage adjustments (`AdjustmentType = 3`) decrement stock.
- Increase adjustments (`AdjustmentType = 2`) increment stock and require `BatchId` for batch-tracked products.
- Posted adjustments CREATE accounting entries via `AccountingIntegrationService` — Dr/Mr depending on adjustment type (Increase: Dr Inventory, Cr Gain; Shortage/Damage: Dr Loss, Cr Inventory).

### 4.7 Physical Count (Deferred to V2)
- Entity schema is fully defined (`InventoryCounts` + `InventoryCountLines`) and migrated.
- CRUD for count documents is implemented (Draft/Posted/Cancelled).
- **V1 scope**: Count documents can be created, lines entered (system vs actual), and posted. Posted counts create an automatic `InventoryAdjustment` document with `AdjustmentType = Shortage` or `Increase` to align actual stock to counted stock.
- **V2 scope**: Dedicated count UI with barcode scanning, batch-level freeze, approval workflow, and count report.

### 4.8 FK Delete Behavior
- ALL foreign keys use `DeleteBehavior.Restrict` — NO cascade delete.
- This means: cannot delete a Product that has WarehouseStock rows, cannot delete a Warehouse with existing transactions, etc.

---

## 5. Design Decisions

### 5.1 InventoryMovements → InventoryTransactions
The old `InventoryMovements` table is replaced by `InventoryTransactions` + `InventoryTransactionLines`. This provides a proper document-lifecycle (Draft/Posted/Cancelled) for inventory operations, line-item detail with batch and cost tracking, and a unified audit trail for all stock movements. The 12-value `TransactionType` enum replaces the old `MovementType` (7 values) — adding OpeningBalance, InternalIssue, InternalReceipt for completeness.

### 5.2 No StockTransfers / StockWriteOffs
- Old `StockTransfers` entity renamed to `WarehouseTransfers` with improved schema (from/to warehouse, batch-level lines, document lifecycle).
- Old `StockWriteOffs` merged into `InventoryAdjustments` with `AdjustmentType = Damage` — no separate entity needed.

### 5.3 Transfers Do NOT Create Accounting Entries
This is a deliberate decision: transferring stock between warehouses within the same legal entity does not affect the balance sheet. Only adjustments (increase, shortage, damage, opening) create journal entries because they represent a change in total inventory asset value.

### 5.4 WarehouseStock — Live Balance with No Soft Delete
WarehouseStocks inherits `AuditableEntity` (not `ActivatableEntity`) — there is no `IsActive` column. This table represents the live, current quantity of each product in each warehouse. Soft-deleting a stock row would destroy audit trail. If a product should no longer be stocked in a warehouse, its quantity is adjusted to zero and the row remains (with quantity = 0).

### 5.5 Batch Tracking Strategy
- Every batch-tracked transaction line references `InventoryBatches` via `BatchId` (nullable FK — NULL for non-batched products).
- Opening balance gets `BatchNo = 0` with no PurchaseInvoiceId.
- Non-expiry products leave `ExpiryDate = NULL` on the batch.
- `SupplierBatchNo` is stored for supplier reference and returns processing.

### 5.6 Document Numbering
All 5 document types (InventoryTransactions, InventoryCounts, InventoryAdjustments, WarehouseTransfers) use `IDocumentSequenceService.GetNextIntAsync(key)` for their `TransactionNo`/`CountNo`/`AdjustmentNo`/`TransferNo` — thread-safe via `SemaphoreSlim`. Separate sequence keys:
- `"InventoryTransaction"`
- `"InventoryCount"`
- `"InventoryAdjustment"`
- `"WarehouseTransfer"`

### 5.7 FK Design: smallint → int Chain
- Warehouses use `smallint PK` (max 32,767) for compact storage.
- All FKs referencing Warehouses (`WarehouseId`) are `smallint` — including in `WarehouseStocks`, `InventoryBatches`, `InventoryTransactions`, `InventoryCounts`, `InventoryAdjustments`, `WarehouseTransfers`.
- All document entities use `int PK` for sufficient range.

---

## 6. Implementation Tasks

### Task Group 1: Domain Layer
- **1.1** Create `Warehouse` entity — ActivatableEntity, smallint PK, BranchId FK, Name, Phone, Address, Notes. Guard clauses: `Create()` validates Name not empty, BranchId > 0.
- **1.2** Create `WarehouseStock` entity — int PK, WarehouseId+ProductId UK, Quantity with Guard (>= 0). Methods: `IncreaseStock()`, `DecreaseStock()`, `SetQuantity()`.
- **1.3** Create `InventoryBatch` entity — BatchNo (int), ProductId, WarehouseId, PurchaseInvoiceId (nullable), SupplierBatchNo, ExpiryDate, QuantityReceived/Remaining, UnitCost. Guards: Quantity >= 0, UnitCost >= 0.
- **1.4** Create `InventoryTransaction` entity — DocumentEntity with TransactionType, WarehouseId, ReferenceType/ReferenceId, TransactionDate.
- **1.5** Create `InventoryTransactionLine` entity — InventoryTransactionId FK, ProductId, ProductUnitId, BatchId (nullable), Quantity, UnitCost, TotalCost.
- **1.6** Create `InventoryCount` entity — DocumentEntity with CountNo, WarehouseId, CountDate.
- **1.7** Create `InventoryCountLine` entity — InventoryCountId FK, ProductId, BatchId, SystemQuantity, ActualQuantity, DifferenceQuantity.
- **1.8** Create `InventoryAdjustment` entity — DocumentEntity with AdjustmentNo, WarehouseId, AdjustmentType, AdjustmentDate.
- **1.9** Create `InventoryAdjustmentLine` entity — InventoryAdjustmentId FK, ProductId, BatchId (nullable), Quantity, UnitCost, TotalCost.
- **1.10** Create `WarehouseTransfer` entity — DocumentEntity with TransferNo, FromWarehouseId, ToWarehouseId. Guard: From != To.
- **1.11** Create `WarehouseTransferLine` entity — WarehouseTransferId FK, ProductId, BatchId, Quantity, UnitCost, TotalCost.
- **1.12** Create/update enums: `InventoryTransactionType`, `InventoryReferenceType`, `AdjustmentType`.
- **1.13** Create all Domain entity unit tests.

### Task Group 2: Infrastructure Layer
- **2.1** Add EF Core Fluent API configurations for all 11 entities:
  - `WarehouseConfiguration` — smallint PK, UK(Name,BranchId) filtered by IsActive, BranchId FK Restrict.
  - `WarehouseStockConfiguration` — UK(WarehouseId, ProductId), CHK Quantity >= 0, both FKs Restrict.
  - `InventoryBatchConfiguration` — indexes on (ProductId, WarehouseId), (ExpiryDate), (PurchaseInvoiceId), CHK Quantity >= 0, CHK UnitCost >= 0. All FKs Restrict.
  - `InventoryTransactionConfiguration` — UK(TransactionNo), indexes on (WarehouseId, TransactionDate DESC), (ReferenceType, ReferenceId). All FKs Restrict.
  - `InventoryTransactionLineConfiguration` — FK to InventoryTransaction Restrict, FK to Product Restrict, FK to ProductUnit Restrict, FK to Batch Restrict (nullable).
  - `InventoryCountConfiguration` — UK(CountNo), FK to Warehouse Restrict.
  - `InventoryCountLineConfiguration` — FK to InventoryCount Restrict, FK to Product Restrict, FK to Batch Restrict.
  - `InventoryAdjustmentConfiguration` — UK(AdjustmentNo), FK to Warehouse Restrict.
  - `InventoryAdjustmentLineConfiguration` — FK to InventoryAdjustment Restrict, FK to Product Restrict, FK to Batch Restrict (nullable).
  - `WarehouseTransferConfiguration` — UK(TransferNo), FKs to FromWarehouse/ToWarehouse both Restrict.
  - `WarehouseTransferLineConfiguration` — FK to WarehouseTransfer Restrict, FK to Product Restrict, FK to Batch Restrict.
- **2.2** Add `DbSet<>` properties to `SalesDbContext` for all 11 entities.
- **2.3** Create `IWarehouseRepository`, `IWarehouseStockRepository`, `IInventoryBatchRepository` + implementations.
- **2.4** Create `IInventoryTransactionRepository`, `IInventoryCountRepository`, `IInventoryAdjustmentRepository`, `IWarehouseTransferRepository`.
- **2.5** Create `IUnitOfWork` methods/properties for all new repositories.
- **2.6** Create EF Core migration for all 11 tables.

### Task Group 3: Application Layer
- **3.1** Create `IWarehouseService` / `WarehouseService` — CRUD + soft delete (check stock before deactivation).
- **3.2** Create `IInventoryService` / `InventoryService` — core stock operations:
  - `IncreaseStock(WarehouseId, ProductId, quantity, batchId, unitCost)`
  - `DecreaseStock(WarehouseId, ProductId, quantity, batchId?)`
  - `TransferStock(fromWarehouseId, toWarehouseId, ProductId, batchId, quantity)`
  - `AdjustStock(WarehouseId, ProductId, batchId, adjustmentType, quantity, unitCost)`
  - All methods: validate stock availability BEFORE transaction, create InventoryTransaction + lines, return `Result<int>`.
- **3.3** Create `IInventoryBatchService` / `InventoryBatchService`:
  - `CreateBatch(productId, warehouseId, purchaseInvoiceId?, supplierBatchNo?, expiryDate?, quantity, unitCost)`
  - `ConsumeFromBatch(productId, warehouseId, quantity)` — FIFO/FEFO consumption logic
  - `GetAvailableBatches(productId, warehouseId)` — for transfer/adjustment UI
- **3.4** Create `IInventoryCountService` / `InventoryCountService` — CRUD + posting logic that auto-creates InventoryAdjustment for variances.
- **3.5** Create `IInventoryAdjustmentService` / `InventoryAdjustmentService` — CRUD + posting that calls `AccountingIntegrationService` for journal entries.
- **3.6** Create `IWarehouseTransferService` / `WarehouseTransferService` — CRUD + posting that creates TransferOut + TransferIn InventoryTransactions.
- **3.7** Create `InventoryTransactionService` — query methods for transaction history by product/warehouse/date range.
- **3.8** Create Application layer unit tests for all 6 services.

### Task Group 4: API Layer
- **4.1** Create `WarehousesController` — `/api/v1/warehouses` — CRUD with AllStaff policy (read)/ManagerAndAbove (write). 404 vs 400 differentiation.
- **4.2** Create `StockController` — `/api/v1/stock` — query stock by warehouse/product, low-stock alerts.
- **4.3** Create `StockTransactionsController` — `/api/v1/stock/transactions` — history queries.
- **4.4** Create `InventoryBatchesController` — `/api/v1/inventory-batches` — query by product/warehouse, get available batches.
- **4.5** Create `InventoryCountsController` — `/api/v1/inventory-counts` — CRUD + post + cancel.
- **4.6** Create `InventoryAdjustmentsController` — `/api/v1/inventory-adjustments` — CRUD + post + cancel.
- **4.7** Create `WarehouseTransfersController` — `/api/v1/warehouse-transfers` — CRUD + post + cancel.
- **4.8** Create FluentValidators for all request DTOs (6 types).
- **4.9** Create all request/response DTOs in `SalesSystem.Contracts`.

### Task Group 5: Desktop Layer
- **5.1** Create `WarehousesListViewModel` + `WarehousesListView` — DataGrid with search by name, soft delete.
- **5.2** Create `WarehouseEditorViewModel` + `WarehouseEditorView` — INotifyDataErrorInfo, Branch dropdown, Name/Phone/Address/Notes fields.
- **5.3** Create `StockStatusViewModel` + `StockStatusView` — per-warehouse stock grid with search/filter, low-stock highlight.
- **5.4** Create `StockTransactionsViewModel` + `StockTransactionsView` — transaction history by product/warehouse/date with filtering.
- **5.5** Create `InventoryBatchesViewModel` + `InventoryBatchesView` — batch list by product/warehouse, expiry tracking.
- **5.6** Create `InventoryCountsListViewModel` + `InventoryCountsListView` + `InventoryCountEditorViewModel` + `InventoryCountEditorView` — count document CRUD with line entry.
- **5.7** Create `InventoryAdjustmentsListViewModel` + `InventoryAdjustmentsListView` + `InventoryAdjustmentEditorViewModel` + `InventoryAdjustmentEditorView` — adjustment document CRUD with type selection.
- **5.8** Create `WarehouseTransfersListViewModel` + `WarehouseTransfersListView` + `WarehouseTransferEditorViewModel` + `WarehouseTransferEditorView` — transfer document CRUD with from/to warehouse selection.
- **5.9** Create API service interfaces + implementations for all new endpoints (IWarehouseApiService, IStockApiService, IBatchApiService, etc.).
- **5.10** Create `InventoryChangedMessage` EventBus message for cross-module refresh.
- **5.11** Register all ViewModels, Views, API services in Desktop DI.
- **5.12** Add navigation entries in MainWindow sidebar for all inventory screens.

### Task Group 6: Integration & Seeder
- **6.1** Update `DbSeeder` to seed default warehouse.
- **6.2** Update `DocumentSequenceService` to register 4 new document sequence keys.
- **6.3** Wire `InventoryService` into existing Sales/Purchase posting services — on SalesInvoice posting, call `DecreaseStock`; on PurchaseInvoice posting, call `IncreaseStock` + `CreateBatch`.
- **6.4** Wire `InventoryService` into SalesReturn/PurchaseReturn services.
- **6.5** Integration tests covering: stock increase/decrease, batch consumption, transfer flow, adjustment posting with journal entries.

---

## 7. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Performance of FIFO batch consumption on high-volume sales | Medium | Index on (ProductId, WarehouseId, ExpiryDate, QuantityRemaining). Consider limiting batch search to active batches only (QuantityRemaining > 0). |
| Transaction deadlocks during concurrent stock operations | High | Use `SqlServerRetryingExecutionStrategy` (already configured). Keep transaction scope minimal. Validate stock BEFORE opening transaction. |
| Migration complexity from old schema (InventoryMovements → InventoryTransactions) | Medium | Full schema rewrite — old data cannot be migrated. Document clear data migration script or accept fresh start for inventory tables. |
| Physical Count not in V1 may cause user confusion | Low | Schema exists; UI is deferred. Clearly document as "available in V2" in release notes. Count functionality without posting creates no value. |
| Accounting entries for adjustments require AccountingIntegrationService not yet fully tested | Medium | Ensure AccountingIntegrationService has unit tests for adjustment scenarios before integration. Use `Result<int>` pattern — never throw. |
| 11 tables × 6 layers = high implementation volume | High | Prioritize: Warehouses + Stock → Batches → Transactions → Transfers → Adjustments → Counts (last). Use reusable patterns (document lifecycle, service base classes) to reduce boilerplate. |

---

## 8. Cross-Module Dependencies

| Depends On | Module | Type |
|------------|--------|------|
| Branches | Phase 19 (Settings) | FK — Warehouses.BranchId |
| Products | Phase 25/28 (Products) | FK — WarehouseStocks, Batches, all line entities |
| ProductUnits | Phase 25/28 (Products) | FK — InventoryTransactionLines |
| PurchaseInvoices | Phase 27 (Purchases) | FK — InventoryBatches.PurchaseInvoiceId |
| AccountingIntegrationService | Phase 24 (Accounting) | Journal entry creation on adjustments |
| DocumentSequenceService | Phase 19 (Settings) | Auto-numbering for 4 document types |
| Users & Permissions | Phase 21 | CreatedByUserId on all entities, policy enforcement |

**Implementation order**: Phase 19 (Branches + DocumentSequence) → Phase 25/28 (Products + Units) → Phase 27 (Purchases — wires stock in) → **Phase 26 (Warehouses)** → Phase 24 (Accounting — adjusts).

---

## 9. Schema Relationships Diagram

```text
Warehouses ──┬── WarehouseStocks (1:N, UK per Product)
             ├── InventoryTransactions (1:N, with DocumentEntity Status)
             ├── InventoryBatches (1:N, per Product+Warehouse)
             ├── InventoryCounts (1:N)
             ├── InventoryAdjustments (1:N)
             ├── WarehouseTransfers.FromWarehouseId (1:N)
             └── WarehouseTransfers.ToWarehouseId (1:N)

InventoryBatches ──┬── InventoryTransactionLines (1:N, nullable FK)
                   ├── InventoryCountLines (1:N)
                   ├── InventoryAdjustmentLines (1:N, nullable FK)
                   └── WarehouseTransferLines (1:N)

Products ──┬── WarehouseStocks (1:N)
           ├── InventoryBatches (1:N)
           ├── InventoryTransactionLines (1:N)
           ├── InventoryCountLines (1:N)
           ├── InventoryAdjustmentLines (1:N)
           └── WarehouseTransferLines (1:N)

InventoryTransactions ──┬── InventoryTransactionLines (1:N)
                        ├── ReferenceType+ReferenceId (polymorphic)
                        └── CancelledBy transaction (self-ref via ReferenceId)

PurchaseInvoices ──┬── InventoryBatches (1:N)
SalesInvoices ───────┐
PurchaseReturns ─────┤── InventoryTransactions.ReferenceType
SalesReturns ────────┘
```

---

## 10. Success Criteria

- [ ] All 11 tables created in migration with correct FKs, unique indexes, CHECK constraints.
- [ ] Warehouse CRUD works via API and Desktop — soft delete blocks when stock > 0.
- [ ] WarehouseStock quantity never goes negative (DB CHECK + service guard).
- [ ] Every stock change creates an InventoryTransaction + lines (audit trail verified).
- [ ] FIFO batch consumption selects oldest batch first (FEFO = nearest expiry first).
- [ ] Transfer creates TWO transactions (out + in) — no accounting entry.
- [ ] Adjustment posting creates journal entry via AccountingIntegrationService.
- [ ] All document types support Draft → Posted → Cancelled lifecycle with stock reversal.
- [ ] Physical Count entity exists (deferred posting logic — count_create + count_lines CRUD works).
- [ ] Build: 0 errors, 0 warnings across all 6 projects.
- [ ] All existing tests pass.
