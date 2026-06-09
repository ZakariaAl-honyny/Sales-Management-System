using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    int? InvoiceNo,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    int? CashBoxId,
    decimal DiscountAmount,
    byte? DiscountType,                          // NEW
    decimal? DiscountRate,                       // NEW
    decimal TaxAmount,
    decimal PaidAmount,
    int? CurrencyId,                             // NEW
    decimal? ExchangeRate,                       // NEW
    string? Notes,
    string? SupplierInvoiceNo,
    string? AttachmentBase64,                    // NEW
    string? AttachmentFileName,                  // NEW
    List<CreatePurchaseInvoiceItemRequest> Items,
    List<CreateAdditionalFeeRequest>? AdditionalFees = null);   // NEW

public record CreatePurchaseInvoiceItemRequest(
    int ProductId,
    int ProductUnitId,                           // NEW (required now)
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    byte? DiscountType,                          // NEW
    decimal? DiscountRate,                       // NEW
    SaleMode Mode,
    string? Notes);
