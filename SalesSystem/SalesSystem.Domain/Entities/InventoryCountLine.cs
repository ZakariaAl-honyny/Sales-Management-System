using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a single product count record within an inventory count session.
/// Contains system quantity, actual counted quantity, and the computed difference.
/// </summary>
public class InventoryCountLine : Entity
{
    public int InventoryCountId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal SystemQuantity { get; private set; }
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Difference = ActualQuantity - SystemQuantity.
    /// Positive means surplus, negative means shortage.
    /// </summary>
    public decimal Difference { get; private set; }

    /// <summary>ملاحظات على صنف الجرد (اختياري)</summary>
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual InventoryCount? InventoryCount { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private InventoryCountLine() { }

    /// <summary>
    /// Creates a count line for a specific product/unit.
    /// Difference is computed as ActualQuantity - SystemQuantity.
    /// </summary>
    /// <param name="inventoryCountId">The parent inventory count ID.</param>
    /// <param name="productId">The product being counted.</param>
    /// <param name="productUnitId">The unit of measure for the product.</param>
    /// <param name="systemQuantity">The expected quantity in the system (must be >= 0).</param>
    /// <param name="actualQuantity">The actual physical counted quantity (must be >= 0).</param>
    /// <param name="notes">Optional notes for this count line.</param>
    public static InventoryCountLine Create(
        int inventoryCountId,
        int productId,
        int productUnitId,
        decimal systemQuantity,
        decimal actualQuantity,
        string? notes = null)
    {
        if (inventoryCountId <= 0)
            throw new DomainException("رقم الجرد مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (systemQuantity < 0)
            throw new DomainException("الكمية النظامية لا يمكن أن تكون سالبة.");
        if (actualQuantity < 0)
            throw new DomainException("الكمية الفعلية لا يمكن أن تكون سالبة.");

        return new InventoryCountLine
        {
            InventoryCountId = inventoryCountId,
            ProductId = productId,
            ProductUnitId = productUnitId,
            SystemQuantity = systemQuantity,
            ActualQuantity = actualQuantity,
            Difference = actualQuantity - systemQuantity,
            Notes = notes
        };
    }
}
