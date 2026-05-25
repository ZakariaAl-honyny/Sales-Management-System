using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class StockWriteOff : BaseEntity
{
    public int ProductId { get; private set; }
    public int WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime WriteOffDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public int? UnitId { get; private set; }

    // Navigation properties
    public virtual Product? Product { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual User? CreatedByUser { get; private set; }

    private StockWriteOff() { }

    public static StockWriteOff Create(
        int productId,
        int warehouseId,
        decimal quantity,
        string reason,
        int? unitId = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("السبب مطلوب.");

        var entry = new StockWriteOff
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            Reason = reason,
            UnitId = unitId,
            WriteOffDate = DateTime.UtcNow
        };
        entry.SetCreatedBy(createdByUserId);
        return entry;
    }
}
