using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

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
