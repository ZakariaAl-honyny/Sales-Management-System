namespace SalesSystem.Contracts.Responses;

public record SupplierResponse(
    int Id, string Name, string? Phone, string? Address, string? Email,
    bool IsActive
);
