# Data Model — Products Module Remaining Work

## Entity Changes Summary

| Entity | Change | Type |
|--------|--------|------|
| `Product` | Remove `Cost` column | Schema change |
| `Product` | Rename `HasExpiry` → `TrackExpiry` | Schema change |
| `ProductBarcode` | Delete entire entity | Schema + Code removal |
| `InventoryBatch` | No changes (already supports "OPENING" BatchNo) | None |
| `ProductCostService` | NEW service (Application layer) | Code addition |
| `AccountingIntegrationService` | New method: `CreateProductOpeningEntryAsync` | Code addition |

---

## Product Entity (Final)

```csharp
public class Product : BaseEntity
{
    // Identity
    public int Id { get; private set; }                   // PK, auto-increment
    public string Name { get; private set; }               // Required, nvarchar(500)
    public string? Barcode { get; private set; }            // Single source of truth, unique, nvarchar(100)
    public int? CategoryId { get; private set; }            // FK → ProductCategory
    
    // Classification
    public string? Description { get; private set; }        // Optional, nvarchar(2000)
    
    // Expiry Tracking (renamed from HasExpiry)
    public bool TrackExpiry { get; private set; }           // When true → FEFO enabled
    
    // Stock Control
    public decimal MinStockLevel { get; private set; }     // decimal(18, 3)
    public decimal ReorderLevel { get; private set; }       // decimal(18, 3)
    
    // REMOVED: public decimal Cost { get; private set; }   // ❌ COST REMOVED
    
    // Soft Delete
    public bool IsActive { get; private set; }
    
    // Navigation Properties
    public virtual Category? Category { get; private set; }
    public virtual ICollection<WarehouseStock> WarehouseStocks { get; private set; }
    public virtual ICollection<ProductUnit> Units { get; private set; }
    public virtual ICollection<InventoryBatch> InventoryBatches { get; private set; }
    public virtual ICollection<ProductImage> Images { get; private set; }
}
```

### Product Factory Method

```csharp
public static Product Create(
    string name,
    int? categoryId = null,
    decimal minStockLevel = 0,
    decimal reorderLevel = 0,
    bool trackExpiry = false,          // ← RENAMED from hasExpiry
    string? barcode = null,
    string? description = null,
    int? createdByUserId = null)
```

### Product Update Method

```csharp
public void Update(
    string name,
    int? categoryId,
    decimal minStockLevel,
    decimal reorderLevel,
    bool trackExpiry,                   // ← RENAMED from hasExpiry
    string? barcode,
    string? description,
    int? updatedByUserId)
```

### REMOVED Domain Methods
```csharp
// ❌ REMOVED — Cost is no longer stored on Product
// public void UpdateCost(decimal newCost)
// public decimal Cost { get; private set; }
```

---

## InventoryBatch Entity (Unchanged)

```csharp
public class InventoryBatch : BaseEntity
{
    public int Id { get; private set; }                    // PK
    public int ProductId { get; private set; }             // FK → Product
    public int WarehouseId { get; private set; }           // FK → Warehouse
    public int? PurchaseInvoiceLineId { get; private set; } // FK → PurchaseInvoiceLine (nullable)
    
    public string BatchNo { get; private set; }            // nvarchar(100) — "OPENING" for opening stock
    public decimal Quantity { get; private set; }          // decimal(18, 3) — current available qty
    public decimal UnitCost { get; private set; }           // decimal(18, 2)
    public DateTime? ManufactureDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsActive { get; private set; }
    
    // Computed
    public decimal TotalValue => Quantity * UnitCost;
    
    // Factory
    public static InventoryBatch Create(
        int productId, int warehouseId, decimal quantity,
        decimal unitCost, string batchNo,
        int? PurchaseInvoiceLineId = null,
        DateTime? manufactureDate = null,
        DateTime? expiryDate = null,
        int? createdByUserId = null)
    
    // Methods
    public void DeductStock(decimal qty)     // Decreases Quantity
    public InventoryBatch TransferStock(decimal qty)  // Creates new batch with same metadata
    public bool IsExpiredOn(DateTime date)   // Checks expiry
}
```

---

## ProductCostService (NEW — Application Layer)

