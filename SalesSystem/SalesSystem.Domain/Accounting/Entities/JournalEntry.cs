using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a journal entry (double-entry accounting record) with 3-state lifecycle:
/// Draft (1) → Posted (2) → Cancelled (3).
/// Supports multi-currency via CurrencyId + ExchangeRate.
/// FK to FiscalYear for period-based reporting.
/// Lines must be added via AddDebitLine / AddCreditLine methods.
/// </summary>
public class JournalEntry : DocumentEntity
{
    /// <summary>
    /// Formatted display string derived from EntryNo (kept for thick-client display).
    /// </summary>
    public string EntryNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Integer sequence number for this journal entry (matches DB sequence).
    /// </summary>
    public int EntryNo { get; private set; }

    /// <summary>
    /// Date the entry was recorded (maps to schema EntryDate).
    /// </summary>
    public DateTime EntryDate { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public JournalEntryType EntryType { get; private set; }
    public JournalEntryStatus Status { get; private set; }

    /// <summary>FK to FiscalYears table (smallint). Required for period reporting.</summary>
    public short FiscalYearId { get; private set; }
    public FiscalYear? FiscalYear { get; private set; }

    /// <summary>FK to Currencies table (smallint). Required for multi-currency support.</summary>
    public short CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }

    /// <summary>Exchange rate to base currency. Precision (18,6).</summary>
    public decimal ExchangeRate { get; private set; } = 1m;

    /// <summary>Optional attachment file path (nvarchar(500)).</summary>
    public string? AttachmentPath { get; private set; }

    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public bool IsReversed { get; private set; }
    public int? ReversedByEntryId { get; private set; }
    public JournalEntry? ReversedByEntry { get; private set; }

    /// <summary>User who reviewed this entry (nullable).</summary>
    public int? ReviewedByUserId { get; private set; }

    /// <summary>Timestamp when this entry was reviewed (nullable).</summary>
    public DateTime? ReviewedAt { get; private set; }

    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    // ─── Computed Totals ─────────────────────────────

    public decimal TotalDebit => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);

    private JournalEntry() { } // EF Core

    public static JournalEntry Create(
        string entryNumber,
        int entryNo,
        DateTime entryDate,
        string description,
        JournalEntryType entryType,
        short fiscalYearId,
        short currencyId,
        int createdBy,
        decimal exchangeRate = 1m,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null,
        string? attachmentPath = null)
    {
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new DomainException("رقم القيد المحاسبي مطلوب");

        if (entryNo <= 0)
            throw new DomainException("الرقم التسلسلي للقيد المحاسبي مطلوب");

        if (entryDate == default)
            throw new DomainException("تاريخ القيد المحاسبي مطلوب");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("الوصف مطلوب");

        if (!Enum.IsDefined(typeof(JournalEntryType), entryType))
            throw new DomainException("نوع القيد المحاسبي غير صالح");

        if (fiscalYearId <= 0)
            throw new DomainException("السنة المالية مطلوبة");

        if (currencyId <= 0)
            throw new DomainException("عملة القيد المحاسبي مطلوبة");

        if (exchangeRate <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر");

        if (createdBy <= 0)
            throw new DomainException("منشئ القيد المحاسبي مطلوب");

        if (attachmentPath?.Length > 500)
            throw new DomainException("مسار المرفق لا يمكن أن يتجاوز 500 حرف");

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber.Trim(),
            EntryNo = entryNo,
            EntryDate = entryDate,
            Description = description.Trim(),
            EntryType = entryType,
            FiscalYearId = fiscalYearId,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Status = JournalEntryStatus.Draft,
            ReferenceType = referenceType?.Trim(),
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber?.Trim(),
            AttachmentPath = attachmentPath?.Trim()
        };
        entry.SetCreatedBy(createdBy);
        return entry;
    }

    // ─── Line Management ─────────────────────────────

    public void AddDebitLine(
        int accountId,
        decimal amount,
        string? description = null)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new DomainException("لا يمكن تعديل قيد محاسبي تم ترحيله أو إلغاؤه");

        var line = JournalEntryLine.CreateDebit(
            accountId, amount, description);
        _lines.Add(line);
        UpdateTimestamp();
    }

    public void AddCreditLine(
        int accountId,
        decimal amount,
        string? description = null)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new DomainException("لا يمكن تعديل قيد محاسبي تم ترحيله أو إلغاؤه");

        var line = JournalEntryLine.CreateCredit(
            accountId, amount, description);
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

        PostedAt = DateTime.UtcNow;
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

        CancelledAt = DateTime.UtcNow;
        Status = JournalEntryStatus.Cancelled;
        ReversedByEntryId = reversedByEntryId;
        UpdateTimestamp();
    }
}
