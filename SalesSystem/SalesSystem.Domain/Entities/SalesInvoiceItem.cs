using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SalesInvoiceItem : Entity
{
    public int SalesInvoiceId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; } = SaleMode.Retail;
    public string? Notes { get; private set; }

    // ─── Phase 28: Profit Tracking & Price Override ─────────────────
    /// <summary>
    /// تكلفة الوحدة بعملة الأساس — تستخدم لحساب الربح
    /// </summary>
    public decimal? CostInBaseCurrency { get; private set; }

    /// <summary>
    /// هل تم تجاوز السعر يدوياً (صلاحية خاصة)
    /// </summary>
    public bool IsPriceOverridden { get; private set; }

    /// <summary>
    /// معرف الوحدة المستخدمة للصنف (ProductUnit)
    /// </summary>
    public int? ProductUnitId { get; private set; }

    // ─── Computed Properties ─────────────────────────────────────────
    /// <summary>
    /// الربح المحقق: إجمالي السطر - (التكلفة × الكمية)
    /// </summary>
    public decimal Profit => LineTotal - ((CostInBaseCurrency ?? 0) * Quantity);

    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private SalesInvoiceItem() { }

    public static SalesInvoiceItem Create(
        int productId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        SaleMode mode = SaleMode.Retail,
        string? notes = null,
        decimal? costInBaseCurrency = null,
        bool isPriceOverridden = false,
        int? productUnitId = null)
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
            Notes = notes,
            CostInBaseCurrency = costInBaseCurrency,
            IsPriceOverridden = isPriceOverridden,
            ProductUnitId = productUnitId
        };

        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
    }
}