# Phase 26 — Warehouses Module: Comprehensive Enhancement & Implementation Plan

> **Version**: 1.0 — Warehouse CRUD Enhancement + Inventory Operations (Issue/Receipt/Transfer/Adjust/Count) + Stock Reports
> **Scope**: Complete 3-sub-module build for V1 — Warehouse CRUD enhancement, Inventory Operations (new), Stock Reports (new)

---

## Table of Contents

1. [Architecture — 3 Sub-Modules](#1-architecture--3-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Warehouse Design Catalog](#4-warehouse-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 3 Sub-Modules

The Warehouses Module is divided into **3 sub-modules**:

| # | Sub-Module | Scope | Status |
|---|------------|-------|--------|
| 🏭 | **Warehouse CRUD (Enhancement)** | Extend existing Warehouse entity with Type, Address, Manager, AccountId. Existing CRUD screens upgraded. | ⚠️ Extends existing |
| 📦 | **Inventory Operations (New)** | Standalone stock in/out/adjust screens. Stock transfer enhancement. **Physical count deferred to V2** (see §7). | ❌ New build |
| 📊 | **Stock Reports (New)** | Stock balance per warehouse, movement history, low stock alerts, inventory valuation. | ❌ New build |

### Data Flow

```
Desktop (WPF) → (HttpClient) → API Controllers → Application Services → Infrastructure → SQL Server
                                                         ↓
                                                  Domain Entities
                                                  (InventoryOperation, WarehouseStock, InventoryMovement,
                                                   StockWriteOff, PhysicalCount)
```

**RULE-007** enforced: Desktop NEVER connects to DB directly. All calls via HTTP.

---

## 2. Full Inventory — What Already Exists

### 2.1 Warehouse Entity ✅ (Exists — Needs Enhancement)

**Domain Entity**: `SalesSystem.Domain.Entities.Warehouse`

| Field | Type | Status |
|-------|------|--------|
| `Id` | `int PK` Auto-Increment | ✅ Exists |
| `Name` | `nvarchar(100)` Required | ✅ Exists |
| `Location` | `nvarchar(250)?` | ✅ Exists |
| `IsDefault` | `bit` | ✅ Exists |
| `IsActive` | `bit` (BaseEntity) | ✅ Exists |
| `CreatedAt` / `CreatedByUserId` | `datetime2` / `int?` | ✅ Exists (BaseEntity) |
| `Type` | `int` (WarehouseType enum) | ❌ **NEW** |
| `Phone` | `nvarchar(20)?` | ❌ **NEW** |
| `Address` | `nvarchar(250)?` | ❌ **NEW** (extends Location) |
| `ManagerName` | `nvarchar(100)?` | ❌ **NEW** |
| `AccountId` | `int?` FK → Account | ❌ **NEW** |

**Existing Configuration**: `WarehouseConfiguration` in `WarehouseStockConfiguration.cs` (file name is misleading — contains both configurations)

**Existing Fluent Validators**: `CreateWarehouseRequestValidator`, `UpdateWarehouseRequestValidator`

### 2.2 Warehouse Service & API ✅ (Exists)

| Component | File | Status |
|-----------|------|--------|
| `IWarehouseService` | `Application/Interfaces/Services/IWarehouseService.cs` | ✅ Exists |
| `WarehouseService` | `Application/Services/WarehouseService.cs` (215 lines) | ✅ Exists — Uses `IUnitOfWork`, returns `Result<T>`, has `PermanentDeleteAsync` with `DbUpdateException` catch |
| `WarehousesController` | `Api/Controllers/WarehousesController.cs` (78 lines) | ✅ Exists — 6 endpoints, `[Authorize(Policy = "AdminOnly")]` |
| `CreateWarehouseRequest` | `Contracts/Requests/WarehouseRequests.cs` | ✅ Exists — `(string Name, string? Location, bool IsDefault)` |
| `UpdateWarehouseRequest` | `Contracts/Requests/WarehouseRequests.cs` | ✅ Exists — `(string Name, string? Location, bool IsDefault, bool IsActive)` |
| `WarehouseDto` | `Contracts/DTOs/AllDtos.cs` | ✅ Exists — `(int Id, string Name, string? Location, bool IsDefault, bool IsActive)` |
| `WarehouseResponse` | `Contracts/Responses/WarehouseResponses.cs` | ✅ Exists — `(int Id, string Name, string? Location, bool IsDefault, bool IsActive)` |

### 2.3 Warehouse Desktop Screens ✅ (Exists)

| Component | File | Status |
|-----------|------|--------|
| `WarehouseListViewModel` | `ViewModels/Warehouses/WarehouseListViewModel.cs` (327 lines) | ✅ Exists — Uses `ExecuteAsync()`, `ShowDialog`, EventBus, DeleteStrategy |
| `WarehouseEditorViewModel` | `ViewModels/Warehouses/WarehouseEditorViewModel.cs` (180 lines) | ✅ Exists — INotifyDataErrorInfo, `SetDialogService()`, `ValidateAllAsync()` |
| `WarehousesListView.xaml` | `Views/Warehouses/WarehousesView.xaml` (248 lines) | ✅ Exists — ModernDataGrid, ToolTips, compact styles |
| `WarehouseEditorView.xaml` | `Views/Warehouses/WarehouseEditorView.xaml` | ✅ Exists — Editor dialog form |
| `WarehouseEditorView.xaml.cs` | `Views/Warehouses/WarehouseEditorView.xaml.cs` | ✅ Exists |
| `WarehouseApiService` | `Services/Api/SupplierWarehouseApiService.cs` | ✅ Exists |

### 2.4 WarehouseStock Entity ✅ (Exists)

**Domain Entity**: `SalesSystem.Domain.Entities.WarehouseStock`

| Field | Type | Status |
|-------|------|--------|
| `WarehouseId` | `int` FK | ✅ Exists |
| `ProductId` | `int` FK | ✅ Exists |
| `Quantity` | `decimal(18,3)` | ✅ Exists — CHECK (Quantity >= 0) |
| `ReorderLevel` | `decimal(18,3)` | ✅ Exists |

**Domain Methods**: `IncreaseQuantity()`, `DecreaseQuantity()`, `SetQuantity()`, `DeductStock()` (with unit conversion), `AddStock()` (with unit conversion)

**Configuration**: `WarehouseStockConfiguration` — HasIndex(WarehouseId, ProductId) Unique, `DeleteBehavior.Restrict`, QueryFilter(IsActive)

**Repository**: Available via `IUnitOfWork.WarehouseStocks` (GenericRepository)

### 2.5 InventoryMovement Entity ✅ (Exists)

**Domain Entity**: `SalesSystem.Domain.Entities.InventoryMovement`

| Field | Type | Status |
|-------|------|--------|
| `ProductId` | `int` FK | ✅ Exists |
| `WarehouseId` | `int` FK | ✅ Exists |
| `MovementType` | `MovementType` enum | ✅ Exists |
| `QuantityChange` | `decimal` | ✅ Exists |
| `QuantityBefore` | `decimal` | ✅ Exists |
| `QuantityAfter` | `decimal` | ✅ Exists |
| `ReferenceType` | `string` | ✅ Exists |
| `ReferenceId` | `int` | ✅ Exists |
| `UnitCost` | `decimal?` | ✅ Exists |
| `MovementDate` | `DateTime` | ✅ Exists |
| `Notes` | `string?` | ✅ Exists |

**Repository**: Available via `IUnitOfWork.InventoryMovements`

### 2.6 InventoryService ✅ (Exists)

**Service**: `SalesSystem.Application.Services.InventoryService` (489 lines)

| Method | Status |
|--------|--------|
| `GetStockAsync(pid, wid, ct)` | ✅ Exists |
| `ValidateStockAsync(pid, wid, qty, allowNegative, ct)` | ✅ Exists |
| `IncreaseStockAsync(...)` | ✅ Exists — Creates InventoryMovement record |
| `DecreaseStockAsync(...)` | ✅ Exists — Creates InventoryMovement record |
| `GetTransferByIdAsync(id, ct)` | ✅ Exists |
| `GetAllTransfersAsync(...)` | ✅ Exists |
| `CreateTransferAsync(req, userId, ct)` | ✅ Exists — Uses transaction + sequence |
| `UpdateTransferAsync(id, req, userId, ct)` | ✅ Exists |
| `PostTransferAsync(id, userId, ct)` | ✅ Exists — Validates stock, deducts from source, adds to destination |
| `CancelTransferAsync(id, userId, ct)` | ✅ Exists — Reverses stock if posted |
| `GetWarehouseStockAsync(wid, search, ct)` | ✅ Exists |
| `GetWarehouseStocksAsync(wid, pid, p, ps, ct)` | ✅ Exists |
| `GetMovementsAsync(pid, wid, mt, p, ps, ct)` | ✅ Exists |

### 2.7 StockTransfer Entity ✅ (Exists)

**Domain Entity**: `SalesSystem.Domain.Entities.StockTransfer` (116 lines)

| Field | Type | Status |
|-------|------|--------|
| `TransferNo` | `string` | ✅ Exists |
| `FromWarehouseId` | `int` FK | ✅ Exists |
| `ToWarehouseId` | `int` FK | ✅ Exists |
| `TransferDate` | `DateTime` | ✅ Exists |
| `Notes` | `string?` | ✅ Exists |
| `Status` | `InvoiceStatus` | ✅ Exists |

**StockTransferItem**: `ProductId`, `Quantity`, `Mode`, `Notes`

**Lifecycle**: Draft (1) → Posted (2) → Cancelled (3)

### 2.8 StockWriteOff Entity ✅ (Exists)

**Domain Entity**: `SalesSystem.Domain.Entities.StockWriteOff`

| Field | Type | Status |
|-------|------|--------|
| `ProductId` | `int` FK | ✅ Exists |
| `WarehouseId` | `int` FK | ✅ Exists |
| `Quantity` | `decimal(18,3)` | ✅ Exists |
| `WriteOffDate` | `DateTime` | ✅ Exists |
| `Reason` | `nvarchar(250)` Required | ✅ Exists |
| `UnitId` | `int?` | ✅ Exists |

**Configuration**: `StockWriteOffConfiguration` — `DeleteBehavior.Restrict`, `HasQueryFilter(IsActive)`

### 2.9 MovementType Enum ✅ (Exists)

```csharp
public enum MovementType : byte
{
    PurchaseIn = 1,
    SaleOut = 2,
    SaleReturnIn = 3,
    PurchaseReturnOut = 4,
    TransferOut = 5,
    TransferIn = 6,
    Adjustment = 7
}
```

**Gap**: No `StockIssue`, `StockReceipt`, `PhysicalCount` values. Need to extend.

### 2.10 Existing DTOs & Requests Summary

| DTO | Status | Notes |
|-----|--------|-------|
| `WarehouseDto` | ✅ Exists | Needs: Type, Phone, ManagerName, Address, AccountId |
| `WarehouseStockDto` | ✅ Exists | `(WarehouseId, WarehouseName, ProductId, ProductName, UnitName, Quantity, ReorderLevel)` |
| `StockTransferDto` | ✅ Exists | 9 fields + items |
| `StockTransferItemDto` | ✅ Exists | 6 fields |
| `InventoryMovementDto` | ✅ Exists | 13 fields |
| `StockWriteOffDto` | ✅ Exists | 8 fields |
| `StockReportDto` | ✅ Exists | For reporting |
| `LowStockReportDto` | ✅ Exists | Includes wholesale/retail conversion |
| `CreateWarehouseRequest` | ✅ Exists | Needs: Type, Phone, ManagerName, Address |
| `UpdateWarehouseRequest` | ✅ Exists | Needs: Type, Phone, ManagerName, Address |
| `CreateStockTransferRequest` | ✅ Exists | In MiscRequests.cs |
| `UpdateStockTransferRequest` | ✅ Exists | In UpdateRequests.cs |

### 2.11 Existing Tests ✅

| Test File | Status |
|-----------|--------|
| `WarehouseTests.cs` (Domain) | ✅ Exists |
| `WarehouseStockTests.cs` (Domain) | ✅ Exists |
| `WarehouseStockUnitConversionTests.cs` (Domain) | ✅ Exists |
| `WarehouseServiceTests.cs` (Application) | ✅ Exists |
| `WarehousesControllerTests.cs` (Api) | ✅ Exists |
| `WarehouseRequestValidatorTests.cs` (Api) | ✅ Exists |
| `WarehouseListViewModelTests.cs` (Desktop) | ✅ Exists |
| `WarehouseEditorViewModelTests.cs` (Desktop) | ✅ Exists |
| `StockTransferTests.cs` (Domain) | ✅ Exists |
| `StockTransfersControllerTests.cs` (Api) | ✅ Exists |
| `StockTransfersListViewModelTests.cs` (Desktop) | ✅ Exists |
| `StockTransferEditorViewModelTests.cs` (Desktop) | ✅ Exists |
| `InventoryMovementTests.cs` (Domain) | ✅ Exists |

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: MovementType Enum Missing Inventory Operation Values

**Problem**: The `MovementType` enum currently has only 7 values for purchase/sale/transfer/adjustment. Standalone inventory operations (صرف مخزني, توريد مخزني) need their own `MovementType` values. Without these, standalone operations would be indistinguishable from invoice-driven stock changes.

**Root cause**: The system originally relied on invoices for all stock changes. Standalone operations were not anticipated.

**Decision**: **Do NOT extend MovementType**. Instead, the new `InventoryOperation` entity will store its own `OperationType` enum and ALWAYS create an `InventoryMovement` record with `MovementType.Adjustment` and `ReferenceType = "InventoryOperation"`. This keeps the existing MovementType enum stable and avoids breaking existing invoice references.

**Alternative considered**: Add `StockIssue = 8`, `StockReceipt = 9`, `PhysicalCount = 10` to MovementType. Rejected because this would break existing `switch` statements expecting only 7 values.

**Fix**:
1. Create new `InventoryOperationType` enum in Domain:
```csharp
public enum InventoryOperationType : byte
{
    StockIssue = 1,       // صرف مخزني
    StockReceipt = 2,     // توريد مخزني
    Adjustment = 3,       // تسوية مخزنية
    PhysicalCount = 4     // جرد مخزني (headers only)
}
```

2. All standalone operations create `InventoryMovement` with `MovementType.Adjustment`, `ReferenceType = "InventoryOperation"`, `ReferenceId = operation.Id`

### 3.2 Blocker 2: No Distinct InventoryOperation Entity

**Problem**: There is no entity to represent standalone stock operations (not linked to an invoice). Currently, all stock changes are tied to invoices, transfers, or write-offs. Standalone issue/receipt/adjust has no parent record.

**Fix**: Create `InventoryOperation` entity that serves as the parent record for standalone stock changes. This is analogous to `StockTransfer` for transfers and `StockWriteOff` for write-offs.

### 3.3 Blocker 3: Physical Count Requires Complex Two-Phase Flow

**Problem**: Physical inventory count requires:
1. **Phase 1 — Count**: User enters counted quantities for products in a warehouse. No stock change yet.
2. **Phase 2 — Compare**: System compares counted vs. system quantity, shows discrepancies.
3. **Phase 3 — Apply**: User approves adjustments → system creates Adjustment entries.

This is fundamentally different from a simple stock adjustment (which directly changes quantity).

**Decision**: Implement PhysicalCount as a **two-phase** operation:
- `PhysicalCount` entity records header (warehouse, date, status)
- `PhysicalCountItem` entity records counted quantity per product
- `PhysicalCountStatus` enum: `Draft = 1`, `Completed = 2`, `Applied = 3`
- On "Apply": system creates `InventoryOperation` (Adjustment) for each discrepancy, then logs `InventoryMovement` for each

> **⏳ DEFERRED TO V2**: According to Analysis Part 4:2957, Physical Count is out of scope for V1. In V1, stock discrepancies should be handled through **Inventory Adjustment with sub-types** (see §4.7 AdjustmentType) instead of a full count→compare→apply workflow.

### 3.4 Blocker 4: Stock Transfer Already Uses DocumentSequenceService

**Problem**: `StockTransfer` uses `DocumentSequenceService` to generate `TransferNo` with `TRF` prefix. The new `InventoryOperation` also needs numbering. If we use the same pattern, we need a new document type `"INVOP"`.

**Fix**: Add `"INVOP"` document type to `DocumentSequenceService`. Create `InventoryOperationNo` as `string` with format `INVOP-{YYYY}-{000001}`.

### 3.5 Blocker 5: WarehouseController Authorization Policy

**Problem**: Current `WarehousesController` uses `[Authorize(Policy = "AdminOnly")]`. According to Permissions Matrix:
- Warehouses CRUD: Admin only (✅ correct for CRUD)
- Stock operations (issue/receipt/transfer): ManagerAndAbove needed

**Fix**: 
- Warehouse CRUD stays `AdminOnly`
- New `InventoryOperationsController` uses `ManagerAndAbove`
- Stock Transfer enhancement keeps existing `PostTransfer` / `CancelTransfer` with `ManagerAndAbove`

---

## 4. Warehouse Design Catalog

### 4.1 Warehouse (Enhanced Entity)

#### Current Fields

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int PK` | Auto-Increment | Warehouse identifier |
| `Name` | `nvarchar(100)` | Required, not empty | Warehouse display name |
| `Location` | `nvarchar(250)?` | Max 250 chars | General location description |
| `IsDefault` | `bit` | Default false | Single default warehouse |
| `IsActive` | `bit` | Global query filter | Soft delete |

#### New Fields

| # | Field | Type | Required | Default | Constraints | Description |
|---|-------|------|----------|---------|-------------|-------------|
| 1 | `Type` | `int` (WarehouseType) | ✅ | `Main = 1` | 1=Main, 2=Secondary, 3=Showroom, 4=Damaged | نوع المستودع |
| 2 | `Phone` | `nvarchar(20)?` | ❌ | `null` | Max 20 chars | هاتف المستودع |
| 3 | `Address` | `nvarchar(250)?` | ❌ | `null` | Max 250 chars | العنوان الكامل (extends Location) |
| 4 | `ManagerName` | `nvarchar(100)?` | ❌ | `null` | Max 100 chars | مدير المستودع |
| 5 | `AccountId` | `int?` FK | ❌ | `null` | `DeleteBehavior.Restrict` | الحساب المحاسبي للمستودع |
| 6 | `Notes` | `nvarchar(max)?` | ❌ | `null` | — | ملاحظات عامة عن المستودع |

> **Rationale for Notes**: Allows warehouse staff to record free-text observations about the warehouse (e.g., "يتطلب صيانة دورية", "مستودع مؤقت"). Simple and additive — no breaking changes.

**WarehouseType Enum** (New — to be added in Domain/Enums):
```csharp
public enum WarehouseType : byte
{
    Main = 1,         // رئيسي
    Secondary = 2,    // ثانوي
    Showroom = 3,     // صالة عرض
    Damaged = 4       // توالف
}
```

### 4.2 InventoryOperation Entity (NEW)

**Purpose**: Parent record for standalone stock operations (not linked to invoices).

**File**: `Domain/Entities/Inventory/InventoryOperation.cs`

| # | Field | Type | Required | Default | Constraints |
|---|-------|------|----------|---------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto-Increment | — |
| 2 | `OperationNo` | `nvarchar(50)` | ✅ | — | Unique Index |
| 3 | `WarehouseId` | `int` FK | ✅ | — | `DeleteBehavior.Restrict` |
| 4 | `OperationType` | `int` (InventoryOperationType) | ✅ | — | 1=Issue, 2=Receipt, 3=Adjustment |
| 5 | `OperationDate` | `datetime2` | ✅ | `DateTime.UtcNow` | — |
| 6 | `ReferenceNo` | `nvarchar(100)?` | ❌ | `null` | External reference number |
| 7 | `Notes` | `nvarchar(500)?` | ❌ | `null` | General notes |
| 8 | `AdjustmentType` | `byte?` | ❌ | `null` | 1=Opening, 2=Damaged, 3=Surplus, 4=Shortage — only for Adjustment ops |
| 9 | `Status` | `int` (InvoiceStatus) | ✅ | `Draft = 1` | Draft → Posted (immutable after post) |
| 10 | `CreatedByUserId` | `int?` FK | ❌ | `null` | From BaseEntity |

**Navigation Properties**:
- `Warehouse` → `Warehouse`
- `Items` → `List<InventoryOperationItem>`
- `CreatedByUser` → `User`

**Domain Methods**:
- `Create(operationNo, warehouseId, operationType, notes?, operationDate?, adjustmentType?, createdByUserId?)` — Static factory
- `AddItem(productId, quantity, unitCost?, stockIssueReason?, notes?)` — Adds item (only in Draft)
- `Post()` — Sets Status = Posted (immutable after)
- `Cancel()` — Sets Status = Cancelled

**Validation** (Guard Clauses):
- `operationNo`: not empty
- `warehouseId`: > 0
- `operationType`: valid enum
- `adjustmentType`: valid if OperationType == Adjustment, null otherwise
- `AddItem`: only in Draft, quantity > 0

### 4.3 InventoryOperationItem Entity (NEW)

**File**: `Domain/Entities/Inventory/InventoryOperationItem.cs`

| # | Field | Type | Required | Constraints |
|---|-------|------|----------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto-Increment |
| 2 | `InventoryOperationId` | `int` FK | ✅ | `DeleteBehavior.Restrict` |
| 3 | `ProductId` | `int` FK | ✅ | `DeleteBehavior.Restrict` |
| 4 | `Quantity` | `decimal(18,3)` | ✅ | > 0 |
| 5 | `UnitCost` | `decimal(18,2)?` | ❌ | Optional cost tracking |
| 6 | `StockIssueReason` | `byte?` | ❌ | Only for Issue ops — 1=Damaged, 2=InternalUse, 3=FreeSample, 4=Other |
| 7 | `Notes` | `nvarchar(250)?` | ❌ | Per-item notes |

**Navigation Properties**:
- `InventoryOperation` → `InventoryOperation`
- `Product` → `Product`

**Domain Methods**:
- `Create(inventoryOperationId, productId, quantity, unitCost?, notes?)`
- `SetQuantity(decimal quantity)` — Only while operation is Draft

### 4.4 PhysicalCount Entity (NEW — Two-Phase Count) ⏳ DEFERRED TO V2

**File**: `Domain/Entities/Inventory/PhysicalCount.cs`

| # | Field | Type | Required | Constraints |
|---|-------|------|----------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto-Increment |
| 2 | `CountNo` | `nvarchar(50)` | ✅ | Unique Index |
| 3 | `WarehouseId` | `int` FK | ✅ | `DeleteBehavior.Restrict` |
| 4 | `CountDate` | `datetime2` | ✅ | `DateTime.UtcNow` |
| 5 | `Notes` | `nvarchar(500)?` | ❌ | — |
| 6 | `Status` | `int` (PhysicalCountStatus) | ✅ | 1=Draft, 2=Completed, 3=Applied |

**InventoryOperationType.PhysicalCount (4)** is NOT a standalone operation — it's the HEADER. Adjustment operations are created when "Apply" is executed.

**PhysicalCountStatus Enum** (New):
```csharp
public enum PhysicalCountStatus : byte
{
    Draft = 1,      // Counting in progress
    Completed = 2,  // Count done, discrepancies visible
    Applied = 3     // Adjustments applied to stock (terminal)
}
```

### 4.5 PhysicalCountItem Entity (NEW) ⏳ DEFERRED TO V2

**File**: `Domain/Entities/Inventory/PhysicalCountItem.cs`

| # | Field | Type | Required | Constraints |
|---|-------|------|----------|-------------|
| 1 | `Id` | `int PK` | ✅ | Auto-Increment |
| 2 | `PhysicalCountId` | `int` FK | ✅ | `DeleteBehavior.Restrict` |
| 3 | `ProductId` | `int` FK | ✅ | `DeleteBehavior.Restrict` |
| 4 | `SystemQuantity` | `decimal(18,3)` | ✅ | Read-only at count start |
| 5 | `CountedQuantity` | `decimal(18,3)` | ✅ | User-entered |
| 6 | `Difference` | `decimal(18,3)` | ✅ | Computed: `CountedQuantity - SystemQuantity` |
| 7 | `UnitCost` | `decimal(18,2)?` | ❌ | Cost at time of count |
| 8 | `Notes` | `nvarchar(250)?` | ❌ | Reason for discrepancy |

**Domain Method**: `ComputeDifference()` — called in setter, difference = counted - system. Positive = surplus, negative = shortage.

### 4.6 Enhanced StockTransfer (Enhancement)

**Current**: StockTransfer has basic flow (Draft → Posted → Cancelled). Stock movement recorded via `InventoryService.IncreaseStockAsync` / `DecreaseStockAsync`.

**Enhancements**:
1. Add `TransferNo` format enhancement — use `DocumentSequenceService` (already done)
2. Add `CreatedByUserId` — already inherits from BaseEntity
3. Add `ApprovedByUserId` — for future approval workflow (Non-V1)
4. Add `TransferType` — `Internal = 1`, `Branch = 2` (for future)
5. Add print support (transfer document printing via QuestPDF)

### 4.7 New AdjustmentType Enum (Sub-types for Adjustment Operations)

**Purpose**: Analysis Part 4:2902–2953 requires 4 distinct sub-types for Inventory Adjustment, each with different accounting treatment (different debit/credit accounts).

**File**: `Domain/Enums/AdjustmentType.cs`

```csharp
public enum AdjustmentType : byte
{
    Opening = 1,   // افتتاحي — opening stock entry for new warehouse
    Damaged = 2,   // تالف — damaged/write-off stock
    Surplus = 3,   // زيادة — inventory surplus (counted > system)
    Shortage = 4   // عجز — inventory shortage (counted < system)
}
```

**Integration**:
- `InventoryOperation` entity gains an optional `AdjustmentType` field (`byte?`) — only populated when `OperationType = Adjustment`.
- V1 UI: ComboBox to select sub-type when creating an Adjustment.
- Accounting impact (Phase 27+):
  - `Opening` → Debit: Stock Asset, Credit: Opening Balance Equity
  - `Damaged` → Debit: Loss/Expense, Credit: Stock Asset
  - `Surplus` → Debit: Stock Asset, Credit: Income (Inventory Surplus)
  - `Shortage` → Debit: Loss/Expense (Inventory Shortage), Credit: Stock Asset

> **V1 stock handling**: All 4 sub-types follow the same stock change flow (increase or decrease WarehouseStock.Quantity). The `AdjustmentType` field is recorded for audit trail and future accounting integration. In V1, discrepancies found during informal counting should use `Surplus` or `Shortage` adjustments rather than the deferred Physical Count workflow.

---

### 4.8 StockIssueReason Enum (For Issue Operations, V1)

**Purpose**: Analysis Part 4:2860–2876 recommends showing reasons (تالف/استهلاك داخلي/عينات مجانية) instead of accounts in the Issue operation UI.

**File**: `Domain/Enums/StockIssueReason.cs`

```csharp
public enum StockIssueReason : byte
{
    Damaged = 1,       // تالف
    InternalUse = 2,   // استهلاك داخلي
    FreeSample = 3,    // عينات مجانية
    Other = 4          // أخرى
}
```

**Integration**:
- `InventoryOperationItem` gains an optional `StockIssueReason` field (`byte?`) — only populated when the parent `OperationType = StockIssue`.
- V1 UI: ComboBox to select reason in each Issue item row.
- `Notes` field is still available for free-text explanation.
- The reason field is for informational/audit purposes only — no stock change logic difference between reasons in V1.

---

## 5. Gap Analysis

### 5.1 Warehouse Entity Gaps

| Feature | Status | Action |
|---------|--------|--------|
| Warehouse Type (Main/Secondary/Showroom/Damaged) | ❌ Missing | Add `Type` field + `WarehouseType` enum |
| Warehouse Phone | ❌ Missing | Add `Phone` field |
| Warehouse Address | ⚠️ Partial | Rename `Location` → `Address` or add separate `Address` field |
| Warehouse Manager | ❌ Missing | Add `ManagerName` field |
| Account Link (AccountId FK) | ❌ Missing | Add `AccountId` FK → Account table |
| Warehouse type filter in list | ❌ Missing | Add filter combo in toolbar |
| Warehouse stock summary in list | ❌ Missing | Show product count + total stock value per warehouse |

### 5.2 Inventory Operations Gaps

| Operation | Status | Action |
|-----------|--------|--------|
| Stock Issue (صرف مخزني) | ❌ Missing | Create InventoryOperation entity + UI |
| Stock Receipt (توريد مخزني) | ❌ Missing | Same entity, different OperationType |
| Stock Adjustment (تسوية مخزنية) | ❌ Missing | Same entity, direct SetQuantity |
| Stock Transfer (تحويل مخزني) | ✅ Exists | Enhancement: print, approval |
| Physical Count (جرد مخزني) | ⏳ V2 | **Deferred to V2** (Analysis Part 4:2957). Use Adjustment sub-types (Surplus/Shortage) in V1. |
| InventoryOperation entity | ❌ Missing | Create from scratch |
| InventoryOperationItem entity | ❌ Missing | Create from scratch |
| PhysicalCount entity | ⏳ V2 | **Deferred to V2** — part of Physical Count flow |
| PhysicalCountItem entity | ⏳ V2 | **Deferred to V2** — part of Physical Count flow |
| InventoryOperationService | ❌ Missing | Create with Result<T> + IUnitOfWork |
| PhysicalCountService | ⏳ V2 | **Deferred to V2** — part of Physical Count flow |
| InventoryOperationApiService (Desktop) | ❌ Missing | Add to IApiService.cs |
| PhysicalCountApiService (Desktop) | ⏳ V2 | **Deferred to V2** — part of Physical Count flow |

### 5.3 Stock Reports Gaps

| Report | Status | Action |
|--------|--------|--------|
| Stock balance per warehouse | ✅ Exists via `GetWarehouseStocksAsync` | Needs dedicated View + ViewModel |
| Stock movement history | ✅ Exists via `GetMovementsAsync` | Needs dedicated View + ViewModel |
| Low stock alerts per warehouse | ✅ Exists via `LowStockReportDto` | Needs per-warehouse filter in existing LowStock screen |
| Inventory valuation per warehouse | ❌ Missing | New report: Quantity × AvgCost per product |
| Stock status dashboard (warehouse overview) | ❌ Missing | Create inline summary cards in warehouse list |

### 5.4 Existing Service Gaps in InventoryService

| Method | Status | Action |
|--------|--------|--------|
| `GetStockAsync` | ✅ | No change |
| `ValidateStockAsync` | ✅ | No change |
| `IncreaseStockAsync` | ✅ | No change |
| `DecreaseStockAsync` | ✅ | No change |
| Standalone issue/receipt/adjust | ❌ | Create `InventoryOperationService` (separate from `InventoryService`) |
| Physical count | ❌ | Create `PhysicalCountService` (separate) |
| Inventory valuation | ❌ | Add to `ReportService` or create `InventoryValuationService` |

### 5.5 API Gaps

| Endpoint | Status | Action |
|----------|--------|--------|
| `GET /api/v1/warehouses/{id}/stocks` | ✅ Exists (via inventory controller) | No change |
| `GET /api/v1/warehouses/{id}/movements` | ✅ Exists (via inventory controller) | No change |
| `POST /api/v1/inventory-operations/issue` | ❌ Missing | New controller |
| `POST /api/v1/inventory-operations/receipt` | ❌ Missing | New controller |
| `POST /api/v1/inventory-operations/adjust` | ❌ Missing | New controller |
| `GET /api/v1/inventory-operations` | ❌ Missing | List with filtering |
| `GET /api/v1/inventory-operations/{id}` | ❌ Missing | Get by ID |
| `POST /api/v1/inventory-operations/{id}/post` | ❌ Missing | Post operation |
| `POST /api/v1/inventory-operations/{id}/cancel` | ❌ Missing | Cancel operation |
| `POST /api/v1/physical-counts` | ⏳ V2 | **Deferred to V2** — part of Physical Count |
| `POST /api/v1/physical-counts/{id}/items` | ⏳ V2 | **Deferred to V2** |
| `POST /api/v1/physical-counts/{id}/complete` | ⏳ V2 | **Deferred to V2** |
| `POST /api/v1/physical-counts/{id}/apply` | ⏳ V2 | **Deferred to V2** |
| `GET /api/v1/physical-counts` | ⏳ V2 | **Deferred to V2** |
| `GET /api/v1/physical-counts/{id}` | ⏳ V2 | **Deferred to V2** |
| `GET /api/v1/reports/stock-valuation/{warehouseId}` | ❌ Missing | Valuation report |

---

## 6. Architectural Decisions

### 6.1 Standalone InventoryOperation Entity vs Reusing InventoryMovement

**Problem**: Should standalone stock issue/receipt/adjust create `InventoryMovement` records directly without a parent entity?

**Decision**: **Create `InventoryOperation` entity as parent.**

**Rationale**:
- Similar pattern to `StockTransfer` (which has header + items + posts as a unit)
- `InventoryMovement` is already used by invoices and transfers — a standalone operation needs its own referenceable parent
- Enables Draft → Posted lifecycle (user can prepare, review, then post)
- `InventoryMovement` still records EVERY change (RULE-028) with `ReferenceType = "InventoryOperation"` and `ReferenceId = operation.Id`

**Stock change flow for standalone operations**:
```
User saves InventoryOperation (Draft)   → No stock change
User posts InventoryOperation            → Each item: InventoryMovement created + WarehouseStock updated
User cancels (if posted)                 → Reverse InventoryMovement created + WarehouseStock reversed
```

### 6.2 PhysicalCount vs Separate Adjustment Entries

**Problem**: Physical count produces multiple adjustments. Should each discrepancy be a separate `InventoryOperation` or one batch?

**Decision**: **One `PhysicalCount` header → multiple `InventoryOperation` (Adjustment) records on Apply.**

**Flow**:
1. User creates `PhysicalCount` (Draft)
2. User scans/adds products with counted quantities → `PhysicalCountItem` records
3. System computes `SystemQuantity` at time of record creation (snapshot)
4. User reviews discrepancies in completion screen
5. User clicks "Apply" → system creates one `InventoryOperation` (Adjustment) per product with discrepancy
6. Each `InventoryOperation` is immediately posted (skips Draft)
7. `PhysicalCount.Status` → `Applied`

### 6.3 Two Services vs One

**Problem**: Should `InventoryOperation` and `PhysicalCount` share one service or be separate?

**Decision**: **Two separate services**:
- `IInventoryOperationService` — Create/Post/Cancel standalone operations
- `IPhysicalCountService` — Create/Complete/Apply physical counts (calls InventoryOperationService internally on Apply)

**Rationale**: Physical count has distinct two-phase logic that would complicate a combined service.

### 6.4 Warehouse Account Link (AccountId FK)

**Problem**: Should `Warehouse` link to an `Account` for financial accounting?

**Decision**: **Add `int? AccountId` FK — optional in V1.**

**Rationale**: Required for future accounting integration (Phase 27+). Each warehouse's stock value needs a corresponding account in the chart of accounts. Adding the FK now is additive and non-breaking (nullable).

### 6.5 Stock Issue/Receipt Cost Tracking

**Problem**: Should standalone stock issue/receipt track unit cost?

**Decision**: **Track `UnitCost` per InventoryOperationItem — optional.**

- For **Stock Receipt** (توريد مخزني): User can enter unit cost, which will update the weighted average (if CostingMethod = WeightedAverage)
- For **Stock Issue** (صرف مخزني): Cost is read from current `AvgCost` at time of posting
- For **Adjustment**: Cost can be entered (for surplus/positive adjustment) or read from AvgCost (for shortage/negative)

### 6.6 Why NOT Extend MovementType Enum

**Decision**: Do NOT add `StockIssue = 8`, `StockReceipt = 9` to `MovementType`.

**Rationale**:
- `MovementType` is used in switch statements for invoice/transfer logic — adding values risks missing cases
- `MovementType.Adjustment (7)` is semantically correct: standalone operations ARE adjustments (just with a parent reference)
- The `InventoryOperation.OperationType` field distinguishes Issue vs Receipt vs Adjustment
- `InventoryMovement.ReferenceType = "InventoryOperation"` provides traceability

### 6.7 UI Pattern for Inventory Operations

All inventory operation screens follow this pattern:

| Screen | Opens via | Pattern |
|--------|-----------|---------|
| Stock Issue List | MainWindow navigation | `ScreenWindowService.OpenScreen` |
| Stock Issue Editor | Add/Edit button | `ScreenWindowService.OpenScreen` (non-modal) |
| Stock Receipt List | MainWindow navigation | `ScreenWindowService.OpenScreen` |
| Stock Receipt Editor | Add/Edit button | `ScreenWindowService.OpenScreen` (non-modal) |
| Stock Adjustment | MainWindow navigation | `ScreenWindowService.OpenScreen` |
| Stock Adjustment Editor | Add/Edit button | `ScreenWindowService.OpenScreen` (non-modal) |
| Physical Count List | MainWindow navigation | `ScreenWindowService.OpenScreen` |
| Physical Count Editor | Add/Edit button | `ScreenWindowService.OpenScreen` (non-modal) |
| Physical Count Compare | Complete button | `ScreenWindowService.OpenScreen` (non-modal) |
| Stock Transfer (existing) | MainWindow navigation | Already exists |

**RULE-160**: `ShowDialog()` NEVER used for editors — always `ScreenWindowService.OpenScreen`.

### 6.8 Stock Change Authorization

| Operation | Required Policy |
|-----------|----------------|
| Warehouses CRUD | `AdminOnly` |
| Stock Issue (صرف مخزني) | `ManagerAndAbove` |
| Stock Receipt (توريد مخزني) | `ManagerAndAbove` |
| Stock Adjustment (تسوية مخزنية) | `AdminOnly` |
| Physical Count (جرد مخزني) | `ManagerAndAbove` |
| Stock Transfer (تحويل مخزني) | `ManagerAndAbove` |

**Rule**: Only Admin can adjust stock without reference (adjustments). Managers can issue/receive/transfer.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason |
|---------|--------|
| Warehouse Transfer Approval Workflow | Requires an approval system with `ApprovedByUserId` + notification — complex add-on |
| Warehouse Zones/ Locations (bin locations) | Requires `WarehouseZone` entity + bin tracking — major scope expansion |
| Warehouse Barcode/QR labeling | Physical labeling system — out of scope for software V1 |
| Negative stock allowed per warehouse | Already handled via `AllowNegativeStock` system setting — separate task |
| Warehouse-specific pricing | Pricing is per `ProductUnit`, not per warehouse — not needed now |
| Multi-warehouse transfer approval chain | Requires user-based approval routing — deferred to V2 |
| Stock expiry tracking per warehouse | Requires `StockBatch` entity by warehouse — large feature |
| Automatic reorder generation | Would generate PurchaseInvoice drafts from low-stock alerts — complex |
| Warehouse performance analytics | Turnover rate, slow-movers, cost of carrying — reporting enhancement |
| Physical Count (جرد مخزني) | **Analysis Part 4:2957 confirms V1 does NOT require a Physical Count screen.** Use Adjustment with sub-types (Surplus/Shortage) for discrepancies instead. Full count→compare→apply flow deferred to V2. |
| Physical count using mobile device | Requires mobile app / barcode scanner API — separate project |
| FIFO/FEFO per warehouse | Costing method is system-wide, not per-warehouse — Constitution RULE-068 |

---

## 8. Implementation Tasks

All tasks include:
- Logging (RULE-035/036): Log.Information on create/update/post/cancel
- Error handling (RULE-199/200/201): LogSystemError() in ViewModels
- ToolTips (RULE-185-190): Arabic ToolTips on ALL interactive controls
- UI Compact styles (RULE-262-274): 28px inputs via styles, compact margins
- INotifyDataErrorInfo (RULE-228): No HasXxxError booleans
- SetDialogService() (RULE-227): Called in every Editor VM constructor
- ValidateAllAsync() (RULE-229): Pre-save validation with ErrorTemplate
- ExecuteAsync() (RULE-141): ALL async commands wrapped

---

### Task 1 — Enhance Warehouse Entity (Type, Phone, Address, ManagerName, AccountId)

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/WarehouseType.cs` | **NEW** — `enum WarehouseType : byte { Main=1, Secondary=2, Showroom=3, Damaged=4 }` |
| `Domain/Entities/Warehouse.cs` | Add `Type`, `Phone`, `Address`, `ManagerName`, `AccountId`, `Notes` properties. Update `Create()` and `Update()` signatures. Add `IsDefault` guard — can only have one default. |
| `Infrastructure/Data/Configurations/WarehouseStockConfiguration.cs` | Add `WarehouseConfiguration` updates: new properties with `HasMaxLength`, `Phone` max 20, `Address` max 250, `ManagerName` max 100, `AccountId` FK with `DeleteBehavior.Restrict` |
| `Contracts/DTOs/AllDtos.cs` — `WarehouseDto` | Add `int Type`, `string? Phone`, `string? Address`, `string? ManagerName`, `int? AccountId` |
| `Contracts/Requests/WarehouseRequests.cs` | Update `CreateWarehouseRequest`: add `int Type = 1`, `string? Phone`, `string? Address`, `string? ManagerName`. Update `UpdateWarehouseRequest` similarly. |
| `Contracts/Responses/WarehouseResponses.cs` | Update `WarehouseResponse`: add `Type`, `Phone`, `Address`, `ManagerName` |
| `Application/Services/WarehouseService.cs` | Update `MapToDto()`, `CreateAsync()`, `UpdateAsync()` to handle new fields. Add `IsDefault` uniqueness check. |
| `Api/Validators/WarehouseRequestValidators.cs` | Update validators: `Type` must be 1-4, `Phone` max 20, `Address` max 250, `ManagerName` max 100 |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE Warehouses ADD ...` |
| **Desktop** — `ViewModels/Warehouses/WarehouseEditorViewModel.cs` | Add `Type` (int with combo), `Phone`, `Address`, `ManagerName` properties. Update `SaveOperationAsync()`. |
| **Desktop** — `Views/Warehouses/WarehouseEditorView.xaml` | Add fields: Type dropdown, Phone, Address, ManagerName inputs. Compact layout. |
| **Desktop** — `Views/Warehouses/WarehousesView.xaml` | Add Type column to DataGrid. Add Type filter combo in toolbar. Update empty state. |
| **Desktop** — `ViewModels/Warehouses/WarehouseListViewModel.cs` | Add `SelectedType` filter property. Update `FilterWarehouses()`. |
| **Desktop** — `Services/Api/IApiService.cs` — `IWarehouseApiService` | Update `CreateAsync`/`UpdateAsync` signatures (no breaking change — parameter objects changed). |

**Warehouse.Create enhanced signature**:
```csharp
public static Warehouse Create(
    string name,
    WarehouseType type = WarehouseType.Main,
    string? location = null,
    string? phone = null,
    string? address = null,
    string? managerName = null,
    bool isDefault = false,
    int? createdByUserId = null)
```

**Warehouse.Update enhanced signature**:
```csharp
public void Update(
    string name,
    WarehouseType type,
    string? location,
    string? phone,
    string? address,
    string? managerName,
    bool isDefault,
    int? updatedByUserId = null)
```

**Logging**: `Log.Information("Warehouse {Id} {Name} enhanced with Type {Type}", id, name, type)`

**Estimate**: ~2 hours

---

### Task 2 — Create InventoryOperation Entity + Configuration (AdjustmentType + StockIssueReason)

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/InventoryOperationType.cs` | **NEW** — `enum InventoryOperationType : byte { StockIssue=1, StockReceipt=2, Adjustment=3 }` |
| `Domain/Entities/Inventory/InventoryOperation.cs` | **NEW** — Entity with `Id`, `OperationNo`, `WarehouseId`, `OperationType`, `OperationDate`, `ReferenceNo`, `Notes`, `AdjustmentType`, `Status` + navigation properties + domain methods |
| `Domain/Entities/Inventory/InventoryOperationItem.cs` | **NEW** — Entity with `Id`, `InventoryOperationId`, `ProductId`, `Quantity`(18,3), `UnitCost`(18,2), `Notes` |
| `Infrastructure/Data/Configurations/InventoryOperationConfiguration.cs` | **NEW** — Fluent API: Table `InventoryOperations`, FKs with `Restrict`, HasIndex on `OperationNo` Unique, CHECK on `OperationType` in (1,2,3), CHECK on `Quantity > 0`, QueryFilter(IsActive) |
| `Infrastructure/Data/Configurations/InventoryOperationItemConfiguration.cs` | **NEW** — Same pattern |
| `Contracts/DTOs/AllDtos.cs` | Add `InventoryOperationDto`, `InventoryOperationItemDto` |
| `Contracts/Requests/InventoryOperationRequests.cs` | **NEW** — `CreateInventoryOperationItemRequest`, `CreateInventoryOperationRequest`. Issue items include optional `StockIssueReason`. Adjustment include optional `AdjustmentType`. |

**InventoryOperationDto**:
```csharp
public record InventoryOperationDto(
    int Id,
    string OperationNo,
    int WarehouseId,
    string WarehouseName,
    byte OperationType,
    DateTime OperationDate,
    string? ReferenceNo,
    string? Notes,
    byte Status,
    IReadOnlyList<InventoryOperationItemDto> Items)
{
    public string OperationTypeDisplay => OperationType switch
    {
        1 => "صرف مخزني",
        2 => "توريد مخزني",
        3 => "تسوية مخزنية",
        _ => "غير معروف"
    };
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record InventoryOperationItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal? UnitCost,
    string? Notes);
```

**CreateInventoryOperationRequest**:
```csharp
public record CreateInventoryOperationRequest(
    int WarehouseId,
    byte OperationType,
    byte? AdjustmentType,         // Only for Adjustment ops (1-4)
    DateTime? OperationDate,
    string? ReferenceNo,
    string? Notes,
    List<CreateInventoryOperationItemRequest> Items);

public record CreateInventoryOperationItemRequest(
    int ProductId,
    decimal Quantity,
    decimal? UnitCost,
    byte? StockIssueReason,       // Only for Issue ops (1-4, see §4.8)
    string? Notes);
```

**Estimate**: ~2 hours

---

### Task 3 — InventoryOperationService (with Result<T>, Transactions, Inventory Movement Logging)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IInventoryOperationService.cs` | **NEW** — Interface with 6 methods |
| `Application/Services/InventoryOperationService.cs` | **NEW** — Full implementation (see below) |

**Interface**:
```csharp
public interface IInventoryOperationService
{
    Task<Result<InventoryOperationDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<InventoryOperationDto>>> GetAllAsync(int? warehouseId, byte? operationType, int page, int pageSize, CancellationToken ct);
    Task<Result<InventoryOperationDto>> CreateAsync(CreateInventoryOperationRequest request, int userId, CancellationToken ct);
    Task<Result<InventoryOperationDto>> PostAsync(int id, int userId, CancellationToken ct);  // Stock change happens HERE
    Task<Result<InventoryOperationDto>> CancelAsync(int id, int userId, CancellationToken ct);  // Reverses if posted
}
```

**Service Implementation Pattern** (RULE-003, RULE-006, RULE-024, RULE-028):

```csharp
public async Task<Result<InventoryOperationDto>> PostAsync(int id, int userId, CancellationToken ct)
{
    var operation = await _uow.InventoryOperations.FirstOrDefaultAsync(
        o => o.Id == id, ct, "Items.Product", "Warehouse");

    if (operation == null)
        return Result<InventoryOperationDto>.Failure("العملية غير موجودة", ErrorCodes.NotFound);
    if (operation.Status != InvoiceStatus.Draft)
        return Result<InventoryOperationDto>.Failure("يمكن فقط ترحيل العمليات المسودة");

    // Validate stock for Issue operations
    if (operation.OperationType == InventoryOperationType.StockIssue)
    {
        foreach (var item in operation.Items)
        {
            var validation = await _inventoryService.ValidateStockAsync(
                item.ProductId, operation.WarehouseId, item.Quantity, false, ct);
            if (!validation.IsSuccess)
                return Result<InventoryOperationDto>.Failure(validation.Error!);
        }
    }

    return await _uow.ExecuteAsync(async () =>
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            operation.Post();

            foreach (var item in operation.Items)
            {
                decimal qtyBefore, qtyAfter;
                if (operation.OperationType == InventoryOperationType.StockReceipt)
                {
                    // Increase stock
                    qtyBefore = (await _inventoryService.GetStockAsync(item.ProductId, operation.WarehouseId, ct)).Value;
                    await _inventoryService.IncreaseStockAsync(
                        item.ProductId, operation.WarehouseId, item.Quantity,
                        MovementType.Adjustment, "InventoryOperation", operation.Id,
                        item.UnitCost, userId, ct);
                    qtyAfter = qtyBefore + item.Quantity;

                    // Update costing if UnitCost provided
                    if (item.UnitCost.HasValue)
                    {
                        await _pricingService.UpdateCostAsync(item.ProductId,
                            operation.WarehouseId, item.Quantity, item.UnitCost.Value, userId, ct);
                    }
                }
                else if (operation.OperationType == InventoryOperationType.StockIssue)
                {
                    // Decrease stock
                    qtyBefore = (await _inventoryService.GetStockAsync(item.ProductId, operation.WarehouseId, ct)).Value;
                    await _inventoryService.DecreaseStockAsync(
                        item.ProductId, operation.WarehouseId, item.Quantity,
                        MovementType.Adjustment, "InventoryOperation", operation.Id,
                        null, userId, ct);
                    qtyAfter = qtyBefore - item.Quantity;
                }
                else // Adjustment
                {
                    var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                        ws => ws.WarehouseId == operation.WarehouseId && ws.ProductId == item.ProductId, ct);
                    qtyBefore = stock?.Quantity ?? 0;
                    
                    // Adjustment can go either direction via SetQuantity
                    if (stock != null)
                    {
                        stock.SetQuantity(qtyBefore + item.Quantity); // item.Quantity is the CHANGE (+ or -)
                        qtyAfter = stock.Quantity;
                    }
                    else
                    {
                        stock = WarehouseStock.Create(operation.WarehouseId, item.ProductId, item.Quantity);
                        await _uow.WarehouseStocks.AddAsync(stock, ct);
                        qtyAfter = item.Quantity;
                    }

                    var movement = InventoryMovement.Create(
                        item.ProductId, operation.WarehouseId,
                        MovementType.Adjustment, item.Quantity,
                        qtyBefore, qtyAfter,
                        "InventoryOperation", operation.Id,
                        item.UnitCost, null, userId);
                    await _uow.InventoryMovements.AddAsync(movement, ct);
                }
            }

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "InventoryOperation {Id} ({Type}) posted for Warehouse {WarehouseId}",
                operation.Id, operation.OperationType, operation.WarehouseId);

            return await GetByIdAsync(operation.Id, ct);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            return Result<InventoryOperationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error posting inventory operation {Id}", id);
            return Result<InventoryOperationDto>.Failure("حدث خطأ أثناء ترحيل العملية المخزنية");
        }
    }, ct);
}
```

**Logging** (RULE-035/036):
- `Log.Information("InventoryOperation {Id} created: {Type}", id, type)` on create
- `Log.Information("InventoryOperation {Id} posted for Warehouse {Wid}", id, wid)` on post
- `Log.Warning("InventoryOperation {Id} cancelled", id)` on cancel (RULE-183)
- `Log.Error(ex, "Error posting inventory operation {Id}", id)` on system error (RULE-182)

**Estimate**: ~3 hours

---

### Task 4 — InventoryOperationsController (API Endpoints)

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/InventoryOperationsController.cs` | **NEW** — 6 endpoints |

**Endpoints**:

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| `GET` | `/api/v1/inventory-operations` | `ManagerAndAbove` | List with filtering (warehouseId, operationType) |
| `GET` | `/api/v1/inventory-operations/{id}` | `ManagerAndAbove` | Get by ID with items |
| `POST` | `/api/v1/inventory-operations` | `ManagerAndAbove` | Create operation (Draft) |
| `POST` | `/api/v1/inventory-operations/{id}/post` | `ManagerAndAbove` | Post — stock change happens |
| `POST` | `/api/v1/inventory-operations/{id}/cancel` | `ManagerAndAbove` | Cancel — reverse if posted |
| `POST` | `/api/v1/inventory-operations/{id}/adjust` | `AdminOnly` | Quick adjust without items (direct Qty) |

**Controller purity** (RULE-203): Injects `IInventoryOperationService` only — NO `DbContext` or `IUnitOfWork`.

**FluentValidation** (RULE-044):
- `CreateInventoryOperationRequestValidator`: WarehouseId > 0, OperationType 1-3, Items not empty, each item Quantity > 0, ProductId > 0
- For Issue: validate stock exists before create (service layer)

**Estimate**: ~1.5 hours

---

### Task 5 — Stock Issue (صرف مخزني) — Desktop Screens

> **Reason-based UX** (Analysis Part 4:2860–2876): Issue items show a `StockIssueReason` combo (see §4.8) instead of raw account fields. Each item row includes: تالف / استهلاك داخلي / عينات مجانية / أخرى. This is informational only — no stock change logic difference between reasons in V1.

**Files**:

| File | Content |
|------|---------|
| `Services/Api/IApiService.cs` | Add `IInventoryOperationApiService` interface |
| `Services/Api/InventoryOperationApiService.cs` | **NEW** — HTTP client implementation with content-type guard (RULE-184) |
| `ViewModels/InventoryOperations/InventoryOperationListViewModel.cs` | **NEW** — List ViewModel, filterable by type (default: StockIssue) |
| `Views/InventoryOperations/InventoryOperationListView.xaml` | **NEW** — DataGrid with Type, No, Warehouse, Date, Status, Items count |
| `Views/InventoryOperations/InventoryOperationListView.xaml.cs` | **NEW** — Code-behind |
| `ViewModels/InventoryOperations/InventoryOperationEditorViewModel.cs` | **NEW** — Editor for Create/Edit/View operations |
| `Views/InventoryOperations/InventoryOperationEditorView.xaml` | **NEW** — Form with warehouse selector, items grid, notes |
| `Views/InventoryOperations/InventoryOperationEditorView.xaml.cs` | **NEW** — Code-behind |
| `Messaging/Messages/AppMessages.cs` | Add `InventoryOperationChangedMessage` |
| `App.xaml.cs` | DI registration + navigation |

**Editor ViewModel Pattern** (following StockTransferEditorViewModel pattern):
- Combobox for Warehouse (loaded on init)
- Product search + add to items grid
- Items DataGrid: Product, Quantity, UnitCost (optional), Notes
- Save as Draft → Post workflow
- Post button with stock validation
- ToolTips on ALL buttons (RULE-185)

**InventoryOperationListViewModel**:
- Default filter: `OperationType = InventoryOperationType.StockIssue` (1)
- Dropdown to switch between Issue/Receipt/Adjustment
- Newest-first sort (RULE-220): `OrderByDescending(x => x.Id)`
- DeleteStrategy for Draft operations (cancel only)

**UI Compact** (RULE-262-274):
- All inputs 28px via styles, no hardcoded heights
- Card padding: header `12,6`, footer `12,8`
- Section margins: `0,0,0,6`
- Dialog titles: `FontSize="16"`, section headers: `FontSize="14"`
- Empty-state: `Margin="0,12,0,0"` Width="140"

**Arabic ToolTips** (RULE-185-190):
- Add button: `"إنشاء عملية صرف مخزني جديدة"`
- Post button: `"ترحيل العملية — سيتم خصم الكميات من المخزون"`
- Add item: `"إضافة صنف للصرف"`
- Remove item: `"إزالة الصنف من القائمة"`
- Save Draft: `"حفظ كمسودة — لن يتم تغيير المخزون"`
- Cancel op: `"إلغاء العملية — عكس التغييرات إذا كانت مرحّلة"`

**Estimate**: ~4 hours

---

### Task 6 — Stock Receipt (توريد مخزني) — Desktop Screens

**Files**: Same as Task 5 but with `OperationType = StockReceipt` (2).

**Differences from Issue**:
- No stock validation (receipt always increases stock)
- UnitCost field is **visible and editable** (affects costing)
- Cannot "issue negative" — receipt is always positive quantity
- If UnitCost entered → calls `UpdateProductPricingService` to update weighted average
- Title: `"توريد مخزني"`, button texts: `"توريد جديد"`, `"ترحيل التوريد"`
- ToolTips adapted for receipt context

**Reuses**: Same ViewModels as Task 5 with an `OperationType` parameter passed to editor. No duplicate files.

**Editor constructor**:
```csharp
public InventoryOperationEditorViewModel(
    IInventoryOperationApiService operationService,
    IWarehouseApiService warehouseService,
    IProductApiService productService,
    IEventBus eventBus,
    IDialogService dialogService,
    byte operationType,  // 1=Issue, 2=Receipt, 3=Adjustment
    int? operationId = null)
```

**Estimate**: ~1 hour (reuses Task 5 infrastructure)

---

### Task 7 — Stock Transfer Enhancement (تحويل مخزني)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/StockTransfer.cs` | Add `ApprovedByUserId` (for future). Add `TransferType` enum (for future). Current flow unchanged for V1. |
| `Contracts/DTOs/AllDtos.cs` — `StockTransferDto` | Add `warehouseName` fallback handling |
| `Application/Services/InventoryService.cs` | Enhance `PostTransferAsync` with: (1) better logging including items count, (2) support for `TransferType`, (3) add `CreatedByUserId` in post log |
| `Api/Controllers/StockTransfersController.cs` | Add print endpoints: `GET /api/v1/stock-transfers/{id}/preview`, `POST /api/v1/stock-transfers/{id}/print` |
| `Application/Printing/Contracts/InvoicePrintDto.cs` | Add overload: `FromStockTransfer(StockTransferDto)` builder method in `InvoicePrintDtoBuilder` |
| `Infrastructure/Printing/A4InvoiceDocument.cs` | Add `StockTransferDocument` section — prints transfer document |
| **Desktop** — `ViewModels/Transfers/StockTransferEditorViewModel.cs` | Add print button, enhance notes field, add barcode scanning for item entry |
| **Desktop** — `Views/Transfers/StockTransferEditorView.xaml` | Add print button in toolbar, enhance items DataGrid with barcode column |
| **Desktop** — `ViewModels/Transfers/StockTransfersListViewModel.cs` | Add print command, add warehouse filter dropdowns |

**Estimate**: ~2 hours

---

### Task 8 — Stock Adjustment (تسوية مخزنية) — Desktop Screen

**Files**: Reuses same `InventoryOperationEditorViewModel` from Task 5 with `OperationType = Adjustment` (3).

**Differences from Issue/Receipt**:
- No stock validation (adjustment can go either direction)
- Quantity can be **positive or negative** (but NOT zero — must have direction)
- Positive quantity = increase stock (surplus found)
- Negative quantity = decrease stock (shortage found)
- `UnitCost` field visible — used for valuation impact
- Requires `AdminOnly` permission
- Warning before posting: `"سيتم تعديل رصيد المخزون مباشرة"`
- Title: `"تسوية مخزنية"`, button texts: `"تسوية جديدة"`, `"ترحيل التسوية"`
- **AdjustmentType selection required** — ComboBox with 4 sub-types (see §4.7):
  - افتتاحي (Opening) — opening stock entry
  - تالف (Damaged) — damaged/write-off
  - زيادة (Surplus) — inventory surplus
  - عجز (Shortage) — inventory shortage
- `AdjustmentType` field stored on `InventoryOperation` entity as `byte?` (only for Adjustment ops)

**Adjustment-specific ToolTips**:
- Save: `"حفظ التسوية كمسودة — لن يتم تغيير المخزون حتى الترحيل"`
- Post: `"ترحيل التسوية — سيتم تعديل أرصدة المخزون بشكل مباشر — هذا الإجراء لا يمكن التراجع عنه"`

**Estimate**: ~0.5 hours (reuses Task 5 infrastructure)

---

### Task 9 — Physical Count (جرد مخزني) — Desktop Screen ⏳ DEFERRED TO V2

> **V1 Direction**: Per Analysis Part 4:2957, Physical Count is a V2 feature. In V1, stock discrepancies should be handled through **Inventory Adjustment with sub-types** (see §4.7). The Adjustment operation's `Surplus` (زيادة) and `Shortage` (عجز) sub-types cover the same business need without the two-phase count flow.
>
> The following files are DESIGNED but will NOT be implemented in V1:

**Files**:

| File | Content |
|------|--------|
| `Domain/Enums/PhysicalCountStatus.cs` | **NEW** — `enum PhysicalCountStatus : byte { Draft=1, Completed=2, Applied=3 }` |
| `Domain/Entities/Inventory/PhysicalCount.cs` | **NEW** — Entity with 7 fields + navigation |
| `Domain/Entities/Inventory/PhysicalCountItem.cs` | **NEW** — Entity with 9 fields + `ComputeDifference()` |
| `Infrastructure/Data/Configurations/PhysicalCountConfiguration.cs` | **NEW** — Fluent API |
| `Infrastructure/Data/Configurations/PhysicalCountItemConfiguration.cs` | **NEW** — Fluent API |
| `Contracts/DTOs/AllDtos.cs` | Add `PhysicalCountDto`, `PhysicalCountItemDto` |
| `Contracts/Requests/PhysicalCountRequests.cs` | **NEW** — `CreatePhysicalCountRequest`, `AddCountItemRequest` |
| `Application/Interfaces/Services/IPhysicalCountService.cs` | **NEW** — Interface with 7 methods |
| `Application/Services/PhysicalCountService.cs` | **NEW** — Full two-phase implementation |
| `Api/Controllers/PhysicalCountsController.cs` | **NEW** — 8 endpoints |
| `Services/Api/IApiService.cs` | Add `IPhysicalCountApiService` |
| `Services/Api/PhysicalCountApiService.cs` | **NEW** — HTTP client |
| `ViewModels/Inventory/PhysicalCountListViewModel.cs` | **NEW** — List with status filtering |
| `Views/Inventory/PhysicalCountListView.xaml` | **NEW** — DataGrid |
| `ViewModels/Inventory/PhysicalCountEditorViewModel.cs` | **NEW** — Two-phase editor |
| `Views/Inventory/PhysicalCountEditorView.xaml` | **NEW** — Phase 1: product scanning + counting |
| `Views/Inventory/PhysicalCountEditorView.xaml.cs` | **NEW** — Code-behind |
| `ViewModels/Inventory/PhysicalCountCompareViewModel.cs` | **NEW** — Phase 2: review discrepancies |
| `Views/Inventory/PhysicalCountCompareView.xaml` | **NEW** — Phase 2 view with diff grid |
| `Messaging/Messages/AppMessages.cs` | Add `PhysicalCountChangedMessage` |
| `App.xaml.cs` | DI registration + navigation |

**PhysicalCountDto**:
```csharp
public record PhysicalCountDto(
    int Id,
    string CountNo,
    int WarehouseId,
    string WarehouseName,
    DateTime CountDate,
    string? Notes,
    byte Status,
    IReadOnlyList<PhysicalCountItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "جاري",
        2 => "مكتمل",
        3 => "تم التطبيق",
        _ => "غير معروف"
    };
    public int DiscrepancyCount => Items?.Count(i => i.Difference != 0) ?? 0;
    public bool HasDiscrepancies => DiscrepancyCount > 0;
}

public record PhysicalCountItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal SystemQuantity,
    decimal CountedQuantity,
    decimal Difference,
    decimal? UnitCost,
    string? Notes);
```

**PhysicalCountService — ApplyAsync Pattern**:
```csharp
public async Task<Result<PhysicalCountDto>> ApplyAsync(int id, int userId, CancellationToken ct)
{
    var count = await _uow.PhysicalCounts.FirstOrDefaultAsync(
        c => c.Id == id, ct, "Items.Product", "Warehouse");

    if (count == null)
        return Result<PhysicalCountDto>.Failure("الجرد غير موجود", ErrorCodes.NotFound);
    if (count.Status != PhysicalCountStatus.Completed)
        return Result<PhysicalCountDto>.Failure("يجب إكمال الجرد أولاً قبل التطبيق");
    if (!count.Items.Any(i => i.Difference != 0))
        return Result<PhysicalCountDto>.Failure("لا توجد فروقات لتطبيقها");

    return await _uow.ExecuteAsync(async () =>
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            foreach (var item in count.Items.Where(i => i.Difference != 0))
            {
                // Create InventoryOperation for each discrepancy
                var op = InventoryOperation.Create(
                    operationNo: GenerateOperationNo(), // Async call
                    warehouseId: count.WarehouseId,
                    operationType: InventoryOperationType.Adjustment,
                    notes: $"تسوية جرد {count.CountNo} — {item.Product?.Name}",
                    createdByUserId: userId);
                op.Post(); // Immediately post

                var opItem = InventoryOperationItem.Create(
                    op.Id, item.ProductId, item.Difference,
                    item.UnitCost,
                    $"الفرق: {item.Difference} (النظام: {item.SystemQuantity}, الجرد: {item.CountedQuantity})");
                op.AddItem(opItem);

                await _uow.InventoryOperations.AddAsync(op, ct);

                // Update stock
                var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                    ws => ws.WarehouseId == count.WarehouseId && ws.ProductId == item.ProductId, ct);
                var qtyBefore = stock?.Quantity ?? 0;
                var qtyAfter = qtyBefore + item.Difference;

                if (stock != null)
                    stock.SetQuantity(qtyAfter);
                else
                    await _uow.WarehouseStocks.AddAsync(
                        WarehouseStock.Create(count.WarehouseId, item.ProductId, qtyAfter), ct);

                // Log movement
                var movement = InventoryMovement.Create(
                    item.ProductId, count.WarehouseId,
                    MovementType.Adjustment, item.Difference,
                    qtyBefore, qtyAfter,
                    "PhysicalCount", count.Id,
                    item.UnitCost, null, userId);
                await _uow.InventoryMovements.AddAsync(movement, ct);
            }

            count.Apply();
            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "PhysicalCount {Id} applied — {Count} discrepancies adjusted",
                count.Id, count.DiscrepancyCount);

            return await GetByIdAsync(count.Id, ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error applying physical count {Id}", id);
            return Result<PhysicalCountDto>.Failure("حدث خطأ أثناء تطبيق الجرد");
        }
    }, ct);
}
```

**Physical Count UI Flow**:
1. User opens `PhysicalCountEditorView` — selects warehouse
2. System loads all products with stock balance → creates `PhysicalCountItem` records with `SystemQuantity` pre-filled
3. User scans barcodes or picks products → enters `CountedQuantity`
4. User clicks **Complete** → `PhysicalCount.Status = Completed`
5. System shows `PhysicalCountCompareView` — DataGrid with columns: Product, System Qty, Counted Qty, Difference
6. Differences highlighted: green (surplus), red (shortage)
7. User reviews and clicks **Apply** → adjustments created, stock updated
8. `PhysicalCount.Status = Applied` (terminal)

**Estimate**: ~6 hours

---

### Task 10 — Warehouse Stock Reports

**Files**:

| File | Content |
|------|--------|
| `ViewModels/Reports/StockBalanceReportViewModel.cs` | **NEW** — Per-warehouse stock balance report |
| `Views/Reports/StockBalanceReportView.xaml` | **NEW** — DataGrid with product, qty, reorder level, value |
| `Views/Reports/StockBalanceReportView.xaml.cs` | **NEW** |
| `ViewModels/Reports/WarehouseMovementReportViewModel.cs` | **NEW** — Movement history for a warehouse |
| `Views/Reports/WarehouseMovementReportView.xaml` | **NEW** — Date filter + DataGrid |
| `Views/Reports/WarehouseMovementReportView.xaml.cs` | **NEW** |
| `ViewModels/Inventory/LowStockViewModel.cs` | **ENHANCE** — Add warehouse filter dropdown |
| `Views/Inventory/LowStockView.xaml` | **ENHANCE** — Add warehouse filter combo |
| `Application/Services/ReportService.cs` | **ENHANCE** — Add `GetStockValuationAsync(warehouseId)` — returns Quantity × AvgCost per product |
| `Api/Controllers/ReportsController.cs` | **ENHANCE** — Add `GET /api/v1/reports/stock-valuation/{warehouseId}` |
| `Services/Api/IApiService.cs` | Add `GetStockValuationAsync` to `IReportApiService` |

**Stock Balance Report** — shows:
| Product | Warehouse | System Qty | Reorder Level | Avg Cost | Total Value |
|---------|-----------|------------|---------------|----------|-------------|
| منتج أ | رئيسي | 150 | 20 | 5.00 | 750.00 |
| منتج ب | رئيسي | 5 | 10 | 12.50 | 62.50 |

**Low stock = `Quantity < ReorderLevel`** (highlighted in orange/red).

**Movement Report** — shows:
| Date | Product | Movement Type | Qty Change | Before | After | Reference |
|------|---------|---------------|------------|-------|-------|-----------|
| 01/06 | منتج أ | PurchaseIn | +100 | 0 | 100 | PUR-2026-000001 |
| 02/06 | منتج أ | SaleOut | -30 | 100 | 70 | INV-2026-000005 |

**Estimate**: ~3 hours

---

### Task 11 — Low Stock Alert Display in Warehouse List

**Enhance existing `WarehouseListViewModel`**:

| File | Change |
|------|--------|
| `ViewModels/Warehouses/WarehouseListViewModel.cs` | Add `LowStockCount` per warehouse. Add total low stock badge. |
| `Views/Warehouses/WarehousesView.xaml` | Add Low Stock column in DataGrid. Add summary card in footer. |

**Pattern**: On load, call `_inventoryService.GetWarehouseStocksAsync(warehouseId)` for each warehouse → count items where `Quantity < ReorderLevel` → display count.

**Performance note**: For large warehouses (1000+ products), consider a dedicated API endpoint `GET /api/v1/warehouses/low-stock-summary` that returns a `Dictionary<int, int>` (warehouseId → lowStockCount) in a single query.

**Estimate**: ~1 hour

---

### Task 12 — API Endpoints + FluentValidation

**Summary of all new/enhanced API endpoints**:

| # | Method | Endpoint | Controller | Policy |
|---|--------|----------|------------|--------|
| 1 | `GET` | `/api/v1/inventory-operations` | InventoryOperations | `ManagerAndAbove` |
| 2 | `GET` | `/api/v1/inventory-operations/{id}` | InventoryOperations | `ManagerAndAbove` |
| 3 | `POST` | `/api/v1/inventory-operations` | InventoryOperations | `ManagerAndAbove` |
| 4 | `POST` | `/api/v1/inventory-operations/{id}/post` | InventoryOperations | `ManagerAndAbove` |
| 5 | `POST` | `/api/v1/inventory-operations/{id}/cancel` | InventoryOperations | `ManagerAndAbove` |
| 6 | `GET` | `/api/v1/physical-counts` | PhysicalCounts | `ManagerAndAbove` | **⏳ V2** |
| 7 | `GET` | `/api/v1/physical-counts/{id}` | PhysicalCounts | `ManagerAndAbove` | **⏳ V2** |
| 8 | `POST` | `/api/v1/physical-counts` | PhysicalCounts | `ManagerAndAbove` | **⏳ V2** |
| 9 | `POST` | `/api/v1/physical-counts/{id}/items` | PhysicalCounts | `ManagerAndAbove` | **⏳ V2** |
| 10 | `POST` | `/api/v1/physical-counts/{id}/complete` | PhysicalCounts | `ManagerAndAbove` | **⏳ V2** |
| 11 | `POST` | `/api/v1/physical-counts/{id}/apply` | PhysicalCounts | `AdminOnly` | **⏳ V2** |
| 12 | `GET` | `/api/v1/reports/stock-valuation/{warehouseId}` | Reports | `ManagerAndAbove` |
| 13 | `GET` | `/api/v1/reports/stock-movements/{warehouseId}` | Reports | `ManagerAndAbove` |
| 14 | `GET` | `/api/v1/warehouses/low-stock-summary` | Warehouses | `ManagerAndAbove` |

**FluentValidators** (RULE-044):

| Validator | Rules |
|-----------|-------|
| `CreateInventoryOperationRequestValidator` | WarehouseId > 0, OperationType 1-3, Items.Count > 0, each item: ProductId > 0, Quantity > 0, UnitCost >= 0 (if provided) |
| `CreatePhysicalCountRequestValidator` | WarehouseId > 0, CountDate <= today | **⏳ V2** |
| `AddCountItemRequestValidator` | ProductId > 0, CountedQuantity >= 0 | **⏳ V2** |
| `WarehouseRequestValidators` (enhance) | Type 1-4, Phone max 20, Address max 250, ManagerName max 100 |

**Estimate**: ~1.5 hours

---

### Task 13 — Navigation + DI Registration in Desktop

**Files**:

| File | Change |
|------|--------|
| `App.xaml.cs` | Register DI: `IInventoryOperationApiService`, InventoryOperation ViewModels, Report ViewModels. **PhysicalCountApiService and PhysicalCount ViewModels are V2 — skip DI registration in V1.** All `AddTransient<T>()`. |
| MainWindow navigation | Add menu entries for: عمليات مخزنية → صرف مخزني, توريد مخزني, تسوية مخزنية, تحويل مخزني. **جرد مخزني deferred to V2** (see §7). Add Reports → كشف رصيد المخازن, حركة المخازن. |

**Navigation Menu Structure**:
```
🏭 المخازن
   ├── 🏭 إدارة المستودعات (existing)
   ├── 📦 عمليات مخزنية
   │   ├── 📤 صرف مخزني (NEW)
   │   ├── 📥 توريد مخزني (NEW)
   │   ├── 🔄 تحويل مخزني (existing)
   │   ├── ⚖️ تسوية مخزنية (NEW)
    │   └── 📋 جرد مخزني (V2 ⏳)
   └── 📊 تقارير المخازن
       ├── 📋 كشف رصيد المخازن (NEW)
       ├── 📈 حركة المخازن (NEW)
       └── ⚠️ إنذار المخزون (existing, enhance)
```

**Navigation ViewModel registration pattern**:
```csharp
// In App.xaml.cs ConfigureServices:
services.AddTransient<InventoryOperationListViewModel>();
services.AddTransient<InventoryOperationEditorViewModel>();
services.AddTransient<PhysicalCountListViewModel>();
services.AddTransient<PhysicalCountEditorViewModel>();
services.AddTransient<PhysicalCountCompareViewModel>();
services.AddTransient<StockBalanceReportViewModel>();
services.AddTransient<WarehouseMovementReportViewModel>();
services.AddTransient<LowStockViewModel>(); // Already exists

services.AddSingleton<IInventoryOperationApiService, InventoryOperationApiService>();
services.AddSingleton<IPhysicalCountApiService, PhysicalCountApiService>();
```

**Navigation command pattern**:
```csharp
private void NavigateToInventoryOperationList(object param)
{
    byte operationType = (byte)param; // 1=Issue, 2=Receipt, 3=Adjustment
    var vm = App.GetService<InventoryOperationListViewModel>();
    vm.SetOperationTypeFilter(operationType);
    OpenScreen(vm, new ScreenWindowOptions
    {
        Title = operationType switch
        {
            1 => "صرف مخزني",
            2 => "توريد مخزني",
            3 => "تسوية مخزنية",
            _ => "عمليات مخزنية"
        }
    });
}
```

**Estimate**: ~1 hour

---

### Task 14 — Comprehensive Unit Tests (Warehouses + Inventory Operations)

**Test projects**:
- `SalesSystem.Domain.Tests/Entities/Inventory/`
- `SalesSystem.Application.Tests/Services/`
- `SalesSystem.Api.Tests/Validators/`
- `SalesSystem.Infrastructure.Tests/Configurations/`

---

#### 14.1 Domain Entity Tests

**WarehouseTests.cs** — Test `Warehouse.Create()`:
| Test | Expected |
|------|----------|
| Valid input → creates with Name, Type, Phone, Address, ManagerName, AccountId, Notes | Passes |
| `null` Name → `DomainException("اسم المستودع مطلوب")` | Throws |
| Empty Name → `DomainException("اسم المستودع مطلوب")` | Throws |
| Name > 200 chars → `DomainException("اسم المستودع يتجاوز 200 حرف")` | Throws |
| Negative AccountId → `DomainException("حساب الأستاذ غير صالح")` | Throws |
| Valid WarehouseType enum (Main=1, Store=2, Showroom=3, Transit=4) → stored correctly | Passes |
| `SetAccountId(0)` → `DomainException("حساب الأستاذ غير صالح")` | Throws |
| `SetAccountId(null)` → allowed (nullable) | Passes |
| `SetPhone("+966501234567")` → stored correctly | Passes |
| `SetPhone("invalid")` → `DomainException("رقم الجوال غير صالح")` | Throws |
| `UpdateDetails(Name, Phone, Address, ManagerName)` with valid data → all fields updated | Passes |
| `UpdateDetails` with null Name → `DomainException("اسم المستودع مطلوب")` | Throws |

**InventoryOperationTests.cs** — Test `InventoryOperation.Create()`:
| Test | Expected |
|------|----------|
| Valid Issue operation → Type=1, Status=Draft | Passes |
| Valid Receipt operation → Type=2, Status=Draft | Passes |
| Valid Adjustment operation → Type=3, Status=Draft | Passes |
| Zero items → `DomainException("يجب إضافة صنف واحد على الأقل")` | Throws |
| Null WarehouseId → `DomainException("المستودع مطلوب")` | Throws |
| `OperationDate == default` → `DomainException("تاريخ العملية مطلوب")` | Throws |
| `Notes > 500 chars` → `DomainException("الملاحظات تتجاوز 500 حرف")` | Throws |
| `ReferenceNo > 100 chars` → `DomainException("الرقم المرجعي يتجاوز 100 حرف")` | Throws |

**InventoryOperationItemTests.cs**:
| Test | Expected |
|------|----------|
| Valid item with ProductId, Quantity, UnitCost → LineTotal = Qty × UnitCost | Passes |
| Quantity = 0 → `DomainException("الكمية يجب أن تكون أكبر من الصفر")` | Throws |
| Quantity = -1 → `DomainException("الكمية يجب أن تكون أكبر من الصفر")` | Throws |
| UnitCost = -5 → `DomainException("التكلفة لا يمكن أن تكون سالبة")` | Throws |
| ProductId = 0 → `DomainException("المنتج مطلوب")` | Throws |
| Null UnitBarcode → allowed (nullable) | Passes |

**AdjustmentType enum tests**:
| Test | Expected |
|------|----------|
| Opening=1 → represents initial stock entry | Correct value |
| Damaged=2 → represents damaged/spoiled stock write-off | Correct value |
| Surplus=3 → represents unexpected surplus found | Correct value |
| Shortage=4 → represents missing stock discovered | Correct value |

**StockIssueReason enum tests**:
| Test | Expected |
|------|----------|
| Damaged=1 → used for damaged goods removal | Correct value |
| InternalUse=2 → used for internal consumption | Correct value |
| FreeSample=3 → used for promotional samples | Correct value |
| Other=4 → used for unclassified reasons | Correct value |

**InventoryOperation.Post() and Cancel() behavior**:
| Test | Expected |
|------|----------|
| `Post()` on Draft operation → Status=Posted, CanBePosted=true → CanBeCancelled=true | Passes |
| `Post()` on already Posted operation → `DomainException("لا يمكن ترحيل عملية مرحلة")` | Throws |
| `Cancel()` on Draft operation → Status=Cancelled (no stock impact) | Passes |
| `Cancel()` on Posted operation → Status=Cancelled (reverses stock) | Passes |
| `Cancel()` on already Cancelled → `DomainException("لا يمكن إلغاء عملية ملغاة")` | Throws |
| Lifecycle transitions: Draft→Posted→Cancelled (valid), Posted→Draft (forbidden) | Verified |

**Stock Transfer domain tests**:
| Test | Expected |
|------|----------|
| `TransferOut` operation creates Out movement from SourceWarehouseId | Passes |
| `TransferIn` operation creates In movement to DestinationWarehouseId | Passes |
| TransferOut must precede TransferIn (two-step) | Verified |
| SourceWarehouseId ≠ DestinationWarehouseId enforced | `DomainException` |
| Transfer with insufficient source stock → validation prevents posting | Passes |

**WarehouseStocks domain constraint**:
| Test | Expected |
|------|----------|
| DeductStock(Quantity > CurrentQuantity) → `DomainException("الرصيد غير كافٍ")` | Throws |
| DeductStock(Quantity <= CurrentQuantity) → CurrentQuantity reduced | Passes |
| AddStock(Quantity) → CurrentQuantity increased by Quantity | Passes |
| `CurrentQuantity` never negative (domain enforcement) | Verified |

---

#### 14.2 Service Tests (using `Mock<IUnitOfWork>`)

**InventoryOperationServiceTests.cs**:
| Test | Expected |
|------|----------|
| `CreateAsync(validRequest)` → `Result<InventoryOperationDto>.Success` | Passes |
| `CreateAsync(null)` → `Result<InventoryOperationDto>.Failure` with error | Passes |
| `CreateAsync` with invalid items → `Result.Failure` | Passes |
| `PostAsync(operationId)` → stock deducted + status changed → Success | Passes |
| `PostAsync` with insufficient stock → `Result.Failure("الرصيد غير كافٍ")` | Passes |
| `PostAsync` with non-existent operation → `Result.Failure("العملية غير موجودة")` | Passes |
| `CancelAsync(operationId)` → stock reversed + status Cancelled → Success | Passes |
| `CancelAsync` on non-existent operation → `Result.Failure` | Passes |
| Transaction rollback: if stock update fails, operation stays in DB with original status | Verified |
| Transaction rollback: if `SaveChangesAsync` throws after stock update, ALL changes undone | Verified |
| `GetByIdAsync(id)` returns correct dto → Success | Passes |
| `GetByIdAsync(0)` → `Result.Failure("المعرف غير صالح")` | Passes |
| `GetPagedAsync(...)` returns paginated results | Passes |

**Stock Transfer Service tests**:
| Test | Expected |
|------|----------|
| `TransferAsync(sourceWhId, destWhId, items)` → TransferOut created then TransferIn | Passes |
| Transfer with insufficient source stock → `Result.Failure` before transaction | Passes |
| Transfer with SourceWarehouseId == DestinationWarehouseId → `Result.Failure` | Passes |
| Transaction rollback on transfer failure → no partial stock changes | Verified |

**WarehouseService enhanced tests**:
| Test | Expected |
|------|----------|
| `CreateAsync(validRequest)` → `Result<WarehouseDto>.Success` | Passes |
| `CreateAsync` with duplicate Name → `Result.Failure("اسم المستودع موجود مسبقاً")` | Passes |
| `UpdateAsync(id, validRequest)` → updates all fields | Passes |
| `DeleteAsync(id)` → soft delete (IsActive=false) | Passes |
| `DeletePermanentlyAsync(id)` with existing FK refs → catches `DbUpdateException` → Arabic Result.Failure | Passes |
| `GetLowStocksAsync()` → returns products below reorder level | Passes |

---

#### 14.3 FluentValidation Tests

**CreateWarehouseRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request → IsValid=true | Passes |
| Name empty → validation error "اسم المستودع مطلوب" | Passes |
| Name > 200 chars → validation error "اسم المستودع يجب أن لا يتجاوز 200 حرف" | Passes |
| Type out of range → validation error for Invalid enum | Passes |
| Phone invalid format → validation error "رقم الجوال غير صالح" (optional) | Passes |
| Notes > 500 chars → validation error "الملاحظات يجب أن لا تتجاوز 500 حرف" | Passes |

**CreateInventoryOperationRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid request (Type=1, at least 1 item) → IsValid=true | Passes |
| Items empty → validation error "يجب إضافة صنف واحد على الأقل" | Passes |
| Item Quantity ≤ 0 → validation error "الكمية يجب أن تكون أكبر من الصفر" | Passes |
| Item UnitCost < 0 → validation error "التكلفة لا يمكن أن تكون سالبة" | Passes |
| Item ProductId = 0 → validation error "المنتج مطلوب" | Passes |
| OperationDate empty → validation error "تاريخ العملية مطلوب" | Passes |
| WarehouseId = 0 → validation error "المستودع مطلوب" | Passes |

**StockTransferRequestValidatorTests.cs**:
| Test | Expected |
|------|----------|
| Valid transfer request → IsValid=true | Passes |
| SourceWarehouseId == DestinationWarehouseId → validation error "المستودع المصدر والمستهدف لا يمكن أن يكونا نفس المستودع" | Passes |
| Items empty → validation error "يجب إضافة صنف واحد على الأقل" | Passes |
| Item Quantity ≤ 0 → validation error "الكمية يجب أن تكون أكبر من الصفر" | Passes |

---

#### 14.4 Database Configuration Tests

**WarehouseConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Name has `HasMaxLength(200)` + `IsRequired()` | Verified |
| Type has `HasConversion<int>()` to store enum as int | Verified |
| Phone has `HasMaxLength(20)` | Verified |
| Address has `HasMaxLength(500)` | Verified |
| ManagerName has `HasMaxLength(200)` | Verified |
| AccountId has FK to `Accounts(Id)` with `DeleteBehavior.Restrict` | Verified |
| Notes has `HasMaxLength(500)` | Verified |
| `HasQueryFilter(x => x.IsActive)` for soft delete | Verified |

**InventoryOperationConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Type stored as int with CHECK constraint `[Type] IN (1,2,3)` | Verified |
| Status stored as int: Draft=1, Posted=2, Cancelled=3 | Verified |
| OperationNo has `HasMaxLength(50)` + `IsRequired()` | Verified |
| WarehouseId FK → `DeleteBehavior.Restrict` | Verified |
| Notes has `HasMaxLength(500)` | Verified |
| ReferenceNo has `HasMaxLength(100)` | Verified |
| OperationDate is `IsRequired()` | Verified |
| `HasQueryFilter(x => x.IsActive)` | Verified |

**InventoryOperationItemConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| Quantity has `HasPrecision(18, 3)` + CHECK `(Quantity > 0)` | Verified |
| UnitCost has `HasPrecision(18, 2)` | Verified |
| LineTotal has `HasPrecision(18, 2)` | Verified |
| ProductId FK → `DeleteBehavior.Restrict` | Verified |
| OperationId FK → `DeleteBehavior.Restrict` | Verified |
| UnitBarcode has `HasMaxLength(50)` | Verified |

**StockTransferConfigurationTests.cs**:
| Test | Expected |
|------|----------|
| SourceWarehouseId FK → `DeleteBehavior.Restrict` | Verified |
| DestinationWarehouseId FK → `DeleteBehavior.Restrict` | Verified |
| TransferDate `IsRequired()` | Verified |
| ReferenceNo `HasMaxLength(100)` | Verified |

**PhysicalCountConfigurationTests.cs** (V2 Deferred — design only):
| Test | Expected |
|------|----------|
| Status stored as int: Draft=1, Completed=2, Applied=3 | Verified (design) |
| CountDate `IsRequired()` | Verified (design) |
| Notes `HasMaxLength(500)` | Verified (design) |

---

#### 14.5 Phase-Specific Tests

| # | Test Area | Test Case | Expected |
|---|-----------|-----------|----------|
| 1 | **Warehouse.AccountId** | Create warehouse linked to valid CoA account | FK constraint satisfied |
| 2 | **Warehouse.AccountId null** | Create warehouse without AccountId | Allowed (nullable FK) |
| 3 | **Warehouse.Type** | All 4 enum values (Main, Store, Showroom, Transit) | Stored/mapped correctly |
| 4 | **AdjustmentType.Opening=1** | Accounting treatment: Debit Inventory, Credit RetainedEarnings | Correct mapping |
| 5 | **AdjustmentType.Damaged=2** | Accounting treatment: Debit Loss, Credit Inventory | Correct mapping |
| 6 | **AdjustmentType.Surplus=3** | Accounting treatment: Debit Inventory, Credit Income | Correct mapping |
| 7 | **AdjustmentType.Shortage=4** | Accounting treatment: Debit Loss, Credit Inventory (same as Damaged) | Correct mapping |
| 8 | **StockIssueReason.Damaged=1** | Issue reason stored correctly | Correct value |
| 9 | **StockIssueReason.InternalUse=2** | Issue reason stored correctly | Correct value |
| 10 | **StockIssueReason.FreeSample=3** | Issue reason stored correctly | Correct value |
| 11 | **StockIssueReason.Other=4** | Issue reason stored correctly | Correct value |
| 12 | **InventoryOperation.MovementType** | Issue→SaleOut, Receipt→PurchaseIn, Adjustment→Adjustment | Correct mapping |
| 13 | **Stock Transfer two-step** | TransferOut → TransferIn sequence | Both must succeed or rollback |
| 14 | **WarehouseStocks.Quantity >= 0** | Domain guard on DeductStock | `DomainException` when qty > available |
| 15 | **Physical Count V1** | No V1 tests — fully deferred to V2 | No tests written |

**Estimate**: ~8 hours (2h Domain, 2h Service, 1h Validation, 2h Configuration, 1h Phase-specific)

---

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | InventoryOperationItem.UnitCost = `HasPrecision(18,2)` | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | InventoryOperationItem.Quantity, PhysicalCountItem.SystemQuantity/CountedQuantity/Difference — all `HasPrecision(18,3)` | ✅ |
| **RULE-003** | Multi-table ops in transaction | InventoryOperationService.PostAsync — BeginTransactionAsync wrapping stock + movement + operation save | ✅ |
| **RULE-005** | Stock deducted AFTER save with ID | InventoryOperationService: save Operation first → get ID → deduct stock → commit | ✅ |
| **RULE-006** | ALL services return `Result<T>` | InventoryOperationService, PhysicalCountService | ✅ |
| **RULE-007** | Desktop calls API via HttpClient | InventoryOperationApiService, PhysicalCountApiService (HTTP client services) | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | OperationNo, Notes, ReferenceNo, Reason — all `nvarchar` | ✅ |
| **RULE-010** | CHECK (Quantity >= 0) on WarehouseStocks | Already exists in WarehouseStockConfiguration | ✅ Existing |
| **RULE-016** | BaseEntity audit fields | All new entities inherit BaseEntity (CreatedAt, CreatedByUserId, IsActive) | ✅ |
| **RULE-022** | Controllers delegate to Services | InventoryOperationsController → IInventoryOperationService only | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | InventoryOperationService, PhysicalCountService | ✅ |
| **RULE-028** | Record EVERY stock change in InventoryMovements | All Post operations create InventoryMovement per item | ✅ |
| **RULE-029** | InventoryMovement stores: ProductId, WarehouseId, MovementType, QtyChange, QtyBefore, QtyAfter, ReferenceType, ReferenceId | All InventoryMovement.Create calls supply all 8 fields | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on create/post/cancel | ✅ |
| **RULE-036** | Log critical operations | Stock changes, operation posts, physical count apply | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets in logs | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All controllers have policy attributes | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | InventoryOperation.Post(), PhysicalCount.Apply(), PhysicalCountItem.ComputeDifference() | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | CreateInventoryOperationRequestValidator, CreatePhysicalCountRequestValidator, enhanced Warehouse validators | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | InventoryOperation: Cancel only (no hard delete). PhysicalCount: Cancel (Draft only) | ✅ |
| **RULE-052** | Guard Clauses on all entities | InventoryOperation.Create, PhysicalCount.Create, all item Creates — Arabic DomainException | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "الكمية يجب أن تكون أكبر من الصفر", "المستودع مطلوب" | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All new ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | All Editor ViewModels | ✅ |
| **RULE-059** | Save always enabled, validate on click | No CanExecute predicates on Save/Post commands | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All new ViewModels | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | All editors open via `OpenScreen()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في ترحيل الصرف"`, `"خطأ في تطبيق الجرد"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, post/cancel crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Validation errors, insufficient stock, "not found" | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | InventoryOperationApiService, PhysicalCountApiService — content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, MenuItems, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | `"ترحيل العملية — سيتم خصم الكميات من المخزون"` ✅ | ✅ |
| **RULE-187** | Action buttons explain consequences | Post: "ترحيل العملية — سيتم تحديث المخزون والرصيد" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | `"إدارة عمليات الصرف المخزني — إخراج أصناف من المخزن"` | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | `"➕ إنشاء أول عملية صرف مخزني"` | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | `"إخفاء رسالة الخطأ"` | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | InventoryOperation cancel catches FK violations | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | InventoryOperationService, PhysicalCountService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | All new controllers inject services only | ✅ |
| **RULE-210** | CHECK constraints at DB level | `CHK_InventoryOperations_Type_Range`, `CHK_InventoryOperationItems_Qty_Positive`, `CHK_PhysicalCountItems_Difference` | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | All new FK configurations: Restrict | ✅ |
| **RULE-220** | Newest-first sorting on lists | All list VMs: `OrderByDescending(x => x.Id)` | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | All Editor ViewModel constructors | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All Editor VMs use AddError/ClearErrors | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in all Editor VMs | ✅ |
| **RULE-240** | Rate limiting on login | Not affected — new endpoints use existing middleware | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not affected | ✅ N/A |
| **RULE-254** | InvoiceNo as int | Not affected | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | All dialog windows | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All section headers | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state views | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | All screen windows | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **InventoryOperation entity overlaps with existing StockWriteOff** | **Medium** — StockWriteOff already tracks write-offs (damaged/lost stock). New InventoryOperation covers issue/receipt/adjust. Clear distinction: WriteOff = damaged goods only, Issue = any stock out (including write-offs if needed). Document the boundary. |
| **Post operation fails mid-way (some stock changed, some not)** | **HIGH** — Transaction wraps ALL stock changes. If any item fails, the entire transaction rolls back (RULE-003). No partial commits. |
| **Physical count with 2000+ products causes long loading** | **Medium** — Use paginated loading for PhysicalCountItem creation. Load products in batches of 50. Show progress bar during initial load. |
| **Adjustment creates InventoryMovement with MovementType.Adjustment — conflicts with existing Adjustment references** | Low | Existing Adjustment movements reference different ReferenceTypes (invoice IDs). The new `ReferenceType = "InventoryOperation"` distinguishes standalone adjustments. |
| **Warehouse Type migration conflicts with existing data** | Low | `ALTER TABLE ADD Type int NOT NULL DEFAULT 1` (Main = 1 for all existing). Fully backwards-compatible. |
| **AccountId FK future-proofing — Account table might not exist yet** | Low | FK is nullable (`int?`). No constraint enforced until Account table is created in Phase 27. Consider adding FK only when Account table exists. |
| **Performance: GetWarehouseStocksAsync called per warehouse in low-stock summary** | Medium | Create dedicated `GET /api/v1/warehouses/low-stock-summary` endpoint that runs a single grouped query: `SELECT WarehouseId, COUNT(*) FROM WarehouseStocks WHERE Quantity < ReorderLevel GROUP BY WarehouseId`. |
| **Desktop ViewModel performance — loading many transfer/operation records** | Low | All list endpoints are paginated via `PagedResult<T>`. Default page size = 20. No unbounded loads. |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| InventoryOperation entity not needed | `DROP TABLE InventoryOperationItems; DROP TABLE InventoryOperations;` Remove all files from Tasks 2-6. |
| PhysicalCount V2 scope | No action needed — Physical Count is already deferred to V2. All Task 9 files are DESIGNED but NOT implemented in V1. |
| Warehouse Type field causes issues | `ALTER TABLE Warehouses DROP COLUMN Type;` (or keep with DEFAULT 1) |
| Warehouse Phone/Address/ManagerName not needed | `ALTER TABLE Warehouses DROP COLUMN Phone, Address, ManagerName;` |
| AccountId FK causes migration errors | Remove FK from Warehouse entity. Keep field in entity as `int?` without FK constraint. |
| Stock movement report duplicates existing functionality | Remove report Views. Existing `GetMovementsAsync` API + existing `SalesSystem.DesktopPWF.ViewModels.ReportsViewModel` already covers product movement. |
| New InventoryOperation API endpoints not needed | Remove InventoryOperationsController. Revert DI registration. |
| Navigation changes cause app startup failure | Remove navigation entries from MainWindow. Keep physical files — they're only instantiated on navigation. |

**Quick revert commands** (if migration not yet applied):
```powershell
# Remove migration
Remove-Migration -Name "Phase26_*" -Project SalesSystem.Infrastructure

# Revert code changes
git checkout -- SalesSystem.Domain/Entities/Inventory/
git checkout -- SalesSystem.Application/Services/InventoryOperationService.cs
git checkout -- SalesSystem.Application/Services/PhysicalCountService.cs
git checkout -- SalesSystem.Api/Controllers/InventoryOperationsController.cs
git checkout -- SalesSystem.Api/Controllers/PhysicalCountsController.cs

# Revert Desktop changes
git checkout -- SalesSystem.DesktopPWF/ViewModels/InventoryOperations/
git checkout -- SalesSystem.DesktopPWF/ViewModels/Inventory/
git checkout -- SalesSystem.DesktopPWF/Views/InventoryOperations/
git checkout -- SalesSystem.DesktopPWF/Views/Inventory/
git checkout -- SalesSystem.DesktopPWF/ViewModels/Reports/StockBalance*
git checkout -- SalesSystem.DesktopPWF/ViewModels/Reports/WarehouseMovement*
git checkout -- SalesSystem.DesktopPWF/Views/Reports/StockBalance*
git checkout -- SalesSystem.DesktopPWF/Views/Reports/WarehouseMovement*

# Clean re-add: restore modified files selectively
git checkout -- SalesSystem.Domain/Entities/Warehouse.cs  # Only if Warehouse entity changes need reverting
```
