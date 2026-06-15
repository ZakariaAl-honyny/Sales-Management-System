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
    public decimal TaxAmount { get; private set; }
    /// <summary>
    /// مصاريف إضافية / أخرى (شحن، تغليف، ...)
    /// </summary>
    public decimal OtherCharges { get; private set; }
    public decimal NetTotal { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public int InvoiceNo { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // ─── Computed Properties ─────────────────────────────────────────────

    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual Currency? Currency { get; private set; }
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
        short currencyId = 0,
        decimal? exchangeRate = null,
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
        if (currencyId > 0 && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var invoice = new SalesInvoice
        {
            WarehouseId = warehouseId,
            InvoiceNo = invoiceNo,
            CashBoxId = cashBoxId,
            TaxId = taxId,
            CustomerId = customerId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            OtherCharges = otherCharges,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
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

    public void UpdateTotals(decimal discountAmount, decimal taxAmount)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");

        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        RecalculateTotals();
    }
}
