using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateJournalEntryRequest(
    DateTime TransactionDate,
    string Description,
    JournalEntryType EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    List<JournalEntryLineRequest> Lines
);

public record JournalEntryLineRequest(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);

public record CreateAccountRequest(
    string AccountCode,
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    bool IsSystemAccount,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    string? Explanation,
    string? Notes);

public record UpdateAccountRequest(
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    string? Explanation,
    string? Notes);
