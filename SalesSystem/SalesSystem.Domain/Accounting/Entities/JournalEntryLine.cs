using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a single line (debit or credit) within a journal entry.
/// Lines are created through JournalEntry methods only (internal constructors).
/// </summary>
public class JournalEntryLine : Entity
{
    public int JournalEntryId { get; private set; }
    public JournalEntry? JournalEntry { get; private set; }
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public short SortOrder { get; private set; }
    public string? Description { get; private set; }

    private JournalEntryLine() { } // EF Core

    /// <summary>
    /// Creates a debit line. Internal — only JournalEntry can create lines.
    /// </summary>
    internal static JournalEntryLine CreateDebit(
        int accountId,
        decimal amount,
        string? description = null)
    {
        if (amount <= 0)
            throw new DomainException("قيمة الخصم يجب أن تكون أكبر من صفر");

        return new JournalEntryLine
        {
            AccountId = accountId,
            Debit = amount,
            Credit = 0,
            Description = description?.Trim()
        };
    }

    /// <summary>
    /// Creates a credit line. Internal — only JournalEntry can create lines.
    /// </summary>
    internal static JournalEntryLine CreateCredit(
        int accountId,
        decimal amount,
        string? description = null)
    {
        if (amount <= 0)
            throw new DomainException("قيمة الإيداع يجب أن تكون أكبر من صفر");

        return new JournalEntryLine
        {
            AccountId = accountId,
            Debit = 0,
            Credit = amount,
            Description = description?.Trim()
        };
    }
}
