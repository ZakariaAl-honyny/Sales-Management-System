namespace SalesSystem.Contracts.Responses;

public record ExpenseDto(
    int Id,
    int ExpenseNo,
    DateTime ExpenseDate,
    int ExpenseAccountId,
    string? ExpenseAccountName,
    int CashBoxId,
    string? CashBoxName,
    int CurrencyId,
    string? CurrencyName,
    decimal Amount,
    string? Notes,
    byte Status,
    string? StatusName,
    bool IsActive
);
