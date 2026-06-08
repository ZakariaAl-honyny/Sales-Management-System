namespace SalesSystem.Contracts.Requests;

public record CreateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal OpeningBalance,
    decimal CreditLimit = 0,
    int? AccountId = null
);

public record UpdateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    bool IsActive,
    int? AccountId = null
);
