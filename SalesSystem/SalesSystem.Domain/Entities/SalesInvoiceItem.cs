using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class SalesInvoiceItem
{
    public int SalesInvoiceItemId { get; private set; }
    public int SalesInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Product? Product { get; private set; }

    private SalesInvoiceItem() { }

    public static SalesInvoiceItem Create(
        int productId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        string? notes = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("UnitPrice cannot be negative.", nameof(unitPrice));
        if (discountAmount < 0)
            throw new ArgumentException("DiscountAmount cannot be negative.", nameof(discountAmount));

        var item = new SalesInvoiceItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
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