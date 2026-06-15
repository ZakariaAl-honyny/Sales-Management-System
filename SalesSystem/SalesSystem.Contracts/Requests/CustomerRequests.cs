namespace SalesSystem.Contracts.Requests;

public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit = 0
);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    bool IsActive
);
