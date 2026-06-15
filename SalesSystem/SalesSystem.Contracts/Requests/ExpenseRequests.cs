namespace SalesSystem.Contracts.Requests;

public record CreateExpenseRequest(
    DateTime ExpenseDate,
    int ExpenseAccountId,
    int CashBoxId,
    int CurrencyId,
    decimal Amount,
    string? Notes
);

public record UpdateExpenseRequest(
    DateTime ExpenseDate,
    int ExpenseAccountId,
    int CashBoxId,
    int CurrencyId,
    decimal Amount,
    string? Notes
);
