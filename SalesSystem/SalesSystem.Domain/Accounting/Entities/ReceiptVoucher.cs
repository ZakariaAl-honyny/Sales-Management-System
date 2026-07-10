using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a receipt voucher (سند قبض) — a record of cash receipt into a cash box.
/// Receipts can come from customer payments, sales income, or other cash inflows.
/// Inherits from <see cref="DocumentEntity"/> with Draft→Posted→Cancelled lifecycle.
/// Schema: §4.7 ReceiptVouchers — VoucherNo (int, unique), VoucherDate (date),
/// CurrencyId (smallint, FK), CashBoxId (int, FK), AccountId (int, FK),
/// TotalAmount (decimal(18,2)), Status (tinyint, VoucherStatus).
/// </summary>
public class ReceiptVoucher : DocumentEntity
{
    /// <summary>
    /// Unique receipt voucher number (user-facing).
    /// </summary>
    public int VoucherNo { get; private set; }

    /// <summary>
    /// Date of the receipt voucher.
    /// </summary>
    public DateTime VoucherDate { get; private set; }

    /// <summary>
    /// FK to CashBoxes table — the cash box receiving the funds.
    /// </summary>
    public int CashBoxId { get; private set; }
    public CashBox? CashBox { get; private set; }

    /// <summary>
    /// FK to Accounts table — the account this receipt is credited to.
    /// </summary>
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// Total receipt amount.
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

    private ReceiptVoucher() { } // EF Core

    public static ReceiptVoucher Create(
        int voucherNo,
        DateTime voucherDate,
        int cashBoxId,
        int accountId,
        decimal totalAmount,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (voucherNo <= 0)
            throw new DomainException("رقم سند القبض مطلوب");

        if (cashBoxId <= 0)
            throw new DomainException("الصندوق النقدي مطلوب");

        if (accountId <= 0)
            throw new DomainException("الحساب المحاسبي مطلوب");

        if (totalAmount <= 0)
            throw new DomainException("مبلغ سند القبض يجب أن يكون أكبر من الصفر");

        var voucher = new ReceiptVoucher
        {
            VoucherNo = voucherNo,
            VoucherDate = voucherDate.Kind == DateTimeKind.Utc ? voucherDate : voucherDate.ToUniversalTime(),
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
    /// Posts the receipt voucher (Draft → Posted).
    /// </summary>
    public void Post(int? postedByUserId = null)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("لا يمكن ترحيل سند القبض إلا في حالة المسودة");

        Status = VoucherStatus.Posted;
        PostedAt = DateTime.UtcNow;
        SetUpdatedBy(postedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the receipt voucher. Allowed from Draft or Posted.
    /// Posted vouchers require reversal entries.
    /// </summary>
    public void Cancel(int? cancelledByUserId = null)
    {
        if (Status == VoucherStatus.Cancelled)
            throw new DomainException("سند القبض ملغي بالفعل");

        Status = VoucherStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        SetUpdatedBy(cancelledByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates mutable fields of the receipt voucher.
    /// Only allowed in Draft status.
    /// </summary>
    public void Update(
        DateTime? voucherDate = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("لا يمكن تعديل سند القبض إلا في حالة المسودة");

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
            throw new DomainException("لا يمكن تعديل مبلغ سند القبض إلا في حالة المسودة");

        if (totalAmount <= 0)
            throw new DomainException("مبلغ سند القبض يجب أن يكون أكبر من الصفر");

        TotalAmount = totalAmount;
        UpdateTimestamp();
    }
}
