namespace SalesSystem.Contracts.Responses;

public record InventoryCountDto(
    int Id,
    int CountNo,
    DateTime CountDate,
    short WarehouseId,
    string? WarehouseName,
    byte Status,
    string? StatusName,
    string? Notes,
    DateTime? PostedAt,
    List<InventoryCountLineDto>? Lines
);

public record InventoryCountLineDto(
    int Id,
    int InventoryCountId,
    int ProductId,
    string? ProductName,
    decimal SystemQuantity,
    decimal ActualQuantity,
    decimal DifferenceQuantity
);
