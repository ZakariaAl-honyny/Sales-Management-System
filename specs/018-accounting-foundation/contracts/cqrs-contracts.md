# Application Contracts (CQRS): Accounting Foundation (Phase 18)

**Feature**: `018-accounting-foundation`
**Date**: 2026-05-25

---

## 1. CreateJournalEntryCommand

This is the primary contract used by other modules (Sales, Purchases, Manual Entry) to write to the ledger.

```csharp
public record CreateJournalEntryCommand : IRequest<int>
{
    public DateTime TransactionDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public JournalEntryType EntryType { get; init; } = JournalEntryType.Manual;
    
    // Optional reference to source document
    public string? ReferenceType { get; init; }
    public int? ReferenceId { get; init; }
    public string? ReferenceNumber { get; init; }
    
    public int? BranchId { get; init; }
    public int CreatedBy { get; init; }
    
    // Lines MUST be balanced (Debit == Credit)
    public List<JournalEntryLineRequest> Lines { get; init; } = new();
}

public record JournalEntryLineRequest(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);
```

## 2. GetAccountBalanceQuery

Used to retrieve the current balance of any account.

```csharp
public record GetAccountBalanceQuery(
    int AccountId,
    DateTime? AsOfDate = null
) : IRequest<AccountBalanceDto>;

public record AccountBalanceDto(
    int AccountId,
    string AccountCode,
    string AccountNameAr,
    AccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance,        // Normal Balance
    bool IsDebitNormal      // Determines if balance = (Debit-Credit) or (Credit-Debit)
);
```

## 3. GetAccountStatementQuery

Used to retrieve a detailed ledger for a specific account.

```csharp
public record GetAccountStatementQuery(
    int AccountId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<AccountStatementDto>;

public record AccountStatementDto(
    string AccountCode,
    string AccountNameAr,
    decimal OpeningBalance,
    List<AccountStatementLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance
);

public record AccountStatementLineDto(
    DateTime Date,
    string EntryNumber,
    string Description,
    string ReferenceNumber,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);
```
