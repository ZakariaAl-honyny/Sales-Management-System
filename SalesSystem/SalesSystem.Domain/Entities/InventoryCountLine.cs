using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single product count record within an inventory count session.
/// Links to a specific batch (BatchId is NOT nullable).
/// Schema: InventoryCountId FK, ProductId FK, BatchId FK (NOT null),
/// SystemQuantity, ActualQuantity, DifferenceQuantity.
/// </summary>
public class InventoryCountLine : Entity
{
    public int InventoryCountId { get; private set; }
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to InventoryBatches — this line is linked to a specific batch.
    /// </summary>
    public int BatchId { get; private set; }

    /// <summary>
    /// Expected quantity from system.
    /// </summary>
    public decimal SystemQuantity { get; private set; }

    /// <summary>
    /// Actual counted quantity.
    /// </summary>
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Difference = ActualQuantity - SystemQuantity.
    /// </summary>
    public decimal DifferenceQuantity { get; private set; }

    // Navigation properties
    public virtual InventoryCount? InventoryCount { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual InventoryBatch? Batch { get; private set; }

    private InventoryCountLine() { }

    public static InventoryCountLine Create(
        int inventoryCountId,
        int productId,
        int batchId,
        decimal systemQuantity,
        decimal actualQuantity)
    {
        if (inventoryCountId <= 0)
            throw new DomainException("رقم الجرد مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (batchId <= 0)
            throw new DomainException("الدفعة مطلوبة.");
        if (systemQuantity < 0)
            throw new DomainException("الكمية النظامية لا يمكن أن تكون سالبة.");
        if (actualQuantity < 0)
            throw new DomainException("الكمية الفعلية لا يمكن أن تكون سالبة.");

        return new InventoryCountLine
        {
            InventoryCountId = inventoryCountId,
            ProductId = productId,
            BatchId = batchId,
            SystemQuantity = systemQuantity,
            ActualQuantity = actualQuantity,
            DifferenceQuantity = actualQuantity - systemQuantity
        };
    }
}
