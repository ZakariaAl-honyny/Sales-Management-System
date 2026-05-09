using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;
public record CreatePurchaseInvoiceRequest(
    int WarehouseId,
    int SupplierId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    string? Notes,
    List<CreatePurchaseInvoiceItemRequest> Items);
public record CreatePurchaseInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    string? Notes);
