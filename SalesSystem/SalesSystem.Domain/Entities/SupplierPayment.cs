using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a payment made to a supplier.
/// Schema 7.5: SupplierPayments. Columns: PaymentNo (int unique), PaymentDate, SupplierId FK,
/// CashBoxId FK (int NOT null), CurrencyId FK (smallint NOT null), Amount, Notes, Status, audit fields.
/// </summary>
public class SupplierPayment : DocumentEntity
{
    /// <summary>
    /// User-facing payment number (int, unique per table).
    /// </summary>
    public int PaymentNo { get; private set; }

    public DateTime PaymentDate { get; private set; }
    public int SupplierId { get; private set; }
    public int CashBoxId { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>
    /// Payment method: Cash, BankTransfer, or CreditCard.
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// An optional external reference number (e.g., bank transfer ref).
    /// </summary>
    public string? ReferenceNo { get; private set; }

    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Supplier? Supplier { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual Currency? Currency { get; private set; }

    private readonly List<SupplierPaymentApplication> _applications = new();
    public IReadOnlyCollection<SupplierPaymentApplication> Applications => _applications.AsReadOnly();

    private SupplierPayment() { } // EF Core

    public static SupplierPayment Create(
        int paymentNo,
        int supplierId,
        int cashBoxId,
        short currencyId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? referenceNo = null,
        string? notes = null,
        int? createdByUserId = null,
        DateTime? paymentDate = null)
    {
        if (paymentNo <= 0)
            throw new DomainException("رقم السداد مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var payment = new SupplierPayment
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            CashBoxId = cashBoxId,
            CurrencyId = currencyId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNo = referenceNo,
            Notes = notes,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Status = InvoiceStatus.Draft
        };
        payment.SetCreatedBy(createdByUserId);
        return payment;
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط سندات الصرف المسودة يمكن ترحيلها.");
        PostedAt = DateTime.UtcNow;
        Status = InvoiceStatus.Posted;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("سند الصرف ملغي بالفعل.");
        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }

    public void Update(
        decimal amount,
        PaymentMethod paymentMethod,
        DateTime? paymentDate,
        string? notes,
        int? updatedByUserId = null)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        Amount = amount;
        PaymentMethod = paymentMethod;
        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value.Kind == DateTimeKind.Utc
                ? paymentDate.Value
                : paymentDate.Value.ToUniversalTime();
        if (notes != null) Notes = notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Adds an application of this payment amount to a specific purchase invoice.
    /// Only allowed while the payment is in Draft status.
    /// </summary>
    public void AddApplication(SupplierPaymentApplication application)
    {
        if (application == null)
            throw new DomainException("التخصيص مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة تخصيصات لسند غير مسود.");
        _applications.Add(application);
        UpdateTimestamp();
    }
}
