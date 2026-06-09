using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record UpdateCustomerPaymentRequest(
    int CustomerId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    string? Notes = null
);

public record UpdateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    string? Notes = null
);

public record UpdateStockTransferRequest(
    int FromWarehouseId,
    int ToWarehouseId,
    DateTime TransferDate,
    string? Notes,
    List<CreateStockTransferItemRequest> Items
);

/// <summary>
/// طلب تحديث فاتورة شراء — مع دعم العملات المتعددة والخصم والمرفقات والمصاريف الإضافية.
/// </summary>
public record UpdatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    int? CashBoxId,
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

public record UpdateSalesInvoiceRequest(
    int WarehouseId,
    int? CustomerId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    int? CashBoxId,
    string? Notes,
    int? QuotationId,
    int? CurrencyId,
    decimal? ExchangeRate,
    int? TaxId,
    List<CreateSalesInvoiceItemRequest> Items);
