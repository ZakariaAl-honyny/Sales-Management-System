using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a journal entry (double-entry accounting record) with 3-state lifecycle:
/// Draft (1) → Posted (2) → Cancelled (3).
/// Lines must be added via AddDebitLine / AddCreditLine methods
/// to ensure the entry remains internally consistent.
/// </summary>
public class JournalEntry : DocumentEntity
{
    public string EntryNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Integer sequence number for this journal entry (matches DB sequence).
    /// Separate from the formatted EntryNumber string for efficient indexing and querying.
    /// </summary>
    public int EntryNo { get; private set; }

    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public JournalEntryType EntryType { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public short? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public string? AttachmentPath { get; private set; }
    public short? BranchId { get; private set; }
    public int? ReversedByEntryId { get; private set; }
    public JournalEntry? ReversedByEntry { get; private set; }

    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    // ─── Computed Totals ─────────────────────────────

    public decimal TotalDebit => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);

    private JournalEntry() { } // EF Core

    public static JournalEntry Create(
        string entryNumber,
        int entryNo,
        DateTime transactionDate,
        string description,
        JournalEntryType entryType,
        int createdBy,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null,
        short? currencyId = null,
        decimal? exchangeRate = null,
        string? attachmentPath = null)
    {
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new DomainException("رقم القيد المحاسبي مطلوب");

        if (entryNo <= 0)
            throw new DomainException("الرقم التسلسلي للقيد المحاسبي مطلوب");

        if (transactionDate == default)
            throw new DomainException("تاريخ القيد المحاسبي مطلوب");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("الوصف مطلوب");

        if (!Enum.IsDefined(typeof(JournalEntryType), entryType))
            throw new DomainException("نوع القيد المحاسبي غير صالح");

        if (createdBy <= 0)
            throw new DomainException("منشئ القيد المحاسبي مطلوب");

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber.Trim(),
            EntryNo = entryNo,
            TransactionDate = transactionDate,
            Description = description.Trim(),
            EntryType = entryType,
            Status = JournalEntryStatus.Draft,
            ReferenceType = referenceType?.Trim(),
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber?.Trim(),
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            AttachmentPath = attachmentPath?.Trim()
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
        if (Status != JournalEntryStatus.Draft)
            throw new DomainException("لا يمكن تعديل قيد محاسبي تم ترحيله أو إلغاؤه");

        var line = JournalEntryLine.CreateDebit(
            accountId, accountCode, accountNameAr, amount, description);
        _lines.Add(line);
        UpdateTimestamp();
    }

    public void AddCreditLine(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new DomainException("لا يمكن تعديل قيد محاسبي تم ترحيله أو إلغاؤه");

        var line = JournalEntryLine.CreateCredit(
            accountId, accountCode, accountNameAr, amount, description);
        _lines.Add(line);
        UpdateTimestamp();
    }

    // ─── Balance & Lifecycle ─────────────────────────

    /// <summary>
    /// Returns true if total debits equal total credits (within 0.001 tolerance).
    /// </summary>
    public bool IsBalanced() => Math.Abs(TotalDebit - TotalCredit) < 0.001m;

    /// <summary>
    /// Posts this journal entry. Validates Draft status, balanced, and has lines.
    /// </summary>
    public void Post(int postedByUserId)
    {
        if (postedByUserId <= 0)
            throw new DomainException("مرحل القيد المحاسبي مطلوب");

        if (Status != JournalEntryStatus.Draft)
            throw new DomainException("لا يمكن ترحيل إلا القيود المحاسبية في حالة مسودة");

        if (_lines.Count == 0)
            throw new DomainException("لا يمكن ترحيل قيد محاسبي بدون بنود");

        if (!IsBalanced())
            throw new DomainException(
                $"القيد المحاسبي غير متوازن — مجموع المدين ({TotalDebit:N2}) لا يساوي مجموع الدائن ({TotalCredit:N2})");

        Status = JournalEntryStatus.Posted;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels this posted journal entry. Optionally links to a reversal entry.
    /// </summary>
    public void Cancel(int cancelledByUserId, int? reversedByEntryId = null)
    {
        if (cancelledByUserId <= 0)
            throw new DomainException("ملغي القيد المحاسبي مطلوب");

        if (Status != JournalEntryStatus.Posted)
            throw new DomainException("لا يمكن إلغاء إلا القيود المحاسبية المرحلة");

        Status = JournalEntryStatus.Cancelled;
        ReversedByEntryId = reversedByEntryId;
        UpdateTimestamp();
    }
}
