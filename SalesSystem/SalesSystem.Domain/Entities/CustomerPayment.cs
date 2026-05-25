using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class CustomerPayment : BaseEntity
{
    public string PaymentNo { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public int? SalesInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public byte PaymentMethod { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }

    public virtual Customer? Customer { get; private set; }
    public virtual SalesInvoice? SalesInvoice { get; private set; }

    private CustomerPayment() { }

    public static CustomerPayment Create(
        string paymentNo,
        int customerId,
        decimal amount,
        byte paymentMethod,
        int? salesInvoiceId = null,
        string? referenceNo = null,
        string? notes = null,
        int? createdByUserId = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new DomainException("رقم السداد مطلوب.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var payment = new CustomerPayment
        {
            PaymentNo = paymentNo,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            SalesInvoiceId = salesInvoiceId,
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