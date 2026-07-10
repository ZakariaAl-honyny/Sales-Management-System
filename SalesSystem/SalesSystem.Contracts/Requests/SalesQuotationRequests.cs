namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب إنشاء عرض سعر جديد.
/// </summary>
public record CreateSalesQuotationRequest(
    int? QuotationNo,                        // null = auto-generate
    DateTime? QuotationDate,
    DateTime? ValidUntil,
    int CustomerId,
    short WarehouseId,
    byte PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    string? Notes,
    string? TermsAndConditions,
    List<CreateSalesQuotationItemRequest> Items);

/// <summary>
/// طلب إنشاء بند في عرض السعر.
/// </summary>
public record CreateSalesQuotationItemRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0,
    string? Notes = null);

/// <summary>
/// طلب تحديث عرض سعر موجود.
/// </summary>
public record UpdateSalesQuotationRequest(
    DateTime? QuotationDate,
    DateTime? ValidUntil,
    int CustomerId,
    short WarehouseId,
    byte PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    string? Notes,
    string? TermsAndConditions,
    List<CreateSalesQuotationItemRequest> Items);

/// <summary>
/// طلب رفض عرض سعر مع ذكر السبب.
/// </summary>
public record RejectSalesQuotationRequest(
    string? Reason);
