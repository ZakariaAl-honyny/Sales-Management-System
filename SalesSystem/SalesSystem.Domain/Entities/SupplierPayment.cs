using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SupplierPayment : BaseEntity
{
    public string PaymentNo { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public byte PaymentMethod { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }

    public virtual Supplier? Supplier { get; private set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }

    private SupplierPayment() { }

    public static SupplierPayment Create(
        string paymentNo,
        int supplierId,
        decimal amount,
        byte paymentMethod,
        int? purchaseInvoiceId = null,
        string? referenceNo = null,
        string? notes = null,
        int? createdByUserId = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new DomainException("رقم السداد مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var payment = new SupplierPayment
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PurchaseInvoiceId = purchaseInvoiceId,
            ReferenceNo = referenceNo,
            Notes = notes,
            PaymentDate = paymentDate ?? DateTime.UtcNow
        };
        payment.SetCreatedBy(createdByUserId);
        return payment;
    }

    public void Update(decimal amount, byte paymentMethod, DateTime? paymentDate, string? notes, int? updatedByUserId = null)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        
        Amount = amount;
        PaymentMethod = paymentMethod;
        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value.Kind == DateTimeKind.Utc ? paymentDate.Value : paymentDate.Value.ToUniversalTime();
        if (notes != null) Notes = notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}