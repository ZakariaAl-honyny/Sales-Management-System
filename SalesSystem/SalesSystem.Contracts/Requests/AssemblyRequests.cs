namespace SalesSystem.Contracts.Requests;

public record CreateBillOfMaterialRequest(
    int AssemblyProductId,
    int ComponentProductId,
    int ComponentUnitId,
    decimal QuantityRequired,
    decimal WastePercentage = 0
);

public record UpdateBillOfMaterialRequest(
    int ComponentUnitId,
    decimal QuantityRequired,
    decimal WastePercentage = 0
);

public record ProduceAssemblyRequest(
    int AssemblyProductId,
    int WarehouseId,
    decimal Quantity
);
