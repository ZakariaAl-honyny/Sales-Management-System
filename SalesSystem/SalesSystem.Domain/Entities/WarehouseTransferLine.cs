using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line item in a warehouse transfer.
/// Maps to "WarehouseTransferLines" table.
/// </summary>
public class WarehouseTransferLine : Entity
{
    public int WarehouseTransferId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public int? BatchId { get; private set; }

    // Navigation properties
    public virtual WarehouseTransfer? WarehouseTransfer { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }
    public virtual InventoryBatch? Batch { get; private set; }

    private WarehouseTransferLine() { } // EF Core

    public static WarehouseTransferLine Create(
        int warehouseTransferId,
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        int? batchId = null)
    {
        if (warehouseTransferId <= 0)
            throw new DomainException("رقم التحويل مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new WarehouseTransferLine
        {
            WarehouseTransferId = warehouseTransferId,
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            BatchId = batchId
        };
    }
}
