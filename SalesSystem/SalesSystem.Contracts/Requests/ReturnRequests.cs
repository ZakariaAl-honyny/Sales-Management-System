using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateSalesReturnRequest(
    int? SalesInvoiceId,
    int? CustomerId,
    short WarehouseId,
    DateTime? ReturnDate,
    string? ReturnReason = null,
    string? Notes = null,
    List<ReturnItemRequest>? Items = null
);

/// <summary>
/// طلب إنشاء مرتجع شراء — مع دعم الربط بالفاتورة والعملات ونوع الخصم.
/// </summary>
public record CreatePurchaseReturnRequest(
    int? PurchaseInvoiceId,
    int SupplierId,
    int WarehouseId,
    DateTime? ReturnDate,
    DiscountType? DiscountType,
    decimal? DiscountRate,
    string? Notes,
    List<CreatePurchaseReturnItemRequest> Items);

/// <summary>
/// طلب إنشاء بند في مرتجع الشراء.
/// </summary>
public record CreatePurchaseReturnItemRequest(
    int PurchaseInvoiceLineId,
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount);

public record ReturnItemRequest(
    int SalesInvoiceLineId,
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount,
    decimal DiscountAmount = 0,
    byte Mode = 1,
    string? Notes = null
);
