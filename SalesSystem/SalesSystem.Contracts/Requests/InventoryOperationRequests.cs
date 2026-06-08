namespace SalesSystem.Contracts.Requests;

public record CreateInventoryOperationRequest(
    int WarehouseId,
    byte OperationType,
    byte? AdjustmentType,
    DateTime? OperationDate,
    string? ReferenceNo,
    string? Notes,
    List<CreateInventoryOperationItemRequest> Items);

public record CreateInventoryOperationItemRequest(
    int ProductId,
    decimal Quantity,
    decimal? UnitCost,
    byte? StockIssueReason,
    string? Notes);
