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
    int Id, int ProductId, string ProductName,
    decimal Quantity, decimal UnitPrice, decimal LineTotal
);

/// <summary>
/// استجابة مرتجع الشراء — مع دعم الربط بالفاتورة ونوع الخصم.
/// </summary>
public record PurchaseReturnResponse(
    int Id, string ReturnNumber,
    int SupplierId, string SupplierName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status,
    decimal TotalAmount, DateTime ReturnDate,
    bool? LinkToInvoice,
    decimal DiscountAmount,
    byte? DiscountType, decimal? DiscountRate,
    string? Notes,
    List<PurchaseReturnItemResponse> Items
);

/// <summary>
/// استجابة بند مرتجع الشراء — مع معرف الوحدة والتكلفة بعملة الأساس.
/// </summary>
public record PurchaseReturnItemResponse(
    int Id, int ProductId, string ProductName,
    int ProductUnitId,
    decimal Quantity, decimal UnitCost, decimal LineTotal,
    decimal? CostInBaseCurrency
);
