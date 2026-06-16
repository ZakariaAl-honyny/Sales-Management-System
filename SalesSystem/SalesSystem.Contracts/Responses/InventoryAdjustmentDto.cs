namespace SalesSystem.Contracts.Responses;

public record InventoryAdjustmentDto(
    int Id,
    string AdjustmentNo,
    short WarehouseId,
    string? WarehouseName,
    byte AdjustmentType,
    string? AdjustmentTypeName,
    string? Reason,
    byte Status,
    string? StatusName,
    DateTime CreatedAt,
    int CreatedByUserId,
    DateTime? PostedAt,
    DateTime? CancelledAt,
    List<InventoryAdjustmentLineDto>? Lines
);

public record InventoryAdjustmentLineDto(
    int Id,
    int InventoryAdjustmentId,
    int ProductUnitId,
    string? ProductUnitName,
    decimal ExpectedQuantity,
    decimal ActualQuantity,
    decimal UnitCost
);
