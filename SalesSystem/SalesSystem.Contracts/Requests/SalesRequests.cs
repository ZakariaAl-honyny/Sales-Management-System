using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

// ═══════════════════════════════════════════════════
// Sales Quotations (Phase 28)
// ═══════════════════════════════════════════════════

/// <summary>
/// طلب إنشاء عرض سعر جديد.
/// </summary>
public record CreateSalesQuotationRequest(
    int? CustomerId,
    int WarehouseId,
    DateTime? QuotationDate,
    DateTime? ExpiryDate,
    decimal DiscountAmount,
    string? Notes,
    int? CurrencyId,
    decimal? ExchangeRate,
    List<CreateSalesQuotationItemRequest> Items);

/// <summary>
/// طلب إنشاء بند في عرض السعر.
/// </summary>
public record CreateSalesQuotationItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0,
    byte Mode = 1,
    string? Notes = null);

/// <summary>
/// طلب تحديث عرض سعر موجود.
/// </summary>
public record UpdateSalesQuotationRequest(
    int? CustomerId,
    int WarehouseId,
    DateTime? QuotationDate,
    DateTime? ExpiryDate,
    decimal DiscountAmount,
    string? Notes,
    int? CurrencyId,
    decimal? ExchangeRate,
    List<CreateSalesQuotationItemRequest> Items);

/// <summary>
/// طلب تحويل عرض سعر إلى فاتورة بيع.
/// </summary>
public record ConvertQuotationToInvoiceRequest(
    int? CustomerId,
    int WarehouseId,
    int? CashBoxId,
    int PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    string? Notes,
    int? CurrencyId,
    decimal? ExchangeRate);

// ═══════════════════════════════════════════════════
// Sales Invoice Post (Phase 28)
// ═══════════════════════════════════════════════════

/// <summary>
/// طلب ترحيل فاتورة بيع — يحتوي على خيارات إضافية للترحيل.
/// </summary>
public record PostSalesInvoiceRequest(
    int? CashBoxId = null,
    string? Notes = null);

// ═══════════════════════════════════════════════════
// Sales Return Post (Phase 28)
// ═══════════════════════════════════════════════════

/// <summary>
/// طلب ترحيل مرتجع مبيعات — يحتوي على خيارات الصندوق النقدي والمبلغ المسترد.
/// </summary>
public record PostSalesReturnRequest(
    int? CashBoxId = null,
    decimal? RefundAmount = null,
    string? Notes = null);
