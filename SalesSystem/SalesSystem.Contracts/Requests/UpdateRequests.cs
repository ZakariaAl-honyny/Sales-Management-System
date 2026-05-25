using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record UpdateCustomerPaymentRequest(
    int CustomerId,
    decimal Amount,
    PaymentType PaymentMethod,
    DateTime? PaymentDate,
    string? Notes = null
);

public record UpdateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    PaymentType PaymentMethod,
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
    string? Notes,
    string? SupplierInvoiceNo,
    List<CreatePurchaseInvoiceItemRequest> Items);

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
    List<CreateSalesInvoiceItemRequest> Items);