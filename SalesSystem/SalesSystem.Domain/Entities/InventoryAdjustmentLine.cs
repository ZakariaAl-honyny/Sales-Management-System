using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single product adjustment line within an inventory adjustment.
/// Schema: InventoryAdjustmentId FK, ProductId FK, BatchId FK (nullable),
/// Quantity, UnitCost, TotalCost.
/// </summary>
public class InventoryAdjustmentLine : Entity
{
    public int InventoryAdjustmentId { get; private set; }
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to InventoryBatches — nullable for opening adjustments.
    /// </summary>
    public int? BatchId { get; private set; }

    /// <summary>
    /// The adjustment quantity.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// The unit cost at the time of adjustment.
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Total cost = Quantity x UnitCost.
    /// </summary>
    public decimal TotalCost { get; private set; }

    // Navigation properties
    public virtual InventoryAdjustment? InventoryAdjustment { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual InventoryBatch? Batch { get; private set; }

    private InventoryAdjustmentLine() { }

    public static InventoryAdjustmentLine Create(
        int inventoryAdjustmentId,
        int productId,
        decimal quantity,
        decimal unitCost,
        int? batchId = null)
    {
        if (inventoryAdjustmentId <= 0)
            throw new DomainException("رقم التسوية مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new InventoryAdjustmentLine
        {
            InventoryAdjustmentId = inventoryAdjustmentId,
            ProductId = productId,
            BatchId = batchId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost
        };
    }
}
