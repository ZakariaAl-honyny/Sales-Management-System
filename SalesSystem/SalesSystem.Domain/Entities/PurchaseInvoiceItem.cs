using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoiceItem : BaseEntity
{
    public int PurchaseInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DiscountType? DiscountType { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public decimal? CostInBaseCurrency { get; private set; }
    public decimal AdditionalFeesAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; } = SaleMode.Retail;
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseInvoiceItem() { }

    public static PurchaseInvoiceItem Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal unitCost,
        decimal discountAmount = 0,
        DiscountType? discountType = null,
        decimal? discountRate = null,
        decimal? costInBaseCurrency = null,
        SaleMode mode = SaleMode.Retail,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (discountType == Enums.DiscountType.Percentage && (!discountRate.HasValue || discountRate < 0 || discountRate > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");
        if (costInBaseCurrency.HasValue && costInBaseCurrency < 0)
            throw new DomainException("التكلفة بعملة الأساس لا يمكن أن تكون سالبة.");

        var item = new PurchaseInvoiceItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
            DiscountType = discountType,
            DiscountRate = discountRate,
            CostInBaseCurrency = costInBaseCurrency,
            AdditionalFeesAmount = 0,
            Mode = mode,
            Notes = notes
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        var baseTotal = Quantity * UnitCost;

        // Calculate effective discount
        var effectiveDiscount = DiscountAmount;
        if (DiscountType == Enums.DiscountType.Percentage && DiscountRate.HasValue)
        {
            var percentageDiscount = baseTotal * (DiscountRate.Value / 100m);
            effectiveDiscount = percentageDiscount;
        }

        LineTotal = baseTotal - effectiveDiscount + AdditionalFeesAmount;
    }

    /// <summary>
    /// Sets the additional fees allocated to this item.
    /// </summary>
    public void SetAdditionalFeesAmount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("مبلغ الرسوم الإضافية لا يمكن أن يكون سالباً.");

        AdditionalFeesAmount = amount;
        RecalculateLineTotal();
    }

    /// <summary>
    /// Sets the cost in base currency for multi-currency purchases.
    /// </summary>
    public void SetCostInBaseCurrency(decimal? costInBaseCurrency)
    {
        if (costInBaseCurrency.HasValue && costInBaseCurrency < 0)
            throw new DomainException("التكلفة بعملة الأساس لا يمكن أن تكون سالبة.");

        CostInBaseCurrency = costInBaseCurrency;
    }
}
