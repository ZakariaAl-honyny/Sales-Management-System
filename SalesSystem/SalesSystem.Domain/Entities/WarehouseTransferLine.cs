using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line item in a warehouse transfer.
/// Schema: int WarehouseTransferId FK, int ProductUnitId FK,
/// decimal(18,3) Quantity, nvarchar(50) BatchNo.
/// Entity (no audit).
/// </summary>
public class WarehouseTransferLine : Entity
{
    public int WarehouseTransferId { get; private set; }

    /// <summary>
    /// FK to ProductUnit — identifies the product and unit.
    /// </summary>
    public int ProductUnitId { get; private set; }

    public decimal Quantity { get; private set; }

    /// <summary>
    /// Optional batch number for FIFO tracking during transfer.
    /// </summary>
    public string? BatchNo { get; private set; }

    // Navigation properties
    public virtual WarehouseTransfer? WarehouseTransfer { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private WarehouseTransferLine() { } // EF Core

    public static WarehouseTransferLine Create(
        int warehouseTransferId,
        int productUnitId,
        decimal quantity,
        string? batchNo = null)
    {
        if (warehouseTransferId <= 0)
            throw new DomainException("رقم التحويل مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");

        return new WarehouseTransferLine
        {
            WarehouseTransferId = warehouseTransferId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            BatchNo = batchNo?.Trim()
        };
    }
}
