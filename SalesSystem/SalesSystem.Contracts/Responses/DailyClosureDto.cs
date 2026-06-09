namespace SalesSystem.Contracts.Responses;

public record DailyClosureDto(
    int Id,
    int CashBoxId,
    DateOnly ClosureDate,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ExpectedClosingBalance,
    decimal ActualCashCount,
    decimal Difference,
    bool IsReconciled,
    int ClosedByUserId,
    string? Notes,
    DateTime CreatedAt
);
