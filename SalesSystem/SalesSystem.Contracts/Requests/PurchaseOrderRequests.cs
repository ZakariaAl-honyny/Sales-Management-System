namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب إنشاء أمر شراء جديد — مع دعم العملات المتعددة.
/// </summary>
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

/// <summary>
/// طلب إنشاء بند في أمر الشراء.
/// </summary>
public record CreatePurchaseOrderItemRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    string? Notes = null);

/// <summary>
/// طلب تحديث أمر شراء موجود.
/// </summary>
public record UpdatePurchaseOrderRequest(
    int SupplierId,
    int WarehouseId,
    DateTime? OrderDate,
    DateOnly? ExpectedDate,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseOrderItemRequest> Items);