```csharp
public interface IProductCostService
{
    /// <summary>
    /// Weighted average cost from all active batches with remaining quantity.
    /// Returns 0 if no batches exist.
    /// </summary>
    Task<Result<decimal>> GetAverageCostAsync(int productId, CancellationToken ct);
    
    /// <summary>
    /// Returns FIFO layers for cost allocation.
    /// Each layer shows how many units to consume from each batch.
    /// </summary>
    Task<Result<List<FifoLayerDto>>> GetFifoLayersAsync(int productId, decimal quantity, CancellationToken ct);
    
    /// <summary>
    /// Returns the most recent purchase cost from the latest batch.
    /// </summary>
    Task<Result<decimal>> GetLatestCostAsync(int productId, CancellationToken ct);
}

public class FifoLayerDto
{
    public int BatchId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public decimal QuantityConsumed { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost => QuantityConsumed * UnitCost;
}
```

---

## Opening Stock Request DTO (Updates to CreateProductRequest)

```csharp
// NEW fields added to CreateProductRequest
public record CreateProductRequest(
    string Name,
    string? Barcode,
    int? CategoryId,
    decimal MinStock,
    string? Description,
    // NEW — Opening Stock Fields (creation only):
    decimal? OpeningQuantity = null,         // decimal(18, 3) — in base units
    decimal? OpeningUnitCost = null,          // decimal(18, 2)
    DateTime? OpeningExpiryDate = null        // Optional — only if TrackExpiry = true
);
```

---

## AccountingIntegrationService — New Method

```csharp
/// <summary>
/// Creates and posts a journal entry for product opening stock.
/// Dr Inventory (totalValue)
/// Cr OpeningBalanceEquity (totalValue)
/// </summary>
Task<Result<int>> CreateProductOpeningEntryAsync(
    int productId,
    string productName,
    decimal totalOpeningValue,
    int createdByUserId,
    DateTime transactionDate,
    CancellationToken ct);
```

---

## Product Import Columns (Excel)

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Product Name | ✅ Yes | String | Product display name |
| Category Name | ✅ Yes | String | Mapped to CategoryId via lookup |
| Barcode | ❌ No | String | Must be unique if provided |
| Base Unit Name | ✅ Yes | String | Mapped to UnitId (e.g., "حبة") |
| TrackExpiry | ❌ No | Boolean | Default false |
| Opening Quantity | ❌ No | Decimal | In base units (18,3) |
| Opening Cost | ❌ No | Decimal | Per-unit cost (18,2) |
| Opening Expiry Date | ❌ No | Date | Only if TrackExpiry = true |
| Min Stock Level | ❌ No | Decimal | (18,3) |
| Description | ❌ No | String | Free text |

---

## Entity Relationship Diagram (Final)

```text
ProductCategories (1) ────── (0..*) Products
     │
     │
Products (1) ────── (0..*) ProductUnits
     │                  │
     │                  └── (0..*) ProductPrices
     │
     ├── (0..*) InventoryBatches (BatchNo = "OPENING" for opening stock)
     │       │
     │       └── (0..1) Warehouses (default = "المخزن الرئيسي")
     │
     ├── (0..*) WarehouseStocks
     │       │
     │       └── (0..1) Warehouses
     │
     ├── (0..*) ProductImages
     │
     └── (0..*) InventoryMovements
```

---

## State Transitions

### Opening Batch Lifecycle

```text
Product Created (openingQty > 0)
    │
    ├──► InventoryBatch("OPENING")
    │      Quantity = openingQty
    │      UnitCost = openingCost
    │      BatchNo = "OPENING"
    │
    ├──► WarehouseStock Created/Increased
    │      Quantity += openingQty
    │
    ├──► InventoryMovement Recorded
    │      MovementType = Adjustment (or PurchaseIn)
    │      QuantityChange = +openingQty
    │
    └──► Journal Entry Created
           Dr Inventory = openingValue
           Cr OpeningBalanceEquity = openingValue
```

### Batch Deduction (for Sales COGS)

```csharp
// When selling, consume oldest batches first (FIFO)
batches = query ORDER BY CreatedAt ASC
for each batch with Quantity > 0:
    consume = min(requestedQty, batch.Quantity)
    batch.DeductStock(consume)
    cost += consume * batch.UnitCost
    requestedQty -= consume
    if requestedQty == 0: break
```

---

## Migration Plan

**Migration name**: `20260610_Phase28_ProductsModuleRemaining`

### Up()
1. `RenameColumn("Products", "HasExpiry", "TrackExpiry")`
2. `DropColumn("Products", "Cost")`
3. `DropTable("ProductBarcodes")`
4. Remove `DbSet<ProductBarcode>` from model snapshot

### Down()
1. `AddColumn<ProductBarcodes>(...)` — restore table
2. `AddColumn<Products>("Cost", typeof(decimal))`
3. `RenameColumn("Products", "TrackExpiry", "HasExpiry")`
