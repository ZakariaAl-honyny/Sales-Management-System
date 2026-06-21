using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record UpdateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    string? Notes = null
);

/// <summary>
/// طلب تحديث فاتورة شراء.
/// </summary>
public record UpdatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    DateTime? InvoiceDate,
    PaymentType? PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    short? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    int? TaxId,
    List<CreatePurchaseInvoiceLineRequest> Items);

public record UpdateSalesInvoiceRequest(
    int WarehouseId,
    int CustomerId,
    DateTime? InvoiceDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    int? CashBoxId,
    string? Notes,
    short? CurrencyId,
    decimal? ExchangeRate,
    int? TaxId,
    List<CreateSalesInvoiceLineRequest> Items);
