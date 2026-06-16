namespace SalesSystem.Contracts.Responses;

public record InventoryCountDto(
    int Id,
    string CountNo,
    short WarehouseId,
    string? WarehouseName,
    byte Status,
    string? StatusName,
    string? Notes,
    DateTime CreatedAt,
    int CreatedByUserId,
    List<InventoryCountLineDto>? Lines
);

public record InventoryCountLineDto(
    int Id,
    int InventoryCountId,
    int ProductUnitId,
    string? ProductUnitName,
    decimal ExpectedQuantity,
    decimal ActualQuantity,
    decimal Difference,
    string? Notes
);
