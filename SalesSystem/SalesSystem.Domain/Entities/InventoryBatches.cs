using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory for FIFO/FEFO cost allocation.
/// Each batch tracks quantity and unit cost for a specific product in a specific warehouse.
/// </summary>
public class InventoryBatch : BaseEntity
{
    /// <summary>
    /// FK to Product.
    /// </summary>
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to PurchaseInvoiceItem — the source purchase line item.
    /// Null for opening stock or manual adjustments.
    /// </summary>
    public int? PurchaseInvoiceItemId { get; private set; }

    /// <summary>
    /// FK to Warehouse where this batch is stored.
    /// </summary>
    public int WarehouseId { get; private set; }

    /// <summary>
    /// Current available quantity in base units. Stored as decimal(18,3).
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Per-unit cost at time of purchase. Stored as decimal(18,2).
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Date of manufacture (optional).
    /// </summary>
    public DateTime? ManufactureDate { get; private set; }

    /// <summary>
    /// Expiry date for FEFO (First Expiry First Out) tracking (optional).
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Supplier batch number or internal batch reference.
    /// </summary>
    public string BatchNo { get; private set; } = string.Empty;

    // ─── Navigation Properties ──────────────────────────

    public Product? Product { get; private set; }

    public PurchaseInvoiceItem? PurchaseInvoiceItem { get; private set; }

    public Warehouse? Warehouse { get; private set; }

    private InventoryBatch() { } // EF Core

    // ─── Factory ──────────────────────────────────

    /// <summary>
    /// Creates a new inventory batch record.
    /// </summary>
    public static InventoryBatch Create(
        int productId,
        int warehouseId,
        decimal quantity,
        decimal unitCost,
        string batchNo,
        int? purchaseInvoiceItemId = null,
        DateTime? manufactureDate = null,
        DateTime? expiryDate = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("معرف المنتج مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("معرف المستودع مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (string.IsNullOrWhiteSpace(batchNo))
            throw new DomainException("رقم الدفعة/الباتش مطلوب.");
        if (manufactureDate.HasValue && expiryDate.HasValue && manufactureDate >= expiryDate)
            throw new DomainException("تاريخ التصنيع يجب أن يكون قبل تاريخ انتهاء الصلاحية.");

        var batch = new InventoryBatch
        {
            ProductId = productId,
            PurchaseInvoiceItemId = purchaseInvoiceItemId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            UnitCost = Math.Round(unitCost, 2),
            ManufactureDate = manufactureDate,
            ExpiryDate = expiryDate,
            BatchNo = batchNo.Trim(),
            IsActive = true
        };
        batch.SetCreatedBy(createdByUserId);
        return batch;
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Deducts the specified quantity from this batch.
    /// Validates that sufficient quantity is available.
    /// </summary>
    public void DeductStock(decimal qty)
    {
        if (qty <= 0)
            throw new DomainException("الكمية المستهلكة يجب أن تكون أكبر من الصفر.");
        if (qty > Quantity)
            throw new DomainException(
                $"الكمية المطلوبة ({qty}) تتجاوز الكمية المتوفرة في الدفعة ({Quantity}).");

        Quantity -= qty;
        UpdateTimestamp();
    }

    /// <summary>
    /// Splits this batch by transferring a quantity to a new batch.
    /// Returns a new InventoryBatch instance with the transferred quantity.
    /// The current batch's quantity is reduced by the transferred amount.
    /// </summary>
    public InventoryBatch TransferStock(decimal qty)
    {
        if (qty <= 0)
            throw new DomainException("الكمية المحولة يجب أن تكون أكبر من الصفر.");
        if (qty > Quantity)
            throw new DomainException(
                $"الكمية المطلوب تحويلها ({qty}) تتجاوز الكمية المتوفرة ({Quantity}).");

        // Reduce current batch
        Quantity -= qty;

        // Create new batch with same properties but transferred quantity
        var newBatch = new InventoryBatch
        {
            ProductId = ProductId,
            PurchaseInvoiceItemId = PurchaseInvoiceItemId,
            WarehouseId = WarehouseId,
            Quantity = qty,
            UnitCost = UnitCost,
            ManufactureDate = ManufactureDate,
            ExpiryDate = ExpiryDate,
            BatchNo = BatchNo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = CreatedByUserId
        };

        UpdateTimestamp();
        return newBatch;
    }

    /// <summary>
    /// Checks if this batch is expired relative to the given date.
    /// </summary>
    public bool IsExpiredOn(DateTime date)
        => ExpiryDate.HasValue && date >= ExpiryDate.Value;

    /// <summary>
    /// Returns the total value of this batch (Quantity × UnitCost).
    /// </summary>
    public decimal TotalValue => Quantity * UnitCost;
}
