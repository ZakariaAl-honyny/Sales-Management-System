namespace SalesSystem.Contracts.Requests;

public record CreateSalesReturnRequest(
    int? SalesInvoiceId,
    int? CustomerId,
    int WarehouseId,
    DateTime? ReturnDate,
    string? Notes,
    int? CashBoxId,
    decimal? RefundAmount,
    List<ReturnItemRequest> Items
);

/// <summary>
/// طلب إنشاء مرتجع شراء — مع دعم الربط بالفاتورة والخصم متعدد الأنواع.
/// </summary>
public record CreatePurchaseReturnRequest(
    int? PurchaseInvoiceId,
    int SupplierId,
    int WarehouseId,
    bool? LinkToInvoice,
    DateTime? ReturnDate,
    decimal DiscountAmount,
    byte? DiscountType,
    decimal? DiscountRate,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseReturnItemRequest> Items);

/// <summary>
/// طلب إنشاء بند في مرتجع الشراء — مع معرف الوحدة.
/// </summary>
public record CreatePurchaseReturnItemRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount = 0,
    byte Mode = 1,
    string? Notes = null);

public record ReturnItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    byte Mode = 1, // Retail by default
    string? Notes = null
);
