using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// فاتورة شراء — مع دعم العملات المتعددة
/// </summary>
public class PurchaseInvoice : DocumentEntity
{
    public int SupplierId { get; private set; }
    public short WarehouseId { get; private set; }
    public short? TaxId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DiscountType? DiscountType { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal OtherCharges { get; private set; }
    public decimal NetTotal { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public int InvoiceNo { get; private set; }
    public short? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public string? AttachmentPath { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual Tax? Tax { get; private set; }
    public virtual List<PurchaseInvoiceItem> Items { get; private set; } = new();
    public virtual List<AdditionalFee> AdditionalFees { get; private set; } = new();

    private PurchaseInvoice() { }

    public static PurchaseInvoice Create(
        int supplierId,
        short warehouseId,
        int invoiceNo,
        DateTime? invoiceDate = null,
        DateOnly? dueDate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        DiscountType? discountType = null,
        decimal? discountRate = null,
        decimal otherCharges = 0,
        string? notes = null,
        short? taxId = null,
        short? currencyId = null,
        decimal? exchangeRate = null,
        int? createdByUserId = null)
    {
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (invoiceNo <= 0)
            throw new DomainException("رقم الفاتورة غير صحيح.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (otherCharges < 0)
            throw new DomainException("مصاريف إضافية لا يمكن أن تكون سالبة.");
        if (dueDate.HasValue && dueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new DomainException("تاريخ الاستحقاق لا يمكن أن يكون في الماضي.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");
        if (discountType == Enums.DiscountType.Percentage && (!discountRate.HasValue || discountRate < 0 || discountRate > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");
        if (discountType != Enums.DiscountType.Percentage && discountRate.HasValue)
            throw new DomainException("نسبة الخصم تستخدم فقط مع خصم النسبة المئوية.");

        var invoice = new PurchaseInvoice
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            InvoiceNo = invoiceNo,
            TaxId = taxId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            DueDate = dueDate,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            DiscountType = discountType,
            DiscountRate = discountRate,
            OtherCharges = otherCharges,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        invoice.SetCreatedBy(createdByUserId);
        return invoice;
    }

    public void AddItem(PurchaseInvoiceItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لفاتورة غير مسودة.");

        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(PurchaseInvoiceItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن حذف أصناف من فاتورة غير مسودة.");

        Items.Remove(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        NetTotal = SubTotal - DiscountAmount + TaxAmount + OtherCharges;
        RemainingAmount = NetTotal - PaidAmount;
    }

    public void SetPaidAmount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("المبلغ المدفوع لا يمكن أن يكون سالباً.");
        if (amount > NetTotal)
            throw new DomainException("المبلغ المدفوع أكبر من الإجمالي.");

        PaidAmount = amount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        TaxAmount = taxAmount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetOtherCharges(decimal otherCharges)
    {
        if (otherCharges < 0)
            throw new DomainException("مصاريف إضافية لا يمكن أن تكون سالبة.");
        OtherCharges = otherCharges;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetTax(short? taxId, decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        TaxId = taxId;
        TaxAmount = taxAmount;
        RecalculateTotals();
    }

    /// <summary>
    /// Sets discount with support for both amount and percentage discount types.
    /// </summary>
    public void SetDiscount(decimal discountAmount, DiscountType? discountType = null, decimal? discountRate = null)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (discountType == Enums.DiscountType.Percentage && (!discountRate.HasValue || discountRate < 0 || discountRate > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");
        if (discountType != Enums.DiscountType.Percentage && discountRate.HasValue)
            throw new DomainException("نسبة الخصم تستخدم فقط مع خصم النسبة المئوية.");

        DiscountAmount = discountAmount;
        DiscountType = discountType;
        DiscountRate = discountRate;
        RecalculateTotals();
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets the attachment file path for this invoice.
    /// </summary>
    public void SetAttachment(string? attachmentPath)
    {
        AttachmentPath = attachmentPath;
        UpdateTimestamp();
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط الفواتير المسودة يمكن ترحيلها.");

        if (!Items.Any())
            throw new DomainException("لا يمكن ترحيل فاتورة بدون أصناف.");

        RecalculateTotals();
        PostedAt = DateTime.UtcNow;
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("الفاتورة ملغاة بالفعل.");

        if (PaidAmount > 0)
            throw new DomainException("لا يمكن إلغاء فاتورة مدفوعة مباشرة.");

        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
    }

    public void UpdateTotals(decimal discountAmount, decimal taxAmount, decimal otherCharges = 0, DiscountType? discountType = null, decimal? discountRate = null)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        if (otherCharges < 0)
            throw new DomainException("مصاريف إضافية لا يمكن أن تكون سالبة.");

        DiscountAmount = discountAmount;
        DiscountType = discountType;
        DiscountRate = discountRate;
        TaxAmount = taxAmount;
        OtherCharges = otherCharges;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetCurrency(short? currencyId, decimal? exchangeRate)
    {
        if (currencyId.HasValue && (!exchangeRate.HasValue || exchangeRate <= 0))
            throw new DomainException("سعر الصرف مطلوب عند اختيار عملة أجنبية.");
        CurrencyId = currencyId;
        ExchangeRate = exchangeRate;
        UpdateTimestamp();
    }
}
