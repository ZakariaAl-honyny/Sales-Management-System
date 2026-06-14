namespace SalesSystem.Contracts.Requests;

public record CreateInventoryCountRequest(
    int WarehouseId,
    DateTime CountDate,
    string? Notes
);

public record UpdateInventoryCountRequest(
    string? Notes
);

public record AddInventoryCountLineRequest(
    int InventoryCountId,
    int ProductId,
    int ProductUnitId,
    decimal SystemQuantity,
    decimal ActualQuantity
);
