# Quickstart — Products Module Remaining Work

## Implementation Order

The tasks should be implemented in **dependency order**:

### Phase A: Domain & Schema Changes (Foundation)

| Order | Task | Files | Description |
|-------|------|-------|-------------|
| A1 | Rename `HasExpiry` → `TrackExpiry` | `Product.cs`, `ProductDto.cs`, `ProductService.cs`, Tests | Property rename across ALL layers |
| A2 | Remove `Product.Cost` + factory method `UpdateCost()` | `Product.cs` | Domain entity cleanup |
| A3 | Delete `ProductBarcode` entity + config | `ProductBarcode.cs`, `ProductBarcodeConfiguration.cs` | Remove deprecated entity |
| A4 | Remove `ProductBarcodes` from DbContext + UnitOfWork + IUnitOfWork | `SalesDbContext.cs`, `UnitOfWork.cs`, `IUnitOfWork.cs` | ORM cleanup |
| A5 | Create migration `Phase28` | Infrastructure/Migrations | Single combined migration |

### Phase B: Service Layer (Business Logic)

| Order | Task | Files | Description |
|-------|------|-------|-------------|
| B1 | Create `IProductCostService` + `ProductCostService` | New files in Application | Weighted avg + FIFO cost from batches |
| B2 | Replace ALL `Product.Cost` reads with `ProductCostService` | SalesService, AccountingIntegrationService, BarcodeLookupService, ReportRepository, Desktop VMs | 30+ reference migration |
| B3 | Add opening fields to `CreateProductRequest` | `ProductRequests.cs` | OpeningQuantity, OpeningUnitCost, OpeningExpiryDate |
| B4 | Update `ProductService.CreateAsync()` with opening batch logic | `ProductService.cs` | Default warehouse → InventoryBatch → WarehouseStock → InventoryMovement |
| B5 | Add `CreateProductOpeningEntryAsync` to `AccountingIntegrationService` | `AccountingIntegrationService.cs` | Dr Inventory / Cr OpeningBalanceEquity |
| B6 | Add `CreateProductRequestValidator` opening field rules | `Validators` | Qty > 0, cost >= 0, expiry required when TrackExpiry |

### Phase C: Desktop UI (User Experience)

| Order | Task | Files | Description |
|-------|------|-------|-------------|
| C1 | Complete `ProductImportView.xaml` bindings | `ProductImportView.xaml` | File selector, preview grid, results |
| C2 | Add opening stock fields to `ProductEditorViewModel` | `ProductEditorViewModel.cs` | OpeningQuantity, OpeningUnitCost, OpeningExpiryDate |
| C3 | Add opening stock section to `ProductEditorView.xaml` | `ProductEditorView.xaml` | New fields in create mode, hidden in edit |
| C4 | Update `ProductDto` → remove `Cost`, rename `HasExpiry` → `TrackExpiry` | `AllDtos.cs` | Contract change |

### Phase D: Tests

| Order | Task | Files | Description |
|-------|------|-------|-------------|
| D1 | Update Domain tests | `ProductTests.cs` | Remove Cost tests, TrackExpiry rename |
| D2 | Update Application tests | `ProductServiceTests.cs`, `SalesServiceTests.cs` | Opening batch tests, cost service tests |
| D3 | Update API tests | `CreateProductRequestValidatorTests.cs` | Opening field validation |
| D4 | Update Desktop tests | `ProductEditorViewModelTests.cs` | Opening field initialization |

---

## Key Patterns

### Transaction Pattern (ProductService.CreateAsync)

