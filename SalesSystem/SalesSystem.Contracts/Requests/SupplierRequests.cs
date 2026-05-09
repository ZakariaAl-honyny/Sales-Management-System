namespace SalesSystem.Contracts.Requests;

public record CreateSupplierRequest(
    string Name, 
    string? Code,
    string? Phone, 
    string? Email, 
    string? Address, 
    decimal OpeningBalance
);

public record UpdateSupplierRequest(
    string Name, 
    string? Code,
    string? Phone, 
    string? Email, 
    string? Address, 
    bool IsActive
);

