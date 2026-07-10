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
/// طلب تحديث فاتورة شراء — مع دعم نوع الخصم.
/// </summary>
public record UpdatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    DateTime? InvoiceDate,
    PaymentType? PaymentType,
    decimal DiscountAmount,
    DiscountType? DiscountType,
    decimal? DiscountRate,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    string? Notes,
    int? TaxId,
    string? AttachmentPath,
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
    int? TaxId,
    List<CreateSalesInvoiceLineRequest> Items,
    DiscountType DiscountType = DiscountType.Amount,
    decimal? DiscountRate = null);
