namespace SalesSystem.Contracts.Requests;

public record CreateSalesReturnRequest(
    int? SalesInvoiceId, 
    int? CustomerId, 
    int WarehouseId, 
    DateTime? ReturnDate, 
    string? Notes, 
    List<ReturnItemRequest> Items
);

public record CreatePurchaseReturnRequest(
    int? PurchaseInvoiceId, 
    int SupplierId, 
    int WarehouseId, 
    DateTime? ReturnDate, 
    string? Notes, 
    List<ReturnItemRequest> Items
);

public record ReturnItemRequest(
    int ProductId, 
    decimal Quantity, 
    decimal UnitPrice, 
    decimal DiscountAmount,
    string? Notes = null
);
