namespace SalesSystem.Contracts.Requests;

public record CreateInventoryAdjustmentRequest(
    short WarehouseId,
    DateTime AdjustmentDate,
    byte AdjustmentType
);

public record AddInventoryAdjustmentLineRequest(
    int InventoryAdjustmentId,
    int ProductId,
    decimal Quantity,
    decimal UnitCost
);
