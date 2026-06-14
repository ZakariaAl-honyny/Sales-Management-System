namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Holds both the formatted entry number string and the underlying int sequence number.
/// </summary>
public record JournalEntryNumberResult(string EntryNumber, int EntryNo);
