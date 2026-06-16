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
    public DateOnly InvoiceDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal OtherCharges { get; private set; }
    public decimal NetTotal { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public int InvoiceNo { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public int? CashBoxId { get; private set; }
    public string? Notes { get; private set; }
    public string? SupplierInvoiceNo { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual Tax? Tax { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual List<PurchaseInvoiceLine> Items { get; private set; } = new();

    private PurchaseInvoice() { }

    public static PurchaseInvoice Create(
        int supplierId,
        short warehouseId,
        int invoiceNo,
        DateOnly? invoiceDate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        decimal otherCharges = 0,
        string? notes = null,
        string? supplierInvoiceNo = null,
        short? taxId = null,
        short currencyId = 1,
        decimal? exchangeRate = null,
        int? cashBoxId = null,
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
        if (currencyId <= 0)
            throw new DomainException("العملة غير صالحة.");
        if (exchangeRate.HasValue && exchangeRate.Value <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");

        var invoice = new PurchaseInvoice
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            InvoiceNo = invoiceNo,
            TaxId = taxId,
            InvoiceDate = invoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            OtherCharges = otherCharges,
            SupplierInvoiceNo = supplierInvoiceNo?.Trim(),
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            CashBoxId = cashBoxId,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        invoice.SetCreatedBy(createdByUserId);
        return invoice;
    }

    public void AddItem(PurchaseInvoiceLine item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لفاتورة غير مسودة.");

        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(PurchaseInvoiceLine item)
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
    /// Sets discount with a fixed amount.
    /// </summary>
    public void SetDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        DiscountAmount = discountAmount;
        RecalculateTotals();
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

    public void UpdateTotals(decimal discountAmount, decimal taxAmount, decimal otherCharges = 0)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        if (otherCharges < 0)
            throw new DomainException("مصاريف إضافية لا يمكن أن تكون سالبة.");

        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        OtherCharges = otherCharges;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetCurrency(short currencyId, decimal? exchangeRate)
    {
        if (currencyId <= 0)
            throw new DomainException("العملة غير صالحة.");
        if (exchangeRate.HasValue && exchangeRate.Value <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        CurrencyId = currencyId;
        ExchangeRate = exchangeRate;
        UpdateTimestamp();
    }
}
