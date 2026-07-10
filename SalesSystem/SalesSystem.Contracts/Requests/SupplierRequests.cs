namespace SalesSystem.Contracts.Requests;

public record CreateSupplierRequest(
    string Name,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? TaxNumber = null,
    string? Notes = null,
    decimal CreditLimit = 0,
    int? CategoryId = null
);

public record UpdateSupplierRequest(
    string Name,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? TaxNumber = null,
    string? Notes = null,
    decimal CreditLimit = 0,
    int? CategoryId = null,
    bool IsActive = true
);
