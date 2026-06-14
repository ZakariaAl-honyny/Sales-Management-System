namespace SalesSystem.Contracts.Requests;

public record CreateInventoryAdjustmentRequest(
    int WarehouseId,
    DateTime AdjustmentDate,
    byte AdjustmentType,
    int AccountId
);

public record AddInventoryAdjustmentLineRequest(
    int InventoryAdjustmentId,
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost
);
