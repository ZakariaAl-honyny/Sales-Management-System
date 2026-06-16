# Audit Report — Module 5 Schema Alignment

## Files Fixed (Compilation Passing)

| File | Changes |
|------|---------|
| `Domain/Entities/InventoryBatch.cs` | BatchNo→string, ExpiryDate→DateOnly?, PurchaseInvoiceLineId |
| `Domain/Entities/InventoryTransaction.cs` | Entity base, MovementType, string TransactionNo, no Status |
| `Domain/Entities/InventoryTransactionLine.cs` | ProductUnitId, BatchNo(string), ExpiryDate(DateOnly?), WarehouseId |
| `Domain/Entities/InventoryAdjustment.cs` | Entity base, Reason, string AdjustmentNo, PostedAt/CancelledAt |
| `Domain/Entities/InventoryAdjustmentLine.cs` | ProductUnitId, ExpectedQuantity, ActualQuantity |
| `Domain/Entities/InventoryCount.cs` | Entity base, string CountNo, no CountDate/PostedAt |
| `Domain/Entities/InventoryCountLine.cs` | ProductUnitId, ExpectedQuantity, Difference, Notes |
| `Domain/Entities/WarehouseTransfer.cs` | Entity base, string TransferNo, SourceDestination warehouse IDs |
| `Domain/Entities/WarehouseTransferLine.cs` | ProductUnitId, BatchNo(string) |
| `Domain/Enums/InventoryAdjustmentType.cs` | Addition=1, Deduction=2, Correction=3 |
| `Infrastructure/Configurations/` (9 files) | Updated to match new entity shapes |
| `Contracts/DTOs/AllDtos.cs` | InventoryTransaction, WarehouseTransfer DTOs updated |
| `Contracts/Responses/InventoryBatchDto.cs` | PurchaseInvoiceLineId, QuantityReceived, SupplierBatchNo |
| `Contracts/Responses/InventoryCountDto.cs` | Already correct — CountNo string, ProductUnitId, Expected/Actual |
| `Contracts/Responses/InventoryAdjustmentDto.cs` | Already correct — AdjustmentNo string, Reason, ProductUnitId |
| `Contracts/Requests/*` | All 4 request files updated |
| `Application/Services/InventoryService.cs` | Rewritten — uses new entities |
| `Application/Services/InventoryCountService.cs` | Updated — already uses new shapes |
| `Application/Services/InventoryAdjustmentService.cs` | Rewritten — uses new entities |
| `Application/Services/InventoryBatchService.cs` | Fixed Create() params and MapToDto() |
| `Api/Validators/*` (3 files) | Updated validation rules |
| `Api/Validators/InventoryBatchRequestsValidator.cs` | New file |
| `Api/Controllers/*` (4 files) | No changes needed — delegate to services |

## Files Still Needing Fixes (Desktop ViewModels)

### 1. `InventoryCountEditorViewModel.cs`
- `_countNo`: `int` → `string`
- Remove `_countDate` field and `CountDate` property (removed from entity)
- Line 103: `_countDate = count.CountDate` → remove
- Lines 359, 411: Remove `CountDate` from BuildRequest/BuildUpdateRequest
- Line items: `ProductUnitId` not `ProductId`, `ExpectedQuantity`/`ActualQuantity` not `Quantity`

### 2. `InventoryAdjustmentEditorViewModel.cs`
- `_adjustmentNo`: `int` → `string`
- Remove `_adjustmentDate` field and `AdjustmentDate` property
- `_notes` → `_reason` (DTO field is `Reason`)
- Line 96: `_adjustmentDate = adjustment.AdjustmentDate` → remove
- Lines 106-108: Line items use `ProductUnitId`, `ProductUnitName`, `ExpectedQuantity`/`ActualQuantity`
- `UnitCost` removed from DTO → keep only on `InventoryAdjustmentLineDto`
- Lines 340, 377: Remove `AdjustmentDate` from build requests

### 3. `WarehouseTransferEditorViewModel.cs`
- Lines 252-254: `FromWarehouseId`→`SourceWarehouseId`, `ToWarehouseId`→`DestinationWarehouseId`, remove `TransferDate`
- Lines 263-268: Line items use `ProductUnitId` not `ProductId`, remove `BatchId`, remove `UnitCost`

### 4. `InventoryTransactionListViewModel.cs`
- Lines 47, 115, 127, 221: `TransactionType`→`MovementType`

### 5. `InventoryTransactionEditorViewModel.cs`
- Lines 71, 79, 87, 91: `TransactionType`→`MovementType`

## Build Status
- API + Application + Infrastructure + Domain projects: ✅ Compilable  
- DesktopPWF project: ❌ 5 ViewModels still reference old property names
