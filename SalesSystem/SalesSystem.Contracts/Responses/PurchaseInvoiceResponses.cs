using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// استجابة فاتورة الشراء — مع دعم العملات المتعددة والخصم والمرفقات والمصاريف الإضافية.
/// </summary>
public record PurchaseInvoiceResponse(
    int Id, string InvoiceNumber,
    int SupplierId, string SupplierName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status, PaymentType PaymentType,
    decimal SubTotal, decimal InvoiceDiscount,
    decimal TaxRate, decimal TaxAmount,
    bool IsTaxInclusive,
    decimal TotalAmount, decimal PaidAmount, decimal DueAmount,
    DateTime InvoiceDate,
    int? CurrencyId, string? CurrencyCode, decimal? ExchangeRate,
    decimal? CostInBaseCurrency,
    decimal AdditionalFeesTotal,
    string? AttachmentPath,
    byte? DiscountType, decimal? DiscountRate,
    string? Notes,
    List<PurchaseInvoiceItemResponse> Items,
    List<AdditionalFeeDto>? AdditionalFees = null
);

/// <summary>
/// استجابة بند فاتورة الشراء — مع معرف الوحدة ونوع الخصم والتكلفة بعملة الأساس.
/// </summary>
public record PurchaseInvoiceItemResponse(
    int Id, int ProductId, string ProductName,
    int ProductUnitId, string ProductUnitName,
    decimal Quantity, decimal UnitCost, decimal DiscountAmount, decimal LineTotal,
    byte? DiscountType, decimal? DiscountRate,
    decimal? CostInBaseCurrency, decimal AdditionalFeesAmount
);
