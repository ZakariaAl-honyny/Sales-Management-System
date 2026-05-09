namespace SalesSystem.Contracts.Requests;

public record CreateStockTransferRequest(
    int FromWarehouseId,
    int ToWarehouseId,
    DateTime? TransferDate,
    string? Notes,
    List<CreateStockTransferItemRequest> Items);
public record CreateStockTransferItemRequest(
    int ProductId,
    decimal Quantity,
    string? Notes);
