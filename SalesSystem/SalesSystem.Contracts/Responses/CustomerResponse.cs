namespace SalesSystem.Contracts.Responses;

public record CustomerResponse(
    int Id, string Name, string? Phone, string? Address, string? Email,
    decimal CurrentBalance, decimal CreditLimit, bool IsActive
);
