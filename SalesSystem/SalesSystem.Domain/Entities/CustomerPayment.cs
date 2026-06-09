using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class CustomerPayment : BaseEntity
{
    public string PaymentNo { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public int? SalesInvoiceId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>
    /// Payment method: Cash, Cheque, BankTransfer, or CreditCard.
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// FK to CashBox when payment is received via a cash register.
    /// </summary>
    public int? CashBoxId { get; private set; }

    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public Currency? Currency { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public virtual Customer? Customer { get; private set; }
    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual CashBox? CashBox { get; private set; }

    /// <summary>
    /// The cheque associated with this payment (when PaymentMethod = Cheque).
    /// </summary>
    public virtual Cheque? Cheque { get; private set; }

    /// <summary>
    /// Allocations of this payment across multiple invoices.
    /// </summary>
    private readonly List<PaymentAllocation> _allocations = new();
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    private CustomerPayment() { }

    public static CustomerPayment Create(
        string paymentNo,
        int customerId,
        decimal amount,
        PaymentMethod paymentMethod,
        int? salesInvoiceId = null,
        string? referenceNo = null,
        string? notes = null,
        int? currencyId = null,
        decimal? exchangeRate = null,
        int? cashBoxId = null,
        int? createdByUserId = null,
        DateTime? paymentDate = null)
    {
        if (string.IsNullOrWhiteSpace(paymentNo))
            throw new DomainException("رقم السداد مطلوب.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var payment = new CustomerPayment
        {
            PaymentNo = paymentNo,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            SalesInvoiceId = salesInvoiceId,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            ReferenceNo = referenceNo,
            Notes = notes,
            CashBoxId = cashBoxId,
            PaymentDate = paymentDate ?? DateTime.UtcNow
        };
        payment.SetCreatedBy(createdByUserId);
        return payment;
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

    /// <summary>
    /// Replaces the current allocations with the given set.
    /// </summary>
    public void UpdateAllocations(IEnumerable<PaymentAllocation> newAllocations)
    {
        _allocations.Clear();
        _allocations.AddRange(newAllocations);
        UpdateTimestamp();
    }
}
