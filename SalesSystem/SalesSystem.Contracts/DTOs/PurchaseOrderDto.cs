namespace SalesSystem.Contracts.DTOs;

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
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemDto> Items)
{
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

public record PurchaseOrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal PendingReceiveQuantity,
    decimal UnitCost,
    decimal LineTotal,
    string? Notes);
