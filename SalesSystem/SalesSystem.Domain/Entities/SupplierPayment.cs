using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class SupplierPayment
{
    public int SupplierPaymentId { get; private set; }
    public string PaymentNo { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public byte PaymentMethod { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
        string? createdBy = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new ArgumentException("PaymentNo is required.", nameof(paymentNo));
        if (supplierId <= 0)
            throw new ArgumentException("SupplierId is required.", nameof(supplierId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        return new SupplierPayment
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PurchaseInvoiceId = purchaseInvoiceId,
            ReferenceNo = referenceNo,
            Notes = notes,
            CreatedBy = createdBy,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}