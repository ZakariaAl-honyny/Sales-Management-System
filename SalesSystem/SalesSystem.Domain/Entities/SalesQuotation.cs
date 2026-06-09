using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// عرض سعر (أمر بيع) — مستند تقديم عرض سعر للعميل
/// </summary>
public class SalesQuotation : BaseEntity
{
    public string QuotationNo { get; private set; } = string.Empty;
    public int? CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime QuotationDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public QuotationStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }

    // Navigation properties
    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual List<SalesQuotationItem> Items { get; private set; } = new();

    private SalesQuotation() { }

    public static SalesQuotation Create(
        string quotationNo,
        int warehouseId,
        int? customerId = null,
        DateTime? quotationDate = null,
        DateTime? expiryDate = null,
        decimal discountAmount = 0,
        string? notes = null,
        int? currencyId = null,
        decimal? exchangeRate = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(quotationNo))
            throw new DomainException("رقم عرض السعر مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (expiryDate.HasValue && expiryDate < quotationDate.GetValueOrDefault(DateTime.UtcNow))
            throw new DomainException("تاريخ الانتهاء لا يمكن أن يكون قبل تاريخ العرض.");
        if (currencyId.HasValue && (!exchangeRate.HasValue || exchangeRate <= 0))
            throw new DomainException("سعر الصرف مطلوب عند اختيار عملة أجنبية.");
        if (!string.IsNullOrEmpty(notes) && notes.Length > 500)
            throw new DomainException("الملاحظات لا تتجاوز 500 حرف.");

        var quotation = new SalesQuotation
        {
            QuotationNo = quotationNo,
            WarehouseId = warehouseId,
            CustomerId = customerId,
            QuotationDate = quotationDate ?? DateTime.UtcNow,
            ExpiryDate = expiryDate,
            Status = QuotationStatus.Draft,
            SubTotal = 0,
            DiscountAmount = discountAmount,
            TaxAmount = 0,
            TotalAmount = 0,
            Notes = notes,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            IsActive = true
        };
        quotation.SetCreatedBy(createdByUserId);
        return quotation;
    }

    public void AddItem(SalesQuotationItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لعرض سعر غير مسودة.");

        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(SalesQuotationItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن حذف أصناف من عرض سعر غير مسودة.");

        Items.Remove(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
    }

    /// <summary>
    /// تأكيد عرض السعر — يغير الحالة إلى مؤكد
    /// </summary>
    public void Confirm()
    {
        if (Status != QuotationStatus.Draft)
            throw new DomainException("يمكن تأكيد عروض الأسعار بحالة مسودة فقط.");
        if (!Items.Any())
            throw new DomainException("لا يمكن تأكيد عرض سعر بدون أصناف.");

        Status = QuotationStatus.Confirmed;
        UpdateTimestamp();
    }

    /// <summary>
    /// تعيين عرض السعر كمنتهي الصلاحية
    /// </summary>
    public void Expire()
    {
        if (Status == QuotationStatus.Expired)
            throw new DomainException("عرض السعر منتهي الصلاحية بالفعل.");
        if (Status == QuotationStatus.Converted)
            throw new DomainException("لا يمكن إنهاء عرض سعر تم تحويله لفاتورة.");
        if (Status == QuotationStatus.Draft)
            throw new DomainException("لا يمكن إنهاء عرض سعر في حالة مسودة — يجب تأكيده أولاً.");

        Status = QuotationStatus.Expired;
        UpdateTimestamp();
    }

    /// <summary>
    /// تعيين عرض السعر كتم تحويله لفاتورة بيع
    /// </summary>
    public void MarkAsConverted()
    {
        if (Status != QuotationStatus.Confirmed)
            throw new DomainException("يمكن تحويل عروض الأسعار المؤكدة فقط.");
        if (!Items.Any())
            throw new DomainException("لا يمكن تحويل عرض سعر بدون أصناف.");

        Status = QuotationStatus.Converted;
        UpdateTimestamp();
    }

    /// <summary>
    /// إلغاء عرض السعر
    /// </summary>
    public void Cancel()
    {
        if (Status == QuotationStatus.Converted)
            throw new DomainException("لا يمكن إلغاء عرض سعر تم تحويله لفاتورة.");
        if (Status == QuotationStatus.Expired)
            throw new DomainException("عرض السعر منتهي الصلاحية بالفعل.");

        Status = QuotationStatus.Expired;
        UpdateTimestamp();
    }
}

/// <summary>
/// صنف عرض السعر — منتج معروض ضمن عرض السعر
/// </summary>
public class SalesQuotationItem : BaseEntity
{
    public int SalesQuotationId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public byte Mode { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual SalesQuotation? Quotation { get; private set; }
    public virtual Product? Product { get; private set; }

    private SalesQuotationItem() { }

    public static SalesQuotationItem Create(
        int productId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        byte mode = 1,
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
        if (!string.IsNullOrEmpty(notes) && notes.Length > 250)
            throw new DomainException("ملاحظات الصنف لا تتجاوز 250 حرف.");

        var item = new SalesQuotationItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            Mode = mode,
            Notes = notes,
            IsActive = true
        };
        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
    }
}
