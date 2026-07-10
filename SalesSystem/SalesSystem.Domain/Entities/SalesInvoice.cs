using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SalesInvoice : DocumentEntity
{
    public int CustomerId { get; private set; }
    public short WarehouseId { get; private set; }
    public int? CashBoxId { get; private set; }
    public short? TaxId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    /// <summary>
    /// مصاريف إضافية / أخرى (شحن، تغليف، ...)
    /// </summary>
    public decimal OtherCharges { get; private set; }
    public decimal NetTotal { get; private set; }
    /// <summary>
    /// القيمة المكافئة بالعملة الأساسية (NetTotal × ExchangeRate)
    /// </summary>
    public decimal? BaseNetTotal { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public decimal? CostInBaseCurrency { get; private set; }
    public int InvoiceNo { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // ─── Computed Properties ─────────────────────────────────────────────

    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual Tax? Tax { get; private set; }
    public virtual List<SalesInvoiceLine> Items { get; private set; } = new();

    private SalesInvoice() { }

    public static SalesInvoice Create(
        short warehouseId,
        int invoiceNo,
        int customerId,
        DateTime? invoiceDate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        decimal otherCharges = 0,
        string? notes = null,
        int? cashBoxId = null,
        short? taxId = null,
        DiscountType discountType = DiscountType.Amount,
        decimal? discountRate = null,
        decimal? baseNetTotal = null,
        int? createdByUserId = null)
    {
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (invoiceNo <= 0)
            throw new DomainException("رقم الفاتورة غير صحيح.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (otherCharges < 0)
            throw new DomainException("المصاريف الإضافية لا يمكن أن تكون سالبة.");
        if (discountType == DiscountType.Percentage && (!discountRate.HasValue || discountRate.Value <= 0))
            throw new DomainException("نسبة الخصم يجب أن تكون أكبر من صفر عند اختيار الخصم بالنسبة المئوية.");
        if (discountType == DiscountType.Percentage && discountAmount != 0)
            throw new DomainException("لا يمكن تحديد خصم بقيمة ثابتة ونسبة مئوية معاً.");

        var invoice = new SalesInvoice
        {
            WarehouseId = warehouseId,
            InvoiceNo = invoiceNo,
            CashBoxId = cashBoxId,
            TaxId = taxId,
            CustomerId = customerId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            PaymentType = paymentType,
            DiscountType = discountType,
            DiscountRate = discountType == DiscountType.Percentage ? discountRate : null,
            DiscountAmount = discountType == DiscountType.Percentage ? 0 : discountAmount,
            OtherCharges = otherCharges,
            Notes = notes,
            BaseNetTotal = baseNetTotal,
            Status = InvoiceStatus.Draft
        };
        invoice.SetCreatedBy(createdByUserId);
        return invoice;
    }

    public void AddItem(SalesInvoiceLine item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لفاتورة غير مسودة.");

        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(SalesInvoiceLine item)
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

        // Compute discount based on type
        if (DiscountType == DiscountType.Percentage && DiscountRate.HasValue && DiscountRate.Value > 0)
        {
            DiscountAmount = SubTotal * DiscountRate.Value / 100m;
        }

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
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        TaxAmount = taxAmount;
        RecalculateTotals();
    }

    public void SetTax(short? taxId, decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        TaxId = taxId;
        TaxAmount = taxAmount;
        RecalculateTotals();
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

    public void SetOtherCharges(decimal otherCharges)
    {
        if (otherCharges < 0)
            throw new DomainException("المصاريف الإضافية لا يمكن أن تكون سالبة.");
        OtherCharges = otherCharges;
        RecalculateTotals();
    }

    /// <summary>
    /// Sets discount with a fixed amount (resets discount type to Amount).
    /// </summary>
    public void SetDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        DiscountType = DiscountType.Amount;
        DiscountRate = null;
        DiscountAmount = discountAmount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets discount with type and optional rate.
    /// </summary>
    public void SetDiscount(DiscountType type, decimal? rate)
    {
        if (type == DiscountType.Percentage)
        {
            if (!rate.HasValue || rate.Value <= 0)
                throw new DomainException("نسبة الخصم يجب أن تكون أكبر من صفر.");
            if (rate.Value > 100)
                throw new DomainException("نسبة الخصم لا يمكن أن تتجاوز 100%.");
        }

        DiscountType = type;
        DiscountRate = type == DiscountType.Percentage ? rate : null;
        if (type == DiscountType.Percentage)
        {
            // DiscountAmount will be computed in RecalculateTotals
            DiscountAmount = 0;
        }
        RecalculateTotals();
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets the computed cost in base currency.
    /// </summary>
    public void SetCostInBaseCurrency(decimal cost)
    {
        if (cost < 0)
            throw new DomainException("التكلفة بعملة الأساس لا يمكن أن تكون سالبة.");
        CostInBaseCurrency = cost;
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets the base-currency equivalent of NetTotal (NetTotal × ExchangeRate).
    /// </summary>
    public void SetBaseNetTotal(decimal baseNetTotal)
    {
        if (baseNetTotal < 0)
            throw new DomainException("القيمة بالعملة الأساسية لا يمكن أن تكون سالبة.");
        BaseNetTotal = baseNetTotal;
        UpdateTimestamp();
    }

    public void UpdateTotals(decimal discountAmount, decimal taxAmount, decimal otherCharges = 0,
        DiscountType? discountType = null, decimal? discountRate = null)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        if (otherCharges < 0)
            throw new DomainException("المصاريف الإضافية لا يمكن أن تكون سالبة.");

        if (discountType.HasValue)
        {
            if (discountType.Value == DiscountType.Percentage && (!discountRate.HasValue || discountRate.Value <= 0))
                throw new DomainException("نسبة الخصم يجب أن تكون أكبر من صفر عند اختيار الخصم بالنسبة المئوية.");

            DiscountType = discountType.Value;
            DiscountRate = discountType.Value == DiscountType.Percentage ? discountRate : null;
        }

        DiscountAmount = DiscountType == DiscountType.Percentage ? 0 : discountAmount;
        TaxAmount = taxAmount;
        OtherCharges = otherCharges;
        RecalculateTotals();
        UpdateTimestamp();
    }
}
