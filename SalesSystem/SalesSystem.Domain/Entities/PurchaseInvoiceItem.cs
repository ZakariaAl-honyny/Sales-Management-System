using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoiceItem
{
    public int PurchaseInvoiceItemId { get; private set; }
    public int PurchaseInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Product? Product { get; private set; }

    private PurchaseInvoiceItem() { }

    public static PurchaseInvoiceItem Create(
        int productId,
        decimal quantity,
        decimal unitCost,
        decimal discountAmount = 0,
        string? notes = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitCost < 0)
            throw new ArgumentException("UnitCost cannot be negative.", nameof(unitCost));
        if (discountAmount < 0)
            throw new ArgumentException("DiscountAmount cannot be negative.", nameof(discountAmount));

        var item = new PurchaseInvoiceItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
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