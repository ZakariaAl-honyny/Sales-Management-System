using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

public record SalesReturnResponse(
    int Id, string ReturnNumber,
    int CustomerId, string CustomerName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status,
    decimal TotalAmount, DateTime ReturnDate, string? Notes,
    List<SalesReturnItemResponse> Items
);

public record SalesReturnItemResponse(
    int Id, int ProductId, string ProductName, string ProductCode,
    decimal Quantity, decimal UnitPrice, decimal LineTotal
);

public record PurchaseReturnResponse(
    int Id, string ReturnNumber,
    int SupplierId, string SupplierName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status,
    decimal TotalAmount, DateTime ReturnDate, string? Notes,
    List<PurchaseReturnItemResponse> Items
);

public record PurchaseReturnItemResponse(
    int Id, int ProductId, string ProductName, string ProductCode,
    decimal Quantity, decimal UnitCost, decimal LineTotal
);
