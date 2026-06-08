using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    // Computed
    public decimal PendingReceiveQuantity => Quantity - ReceivedQuantity;

    // Navigation properties
    public virtual PurchaseOrder? PurchaseOrder { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseOrderItem() { }

    public static PurchaseOrderItem Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        var item = new PurchaseOrderItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            Notes = notes,
            ReceivedQuantity = 0,
            IsActive = true
        };
        item.RecalculateLineTotal();
        item.SetCreatedBy(createdByUserId);
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = Quantity * UnitCost;
    }

    /// <summary>
    /// Records a received quantity against this purchase order item.
    /// Validates that the received quantity does not exceed the pending quantity.
    /// </summary>
    public void AddReceivedQuantity(decimal qty)
    {
        if (qty <= 0)
            throw new DomainException("الكمية المستلمة يجب أن تكون أكبر من الصفر.");
        if (qty > PendingReceiveQuantity)
            throw new DomainException(
                $"الكمية المستلمة ({qty}) تتجاوز الكمية المتبقية ({PendingReceiveQuantity}).");

        ReceivedQuantity += qty;
        UpdateTimestamp();
    }
}
