namespace SalesSystem.Contracts.Requests;

public record CreateInventoryCountRequest(
    short WarehouseId,
    string? Notes = null
);

public record UpdateInventoryCountRequest(
    string? Notes = null
);

public record AddInventoryCountLineRequest(
    int InventoryCountId,
    int ProductUnitId,
    decimal ExpectedQuantity,
    decimal ActualQuantity,
    string? Notes = null
);
