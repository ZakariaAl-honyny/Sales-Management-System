namespace SalesSystem.Contracts.Requests;

public record CreateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber
);

public record UpdateSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    bool IsActive
);
