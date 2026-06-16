using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities; 

public class PurchaseInvoiceLine : Entity
{
    public int PurchaseInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal LandedUnitCost { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseInvoiceLine() { }

    public static PurchaseInvoiceLine Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitPrice)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitPrice < 0)
            throw new DomainException("سعر الوحدة لا يمكن أن يكون سالباً.");

        var item = new PurchaseInvoiceLine
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LandedUnitCost = unitPrice,
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = Quantity * UnitPrice;
    }

    /// <summary>
    /// Sets the landed unit cost after distributing additional charges proportionally.
    /// </summary>
    public void SetLandedUnitCost(decimal landedUnitCost)
    {
        if (landedUnitCost < 0)
            throw new DomainException("تكلفة الوحدة بعد التوزيع لا يمكن أن تكون سالبة.");
        LandedUnitCost = landedUnitCost;
    }
}
