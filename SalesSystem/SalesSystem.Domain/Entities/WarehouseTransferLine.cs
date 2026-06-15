using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line item in a warehouse transfer.
/// Schema: WarehouseTransferId FK, ProductId FK, BatchId FK (NOT nullable),
/// Quantity, UnitCost, TotalCost.
/// </summary>
public class WarehouseTransferLine : Entity
{
    public int WarehouseTransferId { get; private set; }
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to InventoryBatches — the specific batch being transferred.
    /// </summary>
    public int BatchId { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }

    // Navigation properties
    public virtual WarehouseTransfer? WarehouseTransfer { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual InventoryBatch? Batch { get; private set; }

    private WarehouseTransferLine() { } // EF Core

    public static WarehouseTransferLine Create(
        int warehouseTransferId,
        int productId,
        int batchId,
        decimal quantity,
        decimal unitCost)
    {
        if (warehouseTransferId <= 0)
            throw new DomainException("رقم التحويل مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (batchId <= 0)
            throw new DomainException("الدفعة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new WarehouseTransferLine
        {
            WarehouseTransferId = warehouseTransferId,
            ProductId = productId,
            BatchId = batchId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost
        };
    }
}
