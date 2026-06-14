namespace SalesSystem.Contracts.Responses;

public record BankDto(
    int Id,
    int AccountId,
    string? AccountName,
    string Name,
    bool IsActive
);
