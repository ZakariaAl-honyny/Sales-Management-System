namespace SalesSystem.Contracts.Responses;

public record InventoryAdjustmentDto(
    int Id,
    int AdjustmentNo,
    DateTime AdjustmentDate,
    short WarehouseId,
    string? WarehouseName,
    byte AdjustmentType,
    string? AdjustmentTypeName,
    byte Status,
    string? StatusName,
    DateTime? PostedAt,
    List<InventoryAdjustmentLineDto>? Lines
);

public record InventoryAdjustmentLineDto(
    int Id,
    int InventoryAdjustmentId,
    int ProductId,
    string? ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost
);
