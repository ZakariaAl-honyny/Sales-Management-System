namespace SalesSystem.Contracts.Responses;

public record WarehouseResponse(
    int Id,
    string Name,
    byte Type,
    string? Location,
    string? Phone,
    string? Address,
    string? ManagerName,
    bool IsDefault,
    bool IsActive,
    int? AccountId,
    string? Notes);

public record WarehouseStockSummaryResponse(
    int ProductId, string ProductName,
    decimal Quantity
);
