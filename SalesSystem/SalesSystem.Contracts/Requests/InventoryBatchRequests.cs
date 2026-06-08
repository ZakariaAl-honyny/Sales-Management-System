namespace SalesSystem.Contracts.Requests;

public record CreateInventoryBatchRequest(
    int ProductId,
    int WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    string BatchNo,
    DateTime? ManufactureDate = null,
    DateTime? ExpiryDate = null);
