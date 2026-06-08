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
    int? CurrencyId,                             // NEW
    decimal? ExchangeRate,                       // NEW
    decimal DiscountAmount,                      // NEW
    byte? DiscountType,                          // NEW
    decimal? DiscountRate,                       // NEW
    string? Notes,
    List<ReturnItemRequest> Items
);

public record ReturnItemRequest(
    int ProductId,
    int ProductUnitId,                           // NEW
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    byte Mode = 1, // Retail by default
    string? Notes = null
);
