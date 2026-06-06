namespace SalesSystem.Contracts.Responses;

public record CashBoxDto(
    int Id,
    string BoxName,
    decimal OpeningBalance,
    decimal CurrentBalance,
    int? BranchId,
    int? CurrencyId,
    string? CurrencyName,
    string? CurrencyCode,
    int? AssignedUserId,
    string? Notes,
    bool IsActive
);
