using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a single product adjustment line within an inventory adjustment.
/// Stores the adjusted quantity and unit cost for journal entry valuation.
/// </summary>
public class InventoryAdjustmentLine : Entity
{
    public int InventoryAdjustmentId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }

    /// <summary>
    /// The quantity being adjusted (positive for surplus, negative conceptually for shortage,
    /// stored as absolute value — the direction is determined by the parent AdjustmentType).
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// The unit cost at the time of adjustment — used for journal entry valuation.
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// إجمالي السطر = الكمية × تكلفة الوحدة — يستخدم في قيد اليومية
    /// </summary>
    public decimal LineTotal => Quantity * UnitCost;

    // Navigation properties
    public virtual InventoryAdjustment? InventoryAdjustment { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private InventoryAdjustmentLine() { }

    /// <summary>
    /// Creates an adjustment line for a specific product/unit.
    /// </summary>
    /// <param name="inventoryAdjustmentId">The parent inventory adjustment ID.</param>
    /// <param name="productId">The product being adjusted.</param>
    /// <param name="productUnitId">The unit of measure for the product.</param>
    /// <param name="quantity">The adjustment quantity (must be > 0).</param>
    /// <param name="unitCost">The unit cost for valuation (must be >= 0).</param>
    public static InventoryAdjustmentLine Create(
        int inventoryAdjustmentId,
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost)
    {
        if (inventoryAdjustmentId <= 0)
            throw new DomainException("رقم التسوية مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new InventoryAdjustmentLine
        {
            InventoryAdjustmentId = inventoryAdjustmentId,
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost
        };
    }
}
