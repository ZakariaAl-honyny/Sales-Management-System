namespace SalesSystem.Contracts.Requests;

public record CreateSupplierRequest(
    string Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal OpeningBalance,
    decimal CreditLimit = 0
);

public record UpdateSupplierRequest(
    string Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    bool IsActive
);
