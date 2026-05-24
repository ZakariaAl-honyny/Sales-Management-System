using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoice : BaseEntity
{
    public string InvoiceNo { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public int? CashBoxId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal DueAmount { get; private set; }
    public string? SupplierInvoiceNo { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual List<PurchaseInvoiceItem> Items { get; private set; } = new();

    private PurchaseInvoice() { }

    public static PurchaseInvoice Create(
        string invoiceNo,
        int supplierId,
        int warehouseId,
        DateTime? invoiceDate = null,
        DateOnly? dueDate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        string? supplierInvoiceNo = null,
        string? notes = null,
        int? cashBoxId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new DomainException("رقم الفاتورة مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (dueDate.HasValue && dueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new DomainException("تاريخ الاستحقاق لا يمكن أن يكون في الماضي.");

        var invoice = new PurchaseInvoice
        {
            InvoiceNo = invoiceNo,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            CashBoxId = cashBoxId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            DueDate = dueDate,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            SupplierInvoiceNo = supplierInvoiceNo,
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