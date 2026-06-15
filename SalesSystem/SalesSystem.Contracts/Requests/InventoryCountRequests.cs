namespace SalesSystem.Contracts.Requests;

public record CreateInventoryCountRequest(
    short WarehouseId,
    DateTime CountDate,
    string? Notes
);

public record UpdateInventoryCountRequest(
    string? Notes
);

public record AddInventoryCountLineRequest(
    int InventoryCountId,
    int ProductId,
    decimal SystemQuantity,
    decimal ActualQuantity
);
