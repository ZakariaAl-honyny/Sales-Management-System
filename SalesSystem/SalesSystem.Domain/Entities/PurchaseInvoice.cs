using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoice : BaseEntity
{
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public int? CashBoxId { get; private set; }
    public int? TaxId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal DueAmount { get; private set; }
    public int InvoiceNo { get; private set; }
    public string? SupplierInvoiceNo { get; private set; }
    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual Tax? Tax { get; private set; }
    public virtual List<PurchaseInvoiceItem> Items { get; private set; } = new();

    private PurchaseInvoice() { }

    public static PurchaseInvoice Create(
        int supplierId,
        int warehouseId,
        int invoiceNo,
        DateTime? invoiceDate = null,
        DateOnly? dueDate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        string? supplierInvoiceNo = null,
        string? notes = null,
        int? cashBoxId = null,
        int? taxId = null,
        int? currencyId = null,
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
        if (dueDate.HasValue && dueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new DomainException("تاريخ الاستحقاق لا يمكن أن يكون في الماضي.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var invoice = new PurchaseInvoice
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            InvoiceNo = invoiceNo,
            CashBoxId = cashBoxId,
            TaxId = taxId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            DueDate = dueDate,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            SupplierInvoiceNo = supplierInvoiceNo,
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
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
        DueAmount = TotalAmount - PaidAmount;
    }

    public void SetPaidAmount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("المبلغ المدفوع لا يمكن أن يكون سالباً.");
        if (amount > TotalAmount)
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

    public void SetTax(int? taxId, decimal taxAmount)
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
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("الفاتورة ملغاة بالفعل.");

        if (PaidAmount > 0)
            throw new DomainException("لا يمكن إلغاء فاتورة مدفوعة مباشرة.");

        Status = InvoiceStatus.Cancelled;
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