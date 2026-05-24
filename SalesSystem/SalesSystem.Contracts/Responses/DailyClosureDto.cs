namespace SalesSystem.Contracts.Responses;

public record DailyClosureDto(
    int Id,
    int CashBoxId,
    DateOnly ClosureDate,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    int ClosedByUserId,
    DateTime CreatedAt
);
