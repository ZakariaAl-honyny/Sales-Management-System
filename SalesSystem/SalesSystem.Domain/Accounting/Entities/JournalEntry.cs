using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a journal entry (double-entry accounting record).
/// Lines must be added via AddDebitLine / AddCreditLine methods
/// to ensure the entry remains internally consistent.
/// </summary>
public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    public string? Description { get; private set; }
    public JournalEntryType EntryType { get; private set; }
    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public bool IsPosted { get; private set; }
    public bool IsReversed { get; private set; }
    public int? PostedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    // ─── Computed Totals ─────────────────────────────

    public decimal TotalDebit => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);

    private JournalEntry() { } // EF Core

    public static JournalEntry Create(
        string entryNumber,
        DateTime transactionDate,
        JournalEntryType entryType,
        int createdBy,
        string? description = null,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null)
    {
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new DomainException("رقم القيد المحاسبي مطلوب");

        if (transactionDate == default)
            throw new DomainException("تاريخ القيد المحاسبي مطلوب");

        if (!Enum.IsDefined(typeof(JournalEntryType), entryType))
            throw new DomainException("نوع القيد المحاسبي غير صالح");

        if (createdBy <= 0)
            throw new DomainException("منشئ القيد المحاسبي مطلوب");

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber.Trim(),
            TransactionDate = transactionDate,
            Description = description?.Trim(),
            EntryType = entryType,
            ReferenceType = referenceType?.Trim(),
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber?.Trim(),
            IsPosted = false,
            IsReversed = false
        };
        entry.SetCreatedBy(createdBy);
        return entry;
    }

    // ─── Line Management ─────────────────────────────

    public void AddDebitLine(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        if (IsPosted)
            throw new DomainException("لا يمكن إضافة قيد إلى قيد تم ترحيله");

        var line = JournalEntryLine.CreateDebit(
            accountId, accountCode, accountNameAr, amount, description);
        _lines.Add(line);
    }

    public void AddCreditLine(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        if (IsPosted)
            throw new DomainException("لا يمكن إضافة قيد إلى قيد تم ترحيله");

        var line = JournalEntryLine.CreateCredit(
            accountId, accountCode, accountNameAr, amount, description);
        _lines.Add(line);
    }

    // ─── Balance & Posting ───────────────────────────

    /// <summary>
    /// Returns true if total debits equal total credits (within 0.001 tolerance).
    /// </summary>
    public bool IsBalanced() => Math.Abs(TotalDebit - TotalCredit) < 0.001m;

    /// <summary>
    /// Validates and posts this journal entry.
    /// Throws DomainException with Arabic details if validation fails.
    /// </summary>
    public void ValidateAndPost(int postedBy)
    {
        if (postedBy <= 0)
            throw new DomainException("مرحل القيد المحاسبي مطلوب");

        if (_lines.Count == 0)
            throw new DomainException("لا يمكن ترحيل قيد محاسبي بدون بنود");

        if (!IsBalanced())
            throw new DomainException(
                $"القيد المحاسبي غير متوازن — مجموع الخصوم ({TotalDebit:N2}) لا يساوي مجموع الإيداعات ({TotalCredit:N2})");

        IsPosted = true;
        PostedBy = postedBy;
        PostedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}
