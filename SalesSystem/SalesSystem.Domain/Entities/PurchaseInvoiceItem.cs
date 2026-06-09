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
    public decimal LineTotal { get; private set; }
    public decimal? CostInBaseCurrency { get; private set; }
    public decimal AdditionalFeesAmount { get; private set; }
    public SaleMode Mode { get; private set; } = SaleMode.Retail;
    public string? Notes { get; private set; }

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
        Enums.DiscountType discountType = Enums.DiscountType.Amount,
        decimal? discountRate = null,
        SaleMode mode = SaleMode.Retail,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (discountType == Enums.DiscountType.Percentage && (!discountRate.HasValue || discountRate < 0 || discountRate > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");

        var item = new PurchaseInvoiceItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
            DiscountType = discountType,
            DiscountRate = discountRate,
            Mode = mode,
            Notes = notes
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        var discount = DiscountType == Enums.DiscountType.Percentage
            ? (Quantity * UnitCost) * (DiscountRate ?? 0) / 100m
            : DiscountAmount;
        DiscountAmount = discount;
        LineTotal = (Quantity * UnitCost) - DiscountAmount;
    }

    public void SetAdditionalFeesAllocation(decimal allocatedAmount)
    {
        if (allocatedAmount < 0)
            throw new DomainException("مبلغ الرسوم الإضافية لا يمكن أن يكون سالباً.");
        AdditionalFeesAmount = allocatedAmount;
    }

    public void SetCostInBaseCurrency(decimal? costInBaseCurrency)
    {
        CostInBaseCurrency = costInBaseCurrency;
    }
}