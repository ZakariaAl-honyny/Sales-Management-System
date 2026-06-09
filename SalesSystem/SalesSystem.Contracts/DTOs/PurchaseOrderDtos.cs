namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// DTO لعرض أمر الشراء — يحتوي على بيانات المورد والمستودع والحالة والبنود.
/// </summary>
public record PurchaseOrderDto(
    int Id,
    int OrderNo,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    DateTime OrderDate,
    DateOnly? ExpectedDate,
    byte Status,
    int? CurrencyId,
    string? CurrencyCode,
    decimal? ExchangeRate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemDto> Items)
{
    /// <summary>نص حالة أمر الشراء المترجم للعرض.</summary>
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "معتمد",
        3 => "مستلم جزئياً",
        4 => "مستلم بالكامل",
        5 => "ملغي",
        _ => "غير معروف"
    };
}

/// <summary>
/// DTO لعرض بند في أمر الشراء — يحتوي على المنتج والوحدة والكمية والتكلفة.
/// </summary>
public record PurchaseOrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string ProductUnitName,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal PendingReceiveQuantity,
    decimal UnitCost,
    decimal LineTotal,
    string? Notes);

/// <summary>
/// DTO لعرض المصاريف الإضافية المرتبطة بفاتورة الشراء.
/// </summary>
public record AdditionalFeeDto(
    int Id,
    string FeeName,
    decimal FeeAmount,
    byte DistributionMethod,
    int? AccountId,
    string? AccountName);
