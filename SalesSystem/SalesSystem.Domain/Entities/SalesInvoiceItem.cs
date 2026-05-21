using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SalesInvoiceItem : BaseEntity
{
    public int SalesInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; } = SaleMode.Retail;
    public string? Notes { get; private set; }

    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Product? Product { get; private set; }

    private SalesInvoiceItem() { }

    public static SalesInvoiceItem Create(
        int productId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        SaleMode mode = SaleMode.Retail,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitPrice < 0)
            throw new DomainException("سعر الوحدة لا يمكن أن يكون سالباً.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        var item = new SalesInvoiceItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            Mode = mode,
            Notes = notes
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
    }
}