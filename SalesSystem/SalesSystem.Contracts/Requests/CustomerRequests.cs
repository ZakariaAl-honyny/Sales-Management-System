namespace SalesSystem.Contracts.Requests;

public record CreateCustomerRequest(
    string Name, 
    string? Code,
    string? Phone, 
    string? Email, 
    string? Address, 
    decimal OpeningBalance,
    decimal CreditLimit = 0
);

public record UpdateCustomerRequest(
    string Name, 
    string? Code,
    string? Phone, 
    string? Email, 
    string? Address, 
    decimal CreditLimit,
    bool IsActive
);

