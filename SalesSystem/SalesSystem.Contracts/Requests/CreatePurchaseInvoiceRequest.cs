using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب إنشاء فاتورة شراء جديدة — مع دعم العملات المتعددة والخصم والمرفقات والمصاريف الإضافية.
/// </summary>
public record CreatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    int? InvoiceNo,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    int? CashBoxId,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    int? CurrencyId,
    decimal? ExchangeRate,
    byte? DiscountType,
    decimal? DiscountRate,
    string? AttachmentBase64,
    string? AttachmentFileName,
    string? Notes,
    string? SupplierInvoiceNo,
    List<CreatePurchaseInvoiceItemRequest> Items,
    List<CreateAdditionalFeeRequest>? AdditionalFees = null);

/// <summary>
/// طلب إنشاء بند في فاتورة الشراء — مع معرف الوحدة ونوع الخصم.
/// </summary>
public record CreatePurchaseInvoiceItemRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    byte? DiscountType = null,
    decimal? DiscountRate = null,
    SaleMode Mode = SaleMode.Retail,
    string? Notes = null);

/// <summary>
/// طلب إنشاء مصروف إضافي لفواتير الشراء — يوزع على بنود الفاتورة حسب طريقة التوزيع.
/// </summary>
public record CreateAdditionalFeeRequest(
    string FeeName,
    decimal FeeAmount,
    byte DistributionMethod,
    int? AccountId);
