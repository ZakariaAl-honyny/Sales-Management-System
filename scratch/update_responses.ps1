namespace SalesSystem.Contracts.Responses;

public record CategoryResponse(int Id, string Name, string? Description, bool IsActive);

public record CustomerResponse(
    int Id, string Name, string? Phone, string? Address,
    decimal CurrentBalance, bool IsActive
);

public record SupplierResponse(
    int Id, string Name, string? Phone, string? Address,
    decimal CurrentBalance, bool IsActive
);

public record UnitResponse(int Id, string Name, string? Description, bool IsActive);

public record WarehouseResponse(int Id, string Name, string? Location, string? Phone, bool IsActive);
