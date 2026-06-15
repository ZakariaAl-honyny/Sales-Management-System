using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SupplierPayment : DocumentEntity
{
    public string PaymentNo { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>
    /// Payment method: Cash, BankTransfer, or CreditCard.
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// FK to CashBox when payment is made from a cash register.
    /// </summary>
    public int? CashBoxId { get; private set; }

    public short? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public Currency? Currency { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation
    public virtual Supplier? Supplier { get; private set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual CashBox? CashBox { get; private set; }

    private SupplierPayment() { }

    public static SupplierPayment Create(
        string paymentNo,
        int supplierId,
        decimal amount,
        PaymentMethod paymentMethod,
        int? purchaseInvoiceId = null,
        string? referenceNo = null,
        string? notes = null,
        short? currencyId = null,
        decimal? exchangeRate = null,
        int? cashBoxId = null,
        int? createdByUserId = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new DomainException("رقم السداد مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var payment = new SupplierPayment
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PurchaseInvoiceId = purchaseInvoiceId,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            ReferenceNo = referenceNo,
            Notes = notes,
            CashBoxId = cashBoxId,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Status = InvoiceStatus.Draft
        };
        payment.SetCreatedBy(createdByUserId);
        return payment;
    }

    public void Post(DateTime? postedAt = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط سندات الصرف المسودة يمكن ترحيلها.");

        Status = InvoiceStatus.Posted;
        PostedAt = postedAt ?? DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("سند الصرف ملغي بالفعل.");

        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }

    public void Update(
        decimal amount,
        PaymentMethod paymentMethod,
        DateTime? paymentDate,
        string? notes,
        int? cashBoxId = null,
        int? updatedByUserId = null)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        Amount = amount;
        PaymentMethod = paymentMethod;
        CashBoxId = cashBoxId;
        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value.Kind == DateTimeKind.Utc ? paymentDate.Value : paymentDate.Value.ToUniversalTime();
        if (notes != null) Notes = notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
