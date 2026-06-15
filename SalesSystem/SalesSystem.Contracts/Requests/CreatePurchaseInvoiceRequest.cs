using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب إنشاء فاتورة شراء جديدة — مع دعم العملات المتعددة.
/// </summary>
public record CreatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    int? InvoiceNo,
    DateTime? InvoiceDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    short? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseInvoiceLineRequest> Items);

/// <summary>
/// طلب إنشاء بند في فاتورة الشراء.
/// </summary>
public record CreatePurchaseInvoiceLineRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitPrice);
