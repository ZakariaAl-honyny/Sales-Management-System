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

    public DateOnly PaymentDate { get; private set; }
    public int SupplierId { get; private set; }
    public int? CashBoxId { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>
    /// The payment amount converted to the base currency using the exchange rate.
    /// Null when no exchange rate is specified (same as Amount).
    /// </summary>
    public decimal? BaseNetTotal { get; private set; }

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
    private readonly List<SupplierPaymentApplication> _applications = new();
    public IReadOnlyCollection<SupplierPaymentApplication> Applications => _applications.AsReadOnly();

    private SupplierPayment() { } // EF Core

    public static SupplierPayment Create(
        int paymentNo,
        int supplierId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? referenceNo = null,
        string? notes = null,
        int? cashBoxId = null,
        int? createdByUserId = null,
        DateOnly? paymentDate = null,
        decimal? baseNetTotal = null)
    {
        if (paymentNo <= 0)
            throw new DomainException("رقم السداد مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var payment = new SupplierPayment
        {
            PaymentNo = paymentNo,
            SupplierId = supplierId,
            CashBoxId = cashBoxId,
            Amount = amount,
            BaseNetTotal = baseNetTotal,
            PaymentMethod = paymentMethod,
            ReferenceNo = referenceNo,
            Notes = notes,
            PaymentDate = paymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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
        DateOnly? paymentDate,
        string? notes,
        decimal? baseNetTotal = null,
        int? updatedByUserId = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تعديل سند صرف مرحل أو ملغي");

        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        Amount = amount;
        BaseNetTotal = baseNetTotal;
        PaymentMethod = paymentMethod;
        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value;
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
