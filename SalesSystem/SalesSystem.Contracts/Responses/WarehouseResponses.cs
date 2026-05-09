namespace SalesSystem.Contracts.Responses;

public record WarehouseResponse(
    int Id, string Code, string Name, string? Location, bool IsDefault, bool IsActive
);

public record WarehouseStockSummaryResponse(
    int ProductId, string ProductName, string ProductCode,
    decimal Quantity
);
