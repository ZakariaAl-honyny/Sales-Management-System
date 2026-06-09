using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a cheque used for customer or supplier payments.
/// Tracks lifecycle: Pending → Cleared | Bounced | Cancelled.
/// </summary>
public class Cheque : BaseEntity
{
    public string ChequeNumber { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public DateTime MaturityDate { get; private set; }
    public ChequeStatus Status { get; private set; } = ChequeStatus.Pending;
    public decimal Amount { get; private set; }

    /// <summary>
    /// FK to CustomerPayment when this cheque is linked to a customer payment.
    /// </summary>
    public int? CustomerPaymentId { get; private set; }

    /// <summary>
    /// FK to SupplierPayment when this cheque is linked to a supplier payment.
    /// </summary>
    public int? SupplierPaymentId { get; private set; }

    public string? Notes { get; private set; }

    // Navigation
    public virtual CustomerPayment? CustomerPayment { get; private set; }
    public virtual SupplierPayment? SupplierPayment { get; private set; }

    private Cheque() { } // EF Core

    public static Cheque Create(
        string chequeNumber,
        string bankName,
        DateTime issueDate,
        DateTime maturityDate,
        decimal amount,
        int? customerPaymentId = null,
        int? supplierPaymentId = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(chequeNumber))
            throw new DomainException("رقم الشيك مطلوب");

        if (string.IsNullOrWhiteSpace(bankName))
            throw new DomainException("اسم البنك مطلوب");

        if (maturityDate < issueDate)
            throw new DomainException("تاريخ الاستحقاق يجب أن يكون بعد تاريخ الإصدار");

        if (amount <= 0)
            throw new DomainException("قيمة الشيك يجب أن تكون أكبر من الصفر");

        var cheque = new Cheque
        {
            ChequeNumber = chequeNumber.Trim(),
            BankName = bankName.Trim(),
            IssueDate = issueDate.Kind == DateTimeKind.Utc ? issueDate : issueDate.ToUniversalTime(),
            MaturityDate = maturityDate.Kind == DateTimeKind.Utc ? maturityDate : maturityDate.ToUniversalTime(),
            Status = ChequeStatus.Pending,
            Amount = amount,
            CustomerPaymentId = customerPaymentId,
            SupplierPaymentId = supplierPaymentId,
            Notes = notes?.Trim(),
            IsActive = true
        };
        cheque.SetCreatedBy(createdByUserId);
        return cheque;
    }

    /// <summary>
    /// Marks the cheque as cleared. Only allowed when status is Pending.
    /// </summary>
    public void Clear()
    {
        if (Status != ChequeStatus.Pending)
            throw new DomainException("لا يمكن تصفية الشيك إلا عندما يكون قيد الانتظار");

        Status = ChequeStatus.Cleared;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the cheque as bounced. Allowed from Pending or Cleared.
    /// </summary>
    public void Bounce()
    {
        if (Status != ChequeStatus.Pending && Status != ChequeStatus.Cleared)
            throw new DomainException("لا يمكن إرجاع الشيك إلا عندما يكون قيد الانتظار أو تم تصفيته");

        Status = ChequeStatus.Bounced;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the cheque. Only allowed when status is Pending.
    /// </summary>
    public void Cancel()
    {
        if (Status != ChequeStatus.Pending)
            throw new DomainException("لا يمكن إلغاء الشيك إلا عندما يكون قيد الانتظار");

        Status = ChequeStatus.Cancelled;
        UpdateTimestamp();
    }
}
