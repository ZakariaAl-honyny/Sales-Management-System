using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
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
    public DiscountType DiscountType { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal? CostInBaseCurrency { get; private set; }
    public decimal AdditionalFeesAmount { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseInvoiceLine() { }

    public static PurchaseInvoiceLine Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitPrice,
        DiscountType discountType = DiscountType.Amount,
        decimal? discountRate = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitPrice < 0)
            throw new DomainException("سعر الوحدة لا يمكن أن يكون سالباً.");
        if (discountType == DiscountType.Percentage && (discountRate is null or < 0 or > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");

        var item = new PurchaseInvoiceLine
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LandedUnitCost = unitPrice,
            DiscountType = discountType,
            DiscountRate = discountType == DiscountType.Percentage ? discountRate : null,
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        var grossTotal = Quantity * UnitPrice;

        DiscountAmount = DiscountType switch
        {
            DiscountType.Amount => DiscountRate ?? 0,
            DiscountType.Percentage => grossTotal * (DiscountRate ?? 0) / 100m,
            _ => 0
        };

        LineTotal = grossTotal - DiscountAmount;
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

    /// <summary>
    /// Sets the discount type and rate, then recalculates the line total.
    /// </summary>
    public void SetDiscount(DiscountType type, decimal? rate)
    {
        if (type == DiscountType.Percentage && (rate is null or < 0 or > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");

        DiscountType = type;
        DiscountRate = type == DiscountType.Percentage ? rate : null;
        RecalculateLineTotal();
    }

    /// <summary>
    /// Sets the cost in base currency for multi-currency support.
    /// </summary>
    public void SetCostInBaseCurrency(decimal cost)
    {
        if (cost < 0)
            throw new DomainException("التكلفة بعملة الأساس لا يمكن أن تكون سالبة.");
        CostInBaseCurrency = cost;
    }

    /// <summary>
    /// Sets the allocated additional fees amount (landed cost distribution).
    /// </summary>
    public void SetAdditionalFeesAmount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("مبلغ الرسوم الإضافية لا يمكن أن يكون سالباً.");
        AdditionalFeesAmount = amount;
    }
}
