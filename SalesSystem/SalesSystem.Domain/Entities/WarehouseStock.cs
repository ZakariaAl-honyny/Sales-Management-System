using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReorderLevel { get; private set; }

    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Product? Product { get; private set; }

    private WarehouseStock() { }

    public static WarehouseStock Create(int warehouseId, int productId, decimal quantity = 0, decimal reorderLevel = 0)
    {
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");
        if (reorderLevel < 0)
            throw new DomainException("نقطة إعادة الطلب لا يمكن أن تكون سالبة.");

        return new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            ReorderLevel = reorderLevel
        };
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

    public void SetReorderLevel(decimal level)
    {
        if (level < 0)
            throw new DomainException("نقطة إعادة الطلب لا يمكن أن تكون سالبة.");
        ReorderLevel = level;
        UpdateTimestamp();
    }

    public void DeductStock(decimal quantity, SalesSystem.Domain.Enums.UnitType unitType, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        var quantityInPieces = unitType == SalesSystem.Domain.Enums.UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        if (Quantity < quantityInPieces)
            throw new DomainException($"المخزون غير كافٍ. المتاح: {Quantity}, المطلوب: {quantityInPieces}");

        Quantity -= quantityInPieces;
        UpdateTimestamp();
    }

    public void AddStock(decimal quantity, SalesSystem.Domain.Enums.UnitType unitType, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        var quantityInPieces = unitType == SalesSystem.Domain.Enums.UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        Quantity += quantityInPieces;
        UpdateTimestamp();
    }
}