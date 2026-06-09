using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// صنف أمر الشراء — منتج مطلوب شراؤه ضمن أمر الشراء
/// </summary>
public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal PendingReceiveQuantity => Quantity - ReceivedQuantity;
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    public virtual PurchaseOrder? PurchaseOrder { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseOrderItem() { }

    public static PurchaseOrderItem Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("التكلفة لا يمكن أن تكون سالبة.");
        if (!string.IsNullOrEmpty(notes) && notes.Length > 250)
            throw new DomainException("ملاحظات الصنف لا تتجاوز 250 حرف.");

        var item = new PurchaseOrderItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            ReceivedQuantity = 0,
            LineTotal = quantity * unitCost,
            Notes = notes,
            IsActive = true
        };
        return item;
    }

    public void AddReceivedQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية المستلمة يجب أن تكون أكبر من الصفر.");
        if (quantity > PendingReceiveQuantity)
            throw new DomainException("الكمية المستلمة أكبر من الكمية المطلوبة المتبقية.");

        ReceivedQuantity += quantity;
    }
}
