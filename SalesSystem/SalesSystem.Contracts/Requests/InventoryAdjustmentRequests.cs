namespace SalesSystem.Contracts.Requests;

public record CreateInventoryAdjustmentRequest(
    short WarehouseId,
    byte AdjustmentType,
    string? Reason = null
);

public record AddInventoryAdjustmentLineRequest(
    int InventoryAdjustmentId,
    int ProductUnitId,
    decimal ExpectedQuantity,
    decimal ActualQuantity,
    decimal UnitCost
);
