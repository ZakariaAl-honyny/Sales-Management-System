using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    int? CashBoxId,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    string? Notes,
    string? SupplierInvoiceNo,
    List<CreatePurchaseInvoiceItemRequest> Items);
public record CreatePurchaseInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    SaleMode Mode,
    string? Notes);
