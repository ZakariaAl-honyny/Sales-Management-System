namespace SalesSystem.Contracts.Responses;

public record InventoryCountDto(
    int Id,
    int CountNo,
    DateTime CountDate,
    int WarehouseId,
    string? WarehouseName,
    byte Status,
    string? StatusName,
    string? Notes,
    DateTime? PostedAt,
    int? PostedByUserId,
    List<InventoryCountLineDto>? Lines,
    bool IsActive
);

public record InventoryCountLineDto(
    int Id,
    int InventoryCountId,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal SystemQuantity,
    decimal ActualQuantity,
    decimal Difference,
    bool IsActive
);