```csharp
public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
{
    try
    {
        // 1. Domain factory
        var product = Product.Create(request.Name, request.CategoryId, request.MinStock, 0,
            request.TrackExpiry ?? false, request.Barcode, request.Description);
        
        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);  // Get Product.Id
        
        // 2. Opening stock
        if (request.OpeningQuantity > 0)
        {
            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Default warehouse
                var warehouses = await _uow.Warehouses.GetAllAsync(ct);
                var defaultWarehouse = warehouses.FirstOrDefault(w => w.IsDefault) ?? warehouses.First();
                
                // Create batch
                var batch = InventoryBatch.Create(
                    product.Id, defaultWarehouse.Id,
                    request.OpeningQuantity.Value,
                    request.OpeningUnitCost ?? 0m,
                    "OPENING",
                    expiryDate: request.OpeningExpiryDate);
                await _uow.InventoryBatches.AddAsync(batch, ct);
                
                // Update stock
                await _inventoryService.IncreaseStockAsync(
                    product.Id, defaultWarehouse.Id,
                    request.OpeningQuantity.Value,
                    MovementType.Adjustment,
                    "Product", product.Id,
                    request.OpeningUnitCost ?? 0m, userId, ct);
                
                // Journal entry
                if (request.OpeningUnitCost > 0)
                {
                    var totalValue = request.OpeningQuantity.Value * request.OpeningUnitCost.Value;
                    await _accountingService.CreateProductOpeningEntryAsync(
                        product.Id, product.Name, totalValue, userId, DateTime.UtcNow, ct);
                }
                
                await _uow.SaveChangesAsync(ct);
            }, ct);
        }
        
        _logger.LogInformation("Product created: {ProductId}", product.Id);
        return await GetByIdAsync(product.Id, ct);
    }
    catch (Exception ex) when (ex is DomainException or ArgumentException)
    {
        return Result<ProductDto>.Failure(ex.Message);
    }
}
```

### Product Cost Query (Replacement Pattern)

```csharp
// BEFORE — reading from Product.Cost:
var cost = item.Product.Cost;

// AFTER — querying from batches:
var costResult = await _productCostService.GetAverageCostAsync(item.ProductId, ct);
var cost = costResult.IsSuccess ? costResult.Value : 0m;
```

### Cost for COGS (FIFO Layers)

```csharp
// In SalesService.PostAsync():
var layers = await _productCostService.GetFifoLayersAsync(
    item.ProductId, baseUnitQuantity, ct);
if (layers.IsSuccess)
{
    var totalCogs = layers.Value.Sum(l => l.TotalCost);
    // Record COGS = totalCogs
}
```

### Opening Balance Journal Entry

```csharp
// In AccountingIntegrationService.CreateProductOpeningEntryAsync():
var mappings = await _systemAccountService.GetMappingsAsync(ct);
var lines = new List<JournalEntryLineRequest>
{
    new() { AccountId = mappings.InventoryAccountId, Debit = totalValue, Credit = 0 },
    new() { AccountId = mappings.OpeningBalanceEquityAccountId, Debit = 0, Credit = totalValue }
};
// Create Draft → Post (following existing pattern)
```

### Desktop Opening Stock Fields (Create Mode Only)

```csharp
// ProductEditorViewModel:
private bool _isEditMode;
private decimal _openingQuantity;
private decimal? _openingUnitCost;
private DateTime? _openingExpiryDate;

// Show opening fields only in create mode
public bool ShowOpeningFields => !_isEditMode;
public bool ShowOpeningExpiry => ShowOpeningFields && TrackExpiry;

// Save: populate CreateProductRequest
var request = new CreateProductRequest(
    Name, Barcode, CategoryId, MinStock, Description,
    OpeningQuantity: _isEditMode ? null : _openingQuantity,
    OpeningUnitCost: _isEditMode ? null : _openingUnitCost,
    OpeningExpiryDate: _isEditMode ? null : _openingExpiryDate);
```

---

## Build Verification

```bash
# After each task, verify:
dotnet build SalesSystem.sln  # 0 errors, 0 warnings

# Run relevant tests:
dotnet test tests/SalesSystem.Domain.Tests
dotnet test tests/SalesSystem.Application.Tests
dotnet test tests/SalesSystem.DesktopPWF.Tests

# Final verification:
dotnet test  # All 1,400+ tests pass
```
