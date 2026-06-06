using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateJournalEntryRequest(
    DateTime TransactionDate,
    string Description,
    JournalEntryType EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    int CreatedBy,
    List<JournalEntryLineRequest> Lines
);

public record JournalEntryLineRequest(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);
