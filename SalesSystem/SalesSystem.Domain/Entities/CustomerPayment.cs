using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class CustomerPayment
{
    public int CustomerPaymentId { get; private set; }
    public string PaymentNo { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public int? SalesInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public byte PaymentMethod { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
        string? createdBy = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new ArgumentException("PaymentNo is required.", nameof(paymentNo));
        if (customerId <= 0)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        return new CustomerPayment
        {
            PaymentNo = paymentNo,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            SalesInvoiceId = salesInvoiceId,
            ReferenceNo = referenceNo,
            Notes = notes,
            CreatedBy = createdBy,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}