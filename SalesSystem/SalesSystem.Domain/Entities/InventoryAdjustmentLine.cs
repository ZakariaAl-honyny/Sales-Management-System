using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single product adjustment line within an inventory adjustment.
/// Schema: int InventoryAdjustmentId FK, int ProductUnitId FK,
/// decimal(18,3) ExpectedQuantity, decimal(18,3) ActualQuantity, decimal(18,2) UnitCost.
/// Entity (no audit).
/// </summary>
public class InventoryAdjustmentLine : Entity
{
    public int InventoryAdjustmentId { get; private set; }

    /// <summary>
    /// FK to ProductUnit — identifies the product and unit.
    /// </summary>
    public int ProductUnitId { get; private set; }

    /// <summary>
    /// Expected quantity per system records.
    /// </summary>
    public decimal ExpectedQuantity { get; private set; }

    /// <summary>
    /// Actual quantity counted/adjusted.
    /// </summary>
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Unit cost at the time of adjustment. decimal(18,2).
    /// </summary>
    public decimal UnitCost { get; private set; }

    // Navigation properties
    public virtual InventoryAdjustment? InventoryAdjustment { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private InventoryAdjustmentLine() { }

    public static InventoryAdjustmentLine Create(
        int inventoryAdjustmentId,
        int productUnitId,
        decimal expectedQuantity,
        decimal actualQuantity,
        decimal unitCost)
    {
        if (inventoryAdjustmentId <= 0)
            throw new DomainException("رقم التسوية مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (expectedQuantity < 0)
            throw new DomainException("الكمية المتوقعة لا يمكن أن تكون سالبة.");
        if (actualQuantity < 0)
            throw new DomainException("الكمية الفعلية لا يمكن أن تكون سالبة.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new InventoryAdjustmentLine
        {
            InventoryAdjustmentId = inventoryAdjustmentId,
            ProductUnitId = productUnitId,
            ExpectedQuantity = expectedQuantity,
            ActualQuantity = actualQuantity,
            UnitCost = unitCost
        };
    }
}
