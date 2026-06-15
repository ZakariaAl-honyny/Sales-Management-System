using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a cheque received from a customer or issued to a supplier.
/// Tracks the full lifecycle: UnderCollection → Deposited → Cleared/Bounced/Cancelled.
/// A cheque can be linked to a SupplierPayment, CustomerReceipt, ReceiptVoucher, or PaymentVoucher.
/// </summary>
public class Cheque : ActivatableEntity
{
    public string ChequeNumber { get; private set; } = string.Empty;
    public string? BankName { get; private set; }
    public string? BankBranch { get; private set; }

    /// <summary>
    /// FK to SupplierPayment — when the cheque is issued to a supplier.
    /// </summary>
    public int? PaymentId { get; private set; }

    /// <summary>
    /// FK to CustomerReceipt — when the cheque is received from a customer.
    /// </summary>
    public int? CustomerReceiptId { get; private set; }

    /// <summary>
    /// FK to ReceiptVoucher (manual receipt entry).
    /// </summary>
    public int? ReceiptVoucherId { get; private set; }

    /// <summary>
    /// FK to PaymentVoucher (manual payment entry).
    /// </summary>
    public int? PaymentVoucherId { get; private set; }

    public DateTime IssueDate { get; private set; }
    public DateTime? MaturityDate { get; private set; }
    public decimal Amount { get; private set; }
    public ChequeStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual SupplierPayment? Payment { get; private set; }
    public virtual CustomerReceipt? CustomerReceipt { get; private set; }
    public virtual ReceiptVoucher? ReceiptVoucher { get; private set; }
    public virtual PaymentVoucher? PaymentVoucher { get; private set; }

    private Cheque() { } // EF Core

    /// <summary>
    /// Creates a new cheque in UnderCollection status.
    /// </summary>
    public static Cheque Create(
        string chequeNumber,
        string? bankName,
        string? bankBranch,
        DateTime issueDate,
        DateTime? maturityDate,
        decimal amount,
        string? notes = null,
        int? paymentId = null,
        int? customerReceiptId = null,
        int? receiptVoucherId = null,
        int? paymentVoucherId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(chequeNumber))
            throw new DomainException("رقم الشيك مطلوب.");
        if (amount <= 0)
            throw new DomainException("قيمة الشيك يجب أن تكون أكبر من الصفر.");

        var cheque = new Cheque
        {
            ChequeNumber = chequeNumber.Trim(),
            BankName = bankName?.Trim(),
            BankBranch = bankBranch?.Trim(),
            IssueDate = issueDate.Kind == DateTimeKind.Utc ? issueDate : issueDate.ToUniversalTime(),
            MaturityDate = maturityDate.HasValue
                ? (maturityDate.Value.Kind == DateTimeKind.Utc ? maturityDate.Value : maturityDate.Value.ToUniversalTime())
                : null,
            Amount = amount,
            Status = ChequeStatus.UnderCollection,
            Notes = notes?.Trim(),
            PaymentId = paymentId,
            CustomerReceiptId = customerReceiptId,
            ReceiptVoucherId = receiptVoucherId,
            PaymentVoucherId = paymentVoucherId
        };
        cheque.SetCreatedBy(createdByUserId);
        return cheque;
    }

    /// <summary>
    /// Updates the cheque fields. Only allowed when cheque is UnderCollection or Deposited.
    /// </summary>
    public void Update(
        string chequeNumber,
        string? bankName,
        string? bankBranch,
        DateTime issueDate,
        DateTime? maturityDate,
        decimal amount,
        string? notes,
        int? paymentId,
        int? customerReceiptId,
        int? receiptVoucherId,
        int? paymentVoucherId,
        int? updatedByUserId = null)
    {
        if (Status == ChequeStatus.Cleared)
            throw new DomainException("لا يمكن تعديل شيك تم صرفه.");
        if (Status == ChequeStatus.Bounced)
            throw new DomainException("لا يمكن تعديل شيك مرتجع.");
        if (Status == ChequeStatus.Cancelled)
            throw new DomainException("لا يمكن تعديل شيك ملغي.");

        if (string.IsNullOrWhiteSpace(chequeNumber))
            throw new DomainException("رقم الشيك مطلوب.");
        if (amount <= 0)
            throw new DomainException("قيمة الشيك يجب أن تكون أكبر من الصفر.");

        ChequeNumber = chequeNumber.Trim();
        BankName = bankName?.Trim();
        BankBranch = bankBranch?.Trim();
        IssueDate = issueDate.Kind == DateTimeKind.Utc ? issueDate : issueDate.ToUniversalTime();
        MaturityDate = maturityDate.HasValue
            ? (maturityDate.Value.Kind == DateTimeKind.Utc ? maturityDate.Value : maturityDate.Value.ToUniversalTime())
            : null;
        Amount = amount;
        if (notes != null) Notes = notes?.Trim();
        PaymentId = paymentId;
        CustomerReceiptId = customerReceiptId;
        ReceiptVoucherId = receiptVoucherId;
        PaymentVoucherId = paymentVoucherId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the cheque as deposited (transitioned from UnderCollection).
    /// </summary>
    public void MarkAsDeposited()
    {
        if (Status != ChequeStatus.UnderCollection)
            throw new DomainException("فقط الشيكات تحت التحصيل يمكن إيداعها.");
        Status = ChequeStatus.Deposited;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the cheque as cleared (payment confirmed). Can transition from UnderCollection or Deposited.
    /// </summary>
    public void MarkAsCleared()
    {
        if (Status == ChequeStatus.Cleared)
            throw new DomainException("الشيك مقبوض بالفعل.");
        if (Status == ChequeStatus.Bounced)
            throw new DomainException("لا يمكن قبض شيك مرتجع.");
        if (Status == ChequeStatus.Cancelled)
            throw new DomainException("لا يمكن قبض شيك ملغي.");
        Status = ChequeStatus.Cleared;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the cheque as bounced (returned unpaid). Can transition from UnderCollection or Deposited.
    /// </summary>
    public void MarkAsBounced()
    {
        if (Status == ChequeStatus.Bounced)
            throw new DomainException("الشيك مرتجع بالفعل.");
        if (Status == ChequeStatus.Cleared)
            throw new DomainException("لا يمكن إرجاع شيك تم صرفه.");
        if (Status == ChequeStatus.Cancelled)
            throw new DomainException("لا يمكن إرجاع شيك ملغي.");
        Status = ChequeStatus.Bounced;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the cheque as cancelled. Can be cancelled from any non-terminal status.
    /// </summary>
    public void MarkAsCancelled()
    {
        if (Status == ChequeStatus.Cancelled)
            throw new DomainException("الشيك ملغي بالفعل.");
        if (Status == ChequeStatus.Cleared)
            throw new DomainException("لا يمكن إلغاء شيك تم صرفه.");
        Status = ChequeStatus.Cancelled;
        UpdateTimestamp();
    }
}
