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
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    List<CreatePurchaseInvoiceItemRequest> Items);

public record UpdateSalesInvoiceRequest(
    int WarehouseId,
    int? CustomerId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    int? CashBoxId,
    string? Notes,
    int? CurrencyId,
    decimal? ExchangeRate,
    int? TaxId,
    List<CreateSalesInvoiceItemRequest> Items);
