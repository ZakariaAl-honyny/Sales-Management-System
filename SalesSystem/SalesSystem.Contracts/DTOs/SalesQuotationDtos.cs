namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// DTO لعرض عرض السعر — يحتوي على بيانات العميل والمستودع والحالة والبنود.
/// </summary>
public record SalesQuotationDto(
    int Id,
    string QuotationNo,
    int? CustomerId,
    string? CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime QuotationDate,
    DateTime? ExpiryDate,
    byte Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    int? CurrencyId,
    string? CurrencyCode,
    string? CreatedByUserName,
    DateTime CreatedAt,
    IReadOnlyList<SalesQuotationItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "مؤكد",
        3 => "منتهي",
        4 => "محول",
        _ => "غير معروف"
    };
}

/// <summary>
/// DTO لعرض بند في عرض السعر.
/// </summary>
public record SalesQuotationItemDto(
    int Id,
    int ProductId,
    string? ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode,
    string? Notes = null);
