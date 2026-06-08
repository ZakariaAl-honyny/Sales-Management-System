namespace SalesSystem.Contracts.Responses;

public record CashBoxDto(
    int Id,
    string BoxName,
    int? AccountId,
    string? AccountName,
    int? CategoryId,
    string? CategoryName,
    int? BranchId,
    int? CurrencyId,
    string? CurrencyName,
    string? CurrencyCode,
    int? AssignedUserId,
    string? PhoneNumber,
    string? TaxNumber,
    string? Address,
    string? Notes,
    bool IsActive
);
