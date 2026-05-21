using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoiceItem : BaseEntity
{
    public int PurchaseInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; } = SaleMode.Retail;
    public string? Notes { get; private set; }

    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Product? Product { get; private set; }

    private PurchaseInvoiceItem() { }

    public static PurchaseInvoiceItem Create(
        int productId,
        decimal quantity,
        decimal unitCost,
        decimal discountAmount = 0,
        SaleMode mode = SaleMode.Retail,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        var item = new PurchaseInvoiceItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
            Mode = mode,
            Notes = notes
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitCost) - DiscountAmount;
    }
}