using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line item in a Sales Quotation.
/// No stock impact — purely informational until converted to an invoice.
/// </summary>
public class SalesQuotationItem : Entity
{
    public int SalesQuotationId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    /// <summary>Quantity in the selected product unit.</summary>
    public decimal Quantity { get; private set; }
    /// <summary>Price per unit in the quotation's currency.</summary>
    public decimal UnitPrice { get; private set; }
    /// <summary>Discount amount applied to this line (not percentage).</summary>
    public decimal DiscountAmount { get; private set; }
    /// <summary>Line total = (Quantity × UnitPrice) − DiscountAmount.</summary>
    public decimal LineTotal { get; private set; }
    /// <summary>Optional notes for this line item (max 500).</summary>
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual SalesQuotation? SalesQuotation { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private SalesQuotationItem() { } // EF Core

    public static SalesQuotationItem Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitPrice < 0)
            throw new DomainException("سعر الوحدة لا يمكن أن يكون سالباً.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        var item = new SalesQuotationItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            Notes = notes?.Trim()
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
        if (LineTotal < 0)
            LineTotal = 0;
    }
}
