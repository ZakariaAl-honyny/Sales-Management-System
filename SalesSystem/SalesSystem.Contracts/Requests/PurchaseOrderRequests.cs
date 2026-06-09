namespace SalesSystem.Contracts.Requests;

public record CreatePurchaseOrderRequest(
    int SupplierId,
    int WarehouseId,
    int? OrderNo,
    DateTime? OrderDate,
    DateOnly? ExpectedDate,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseOrderItemRequest> Items);

public record CreatePurchaseOrderItemRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    string? Notes);

public record UpdatePurchaseOrderRequest(
    int SupplierId,
    int WarehouseId,
    DateOnly? ExpectedDate,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseOrderItemRequest> Items);
