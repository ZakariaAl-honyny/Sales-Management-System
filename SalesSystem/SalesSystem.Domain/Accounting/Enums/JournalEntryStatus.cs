namespace SalesSystem.Domain.Accounting.Enums;

/// <summary>
/// Represents the 3-state lifecycle of a journal entry:
/// Draft → Posted → Cancelled (terminal).
/// </summary>
public enum JournalEntryStatus : byte
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}
