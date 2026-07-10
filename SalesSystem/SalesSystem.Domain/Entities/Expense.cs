using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an expense transaction (cash outflow from a cash box).
/// Supports lifecycle: Draft → Posted → Cancelled (terminal state).
/// </summary>
public class Expense : DocumentEntity
{
    public int ExpenseNo { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public int ExpenseAccountId { get; private set; }
    public int CashBoxId { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>
    /// The expense amount converted to the base currency using the exchange rate.
    /// Null when no exchange rate is specified (same as Amount).
    /// </summary>
    public decimal? BaseNetTotal { get; private set; }

    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Account? ExpenseAccount { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    private Expense() { } // EF Core

    /// <summary>
    /// Creates a new expense in Draft status.
    /// </summary>
    public static Expense Create(
        int expenseNo,
        DateTime expenseDate,
        int expenseAccountId,
        int cashBoxId,
        decimal amount,
        string? notes = null,
        decimal? baseNetTotal = null,
        int? createdByUserId = null)
    {
        if (expenseNo <= 0)
            throw new DomainException("رقم المصروف يجب أن يكون أكبر من الصفر.");
        if (expenseAccountId <= 0)
            throw new DomainException("حساب المصروف مطلوب.");
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var expense = new Expense
        {
            ExpenseNo = expenseNo,
            ExpenseDate = expenseDate.Kind == DateTimeKind.Utc
                ? expenseDate
                : expenseDate.ToUniversalTime(),
            ExpenseAccountId = expenseAccountId,
            CashBoxId = cashBoxId,
            Amount = amount,
            BaseNetTotal = baseNetTotal,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        expense.SetCreatedBy(createdByUserId);
        return expense;
    }

    /// <summary>
    /// Posts the expense — confirms the transaction. Only drafts can be posted.
    /// </summary>
    public void Post(DateTime? postedAt = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط المصروفات المسودة يمكن ترحيلها.");

        Status = InvoiceStatus.Posted;
        PostedAt = postedAt ?? DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the expense. Only drafts and posted expenses can be cancelled.
    /// Once cancelled, the expense cannot be modified or re-posted.
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("المصروف ملغي بالفعل.");

        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the expense fields. Only allowed while in Draft status.
    /// </summary>
    public void Update(
        DateTime expenseDate,
        int expenseAccountId,
        int cashBoxId,
        decimal amount,
        string? notes,
        decimal? baseNetTotal = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تعديل مصروف بعد ترحيله.");

        if (expenseAccountId <= 0)
            throw new DomainException("حساب المصروف مطلوب.");
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        ExpenseDate = expenseDate.Kind == DateTimeKind.Utc
            ? expenseDate
            : expenseDate.ToUniversalTime();
        ExpenseAccountId = expenseAccountId;
        CashBoxId = cashBoxId;
        Amount = amount;
        BaseNetTotal = baseNetTotal;
        Notes = notes;

        UpdateTimestamp();
    }
}
