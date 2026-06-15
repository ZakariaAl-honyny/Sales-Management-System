using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateJournalEntryRequest(
    DateTime EntryDate,
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

/// <summary>
/// Request to create a new account matching schema §4.2.
/// Nature: 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense.
/// </summary>
public record CreateAccountRequest(
    string AccountCode,
    string NameAr,
    string? NameEn,
    byte Nature,
    bool IsLeaf,
    int? ParentId,
    bool IsSystem,
    short? CategoryId);

/// <summary>
/// Request to update an existing account.
/// AccountCode is NOT changeable after creation (read-only in edit mode).
/// </summary>
public record UpdateAccountRequest(
    string NameAr,
    string? NameEn,
    byte Nature,
    bool IsLeaf,
    int? ParentId,
    short? CategoryId);
