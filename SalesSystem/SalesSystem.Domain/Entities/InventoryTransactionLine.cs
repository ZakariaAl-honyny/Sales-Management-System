using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line within an inventory transaction.
/// Stores product, unit, quantity, unit cost, and total cost.
/// Optionally links to a specific batch for FIFO/FEFO tracking.
/// Maps to "InventoryTransactionLines" table.
/// </summary>
public class InventoryTransactionLine : Entity
{
    public int InventoryTransactionId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public int? BatchId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }

    // Navigation properties
    public virtual InventoryTransaction? InventoryTransaction { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }
    public virtual InventoryBatch? Batch { get; private set; }

    private InventoryTransactionLine() { } // EF Core

    /// <summary>
    /// Creates a new inventory transaction line.
    /// </summary>
    public static InventoryTransactionLine Create(
        int inventoryTransactionId,
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        int? batchId = null)
    {
        if (inventoryTransactionId <= 0)
            throw new DomainException("رقم المعاملة مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new InventoryTransactionLine
        {
            InventoryTransactionId = inventoryTransactionId,
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            BatchId = batchId
        };
    }
}
