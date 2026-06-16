using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a payment voucher (سند صرف) — a record of cash disbursement from a cash box.
/// Payments can go to suppliers, expenses, or other cash outflows.
/// Inherits from <see cref="DocumentEntity"/> with Draft→Posted→Cancelled lifecycle.
/// Schema: §4.8 PaymentVouchers — VoucherNo (int, unique), VoucherDate (date),
/// CurrencyId (smallint, FK), CashBoxId (int, FK), AccountId (int, FK),
/// TotalAmount (decimal(18,2)), Status (tinyint, VoucherStatus).
/// </summary>
public class PaymentVoucher : DocumentEntity
{
    /// <summary>
    /// Unique payment voucher number (user-facing).
    /// </summary>
    public int VoucherNo { get; private set; }

    /// <summary>
    /// Date of the payment voucher.
    /// </summary>
    public DateTime VoucherDate { get; private set; }

    /// <summary>
    /// FK to Currencies table.
    /// </summary>
    public short CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }

    /// <summary>
    /// FK to CashBoxes table — the cash box disbursing the funds.
    /// </summary>
    public int CashBoxId { get; private set; }
    public CashBox? CashBox { get; private set; }

    /// <summary>
    /// FK to Accounts table — the account this payment is debited to.
    /// </summary>
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// Total payment amount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Optional notes/description.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Document status: Draft=1, Posted=2, Cancelled=3.
    /// </summary>
    public VoucherStatus Status { get; private set; }

    private readonly List<JournalEntry> _journalEntries = new();
    public IReadOnlyList<JournalEntry> JournalEntries => _journalEntries.AsReadOnly();

    private PaymentVoucher() { } // EF Core

    public static PaymentVoucher Create(
        int voucherNo,
        DateTime voucherDate,
        short currencyId,
        int cashBoxId,
        int accountId,
        decimal totalAmount,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (voucherNo <= 0)
            throw new DomainException("رقم سند الصرف مطلوب");

        if (currencyId <= 0)
            throw new DomainException("عملة سند الصرف مطلوبة");

        if (cashBoxId <= 0)
            throw new DomainException("الصندوق النقدي مطلوب");

        if (accountId <= 0)
            throw new DomainException("الحساب المحاسبي مطلوب");

        if (totalAmount <= 0)
            throw new DomainException("مبلغ سند الصرف يجب أن يكون أكبر من الصفر");

        var voucher = new PaymentVoucher
        {
            VoucherNo = voucherNo,
            VoucherDate = voucherDate.Kind == DateTimeKind.Utc ? voucherDate : voucherDate.ToUniversalTime(),
            CurrencyId = currencyId,
            CashBoxId = cashBoxId,
            AccountId = accountId,
            TotalAmount = totalAmount,
            Notes = notes?.Trim(),
            Status = VoucherStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };
        voucher.SetCreatedBy(createdByUserId);
        return voucher;
    }

    /// <summary>
    /// Posts the payment voucher (Draft → Posted).
    /// </summary>
    public void Post(int? postedByUserId = null)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("لا يمكن ترحيل سند الصرف إلا في حالة المسودة");

        Status = VoucherStatus.Posted;
        PostedAt = DateTime.UtcNow;
        SetUpdatedBy(postedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the payment voucher. Allowed from Draft or Posted.
    /// Posted vouchers require reversal entries.
    /// </summary>
    public void Cancel(int? cancelledByUserId = null)
    {
        if (Status == VoucherStatus.Cancelled)
            throw new DomainException("سند الصرف ملغي بالفعل");

        Status = VoucherStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        SetUpdatedBy(cancelledByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates mutable fields of the payment voucher.
    /// Only allowed in Draft status.
    /// </summary>
    public void Update(
        DateTime? voucherDate = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("لا يمكن تعديل سند الصرف إلا في حالة المسودة");

        if (voucherDate.HasValue)
            VoucherDate = voucherDate.Value.Kind == DateTimeKind.Utc ? voucherDate.Value : voucherDate.Value.ToUniversalTime();

        if (notes != null)
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the total amount. Only allowed in Draft status.
    /// </summary>
    public void UpdateTotalAmount(decimal totalAmount)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("لا يمكن تعديل مبلغ سند الصرف إلا في حالة المسودة");

        if (totalAmount <= 0)
            throw new DomainException("مبلغ سند الصرف يجب أن يكون أكبر من الصفر");

        TotalAmount = totalAmount;
        UpdateTimestamp();
    }
}
