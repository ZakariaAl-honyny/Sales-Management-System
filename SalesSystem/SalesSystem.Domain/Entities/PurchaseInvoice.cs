using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseInvoice : BaseEntity
{
    public string InvoiceNo { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal DueAmount { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
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
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new ArgumentException("InvoiceNo is required.", nameof(invoiceNo));
        if (supplierId <= 0)
            throw new ArgumentException("SupplierId is required.", nameof(supplierId));
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));

        var invoice = new PurchaseInvoice
        {
            InvoiceNo = invoiceNo,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            DueDate = dueDate,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        invoice.SetCreatedBy(createdByUserId);
        return invoice;
    }

    public void AddItem(PurchaseInvoiceItem item)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Cannot add items to a non-draft invoice.");
        
        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(PurchaseInvoiceItem item)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Cannot remove items from a non-draft invoice.");
        
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
            throw new ArgumentException("PaidAmount cannot be negative.", nameof(amount));
        if (amount > TotalAmount)
            throw new DomainException("المبلغ المدفوع أكبر من الإجمالي");
        
        PaidAmount = amount;
        RecalculateTotals();
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("TaxAmount cannot be negative.", nameof(taxAmount));
        TaxAmount = taxAmount;
        RecalculateTotals();
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Only draft invoices can be posted.");
        
        if (!Items.Any())
            throw new DomainException("Cannot post an invoice with no items.");
        
        RecalculateTotals();
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        // 1. التحقق من الحالة الحالية (Invariants)
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("Invoice is already cancelled.");
            
        // Check if PaidAmount > 0 because InvoiceStatus.Paid does not exist in the enum
        if (PaidAmount > 0)
            throw new DomainException("Cannot directly cancel a paid invoice.");

        // 2. تغيير الحالة
        Status = InvoiceStatus.Cancelled;

        // 3. إطلاق حدث لتقوم الأنظمة الأخرى (مثل المخازن أو الحسابات) بالتفاعل
        // AddDomainEvent(new InvoiceCancelledDomainEvent(this));
    }

    public void UpdateTotals(decimal discountAmount, decimal taxAmount)
    {
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        RecalculateTotals();
    }
}