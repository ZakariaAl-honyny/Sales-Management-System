using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class InventoryMovement
{
    public long InventoryMovementId { get; private set; }
    public int ProductId { get; private set; }
    public int WarehouseId { get; private set; }
    public MovementType MovementType { get; private set; }
    public decimal QuantityChange { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public int ReferenceId { get; private set; }
    public decimal? UnitCost { get; private set; }
    public DateTime MovementDate { get; private set; }
    public string? Notes { get; private set; }
    public int? CreatedByUserId { get; private set; }

    public virtual Product? Product { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual User? CreatedByUser { get; private set; }

    private InventoryMovement() { }

    public static InventoryMovement Create(
        int productId,
        int warehouseId,
        MovementType movementType,
        decimal quantityChange,
        decimal quantityBefore,
        decimal quantityAfter,
        string referenceType,
        int referenceId,
        decimal? unitCost = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));
        if (string.IsNullOrWhiteSpace(referenceType))
            throw new ArgumentException("ReferenceType is required.", nameof(referenceType));
        if (referenceId <= 0)
            throw new ArgumentException("ReferenceId is required.", nameof(referenceId));

        return new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = quantityChange,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            UnitCost = unitCost,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            MovementDate = DateTime.UtcNow
        };
    }
}