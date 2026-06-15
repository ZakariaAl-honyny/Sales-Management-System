using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks stock quantity per product per warehouse.
/// Inherits <see cref="AuditableEntity"/> for audit trail.
/// Maps to "WarehouseStocks" table.
/// UK: UNIQUE(WarehouseId, ProductId).
/// </summary>
public class WarehouseStock : AuditableEntity
{
    public short WarehouseId { get; private set; }
    public int ProductId { get; private set; }

    /// <summary>
    /// Current stock quantity in base units. decimal(18,3).
    /// DB CHECK constraint ensures Quantity >= 0.
    /// </summary>
    public decimal Quantity { get; private set; }

    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Product? Product { get; private set; }

    private WarehouseStock() { }

    public static WarehouseStock Create(
        short warehouseId,
        int productId,
        decimal quantity = 0,
        int? createdByUserId = null)
    {
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");

        var stock = new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity
        };
        stock.SetCreatedBy(createdByUserId);
        return stock;
    }

    public void IncreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        Quantity += amount;
        UpdateTimestamp();
    }

    public void DecreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        if (Quantity < amount)
            throw new DomainException("المخزون غير كافٍ.");
        Quantity -= amount;
        UpdateTimestamp();
    }

    public void SetQuantity(decimal quantity)
    {
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");
        Quantity = quantity;
        UpdateTimestamp();
    }
}
