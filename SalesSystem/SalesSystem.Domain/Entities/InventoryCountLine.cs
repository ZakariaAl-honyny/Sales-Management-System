using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single product count record within an inventory count session.
/// Schema: int InventoryCountId FK, int ProductUnitId FK,
/// decimal(18,3) ExpectedQuantity, decimal(18,3) ActualQuantity,
/// decimal(18,3) Difference, nvarchar(300) Notes.
/// Entity (no audit).
/// </summary>
public class InventoryCountLine : Entity
{
    public int InventoryCountId { get; private set; }

    /// <summary>
    /// FK to ProductUnit — identifies the product and unit.
    /// </summary>
    public int ProductUnitId { get; private set; }

    /// <summary>
    /// Expected quantity from system records.
    /// </summary>
    public decimal ExpectedQuantity { get; private set; }

    /// <summary>
    /// Actual counted quantity.
    /// </summary>
    public decimal ActualQuantity { get; private set; }

    /// <summary>
    /// Difference = ActualQuantity - ExpectedQuantity.
    /// </summary>
    public decimal Difference { get; private set; }

    /// <summary>
    /// Optional notes for this count line.
    /// </summary>
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual InventoryCount? InventoryCount { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private InventoryCountLine() { }

    public static InventoryCountLine Create(
        int inventoryCountId,
        int productUnitId,
        decimal expectedQuantity,
        decimal actualQuantity,
        string? notes = null)
    {
        if (inventoryCountId <= 0)
            throw new DomainException("رقم الجرد مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (expectedQuantity < 0)
            throw new DomainException("الكمية المتوقعة لا يمكن أن تكون سالبة.");
        if (actualQuantity < 0)
            throw new DomainException("الكمية الفعلية لا يمكن أن تكون سالبة.");

        return new InventoryCountLine
        {
            InventoryCountId = inventoryCountId,
            ProductUnitId = productUnitId,
            ExpectedQuantity = expectedQuantity,
            ActualQuantity = actualQuantity,
            Difference = actualQuantity - expectedQuantity,
            Notes = notes?.Trim()
        };
    }
}
