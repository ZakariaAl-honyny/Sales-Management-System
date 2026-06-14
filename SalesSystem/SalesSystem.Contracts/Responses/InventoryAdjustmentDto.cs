namespace SalesSystem.Contracts.Responses;

public record InventoryAdjustmentDto(
    int Id,
    int AdjustmentNo,
    DateTime AdjustmentDate,
    int WarehouseId,
    string? WarehouseName,
    byte AdjustmentType,
    string? AdjustmentTypeName,
    int AccountId,
    string? AccountName,
    byte Status,
    string? StatusName,
    DateTime? PostedAt,
    List<InventoryAdjustmentLineDto>? Lines,
    bool IsActive
);

public record InventoryAdjustmentLineDto(
    int Id,
    int InventoryAdjustmentId,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal LineTotal,
    bool IsActive
);
