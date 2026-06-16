using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line within an inventory transaction.
/// Stores ProductUnitId, Quantity, UnitCost, and optional batch info.
/// Maps to "InventoryTransactionLines" table.
/// Schema: int PK, int InventoryTransactionId FK, int ProductUnitId FK,
/// decimal(18,3) Quantity, decimal(18,2) UnitCost,
/// nvarchar(50) BatchNo (nullable), date ExpiryDate (nullable), smallint WarehouseId (nullable).
/// Entity (no audit).
/// </summary>
public class InventoryTransactionLine : Entity
{
    public int InventoryTransactionId { get; private set; }

    /// <summary>
    /// FK to ProductUnit — identifies the product and unit.
    /// </summary>
    public int ProductUnitId { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Optional batch number for FIFO/FEFO tracking.
    /// </summary>
    public string? BatchNo { get; private set; }

    /// <summary>
    /// Optional expiry date for batch tracking.
    /// </summary>
    public DateOnly? ExpiryDate { get; private set; }

    /// <summary>
    /// Optional warehouse override (if different from parent transaction).
    /// </summary>
    public short? WarehouseId { get; private set; }

    // Navigation properties
    public virtual InventoryTransaction? InventoryTransaction { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private InventoryTransactionLine() { } // EF Core

    /// <summary>
    /// Creates a new inventory transaction line.
    /// </summary>
    public static InventoryTransactionLine Create(
        int inventoryTransactionId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        string? batchNo = null,
        DateOnly? expiryDate = null,
        short? warehouseId = null)
    {
        if (inventoryTransactionId <= 0)
            throw new DomainException("رقم المعاملة مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        return new InventoryTransactionLine
        {
            InventoryTransactionId = inventoryTransactionId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            BatchNo = batchNo?.Trim(),
            ExpiryDate = expiryDate,
            WarehouseId = warehouseId
        };
    }
}
