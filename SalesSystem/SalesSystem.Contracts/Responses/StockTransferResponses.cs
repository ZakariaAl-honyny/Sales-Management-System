using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

public record StockTransferResponse(
    int Id, string TransferNumber,
    int SourceWarehouseId, string SourceWarehouseName,
    int DestinationWarehouseId, string DestinationWarehouseName,
    InvoiceStatus Status, DateTime TransferDate, string? Notes,
    List<StockTransferItemResponse> Items
);
public record StockTransferItemResponse(int Id, int ProductId, string ProductName, decimal Quantity);
